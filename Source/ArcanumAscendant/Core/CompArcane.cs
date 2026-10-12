using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArcanumAscendant
{
    public class CompProperties_Arcane : CompProperties
    {
        public CompProperties_Arcane()
        {
            compClass = typeof(CompArcane);
        }
    }

    /// <summary>
    /// Holds everything about a pawn's arcane progression: class, level, attributes, skill tree ranks,
    /// mana and ward. Injected onto every humanlike race at startup.
    /// </summary>
    public class CompArcane : ThingComp
    {
        public const int MaxLevel = 50;
        public const int AttributeBase = 3;
        public const int AttributeMax = 20;
        public const float ParagonXp = 6000f;

        public ArcaneClassDef classDef;
        public ArcaneClassDef subclassDef;
        public int level = 1;
        public float xp;
        public int paragon;
        public int perkPoints;
        public int attributePoints;
        public float mana;
        public float ward;
        public int respecsUsed;
        private bool rolledEnemyMage;

        private Dictionary<ArcaneAttributeDef, int> attributes = new Dictionary<ArcaneAttributeDef, int>();
        private Dictionary<ArcaneNodeDef, int> ranks = new Dictionary<ArcaneNodeDef, int>();
        private HashSet<AbilityDef> hiddenSpells = new HashSet<AbilityDef>();
        private HashSet<AbilityDef> autocastSpells = new HashSet<AbilityDef>();
        private Dictionary<string, int> passiveCooldowns = new Dictionary<string, int>();
        public int lastHarmTick = -99999;
        public int lastCombatTick = -99999;
        private int lastArcaneTick = -1;
        private int lastAiTick;

        // scribe helpers
        private List<ArcaneAttributeDef> tmpAttrKeys;
        private List<int> tmpAttrVals;
        private List<ArcaneNodeDef> tmpNodeKeys;
        private List<int> tmpNodeVals;
        private List<string> tmpCdKeys;
        private List<int> tmpCdVals;

        // caches
        [Unsaved] private bool cacheDirty = true;
        [Unsaved] private List<(ArcanePassive passive, int rank, ArcaneNodeDef node)> passiveCache = new List<(ArcanePassive, int, ArcaneNodeDef)>();
        [Unsaved] private Dictionary<AbilityDef, SpellModSummary> modCache = new Dictionary<AbilityDef, SpellModSummary>();
        [Unsaved] private HediffStage stageCache;
        [Unsaved] private float cachedManaMax = -1f;
        [Unsaved] private int cachedManaMaxTick = -1;
        [Unsaved] private float cachedWardMax = -1f;
        [Unsaved] private int cachedWardMaxTick = -1;
        [Unsaved] public static bool applyingSpellDamage;

        public Pawn Pawn => parent as Pawn;
        public bool IsMage => classDef != null;
        public ArcaneClassDef DisplayClass => subclassDef ?? classDef;
        public Color WardColor => DisplayClass?.color ?? new Color(0.85f, 0.8f, 0.5f);
        public IReadOnlyDictionary<ArcaneNodeDef, int> Ranks => ranks;

        #region Derived values

        public float ManaMax
        {
            get
            {
                int now = Find.TickManager?.TicksGame ?? 0;
                if (cachedManaMaxTick != now)
                {
                    cachedManaMax = Mathf.Max(10f, Pawn.GetStatValue(ArcaneDefOf.AA_ManaMax));
                    cachedManaMaxTick = now;
                }
                return cachedManaMax;
            }
        }

        public float WardMax
        {
            get
            {
                int now = Find.TickManager?.TicksGame ?? 0;
                if (cachedWardMaxTick != now)
                {
                    cachedWardMax = Mathf.Max(0f, Pawn.GetStatValue(ArcaneDefOf.AA_WardMax));
                    cachedWardMaxTick = now;
                }
                return cachedWardMax;
            }
        }

        public float ManaRegenPerSecond => Pawn.GetStatValue(ArcaneDefOf.AA_ManaRegen) * ArcanumMod.Settings.manaRegenMultiplier;

        public float LevelPowerFactor => (1f + 0.035f * (level - 1)) * (1f + 0.015f * paragon);

        /// <summary>Overall multiplier applied to spell damage, healing and shields.</summary>
        public float SpellPower
        {
            get
            {
                float p = Pawn.GetStatValue(ArcaneDefOf.AA_SpellPower) * LevelPowerFactor * ArcanumMod.Settings.spellPowerMultiplier;
                foreach (var (passive, rank, _) in Passives) p *= passive.SpellPowerFactor(this, rank);
                return p;
            }
        }

        public float XpToNextLevel => XpRequiredFor(level);

        public static float XpRequiredFor(int lvl)
        {
            if (lvl >= MaxLevel) return ParagonXp;
            return Mathf.Round(40f + 18f * Mathf.Pow(lvl, 1.45f));
        }

        public int Attribute(ArcaneAttributeDef def)
        {
            if (def == null) return 0;
            return attributes.TryGetValue(def, out int v) ? v : AttributeBase;
        }

        public int Rank(ArcaneNodeDef node) => node != null && ranks.TryGetValue(node, out int r) ? r : 0;

        public int SpentPerkPoints => ranks.Sum(kv => kv.Value * kv.Key.costPerRank);

        public List<(ArcanePassive passive, int rank, ArcaneNodeDef node)> Passives
        {
            get
            {
                if (cacheDirty) RebuildCaches();
                return passiveCache;
            }
        }

        public HediffStage Stage
        {
            get
            {
                if (cacheDirty || stageCache == null) RebuildCaches();
                return stageCache;
            }
        }

        public bool IsHidden(AbilityDef def) => hiddenSpells.Contains(def);
        public void SetHidden(AbilityDef def, bool hidden)
        {
            if (hidden) hiddenSpells.Add(def); else hiddenSpells.Remove(def);
        }

        public bool IsAutocast(AbilityDef def) => autocastSpells.Contains(def);
        public void SetAutocast(AbilityDef def, bool on)
        {
            if (on) autocastSpells.Add(def); else autocastSpells.Remove(def);
        }

        public SpellModSummary ModsFor(AbilityDef def)
        {
            if (cacheDirty) RebuildCaches();
            if (!modCache.TryGetValue(def, out var sum))
            {
                sum = new SpellModSummary();
                var ext = def.GetModExtension<SpellExtension>();
                foreach (var kv in ranks)
                {
                    if (kv.Value <= 0 || kv.Key.spellMods.NullOrEmpty() || !NodeInActiveTree(kv.Key)) continue;
                    foreach (var m in kv.Key.spellMods)
                        if (m.Matches(def, ext)) sum.Add(m, kv.Value);
                }
                foreach (var (passive, rank, _) in passiveCache) passive.ModifySpell(this, def, ext, sum, rank);
                modCache[def] = sum;
            }
            return sum;
        }

        public bool PassiveReady(string key) => !passiveCooldowns.TryGetValue(key, out int until) || Find.TickManager.TicksGame >= until;
        public void StartPassiveCooldown(string key, int ticks) => passiveCooldowns[key] = Find.TickManager.TicksGame + ticks;
        public int PassiveCooldownLeft(string key) => passiveCooldowns.TryGetValue(key, out int until) ? Mathf.Max(0, until - Find.TickManager.TicksGame) : 0;

        #endregion

        #region Progression

        public void Awaken(ArcaneClassDef def, bool silent = false)
        {
            if (def == null) return;
            if (def.IsSubclass) def = def.parentClass;
            classDef = def;
            subclassDef = null;
            level = 1;
            xp = 0;
            paragon = 0;
            perkPoints = 2;
            attributePoints = 4;
            attributes.Clear();
            ranks.Clear();
            hiddenSpells.Clear();
            autocastSpells.Clear();
            MarkDirty();
            EnsureCoreHediff();
            SyncAbilities();
            mana = ManaMax;
            ward = WardMax;
            if (!silent && Pawn.Spawned)
            {
                ArcaneDefOf.AA_Awaken?.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
                ArcaneFx.LevelUpBurst(Pawn, def.color, big: true);
                Messages.Message("AA_Awakened".Translate(Pawn.LabelShort, def.label, Pawn.Named("PAWN")), Pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        public void ChooseSubclass(ArcaneClassDef sub)
        {
            if (sub == null || sub.parentClass != classDef) return;
            subclassDef = sub;
            MarkDirty();
            SyncAbilities();
            if (Pawn.Spawned)
            {
                ArcaneDefOf.AA_Awaken?.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
                ArcaneFx.LevelUpBurst(Pawn, sub.color, big: true);
                Messages.Message("AA_SubclassChosen".Translate(Pawn.LabelShort, sub.label, Pawn.Named("PAWN")), Pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        public bool CanChooseSubclass => IsMage && subclassDef == null && level >= classDef.subclassUnlockLevel && classDef.subclasses.Count > 0;

        public void Unawaken()
        {
            RemoveAllArcaneAbilities();
            classDef = null;
            subclassDef = null;
            ranks.Clear();
            attributes.Clear();
            var h = Pawn.health?.hediffSet?.GetFirstHediffOfDef(ArcaneDefOf.AA_ArcaneCore);
            if (h != null) Pawn.health.RemoveHediff(h);
            MarkDirty();
        }

        public void GainXp(float amount, bool applyGainStat = true)
        {
            if (!IsMage || amount <= 0f || Pawn.Dead) return;
            if (applyGainStat) amount *= Pawn.GetStatValue(ArcaneDefOf.AA_XpGain) * ArcanumMod.Settings.xpMultiplier;
            xp += amount;
            int guard = 0;
            while (xp >= XpToNextLevel && guard++ < 200)
            {
                xp -= XpToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            if (level >= MaxLevel)
            {
                paragon++;
                perkPoints += paragon % 5 == 0 ? 1 : 0;
                MarkDirty();
                if (Pawn.Spawned && Pawn.Faction == Faction.OfPlayer)
                    MoteMaker.ThrowText(Pawn.DrawPos + new Vector3(0, 0, 0.6f), Pawn.Map, "AA_ParagonUp".Translate(paragon), DisplayClass.color, 3.5f);
                return;
            }
            level++;
            perkPoints += 1 + (level % 5 == 0 ? 1 : 0);
            attributePoints += 1;
            MarkDirty();
            mana = ManaMax;
            if (Pawn.Spawned)
            {
                ArcaneDefOf.AA_LevelUp?.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
                ArcaneFx.LevelUpBurst(Pawn, DisplayClass.color, big: false);
                MoteMaker.ThrowText(Pawn.DrawPos + new Vector3(0, 0, 0.6f), Pawn.Map, "AA_LevelUpMote".Translate(level), DisplayClass.color, 3.5f);
            }
            if (Pawn.Faction == Faction.OfPlayer)
            {
                if (level == classDef.subclassUnlockLevel && subclassDef == null)
                    Find.LetterStack.ReceiveLetter("AA_SubclassReadyLabel".Translate(Pawn.LabelShort, Pawn.Named("PAWN")),
                        "AA_SubclassReadyText".Translate(Pawn.LabelShort, classDef.label, Pawn.Named("PAWN")), LetterDefOf.PositiveEvent, Pawn);
                else if (level == MaxLevel)
                    Find.LetterStack.ReceiveLetter("AA_MaxLevelLabel".Translate(Pawn.LabelShort, Pawn.Named("PAWN")),
                        "AA_MaxLevelText".Translate(Pawn.LabelShort, Pawn.Named("PAWN")), LetterDefOf.PositiveEvent, Pawn);
            }
        }

        /// <summary>Directly set level (used for generated enemy mages and dev tools). Grants points.</summary>
        public void SetLevelDirect(int newLevel)
        {
            newLevel = Mathf.Clamp(newLevel, 1, MaxLevel);
            while (level < newLevel)
            {
                level++;
                perkPoints += 1 + (level % 5 == 0 ? 1 : 0);
                attributePoints += 1;
            }
            xp = 0;
            MarkDirty();
        }

        public bool CanRaiseAttribute(ArcaneAttributeDef def) => attributePoints > 0 && Attribute(def) < AttributeMax;

        public void RaiseAttribute(ArcaneAttributeDef def)
        {
            if (!CanRaiseAttribute(def)) return;
            attributes[def] = Attribute(def) + 1;
            attributePoints--;
            MarkDirty();
        }

        public bool NodeInActiveTree(ArcaneNodeDef node) => node.tree == classDef || (subclassDef != null && node.tree == subclassDef);

        public AcceptanceReport CanRankUp(ArcaneNodeDef node)
        {
            if (!IsMage) return false;
            if (!NodeInActiveTree(node))
                return node.tree.IsSubclass && subclassDef == null ? "AA_Reason_ChooseSubclass".Translate() : "AA_Reason_OtherTree".Translate();
            int r = Rank(node);
            if (r >= node.maxRank) return "AA_Reason_MaxRank".Translate();
            if (perkPoints < node.costPerRank) return "AA_Reason_NoPoints".Translate();
            if (level < node.requiredLevel) return "AA_Reason_Level".Translate(node.requiredLevel);
            if (node.requiredAttribute != null && Attribute(node.requiredAttribute) < node.requiredAttributeValue)
                return "AA_Reason_Attribute".Translate(node.requiredAttribute.label, node.requiredAttributeValue);
            if (!node.prerequisites.NullOrEmpty())
            {
                bool ok = node.requireAllPrerequisites ? node.prerequisites.All(p => Rank(p) > 0) : node.prerequisites.Any(p => Rank(p) > 0);
                if (!ok) return "AA_Reason_Prereq".Translate();
            }
            if (!node.exclusiveGroup.NullOrEmpty() && r == 0)
            {
                var other = ranks.FirstOrDefault(kv => kv.Value > 0 && kv.Key != node && kv.Key.exclusiveGroup == node.exclusiveGroup).Key;
                if (other != null) return "AA_Reason_Exclusive".Translate(other.label);
            }
            return true;
        }

        public void RankUp(ArcaneNodeDef node)
        {
            if (!CanRankUp(node).Accepted) return;
            ranks[node] = Rank(node) + 1;
            perkPoints -= node.costPerRank;
            MarkDirty();
            if (node.unlockSpell != null) SyncAbilities();
            foreach (var p in node.passives) p.Notify_Learned(this, Rank(node));
            if (Pawn.Spawned && Find.Selector.IsSelected(Pawn))
                ArcaneFx.NodeUnlockPulse(Pawn, node.tree.color);
        }

        /// <summary>Refund all nodes and attribute points. Keeps class, subclass and level.</summary>
        public void Respec()
        {
            perkPoints += SpentPerkPoints;
            ranks.Clear();
            foreach (var kv in attributes) attributePoints += kv.Value - AttributeBase;
            attributes.Clear();
            respecsUsed++;
            MarkDirty();
            SyncAbilities();
        }

        public void ForceRank(ArcaneNodeDef node, int rank)
        {
            if (rank <= 0) ranks.Remove(node);
            else ranks[node] = Mathf.Min(rank, node.maxRank);
            MarkDirty();
            SyncAbilities();
        }

        public void ForceAttribute(ArcaneAttributeDef def, int value)
        {
            attributes[def] = Mathf.Clamp(value, 0, AttributeMax);
            MarkDirty();
        }

        #endregion

        #region Abilities

        public IEnumerable<AbilityDef> KnownSpells()
        {
            if (!IsMage) yield break;
            var seen = new HashSet<AbilityDef>();
            foreach (var s in classDef.startingSpells) if (seen.Add(s)) yield return s;
            if (subclassDef != null)
                foreach (var s in subclassDef.startingSpells) if (seen.Add(s)) yield return s;
            foreach (var kv in ranks)
                if (kv.Value > 0 && kv.Key.unlockSpell != null && NodeInActiveTree(kv.Key) && seen.Add(kv.Key.unlockSpell))
                    yield return kv.Key.unlockSpell;
        }

        public void SyncAbilities()
        {
            var p = Pawn;
            if (p?.abilities == null) return;
            var known = KnownSpells().ToHashSet();
            foreach (var ab in p.abilities.abilities.ToList())
                if (ab.def.HasModExtension<SpellExtension>() && !known.Contains(ab.def))
                    p.abilities.RemoveAbility(ab.def);
            foreach (var def in known)
            {
                if (p.abilities.GetAbility(def) == null)
                {
                    p.abilities.GainAbility(def);
                    var ext = def.GetModExtension<SpellExtension>();
                    if (ext != null && ext.autocastByDefault && ArcanumMod.Settings.autocastNewSpells) autocastSpells.Add(def);
                }
            }
        }

        private void RemoveAllArcaneAbilities()
        {
            var p = Pawn;
            if (p?.abilities == null) return;
            foreach (var ab in p.abilities.abilities.ToList())
                if (ab.def.HasModExtension<SpellExtension>()) p.abilities.RemoveAbility(ab.def);
        }

        public IEnumerable<ArcaneAbility> ArcaneAbilities()
        {
            var p = Pawn;
            if (p?.abilities == null) yield break;
            foreach (var ab in p.abilities.abilities)
                if (ab is ArcaneAbility aa) yield return aa;
        }

        #endregion

        #region Caches & stats

        public void MarkDirty()
        {
            cacheDirty = true;
            modCache.Clear();
            cachedManaMaxTick = -1;
            cachedWardMaxTick = -1;
            var h = Pawn?.health?.hediffSet?.GetFirstHediffOfDef(ArcaneDefOf.AA_ArcaneCore);
            if (h != null)
            {
                RebuildCaches();
                Pawn.health.Notify_HediffChanged(h);
            }
        }

        private void RebuildCaches()
        {
            cacheDirty = false;
            modCache.Clear();
            passiveCache.Clear();
            var offsets = new Dictionary<StatDef, float>();
            var percents = new Dictionary<StatDef, float>();
            var caps = new Dictionary<PawnCapacityDef, float>();
            void AddOff(StatDef s, float v) { if (s != null) offsets[s] = (offsets.TryGetValue(s, out float c) ? c : 0f) + v; }
            void AddPct(StatDef s, float v) { if (s != null) percents[s] = (percents.TryGetValue(s, out float c) ? c : 0f) + v; }

            if (IsMage)
            {
                foreach (var attr in DefDatabase<ArcaneAttributeDef>.AllDefsListForReading)
                {
                    int pts = Attribute(attr);
                    foreach (var sm in attr.statOffsetsPerPoint) AddOff(sm.stat, sm.value * pts);
                }
                AddOff(ArcaneDefOf.AA_ManaMax, 3f * (level - 1) + 4f * paragon);
                AddOff(ArcaneDefOf.AA_WardMax, 1.5f * (level - 1));
                foreach (var sm in classDef.innateStatOffsets) AddOff(sm.stat, sm.value);
                if (subclassDef != null) foreach (var sm in subclassDef.innateStatOffsets) AddOff(sm.stat, sm.value);
                foreach (var kv in ranks)
                {
                    if (kv.Value <= 0 || !NodeInActiveTree(kv.Key)) continue;
                    var node = kv.Key;
                    foreach (var sm in node.statOffsetsPerRank) AddOff(sm.stat, sm.value * kv.Value);
                    foreach (var sm in node.statPercentPerRank) AddPct(sm.stat, sm.value * kv.Value);
                    foreach (var co in node.capacityOffsetsPerRank)
                        if (co.capacity != null) caps[co.capacity] = (caps.TryGetValue(co.capacity, out float c) ? c : 0f) + co.offset * kv.Value;
                    foreach (var p in node.passives) passiveCache.Add((p, kv.Value, node));
                }
                foreach (var (passive, rank, _) in passiveCache) passive.ModifyStats(this, rank, AddOff, AddPct);
            }

            stageCache = new HediffStage
            {
                becomeVisible = true,
                statOffsets = offsets.Where(kv => Mathf.Abs(kv.Value) > 0.0001f).Select(kv => new StatModifier { stat = kv.Key, value = kv.Value }).ToList(),
                statFactors = percents.Where(kv => Mathf.Abs(kv.Value) > 0.0001f).Select(kv => new StatModifier { stat = kv.Key, value = Mathf.Max(0.05f, 1f + kv.Value) }).ToList(),
                capMods = caps.Select(kv => new PawnCapacityModifier { capacity = kv.Key, offset = kv.Value }).ToList(),
            };
        }

        public void EnsureCoreHediff()
        {
            var p = Pawn;
            if (p?.health == null || !IsMage || p.Dead) return;
            if (!p.health.hediffSet.HasHediff(ArcaneDefOf.AA_ArcaneCore))
                p.health.AddHediff(ArcaneDefOf.AA_ArcaneCore);
        }

        #endregion

        #region Ticking

        public override void CompTick()
        {
            base.CompTick();
            ArcaneTick();
        }

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            ArcaneTick();
        }

        private void ArcaneTick()
        {
            if (!IsMage)
            {
                if (ward > 0f && Pawn != null && Pawn.IsHashIntervalTick(60))
                    ward = Mathf.Max(0f, ward - (1f + ward * 0.02f));
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (lastArcaneTick < 0) lastArcaneTick = now;
            int elapsed = now - lastArcaneTick;
            if (elapsed < 15) return;
            lastArcaneTick = now;
            elapsed = Mathf.Min(elapsed, 250);
            var p = Pawn;
            if (p.Dead) return;
            float seconds = elapsed / 60f;

            float max = ManaMax;
            if (mana < max) mana = Mathf.Min(max, mana + ManaRegenPerSecond * seconds);
            else if (mana > max) mana = max;

            float wmax = WardMax;
            if (ward > wmax) ward = Mathf.Max(wmax, ward - (0.5f + (ward - wmax) * 0.04f) * seconds);
            else if (ward < wmax && now - lastHarmTick > 300)
                ward = Mathf.Min(wmax, ward + p.GetStatValue(ArcaneDefOf.AA_WardRegen) * seconds);

            if (!p.Spawned) return;
            foreach (var (passive, rank, node) in Passives) passive.Tick(this, rank, elapsed);

            if (now - lastAiTick >= 40)
            {
                lastAiTick = now;
                ArcaneAI.TryAutocast(this);
            }
        }

        public void Notify_CombatActivity() => lastCombatTick = Find.TickManager.TicksGame;

        #endregion

        #region Combat hooks

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (Pawn == null || Pawn.Dead) return;
            if (dinfo.Def == null || !dinfo.Def.harmsHealth) return;
            if (!IsMage && ward <= 0.5f) return;
            lastHarmTick = Find.TickManager.TicksGame;
            if (IsMage)
            {
                Notify_CombatActivity();
                foreach (var (passive, rank, _) in Passives)
                {
                    passive.PreDamageTaken(this, rank, ref dinfo, ref absorbed);
                    if (absorbed) return;
                }
            }

            if (ward > 0.5f && dinfo.Amount > 0f)
            {
                float amt = dinfo.Amount;
                if (ward >= amt)
                {
                    ward -= amt;
                    absorbed = true;
                    ArcaneFx.WardHit(Pawn, WardColor, dinfo, broke: false);
                    return;
                }
                dinfo.SetAmount(amt - ward);
                ward = 0f;
                ArcaneFx.WardHit(Pawn, WardColor, dinfo, broke: true);
            }
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (!IsMage || Pawn.Dead) return;
            foreach (var (passive, rank, _) in Passives) passive.PostDamageTaken(this, rank, dinfo, totalDamageDealt);
        }

        /// <summary>Called for weapon (non-spell) damage dealt by this pawn.</summary>
        public void Notify_DealtWeaponDamage(Thing victim, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            if (applyingSpellDamage || !IsMage) return;
            Notify_CombatActivity();
            foreach (var (passive, rank, _) in Passives) passive.WeaponHit(this, rank, victim, dinfo, result);
        }

        public void Notify_KilledByWeapon(Pawn victim)
        {
            if (applyingSpellDamage || !IsMage) return;
            Notify_Kill(victim, null);
        }

        public void Notify_Kill(Pawn victim, SpellContext ctx)
        {
            if (!IsMage || victim == null) return;
            float cp = victim.kindDef?.combatPower ?? 40f;
            if (victim.Faction != null && Pawn.Faction != null && !victim.Faction.HostileTo(Pawn.Faction)) cp *= 0.2f;
            GainXp(Mathf.Clamp(cp, 10f, 2000f) * 0.8f);
            foreach (var (passive, rank, _) in Passives) passive.Kill(this, rank, victim, ctx);
        }

        public void Notify_SpellCast(SpellContext ctx)
        {
            Notify_CombatActivity();
            foreach (var (passive, rank, _) in Passives) passive.SpellCast(this, rank, ctx);
        }

        public void Notify_SpellHit(SpellContext ctx, Pawn target, float damage)
        {
            if (damage > 0f) GainXp(damage * 0.15f);
            foreach (var (passive, rank, _) in Passives) passive.SpellHit(this, rank, ctx, target, damage);
        }

        #endregion

        #region Lifecycle & UI

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && !rolledEnemyMage && !IsMage)
            {
                rolledEnemyMage = true;
                TryRollEnemyMage();
            }
            if (IsMage)
            {
                EnsureCoreHediff();
                if (respawningAfterLoad) SyncAbilities();
            }
        }

        private void TryRollEnemyMage()
        {
            var p = Pawn;
            if (p?.Faction == null || p.Faction == Faction.OfPlayer || !p.RaceProps.Humanlike) return;
            if (!p.Faction.HostileTo(Faction.OfPlayer)) return;
            var s = ArcanumMod.Settings;
            if (!Rand.Chance(s.enemyMageChance)) return;
            var cls = DefDatabase<ArcaneClassDef>.AllDefsListForReading.Where(d => !d.IsSubclass).RandomElementWithFallback();
            if (cls == null) return;
            Awaken(cls, true);
            float days = GenDate.DaysPassedSinceSettleFloat;
            int lvl = Mathf.RoundToInt((3f + days / 5f) * s.enemyLevelScale + Rand.Range(-3f, 3f));
            if (p.kindDef != null && p.kindDef.combatPower > 150f) lvl += 4;
            SetLevelDirect(Mathf.Clamp(lvl, 1, MaxLevel));
            ArcaneUtility.AutoAllocate(this);
            mana = ManaMax;
            ward = WardMax;
        }

        [Unsaved] private int lastDownedTick = -1;

        public override void Notify_Downed()
        {
            base.Notify_Downed();
            HandleDowned();
        }

        public void HandleDowned()
        {
            if (!IsMage) return;
            int now = Find.TickManager.TicksGame;
            if (lastDownedTick == now) return;
            lastDownedTick = now;
            foreach (var (passive, rank, _) in Passives) passive.Downed(this, rank);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
            var p = Pawn;
            if (p == null || p.Dead) yield break;
            bool controllable = p.Faction == Faction.OfPlayer && (p.IsColonistPlayerControlled || p.IsColonist);
            if (IsMage)
            {
                if (controllable || DebugSettings.ShowDevGizmos)
                    yield return new Gizmo_ArcaneStatus(this);
            }
            else if (controllable && p.RaceProps.Humanlike && ArcanumMod.Settings.freeAwakening)
            {
                yield return new Command_Action
                {
                    defaultLabel = "AA_AwakenCommand".Translate(),
                    defaultDesc = "AA_AwakenCommandDesc".Translate(),
                    icon = ArcaneTex.AwakenIcon,
                    action = () => Find.WindowStack.Add(new Dialog_ChooseClass(this)),
                };
            }
            if (DebugSettings.ShowDevGizmos && p.RaceProps.Humanlike)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Arcanum",
                    defaultDesc = "Developer tools for Arcanum Ascendant.",
                    icon = ArcaneTex.AwakenIcon,
                    action = () => Find.WindowStack.Add(new FloatMenu(ArcaneDebug.Options(this).ToList())),
                };
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!IsMage) return null;
            var sb = new StringBuilder();
            sb.Append("AA_InspectLine".Translate(DisplayClass.LabelCap, level, mana.ToString("F0"), ManaMax.ToString("F0")));
            if (WardMax > 0.5f) sb.Append(" · " + "AA_WardShort".Translate(ward.ToString("F0"), WardMax.ToString("F0")));
            return sb.ToString();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref classDef, "classDef");
            Scribe_Defs.Look(ref subclassDef, "subclassDef");
            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref xp, "xp");
            Scribe_Values.Look(ref paragon, "paragon");
            Scribe_Values.Look(ref perkPoints, "perkPoints");
            Scribe_Values.Look(ref attributePoints, "attributePoints");
            Scribe_Values.Look(ref mana, "mana");
            Scribe_Values.Look(ref ward, "ward");
            Scribe_Values.Look(ref respecsUsed, "respecsUsed");
            Scribe_Values.Look(ref rolledEnemyMage, "rolledEnemyMage");
            Scribe_Values.Look(ref lastHarmTick, "lastHarmTick", -99999);
            Scribe_Collections.Look(ref attributes, "attributes", LookMode.Def, LookMode.Value, ref tmpAttrKeys, ref tmpAttrVals);
            Scribe_Collections.Look(ref ranks, "ranks", LookMode.Def, LookMode.Value, ref tmpNodeKeys, ref tmpNodeVals);
            Scribe_Collections.Look(ref hiddenSpells, "hiddenSpells", LookMode.Def);
            Scribe_Collections.Look(ref autocastSpells, "autocastSpells", LookMode.Def);
            Scribe_Collections.Look(ref passiveCooldowns, "passiveCooldowns", LookMode.Value, LookMode.Value, ref tmpCdKeys, ref tmpCdVals);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                attributes ??= new Dictionary<ArcaneAttributeDef, int>();
                ranks ??= new Dictionary<ArcaneNodeDef, int>();
                hiddenSpells ??= new HashSet<AbilityDef>();
                autocastSpells ??= new HashSet<AbilityDef>();
                passiveCooldowns ??= new Dictionary<string, int>();
                attributes.RemoveAll(kv => kv.Key == null);
                ranks.RemoveAll(kv => kv.Key == null);
                hiddenSpells.RemoveWhere(d => d == null);
                autocastSpells.RemoveWhere(d => d == null);
                if (subclassDef != null && subclassDef.parentClass != classDef) subclassDef = null;
                cacheDirty = true;
            }
        }

        #endregion
    }
}

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
        public const int MaxLevel = 30;
        public const int MaxCircle = 5;
        public const int AttributeBase = 3;
        public const int AttributeMax = 20;
        public const float ParagonXp = 20000f;
        public const int ProgressionVersion = 2;
        /// <summary>Highest level reachable in each circle. Going past it needs an ascension sigil.</summary>
        public static readonly int[] CircleLevelCap = { 0, 6, 12, 18, 24, 30 };
        /// <summary>Hours of study at a lectern needed to finish attuning, for an average pawn.</summary>
        public const float AttunementHours = 16f;
        public const float StudyXpPerHour = 60f;

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

        /// <summary>Arcane circle (1-5). Each circle caps the level until the mage ascends with a sigil.</summary>
        public int circle = 1;
        /// <summary>True between reading a grimoire and finishing attunement.</summary>
        public bool attuning;
        /// <summary>Attunement progress, 0 to 1.</summary>
        public float attunement;
        /// <summary>Attunement is complete: the pawn can choose a class.</summary>
        public bool awakenReady;
        private bool ascendNotified;
        private int progressionVersion = ProgressionVersion;
        [Unsaved] private bool migratedThisLoad;

        /// <summary>Mastery points per spell. See <see cref="Mastery"/>.</summary>
        private Dictionary<AbilityDef, int> mastery = new Dictionary<AbilityDef, int>();
        private List<AbilityDef> tmpMasteryKeys;
        private List<int> tmpMasteryVals;
        /// <summary>When on, spells can be cast without enough mana by paying the difference in strain.</summary>
        public bool overchannel;
        public float strain;
        public FamiliarDef familiar;
        public int familiarCooldown;

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

        public float LevelPowerFactor => (1f + 0.06f * (level - 1)) * (1f + 0.01f * paragon);

        public int LevelCap => CircleLevelCap[Mathf.Clamp(circle, 1, MaxCircle)];

        /// <summary>The mage has hit their circle's level cap and needs to ascend before levelling further.</summary>
        public bool AtCircleCap => IsMage && circle < MaxCircle && level >= LevelCap;

        public bool ReadyToAscend => AtCircleCap && xp >= XpToNextLevel - 0.01f;

        public static int CircleForLevel(int lvl)
        {
            for (int c = 1; c <= MaxCircle; c++)
                if (lvl <= CircleLevelCap[c]) return c;
            return MaxCircle;
        }

        public static int PerkPointsFor(int lvl, int circ) => 2 + (lvl - 1) + lvl / 3 + 3 * (circ - 1);
        public static int AttributePointsFor(int lvl, int circ) => 3 + (lvl - 1) + (circ - 1);

        /// <summary>How quickly this pawn studies and attunes. Psychic sensitivity and intellect both help.</summary>
        public static float StudyFactor(Pawn p)
        {
            if (p == null) return 0f;
            float sens = Mathf.Clamp(p.GetStatValue(StatDefOf.PsychicSensitivity), 0f, 2.5f);
            int intellect = p.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 4;
            return sens * (0.75f + 0.025f * intellect);
        }

        public static AcceptanceReport CanAttune(Pawn p)
        {
            if (p == null || !p.RaceProps.Humanlike) return "AA_CannotAwaken".Translate();
            if (p.GetStatValue(StatDefOf.PsychicSensitivity) <= 0.05f) return "AA_PsychicallyDeaf".Translate(p.LabelShort, p.Named("PAWN"));
            return true;
        }

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
            return Mathf.Round(100f + 140f * Mathf.Pow(lvl, 1.5f));
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
                Mastery.Apply(MasteryRank(def), sum);
                modCache[def] = sum;
            }
            return sum;
        }

        public int MasteryPoints(AbilityDef def) => def != null && mastery.TryGetValue(def, out int v) ? v : 0;
        public int MasteryRank(AbilityDef def) => Mastery.RankFor(MasteryPoints(def));

        public void GainMastery(AbilityDef def, int points)
        {
            if (def == null || points <= 0 || !IsMage) return;
            int before = MasteryRank(def);
            mastery[def] = MasteryPoints(def) + points;
            int after = MasteryRank(def);
            if (after > before)
            {
                modCache.Remove(def);
                if (Pawn.Faction == Faction.OfPlayer) Mastery.RankUpFx(Pawn, def, after);
            }
        }

        public void Notify_Reaction(ArcaneReactionDef r)
        {
            if (Pawn?.Faction == Faction.OfPlayer) Current.Game?.GetComponent<GameComponent_Arcanum>()?.Discover(r, Pawn);
        }

        public bool BurnedOut => ArcaneDefOf.AA_ArcaneBurnout != null && Pawn?.health?.hediffSet?.HasHediff(ArcaneDefOf.AA_ArcaneBurnout) == true;

        /// <summary>Pays for a spell with strain instead of missing mana. Returns false if overchanneling is not possible.</summary>
        public bool CanOverchannel => IsMage && overchannel && !BurnedOut;

        public void AddStrain(float amount)
        {
            if (amount <= 0f) return;
            strain += amount * ArcaneStrain.StrainPerMana;
            UpdateStrainHediff();
            if (strain >= ArcaneStrain.BurnoutThreshold) ArcaneStrain.Burnout(this);
            else if (Rand.Chance(ArcaneStrain.SurgeChance(strain))) ArcaneStrain.WildSurge(this);
        }

        private void UpdateStrainHediff()
        {
            var def = ArcaneDefOf.AA_ArcaneStrain;
            var p = Pawn;
            if (def == null || p?.health == null || p.Dead) return;
            var h = p.health.hediffSet.GetFirstHediffOfDef(def);
            if (strain < 1f)
            {
                if (h != null) p.health.RemoveHediff(h);
                return;
            }
            if (h == null)
            {
                h = HediffMaker.MakeHediff(def, p);
                p.health.AddHediff(h);
            }
            h.Severity = Mathf.Clamp(strain / 100f, 0.01f, 1.2f);
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
            circle = 1;
            xp = 0;
            paragon = 0;
            perkPoints = PerkPointsFor(1, 1);
            attributePoints = AttributePointsFor(1, 1);
            attuning = false;
            attunement = 1f;
            awakenReady = false;
            ascendNotified = false;
            progressionVersion = ProgressionVersion;
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

        public bool CanChooseSubclass => IsMage && subclassDef == null && circle >= 2 && level >= classDef.subclassUnlockLevel && classDef.subclasses.Count > 0;

        #region Attunement & ascension

        public void StartAttunement()
        {
            if (IsMage || attuning || awakenReady) return;
            attuning = true;
            attunement = 0f;
        }

        /// <summary>Adds attunement progress (0-1 scale). Completing it lets the pawn choose a class.</summary>
        public void AddAttunement(float amount)
        {
            if (!attuning || amount <= 0f) return;
            attunement = Mathf.Min(1f, attunement + amount);
            if (attunement >= 1f) CompleteAttunement();
        }

        public void CompleteAttunement(bool silent = false)
        {
            attuning = false;
            attunement = 1f;
            awakenReady = true;
            var p = Pawn;
            if (p == null || silent) return;
            if (p.Faction != Faction.OfPlayer)
            {
                Awaken(DefDatabase<ArcaneClassDef>.AllDefs.Where(d => !d.IsSubclass).RandomElement(), true);
                return;
            }
            if (p.Spawned)
            {
                ArcaneDefOf.AA_Awaken?.PlayOneShot(new TargetInfo(p.Position, p.Map));
                ArcaneFx.LevelUpBurst(p, new Color(0.75f, 0.55f, 1f), big: true);
            }
            Find.LetterStack.ReceiveLetter("AA_AttunedLabel".Translate(p.LabelShort, p.Named("PAWN")),
                "AA_AttunedText".Translate(p.LabelShort, p.Named("PAWN")), LetterDefOf.PositiveEvent, p);
        }

        /// <summary>Advance to the next circle. Called by ascension sigils.</summary>
        public void Ascend()
        {
            if (!IsMage || circle >= MaxCircle) return;
            circle++;
            perkPoints += 3;
            attributePoints += 1;
            ascendNotified = false;
            MarkDirty();
            mana = ManaMax;
            ward = Mathf.Max(ward, WardMax);
            var p = Pawn;
            if (p.Spawned)
            {
                ArcaneDefOf.AA_Awaken?.PlayOneShot(new TargetInfo(p.Position, p.Map));
                ArcaneFx.LevelUpBurst(p, DisplayClass.color, big: true);
                ArcaneFx.Shake(p.Map, 1.5f);
                MoteMaker.ThrowText(p.DrawPos + new Vector3(0, 0, 0.8f), p.Map, "AA_AscendedMote".Translate(CircleLabel(circle)), DisplayClass.color, 5f);
            }
            if (p.Faction == Faction.OfPlayer)
            {
                string text = "AA_AscendedText".Translate(p.LabelShort, CircleLabel(circle), LevelCap, p.Named("PAWN"));
                Find.LetterStack.ReceiveLetter("AA_AscendedLabel".Translate(p.LabelShort, CircleLabel(circle), p.Named("PAWN")), text, LetterDefOf.PositiveEvent, p);
            }
            ProcessLevelUps();
        }

        public static string CircleLabel(int c) => ("AA_Circle" + Mathf.Clamp(c, 1, MaxCircle)).Translate();

        #endregion

        public void Unawaken()
        {
            RemoveAllArcaneAbilities();
            classDef = null;
            subclassDef = null;
            ranks.Clear();
            attributes.Clear();
            mastery.Clear();
            familiar = null;
            var h = Pawn.health?.hediffSet?.GetFirstHediffOfDef(ArcaneDefOf.AA_ArcaneCore);
            if (h != null) Pawn.health.RemoveHediff(h);
            MarkDirty();
        }

        public void GainXp(float amount, bool applyGainStat = true)
        {
            if (!IsMage || amount <= 0f || Pawn.Dead) return;
            if (applyGainStat) amount *= Pawn.GetStatValue(ArcaneDefOf.AA_XpGain) * ArcanumMod.Settings.xpMultiplier;
            xp += amount;
            ProcessLevelUps();
        }

        private void ProcessLevelUps()
        {
            int guard = 0;
            while (xp >= XpToNextLevel && guard++ < 200)
            {
                if (AtCircleCap)
                {
                    xp = XpToNextLevel;
                    if (!ascendNotified)
                    {
                        ascendNotified = true;
                        if (Pawn.Faction == Faction.OfPlayer)
                            Find.LetterStack.ReceiveLetter("AA_AscendReadyLabel".Translate(Pawn.LabelShort, Pawn.Named("PAWN")),
                                "AA_AscendReadyText".Translate(Pawn.LabelShort, CircleLabel(circle + 1), SigilFor(circle + 1)?.label ?? "?", Pawn.Named("PAWN")),
                                LetterDefOf.PositiveEvent, Pawn);
                    }
                    return;
                }
                xp -= XpToNextLevel;
                LevelUp();
            }
        }

        // ThingDef names may not end in a digit, so the sigils are named by ordinal.
        private static readonly string[] SigilNames = { null, null, "AA_Sigil_SecondCircle", "AA_Sigil_ThirdCircle", "AA_Sigil_FourthCircle", "AA_Sigil_FifthCircle" };

        public static ThingDef SigilFor(int targetCircle) =>
            targetCircle >= 2 && targetCircle < SigilNames.Length ? DefDatabase<ThingDef>.GetNamedSilentFail(SigilNames[targetCircle]) : null;

        private void LevelUp()
        {
            if (level >= MaxLevel)
            {
                paragon++;
                perkPoints += paragon % 2 == 0 ? 1 : 0;
                MarkDirty();
                if (Pawn.Spawned && Pawn.Faction == Faction.OfPlayer)
                    MoteMaker.ThrowText(Pawn.DrawPos + new Vector3(0, 0, 0.6f), Pawn.Map, "AA_ParagonUp".Translate(paragon), DisplayClass.color, 3.5f);
                return;
            }
            level++;
            perkPoints += 1 + (level % 3 == 0 ? 1 : 0);
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
                if (level == classDef.subclassUnlockLevel && subclassDef == null && circle >= 2)
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
            while (circle < MaxCircle && CircleLevelCap[circle] < newLevel)
            {
                circle++;
                perkPoints += 3;
                attributePoints += 1;
            }
            while (level < newLevel)
            {
                level++;
                perkPoints += 1 + (level % 3 == 0 ? 1 : 0);
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
                AddOff(ArcaneDefOf.AA_ManaMax, 5f * (level - 1) + 10f * (circle - 1) + 3f * paragon);
                AddOff(ArcaneDefOf.AA_WardMax, 2.5f * (level - 1) + 5f * (circle - 1));
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
                if (familiar != null)
                    foreach (var sm in familiar.statOffsets) AddOff(sm.stat, sm.value);
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
            if (migratedThisLoad)
            {
                migratedThisLoad = false;
                if (Pawn.Faction == Faction.OfPlayer)
                    Messages.Message("AA_Migrated".Translate(Pawn.LabelShort, level, CircleLabel(circle), Pawn.Named("PAWN")), Pawn, MessageTypeDefOf.NeutralEvent);
                SyncAbilities();
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
            float regen = ManaRegenPerSecond;
            if (p.Spawned)
            {
                var font = ArcaneVfxManager.For(p.Map)?.FontNear(p.Position);
                if (font != null) regen += font.Props.manaPerSecond;
            }
            if (BurnedOut) regen = 0f;
            if (mana < max) mana = Mathf.Min(max, mana + regen * seconds);
            else if (mana > max) mana = max;
            if (strain > 0f)
            {
                bool calm = now - lastCombatTick > 900;
                strain = Mathf.Max(0f, strain - (calm ? 1.2f : 0.4f) * seconds);
                if (p.IsHashIntervalTick(60) || strain <= 0f) UpdateStrainHediff();
            }
            if (familiar != null) Familiars.Tick(this, elapsed);

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
            if (HediffComp_DamageShell.Apply(Pawn, ref dinfo))
            {
                absorbed = true;
                return;
            }
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
            if (!victim.HostileTo(Pawn)) cp *= 0.1f;
            GainXp(Mathf.Clamp(cp, 4f, 2000f));
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
                if (familiar != null) ArcaneVfxManager.For(Pawn.Map)?.RegisterFamiliar(this);
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
            int lvl = Mathf.RoundToInt((2f + days / 6f) * s.enemyLevelScale + Rand.Range(-2f, 2f));
            if (p.kindDef != null && p.kindDef.combatPower > 150f) lvl += 3;
            SetLevelDirect(Mathf.Clamp(lvl, 1, MaxLevel));
            ArcaneUtility.AutoAllocate(this);
            mana = ManaMax;
            ward = WardMax;
        }

        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            base.Notify_Killed(prevMap, dinfo);
            var p = Pawn;
            if (!IsMage || prevMap == null || p.Faction == Faction.OfPlayer || ArcaneDefOf.AA_ArcaneCrystal == null) return;
            if (p.Faction != null && !p.Faction.HostileTo(Faction.OfPlayer)) return;
            int count = Rand.RangeInclusive(1, 2) + circle * 2;
            var cell = p.PositionHeld;
            if (!cell.IsValid) return;
            var crystals = ThingMaker.MakeThing(ArcaneDefOf.AA_ArcaneCrystal);
            crystals.stackCount = count;
            GenPlace.TryPlaceThing(crystals, cell, prevMap, ThingPlaceMode.Near);
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
                {
                    yield return new Gizmo_ArcaneStatus(this);
                    yield return new Command_Toggle
                    {
                        defaultLabel = "AA_Overchannel".Translate(),
                        defaultDesc = "AA_OverchannelDesc".Translate(strain.ToString("F0"), ArcaneStrain.BurnoutThreshold.ToString("F0")),
                        icon = ArcaneTex.OverchannelIcon,
                        isActive = () => overchannel,
                        toggleAction = () => overchannel = !overchannel,
                    };
                }
            }
            else if (controllable && p.RaceProps.Humanlike && (awakenReady || ArcanumMod.Settings.freeAwakening))
            {
                yield return new Command_Action
                {
                    defaultLabel = "AA_AwakenCommand".Translate(),
                    defaultDesc = (awakenReady ? "AA_AwakenReadyDesc" : "AA_AwakenCommandDesc").Translate(),
                    icon = ArcaneTex.AwakenIcon,
                    action = () => Find.WindowStack.Add(new Dialog_ChooseClass(this)),
                };
            }
            else if (controllable && attuning)
            {
                yield return new Command_Action
                {
                    defaultLabel = "AA_AttuningCommand".Translate(attunement.ToStringPercent("F0")),
                    defaultDesc = "AA_AttuningCommandDesc".Translate(p.LabelShort, attunement.ToStringPercent("F0"), StudyFactor(p).ToStringPercent(), p.Named("PAWN")),
                    icon = ArcaneTex.AwakenIcon,
                    action = () => { },
                    Disabled = true,
                    disabledReason = "AA_AttuningCommandDesc".Translate(p.LabelShort, attunement.ToStringPercent("F0"), StudyFactor(p).ToStringPercent(), p.Named("PAWN")),
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
            if (!IsMage)
            {
                if (attuning) return "AA_AttuningInspect".Translate(attunement.ToStringPercent("F0"));
                if (awakenReady) return "AA_AwakenReadyInspect".Translate();
                return null;
            }
            var sb = new StringBuilder();
            sb.Append("AA_InspectLine".Translate(DisplayClass.LabelCap, level, mana.ToString("F0"), ManaMax.ToString("F0")));
            if (ReadyToAscend) sb.Append(" · " + "AA_ReadyToAscendShort".Translate());
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
            Scribe_Values.Look(ref circle, "circle", 1);
            Scribe_Values.Look(ref attuning, "attuning");
            Scribe_Values.Look(ref attunement, "attunement");
            Scribe_Values.Look(ref awakenReady, "awakenReady");
            Scribe_Values.Look(ref ascendNotified, "ascendNotified");
            Scribe_Values.Look(ref progressionVersion, "progressionVersion", 1);
            Scribe_Collections.Look(ref mastery, "mastery", LookMode.Def, LookMode.Value, ref tmpMasteryKeys, ref tmpMasteryVals);
            Scribe_Values.Look(ref overchannel, "overchannel");
            Scribe_Values.Look(ref strain, "strain");
            Scribe_Defs.Look(ref familiar, "familiar");
            Scribe_Values.Look(ref familiarCooldown, "familiarCooldown");
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
                mastery ??= new Dictionary<AbilityDef, int>();
                mastery.RemoveAll(kv => kv.Key == null);
                attributes.RemoveAll(kv => kv.Key == null);
                ranks.RemoveAll(kv => kv.Key == null);
                hiddenSpells.RemoveWhere(d => d == null);
                autocastSpells.RemoveWhere(d => d == null);
                if (subclassDef != null && subclassDef.parentClass != classDef) subclassDef = null;
                if (progressionVersion < ProgressionVersion) MigrateProgression();
                cacheDirty = true;
            }
        }

        /// <summary>
        /// Saves from before the circle system used a 50-level cap and a faster point curve. Rescale the level,
        /// keep the class and subclass, and refund every point under the new budget.
        /// </summary>
        private void MigrateProgression()
        {
            progressionVersion = ProgressionVersion;
            if (!IsMage) return;
            int newLevel = Mathf.Clamp(Mathf.CeilToInt(level * 0.6f), 1, MaxLevel);
            int newCircle = CircleForLevel(newLevel);
            if (subclassDef != null && newCircle < 2)
            {
                newCircle = 2;
                newLevel = Mathf.Max(newLevel, CircleLevelCap[1] + 1);
            }
            level = newLevel;
            circle = newCircle;
            xp = 0f;
            paragon = 0;
            ranks.Clear();
            attributes.Clear();
            perkPoints = PerkPointsFor(level, circle);
            attributePoints = AttributePointsFor(level, circle);
            migratedThisLoad = true;
        }

        #endregion
    }
}

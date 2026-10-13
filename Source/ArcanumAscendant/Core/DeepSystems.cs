using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Spell mastery
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Every spell has its own mastery. Casting it earns mastery points (more for spells with long cooldowns); each rank
    /// makes that spell stronger, cheaper and faster, and Grandmaster adds an extra target and more reach.
    /// </summary>
    public static class Mastery
    {
        public static readonly int[] Thresholds = { 0, 20, 60, 140, 280, 500 };
        public const int MaxRank = 5;

        public static int RankFor(int points)
        {
            int r = 0;
            for (int i = 1; i <= MaxRank; i++)
                if (points >= Thresholds[i]) r = i;
            return r;
        }

        public static int PointsPerCast(AbilityDef def)
        {
            float cdSeconds = def.cooldownTicksRange.max / 60f;
            var ext = def.GetModExtension<SpellExtension>();
            float p = Mathf.Clamp(cdSeconds / 10f, 1f, 8f);
            if (ext != null && ext.ultimate) p = Mathf.Max(p, 6f);
            return Mathf.RoundToInt(p);
        }

        public static string RankLabel(int rank) => ("AA_MasteryRank" + Mathf.Clamp(rank, 0, MaxRank)).Translate();

        public static void Apply(int rank, SpellModSummary sum)
        {
            if (rank <= 0) return;
            sum.damage += 0.04f * rank;
            sum.healing += 0.04f * rank;
            sum.manaCost -= 0.03f * rank;
            sum.cooldown -= 0.03f * rank;
            if (rank >= 3)
            {
                sum.radius += 0.1f;
                sum.duration += 0.1f;
            }
            if (rank >= MaxRank)
            {
                sum.extraTargets += 1;
                sum.critChance += 0.1f;
                sum.radius += 0.05f;
                sum.duration += 0.05f;
            }
        }

        public static string BonusText(int rank)
        {
            if (rank <= 0) return "AA_MasteryNone".Translate();
            string s = "AA_MasteryBonus".Translate((0.04f * rank).ToStringPercent(), (0.03f * rank).ToStringPercent());
            if (rank >= 3) s += "\n" + "AA_MasteryBonus3".Translate();
            if (rank >= MaxRank) s += "\n" + "AA_MasteryBonus5".Translate();
            return s;
        }

        public static void RankUpFx(Pawn p, AbilityDef def, int rank)
        {
            if (p?.Map == null) return;
            var props = def.comps?.OfType<CompProperties_ArcaneSpell>().FirstOrDefault();
            Color c = props?.color ?? new Color(1f, 0.85f, 0.4f);
            var map = p.Map;
            var mgr = ArcaneVfxManager.For(map);
            mgr.Add(new VfxSprite { mat = ArcaneMats.Flare, pos = p.DrawPos, scaleStart = 0.5f, scaleEnd = 2.6f, duration = 26, color = new Color(1f, 0.9f, 0.5f), popIn = true, spin = 8f });
            mgr.Add(new VfxSprite { mat = ArcaneMats.Ring, pos = p.DrawPos, scaleStart = 0.4f, scaleEnd = 3f, duration = 24, color = c, layer = AltitudeLayer.MoteLow });
            Fx.Rise(map, p.DrawPos, new Color(1f, 0.9f, 0.5f), 10f, PMat.Star, 0.5f, 2f, 1.1f, 0.2f, PFlags.Glint);
            ArcaneDefOf.AA_LevelUp?.PlayOneShot(new TargetInfo(p.Position, map));
            if (map == Find.CurrentMap)
                mgr.screen.AddText(p.DrawPos + new Vector3(0f, 0f, 0.6f), "AA_MasteryUp".Translate(def.LabelCap, RankLabel(rank)), new Color(1f, 0.88f, 0.45f), 1.25f, mgr.Now, 140f, true);
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Arcane strain and wild surges
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Overchanneling lets a mage cast without enough mana by burning their own mind instead. Strain builds up, hurts,
    /// and makes the weave unstable: past a point, spells can set off wild surges, and too much burns the mage out.
    /// </summary>
    public static class ArcaneStrain
    {
        public const float BurnoutThreshold = 120f;
        public const float StrainPerMana = 1f;

        /// <summary>Chance that a cast at this strain triggers a wild surge.</summary>
        public static float SurgeChance(float strain) => Mathf.Clamp01((strain - 40f) / 160f);

        public static void Burnout(CompArcane c)
        {
            var p = c.Pawn;
            if (p == null) return;
            c.strain = 50f;
            c.UpdateStrainHediff();
            if (ArcaneDefOf.AA_ArcaneBurnout != null) ArcaneUtility.ApplyHediff(p, ArcaneDefOf.AA_ArcaneBurnout, 1f, -1, null);
            if (p.Spawned)
            {
                var map = p.Map;
                ArcaneFx.Flash(map, p.DrawPos, new Color(1f, 0.3f, 0.2f), 4f);
                Fx.Sparks(map, p.DrawPos, new Color(1f, 0.4f, 0.3f), 30f, 6f);
                Fx.Smoke(map, p.DrawPos, new Color(0.2f, 0.12f, 0.12f, 0.7f), 6f, 0.5f, 1f, 2.4f, 0.8f, true, 1f);
                ArcaneFx.Shake(map, 1.5f);
                ArcaneFx.Text(p, "AA_BurnoutMote".Translate(), new Color(1f, 0.35f, 0.3f));
            }
            if (p.Faction == Faction.OfPlayer)
                Messages.Message("AA_BurnoutMessage".Translate(p.LabelShort, p.Named("PAWN")), p, MessageTypeDefOf.NegativeHealthEvent);
        }

        /// <summary>A random magical side effect: some help, some hurt, some are just strange.</summary>
        public static void WildSurge(CompArcane c)
        {
            var p = c.Pawn;
            if (p == null || !p.Spawned || p.Dead) return;
            var map = p.Map;
            Vector3 pos = p.DrawPos;
            Color violet = new Color(0.8f, 0.4f, 1f);
            ArcaneFx.Flash(map, pos, violet, 3f);
            Fx.Burst(map, pos, violet, 14f, PMat.Rune, 3f, 1f, 0.3f, PFlags.None, 400f);
            string key;
            int roll = Rand.RangeInclusive(0, 11);
            switch (roll)
            {
                case 0:
                    c.mana = c.ManaMax;
                    Fx.Rise(map, pos, new Color(0.4f, 0.6f, 1f), 20f, PMat.Mote, 0.6f, 3f, 1.2f, 0.25f);
                    key = "AA_Surge_Mana";
                    break;
                case 1:
                {
                    var victim = GenRadial.RadialDistinctThingsAround(p.Position, map, 10f, true).OfType<Pawn>().Where(x => x != p && !x.Dead).RandomElementWithFallback();
                    IntVec3 cell = victim?.Position ?? (p.Position + GenRadial.RadialPattern[Rand.Range(10, 60)]);
                    if (cell.InBounds(map))
                    {
                        Vector3 at = cell.ToVector3Shifted();
                        ArcaneVfxManager.For(map).Add(new VfxLightning(at + new Vector3(Rand.Range(-2f, 2f), 0f, 16f), at, 14, new Color(0.7f, 0.8f, 1f), Color.white, 1.4f));
                        victim?.TakeDamage(new DamageInfo(ArcaneDefOf.AA_Arcane, 18f, 0.3f, -1f, p));
                    }
                    key = "AA_Surge_Bolt";
                    break;
                }
                case 2:
                {
                    var dest = CellFinder.RandomClosewalkCellNear(p.Position, map, 8);
                    if (dest.IsValid && dest != p.Position)
                    {
                        Fx.Implode(map, pos, violet, 12f, 1.6f, 0.3f);
                        p.Position = dest;
                        p.Notify_Teleported(true, true);
                        ArcaneFx.Flash(map, p.DrawPos, violet, 2.5f);
                    }
                    key = "AA_Surge_Blink";
                    break;
                }
                case 3:
                    Fx.Burst(map, pos, new Color(1f, 0.6f, 0.85f), 40f, PMat.Petal, 3f, 3f, 0.3f, PFlags.Wobble, 300f, 0.5f, 1f, 3f);
                    Fx.Burst(map, pos, new Color(0.7f, 0.9f, 1f), 20f, PMat.Star, 2f, 2f, 0.2f, PFlags.Glint);
                    key = "AA_Surge_Petals";
                    break;
                case 4:
                    GenExplosion.DoExplosion(p.Position, map, 1.9f, DamageDefOf.Flame, p, 8);
                    key = "AA_Surge_Fire";
                    break;
                case 5:
                    if (ArcaneDefOf.AA_Haste != null) ArcaneUtility.ApplyHediff(p, ArcaneDefOf.AA_Haste, 1f, 1800, null);
                    key = "AA_Surge_Haste";
                    break;
                case 6:
                    foreach (var a in c.ArcaneAbilities()) a.ResetCooldown();
                    Fx.Vortex(map, pos, new Color(0.5f, 1f, 0.95f), 16f, 2f, PMat.Clock, 0.8f, 0.3f, 9f);
                    key = "AA_Surge_Time";
                    break;
                case 7:
                    HealthUtility.AdjustSeverity(p, HediffDefOf.BloodLoss, 0.08f);
                    Fx.Drips(map, pos, new Color(0.6f, 0.05f, 0.08f, 0.9f), 6f);
                    key = "AA_Surge_Nosebleed";
                    break;
                case 8:
                    foreach (var o in GenRadial.RadialDistinctThingsAround(p.Position, map, 4f, true).OfType<Pawn>().Where(x => x != p && !x.Dead).ToList())
                    {
                        Vector3 dir = (o.DrawPos - pos).Yto0().normalized;
                        IntVec3 to = (o.DrawPos + dir * 3f).ToIntVec3();
                        if (to.InBounds(map) && to.Standable(map))
                        {
                            o.Position = to;
                            o.Notify_Teleported(false, true);
                        }
                    }
                    ArcaneVfxManager.For(map).Add(new VfxSprite { mat = ArcaneMats.Shock, pos = pos, scaleStart = 0.4f, scaleEnd = 9f, duration = 18, color = violet });
                    key = "AA_Surge_Gravity";
                    break;
                case 9:
                    c.mana = 0f;
                    c.strain += 15f;
                    Fx.Implode(map, pos, new Color(0.3f, 0.5f, 1f), 16f, 2f, 0.4f);
                    key = "AA_Surge_Drain";
                    break;
                case 10:
                    c.ward = Mathf.Max(c.ward, c.WardMax + 40f);
                    ArcaneFx.WardHit(p, violet, new DamageInfo(DamageDefOf.Blunt, 0f), false);
                    key = "AA_Surge_Ward";
                    break;
                default:
                    foreach (var o in GenRadial.RadialDistinctThingsAround(p.Position, map, 6f, true).OfType<Pawn>().Where(x => !x.Dead && !x.HostileTo(p)).ToList())
                        ArcaneUtility.HealPawn(o, 15f);
                    Fx.Rise(map, pos, new Color(0.5f, 1f, 0.6f), 20f, PMat.Heart, 3f, 1.4f, 1.4f, 0.24f);
                    key = "AA_Surge_Heal";
                    break;
            }
            ArcaneFx.Text(p, "AA_WildSurge".Translate() + ": " + key.Translate(), violet);
            if (p.Faction == Faction.OfPlayer)
                Messages.Message("AA_WildSurgeMessage".Translate(p.LabelShort, key.Translate(), p.Named("PAWN")), p, MessageTypeDefOf.NeutralEvent, false);
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Familiars
    // ------------------------------------------------------------------------------------------------

    public enum FamiliarAbility
    {
        None,
        Bolt,
        Heal,
        Blind,
        Ward,
        Breath,
    }

    /// <summary>A bonded familiar: a small spirit companion that follows its mage, grants bonuses and helps in fights.</summary>
    public class FamiliarDef : Def
    {
        public string texPath;
        public bool glowing = true;
        public Color color = Color.white;
        public Color color2 = Color.white;
        public float drawSize = 0.8f;
        public float orbitRadius = 1.1f;
        public float orbitSpeed = 1.2f;
        public FamiliarAbility ability = FamiliarAbility.None;
        public int abilityInterval = 180;
        public float abilityRange = 12f;
        public float abilityPower = 8f;
        public DamageDef damageDef;
        public HediffDef hediff;
        public float hediffSeverity = 1f;
        public VfxStyle style = VfxStyle.Arcane;
        public List<StatModifier> statOffsets = new List<StatModifier>();
        public string flavor;

        [Unsaved] private Material mat;
        [Unsaved] private Texture2D icon;

        public Material Mat => mat ??= ArcaneMats.Get(texPath, glowing);
        public Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(texPath, false) ?? BaseContent.BadTex;
    }

    public static class Familiars
    {
        /// <summary>Where the familiar is drawn this frame: circling and bobbing around its mage.</summary>
        public static Vector3 Position(CompArcane c, float now)
        {
            var p = c.Pawn;
            var f = c.familiar;
            float seed = (p.thingIDNumber % 97) * 3.7f;
            float a = now * 0.017f * f.orbitSpeed + seed;
            float rad = f.orbitRadius * (0.85f + 0.15f * Mathf.Sin(now * 0.011f + seed));
            Vector3 off = new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad * 0.7f + 0.55f);
            off.z += 0.12f * Mathf.Sin(now * 0.09f + seed);
            return p.DrawPos + off;
        }

        public static void Draw(CompArcane c, float now, RibbonTrail trail)
        {
            var f = c.familiar;
            Vector3 pos = Position(c, now);
            if (trail != null)
            {
                trail.AddPoint(pos, now);
                trail.Age(now);
                trail.Draw(now, 0.8f);
            }
            Vector3 p = pos;
            p.y = VAlt.Glow - 0.012f;
            float pulse = 1f + 0.08f * Mathf.Sin(now * 0.2f);
            ArcaneMats.DrawQuad(ArcaneMats.Glow, p, f.drawSize * 2.6f * pulse, 0f, f.color * new Color(1, 1, 1, 0.45f));
            Vector3 sp = pos;
            sp.y = f.glowing ? VAlt.Glow - 0.008f : AltitudeLayer.MoteOverhead.AltitudeFor();
            ArcaneMats.Draw(f.Mat, sp, Quaternion.identity, new Vector3(f.drawSize, 1f, f.drawSize), Color.white);
            Vector3 shadow = c.Pawn.DrawPos + new Vector3(pos.x - c.Pawn.DrawPos.x, 0f, (pos.z - c.Pawn.DrawPos.z) - 0.55f);
            shadow.y = VAlt.Ground;
            ArcaneMats.Draw(PMat.Get(PMat.Shadow), shadow, Quaternion.identity, new Vector3(f.drawSize * 0.7f, 1f, f.drawSize * 0.35f), new Color(0f, 0f, 0f, 0.35f));
        }

        /// <summary>Runs the familiar's ability. Called from the mage's arcane tick.</summary>
        public static void Tick(CompArcane c, int elapsed)
        {
            var f = c.familiar;
            var p = c.Pawn;
            if (f == null || p == null || !p.Spawned || p.Dead || p.Downed) return;
            var map = p.Map;
            // The arcane tick runs every 15+ ticks, so a plain chance gives a steady trickle of sparkles.
            if (map == Find.CurrentMap && VRand.Chance(0.5f))
            {
                Vector3 fp = Position(c, ArcaneVfxManager.For(map).Now);
                Fx.Burst(map, fp, f.color, 1f, f.style == VfxStyle.Fire ? PMat.Ember : PMat.Mote, 0.3f, 0.7f, 0.14f);
            }
            if (f.ability == FamiliarAbility.None) return;
            c.familiarCooldown -= elapsed;
            if (c.familiarCooldown > 0) return;
            bool combat = p.Drafted || Find.TickManager.TicksGame - c.lastCombatTick < 900;
            float power = c.IsMage ? Mathf.Sqrt(Mathf.Max(0.25f, c.SpellPower)) : 1f;
            var mgr = ArcaneVfxManager.For(map);
            Vector3 from = Position(c, mgr.Now);
            switch (f.ability)
            {
                case FamiliarAbility.Bolt:
                case FamiliarAbility.Blind:
                case FamiliarAbility.Breath:
                {
                    if (!combat) return;
                    var target = GenRadial.RadialDistinctThingsAround(p.Position, map, f.abilityRange, true).OfType<Pawn>()
                        .Where(x => !x.Dead && !x.Downed && x.HostileTo(p)).OrderBy(x => x.Position.DistanceToSquared(p.Position)).FirstOrDefault();
                    if (target == null) return;
                    c.familiarCooldown = f.abilityInterval;
                    Vector3 to = target.DrawPos;
                    int ticks = Mathf.Clamp(Mathf.RoundToInt((to - from).MagnitudeHorizontal() * 2.2f), 6, 30);
                    mgr.Add(new VfxProjectile(from, to, ticks, ArcaneMats.Get(f.ability == FamiliarAbility.Breath ? "ArcanumAscendant/Vfx/Fireball" : "ArcanumAscendant/Vfx/Orb"),
                        f.color, f.color2, f.ability == FamiliarAbility.Breath ? 0.7f : 0.45f, 0.3f, false, false) { style = f.style });
                    var dmgDef = f.damageDef ?? ArcaneDefOf.AA_Arcane;
                    float amount = f.abilityPower * power;
                    mgr.Schedule(ticks, () =>
                    {
                        if (target.Dead || !target.Spawned) return;
                        CompArcane.applyingSpellDamage = true;
                        try
                        {
                            target.TakeDamage(new DamageInfo(dmgDef, amount, 0.25f, -1f, p));
                            if (f.ability == FamiliarAbility.Breath)
                                foreach (var o in GenRadial.RadialDistinctThingsAround(target.Position, map, 1.6f, true).OfType<Pawn>().Where(x => x != target && !x.Dead && x.HostileTo(p)).ToList())
                                    o.TakeDamage(new DamageInfo(dmgDef, amount * 0.5f, 0.25f, -1f, p));
                        }
                        finally
                        {
                            CompArcane.applyingSpellDamage = false;
                        }
                        if (f.hediff != null && !target.Dead) ArcaneUtility.ApplyHediff(target, f.hediff, f.hediffSeverity, -1, null);
                        ArcaneFx.Flash(map, target.DrawPos, f.color, 1.4f);
                        Fx.Sparks(map, target.DrawPos, f.color2, 6f, 4f);
                    });
                    break;
                }
                case FamiliarAbility.Heal:
                {
                    var ally = GenRadial.RadialDistinctThingsAround(p.Position, map, f.abilityRange, true).OfType<Pawn>()
                        .Where(x => !x.Dead && !x.HostileTo(p) && (x.Faction == p.Faction || x == p) && x.health.summaryHealth.SummaryHealthPercent < 0.98f)
                        .OrderBy(x => x.health.summaryHealth.SummaryHealthPercent).FirstOrDefault();
                    if (ally == null)
                    {
                        c.familiarCooldown = 120;
                        return;
                    }
                    c.familiarCooldown = f.abilityInterval;
                    float amount = f.abilityPower * power;
                    mgr.Add(new VfxSoulWisp(from, ally, f.color, f.color2, () =>
                    {
                        if (ally.Dead) return;
                        float healed = ArcaneUtility.HealPawn(ally, amount);
                        if (healed > 0.5f && ally.Spawned) ArcaneFx.HealNumber(ally.Map, ally.DrawPos, healed, false);
                    }, PMat.Heart));
                    break;
                }
                case FamiliarAbility.Ward:
                {
                    if (!combat || c.ward >= c.WardMax - 0.5f) return;
                    c.familiarCooldown = f.abilityInterval;
                    float amount = f.abilityPower * power;
                    mgr.Add(new VfxSoulWisp(from, p, f.color, f.color2, () =>
                    {
                        c.ward = Mathf.Min(c.WardMax, c.ward + amount);
                        if (p.Spawned) ArcaneFx.WardHit(p, f.color, new DamageInfo(DamageDefOf.Blunt, 0f), false);
                    }, PMat.Gear));
                    break;
                }
            }
        }

        public static void Bond(CompArcane c, FamiliarDef def)
        {
            if (c == null || def == null) return;
            c.familiar = def;
            c.familiarCooldown = 120;
            c.MarkDirty();
            var p = c.Pawn;
            if (p.Spawned)
            {
                var map = p.Map;
                var mgr = ArcaneVfxManager.For(map);
                mgr.Add(new VfxPillar(p.DrawPos, 60, def.color, def.color2, 1.4f));
                mgr.Add(new VfxSprite { mat = PMat.Get(PMat.Ring), pos = p.DrawPos, scaleStart = 0.3f, scaleEnd = 4f, duration = 30, color = def.color });
                Fx.Implode(map, p.DrawPos, def.color, 30f, 3f, 0.6f, -1, 0.15f, 8f);
                ArcaneDefOf.AA_Awaken?.PlayOneShot(new TargetInfo(p.Position, map));
                mgr.RegisterFamiliar(c);
            }
            Messages.Message("AA_FamiliarBonded".Translate(p.LabelShort, def.label, p.Named("PAWN")), p, MessageTypeDefOf.PositiveEvent);
        }
    }

    /// <summary>Shatter a familiar vessel to bond a familiar. The dialog cannot be dismissed, so the vessel is never wasted.</summary>
    public class CompUseEffect_BondFamiliar : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            var c = usedBy.TryGetComp<CompArcane>();
            if (c == null || !c.IsMage) return;
            if (usedBy.Faction == Faction.OfPlayer) Find.WindowStack.Add(new Dialog_ChooseFamiliar(c));
            else Familiars.Bond(c, DefDatabase<FamiliarDef>.AllDefsListForReading.RandomElement());
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            var c = p.TryGetComp<CompArcane>();
            if (c == null || !c.IsMage) return "AA_NotAMage".Translate();
            if (c.circle < 2) return "AA_NeedPreviousCircle".Translate(CompArcane.CircleLabel(2));
            return base.CanBeUsedBy(p);
        }
    }

    public class Dialog_ChooseFamiliar : Window
    {
        private readonly CompArcane comp;
        private FamiliarDef selected;
        private readonly List<FamiliarDef> defs;

        public override Vector2 InitialSize => new Vector2(900f, 600f);

        public Dialog_ChooseFamiliar(CompArcane comp)
        {
            this.comp = comp;
            defs = DefDatabase<FamiliarDef>.AllDefsListForReading.ToList();
            selected = comp.familiar ?? defs.FirstOrDefault();
            doCloseX = defs.Count == 0;
            closeOnCancel = defs.Count == 0;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "AA_ChooseFamiliar".Translate(comp.Pawn.LabelShort));
            Text.Font = GameFont.Small;
            float y = inRect.y + 44f;
            var left = new Rect(inRect.x, y, 300f, inRect.height - 50f);
            var right = new Rect(left.xMax + 14f, y, inRect.width - left.width - 14f, inRect.height - 50f);
            float rowH = 58f;
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                var r = new Rect(left.x, left.y + i * (rowH + 6f), left.width, rowH);
                bool sel = d == selected;
                Widgets.DrawBoxSolid(r, new Color(0.07f, 0.06f, 0.1f));
                Widgets.DrawBoxSolidWithOutline(r, Color.clear, sel ? d.color : d.color * new Color(1, 1, 1, 0.3f), sel ? 2 : 1);
                var ic = new Rect(r.x + 6f, r.y + 5f, 48f, 48f);
                GUI.color = d.color * new Color(1, 1, 1, 0.4f);
                GUI.DrawTexture(ic.ExpandedBy(6f), ArcaneTex.NodeGlow);
                GUI.color = Color.white;
                GUI.DrawTexture(ic, d.Icon);
                Widgets.Label(new Rect(ic.xMax + 10f, r.y + 18f, r.width - 70f, 24f), d.LabelCap.Colorize(d.color));
                if (Widgets.ButtonInvisible(r)) selected = d;
            }
            if (selected == null) return;
            Widgets.DrawBoxSolid(right, new Color(0.05f, 0.045f, 0.08f));
            var inner = right.ContractedBy(14f);
            var big = new Rect(inner.x, inner.y, 110f, 110f);
            GUI.color = selected.color * new Color(1, 1, 1, 0.5f);
            GUI.DrawTexture(big.ExpandedBy(14f), ArcaneTex.NodeGlow);
            GUI.color = Color.white;
            GUI.DrawTexture(big, selected.Icon);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(big.xMax + 14f, inner.y + 8f, inner.width - 130f, 34f), selected.LabelCap.Colorize(selected.color));
            Text.Font = GameFont.Small;
            float ty = big.yMax + 12f;
            string text = selected.description;
            if (!selected.statOffsets.NullOrEmpty())
                text += "\n\n" + string.Join("\n", selected.statOffsets.Select(s => "  • " + s.stat.LabelCap + ": " + s.ValueToStringAsOffset));
            if (!selected.flavor.NullOrEmpty()) text += "\n\n" + selected.flavor.Colorize(ColoredText.SubtleGrayColor);
            float h = Text.CalcHeight(text, inner.width);
            Widgets.Label(new Rect(inner.x, ty, inner.width, h), text);
            if (Widgets.ButtonText(new Rect(inner.x, inner.yMax - 40f, inner.width, 40f), "AA_BondFamiliar".Translate(selected.label)))
            {
                Familiars.Bond(comp, selected);
                Close();
            }
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Arcane buildings
    // ------------------------------------------------------------------------------------------------

    public class CompProperties_ManaFont : CompProperties
    {
        public float radius = 7.9f;
        public float manaPerSecond = 1.5f;
        public Color color = new Color(0.45f, 0.65f, 1f);

        public CompProperties_ManaFont()
        {
            compClass = typeof(CompManaFont);
        }
    }

    /// <summary>A font of raw mana: mages nearby regenerate faster, and you can see the mana stream into them.</summary>
    public class CompManaFont : ThingComp
    {
        public CompProperties_ManaFont Props => (CompProperties_ManaFont)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            ArcaneVfxManager.For(parent.Map)?.RegisterFont(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            ArcaneVfxManager.For(map)?.UnregisterFont(this);
        }

        public override void CompTick()
        {
            base.CompTick();
            var map = parent.Map;
            if (map == null || map != Find.CurrentMap) return;
            if (parent.IsHashIntervalTick(9))
                Fx.Rise(map, parent.DrawPos + new Vector3(0f, 0f, 0.3f), Props.color, 1f, PMat.Mote, 0.35f, 1.1f, 1.4f, 0.16f);
            if (parent.IsHashIntervalTick(25))
            {
                foreach (var c in ArcaneVfxManager.For(map).MagesNear(parent.Position, Props.radius))
                {
                    if (c.mana >= c.ManaMax - 0.5f) continue;
                    var p = c.Pawn;
                    Fx.One(map, new ArcaneParticle
                    {
                        pos = parent.DrawPos + new Vector3(0f, 0f, 0.6f), vel = VRand.InsideCircle(1.5f), attract = p.DrawPos, attractK = 22f, drag = 1.6f,
                        life = 1.2f, size0 = 0.3f, size1 = 0.18f, c0 = Props.color, c1 = new Color(1f, 1f, 1f, 0.2f), mat = PMat.Streak, flags = PFlags.Stretch, stretch = 0.1f,
                    });
                }
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Props.radius < GenRadial.MaxRadialPatternRadius) GenDraw.DrawRadiusRing(parent.Position, Props.radius, Props.color);
        }

        public override string CompInspectStringExtra() => "AA_ManaFontInspect".Translate(Props.manaPerSecond.ToString("F1"), Props.radius.ToString("F0"));
    }

    /// <summary>Ambient glints and motes for arcane buildings.</summary>
    public class CompProperties_ArcaneAmbience : CompProperties
    {
        public Color color = new Color(0.75f, 0.55f, 1f);
        public int interval = 12;
        public float height = 0.6f;

        public CompProperties_ArcaneAmbience()
        {
            compClass = typeof(CompArcaneAmbience);
        }
    }

    public class CompArcaneAmbience : ThingComp
    {
        public CompProperties_ArcaneAmbience Props => (CompProperties_ArcaneAmbience)props;

        public override void CompTick()
        {
            base.CompTick();
            var map = parent.Map;
            if (map == null || map != Find.CurrentMap || !parent.IsHashIntervalTick(Props.interval)) return;
            Vector3 pos = parent.DrawPos + new Vector3(0f, 0f, Props.height);
            Fx.Burst(map, pos, Props.color, 1f, VRand.Chance(0.3f) ? PMat.Rune : PMat.Mote, 0.4f, 1.2f, 0.16f, PFlags.Glint, 90f, -0.4f, 0.1f, 0.3f);
        }
    }
}

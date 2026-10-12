using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>Helpers for age-related conditions, shared by the aging and restoration effects.</summary>
    public static class AgeConditions
    {
        /// <summary>Hediffs this pawn's race gets from growing old (bad back, cataracts, dementia and so on).</summary>
        public static HashSet<HediffDef> AgeHediffs(Pawn p)
        {
            var set = new HashSet<HediffDef>();
            var sets = p?.RaceProps?.hediffGiverSets;
            if (sets == null) return set;
            foreach (var gs in sets)
                if (gs?.hediffGivers != null)
                    foreach (var g in gs.hediffGivers)
                        if (g is HediffGiver_Birthday && g.hediff != null) set.Add(g.hediff);
            return set;
        }

        public static IEnumerable<HediffGiver_Birthday> AgeGivers(Pawn p)
        {
            var sets = p?.RaceProps?.hediffGiverSets;
            if (sets == null) yield break;
            foreach (var gs in sets)
                if (gs?.hediffGivers != null)
                    foreach (var g in gs.hediffGivers)
                        if (g is HediffGiver_Birthday b && b.hediff != null) yield return b;
        }

        /// <summary>Removes up to <paramref name="count"/> chronic or age-related conditions (-1 = all). Returns their labels.</summary>
        public static List<string> CureChronic(Pawn p, int count)
        {
            var cured = new List<string>();
            if (count == 0 || p?.health == null) return cured;
            var age = AgeHediffs(p);
            foreach (var h in p.health.hediffSet.hediffs.Where(h => h.def.isBad && (age.Contains(h.def) || h.def.chronic)).ToList())
            {
                if (count >= 0 && cured.Count >= count) break;
                cured.Add(h.LabelBase);
                p.health.RemoveHediff(h);
            }
            return cured;
        }

        public static int HealScars(Pawn p, int count)
        {
            int n = 0;
            if (count == 0 || p?.health == null) return 0;
            foreach (var h in p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Where(h => h.IsPermanent()).OrderByDescending(h => h.Severity).ToList())
            {
                if (count >= 0 && n >= count) break;
                p.health.RemoveHediff(h);
                n++;
            }
            return n;
        }

        public static int RegrowParts(Pawn p, int count)
        {
            int n = 0;
            if (count == 0 || p?.health == null) return 0;
            for (int guard = 0; guard < 40 && (count < 0 || n < count); guard++)
            {
                var missing = p.health.hediffSet.hediffs.OfType<Hediff_MissingPart>()
                    .Where(m => m.Part != null && (m.Part.parent == null || !p.health.hediffSet.PartIsMissing(m.Part.parent)))
                    .OrderByDescending(m => m.Part.def.hitPoints).FirstOrDefault();
                if (missing == null) break;
                p.health.RestorePart(missing.Part);
                n++;
            }
            return n;
        }
    }

    /// <summary>
    /// Shifts a pawn's biological age. Turning back the clock can also cure age-related conditions, fade scars and
    /// regrow lost parts, a little with each cast. Ageing someone can inflict the ailments of old age and, past the
    /// natural lifespan, kill them outright.
    /// </summary>
    public class Effect_AgeShift : SpellEffect
    {
        public float years = -3f;
        public float minAge = 18f;
        public int cureConditions;
        public int healScars;
        public int regrowParts;
        public float ailmentChance;
        public float lethalAgeFactor;
        public bool scaleWithPower = true;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var at = p.ageTracker;
            bool ages = at != null && !p.RaceProps.IsMechanoid && p.RaceProps.lifeExpectancy > 0f;
            float y = years;
            if (scaleWithPower) y *= years < 0f ? Mathf.Clamp(ctx.HealFactor, 0.5f, 3f) : Mathf.Clamp(ctx.DamageFactor, 0.5f, 3f);
            // Scale to the species: a year means more to a rat than to a thrumbo.
            if (ages && !p.RaceProps.Humanlike) y *= Mathf.Clamp(p.RaceProps.lifeExpectancy / 80f, 0.1f, 3f);
            if (years > 0f && (p.kindDef?.combatPower ?? 0f) > 300f) y *= 0.5f;
            var notes = new List<string>();
            if (ages)
            {
                if (y < 0f)
                {
                    float floor = Mathf.Max(at.AdultMinAge, p.RaceProps.Humanlike ? minAge : 0f);
                    long min = (long)(Mathf.Min(floor, at.AgeBiologicalYearsFloat) * GenDate.TicksPerYear);
                    at.AgeBiologicalTicks = System.Math.Max(min, at.AgeBiologicalTicks + (long)(y * GenDate.TicksPerYear));
                }
                else if (y > 0f)
                {
                    at.AgeBiologicalTicks += (long)(y * GenDate.TicksPerYear);
                }
                notes.Add((y < 0f ? "AA_Younger" : "AA_Older").Translate(Mathf.Abs(y).ToString("F0")));
            }

            if (y < 0f)
            {
                foreach (var c in AgeConditions.CureChronic(p, cureConditions)) notes.Add("AA_CuredX".Translate(c));
                int s = AgeConditions.HealScars(p, healScars);
                if (s > 0) notes.Add("AA_ScarsFaded".Translate(s));
                int r = AgeConditions.RegrowParts(p, regrowParts);
                if (r > 0) notes.Add("AA_PartsRegrown".Translate(r));
            }
            else if (ages && ailmentChance > 0f)
            {
                float ratio = at.AgeBiologicalYearsFloat / p.RaceProps.lifeExpectancy;
                if (Rand.Chance(ailmentChance * Mathf.Clamp(ratio, 0.4f, 2f)))
                {
                    var givers = AgeConditions.AgeGivers(p).Where(g => !p.health.hediffSet.HasHediff(g.hediff)).ToList();
                    if (givers.TryRandomElement(out var giver))
                    {
                        var added = new List<Hediff>();
                        if (giver.TryApply(p, added))
                            foreach (var h in added) notes.Add(h.LabelBase);
                    }
                }
                if (lethalAgeFactor > 0f && ratio >= lethalAgeFactor && !p.Dead)
                {
                    ArcaneFx.Text(p, "AA_AgedToDust".Translate(), ctx.Color);
                    var map = p.MapHeld;
                    p.Kill(null);
                    var corpse = p.Corpse;
                    var rot = corpse?.TryGetComp<CompRottable>();
                    if (rot != null) rot.RotProgress = rot.PropsRot.TicksToDessicated + 1;
                    if (map != null && corpse != null && corpse.Spawned)
                        ArcaneFx.Particles(map, corpse.DrawPos, new Color(0.75f, 0.7f, 0.6f), ArcaneDefOf.AA_Fleck_Smoke, 10, 1.2f, 1.2f);
                    ctx.arcane?.Notify_Kill(p, ctx);
                    return;
                }
            }
            if (notes.Count > 0) ArcaneFx.Text(p, string.Join(", ", notes), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return (years < 0 ? "AA_Desc_Younger" : "AA_Desc_Older").Translate(Mathf.Abs(years).ToString("F0")) + Chance(chance);
            if (years < 0)
            {
                if (cureConditions != 0) yield return "AA_Desc_CureConditions".Translate(cureConditions < 0 ? "AA_All".Translate().ToString() : cureConditions.ToString());
                if (healScars != 0) yield return "AA_Desc_HealScars".Translate(healScars < 0 ? "AA_All".Translate().ToString() : healScars.ToString());
                if (regrowParts != 0) yield return "AA_Desc_RegrowParts".Translate(regrowParts < 0 ? "AA_All".Translate().ToString() : regrowParts.ToString());
            }
            else
            {
                if (ailmentChance > 0f) yield return "AA_Desc_AgeAilment".Translate(ailmentChance.ToStringPercent());
                if (lethalAgeFactor > 0f) yield return "AA_Desc_AgeLethal".Translate(lethalAgeFactor.ToStringPercent());
            }
        }
    }

    /// <summary>Deep healing: cures chronic conditions, fades scars, regrows parts and breaks addictions.</summary>
    public class Effect_Restore : SpellEffect
    {
        public int chronic;
        public int scars;
        public int regrowParts;
        public bool addictions;

        public Effect_Restore()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var notes = new List<string>();
            foreach (var c in AgeConditions.CureChronic(p, chronic)) notes.Add("AA_CuredX".Translate(c));
            int s = AgeConditions.HealScars(p, scars);
            if (s > 0) notes.Add("AA_ScarsFaded".Translate(s));
            int r = AgeConditions.RegrowParts(p, regrowParts);
            if (r > 0) notes.Add("AA_PartsRegrown".Translate(r));
            if (addictions)
                foreach (var h in p.health.hediffSet.hediffs.Where(h => h is Hediff_Addiction || h.def.IsAddiction).ToList())
                {
                    notes.Add("AA_CuredX".Translate(h.LabelBase));
                    p.health.RemoveHediff(h);
                }
            if (notes.Count > 0)
            {
                ArcaneFx.Text(p, string.Join(", ", notes), ctx.Color);
                ctx.arcane?.GainXp(20f * notes.Count);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            string N(int n) => n < 0 ? "AA_All".Translate().ToString() : n.ToString();
            if (chronic != 0) yield return "AA_Desc_CureConditions".Translate(N(chronic));
            if (scars != 0) yield return "AA_Desc_HealScars".Translate(N(scars));
            if (regrowParts != 0) yield return "AA_Desc_RegrowParts".Translate(N(regrowParts));
            if (addictions) yield return "AA_Desc_CureAddictions".Translate();
        }
    }

    /// <summary>Turns back time for objects: repairs items, un-rots food and makes "tainted" apparel clean again.</summary>
    public class Effect_RestoreItems : SpellEffect
    {
        public float repairFraction = 1f;
        public bool unrot = true;
        public bool untaint = true;
        public int maxItems = 60;

        public Effect_RestoreItems()
        {
            affects = TargetFilter.Allies;
        }

        private bool Restore(Thing t)
        {
            bool did = false;
            if (t.def.useHitPoints && t.HitPoints < t.MaxHitPoints && repairFraction > 0f)
            {
                t.HitPoints = Mathf.Min(t.MaxHitPoints, t.HitPoints + Mathf.CeilToInt(t.MaxHitPoints * repairFraction));
                did = true;
            }
            if (unrot && t.TryGetComp<CompRottable>() is CompRottable rot && rot.RotProgress > 0f && !(t is Corpse))
            {
                rot.RotProgress = 0f;
                did = true;
            }
            if (untaint && t is Apparel ap && ap.WornByCorpse)
            {
                ap.WornByCorpse = false;
                did = true;
            }
            return did;
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            float r = radius >= 0f ? radius * ctx.RadiusFactor : info.radius;
            int n = 0;
            var cells = r < 0.5f ? new[] { info.cell } : GenRadial.RadialCellsAround(info.cell, Mathf.Min(r, GenRadial.MaxRadialPatternRadius - 0.5f), true);
            foreach (var c in cells)
            {
                if (!c.InBounds(map)) continue;
                foreach (var t in c.GetThingList(map).ToList())
                {
                    if (n >= maxItems) break;
                    if (t.def.category != ThingCategory.Item) continue;
                    if (Restore(t))
                    {
                        n++;
                        ArcaneFx.Particles(map, t.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Clock ?? ArcaneDefOf.AA_Fleck_Spark, 2, 0.6f, 0.5f);
                    }
                }
            }
            if (n > 0) MoteMaker.ThrowText(info.pos, map, "AA_ItemsRestored".Translate(n), ctx.Color, 3f);
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            int n = 0;
            if (p.apparel != null) foreach (var ap in p.apparel.WornApparel) if (Restore(ap)) n++;
            if (p.equipment != null) foreach (var eq in p.equipment.AllEquipmentListForReading) if (Restore(eq)) n++;
            if (n > 0) ArcaneFx.Text(p, "AA_ItemsRestored".Translate(n), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_RestoreItems".Translate(repairFraction.ToStringPercent());
            if (unrot) yield return "AA_Desc_Unrot".Translate();
            if (untaint) yield return "AA_Desc_Untaint".Translate();
        }
    }

    /// <summary>Raises the quality of an item (or a pawn's weapon) by a step.</summary>
    public class Effect_ImproveQuality : SpellEffect
    {
        public int steps = 1;
        public QualityCategory maxQuality = QualityCategory.Legendary;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            Thing target = null;
            foreach (var t in info.cell.GetThingList(map))
            {
                if (t.TryGetComp<CompQuality>() != null && t.def.category == ThingCategory.Item) { target = t; break; }
                if (t is Pawn p && p.equipment?.Primary?.TryGetComp<CompQuality>() != null) { target = p.equipment.Primary; break; }
            }
            if (target == null && info.primary is Pawn pp && pp.equipment?.Primary?.TryGetComp<CompQuality>() != null) target = pp.equipment.Primary;
            if (target == null)
            {
                MoteMaker.ThrowText(info.pos, map, "AA_NoQualityItem".Translate(), Color.gray, 3f);
                return;
            }
            var cq = target.TryGetComp<CompQuality>();
            if (cq.Quality >= maxQuality)
            {
                MoteMaker.ThrowText(info.pos, map, "AA_QualityMaxed".Translate(target.LabelShort), Color.gray, 3f);
                return;
            }
            var q = (QualityCategory)Mathf.Min((int)maxQuality, (int)cq.Quality + steps);
            cq.SetQuality(q, ArtGenerationContext.Colony);
            ArcaneFx.Flash(map, info.pos, ctx.Color, 2.5f);
            ArcaneFx.Particles(map, info.pos, ctx.Color2, ArcaneDefOf.AA_Fleck_Rune ?? ArcaneDefOf.AA_Fleck_Spark, 12, 1.6f, 0.6f);
            MoteMaker.ThrowText(info.pos, map, "AA_QualityRaised".Translate(target.LabelShort, q.GetLabel()), ctx.Color, 4f);
            ctx.arcane?.GainXp(40f * (int)q);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_ImproveQuality".Translate(steps, maxQuality.GetLabel());
        }
    }

    /// <summary>The Midas touch: weakened or small creatures turn to solid gold. The mighty are only gilded and slowed.</summary>
    public class Effect_Midas : SpellEffect
    {
        public float healthThreshold = 0.5f;
        public float goldPerBodySize = 40f;
        public float maxBodySize = 2.5f;
        public float damage = 30f;
        public HediffDef gildedHediff;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var map = p.MapHeld;
            bool boss = (p.kindDef?.combatPower ?? 0f) > 400f || p.BodySize > maxBodySize;
            float hp = p.health.summaryHealth.SummaryHealthPercent;
            if (!boss && map != null && hp <= Mathf.Clamp(healthThreshold * ctx.DamageFactor, healthThreshold, 0.95f))
            {
                var pos = p.PositionHeld;
                int gold = Mathf.Max(5, Mathf.RoundToInt(goldPerBodySize * p.BodySize * Rand.Range(0.75f, 1.25f)));
                ArcaneFx.Text(p, "AA_TurnedToGold".Translate(), new Color(1f, 0.85f, 0.3f));
                p.Kill(null);
                p.Corpse?.Destroy();
                var g = ThingMaker.MakeThing(ThingDefOf.Gold);
                g.stackCount = gold;
                GenPlace.TryPlaceThing(g, pos, map, ThingPlaceMode.Near);
                ArcaneFx.Flash(map, pos.ToVector3Shifted(), new Color(1f, 0.85f, 0.3f), 3f);
                ArcaneFx.Particles(map, pos.ToVector3Shifted(), new Color(1f, 0.85f, 0.3f), ArcaneDefOf.AA_Fleck_Spark, 24, 2.5f, 0.6f);
                ctx.arcane?.Notify_Kill(p, ctx);
                return;
            }
            var dinfo = new DamageInfo(ArcaneDefOf.AA_ArcaneTrue, damage * ctx.DamageFactor, 1f, -1f, ctx.caster);
            CompArcane.applyingSpellDamage = true;
            try { p.TakeDamage(dinfo); }
            finally { CompArcane.applyingSpellDamage = false; }
            if (gildedHediff != null && !p.Dead) ArcaneUtility.ApplyHediff(p, gildedHediff, 1f, -1, ctx);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Midas".Translate(healthThreshold.ToStringPercent(), goldPerBodySize.ToString("F0"), damage.ToString("F0"));
        }
    }

    /// <summary>Calls a meteorite full of ore down from the sky.</summary>
    public class Effect_CallMeteorite : SpellEffect
    {
        public ThingDef mineable;
        public int count = -1;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            List<Thing> things;
            if (mineable != null)
            {
                things = new List<Thing>();
                int n = count > 0 ? count : Rand.RangeInclusive(5, 10);
                for (int i = 0; i < n; i++) things.Add(ThingMaker.MakeThing(mineable));
            }
            else things = ThingSetMakerDefOf.Meteorite.root.Generate();
            if (!CellFinderLoose.TryFindSkyfallerCell(ThingDefOf.MeteoriteIncoming, map, TerrainAffordanceDefOf.Walkable, out var cell, 5, info.cell, 12,
                    allowRoofedCells: false, allowCellsWithItems: true, allowCellsWithBuildings: false, colonyReachable: false, avoidColonistsIfExplosive: true,
                    alwaysAvoidColonists: true))
            {
                MoteMaker.ThrowText(info.pos, map, "AA_NoRoomForMeteor".Translate(), Color.gray, 3f);
                foreach (var t in things) t.Destroy();
                return;
            }
            SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, things, cell, map);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_CallMeteorite".Translate(mineable?.label ?? "AA_RandomOre".Translate().ToString());
        }
    }

    /// <summary>Devour corpses: each one feeds the caster health and mana and is consumed.</summary>
    public class Effect_ConsumeCorpses : SpellEffect
    {
        public float healPerBodySize = 25f;
        public float manaPerBodySize = 20f;
        public int maxCorpses = 6;
        public bool destroy = true;

        [Unsaved] private int castId = -1;
        [Unsaved] private int consumedThisCast;

        public Effect_ConsumeCorpses()
        {
            affects = TargetFilter.Corpses;
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse c, ImpactInfo info)
        {
            int id = ctx.GetHashCode();
            if (castId != id)
            {
                castId = id;
                consumedThisCast = 0;
            }
            if (consumedThisCast >= maxCorpses || c.InnerPawn == null || !c.Spawned) return;
            if (c.InnerPawn.RaceProps.IsMechanoid) return;
            consumedThisCast++;
            float size = c.InnerPawn.BodySize;
            var map = c.Map;
            var pos = c.DrawPos;
            if (ctx.caster != null && !ctx.caster.Dead)
            {
                ArcaneUtility.HealPawn(ctx.caster, healPerBodySize * size * ctx.HealFactor);
                if (ctx.arcane != null && ctx.arcane.IsMage)
                    ctx.arcane.mana = Mathf.Min(ctx.arcane.ManaMax, ctx.arcane.mana + manaPerBodySize * size);
                if (ctx.caster.Spawned)
                    ArcaneVfxManager.For(map).Add(new VfxBeam(pos, ctx.caster.DrawPos, 24, ArcaneMats.Get("ArcanumAscendant/Vfx/Beam"), ctx.Color, ctx.Color2, 0.6f));
            }
            ArcaneFx.Particles(map, pos, ctx.Color, ArcaneDefOf.AA_Fleck_Smoke, 6, 1.2f, 1f);
            if (destroy)
            {
                FilthMaker.TryMakeFilth(c.Position, map, ThingDefOf.Filth_Blood, 3);
                c.Destroy();
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_ConsumeCorpses".Translate(maxCorpses, healPerBodySize.ToString("F0"), manaPerBodySize.ToString("F0"));
        }
    }

    /// <summary>Steals experience from the target's best skill and teaches it to the caster.</summary>
    public class Effect_StealSkill : SpellEffect
    {
        public float xp = 4000f;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var caster = ctx.caster;
            if (p.skills == null || caster?.skills == null || p == caster) return;
            var rec = p.skills.skills.Where(s => !s.TotallyDisabled && s.Level > 0 && !caster.skills.GetSkill(s.def).TotallyDisabled)
                .OrderByDescending(s => s.Level).FirstOrDefault();
            if (rec == null) return;
            float amount = Mathf.Min(xp * Mathf.Clamp(ctx.basePower, 0.5f, 3f), rec.XpTotalEarned);
            rec.Learn(-amount, direct: true);
            caster.skills.GetSkill(rec.def).Learn(amount, direct: true);
            ArcaneFx.Text(p, "AA_SkillStolen".Translate(rec.def.label), ctx.Color);
            if (caster.Spawned && p.Spawned)
                ArcaneVfxManager.For(ctx.map).Add(new VfxBeam(p.DrawPos, caster.DrawPos, 26, ArcaneMats.Get("ArcanumAscendant/Vfx/Beam"), ctx.Color, ctx.Color2, 0.5f));
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_StealSkill".Translate(xp.ToString("F0"));
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Hediff comps
    // ------------------------------------------------------------------------------------------------

    /// <summary>Implemented by hediff comps that want to know which spell (and caster) applied them.</summary>
    public interface IArcaneSourced
    {
        void SetSource(SpellContext ctx);
    }

    public class HediffCompProperties_DamageShell : HediffCompProperties
    {
        /// <summary>Multiplier on incoming damage. 0 makes the bearer invulnerable.</summary>
        public float damageFactor = 1f;
        /// <summary>Fraction of the original damage sent back at the attacker.</summary>
        public float reflectFraction;
        public DamageDef reflectDamageDef;
        public Color color = Color.white;

        public HediffCompProperties_DamageShell()
        {
            compClass = typeof(HediffComp_DamageShell);
        }
    }

    /// <summary>Reduces or reflects incoming damage. Read by <see cref="CompArcane.PostPreApplyDamage"/>, which every race carries.</summary>
    public class HediffComp_DamageShell : HediffComp
    {
        public HediffCompProperties_DamageShell Props => (HediffCompProperties_DamageShell)props;

        private static bool reflecting;

        /// <summary>Applies every damage shell on the pawn. Returns true if the hit was fully absorbed.</summary>
        public static bool Apply(Pawn p, ref DamageInfo dinfo)
        {
            if (p?.health?.hediffSet == null || reflecting) return false;
            float original = dinfo.Amount;
            float factor = 1f;
            float reflect = 0f;
            DamageDef reflectDef = null;
            Color col = Color.white;
            bool any = false;
            foreach (var h in p.health.hediffSet.hediffs)
            {
                if (!(h is HediffWithComps hc)) continue;
                foreach (var c in hc.comps)
                {
                    if (!(c is HediffComp_DamageShell s)) continue;
                    any = true;
                    factor *= s.Props.damageFactor;
                    if (s.Props.reflectFraction > reflect)
                    {
                        reflect = s.Props.reflectFraction;
                        reflectDef = s.Props.reflectDamageDef;
                    }
                    col = s.Props.color;
                }
            }
            if (!any) return false;
            if (reflect > 0f && dinfo.Instigator is Pawn attacker && attacker != p && !attacker.Dead && original > 0f)
            {
                reflecting = true;
                CompArcane.applyingSpellDamage = true;
                try
                {
                    attacker.TakeDamage(new DamageInfo(reflectDef ?? ArcaneDefOf.AA_Arcane, original * reflect, 1f, -1f, p));
                    if (attacker.Spawned && p.Spawned)
                        ArcaneVfxManager.For(p.Map).Add(new VfxBeam(p.DrawPos, attacker.DrawPos, 12, ArcaneMats.Get("ArcanumAscendant/Vfx/Beam"), col, Color.white, 0.4f));
                }
                finally
                {
                    reflecting = false;
                    CompArcane.applyingSpellDamage = false;
                }
            }
            if (p.Spawned && factor < 0.99f) ArcaneFx.WardHit(p, col, dinfo, broke: false);
            if (factor <= 0.001f) return true;
            dinfo.SetAmount(original * factor);
            return false;
        }
    }

    public class HediffCompProperties_DeathWard : HediffCompProperties
    {
        public Color color = new Color(1f, 0.85f, 0.5f);
        public string labelKey = "AA_FateDenied";

        public HediffCompProperties_DeathWard()
        {
            compClass = typeof(HediffComp_DeathWard);
        }
    }

    /// <summary>If the bearer dies while this lasts, they are pulled back to life a moment later. Used once.</summary>
    public class HediffComp_DeathWard : HediffComp
    {
        public HediffCompProperties_DeathWard Props => (HediffCompProperties_DeathWard)props;

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            var p = Pawn;
            var map = p.Corpse?.MapHeld ?? p.MapHeld;
            if (map == null) return;
            var color = Props.color;
            string label = Props.labelKey;
            var def = parent.def;
            ArcaneVfxManager.For(map).Schedule(20, () =>
            {
                if (!p.Dead || p.Corpse == null || !p.Corpse.Spawned) return;
                var pos = p.Corpse.DrawPos;
                if (!ResurrectionUtility.TryResurrect(p, new ResurrectionParams { gettingScarsChance = 0f, restoreMissingParts = true, removeDiedThoughts = true })) return;
                ArcaneUtility.HealPawn(p, 9999f);
                foreach (var h in p.health.hediffSet.hediffs.ToList())
                    if (h.def == HediffDefOf.BloodLoss || h.def == def) p.health.RemoveHediff(h);
                var mgr = ArcaneVfxManager.For(map);
                mgr.Add(new VfxPillar(pos, 70, color, Color.white, 2.2f));
                ArcaneFx.Flash(map, pos, color, 4f);
                ArcaneFx.Particles(map, pos, color, ArcaneDefOf.AA_Fleck_Star ?? ArcaneDefOf.AA_Fleck_Spark, 24, 2.5f, 0.7f);
                ArcaneFx.Shake(map, 1.5f);
                if (p.Spawned) ArcaneFx.Text(p, label.Translate(), color);
            });
        }
    }

    public class HediffCompProperties_RaiseOnDeath : HediffCompProperties
    {
        public int durationTicks = 60000;
        public HediffDef undeadHediff;

        public HediffCompProperties_RaiseOnDeath()
        {
            compClass = typeof(HediffComp_RaiseOnDeath);
        }
    }

    /// <summary>Death mark: if the bearer dies, they rise as the caster's undead servant.</summary>
    public class HediffComp_RaiseOnDeath : HediffComp, IArcaneSourced
    {
        public Pawn master;
        public AbilityDef spell;

        public HediffCompProperties_RaiseOnDeath Props => (HediffCompProperties_RaiseOnDeath)props;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster != null) master = ctx.caster;
            if (ctx?.def != null) spell = ctx.def;
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            var p = Pawn;
            var m = master;
            var sp = spell;
            if (m == null || m.Dead || sp == null) return;
            var map = p.Corpse?.MapHeld;
            if (map == null || m.MapHeld != map) return;
            int dur = Props.durationTicks;
            var undead = Props.undeadHediff ?? ArcaneDefOf.AA_Undead;
            ArcaneVfxManager.For(map).Schedule(45, () =>
            {
                if (m.Dead || !p.Dead) return;
                var ctx = SpellContext.For(m, sp);
                if (ctx.props == null) return;
                ArcaneUtility.RaiseUndead(p, ctx, Mathf.RoundToInt(dur * ctx.DurationFactor), undead);
            });
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref master, "master");
            Scribe_Defs.Look(ref spell, "spell");
        }
    }
}

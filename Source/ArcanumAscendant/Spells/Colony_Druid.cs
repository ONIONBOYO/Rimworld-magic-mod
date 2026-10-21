using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Druid family colony expansion: corpses given back to the soil, and the colony's own animals sent into battle.
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Corpses in the area sink into the ground. Their gear stays on the surface, the natural soil around each one becomes
    /// rich soil (wider for bigger bodies), plants there surge, and allies near each corpse are healed. The caster's own
    /// humanlike dead are left alone (they deserve a grave); mechanoids have nothing to give back.
    /// </summary>
    public class Effect_DruidReturnToEarth : SpellEffect
    {
        public int maxCorpses = 8;
        /// <summary>Radius of rich soil around a corpse of body size 1 or less.</summary>
        public float soilRadius = 1.5f;
        /// <summary>Extra radius per point of body size above 1.</summary>
        public float soilRadiusPerBodySize = 0.8f;
        public float healPerCorpse = 6f;
        public float healRadius = 4f;
        public float growth = 0.2f;
        public bool spareColonists = true;

        [Unsaved] private int castId = -1;
        [Unsaved] private int doneThisCast;

        private static TerrainDef rich;
        private static TerrainDef Rich => rich ?? (rich = DefDatabase<TerrainDef>.GetNamedSilentFail("SoilRich"));

        public Effect_DruidReturnToEarth()
        {
            affects = TargetFilter.Corpses;
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse c, ImpactInfo info)
        {
            int id = ctx.GetHashCode();
            if (castId != id)
            {
                castId = id;
                doneThisCast = 0;
            }
            var p = c?.InnerPawn;
            if (p == null || !c.Spawned || c.Destroyed || doneThisCast >= maxCorpses + ctx.mods.extraTargets) return;
            if (p.RaceProps == null || !p.RaceProps.IsFlesh) return;
            if (spareColonists && p.RaceProps.Humanlike && ctx.caster?.Faction != null && p.Faction == ctx.caster.Faction) return;
            doneThisCast++;

            var map = c.Map;
            var pos = c.Position;
            var draw = c.DrawPos;
            float size = Mathf.Max(0.2f, p.BodySize);
            try
            {
                if (c.AnythingToStrip()) c.Strip(false);
            }
            catch (System.Exception e)
            {
                Log.Warning("[Arcanum Ascendant] Could not strip " + c + ": " + e.Message);
            }
            if (!c.Destroyed) c.Destroy(DestroyMode.Vanish);

            float r = Mathf.Clamp(soilRadius + soilRadiusPerBodySize * Mathf.Max(0f, size - 1f), 0.9f, 4.9f);
            float grow = growth * Mathf.Sqrt(Mathf.Max(0.1f, ctx.HealFactor));
            var to = Rich;
            foreach (var cell in GenRadial.RadialCellsAround(pos, r, true))
            {
                if (!cell.InBounds(map)) continue;
                if (to != null)
                {
                    var cur = cell.GetTerrain(map);
                    if (cur != null && cur != to && !cur.layerable && !cur.BuildableByPlayer && !cur.IsWater && cur.fertility > 0f && cur.fertility < to.fertility)
                        map.terrainGrid.SetTerrain(cell, to);
                }
                if (grow > 0f)
                {
                    var plant = cell.GetPlant(map);
                    if (plant != null && !plant.Destroyed)
                    {
                        plant.Growth = Mathf.Min(1f, plant.Growth + grow);
                        plant.DirtyMapMesh(map);
                    }
                }
            }

            if (healPerCorpse > 0f)
            {
                float amount = healPerCorpse * ctx.HealFactor;
                foreach (var t in GenRadial.RadialDistinctThingsAround(pos, map, healRadius, true).ToList())
                    if (t is Pawn a && !a.Dead && ctx.Passes(a, TargetFilter.Allies))
                        ArcaneUtility.HealPawn(a, amount);
            }

            if (ColonyFx.Visible(map))
            {
                ArcaneFx.Particles(map, draw, ctx.Color, ArcaneDefOf.AA_Fleck_Leaf, 10, 1.2f, 0.8f);
                ColonyFx.Sparkle(map, draw, ctx.Color2, 5f);
            }
            if (ctx.caster != null && ctx.caster.Spawned && ctx.Once("druidReturnToEarth"))
                ArcaneFx.Text(ctx.caster, "AA_Druid_ReturnedToEarth".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_DruidReturnToEarth".Translate(maxCorpses, soilRadius.ToString("0.#"));
            if (healPerCorpse > 0f)
                yield return "AA_Desc_DruidReturnToEarthHeal".Translate((healPerCorpse * (ctx?.HealFactor ?? 1f)).ToString("F0"), healRadius.ToString("0.#"));
            if (spareColonists) yield return "AA_Desc_DruidReturnToEarthSpare".Translate();
        }
    }

    /// <summary>
    /// The colony's own animals answer the caster: every tame animal near the caster that has learned Release (or is bonded to
    /// the caster) gets the given effects and charges the target, attacking it until it falls or the time runs out. Babies,
    /// the downed, roped animals, animals in a mental state and conjured creatures stay put.
    /// </summary>
    public class Effect_DruidUnleashPack : SpellEffect
    {
        public float callRadius = 20f;
        public int attackTicks = 1500;
        public bool requireRelease = true;
        public List<SpellEffect> animalEffects = new List<SpellEffect>();

        public Effect_DruidUnleashPack()
        {
            affects = TargetFilter.Enemies;
        }

        private bool Answers(SpellContext ctx, Pawn a, Pawn target)
        {
            var caster = ctx.caster;
            if (a == null || a == caster || !a.Spawned || a.Dead || a.Downed || a.InMentalState || !a.RaceProps.Animal) return false;
            if (a.ageTracker != null && a.ageTracker.CurLifeStageIndex == 0) return false;
            if (a.roping != null && a.roping.IsRoped) return false;
            if (a.health?.hediffSet != null && a.health.hediffSet.HasHediff(ArcaneDefOf.AA_Summoned)) return false;
            if (!a.Position.InHorDistOf(caster.Position, callRadius * ctx.RadiusFactor)) return false;
            bool bonded = caster.relations != null && caster.relations.DirectRelationExists(PawnRelationDefOf.Bond, a);
            if (requireRelease && !bonded && (a.training == null || !a.training.HasLearned(TrainableDefOf.Release))) return false;
            return a.CanReach(target, PathEndMode.Touch, Danger.Deadly);
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn target, ImpactInfo info)
        {
            var caster = ctx.caster;
            if (caster?.Faction == null || target == null || target.Dead || !target.Spawned || caster.Map != target.Map) return;
            if (!ctx.Once("druidUnleashPack")) return;
            var map = target.Map;
            int ticks = Mathf.RoundToInt(attackTicks * ctx.DurationFactor);
            int n = 0;
            foreach (var a in map.mapPawns.SpawnedPawnsInFaction(caster.Faction).ToList())
            {
                if (!Answers(ctx, a, target)) continue;
                foreach (var e in animalEffects)
                    if (e != null && Rand.Chance(e.chance)) e.ApplyToPawn(ctx, a, info);
                var job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                job.expiryInterval = ticks;
                job.checkOverrideOnExpire = true;
                job.killIncappedTarget = false;
                a.jobs?.StartJob(job, JobCondition.InterruptForced);
                n++;
                if (ColonyFx.Visible(map))
                {
                    ColonyFx.Fly(map, a.DrawPos, target.DrawPos, ctx.Color);
                    ArcaneFx.Particles(map, a.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Leaf, 4, 1.2f, 0.6f);
                }
                ArcaneFx.Text(a, "AA_Druid_Unleashed".Translate(), ctx.Color);
            }
            if (n == 0 && caster.Spawned) ArcaneFx.Text(caster, "AA_Druid_NoPack".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_DruidUnleashPack".Translate(callRadius.ToString("F0"), (attackTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
            if (requireRelease) yield return "AA_Desc_DruidUnleashPackRelease".Translate();
            foreach (var e in animalEffects)
                if (e != null)
                    foreach (var s in e.Describe(ctx))
                        yield return "AA_Desc_DruidUnleashPackEach".Translate(s);
        }
    }
}

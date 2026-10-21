using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Colony work done by magic: research, building, crafting, farming, animals, prisoners, cleaning, power, incidents,
    // diplomacy, medicine, passions and traits, gatherings, hauling, smoothing and preserving.
    // ------------------------------------------------------------------------------------------------

    public static class ColonyFx
    {
        public static bool Visible(Map map) => map != null && map == Find.CurrentMap;

        public static void Sparkle(Map map, Vector3 pos, Color c, float count = 4f)
        {
            if (!Visible(map)) return;
            Fx.Rise(map, pos, c, count, PMat.Star, 0.45f, 0.5f, 1.1f, 0.15f, PFlags.Glint);
        }

        /// <summary>A thing flying from one cell to another, for items moved by magic.</summary>
        public static void Fly(Map map, Vector3 from, Vector3 to, Color c)
        {
            if (!Visible(map)) return;
            ArcaneVfxManager.For(map).Add(new VfxProjectile(from, to, 24, ArcaneMats.Glow, c, Color.white, 0.5f, 1.5f, false, false));
        }

        public static IEnumerable<Thing> ThingsIn(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var seen = new HashSet<Thing>();
            foreach (var c in info.Cells(map))
                foreach (var t in c.GetThingList(map).ToList())
                    if (seen.Add(t)) yield return t;
        }

        public static bool Friendly(SpellContext ctx, Thing t) => t.Faction == null || ctx.caster?.Faction == null || !t.Faction.HostileTo(ctx.caster.Faction);
    }

    /// <summary>Pours insight into the colony's current research project, and can finish one that is nearly done.</summary>
    public class Effect_Research : SpellEffect
    {
        public float points = 300f;
        /// <summary>Finish the project outright when less than this fraction of it is left.</summary>
        public float finishBelow;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (!ctx.Once("research")) return;
            var rm = Find.ResearchManager;
            var proj = rm?.GetProject();
            if (proj == null)
            {
                if (ctx.caster != null) ArcaneFx.Text(ctx.caster, "AA_NoResearch".Translate(), ctx.Color);
                return;
            }
            float amount = points * Mathf.Sqrt(Mathf.Max(0.1f, ctx.DamageFactor));
            rm.AddProgress(proj, amount, ctx.caster);
            if (!proj.IsFinished && finishBelow > 0f && 1f - proj.ProgressPercent <= finishBelow) rm.FinishProject(proj, true, ctx.caster);
            if (ctx.caster != null) ArcaneFx.Text(ctx.caster, "AA_ResearchGained".Translate(amount.ToString("F0"), proj.LabelCap), ctx.Color);
            ColonyFx.Sparkle(ctx.map, info.pos, ctx.Color, 10f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Research".Translate((points * Mathf.Sqrt(Mathf.Max(0.1f, ctx?.DamageFactor ?? 1f))).ToString("F0"));
            if (finishBelow > 0f) yield return "AA_Desc_ResearchFinish".Translate(finishBelow.ToStringPercent());
        }
    }

    /// <summary>Adds work to construction frames that have all their materials, completing them when the work is done.</summary>
    public class Effect_Construct : SpellEffect
    {
        public float work = 500f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (!(ctx.caster is Pawn worker)) return;
            float w = work * Mathf.Max(0.2f, ctx.HealFactor);
            foreach (var t in ColonyFx.ThingsIn(ctx, info))
            {
                if (!(t is Frame f) || f.Destroyed || !ColonyFx.Friendly(ctx, f)) continue;
                if (f.TotalMaterialCost().Any(tc => f.ThingCountNeeded(tc.thingDef) > 0)) continue;
                f.workDone += w;
                ColonyFx.Sparkle(ctx.map, f.DrawPos, ctx.Color, 3f);
                if (f.workDone < f.WorkToBuild) continue;
                try
                {
                    f.CompleteConstruction(worker);
                }
                catch (System.Exception e)
                {
                    Log.Warning("[Arcanum Ascendant] Could not complete " + f + ": " + e.Message);
                }
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Construct".Translate((work * Mathf.Max(0.2f, ctx?.HealFactor ?? 1f)).ToString("F0"));
        }
    }

    /// <summary>Takes work off the bills in progress (unfinished things) in the area.</summary>
    public class Effect_Craft : SpellEffect
    {
        public float work = 1000f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            float w = work * Mathf.Max(0.2f, ctx.HealFactor);
            foreach (var t in ColonyFx.ThingsIn(ctx, info))
            {
                if (!(t is UnfinishedThing u) || u.Destroyed || u.workLeft <= 0f || !ColonyFx.Friendly(ctx, u)) continue;
                u.workLeft = Mathf.Max(0f, u.workLeft - w);
                ColonyFx.Sparkle(ctx.map, u.DrawPos, ctx.Color, 3f);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Craft".Translate((work * Mathf.Max(0.2f, ctx?.HealFactor ?? 1f)).ToString("F0"));
        }
    }

    /// <summary>Harvests every ripe plant in the area at once (only crops in growing zones unless wild is set).</summary>
    public class Effect_Harvest : SpellEffect
    {
        public float yieldFactor = 1f;
        public bool wild;
        public bool trees;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int total = 0;
            foreach (var c in info.Cells(map))
            {
                var plant = c.GetPlant(map);
                if (plant == null || plant.Destroyed || !plant.HarvestableNow || plant.def.plant.harvestedThingDef == null) continue;
                if (plant.def.plant.IsTree && !trees) continue;
                if (!wild && !(map.zoneManager.ZoneAt(c) is Zone_Growing)) continue;
                int n = Mathf.RoundToInt(plant.YieldNow() * yieldFactor);
                var def = plant.def.plant.harvestedThingDef;
                try
                {
                    plant.PlantCollected(ctx.caster, PlantDestructionMode.Cut);
                }
                catch
                {
                    if (!plant.Destroyed) plant.Destroy();
                }
                while (n > 0)
                {
                    var made = ThingMaker.MakeThing(def);
                    made.stackCount = Mathf.Min(n, def.stackLimit);
                    n -= made.stackCount;
                    total += made.stackCount;
                    GenPlace.TryPlaceThing(made, c, map, ThingPlaceMode.Near);
                }
                if (Rand.Chance(0.4f) && ColonyFx.Visible(map)) Fx.Burst(map, c.ToVector3Shifted(), ctx.Color, 2f, PMat.Leaf, 1f, 0.8f, 0.2f, PFlags.Wobble);
            }
            if (total > 0 && ctx.caster != null && ctx.Once("harvest")) ArcaneFx.Text(ctx.caster, "AA_Harvested".Translate(total), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return (wild ? "AA_Desc_HarvestWild" : "AA_Desc_Harvest").Translate(yieldFactor.ToStringPercent());
        }
    }

    /// <summary>Sows each growing zone's crop into its empty cells inside the area.</summary>
    public class Effect_Sow : SpellEffect
    {
        public float growth = 0.05f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            foreach (var c in info.Cells(map))
            {
                if (!(map.zoneManager.ZoneAt(c) is Zone_Growing zone)) continue;
                var def = zone.GetPlantDefToGrow();
                if (def == null || c.GetPlant(map) != null || c.GetFirstItem(map) != null || c.GetEdifice(map) != null) continue;
                if (!PlantUtility.CanNowPlantAt(def, c, map)) continue;
                var plant = (Plant)ThingMaker.MakeThing(def);
                plant.Growth = Mathf.Clamp01(growth);
                plant.sown = true;
                GenSpawn.Spawn(plant, c, map);
                if (Rand.Chance(0.25f) && ColonyFx.Visible(map)) Fx.Burst(map, c.ToVector3Shifted(), ctx.Color, 1f, PMat.Leaf, 0.6f, 0.7f, 0.18f, PFlags.Wobble);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Sow".Translate();
        }
    }

    /// <summary>Tame animals fill up with milk and wool, and egg-layers get closer to their next egg.</summary>
    public class Effect_AnimalProduce : SpellEffect
    {
        public float fraction = 0.5f;

        private static readonly FieldInfo Fullness = typeof(CompHasGatherableBodyResource).GetField("fullness", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo EggProgress = typeof(CompEggLayer).GetField("eggProgress", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public Effect_AnimalProduce()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.RaceProps.Animal) return;
            bool any = false;
            foreach (var comp in p.AllComps)
            {
                if (comp is CompHasGatherableBodyResource g && Fullness != null)
                {
                    Fullness.SetValue(g, Mathf.Clamp01((float)Fullness.GetValue(g) + fraction));
                    any = true;
                }
                else if (comp is CompEggLayer e && EggProgress != null)
                {
                    EggProgress.SetValue(e, Mathf.Clamp01((float)EggProgress.GetValue(e) + fraction));
                    any = true;
                }
            }
            if (any) ColonyFx.Sparkle(p.Map, p.DrawPos, ctx.Color, 3f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_AnimalProduce".Translate(fraction.ToStringPercent());
        }
    }

    /// <summary>Advances a tame animal's training by a number of steps.</summary>
    public class Effect_TrainAnimal : SpellEffect
    {
        public int steps = 2;

        public Effect_TrainAnimal()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var tr = p.training;
            if (tr == null || !p.RaceProps.Animal || p.Faction != ctx.caster?.Faction) return;
            int done = 0;
            for (int i = 0; i < steps + ctx.mods.extraTargets; i++)
            {
                var td = tr.NextTrainableToTrain() ?? DefDatabase<TrainableDef>.AllDefsListForReading
                    .Where(d => !tr.HasLearned(d) && tr.CanAssignToTrain(d).Accepted).OrderBy(d => d.listPriority).FirstOrDefault();
                if (td == null) break;
                tr.Train(td, ctx.caster);
                done++;
            }
            if (done > 0) ArcaneFx.Text(p, "AA_Trained".Translate(done), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Train".Translate(steps);
        }
    }

    /// <summary>Bonds a tame animal with the caster (who becomes its master).</summary>
    public class Effect_BondAnimal : SpellEffect
    {
        public Effect_BondAnimal()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var c = ctx.caster;
            if (c?.relations == null || !p.RaceProps.Animal || p.Faction != c.Faction || p.relations == null) return;
            if (!c.relations.DirectRelationExists(PawnRelationDefOf.Bond, p)) c.relations.AddDirectRelation(PawnRelationDefOf.Bond, p);
            if (p.playerSettings != null) p.playerSettings.Master = c;
            Romance.LinkFx(c, p, ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_BondAnimal".Translate();
        }
    }

    /// <summary>Softens prisoners (resistance and will) and, with Ideology, makes slaves more suppressed.</summary>
    public class Effect_Prisoner : SpellEffect
    {
        public float resistance = 5f;
        public float will;
        public float suppression;

        public Effect_Prisoner()
        {
            affects = TargetFilter.AllButSelf;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            float f = Mathf.Sqrt(Mathf.Max(0.1f, ctx.DamageFactor));
            if (p.IsPrisonerOfColony && p.guest != null)
            {
                if (resistance != 0f) p.guest.resistance = Mathf.Max(0f, p.guest.resistance - resistance * f);
                if (will != 0f) p.guest.will = Mathf.Max(0f, p.guest.will - will * f);
                ArcaneFx.Text(p, "AA_Softened".Translate(p.guest.resistance.ToString("F1")), ctx.Color);
            }
            else if (p.IsSlaveOfColony && suppression != 0f)
            {
                var need = p.needs?.TryGetNeed(Dlc.Def<NeedDef>("Suppression"));
                if (need != null) need.CurLevel = Mathf.Clamp01(need.CurLevel + suppression);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (resistance != 0f) yield return "AA_Desc_Resistance".Translate(resistance.ToString("0.#"));
            if (will != 0f) yield return "AA_Desc_Will".Translate(will.ToString("0.#"));
            if (suppression != 0f) yield return "AA_Desc_Suppression".Translate(suppression.ToStringPercent());
        }
    }

    /// <summary>Cleans filth (and optionally snow and Biotech pollution) from the area.</summary>
    public class Effect_Clean : SpellEffect
    {
        public bool filth = true;
        public bool snow;
        public bool pollution;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int n = 0;
            foreach (var c in info.Cells(map))
            {
                if (filth)
                    foreach (var t in c.GetThingList(map).ToList())
                        if (t is Filth && !t.Destroyed)
                        {
                            t.Destroy();
                            n++;
                        }
                if (snow && map.snowGrid.GetDepth(c) > 0f) map.snowGrid.SetDepth(c, 0f);
                if (pollution && Dlc.Biotech && map.pollutionGrid != null && map.pollutionGrid.IsPolluted(c))
                {
                    map.pollutionGrid.SetPolluted(c, false);
                    n++;
                }
                if (Rand.Chance(0.05f) && ColonyFx.Visible(map)) Fx.Burst(map, c.ToVector3Shifted(), ctx.Color, 1f, PMat.Mote, 0.8f, 0.6f, 0.2f);
            }
            if (n > 0 && ctx.caster != null && ctx.Once("clean")) ArcaneFx.Text(ctx.caster, "AA_Cleaned".Translate(n), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (filth) yield return "AA_Desc_CleanFilth".Translate();
            if (snow) yield return "AA_Desc_CleanSnow".Translate();
            if (pollution) yield return "AA_Desc_CleanPollution".Translate();
        }
    }

    /// <summary>Charges batteries and refuels refuelable buildings in the area.</summary>
    public class Effect_Charge : SpellEffect
    {
        /// <summary>Watt-days added to each battery.</summary>
        public float energy = 200f;
        /// <summary>Fraction of a refuelable building's capacity added.</summary>
        public float fuel;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            float f = Mathf.Max(0.2f, ctx.DamageFactor);
            foreach (var t in ColonyFx.ThingsIn(ctx, info))
            {
                if (!(t is ThingWithComps tw) || !ColonyFx.Friendly(ctx, t)) continue;
                bool any = false;
                var bat = tw.GetComp<CompPowerBattery>();
                if (bat != null && energy > 0f)
                {
                    bat.AddEnergy(energy * f);
                    any = true;
                }
                var fuelComp = tw.GetComp<CompRefuelable>();
                if (fuelComp != null && fuel > 0f)
                {
                    fuelComp.Refuel(fuelComp.Props.fuelCapacity * fuel);
                    any = true;
                }
                if (any && ColonyFx.Visible(ctx.map)) Fx.Sparks(ctx.map, t.DrawPos, ctx.Color, 6f, 2f);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (energy > 0f) yield return "AA_Desc_ChargeBattery".Translate((energy * Mathf.Max(0.2f, ctx?.DamageFactor ?? 1f)).ToString("F0"));
            if (fuel > 0f) yield return "AA_Desc_Refuel".Translate(fuel.ToStringPercent());
        }
    }

    /// <summary>Fires one of a list of incidents on the map (looked up by name; missing or impossible ones are skipped).</summary>
    public class Effect_Incident : SpellEffect
    {
        public List<string> options = new List<string>();
        /// <summary>Threat points for incidents that use them (0 = the storyteller's usual amount).</summary>
        public float points;
        public string descKey;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (!ctx.Once("incident")) return;
            var map = ctx.map;
            foreach (var name in options.InRandomOrder())
            {
                var def = Dlc.Def<IncidentDef>(name);
                if (def == null) continue;
                var parms = StorytellerUtility.DefaultParmsNow(def.category, map);
                parms.forced = true;
                if (points > 0f) parms.points = points * Mathf.Max(0.5f, ctx.DamageFactor);
                try
                {
                    if (def.Worker.CanFireNow(parms) && def.Worker.TryExecute(parms)) return;
                }
                catch (System.Exception e)
                {
                    Log.Warning("[Arcanum Ascendant] Incident " + name + " failed: " + e.Message);
                }
            }
            if (ctx.caster != null) ArcaneFx.Text(ctx.caster, "AA_NothingHappens".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (!descKey.NullOrEmpty())
            {
                yield return descKey.Translate();
                yield break;
            }
            var labels = options.Select(o => Dlc.Def<IncidentDef>(o)).Where(d => d != null).Select(d => d.label).ToList();
            if (labels.Count > 0) yield return "AA_Desc_Incident".Translate(labels.ToCommaList(false, true));
        }
    }

    public enum GoodwillMode
    {
        TargetFaction,
        AllNonHostile,
    }

    /// <summary>Improves (or worsens) relations with the target's faction or every friendly faction.</summary>
    public class Effect_Goodwill : SpellEffect
    {
        public int amount = 10;
        public GoodwillMode mode = GoodwillMode.TargetFaction;

        public Effect_Goodwill()
        {
            affects = TargetFilter.AllButSelf;
        }

        public override bool AppliesToPawns => mode == GoodwillMode.TargetFaction;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var f = p.Faction;
            if (f == null || f.IsPlayer || f.def.PermanentlyHostileTo(FactionDefOf.PlayerColony) || !ctx.Once("goodwill" + f.loadID)) return;
            f.TryAffectGoodwillWith(Faction.OfPlayer, amount, true, true);
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (mode != GoodwillMode.AllNonHostile || !ctx.Once("goodwillAll")) return;
            foreach (var f in Find.FactionManager.AllFactionsVisible)
                if (!f.IsPlayer && !f.HostileTo(Faction.OfPlayer) && !f.def.PermanentlyHostileTo(FactionDefOf.PlayerColony))
                    f.TryAffectGoodwillWith(Faction.OfPlayer, amount, true, true);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return ("AA_Desc_Goodwill_" + mode).Translate(amount.ToString("+0;-0"));
        }
    }

    /// <summary>Tends every condition that needs it, at a fixed quality.</summary>
    public class Effect_Tend : SpellEffect
    {
        public float quality = 0.8f;

        public Effect_Tend()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            float q = Mathf.Clamp01(quality * Mathf.Min(1.3f, Mathf.Sqrt(Mathf.Max(0.1f, ctx.HealFactor))));
            int n = 0;
            foreach (var h in p.health.hediffSet.hediffs.ToList())
            {
                if (!h.TendableNow()) continue;
                h.Tended(q, 1f, n);
                n++;
            }
            if (n > 0) ArcaneFx.Text(p, "AA_Tended".Translate(n, q.ToStringPercent()), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Tend".Translate(quality.ToStringPercent());
        }
    }

    /// <summary>Speeds immunity against every disease the target is fighting.</summary>
    public class Effect_Immunize : SpellEffect
    {
        public float amount = 0.25f;

        public Effect_Immunize()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var imm = p.health?.immunity;
            if (imm == null) return;
            int n = 0;
            foreach (var h in p.health.hediffSet.hediffs.ToList())
            {
                if (!(h is HediffWithComps hc) || hc.TryGetComp<HediffComp_Immunizable>() == null) continue;
                imm.TryAddImmunityRecord(h.def, h.def);
                var rec = imm.GetImmunityRecord(h.def);
                if (rec == null) continue;
                rec.immunity = Mathf.Min(1f, rec.immunity + amount * Mathf.Sqrt(Mathf.Max(0.1f, ctx.HealFactor)));
                n++;
            }
            if (n > 0) ArcaneFx.Text(p, "AA_Immunized".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Immunize".Translate(amount.ToStringPercent());
        }
    }

    public enum PassionMode
    {
        AddMinor,
        Upgrade,
    }

    /// <summary>Kindles a passion: none becomes minor, or (Upgrade) a minor passion becomes burning.</summary>
    public class Effect_Passion : SpellEffect
    {
        public PassionMode mode = PassionMode.AddMinor;
        public SkillDef skill;

        public Effect_Passion()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p.skills == null) return;
            SkillRecord s;
            if (skill != null)
            {
                s = p.skills.GetSkill(skill);
                if (s == null || s.TotallyDisabled) return;
            }
            else
            {
                var pool = p.skills.skills.Where(x => !x.TotallyDisabled && (mode == PassionMode.AddMinor ? x.passion == Passion.None : x.passion != Passion.Major));
                s = pool.MaxByWithFallback(x => x.Level + (x.passion == Passion.Minor ? 5 : 0));
            }
            if (s == null) return;
            var before = s.passion;
            if (s.passion == Passion.None) s.passion = Passion.Minor;
            else if (mode == PassionMode.Upgrade && s.passion == Passion.Minor) s.passion = Passion.Major;
            if (s.passion != before)
            {
                ArcaneFx.Text(p, "AA_PassionKindled".Translate(s.def.LabelCap), ctx.Color);
                ColonyFx.Sparkle(p.Map, p.DrawPos, ctx.Color, 6f);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return ("AA_Desc_Passion_" + mode).Translate(skill?.label ?? "AA_BestSkill".Translate().ToString());
        }
    }

    /// <summary>Removes the first trait it finds from a list, or grants one (when it doesn't conflict).</summary>
    public class Effect_Trait : SpellEffect
    {
        public TraitDef add;
        public int degree;
        public List<TraitDef> remove = new List<TraitDef>();

        public Effect_Trait()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var traits = p.story?.traits;
            if (traits == null) return;
            var gone = traits.allTraits.FirstOrDefault(t => remove.Contains(t.def) && !t.ScenForced);
            if (gone != null)
            {
                traits.RemoveTrait(gone);
                ArcaneFx.Text(p, "AA_TraitRemoved".Translate(gone.LabelCap), ctx.Color);
            }
            if (add != null && !traits.HasTrait(add) && !traits.allTraits.Any(t => t.def.ConflictsWith(add) || add.ConflictsWith(t)))
            {
                var t = new Trait(add, degree);
                traits.GainTrait(t);
                ArcaneFx.Text(p, "AA_TraitGained".Translate(t.LabelCap), ctx.Color);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (!remove.NullOrEmpty()) yield return "AA_Desc_TraitRemove".Translate(remove.Select(t => t.degreeDatas?.FirstOrDefault()?.label ?? t.label).ToCommaList(false, true));
            if (add != null) yield return "AA_Desc_TraitAdd".Translate(add.DataAtDegree(degree)?.label ?? add.label);
        }
    }

    /// <summary>Starts a party (or another gathering) with the caster as its organizer.</summary>
    public class Effect_Gathering : SpellEffect
    {
        public string kind = "Party";

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (!ctx.Once("gathering")) return;
            var def = Dlc.Def<GatheringDef>(kind) ?? Dlc.Def<GatheringDef>("Party");
            if (def == null || ctx.caster == null) return;
            bool ok = false;
            try
            {
                ok = def.Worker.CanExecute(ctx.map, ctx.caster) && def.Worker.TryExecute(ctx.map, ctx.caster);
            }
            catch (System.Exception e)
            {
                Log.Warning("[Arcanum Ascendant] Gathering " + kind + " failed: " + e.Message);
            }
            if (!ok) ArcaneFx.Text(ctx.caster, "AA_GatheringFailed".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Gathering".Translate(Dlc.Def<GatheringDef>(kind)?.label ?? kind);
        }
    }

    /// <summary>Items in the area fly straight to the best storage that will take them.</summary>
    public class Effect_Haul : SpellEffect
    {
        public int maxItems = 30;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var caster = ctx.caster;
            if (caster == null) return;
            int moved = 0;
            int cap = maxItems + ctx.mods.extraTargets * 5;
            foreach (var t in ColonyFx.ThingsIn(ctx, info).ToList())
            {
                if (moved >= cap) break;
                if (t.Destroyed || !t.Spawned || t.def.category != ThingCategory.Item || !t.def.EverHaulable || t.IsForbidden(caster)) continue;
                var pri = StoreUtility.CurrentStoragePriorityOf(t);
                if (!StoreUtility.TryFindBestBetterStoreCellFor(t, caster, map, pri, caster.Faction, out IntVec3 cell)) continue;
                Vector3 from = t.DrawPos;
                t.DeSpawn();
                if (!GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near))
                {
                    GenPlace.TryPlaceThing(t, from.ToIntVec3(), map, ThingPlaceMode.Near);
                    continue;
                }
                ColonyFx.Fly(map, from, cell.ToVector3Shifted(), ctx.Color);
                moved++;
            }
            if (moved > 0 && ctx.Once("haul")) ArcaneFx.Text(caster, "AA_Hauled".Translate(moved), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Haul".Translate(maxItems);
        }
    }

    /// <summary>Smooths natural stone floors and walls in the area.</summary>
    public class Effect_Smooth : SpellEffect
    {
        public bool floors = true;
        public bool walls = true;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            foreach (var c in info.Cells(map))
            {
                var e = c.GetEdifice(map);
                if (walls && e != null && e.def.building?.smoothedThing != null && ctx.caster != null)
                {
                    try
                    {
                        SmoothableWallUtility.SmoothWall(e, ctx.caster);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning("[Arcanum Ascendant] Could not smooth " + e + ": " + ex.Message);
                    }
                    continue;
                }
                if (!floors) continue;
                var terr = c.GetTerrain(map);
                if (terr?.smoothedTerrain != null && e == null) map.terrainGrid.SetTerrain(c, terr.smoothedTerrain);
                if (Rand.Chance(0.08f) && ColonyFx.Visible(map)) Fx.Burst(map, c.ToVector3Shifted(), ctx.Color, 1f, PMat.Dust, 0.8f, 0.6f, 0.25f);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Smooth".Translate();
        }
    }

    /// <summary>Stops the rot: food and corpses in the area become fresh again.</summary>
    public class Effect_Preserve : SpellEffect
    {
        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            foreach (var t in ColonyFx.ThingsIn(ctx, info))
            {
                var rot = (t as ThingWithComps)?.GetComp<CompRottable>();
                if (rot == null || rot.RotProgress <= 0f) continue;
                rot.RotProgress = 0f;
                if (Rand.Chance(0.3f)) ColonyFx.Sparkle(ctx.map, t.DrawPos, ctx.Color, 1f);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Preserve".Translate();
        }
    }

    /// <summary>
    /// Raises the dead as servants of the colony: they work (no fighting), need nothing, and lie down dead again when the
    /// binding ends. Unlike Raise Dead it also takes the colony's own fallen.
    /// </summary>
    public class Effect_RaiseServant : SpellEffect
    {
        public int maxCount = 1;
        public float days = 3f;
        public HediffDef servantHediff;
        public bool animals;

        public Effect_RaiseServant()
        {
            affects = TargetFilter.Corpses;
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse corpse, ImpactInfo info)
        {
            var def = servantHediff ?? Dlc.Def<HediffDef>("AA_RisenServant");
            if (def == null || ctx.caster == null) return;
            if (ctx.hitThisCast.Count(t => t is Pawn sp && sp.health.hediffSet.HasHediff(def)) >= maxCount + ctx.mods.extraTargets) return;
            var p = corpse.InnerPawn;
            if (p == null || p.RaceProps.IsMechanoid || corpse.GetRotStage() == RotStage.Dessicated || (p.kindDef?.isBoss ?? false)) return;
            if (!p.RaceProps.Humanlike && !(animals && p.RaceProps.Animal)) return;
            int ticks = Mathf.RoundToInt(days * GenDate.TicksPerDay * ctx.DurationFactor);
            if (!ArcaneUtility.RaiseUndead(p, ctx, ticks, def)) return;
            if (p.Faction != ctx.caster.Faction) ArcaneUtility.BindToFaction(p, ctx.caster.Faction);
            if (p.Faction == Faction.OfPlayer && p.workSettings != null) p.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
            ctx.hitThisCast.Add(p);
            Messages.Message("AA_ServantRises".Translate(p.LabelShort, (ticks / (float)GenDate.TicksPerDay).ToString("0.#"), p.Named("PAWN")), p, MessageTypeDefOf.NeutralEvent);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_RaiseServant".Translate(maxCount, (days * (ctx?.DurationFactor ?? 1f)).ToString("0.#"));
        }
    }
}

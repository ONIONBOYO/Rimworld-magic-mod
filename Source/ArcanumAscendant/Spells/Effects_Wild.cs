using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArcanumAscendant
{
    // ================================================================================================
    // Effects on the living and the dead: livestock, wild beasts, corpses and graves.
    // ================================================================================================

    /// <summary>Raises corpses as undead labourers who join the colony and work until the binding ends.</summary>
    public class Effect_RaiseLaborer : SpellEffect
    {
        public int maxCount = 3;
        public int durationTicks = 180000;
        public HediffDef laborerHediff;

        public Effect_RaiseLaborer()
        {
            affects = TargetFilter.Corpses;
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse corpse, ImpactInfo info)
        {
            if (laborerHediff == null || ctx.caster?.Faction == null) return;
            if (ctx.hitThisCast.Count(t => t is Pawn) >= maxCount + ctx.mods.extraTargets) return;
            var p = corpse.InnerPawn;
            if (p == null || !p.RaceProps.Humanlike || corpse.GetRotStage() == RotStage.Dessicated || (p.kindDef?.isBoss ?? false)) return;
            if (p.Faction == ctx.caster.Faction) return;
            if (!corpse.Spawned) return;
            var map = corpse.Map;
            var pos = corpse.DrawPos;
            if (!ResurrectionUtility.TryResurrect(p, new ResurrectionParams { gettingScarsChance = 0f, restoreMissingParts = true, noLord = true, removeDiedThoughts = true }))
                return;
            if (p.Faction != ctx.caster.Faction) p.SetFaction(ctx.caster.Faction);
            p.mindState?.mentalStateHandler?.Reset();
            p.workSettings?.EnableAndInitializeIfNotAlreadyInitialized();
            ArcaneUtility.HealPawn(p, 9999f);
            var h = HediffMaker.MakeHediff(laborerHediff, p);
            p.health.AddHediff(h);
            var comp = (h as HediffWithComps)?.TryGetComp<HediffComp_Laborer>();
            if (comp != null) comp.ticksLeft = Mathf.RoundToInt(durationTicks * ctx.DurationFactor);
            ctx.hitThisCast.Add(p);
            ArcaneVfxManager.For(map).Add(new VfxPillar(pos, 40, ctx.Color, ctx.Color2, 1.2f));
            ArcaneFx.Text(p, "AA_RisesToWork".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_RaiseLaborer".Translate(maxCount, (durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    /// <summary>Opens the graves in an area and raises their dead to fight.</summary>
    public class Effect_Exhume : SpellEffect
    {
        public int maxGraves = 4;
        public int durationTicks = 60000;
        public HediffDef undeadHediff;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (n >= maxGraves + ctx.mods.extraTargets) break;
                if (!(t is Building_Grave g) || !g.HasCorpse) continue;
                var corpse = g.Corpse;
                var p = corpse?.InnerPawn;
                if (p == null || p.RaceProps.IsMechanoid) continue;
                g.EjectContents();
                if (!corpse.Spawned) continue;
                if (ArcaneUtility.RaiseUndead(p, ctx, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), undeadHediff ?? ArcaneDefOf.AA_Undead)) n++;
            }
            if (n == 0) HearthUtil.Note(ctx, info, "AA_NoGraves".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Exhume".Translate(maxGraves, (durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    /// <summary>Animates the wreck of a dead mechanoid to fight for the caster for a while.</summary>
    public class Effect_ReanimateWreck : SpellEffect
    {
        public int maxCount = 2;
        public int durationTicks = 30000;
        public float maxCombatPower = 400f;

        public Effect_ReanimateWreck()
        {
            affects = TargetFilter.Corpses;
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse corpse, ImpactInfo info)
        {
            if (ctx.hitThisCast.Count(t => t is Pawn) >= maxCount + ctx.mods.extraTargets) return;
            var p = corpse.InnerPawn;
            if (p == null || !p.RaceProps.IsMechanoid || (p.kindDef?.isBoss ?? false) || (p.kindDef?.combatPower ?? 0f) > maxCombatPower) return;
            if (ArcaneUtility.RaiseUndead(p, ctx, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), ArcaneDefOf.AA_Undead)) ctx.hitThisCast.Add(p);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_ReanimateWreck".Translate(maxCount, (durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    /// <summary>Butchers the animal carcasses in an area.</summary>
    public class Effect_Butcher : SpellEffect
    {
        public float efficiency = 1f;
        public int maxCorpses = 10;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (n >= maxCorpses) break;
                if (!(t is Corpse c) || c.InnerPawn == null || !c.InnerPawn.RaceProps.Animal || c.GetRotStage() != RotStage.Fresh) continue;
                var pos = c.Position;
                List<Thing> products;
                try { products = c.ButcherProducts(ctx.caster, efficiency).ToList(); }
                catch { continue; }
                c.Destroy();
                foreach (var pr in products) HearthUtil.PlaceNear(pr, pos, map);
                FilthMaker.TryMakeFilth(pos, map, ThingDefOf.Filth_Blood, 2);
                n++;
                ArcaneFx.Particles(map, pos.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Blood, 4, 1f, 0.5f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Flayed".Translate(n) : "AA_NoCarcasses".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Butcher".Translate();
        }
    }

    /// <summary>Stops corpses and food in an area from rotting for a long time.</summary>
    public class Effect_Preserve : SpellEffect
    {
        public int durationTicks = 900000;
        public bool items = true;
        public bool corpses = true;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var tracker = MapComponent_ArcaneColony.For(ctx.map);
            int until = Find.TickManager.TicksGame + Mathf.RoundToInt(durationTicks * ctx.DurationFactor);
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (t is Corpse c ? !corpses || c.GetRotStage() == RotStage.Dessicated : !items || t.def.category != ThingCategory.Item) continue;
                var rot = t.TryGetComp<CompRottable>();
                if (rot == null) continue;
                rot.RotProgress = 0f;
                tracker.AddPreserved(t, until);
                n++;
                if (n < 20) ArcaneFx.Particles(ctx.map, t.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Mote, 2, 0.6f, 0.5f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Preserved".Translate(n) : "AA_NothingToPreserve".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Preserve".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    /// <summary>Hostile animals and insects nearby are overcome with terror and flee.</summary>
    public class Effect_ScareBeasts : SpellEffect
    {
        public bool insects = true;

        public Effect_ScareBeasts()
        {
            affects = TargetFilter.AllButSelf;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!(p.RaceProps.Animal || (insects && p.RaceProps.Insect))) return;
            bool hostile = ctx.IsEnemy(p) || (p.InMentalState && p.MentalStateDef.IsAggro);
            if (!hostile || (p.kindDef?.isBoss ?? false)) return;
            if (p.InMentalState) p.MentalState.RecoverFromState();
            p.mindState?.mentalStateHandler?.TryStartMentalState(MentalStateDefOf.PanicFlee, null, true);
            ArcaneFx.Text(p, "AA_Terrified".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_ScareBeasts".Translate();
        }
    }

    /// <summary>Ends the rage of manhunting and berserk animals.</summary>
    public class Effect_CalmBeasts : SpellEffect
    {
        public Effect_CalmBeasts()
        {
            affects = TargetFilter.AllButSelf;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.RaceProps.Animal || !p.InMentalState || !p.MentalStateDef.IsAggro) return;
            p.MentalState.RecoverFromState();
            p.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            ArcaneFx.Text(p, "AA_Calmed".Translate(), ctx.Color);
            ArcaneFx.Particles(p.Map, p.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Leaf, 4, 0.8f, 0.5f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_CalmBeasts".Translate();
        }
    }

    /// <summary>Brings the colony's female animals into season: each one with a mate of her kind in the colony is bred.</summary>
    public class Effect_BreedAnimals : SpellEffect
    {
        public Effect_BreedAnimals()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.RaceProps.Animal || p.gender != Gender.Female || p.Faction == null || !p.Spawned) return;
            if (!(p.ageTracker?.CurLifeStage?.reproductive ?? false) || PawnRequirement.IsPregnant(p)) return;
            if (p.health.hediffSet.HasHediff(HediffDefOf.Sterilized)) return;
            var male = p.Map.mapPawns.SpawnedPawnsInFaction(p.Faction)
                .Where(m => m.def == p.def && m.gender == Gender.Male && (m.ageTracker?.CurLifeStage?.reproductive ?? false) && !m.health.hediffSet.HasHediff(HediffDefOf.Sterilized))
                .OrderBy(m => m.Position.DistanceTo(p.Position)).FirstOrDefault();
            if (male == null)
            {
                ArcaneFx.Text(p, "AA_NoMate".Translate(), Color.gray);
                return;
            }
            var eggs = p.GetComp<CompEggLayer>();
            if (eggs != null) eggs.Fertilize(male);
            else
            {
                var h = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.Pregnant, p);
                h.SetParents(null, male, null);
                p.health.AddHediff(h);
            }
            ArcaneFx.Text(p, "AA_Bred".Translate(), ctx.Color);
            ArcaneFx.Particles(p.Map, p.DrawPos, ctx.Color2, ArcaneDefOf.AA_Fleck_Petal, 5, 0.8f, 0.5f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_BreedAnimals".Translate();
        }
    }

    /// <summary>Fills animals' milk, wool and eggs (fill) or collects whatever is ready (collect).</summary>
    public class Effect_AnimalProduce : SpellEffect
    {
        public bool fill = true;
        public bool collect;

        private static readonly FieldInfo FullnessField = typeof(CompHasGatherableBodyResource).GetField("fullness", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo EggProgressField = typeof(CompEggLayer).GetField("eggProgress", BindingFlags.NonPublic | BindingFlags.Instance);

        public Effect_AnimalProduce()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.RaceProps.Animal) return;
            bool any = false;
            foreach (var c in p.AllComps)
            {
                if (c is CompHasGatherableBodyResource g)
                {
                    if (fill && FullnessField != null)
                    {
                        FullnessField.SetValue(g, 1f);
                        any = true;
                    }
                    if (collect && ctx.caster != null && g.ActiveAndFull)
                    {
                        g.Gathered(ctx.caster);
                        any = true;
                    }
                }
                else if (c is CompEggLayer e && fill && EggProgressField != null && (p.ageTracker?.CurLifeStage?.reproductive ?? false))
                {
                    EggProgressField.SetValue(e, 1f);
                    any = true;
                }
            }
            if (any) ArcaneFx.Particles(p.Map, p.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Leaf, 3, 0.8f, 0.5f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (fill) yield return "AA_Desc_FillProduce".Translate();
            if (collect) yield return "AA_Desc_CollectProduce".Translate();
        }
    }

    /// <summary>Fully trains a colony animal in everything it can learn, and bonds it to the caster.</summary>
    public class Effect_TrainAnimal : SpellEffect
    {
        public bool bond = true;

        public Effect_TrainAnimal()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var caster = ctx.caster;
            if (!p.RaceProps.Animal || p.training == null || caster == null || p.Faction != caster.Faction) return;
            int n = 0;
            foreach (var td in DefDatabase<TrainableDef>.AllDefsListForReading)
            {
                if (p.training.HasLearned(td) || !p.training.CanAssignToTrain(td).Accepted) continue;
                p.training.Train(td, caster, true);
                n++;
            }
            if (bond && caster.RaceProps.Humanlike && !caster.relations.DirectRelationExists(PawnRelationDefOf.Bond, p))
            {
                caster.relations.AddDirectRelation(PawnRelationDefOf.Bond, p);
                if (p.playerSettings != null) p.playerSettings.Master = caster;
            }
            ArcaneFx.Text(p, n > 0 ? "AA_Trained".Translate(n) : "AA_Bonded".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_TrainAnimal".Translate();
        }
    }

    /// <summary>Young animals grow to adulthood in a moment.</summary>
    public class Effect_GrowAnimals : SpellEffect
    {
        public Effect_GrowAnimals()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var at = p.ageTracker;
            if (!p.RaceProps.Animal || at == null || at.Adult) return;
            at.AgeBiologicalTicks = System.Math.Max(at.AgeBiologicalTicks, at.AdultMinAgeTicks + 2500);
            ArcaneFx.Text(p, "AA_GrownUp".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_GrowAnimals".Translate();
        }
    }

    /// <summary>A new animal joins the colony, bonded to the caster.</summary>
    public class Effect_SpawnTamedAnimal : SpellEffect
    {
        public PawnKindDef kind;
        public bool bond = true;
        public HediffDef hediff;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var caster = ctx.caster;
            if (kind == null || caster?.Faction == null) return;
            var map = ctx.map;
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, caster.Faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true,
                canGeneratePawnRelations: false, allowFood: false));
            if (pawn.Name == null || pawn.Name.Numerical) pawn.Name = PawnBioAndNameGenerator.GeneratePawnName(pawn, NameStyle.Full);
            var cell = CellFinder.StandableCellNear(info.cell, map, 3f);
            GenSpawn.Spawn(pawn, cell.IsValid ? cell : info.cell, map);
            if (hediff != null) pawn.health.AddHediff(hediff);
            if (bond && caster.RaceProps.Humanlike)
            {
                caster.relations.AddDirectRelation(PawnRelationDefOf.Bond, pawn);
                if (pawn.playerSettings != null) pawn.playerSettings.Master = caster;
            }
            ArcaneVfxManager.For(map).Add(new VfxPillar(pawn.DrawPos, 50, ctx.Color, ctx.Color2, 1.4f));
            HeartUtil.Letter("AA_AnimalJoinsLabel", "AA_AnimalJoinsText", LetterDefOf.PositiveEvent, new LookTargets(pawn),
                pawn.LabelShort.Named("NAME"), kind.label.Named("KIND"), caster.LabelShort.Named("CASTER"));
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (kind != null) yield return "AA_Desc_SpawnTamed".Translate(kind.label);
        }
    }

    /// <summary>Kills a wild animal so cleanly that its hide and meat are untouched.</summary>
    public class Effect_CleanKill : SpellEffect
    {
        public float maxBodySize = 2.5f;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.RaceProps.Animal || p.Faction != null || (p.kindDef?.isBoss ?? false)) return;
            if (p.BodySize > maxBodySize * Mathf.Sqrt(Mathf.Max(0.5f, ctx.DamageFactor)))
            {
                ArcaneFx.Text(p, "AA_TooBigToKill".Translate(), Color.gray);
                return;
            }
            ArcaneFx.Text(p, "AA_CleanKill".Translate(), ctx.Color);
            p.Kill(null);
            ctx.arcane?.Notify_Kill(p, ctx);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_CleanKill".Translate(maxBodySize.ToString("F1"));
        }
    }

    /// <summary>Speeds up the fermenting barrels in an area.</summary>
    public class Effect_Ferment : SpellEffect
    {
        public float progress = 1f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (!(t is Building_FermentingBarrel b) || b.SpaceLeftForWort >= Building_FermentingBarrel.MaxCapacity || b.Fermented) continue;
                b.Progress = Mathf.Min(1f, b.Progress + progress * HearthUtil.Power(ctx));
                n++;
                ArcaneFx.Particles(ctx.map, t.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Smoke, 3, 0.6f, 0.6f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Fermented".Translate(n) : "AA_NoBarrels".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Ferment".Translate(progress.ToStringPercent());
        }
    }

    /// <summary>Seals a pawn in an ice coffin where time does not touch them, like a cryptosleep casket.</summary>
    public class Effect_EncaseInIce : SpellEffect
    {
        public ThingDef coffin;
        public int durationTicks = 900000;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (coffin == null || !p.Spawned || p.RaceProps.IsMechanoid) return;
            if (!(ctx.IsAlly(p) || p.Downed)) return;
            var map = p.Map;
            var pos = p.Position;
            if (pos.GetEdifice(map) != null)
            {
                ArcaneFx.Text(p, "AA_NoRoomForCoffin".Translate(), Color.gray);
                return;
            }
            p.jobs?.StopAll();
            p.DeSpawn();
            var t = ThingMaker.MakeThing(coffin);
            if (ctx.caster?.Faction != null && t.def.CanHaveFaction) t.SetFaction(ctx.caster.Faction);
            var c = (Building_IceCoffin)GenSpawn.Spawn(t, pos, map, WipeMode.VanishOrMoveAside);
            c.ticksLeft = Mathf.RoundToInt(durationTicks * ctx.DurationFactor);
            if (!c.TryAcceptThing(p))
            {
                GenSpawn.Spawn(p, pos, map);
                c.Destroy();
                return;
            }
            ArcaneFx.Flash(map, pos.ToVector3Shifted(), ctx.Color, 2.5f);
            ArcaneFx.Particles(map, pos.ToVector3Shifted(), ctx.Color2, ArcaneDefOf.AA_Fleck_Shard, 14, 2f, 0.5f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_IceCoffin".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }
}

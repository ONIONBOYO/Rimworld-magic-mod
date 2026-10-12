using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ArcanumAscendant
{
    /// <summary>Shared study logic: attunement for pawns who read a grimoire, experience for mages.</summary>
    public static class ArcaneStudy
    {
        /// <summary>Who would benefit from studying right now.</summary>
        public static bool WantsToStudy(Pawn p)
        {
            var c = p?.TryGetComp<CompArcane>();
            if (c == null) return false;
            if (c.attuning) return true;
            return c.IsMage && !c.ReadyToAscend && c.level < CompArcane.MaxLevel;
        }

        /// <summary>Applies one interval of study. Returns false once there is nothing more to gain.</summary>
        public static bool StudyTick(Pawn p, int delta)
        {
            var c = p.TryGetComp<CompArcane>();
            if (c == null) return false;
            float f = CompArcane.StudyFactor(p);
            if (f <= 0f) return false;
            p.skills?.Learn(SkillDefOf.Intellectual, 0.02f * delta);
            if (c.attuning)
            {
                c.AddAttunement(f * delta / (CompArcane.AttunementHours * GenDate.TicksPerHour));
                return c.attuning;
            }
            if (!c.IsMage || c.ReadyToAscend) return false;
            c.GainXp(CompArcane.StudyXpPerHour * f * delta / GenDate.TicksPerHour);
            return !c.ReadyToAscend;
        }
    }

    /// <summary>Study at an arcane lectern. The same driver serves the ordered job and the recreation job.</summary>
    public class JobDriver_ArcaneStudy : JobDriver
    {
        private const int OrderedDuration = 10000;

        private Thing Lectern => job.GetTarget(TargetIndex.A).Thing;
        private bool IsJoy => job.def.joyKind != null;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            var study = ToilMaker.MakeToil("ArcaneStudy");
            study.defaultCompleteMode = ToilCompleteMode.Delay;
            study.defaultDuration = IsJoy && job.def.joyDuration > 0 ? job.def.joyDuration : OrderedDuration;
            study.handlingFacing = true;
            study.socialMode = RandomSocialMode.Off;
            study.initAction = () => pawn.rotationTracker.FaceTarget(Lectern);
            study.tickIntervalAction = delta =>
            {
                pawn.rotationTracker.FaceTarget(Lectern);
                bool more = ArcaneStudy.StudyTick(pawn, delta);
                if (pawn.IsHashIntervalTick(90, delta) && pawn.Map != null)
                {
                    var c = pawn.TryGetComp<CompArcane>();
                    var col = c?.DisplayClass?.color ?? new Color(0.75f, 0.55f, 1f);
                    ArcaneFx.Particles(pawn.Map, Lectern.DrawPos + new Vector3(0f, 0f, 0.3f), col, ArcaneDefOf.AA_Fleck_Rune ?? ArcaneDefOf.AA_Fleck_Spark, 2, 0.4f, 0.45f);
                }
                if (IsJoy)
                {
                    if (JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.EndJob, 1f, Lectern as Building)) return;
                }
                else if (!more) EndJobWith(JobCondition.Succeeded);
            };
            study.WithEffect(EffecterDefOf.Research, TargetIndex.A);
            study.AddFinishAction(() =>
            {
                if (IsJoy) JoyUtility.TryGainRecRoomThought(pawn);
            });
            yield return study;
        }

        public override string GetReport()
        {
            var c = pawn.TryGetComp<CompArcane>();
            if (c != null && c.attuning) return "AA_StudyReportAttune".Translate(c.attunement.ToStringPercent("F0"));
            return "AA_StudyReport".Translate();
        }
    }

    /// <summary>Recreation: mages and attuning pawns like to spend free time at the lectern, much as psycasters meditate.</summary>
    public class JoyGiver_ArcaneStudy : JoyGiver_InteractBuildingInteractionCell
    {
        public override bool CanBeGivenTo(Pawn pawn) => ArcaneStudy.WantsToStudy(pawn) && base.CanBeGivenTo(pawn);

        public override float GetChance(Pawn pawn)
        {
            if (!ArcaneStudy.WantsToStudy(pawn)) return 0f;
            var c = pawn.TryGetComp<CompArcane>();
            return base.GetChance(pawn) * (c != null && c.attuning ? 3f : 1f);
        }

        public override Job TryGiveJob(Pawn pawn) => ArcaneStudy.WantsToStudy(pawn) ? base.TryGiveJob(pawn) : null;

        public override Job TryGiveJobInGatheringArea(Pawn pawn, IntVec3 gatheringSpot, float maxRadius = -1f) => null;
    }

    public class CompProperties_ArcaneLectern : CompProperties
    {
        public CompProperties_ArcaneLectern()
        {
            compClass = typeof(CompArcaneLectern);
        }
    }

    /// <summary>Adds "study" and "attune" orders to the arcane lectern.</summary>
    public class CompArcaneLectern : ThingComp
    {
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            var c = selPawn.TryGetComp<CompArcane>();
            if (c == null || (!c.IsMage && !c.attuning)) yield break;
            string label = c.attuning ? "AA_StudyAttune".Translate(c.attunement.ToStringPercent("F0")) : "AA_StudyXp".Translate();
            if (c.IsMage && c.ReadyToAscend)
            {
                yield return new FloatMenuOption(label + ": " + "AA_StudyCapped".Translate(CompArcane.CircleLabel(c.circle + 1)), null);
                yield break;
            }
            if (c.IsMage && c.level >= CompArcane.MaxLevel)
            {
                yield return new FloatMenuOption(label + ": " + "AA_StudyMaxed".Translate(), null);
                yield break;
            }
            if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            if (!selPawn.CanReserve(parent))
            {
                yield return new FloatMenuOption(label + ": " + "Reserved".Translate().CapitalizeFirst(), null);
                yield break;
            }
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, () =>
            {
                var job = JobMaker.MakeJob(ArcaneDefOf.AA_ArcaneStudy, parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }), selPawn, parent);
        }
    }

    public class CompProperties_UseEffectAscend : CompProperties_UseEffect
    {
        /// <summary>The circle this sigil raises a mage to.</summary>
        public int circle = 2;

        public CompProperties_UseEffectAscend()
        {
            compClass = typeof(CompUseEffect_Ascend);
        }
    }

    /// <summary>Ascension sigil: breaks a mage's circle level cap, like a neuroformer raises a psylink.</summary>
    public class CompUseEffect_Ascend : CompUseEffect
    {
        public CompProperties_UseEffectAscend Props => (CompProperties_UseEffectAscend)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            usedBy.TryGetComp<CompArcane>()?.Ascend();
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            var c = p.TryGetComp<CompArcane>();
            if (c == null || !c.IsMage) return "AA_NotAMage".Translate();
            if (c.circle >= Props.circle) return "AA_AlreadyCircle".Translate(CompArcane.CircleLabel(Props.circle));
            if (c.circle < Props.circle - 1) return "AA_NeedPreviousCircle".Translate(CompArcane.CircleLabel(Props.circle - 1));
            if (c.level < c.LevelCap) return "AA_NeedLevelForAscension".Translate(c.LevelCap);
            return base.CanBeUsedBy(p);
        }
    }

    /// <summary>Rare event: a colonist's latent gift wakes on its own.</summary>
    public class IncidentWorker_LatentAwakening : IncidentWorker
    {
        private static IEnumerable<Pawn> Candidates(Map map)
        {
            if (map == null) return Enumerable.Empty<Pawn>();
            return map.mapPawns.FreeColonistsSpawned.Where(p =>
            {
                var c = p.TryGetComp<CompArcane>();
                return c != null && !c.IsMage && !c.awakenReady && CompArcane.CanAttune(p).Accepted && !p.IsQuestLodger();
            });
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (ArcanumMod.Settings.freeAwakening) return false;
            return Candidates(parms.target as Map).Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = parms.target as Map;
            if (!Candidates(map).TryRandomElementByWeight(p => Mathf.Max(0.05f, p.GetStatValue(StatDefOf.PsychicSensitivity)), out var pawn)) return false;
            var c = pawn.TryGetComp<CompArcane>();
            c.CompleteAttunement(silent: true);
            ArcaneFx.LevelUpBurst(pawn, new Color(0.75f, 0.55f, 1f), big: true);
            ArcaneDefOf.AA_Awaken?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            SendStandardLetter(parms, pawn, pawn.Named("PAWN"));
            return true;
        }
    }
}

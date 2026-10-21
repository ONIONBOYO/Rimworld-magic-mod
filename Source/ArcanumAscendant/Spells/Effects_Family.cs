using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Love, family and sleep: matchmaking, rekindled marriages, conception, gestation, sleep and calm.
    // Human pregnancy is Biotech content; every DLC def is looked up by name so nothing breaks without it.
    // ------------------------------------------------------------------------------------------------

    public static class Romance
    {
        public static bool AdultHuman(Pawn p) => p != null && !p.Dead && p.RaceProps.Humanlike && p.relations != null && (p.ageTracker?.Adult ?? false);

        public static bool Attracted(Pawn a, Pawn b) => RelationsUtility.AttractedToGender(a, b.gender) && RelationsUtility.AttractedToGender(b, a.gender);

        public static PawnRelationDef Bond(Pawn a, Pawn b) => LovePartnerRelationUtility.ExistingLoveRealtionshipBetween(a, b, false)?.def;

        public static bool Single(Pawn p) => !LovePartnerRelationUtility.HasAnyLovePartner(p, false);

        public static int Rank(PawnRelationDef d)
        {
            if (d == null) return 0;
            if (d == PawnRelationDefOf.Spouse) return 3;
            if (d == PawnRelationDefOf.Fiance) return 2;
            return d == PawnRelationDefOf.Lover ? 1 : 0;
        }

        /// <summary>Could these two become a couple? Already being one counts.</summary>
        public static bool CanPair(Pawn a, Pawn b, bool requireAttraction)
        {
            if (a == b || !AdultHuman(a) || !AdultHuman(b)) return false;
            if (a.relations.FamilyByBlood.Contains(b)) return false;
            if (Bond(a, b) != null) return true;
            if (requireAttraction && !Attracted(a, b)) return false;
            return Single(a) && Single(b);
        }

        /// <summary>How well two people would get on as a couple: attraction both ways, what they already think of each other.</summary>
        public static float Score(Pawn a, Pawn b)
        {
            float attraction = a.relations.SecondaryRomanceChanceFactor(b) * b.relations.SecondaryRomanceChanceFactor(a);
            float opinion = (a.relations.OpinionOf(b) + b.relations.OpinionOf(a)) / 200f;
            return attraction * (1.5f + opinion) + a.relations.CompatibilityWith(b) * 0.15f;
        }

        /// <summary>Raises the couple's relationship to at least the given one (lover, fiance, spouse). Returns the new relation, or null.</summary>
        public static PawnRelationDef Raise(Pawn a, Pawn b, PawnRelationDef target)
        {
            if (target == null) return null;
            var cur = Bond(a, b);
            if (Rank(cur) >= Rank(target)) return null;
            if (cur == null && (!Single(a) || !Single(b))) return null;
            a.relations.TryRemoveDirectRelation(PawnRelationDefOf.ExLover, b);
            a.relations.TryRemoveDirectRelation(PawnRelationDefOf.ExSpouse, b);
            if (target == PawnRelationDefOf.Lover)
            {
                a.relations.AddDirectRelation(PawnRelationDefOf.Lover, b);
                TaleRecorder.RecordTale(TaleDefOf.BecameLover, a, b);
            }
            else
            {
                if (cur == PawnRelationDefOf.Lover) a.relations.TryRemoveDirectRelation(PawnRelationDefOf.Lover, b);
                if (cur != PawnRelationDefOf.Fiance) a.relations.AddDirectRelation(PawnRelationDefOf.Fiance, b);
                // the vanilla wedding does the rest: names, thoughts, the tale
                if (target == PawnRelationDefOf.Spouse) MarriageCeremonyUtility.Married(a, b);
            }
            if (a.Spawned && b.Spawned && a.Map == b.Map) LovePartnerRelationUtility.TryToShareBed(a, b);
            return target;
        }

        public static void GainMemory(Pawn p, ThoughtDef def, Pawn other = null)
        {
            var mem = p?.needs?.mood?.thoughts?.memories;
            if (def == null || mem == null) return;
            bool social = def.thoughtClass != null && typeof(Thought_MemorySocial).IsAssignableFrom(def.thoughtClass);
            if (social && other == null) return;
            mem.TryGainMemory(def, social ? other : null);
        }

        /// <summary>A heart floating from one lover to the other, and a burst of petals around both.</summary>
        public static void LinkFx(Pawn a, Pawn b, Color c)
        {
            if (a?.Map == null || a.Map != Find.CurrentMap) return;
            var map = a.Map;
            if (b != null && b.Spawned && b.Map == map)
            {
                ArcaneVfxManager.For(map).Add(new VfxProjectile(a.DrawPos, b.DrawPos, 40, ArcaneMats.Get("ArcanumAscendant/Vfx/Heart"), c, Color.white, 0.55f, 1.2f, false, false)
                    { style = VfxStyle.Desire });
                ArcaneVfxManager.For(map).Add(new VfxProjectile(b.DrawPos, a.DrawPos, 40, ArcaneMats.Get("ArcanumAscendant/Vfx/Heart"), c, Color.white, 0.55f, 1.2f, false, false)
                    { style = VfxStyle.Desire });
                Fx.Burst(map, b.DrawPos, c, 6f, PMat.Heart, 1.2f, 0.9f, 0.2f, PFlags.Wobble);
            }
            Fx.Burst(map, a.DrawPos, c, 6f, PMat.Heart, 1.2f, 0.9f, 0.2f, PFlags.Wobble);
            Fx.Rise(map, a.DrawPos, Color.white, 4f, PMat.Petal, 0.6f, 0.5f, 1.4f, 0.18f, PFlags.Wobble);
        }
    }

    public enum MatchMode
    {
        /// <summary>The target and its best match among the people of its faction on the map.</summary>
        BestMatch,
        /// <summary>Pairs people inside the area, best matches first.</summary>
        InArea,
        /// <summary>The target and the caster.</summary>
        Caster,
    }

    /// <summary>
    /// Kindles love: the pair gain a mutual opinion memory (and a mood memory), and with a relation set they become lovers,
    /// get engaged or marry, provided both are adults who could love each other and are single or already together.
    /// </summary>
    public class Effect_Matchmake : SpellEffect
    {
        public MatchMode mode = MatchMode.BestMatch;
        public PawnRelationDef relation;
        public ThoughtDef opinionThought;
        public ThoughtDef moodThought;
        public bool requireAttraction = true;
        public int maxPairs = 3;

        public Effect_Matchmake()
        {
            affects = TargetFilter.Allies;
        }

        public override bool AppliesToPawns => mode != MatchMode.InArea;

        private IEnumerable<Pawn> Candidates(Pawn p) => p.Map.mapPawns.AllPawnsSpawned.Where(o => o != p && o.Faction == p.Faction && !o.IsPrisoner && Romance.AdultHuman(o));

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!Romance.AdultHuman(p) || !p.Spawned) return;
            Pawn other = null;
            if (mode == MatchMode.Caster) other = ctx.caster != p && Romance.AdultHuman(ctx.caster) ? ctx.caster : null;
            else
            {
                var partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(p, false);
                // someone already in love is matched with the one they love
                other = partner != null && partner.Spawned && partner.Map == p.Map ? partner
                    : Candidates(p).Where(o => Romance.CanPair(p, o, requireAttraction)).MaxByWithFallback(o => Romance.Score(p, o));
            }
            if (other == null)
            {
                ArcaneFx.Text(p, "AA_NoMatch".Translate(), ctx.Color);
                return;
            }
            Match(ctx, p, other);
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (mode != MatchMode.InArea) return;
            var map = ctx.map;
            var pool = new List<Pawn>();
            foreach (var c in info.Cells(map))
                foreach (var t in c.GetThingList(map))
                    if (t is Pawn p && Romance.AdultHuman(p) && !p.IsPrisoner && ctx.Passes(p, affects) && !pool.Contains(p)) pool.Add(p);
            var pairs = new List<(Pawn, Pawn, float)>();
            for (int i = 0; i < pool.Count; i++)
                for (int j = i + 1; j < pool.Count; j++)
                    if (pool[i].Faction == pool[j].Faction && Romance.CanPair(pool[i], pool[j], requireAttraction))
                        pairs.Add((pool[i], pool[j], Romance.Score(pool[i], pool[j]) + (Romance.Bond(pool[i], pool[j]) != null ? 10f : 0f)));
            var used = new HashSet<Pawn>();
            int made = 0;
            foreach (var (a, b, _) in pairs.OrderByDescending(x => x.Item3))
            {
                if (made >= maxPairs + ctx.mods.extraTargets) break;
                if (used.Contains(a) || used.Contains(b)) continue;
                used.Add(a);
                used.Add(b);
                Match(ctx, a, b);
                made++;
            }
        }

        private void Match(SpellContext ctx, Pawn a, Pawn b)
        {
            if (!ctx.Once("match" + Mathf.Min(a.thingIDNumber, b.thingIDNumber) + "_" + Mathf.Max(a.thingIDNumber, b.thingIDNumber))) return;
            Romance.GainMemory(a, opinionThought, b);
            Romance.GainMemory(b, opinionThought, a);
            Romance.GainMemory(a, moodThought, b);
            Romance.GainMemory(b, moodThought, a);
            PawnRelationDef made = null;
            if (relation != null && a.Faction == b.Faction && !a.IsPrisoner && !b.IsPrisoner && Romance.CanPair(a, b, requireAttraction))
                made = Romance.Raise(a, b, relation);
            Romance.LinkFx(a, b, ctx.Color);
            if (made != null && (PawnUtility.ShouldSendNotificationAbout(a) || PawnUtility.ShouldSendNotificationAbout(b)))
            {
                string key = made == PawnRelationDefOf.Spouse ? "AA_Matchmade_Spouse" : made == PawnRelationDefOf.Fiance ? "AA_Matchmade_Fiance" : "AA_Matchmade_Lover";
                Find.LetterStack.ReceiveLetter((key + "Label").Translate(a.LabelShort, b.LabelShort, a.Named("A"), b.Named("B")),
                    key.Translate(a.LabelShort, b.LabelShort, ctx.caster?.LabelShort ?? "?", a.Named("A"), b.Named("B")), LetterDefOf.PositiveEvent, new LookTargets(a, b));
            }
            else ArcaneFx.Text(a, "AA_Smitten".Translate(b.LabelShort), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return ("AA_Desc_Matchmake_" + mode).Translate();
            if (relation != null) yield return "AA_Desc_MatchmakeRelation".Translate(relation.label);
            if (opinionThought?.stages?.FirstOrDefault() is ThoughtStage os) yield return "AA_Desc_OpinionMemory".Translate(os.label, os.baseOpinionOffset.ToString("+0;-0"));
            if (moodThought?.stages?.FirstOrDefault() is ThoughtStage ms) yield return "AA_Desc_MoodMemory".Translate(ms.label, ms.baseMoodEffect.ToString("+0;-0"));
        }
    }

    /// <summary>Couples fall in love all over again: the target and their partner (wherever they are on the map) gain memories of each other.</summary>
    public class Effect_Rekindle : SpellEffect
    {
        public ThoughtDef opinionThought;
        public ThoughtDef moodThought;
        /// <summary>Also raises the couple one step (lovers get engaged, fiances marry) when set.</summary>
        public bool raise;

        public Effect_Rekindle()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!Romance.AdultHuman(p)) return;
            var partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(p, false);
            if (partner == null || partner.Dead)
            {
                ArcaneFx.Text(p, "AA_NoPartner".Translate(), ctx.Color);
                return;
            }
            if (!ctx.Once("rekindle" + Mathf.Min(p.thingIDNumber, partner.thingIDNumber) + "_" + Mathf.Max(p.thingIDNumber, partner.thingIDNumber))) return;
            Romance.GainMemory(p, opinionThought, partner);
            Romance.GainMemory(partner, opinionThought, p);
            Romance.GainMemory(p, moodThought, partner);
            Romance.GainMemory(partner, moodThought, p);
            if (raise)
            {
                var cur = Romance.Bond(p, partner);
                var next = cur == PawnRelationDefOf.Lover ? PawnRelationDefOf.Fiance : cur == PawnRelationDefOf.Fiance ? PawnRelationDefOf.Spouse : null;
                if (next != null && Romance.Raise(p, partner, next) != null)
                    Messages.Message((next == PawnRelationDefOf.Spouse ? "AA_Rekindled_Married" : "AA_Rekindled_Engaged").Translate(p.LabelShort, partner.LabelShort),
                        new LookTargets(p, partner), MessageTypeDefOf.PositiveEvent);
            }
            Romance.LinkFx(p, partner, ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Rekindle".Translate();
            if (opinionThought?.stages?.FirstOrDefault() is ThoughtStage os) yield return "AA_Desc_OpinionMemory".Translate(os.label, os.baseOpinionOffset.ToString("+0;-0"));
            if (moodThought?.stages?.FirstOrDefault() is ThoughtStage ms) yield return "AA_Desc_MoodMemory".Translate(ms.label, ms.baseMoodEffect.ToString("+0;-0"));
            if (raise) yield return "AA_Desc_RekindleRaise".Translate();
        }
    }

    /// <summary>
    /// A woman and her partner (Biotech), or a female animal and a male of her kind nearby, conceive right now. Vanilla
    /// fertility rules still apply: the sterile, the too young and the too old are not helped.
    /// </summary>
    public class Effect_Conceive : SpellEffect
    {
        public bool humans = true;
        public bool animals = true;
        /// <summary>How far to look for a mate for an animal (0 = anywhere on the map).</summary>
        public float mateRadius = 15f;

        public Effect_Conceive()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p.RaceProps.Humanlike)
            {
                if (humans) Human(ctx, p);
            }
            else if (animals && p.RaceProps.Animal) Animal(ctx, p);
        }

        private void Human(SpellContext ctx, Pawn p)
        {
            if (!Dlc.Biotech)
            {
                ArcaneFx.Text(p, "AA_NeedsBiotech".Translate(), ctx.Color);
                return;
            }
            Pawn woman = null, man = null;
            if (p.gender == Gender.Female) woman = p;
            else if (p.gender == Gender.Male) man = p;
            var partner = LovePartnerRelationUtility.ExistingLovePartners(p, false)
                .Select(r => r.otherPawn).Where(o => o != null && !o.Dead && o.gender != p.gender && o.gender != Gender.None)
                .MaxByWithFallback(o => p.relations.OpinionOf(o));
            if (woman == null) woman = partner;
            else man = partner;
            if (woman == null || man == null)
            {
                ArcaneFx.Text(p, "AA_NoPartner".Translate(), ctx.Color);
                return;
            }
            if (PregnancyUtility.GetPregnancyHediff(woman) != null)
            {
                ArcaneFx.Text(woman, "AA_AlreadyPregnant".Translate(), ctx.Color);
                return;
            }
            var ok = PregnancyUtility.CanEverProduceChild(man, woman);
            if (!ok.Accepted)
            {
                ArcaneFx.Text(p, ok.Reason.NullOrEmpty() ? "AA_CannotConceive".Translate().ToString() : ok.Reason, ctx.Color);
                return;
            }
            var genes = PregnancyUtility.GetInheritedGeneSet(man, woman, out bool success);
            var def = HediffDefOf.PregnantHuman;
            if (!success || def == null)
            {
                ArcaneFx.Text(woman, "AA_CannotConceive".Translate(), ctx.Color);
                return;
            }
            var h = (Hediff_Pregnant)HediffMaker.MakeHediff(def, woman);
            h.SetParents(null, man, genes);
            woman.health.AddHediff(h);
            Romance.LinkFx(woman, man, ctx.Color);
            Messages.Message("AA_Conceived".Translate(woman.LabelShort, man.LabelShort, woman.Named("MOTHER"), man.Named("FATHER")), woman, MessageTypeDefOf.PositiveEvent);
        }

        private void Animal(SpellContext ctx, Pawn p)
        {
            if (p.gender == Gender.None || !(p.ageTracker?.CurLifeStage?.reproductive ?? false)) return;
            Pawn female = p.gender == Gender.Female ? p : null, male = p.gender == Gender.Male ? p : null;
            bool Mate(Pawn o) => o != p && !o.Dead && o.def == p.def && o.gender != p.gender && (o.ageTracker?.CurLifeStage?.reproductive ?? false)
                && (mateRadius <= 0f || o.Position.InHorDistOf(p.Position, mateRadius));
            var mate = p.Map?.mapPawns.AllPawnsSpawned.Where(Mate).OrderBy(o => o.Position.DistanceToSquared(p.Position)).FirstOrDefault();
            if (mate == null)
            {
                ArcaneFx.Text(p, "AA_NoMate".Translate(), ctx.Color);
                return;
            }
            if (female == null) female = mate;
            else male = mate;
            var eggs = female.TryGetComp<CompEggLayer>();
            if (eggs != null) eggs.Fertilize(male);
            else
            {
                if (PregnancyUtility.GetPregnancyHediff(female) != null || female.health.hediffSet.HasHediff(HediffDefOf.Pregnant)) return;
                var h = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.Pregnant, female);
                h.SetParents(null, male, null);
                female.health.AddHediff(h);
            }
            Romance.LinkFx(female, male, ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (humans) yield return "AA_Desc_ConceiveHuman".Translate();
            if (animals) yield return "AA_Desc_ConceiveAnimal".Translate();
        }
    }

    /// <summary>Speeds a pregnancy along by a number of days (never quite to the birth: nature finishes the job).</summary>
    public class Effect_Gestate : SpellEffect
    {
        public float days = 3f;

        public Effect_Gestate()
        {
            affects = TargetFilter.All;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var preg = PregnancyUtility.GetPregnancyHediff(p) as Hediff_Pregnant ?? p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Pregnant) as Hediff_Pregnant;
            if (preg == null) return;
            float period = Mathf.Max(1f, p.RaceProps.gestationPeriodDays);
            float d = days * Mathf.Sqrt(Mathf.Max(0.1f, ctx.HealFactor));
            float before = preg.GestationProgress;
            // gestation progress is the hediff's severity
            preg.Severity = Mathf.Clamp(before + d / period, 0.001f, 0.995f);
            if (Mathf.Abs(preg.GestationProgress - before) > 0.0001f)
            {
                ArcaneFx.Text(p, (d >= 0 ? "AA_GestationHastened" : "AA_GestationSlowed").Translate(Mathf.Abs(d).ToString("0.#")), ctx.Color);
                if (p.Map == Find.CurrentMap) Fx.Rise(p.Map, p.DrawPos, ctx.Color, 6f, PMat.Star, 0.5f, 0.5f, 1.2f, 0.16f, PFlags.Glint);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Gestate".Translate((days * Mathf.Sqrt(Mathf.Max(0.1f, ctx?.HealFactor ?? 1f))).ToString("0.#"));
        }
    }

    /// <summary>An ally lies down and sleeps at once, in their own bed if they can reach it.</summary>
    public class Effect_SleepNow : SpellEffect
    {
        public bool inBed = true;

        public Effect_SleepNow()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.Spawned || p.Downed || p.Drafted || p.InMentalState || p.jobs == null || p.needs?.rest == null) return;
            if (ctx.caster != null && p.Faction != ctx.caster.Faction && !p.IsPrisonerOfColony) return;
            if (!RestUtility.Awake(p)) return;
            Building_Bed bed = null;
            if (inBed)
            {
                try
                {
                    bed = RestUtility.FindBedFor(p);
                }
                catch
                {
                    bed = null;
                }
            }
            var job = bed != null ? JobMaker.MakeJob(JobDefOf.LayDown, bed) : JobMaker.MakeJob(JobDefOf.LayDown, p.Position);
            job.forceSleep = true;
            p.jobs.StartJob(job, JobCondition.InterruptForced);
            if (p.Map == Find.CurrentMap) Fx.Rise(p.Map, p.DrawPos, new Color(0.75f, 0.7f, 1f), 5f, PMat.Star, 0.4f, 0.4f, 1.6f, 0.14f, PFlags.Glint);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_SleepNow".Translate();
        }
    }

    /// <summary>Ends a mental break on the spot.</summary>
    public class Effect_Calm : SpellEffect
    {
        /// <summary>Also ends violent breaks (berserk, murderous rage...).</summary>
        public bool violent;
        public ThoughtDef thought;

        public Effect_Calm()
        {
            affects = TargetFilter.All;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var ms = p.MentalState;
            if (ms == null) return;
            if (!violent && ms.def.IsAggro) return;
            ms.RecoverFromState();
            if (thought != null) Romance.GainMemory(p, thought, ctx.caster);
            ArcaneFx.Text(p, "AA_Calmed".Translate(), ctx.Color);
            if (p.Map == Find.CurrentMap) Fx.Rise(p.Map, p.DrawPos, ctx.Color, 6f, PMat.Mote, 0.6f, 0.4f, 1.2f, 0.18f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return (violent ? "AA_Desc_CalmAll" : "AA_Desc_Calm").Translate();
        }
    }
}

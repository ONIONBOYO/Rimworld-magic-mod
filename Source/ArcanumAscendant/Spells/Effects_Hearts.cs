using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    // ================================================================================================
    // Effects that work on people's hearts and minds: love, family, memories, grudges, talents and
    // allegiances. Romance magic only ever touches adults, and respects who people are attracted to.
    // ================================================================================================

    public static class HeartUtil
    {
        public const float AdultAge = 18f;

        public static bool Adult(Pawn p) => p?.RaceProps?.Humanlike == true && (p.ageTracker?.AgeBiologicalYearsFloat ?? 0f) >= AdultAge;

        public static MemoryThoughtHandler Memories(Pawn p) => p?.needs?.mood?.thoughts?.memories;

        public static void Gain(Pawn p, ThoughtDef def, Pawn other = null)
        {
            if (def == null) return;
            Memories(p)?.TryGainMemory(def, other);
        }

        public static IEnumerable<ThoughtDef> Named(IEnumerable<string> names)
        {
            foreach (var n in names)
            {
                var d = DefDatabase<ThoughtDef>.GetNamedSilentFail(n);
                if (d != null) yield return d;
            }
        }

        /// <summary>The thoughts a category of memory-removal spell works on.</summary>
        public static HashSet<ThoughtDef> Category(MemoryCategory cat)
        {
            var set = new HashSet<ThoughtDef>();
            switch (cat)
            {
                case MemoryCategory.Grief:
                    set.AddRange(Named(new[]
                    {
                        "KnowColonistDied", "PawnWithGoodOpinionDied", "ColonistLost", "PawnWithGoodOpinionLost", "WitnessedDeathAlly", "WitnessedDeathFamily",
                        "BondedAnimalDied", "BondedAnimalLost", "Stillbirth", "Miscarried", "PartnerMiscarried", "KnowColonistExecuted", "KnowPrisonerDiedInnocent",
                    }));
                    foreach (var r in DefDatabase<PawnRelationDef>.AllDefsListForReading)
                    {
                        if (r.diedThought != null) set.Add(r.diedThought);
                        if (r.diedThoughtFemale != null) set.Add(r.diedThoughtFemale);
                        if (r.lostThought != null) set.Add(r.lostThought);
                        if (r.lostThoughtFemale != null) set.Add(r.lostThoughtFemale);
                    }
                    break;
                case MemoryCategory.Heartbreak:
                    set.AddRange(Named(new[]
                    {
                        "BrokeUpWithMe", "BrokeUpWithMeMood", "DivorcedMe", "DivorcedMeMood", "CheatedOnMe", "CheatedOnMeMood", "RebuffedMyRomanceAttempt",
                        "RebuffedMyRomanceAttemptMood", "FailedRomanceAttemptOnMe", "FailedRomanceAttemptOnMeLowOpinionMood", "RejectedMyProposal",
                        "RejectedMyProposalMood", "IRejectedTheirProposal",
                    }));
                    break;
                case MemoryCategory.Corpses:
                    set.AddRange(Named(new[] { "ObservedLayingCorpse", "ObservedLayingRottingCorpse" }));
                    break;
            }
            return set;
        }

        public static void Letter(string labelKey, string textKey, LetterDef def, LookTargets look, params NamedArgument[] args)
        {
            Find.LetterStack.ReceiveLetter(labelKey.Translate(args), textKey.Translate(args), def, look);
        }
    }

    public enum MemoryCategory
    {
        /// <summary>Grief over the dead and the lost.</summary>
        Grief,
        /// <summary>Breakups, rejections, cheating, divorce.</summary>
        Heartbreak,
        Corpses,
        /// <summary>Every bad memory.</summary>
        AllNegative,
    }

    /// <summary>Lifts a category of painful memories from a pawn and leaves something kinder in their place.</summary>
    public class Effect_RemoveMemories : SpellEffect
    {
        public MemoryCategory category = MemoryCategory.Grief;
        public int maxCount = -1;
        public ThoughtDef replaceWith;
        /// <summary>Only works on someone whose mood is at or below this (0-1). Rise From the Ashes needs a pawn at their lowest.</summary>
        public float onlyBelowMood = -1f;
        public bool silentIfNone;

        public Effect_RemoveMemories()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var mem = HeartUtil.Memories(p);
            if (mem == null) return;
            if (onlyBelowMood >= 0f && (p.needs.mood?.CurLevelPercentage ?? 1f) > onlyBelowMood)
            {
                if (info.primary == p) ArcaneFx.Text(p, "AA_NotLowEnough".Translate(onlyBelowMood.ToStringPercent()), Color.gray);
                return;
            }
            var defs = category == MemoryCategory.AllNegative ? null : HeartUtil.Category(category);
            var victims = mem.Memories.Where(m => m != null && (defs == null ? m.MoodOffset() < 0f : defs.Contains(m.def))).ToList();
            int n = 0;
            foreach (var m in victims)
            {
                if (maxCount >= 0 && n >= maxCount) break;
                mem.RemoveMemory(m);
                n++;
            }
            if (n == 0)
            {
                if (!silentIfNone && info.primary == p) ArcaneFx.Text(p, "AA_NothingToForget".Translate(), Color.gray);
                return;
            }
            HeartUtil.Gain(p, replaceWith);
            if (p.InMentalState && !p.MentalStateDef.IsAggro && category == MemoryCategory.AllNegative) p.MentalState.RecoverFromState();
            ArcaneFx.Text(p, "AA_MemoriesEased".Translate(n), ctx.Color);
            ctx.arcane?.GainXp(6f * n);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return ("AA_Desc_Forget_" + category).Translate();
            if (onlyBelowMood >= 0f) yield return "AA_Desc_OnlyBelowMood".Translate(onlyBelowMood.ToStringPercent());
            if (replaceWith != null) yield return "AA_Desc_Thought".Translate(replaceWith.stages?.FirstOrDefault()?.label ?? replaceWith.label);
        }
    }

    /// <summary>Relives the best thing that has happened to a pawn lately: the memory is renewed and felt again.</summary>
    public class Effect_ReliveMemory : SpellEffect
    {
        public Effect_ReliveMemory()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var mem = HeartUtil.Memories(p);
            var best = mem?.Memories.Where(m => m != null && !m.permanent && m.MoodOffset() > 0f).OrderByDescending(m => m.MoodOffset()).FirstOrDefault();
            if (best == null)
            {
                ArcaneFx.Text(p, "AA_NoHappyMemory".Translate(), Color.gray);
                return;
            }
            best.Renew();
            if (best.def.stackLimit > 1 && mem.NumMemoriesOfDef(best.def) < best.def.stackLimit)
            {
                var copy = ThoughtMaker.MakeThought(best.def, best.CurStageIndex);
                mem.TryGainMemory(copy, best.otherPawn);
            }
            ArcaneFx.Text(p, "AA_Relived".Translate(best.LabelCap), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Relive".Translate();
        }
    }

    /// <summary>
    /// Kindles love: the target and the colonist they are most drawn to grow fond of each other, and if both are free and
    /// the feeling is mutual they become lovers. Only adults, only people who could be attracted to each other.
    /// </summary>
    public class Effect_KindleRomance : SpellEffect
    {
        public ThoughtDef smittenThought;
        public ThoughtDef newLoveThought;
        public float minAttraction = 0.15f;
        public int opinionToBecomeLovers = 15;
        public float searchRadius = 60f;

        public Effect_KindleRomance()
        {
            affects = TargetFilter.Allies;
        }

        private static bool Free(Pawn p) => LovePartnerRelationUtility.ExistingLovePartner(p, false) == null;

        private bool Compatible(Pawn a, Pawn b)
        {
            if (a == b || !HeartUtil.Adult(b) || b.Dead) return false;
            if (a.relations.FamilyByBlood.Contains(b)) return false;
            return a.relations.SecondaryRomanceChanceFactor(b) >= minAttraction && b.relations.SecondaryRomanceChanceFactor(a) >= minAttraction;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!HeartUtil.Adult(p) || p.relations == null || !p.Spawned) return;
            var partner = LovePartnerRelationUtility.ExistingLovePartner(p, false);
            if (partner != null)
            {
                HeartUtil.Gain(p, smittenThought, partner);
                HeartUtil.Gain(partner, smittenThought, p);
                ArcaneFx.Text(p, "AA_LoveRekindled".Translate(partner.LabelShort), ctx.Color);
                Link(ctx, p, partner);
                return;
            }
            var map = p.Map;
            var candidate = map.mapPawns.AllPawnsSpawned
                .Where(o => o.Faction == p.Faction && o.RaceProps.Humanlike && Free(o) && o.Position.DistanceTo(p.Position) <= searchRadius && Compatible(p, o))
                .OrderByDescending(o => p.relations.OpinionOf(o) + o.relations.OpinionOf(p) + 40f * p.relations.CompatibilityWith(o))
                .FirstOrDefault();
            if (candidate == null)
            {
                ArcaneFx.Text(p, "AA_NoOneForThem".Translate(), Color.gray);
                return;
            }
            HeartUtil.Gain(p, smittenThought, candidate);
            HeartUtil.Gain(candidate, smittenThought, p);
            Link(ctx, p, candidate);
            if (p.relations.OpinionOf(candidate) >= opinionToBecomeLovers && candidate.relations.OpinionOf(p) >= opinionToBecomeLovers)
            {
                p.relations.AddDirectRelation(PawnRelationDefOf.Lover, candidate);
                foreach (var d in HeartUtil.Named(new[] { "RebuffedMyRomanceAttempt", "FailedRomanceAttemptOnMe", "BrokeUpWithMe" }))
                {
                    HeartUtil.Memories(p)?.RemoveMemoriesOfDefWhereOtherPawnIs(d, candidate);
                    HeartUtil.Memories(candidate)?.RemoveMemoriesOfDefWhereOtherPawnIs(d, p);
                }
                HeartUtil.Gain(p, newLoveThought, candidate);
                HeartUtil.Gain(candidate, newLoveThought, p);
                LovePartnerRelationUtility.TryToShareBed(p, candidate);
                HeartUtil.Letter("AA_NewLoversLabel", "AA_NewLoversText", LetterDefOf.PositiveEvent, new LookTargets(p, candidate),
                    p.LabelShort.Named("A"), candidate.LabelShort.Named("B"), ctx.def.label.Named("SPELL"));
            }
            else
            {
                ArcaneFx.Text(p, "AA_SparkKindled".Translate(candidate.LabelShort), ctx.Color);
            }
        }

        private static void Link(SpellContext ctx, Pawn a, Pawn b)
        {
            if (!a.Spawned || !b.Spawned || a.Map != b.Map) return;
            ArcaneVfxManager.For(a.Map).Add(new VfxBeam(a.DrawPos, b.DrawPos, 40, ArcaneMats.Beam, ctx.Color, ctx.Color2, 0.35f));
            ArcaneFx.Particles(a.Map, b.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Petal, 8, 1f, 0.5f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_KindleRomance".Translate();
        }
    }

    /// <summary>Ends the target's relationship cleanly, with no hard feelings on either side.</summary>
    public class Effect_Breakup : SpellEffect
    {
        public ThoughtDef partingThought;

        public Effect_Breakup()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var rel = LovePartnerRelationUtility.ExistingLeastLikedRel(p, r => !r.otherPawn.Dead);
            if (rel == null)
            {
                ArcaneFx.Text(p, "AA_NoPartner".Translate(), Color.gray);
                return;
            }
            var other = rel.otherPawn;
            var ex = rel.def == PawnRelationDefOf.Spouse ? PawnRelationDefOf.ExSpouse : PawnRelationDefOf.ExLover;
            p.relations.TryRemoveDirectRelation(rel.def, other);
            if (!p.relations.DirectRelationExists(ex, other)) p.relations.AddDirectRelation(ex, other);
            foreach (var d in HeartUtil.Category(MemoryCategory.Heartbreak))
            {
                HeartUtil.Memories(p)?.RemoveMemoriesOfDefWhereOtherPawnIs(d, other);
                HeartUtil.Memories(other)?.RemoveMemoriesOfDefWhereOtherPawnIs(d, p);
            }
            if (rel.def == PawnRelationDefOf.Spouse)
                foreach (var d in HeartUtil.Named(new[] { "GotMarried", "HoneymoonPhase" }))
                {
                    HeartUtil.Memories(p)?.RemoveMemoriesOfDefWhereOtherPawnIs(d, other);
                    HeartUtil.Memories(other)?.RemoveMemoriesOfDefWhereOtherPawnIs(d, p);
                }
            HeartUtil.Gain(p, partingThought, other);
            HeartUtil.Gain(other, partingThought, p);
            var bed = p.ownership?.OwnedBed;
            if (bed != null && bed.OwnersForReading.Contains(other)) other.ownership.UnclaimBed();
            Messages.Message("AA_PartedWays".Translate(p.LabelShort, other.LabelShort), new LookTargets(p, other), MessageTypeDefOf.NeutralEvent);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Breakup".Translate();
        }
    }

    /// <summary>Marries the target to their lover or fiancé on the spot. Everyone nearby attends the wedding.</summary>
    public class Effect_Marry : SpellEffect
    {
        public float guestRadius = 14f;

        public Effect_Marry()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            Pawn partner = null;
            foreach (var r in p.relations.DirectRelations)
                if ((r.def == PawnRelationDefOf.Fiance || r.def == PawnRelationDefOf.Lover) && !r.otherPawn.Dead && HeartUtil.Adult(r.otherPawn)
                    && (partner == null || r.def == PawnRelationDefOf.Fiance))
                    partner = r.otherPawn;
            if (partner == null || !HeartUtil.Adult(p))
            {
                ArcaneFx.Text(p, "AA_NoOneToMarry".Translate(), Color.gray);
                return;
            }
            MarriageCeremonyUtility.Married(p, partner);
            if (p.Spawned)
            {
                foreach (var g in p.Map.mapPawns.FreeColonistsSpawned)
                    if (g != p && g != partner && g.Position.DistanceTo(p.Position) <= guestRadius)
                        HeartUtil.Gain(g, ThoughtDefOf.AttendedWedding);
                ArcaneVfxManager.For(p.Map).Add(new VfxPillar(p.DrawPos, 70, ctx.Color, ctx.Color2, 1.6f));
                ArcaneFx.Particles(p.Map, p.DrawPos, ctx.Color2, ArcaneDefOf.AA_Fleck_Petal, 30, 2f, 0.6f);
            }
            HeartUtil.Letter("AA_WeddingLabel", "AA_WeddingText", LetterDefOf.PositiveEvent, new LookTargets(p, partner),
                p.LabelShort.Named("A"), partner.LabelShort.Named("B"));
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Marry".Translate();
        }
    }

    /// <summary>Washes away the grudges between everyone in the area and leaves them better disposed to each other.</summary>
    public class Effect_Reconcile : SpellEffect
    {
        public ThoughtDef bondThought;
        public bool removeGrudges = true;
        public bool endSocialFights = true;
        public float joy;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var group = GenRadial.RadialDistinctThingsAround(info.cell, map, Mathf.Max(info.radius, 1f), true).OfType<Pawn>()
                .Where(p => p.RaceProps.Humanlike && !p.Dead && p.needs?.mood != null && (ctx.IsAlly(p) || p.IsPrisonerOfColony)).ToList();
            int grudges = 0;
            foreach (var a in group)
            {
                if (endSocialFights && a.InMentalState && a.MentalStateDef == MentalStateDefOf.SocialFighting) a.MentalState.RecoverFromState();
                if (joy > 0f && a.needs.joy != null) a.needs.joy.CurLevel = Mathf.Clamp01(a.needs.joy.CurLevel + joy);
                foreach (var b in group)
                {
                    if (a == b) continue;
                    if (removeGrudges)
                    {
                        var mem = HeartUtil.Memories(a);
                        foreach (var m in mem.Memories.OfType<Thought_MemorySocial>().Where(m => m.otherPawn == b && m.OpinionOffset() < 0f).ToList())
                        {
                            mem.RemoveMemory(m);
                            grudges++;
                        }
                    }
                    if (bondThought != null) HeartUtil.Gain(a, bondThought, b);
                }
            }
            if (group.Count > 1) ArcaneFx.Particles(map, info.pos, ctx.Color, ArcaneDefOf.AA_Fleck_Note, 16, 1.5f, 0.6f);
            if (removeGrudges) HearthUtil.Note(ctx, info, grudges > 0 ? "AA_GrudgesForgiven".Translate(grudges) : "AA_NoGrudges".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (removeGrudges) yield return "AA_Desc_Reconcile".Translate();
            if (bondThought != null) yield return "AA_Desc_Bond".Translate(bondThought.stages?.FirstOrDefault()?.label ?? bondThought.label);
            if (endSocialFights) yield return "AA_Desc_EndFights".Translate();
        }
    }

    /// <summary>Wakes a passion in a skill the target has none for, or deepens one they already have.</summary>
    public class Effect_Passion : SpellEffect
    {
        public bool allowMajor = true;

        public Effect_Passion()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p.skills == null) return;
            var usable = p.skills.skills.Where(s => !s.TotallyDisabled).ToList();
            var none = usable.Where(s => s.passion == Passion.None).ToList();
            SkillRecord pick = none.RandomElementByWeightWithFallback(s => 1f + s.Level);
            Passion to = Passion.Minor;
            if (pick == null && allowMajor)
            {
                pick = usable.Where(s => s.passion == Passion.Minor).RandomElementByWeightWithFallback(s => 1f + s.Level);
                to = Passion.Major;
            }
            if (pick == null)
            {
                ArcaneFx.Text(p, "AA_AllPassionate".Translate(), Color.gray);
                return;
            }
            pick.passion = to;
            ArcaneFx.Text(p, (to == Passion.Major ? "AA_PassionBurns" : "AA_PassionWakes").Translate(pick.def.label), ctx.Color);
            Messages.Message((to == Passion.Major ? "AA_PassionBurnsMsg" : "AA_PassionWakesMsg").Translate(p.LabelShort, pick.def.label), p, MessageTypeDefOf.PositiveEvent);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Passion".Translate();
        }
    }

    /// <summary>Swaps one of the target's traits for one they might have had in another life.</summary>
    public class Effect_RerollTrait : SpellEffect
    {
        public Effect_RerollTrait()
        {
            affects = TargetFilter.Allies;
        }

        private static bool Sexuality(TraitDef d) => d == TraitDefOf.Gay || d == TraitDefOf.Bisexual || d == TraitDefOf.Asexual;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var traits = p.story?.traits;
            if (traits == null) return;
            var old = traits.allTraits.Where(t => !t.ScenForced && t.sourceGene == null && !t.Suppressed && !Sexuality(t.def)).RandomElementWithFallback();
            if (old == null)
            {
                ArcaneFx.Text(p, "AA_NoTraitToChange".Translate(), Color.gray);
                return;
            }
            traits.RemoveTrait(old);
            var options = new List<(TraitDef def, int degree, float w)>();
            foreach (var d in DefDatabase<TraitDef>.AllDefsListForReading)
            {
                if (Sexuality(d) || traits.HasTrait(d) || d == old.def) continue;
                if (traits.allTraits.Any(t => d.ConflictsWith(t))) continue;
                float w = d.GetGenderSpecificCommonality(p.gender);
                if (w <= 0f || d.degreeDatas.NullOrEmpty()) continue;
                foreach (var dd in d.degreeDatas)
                    if (dd.commonality > 0f) options.Add((d, dd.degree, w * dd.commonality));
            }
            if (options.Count == 0)
            {
                traits.GainTrait(old);
                return;
            }
            var pick = options.RandomElementByWeight(o => o.w);
            var nt = new Trait(pick.def, pick.degree);
            traits.GainTrait(nt);
            p.Notify_DisabledWorkTypesChanged();
            Messages.Message("AA_TraitRewritten".Translate(p.LabelShort, old.LabelCap, nt.LabelCap), p, MessageTypeDefOf.NeutralEvent);
            ArcaneFx.Text(p, nt.LabelCap, ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_RerollTrait".Translate();
        }
    }

    /// <summary>Starts a vanilla gathering (a party) with the caster as the host.</summary>
    public class Effect_StartGathering : SpellEffect
    {
        public GatheringDef gathering;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var g = gathering ?? GatheringDefOf.Party;
            if (g == null || ctx.caster == null) return;
            bool ok = false;
            try { ok = g.Worker.TryExecute(ctx.map, ctx.caster); }
            catch (System.Exception e) { Log.Warning("[Arcanum Ascendant] Gathering failed: " + e.Message); }
            if (!ok) Messages.Message("AA_GatheringFailed".Translate(g.label), MessageTypeDefOf.RejectInput, false);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Gathering".Translate((gathering ?? GatheringDefOf.Party)?.label ?? "party");
        }
    }

    /// <summary>Soothes babies and children: crying fits end, babies are played with and children learn.</summary>
    public class Effect_Childcare : SpellEffect
    {
        public float play = 0.6f;
        public float learning = 0.4f;
        public ThoughtDef thought;

        public Effect_Childcare()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.RaceProps.Humanlike || p.DevelopmentalStage.Adult()) return;
            if (p.InMentalState && !p.MentalStateDef.IsAggro) p.MentalState.RecoverFromState();
            if (p.needs?.play != null) p.needs.play.CurLevel = Mathf.Clamp01(p.needs.play.CurLevel + play);
            if (p.needs?.learning != null) p.needs.learning.CurLevel = Mathf.Clamp01(p.needs.learning.CurLevel + learning);
            HeartUtil.Gain(p, thought);
            ArcaneFx.Particles(p.Map, p.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Note, 4, 0.8f, 0.5f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Childcare".Translate();
        }
    }

    /// <summary>A child skips ahead to their next birthday, with the growth of a year well spent.</summary>
    public class Effect_GrowChild : SpellEffect
    {
        public int birthdays = 1;
        public float growthPoints = 30f;

        public Effect_GrowChild()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var at = p.ageTracker;
            if (at == null || !p.RaceProps.Humanlike || p.DevelopmentalStage.Adult()) return;
            at.growthPoints += growthPoints * HearthUtil.Power(ctx);
            for (int i = 0; i < birthdays && !p.DevelopmentalStage.Adult(); i++)
                at.DebugForceBirthdayBiological();
            ArcaneFx.Text(p, "AA_ChildGrew".Translate(at.AgeBiologicalYears), ctx.Color);
            if (p.Spawned) ArcaneVfxManager.For(p.Map).Add(new VfxPillar(p.DrawPos, 50, ctx.Color, ctx.Color2, 1.2f));
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_GrowChild".Translate(birthdays);
        }
    }

    /// <summary>Hurries a pregnancy along (human or animal).</summary>
    public class Effect_Gestation : SpellEffect
    {
        public float amount = 0.15f;

        public Effect_Gestation()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var h = p.health.hediffSet.hediffs.OfType<Hediff_Pregnant>().FirstOrDefault();
            if (h == null)
            {
                if (info.primary == p) ArcaneFx.Text(p, "AA_NotPregnant".Translate(), Color.gray);
                return;
            }
            h.Severity = Mathf.Min(0.995f, h.Severity + amount * HearthUtil.Power(ctx));
            ArcaneFx.Text(p, "AA_GestationHastened".Translate(h.Severity.ToStringPercent()), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Gestation".Translate(amount.ToStringPercent());
        }
    }

    /// <summary>Helps the body win its fights against disease: immunity jumps ahead for every illness it is fighting.</summary>
    public class Effect_Immunity : SpellEffect
    {
        public float amount = 0.35f;

        public Effect_Immunity()
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
                if (!h.def.PossibleToDevelopImmunityNaturally()) continue;
                imm.TryAddImmunityRecord(h.def, h.def);
                var rec = imm.GetImmunityRecord(h.def);
                if (rec == null) continue;
                rec.immunity = Mathf.Min(1f, rec.immunity + amount * HearthUtil.Power(ctx));
                n++;
            }
            if (n > 0) ArcaneFx.Text(p, "AA_ImmunityBoosted".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Immunity".Translate(amount.ToStringPercent());
        }
    }

    /// <summary>Moves an illness out of an ally and into the nearest creature that is not one.</summary>
    public class Effect_TransferDisease : SpellEffect
    {
        public float searchRadius = 8f;

        public Effect_TransferDisease()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var sick = p.health.hediffSet.hediffs.Where(h => h.Part == null && h.def.isBad && h.def.makesSickThought && !h.def.chronic && !h.def.defName.StartsWith("AA_"))
                .OrderByDescending(h => h.Severity).FirstOrDefault();
            if (sick == null)
            {
                ArcaneFx.Text(p, "AA_NotSick".Translate(), Color.gray);
                return;
            }
            if (!p.Spawned) return;
            var host = GenRadial.RadialDistinctThingsAround(p.Position, p.Map, searchRadius, true).OfType<Pawn>()
                .Where(o => o != p && !o.Dead && !o.RaceProps.IsMechanoid && (o.IsPrisonerOfColony || ctx.IsEnemy(o) || (o.RaceProps.Animal && o.Faction == null)))
                .OrderBy(o => o.Position.DistanceTo(p.Position)).FirstOrDefault();
            if (host == null)
            {
                ArcaneFx.Text(p, "AA_NoHost".Translate(), Color.gray);
                return;
            }
            var def = sick.def;
            float sev = sick.Severity;
            p.health.RemoveHediff(sick);
            var added = HediffMaker.MakeHediff(def, host);
            added.Severity = sev;
            host.health.AddHediff(added);
            ArcaneVfxManager.For(p.Map).Add(new VfxBeam(p.DrawPos, host.DrawPos, 30, ArcaneMats.Beam, ctx.Color, ctx.Color2, 0.4f));
            ArcaneFx.Text(host, "AA_DiseasePassed".Translate(def.label), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_TransferDisease".Translate();
        }
    }

    /// <summary>Wears down a prisoner's resistance to recruitment (and their will, where that matters).</summary>
    public class Effect_PrisonerResistance : SpellEffect
    {
        public float resistanceFraction = 0.5f;
        public float willFraction = 0.3f;
        public ThoughtDef prisonerThought;

        public Effect_PrisonerResistance()
        {
            affects = TargetFilter.All;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.IsPrisonerOfColony || p.guest == null) return;
            float before = p.guest.resistance;
            p.guest.resistance = Mathf.Max(0f, p.guest.resistance * (1f - Mathf.Clamp01(resistanceFraction * HearthUtil.Power(ctx))));
            p.guest.will = Mathf.Max(0f, p.guest.will * (1f - Mathf.Clamp01(willFraction)));
            HeartUtil.Gain(p, prisonerThought);
            ArcaneFx.Text(p, "AA_ResistanceBroken".Translate(before.ToString("F1"), p.guest.resistance.ToString("F1")), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Resistance".Translate(resistanceFraction.ToStringPercent());
        }
    }

    /// <summary>Improves relations with the target's faction.</summary>
    public class Effect_Goodwill : SpellEffect
    {
        public int amount = 12;
        public bool healTarget = true;

        public Effect_Goodwill()
        {
            affects = TargetFilter.All;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var f = p.Faction;
            if (f == null || f.IsPlayer || f.def.hidden || f.def.permanentEnemy || f.def.defName == "AA_ArcaneThralls")
            {
                ArcaneFx.Text(p, "AA_NoFactionToSway".Translate(), Color.gray);
                return;
            }
            if (!ctx.Once("goodwill_" + f.loadID)) return;
            int amt = Mathf.RoundToInt(amount * HearthUtil.Power(ctx));
            if (!f.CanChangeGoodwillFor(Faction.OfPlayer, amt))
            {
                ArcaneFx.Text(p, "AA_NoFactionToSway".Translate(), Color.gray);
                return;
            }
            f.TryAffectGoodwillWith(Faction.OfPlayer, amt, true, true, null);
            if (healTarget) ArcaneUtility.HealPawn(p, 30f * ctx.HealFactor);
            ArcaneFx.Text(p, "AA_GoodwillGained".Translate(amt, f.Name), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Goodwill".Translate(amount);
        }
    }

    /// <summary>Lifts the most valuable thing a visitor is carrying. If they notice, their faction will not be pleased.</summary>
    public class Effect_Pickpocket : SpellEffect
    {
        public float detectChance = 0.3f;
        public int goodwillPenalty = 15;

        public Effect_Pickpocket()
        {
            affects = TargetFilter.AllButSelf;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var caster = ctx.caster;
            if (caster == null || p.Faction == caster.Faction || p.inventory == null || !p.Spawned) return;
            var loot = p.inventory.innerContainer.Where(t => t.def.category == ThingCategory.Item && t.def.EverHaulable)
                .OrderByDescending(t => t.MarketValue * t.stackCount).FirstOrDefault();
            if (loot == null)
            {
                ArcaneFx.Text(p, "AA_EmptyPockets".Translate(), Color.gray);
                return;
            }
            int take = loot.def.stackLimit > 1 ? Mathf.Max(1, Mathf.CeilToInt(loot.stackCount * 0.5f)) : 1;
            var stolen = loot.SplitOff(take);
            HearthUtil.PlaceNear(stolen, caster.Position, caster.Map);
            ArcaneFx.Text(caster, "AA_Pickpocketed".Translate(stolen.LabelCap), ctx.Color);
            if (Rand.Chance(detectChance / Mathf.Max(0.5f, HearthUtil.Power(ctx))) && p.Faction != null && !p.Faction.IsPlayer)
            {
                p.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -goodwillPenalty, true, true, null);
                Messages.Message("AA_PickpocketCaught".Translate(p.LabelShort, p.Faction.Name), p, MessageTypeDefOf.NegativeEvent);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Pickpocket".Translate(detectChance.ToStringPercent(), goodwillPenalty);
        }
    }

    /// <summary>Advances the colony's current research project.</summary>
    public class Effect_Research : SpellEffect
    {
        /// <summary>Fraction of the current project's cost.</summary>
        public float fraction = 0.2f;
        public float maxPoints = 1500f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var rm = Find.ResearchManager;
            var proj = rm?.GetProject();
            if (proj == null || proj.IsFinished)
            {
                Messages.Message("AA_NoResearchProject".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            float pts = Mathf.Min(maxPoints * HearthUtil.Power(ctx), proj.CostApparent * fraction * HearthUtil.Power(ctx));
            rm.AddProgress(proj, pts, ctx.caster);
            Messages.Message("AA_ResearchAdvanced".Translate(proj.LabelCap, pts.ToString("F0")), MessageTypeDefOf.PositiveEvent, false);
            ctx.arcane?.GainXp(pts * 0.05f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Research".Translate(fraction.ToStringPercent(), maxPoints.ToString("F0"));
        }
    }

    /// <summary>Fires one of a list of vanilla incidents (traders, wanderers, animals, raids...).</summary>
    public class Effect_Incident : SpellEffect
    {
        public List<IncidentDef> incidents = new List<IncidentDef>();
        public bool random = true;
        public float pointsFactor = 1f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var list = incidents.Where(i => i != null).ToList();
            if (random) list.Shuffle();
            foreach (var inc in list)
            {
                try
                {
                    var parms = StorytellerUtility.DefaultParmsNow(inc.category, ctx.map);
                    parms.forced = true;
                    parms.points *= pointsFactor;
                    if (inc.Worker.CanFireNow(parms) && inc.Worker.TryExecute(parms)) return;
                }
                catch (System.Exception e)
                {
                    Log.Warning("[Arcanum Ascendant] Incident " + inc.defName + " failed: " + e.Message);
                }
            }
            Messages.Message("AA_IncidentFailed".Translate(), MessageTypeDefOf.RejectInput, false);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (incidents.Count == 1) yield return "AA_Desc_Incident".Translate(incidents[0].label);
            else yield return "AA_Desc_IncidentOneOf".Translate(string.Join(", ", incidents.Where(i => i != null).Select(i => i.label)));
        }
    }

    /// <summary>Gives a pawn genes for a while (Biotech). They are removed when the status that holds them ends.</summary>
    public class Effect_TemporaryGene : SpellEffect
    {
        public List<GeneDef> genes = new List<GeneDef>();
        public int count = 1;
        public HediffDef holder;
        public int durationTicks = 300000;

        public Effect_TemporaryGene()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!ModsConfig.BiotechActive || p.genes == null || holder == null) return;
            var options = genes.Where(g => g != null && !p.genes.HasActiveGene(g)).ToList();
            if (options.Count == 0)
            {
                if (info.primary == p) ArcaneFx.Text(p, "AA_NoGeneToGive".Translate(), Color.gray);
                return;
            }
            var h = ArcaneUtility.ApplyHediff(p, holder, 1f, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), ctx) as HediffWithComps;
            var comp = h?.TryGetComp<HediffComp_TemporaryGenes>();
            if (comp == null) return;
            var given = new List<string>();
            foreach (var g in options.InRandomOrder().Take(count))
            {
                p.genes.AddGene(g, true);
                comp.Track(g);
                given.Add(g.label);
            }
            ArcaneFx.Text(p, string.Join(", ", given), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_TempGene".Translate(count, string.Join(", ", genes.Where(g => g != null).Select(g => g.label)),
                (durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    /// <summary>Feeds a bloodfeeder's hemogen, or restores lost blood to anyone else.</summary>
    public class Effect_Hemogen : SpellEffect
    {
        public float hemogen = 0.35f;
        public float bloodLossHeal = 0.4f;

        public Effect_Hemogen()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            float pw = HearthUtil.Power(ctx);
            if (ModsConfig.BiotechActive && p.genes?.GetFirstGeneOfType<Gene_Hemogen>() is Gene_Hemogen g)
            {
                g.Value = Mathf.Min(g.Max, g.Value + hemogen * pw);
                ArcaneFx.Text(p, "AA_HemogenRestored".Translate(), ctx.Color);
            }
            var bl = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            if (bl != null)
            {
                bl.Severity -= bloodLossHeal * pw;
                if (bl.Severity <= 0.01f) p.health.RemoveHediff(bl);
                ArcaneFx.Text(p, "AA_BloodRestored".Translate(), ctx.Color);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Hemogen".Translate(hemogen.ToStringPercent());
        }
    }

    /// <summary>Records the target's best skill onto a tape that anyone can later watch to learn from.</summary>
    public class Effect_RecordTape : SpellEffect
    {
        public ThingDef tape;
        public float xp = 4000f;

        public Effect_RecordTape()
        {
            affects = TargetFilter.All;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (tape == null || p.skills == null || !p.Spawned) return;
            var best = p.skills.skills.Where(s => !s.TotallyDisabled).OrderByDescending(s => s.Level).FirstOrDefault();
            if (best == null || best.Level <= 0) return;
            var t = ThingMaker.MakeThing(tape);
            var comp = t.TryGetComp<CompTapeRecording>();
            if (comp != null)
            {
                comp.skill = best.def;
                comp.xp = xp * HearthUtil.Power(ctx) * Mathf.Lerp(0.5f, 1.5f, best.Level / 20f);
                comp.recordedFrom = p.LabelShort;
            }
            HearthUtil.PlaceNear(t, p.Position, p.Map);
            ArcaneFx.Text(p, "AA_TapeRecorded".Translate(best.def.label), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_RecordTape".Translate(xp.ToString("F0"));
        }
    }

    /// <summary>Applies a status to everyone who arrives on this map hostile for a while (prophecy of doom on the next raid).</summary>
    public class Effect_MarkArrivals : SpellEffect
    {
        public HediffDef hediff;
        public float severity = 1f;
        public int windowTicks = 180000;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (hediff == null || ctx.caster == null) return;
            MapComponent_ArcaneColony.For(ctx.map).AddArrivalMark(hediff, severity, Find.TickManager.TicksGame + Mathf.RoundToInt(windowTicks * ctx.DurationFactor),
                ctx.caster.Faction);
            Messages.Message("AA_ArrivalsForetold".Translate((windowTicks * ctx.DurationFactor).ToStringTicksToPeriod()), MessageTypeDefOf.PositiveEvent, false);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (hediff != null) yield return "AA_Desc_MarkArrivals".Translate(hediff.label, (windowTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    /// <summary>Blesses a pregnancy (or the mother). The work is done by the hediff's birth-watch comp.</summary>
    public class Effect_BlessPregnancy : SpellEffect
    {
        public HediffDef blessing;

        public Effect_BlessPregnancy()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (blessing == null) return;
            if (!PawnRequirement.IsPregnant(p))
            {
                if (info.primary == p) ArcaneFx.Text(p, "AA_NotPregnant".Translate(), Color.gray);
                return;
            }
            var h = ArcaneUtility.ApplyHediff(p, blessing, Mathf.Clamp(ctx.HealFactor, 1f, 3f), -1, ctx);
            if (h == null) return;
            ArcaneFx.Text(p, blessing.LabelCap, ctx.Color);
            if (p.Spawned) ArcaneFx.Particles(p.Map, p.DrawPos, ctx.Color2, ArcaneDefOf.AA_Fleck_Petal, 16, 1.4f, 0.6f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (blessing != null) yield return "AA_Desc_Hediff".Translate(blessing.LabelCap);
        }
    }
}

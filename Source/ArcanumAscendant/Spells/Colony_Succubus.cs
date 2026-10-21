using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Succubus colony expansion: mending feuds (Kiss and Make Up) and cashing in Infatuation for an
    // outcome other than damage (Swoon, Homeward Heart).
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// The target and the person they get on worst with (on the same map, of the same faction) make up: both gain an opinion
    /// memory of each other (and a mood memory). Sometimes, when both are single adults who could love each other, the old
    /// enemies fall in love instead.
    /// </summary>
    public class Effect_SuccubusReconcile : SpellEffect
    {
        public ThoughtDef opinionThought;
        public ThoughtDef moodThought;
        /// <summary>Chance that a pair of single, mutually attracted adults become lovers.</summary>
        public float loveChance = 0.3f;

        public Effect_SuccubusReconcile()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p == null || p.Dead || !p.Spawned || p.relations == null || !p.RaceProps.Humanlike || p.IsPrisoner || p.Map == null) return;
            Pawn rival = null;
            float worst = 0f;
            foreach (var o in p.Map.mapPawns.AllPawnsSpawned)
            {
                if (o == p || o.Dead || o.relations == null || !o.RaceProps.Humanlike || o.IsPrisoner || o.Faction != p.Faction) continue;
                float s = p.relations.OpinionOf(o) + o.relations.OpinionOf(p);
                if (s < worst)
                {
                    worst = s;
                    rival = o;
                }
            }
            if (rival == null)
            {
                ArcaneFx.Text(p, "AA_SuccubusNoFeud".Translate(), ctx.Color);
                return;
            }
            if (!ctx.Once("reconcile" + Mathf.Min(p.thingIDNumber, rival.thingIDNumber) + "_" + Mathf.Max(p.thingIDNumber, rival.thingIDNumber))) return;
            Romance.GainMemory(p, opinionThought, rival);
            Romance.GainMemory(rival, opinionThought, p);
            Romance.GainMemory(p, moodThought, rival);
            Romance.GainMemory(rival, moodThought, p);
            Romance.LinkFx(p, rival, ctx.Color);
            bool lovers = false;
            if (loveChance > 0f && Romance.Bond(p, rival) == null && Romance.CanPair(p, rival, true) && Rand.Chance(loveChance))
                lovers = Romance.Raise(p, rival, PawnRelationDefOf.Lover) != null;
            if (lovers && (PawnUtility.ShouldSendNotificationAbout(p) || PawnUtility.ShouldSendNotificationAbout(rival)))
                Find.LetterStack.ReceiveLetter("AA_SuccubusEnemiesToLoversLabel".Translate(p.LabelShort, rival.LabelShort, p.Named("A"), rival.Named("B")),
                    "AA_SuccubusEnemiesToLovers".Translate(p.LabelShort, rival.LabelShort, ctx.caster?.LabelShort ?? "?", p.Named("A"), rival.Named("B")),
                    LetterDefOf.PositiveEvent, new LookTargets(p, rival));
            else ArcaneFx.Text(p, "AA_SuccubusMadeUp".Translate(rival.LabelShort), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_SuccubusReconcile".Translate();
            if (opinionThought?.stages != null && opinionThought.stages.Count > 0)
                yield return "AA_Desc_OpinionMemory".Translate(opinionThought.stages[0].label, opinionThought.stages[0].baseOpinionOffset.ToString("+0;-0"));
            if (moodThought?.stages != null && moodThought.stages.Count > 0)
                yield return "AA_Desc_MoodMemory".Translate(moodThought.stages[0].label, moodThought.stages[0].baseMoodEffect.ToString("+0;-0"));
            if (loveChance > 0f) yield return "AA_Desc_SuccubusReconcileLove".Translate(loveChance.ToStringPercent());
        }
    }

    /// <summary>
    /// Cashes in a stacking status: only targets carrying at least minStacks of it are affected, the stacks are spent, and the
    /// inner effects play on them. Bosses, the very large and (optionally) machines are unmoved.
    /// </summary>
    public class Effect_SuccubusHeartsDue : SpellEffect
    {
        public HediffDef stackHediff;
        public float minStacks = 1f;
        public bool consume = true;
        public float maxBodySize = 999f;
        public bool fleshOnly;
        public bool noBosses = true;
        public List<SpellEffect> effects = new List<SpellEffect>();

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (stackHediff == null || p?.health?.hediffSet == null || p.Dead || effects.NullOrEmpty()) return;
            if (noBosses && (p.kindDef?.isBoss ?? false) || p.BodySize > maxBodySize || fleshOnly && !p.RaceProps.IsFlesh)
            {
                if (p.Spawned) ArcaneFx.Text(p, "AA_SuccubusUnmoved".Translate(), ctx.Color);
                return;
            }
            var h = p.health.hediffSet.GetFirstHediffOfDef(stackHediff);
            if (h == null || h.Severity < minStacks - 0.01f)
            {
                if (p.Spawned) ArcaneFx.Text(p, "AA_SuccubusNotSmitten".Translate(Mathf.FloorToInt(h?.Severity ?? 0f), Mathf.CeilToInt(minStacks)), ctx.Color);
                return;
            }
            if (consume) p.health.RemoveHediff(h);
            foreach (var e in effects)
            {
                if (e == null || p.Dead || p.Destroyed || !e.AppliesToPawns || !Rand.Chance(e.chance)) continue;
                if (!ctx.Passes(p, e.affects == TargetFilter.Inherit ? TargetFilter.All : e.affects)) continue;
                e.ApplyToPawn(ctx, p, info);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (stackHediff == null) yield break;
            yield return (consume ? "AA_Desc_SuccubusHeartsDueSpent" : "AA_Desc_SuccubusHeartsDue").Translate(Mathf.CeilToInt(minStacks), stackHediff.label);
            foreach (var e in effects)
            {
                if (e == null) continue;
                foreach (var s in e.Describe(ctx)) yield return "  " + s;
            }
        }
    }
}

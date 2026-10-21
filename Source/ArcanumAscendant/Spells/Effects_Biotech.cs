using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Biotech: children's growth, hemogen, mech energy and genes. Each one quietly does nothing without the DLC.
    // ------------------------------------------------------------------------------------------------

    /// <summary>Children gain growth points and learning; babies get to play.</summary>
    public class Effect_ChildGrowth : SpellEffect
    {
        public float points = 4f;
        public float learning = 0.3f;

        public Effect_ChildGrowth()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!Dlc.Biotech || !p.RaceProps.Humanlike || p.ageTracker == null) return;
            var stage = p.DevelopmentalStage;
            if (stage.Child())
            {
                p.ageTracker.growthPoints += points * Mathf.Sqrt(Mathf.Max(0.1f, ctx.HealFactor));
                if (learning > 0f && p.needs?.learning != null) p.needs.learning.CurLevel = Mathf.Clamp01(p.needs.learning.CurLevel + learning);
                ArcaneFx.Text(p, "AA_GrowthGained".Translate(points.ToString("0.#")), ctx.Color);
            }
            else if (stage.Baby())
            {
                var play = p.needs?.TryGetNeed(Dlc.Def<NeedDef>("Play"));
                if (play != null) play.CurLevel = Mathf.Clamp01(play.CurLevel + Mathf.Max(0.3f, learning));
            }
            else return;
            ColonyFx.Sparkle(p.Map, p.DrawPos, ctx.Color, 5f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_ChildGrowth".Translate(points.ToString("0.#"), learning.ToStringPercent());
        }
    }

    /// <summary>Fills (or drains) the hemogen of sanguophages and other hemogenic people.</summary>
    public class Effect_Hemogen : SpellEffect
    {
        /// <summary>Fraction of a full hemogen bar (0.3 = 30 hemogen).</summary>
        public float amount = 0.3f;

        public Effect_Hemogen()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!Dlc.Biotech) return;
            var g = p.genes?.GetFirstGeneOfType<Gene_Hemogen>();
            if (g == null) return;
            g.Value = Mathf.Clamp(g.Value + amount, 0f, g.Max);
            ArcaneFx.Text(p, "AA_HemogenGained".Translate((amount * 100f).ToString("+0;-0")), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Hemogen".Translate((amount * 100f).ToString("+0;-0"));
        }
    }

    /// <summary>Recharges (or drains) a mechanoid's energy.</summary>
    public class Effect_MechEnergy : SpellEffect
    {
        /// <summary>Fraction of a full battery.</summary>
        public float amount = 0.3f;

        public Effect_MechEnergy()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var e = p.needs?.energy;
            if (!Dlc.Biotech || e == null) return;
            e.CurLevel = Mathf.Clamp(e.CurLevel + amount * e.MaxLevel, 0f, e.MaxLevel);
            if (p.Map == Find.CurrentMap) Fx.Sparks(p.Map, p.DrawPos, ctx.Color, 8f, 2f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_MechEnergy".Translate(amount.ToStringPercent());
        }
    }

    /// <summary>Writes a gene into the target as a xenogene (or germline gene). Named, so it loads without Biotech.</summary>
    public class Effect_Gene : SpellEffect
    {
        public string add;
        public bool xenogene = true;

        public Effect_Gene()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!Dlc.Biotech || p.genes == null) return;
            var def = Dlc.Def<GeneDef>(add);
            if (def == null || p.genes.HasActiveGene(def)) return;
            p.genes.AddGene(def, xenogene);
            ArcaneFx.Text(p, "AA_GeneGained".Translate(def.LabelCap), ctx.Color);
            ColonyFx.Sparkle(p.Map, p.DrawPos, ctx.Color, 8f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Gene".Translate(Dlc.Def<GeneDef>(add)?.label ?? add);
        }
    }
}

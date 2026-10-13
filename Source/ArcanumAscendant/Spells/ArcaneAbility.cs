using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>Ability subclass that pays mana, applies cooldown reduction and grants XP.</summary>
    public class ArcaneAbility : Ability
    {
        private SpellExtension extInt;
        [Unsaved] private VfxCastCircle circle;

        public ArcaneAbility() { }
        public ArcaneAbility(Pawn pawn) : base(pawn) { }
        public ArcaneAbility(Pawn pawn, AbilityDef def) : base(pawn, def) { }
        public ArcaneAbility(Pawn pawn, Precept sourcePrecept) : base(pawn, sourcePrecept) { }
        public ArcaneAbility(Pawn pawn, Precept sourcePrecept, AbilityDef def) : base(pawn, sourcePrecept, def) { }

        private static HediffDef silencedDef;
        private static bool silencedLooked;

        public static HediffDef SilencedDef
        {
            get
            {
                if (!silencedLooked)
                {
                    silencedLooked = true;
                    silencedDef = DefDatabase<HediffDef>.GetNamedSilentFail("AA_Silenced");
                }
                return silencedDef;
            }
        }

        public SpellExtension Ext => extInt ??= def.GetModExtension<SpellExtension>() ?? new SpellExtension();
        public CompArcane Arcane => pawn?.TryGetComp<CompArcane>();
        public CompProperties_ArcaneSpell SpellProps => def.comps?.OfType<CompProperties_ArcaneSpell>().FirstOrDefault();

        public float ManaCost
        {
            get
            {
                var c = Arcane;
                float factor = 1f;
                if (c != null && c.IsMage)
                {
                    factor *= pawn.GetStatValue(ArcaneDefOf.AA_ManaCostFactor);
                    factor *= Mathf.Max(0f, 1f + c.ModsFor(def).manaCost);
                }
                return Mathf.Max(0f, Ext.manaCost * factor);
            }
        }

        public bool CanPayWithHealth(out float hpCost)
        {
            hpCost = 0f;
            var c = Arcane;
            if (c == null) return false;
            foreach (var (passive, rank, _) in c.Passives)
                if (passive is P_BloodPact bp)
                {
                    hpCost = (ManaCost - c.mana) * bp.HealthPerMana(rank);
                    return pawn.health.summaryHealth.SummaryHealthPercent > 0.35f;
                }
            return false;
        }

        public int AdjustedCooldown(int baseTicks)
        {
            var c = Arcane;
            if (c == null || !c.IsMage) return baseTicks;
            float cdr = Mathf.Clamp(pawn.GetStatValue(ArcaneDefOf.AA_CooldownReduction), 0f, 0.75f);
            float mod = Mathf.Max(0.1f, 1f + c.ModsFor(def).cooldown);
            return Mathf.Max(30, Mathf.RoundToInt(baseTicks * (1f - cdr) * mod));
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                var baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;
                var c = Arcane;
                if (c != null && c.mana + 0.01f < ManaCost && !CanPayWithHealth(out _) && !c.CanOverchannel) return "AA_NotEnoughMana".Translate();
                if (SilencedDef != null && pawn.health.hediffSet.HasHediff(SilencedDef)) return "AA_SilencedReason".Translate();
                return true;
            }
        }

        public override bool GizmoDisabled(out string reason)
        {
            if (base.GizmoDisabled(out reason)) return true;
            var c = Arcane;
            if (c != null && c.mana + 0.01f < ManaCost && !CanPayWithHealth(out _) && !c.CanOverchannel)
            {
                reason = "AA_NotEnoughManaDetail".Translate(ManaCost.ToString("F0"), c.mana.ToString("F0"));
                return true;
            }
            if (pawn.Downed)
            {
                reason = "AA_Downed".Translate();
                return true;
            }
            if (SilencedDef != null && pawn.health.hediffSet.HasHediff(SilencedDef))
            {
                reason = "AA_SilencedReason".Translate();
                return true;
            }
            return false;
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var c = Arcane;
            float cost = ManaCost;
            float strainOwed = 0f;
            if (c != null)
            {
                if (c.mana >= cost) c.mana -= cost;
                else if (CanPayWithHealth(out float hp))
                {
                    c.mana = 0f;
                    ArcaneUtility.PayHealth(pawn, hp);
                }
                else if (c.CanOverchannel)
                {
                    strainOwed = cost - c.mana;
                    c.mana = 0f;
                }
                else c.mana = Mathf.Max(0f, c.mana - cost);
            }
            if (Ext.healthCost > 0f) ArcaneUtility.PayHealth(pawn, Ext.healthCost);
            bool result = base.Activate(target, dest);
            if (def.cooldownTicksRange.max > 0)
                StartCooldown(AdjustedCooldown(def.cooldownTicksRange.RandomInRange));
            if (c != null && c.IsMage)
            {
                c.GainXp((4f + cost * 0.6f) * Ext.xpFactor);
                c.GainMastery(def, Mastery.PointsPerCast(def));
                if (strainOwed > 0f) c.AddStrain(strainOwed);
            }
            return result;
        }

        public override void AbilityTick()
        {
            base.AbilityTick();
            if (!Casting || !pawn.Spawned) return;
            if (circle != null && !circle.finished && !circle.Ending) return;
            var props = SpellProps;
            circle = new VfxCastCircle
            {
                pawn = pawn, ability = this, style = StyleFx.Resolve(props, Ext), c1 = props?.color ?? Color.white, c2 = props?.color2 ?? Color.white,
                ultimate = Ext.ultimate,
            };
            ArcaneVfxManager.For(pawn.Map)?.Add(circle);
        }

        public override IEnumerable<Command> GetGizmos()
        {
            var c = Arcane;
            if (c != null && c.IsHidden(def)) yield break;
            if (pawn.Faction != Faction.OfPlayer && !DebugSettings.ShowDevGizmos) yield break;
            foreach (var g in base.GetGizmos()) yield return g;
        }

        public override string Tooltip
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine(ArcaneUtility.SpellTooltip(def, pawn));
                return sb.ToString().TrimEndNewlines();
            }
        }
    }

    /// <summary>The single effect comp that runs an arcane spell.</summary>
    public class CompAbilityEffect_ArcaneSpell : CompAbilityEffect
    {
        public new CompProperties_ArcaneSpell Props => (CompProperties_ArcaneSpell)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var caster = parent.pawn;
            if (caster == null || caster.MapHeld == null) return;
            var ctx = SpellContext.For(caster, parent.def);
            try
            {
                StyleFx.Release(ctx, target.IsValid ? target.CenterVector3 : caster.DrawPos);
            }
            catch (System.Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Release effect error: " + e, 0x5A1C0F0);
            }
            SpellRunner.Execute(ctx, target, dest);
            ctx.arcane?.Notify_SpellCast(ctx);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            base.DrawEffectPreview(target);
            var p = Props;
            var ctx = SpellContext.For(parent.pawn, parent.def);
            float r = 0f;
            switch (p.delivery)
            {
                case DeliveryType.Barrage: r = p.barrageRadius * ctx.RadiusFactor; break;
                case DeliveryType.Cone: r = 0f; break;
                default: r = p.radius * ctx.RadiusFactor; break;
            }
            if (p.delivery == DeliveryType.Orbitals)
            {
                float orb = p.orbitalRadius * Mathf.Max(0.6f, ctx.RadiusFactor) + p.orbitalHitRadius;
                if (orb < GenRadial.MaxRadialPatternRadius) GenDraw.DrawRadiusRing(parent.pawn.Position, orb, p.color);
                return;
            }
            if (p.delivery == DeliveryType.Nova || p.delivery == DeliveryType.Self)
            {
                if (r > 0.5f) GenDraw.DrawRadiusRing(parent.pawn.Position, r, p.color);
                return;
            }
            if (r > 0.5f && target.IsValid && r < GenRadial.MaxRadialPatternRadius)
                GenDraw.DrawRadiusRing(target.Cell, r, p.color);
            foreach (var e in p.effects)
                if (e is Effect_Field f && f.fieldRadius > 0.5f && target.IsValid)
                {
                    float fr = f.fieldRadius * ctx.RadiusFactor;
                    if (fr < GenRadial.MaxRadialPatternRadius) GenDraw.DrawRadiusRing(target.Cell, fr, p.color2);
                }
        }

        public override bool AICanTargetNow(LocalTargetInfo target) => false;
    }

    /// <summary>Verb that applies cast speed and range modifiers.</summary>
    public class Verb_ArcaneCast : Verb_CastAbility
    {
        public override float WarmupTime
        {
            get
            {
                float w = base.WarmupTime;
                if (CasterPawn != null && CasterPawn.TryGetComp<CompArcane>() is CompArcane c && c.IsMage)
                    w /= Mathf.Max(0.25f, CasterPawn.GetStatValue(ArcaneDefOf.AA_CastSpeed));
                return w;
            }
        }

        public override float EffectiveRange
        {
            get
            {
                float r = base.EffectiveRange;
                if (r > 0f && ability != null && CasterPawn?.TryGetComp<CompArcane>() is CompArcane c && c.IsMage)
                    r *= Mathf.Max(0.3f, 1f + c.ModsFor(ability.def).range);
                return Mathf.Min(r, GenRadial.MaxRadialPatternRadius - 1f);
            }
        }
    }

    /// <summary>Gizmo for arcane spells: shows mana cost, autocast state and offers a right-click menu.</summary>
    public class Command_ArcaneSpell : Command_Ability
    {
        public Command_ArcaneSpell(Ability ability, Pawn pawn) : base(ability, pawn) { }

        private ArcaneAbility AA => ability as ArcaneAbility;

        public override string TopRightLabel
        {
            get
            {
                var a = AA;
                if (a == null) return base.TopRightLabel;
                string b = base.TopRightLabel;
                string m = a.ManaCost.ToString("F0");
                return b.NullOrEmpty() ? m : b + "\n" + m;
            }
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                foreach (var o in base.RightClickFloatMenuOptions) yield return o;
                var a = AA;
                var c = a?.Arcane;
                if (c == null) yield break;
                bool auto = c.IsAutocast(a.def);
                yield return new FloatMenuOption((auto ? "AA_DisableAutocast" : "AA_EnableAutocast").Translate(), () => c.SetAutocast(a.def, !auto));
                yield return new FloatMenuOption("AA_HideSpell".Translate(), () => c.SetHidden(a.def, true));
                yield return new FloatMenuOption("AA_OpenGrimoire".Translate(), () => Find.WindowStack.Add(new Dialog_Grimoire(c)));
            }
        }

        protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms)
        {
            var res = base.GizmoOnGUIInt(butRect, parms);
            var a = AA;
            var c = a?.Arcane;
            if (c != null)
            {
                if (c.IsAutocast(a.def))
                {
                    var r = new Rect(butRect.x + 3f, butRect.y + 3f, 16f, 16f);
                    GUI.DrawTexture(r, ArcaneTex.AutocastIcon);
                }
                if (a.Ext.ultimate)
                {
                    Widgets.DrawBoxSolidWithOutline(new Rect(butRect.x, butRect.yMax - 3f, butRect.width, 3f), a.SpellProps?.color ?? Color.white, Color.clear);
                }
                int rank = c.MasteryRank(a.def);
                if (rank > 0)
                {
                    var old = GUI.color;
                    GUI.color = rank >= Mastery.MaxRank ? new Color(1f, 0.85f, 0.35f) : new Color(0.85f, 0.85f, 0.95f);
                    for (int i = 0; i < rank; i++) GUI.DrawTexture(new Rect(butRect.x + 3f + i * 9f, butRect.yMax - 14f, 10f, 10f), ArcaneTex.Star);
                    GUI.color = old;
                }
                if (!a.OnCooldown && c.mana < a.ManaCost)
                {
                    Widgets.DrawBoxSolid(butRect, new Color(0.1f, 0.15f, 0.5f, 0.35f));
                }
            }
            return res;
        }
    }
}

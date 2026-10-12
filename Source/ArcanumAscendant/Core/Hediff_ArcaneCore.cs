using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>
    /// Invisible-ish hediff carried by every mage. Its stage is generated from attributes and skill tree ranks,
    /// and it relays weapon hits and kills back to <see cref="CompArcane"/> for on-hit passives.
    /// </summary>
    public class Hediff_ArcaneCore : Hediff
    {
        private CompArcane compInt;

        public CompArcane Arcane => compInt ??= pawn?.TryGetComp<CompArcane>();

        public override HediffStage CurStage
        {
            get
            {
                var c = Arcane;
                return c != null && c.IsMage ? c.Stage : base.CurStage;
            }
        }

        public override bool ShouldRemove => Arcane == null || !Arcane.IsMage;

        public override string LabelBase
        {
            get
            {
                var c = Arcane;
                if (c == null || !c.IsMage) return base.LabelBase;
                return "AA_CoreLabel".Translate(c.DisplayClass.label);
            }
        }

        public override string LabelInBrackets
        {
            get
            {
                var c = Arcane;
                return c != null && c.IsMage ? "AA_LevelShort".Translate(c.level).ToString() : null;
            }
        }

        public override Color LabelColor => Arcane?.DisplayClass?.color ?? base.LabelColor;

        public override string TipStringExtra
        {
            get
            {
                var c = Arcane;
                if (c == null || !c.IsMage) return base.TipStringExtra;
                var sb = new StringBuilder();
                sb.AppendLine("AA_SpellPowerLine".Translate(c.SpellPower.ToStringPercent()));
                sb.AppendLine("AA_ManaLine".Translate(c.mana.ToString("F0"), c.ManaMax.ToString("F0"), c.ManaRegenPerSecond.ToString("F1")));
                if (c.WardMax > 0.5f) sb.AppendLine("AA_WardLine".Translate(c.ward.ToString("F0"), c.WardMax.ToString("F0")));
                sb.Append(base.TipStringExtra);
                return sb.ToString().TrimEndNewlines();
            }
        }

        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);
            Arcane?.Notify_DealtWeaponDamage(thing, dinfo, result);
        }

        public override void Notify_Downed()
        {
            base.Notify_Downed();
            Arcane?.HandleDowned();
        }

        public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
        {
            base.Notify_KilledPawn(victim, dinfo);
            Arcane?.Notify_KilledByWeapon(victim);
        }
    }
}

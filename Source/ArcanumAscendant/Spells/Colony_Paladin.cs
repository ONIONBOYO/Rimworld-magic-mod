using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Paladin family colony expansion: a verdict over the fallen foe (mercy or the pyre), and an oath that turns wounds aside
    // onto the paladin.
    // ------------------------------------------------------------------------------------------------

    public enum PaladinVerdict
    {
        /// <summary>Mercy: the fallen enemy is taken captive and released at once, and walks home when able.</summary>
        Quarter,
        /// <summary>Judgment: the fallen enemy is burned where they lie and the pyre bursts over the enemies around them.</summary>
        Pyre,
    }

    /// <summary>
    /// A paladin's verdict over a fallen foe. Only downed enemies of another faction are judged; anyone still standing is left
    /// alone (a small text says so). Quarter: humanlikes only; they are captured and released on the spot, so they are no
    /// longer hostile and walk home through the colony's normal released-prisoner behaviour once they can stand; their faction
    /// warms to the colony (unless it can never make peace), spareEffects play on them first and the caster gains an optional memory.
    /// Pyre: the downed enemy is killed by holy fire and burstEffects play on everything within burstRadius of where they lay.
    /// </summary>
    public class Effect_PaladinVerdict : SpellEffect
    {
        public PaladinVerdict verdict = PaladinVerdict.Quarter;
        public int goodwill = 12;
        /// <summary>Quarter: played on the spared pawn before they are released (heals, a status for the walk home...).</summary>
        public List<SpellEffect> spareEffects;
        public ThoughtDef casterThought;
        public DamageDef killDamageDef;
        public List<SpellEffect> burstEffects;
        public float burstRadius = 5f;
        public List<VfxSpec> burstVfx;

        public Effect_PaladinVerdict()
        {
            affects = TargetFilter.Enemies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p == null || p.Dead || !p.Spawned || p.Faction == Faction.OfPlayer || p.IsPrisoner || p.IsSlave) return;
            if (!p.Downed)
            {
                ArcaneFx.Text(p, "AA_Paladin_VerdictNotDowned".Translate(), ctx.Color);
                return;
            }
            if (verdict == PaladinVerdict.Pyre) Pyre(ctx, p);
            else Quarter(ctx, p, info);
        }

        private void Quarter(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.RaceProps.Humanlike || p.Faction == null || p.guest == null) return;
            var faction = p.Faction;
            if (!spareEffects.NullOrEmpty())
                foreach (var e in spareEffects)
                    if (e != null && !p.Dead && Rand.Chance(e.chance)) e.ApplyToPawn(ctx, p, info);
            if (p.Dead || !p.Spawned) return;
            try
            {
                p.guest.CapturedBy(Faction.OfPlayer, ctx.caster);
                p.guest.Released = true;
            }
            catch (System.Exception e)
            {
                Log.Warning("[Arcanum Ascendant] Quarter could not release " + p + ": " + e.Message);
                return;
            }
            if (goodwill != 0 && !faction.IsPlayer && !faction.def.PermanentlyHostileTo(FactionDefOf.PlayerColony))
                faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwill, true, true);
            if (casterThought != null && ctx.caster?.needs?.mood != null)
                ctx.caster.needs.mood.thoughts.memories.TryGainMemory(casterThought);
            if (p.Spawned)
            {
                ArcaneFx.Text(p, "AA_Paladin_QuarterGiven".Translate(), ctx.Color);
                ArcaneFx.Flash(p.Map, p.DrawPos, ctx.Color, 2.0f);
                ArcaneFx.Particles(p.Map, p.DrawPos, ctx.Color2, ArcaneDefOf.AA_Fleck_Feather, 8, 1.2f, 0.6f);
            }
            if (ctx.caster != null && ctx.caster.Faction == Faction.OfPlayer)
                Messages.Message("AA_Paladin_QuarterMessage".Translate(ctx.caster.LabelShort, p.LabelShort, faction.Name, ctx.caster.Named("CASTER"), p.Named("PAWN")),
                    new LookTargets(p), MessageTypeDefOf.PositiveEvent);
        }

        private void Pyre(SpellContext ctx, Pawn p)
        {
            var map = p.Map;
            var cell = p.Position;
            var pos = p.DrawPos;
            if (map == Find.CurrentMap) ArcaneFx.Text(p, "AA_Paladin_PyreText".Translate(), ctx.Color);
            bool prev = CompArcane.applyingSpellDamage;
            CompArcane.applyingSpellDamage = true;
            try
            {
                p.Kill(new DamageInfo(killDamageDef ?? ArcaneDefOf.AA_ArcaneTrue, 9999f, 999f, -1f, ctx.caster));
            }
            finally
            {
                CompArcane.applyingSpellDamage = prev;
            }
            if (!p.Dead) return;
            ctx.arcane?.Notify_Kill(p, ctx);
            float r = burstRadius * ctx.RadiusFactor;
            if (map == Find.CurrentMap) ArcaneFx.PlayAll(burstVfx, map, pos, ctx, r);
            if (!burstEffects.NullOrEmpty())
                SpellRunner.Impact(ctx, new ImpactInfo { cell = cell, pos = pos, radius = r, sourcePos = pos }, burstEffects, playVfx: false);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (verdict == PaladinVerdict.Pyre)
            {
                yield return "AA_Desc_PaladinPyre".Translate((burstRadius * (ctx?.RadiusFactor ?? 1f)).ToString("0.#"));
                if (!burstEffects.NullOrEmpty())
                    foreach (var e in burstEffects)
                        if (e != null)
                            foreach (var line in e.Describe(ctx)) yield return "  " + line;
                yield break;
            }
            yield return "AA_Desc_PaladinQuarter".Translate();
            if (!spareEffects.NullOrEmpty())
                foreach (var e in spareEffects)
                    if (e != null)
                        foreach (var line in e.Describe(ctx)) yield return "  " + line;
            if (goodwill != 0) yield return "AA_Desc_PaladinQuarterGoodwill".Translate(goodwill.ToString("+0;-0"));
        }
    }

    /// <summary>
    /// The guardian's oath. A damage shell (the props are an ordinary <see cref="HediffCompProperties_DamageShell"/> whose
    /// compClass points here): the bearer takes only damageFactor of every harmful blow, and the part that was turned aside
    /// strikes the paladin who swore the oath instead, through their own armour and ward. The oath breaks when that paladin
    /// is dead, downed or not on the bearer's map.
    /// </summary>
    public class HediffComp_PaladinGuardianOath : HediffComp_DamageShell, IArcaneSourced
    {
        public Pawn source;
        private bool broken;
        private static bool transferring;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster == null || ctx.caster == Pawn) return;
            source = ctx.caster;
            broken = false;
        }

        private bool SourceValid()
        {
            var p = Pawn;
            return source != null && source != p && !source.Dead && !source.Downed && source.MapHeld != null && source.MapHeld == p?.MapHeld;
        }

        public override bool CompShouldRemove => broken || base.CompShouldRemove;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Check();
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Check();
        }

        [Unsaved] private int lastCheck = -1;

        private void Check()
        {
            int now = Find.TickManager.TicksGame;
            if (broken || now - lastCheck < 60) return;
            lastCheck = now;
            if (!SourceValid()) Break();
        }

        private void Break()
        {
            if (broken) return;
            broken = true;
            var p = Pawn;
            if (p != null && p.Spawned) ArcaneFx.Text(p, "AA_Paladin_OathBroken".Translate(), Props.color);
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (transferring || broken || totalDamageDealt < 0.5f || dinfo.Def == null || !dinfo.Def.harmsHealth) return;
            if (dinfo.Def == DamageDefOf.SurgicalCut || dinfo.Def == DamageDefOf.ExecutionCut) return;
            if (!SourceValid())
            {
                Break();
                return;
            }
            float f = Mathf.Clamp(Props.damageFactor, 0.05f, 1f);
            if (f >= 0.999f) return;
            float amount = totalDamageDealt * (1f - f) / f;
            if (amount < 0.5f) return;
            var def = dinfo.Def.Worker is DamageWorker_AddInjury ? dinfo.Def : DamageDefOf.Blunt;
            var p = Pawn;
            transferring = true;
            try
            {
                var di = new DamageInfo(def, amount, dinfo.ArmorPenetrationInt, -1f, dinfo.Instigator, null, dinfo.Weapon);
                source.TakeDamage(di);
                if (p.Spawned && source.Spawned && p.Map == source.Map && p.Map == Find.CurrentMap)
                    ArcaneVfxManager.For(p.Map).Add(new VfxBeam(p.DrawPos, source.DrawPos, 14, ArcaneMats.Get("ArcanumAscendant/Vfx/Beam"), Props.color, Color.white, 0.35f));
            }
            catch (System.Exception e)
            {
                Log.Warning("[Arcanum Ascendant] Guardian's oath could not pass a wound on: " + e.Message);
            }
            finally
            {
                transferring = false;
            }
        }

        public override string CompLabelInBracketsExtra => source != null ? source.LabelShort : null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "aaOathSource");
            Scribe_Values.Look(ref broken, "aaOathBroken");
        }
    }
}

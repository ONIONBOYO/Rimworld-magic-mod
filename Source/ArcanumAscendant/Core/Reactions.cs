using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArcanumAscendant
{
    /// <summary>
    /// An elemental reaction: when a spell of one element hits a target carrying a status from another, the two combine
    /// into something stronger (fire on a frozen target shatters it, lightning on a chilled one superconducts...).
    /// </summary>
    public class ArcaneReactionDef : Def
    {
        /// <summary>Damage types that set the reaction off.</summary>
        public List<DamageDef> triggerDamage = new List<DamageDef>();
        /// <summary>Statuses the target must carry (any of them).</summary>
        public List<HediffDef> requiredHediffs = new List<HediffDef>();
        public bool consume = true;
        public float bonusDamage;
        public DamageDef bonusDamageDef;
        public bool bonusTrue;
        /// <summary>When above zero the bonus damage and effects hit every enemy in this radius.</summary>
        public float radius;
        /// <summary>Extra effects (stuns, statuses, heals) applied to the target or the area.</summary>
        public List<SpellEffect> effects = new List<SpellEffect>();
        public float healCaster;
        public float manaCaster;
        public VfxStyle style = VfxStyle.Arcane;
        public Color color = Color.white;
        public Color color2 = Color.white;
        /// <summary>Visual preset: Shatter, Steam, Overload, Superconduct, Wildfire, Purge, Fracture, Resonance, Bloom, Boil, Twilight.</summary>
        public string vfx;
        public float shake = 0.6f;
        public SoundDef sound;
        public int cooldownTicks = 45;
        /// <summary>Short callout shown over the target, e.g. "SHATTER!".</summary>
        public string callout;
        public string descriptionShort;

        public string Callout => callout.NullOrEmpty() ? label.CapitalizeFirst() + "!" : callout;
    }

    public static class ArcaneReactions
    {
        private static readonly Dictionary<int, int> lastTriggered = new Dictionary<int, int>();
        private static List<ArcaneReactionDef> all;

        public static List<ArcaneReactionDef> All => all ??= DefDatabase<ArcaneReactionDef>.AllDefsListForReading.ToList();

        /// <summary>Checks a fresh spell hit for a reaction and sets it off. At most one reaction per hit.</summary>
        public static void TryTrigger(SpellContext ctx, Pawn p, DamageDef dmg)
        {
            if (ctx == null || p == null || dmg == null || ctx.inReaction || p.Dead || p.Destroyed || p.MapHeld == null) return;
            if (!ArcanumMod.Settings.reactions) return;
            var hediffs = p.health?.hediffSet;
            if (hediffs == null) return;
            int now = Find.TickManager.TicksGame;
            foreach (var r in All)
            {
                if (!r.triggerDamage.Contains(dmg)) continue;
                Hediff h = null;
                foreach (var hd in r.requiredHediffs)
                {
                    h = hediffs.GetFirstHediffOfDef(hd);
                    if (h != null) break;
                }
                if (h == null) continue;
                int key = p.thingIDNumber * 31 + r.index;
                if (lastTriggered.TryGetValue(key, out int last) && now - last < r.cooldownTicks) continue;
                lastTriggered[key] = now;
                if (lastTriggered.Count > 4000) lastTriggered.Clear();
                Trigger(r, ctx, p, h);
                return;
            }
        }

        private static void Trigger(ArcaneReactionDef r, SpellContext ctx, Pawn p, Hediff h)
        {
            var map = p.MapHeld;
            Vector3 pos = p.DrawPos;
            IntVec3 cell = p.PositionHeld;
            ctx.inReaction = true;
            try
            {
                if (r.consume && !p.Dead) p.health.RemoveHediff(h);
                ReactionFx.Play(r, map, pos, ctx);
                if (map == Find.CurrentMap)
                {
                    var mgr = ArcaneVfxManager.For(map);
                    mgr.screen.AddText(pos + new Vector3(0f, 0f, 0.5f), r.Callout, Color.Lerp(r.color, Color.white, 0.25f), 1.4f, mgr.Now, 95f, true);
                }
                r.sound?.PlayOneShot(new TargetInfo(cell, map));
                var targets = new List<Pawn>();
                if (r.radius > 0.5f)
                {
                    foreach (var t in GenRadial.RadialDistinctThingsAround(cell, map, Mathf.Min(r.radius * ctx.RadiusFactor, GenRadial.MaxRadialPatternRadius - 1f), true))
                        if (t is Pawn o && !o.Dead && (o == p || ctx.Passes(o, TargetFilter.Enemies))) targets.Add(o);
                }
                else targets.Add(p);
                if (r.bonusDamage > 0f)
                {
                    var effect = new Effect_Damage { amount = r.bonusDamage, damageDef = r.bonusDamageDef, trueDamage = r.bonusTrue, noCrit = false };
                    foreach (var t in targets)
                        if (!t.Dead) effect.DealDamage(ctx, t, r.bonusDamage * ctx.DamageFactor, false);
                }
                if (!r.effects.NullOrEmpty())
                {
                    var info = new ImpactInfo { cell = cell, pos = pos, primary = p, radius = r.radius > 0.5f ? r.radius : 0f, sourcePos = pos };
                    SpellRunner.Impact(ctx, info, r.effects, playVfx: false);
                }
                if (ctx.caster != null && !ctx.caster.Dead)
                {
                    if (r.healCaster > 0f) ArcaneUtility.HealPawn(ctx.caster, r.healCaster * ctx.HealFactor);
                    if (r.manaCaster > 0f && ctx.arcane != null && ctx.arcane.IsMage) ctx.arcane.mana = Mathf.Min(ctx.arcane.ManaMax, ctx.arcane.mana + r.manaCaster);
                }
                ctx.arcane?.GainXp(12f);
                ctx.arcane?.Notify_Reaction(r);
                ArcaneFx.Shake(map, r.shake);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Reaction " + r.defName + " failed: " + e, r.shortHash ^ 0x5A1C);
            }
            finally
            {
                ctx.inReaction = false;
            }
        }
    }

    /// <summary>The look of each reaction. They are deliberately louder than normal hits.</summary>
    public static class ReactionFx
    {
        public static void Play(ArcaneReactionDef r, Map map, Vector3 pos, SpellContext ctx)
        {
            if (map == null || map != Find.CurrentMap) return;
            var mgr = ArcaneVfxManager.For(map);
            Color c = r.color, c2 = r.color2;
            float rad = Mathf.Max(1f, r.radius);
            ArcaneFx.Flash(map, pos, Color.Lerp(c, Color.white, 0.5f), 2.6f + rad);
            mgr.Add(new VfxSprite { mat = ArcaneMats.Flare, pos = pos, scaleStart = 0.6f, scaleEnd = 3f + rad, duration = 18, color = Color.Lerp(c2, Color.white, 0.4f), popIn = true, spin = 12f });
            mgr.Add(new VfxSprite { mat = ArcaneMats.Shock, pos = pos, scaleStart = 0.3f, scaleEnd = rad * 2.8f + 1.5f, duration = 18, color = c });
            ArcaneLightFlash.Spawn(map, pos, c, 4f + rad * 1.5f, 16);
            switch (r.vfx)
            {
                case "Shatter":
                    Fx.Shards(map, pos, Color.Lerp(c, Color.white, 0.4f), 24f, 6f, 0.36f);
                    Fx.Debris(map, pos, new Color(0.8f, 0.9f, 1f, 0.95f), 8f, 4f, 0.24f, PMat.IceChunk);
                    Fx.Sparks(map, pos, c2, 14f, 7f);
                    mgr.decals.Add(pos, DecalKind.Frost, 2.6f, Color.white, 14f);
                    break;
                case "Steam":
                    Fx.Smoke(map, pos, new Color(0.92f, 0.94f, 0.96f, 0.7f), 10f, 1.2f, 1.4f, 2.4f, 1.2f, false, 2.2f);
                    Fx.Mist(map, pos, new Color(0.95f, 0.97f, 1f, 0.35f), 6f, 2f, 3f, 1.8f);
                    break;
                case "Overload":
                    Fx.Flames(map, pos, c, c2, 14f, 1.2f, 0.8f);
                    Fx.Sparks(map, pos, c2, 30f, 9f);
                    Fx.Smoke(map, pos, new Color(0.15f, 0.14f, 0.13f, 0.75f), 6f, 1.2f, 1.4f, 3f, 0.8f, true, 1.5f);
                    Fx.Debris(map, pos, new Color(0.4f, 0.37f, 0.33f), 6f, 4f);
                    mgr.Add(new VfxArcs { pos = pos, radius = rad, color = c2, duration = 30, intensity = 1.5f });
                    mgr.decals.Add(pos, DecalKind.Scorch, rad * 2.4f + 1f, Color.white, 40f);
                    mgr.screen.AddFlash(Color.Lerp(c, Color.white, 0.5f), 0.12f, 12f, false, mgr.Now);
                    break;
                case "Superconduct":
                    mgr.Add(new VfxArcs { pos = pos, radius = rad + 0.5f, color = c, duration = 40, intensity = 2f });
                    Fx.Shards(map, pos, c2, 12f, 4f, 0.28f);
                    Fx.Sparks(map, pos, Color.Lerp(c, Color.white, 0.4f), 20f, 7f);
                    break;
                case "Wildfire":
                    for (int i = 0; i < 10; i++) Fx.Flames(map, pos + VRand.OnCircle(rad * VRand.Range(0.4f, 1f)), c, c2, 3f, 0.3f, 0.7f);
                    Fx.Embers(map, pos, c, 24f, rad, 1.6f, 2.4f);
                    Fx.Burst(map, pos, new Color(0.45f, 0.6f, 0.25f), 10f, PMat.Leaf, 3f, 1.6f, 0.26f, PFlags.Wobble | PFlags.Lit, 300f, 2f);
                    mgr.decals.Add(pos, DecalKind.Scorch, rad * 2.2f, Color.white, 40f);
                    break;
                case "Purge":
                    mgr.Add(new VfxPillar(pos, 50, c, c2, 2f));
                    mgr.Add(new VfxGodRays(pos, rad, new Color(c.r, c.g, c.b, 0.7f), 5, 80));
                    Fx.Rise(map, pos, Color.Lerp(c, Color.white, 0.5f), 18f, PMat.Star, rad, 2.4f, 1.4f, 0.22f, PFlags.Glint);
                    Fx.Smoke(map, pos, new Color(0.15f, 0.08f, 0.2f, 0.6f), 5f, 0.6f, 1f, 2f, 1.4f, true, 1f);
                    mgr.decals.Add(pos, DecalKind.Holy, rad * 2.4f + 1f, new Color(c.r, c.g, c.b, 0.85f), 6f);
                    break;
                case "Fracture":
                    Fx.Implode(map, pos, c, 30f, 3f, 0.4f, -1, 0.16f, 12f);
                    mgr.Add(new VfxSprite { mat = PMat.Get(PMat.Clock), pos = pos, scaleStart = 3.2f, scaleEnd = 0.2f, duration = 26, color = c2, spin = -30f });
                    mgr.Schedule(10, () =>
                    {
                        Fx.Shards(map, pos, c2, 18f, 5f, 0.3f);
                        mgr.Add(new VfxSprite { mat = ArcaneMats.BlackHole, pos = pos, scaleStart = 0.5f, scaleEnd = 2.6f, duration = 24, color = Color.white, popIn = true, layer = AltitudeLayer.MoteOverhead });
                    });
                    break;
                case "Resonance":
                    for (int i = 0; i < 4; i++)
                    {
                        int k = i;
                        mgr.Schedule(i * 4, () => mgr.Add(new VfxSprite { mat = ArcaneMats.Ring, pos = pos, scaleStart = 0.3f, scaleEnd = 3f + k * 1.2f, duration = 18, color = c }));
                    }
                    Fx.Shards(map, pos, Color.Lerp(c2, Color.white, 0.3f), 22f, 6f, 0.32f);
                    Fx.Burst(map, pos, c, 8f, PMat.Note, 3f, 1f, 0.3f, PFlags.Wobble);
                    break;
                case "Bloom":
                    Fx.Rise(map, pos, c, 16f, PMat.Bubble, rad, 1.2f, 1.6f, 0.2f);
                    Fx.Mist(map, pos, new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, 0.4f), 8f, rad, 3.4f, 1.6f);
                    Fx.Burst(map, pos, new Color(0.55f, 0.04f, 0.06f, 0.95f), 16f, PMat.BloodAlpha, 4f, 0.9f, 0.16f, PFlags.Lit | PFlags.AlignVel, 0f, 9f, 0.4f);
                    mgr.decals.Add(pos, DecalKind.Rot, rad * 2.2f, Color.white, 30f);
                    break;
                case "Boil":
                    Fx.Burst(map, pos, new Color(0.6f, 0.05f, 0.07f, 0.95f), 18f, PMat.BloodAlpha, 4f, 1f, 0.16f, PFlags.Lit | PFlags.AlignVel, 0f, 9f, 0.4f);
                    Fx.Smoke(map, pos, new Color(0.9f, 0.85f, 0.85f, 0.6f), 8f, 0.8f, 1.2f, 2f, 1.4f, false, 1.4f);
                    Fx.Flames(map, pos, c, c2, 8f, 0.5f, 0.6f);
                    mgr.decals.Add(pos, DecalKind.Blood, 2.2f, Color.white, 30f);
                    break;
                case "Twilight":
                    Fx.Smoke(map, pos, new Color(0.1f, 0.05f, 0.15f, 0.8f), 8f, 1f, 1.2f, 2.2f, 0.6f, true, 1.5f);
                    Fx.Rise(map, pos, Color.Lerp(c2, Color.white, 0.4f), 14f, PMat.Star, 1f, 2f, 1.2f, 0.2f, PFlags.Glint);
                    Fx.Implode(map, pos, c, 16f, 2f, 0.4f, PMat.Wisp, 0.3f, 8f);
                    break;
                default:
                    Fx.Sparks(map, pos, c2, 20f, 6f);
                    break;
            }
        }
    }
}

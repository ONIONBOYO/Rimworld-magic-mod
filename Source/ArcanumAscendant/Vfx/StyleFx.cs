using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>
    /// The visual language of each school. Every spell gets these layers on top of its own authored effects: a casting
    /// circle and charge, a styled projectile, a signature impact, a signature death, ambience for persistent fields and
    /// emitters for status effects.
    /// </summary>
    public static class StyleFx
    {
        private static readonly Color Smoke = new Color(0.16f, 0.15f, 0.14f, 0.75f);
        private static readonly Color Dust = new Color(0.55f, 0.5f, 0.42f, 0.6f);
        private static readonly Color Rock = new Color(0.42f, 0.38f, 0.34f, 1f);

        public static VfxStyle Resolve(SpellContext ctx) => Resolve(ctx?.props, ctx?.ext);

        public static VfxStyle Resolve(CompProperties_ArcaneSpell props, SpellExtension ext)
        {
            var s = props?.style ?? VfxStyle.Auto;
            if (s != VfxStyle.Auto) return s;
            return FromTags(ext?.tags);
        }

        public static VfxStyle FromTags(List<string> tags)
        {
            if (tags != null)
                foreach (var t in tags)
                    switch (t)
                    {
                        case "Fire": return VfxStyle.Fire;
                        case "Frost": return VfxStyle.Frost;
                        case "Storm": return VfxStyle.Storm;
                        case "Plague": return VfxStyle.Plague;
                        case "Necromancy": return VfxStyle.Necro;
                        case "Holy": return VfxStyle.Holy;
                        case "Time": return VfxStyle.Time;
                        case "Void":
                        case "Space": return VfxStyle.Void;
                        case "Nature":
                        case "Beast": return VfxStyle.Nature;
                        case "Rune":
                        case "Gun": return VfxStyle.Rune;
                        case "Blood": return VfxStyle.Blood;
                        case "Shadow":
                        case "Hex": return VfxStyle.Shadow;
                        case "Song":
                        case "Chaos": return VfxStyle.Song;
                        case "Wind":
                        case "Air": return VfxStyle.Wind;
                        case "Doom":
                        case "Omen": return VfxStyle.Doom;
                        case "Desire":
                        case "Charm": return VfxStyle.Desire;
                        case "Liminal":
                        case "Backrooms": return VfxStyle.Liminal;
                    }
            return VfxStyle.Arcane;
        }

        private static bool Harmful(SpellContext ctx)
        {
            var role = ctx?.ext?.role ?? SpellRole.Attack;
            if (role == SpellRole.Heal || role == SpellRole.Buff || role == SpellRole.Utility || role == SpellRole.Mobility || role == SpellRole.Summon) return false;
            var a = ctx?.props?.affects ?? TargetFilter.Enemies;
            return a == TargetFilter.Enemies || a == TargetFilter.AllButSelf || a == TargetFilter.All || a == TargetFilter.Inherit;
        }

        private static Color W(Color c, float t) => Color.Lerp(c, Color.white, t);
        private static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static void Sprite(Map map, Material mat, Vector3 pos, float s0, float s1, int ticks, Color c, bool pop = false, float spin = 0f,
            AltitudeLayer layer = AltitudeLayer.VisEffects, float fadeIn = 0.02f, float fadeOut = 0.75f)
        {
            ArcaneVfxManager.For(map)?.Add(new VfxSprite
            {
                mat = mat, pos = pos, scaleStart = s0, scaleEnd = s1, duration = ticks, color = c, popIn = pop, spin = spin, angle = VRand.Range(0f, 360f),
                layer = layer, fadeIn = fadeIn, fadeOut = fadeOut,
            });
        }

        private static void Shockwave(Map map, Vector3 pos, float radius, Color c, Color dust)
        {
            Sprite(map, ArcaneMats.Shock, pos, 0.3f, radius * 2.6f + 1f, 18, c);
            Sprite(map, ArcaneMats.Ring, pos, 0.2f, radius * 2.2f + 0.6f, 24, A(c, c.a * 0.6f), layer: AltitudeLayer.MoteLow);
            if (dust.a > 0f) Fx.RingBurst(map, pos, dust, 8f + radius * 6f, PMat.Dust, Mathf.Max(0.3f, radius * 0.4f), 2.5f + radius, 1.6f, 0.8f + radius * 0.15f);
        }

        private static void Decal(Map map, Vector3 pos, DecalKind k, float size, Color c, float life)
        {
            ArcaneVfxManager.For(map)?.decals.Add(pos, k, size, c, life);
        }

        private static void Light(Map map, Vector3 pos, Color c, float radius, int ticks) => ArcaneLightFlash.Spawn(map, pos, c, radius, ticks);

        public static float TrailWidth(VfxStyle s)
        {
            switch (s)
            {
                case VfxStyle.Storm: return 0.5f;
                case VfxStyle.Frost: return 0.7f;
                case VfxStyle.Fire: return 1.15f;
                case VfxStyle.Void: return 1.1f;
                case VfxStyle.Shadow: return 1.25f;
                case VfxStyle.Holy: return 0.9f;
                case VfxStyle.Necro: return 1f;
                case VfxStyle.Blood: return 0.85f;
                case VfxStyle.Wind: return 0.55f;
                case VfxStyle.Doom: return 1.05f;
                case VfxStyle.Desire: return 0.75f;
                case VfxStyle.Liminal: return 0.6f;
                default: return 0.8f;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Projectiles and beams
        // ------------------------------------------------------------------------------------------------

        public static void Muzzle(VfxStyle s, Map map, Vector3 pos, float angle, Color c, Color c2, float scale)
        {
            if (map == null) return;
            ArcaneFx.Flash(map, pos, c, 1.2f + scale);
            Fx.Sparks(map, pos, c2, 5f, 4f, 0.1f, 0.3f, angle, 50f, 0f);
            switch (s)
            {
                case VfxStyle.Fire:
                    Fx.Flames(map, pos, c, c2, 3f, 0.15f, 0.4f);
                    Fx.Smoke(map, pos, Smoke, 1f, 0.1f, 0.5f, 1.4f, 0.4f, true, 0.6f);
                    break;
                case VfxStyle.Frost:
                    Fx.Burst(map, pos, W(c2, 0.4f), 4f, PMat.Snow, 1.5f, 0.7f, 0.22f, PFlags.Wobble);
                    break;
                case VfxStyle.Storm:
                    ArcaneVfxManager.For(map)?.Add(new VfxArcs { pos = pos, radius = 0.45f, color = c, duration = 10 });
                    break;
                case VfxStyle.Necro:
                case VfxStyle.Shadow:
                    Fx.Smoke(map, pos, A(c * 0.35f, 0.6f), 1f, 0.15f, 0.6f, 1.2f, 0.3f, true);
                    break;
                case VfxStyle.Holy:
                    Sprite(map, ArcaneMats.Flare, pos, 0.4f, 1.6f, 12, W(c2, 0.5f), true);
                    break;
                case VfxStyle.Rune:
                    Fx.Burst(map, pos, c, 2f, PMat.Rune, 1.5f, 0.5f, 0.25f);
                    break;
                case VfxStyle.Song:
                    Fx.Burst(map, pos, c, 2f, PMat.Note, 1.2f, 0.8f, 0.3f, PFlags.Wobble);
                    break;
                case VfxStyle.Wind:
                    Fx.Burst(map, pos, W(c2, 0.3f), 5f, PMat.Gust, 3f, 0.4f, 0.5f, PFlags.Stretch | PFlags.AlignVel, 0f, 0f, 0.2f, 0.2f);
                    Fx.Burst(map, pos, new Color(0.8f, 0.78f, 0.7f, 0.5f), 2f, PMat.Dust, 1f, 0.8f, 0.4f, PFlags.Lit);
                    break;
                case VfxStyle.Doom:
                    Fx.Smoke(map, pos, A(c * 0.25f, 0.6f), 1f, 0.15f, 0.6f, 1.4f, 0.3f, true);
                    Fx.Burst(map, pos, new Color(0.08f, 0.06f, 0.07f, 0.95f), 2f, PMat.FeatherDark, 1.2f, 1.4f, 0.28f, PFlags.Wobble | PFlags.Lit, 120f, 0.8f, 0.5f);
                    break;
                case VfxStyle.Desire:
                    Fx.Burst(map, pos, c, 3f, PMat.Heart, 1.6f, 0.7f, 0.22f, PFlags.Wobble);
                    Fx.Burst(map, pos, W(c2, 0.4f), 3f, PMat.Star, 1.8f, 0.5f, 0.16f, PFlags.Glint);
                    break;
                case VfxStyle.Liminal:
                    Fx.Burst(map, pos, c, 4f, PMat.Glitch, 1.5f, 0.3f, 0.22f, PFlags.Flicker, 0f, 0f, 0.3f, 0.3f);
                    break;
            }
        }

        public static void ProjectileTick(VfxStyle s, Map map, Vector3 p, int age, Color c, Color c2, float scale, bool meteor)
        {
            if (map == null) return;
            if (meteor)
            {
                Fx.Flames(map, p, c, c2, 2f * scale, 0.25f * scale, 0.7f * scale);
                if (age % 2 == 0) Fx.Embers(map, p, c, 2f, 0.3f, 0.4f, 1.2f);
                if (age % 3 == 0) Fx.Smoke(map, p, Smoke, 1f, 0.3f, 1f * scale, 2.2f, 0.2f, true, 0.2f);
                return;
            }
            switch (s)
            {
                case VfxStyle.Fire:
                    Fx.Flames(map, p, c, c2, 0.8f, 0.1f, 0.35f * scale);
                    if (age % 3 == 0) Fx.Embers(map, p, c, 1f, 0.1f, 0.6f, 0.9f, 0.12f);
                    break;
                case VfxStyle.Frost:
                    if (age % 2 == 0) Fx.Burst(map, p, W(c2, 0.3f), 1f, PMat.Snow, 0.4f, 0.8f, 0.18f, PFlags.Wobble | PFlags.Glint);
                    break;
                case VfxStyle.Storm:
                    if (age % 2 == 0) Fx.Sparks(map, p, c, 1.5f, 2.5f, 0.08f, 0.2f, -1f, 360f, 0f);
                    break;
                case VfxStyle.Necro:
                    if (age % 2 == 0) Fx.Rise(map, p, A(c, 0.8f), 1f, PMat.Wisp, 0.1f, 0.6f, 0.6f, 0.3f, PFlags.None, 200f);
                    break;
                case VfxStyle.Holy:
                    if (age % 2 == 0) Fx.Burst(map, p, W(c, 0.4f), 1f, PMat.Star, 0.3f, 0.6f, 0.2f, PFlags.Glint);
                    break;
                case VfxStyle.Time:
                    if (age % 3 == 0) Fx.Burst(map, p, c, 1f, PMat.Clock, 0.3f, 0.7f, 0.25f);
                    break;
                case VfxStyle.Void:
                    if (age % 2 == 0) Fx.Implode(map, p, c, 1f, 0.8f, 0.3f, -1, 0.1f, 6f);
                    break;
                case VfxStyle.Nature:
                    if (age % 3 == 0) Fx.Burst(map, p, c, 1f, PMat.Leaf, 0.5f, 1f, 0.22f, PFlags.Wobble | PFlags.Lit, 120f, 1.2f);
                    break;
                case VfxStyle.Rune:
                    if (age % 3 == 0) Fx.Burst(map, p, c, 1f, PMat.Rune, 0.4f, 0.6f, 0.2f);
                    break;
                case VfxStyle.Shadow:
                    if (age % 2 == 0) Fx.Smoke(map, p, A(c * 0.3f, 0.5f), 1f, 0.1f, 0.5f, 0.9f, 0.1f, true, 0.2f);
                    break;
                case VfxStyle.Blood:
                    if (age % 2 == 0) Fx.Drips(map, p, A(c, 0.9f), 1f, 0.3f);
                    break;
                case VfxStyle.Song:
                    if (age % 3 == 0) Fx.Burst(map, p, c, 1f, PMat.Note, 0.3f, 0.8f, 0.22f, PFlags.Wobble);
                    break;
                case VfxStyle.Plague:
                    if (age % 3 == 0) Fx.Rise(map, p, c, 1f, PMat.Bubble, 0.1f, 0.5f, 0.6f, 0.15f);
                    break;
                case VfxStyle.Wind:
                    Fx.Burst(map, p, A(W(c2, 0.2f), 0.7f), 1f, PMat.Gust, 0.6f, 0.4f, 0.45f, PFlags.Stretch | PFlags.AlignVel, 0f, 0f, 0.1f, 0.15f);
                    if (age % 5 == 0) Fx.Burst(map, p, new Color(0.55f, 0.7f, 0.35f, 1f), 1f, PMat.Leaf, 0.8f, 1.2f, 0.18f, PFlags.Wobble | PFlags.Lit, 300f, 0.5f);
                    break;
                case VfxStyle.Doom:
                    if (age % 3 == 0) Fx.Burst(map, p, new Color(0.08f, 0.06f, 0.07f, 0.95f), 1f, PMat.FeatherDark, 0.4f, 1.3f, 0.24f, PFlags.Wobble | PFlags.Lit, 140f, 0.6f, 0.3f);
                    if (age % 2 == 0) Fx.Embers(map, p, c, 1f, 0.1f, 0.4f, 0.8f, 0.1f);
                    break;
                case VfxStyle.Desire:
                    if (age % 2 == 0) Fx.Burst(map, p, c, 1f, PMat.Heart, 0.3f, 0.7f, 0.18f, PFlags.Wobble);
                    if (age % 4 == 0) Fx.Burst(map, p, W(c2, 0.2f), 1f, PMat.Petal, 0.5f, 1.2f, 0.18f, PFlags.Wobble, 200f, 0.4f);
                    break;
                case VfxStyle.Liminal:
                    if (age % 2 == 0) Fx.Burst(map, p + VRand.InsideCircle(0.25f), c, 1f, PMat.Glitch, 0.1f, 0.35f, 0.2f, PFlags.Flicker, 0f);
                    break;
                default:
                    if (age % 2 == 0) Fx.Burst(map, p, c, 1f, PMat.Mote, 0.3f, 0.5f, 0.2f);
                    break;
            }
        }

        /// <summary>Extra layers drawn on a projectile's head every frame.</summary>
        public static void ProjectileHead(VfxStyle s, Vector3 hp, float size, float now, Color c, Color c2)
        {
            switch (s)
            {
                case VfxStyle.Holy:
                    ArcaneMats.DrawQuad(ArcaneMats.Flare, hp + new Vector3(0, 0.001f, 0), size * 2.6f, now * 6f, W(c2, 0.5f) * new Color(1, 1, 1, 0.75f));
                    break;
                case VfxStyle.Void:
                    ArcaneMats.DrawQuad(ArcaneMats.BlackHole, hp + new Vector3(0, 0.001f, 0), size * 1.1f, 0f, new Color(1f, 1f, 1f, 0.9f));
                    ArcaneMats.DrawQuad(ArcaneMats.Ring, hp + new Vector3(0, 0.002f, 0), size * 1.8f, 0f, c * new Color(1, 1, 1, 0.8f));
                    break;
                case VfxStyle.Time:
                    ArcaneMats.DrawQuad(PMat.Get(PMat.Clock), hp + new Vector3(0, 0.001f, 0), size * 1.9f, -now * 12f, c * new Color(1, 1, 1, 0.6f));
                    break;
                case VfxStyle.Storm:
                    float j = Mathf.PerlinNoise(now * 0.9f, 1f);
                    ArcaneMats.DrawQuad(ArcaneMats.Flare, hp + new Vector3(0, 0.001f, 0), size * (1.8f + j), now * 20f, new Color(1f, 1f, 1f, 0.6f + 0.4f * j));
                    break;
                case VfxStyle.Fire:
                    ArcaneMats.DrawQuad(PMat.Get(PMat.Flame), hp + new Vector3(0f, 0.001f, 0.1f), size * 1.6f, Mathf.Sin(now * 0.7f) * 12f, c2 * new Color(1, 1, 1, 0.8f));
                    break;
                case VfxStyle.Wind:
                    ArcaneMats.DrawQuad(PMat.Get(PMat.Gust), hp + new Vector3(0, 0.001f, 0), size * 2.2f, now * 25f, W(c2, 0.3f) * new Color(1, 1, 1, 0.8f));
                    ArcaneMats.DrawQuad(PMat.Get(PMat.Gust), hp + new Vector3(0, 0.002f, 0), size * 1.6f, -now * 31f, c * new Color(1, 1, 1, 0.7f));
                    break;
                case VfxStyle.Doom:
                    ArcaneMats.DrawQuad(PMat.Get(PMat.Eye), hp + new Vector3(0, 0.001f, 0), size * (1.5f + 0.15f * Mathf.Sin(now * 0.5f)), 0f, W(c, 0.15f));
                    break;
                case VfxStyle.Desire:
                    ArcaneMats.DrawQuad(PMat.Get(PMat.Heart), hp + new Vector3(0, 0.001f, 0), size * (1.4f + 0.2f * Mathf.Sin(now * 0.6f)), 0f, W(c, 0.25f));
                    break;
                case VfxStyle.Liminal:
                    if (Mathf.PerlinNoise(now * 0.4f, 3.3f) > 0.35f)
                        ArcaneMats.DrawQuad(PMat.Get(PMat.Glitch), hp + new Vector3(VRand.Range(-0.05f, 0.05f), 0.001f, 0f), size * 1.8f, 0f, c * new Color(1, 1, 1, 0.8f));
                    break;
            }
        }

        public static void BeamImpactTick(VfxStyle s, Map map, Vector3 end, Color c, Color c2)
        {
            switch (s)
            {
                case VfxStyle.Fire:
                    Fx.Flames(map, end, c, c2, 2f, 0.3f, 0.5f);
                    Fx.Smoke(map, end, Smoke, 0.5f, 0.3f, 0.8f, 2f, 0.5f, true);
                    break;
                case VfxStyle.Frost:
                    Fx.Mist(map, end, A(W(c, 0.5f), 0.35f), 0.6f, 0.4f, 2f, 1f);
                    break;
                case VfxStyle.Void:
                    Fx.Implode(map, end, c, 2f, 1.2f, 0.35f, -1, 0.12f, 8f);
                    break;
                case VfxStyle.Holy:
                    Fx.Rise(map, end, W(c, 0.5f), 1.5f, PMat.Star, 0.4f, 1.5f, 0.7f, 0.2f, PFlags.Glint);
                    break;
                case VfxStyle.Wind:
                    Fx.Burst(map, end, W(c2, 0.2f), 2f, PMat.Gust, 2.5f, 0.4f, 0.45f, PFlags.Stretch | PFlags.AlignVel, 0f, 0f, 0.1f, 0.3f);
                    break;
                case VfxStyle.Doom:
                    Fx.Embers(map, end, c, 1.5f, 0.3f, 0.6f, 0.9f, 0.12f);
                    break;
                case VfxStyle.Desire:
                    Fx.Burst(map, end, c, 1.5f, PMat.Heart, 1.2f, 0.6f, 0.18f, PFlags.Wobble);
                    break;
                case VfxStyle.Liminal:
                    Fx.Burst(map, end, c, 2f, PMat.Glitch, 1.2f, 0.3f, 0.2f, PFlags.Flicker);
                    break;
                default:
                    Fx.Burst(map, end, c, 1.5f, PMat.Mote, 1.5f, 0.5f, 0.2f);
                    break;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Casting
        // ------------------------------------------------------------------------------------------------

        public static void CastTick(VfxStyle s, Map map, Pawn pawn, Vector3 hand, int age, float progress, Color c1, Color c2, bool ultimate)
        {
            Vector3 feet = pawn.DrawPos;
            switch (s)
            {
                case VfxStyle.Fire:
                    if (age % 3 == 0) Fx.Flames(map, hand, c1, c2, 0.6f + progress, 0.08f, 0.25f + progress * 0.2f);
                    if (age % 8 == 0) Fx.Embers(map, feet, c1, 1f + progress * 2f, 1f, 0.8f, 1.2f, 0.12f);
                    break;
                case VfxStyle.Frost:
                    if (age % 6 == 0) Fx.Mist(map, feet, A(W(c1, 0.5f), 0.25f), 1f, 1f, 2f, 1.2f);
                    if (age % 5 == 0) Fx.Burst(map, hand, W(c2, 0.4f), 1f, PMat.Snow, 0.6f, 0.7f, 0.18f, PFlags.Wobble);
                    break;
                case VfxStyle.Storm:
                    if (age % 9 == 0) ArcaneVfxManager.For(map)?.Add(new VfxArcs { follow = pawn, radius = 0.6f + progress * 0.4f, color = c1, duration = 8, intensity = 1f });
                    break;
                case VfxStyle.Necro:
                case VfxStyle.Plague:
                    if (age % 4 == 0) Fx.Rise(map, feet, A(c1, 0.7f), 1f, PMat.Wisp, 1.2f, 0.9f, 0.8f, 0.3f, PFlags.None, 200f);
                    break;
                case VfxStyle.Holy:
                    if (age % 4 == 0) Fx.Rise(map, feet, W(c1, 0.4f), 1f, PMat.Star, 1.2f, 1.6f, 0.9f, 0.18f, PFlags.Glint);
                    if (age % 9 == 0) Fx.Burst(map, feet + new Vector3(0f, 0f, 1.2f), W(c2, 0.5f), 1f, PMat.Feather, 0.3f, 1.6f, 0.3f, PFlags.Wobble, 60f, 0.5f, 0.6f);
                    break;
                case VfxStyle.Time:
                    if (age % 6 == 0) Fx.Vortex(map, feet, c1, 1f, 1.8f, PMat.Clock, 0.9f, 0.25f, 5f);
                    break;
                case VfxStyle.Void:
                    if (age % 2 == 0) Fx.Implode(map, hand, A(c1, 0.8f), 1f, 1.6f, 0.4f, -1, 0.1f, 10f);
                    break;
                case VfxStyle.Nature:
                    if (age % 6 == 0) Fx.Rise(map, feet, c1, 1f, PMat.Leaf, 1f, 0.8f, 1.2f, 0.22f, PFlags.Wobble | PFlags.Lit, 120f);
                    break;
                case VfxStyle.Rune:
                    if (age % 5 == 0) Fx.Rise(map, feet, c1, 1f, PMat.Rune, 1f, 1.2f, 0.8f, 0.2f);
                    break;
                case VfxStyle.Shadow:
                    if (age % 4 == 0) Fx.Smoke(map, feet, A(c1 * 0.25f, 0.45f), 1f, 0.8f, 0.7f, 1.2f, 0.2f, true, 0.3f);
                    break;
                case VfxStyle.Blood:
                    if (age % 5 == 0) Fx.Implode(map, hand, c1, 1f, 1.4f, 0.5f, PMat.Blood, 0.14f, 4f);
                    break;
                case VfxStyle.Song:
                    if (age % 6 == 0) Fx.Rise(map, feet, c1, 1f, PMat.Note, 0.8f, 1f, 1f, 0.28f, PFlags.Wobble);
                    if (age % 15 == 0) Sprite(map, ArcaneMats.Ring, feet, 0.4f, 2.6f, 20, A(c1, 0.6f), layer: AltitudeLayer.MoteLow);
                    break;
                case VfxStyle.Wind:
                    if (age % 3 == 0) Fx.Vortex(map, feet, A(W(c2, 0.2f), 0.75f), 1f, 1.5f + progress, PMat.Gust, 0.6f, 0.45f, 12f);
                    if (age % 7 == 0) Fx.Rise(map, feet, new Color(0.55f, 0.7f, 0.35f, 1f), 1f, PMat.Leaf, 1.2f, 1.8f, 0.9f, 0.18f, PFlags.Wobble | PFlags.Lit, 300f);
                    if (age % 10 == 0) Fx.RingBurst(map, feet, new Color(0.8f, 0.78f, 0.7f, 0.35f), 4f, PMat.Dust, 0.6f, 1.2f, 1f, 0.5f);
                    break;
                case VfxStyle.Doom:
                    if (age % 5 == 0) Fx.Vortex(map, feet, new Color(0.08f, 0.06f, 0.07f, 0.95f), 1f, 1.6f, PMat.Crow, 1.1f, 0.34f, 6f);
                    if (age % 8 == 0) Fx.Rise(map, feet, c1, 1f, PMat.Eye, 1f, 0.8f, 1f, 0.24f, PFlags.Pulse);
                    if (age % 20 == 0) Sprite(map, PMat.Get(PMat.Seal), feet, 0.5f, 2.4f + progress, 24, A(c1, 0.55f), spin: 30f, layer: AltitudeLayer.MoteLow);
                    break;
                case VfxStyle.Desire:
                    if (age % 4 == 0) Fx.Vortex(map, feet, W(c2, 0.2f), 1f, 1.4f, PMat.Petal, 1f, 0.2f, 7f);
                    if (age % 7 == 0) Fx.Rise(map, hand, c1, 1f, PMat.Kiss, 0.3f, 0.9f, 0.9f, 0.22f, PFlags.Wobble);
                    if (age % 9 == 0) Fx.Mist(map, feet, A(c1, 0.18f), 1f, 0.9f, 1.8f, 1f, true);
                    break;
                case VfxStyle.Liminal:
                    if (age % 3 == 0) Fx.Burst(map, feet + VRand.InsideCircle(1f), c1, 1f, PMat.Glitch, 0.2f, 0.3f, 0.2f, PFlags.Flicker, 0f);
                    if (age % 12 == 0) Fx.Rise(map, feet, A(c1, 0.8f), 1f, PMat.Tube, 0.8f, 0.4f, 0.6f, 0.4f, PFlags.Flicker, 0f);
                    if (age % 25 == 0) ArcaneLightFlash.Spawn(map, feet, c1, 2.5f + progress * 2f, 6);
                    break;
            }
        }

        /// <summary>The moment a spell is released: a flash at the hand, a kick of sparks toward the target, and for ultimates a cinematic beat.</summary>
        public static void Release(SpellContext ctx, Vector3 target)
        {
            var caster = ctx?.caster;
            var map = ctx?.map;
            if (caster == null || map == null || !caster.Spawned) return;
            var s = Resolve(ctx);
            Vector3 d = (target - caster.DrawPos).Yto0();
            Vector3 dir = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
            Vector3 hand = caster.DrawPos + dir * 0.45f + new Vector3(0f, 0f, 0.15f);
            Color c = ctx.Color, c2 = ctx.Color2;
            ArcaneFx.Flash(map, hand, W(c, 0.3f), 1.6f);
            Sprite(map, ArcaneMats.Ring, hand, 0.2f, 1.4f, 10, A(c2, 0.8f));
            if (d.sqrMagnitude > 0.01f) Fx.Sparks(map, hand, c2, 6f, 4.5f, 0.1f, 0.3f, dir.AngleFlat(), 45f, 0f);
            bool ult = ctx.ext?.ultimate ?? false;
            if (!ult) return;
            var mgr = ArcaneVfxManager.For(map);
            Shockwave(map, caster.DrawPos, 3.5f, c, A(Dust, 0.5f));
            mgr.Add(new VfxPillar(caster.DrawPos, 50, c, c2, 1.6f));
            Light(map, caster.DrawPos, c, 9f, 40);
            ArcaneFx.Shake(map, 1.2f);
            if (map == Find.CurrentMap)
            {
                mgr.screen.AddFlash(W(c, 0.6f), 0.35f, 22f, false, mgr.Now);
                mgr.screen.AddFlash(c * 0.4f, 0.8f, 90f, true, mgr.Now);
                mgr.screen.ShowBanner(ctx.def.LabelCap.ToString().ToUpperInvariant(), caster.LabelShortCap + " · " + (ctx.arcane?.DisplayClass?.LabelCap.ToString() ?? ""), c, mgr.Now);
            }
        }

        /// <summary>A breath or spray: the school's matter rushing out across the cone.</summary>
        public static void Cone(SpellContext ctx, Vector3 origin, float angle, float coneAngle, float range)
        {
            var map = ctx?.map;
            if (map == null || map != Find.CurrentMap) return;
            var s = Resolve(ctx);
            Color c = ctx.Color, c2 = ctx.Color2;
            int n = Mathf.Clamp(Mathf.RoundToInt(range * coneAngle / 18f), 6, 40);
            for (int i = 0; i < n; i++)
            {
                float a = angle + VRand.Range(-coneAngle * 0.5f, coneAngle * 0.5f);
                Vector3 dir = VRand.Dir(a);
                float speed = range * VRand.Range(1.6f, 2.6f);
                var p = new ArcaneParticle
                {
                    pos = origin + dir * 0.4f,
                    vel = dir * speed,
                    life = VRand.Range(0.35f, 0.6f),
                    drag = 2.2f,
                    rot = VRand.Range(0f, 360f),
                    spin = VRand.Range(-200f, 200f),
                };
                switch (s)
                {
                    case VfxStyle.Fire:
                        p.mat = PMat.Flame; p.size0 = 0.4f; p.size1 = 1.3f; p.c0 = W(c2, 0.4f); p.c1 = A(c, 0f); p.flags = PFlags.Flicker | PFlags.AlignVel; p.turb = 0.6f;
                        break;
                    case VfxStyle.Frost:
                        p.mat = PMat.Glow; p.size0 = 0.4f; p.size1 = 1.6f; p.c0 = A(W(c, 0.6f), 0.7f); p.c1 = A(c, 0f);
                        if (i % 3 == 0) Fx.Burst(map, origin + dir * VRand.Range(1f, range), W(c2, 0.4f), 1f, PMat.Shard, 1f, 0.6f, 0.22f, PFlags.Glint);
                        break;
                    case VfxStyle.Storm:
                        p.mat = PMat.Streak; p.size0 = 0.14f; p.size1 = 0.06f; p.c0 = W(c, 0.6f); p.c1 = A(c, 0f); p.flags = PFlags.Stretch; p.stretch = 0.06f;
                        break;
                    case VfxStyle.Shadow:
                    case VfxStyle.Necro:
                    case VfxStyle.Plague:
                        p.mat = PMat.SmokeDark; p.size0 = 0.5f; p.size1 = 1.6f; p.c0 = A(c * 0.35f, 0.6f); p.c1 = A(c * 0.2f, 0f); p.flags = PFlags.Lit; p.turb = 0.3f;
                        break;
                    case VfxStyle.Song:
                        p.mat = i % 2 == 0 ? PMat.Note : PMat.Ring; p.size0 = 0.3f; p.size1 = 0.9f; p.c0 = c; p.c1 = A(c2, 0f); p.flags = PFlags.Wobble;
                        break;
                    case VfxStyle.Nature:
                        p.mat = PMat.Leaf; p.size0 = 0.25f; p.size1 = 0.3f; p.c0 = c; p.c1 = A(c, 0f); p.flags = PFlags.Wobble | PFlags.Lit; p.life *= 1.6f;
                        break;
                    case VfxStyle.Blood:
                        p.mat = PMat.BloodAlpha; p.size0 = 0.18f; p.size1 = 0.12f; p.c0 = A(c * 0.8f, 0.95f); p.c1 = A(c * 0.6f, 0f); p.flags = PFlags.Lit | PFlags.AlignVel;
                        break;
                    case VfxStyle.Wind:
                        p.mat = PMat.Gust; p.size0 = 0.4f; p.size1 = 1.2f; p.c0 = A(W(c2, 0.2f), 0.8f); p.c1 = A(c, 0f); p.flags = PFlags.Stretch | PFlags.AlignVel; p.stretch = 0.05f;
                        if (i % 4 == 0) Fx.Burst(map, origin + dir * VRand.Range(1f, range), new Color(0.8f, 0.78f, 0.7f, 0.4f), 1f, PMat.Dust, 1.5f, 0.9f, 0.5f, PFlags.Lit);
                        break;
                    case VfxStyle.Doom:
                        p.mat = i % 3 == 0 ? PMat.FeatherDark : PMat.SmokeDark; p.size0 = 0.3f; p.size1 = 1.2f; p.c0 = i % 3 == 0 ? new Color(0.08f, 0.06f, 0.07f, 0.95f) : A(c * 0.35f, 0.6f);
                        p.c1 = A(c * 0.2f, 0f); p.flags = PFlags.Lit | PFlags.Wobble; p.turb = 0.3f;
                        break;
                    case VfxStyle.Desire:
                        p.mat = i % 2 == 0 ? PMat.Heart : PMat.Petal; p.size0 = 0.22f; p.size1 = 0.4f; p.c0 = i % 2 == 0 ? c : W(c2, 0.3f); p.c1 = A(c2, 0f); p.flags = PFlags.Wobble;
                        break;
                    case VfxStyle.Liminal:
                        p.mat = PMat.Glitch; p.size0 = 0.25f; p.size1 = 0.5f; p.c0 = c; p.c1 = A(c2, 0f); p.flags = PFlags.Flicker;
                        break;
                    default:
                        p.mat = PMat.Glow; p.size0 = 0.3f; p.size1 = 1.1f; p.c0 = W(c, 0.3f); p.c1 = A(c2, 0f);
                        break;
                }
                Fx.One(map, p);
            }
            if (s == VfxStyle.Fire) Fx.Smoke(map, origin + VRand.Dir(angle) * range * 0.6f, Smoke, 3f, range * 0.25f, 1.1f, 2.4f, 0.6f, true, 1f);
            if (s == VfxStyle.Frost) Fx.Mist(map, origin + VRand.Dir(angle) * range * 0.5f, A(W(c, 0.6f), 0.35f), 4f, range * 0.35f, 2.6f, 1.4f);
            Light(map, origin + VRand.Dir(angle) * range * 0.4f, c, Mathf.Min(10f, 2f + range * 0.6f), 16);
        }

        // ------------------------------------------------------------------------------------------------
        // Impacts
        // ------------------------------------------------------------------------------------------------

        public static void Impact(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx?.map;
            if (map == null || map != Find.CurrentMap) return;
            var s = Resolve(ctx);
            Vector3 pos = info.pos;
            float r = Mathf.Max(0f, info.radius);
            int tier = ctx.ext?.tier ?? 1;
            float m = Mathf.Clamp(0.6f + r * 0.45f + (tier - 1) * 0.15f + ((ctx.ext?.ultimate ?? false) ? 0.8f : 0f), 0.5f, 3.5f) * Mathf.Max(0.5f, info.scale);
            Color c = ctx.Color, c2 = ctx.Color2;
            if (!Harmful(ctx))
            {
                Benign(s, map, pos, r, m, c, c2);
                return;
            }
            var mgr = ArcaneVfxManager.For(map);
            switch (s)
            {
                case VfxStyle.Fire:
                    ArcaneFx.Flash(map, pos, W(c2, 0.6f), 1.2f + r * 1.5f);
                    Sprite(map, ArcaneMats.Glow, pos, 0.6f + r * 1.4f, 1.6f + r * 2.6f, 16, Color.Lerp(c2, c, 0.35f), true);
                    Fx.Flames(map, pos, c, c2, 6f * m, 0.25f + r * 0.5f, 0.5f + r * 0.12f);
                    Fx.Sparks(map, pos, c2, 10f * m, 5f + r);
                    Fx.Embers(map, pos, c, 8f * m, 0.3f + r * 0.7f, 1.2f, 1.9f);
                    mgr.Schedule(4, () => Fx.Smoke(map, pos, Smoke, 3f * m, 0.2f + r * 0.5f, 0.8f + r * 0.35f, 3.2f, 0.7f, true, 0.6f));
                    if (r > 0.5f) Shockwave(map, pos, r, A(c, 0.9f), A(Dust, 0.45f));
                    if (r >= 1.5f)
                    {
                        Fx.Debris(map, pos, Rock, r * 3f, 2.5f + r);
                        Decal(map, pos, DecalKind.Crater, r * 1.3f + 0.5f, new Color(1f, 1f, 1f, 0.8f), 45f);
                    }
                    Decal(map, pos, DecalKind.Scorch, 0.9f + r * 2.1f, new Color(1f, 1f, 1f, 0.85f), 40f);
                    Light(map, pos, c, 3f + r * 1.8f, 14 + Mathf.RoundToInt(r * 5f));
                    if (r >= 3f) mgr.screen.AddFlash(W(c, 0.5f), 0.12f, 14f, false, mgr.Now);
                    break;
                case VfxStyle.Frost:
                    ArcaneFx.Flash(map, pos, W(c, 0.6f), 1.2f + r * 1.3f);
                    Fx.Shards(map, pos, W(c2, 0.2f), 8f * m, 3.5f + r);
                    Fx.Burst(map, pos, W(c, 0.5f), 6f * m, PMat.Snow, 1.6f, 1.4f, 0.24f, PFlags.Wobble | PFlags.Glint, 90f, 1.2f, 0.4f);
                    Fx.Mist(map, pos, A(W(c, 0.6f), 0.35f), 3f * m, r + 0.3f, 3f, 1.2f + r * 0.2f);
                    if (r > 0.5f) Shockwave(map, pos, r, A(W(c, 0.4f), 0.9f), A(new Color(0.85f, 0.92f, 1f), 0.3f));
                    if (r >= 1.5f) Fx.Debris(map, pos, new Color(0.8f, 0.9f, 1f, 0.9f), r * 3f, 2.5f + r, 0.22f, PMat.IceChunk);
                    Decal(map, pos, DecalKind.Frost, 1.2f + r * 2.2f, W(c, 0.3f), 16f);
                    Light(map, pos, W(c, 0.4f), 2.5f + r * 1.4f, 14);
                    break;
                case VfxStyle.Storm:
                    ArcaneFx.Flash(map, pos, Color.white, 1.6f + r * 1.6f);
                    Sprite(map, ArcaneMats.Flare, pos, 0.5f, 2f + r * 1.5f, 10, W(c, 0.6f), true, 20f);
                    Fx.Sparks(map, pos, W(c2, 0.3f), 16f * m, 7f + r, 0.1f, 0.3f);
                    Fx.Burst(map, pos, W(c, 0.4f), 2f * m, PMat.Bolt, 2f, 0.4f, 0.3f);
                    mgr.Add(new VfxArcs { pos = pos, radius = 0.6f + r * 0.5f, color = c, duration = 22 + Mathf.RoundToInt(r * 6f), intensity = 1.2f });
                    Fx.Smoke(map, pos, new Color(0.35f, 0.35f, 0.38f, 0.5f), 2f * m, 0.2f + r * 0.4f, 0.6f, 1.8f, 0.6f);
                    Decal(map, pos, DecalKind.Scorch, 0.7f + r * 1.4f, new Color(1f, 1f, 1f, 0.7f), 30f);
                    if (r >= 1.5f) Decal(map, pos, DecalKind.Crack, r * 2f, new Color(1f, 1f, 1f, 0.6f), 25f);
                    Light(map, pos, W(c, 0.6f), 5f + r * 2f, 8);
                    if (r >= 2.5f) mgr.screen.AddFlash(W(c, 0.7f), 0.12f, 10f, false, mgr.Now);
                    break;
                case VfxStyle.Necro:
                    ArcaneFx.Flash(map, pos, c, 1.1f + r * 1.2f);
                    Fx.Rise(map, pos, A(c, 0.85f), 5f * m, PMat.Wisp, 0.2f + r * 0.6f, 1.4f, 1.2f, 0.4f, PFlags.None, 220f);
                    Fx.Rise(map, pos, A(W(c, 0.3f), 0.9f), 0.6f + r * 0.4f, PMat.Skull, 0.2f + r * 0.4f, 0.9f, 1f, 0.45f, PFlags.Pulse, 20f);
                    Fx.Smoke(map, pos, A(c * 0.3f, 0.6f), 3f * m, 0.2f + r * 0.5f, 0.8f + r * 0.3f, 2.4f, 0.4f, true);
                    if (r > 0.5f) Shockwave(map, pos, r, A(c, 0.8f), A(c * 0.4f, 0.35f));
                    Decal(map, pos, DecalKind.Rot, 1f + r * 2f, new Color(1f, 1f, 1f, 0.8f), 30f);
                    Light(map, pos, c, 2.5f + r * 1.2f, 16);
                    break;
                case VfxStyle.Plague:
                    ArcaneFx.Flash(map, pos, c, 1f + r);
                    Fx.Rise(map, pos, A(c, 0.8f), 6f * m, PMat.Bubble, 0.3f + r * 0.6f, 0.8f, 1.6f, 0.22f);
                    Fx.Mist(map, pos, A(c * 0.6f, 0.4f), 4f * m, r + 0.4f, 3.5f, 1.4f + r * 0.2f);
                    Fx.Smoke(map, pos, A(c * 0.35f, 0.5f), 2f * m, 0.2f + r * 0.4f, 0.8f, 2.4f, 0.3f, true);
                    Decal(map, pos, DecalKind.Rot, 1f + r * 2.2f, new Color(1f, 1f, 1f, 0.85f), 35f);
                    Light(map, pos, c, 2f + r, 14);
                    break;
                case VfxStyle.Holy:
                    ArcaneFx.Flash(map, pos, W(c, 0.5f), 1.5f + r * 1.6f);
                    Sprite(map, ArcaneMats.Flare, pos, 0.5f, 2.2f + r * 1.4f, 22, W(c2, 0.4f), true, 6f);
                    Fx.Rise(map, pos, W(c, 0.4f), 6f * m, PMat.Star, 0.2f + r * 0.6f, 1.6f, 1.1f, 0.2f, PFlags.Glint);
                    Fx.Burst(map, pos, W(c2, 0.5f), 3f * m, PMat.Feather, 1.2f, 2f, 0.3f, PFlags.Wobble, 60f, 0.6f, 0.8f);
                    if (r > 0.5f)
                    {
                        Shockwave(map, pos, r, A(W(c, 0.3f), 0.9f), new Color(0, 0, 0, 0));
                        Decal(map, pos, DecalKind.Holy, 1.5f + r * 2f, A(c, 0.8f), 6f);
                        mgr.Add(new VfxGodRays(pos, r, A(W(c, 0.4f), 0.75f), 3 + Mathf.RoundToInt(r), 70));
                    }
                    Light(map, pos, W(c, 0.3f), 4f + r * 1.5f, 20);
                    break;
                case VfxStyle.Time:
                    ArcaneFx.Flash(map, pos, W(c, 0.4f), 1.2f + r * 1.2f);
                    Sprite(map, PMat.Get(PMat.Clock), pos, 2f + r * 2f, 0.4f, 24, A(c, 0.85f), false, -16f);
                    Fx.Vortex(map, pos, c, 6f * m, 1.2f + r, PMat.Clock, 0.7f, 0.25f, 9f);
                    Fx.Burst(map, pos, Dust, 4f * m, PMat.Dust, 1.2f, 1.6f, 0.4f, PFlags.Lit, 40f, 1.5f, 0.6f);
                    Decal(map, pos, DecalKind.Glyph, 1f + r * 2f, A(c, 0.8f), 4f);
                    Light(map, pos, c, 2.5f + r * 1.2f, 14);
                    break;
                case VfxStyle.Void:
                    Fx.Implode(map, pos, c, 12f * m, 1.5f + r, 0.45f, -1, 0.14f, 8f);
                    Sprite(map, ArcaneMats.BlackHole, pos, 0.4f, 1f + r * 1.3f, 26, new Color(1f, 1f, 1f, 0.95f), true, 0f, AltitudeLayer.MoteOverhead);
                    mgr.Schedule(8, () =>
                    {
                        Sprite(map, ArcaneMats.Shock, pos, 0.3f, 2f + r * 2.4f, 16, c);
                        Fx.Sparks(map, pos, W(c, 0.3f), 6f * m, 4f + r);
                    });
                    Decal(map, pos, DecalKind.Void, 1f + r * 2f, new Color(1f, 1f, 1f, 0.85f), 14f);
                    Light(map, pos, c, 2f + r, 18);
                    if (r >= 3f) mgr.screen.AddFlash(new Color(0.1f, 0f, 0.15f), 0.5f, 30f, true, mgr.Now);
                    break;
                case VfxStyle.Nature:
                    ArcaneFx.Flash(map, pos, c, 1f + r * 1.2f);
                    Fx.Burst(map, pos, c, 7f * m, PMat.Leaf, 2.5f, 2f, 0.28f, PFlags.Wobble | PFlags.Lit, 200f, 1.2f, 0.6f);
                    Fx.Burst(map, pos, W(c2, 0.2f), 3f * m, PMat.Petal, 1.6f, 1.6f, 0.22f, PFlags.Wobble, 200f, 0.8f, 0.5f);
                    Fx.Rise(map, pos, new Color(0.95f, 1f, 0.6f, 0.8f), 3f * m, PMat.Mote, 0.3f + r * 0.6f, 0.6f, 1.6f, 0.12f);
                    if (r > 0.5f) Shockwave(map, pos, r, A(c, 0.7f), A(Dust, 0.4f));
                    Decal(map, pos, DecalKind.Leaves, 1f + r * 2f, new Color(1f, 1f, 1f, 0.9f), 25f);
                    Light(map, pos, c, 2f + r, 12);
                    break;
                case VfxStyle.Rune:
                    ArcaneFx.Flash(map, pos, W(c, 0.4f), 1.3f + r * 1.3f);
                    Fx.Burst(map, pos, W(c, 0.2f), 6f * m, PMat.Rune, 2.6f, 0.9f, 0.26f, PFlags.None, 360f);
                    Fx.Burst(map, pos, c2, 2f * m, PMat.Gear, 2f, 1.2f, 0.25f, PFlags.None, 300f, 3f);
                    Fx.Sparks(map, pos, c, 8f * m, 5f + r);
                    if (r > 0.5f) Shockwave(map, pos, r, A(c, 0.9f), A(Dust, 0.4f));
                    if (r >= 1.5f) Fx.Debris(map, pos, Rock, r * 2.5f, 2f + r);
                    Decal(map, pos, DecalKind.RuneBurn, 1f + r * 2f, A(c, 0.8f), 3.5f);
                    Decal(map, pos, DecalKind.Scorch, 0.6f + r * 1.5f, new Color(1f, 1f, 1f, 0.6f), 25f);
                    Light(map, pos, c, 3f + r * 1.4f, 12);
                    break;
                case VfxStyle.Shadow:
                    ArcaneFx.Flash(map, pos, c * 0.7f, 1f + r);
                    Fx.Smoke(map, pos, A(c * 0.22f, 0.7f), 6f * m, 0.3f + r * 0.6f, 1f + r * 0.3f, 2.2f, 0.3f, true, 1f);
                    Fx.Implode(map, pos, A(c, 0.7f), 5f * m, 1.2f + r, 0.4f, PMat.Wisp, 0.3f, 6f);
                    Fx.Sparks(map, pos, c, 4f * m, 3f + r);
                    Decal(map, pos, DecalKind.Void, 1f + r * 2f, new Color(0.9f, 0.8f, 1f, 0.75f), 14f);
                    Light(map, pos, c * 0.7f, 1.5f + r, 12);
                    break;
                case VfxStyle.Blood:
                    ArcaneFx.Flash(map, pos, c * 0.8f, 1f + r);
                    Fx.Burst(map, pos, A(c * 0.8f, 0.95f), 12f * m, PMat.BloodAlpha, 3.5f, 0.9f, 0.15f, PFlags.Lit | PFlags.AlignVel, 0f, 9f, 0.3f);
                    Fx.Drips(map, pos, A(c * 0.8f, 0.9f), 4f * m);
                    Fx.Mist(map, pos, A(c * 0.6f, 0.35f), 2f * m, r + 0.3f, 2.4f, 1.2f);
                    Decal(map, pos, DecalKind.Blood, 1f + r * 1.8f, new Color(1f, 1f, 1f, 0.9f), 35f);
                    Light(map, pos, c, 1.5f + r, 10);
                    break;
                case VfxStyle.Song:
                    for (int i = 0; i < 3; i++)
                    {
                        int k = i;
                        mgr.Schedule(i * 5, () => Sprite(map, ArcaneMats.Ring, pos, 0.3f, (1.4f + r * 2f) * (1f + k * 0.3f), 20, A(W(c, 0.3f), 0.8f - k * 0.2f)));
                    }
                    Fx.Burst(map, pos, c, 5f * m, PMat.Note, 1.8f, 1.2f, 0.32f, PFlags.Wobble, 90f, 0f, 0.4f);
                    Fx.Burst(map, pos, W(c2, 0.4f), 4f * m, PMat.Star, 2f, 0.8f, 0.2f, PFlags.Glint);
                    Light(map, pos, c, 2f + r, 12);
                    break;
                case VfxStyle.Wind:
                    ArcaneFx.Flash(map, pos, W(c2, 0.6f), 1f + r * 1.2f);
                    Sprite(map, ArcaneMats.Shock, pos, 0.3f, 1.8f + r * 2.4f, 14, A(W(c2, 0.4f), 0.8f));
                    Fx.Burst(map, pos, A(W(c2, 0.2f), 0.85f), 10f * m, PMat.Gust, 4.5f + r, 0.4f, 0.55f, PFlags.Stretch | PFlags.AlignVel, 0f, 0f, 0.15f, 0.2f + r * 0.3f);
                    Fx.RingBurst(map, pos, new Color(0.8f, 0.78f, 0.7f, 0.45f), 8f + r * 6f, PMat.Dust, Mathf.Max(0.3f, r * 0.4f), 3f + r, 1.2f, 0.7f + r * 0.12f);
                    Fx.Burst(map, pos, new Color(0.55f, 0.7f, 0.35f, 1f), 4f * m, PMat.Leaf, 3f, 1.6f, 0.2f, PFlags.Wobble | PFlags.Lit, 400f, 0.8f, 0.6f);
                    if (r >= 1f) Fx.Debris(map, pos, new Color(0.6f, 0.55f, 0.48f, 1f), r * 2f, 2.5f + r, 0.14f);
                    if (r >= 2f) Decal(map, pos, DecalKind.Crack, r * 1.6f, new Color(1f, 1f, 1f, 0.45f), 20f);
                    Light(map, pos, W(c, 0.5f), 2f + r, 10);
                    break;
                case VfxStyle.Doom:
                    ArcaneFx.Flash(map, pos, c * 0.8f, 1.2f + r * 1.3f);
                    Sprite(map, PMat.Get(PMat.Seal), pos, 0.6f + r * 1.2f, 1.6f + r * 2.2f, 26, A(W(c, 0.15f), 0.9f), true, 40f, AltitudeLayer.MoteLow);
                    Fx.Embers(map, pos, c, 8f * m, 0.3f + r * 0.6f, 1.2f, 1.8f);
                    Fx.Burst(map, pos, new Color(0.08f, 0.06f, 0.07f, 0.95f), 6f * m, PMat.FeatherDark, 2.6f, 1.8f, 0.3f, PFlags.Wobble | PFlags.Lit, 160f, 0.8f, 0.5f);
                    Fx.Smoke(map, pos, A(c * 0.2f, 0.7f), 4f * m, 0.3f + r * 0.5f, 0.9f + r * 0.3f, 2.6f, 0.4f, true, 0.6f);
                    if (r > 0.5f) Shockwave(map, pos, r, A(c * 0.7f, 0.9f), A(c * 0.25f, 0.4f));
                    Decal(map, pos, DecalKind.Glyph, 1f + r * 2f, A(c, 0.85f), 6f);
                    Decal(map, pos, DecalKind.Scorch, 0.8f + r * 1.6f, new Color(0.8f, 0.6f, 0.6f, 0.7f), 30f);
                    Light(map, pos, c, 2.5f + r * 1.3f, 16);
                    if (r >= 3f) mgr.screen.AddFlash(new Color(0.25f, 0f, 0.02f), 0.45f, 26f, true, mgr.Now);
                    break;
                case VfxStyle.Desire:
                    ArcaneFx.Flash(map, pos, W(c, 0.4f), 1.1f + r * 1.2f);
                    Sprite(map, PMat.Get(PMat.Heart), pos, 0.4f + r * 0.6f, 1.4f + r * 1.6f, 20, A(W(c, 0.2f), 0.8f), true);
                    Fx.Burst(map, pos, c, 6f * m, PMat.Heart, 2.4f, 1f, 0.24f, PFlags.Wobble, 90f, 0.6f, 0.4f);
                    Fx.Burst(map, pos, W(c2, 0.3f), 5f * m, PMat.Petal, 2f, 1.8f, 0.2f, PFlags.Wobble, 220f, 0.6f, 0.6f);
                    Fx.Burst(map, pos, W(c2, 0.5f), 4f * m, PMat.Star, 2.4f, 0.6f, 0.16f, PFlags.Glint);
                    Fx.Rise(map, pos, c, 2f * m, PMat.Kiss, 0.3f + r * 0.4f, 0.9f, 1.1f, 0.26f, PFlags.Wobble);
                    Fx.Mist(map, pos, A(c, 0.22f), 2f * m, r + 0.3f, 2.4f, 1.2f, true);
                    if (r > 0.5f) Sprite(map, ArcaneMats.Ring, pos, 0.3f, 1.6f + r * 2.2f, 22, A(c, 0.6f), layer: AltitudeLayer.MoteLow);
                    Decal(map, pos, DecalKind.Leaves, 0.8f + r * 1.6f, new Color(1f, 0.55f, 0.75f, 0.75f), 20f);
                    Light(map, pos, c, 2f + r, 14);
                    break;
                case VfxStyle.Liminal:
                    BanishUtility.GlitchBurst(map, pos, c, Mathf.Clamp(0.5f + r * 0.3f, 0.5f, 2f));
                    ArcaneFx.Flash(map, pos, W(c, 0.5f), 1f + r);
                    Sprite(map, ArcaneMats.Ring, pos, 0.2f, 1.4f + r * 2f, 12, A(c, 0.7f), layer: AltitudeLayer.MoteLow);
                    if (r > 0.5f) mgr.Schedule(6, () => Fx.Burst(map, pos, A(c, 0.7f), 6f * m, PMat.Tube, 0.6f, 0.4f, 0.4f, PFlags.Flicker, 0f, 0f, 0.6f, r));
                    Decal(map, pos, DecalKind.Rot, 1f + r * 1.8f, new Color(0.95f, 0.88f, 0.45f, 0.6f), 30f);
                    for (int i = 0; i < 3; i++) mgr.Schedule(i * 4 + 2, () => Light(map, pos, c, 2f + r * 1.2f, 3));
                    break;
                default:
                    ArcaneFx.Flash(map, pos, W(c, 0.4f), 1.2f + r * 1.3f);
                    Fx.Sparks(map, pos, c2, 8f * m, 4.5f + r);
                    Fx.Burst(map, pos, c, 3f * m, PMat.Rune, 1.8f, 0.7f, 0.22f);
                    if (r > 0.5f) Shockwave(map, pos, r, A(c, 0.9f), A(Dust, 0.35f));
                    Light(map, pos, c, 3f + r, 12);
                    break;
            }
        }

        /// <summary>Gentle signatures for heals, buffs and utility spells.</summary>
        private static void Benign(VfxStyle s, Map map, Vector3 pos, float r, float m, Color c, Color c2)
        {
            ArcaneFx.Flash(map, pos, W(c, 0.5f), 1f + r * 0.8f);
            Sprite(map, ArcaneMats.Ring, pos, 0.3f, 1.6f + r * 2f, 22, A(c, 0.7f), layer: AltitudeLayer.MoteLow);
            int mat;
            PFlags flags = PFlags.None;
            switch (s)
            {
                case VfxStyle.Holy:
                    mat = PMat.Star;
                    flags = PFlags.Glint;
                    Fx.Burst(map, pos + new Vector3(0f, 0f, 1f), W(c2, 0.5f), 2f * m, PMat.Feather, 0.4f, 2f, 0.3f, PFlags.Wobble, 60f, 0.5f, 0.6f);
                    if (r > 0.5f) ArcaneVfxManager.For(map)?.Add(new VfxGodRays(pos, r, A(W(c, 0.5f), 0.6f), 2 + Mathf.RoundToInt(r), 60));
                    break;
                case VfxStyle.Nature: mat = PMat.Leaf; flags = PFlags.Wobble | PFlags.Lit; break;
                case VfxStyle.Fire: mat = PMat.Ember; flags = PFlags.Flicker; break;
                case VfxStyle.Frost: mat = PMat.Snow; flags = PFlags.Glint; break;
                case VfxStyle.Time: mat = PMat.Clock; break;
                case VfxStyle.Necro:
                case VfxStyle.Plague: mat = PMat.Wisp; break;
                case VfxStyle.Song: mat = PMat.Note; flags = PFlags.Wobble; break;
                case VfxStyle.Rune: mat = PMat.Rune; break;
                case VfxStyle.Blood: mat = PMat.Heart; break;
                case VfxStyle.Storm: mat = PMat.Spark; break;
                case VfxStyle.Wind:
                    mat = PMat.Leaf;
                    flags = PFlags.Wobble | PFlags.Lit;
                    Fx.Vortex(map, pos, A(W(c2, 0.3f), 0.7f), 5f * m, 0.8f + r * 0.6f, PMat.Gust, 0.7f, 0.4f, 10f);
                    break;
                case VfxStyle.Doom:
                    mat = PMat.Eye;
                    flags = PFlags.Pulse;
                    Fx.Burst(map, pos, new Color(0.08f, 0.06f, 0.07f, 0.95f), 2f * m, PMat.FeatherDark, 1.2f, 1.8f, 0.26f, PFlags.Wobble | PFlags.Lit, 120f, 0.5f, 0.6f);
                    break;
                case VfxStyle.Desire:
                    mat = PMat.Heart;
                    flags = PFlags.Wobble;
                    Fx.Rise(map, pos, c, 2f * m, PMat.Kiss, 0.3f + r * 0.4f, 0.8f, 1.1f, 0.24f, PFlags.Wobble);
                    break;
                case VfxStyle.Liminal:
                    mat = PMat.Glitch;
                    flags = PFlags.Flicker;
                    Fx.Rise(map, pos, A(c, 0.6f), 1f + r * 0.5f, PMat.Tube, 0.3f + r * 0.5f, 0.1f, 1f, 0.4f, PFlags.Flicker, 0f);
                    break;
                default: mat = PMat.Mote; break;
            }
            Fx.Rise(map, pos, W(c, 0.3f), 6f * m, mat, 0.3f + r * 0.6f, 1.3f, 1.2f, 0.24f, flags);
            Fx.Rise(map, pos, W(c2, 0.5f), 4f * m, PMat.Mote, 0.3f + r * 0.6f, 1.8f, 0.9f, 0.14f);
            Light(map, pos, c, 2.5f + r, 16);
        }

        // ------------------------------------------------------------------------------------------------
        // Deaths
        // ------------------------------------------------------------------------------------------------

        public static void Death(SpellContext ctx, Pawn victim, Vector3 pos)
        {
            var map = ctx?.map;
            if (map == null || map != Find.CurrentMap) return;
            var s = Resolve(ctx);
            Color c = ctx.Color, c2 = ctx.Color2;
            float size = Mathf.Clamp(victim?.BodySize ?? 1f, 0.3f, 4f);
            var mgr = ArcaneVfxManager.For(map);
            switch (s)
            {
                case VfxStyle.Fire:
                    Fx.Embers(map, pos, c, 14f * size, 0.4f * size, 1.4f, 2.2f);
                    Fx.Flames(map, pos, c, c2, 6f * size, 0.3f * size, 0.6f);
                    Fx.Burst(map, pos, new Color(0.2f, 0.19f, 0.18f, 0.9f), 8f * size, PMat.Ash, 1.2f, 2.2f, 0.18f, PFlags.Lit | PFlags.Wobble, 90f, -0.3f, 0.4f);
                    Fx.Smoke(map, pos, Smoke, 4f * size, 0.3f * size, 1f * size, 3f, 0.6f, true);
                    Decal(map, pos, DecalKind.Ash, 1.1f * size, new Color(1f, 1f, 1f, 0.8f), 40f);
                    break;
                case VfxStyle.Frost:
                    Fx.Shards(map, pos, W(c, 0.3f), 14f * size, 4.5f, 0.32f);
                    Fx.Debris(map, pos, new Color(0.8f, 0.9f, 1f, 0.9f), 6f * size, 3f, 0.22f, PMat.IceChunk);
                    Fx.Mist(map, pos, A(W(c, 0.6f), 0.4f), 3f, 0.6f, 3f, 1.3f);
                    ArcaneFx.Text(victim, "AA_Shattered".Translate(), W(c, 0.5f));
                    break;
                case VfxStyle.Storm:
                    Fx.Sparks(map, pos, W(c, 0.4f), 16f * size, 6f);
                    Fx.Smoke(map, pos, new Color(0.3f, 0.3f, 0.32f, 0.6f), 3f * size, 0.3f, 0.8f, 2.4f, 0.8f, true);
                    mgr.Add(new VfxArcs { pos = pos, radius = 0.8f * size, color = c, duration = 30 });
                    break;
                case VfxStyle.Necro:
                case VfxStyle.Plague:
                    var caster = ctx.caster;
                    if (caster != null && caster.Spawned && caster.Map == map && (caster.DrawPos - pos).MagnitudeHorizontal() < 45f)
                        mgr.Add(new VfxSoulWisp(pos, caster, c, c2));
                    Fx.Rise(map, pos, A(c, 0.8f), 6f * size, PMat.Wisp, 0.4f, 1.6f, 1.4f, 0.4f, PFlags.None, 240f);
                    Fx.Rise(map, pos, A(W(c, 0.4f), 0.9f), 1f, PMat.Skull, 0.1f, 1.2f, 1.4f, 0.5f, PFlags.Pulse, 10f);
                    break;
                case VfxStyle.Holy:
                    Fx.Rise(map, pos, W(c, 0.4f), 12f * size, PMat.Star, 0.4f * size, 2.4f, 1.6f, 0.2f, PFlags.Glint);
                    Fx.Burst(map, pos + new Vector3(0f, 0f, 0.6f), W(c2, 0.5f), 5f, PMat.Feather, 0.8f, 2.4f, 0.32f, PFlags.Wobble, 60f, 0.4f, 0.6f);
                    mgr.Add(new VfxPillar(pos, 40, c, c2, 0.9f));
                    break;
                case VfxStyle.Time:
                    Fx.Burst(map, pos, Dust, 14f * size, PMat.Dust, 1f, 2f, 0.4f, PFlags.Lit, 40f, 2f, 0.8f, 0.5f);
                    Fx.Vortex(map, pos, c, 8f, 1.6f, PMat.Clock, 0.9f, 0.25f, 9f);
                    break;
                case VfxStyle.Void:
                    Fx.Implode(map, pos, c, 18f * size, 2.2f, 0.5f, -1, 0.14f, 10f);
                    Sprite(map, ArcaneMats.BlackHole, pos, 0.2f, 1.4f * size, 30, Color.white, true, 0f, AltitudeLayer.MoteOverhead);
                    break;
                case VfxStyle.Nature:
                    Fx.Burst(map, pos, c, 10f * size, PMat.Leaf, 2f, 2.2f, 0.28f, PFlags.Wobble | PFlags.Lit, 200f, 1f, 0.6f);
                    Fx.Burst(map, pos, W(c2, 0.2f), 6f * size, PMat.Petal, 1.5f, 2f, 0.24f, PFlags.Wobble, 200f, 0.6f, 0.6f);
                    break;
                case VfxStyle.Rune:
                    Fx.Burst(map, pos, W(c, 0.2f), 10f * size, PMat.Rune, 3f, 1f, 0.28f, PFlags.None, 360f);
                    Fx.Burst(map, pos, c2, 4f * size, PMat.Gear, 2.5f, 1.4f, 0.28f, PFlags.Shadow, 360f, 6f, 0.5f);
                    break;
                case VfxStyle.Shadow:
                    Fx.Smoke(map, pos, A(c * 0.2f, 0.8f), 7f * size, 0.4f * size, 1.1f, 2.6f, 0.9f, true, 0.6f);
                    Fx.Rise(map, pos, A(c, 0.7f), 6f * size, PMat.Wisp, 0.4f, 1.4f, 1.2f, 0.35f, PFlags.None, 200f);
                    break;
                case VfxStyle.Blood:
                    Fx.Burst(map, pos, A(c * 0.75f, 0.95f), 20f * size, PMat.BloodAlpha, 4f, 1f, 0.16f, PFlags.Lit | PFlags.AlignVel, 0f, 9f, 0.4f);
                    Decal(map, pos, DecalKind.Blood, 1.8f * size, new Color(1f, 1f, 1f, 0.95f), 40f);
                    break;
                case VfxStyle.Song:
                    Fx.Burst(map, pos, c, 10f * size, PMat.Note, 2.4f, 1.4f, 0.32f, PFlags.Wobble, 120f);
                    Sprite(map, ArcaneMats.Ring, pos, 0.3f, 3.5f, 24, A(c, 0.8f));
                    break;
                case VfxStyle.Wind:
                    Fx.Burst(map, pos, A(W(c2, 0.2f), 0.85f), 10f * size, PMat.Gust, 5f, 0.45f, 0.6f, PFlags.Stretch | PFlags.AlignVel, 0f, 0f, 0.2f, 0.3f);
                    Fx.Vortex(map, pos, new Color(0.8f, 0.78f, 0.7f, 0.5f), 8f * size, 1.4f * size, PMat.Dust, 1.1f, 0.5f, 14f);
                    Fx.Burst(map, pos, new Color(0.55f, 0.7f, 0.35f, 1f), 6f * size, PMat.Leaf, 3.5f, 2f, 0.22f, PFlags.Wobble | PFlags.Lit, 500f, 0.6f, 0.8f);
                    break;
                case VfxStyle.Doom:
                    var reaper = ctx.caster;
                    if (reaper != null && reaper.Spawned && reaper.Map == map && (reaper.DrawPos - pos).MagnitudeHorizontal() < 45f)
                        mgr.Add(new VfxSoulWisp(pos, reaper, c, c2));
                    Fx.Burst(map, pos, new Color(0.08f, 0.06f, 0.07f, 0.95f), 10f * size, PMat.FeatherDark, 2.8f, 2.4f, 0.32f, PFlags.Wobble | PFlags.Lit, 200f, 0.5f, 0.8f);
                    Fx.Vortex(map, pos, new Color(0.08f, 0.06f, 0.07f, 0.95f), 4f * size, 1.6f, PMat.Crow, 1.4f, 0.4f, 8f);
                    Fx.Rise(map, pos, W(c, 0.2f), 1f, PMat.Eye, 0.1f, 1.1f, 1.5f, 0.5f, PFlags.Pulse, 0f);
                    Fx.Embers(map, pos, c, 8f * size, 0.4f * size, 1.3f, 2f);
                    Decal(map, pos, DecalKind.Glyph, 1.4f * size, A(c, 0.85f), 8f);
                    break;
                case VfxStyle.Desire:
                    Fx.Burst(map, pos, c, 12f * size, PMat.Heart, 3f, 1.2f, 0.26f, PFlags.Wobble, 220f, 1.4f, 0.6f);
                    Sprite(map, PMat.Get(PMat.Heart), pos + new Vector3(0f, 0f, 0.5f), 1.2f, 0.2f, 22, W(c, 0.3f), true);
                    Fx.Rise(map, pos, A(W(c, 0.3f), 0.8f), 4f * size, PMat.Wisp, 0.4f, 1.4f, 1.3f, 0.36f, PFlags.None, 200f);
                    var lover = ctx.caster;
                    if (lover != null && lover.Spawned && lover.Map == map && (lover.DrawPos - pos).MagnitudeHorizontal() < 45f)
                        mgr.Add(new VfxSoulWisp(pos, lover, c, c2, null, PMat.Heart));
                    break;
                case VfxStyle.Liminal:
                    BanishUtility.GlitchBurst(map, pos, c, 1.2f * size);
                    Fx.Implode(map, pos, A(c, 0.9f), 14f * size, 1.6f, 0.45f, PMat.Glitch, 0.2f);
                    mgr.Schedule(10, () => Fx.Rise(map, pos, new Color(1f, 1f, 1f, 0.9f), 1f, PMat.Smile, 0.05f, 0.5f, 1.4f, 0.55f, PFlags.Flicker, 0f));
                    Decal(map, pos, DecalKind.Rot, 1.2f * size, new Color(0.95f, 0.88f, 0.45f, 0.7f), 30f);
                    break;
                default:
                    Fx.Burst(map, pos, c, 10f * size, PMat.Mote, 2.5f, 0.8f, 0.24f);
                    break;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Persistent fields
        // ------------------------------------------------------------------------------------------------

        public static void FieldTick(VfxStyle s, Map map, Vector3 center, float radius, Color c, Color c2, int age, bool harmful)
        {
            if (map != Find.CurrentMap) return;
            float n = Mathf.Clamp(radius * radius * 0.07f, 0.3f, 6f);
            switch (s)
            {
                case VfxStyle.Fire:
                    Fx.Flames(map, center, c, c2, n * 0.8f, radius * 0.9f, 0.5f);
                    Fx.Embers(map, center, c, n * 0.4f, radius, 1f, 1.6f);
                    if (age % 16 == 0) Fx.Smoke(map, center, Smoke, n * 0.4f, radius * 0.8f, 1.2f, 3f, 0.6f, true);
                    break;
                case VfxStyle.Frost:
                    if (age % 8 == 0) Fx.Mist(map, center, A(W(c, 0.6f), 0.28f), n * 0.5f, radius, 3.5f, 1.6f);
                    Fx.Burst(map, center + new Vector3(0f, 0f, 0f), W(c, 0.5f), n * 0.4f, PMat.Snow, 0.4f, 1.6f, 0.2f, PFlags.Wobble | PFlags.Glint, 90f, 0.6f, 1.6f, radius);
                    break;
                case VfxStyle.Storm:
                    if (age % 12 == 0 && VRand.Chance(0.7f))
                        ArcaneVfxManager.For(map)?.Add(new VfxArcs { pos = center + VRand.InsideCircle(radius * 0.8f), radius = 0.6f, color = c, duration = 10 });
                    Fx.Sparks(map, center + VRand.InsideCircle(radius), c, n * 0.2f, 2.5f, 0.08f, 0.25f);
                    break;
                case VfxStyle.Necro:
                    Fx.Rise(map, center, A(c, 0.7f), n * 0.35f, PMat.Wisp, radius, 0.8f, 1.4f, 0.32f, PFlags.None, 200f);
                    if (age % 12 == 0) Fx.Smoke(map, center, A(c * 0.3f, 0.45f), n * 0.3f, radius, 1.1f, 3f, 0.25f, true);
                    break;
                case VfxStyle.Plague:
                    Fx.Rise(map, center, A(c, 0.8f), n * 0.4f, PMat.Bubble, radius, 0.6f, 1.4f, 0.18f);
                    if (age % 10 == 0) Fx.Mist(map, center, A(c * 0.6f, 0.3f), n * 0.4f, radius, 3.5f, 1.6f);
                    break;
                case VfxStyle.Holy:
                    Fx.Rise(map, center, W(c, 0.4f), n * 0.45f, PMat.Star, radius, 1.4f, 1.3f, 0.16f, PFlags.Glint);
                    if (age % 150 == 0) ArcaneVfxManager.For(map)?.Add(new VfxGodRays(center, radius * 0.7f, A(W(c, 0.5f), 0.5f), 2, 90));
                    break;
                case VfxStyle.Time:
                    if (age % 8 == 0) Fx.Vortex(map, center, A(c, 0.8f), n * 0.4f, radius, PMat.Clock, 1.4f, 0.24f, 5f);
                    Fx.Burst(map, center, A(Dust, 0.4f), n * 0.2f, PMat.Dust, 0.2f, 1.6f, 0.3f, PFlags.Lit, 20f, 1f, 1.2f, radius);
                    break;
                case VfxStyle.Void:
                    Fx.Implode(map, center, c, n * 0.6f, radius, 0.7f, -1, 0.12f, 6f);
                    break;
                case VfxStyle.Nature:
                    Fx.Rise(map, center, new Color(0.9f, 1f, 0.55f, 0.8f), n * 0.4f, PMat.Mote, radius, 0.5f, 1.8f, 0.1f);
                    if (age % 8 == 0) Fx.Burst(map, center, c, n * 0.3f, PMat.Leaf, 0.4f, 2f, 0.24f, PFlags.Wobble | PFlags.Lit, 120f, 0.6f, 1.4f, radius);
                    break;
                case VfxStyle.Rune:
                    Fx.Rise(map, center, c, n * 0.3f, PMat.Rune, radius, 1f, 1f, 0.2f);
                    break;
                case VfxStyle.Shadow:
                    if (age % 6 == 0) Fx.Smoke(map, center, A(c * 0.22f, 0.55f), n * 0.4f, radius, 1.2f, 2.6f, 0.25f, true, 0.3f);
                    break;
                case VfxStyle.Blood:
                    if (age % 8 == 0) Fx.Mist(map, center, A(c * 0.6f, 0.3f), n * 0.3f, radius, 3f, 1.4f);
                    break;
                case VfxStyle.Song:
                    Fx.Rise(map, center, c, n * 0.3f, PMat.Note, radius, 0.9f, 1.4f, 0.26f, PFlags.Wobble);
                    if (age % 40 == 0) Sprite(map, ArcaneMats.Ring, center, 0.3f, radius * 2.2f, 30, A(c, 0.5f), layer: AltitudeLayer.MoteLow);
                    break;
                case VfxStyle.Wind:
                    if (age % 4 == 0) Fx.Vortex(map, center, A(W(c2, 0.2f), 0.6f), n * 0.5f, radius, PMat.Gust, 0.8f, 0.5f, 12f);
                    if (age % 9 == 0) Fx.Burst(map, center, new Color(0.55f, 0.7f, 0.35f, 1f), n * 0.2f, PMat.Leaf, 1.6f, 1.6f, 0.18f, PFlags.Wobble | PFlags.Lit, 400f, 0.3f, 0.6f, radius);
                    if (age % 14 == 0) Fx.Burst(map, center, new Color(0.8f, 0.78f, 0.7f, 0.3f), n * 0.3f, PMat.Dust, 1f, 1.4f, 0.5f, PFlags.Lit, 30f, 0f, 0.2f, radius);
                    break;
                case VfxStyle.Doom:
                    Fx.Burst(map, center, new Color(0.2f, 0.19f, 0.18f, 0.9f), n * 0.3f, PMat.Ash, 0.3f, 2.2f, 0.16f, PFlags.Lit | PFlags.Wobble, 90f, -0.25f, 1.6f, radius);
                    Fx.Embers(map, center, c, n * 0.25f, radius, 0.9f, 1.6f, 0.1f);
                    if (age % 14 == 0) Fx.Smoke(map, center, A(c * 0.18f, 0.5f), n * 0.3f, radius, 1.2f, 3f, 0.25f, true, 0.3f);
                    if (age % 60 == 0) Fx.Vortex(map, center, new Color(0.08f, 0.06f, 0.07f, 0.95f), 2f, radius * 0.8f, PMat.Crow, 2f, 0.32f, 4f);
                    break;
                case VfxStyle.Desire:
                    Fx.Burst(map, center, W(c2, 0.3f), n * 0.25f, PMat.Petal, 0.4f, 2f, 0.2f, PFlags.Wobble, 200f, 0.4f, 1.4f, radius);
                    Fx.Rise(map, center, c, n * 0.2f, PMat.Heart, radius, 0.7f, 1.4f, 0.2f, PFlags.Wobble);
                    if (age % 10 == 0) Fx.Mist(map, center, A(c, 0.16f), n * 0.3f, radius, 3f, 1.6f, true);
                    break;
                case VfxStyle.Liminal:
                    if (age % 3 == 0) Fx.Burst(map, center + VRand.InsideCircle(radius), c, 1f, PMat.Glitch, 0.1f, 0.35f, 0.24f, PFlags.Flicker, 0f, 0f, 0.3f, 0.1f);
                    if (age % 20 == 0) Fx.Rise(map, center, A(c, 0.6f), n * 0.2f, PMat.Tube, radius, 0.05f, 1.2f, 0.45f, PFlags.Flicker, 0f);
                    if (age % 45 == 0 && VRand.Chance(0.5f)) Light(map, center + VRand.InsideCircle(radius * 0.6f), c, 2.5f, 4);
                    break;
                default:
                    Fx.Rise(map, center, c, n * 0.3f, PMat.Mote, radius, 0.8f, 1.2f, 0.15f);
                    break;
            }
        }

        public static bool FieldGlows(VfxStyle s) => s == VfxStyle.Fire || s == VfxStyle.Holy || s == VfxStyle.Storm || s == VfxStyle.Rune || s == VfxStyle.Arcane
                                                     || s == VfxStyle.Necro || s == VfxStyle.Time || s == VfxStyle.Void
                                                     || s == VfxStyle.Doom || s == VfxStyle.Desire || s == VfxStyle.Liminal;
    }

    /// <summary>Per-status particle emitters and clinging overlays.</summary>
    public static class StatusFx
    {
        private static readonly Color Dust = new Color(0.6f, 0.55f, 0.48f, 0.5f);

        public static void Emit(string preset, Pawn p, Color c, float severity)
        {
            var map = p?.Map;
            if (map == null || map != Find.CurrentMap || preset.NullOrEmpty()) return;
            Vector3 pos = p.DrawPos;
            float k = Mathf.Clamp(severity, 0.5f, 3f);
            switch (preset)
            {
                case "Burning":
                    Fx.Flames(map, pos, c, new Color(1f, 0.85f, 0.4f), 1.2f * k, 0.25f, 0.42f, 0.05f);
                    Fx.Embers(map, pos, c, 0.4f * k, 0.3f, 1f, 1.3f, 0.12f);
                    if (VRand.Chance(0.15f)) Fx.Smoke(map, pos, new Color(0.15f, 0.14f, 0.13f, 0.6f), 1f, 0.2f, 0.6f, 2f, 0.6f, true);
                    break;
                case "Chill":
                    Fx.Burst(map, pos + new Vector3(0f, 0f, 0.4f), new Color(0.85f, 0.95f, 1f, 0.9f), 0.5f, PMat.Snow, 0.3f, 1f, 0.16f, PFlags.Wobble, 90f, 0.6f, 0.5f);
                    break;
                case "Frozen":
                    Fx.Mist(map, pos, new Color(0.85f, 0.95f, 1f, 0.25f), 0.3f, 0.4f, 2.4f, 1.1f);
                    break;
                case "Shocked":
                    if (VRand.Chance(0.35f)) ArcaneVfxManager.For(map)?.Add(new VfxArcs { follow = p, radius = 0.5f, color = c, duration = 8, intensity = 1.2f });
                    break;
                case "Poison":
                case "Plague":
                    Fx.Rise(map, pos, new Color(c.r, c.g, c.b, 0.8f), 0.5f * k, PMat.Bubble, 0.3f, 0.6f, 1.2f, 0.14f);
                    if (preset == "Plague" && VRand.Chance(0.25f)) Fx.Mist(map, pos, new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, 0.3f), 1f, 0.4f, 2.4f, 1f);
                    break;
                case "Bleed":
                    Fx.Drips(map, pos, new Color(0.6f, 0.05f, 0.08f, 0.9f), 0.5f * k, 0.5f);
                    break;
                case "Doom":
                case "Shadow":
                    Fx.Rise(map, pos, new Color(c.r, c.g, c.b, 0.6f), 0.4f, PMat.Wisp, 0.3f, 0.7f, 1.2f, 0.28f, PFlags.None, 200f);
                    break;
                case "Slow":
                    Fx.Burst(map, pos, new Color(c.r, c.g, c.b, 0.8f), 0.3f, PMat.Clock, 0.4f, 1f, 0.22f);
                    break;
                case "Haste":
                    if (p.pather != null && p.pather.Moving)
                    {
                        Vector3 back = (pos - p.pather.nextCell.ToVector3Shifted()).Yto0().normalized;
                        Fx.Burst(map, pos + back * 0.3f, new Color(c.r, c.g, c.b, 0.6f), 1.2f, PMat.Streak, 0.5f, 0.3f, 0.35f, PFlags.Stretch, 0f, 0f, 0.3f, 0.3f);
                    }
                    else Fx.Burst(map, pos, new Color(c.r, c.g, c.b, 0.6f), 0.3f, PMat.Mote, 0.8f, 0.5f, 0.15f);
                    break;
                case "Wither":
                    Fx.Burst(map, pos, Dust, 0.5f * k, PMat.Dust, 0.3f, 1.4f, 0.25f, PFlags.Lit, 30f, 1.2f, 0.7f);
                    break;
                case "Void":
                    Fx.Implode(map, pos, new Color(c.r, c.g, c.b, 0.8f), 0.6f, 0.9f, 0.4f, -1, 0.08f, 6f);
                    break;
                case "Holy":
                case "Divine":
                    Fx.Rise(map, pos, new Color(1f, 0.95f, 0.7f, 0.9f), 0.5f, PMat.Star, 0.4f, 1.2f, 1f, 0.15f, PFlags.Glint);
                    if (preset == "Divine" && VRand.Chance(0.2f))
                        Fx.Burst(map, pos + new Vector3(0f, 0f, 0.8f), Color.white, 1f, PMat.Feather, 0.3f, 1.6f, 0.26f, PFlags.Wobble, 60f, 0.4f, 0.5f);
                    break;
                case "Roots":
                    Fx.Burst(map, pos, c, 0.3f, PMat.Leaf, 0.3f, 1.4f, 0.2f, PFlags.Wobble | PFlags.Lit, 90f, 0.6f, 0.3f);
                    break;
                case "Stone":
                    Fx.Burst(map, pos, new Color(0.6f, 0.58f, 0.55f, 0.6f), 0.2f, PMat.Dust, 0.2f, 1.2f, 0.25f, PFlags.Lit, 20f, 1f, 0.5f);
                    break;
                case "Gold":
                    Fx.Burst(map, pos, new Color(1f, 0.85f, 0.35f), 0.5f, PMat.Gold, 0.4f, 0.8f, 0.14f, PFlags.Glint, 90f, 0.8f, 0.6f);
                    break;
                case "Undead":
                    Fx.Rise(map, pos, new Color(c.r, c.g, c.b, 0.55f), 0.25f, PMat.Wisp, 0.3f, 0.6f, 1.2f, 0.26f, PFlags.None, 200f);
                    break;
                case "Song":
                    Fx.Rise(map, pos, c, 0.3f, PMat.Note, 0.4f, 0.8f, 1.2f, 0.22f, PFlags.Wobble);
                    break;
                case "Fire":
                    Fx.Embers(map, pos, c, 0.5f, 0.3f, 0.9f, 1.2f, 0.12f);
                    break;
                case "Ward":
                    Fx.Burst(map, pos, c, 0.3f, PMat.Spark, 0.6f, 0.6f, 0.12f);
                    break;
                case "Blood":
                    Fx.Rise(map, pos, c, 0.4f, PMat.Blood, 0.3f, 0.7f, 1f, 0.12f);
                    break;
                case "Omen":
                    if (VRand.Chance(0.5f))
                        Fx.Burst(map, pos + new Vector3(0f, 0f, 0.6f), new Color(0.08f, 0.06f, 0.07f, 0.95f), 1f, PMat.FeatherDark, 0.3f, 1.6f, 0.2f, PFlags.Wobble | PFlags.Lit, 90f, 0.4f, 0.6f);
                    if (severity >= 4f && VRand.Chance(0.3f)) Fx.Rise(map, pos, c, 1f, PMat.Eye, 0.2f, 0.6f, 0.9f, 0.2f, PFlags.Pulse, 0f);
                    break;
                case "Gale":
                    Fx.Vortex(map, pos, new Color(c.r, c.g, c.b, 0.6f), 0.6f, 0.6f, PMat.Gust, 0.6f, 0.3f, 12f);
                    break;
                case "Charm":
                    Fx.Rise(map, pos + new Vector3(0f, 0f, 0.5f), c, 0.4f * k, PMat.Heart, 0.2f, 0.7f, 1f, 0.16f, PFlags.Wobble);
                    if (VRand.Chance(0.15f)) Fx.Burst(map, pos, new Color(1f, 0.8f, 0.9f, 0.9f), 1f, PMat.Petal, 0.3f, 1.4f, 0.16f, PFlags.Wobble, 200f, 0.3f, 0.6f);
                    break;
                case "Lost":
                    Fx.Burst(map, pos + VRand.InsideCircle(0.3f), c, 0.5f * k, PMat.Glitch, 0.2f, 0.3f, 0.18f, PFlags.Flicker, 0f);
                    break;
                case "Debt":
                    Fx.Burst(map, pos, c, 0.4f, PMat.Gold, 0.4f, 0.8f, 0.14f, PFlags.Glint, 90f, 0.8f, 0.6f);
                    if (VRand.Chance(0.2f)) Fx.Embers(map, pos, c, 1f, 0.3f, 0.8f, 1.2f, 0.12f);
                    break;
                case "Dream":
                    Fx.Rise(map, pos + new Vector3(0.2f, 0f, 0.5f), c, 0.25f, PMat.Star, 0.1f, 0.5f, 1.4f, 0.14f, PFlags.Glint);
                    if (VRand.Chance(0.1f)) Fx.Mist(map, pos, new Color(c.r, c.g, c.b, 0.2f), 1f, 0.4f, 2.4f, 1f, true);
                    break;
                case "Infernal":
                    Fx.Flames(map, pos, c, new Color(1f, 0.7f, 0.3f), 0.6f * k, 0.25f, 0.32f, 0.05f);
                    break;
                case "Watched":
                    if (VRand.Chance(0.08f)) Fx.Rise(map, pos + new Vector3(0f, 0f, 0.7f), new Color(1f, 1f, 1f, 0.85f), 1f, PMat.Eye, 0.05f, 0.2f, 0.8f, 0.2f, PFlags.Pulse, 0f);
                    break;
                case "Strain":
                    Fx.Sparks(map, pos + new Vector3(0f, 0f, 0.45f), new Color(1f, 0.35f, 0.25f), 1.5f * k, 2f, 0.08f, 0.3f, -1f, 360f, 1f, 0.1f);
                    if (severity > 0.6f && VRand.Chance(0.4f)) Fx.Drips(map, pos + new Vector3(0f, 0f, 0.2f), new Color(0.6f, 0.05f, 0.08f, 0.9f), 1f, 0.3f);
                    break;
            }
        }
    }
}

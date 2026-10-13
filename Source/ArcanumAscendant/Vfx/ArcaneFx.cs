using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArcanumAscendant
{
    /// <summary>High level visual effect helpers.</summary>
    public static class ArcaneFx
    {
        private static float Intensity => ArcanumMod.Settings.vfxIntensity;

        public static void PlayAll(List<VfxSpec> specs, Map map, Vector3 pos, SpellContext ctx, float radius, Thing follow = null)
        {
            if (specs.NullOrEmpty() || map == null) return;
            foreach (var s in specs) Play(s, map, pos, ctx?.Color ?? Color.white, ctx?.Color2 ?? Color.white, radius, follow);
        }

        public static void Play(VfxSpec s, Map map, Vector3 pos, Color c1, Color c2, float radius, Thing follow = null)
        {
            if (s == null || map == null) return;
            if (s.delay > 0)
            {
                ArcaneVfxManager.For(map).Schedule(s.delay, () => PlayNow(s, map, pos, c1, c2, radius, follow));
                return;
            }
            PlayNow(s, map, pos, c1, c2, radius, follow);
        }

        private static void PlayNow(VfxSpec s, Map map, Vector3 pos, Color c1, Color c2, float radius, Thing follow)
        {
            var mgr = ArcaneVfxManager.For(map);
            Color color = s.color.a > 0f ? s.color : (s.useColor2 ? c2 : c1);
            color.a *= s.alpha;
            float sizeMul = s.scaleByRadius ? Mathf.Max(1f, radius * 2f) : 1f;
            float s0 = s.scale * sizeMul;
            float s1 = (s.scaleEnd >= 0f ? s.scaleEnd : s.scale) * sizeMul;
            Thing f = s.attach ? follow : null;
            switch (s.type)
            {
                case VfxType.Glow:
                case VfxType.Flash:
                    mgr.Add(new VfxSprite
                    {
                        mat = ArcaneMats.Get(s.tex ?? "ArcanumAscendant/Vfx/Glow"), pos = pos, follow = f, scaleStart = s0, scaleEnd = s1,
                        duration = s.ticks, color = color, fadeIn = s.type == VfxType.Flash ? 0.02f : 0.15f, fadeOut = 0.6f, spin = s.rotation,
                        angle = VRand.Range(0f, 360f),
                    });
                    break;
                case VfxType.Ring:
                case VfxType.Shockwave:
                    mgr.Add(new VfxSprite
                    {
                        mat = s.type == VfxType.Shockwave ? ArcaneMats.Shock : ArcaneMats.Get(s.tex ?? "ArcanumAscendant/Vfx/Ring"), pos = pos, follow = f,
                        scaleStart = s0, scaleEnd = s1, duration = s.ticks, color = color, fadeIn = 0.02f, fadeOut = 0.7f, spin = s.rotation,
                        layer = s.ground ? AltitudeLayer.MoteLow : AltitudeLayer.MoteOverhead,
                    });
                    break;
                case VfxType.Rune:
                case VfxType.Hex:
                    mgr.Add(new VfxSprite
                    {
                        mat = s.type == VfxType.Hex ? ArcaneMats.Hex : ArcaneMats.Get(s.tex ?? "ArcanumAscendant/Vfx/RuneCircle"), pos = pos, follow = f,
                        scaleStart = s0, scaleEnd = s1, duration = s.ticks, color = color, fadeIn = 0.2f, fadeOut = 0.4f,
                        spin = s.rotation == 0f && s.type == VfxType.Rune ? 1.5f : s.rotation,
                        layer = s.type == VfxType.Rune || s.ground ? AltitudeLayer.MoteLow : AltitudeLayer.MoteOverhead,
                    });
                    break;
                case VfxType.Pillar:
                    mgr.Add(new VfxPillar(pos, s.ticks, color, c2, s0));
                    break;
                case VfxType.Lightning:
                    mgr.Add(new VfxLightning(pos + new Vector3(VRand.Range(-2f, 2f), 0f, 16f), pos, s.ticks, color, c2, s0));
                    break;
                case VfxType.Particles:
                    Particles(map, pos, color, s.fleck ?? ArcaneDefOf.AA_Fleck_Spark, Mathf.RoundToInt(s.count * (s.scaleByRadius ? Mathf.Max(1f, radius) : 1f)), s.speed, s.scale, -1f, s.scaleByRadius ? radius : 0.3f);
                    break;
                case VfxType.Spiral:
                    for (int i = 0; i < s.count; i++)
                    {
                        float ang = i * 360f / s.count;
                        Vector3 off = Quaternion.AngleAxis(ang, Vector3.up) * new Vector3(Mathf.Max(0.6f, radius), 0f, 0f);
                        var data = FleckMaker.GetDataStatic(pos + off, map, s.fleck ?? ArcaneDefOf.AA_Fleck_Mote, s.scale);
                        data.velocityAngle = ang + 180f + 35f;
                        data.velocitySpeed = s.speed;
                        data.instanceColor = color;
                        data.rotationRate = VRand.Range(-90f, 90f);
                        map.flecks.CreateFleck(data);
                    }
                    break;
                case VfxType.Smoke:
                    for (int i = 0; i < s.count; i++)
                    {
                        Vector3 r = VRand.InsideCircle(Mathf.Max(0.5f, radius));
                        Particles(map, pos + r, color, ArcaneDefOf.AA_Fleck_Smoke, 1, s.speed * 0.3f, s.scale * VRand.Range(1.5f, 2.5f));
                    }
                    break;
                case VfxType.Orbit:
                    mgr.Add(new VfxOrbit
                    {
                        follow = f, pos = pos, mat = ArcaneMats.Get(s.tex ?? "ArcanumAscendant/Vfx/Spark"), color = color, count = Mathf.Max(1, s.count),
                        radius = Mathf.Max(0.5f, s.scale), speed = s.speed, duration = s.ticks, size = 0.35f,
                    });
                    break;
                case VfxType.Light:
                    // scale is the light radius in cells.
                    ArcaneLightFlash.Spawn(map, pos, color, s0, s.ticks);
                    break;
                case VfxType.Decal:
                    // Decals linger: ticks is their lifetime, never shorter than four seconds.
                    mgr.decals.Add(pos, s.decal, s0, color.a > 0f ? color : Color.white, Mathf.Max(4f, s.ticks / 60f));
                    break;
                case VfxType.ScreenFlash:
                    // alpha is the strength; ground switches to a darkening vignette instead of a full-screen wash.
                    if (map == Find.CurrentMap) mgr.screen.AddFlash(new Color(color.r, color.g, color.b, 1f), s.alpha, s.ticks, s.ground, mgr.Now);
                    break;
                case VfxType.Sparks:
                    Fx.Sparks(map, pos, color, s.count, s.speed, 0.11f * s.scale);
                    break;
                case VfxType.Embers:
                    Fx.Embers(map, pos, color, s.count, s.scaleByRadius ? Mathf.Max(0.4f, radius) : 0.4f, Mathf.Max(0.2f, s.speed * 0.5f), 1.6f, 0.16f * s.scale);
                    break;
                case VfxType.SmokePlume:
                    Fx.Smoke(map, pos, color, s.count, s.scaleByRadius ? Mathf.Max(0.6f, radius) : 0.6f, s.scale, 2.6f, 0.35f, s.ground, Mathf.Max(0.1f, s.speed * 0.25f));
                    break;
                case VfxType.Debris:
                    Fx.Debris(map, pos, color, s.count, s.speed, 0.2f * s.scale);
                    break;
                case VfxType.Shards:
                    Fx.Shards(map, pos, color, s.count, s.speed, 0.28f * s.scale);
                    break;
                case VfxType.GodRays:
                    mgr.Add(new VfxGodRays(pos, Mathf.Max(0.8f, s.scaleByRadius ? radius : s.scale), color, Mathf.Max(1, s.count), s.ticks));
                    break;
                case VfxType.Implode:
                    Fx.Implode(map, pos, color, s.count, Mathf.Max(1f, s.scaleByRadius ? radius + 1f : 2.5f * s.scale), Mathf.Clamp(s.ticks / 60f, 0.3f, 2f));
                    break;
                case VfxType.Flare:
                    mgr.Add(new VfxSprite
                    {
                        mat = ArcaneMats.Flare, pos = pos, follow = f, scaleStart = s0, scaleEnd = s1, duration = s.ticks, color = color,
                        fadeIn = 0.04f, fadeOut = 0.7f, spin = s.rotation, angle = VRand.Range(0f, 360f),
                    });
                    break;
                case VfxType.Mist:
                    Fx.Mist(map, pos, color, s.count, Mathf.Max(0.6f, s.scaleByRadius ? radius : s.scale), 3.2f, 1.6f, !s.ground);
                    break;
                case VfxType.Ripples:
                {
                    // Several rings rolling out one after another, like a stone dropped in water.
                    int rings = Mathf.Clamp(s.count, 1, 6);
                    for (int i = 0; i < rings; i++)
                    {
                        float k = 1f - i * 0.12f;
                        mgr.Schedule(i * Mathf.Max(2, s.ticks / (rings * 2)), () => mgr.Add(new VfxSprite
                        {
                            mat = ArcaneMats.Ring, pos = pos, follow = f, scaleStart = s0 * 0.2f, scaleEnd = s1 * k, duration = s.ticks,
                            color = color * new Color(1f, 1f, 1f, k), fadeIn = 0.02f, fadeOut = 0.8f, layer = AltitudeLayer.MoteLow,
                        }));
                    }
                    break;
                }
                case VfxType.Arcs:
                    mgr.Add(new VfxArcs { follow = f, pos = pos, radius = Mathf.Max(0.4f, s.scaleByRadius ? radius : s.scale * 0.7f), color = color, duration = s.ticks });
                    break;
                case VfxType.Telegraph:
                    mgr.Add(new VfxTelegraph { follow = f, pos = pos, radius = Mathf.Max(0.5f, s.scaleByRadius ? radius : s.scale), color = color, duration = s.ticks });
                    break;
            }
        }

        public static void Particles(Map map, Vector3 pos, Color color, FleckDef fleck, int count, float speed, float scale, float angle = -1f, float spread = 0.25f)
        {
            if (map == null || fleck == null) return;
            count = Mathf.RoundToInt(count * Intensity);
            if (count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                var data = FleckMaker.GetDataStatic(pos + VRand.InsideCircle(spread), map, fleck, scale * VRand.Range(0.7f, 1.3f));
                data.velocityAngle = angle >= 0f ? angle + VRand.Range(-20f, 20f) : VRand.Range(0f, 360f);
                data.velocitySpeed = speed * VRand.Range(0.5f, 1.4f);
                data.rotationRate = VRand.Range(-180f, 180f);
                data.rotation = VRand.Range(0f, 360f);
                data.instanceColor = color;
                map.flecks.CreateFleck(data);
            }
        }

        public static void Flash(Map map, Vector3 pos, Color color, float size)
        {
            var mgr = ArcaneVfxManager.For(map);
            mgr?.Add(new VfxSprite { mat = ArcaneMats.Glow, pos = pos, scaleStart = size, scaleEnd = size * 1.3f, duration = 12, color = color, fadeIn = 0.01f, fadeOut = 0.8f });
        }

        public static void Shake(Map map, float mag)
        {
            if (!ArcanumMod.Settings.screenShake || map != Find.CurrentMap || mag <= 0f) return;
            Find.CameraDriver.shaker.DoShake(mag);
        }

        public static void CastingCircle(Pawn pawn, Color c1, Color c2, bool ultimate)
        {
            var mgr = ArcaneVfxManager.For(pawn.Map);
            if (mgr == null) return;
            float size = ultimate ? 3.2f : 1.9f;
            mgr.Add(new VfxSprite
            {
                mat = ArcaneMats.Rune, follow = pawn, pos = pawn.DrawPos, scaleStart = size * 0.85f, scaleEnd = size, duration = 26, color = c1 * new Color(1, 1, 1, 0.75f),
                spin = 2.5f, fadeIn = 0.25f, fadeOut = 0.35f, layer = AltitudeLayer.MoteLow,
            });
            if (ultimate)
            {
                mgr.Add(new VfxSprite
                {
                    mat = ArcaneMats.Get("ArcanumAscendant/Vfx/RuneCircle2"), follow = pawn, pos = pawn.DrawPos, scaleStart = size * 1.5f, scaleEnd = size * 1.45f,
                    duration = 26, color = c2 * new Color(1, 1, 1, 0.6f), spin = -1.8f, fadeIn = 0.25f, fadeOut = 0.35f, layer = AltitudeLayer.MoteLow,
                });
            }
            Particles(pawn.Map, pawn.DrawPos + new Vector3(0f, 0f, -0.2f), c2, ArcaneDefOf.AA_Fleck_Mote, ultimate ? 3 : 1, 0.6f, 0.5f, 0f, size * 0.4f);
        }

        public static void LevelUpBurst(Pawn pawn, Color color, bool big)
        {
            var map = pawn.Map;
            if (map == null) return;
            var mgr = ArcaneVfxManager.For(map);
            Vector3 pos = pawn.DrawPos;
            mgr.Add(new VfxPillar(pos, big ? 60 : 40, color, Color.white, big ? 2.2f : 1.4f));
            mgr.Add(new VfxSprite { mat = ArcaneMats.Rune, pos = pos, scaleStart = 1f, scaleEnd = big ? 5f : 3.5f, duration = big ? 70 : 50, color = color, spin = 3f, layer = AltitudeLayer.MoteLow });
            mgr.Add(new VfxSprite { mat = ArcaneMats.Shock, pos = pos, scaleStart = 0.5f, scaleEnd = big ? 8f : 5f, duration = 35, color = color, fadeIn = 0.02f, fadeOut = 0.8f });
            Particles(map, pos, color, ArcaneDefOf.AA_Fleck_Spark, big ? 30 : 16, 3f, 0.6f);
            Particles(map, pos, Color.white, ArcaneDefOf.AA_Fleck_Rune, big ? 8 : 4, 1.2f, 0.7f, 0f, 1f);
            if (big) Shake(map, 1.5f);
        }

        public static void NodeUnlockPulse(Pawn pawn, Color color)
        {
            var mgr = ArcaneVfxManager.For(pawn.Map);
            mgr?.Add(new VfxSprite { mat = ArcaneMats.Ring, follow = pawn, pos = pawn.DrawPos, scaleStart = 0.5f, scaleEnd = 2.6f, duration = 30, color = color, fadeIn = 0.02f, fadeOut = 0.8f });
            Particles(pawn.Map, pawn.DrawPos, color, ArcaneDefOf.AA_Fleck_Spark, 8, 1.6f, 0.5f);
        }

        public static void WardHit(Pawn pawn, Color color, DamageInfo dinfo, bool broke)
        {
            var map = pawn.Map;
            if (map == null) return;
            var mgr = ArcaneVfxManager.For(map);
            mgr.Add(new VfxSprite
            {
                mat = ArcaneMats.Hex, follow = pawn, pos = pawn.DrawPos, scaleStart = 1.9f, scaleEnd = broke ? 2.8f : 2.1f, duration = broke ? 30 : 18,
                color = (broke ? Color.white : color) * new Color(1, 1, 1, 0.8f), fadeIn = 0.02f, fadeOut = 0.8f, angle = VRand.Range(0f, 60f),
            });
            Vector3 impact = pawn.DrawPos;
            if (dinfo.Instigator != null && dinfo.Instigator.Spawned)
                impact += (dinfo.Instigator.DrawPos - pawn.DrawPos).Yto0().normalized * 0.8f;
            Particles(map, impact, color, ArcaneDefOf.AA_Fleck_Spark, broke ? 14 : 4, broke ? 3f : 1.5f, 0.5f);
            if (broke)
            {
                SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(pawn.Position, map));
                Particles(map, pawn.DrawPos, color, ArcaneDefOf.AA_Fleck_Shard, 10, 2.5f, 0.5f);
            }
            else SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(pawn.Position, map));
        }

        public static void ConeSpray(Map map, Vector3 origin, float angle, float coneAngle, float range, SpellContext ctx)
        {
            int n = Mathf.RoundToInt(range * coneAngle / 6f);
            for (int i = 0; i < n; i++)
            {
                float a = angle + VRand.Range(-coneAngle * 0.5f, coneAngle * 0.5f);
                var fleck = VRand.Chance(0.5f) ? ArcaneDefOf.AA_Fleck_Ember : ArcaneDefOf.AA_Fleck_Mote;
                var data = FleckMaker.GetDataStatic(origin, map, fleck, VRand.Range(0.6f, 1.3f));
                data.velocityAngle = a;
                data.velocitySpeed = range * VRand.Range(1.4f, 2.2f);
                data.instanceColor = VRand.Chance(0.5f) ? ctx.Color : ctx.Color2;
                data.rotationRate = VRand.Range(-200f, 200f);
                map.flecks.CreateFleck(data);
            }
            Flash(map, origin, ctx.Color, 2.5f);
        }

        public static void Eruption(Map map, Vector3 pos, Color c1, Color c2, float r)
        {
            var mgr = ArcaneVfxManager.For(map);
            mgr.Add(new VfxSprite { mat = ArcaneMats.Glow, pos = pos, scaleStart = r * 1.5f, scaleEnd = r * 3f, duration = 18, color = c1, fadeIn = 0.02f, fadeOut = 0.8f });
            mgr.Add(new VfxSprite { mat = ArcaneMats.Shock, pos = pos, scaleStart = 0.3f, scaleEnd = r * 3f + 1f, duration = 20, color = c2, fadeIn = 0.02f, fadeOut = 0.8f, layer = AltitudeLayer.MoteLow });
            Particles(map, pos, c1, ArcaneDefOf.AA_Fleck_Ember, 5, 2.5f, 0.8f);
            FleckMaker.ThrowDustPuffThick(pos, map, 1.5f, new Color(0.6f, 0.55f, 0.5f));
        }

        /// <summary>A floating damage number. Crits are bigger, gold and punctuated.</summary>
        public static void DamageNumber(Map map, Vector3 pos, float amount, bool crit, Color school)
        {
            if (map == null || map != Find.CurrentMap || amount < 0.5f) return;
            var mgr = ArcaneVfxManager.For(map);
            int n = Mathf.RoundToInt(amount);
            Color c = crit ? new Color(1f, 0.82f, 0.25f) : Color.Lerp(school, Color.white, 0.55f);
            float scale = crit ? 1.45f : amount >= 60f ? 1.3f : 1f;
            mgr.screen.AddText(pos, crit ? n + "!" : n.ToString(), c, scale, mgr.Now);
        }

        public static void HealNumber(Map map, Vector3 pos, float amount, bool crit)
        {
            if (map == null || map != Find.CurrentMap || amount < 0.5f) return;
            var mgr = ArcaneVfxManager.For(map);
            mgr.screen.AddText(pos, "+" + Mathf.RoundToInt(amount), crit ? new Color(0.6f, 1f, 0.75f) : new Color(0.45f, 0.95f, 0.55f), crit ? 1.3f : 1f, mgr.Now);
        }

        /// <summary>Floating text with the combat-text styling (outlined, pops in, rises).</summary>
        public static void FloatText(Map map, Vector3 pos, string text, Color color, float scale = 1.1f)
        {
            if (map == null || map != Find.CurrentMap) return;
            var mgr = ArcaneVfxManager.For(map);
            mgr.screen.AddText(pos, text, color, scale, mgr.Now, 90f);
        }

        public static void Text(Thing t, string text, Color color)
        {
            if (t?.Map == null) return;
            if (t.Map == Find.CurrentMap)
            {
                var mgr = ArcaneVfxManager.For(t.Map);
                mgr.screen.AddText(t.DrawPos + new Vector3(0f, 0f, 0.3f), text, color, 1.05f, mgr.Now, 100f, true);
                return;
            }
            MoteMaker.ThrowText(t.DrawPos + new Vector3(0f, 0f, 0.5f), t.Map, text, color, 2.5f);
        }

        public static void Aura(Thing t, Color color, int ticks, float size = 1.6f, string tex = null)
        {
            if (t?.Map == null) return;
            ArcaneVfxManager.For(t.Map).Add(new VfxSprite
            {
                mat = ArcaneMats.Get(tex ?? "ArcanumAscendant/Vfx/RuneCircle"), follow = t, pos = t.DrawPos, scaleStart = size, scaleEnd = size, duration = ticks,
                color = color * new Color(1, 1, 1, 0.6f), spin = 2f, fadeIn = 0.1f, fadeOut = 0.2f, layer = AltitudeLayer.MoteLow,
            });
        }
    }
}

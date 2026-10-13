using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>
    /// The charge-up of a spell: a school-specific circle unfolds under the caster, energy gathers into a growing orb at
    /// their hand, and it all brightens as the cast nears completion.
    /// </summary>
    public class VfxCastCircle : Vfx
    {
        public Pawn pawn;
        public Ability ability;
        public VfxStyle style;
        public Color c1;
        public Color c2;
        public bool ultimate;
        private int maxWarmup = 1;
        private float progress;
        private bool ending;
        private int endTick = -1;
        private Vector3 dir = Vector3.forward;
        private bool lightOn;
        private Material inner;

        public VfxCastCircle()
        {
            duration = 100000;
        }

        public bool Ending => ending;

        private bool ReadWarmup()
        {
            if (pawn.stances?.curStance is Stance_Warmup w && w.verb is Verb_CastAbility v && v.ability == ability)
            {
                if (w.ticksLeft > maxWarmup) maxWarmup = w.ticksLeft;
                progress = 1f - w.ticksLeft / (float)Mathf.Max(1, maxWarmup);
                if (w.focusTarg.IsValid)
                {
                    Vector3 d = (w.focusTarg.CenterVector3 - pawn.DrawPos).Yto0();
                    if (d.sqrMagnitude > 0.01f) dir = d.normalized;
                }
                return true;
            }
            return false;
        }

        private Vector3 Hand() => pawn.DrawPos + dir * 0.42f + new Vector3(0f, 0f, 0.18f);

        public override void Tick(int now, Map map)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map != map || pawn.Dead)
            {
                finished = true;
                return;
            }
            if (!ending && !ReadWarmup())
            {
                ending = true;
                endTick = now;
            }
            if (ending)
            {
                if (now - endTick > 14) finished = true;
                return;
            }
            int age = now - startTick;
            Vector3 hand = Hand();
            if (age % 2 == 0) Fx.Implode(map, hand, c2, ultimate ? 2.2f : 1f, ultimate ? 3.2f : 2.1f, 0.5f, -1, 0.13f, ultimate ? 6f : 3f);
            if (!lightOn && age > 5)
            {
                lightOn = true;
                ArcaneLightFlash.Spawn(map, pawn.DrawPos, c1, ultimate ? 6f : 3.5f, Mathf.Clamp(maxWarmup + 20, 30, 600));
            }
            if (ultimate && age % 20 == 0) ArcaneFx.Shake(map, 0.2f + progress * 0.5f);
            StyleFx.CastTick(style, map, pawn, hand, age, progress, c1, c2, ultimate);
        }

        public override void Draw(float now)
        {
            if (pawn == null || !pawn.Spawned) return;
            float age = now - startTick;
            float appear = Mathf.Clamp01(age / 10f);
            float fade = ending ? Mathf.Clamp01(1f - (now - endTick) / 14f) : 1f;
            float baseSize = ultimate ? 3.6f : 2.2f;
            float size = baseSize * Mathf.Max(0.05f, EaseOutBack(appear)) * (ending ? 1f + (1f - fade) * 0.4f : 1f);
            float a = (0.45f + 0.55f * progress) * fade;
            Vector3 p = pawn.DrawPos;
            p.y = VAlt.Ground;
            inner ??= ArcaneMats.Get("ArcanumAscendant/Vfx/Circle_" + style);
            ArcaneMats.DrawQuad(ArcaneMats.Glow, p, size * 1.3f, 0f, c1 * new Color(1, 1, 1, 0.22f * a));
            ArcaneMats.DrawQuad(inner, p + new Vector3(0, 0.002f, 0), size, now * (1.5f + progress * 4f), c1 * new Color(1, 1, 1, a));
            ArcaneMats.DrawQuad(ArcaneMats.Rune, p + new Vector3(0, 0.004f, 0), size * 1.35f, -now * 1.2f, c2 * new Color(1, 1, 1, a * 0.55f));
            if (ultimate)
                ArcaneMats.DrawQuad(ArcaneMats.Get("ArcanumAscendant/Vfx/RuneCircle2"), p + new Vector3(0, 0.006f, 0), size * 1.85f, now * 0.7f, c1 * new Color(1, 1, 1, a * 0.45f));
            Vector3 h = Hand();
            h.y = VAlt.Glow;
            float orb = (0.22f + 0.8f * progress) * (ultimate ? 1.6f : 1f) * (1f + 0.1f * Mathf.Sin(now * 2.5f)) * fade;
            ArcaneMats.DrawQuad(ArcaneMats.Glow, h, orb * 2.8f, 0f, c1 * new Color(1, 1, 1, 0.65f * fade));
            ArcaneMats.DrawQuad(ArcaneMats.Glow, h + new Vector3(0, 0.002f, 0), orb, 0f, Color.Lerp(c2, Color.white, 0.6f) * new Color(1, 1, 1, fade));
            ArcaneMats.DrawQuad(ArcaneMats.Flare, h + new Vector3(0, 0.004f, 0), orb * 2f, now * 5f, new Color(1f, 1f, 1f, 0.55f * progress * fade));
            Vector3 body = pawn.DrawPos;
            body.y = VAlt.Glow - 0.01f;
            ArcaneMats.DrawQuad(ArcaneMats.Glow, body, 1.3f + progress * (ultimate ? 2f : 0.8f), 0f, c1 * new Color(1, 1, 1, 0.16f * progress * fade));
        }
    }

    /// <summary>A warning circle on the ground that fills up until the strike lands.</summary>
    public class VfxTelegraph : Vfx
    {
        public Vector3 pos;
        public Thing follow;
        public float radius = 1.5f;
        public Color color = new Color(1f, 0.35f, 0.2f);

        public override void Draw(float now)
        {
            float t = T(now);
            Vector3 p = follow != null && follow.Spawned ? follow.DrawPos : pos;
            p.y = VAlt.Ground;
            float pulse = 0.6f + 0.4f * Mathf.Sin(now * 0.6f + t * 12f);
            float d = radius * 2f;
            ArcaneMats.DrawQuad(ArcaneMats.Ring, p, d, 0f, color * new Color(1, 1, 1, 0.85f * pulse));
            ArcaneMats.DrawQuad(ArcaneMats.Telegraph, p + new Vector3(0, 0.002f, 0), d * Mathf.Pow(t, 0.7f), 0f, color * new Color(1, 1, 1, 0.3f + 0.45f * t));
            ArcaneMats.DrawQuad(ArcaneMats.Rune, p + new Vector3(0, 0.004f, 0), d * 0.75f, now * 3f, color * new Color(1, 1, 1, 0.25f));
        }
    }

    /// <summary>Soft shafts of light falling from the sky onto an area.</summary>
    public class VfxGodRays : Vfx
    {
        private readonly Vector3 pos;
        private readonly Color color;
        private readonly Vector3[] offsets;
        private readonly float[] widths;
        private readonly float[] phases;

        public VfxGodRays(Vector3 pos, float radius, Color color, int rays, int ticks)
        {
            this.pos = pos;
            this.color = color;
            duration = ticks;
            offsets = new Vector3[rays];
            widths = new float[rays];
            phases = new float[rays];
            for (int i = 0; i < rays; i++)
            {
                offsets[i] = VRand.InsideCircle(radius);
                widths[i] = VRand.Range(0.5f, 1.3f) * Mathf.Clamp(radius * 0.5f, 0.6f, 1.8f);
                phases[i] = VRand.Range(0f, 10f);
            }
        }

        public override void Draw(float now)
        {
            float t = T(now);
            float a0 = FadeInOut(t, 0.2f, 0.5f);
            float y = VAlt.Glow - 0.02f;
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 b = pos + offsets[i];
                b.y = y;
                Vector3 top = b + new Vector3(2.5f, 0f, 15f);
                float a = a0 * (0.45f + 0.35f * Mathf.Sin(now * 0.08f + phases[i]));
                ArcaneMats.DrawStretched(ArcaneMats.GodRay, b, top, widths[i], color * new Color(1, 1, 1, a));
            }
        }
    }

    /// <summary>Little electric arcs crackling around a point or a pawn.</summary>
    public class VfxArcs : Vfx
    {
        public Thing follow;
        public Vector3 pos;
        public float radius = 0.7f;
        public Color color = new Color(0.6f, 0.75f, 1f);
        public float intensity = 1f;
        private readonly List<(List<Vector3> pts, int born)> arcs = new List<(List<Vector3>, int)>();

        private Vector3 Center => follow != null && follow.Spawned ? follow.DrawPos : pos;

        public override void Tick(int now, Map map)
        {
            base.Tick(now, map);
            if (follow != null && !follow.Spawned)
            {
                finished = true;
                return;
            }
            arcs.RemoveAll(a => now - a.born > 4);
            if ((now - startTick) % 2 == 0 && VRand.Chance(0.75f * intensity))
            {
                Vector3 a = VRand.OnCircle(radius * VRand.Range(0.3f, 1f));
                Vector3 b = VRand.OnCircle(radius * VRand.Range(0.5f, 1.1f));
                var pts = new List<Vector3> { a };
                int n = VRand.RangeInt(3, 6);
                for (int i = 1; i < n; i++)
                {
                    Vector3 p = Vector3.Lerp(a, b, i / (float)n);
                    pts.Add(p + VRand.InsideCircle(radius * 0.25f));
                }
                pts.Add(b);
                arcs.Add((pts, now));
                if (VRand.Chance(0.3f)) Fx.Sparks(map, Center + b, color, 2f, 3f, 0.08f, 0.25f);
            }
        }

        public override void Draw(float now)
        {
            if (arcs.Count == 0) return;
            float t = T(now);
            float fade = Mathf.Clamp01((1f - t) / 0.2f);
            Vector3 c = Center;
            float y = VAlt.Glow;
            foreach (var (pts, born) in arcs)
            {
                float a = fade * Mathf.Clamp01(1f - (now - born) / 4f);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    Vector3 p0 = c + pts[i]; p0.y = y;
                    Vector3 p1 = c + pts[i + 1]; p1.y = y;
                    ArcaneMats.DrawStretched(ArcaneMats.Beam, p0, p1, 0.3f, color * new Color(1, 1, 1, 0.5f * a));
                    ArcaneMats.DrawStretched(ArcaneMats.Beam, p0 + new Vector3(0, 0.002f, 0), p1 + new Vector3(0, 0.002f, 0), 0.1f, new Color(1f, 1f, 1f, a));
                }
            }
        }
    }

    /// <summary>A wisp (a soul, a mote of stolen life) that arcs from one place to a thing, trailing light.</summary>
    public class VfxSoulWisp : Vfx
    {
        private readonly Vector3 from;
        private readonly Thing to;
        private Vector3 toPos;
        private readonly Vector3 ctrl;
        private readonly Color color;
        private readonly Color color2;
        private readonly Action onArrive;
        private readonly int mat;
        private RibbonTrail trail;
        private bool arrived;
        private Map map;

        public VfxSoulWisp(Vector3 from, Thing to, Color color, Color color2, Action onArrive = null, int mat = -1)
        {
            this.from = from;
            this.to = to;
            toPos = to?.DrawPos ?? from;
            this.color = color;
            this.color2 = color2;
            this.onArrive = onArrive;
            this.mat = mat >= 0 ? mat : PMat.Wisp;
            float dist = (toPos - from).MagnitudeHorizontal();
            duration = Mathf.Clamp(Mathf.RoundToInt(dist * 3.5f), 18, 70);
            Vector3 d = (toPos - from).Yto0();
            Vector3 perp = new Vector3(-d.z, 0f, d.x).normalized;
            ctrl = (from + toPos) * 0.5f + perp * dist * VRand.Range(0.25f, 0.5f) * VRand.Sign + new Vector3(0f, 0f, Mathf.Clamp(dist * 0.35f, 0.8f, 3f));
        }

        private Vector3 PosAt(float t)
        {
            if (to != null && to.Spawned) toPos = to.DrawPos;
            float u = 1f - t;
            return u * u * from + 2f * u * t * ctrl + t * t * toPos;
        }

        public override void Tick(int now, Map map)
        {
            this.map = map;
            int age = now - startTick;
            Vector3 p = PosAt(T(now));
            if (age % 2 == 0) Fx.Burst(map, p, color, 1f, PMat.Mote, 0.4f, 0.5f, 0.18f);
            if (!arrived && age >= duration - 1)
            {
                arrived = true;
                Fx.Burst(map, toPos, Color.Lerp(color, Color.white, 0.4f), 8f, PMat.Mote, 2f, 0.5f, 0.25f);
                ArcaneFx.Flash(map, toPos, color, 1.6f);
                try
                {
                    onArrive?.Invoke();
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[Arcanum Ascendant] Wisp callback error: " + e, 0x5A1C0E6);
                }
            }
            base.Tick(now, map);
        }

        public override void Draw(float now)
        {
            float t = T(now);
            Vector3 p = PosAt(t);
            trail ??= new RibbonTrail(16, 0.45f, 0.3f, ArcaneMats.Trail, color);
            trail.AddPoint(p, now);
            trail.Age(now);
            trail.Draw(now);
            p.y = VAlt.Glow + 0.003f;
            ArcaneMats.DrawQuad(ArcaneMats.Glow, p, 1.1f, 0f, color * new Color(1, 1, 1, 0.8f));
            ArcaneMats.DrawQuad(PMat.Get(mat), p + new Vector3(0, 0.002f, 0), 0.5f, now * 8f, Color.Lerp(color2, Color.white, 0.5f));
        }

        public override void OnEnd() => trail?.Dispose();
    }

    /// <summary>A sprite that clings to a pawn for as long as a hediff lasts (ice crust, divine shield, doom sigil).</summary>
    public class VfxStatusOverlay : Vfx
    {
        public Pawn pawn;
        public HediffDef hediff;
        public Material mat;
        public Color color = Color.white;
        public float size = 1.5f;
        public bool glow = true;
        public float spin;
        public float bob;
        public float zOffset;
        public bool pulse;

        public VfxStatusOverlay()
        {
            duration = 1000000;
        }

        public override void Tick(int now, Map map)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Map != map || pawn.health?.hediffSet?.GetFirstHediffOfDef(hediff) == null) finished = true;
        }

        public override void Draw(float now)
        {
            if (pawn == null || !pawn.Spawned) return;
            float age = now - startTick;
            float a = Mathf.Clamp01(age / 12f);
            Vector3 p = pawn.DrawPos + new Vector3(0f, 0f, zOffset + bob * Mathf.Sin(now * 0.08f));
            p.y = glow ? VAlt.Glow - 0.015f : AltitudeLayer.MoteOverheadLow.AltitudeFor();
            float s = size * (pulse ? 1f + 0.06f * Mathf.Sin(now * 0.15f) : 1f);
            ArcaneMats.DrawQuad(mat, p, s, spin * now, color * new Color(1, 1, 1, a));
        }
    }
}

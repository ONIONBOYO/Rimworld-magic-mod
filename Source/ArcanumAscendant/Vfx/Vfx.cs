using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    [StaticConstructorOnStartup]
    public static class ArcaneMats
    {
        private static readonly Dictionary<(string, bool), Material> cache = new Dictionary<(string, bool), Material>();
        public static readonly MaterialPropertyBlock MPB = new MaterialPropertyBlock();
        public static readonly Material Glow;
        public static readonly Material Ring;
        public static readonly Material Rune;
        public static readonly Material Beam;
        public static readonly Material Pillar;
        public static readonly Material Hex;
        public static readonly Material Spark;
        public static readonly Material Smoke;
        public static readonly Material Shock;
        public static readonly Material Trail;
        public static readonly Material TrailLit;
        public static readonly Material Flare;
        public static readonly Material GodRay;
        public static readonly Material Telegraph;
        public static readonly Material BlackHole;

        static ArcaneMats()
        {
            Glow = Get("ArcanumAscendant/Vfx/Glow");
            Ring = Get("ArcanumAscendant/Vfx/Ring");
            Rune = Get("ArcanumAscendant/Vfx/RuneCircle");
            Beam = Get("ArcanumAscendant/Vfx/Beam");
            Pillar = Get("ArcanumAscendant/Vfx/Pillar");
            Hex = Get("ArcanumAscendant/Vfx/HexShield");
            Spark = Get("ArcanumAscendant/Vfx/Spark");
            Smoke = Get("ArcanumAscendant/Vfx/Smoke", false);
            Shock = Get("ArcanumAscendant/Vfx/Shockwave");
            Trail = Get("ArcanumAscendant/Vfx/Trail");
            TrailLit = Get("ArcanumAscendant/Vfx/TrailSmoke", false);
            Flare = Get("ArcanumAscendant/Vfx/Flare");
            GodRay = Get("ArcanumAscendant/Vfx/GodRay");
            Telegraph = Get("ArcanumAscendant/Vfx/Telegraph");
            BlackHole = Get("ArcanumAscendant/Vfx/BlackHole", false);
        }

        public static Material Get(string path, bool additive = true)
        {
            if (path.NullOrEmpty()) path = "ArcanumAscendant/Vfx/Glow";
            if (!cache.TryGetValue((path, additive), out var mat))
            {
                var tex = ContentFinder<Texture2D>.Get(path, false) ?? ContentFinder<Texture2D>.Get("ArcanumAscendant/Vfx/Glow", false) ?? BaseContent.WhiteTex;
                mat = MaterialPool.MatFrom(tex, additive ? ShaderDatabase.MoteGlow : ShaderDatabase.Mote, Color.white);
                cache[(path, additive)] = mat;
            }
            return mat;
        }

        public static void Draw(Material mat, Vector3 pos, Quaternion rot, Vector3 scale, Color color)
        {
            if (mat == null || color.a <= 0.003f) return;
            MPB.SetColor(ShaderPropertyIDs.Color, color);
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, rot, scale), mat, 0, null, 0, MPB);
        }

        public static void DrawQuad(Material mat, Vector3 pos, float size, float angle, Color color)
        {
            Draw(mat, pos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(size, 1f, size), color);
        }

        public static void DrawStretched(Material mat, Vector3 a, Vector3 b, float width, Color color)
        {
            Vector3 d = (b - a).Yto0();
            float len = d.magnitude;
            if (len < 0.01f) return;
            Vector3 mid = (a + b) * 0.5f;
            mid.y = a.y;
            Draw(mat, mid, Quaternion.LookRotation(d), new Vector3(width, 1f, len), color);
        }
    }

    [StaticConstructorOnStartup]
    public static class ArcaneTex
    {
        public static readonly Texture2D AwakenIcon = ContentFinder<Texture2D>.Get("ArcanumAscendant/UI/Awaken", false) ?? BaseContent.BadTex;
        public static readonly Texture2D AutocastIcon = ContentFinder<Texture2D>.Get("ArcanumAscendant/UI/Autocast", false) ?? BaseContent.WhiteTex;
        public static readonly Texture2D GrimoireIcon = ContentFinder<Texture2D>.Get("ArcanumAscendant/UI/Grimoire", false) ?? BaseContent.BadTex;
        public static readonly Texture2D NodeFrame = ContentFinder<Texture2D>.Get("ArcanumAscendant/UI/NodeFrame", false) ?? BaseContent.WhiteTex;
        public static readonly Texture2D NodeFrameKeystone = ContentFinder<Texture2D>.Get("ArcanumAscendant/UI/NodeFrameKeystone", false) ?? BaseContent.WhiteTex;
        public static readonly Texture2D NodeFrameSpell = ContentFinder<Texture2D>.Get("ArcanumAscendant/UI/NodeFrameSpell", false) ?? BaseContent.WhiteTex;
        public static readonly Texture2D NodeGlow = ContentFinder<Texture2D>.Get("ArcanumAscendant/Vfx/Glow", false) ?? BaseContent.WhiteTex;
        public static readonly Texture2D TreeBackground = ContentFinder<Texture2D>.Get("ArcanumAscendant/UI/TreeBackground", false) ?? BaseContent.BlackTex;
        public static readonly Texture2D Rune = ContentFinder<Texture2D>.Get("ArcanumAscendant/Vfx/RuneCircle", false) ?? BaseContent.WhiteTex;
        public static readonly Texture2D LineTex = ContentFinder<Texture2D>.Get("ArcanumAscendant/UI/Line", false) ?? BaseContent.WhiteTex;
        public static readonly Texture2D Star = ContentFinder<Texture2D>.Get("ArcanumAscendant/Vfx/Star", false) ?? BaseContent.WhiteTex;
        public static readonly Texture2D OverchannelIcon = ContentFinder<Texture2D>.Get("ArcanumAscendant/UI/Overchannel", false) ?? BaseContent.BadTex;
        public static readonly Texture2D ManaFill = SolidColorMaterials.NewSolidColorTexture(new Color(0.25f, 0.55f, 1f));
        public static readonly Texture2D ManaBg = SolidColorMaterials.NewSolidColorTexture(new Color(0.05f, 0.07f, 0.15f));
        public static readonly Texture2D WardFill = SolidColorMaterials.NewSolidColorTexture(new Color(0.85f, 0.8f, 0.4f));
        public static readonly Texture2D XpFill = SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.45f, 1f));
        public static readonly Texture2D StrainFill = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.3f, 0.25f));
        public static readonly Texture2D BarBg = SolidColorMaterials.NewSolidColorTexture(new Color(0.08f, 0.08f, 0.1f));
    }

    /// <summary>The visual school of a spell. Decides its casting circle, projectile, impact, death and status effects.</summary>
    public enum VfxStyle
    {
        Auto,
        Arcane,
        Fire,
        Frost,
        Storm,
        Necro,
        Holy,
        Time,
        Void,
        Nature,
        Rune,
        Shadow,
        Blood,
        Song,
        Plague,
        Wind,
        Doom,
        Desire,
        Liminal,
    }

    public enum VfxType
    {
        Glow,
        Ring,
        Rune,
        Pillar,
        Particles,
        Flash,
        Spiral,
        Lightning,
        Smoke,
        Shockwave,
        Orbit,
        Hex,
        Light,
        Decal,
        ScreenFlash,
        Sparks,
        Embers,
        SmokePlume,
        Debris,
        Shards,
        GodRays,
        Implode,
        Flare,
        Mist,
        Ripples,
        Arcs,
        Telegraph,
    }

    /// <summary>Data-driven visual effect description.</summary>
    public class VfxSpec
    {
        public VfxType type = VfxType.Glow;
        public string tex;
        public FleckDef fleck;
        public float scale = 1f;
        public float scaleEnd = -1f;
        public int ticks = 30;
        public bool useColor2;
        public Color color = new Color(0f, 0f, 0f, 0f);
        public int count = 8;
        public float speed = 2f;
        public float rotation;
        public bool scaleByRadius;
        public int delay;
        public float alpha = 1f;
        public bool ground;
        public bool attach;
        public DecalKind decal = DecalKind.Scorch;
    }

    public abstract class Vfx
    {
        public int startTick;
        public int duration = 30;
        public bool finished;

        public float T(float now) => Mathf.Clamp01((now - startTick) / Mathf.Max(1f, duration));

        public virtual void Tick(int now, Map map)
        {
            if (now - startTick >= duration) finished = true;
        }

        public abstract void Draw(float now);

        /// <summary>Called once when the effect is removed. Free meshes here.</summary>
        public virtual void OnEnd()
        {
        }

        protected static float FadeInOut(float t, float fadeIn = 0.15f, float fadeOut = 0.4f)
        {
            if (t < fadeIn) return t / fadeIn;
            if (t > 1f - fadeOut) return Mathf.Clamp01((1f - t) / fadeOut);
            return 1f;
        }

        protected static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }

    /// <summary>A sprite that scales, spins and fades. Optionally follows a thing.</summary>
    public class VfxSprite : Vfx
    {
        public Material mat;
        public Vector3 pos;
        public Thing follow;
        public float scaleStart = 1f;
        public float scaleEnd = 1f;
        public float angle;
        public float spin;
        public Color color;
        public float alpha = 1f;
        public float fadeIn = 0.1f;
        public float fadeOut = 0.5f;
        public bool popIn;
        public AltitudeLayer layer = AltitudeLayer.VisEffects;

        public override void Draw(float now)
        {
            float t = T(now);
            Vector3 p = follow != null && follow.Spawned ? follow.DrawPos : pos;
            p.y = layer.AltitudeFor();
            float e = popIn ? EaseOutBack(Mathf.Clamp01(t * 2.2f)) : 1f - (1f - t) * (1f - t);
            float s = Mathf.LerpUnclamped(scaleStart, scaleEnd, e);
            var c = color;
            c.a *= alpha * FadeInOut(t, fadeIn, fadeOut);
            ArcaneMats.DrawQuad(mat, p, s, angle + spin * (now - startTick), c);
        }
    }

    /// <summary>A strip mesh along a polyline, used for lightning bolts and long beams. UV.x runs across the width.</summary>
    public class StripMesh
    {
        private Mesh mesh;
        private int capacity;
        private Vector3[] verts;
        private Vector2[] uvs;

        public void Build(List<Vector3> pts, float width, float y, float taper = 0f)
        {
            int n = pts.Count;
            if (n < 2) return;
            if (mesh == null || capacity < n)
            {
                if (mesh != null) Object.Destroy(mesh);
                capacity = n;
                mesh = new Mesh { name = "AA_Strip" };
                mesh.MarkDynamic();
                verts = new Vector3[n * 2];
                uvs = new Vector2[n * 2];
                var tris = new int[(n - 1) * 6];
                for (int i = 0; i < n - 1; i++)
                {
                    int v = i * 2, k = i * 6;
                    tris[k] = v;
                    tris[k + 1] = v + 2;
                    tris[k + 2] = v + 1;
                    tris[k + 3] = v + 1;
                    tris[k + 4] = v + 2;
                    tris[k + 5] = v + 3;
                }
                mesh.vertices = verts;
                mesh.uv = uvs;
                mesh.triangles = tris;
            }
            for (int i = 0; i < capacity; i++)
            {
                int j = Mathf.Min(i, n - 1);
                Vector3 p = pts[j];
                Vector3 d = j < n - 1 ? pts[j + 1] - p : p - pts[j - 1];
                if (j > 0 && j < n - 1) d = pts[j + 1] - pts[j - 1];
                d.y = 0f;
                Vector3 perp = new Vector3(-d.z, 0f, d.x);
                float m = perp.magnitude;
                perp = m > 0.0001f ? perp / m : Vector3.right;
                float u = j / (float)(n - 1);
                float w = width * (taper > 0f ? Mathf.Lerp(1f, 1f - taper, u) : 1f);
                p.y = y;
                verts[i * 2] = p + perp * w * 0.5f;
                verts[i * 2 + 1] = p - perp * w * 0.5f;
                uvs[i * 2] = new Vector2(0f, u);
                uvs[i * 2 + 1] = new Vector2(1f, u);
            }
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.RecalculateBounds();
        }

        public void Draw(Material mat, Color color)
        {
            if (mesh == null || mat == null || color.a <= 0.003f) return;
            ArcaneMats.MPB.SetColor(ShaderPropertyIDs.Color, color);
            Graphics.DrawMesh(mesh, Matrix4x4.identity, mat, 0, null, 0, ArcaneMats.MPB);
        }

        public void Dispose()
        {
            if (mesh != null) Object.Destroy(mesh);
            mesh = null;
        }
    }

    /// <summary>
    /// A beam of energy: it snaps on with an overshoot, pulses, carries travelling bright pulses from caster to target,
    /// sprays sparks where it lands and fades out thin.
    /// </summary>
    public class VfxBeam : Vfx
    {
        private readonly Vector3 a;
        private readonly Vector3 b;
        private readonly Material mat;
        private readonly Color color;
        private readonly Color color2;
        private readonly float width;
        public VfxStyle style = VfxStyle.Arcane;
        private bool lit;

        public VfxBeam(Vector3 a, Vector3 b, int ticks, Material mat, Color color, Color color2, float width)
        {
            this.a = a;
            this.b = b;
            duration = ticks;
            this.mat = mat;
            this.color = color;
            this.color2 = color2;
            this.width = width;
        }

        public override void Tick(int now, Map map)
        {
            base.Tick(now, map);
            int age = now - startTick;
            if (!lit)
            {
                lit = true;
                ArcaneLightFlash.Spawn(map, b, color, 3f + width * 2f, duration + 6);
            }
            Vector3 dir = (a - b).Yto0().normalized;
            if (age % 2 == 0) Fx.Sparks(map, b, color, 3f, 4.5f, 0.1f, 0.35f, dir.AngleFlat(), 140f);
            if (age % 3 == 0)
            {
                Vector3 p = Vector3.Lerp(a, b, VRand.Value);
                Fx.Burst(map, p, Color.Lerp(color, color2, VRand.Value), 1f, PMat.Mote, 0.6f, 0.5f, 0.22f);
            }
            if (age % 5 == 0) StyleFx.BeamImpactTick(style, map, b, color, color2);
        }

        public override void Draw(float now)
        {
            float t = T(now);
            float age = now - startTick;
            float grow = Mathf.Clamp01(age / 4f);
            float w = width * (grow < 1f ? EaseOutBack(grow) : 1f) * (0.85f + 0.15f * Mathf.Sin(now * 1.3f)) * (t > 0.6f ? Mathf.Lerp(1f, 0.15f, (t - 0.6f) / 0.4f) : 1f);
            float alpha = t > 0.85f ? (1f - t) / 0.15f : 1f;
            float y = VAlt.Glow;
            Vector3 pa = a; pa.y = y;
            Vector3 pb = b; pb.y = y;
            ArcaneMats.DrawStretched(mat, pa, pb, w * 2.8f, color * new Color(1, 1, 1, alpha * 0.4f));
            ArcaneMats.DrawStretched(mat, pa + new Vector3(0, 0.002f, 0), pb + new Vector3(0, 0.002f, 0), w * 1.2f, Color.Lerp(color, color2, 0.4f) * new Color(1, 1, 1, alpha * 0.85f));
            ArcaneMats.DrawStretched(mat, pa + new Vector3(0, 0.004f, 0), pb + new Vector3(0, 0.004f, 0), w * 0.4f, new Color(1f, 1f, 1f, alpha));
            Vector3 d = pb - pa;
            float len = d.MagnitudeHorizontal();
            for (int i = 0; i < 4; i++)
            {
                float f = Mathf.Repeat(age * 0.09f * (10f / Mathf.Max(3f, len)) + i * 0.25f, 1f);
                Vector3 p = pa + d * f;
                p.y = y + 0.006f;
                ArcaneMats.DrawQuad(ArcaneMats.Glow, p, w * 2.2f, 0f, Color.Lerp(color2, Color.white, 0.5f) * new Color(1, 1, 1, alpha * 0.8f * Mathf.Sin(f * Mathf.PI)));
            }
            ArcaneMats.DrawQuad(ArcaneMats.Glow, pa, w * 4f, 0f, color * new Color(1, 1, 1, alpha * 0.9f));
            ArcaneMats.DrawQuad(ArcaneMats.Flare, pa + new Vector3(0, 0.003f, 0), w * 3.5f, now * 4f, Color.Lerp(color2, Color.white, 0.5f) * new Color(1, 1, 1, alpha * 0.7f));
            ArcaneMats.DrawQuad(ArcaneMats.Glow, pb, w * 5.5f, 0f, color * new Color(1, 1, 1, alpha));
            ArcaneMats.DrawQuad(ArcaneMats.Flare, pb + new Vector3(0, 0.003f, 0), w * 5f, -now * 6f, new Color(1f, 1f, 1f, alpha * 0.8f));
        }
    }

    /// <summary>
    /// Fractal lightning: a jagged main channel with forked branches. It re-strikes a few times (the flicker of real
    /// lightning), then leaves a fading afterglow.
    /// </summary>
    public class VfxLightning : Vfx
    {
        private readonly Vector3 a;
        private readonly Vector3 b;
        private readonly Color color;
        private readonly Color color2;
        private readonly float width;
        private readonly List<List<Vector3>> bolts = new List<List<Vector3>>();
        private readonly List<StripMesh> meshes = new List<StripMesh>();
        private readonly List<float> widths = new List<float>();
        private int strikes;
        private int lastGen = -99;
        private bool struck;

        public VfxLightning(Vector3 a, Vector3 b, int ticks, Color color, Color color2, float width)
        {
            this.a = a;
            this.b = b;
            duration = ticks;
            this.color = color;
            this.color2 = color2;
            this.width = width;
        }

        private static void Displace(List<Vector3> pts, Vector3 from, Vector3 to, float offset, int depth)
        {
            if (depth <= 0)
            {
                pts.Add(to);
                return;
            }
            Vector3 mid = (from + to) * 0.5f;
            Vector3 d = to - from;
            Vector3 perp = new Vector3(-d.z, 0f, d.x).normalized;
            mid += perp * VRand.Range(-offset, offset);
            Displace(pts, from, mid, offset * 0.55f, depth - 1);
            Displace(pts, mid, to, offset * 0.55f, depth - 1);
        }

        private void Generate()
        {
            bolts.Clear();
            widths.Clear();
            float len = (b - a).MagnitudeHorizontal();
            int depth = Mathf.Clamp(Mathf.CeilToInt(Mathf.Log(Mathf.Max(2f, len * 3f), 2f)), 2, 6);
            var main = new List<Vector3> { a };
            Displace(main, a, b, Mathf.Min(1.4f, len * 0.16f), depth);
            bolts.Add(main);
            widths.Add(1f);
            int branches = len > 3f ? VRand.RangeInt(1, 4) : 0;
            for (int i = 0; i < branches && main.Count > 4; i++)
            {
                int k = VRand.RangeInt(1, main.Count - 2);
                Vector3 start = main[k];
                Vector3 dir = (b - a).Yto0().normalized;
                float ang = VRand.Range(25f, 60f) * VRand.Sign;
                Vector3 bdir = Quaternion.AngleAxis(ang, Vector3.up) * dir;
                float blen = len * VRand.Range(0.15f, 0.35f);
                var br = new List<Vector3> { start };
                Displace(br, start, start + bdir * blen, blen * 0.2f, Mathf.Max(2, depth - 2));
                bolts.Add(br);
                widths.Add(0.45f);
            }
            while (meshes.Count < bolts.Count) meshes.Add(new StripMesh());
            float y = VAlt.Glow;
            for (int i = 0; i < bolts.Count; i++) meshes[i].Build(bolts[i], width * widths[i], y, widths[i] < 1f ? 0.8f : 0.2f);
        }

        public override void Tick(int now, Map map)
        {
            base.Tick(now, map);
            if (!struck)
            {
                struck = true;
                ArcaneLightFlash.Spawn(map, b, Color.Lerp(color, Color.white, 0.5f), 7f + width * 2f, 8);
                Fx.Sparks(map, b, color, 10f, 6f, 0.11f, 0.35f);
            }
        }

        public override void Draw(float now)
        {
            int tick = Mathf.FloorToInt(now) - startTick;
            if (strikes < 3 && (lastGen < 0 || (tick >= 3 && strikes == 1) || (tick >= 7 && strikes == 2)))
            {
                lastGen = tick;
                strikes++;
                Generate();
            }
            float t = T(now);
            float flicker = t < 0.5f ? ((Mathf.FloorToInt(now * 2f) % 2 == 0) ? 1f : 0.55f) : 1f;
            float alpha = (t < 0.04f ? t / 0.04f : Mathf.Clamp01((1f - t) / 0.7f)) * flicker;
            for (int i = 0; i < bolts.Count && i < meshes.Count; i++)
            {
                meshes[i].Draw(ArcaneMats.Beam, color * new Color(1, 1, 1, alpha * 0.55f));
            }
            float y = VAlt.Glow + 0.004f;
            var core = Color.Lerp(color2, Color.white, 0.75f) * new Color(1, 1, 1, alpha);
            for (int i = 0; i < bolts.Count; i++)
            {
                var pts = bolts[i];
                float w = width * widths[i] * 0.35f;
                for (int k = 0; k < pts.Count - 1; k++)
                {
                    Vector3 p0 = pts[k]; p0.y = y;
                    Vector3 p1 = pts[k + 1]; p1.y = y;
                    ArcaneMats.DrawStretched(ArcaneMats.Beam, p0, p1, w, core);
                }
            }
            Vector3 end = b; end.y = y;
            ArcaneMats.DrawQuad(ArcaneMats.Glow, end, width * 4.5f, 0f, color * new Color(1, 1, 1, alpha));
            ArcaneMats.DrawQuad(ArcaneMats.Flare, end + new Vector3(0, 0.002f, 0), width * 4f, now * 10f, new Color(1f, 1f, 1f, alpha * 0.8f));
        }

        public override void OnEnd()
        {
            foreach (var m in meshes) m.Dispose();
            meshes.Clear();
        }
    }

    /// <summary>A column of light from the sky with a ground flare and rising motes.</summary>
    public class VfxPillar : Vfx
    {
        private readonly Vector3 pos;
        private readonly Color color;
        private readonly Color color2;
        private readonly float width;
        private bool lit;

        public VfxPillar(Vector3 pos, int ticks, Color color, Color color2, float width)
        {
            this.pos = pos;
            duration = ticks;
            this.color = color;
            this.color2 = color2;
            this.width = width;
        }

        public override void Tick(int now, Map map)
        {
            base.Tick(now, map);
            int age = now - startTick;
            if (!lit)
            {
                lit = true;
                ArcaneLightFlash.Spawn(map, pos, color, 4f + width * 2f, duration);
            }
            if (age % 3 == 0) Fx.Rise(map, pos, Color.Lerp(color, Color.white, 0.4f), 1.2f * width, PMat.Mote, width * 0.4f, 3.5f, 0.7f, 0.2f);
        }

        public override void Draw(float now)
        {
            float t = T(now);
            float alpha = FadeInOut(t, 0.08f, 0.6f);
            float y = VAlt.Glow;
            Vector3 bottom = pos; bottom.y = y;
            Vector3 top = bottom + new Vector3(0f, 0f, 16f);
            float w = width * (t < 0.15f ? EaseOutBack(t / 0.15f) * 1.2f : Mathf.Lerp(1.2f, 0.25f, (t - 0.15f) / 0.85f));
            ArcaneMats.DrawStretched(ArcaneMats.Pillar, bottom, top, w * 2.2f, color * new Color(1, 1, 1, alpha * 0.55f));
            ArcaneMats.DrawStretched(ArcaneMats.Pillar, bottom + new Vector3(0, 0.002f, 0), top, w * 0.9f, Color.Lerp(color2, Color.white, 0.4f) * new Color(1, 1, 1, alpha));
            ArcaneMats.DrawStretched(ArcaneMats.Pillar, bottom + new Vector3(0, 0.004f, 0), top, w * 0.3f, new Color(1f, 1f, 1f, alpha));
            ArcaneMats.DrawQuad(ArcaneMats.Glow, bottom, w * 4f, 0f, color * new Color(1, 1, 1, alpha));
            ArcaneMats.DrawQuad(ArcaneMats.Flare, bottom + new Vector3(0, 0.003f, 0), w * 3.5f, now * 3f, Color.Lerp(color2, Color.white, 0.6f) * new Color(1, 1, 1, alpha * 0.8f));
            Vector3 g = bottom; g.y = VAlt.Ground;
            ArcaneMats.DrawQuad(ArcaneMats.Ring, g, w * (2f + t * 5f), 0f, color2 * new Color(1, 1, 1, alpha * 0.8f));
        }
    }

    /// <summary>
    /// A styled projectile: a bright head, a ribbon trail and particles shed along the way according to its school.
    /// Meteors fall steeply with a thick fire-and-smoke trail.
    /// </summary>
    public class VfxProjectile : Vfx
    {
        private readonly Vector3 start;
        private readonly Vector3 end;
        private readonly Material mat;
        private readonly Color color;
        private readonly Color color2;
        private readonly float scale;
        private readonly float arc;
        private readonly bool spin;
        private readonly bool meteor;
        private readonly float angle;
        public VfxStyle style = VfxStyle.Arcane;
        private RibbonTrail trail;
        private RibbonTrail smokeTrail;
        private bool launched;

        public VfxProjectile(Vector3 start, Vector3 end, int ticks, Material mat, Color color, Color color2, float scale, float arc, bool spin, bool meteor)
        {
            this.start = start;
            this.end = end;
            duration = ticks;
            this.mat = mat;
            this.color = color;
            this.color2 = color2;
            this.scale = scale;
            this.arc = arc;
            this.spin = spin;
            this.meteor = meteor;
            angle = (end - start).Yto0().AngleFlat();
        }

        private Vector3 PosAt(float t)
        {
            float tt = meteor ? t * t : t;
            Vector3 p = Vector3.Lerp(start, end, tt);
            if (arc > 0f) p.z += Mathf.Sin(t * Mathf.PI) * arc;
            return p;
        }

        private void EnsureTrails()
        {
            if (trail != null) return;
            float w = scale * (meteor ? 1.6f : 0.75f) * StyleFx.TrailWidth(style);
            trail = new RibbonTrail(meteor ? 26 : 18, w, meteor ? 0.45f : 0.22f, ArcaneMats.Trail, Color.Lerp(color, color2, 0.25f));
            if (meteor || style == VfxStyle.Fire || style == VfxStyle.Shadow || style == VfxStyle.Necro || style == VfxStyle.Void)
            {
                Color sc = meteor || style == VfxStyle.Fire ? new Color(0.25f, 0.22f, 0.2f, 0.55f) : new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.5f);
                smokeTrail = new RibbonTrail(meteor ? 30 : 20, w * (meteor ? 2.6f : 1.8f), meteor ? 1.4f : 0.6f, ArcaneMats.TrailLit, sc) { lit = true, minStep = 0.2f };
            }
        }

        public override void Tick(int now, Map map)
        {
            base.Tick(now, map);
            if (!launched)
            {
                launched = true;
                if (!meteor) StyleFx.Muzzle(style, map, start, angle, color, color2, scale);
            }
            float t = T(now);
            Vector3 p = PosAt(t);
            StyleFx.ProjectileTick(style, map, p, now - startTick, color, color2, scale, meteor);
        }

        public override void Draw(float now)
        {
            EnsureTrails();
            float t = T(now);
            Vector3 p = PosAt(t);
            trail.AddPoint(p, now);
            smokeTrail?.AddPoint(p, now);
            trail.Age(now);
            smokeTrail?.Age(now);
            smokeTrail?.Draw(now);
            trail.Draw(now);
            float y = VAlt.Glow;
            float s = scale * (meteor ? 1.7f : 1f);
            float rot = spin ? (now - startTick) * 18f : angle;
            Vector3 hp = p;
            hp.y = y + 0.003f;
            float pulse = 1f + 0.12f * Mathf.Sin(now * 1.7f);
            ArcaneMats.DrawQuad(ArcaneMats.Glow, hp, s * 3f * pulse, 0f, color * new Color(1, 1, 1, 0.75f));
            ArcaneMats.DrawQuad(ArcaneMats.Glow, hp + new Vector3(0, 0.002f, 0), s * 1.3f, 0f, Color.Lerp(color2, Color.white, 0.6f) * new Color(1, 1, 1, 0.9f));
            StyleFx.ProjectileHead(style, hp, s, now, color, color2);
            ArcaneMats.DrawQuad(mat, hp + new Vector3(0, 0.004f, 0), s, rot, Color.Lerp(color2, Color.white, 0.35f));
        }

        public override void OnEnd()
        {
            trail?.Dispose();
            smokeTrail?.Dispose();
        }
    }

    /// <summary>Particles orbiting a point or thing (used for buffs and channel effects).</summary>
    public class VfxOrbit : Vfx
    {
        public Thing follow;
        public Vector3 pos;
        public Material mat;
        public Color color;
        public int count = 5;
        public float radius = 0.8f;
        public float speed = 6f;
        public float size = 0.35f;

        public override void Draw(float now)
        {
            float t = T(now);
            float alpha = FadeInOut(t, 0.15f, 0.3f);
            Vector3 c = follow != null && follow.Spawned ? follow.DrawPos : pos;
            c.y = VAlt.Glow;
            for (int i = 0; i < count; i++)
            {
                float a = (now - startTick) * speed + i * 360f / count;
                Vector3 off = Quaternion.AngleAxis(a, Vector3.up) * new Vector3(radius, 0f, 0f);
                off.z *= 0.6f;
                ArcaneMats.DrawQuad(ArcaneMats.Glow, c + off, size * 2.2f, 0f, color * new Color(1, 1, 1, alpha * 0.4f));
                ArcaneMats.DrawQuad(mat, c + off + new Vector3(0, 0.002f, 0), size, a, color * new Color(1, 1, 1, alpha));
            }
        }
    }
}

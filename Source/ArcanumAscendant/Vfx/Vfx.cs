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
            if (mat == null) return;
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
        public static readonly Texture2D ManaFill = SolidColorMaterials.NewSolidColorTexture(new Color(0.25f, 0.55f, 1f));
        public static readonly Texture2D ManaBg = SolidColorMaterials.NewSolidColorTexture(new Color(0.05f, 0.07f, 0.15f));
        public static readonly Texture2D WardFill = SolidColorMaterials.NewSolidColorTexture(new Color(0.85f, 0.8f, 0.4f));
        public static readonly Texture2D XpFill = SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.45f, 1f));
        public static readonly Texture2D BarBg = SolidColorMaterials.NewSolidColorTexture(new Color(0.08f, 0.08f, 0.1f));
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

        protected static float FadeInOut(float t, float fadeIn = 0.15f, float fadeOut = 0.4f)
        {
            if (t < fadeIn) return t / fadeIn;
            if (t > 1f - fadeOut) return Mathf.Clamp01((1f - t) / fadeOut);
            return 1f;
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
        public AltitudeLayer layer = AltitudeLayer.MoteOverhead;

        public override void Draw(float now)
        {
            float t = T(now);
            Vector3 p = follow != null && follow.Spawned ? follow.DrawPos : pos;
            p.y = layer.AltitudeFor();
            float s = Mathf.Lerp(scaleStart, scaleEnd, 1f - (1f - t) * (1f - t));
            var c = color;
            c.a *= alpha * FadeInOut(t, fadeIn, fadeOut);
            ArcaneMats.DrawQuad(mat, p, s, angle + spin * (now - startTick), c);
        }
    }

    public class VfxBeam : Vfx
    {
        private readonly Vector3 a;
        private readonly Vector3 b;
        private readonly Material mat;
        private readonly Color color;
        private readonly Color color2;
        private readonly float width;

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
            if ((now - startTick) % 3 == 0)
            {
                float f = Rand.Value;
                ArcaneFx.Particles(map, Vector3.Lerp(a, b, f), color, ArcaneDefOf.AA_Fleck_Spark, 1, 1.2f, 0.8f);
            }
        }

        public override void Draw(float now)
        {
            float t = T(now);
            float alpha = FadeInOut(t, 0.08f, 0.5f);
            float w = width * (0.6f + 0.4f * Mathf.Sin(now * 0.9f)) * (1f - t * 0.5f);
            Vector3 pa = a; pa.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Vector3 pb = b; pb.y = pa.y;
            ArcaneMats.DrawStretched(mat, pa, pb, w * 2.2f, color * new Color(1, 1, 1, alpha * 0.55f));
            ArcaneMats.DrawStretched(mat, pa + new Vector3(0, 0.01f, 0), pb + new Vector3(0, 0.01f, 0), w * 0.8f, Color.Lerp(color2, Color.white, 0.6f) * new Color(1, 1, 1, alpha));
            ArcaneMats.DrawQuad(ArcaneMats.Glow, pa, w * 3f, 0f, color * new Color(1, 1, 1, alpha));
            ArcaneMats.DrawQuad(ArcaneMats.Glow, pb, w * 4f, 0f, color * new Color(1, 1, 1, alpha));
        }
    }

    public class VfxLightning : Vfx
    {
        private readonly Vector3 a;
        private readonly Vector3 b;
        private readonly Color color;
        private readonly Color color2;
        private readonly float width;
        private List<Vector3> points = new List<Vector3>();
        private int lastGen = -99;

        public VfxLightning(Vector3 a, Vector3 b, int ticks, Color color, Color color2, float width)
        {
            this.a = a;
            this.b = b;
            duration = ticks;
            this.color = color;
            this.color2 = color2;
            this.width = width;
        }

        private void Generate()
        {
            points.Clear();
            Vector3 d = b - a;
            float len = d.MagnitudeHorizontal();
            int segs = Mathf.Clamp(Mathf.CeilToInt(len * 1.6f), 3, 60);
            Vector3 perp = new Vector3(-d.z, 0f, d.x).normalized;
            for (int i = 0; i <= segs; i++)
            {
                float f = i / (float)segs;
                Vector3 p = Vector3.Lerp(a, b, f);
                if (i > 0 && i < segs) p += perp * Rand.Range(-0.45f, 0.45f) * Mathf.Min(1f, len / 4f);
                points.Add(p);
            }
        }

        public override void Draw(float now)
        {
            int tick = Mathf.FloorToInt(now);
            if (tick - lastGen >= 2)
            {
                lastGen = tick;
                Generate();
            }
            float t = T(now);
            float alpha = FadeInOut(t, 0.02f, 0.6f);
            float y = AltitudeLayer.MoteOverhead.AltitudeFor();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p0 = points[i]; p0.y = y;
                Vector3 p1 = points[i + 1]; p1.y = y;
                ArcaneMats.DrawStretched(ArcaneMats.Beam, p0, p1, width * 1.4f, color * new Color(1, 1, 1, alpha * 0.6f));
                ArcaneMats.DrawStretched(ArcaneMats.Beam, p0 + new Vector3(0, 0.01f, 0), p1 + new Vector3(0, 0.01f, 0), width * 0.45f, Color.Lerp(color2, Color.white, 0.7f) * new Color(1, 1, 1, alpha));
            }
            Vector3 end = b; end.y = y;
            ArcaneMats.DrawQuad(ArcaneMats.Glow, end, width * 3.5f, 0f, color * new Color(1, 1, 1, alpha));
        }
    }

    public class VfxPillar : Vfx
    {
        private readonly Vector3 pos;
        private readonly Color color;
        private readonly Color color2;
        private readonly float width;

        public VfxPillar(Vector3 pos, int ticks, Color color, Color color2, float width)
        {
            this.pos = pos;
            duration = ticks;
            this.color = color;
            this.color2 = color2;
            this.width = width;
        }

        public override void Draw(float now)
        {
            float t = T(now);
            float alpha = FadeInOut(t, 0.1f, 0.6f);
            float y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Vector3 bottom = pos; bottom.y = y;
            Vector3 top = bottom + new Vector3(0f, 0f, 14f);
            float w = width * (t < 0.2f ? Mathf.Lerp(0.2f, 1.2f, t / 0.2f) : Mathf.Lerp(1.2f, 0.3f, (t - 0.2f) / 0.8f));
            ArcaneMats.DrawStretched(ArcaneMats.Pillar, bottom, top, w * 1.8f, color * new Color(1, 1, 1, alpha * 0.7f));
            ArcaneMats.DrawStretched(ArcaneMats.Pillar, bottom + new Vector3(0, 0.01f, 0), top, w * 0.6f, Color.Lerp(color2, Color.white, 0.5f) * new Color(1, 1, 1, alpha));
            ArcaneMats.DrawQuad(ArcaneMats.Glow, bottom, w * 3.5f, 0f, color * new Color(1, 1, 1, alpha));
            ArcaneMats.DrawQuad(ArcaneMats.Ring, bottom, w * (2f + t * 4f), 0f, color2 * new Color(1, 1, 1, alpha * 0.8f));
        }
    }

    /// <summary>A glowing projectile with a ghost trail and ember particles.</summary>
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

        public override void Tick(int now, Map map)
        {
            base.Tick(now, map);
            float t = T(now);
            if (map != null && (now - startTick) % 2 == 0)
            {
                Vector3 p = PosAt(t);
                ArcaneFx.Particles(map, p, color, meteor ? ArcaneDefOf.AA_Fleck_Ember : ArcaneDefOf.AA_Fleck_Mote, meteor ? 2 : 1, meteor ? 0.8f : 0.4f, scale * (meteor ? 1.1f : 0.6f));
            }
        }

        public override void Draw(float now)
        {
            float t = T(now);
            float y = AltitudeLayer.MoteOverhead.AltitudeFor();
            float s = scale * (meteor ? 1.6f : 1f);
            float rot = spin ? (now - startTick) * 18f : angle;
            for (int i = 6; i >= 1; i--)
            {
                float tg = t - i * 0.035f;
                if (tg < 0f) continue;
                Vector3 g = PosAt(tg); g.y = y;
                float f = 1f - i / 7f;
                ArcaneMats.DrawQuad(ArcaneMats.Glow, g, s * (0.4f + 0.8f * f), 0f, color * new Color(1, 1, 1, 0.45f * f));
            }
            Vector3 p = PosAt(t); p.y = y + 0.01f;
            ArcaneMats.DrawQuad(ArcaneMats.Glow, p, s * 2.2f, 0f, color * new Color(1, 1, 1, 0.8f));
            ArcaneMats.DrawQuad(mat, p + new Vector3(0, 0.01f, 0), s, rot, Color.Lerp(color2, Color.white, 0.35f));
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
            c.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            for (int i = 0; i < count; i++)
            {
                float a = (now - startTick) * speed + i * 360f / count;
                Vector3 off = Quaternion.AngleAxis(a, Vector3.up) * new Vector3(radius, 0f, 0f);
                off.z *= 0.6f;
                ArcaneMats.DrawQuad(mat, c + off, size, a, color * new Color(1, 1, 1, alpha));
            }
        }
    }
}

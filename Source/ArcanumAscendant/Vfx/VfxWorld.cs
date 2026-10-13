using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Ground decals
    // ------------------------------------------------------------------------------------------------

    public enum DecalKind
    {
        Scorch,
        Crack,
        Frost,
        Blood,
        Rot,
        Holy,
        RuneBurn,
        Void,
        Crater,
        Ash,
        Leaves,
        Glyph,
    }

    public struct ArcaneDecal
    {
        public Vector3 pos;
        public float size, angle, age, life;
        public int mat;
        public Color color;
        public bool glow;
        public bool grow;
    }

    [StaticConstructorOnStartup]
    public static class DecalMats
    {
        private static readonly Dictionary<DecalKind, (int mat, bool glow)> map = new Dictionary<DecalKind, (int, bool)>();

        static DecalMats()
        {
            map[DecalKind.Scorch] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Scorch", false), false);
            map[DecalKind.Crack] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Crack", false), false);
            map[DecalKind.Frost] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Frost", false), false);
            map[DecalKind.Blood] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Blood", false), false);
            map[DecalKind.Rot] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Rot", false), false);
            map[DecalKind.Holy] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Sigil", true), true);
            map[DecalKind.RuneBurn] = (PMat.Register("ArcanumAscendant/Vfx/RuneCircle", true), true);
            map[DecalKind.Void] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Void", false), false);
            map[DecalKind.Crater] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Crater", false), false);
            map[DecalKind.Ash] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Ash", false), false);
            map[DecalKind.Leaves] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Leaves", false), false);
            map[DecalKind.Glyph] = (PMat.Register("ArcanumAscendant/Vfx/Decal_Glyph", true), true);
        }

        public static (int mat, bool glow) Get(DecalKind k) => map.TryGetValue(k, out var v) ? v : map[DecalKind.Scorch];
    }

    /// <summary>Scorch marks, frost, cracks and sigils left on the ground. They fade out on their own.</summary>
    public class ArcaneDecalSystem
    {
        public const int Max = 160;
        private readonly List<ArcaneDecal> decals = new List<ArcaneDecal>();

        public void Add(Vector3 pos, DecalKind kind, float size, Color color, float life)
        {
            if (!ArcanumMod.Settings.decals || size <= 0.05f) return;
            var (mat, glow) = DecalMats.Get(kind);
            if (decals.Count >= Max) decals.RemoveAt(0);
            decals.Add(new ArcaneDecal
            {
                pos = pos, size = size, angle = VRand.Range(0f, 360f), life = life, mat = mat, color = color, glow = glow,
                grow = kind == DecalKind.Frost || kind == DecalKind.Holy || kind == DecalKind.Glyph || kind == DecalKind.RuneBurn,
            });
        }

        public void Update(float dt)
        {
            if (dt <= 0f) return;
            for (int i = decals.Count - 1; i >= 0; i--)
            {
                var d = decals[i];
                d.age += dt;
                if (d.age >= d.life) decals.RemoveAt(i);
                else decals[i] = d;
            }
        }

        public void Draw(CellRect view)
        {
            if (decals.Count == 0) return;
            var mpb = ArcaneMats.MPB;
            var mesh = MeshPool.plane10;
            float baseY = VAlt.Decal, glowY = VAlt.Ground;
            for (int i = 0; i < decals.Count; i++)
            {
                var d = decals[i];
                if (d.pos.x < view.minX - d.size || d.pos.x > view.maxX + d.size || d.pos.z < view.minZ - d.size || d.pos.z > view.maxZ + d.size) continue;
                float t = d.age / d.life;
                float a = Mathf.Clamp01(d.age / 0.12f) * (1f - Mathf.SmoothStep(0.55f, 1f, t));
                if (d.glow) a *= 1f - t * 0.6f;
                float s = d.grow ? d.size * (0.7f + 0.3f * Mathf.Clamp01(d.age / 0.35f)) : d.size;
                Vector3 p = d.pos;
                p.y = (d.glow ? glowY : baseY) + 0.0002f * i;
                var c = d.color;
                c.a *= a;
                mpb.SetColor(ShaderPropertyIDs.Color, c);
                Graphics.DrawMesh(mesh, Matrix4x4.TRS(p, Quaternion.AngleAxis(d.angle + (d.glow ? d.age * 6f : 0f), Vector3.up), new Vector3(s, 1f, s)),
                    PMat.Get(d.mat), 0, null, 0, mpb);
            }
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Dynamic light flashes
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// An invisible thing carrying a glower, spawned for a moment so spells really light up their surroundings at night.
    /// </summary>
    public class ArcaneLightFlash : ThingWithComps
    {
        public int ticksLeft = 30;

        private static readonly List<ArcaneLightFlash> live = new List<ArcaneLightFlash>();

        public static void ResetLive() => live.Clear();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            live.Add(this);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            live.Remove(this);
            base.DeSpawn(mode);
        }

        protected override void Tick()
        {
            base.Tick();
            if (--ticksLeft <= 0 && !Destroyed) Destroy();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 30);
        }

        /// <summary>Spawns a coloured light for a number of ticks. Returns null when lights are disabled or capped.</summary>
        public static ArcaneLightFlash Spawn(Map map, Vector3 pos, Color color, float radius, int ticks)
        {
            if (map == null || !ArcanumMod.Settings.dynamicLights || ticks <= 0 || radius < 1f) return null;
            live.RemoveAll(l => l == null || l.Destroyed || !l.Spawned);
            if (live.Count >= 10) return null;
            var def = ArcaneDefOf.AA_LightFlash;
            if (def == null) return null;
            var cell = pos.ToIntVec3();
            if (!cell.InBounds(map)) return null;
            ArcaneLightFlash t;
            try
            {
                t = (ArcaneLightFlash)ThingMaker.MakeThing(def);
                t.ticksLeft = ticks;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Light flash failed; disabling dynamic lights for this session: " + e.Message, 0x5A1C0E5);
                ArcanumMod.Settings.dynamicLights = false;
                return null;
            }
            var g = t.GetComp<CompGlower>();
            float r = Mathf.Clamp(radius, 1f, 14f);
            var ci = ColorInt(color);
            // Prefer configuring the glower before it registers with the glow grid; if the setters need a spawned
            // parent, apply them again once it is on the map. A failed setter just leaves the def's default light.
            bool applied = TryConfigure(g, r, ci);
            try
            {
                GenSpawn.Spawn(t, cell, map);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Light flash failed to spawn; disabling dynamic lights for this session: " + e.Message, 0x5A1C0E6);
                ArcanumMod.Settings.dynamicLights = false;
                return null;
            }
            if (!applied) TryConfigure(g, r, ci);
            return t;
        }

        private static bool TryConfigure(CompGlower g, float radius, ColorInt color)
        {
            if (g == null) return true;
            try
            {
                g.GlowRadius = radius;
                g.GlowColor = color;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static ColorInt ColorInt(Color c)
        {
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (m > 0.01f) c = new Color(c.r / m, c.g / m, c.b / m);
            return new ColorInt(Mathf.RoundToInt(c.r * 255f), Mathf.RoundToInt(c.g * 255f), Mathf.RoundToInt(c.b * 255f), 0);
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Screen effects and combat text
    // ------------------------------------------------------------------------------------------------

    [StaticConstructorOnStartup]
    public static class ScreenTex
    {
        public static readonly Texture2D Vignette = ContentFinder<Texture2D>.Get("ArcanumAscendant/UI/Vignette", false) ?? BaseContent.ClearTex;
        public static readonly Texture2D White = BaseContent.WhiteTex;
        public static readonly Texture2D Black = BaseContent.BlackTex;
    }

    /// <summary>Screen flashes, vignettes, cinematic bars for ultimates, and floating damage numbers.</summary>
    public class ArcaneScreenFx
    {
        private struct Flash
        {
            public Color color;
            public float strength;
            public float start;
            public float duration;
            public bool vignette;
        }

        private struct Banner
        {
            public string text;
            public string sub;
            public Color color;
            public float start;
        }

        private struct FloatText
        {
            public Vector3 pos;
            public string text;
            public Color color;
            public float start;
            public float duration;
            public float scale;
            public float driftX;
        }

        private readonly List<Flash> flashes = new List<Flash>();
        private readonly List<FloatText> texts = new List<FloatText>();
        private Banner? banner;
        private const float BannerTime = 150f;

        public void AddFlash(Color color, float strength, float durationTicks, bool vignette, float now)
        {
            if (!ArcanumMod.Settings.screenFlashes || strength <= 0.01f) return;
            if (flashes.Count > 12) flashes.RemoveAt(0);
            flashes.Add(new Flash { color = color, strength = Mathf.Clamp01(strength), start = now, duration = Mathf.Max(4f, durationTicks), vignette = vignette });
        }

        public void ShowBanner(string text, string sub, Color color, float now)
        {
            if (!ArcanumMod.Settings.cinematicUltimates) return;
            banner = new Banner { text = text, sub = sub, color = color, start = now };
        }

        public void AddText(Vector3 pos, string text, Color color, float scale, float now, float duration = 70f, bool always = false)
        {
            if ((!always && !ArcanumMod.Settings.damageNumbers) || text.NullOrEmpty()) return;
            if (texts.Count > 90) texts.RemoveAt(0);
            texts.Add(new FloatText { pos = pos, text = text, color = color, start = now, duration = duration, scale = scale, driftX = VRand.Range(-0.35f, 0.35f) });
        }

        public void OnGUI(Map map, float now)
        {
            if (Event.current.type != EventType.Repaint) return;
            DrawTexts(now);
            DrawFlashes(now);
            DrawBanner(now);
        }

        private void DrawFlashes(float now)
        {
            if (flashes.Count == 0) return;
            var full = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
            var old = GUI.color;
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                var f = flashes[i];
                float t = (now - f.start) / f.duration;
                if (t >= 1f || t < -0.01f)
                {
                    flashes.RemoveAt(i);
                    continue;
                }
                float a = f.strength * (1f - t) * (1f - t);
                if (f.vignette)
                {
                    GUI.color = new Color(f.color.r, f.color.g, f.color.b, a);
                    GUI.DrawTexture(full, ScreenTex.Vignette, ScaleMode.StretchToFill, true);
                }
                else
                {
                    GUI.color = new Color(f.color.r, f.color.g, f.color.b, a * 0.55f);
                    GUI.DrawTexture(full, ScreenTex.White);
                }
            }
            GUI.color = old;
        }

        private void DrawBanner(float now)
        {
            if (banner == null) return;
            var b = banner.Value;
            float t = (now - b.start) / BannerTime;
            if (t >= 1f || t < -0.01f)
            {
                banner = null;
                return;
            }
            var old = GUI.color;
            float w = UI.screenWidth, h = UI.screenHeight;
            float barIn = Mathf.Clamp01(t / 0.12f), barOut = Mathf.Clamp01((1f - t) / 0.18f);
            float bar = h * 0.085f * Mathf.Min(barIn, barOut);
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0f, 0f, w, bar), ScreenTex.Black);
            GUI.DrawTexture(new Rect(0f, h - bar, w, bar), ScreenTex.Black);
            float ta = Mathf.Clamp01((t - 0.06f) / 0.1f) * Mathf.Clamp01((1f - t) / 0.25f);
            if (ta > 0.01f)
            {
                var oldAnchor = Text.Anchor;
                var oldFont = Text.Font;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                float slide = (1f - Mathf.Clamp01((t - 0.06f) / 0.14f)) * 60f;
                var r = new Rect(slide, h * 0.2f, w, 40f);
                GUI.color = new Color(0f, 0f, 0f, ta * 0.8f);
                Widgets.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), b.text);
                GUI.color = new Color(Mathf.Lerp(b.color.r, 1f, 0.35f), Mathf.Lerp(b.color.g, 1f, 0.35f), Mathf.Lerp(b.color.b, 1f, 0.35f), ta);
                Widgets.Label(r, b.text);
                if (!b.sub.NullOrEmpty())
                {
                    Text.Font = GameFont.Small;
                    GUI.color = new Color(1f, 1f, 1f, ta * 0.75f);
                    Widgets.Label(new Rect(-slide, r.yMax, w, 24f), b.sub);
                }
                float line = w * 0.35f * Mathf.Clamp01((t - 0.06f) / 0.2f);
                GUI.color = new Color(b.color.r, b.color.g, b.color.b, ta * 0.8f);
                GUI.DrawTexture(new Rect((w - line) / 2f, r.y - 4f, line, 2f), ScreenTex.White);
                GUI.DrawTexture(new Rect((w - line) / 2f, r.yMax + 26f, line, 2f), ScreenTex.White);
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
            }
            GUI.color = old;
        }

        private void DrawTexts(float now)
        {
            if (texts.Count == 0) return;
            var oldAnchor = Text.Anchor;
            var oldFont = Text.Font;
            var old = GUI.color;
            Text.Anchor = TextAnchor.MiddleCenter;
            float zoom = Find.CameraDriver.ZoomRootSize;
            bool farAway = zoom > 45f;
            for (int i = texts.Count - 1; i >= 0; i--)
            {
                var f = texts[i];
                float t = (now - f.start) / f.duration;
                if (t >= 1f || t < -0.01f)
                {
                    texts.RemoveAt(i);
                    continue;
                }
                if (farAway) continue;
                float rise = 0.55f + 1.1f * (1f - (1f - t) * (1f - t));
                Vector3 wp = f.pos + new Vector3(f.driftX * t, 0f, rise);
                Vector2 sp = UI.MapToUIPosition(wp);
                if (sp.x < -50f || sp.y < -50f || sp.x > UI.screenWidth + 50f || sp.y > UI.screenHeight + 50f) continue;
                float pop = t < 0.12f ? Mathf.Lerp(1.7f, 1f, t / 0.12f) : 1f;
                float a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                Text.Font = f.scale >= 1.25f ? GameFont.Medium : GameFont.Small;
                float s = pop * Mathf.Clamp(f.scale, 0.6f, 1.6f) * (f.scale >= 1.25f ? 0.9f : 1f);
                var size = Text.CalcSize(f.text);
                var rect = new Rect(sp.x - size.x / 2f - 4f, sp.y - size.y / 2f, size.x + 8f, size.y);
                var mtx = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(s, s), sp);
                GUI.color = new Color(0f, 0f, 0f, a * 0.85f);
                Widgets.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height), f.text);
                Widgets.Label(new Rect(rect.x - 1f, rect.y + 1f, rect.width, rect.height), f.text);
                GUI.color = new Color(f.color.r, f.color.g, f.color.b, a);
                Widgets.Label(rect, f.text);
                GUI.matrix = mtx;
            }
            GUI.color = old;
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Ribbon trails
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// A smooth trail behind a moving point, built as a tapering mesh strip each frame. One draw call per trail.
    /// The trail texture fades along its length, so the tail dissolves without needing vertex colours.
    /// </summary>
    public class RibbonTrail
    {
        private readonly Vector3[] points;
        private readonly float[] times;
        private int count;
        private Mesh mesh;
        private Vector3[] verts;
        private Vector2[] uvs;
        public float width;
        public float lifetime;
        public Material mat;
        public Color color;
        public float minStep = 0.12f;
        public bool lit;

        public RibbonTrail(int maxPoints, float width, float lifetime, Material mat, Color color)
        {
            points = new Vector3[Mathf.Max(3, maxPoints)];
            times = new float[points.Length];
            this.width = width;
            this.lifetime = lifetime;
            this.mat = mat;
            this.color = color;
        }

        public void AddPoint(Vector3 p, float now)
        {
            p.y = 0f;
            if (count > 0 && (points[count - 1] - p).sqrMagnitude < minStep * minStep)
            {
                points[count - 1] = p;
                times[count - 1] = now;
                return;
            }
            if (count == points.Length)
            {
                Array.Copy(points, 1, points, 0, count - 1);
                Array.Copy(times, 1, times, 0, count - 1);
                count--;
            }
            points[count] = p;
            times[count] = now;
            count++;
        }

        /// <summary>Drops points older than the lifetime. Returns false once the trail has fully faded.</summary>
        public bool Age(float now)
        {
            int drop = 0;
            while (drop < count && now - times[drop] > lifetime) drop++;
            if (drop > 0)
            {
                Array.Copy(points, drop, points, 0, count - drop);
                Array.Copy(times, drop, times, 0, count - drop);
                count -= drop;
            }
            return count > 1;
        }

        public void Draw(float now, float alpha = 1f)
        {
            if (count < 2 || mat == null || alpha <= 0.01f) return;
            int n = count;
            if (mesh == null)
            {
                mesh = new Mesh { name = "AA_Ribbon" };
                mesh.MarkDynamic();
                verts = new Vector3[points.Length * 2];
                uvs = new Vector2[points.Length * 2];
                var tris = new int[(points.Length - 1) * 6];
                for (int i = 0; i < points.Length - 1; i++)
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
            float y = lit ? VAlt.Lit : VAlt.Glow;
            for (int i = 0; i < points.Length; i++)
            {
                int j = Mathf.Min(i, n - 1);
                Vector3 p = points[j];
                Vector3 d = j < n - 1 ? points[j + 1] - points[j] : points[j] - points[j - 1];
                d.y = 0f;
                Vector3 perp = new Vector3(-d.z, 0f, d.x);
                float m = perp.magnitude;
                perp = m > 0.0001f ? perp / m : Vector3.right;
                float u = n > 1 ? j / (float)(n - 1) : 1f;
                float age = Mathf.Clamp01((now - times[j]) / Mathf.Max(0.01f, lifetime));
                float w = width * (0.12f + 0.88f * u) * (1f - age * 0.5f);
                p.y = y;
                verts[i * 2] = p + perp * w * 0.5f;
                verts[i * 2 + 1] = p - perp * w * 0.5f;
                float uu = u * (1f - age * 0.35f);
                uvs[i * 2] = new Vector2(uu, 1f);
                uvs[i * 2 + 1] = new Vector2(uu, 0f);
            }
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.RecalculateBounds();
            var c = color;
            c.a *= alpha;
            ArcaneMats.MPB.SetColor(ShaderPropertyIDs.Color, c);
            Graphics.DrawMesh(mesh, Matrix4x4.identity, mat, 0, null, 0, ArcaneMats.MPB);
        }

        public void Dispose()
        {
            if (mesh != null)
            {
                UnityEngine.Object.Destroy(mesh);
                mesh = null;
            }
        }
    }
}

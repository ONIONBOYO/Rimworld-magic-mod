using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>Random numbers for visuals only, so effects never disturb the game's own random sequence.</summary>
    public static class VRand
    {
        public static float Value => UnityEngine.Random.value;
        public static float Range(float a, float b) => UnityEngine.Random.Range(a, b);
        public static int RangeInt(int a, int bExclusive) => UnityEngine.Random.Range(a, bExclusive);
        public static bool Chance(float c) => UnityEngine.Random.value < c;
        public static float Sign => UnityEngine.Random.value < 0.5f ? -1f : 1f;

        public static Vector3 InsideCircle(float r)
        {
            var v = UnityEngine.Random.insideUnitCircle * r;
            return new Vector3(v.x, 0f, v.y);
        }

        public static Vector3 OnCircle(float r)
        {
            float a = UnityEngine.Random.value * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        public static Vector3 Dir(float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        /// <summary>Rounds a fractional count randomly, so 0.4 particles means a 40% chance of one.</summary>
        public static int Count(float n)
        {
            if (n <= 0f) return 0;
            int i = Mathf.FloorToInt(n);
            return i + (UnityEngine.Random.value < n - i ? 1 : 0);
        }
    }

    /// <summary>Altitudes used by the effect system.</summary>
    public static class VAlt
    {
        /// <summary>Above the lighting overlay: emissive effects stay bright at night.</summary>
        public static float Glow => AltitudeLayer.VisEffects.AltitudeFor();
        /// <summary>Above pawns but under the lighting: smoke, dust and debris darken at night like real matter.</summary>
        public static float Lit => AltitudeLayer.MoteOverhead.AltitudeFor();
        /// <summary>On the ground under pawns.</summary>
        public static float Ground => AltitudeLayer.MoteLow.AltitudeFor();
        /// <summary>Ground decals (scorch marks, frost, blood).</summary>
        public static float Decal => AltitudeLayer.Filth.AltitudeFor();
    }

    [Flags]
    public enum PFlags : ushort
    {
        None = 0,
        /// <summary>Soft shadow on the ground under a particle with height.</summary>
        Shadow = 1,
        /// <summary>Bounces when it hits the ground (debris, shards).</summary>
        Bounce = 2,
        /// <summary>Drawn on the ground under pawns.</summary>
        Ground = 4,
        /// <summary>Stretched along its on-screen velocity (sparks, streaks).</summary>
        Stretch = 8,
        /// <summary>Rotated to face its velocity.</summary>
        AlignVel = 16,
        /// <summary>Alpha flickers like a flame.</summary>
        Flicker = 32,
        /// <summary>Occasional bright glint (ice, crystal, gold).</summary>
        Glint = 64,
        /// <summary>Sways from side to side (leaves, feathers, notes).</summary>
        Wobble = 128,
        /// <summary>Drawn under the lighting overlay instead of glowing.</summary>
        Lit = 256,
        /// <summary>Size pulses in and out.</summary>
        Pulse = 512,
    }

    public struct ArcaneParticle
    {
        public Vector3 pos;
        public Vector3 vel;
        public float h, vh;
        public float age, life;
        public float size0, size1;
        public float rot, spin;
        public Color c0, c1;
        public float drag, gravity, stretch, turb;
        public Vector3 attract;
        public float attractK;
        public float swirl;
        public int mat;
        public PFlags flags;
        public float seed;
        public byte bounces;
    }

    /// <summary>Particle textures. Index into <see cref="Get"/>.</summary>
    [StaticConstructorOnStartup]
    public static class PMat
    {
        private static readonly List<Material> mats = new List<Material>();
        private static readonly Dictionary<string, int> byName = new Dictionary<string, int>();

        public static readonly int Glow, Spark, Streak, Ember, Mote, Smoke, SmokeDark, Dust, Shard, Snow, Leaf, Petal, Feather, Note, Rune, Skull, Bone,
            Bolt, Star, Ring, Clock, Debris, Debris2, Shadow, Blood, BloodAlpha, Wisp, Flame, Bubble, Gear, Flare, Ash, IceChunk, Gold, Heart, Crystal,
            Gust, Glitch, Eye, Seal, Crow, FeatherDark, Kiss, Smile, Tube, Confetti;

        static PMat()
        {
            Glow = Register("ArcanumAscendant/Vfx/Glow", true);
            Spark = Register("ArcanumAscendant/Vfx/Spark", true);
            Streak = Register("ArcanumAscendant/Vfx/Streak", true);
            Ember = Register("ArcanumAscendant/Vfx/Ember", true);
            Mote = Register("ArcanumAscendant/Vfx/Mote", true);
            Smoke = Register("ArcanumAscendant/Vfx/SmokePuff", false);
            SmokeDark = Register("ArcanumAscendant/Vfx/SmokePuff2", false);
            Dust = Register("ArcanumAscendant/Vfx/Dust", false);
            Shard = Register("ArcanumAscendant/Vfx/Shard", true);
            Snow = Register("ArcanumAscendant/Vfx/Snow", true);
            Leaf = Register("ArcanumAscendant/Vfx/Leaf", false);
            Petal = Register("ArcanumAscendant/Vfx/Petal", true);
            Feather = Register("ArcanumAscendant/Vfx/Feather", true);
            Note = Register("ArcanumAscendant/Vfx/Note", true);
            Rune = Register("ArcanumAscendant/Vfx/Rune", true);
            Skull = Register("ArcanumAscendant/Vfx/Skull", true);
            Bone = Register("ArcanumAscendant/Vfx/BoneChip", false);
            Bolt = Register("ArcanumAscendant/Vfx/Bolt", true);
            Star = Register("ArcanumAscendant/Vfx/Star", true);
            Ring = Register("ArcanumAscendant/Vfx/Ring", true);
            Clock = Register("ArcanumAscendant/Vfx/Clock", true);
            Debris = Register("ArcanumAscendant/Vfx/Debris", false);
            Debris2 = Register("ArcanumAscendant/Vfx/Debris2", false);
            Shadow = Register("ArcanumAscendant/Vfx/Shadow", false);
            Blood = Register("ArcanumAscendant/Vfx/BloodDrop", true);
            BloodAlpha = Register("ArcanumAscendant/Vfx/BloodDrop", false);
            Wisp = Register("ArcanumAscendant/Vfx/Wisp", true);
            Flame = Register("ArcanumAscendant/Vfx/Flame", true);
            Bubble = Register("ArcanumAscendant/Vfx/Bubble", true);
            Gear = Register("ArcanumAscendant/Vfx/Gear", true);
            Flare = Register("ArcanumAscendant/Vfx/Flare", true);
            Ash = Register("ArcanumAscendant/Vfx/Ash", false);
            IceChunk = Register("ArcanumAscendant/Vfx/IceChunk", false);
            Gold = Register("ArcanumAscendant/Vfx/GoldFleck", true);
            Heart = Register("ArcanumAscendant/Vfx/Heart", true);
            Crystal = Register("ArcanumAscendant/Vfx/Crystal", true);
            Gust = Register("ArcanumAscendant/Vfx/Gust", true);
            Glitch = Register("ArcanumAscendant/Vfx/Glitch", true);
            Eye = Register("ArcanumAscendant/Vfx/OmenEye", true);
            Seal = Register("ArcanumAscendant/Vfx/OmenSeal", true);
            Crow = Register("ArcanumAscendant/Vfx/Crow", false);
            FeatherDark = Register("ArcanumAscendant/Vfx/Feather", false);
            Kiss = Register("ArcanumAscendant/Vfx/Kiss", true);
            Smile = Register("ArcanumAscendant/Vfx/Smile", true);
            Tube = Register("ArcanumAscendant/Vfx/Tube", true);
            Confetti = Register("ArcanumAscendant/Vfx/Confetti", false);
        }

        public static int Register(string path, bool additive)
        {
            string key = path + (additive ? "+" : "-");
            if (byName.TryGetValue(key, out int i)) return i;
            mats.Add(ArcaneMats.Get(path, additive));
            byName[key] = mats.Count - 1;
            return mats.Count - 1;
        }

        public static Material Get(int i) => i >= 0 && i < mats.Count ? mats[i] : mats[0];
    }

    /// <summary>
    /// A lightweight CPU particle system. Particles live in a flat array, are simulated in game time (they freeze when the
    /// game is paused and speed up with the game) and are culled to the camera view before drawing.
    /// </summary>
    public class ArcaneParticleSystem
    {
        public const int Max = 2400;
        private readonly ArcaneParticle[] parts = new ArcaneParticle[Max];
        private int count;

        public int Count => count;

        public void Emit(ArcaneParticle p)
        {
            if (count >= Max) return;
            if (p.life <= 0f) p.life = 1f;
            p.seed = VRand.Range(0f, 100f);
            parts[count++] = p;
        }

        public void Clear() => count = 0;

        public void Update(float dt)
        {
            if (dt <= 0f) return;
            for (int i = count - 1; i >= 0; i--)
            {
                ref var q = ref parts[i];
                q.age += dt;
                if (q.age >= q.life)
                {
                    parts[i] = parts[--count];
                    continue;
                }
                if (q.drag > 0f)
                {
                    float k = Mathf.Exp(-q.drag * dt);
                    q.vel *= k;
                }
                if (q.turb > 0f)
                {
                    float t = q.age * 1.7f + q.seed;
                    q.vel.x += (Mathf.PerlinNoise(t, q.seed) - 0.5f) * q.turb * dt * 10f;
                    q.vel.z += (Mathf.PerlinNoise(q.seed + 31f, t) - 0.5f) * q.turb * dt * 10f;
                }
                if (q.attractK != 0f || q.swirl != 0f)
                {
                    Vector3 d = q.attract - q.pos;
                    d.y = 0f;
                    float dist = d.magnitude;
                    if (dist > 0.05f)
                    {
                        Vector3 n = d / dist;
                        q.vel += n * q.attractK * dt;
                        if (q.swirl != 0f) q.vel += new Vector3(-n.z, 0f, n.x) * q.swirl * dt;
                    }
                    else if (q.attractK > 0f)
                    {
                        q.age = Mathf.Max(q.age, q.life * 0.9f);
                    }
                }
                q.pos += q.vel * dt;
                if (q.gravity != 0f || q.vh != 0f || q.h > 0f)
                {
                    q.vh -= q.gravity * dt;
                    q.h += q.vh * dt;
                    if (q.h < 0f)
                    {
                        if ((q.flags & PFlags.Bounce) != 0 && q.bounces < 2 && q.vh < -0.6f)
                        {
                            q.h = 0f;
                            q.vh = -q.vh * 0.32f;
                            q.vel *= 0.45f;
                            q.spin *= 0.5f;
                            q.bounces++;
                        }
                        else if (q.gravity > 0f)
                        {
                            q.h = 0f;
                            q.vh = 0f;
                            q.vel *= Mathf.Exp(-8f * dt);
                            q.spin *= Mathf.Exp(-8f * dt);
                        }
                    }
                }
                q.rot += q.spin * dt;
            }
        }

        public void Draw(CellRect view)
        {
            if (count == 0) return;
            float yGlow = VAlt.Glow, yLit = VAlt.Lit, yGround = VAlt.Ground;
            int minX = view.minX - 2, maxX = view.maxX + 2, minZ = view.minZ - 2, maxZ = view.maxZ + 4;
            var mpb = ArcaneMats.MPB;
            var mesh = MeshPool.plane10;
            var shadowMat = PMat.Get(PMat.Shadow);
            for (int i = 0; i < count; i++)
            {
                ref var q = ref parts[i];
                if (q.pos.x < minX || q.pos.x > maxX || q.pos.z < minZ || q.pos.z > maxZ) continue;
                float t = q.age / q.life;
                float e = 1f - (1f - t) * (1f - t);
                float size = Mathf.LerpUnclamped(q.size0, q.size1, e);
                if ((q.flags & PFlags.Pulse) != 0) size *= 0.85f + 0.15f * Mathf.Sin(q.age * 9f + q.seed);
                if (size <= 0.005f) continue;
                Color c = Color.LerpUnclamped(q.c0, q.c1, t);
                float fin = Mathf.Clamp01(q.age / Mathf.Max(0.02f, Mathf.Min(0.08f, q.life * 0.2f)));
                c.a *= fin;
                if ((q.flags & PFlags.Flicker) != 0) c.a *= 0.55f + 0.45f * Mathf.PerlinNoise(q.seed * 3f, q.age * 14f);
                if ((q.flags & PFlags.Glint) != 0)
                {
                    float g = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(q.age * 7f + q.seed)), 24f);
                    c = Color.Lerp(c, new Color(1f, 1f, 1f, c.a), g);
                    size *= 1f + g * 0.6f;
                }
                if (c.a <= 0.004f) continue;
                Vector3 dp = q.pos;
                dp.z += q.h;
                if ((q.flags & PFlags.Wobble) != 0) dp.x += Mathf.Sin(q.age * 3.5f + q.seed) * 0.18f;
                dp.y = (q.flags & PFlags.Ground) != 0 ? yGround : (q.flags & PFlags.Lit) != 0 ? yLit : yGlow;
                Quaternion rot;
                Vector3 scale;
                if ((q.flags & (PFlags.Stretch | PFlags.AlignVel)) != 0)
                {
                    float sx = q.vel.x, sz = q.vel.z + q.vh;
                    float sp = Mathf.Sqrt(sx * sx + sz * sz);
                    rot = sp > 0.01f ? Quaternion.LookRotation(new Vector3(sx, 0f, sz)) : Quaternion.AngleAxis(q.rot, Vector3.up);
                    float len = (q.flags & PFlags.Stretch) != 0 ? size * (1f + sp * q.stretch) : size;
                    scale = new Vector3(size, 1f, len);
                }
                else
                {
                    rot = Quaternion.AngleAxis(q.rot, Vector3.up);
                    scale = new Vector3(size, 1f, size);
                }
                if ((q.flags & PFlags.Shadow) != 0 && q.h > 0.04f)
                {
                    Vector3 sp = q.pos;
                    sp.y = yGround;
                    float ss = size * Mathf.Clamp(1.1f - q.h * 0.12f, 0.4f, 1.1f);
                    mpb.SetColor(ShaderPropertyIDs.Color, new Color(0f, 0f, 0f, 0.38f * fin * (1f - t * t)));
                    Graphics.DrawMesh(mesh, Matrix4x4.TRS(sp, Quaternion.identity, new Vector3(ss, 1f, ss * 0.6f)), shadowMat, 0, null, 0, mpb);
                }
                mpb.SetColor(ShaderPropertyIDs.Color, c);
                Graphics.DrawMesh(mesh, Matrix4x4.TRS(dp, rot, scale), PMat.Get(q.mat), 0, null, 0, mpb);
            }
        }
    }

    /// <summary>
    /// Particle emitters. Counts are scaled by the effect-intensity setting and by camera zoom, so effects stay readable up close
    /// and cheap when zoomed out.
    /// </summary>
    public static class Fx
    {
        public static ArcaneParticleSystem Sys(Map map) => map == null ? null : ArcaneVfxManager.For(map)?.particles;

        /// <summary>Level of detail for the current camera and settings.</summary>
        public static float Lod(Map map)
        {
            if (map == null || map != Find.CurrentMap) return 0f;
            float k = ArcanumMod.Settings.vfxIntensity;
            switch (Find.CameraDriver.CurrentZoom)
            {
                case CameraZoomRange.Closest:
                case CameraZoomRange.Close:
                    break;
                case CameraZoomRange.Middle:
                    k *= 0.85f;
                    break;
                case CameraZoomRange.Far:
                    k *= 0.55f;
                    break;
                default:
                    k *= 0.35f;
                    break;
            }
            return k;
        }

        private static bool Visible(Map map, Vector3 pos, float margin = 6f)
        {
            if (map != Find.CurrentMap) return false;
            var v = Find.CameraDriver.CurrentViewRect;
            return pos.x >= v.minX - margin && pos.x <= v.maxX + margin && pos.z >= v.minZ - margin && pos.z <= v.maxZ + margin;
        }

        private static int N(Map map, Vector3 pos, float n)
        {
            if (!Visible(map, pos)) return 0;
            return VRand.Count(n * Lod(map));
        }

        private static Color Jitter(Color c, float amount)
        {
            if (amount <= 0f) return c;
            return new Color(Mathf.Clamp01(c.r + VRand.Range(-amount, amount)), Mathf.Clamp01(c.g + VRand.Range(-amount, amount)),
                Mathf.Clamp01(c.b + VRand.Range(-amount, amount)), c.a);
        }

        public static Color Fade(Color c, float a = 0f) => new Color(c.r, c.g, c.b, a);

        /// <summary>Hot sparks that streak outward, arc under gravity and fade from white to the given color.</summary>
        public static void Sparks(Map map, Vector3 pos, Color c, float count, float speed, float size = 0.11f, float life = 0.45f,
            float dirAngle = -1f, float cone = 360f, float gravity = 4f, float height = 0.25f)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                float a = dirAngle >= 0f ? dirAngle + VRand.Range(-cone * 0.5f, cone * 0.5f) : VRand.Range(0f, 360f);
                float sp = speed * VRand.Range(0.35f, 1.25f);
                var p = new ArcaneParticle
                {
                    pos = pos + VRand.InsideCircle(0.15f),
                    vel = VRand.Dir(a) * sp,
                    h = height,
                    vh = VRand.Range(-0.2f, 1f) * sp * 0.6f,
                    gravity = gravity,
                    life = life * VRand.Range(0.6f, 1.4f),
                    size0 = size * VRand.Range(0.8f, 1.3f),
                    size1 = size * 0.4f,
                    c0 = Color.Lerp(c, Color.white, 0.7f),
                    c1 = Fade(c),
                    drag = 1.6f,
                    stretch = 0.09f,
                    mat = PMat.Streak,
                    flags = PFlags.Stretch,
                };
                sys.Emit(p);
            }
        }

        /// <summary>Glowing embers that drift upward on turbulent air and wink out.</summary>
        public static void Embers(Map map, Vector3 pos, Color c, float count, float radius = 0.4f, float rise = 1.0f, float life = 1.6f, float size = 0.16f)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                var p = new ArcaneParticle
                {
                    pos = pos + VRand.InsideCircle(radius),
                    vel = VRand.InsideCircle(0.6f),
                    h = VRand.Range(0f, 0.4f),
                    vh = rise * VRand.Range(0.5f, 1.4f),
                    gravity = -rise * 0.3f,
                    life = life * VRand.Range(0.6f, 1.4f),
                    size0 = size * VRand.Range(0.7f, 1.4f),
                    size1 = size * 0.2f,
                    c0 = Color.Lerp(c, new Color(1f, 0.95f, 0.7f), 0.5f),
                    c1 = Fade(c),
                    turb = 0.9f,
                    drag = 0.6f,
                    mat = PMat.Ember,
                    flags = PFlags.Flicker,
                };
                sys.Emit(p);
            }
        }

        /// <summary>Billowing smoke or dust that expands, rises and thins out. Drawn under the lighting so it darkens at night.</summary>
        public static void Smoke(Map map, Vector3 pos, Color c, float count, float radius = 0.6f, float size = 1.0f, float life = 2.6f, float rise = 0.35f,
            bool dark = false, float speed = 0.5f)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                Vector3 off = VRand.InsideCircle(radius);
                var p = new ArcaneParticle
                {
                    pos = pos + off,
                    vel = (off.sqrMagnitude > 0.001f ? off.normalized : VRand.OnCircle(1f)) * speed * VRand.Range(0.4f, 1.2f),
                    h = VRand.Range(0f, 0.3f),
                    vh = rise * VRand.Range(0.6f, 1.3f),
                    life = life * VRand.Range(0.7f, 1.3f),
                    size0 = size * VRand.Range(0.5f, 0.8f),
                    size1 = size * VRand.Range(1.5f, 2.3f),
                    rot = VRand.Range(0f, 360f),
                    spin = VRand.Range(-25f, 25f),
                    c0 = Jitter(c, 0.04f),
                    c1 = Fade(c),
                    drag = 1.2f,
                    turb = 0.25f,
                    mat = dark ? PMat.SmokeDark : PMat.Smoke,
                    flags = PFlags.Lit,
                };
                sys.Emit(p);
            }
        }

        /// <summary>Chunks of rock or ice thrown up in arcs. They cast shadows, bounce and settle.</summary>
        public static void Debris(Map map, Vector3 pos, Color c, float count, float speed = 3f, float size = 0.2f, int mat = -1)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                float sp = speed * VRand.Range(0.3f, 1.1f);
                var p = new ArcaneParticle
                {
                    pos = pos + VRand.InsideCircle(0.3f),
                    vel = VRand.OnCircle(1f) * sp,
                    h = 0.1f,
                    vh = VRand.Range(2.5f, 6f) * (0.6f + speed * 0.12f),
                    gravity = 14f,
                    life = VRand.Range(1.4f, 2.6f),
                    size0 = size * VRand.Range(0.6f, 1.5f),
                    size1 = size * 0.9f,
                    rot = VRand.Range(0f, 360f),
                    spin = VRand.Range(-540f, 540f),
                    c0 = Jitter(c, 0.05f),
                    c1 = Fade(c, c.a),
                    mat = mat >= 0 ? mat : (VRand.Chance(0.5f) ? PMat.Debris : PMat.Debris2),
                    flags = PFlags.Lit | PFlags.Shadow | PFlags.Bounce,
                };
                p.c1.a = 0f;
                sys.Emit(p);
            }
        }

        /// <summary>Glittering shards (ice, crystal, glass) that burst outward, glint and fall.</summary>
        public static void Shards(Map map, Vector3 pos, Color c, float count, float speed = 3.5f, float size = 0.28f)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                float sp = speed * VRand.Range(0.4f, 1.2f);
                var p = new ArcaneParticle
                {
                    pos = pos + VRand.InsideCircle(0.2f),
                    vel = VRand.OnCircle(1f) * sp,
                    h = 0.3f,
                    vh = VRand.Range(1.5f, 4.5f),
                    gravity = 12f,
                    life = VRand.Range(0.9f, 1.6f),
                    size0 = size * VRand.Range(0.6f, 1.4f),
                    size1 = size * 0.6f,
                    rot = VRand.Range(0f, 360f),
                    spin = VRand.Range(-720f, 720f),
                    c0 = Color.Lerp(c, Color.white, 0.4f),
                    c1 = Fade(c),
                    mat = PMat.Shard,
                    flags = PFlags.Shadow | PFlags.Bounce | PFlags.Glint,
                };
                sys.Emit(p);
            }
        }

        /// <summary>Particles drawn inward from a ring toward a point (charging, absorbing, implosions).</summary>
        public static void Implode(Map map, Vector3 pos, Color c, float count, float radius = 2.5f, float life = 0.6f, int mat = -1, float size = 0.18f,
            float swirl = 0f)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                Vector3 off = VRand.OnCircle(radius * VRand.Range(0.7f, 1.1f));
                float l = life * VRand.Range(0.8f, 1.2f);
                var p = new ArcaneParticle
                {
                    pos = pos + off,
                    vel = -off / l * 0.6f,
                    attract = pos,
                    attractK = radius * 6f / (l * l),
                    swirl = swirl,
                    life = l,
                    size0 = size * VRand.Range(0.6f, 1.3f),
                    size1 = size * 1.4f,
                    c0 = Fade(c, c.a * 0.4f),
                    c1 = Color.Lerp(c, Color.white, 0.6f),
                    drag = 2.5f,
                    stretch = 0.12f,
                    mat = mat >= 0 ? mat : PMat.Streak,
                    flags = mat >= 0 ? PFlags.None : PFlags.Stretch,
                };
                sys.Emit(p);
            }
        }

        /// <summary>Generic burst of textured particles flying outward.</summary>
        public static void Burst(Map map, Vector3 pos, Color c, float count, int mat, float speed, float life, float size, PFlags flags = PFlags.None,
            float spin = 180f, float gravity = 0f, float height = 0.2f, float radius = 0.2f, float drag = 1.5f, float sizeEnd = -1f)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                var p = new ArcaneParticle
                {
                    pos = pos + VRand.InsideCircle(radius),
                    vel = VRand.OnCircle(1f) * speed * VRand.Range(0.3f, 1.2f),
                    h = height,
                    vh = gravity > 0f ? VRand.Range(1f, 3.5f) : VRand.Range(-0.2f, 0.4f),
                    gravity = gravity,
                    life = life * VRand.Range(0.7f, 1.3f),
                    size0 = size * VRand.Range(0.7f, 1.3f),
                    size1 = sizeEnd >= 0f ? sizeEnd : size * 0.6f,
                    rot = VRand.Range(0f, 360f),
                    spin = VRand.Range(-spin, spin),
                    c0 = c,
                    c1 = Fade(c),
                    drag = drag,
                    mat = mat,
                    flags = flags,
                };
                sys.Emit(p);
            }
        }

        /// <summary>Particles that float upward from an area (souls, holy motes, bubbles, notes).</summary>
        public static void Rise(Map map, Vector3 pos, Color c, float count, int mat, float radius, float speed, float life, float size, PFlags flags = PFlags.None,
            float spin = 40f)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                var p = new ArcaneParticle
                {
                    pos = pos + VRand.InsideCircle(radius),
                    vel = VRand.InsideCircle(0.15f),
                    h = VRand.Range(0f, 0.3f),
                    vh = speed * VRand.Range(0.6f, 1.4f),
                    life = life * VRand.Range(0.7f, 1.3f),
                    size0 = size * VRand.Range(0.7f, 1.3f),
                    size1 = size * 0.5f,
                    rot = VRand.Range(0f, 360f),
                    spin = VRand.Range(-spin, spin),
                    c0 = c,
                    c1 = Fade(c),
                    drag = 0.4f,
                    turb = 0.3f,
                    mat = mat,
                    flags = flags,
                };
                sys.Emit(p);
            }
        }

        /// <summary>Particles laid out on a ring that rush outward: the dust and debris of a shockwave.</summary>
        public static void RingBurst(Map map, Vector3 pos, Color c, float count, int mat, float radius, float speed, float life, float size, PFlags flags = PFlags.Lit)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / Mathf.Max(1, n) + VRand.Range(-8f, 8f);
                Vector3 dir = VRand.Dir(a);
                var p = new ArcaneParticle
                {
                    pos = pos + dir * radius,
                    vel = dir * speed * VRand.Range(0.7f, 1.2f),
                    h = 0.05f,
                    vh = VRand.Range(0f, 0.4f),
                    life = life * VRand.Range(0.7f, 1.2f),
                    size0 = size * VRand.Range(0.6f, 1f),
                    size1 = size * VRand.Range(1.6f, 2.4f),
                    rot = VRand.Range(0f, 360f),
                    spin = VRand.Range(-60f, 60f),
                    c0 = c,
                    c1 = Fade(c),
                    drag = 2.6f,
                    mat = mat,
                    flags = flags,
                };
                sys.Emit(p);
            }
        }

        /// <summary>Drops that fall from a point (blood, water, molten metal).</summary>
        public static void Drips(Map map, Vector3 pos, Color c, float count, float height = 0.6f, int mat = -1, float size = 0.14f)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                var p = new ArcaneParticle
                {
                    pos = pos + VRand.InsideCircle(0.25f),
                    vel = VRand.InsideCircle(0.5f),
                    h = height * VRand.Range(0.5f, 1.2f),
                    vh = VRand.Range(-0.5f, 0.8f),
                    gravity = 9f,
                    life = VRand.Range(0.5f, 0.9f),
                    size0 = size * VRand.Range(0.7f, 1.3f),
                    size1 = size * 0.7f,
                    c0 = c,
                    c1 = Fade(c, c.a * 0.6f),
                    mat = mat >= 0 ? mat : PMat.BloodAlpha,
                    flags = PFlags.Lit | PFlags.AlignVel,
                };
                sys.Emit(p);
            }
        }

        /// <summary>Low, slow ground mist (frost breath, miasma, holy haze).</summary>
        public static void Mist(Map map, Vector3 pos, Color c, float count, float radius, float life = 3.2f, float size = 1.6f, bool glow = false)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                var p = new ArcaneParticle
                {
                    pos = pos + VRand.InsideCircle(radius),
                    vel = VRand.InsideCircle(0.35f),
                    life = life * VRand.Range(0.7f, 1.3f),
                    size0 = size * VRand.Range(0.6f, 1f),
                    size1 = size * VRand.Range(1.4f, 2f),
                    rot = VRand.Range(0f, 360f),
                    spin = VRand.Range(-12f, 12f),
                    c0 = c,
                    c1 = Fade(c),
                    drag = 0.5f,
                    turb = 0.12f,
                    mat = glow ? PMat.Glow : PMat.Smoke,
                    flags = PFlags.Ground | (glow ? PFlags.None : PFlags.Lit),
                };
                sys.Emit(p);
            }
        }

        /// <summary>Licking flame tongues: they rise quickly, shrink and shift from yellow-white to deep color.</summary>
        public static void Flames(Map map, Vector3 pos, Color c, Color c2, float count, float radius, float size = 0.5f, float height = 0f)
        {
            var sys = Sys(map);
            int n = N(map, pos, count);
            for (int i = 0; i < n; i++)
            {
                var p = new ArcaneParticle
                {
                    pos = pos + VRand.InsideCircle(radius),
                    vel = VRand.InsideCircle(0.25f),
                    h = height + VRand.Range(0f, 0.15f),
                    vh = VRand.Range(0.9f, 1.8f),
                    life = VRand.Range(0.35f, 0.7f),
                    size0 = size * VRand.Range(0.7f, 1.2f),
                    size1 = size * 0.25f,
                    rot = VRand.Range(-15f, 15f),
                    spin = VRand.Range(-40f, 40f),
                    c0 = Color.Lerp(c2, new Color(1f, 0.95f, 0.75f, c2.a), 0.5f),
                    c1 = Fade(c),
                    drag = 1f,
                    turb = 0.4f,
                    mat = PMat.Flame,
                    flags = PFlags.Flicker,
                };
                sys.Emit(p);
            }
        }

        /// <summary>Particles that orbit and spiral in toward a point.</summary>
        public static void Vortex(Map map, Vector3 pos, Color c, float count, float radius, int mat, float life = 0.9f, float size = 0.2f, float swirl = 9f)
        {
            Implode(map, pos, c, count, radius, life, mat, size, swirl);
        }

        /// <summary>One particle, fully specified. Use for bespoke effects.</summary>
        public static void One(Map map, ArcaneParticle p)
        {
            if (!Visible(map, p.pos)) return;
            Sys(map)?.Emit(p);
        }
    }
}

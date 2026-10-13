using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>
    /// Per-map manager for arcane visuals, delayed spell actions, persistent spell fields and polymorphs.
    /// </summary>
    public class ArcaneVfxManager : MapComponent
    {
        private readonly List<Vfx> active = new List<Vfx>();
        private readonly List<(int tick, Action action)> scheduled = new List<(int, Action)>();
        private List<ArcaneField> fields = new List<ArcaneField>();
        public PolymorphTracker polymorphs = new PolymorphTracker();
        [Unsaved] public readonly ArcaneParticleSystem particles = new ArcaneParticleSystem();
        [Unsaved] public readonly ArcaneDecalSystem decals = new ArcaneDecalSystem();
        [Unsaved] public readonly ArcaneScreenFx screen = new ArcaneScreenFx();
        [Unsaved] private readonly List<CompArcane> familiarOwners = new List<CompArcane>();
        [Unsaved] private readonly Dictionary<CompArcane, RibbonTrail> familiarTrails = new Dictionary<CompArcane, RibbonTrail>();
        [Unsaved] private readonly List<CompManaFont> fonts = new List<CompManaFont>();
        private int lastTicks = -1;
        private float frac;
        private float lastNow = -1f;

        /// <summary>Current game time in ticks, with a fractional part for smooth animation between ticks.</summary>
        public float Now => Find.TickManager.TicksGame + frac;

        public ArcaneVfxManager(Map map) : base(map)
        {
        }

        public static ArcaneVfxManager For(Map map)
        {
            if (map == null) return null;
            var c = map.GetComponent<ArcaneVfxManager>();
            if (c == null)
            {
                c = new ArcaneVfxManager(map);
                map.components.Add(c);
            }
            return c;
        }

        public IReadOnlyList<ArcaneField> Fields => fields;

        public void Add(Vfx v)
        {
            if (v == null) return;
            v.startTick = Find.TickManager.TicksGame;
            if (active.Count < 1500) active.Add(v);
        }

        public void Schedule(int delayTicks, Action action)
        {
            if (delayTicks <= 0)
            {
                SafeRun(action);
                return;
            }
            scheduled.Add((Find.TickManager.TicksGame + delayTicks, action));
        }

        public void RegisterFamiliar(CompArcane c)
        {
            if (c != null && !familiarOwners.Contains(c)) familiarOwners.Add(c);
        }

        public void RegisterFont(CompManaFont f)
        {
            if (f != null && !fonts.Contains(f)) fonts.Add(f);
        }

        public void UnregisterFont(CompManaFont f) => fonts.Remove(f);

        /// <summary>The mana font whose radius covers this cell, if any.</summary>
        public CompManaFont FontNear(IntVec3 cell)
        {
            for (int i = 0; i < fonts.Count; i++)
            {
                var f = fonts[i];
                if (f.parent.Spawned && f.parent.Position.DistanceTo(cell) <= f.Props.radius) return f;
            }
            return null;
        }

        public IEnumerable<CompArcane> MagesNear(IntVec3 cell, float radius)
        {
            foreach (var p in map.mapPawns.AllPawnsSpawned)
            {
                if (p.Position.DistanceTo(cell) > radius) continue;
                var c = p.TryGetComp<CompArcane>();
                if (c != null && c.IsMage) yield return c;
            }
        }

        private void DrawFamiliars(float now)
        {
            for (int i = familiarOwners.Count - 1; i >= 0; i--)
            {
                var c = familiarOwners[i];
                var p = c?.Pawn;
                if (p == null || c.familiar == null || p.Dead || !p.Spawned || p.Map != map)
                {
                    if (c != null && familiarTrails.TryGetValue(c, out var old))
                    {
                        old.Dispose();
                        familiarTrails.Remove(c);
                    }
                    familiarOwners.RemoveAt(i);
                    continue;
                }
                if (!familiarTrails.TryGetValue(c, out var trail))
                {
                    trail = new RibbonTrail(14, 0.3f, 0.35f, ArcaneMats.Trail, c.familiar.color);
                    familiarTrails[c] = trail;
                }
                try
                {
                    Familiars.Draw(c, now, trail);
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[Arcanum Ascendant] Familiar draw error: " + e, 0x5A1C0F2);
                    familiarOwners.RemoveAt(i);
                }
            }
        }

        public void AddField(ArcaneField f)
        {
            f.InitVisuals();
            fields.Add(f);
        }

        private static void SafeRun(Action a)
        {
            try
            {
                a();
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Error in delayed spell action: " + e, 0x5A1C0DE);
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            int now = Find.TickManager.TicksGame;
            if (scheduled.Count > 0)
            {
                var due = scheduled.Where(s => s.tick <= now).ToList();
                if (due.Count > 0)
                {
                    scheduled.RemoveAll(s => s.tick <= now);
                    foreach (var d in due) SafeRun(d.action);
                }
            }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var v = active[i];
                try
                {
                    v.Tick(now, map);
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[Arcanum Ascendant] VFX tick error: " + e, 0x5A1C0E7);
                    v.finished = true;
                }
                if (v.finished)
                {
                    End(v);
                    active.RemoveAt(i);
                }
            }
            for (int i = fields.Count - 1; i >= 0; i--)
            {
                var f = fields[i];
                try
                {
                    f.Tick(map);
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[Arcanum Ascendant] Field error: " + e, 0x5A1C0DF);
                    f.ticksLeft = 0;
                }
                if (f.ticksLeft <= 0) fields.RemoveAt(i);
            }
            polymorphs.Tick(map);
        }

        private static void End(Vfx v)
        {
            try
            {
                v.OnEnd();
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] VFX cleanup error: " + e, 0x5A1C0E8);
            }
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            if (Find.CurrentMap != map) return;
            var tm = Find.TickManager;
            int ticks = tm.TicksGame;
            if (ticks != lastTicks)
            {
                lastTicks = ticks;
                frac = 0f;
            }
            else if (!tm.Paused)
            {
                frac = Mathf.Min(0.99f, frac + Time.deltaTime * 60f * tm.TickRateMultiplier);
            }
            float now = ticks + frac;
            float dt = lastNow < 0f ? 0f : Mathf.Clamp((now - lastNow) / 60f, 0f, 0.25f);
            lastNow = now;
            float intensity = ArcanumMod.Settings.vfxIntensity;
            try
            {
                particles.Update(dt);
                decals.Update(dt);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Particle update error: " + e, 0x5A1C0E9);
                particles.Clear();
            }
            if (intensity <= 0.01f) return;
            var view = Find.CameraDriver.CurrentViewRect;
            try
            {
                decals.Draw(view);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Decal draw error: " + e, 0x5A1C0EA);
            }
            for (int i = 0; i < active.Count; i++)
            {
                try
                {
                    active[i].Draw(now);
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[Arcanum Ascendant] VFX draw error: " + e, 0x5A1C0E0);
                    active[i].finished = true;
                }
            }
            for (int i = 0; i < fields.Count; i++)
            {
                try
                {
                    fields[i].Draw(now);
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[Arcanum Ascendant] Field draw error: " + e, 0x5A1C0EB);
                }
            }
            DrawFamiliars(now);
            try
            {
                particles.Draw(view);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Particle draw error: " + e, 0x5A1C0EC);
                particles.Clear();
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (Find.CurrentMap != map) return;
            try
            {
                screen.OnGUI(map, Now);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Screen effect error: " + e, 0x5A1C0ED);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving) fields.RemoveAll(f => !f.CanSave);
            Scribe_Collections.Look(ref fields, "arcaneFields", LookMode.Deep);
            Scribe_Deep.Look(ref polymorphs, "polymorphs");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                fields ??= new List<ArcaneField>();
                fields.RemoveAll(f => f == null || !f.Resolve());
                polymorphs ??= new PolymorphTracker();
            }
        }
    }

    /// <summary>A persistent area spell (blizzard, consecration, black hole, aura...).</summary>
    public class ArcaneField : IExposable
    {
        public AbilityDef ability;
        public int listIndex;
        public int effectIndex = -1;
        public Pawn caster;
        public IntVec3 cell;
        public bool followCaster;
        public float radius;
        public int ticksLeft;
        public int totalTicks;
        public int age;
        public float power = 1f;
        public float healPower = 1f;
        public float critChance;

        [Unsaved] public Effect_Field effect;
        [Unsaved] public SpellContext ctx;
        [Unsaved] private VfxStyle style = VfxStyle.Arcane;
        [Unsaved] private bool harmful = true;
        [Unsaved] private bool lightSpawned;

        public bool CanSave => ability != null && effectIndex >= 0;

        public Vector3 Center => followCaster && caster != null && caster.Spawned ? caster.DrawPos : cell.ToVector3Shifted();
        public IntVec3 CenterCell => followCaster && caster != null && caster.Spawned ? caster.Position : cell;

        public bool Resolve()
        {
            if (ability == null) return false;
            var props = ability.comps?.OfType<CompProperties_ArcaneSpell>().FirstOrDefault();
            if (props == null) return false;
            var list = listIndex == 0 ? props.effects : props.casterEffects;
            if (effectIndex < 0 || effectIndex >= list.Count) return false;
            effect = list[effectIndex] as Effect_Field;
            if (effect == null) return false;
            ctx = caster != null ? SpellContext.For(caster, ability) : new SpellContext { def = ability, props = props, mods = new SpellModSummary(), ext = ability.GetModExtension<SpellExtension>() };
            ctx.basePower = power;
            ctx.healStat = healPower;
            ctx.critChance = critChance;
            InitVisuals();
            return true;
        }

        public void InitVisuals()
        {
            if (ctx == null || effect == null) return;
            style = StyleFx.Resolve(ctx);
            harmful = effect.pulseEffects.Any(e => e is Effect_Damage || e.affects == TargetFilter.Enemies);
        }

        public void Tick(Map map)
        {
            if (effect == null || ctx == null)
            {
                ticksLeft = 0;
                return;
            }
            ctx.map = map;
            ticksLeft--;
            age++;
            if (followCaster && (caster == null || caster.Dead || !caster.Spawned || caster.Map != map))
            {
                ticksLeft = 0;
                return;
            }
            var center = CenterCell;
            if (age % Mathf.Max(1, effect.pulseInterval) == 0)
            {
                var info = new ImpactInfo { cell = center, pos = Center, radius = radius, sourcePos = Center };
                SpellRunner.Impact(ctx, info, effect.pulseEffects, playVfx: false);
                ArcaneFx.PlayAll(effect.pulseVfx, map, Center, ctx, radius);
                if (effect.pullStrength > 0f) Pull(map, center);
            }
            if (age % 4 == 0) StyleFx.FieldTick(style, map, Center, radius, ctx.Color, ctx.Color2, age, harmful);
            if (!lightSpawned && !followCaster)
            {
                lightSpawned = true;
                if (StyleFx.FieldGlows(style)) ArcaneLightFlash.Spawn(map, Center, ctx.Color, Mathf.Min(radius + 2f, 10f), ticksLeft);
            }
            if (effect.ambientFleck != null && age % 3 == 0)
            {
                int n = Mathf.CeilToInt(effect.ambientDensity * radius * radius * 0.05f);
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = Center + VRand.InsideCircle(radius);
                    ArcaneFx.Particles(map, p, effect.useColor2 ? ctx.Color2 : ctx.Color, effect.ambientFleck, 1, effect.ambientSpeed, effect.ambientScale, effect.ambientAngle);
                }
            }
        }

        private void Pull(Map map, IntVec3 center)
        {
            float r = Mathf.Min(radius, GenRadial.MaxRadialPatternRadius - 1f);
            foreach (var p in GenRadial.RadialDistinctThingsAround(center, map, r, true).OfType<Pawn>().ToList())
            {
                if (p.Position == center || !ctx.Passes(p, effect.pullAffects)) continue;
                if (p.BodySize > 4f * effect.pullStrength) continue;
                Vector3 dir = (center.ToVector3Shifted() - p.DrawPos).Yto0().normalized;
                IntVec3 next = (p.DrawPos + dir * 1.1f).ToIntVec3();
                if (next.InBounds(map) && next.Standable(map) && next != p.Position)
                {
                    p.Position = next;
                    p.Notify_Teleported(false, true);
                    ArcaneFx.Particles(map, p.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Mote, 2, 1f, 0.6f);
                }
            }
        }

        public void Draw(float now)
        {
            if (effect == null || ctx == null) return;
            float lifeT = totalTicks > 0 ? 1f - ticksLeft / (float)totalTicks : 0f;
            float alpha = Mathf.Min(1f, age / 20f) * Mathf.Min(1f, ticksLeft / 30f) * effect.groundAlpha;
            Vector3 c = Center;
            c.y = AltitudeLayer.MoteLow.AltitudeFor();
            var mat = ArcaneMats.Get(effect.groundTex);
            ArcaneMats.DrawQuad(ArcaneMats.Glow, c, radius * 2.4f, 0f, ctx.Color * new Color(1, 1, 1, alpha * 0.35f));
            ArcaneMats.DrawQuad(mat, c + new Vector3(0, 0.002f, 0), radius * 2f * 1.05f, now * effect.groundSpin, ctx.Color * new Color(1, 1, 1, alpha));
            ArcaneMats.DrawQuad(ArcaneMats.Ring, c + new Vector3(0, 0.004f, 0), radius * 2.08f, 0f,
                Color.Lerp(ctx.Color, Color.white, 0.3f) * new Color(1, 1, 1, alpha * (0.55f + 0.2f * Mathf.Sin(now * 0.1f + lifeT * 3f))));
            if (effect.groundTex2 != null)
                ArcaneMats.DrawQuad(ArcaneMats.Get(effect.groundTex2), c + new Vector3(0, 0.005f, 0), radius * 2f * 0.8f, -now * effect.groundSpin * 1.5f, ctx.Color2 * new Color(1, 1, 1, alpha * 0.8f));
            if (effect.pullStrength > 0f)
            {
                c.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                ArcaneMats.DrawQuad(ArcaneMats.Get("ArcanumAscendant/Vfx/Vortex"), c, 2.5f + Mathf.Sin(now * 0.1f) * 0.3f, now * 9f, ctx.Color2 * new Color(1, 1, 1, alpha));
                ArcaneMats.DrawQuad(ArcaneMats.Get("ArcanumAscendant/Vfx/BlackHole", false), c + new Vector3(0, 0.01f, 0), 1.6f, 0f, new Color(1, 1, 1, alpha));
            }
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref ability, "ability");
            Scribe_Values.Look(ref listIndex, "listIndex");
            Scribe_Values.Look(ref effectIndex, "effectIndex", -1);
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref cell, "cell");
            Scribe_Values.Look(ref followCaster, "followCaster");
            Scribe_Values.Look(ref radius, "radius");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
            Scribe_Values.Look(ref totalTicks, "totalTicks");
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref power, "power", 1f);
            Scribe_Values.Look(ref healPower, "healPower", 1f);
            Scribe_Values.Look(ref critChance, "critChance");
        }
    }
}

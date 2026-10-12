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
        private int lastTicks = -1;
        private float frac;

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

        public void AddField(ArcaneField f)
        {
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
                v.Tick(now, map);
                if (v.finished) active.RemoveAt(i);
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
            float intensity = ArcanumMod.Settings.vfxIntensity;
            if (intensity <= 0.01f) return;
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
            for (int i = 0; i < fields.Count; i++) fields[i].Draw(now);
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
            return true;
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
            if (effect.ambientFleck != null && age % 3 == 0)
            {
                int n = Mathf.CeilToInt(effect.ambientDensity * radius * radius * 0.05f);
                for (int i = 0; i < n; i++)
                {
                    Vector2 r = Rand.InsideUnitCircle * radius;
                    Vector3 p = Center + new Vector3(r.x, 0f, r.y);
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
            ArcaneMats.DrawQuad(mat, c, radius * 2f * 1.05f, now * effect.groundSpin, ctx.Color * new Color(1, 1, 1, alpha));
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

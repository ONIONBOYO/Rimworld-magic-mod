using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArcanumAscendant
{
    /// <summary>A homing missile: launched sideways in a fan, it curves in toward its target and tightens its turn as it closes.</summary>
    public class VfxMissile : Vfx
    {
        private Vector3 pos;
        private Vector3 vel;
        private readonly LocalTargetInfo target;
        private Vector3 lastKnown;
        private readonly float speed;
        private readonly Action onHit;
        private readonly SpellContext ctx;
        private readonly Material head;
        private readonly float scale;
        private readonly VfxStyle style;
        private RibbonTrail trail;
        private int age;
        private int tickStamp;

        public VfxMissile(SpellContext ctx, Vector3 from, Vector3 launchDir, LocalTargetInfo target, float speed, Action onHit)
        {
            this.ctx = ctx;
            pos = from;
            this.target = target;
            lastKnown = target.CenterVector3;
            this.speed = Mathf.Max(4f, speed);
            vel = launchDir.normalized * this.speed * 0.8f;
            this.onHit = onHit;
            head = ArcaneMats.Get(ctx.props.projectileTex);
            scale = ctx.props.projectileScale;
            style = StyleFx.Resolve(ctx);
            duration = 600;
        }

        private Vector3 TargetPos()
        {
            if (target.HasThing && target.Thing.Spawned) lastKnown = target.Thing.DrawPos;
            return lastKnown;
        }

        public override void Tick(int now, Map map)
        {
            age++;
            tickStamp = now;
            Vector3 to = TargetPos() - pos;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.45f || age > 420)
            {
                finished = true;
                try
                {
                    onHit?.Invoke();
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[Arcanum Ascendant] Missile impact error: " + e, 0x5A1C0F3);
                }
                return;
            }
            Vector3 desired = to / dist * speed;
            float k = Mathf.Clamp01(0.06f + age * 0.012f);
            vel = Vector3.Lerp(vel, desired, k);
            vel = vel.normalized * speed;
            pos += vel / 60f;
            StyleFx.ProjectileTick(style, map, pos, age, ctx.Color, ctx.Color2, scale, false);
        }

        public override void Draw(float now)
        {
            float f = Mathf.Clamp01(now - tickStamp);
            Vector3 p = pos + vel * (f / 60f);
            trail ??= new RibbonTrail(14, scale * 0.6f * StyleFx.TrailWidth(style), 0.25f, ArcaneMats.Trail, Color.Lerp(ctx.Color, ctx.Color2, 0.3f));
            trail.AddPoint(p, now);
            trail.Age(now);
            trail.Draw(now);
            p.y = VAlt.Glow + 0.003f;
            ArcaneMats.DrawQuad(ArcaneMats.Glow, p, scale * 2.4f, 0f, ctx.Color * new Color(1, 1, 1, 0.75f));
            StyleFx.ProjectileHead(style, p, scale, now, ctx.Color, ctx.Color2);
            ArcaneMats.DrawQuad(head, p + new Vector3(0, 0.003f, 0), scale, vel.AngleFlat(), Color.Lerp(ctx.Color2, Color.white, 0.35f));
        }

        public override void OnEnd() => trail?.Dispose();
    }

    /// <summary>Orbs that circle the caster and strike whatever they touch.</summary>
    public class VfxOrbitals : Vfx
    {
        private readonly SpellContext ctx;
        private readonly Pawn caster;
        private readonly int count;
        private readonly float radius;
        private readonly float speed;
        private readonly float hitRadius;
        private readonly int hitCooldown;
        private readonly Material head;
        private readonly float scale;
        private readonly VfxStyle style;
        private readonly Dictionary<int, int> lastHit = new Dictionary<int, int>();
        private RibbonTrail[] trails;
        private int age;
        private int tickStamp;

        public VfxOrbitals(SpellContext ctx, int count, int ticks)
        {
            this.ctx = ctx;
            caster = ctx.caster;
            var props = ctx.props;
            this.count = Mathf.Clamp(count, 1, 12);
            radius = props.orbitalRadius * Mathf.Max(0.6f, ctx.RadiusFactor);
            speed = props.orbitalSpeed;
            hitRadius = props.orbitalHitRadius;
            hitCooldown = props.orbitalHitCooldown;
            head = ArcaneMats.Get(props.projectileTex);
            scale = props.projectileScale;
            style = StyleFx.Resolve(ctx);
            duration = ticks;
        }

        private Vector3 OrbPos(int i, float t)
        {
            float a = (t * speed + i * 360f / count) * Mathf.Deg2Rad;
            float r = radius * (0.92f + 0.08f * Mathf.Sin(t * 0.07f + i));
            return caster.DrawPos + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r * 0.85f);
        }

        public override void Tick(int now, Map map)
        {
            base.Tick(now, map);
            if (caster == null || caster.Dead || !caster.Spawned || caster.Map != map)
            {
                finished = true;
                return;
            }
            age++;
            tickStamp = now;
            ctx.map = map;
            for (int i = 0; i < count; i++)
                if ((age + i) % 3 == 0) StyleFx.ProjectileTick(style, map, OrbPos(i, age), age, ctx.Color, ctx.Color2, scale, false);
            if (age % 2 != 0) return;
            float reach = radius + hitRadius + 0.5f;
            var near = GenRadial.RadialDistinctThingsAround(caster.Position, map, Mathf.Min(reach, GenRadial.MaxRadialPatternRadius - 1f), true).OfType<Pawn>().ToList();
            foreach (var p in near)
            {
                if (p.Dead || !ctx.Passes(p, ctx.props.affects)) continue;
                if (lastHit.TryGetValue(p.thingIDNumber, out int last) && now - last < hitCooldown) continue;
                for (int i = 0; i < count; i++)
                {
                    Vector3 o = OrbPos(i, age);
                    if ((p.DrawPos - o).MagnitudeHorizontal() > hitRadius) continue;
                    lastHit[p.thingIDNumber] = now;
                    ArcaneFx.Flash(map, o, ctx.Color, 1.2f);
                    Fx.Sparks(map, o, ctx.Color2, 5f, 4f);
                    SpellRunner.Impact(ctx, new ImpactInfo { cell = p.Position, pos = p.DrawPos, primary = p, radius = 0f, sourcePos = o }, playVfx: false);
                    break;
                }
            }
        }

        public override void Draw(float now)
        {
            if (caster == null || !caster.Spawned) return;
            float t = age + Mathf.Clamp01(now - tickStamp);
            float fade = Mathf.Clamp01((duration - (now - startTick)) / 20f) * Mathf.Clamp01((now - startTick) / 8f);
            if (trails == null)
            {
                trails = new RibbonTrail[count];
                for (int i = 0; i < count; i++)
                    trails[i] = new RibbonTrail(12, scale * 0.55f * StyleFx.TrailWidth(style), 0.2f, ArcaneMats.Trail, Color.Lerp(ctx.Color, ctx.Color2, 0.3f));
            }
            for (int i = 0; i < count; i++)
            {
                Vector3 p = OrbPos(i, t);
                trails[i].AddPoint(p, now);
                trails[i].Age(now);
                trails[i].Draw(now, fade);
                p.y = VAlt.Glow + 0.003f;
                ArcaneMats.DrawQuad(ArcaneMats.Glow, p, scale * 2.4f, 0f, ctx.Color * new Color(1, 1, 1, 0.7f * fade));
                StyleFx.ProjectileHead(style, p, scale, now, ctx.Color, ctx.Color2);
                ArcaneMats.DrawQuad(head, p + new Vector3(0, 0.003f, 0), scale, now * 6f + i * 40f, Color.Lerp(ctx.Color2, Color.white, 0.35f) * new Color(1, 1, 1, fade));
            }
        }

        public override void OnEnd()
        {
            if (trails != null)
                foreach (var tr in trails) tr.Dispose();
            if (caster != null && caster.Spawned)
                for (int i = 0; i < count; i++) Fx.Burst(caster.Map, OrbPos(i, age), ctx.Color, 4f, PMat.Mote, 1.5f, 0.5f, 0.2f);
        }
    }

    /// <summary>
    /// A channelled beam. It lasts as long as the caster keeps channelling (an ordered move or a stun breaks it),
    /// pulsing its effects on the target.
    /// </summary>
    public class VfxChannel : Vfx
    {
        private readonly SpellContext ctx;
        private readonly LocalTargetInfo target;
        private readonly Job job;
        private readonly int interval;
        private readonly VfxStyle style;
        private readonly Material beam;
        private readonly float width;
        private bool started;
        private bool ending;
        private int endTick;
        private int age;
        private int pulses;
        private Vector3 lastTarget;

        public VfxChannel(SpellContext ctx, LocalTargetInfo target, Job job, int ticks)
        {
            this.ctx = ctx;
            this.target = target;
            this.job = job;
            interval = Mathf.Max(5, ctx.props.channelInterval);
            style = StyleFx.Resolve(ctx);
            beam = ArcaneMats.Get(ctx.props.beamTex);
            width = ctx.props.beamWidth * Mathf.Max(0.6f, ctx.RadiusFactor);
            duration = ticks + 120;
            lastTarget = target.CenterVector3;
        }

        private Vector3 TargetPos()
        {
            if (target.HasThing && target.Thing.Spawned) lastTarget = target.Thing.DrawPos;
            return lastTarget;
        }

        private Vector3 Hand()
        {
            var c = ctx.caster;
            Vector3 d = (TargetPos() - c.DrawPos).Yto0();
            return c.DrawPos + (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward) * 0.45f + new Vector3(0f, 0f, 0.15f);
        }

        public override void Tick(int now, Map map)
        {
            var c = ctx.caster;
            if (ending)
            {
                if (now - endTick > 10) finished = true;
                return;
            }
            age++;
            bool active = c != null && c.Spawned && !c.Dead && !c.Downed && c.Map == map && c.CurJob == job;
            if (!started)
            {
                if (active) started = true;
                else if (age > 20) finished = true;
                return;
            }
            bool targetGone = target.HasThing && (!target.Thing.Spawned || (target.Thing is Pawn tp && tp.Dead));
            if (!active || targetGone || age > duration)
            {
                ending = true;
                endTick = now;
                if (active && targetGone) c.jobs.EndCurrentJob(JobCondition.Succeeded);
                return;
            }
            ctx.map = map;
            Vector3 end = TargetPos();
            if (age % interval == 0)
            {
                pulses++;
                var cell = end.ToIntVec3();
                if (cell.InBounds(map))
                {
                    var info = new ImpactInfo { cell = cell, pos = end, primary = target.Thing, radius = ctx.props.radius * ctx.RadiusFactor, sourcePos = Hand() };
                    SpellRunner.Impact(ctx, info, playVfx: pulses % 3 == 1);
                }
            }
            if (age % 4 == 0) StyleFx.BeamImpactTick(style, map, end, ctx.Color, ctx.Color2);
            if (age % 2 == 0)
            {
                Vector3 dir = (Hand() - end).Yto0().normalized;
                Fx.Sparks(map, end, ctx.Color2, 2f, 4f, 0.1f, 0.3f, dir.AngleFlat(), 120f);
            }
            if (age % 20 == 1) ArcaneLightFlash.Spawn(map, end, ctx.Color, 3f + width * 2f, 22);
        }

        public override void Draw(float now)
        {
            var c = ctx.caster;
            if (!started || c == null || !c.Spawned) return;
            float fade = ending ? Mathf.Clamp01(1f - (now - endTick) / 10f) : Mathf.Clamp01(age / 6f);
            float w = width * (0.85f + 0.15f * Mathf.Sin(now * 1.1f)) * fade;
            float y = VAlt.Glow;
            Vector3 a = Hand(); a.y = y;
            Vector3 b = TargetPos(); b.y = y;
            ArcaneMats.DrawStretched(beam, a, b, w * 2.8f, ctx.Color * new Color(1, 1, 1, 0.4f * fade));
            ArcaneMats.DrawStretched(beam, a + new Vector3(0, 0.002f, 0), b + new Vector3(0, 0.002f, 0), w * 1.2f, Color.Lerp(ctx.Color, ctx.Color2, 0.4f) * new Color(1, 1, 1, 0.85f * fade));
            ArcaneMats.DrawStretched(beam, a + new Vector3(0, 0.004f, 0), b + new Vector3(0, 0.004f, 0), w * 0.4f, new Color(1f, 1f, 1f, fade));
            Vector3 d = b - a;
            float len = d.MagnitudeHorizontal();
            for (int i = 0; i < 5; i++)
            {
                float f = Mathf.Repeat(now * 0.05f * (10f / Mathf.Max(3f, len)) + i * 0.2f, 1f);
                Vector3 p = a + d * f;
                p.y = y + 0.006f;
                ArcaneMats.DrawQuad(ArcaneMats.Glow, p, w * 2.2f, 0f, Color.Lerp(ctx.Color2, Color.white, 0.5f) * new Color(1, 1, 1, 0.8f * fade * Mathf.Sin(f * Mathf.PI)));
            }
            ArcaneMats.DrawQuad(ArcaneMats.Glow, a, w * 4f, 0f, ctx.Color * new Color(1, 1, 1, 0.9f * fade));
            ArcaneMats.DrawQuad(ArcaneMats.Flare, a + new Vector3(0, 0.003f, 0), w * 3.5f, now * 5f, Color.Lerp(ctx.Color2, Color.white, 0.5f) * new Color(1, 1, 1, 0.7f * fade));
            ArcaneMats.DrawQuad(ArcaneMats.Glow, b, w * 5.5f, 0f, ctx.Color * new Color(1, 1, 1, fade));
            ArcaneMats.DrawQuad(ArcaneMats.Flare, b + new Vector3(0, 0.003f, 0), w * 5f, -now * 7f, new Color(1f, 1f, 1f, 0.8f * fade));
        }
    }

    /// <summary>Holds the caster in place, facing the target, while a channelled spell runs.</summary>
    public class JobDriver_ArcaneChannel : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => job.targetA.HasThing && (job.targetA.Thing.Destroyed || (job.targetA.Thing is Pawn p && p.Dead)));
            var t = ToilMaker.MakeToil("ArcaneChannel");
            t.defaultCompleteMode = ToilCompleteMode.Delay;
            t.defaultDuration = Mathf.Max(1, job.count > 0 ? job.count : 240);
            t.handlingFacing = true;
            t.socialMode = RandomSocialMode.Off;
            t.initAction = () => pawn.rotationTracker.FaceTarget(job.targetA);
            t.tickIntervalAction = delta => pawn.rotationTracker.FaceTarget(job.targetA);
            yield return t;
        }

        public override string GetReport() => "AA_ChannelReport".Translate();
    }

    public static class Deliveries
    {
        public static void Missiles(SpellContext ctx, LocalTargetInfo target, Vector3 origin, float radius, int count, List<LocalTargetInfo> targets)
        {
            var props = ctx.props;
            var mgr = ArcaneVfxManager.For(ctx.map);
            Vector3 baseDir = (target.CenterVector3 - origin).Yto0();
            if (baseDir.sqrMagnitude < 0.01f) baseDir = Vector3.forward;
            float baseAngle = baseDir.AngleFlat();
            float spread = props.missileSpread;
            for (int i = 0; i < targets.Count; i++)
            {
                var tt = targets[i];
                float a = count > 1 ? baseAngle - spread * 0.5f + spread * i / (count - 1) : baseAngle;
                a += (i % 2 == 0 ? 1f : -1f) * 25f;
                Vector3 dir = VRand.Dir(a);
                int delay = i * 3;
                mgr.Schedule(delay, () =>
                {
                    var caster = ctx.caster;
                    if (caster == null || !caster.Spawned) return;
                    Vector3 from = caster.DrawPos + dir * 0.4f + new Vector3(0f, 0f, 0.15f);
                    StyleFx.Muzzle(StyleFx.Resolve(ctx), ctx.map, from, a, ctx.Color, ctx.Color2, props.projectileScale * 0.6f);
                    mgr.Add(new VfxMissile(ctx, from, dir, tt, props.projectileSpeed, () =>
                    {
                        var info = new ImpactInfo { radius = radius, sourcePos = from };
                        if (tt.HasThing && tt.Thing.Spawned)
                        {
                            info.primary = tt.Thing;
                            info.cell = tt.Thing.Position;
                            info.pos = tt.Thing.DrawPos;
                        }
                        else
                        {
                            info.cell = tt.Cell;
                            info.pos = tt.CenterVector3;
                        }
                        SpellRunner.Impact(ctx, info);
                    }));
                });
            }
        }

        public static void Channel(SpellContext ctx, LocalTargetInfo target)
        {
            var caster = ctx.caster;
            if (caster?.jobs == null || ArcaneDefOf.AA_ArcaneChannel == null) return;
            int ticks = Mathf.RoundToInt(ctx.props.channelTicks * ctx.DurationFactor);
            var job = JobMaker.MakeJob(ArcaneDefOf.AA_ArcaneChannel, target);
            job.count = ticks;
            caster.jobs.jobQueue.EnqueueFirst(job);
            ArcaneVfxManager.For(ctx.map).Add(new VfxChannel(ctx, target, job, ticks));
        }

        public const int MaxGlobalTargets = 80;

        /// <summary>Every valid target on the map takes the spell, a handful per tick so huge raids do not hitch.</summary>
        public static void Global(SpellContext ctx, Vector3 origin, float radius)
        {
            var map = ctx.map;
            var affects = ctx.props.affects;
            var targets = map.mapPawns.AllPawnsSpawned.Where(p => !p.Dead && ctx.Passes(p, affects))
                .OrderBy(p => p.Position.DistanceToSquared(ctx.caster?.Position ?? p.Position)).Take(MaxGlobalTargets).ToList();
            var mgr = ArcaneVfxManager.For(map);
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                int delay = i / 5;
                bool vfx = i < 30;
                Action strike = () =>
                {
                    if (!t.Spawned || t.Dead || t.Map != map) return;
                    SpellRunner.Impact(ctx, new ImpactInfo { cell = t.Position, pos = t.DrawPos, primary = t, radius = radius, sourcePos = origin }, playVfx: vfx);
                };
                if (delay <= 0) strike();
                else mgr.Schedule(delay, strike);
            }
        }

        /// <summary>The caster blinks to each enemy in turn, appearing behind it and striking.</summary>
        public static void ChainDash(SpellContext ctx, LocalTargetInfo target, int hops, float range, float radius)
        {
            var caster = ctx.caster;
            var map = ctx.map;
            if (caster == null || !caster.Spawned || map == null) return;
            var mgr = ArcaneVfxManager.For(map);
            var visited = new HashSet<Pawn>();
            Pawn first = target.Thing as Pawn;
            if (first == null || !ctx.Passes(first, ctx.props.affects)) first = NextDashTarget(ctx, target.Cell, range, visited);
            if (first == null) return;
            int gap = Mathf.Max(2, ctx.props.chainDelay);
            Action<int, Pawn> hop = null;
            hop = (i, t) =>
            {
                if (!caster.Spawned || caster.Dead || caster.Downed || caster.Map != map) return;
                if (t == null || !t.Spawned || t.Dead)
                {
                    t = NextDashTarget(ctx, caster.Position, range, visited);
                    if (t == null) return;
                }
                visited.Add(t);
                Vector3 from = caster.DrawPos;
                IntVec3 dest = BehindCell(t, caster, map);
                if (dest.IsValid && dest != caster.Position)
                {
                    caster.Position = dest;
                    caster.Notify_Teleported(false, true);
                }
                Vector3 to = caster.DrawPos;
                if (map == Find.CurrentMap)
                {
                    mgr.Add(new VfxBeam(from, to, 9, ArcaneMats.Get(ctx.props.beamTex), ctx.Color, ctx.Color2, 0.45f) { style = StyleFx.Resolve(ctx) });
                    Fx.Burst(map, from, ctx.Color2, 6f, PMat.Gust, 2f, 0.4f, 0.5f, PFlags.Stretch | PFlags.AlignVel, 0f, 0f, 0.2f, 0.3f);
                    Fx.Sparks(map, t.DrawPos, ctx.Color2, 8f, 5f);
                }
                SpellRunner.Impact(ctx, new ImpactInfo { cell = t.Position, pos = t.DrawPos, primary = t, radius = radius, sourcePos = from });
                if (i >= hops) return;
                var next = NextDashTarget(ctx, t.Position, range, visited);
                if (next != null) mgr.Schedule(gap, () => hop(i + 1, next));
            };
            hop(0, first);
        }

        private static Pawn NextDashTarget(SpellContext ctx, IntVec3 around, float range, HashSet<Pawn> visited)
        {
            var map = ctx.map;
            Pawn best = null;
            float bestD = float.MaxValue;
            foreach (var t in GenRadial.RadialDistinctThingsAround(around, map, Mathf.Min(range, GenRadial.MaxRadialPatternRadius - 1f), true))
            {
                if (!(t is Pawn p) || p.Dead || visited.Contains(p) || p == ctx.caster || !ctx.Passes(p, ctx.props.affects)) continue;
                float d = p.Position.DistanceToSquared(around);
                if (d < bestD)
                {
                    bestD = d;
                    best = p;
                }
            }
            return best;
        }

        private static IntVec3 BehindCell(Pawn t, Pawn caster, Map map)
        {
            IntVec3 best = IntVec3.Invalid;
            float bestScore = float.MinValue;
            Vector3 from = caster.DrawPos;
            foreach (var adj in GenAdj.AdjacentCells)
            {
                var c = t.Position + adj;
                if (!c.InBounds(map) || !c.Standable(map)) continue;
                float score = (c.ToVector3Shifted() - from).MagnitudeHorizontalSquared() - (c.GetFirstPawn(map) != null ? 100f : 0f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }

        public static void Delayed(SpellContext ctx, LocalTargetInfo target, float radius, int count)
        {
            var props = ctx.props;
            var map = ctx.map;
            var mgr = ArcaneVfxManager.For(map);
            Thing follow = count <= 1 && target.Thing is Pawn ? target.Thing : null;
            for (int i = 0; i < Mathf.Max(1, count); i++)
            {
                Vector3 at = target.CenterVector3;
                if (i > 0 && props.delayedSpread > 0.1f)
                {
                    var cell = CellFinder.RandomClosewalkCellNear(target.Cell, map, Mathf.CeilToInt(props.delayedSpread));
                    if (cell.IsValid) at = cell.ToVector3Shifted();
                }
                int d = Mathf.Max(1, props.delayTicks + i * props.delayedStagger);
                Vector3 spot = at;
                mgr.Add(new VfxTelegraph { pos = spot, follow = follow, radius = Mathf.Max(0.8f, radius), color = ctx.Color, duration = d });
                mgr.Schedule(d, () =>
                {
                    Vector3 p = follow != null && follow.Spawned ? follow.DrawPos : spot;
                    var cell = p.ToIntVec3();
                    if (!cell.InBounds(map)) return;
                    switch (props.strikeStyle)
                    {
                        case StrikeStyle.Lightning:
                            mgr.Add(new VfxLightning(p + new Vector3(VRand.Range(-2f, 2f), 0f, 18f), p, 14, ctx.Color, ctx.Color2, 1.5f));
                            break;
                        case StrikeStyle.Pillar:
                            mgr.Add(new VfxPillar(p, 36, ctx.Color, ctx.Color2, 1.4f + radius * 0.5f));
                            break;
                        case StrikeStyle.Eruption:
                            ArcaneFx.Eruption(map, p, ctx.Color, ctx.Color2, Mathf.Max(1f, radius));
                            break;
                        default:
                            ArcaneFx.Flash(map, p, ctx.Color, 2f + radius);
                            break;
                    }
                    SpellRunner.Impact(ctx, new ImpactInfo { cell = cell, pos = p, primary = follow, radius = radius, sourcePos = p });
                });
            }
        }
    }
}

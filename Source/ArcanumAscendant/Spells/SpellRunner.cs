using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArcanumAscendant
{
    /// <summary>Executes spell deliveries (projectiles, beams, chains, barrages...) and impacts.</summary>
    public static class SpellRunner
    {
        public static void CastFree(CompArcane c, AbilityDef def, LocalTargetInfo target)
        {
            if (c?.Pawn == null || def == null || c.Pawn.MapHeld == null) return;
            var ctx = SpellContext.For(c.Pawn, def);
            if (ctx.props == null) return;
            ctx.free = true;
            Execute(ctx, target, target);
        }

        public static void Execute(SpellContext ctx, LocalTargetInfo target, LocalTargetInfo dest)
        {
            var props = ctx.props;
            var caster = ctx.caster;
            var map = ctx.map;
            if (props == null || map == null || caster == null) return;
            if (!target.IsValid) target = caster;

            Vector3 origin = caster.DrawPos;
            ArcaneFx.PlayAll(props.castVfx, map, origin, ctx, 0f);
            props.castSound?.PlayOneShot(new TargetInfo(caster.PositionHeld, map));

            if (!props.casterEffects.NullOrEmpty())
            {
                var selfInfo = new ImpactInfo { cell = caster.PositionHeld, pos = origin, primary = caster, radius = 0f, sourcePos = origin };
                foreach (var e in props.casterEffects)
                {
                    if (!Rand.Chance(e.chance)) continue;
                    e.ApplyAtCell(ctx, selfInfo);
                    if (e.AppliesToPawns && !caster.Dead) e.ApplyToPawn(ctx, caster, selfInfo);
                }
                ArcaneFx.PlayAll(props.casterVfx, map, origin, ctx, 0f);
            }

            float radius = props.radius * ctx.RadiusFactor;
            int extra = ctx.mods.extraTargets;
            var mgr = ArcaneVfxManager.For(map);

            switch (props.delivery)
            {
                case DeliveryType.Instant:
                    Impact(ctx, MakeInfo(target, radius, origin));
                    break;

                case DeliveryType.Self:
                case DeliveryType.Nova:
                    Impact(ctx, new ImpactInfo { cell = caster.Position, pos = caster.DrawPos, primary = caster, radius = radius, sourcePos = origin });
                    break;

                case DeliveryType.Projectile:
                case DeliveryType.Meteor:
                {
                    int count = Mathf.Max(1, props.projectileCount + extra);
                    var targets = MultiTargets(ctx, target, count);
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var t = targets[i];
                        int delay = i * 4;
                        Vector3 start = origin;
                        if (props.delivery == DeliveryType.Meteor)
                            start = t.CenterVector3 + new Vector3(Rand.Range(-4f, -2f), 0f, 16f);
                        else if (count > 1)
                            start += new Vector3(Rand.Range(-0.5f, 0.5f), 0f, Rand.Range(-0.2f, 0.6f));
                        var tt = t;
                        Action launch = () =>
                        {
                            Vector3 end = tt.HasThing && tt.Thing.Spawned ? tt.Thing.DrawPos : tt.CenterVector3;
                            float dist = (end - start).MagnitudeHorizontal();
                            if (props.delivery == DeliveryType.Meteor) dist = Mathf.Max(dist, 14f);
                            int ticks = Mathf.Max(3, Mathf.RoundToInt(dist / Mathf.Max(1f, props.projectileSpeed) * 60f));
                            var proj = new VfxProjectile(start, end, ticks, ArcaneMats.Get(props.projectileTex), ctx.Color, ctx.Color2,
                                props.projectileScale, props.delivery == DeliveryType.Meteor ? 0f : props.projectileArc, props.projectileSpin,
                                props.delivery == DeliveryType.Meteor);
                            mgr.Add(proj);
                            mgr.Schedule(ticks, () =>
                            {
                                var info = MakeInfo(tt, radius, start);
                                Impact(ctx, info);
                            });
                        };
                        if (delay == 0) launch(); else mgr.Schedule(delay, launch);
                    }
                    break;
                }

                case DeliveryType.Beam:
                    DoBeam(ctx, target, origin, radius);
                    break;

                case DeliveryType.Chain:
                    DoChain(ctx, target, origin, radius, props.chainCount + extra);
                    break;

                case DeliveryType.Cone:
                    DoCone(ctx, target, origin);
                    break;

                case DeliveryType.Barrage:
                    DoBarrage(ctx, target, props.barrageCount + extra * 2);
                    break;

                case DeliveryType.Line:
                    DoLine(ctx, target, origin);
                    break;
            }

            if (props.shake > 0f) ArcaneFx.Shake(map, props.shake);
        }

        private static ImpactInfo MakeInfo(LocalTargetInfo t, float radius, Vector3 source)
        {
            var info = new ImpactInfo { radius = radius, sourcePos = source };
            if (t.HasThing && t.Thing.SpawnedOrAnyParentSpawned)
            {
                info.primary = t.Thing;
                info.cell = t.Thing.PositionHeld;
                info.pos = t.Thing.SpawnedOrAnyParentSpawned ? t.Thing.DrawPos : info.cell.ToVector3Shifted();
            }
            else
            {
                info.cell = t.Cell;
                info.pos = t.Cell.ToVector3Shifted();
            }
            return info;
        }

        private static List<LocalTargetInfo> MultiTargets(SpellContext ctx, LocalTargetInfo primary, int count)
        {
            var list = new List<LocalTargetInfo> { primary };
            if (count <= 1) return list;
            var props = ctx.props;
            var candidates = GenRadial.RadialDistinctThingsAround(primary.Cell, ctx.map, props.multiTargetRadius, true)
                .OfType<Pawn>().Where(p => p != primary.Thing && ctx.Passes(p, props.affects))
                .OrderBy(p => p.Position.DistanceToSquared(primary.Cell)).ToList();
            int i = 0;
            while (list.Count < count)
            {
                if (candidates.Count > 0)
                {
                    list.Add(candidates[i % candidates.Count]);
                    i++;
                }
                else list.Add(primary);
            }
            return list;
        }

        public static void Impact(SpellContext ctx, ImpactInfo info, List<SpellEffect> effectsOverride = null, bool uniqueHits = false, bool playVfx = true)
        {
            var map = ctx.map;
            var props = ctx.props;
            if (map == null || props == null) return;
            if (!info.cell.InBounds(map)) return;
            if (playVfx)
            {
                ArcaneFx.PlayAll(props.impactVfx, map, info.pos, ctx, info.radius);
                props.impactSound?.PlayOneShot(new TargetInfo(info.cell, map));
            }
            var effects = effectsOverride ?? props.effects;
            var cache = new Dictionary<float, List<Thing>>();
            foreach (var e in effects)
            {
                if (e == null || !Rand.Chance(e.chance)) continue;
                float r = e.radius >= 0f ? e.radius * ctx.RadiusFactor : info.radius;
                e.ApplyAtCell(ctx, info);
                if (!e.AppliesToPawns) continue;
                foreach (var t in Targets(ctx, info, r, cache).ToList())
                {
                    if (!ctx.Passes(t, e.affects)) continue;
                    if (uniqueHits && ctx.hitThisCast.Contains(t)) continue;
                    if (t is Pawn p)
                    {
                        if (p.Dead || p.Destroyed) continue;
                        e.ApplyToPawn(ctx, p, info);
                        if (!e.vfxOnTarget.NullOrEmpty() && p.Spawned) ArcaneFx.PlayAll(e.vfxOnTarget, map, p.DrawPos, ctx, 0f);
                    }
                    else if (t is Corpse corpse)
                    {
                        e.ApplyToCorpse(ctx, corpse, info);
                    }
                }
            }
            if (uniqueHits)
                foreach (var t in Targets(ctx, info, info.radius, cache)) ctx.hitThisCast.Add(t);
        }

        private static List<Thing> Targets(SpellContext ctx, ImpactInfo info, float r, Dictionary<float, List<Thing>> cache)
        {
            if (cache.TryGetValue(r, out var list)) return list;
            list = new List<Thing>();
            var map = ctx.map;
            if (r < 0.5f && info.areaCells == null)
            {
                if (info.primary != null && (info.primary is Pawn || info.primary is Corpse) && info.primary.Spawned)
                    list.Add(info.primary);
                else
                    foreach (var t in info.cell.GetThingList(map))
                        if (t is Pawn || t is Corpse) list.Add(t);
            }
            else if (info.areaCells != null && Mathf.Abs(r - info.radius) < 0.01f)
            {
                var seen = new HashSet<Thing>();
                foreach (var c in info.areaCells)
                {
                    if (!c.InBounds(map)) continue;
                    foreach (var t in c.GetThingList(map))
                        if ((t is Pawn || t is Corpse) && seen.Add(t)) list.Add(t);
                }
            }
            else
            {
                r = Mathf.Min(r, GenRadial.MaxRadialPatternRadius - 0.5f);
                foreach (var t in GenRadial.RadialDistinctThingsAround(info.cell, map, r, true))
                    if (t is Pawn || t is Corpse) list.Add(t);
            }
            cache[r] = list;
            return list;
        }

        private static void DoBeam(SpellContext ctx, LocalTargetInfo target, Vector3 origin, float radius)
        {
            var props = ctx.props;
            var map = ctx.map;
            Vector3 end = target.HasThing && target.Thing.Spawned ? target.Thing.DrawPos : target.CenterVector3;
            if (props.beamToMaxRange)
            {
                float range = ctx.def.verbProperties?.range ?? 20f;
                range *= Mathf.Max(0.3f, 1f + ctx.mods.range);
                Vector3 dir = (end - origin).Yto0().normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
                end = origin + dir * range;
            }
            IntVec3 endCell = end.ToIntVec3();
            if (!endCell.InBounds(map)) endCell = endCell.ClampInsideMap(map);
            float width = props.beamWidth * ctx.RadiusFactor;
            ArcaneVfxManager.For(map).Add(new VfxBeam(origin, end, props.beamTicks, ArcaneMats.Get(props.beamTex), ctx.Color, ctx.Color2, width));

            var cells = new HashSet<IntVec3>();
            Vector3 d = end - origin;
            float len = d.MagnitudeHorizontal();
            int steps = Mathf.CeilToInt(len * 2f);
            for (int i = 0; i <= steps; i++)
            {
                Vector3 pt = origin + d * (i / (float)Mathf.Max(1, steps));
                var c = pt.ToIntVec3();
                if (!c.InBounds(map)) continue;
                cells.Add(c);
                if (width > 1.2f)
                    foreach (var adj in GenAdj.AdjacentCells)
                    {
                        var a = c + adj;
                        if (a.InBounds(map) && (a.ToVector3Shifted() - pt).MagnitudeHorizontal() <= width * 0.7f) cells.Add(a);
                    }
            }
            cells.Remove(ctx.caster.Position);
            var info = new ImpactInfo { cell = endCell, pos = end, primary = target.Thing, radius = radius, sourcePos = origin, areaCells = cells.ToList() };
            if (radius > 0.5f)
            {
                foreach (var c in GenRadial.RadialCellsAround(endCell, radius, true))
                    if (c.InBounds(map) && !cells.Contains(c)) info.areaCells.Add(c);
                info.radius = 0.001f;
            }
            Impact(ctx, info);
            foreach (var c in cells)
                if (Rand.Chance(0.35f)) ArcaneFx.Particles(map, c.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Spark, 1, 0.6f, 1.5f);
        }

        private static void DoChain(SpellContext ctx, LocalTargetInfo target, Vector3 origin, float radius, int hops)
        {
            var props = ctx.props;
            var map = ctx.map;
            var mgr = ArcaneVfxManager.For(map);
            var hit = new HashSet<Thing>();
            Vector3 from = origin;
            Thing current = target.Thing;
            if (!(current is Pawn))
            {
                mgr.Add(new VfxLightning(from, target.CenterVector3, 14, ctx.Color, ctx.Color2, 0.9f));
                Impact(ctx, MakeInfo(target, radius, origin));
                return;
            }
            float scale = 1f;
            int delay = 0;
            for (int i = 0; i <= hops && current != null; i++)
            {
                var cur = current;
                Vector3 src = from;
                float sc = scale;
                mgr.Schedule(delay, () =>
                {
                    if (!cur.Spawned) return;
                    mgr.Add(new VfxLightning(src, cur.DrawPos, 14, ctx.Color, ctx.Color2, 0.9f * Mathf.Max(0.5f, sc)));
                    var info = MakeInfo(cur, radius, src);
                    info.scale = sc;
                    Impact(ctx, info);
                });
                hit.Add(cur);
                from = cur.DrawPos;
                scale *= props.chainFalloff;
                delay += props.chainDelay;
                float cr = props.chainRange * ctx.RadiusFactor;
                current = GenRadial.RadialDistinctThingsAround(cur.Position, map, Mathf.Min(cr, GenRadial.MaxRadialPatternRadius - 1f), false)
                    .OfType<Pawn>().Where(p => !hit.Contains(p) && ctx.Passes(p, props.affects))
                    .OrderBy(p => p.Position.DistanceToSquared(cur.Position)).FirstOrDefault();
            }
        }

        private static void DoCone(SpellContext ctx, LocalTargetInfo target, Vector3 origin)
        {
            var props = ctx.props;
            var map = ctx.map;
            float range = props.coneRange * ctx.RadiusFactor;
            Vector3 dir = (target.CenterVector3 - origin).Yto0();
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            float baseAngle = dir.AngleFlat();
            var cells = new List<IntVec3>();
            foreach (var c in GenRadial.RadialCellsAround(ctx.caster.Position, Mathf.Min(range, GenRadial.MaxRadialPatternRadius - 1f), false))
            {
                if (!c.InBounds(map)) continue;
                Vector3 v = (c.ToVector3Shifted() - origin).Yto0();
                float a = Mathf.Abs(Mathf.DeltaAngle(baseAngle, v.AngleFlat()));
                if (a <= props.coneAngle * 0.5f && GenSight.LineOfSight(ctx.caster.Position, c, map, true)) cells.Add(c);
            }
            var info = new ImpactInfo { cell = target.Cell, pos = target.CenterVector3, primary = target.Thing, radius = 0.001f, areaCells = cells, sourcePos = origin };
            Impact(ctx, info, playVfx: false);
            ArcaneFx.ConeSpray(map, origin, baseAngle, props.coneAngle, range, ctx);
            props.impactSound?.PlayOneShot(new TargetInfo(ctx.caster.Position, map));
        }

        private static void DoBarrage(SpellContext ctx, LocalTargetInfo target, int count)
        {
            var props = ctx.props;
            var map = ctx.map;
            var mgr = ArcaneVfxManager.For(map);
            float area = props.barrageRadius * ctx.RadiusFactor;
            float strikeR = props.strikeRadius * ctx.RadiusFactor;
            IntVec3 center = target.Cell;
            for (int i = 0; i < count; i++)
            {
                int delay = i * props.barrageInterval + Rand.Range(0, 3);
                mgr.Schedule(delay, () =>
                {
                    IntVec3 cell = center;
                    var enemies = GenRadial.RadialDistinctThingsAround(center, map, Mathf.Min(area, GenRadial.MaxRadialPatternRadius - 1f), true)
                        .OfType<Pawn>().Where(p => ctx.Passes(p, props.affects)).ToList();
                    if (enemies.Count > 0 && Rand.Chance(0.55f)) cell = enemies.RandomElement().Position;
                    else
                    {
                        var opts = GenRadial.RadialCellsAround(center, Mathf.Min(area, GenRadial.MaxRadialPatternRadius - 1f), true).Where(c => c.InBounds(map)).ToList();
                        if (opts.Count > 0) cell = opts.RandomElement();
                    }
                    Strike(ctx, cell, strikeR);
                });
            }
        }

        public static void Strike(SpellContext ctx, IntVec3 cell, float strikeR)
        {
            var props = ctx.props;
            var map = ctx.map;
            var mgr = ArcaneVfxManager.For(map);
            Vector3 pos = cell.ToVector3Shifted();
            switch (props.strikeStyle)
            {
                case StrikeStyle.Meteor:
                case StrikeStyle.Shard:
                {
                    Vector3 start = pos + new Vector3(Rand.Range(-4f, -1f), 0f, 14f);
                    int ticks = 18;
                    mgr.Add(new VfxProjectile(start, pos, ticks, ArcaneMats.Get(props.projectileTex), ctx.Color, ctx.Color2, props.projectileScale, 0f,
                        props.strikeStyle == StrikeStyle.Shard, true));
                    mgr.Schedule(ticks, () => Impact(ctx, new ImpactInfo { cell = cell, pos = pos, radius = strikeR, sourcePos = start }));
                    break;
                }
                case StrikeStyle.Lightning:
                    mgr.Add(new VfxLightning(pos + new Vector3(Rand.Range(-2f, 2f), 0f, 18f), pos, 12, ctx.Color, ctx.Color2, 1.3f));
                    ArcaneFx.Flash(map, pos, ctx.Color, 4f);
                    Impact(ctx, new ImpactInfo { cell = cell, pos = pos, radius = strikeR, sourcePos = pos });
                    break;
                case StrikeStyle.Pillar:
                    mgr.Add(new VfxPillar(pos, 24, ctx.Color, ctx.Color2, 1.4f + strikeR * 0.4f));
                    mgr.Schedule(6, () => Impact(ctx, new ImpactInfo { cell = cell, pos = pos, radius = strikeR, sourcePos = pos }));
                    break;
                case StrikeStyle.Eruption:
                    ArcaneFx.Eruption(map, pos, ctx.Color, ctx.Color2, strikeR);
                    Impact(ctx, new ImpactInfo { cell = cell, pos = pos, radius = strikeR, sourcePos = pos });
                    break;
            }
        }

        private static void DoLine(SpellContext ctx, LocalTargetInfo target, Vector3 origin)
        {
            var props = ctx.props;
            var map = ctx.map;
            var mgr = ArcaneVfxManager.For(map);
            Vector3 dir = (target.CenterVector3 - origin).Yto0();
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();
            float len = props.lineLength * Mathf.Max(0.3f, 1f + ctx.mods.range);
            float strikeR = props.strikeRadius * ctx.RadiusFactor;
            int steps = Mathf.CeilToInt(len);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 p = origin + dir * i;
                IntVec3 cell = p.ToIntVec3();
                if (!cell.InBounds(map)) break;
                if (cell.Impassable(map) && cell.GetEdifice(map) is Building b && b.def.building != null && b.def.building.isNaturalRock) break;
                mgr.Schedule(i * props.lineStepDelay, () =>
                {
                    ArcaneFx.Eruption(map, cell.ToVector3Shifted(), ctx.Color, ctx.Color2, strikeR * 0.7f);
                    Impact(ctx, new ImpactInfo { cell = cell, pos = cell.ToVector3Shifted(), radius = strikeR, sourcePos = origin }, uniqueHits: true, playVfx: false);
                });
            }
            props.impactSound?.PlayOneShot(new TargetInfo(ctx.caster.Position, map));
        }
    }
}

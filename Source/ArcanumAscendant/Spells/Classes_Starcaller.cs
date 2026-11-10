using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ArcanumAscendant
{
    // ================================================================================================
    // The Starcaller: she hangs stars over the battlefield and her spells are verbs on those stars: link them into
    // constellations, pull them across the field, set them off. Moonsworn read the phase of the moon; Signbearers wear
    // the constellations themselves.
    // ================================================================================================

    public class CompProperties_ArcaneStar : CompProperties
    {
        public float size = 1f;
        /// <summary>A great star (Polaris): never pruned when the cap is reached, and it counts double for volleys.</summary>
        public bool great;
        public Color color = new Color(0.62f, 0.7f, 1f);
        public Color color2 = new Color(1f, 0.96f, 0.8f);

        public CompProperties_ArcaneStar()
        {
            compClass = typeof(CompArcaneStar);
        }
    }

    /// <summary>A star hung in the air by a Starcaller. Purely a marker with a light show: the spells do the work.</summary>
    public class CompArcaneStar : ThingComp
    {
        /// <summary>The star is in flight until this tick and is not drawn at its cell.</summary>
        public int hiddenUntil = -1;

        public CompProperties_ArcaneStar Props => (CompProperties_ArcaneStar)props;

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned || parent.Map != Find.CurrentMap || !parent.IsHashIntervalTick(18)) return;
            if (Find.TickManager.TicksGame < hiddenUntil) return;
            Vector3 p = parent.DrawPos + new Vector3(0f, 0f, 0.35f);
            Fx.Burst(parent.Map, p, Color.Lerp(Props.color, Props.color2, VRand.Value), 1f, PMat.Stardust, 0.25f, 1.6f, 0.16f * Props.size, PFlags.Glint, 0f, 0f, 0.3f, 0.5f * Props.size);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (Find.TickManager.TicksGame < hiddenUntil) return;
            float now = Time.realtimeSinceStartup;
            float id = parent.thingIDNumber * 0.37f;
            float tw = 0.86f + 0.14f * Mathf.Sin(now * 3.1f + id) + 0.06f * Mathf.Sin(now * 7.3f + id * 2f);
            float s = Props.size;
            Vector3 g = parent.DrawPos;
            g.y = VAlt.Ground;
            ArcaneMats.DrawQuad(ArcaneMats.Glow, g, s * 2.4f, 0f, Props.color * new Color(1, 1, 1, 0.16f * tw));
            Vector3 p = parent.DrawPos + new Vector3(0f, 0f, 0.35f);
            p.y = VAlt.Glow;
            ArcaneMats.DrawQuad(ArcaneMats.Glow, p, s * 2.1f * tw, 0f, Props.color * new Color(1, 1, 1, 0.42f));
            ArcaneMats.DrawQuad(PMat.Get(PMat.StarSpike), p + new Vector3(0, 0.002f, 0), s * 2.4f * tw, now * 9f + id * 40f, Props.color2 * new Color(1, 1, 1, 0.9f));
            ArcaneMats.DrawQuad(PMat.Get(PMat.StarSpike), p + new Vector3(0, 0.004f, 0), s * 1.35f, 45f - now * 14f, Color.white * new Color(1, 1, 1, 0.7f * tw));
            ArcaneMats.DrawQuad(ArcaneMats.Glow, p + new Vector3(0, 0.006f, 0), s * 0.55f, 0f, Color.white);
            if (Props.great)
                ArcaneMats.DrawQuad(ArcaneMats.Ring, p + new Vector3(0, 0.001f, 0), s * (2.2f + 0.3f * Mathf.Sin(now * 1.7f)), now * 20f, Props.color2 * new Color(1, 1, 1, 0.45f));
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref hiddenUntil, "hiddenUntil", -1);
        }
    }

    public static class Stars
    {
        private static List<ThingDef> defs;

        public static List<ThingDef> Defs => defs ??= DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.HasComp(typeof(CompArcaneStar))).ToList();

        public static bool IsGreat(Thing t) => t.TryGetComp<CompArcaneStar>()?.Props.great ?? false;

        public static Vector3 Pos(Thing star) => star.DrawPos + new Vector3(0f, 0f, 0.35f);

        public static List<Thing> Of(Pawn caster, Map map)
        {
            var list = new List<Thing>();
            if (map == null || caster == null) return list;
            foreach (var def in Defs)
                foreach (var t in map.listerThings.ThingsOfDef(def))
                    if (!t.Destroyed && t.Spawned && t.TryGetComp<CompArcaneConstruct>()?.caster == caster) list.Add(t);
            return list;
        }

        public static Thing Nearest(List<Thing> stars, IntVec3 cell) => stars.Count == 0 ? null : stars.MinBy(s => s.Position.DistanceToSquared(cell));

        public static bool None(SpellContext ctx, List<Thing> stars, int need = 1)
        {
            if (stars.Count >= need) return false;
            if (ctx.caster != null && ctx.Once("nostars")) ArcaneFx.Text(ctx.caster, (need > 1 ? "AA_NeedStars" : "AA_NoStars").Translate(need), ctx.Color);
            return true;
        }

        public static Thing Spawn(SpellContext ctx, ThingDef def, IntVec3 cell, int lifetime)
        {
            var map = ctx.map;
            if (def == null || map == null || !cell.InBounds(map)) return null;
            if (!cell.Walkable(map) || cell.GetThingList(map).Any(t => t.def.HasComp(typeof(CompArcaneStar))))
            {
                cell = CellFinder.StandableCellNear(cell, map, 2.5f, c => !c.GetThingList(map).Any(t => t.def.HasComp(typeof(CompArcaneStar))));
                if (!cell.IsValid) return null;
            }
            var t = ThingMaker.MakeThing(def);
            GenSpawn.Spawn(t, cell, map, WipeMode.VanishOrMoveAside);
            t.TryGetComp<CompArcaneConstruct>()?.Setup(ctx, lifetime > 0 ? Mathf.RoundToInt(lifetime * ctx.DurationFactor) : -1);
            if (map == Find.CurrentMap)
            {
                Vector3 p = Pos(t);
                ArcaneFx.Flash(map, p, ctx.Color2, 2.2f);
                Fx.Burst(map, p, ctx.Color2, 10f, PMat.Stardust, 2.4f, 0.8f, 0.2f, PFlags.Glint);
                ArcaneVfxManager.For(map).Add(new VfxSprite { mat = PMat.Get(PMat.StarSpike), pos = p, scaleStart = 4f, scaleEnd = 1.2f, duration = 14, color = Color.white, spin = 90f });
            }
            return t;
        }

        /// <summary>Burns out the oldest ordinary stars until at most cap remain.</summary>
        public static void Prune(Pawn caster, Map map, int cap)
        {
            var stars = Of(caster, map).Where(s => !IsGreat(s)).OrderBy(s => s.thingIDNumber).ToList();
            for (int i = 0; i < stars.Count - cap; i++) BurnOut(stars[i]);
        }

        public static void BurnOut(Thing star)
        {
            if (star == null || star.Destroyed) return;
            var map = star.Map;
            if (map != null && map == Find.CurrentMap)
            {
                Vector3 p = Pos(star);
                Fx.Burst(map, p, new Color(0.8f, 0.85f, 1f), 6f, PMat.Stardust, 1.4f, 1f, 0.16f, PFlags.Glint, 0f, 0.6f);
                ArcaneVfxManager.For(map).Add(new VfxSprite { mat = PMat.Get(PMat.StarSpike), pos = p, scaleStart = 1.6f, scaleEnd = 0.1f, duration = 12, color = Color.white });
            }
            star.Destroy(DestroyMode.Vanish);
        }

        public static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = (b - a).Yto0();
            Vector3 ap = (p - a).Yto0();
            float len2 = ab.sqrMagnitude;
            float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(ap, ab) / len2);
            return (ap - ab * t).magnitude;
        }

        public static bool InTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            float d1 = Side(p, a, b), d2 = Side(p, b, c), d3 = Side(p, c, a);
            bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool pos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(neg && pos);
        }

        private static float Side(Vector3 p, Vector3 a, Vector3 b) => (p.x - b.x) * (a.z - b.z) - (a.x - b.x) * (p.z - b.z);

        /// <summary>Applies effects to one pawn as its own small impact.</summary>
        public static void Hit(SpellContext ctx, Pawn p, List<SpellEffect> effects, Vector3 source, float scale = 1f)
        {
            if (p == null || p.Dead || !p.Spawned || effects.NullOrEmpty()) return;
            SpellRunner.Impact(ctx, new ImpactInfo { cell = p.Position, pos = p.DrawPos, primary = p, radius = 0f, sourcePos = source, scale = scale }, effects, false, false);
        }

        public static IEnumerable<Pawn> Targets(SpellContext ctx, TargetFilter filter)
        {
            foreach (var p in ctx.map.mapPawns.AllPawnsSpawned.ToList())
                if (!p.Dead && ctx.Passes(p, filter)) yield return p;
        }
    }

    /// <summary>Hangs one star at the target, or three in a triangle around it. Only so many ordinary stars can burn at once.</summary>
    public class Effect_StarcallerKindle : SpellEffect
    {
        public ThingDef thing;
        public int max = 4;
        public int lifetimeTicks = 18000;
        /// <summary>Above zero: three stars in a triangle of this radius around the target.</summary>
        public float triangleRadius;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (thing == null || ctx.caster == null) return;
            var map = ctx.map;
            if (triangleRadius > 0.5f)
            {
                float r = triangleRadius * Mathf.Clamp(ctx.RadiusFactor, 0.8f, 1.6f);
                float a0 = Rand.Range(0f, 120f);
                for (int i = 0; i < 3; i++)
                {
                    var c = (info.pos + VRand.Dir(a0 + i * 120f) * r).ToIntVec3();
                    Stars.Spawn(ctx, thing, c, lifetimeTicks);
                }
            }
            else Stars.Spawn(ctx, thing, info.cell, lifetimeTicks);
            if (!thing.GetCompProperties<CompProperties_ArcaneStar>()?.great ?? true) Stars.Prune(ctx.caster, map, max + ctx.mods.extraTargets);
            else
            {
                // only one great star at a time
                var greats = Stars.Of(ctx.caster, map).Where(s => s.def == thing).OrderByDescending(s => s.thingIDNumber).Skip(1).ToList();
                foreach (var g in greats) Stars.BurnOut(g);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (thing == null) yield break;
            yield return (triangleRadius > 0.5f ? "AA_Desc_StarTriangle" : "AA_Desc_StarKindle").Translate(thing.label, (lifetimeTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod(),
                max + (ctx?.mods?.extraTargets ?? 0));
        }
    }

    /// <summary>
    /// Draws lines of starlight between every pair of the caster's stars that are close enough. Enemies on a line are
    /// struck; enemies inside a triangle of three linked stars are struck by its inside effects as well.
    /// </summary>
    public class Effect_StarcallerWeb : SpellEffect
    {
        public float linkRange = 14f;
        public float lineWidth = 0.9f;
        public List<SpellEffect> lineEffects;
        public List<SpellEffect> insideEffects;
        /// <summary>The lines stay for this long, striking again (at reduced strength) every lingerInterval.</summary>
        public int lingerTicks;
        public int lingerInterval = 45;
        public float lingerScale = 0.4f;
        /// <summary>Builds a wall of this thing along every line.</summary>
        public ThingDef wallThing;
        public int wallLifetimeTicks = 480;

        public override bool AppliesToPawns => false;

        private struct Line
        {
            public Thing a, b;
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (!ctx.Once("starweb")) return;
            var map = ctx.map;
            var stars = Stars.Of(ctx.caster, map);
            if (Stars.None(ctx, stars, 2)) return;
            float range = linkRange * Mathf.Clamp(ctx.RadiusFactor, 0.8f, 1.8f);
            var lines = new List<Line>();
            for (int i = 0; i < stars.Count; i++)
                for (int j = i + 1; j < stars.Count; j++)
                    if (stars[i].Position.DistanceTo(stars[j].Position) <= range) lines.Add(new Line { a = stars[i], b = stars[j] });
            if (lines.Count == 0)
            {
                ArcaneFx.Text(ctx.caster, "AA_StarsTooFar".Translate(), ctx.Color);
                return;
            }
            int linger = Mathf.RoundToInt(lingerTicks * ctx.DurationFactor);
            var mgr = ArcaneVfxManager.For(map);
            if (map == Find.CurrentMap)
            {
                foreach (var l in lines) mgr.Add(new VfxStarLine(Stars.Pos(l.a), Stars.Pos(l.b), 34 + linger, ctx.Color, ctx.Color2, 0.5f));
                ArcaneDefOf.AA_LevelUp?.PlayOneShot(new TargetInfo(stars[0].Position, map));
            }
            Strike(ctx, stars, lines, range, 1f, true);
            for (int t = lingerInterval; t <= linger; t += lingerInterval)
                mgr.Schedule(t, () =>
                {
                    var live = lines.Where(l => !l.a.Destroyed && !l.b.Destroyed).ToList();
                    if (live.Count > 0) Strike(ctx, stars.Where(s => !s.Destroyed).ToList(), live, range, lingerScale, false);
                });
            if (wallThing != null)
                foreach (var l in lines)
                    foreach (var c in GenSight.BresenhamCellsBetween(l.a.Position, l.b.Position))
                    {
                        if (!c.InBounds(map) || !c.Standable(map) || c.GetFirstPawn(map) != null || c.GetEdifice(map) != null) continue;
                        var w = ThingMaker.MakeThing(wallThing);
                        if (w.def.CanHaveFaction && ctx.caster.Faction != null) w.SetFaction(ctx.caster.Faction);
                        GenSpawn.Spawn(w, c, map, WipeMode.VanishOrMoveAside);
                        w.TryGetComp<CompArcaneConstruct>()?.Setup(ctx, Mathf.RoundToInt(wallLifetimeTicks * ctx.DurationFactor));
                    }
        }

        private void Strike(SpellContext ctx, List<Thing> stars, List<Line> lines, float range, float scale, bool first)
        {
            var map = ctx.map;
            var pawns = Stars.Targets(ctx, affects).ToList();
            var onLine = new HashSet<Pawn>();
            foreach (var p in pawns)
                foreach (var l in lines)
                    if (Stars.DistToSegment(p.DrawPos, l.a.DrawPos, l.b.DrawPos) <= lineWidth)
                    {
                        onLine.Add(p);
                        break;
                    }
            foreach (var p in onLine) Stars.Hit(ctx, p, lineEffects, p.DrawPos, scale);
            if (insideEffects.NullOrEmpty() || !first) return;
            // every triangle whose three sides are all lines
            var inside = new HashSet<Pawn>();
            bool Linked(Thing x, Thing y) => lines.Any(l => l.a == x && l.b == y || l.a == y && l.b == x);
            for (int i = 0; i < stars.Count; i++)
                for (int j = i + 1; j < stars.Count; j++)
                    for (int k = j + 1; k < stars.Count; k++)
                    {
                        if (!Linked(stars[i], stars[j]) || !Linked(stars[j], stars[k]) || !Linked(stars[i], stars[k])) continue;
                        Vector3 a = stars[i].DrawPos, b = stars[j].DrawPos, c = stars[k].DrawPos;
                        foreach (var p in pawns)
                            if (!p.Dead && Stars.InTriangle(p.DrawPos, a, b, c)) inside.Add(p);
                        if (map == Find.CurrentMap)
                        {
                            Vector3 mid = (a + b + c) / 3f;
                            float span = Mathf.Max((a - mid).magnitude, (b - mid).magnitude, (c - mid).magnitude);
                            ArcaneVfxManager.For(map).Add(new VfxSprite { mat = PMat.Get(PMat.Nebula), pos = mid, scaleStart = span * 1.2f, scaleEnd = span * 2.2f, duration = 40,
                                color = ctx.Color * new Color(1, 1, 1, 0.55f), spin = 12f, layer = AltitudeLayer.MoteLow });
                            for (int n = 0; n < 14; n++)
                            {
                                float u = VRand.Value, v = VRand.Value;
                                if (u + v > 1f)
                                {
                                    u = 1f - u;
                                    v = 1f - v;
                                }
                                Fx.Burst(map, a + (b - a) * u + (c - a) * v, ctx.Color2, 1f, PMat.Stardust, 0.3f, 1.1f, 0.22f, PFlags.Glint);
                            }
                        }
                    }
            foreach (var p in inside) Stars.Hit(ctx, p, insideEffects, p.DrawPos, scale);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_StarWeb".Translate((linkRange * (ctx?.RadiusFactor ?? 1f)).ToString("F0"));
            if (!lineEffects.NullOrEmpty())
                foreach (var e in lineEffects)
                    foreach (var s in e.Describe(ctx)) yield return "  • " + s;
            if (!insideEffects.NullOrEmpty())
            {
                yield return "AA_Desc_StarWebInside".Translate();
                foreach (var e in insideEffects)
                    foreach (var s in e.Describe(ctx)) yield return "  • " + s;
            }
            if (lingerTicks > 0) yield return "AA_Desc_StarWebLinger".Translate((lingerTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod(), lingerScale.ToStringPercent());
            if (wallThing != null) yield return "AA_Desc_StarWebWall".Translate(wallThing.label, (wallLifetimeTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    /// <summary>Plays effects around every one of the caster's stars; optionally the stars are spent.</summary>
    public class Effect_StarcallerAtStars : SpellEffect
    {
        public List<SpellEffect> effects;
        public float starRadius = 3f;
        public bool consume;
        public int stagger = 4;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (effects.NullOrEmpty() || !ctx.Once("atstars" + GetHashCode())) return;
            var map = ctx.map;
            var stars = Stars.Of(ctx.caster, map);
            if (Stars.None(ctx, stars)) return;
            var mgr = ArcaneVfxManager.For(map);
            for (int i = 0; i < stars.Count; i++)
            {
                var star = stars[i];
                Action go = () =>
                {
                    if (star.Destroyed || !star.Spawned) return;
                    float scale = Stars.IsGreat(star) ? 1.5f : 1f;
                    SpellRunner.Impact(ctx, new ImpactInfo { cell = star.Position, pos = star.DrawPos, radius = starRadius * ctx.RadiusFactor * (scale > 1f ? 1.25f : 1f),
                        sourcePos = star.DrawPos, scale = scale }, effects, false, true);
                    if (consume) Stars.BurnOut(star);
                };
                if (i == 0 || stagger <= 0) go();
                else mgr.Schedule(i * stagger, go);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return (consume ? "AA_Desc_AtStarsSpent" : "AA_Desc_AtStars").Translate((starRadius * (ctx?.RadiusFactor ?? 1f)).ToString("0.#"));
            foreach (var e in effects)
                foreach (var s in e.Describe(ctx)) yield return "  • " + s;
        }
    }

    /// <summary>
    /// A star streaks to the target point and stays there, striking everything along its path. With all set, every star
    /// comes: they merge into one, and the meeting is the stronger the more stars arrive.
    /// </summary>
    public class Effect_StarcallerShoot : SpellEffect
    {
        public bool all;
        public List<SpellEffect> pathEffects;
        public float pathWidth = 1.2f;
        public List<SpellEffect> arriveEffects;
        public float arriveRadius = 2.5f;
        public float bonusPerStar = 0.5f;
        public float speed = 26f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (!ctx.Once("starshoot")) return;
            var map = ctx.map;
            var stars = Stars.Of(ctx.caster, map);
            if (Stars.None(ctx, stars)) return;
            IntVec3 dest = info.cell;
            if (!dest.InBounds(map)) return;
            if (!dest.Walkable(map)) dest = CellFinder.StandableCellNear(dest, map, 3f);
            if (!dest.IsValid) return;
            var moving = all ? stars : new List<Thing> { Stars.Nearest(stars, dest) };
            var mgr = ArcaneVfxManager.For(map);
            Vector3 end = dest.ToVector3Shifted();
            int longest = 1;
            int now = Find.TickManager.TicksGame;
            var struck = new HashSet<Pawn>();
            foreach (var star in moving)
            {
                Vector3 start = star.DrawPos;
                float dist = (end - start).MagnitudeHorizontal();
                int ticks = Mathf.Max(4, Mathf.RoundToInt(dist / speed * 60f));
                longest = Mathf.Max(longest, ticks);
                if (!pathEffects.NullOrEmpty() && dist > 0.5f)
                    foreach (var p in Stars.Targets(ctx, affects).ToList())
                    {
                        if (struck.Contains(p) || Stars.DistToSegment(p.DrawPos, start, end) > pathWidth) continue;
                        struck.Add(p);
                        float along = Mathf.Clamp01(Vector3.Dot((p.DrawPos - start).Yto0(), (end - start).Yto0().normalized) / Mathf.Max(0.1f, dist));
                        var victim = p;
                        mgr.Schedule(Mathf.Max(1, Mathf.RoundToInt(ticks * along)), () => Stars.Hit(ctx, victim, pathEffects, start));
                    }
                if (map == Find.CurrentMap && dist > 0.5f)
                {
                    mgr.Add(new VfxProjectile(Stars.Pos(star), end + new Vector3(0f, 0f, 0.35f), ticks, PMat.Get(PMat.StarSpike), ctx.Color2, Color.white, 1.5f, 0f, true, false)
                        { style = VfxStyle.Astral });
                    mgr.Add(new VfxStarLine(Stars.Pos(star), end + new Vector3(0f, 0f, 0.35f), ticks + 16, ctx.Color, ctx.Color2, 0.7f) { comet = true });
                }
                var comp = star.TryGetComp<CompArcaneStar>();
                if (comp != null) comp.hiddenUntil = now + ticks;
                if (star.Position != dest)
                {
                    star.DeSpawn();
                    GenSpawn.Spawn(star, dest, map, WipeMode.VanishOrMoveAside);
                }
            }
            int n = moving.Count;
            mgr.Schedule(longest, () =>
            {
                if (!arriveEffects.NullOrEmpty())
                    SpellRunner.Impact(ctx, new ImpactInfo { cell = dest, pos = end, radius = arriveRadius * ctx.RadiusFactor * (all ? 1f + 0.12f * (n - 1) : 1f), sourcePos = end,
                        scale = all ? 1f + bonusPerStar * (n - 1) : 1f }, arriveEffects, false, true);
                if (!all) return;
                // the stars have become one
                var keep = moving.FirstOrDefault(s => !s.Destroyed && Stars.IsGreat(s)) ?? moving.FirstOrDefault(s => !s.Destroyed);
                foreach (var s in moving)
                    if (s != keep && !s.Destroyed) s.Destroy(DestroyMode.Vanish);
            });
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return (all ? "AA_Desc_StarShootAll" : "AA_Desc_StarShoot").Translate(bonusPerStar.ToStringPercent());
            if (!pathEffects.NullOrEmpty())
            {
                yield return "AA_Desc_StarShootPath".Translate();
                foreach (var e in pathEffects)
                    foreach (var s in e.Describe(ctx)) yield return "  • " + s;
            }
            if (!arriveEffects.NullOrEmpty())
            {
                yield return "AA_Desc_StarShootArrive".Translate((arriveRadius * (ctx?.RadiusFactor ?? 1f)).ToString("0.#"));
                foreach (var e in arriveEffects)
                    foreach (var s in e.Describe(ctx)) yield return "  • " + s;
            }
        }
    }

    /// <summary>The caster steps through starlight to the star nearest the target point; the star flares as she arrives.</summary>
    public class Effect_StarcallerStep : SpellEffect
    {
        public List<SpellEffect> arriveEffects;
        public float arriveRadius = 3f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var caster = ctx.caster;
            var map = ctx.map;
            if (caster == null || !caster.Spawned || !ctx.Once("starstep")) return;
            var stars = Stars.Of(caster, map);
            if (Stars.None(ctx, stars)) return;
            var star = Stars.Nearest(stars, info.cell);
            IntVec3 dest = star.Position.Standable(map) && star.Position.GetFirstPawn(map) == null ? star.Position
                : CellFinder.StandableCellNear(star.Position, map, 2.5f, c => c.GetFirstPawn(map) == null);
            if (!dest.IsValid) return;
            if (map == Find.CurrentMap)
                ArcaneVfxManager.For(map).Add(new VfxStarLine(caster.DrawPos, Stars.Pos(star), 20, ctx.Color, ctx.Color2, 0.5f) { comet = true });
            Effect_Teleport.Blink(caster, dest, ctx);
            caster.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            if (!arriveEffects.NullOrEmpty())
                SpellRunner.Impact(ctx, new ImpactInfo { cell = star.Position, pos = star.DrawPos, radius = arriveRadius * ctx.RadiusFactor, sourcePos = star.DrawPos }, arriveEffects, false, true);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_StarStep".Translate();
            if (arriveEffects.NullOrEmpty()) yield break;
            yield return "AA_Desc_StarStepFlare".Translate((arriveRadius * (ctx?.RadiusFactor ?? 1f)).ToString("0.#"));
            foreach (var e in arriveEffects)
                foreach (var s in e.Describe(ctx)) yield return "  • " + s;
        }
    }

    /// <summary>Every star in range fires a lance of light at the target: the more stars around it, the more lances.</summary>
    public class Effect_StarcallerVolley : SpellEffect
    {
        public List<SpellEffect> effects;
        public float starRange = 16f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (effects.NullOrEmpty() || !ctx.Once("starvolley")) return;
            var map = ctx.map;
            var stars = Stars.Of(ctx.caster, map).Where(s => s.Position.DistanceTo(info.cell) <= starRange * Mathf.Clamp(ctx.RadiusFactor, 0.8f, 1.8f)).ToList();
            if (Stars.None(ctx, stars)) return;
            var target = info.primary;
            var mgr = ArcaneVfxManager.For(map);
            int i = 0;
            foreach (var star in stars)
            {
                int shots = Stars.IsGreat(star) ? 2 : 1;
                for (int k = 0; k < shots; k++)
                {
                    var from = star;
                    mgr.Schedule(1 + i * 5, () =>
                    {
                        if (from.Destroyed) return;
                        bool live = target != null && target.Spawned && !target.Destroyed;
                        Vector3 end = live ? target.DrawPos : info.pos;
                        IntVec3 cell = live ? target.Position : info.cell;
                        if (map == Find.CurrentMap)
                            mgr.Add(new VfxBeam(Stars.Pos(from), end, 12, ArcaneMats.Get("ArcanumAscendant/Vfx/Beam"), ctx.Color, ctx.Color2, 0.45f) { style = VfxStyle.Astral });
                        SpellRunner.Impact(ctx, new ImpactInfo { cell = cell, pos = end, primary = live ? target : null, radius = 0f, sourcePos = from.DrawPos }, effects, false, true);
                    });
                    i++;
                }
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_StarVolley".Translate((starRange * (ctx?.RadiusFactor ?? 1f)).ToString("F0"));
            foreach (var e in effects)
                foreach (var s in e.Describe(ctx)) yield return "  • " + s;
        }
    }

    /// <summary>The star nearest the target becomes a pulsar: two beams sweep around it, striking whatever they pass over.</summary>
    public class Effect_StarcallerPulsar : SpellEffect
    {
        public List<SpellEffect> effects;
        public int durationTicks = 600;
        public float beamRange = 7f;
        public float degreesPerTick = 3f;
        public float halfWidth = 11f;
        public int hitCooldown = 40;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (effects.NullOrEmpty() || !ctx.Once("pulsar")) return;
            var stars = Stars.Of(ctx.caster, ctx.map);
            if (Stars.None(ctx, stars)) return;
            var star = Stars.Nearest(stars, info.cell);
            ArcaneVfxManager.For(ctx.map).Add(new VfxPulsar(ctx, star, this, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), beamRange * ctx.RadiusFactor));
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_StarPulsar".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod(), (beamRange * (ctx?.RadiusFactor ?? 1f)).ToString("0.#"));
            foreach (var e in effects)
                foreach (var s in e.Describe(ctx)) yield return "  • " + s;
        }
    }

    public class VfxPulsar : Vfx
    {
        private readonly SpellContext ctx;
        private readonly Thing star;
        private readonly Effect_StarcallerPulsar def;
        private readonly float range;
        private float angle;
        private readonly Dictionary<Pawn, int> lastHit = new Dictionary<Pawn, int>();

        public VfxPulsar(SpellContext ctx, Thing star, Effect_StarcallerPulsar def, int ticks, float range)
        {
            this.ctx = ctx;
            this.star = star;
            this.def = def;
            this.range = range;
            duration = ticks;
            angle = Rand.Range(0f, 360f);
        }

        public override void Tick(int now, Map map)
        {
            base.Tick(now, map);
            if (star == null || star.Destroyed || !star.Spawned)
            {
                finished = true;
                return;
            }
            angle = Mathf.Repeat(angle + def.degreesPerTick, 360f);
            int age = now - startTick;
            if (age % 2 != 0) return;
            Vector3 c = star.DrawPos;
            foreach (var t in GenRadial.RadialDistinctThingsAround(star.Position, map, Mathf.Min(range, GenRadial.MaxRadialPatternRadius - 1f), true).ToList())
            {
                if (!(t is Pawn p) || p.Dead || !ctx.Passes(p, def.affects)) continue;
                Vector3 d = (p.DrawPos - c).Yto0();
                if (d.sqrMagnitude < 0.2f) continue;
                float bearing = d.AngleFlat();
                float diff = Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(bearing, angle)), Mathf.Abs(Mathf.DeltaAngle(bearing, angle + 180f)));
                if (diff > def.halfWidth) continue;
                if (lastHit.TryGetValue(p, out int last) && now - last < def.hitCooldown) continue;
                lastHit[p] = now;
                Stars.Hit(ctx, p, def.effects, c);
                if (map == Find.CurrentMap) Fx.Burst(map, p.DrawPos, ctx.Color2, 5f, PMat.Stardust, 2.2f, 0.5f, 0.2f, PFlags.Glint);
            }
            if (map == Find.CurrentMap && age % 4 == 0)
                for (int k = 0; k < 2; k++)
                {
                    Vector3 tip = c + VRand.Dir(angle + k * 180f) * range;
                    Fx.Burst(map, tip, ctx.Color, 1f, PMat.Stardust, 0.6f, 0.6f, 0.2f, PFlags.Glint);
                }
        }

        public override void Draw(float now)
        {
            if (star == null || star.Destroyed) return;
            float t = T(now);
            float a = FadeInOut(t, 0.06f, 0.12f);
            Vector3 c = Stars.Pos(star);
            c.y = VAlt.Glow;
            var beam = ArcaneMats.Get("ArcanumAscendant/Vfx/Beam");
            float sweep = angle + def.degreesPerTick * (now - Mathf.Floor(now));
            for (int k = 0; k < 2; k++)
            {
                Vector3 tip = c + VRand.Dir(sweep + k * 180f) * range;
                ArcaneMats.DrawStretched(beam, c, tip, 1.5f, ctx.Color * new Color(1, 1, 1, 0.35f * a));
                ArcaneMats.DrawStretched(beam, c + new Vector3(0, 0.002f, 0), tip + new Vector3(0, 0.002f, 0), 0.55f, Color.Lerp(ctx.Color2, Color.white, 0.5f) * new Color(1, 1, 1, 0.95f * a));
                ArcaneMats.DrawQuad(PMat.Get(PMat.StarSpike), tip + new Vector3(0, 0.004f, 0), 1.1f, now * 8f, ctx.Color2 * new Color(1, 1, 1, 0.8f * a));
            }
            ArcaneMats.DrawQuad(ArcaneMats.Glow, c + new Vector3(0, 0.006f, 0), 3.2f + 0.4f * Mathf.Sin(now * 0.9f), 0f, ctx.Color * new Color(1, 1, 1, 0.6f * a));
            ArcaneMats.DrawQuad(ArcaneMats.Ring, c + new Vector3(0, 0.008f, 0), 1.6f + Mathf.Repeat(now * 0.08f, 1f) * 2.2f, 0f, ctx.Color2 * new Color(1, 1, 1, (1f - Mathf.Repeat(now * 0.08f, 1f)) * a));
        }
    }

    /// <summary>A line of a constellation: it is drawn from one star to the next, glitters, then fades. With comet set it is a fading trail.</summary>
    public class VfxStarLine : Vfx
    {
        private readonly Vector3 a, b;
        private readonly Color color, color2;
        private readonly float width;
        public bool comet;

        public VfxStarLine(Vector3 a, Vector3 b, int ticks, Color color, Color color2, float width)
        {
            this.a = a;
            this.b = b;
            this.color = color;
            this.color2 = color2;
            this.width = width;
            duration = ticks;
        }

        public override void Tick(int now, Map map)
        {
            base.Tick(now, map);
            if (map != Find.CurrentMap || (now - startTick) % 3 != 0) return;
            Vector3 p = Vector3.Lerp(a, b, VRand.Value);
            Fx.Burst(map, p, Color.Lerp(color, color2, VRand.Value), 1f, PMat.Stardust, 0.25f, 0.9f, 0.17f, PFlags.Glint, 0f, comet ? 0.4f : 0f, 0.3f, 0.15f);
        }

        public override void Draw(float now)
        {
            float t = T(now);
            float age = now - startTick;
            float grow = comet ? 1f : Mathf.Clamp01(age / 7f);
            float alpha = comet ? Mathf.Clamp01(1f - t) : FadeInOut(t, 0.02f, 0.35f);
            float y = VAlt.Glow;
            Vector3 pa = a; pa.y = y;
            Vector3 pb = Vector3.Lerp(a, b, grow); pb.y = y;
            var mat = ArcaneMats.Get("ArcanumAscendant/Vfx/StarLine");
            float w = width * (comet ? Mathf.Lerp(1.3f, 0.3f, t) : 1f + 0.12f * Mathf.Sin(now * 0.7f));
            ArcaneMats.DrawStretched(mat, pa, pb, w * 2.6f, color * new Color(1, 1, 1, 0.3f * alpha));
            ArcaneMats.DrawStretched(mat, pa + new Vector3(0, 0.002f, 0), pb + new Vector3(0, 0.002f, 0), w, Color.Lerp(color2, Color.white, 0.5f) * new Color(1, 1, 1, 0.95f * alpha));
            if (comet) return;
            Vector3 d = pb - pa;
            for (int i = 0; i < 3; i++)
            {
                float f = Mathf.Repeat(age * 0.035f + i / 3f, 1f);
                Vector3 p = pa + d * f;
                p.y = y + 0.004f;
                ArcaneMats.DrawQuad(PMat.Get(PMat.StarSpike), p, width * 1.8f * Mathf.Sin(f * Mathf.PI), now * 6f, Color.white * new Color(1, 1, 1, alpha * Mathf.Sin(f * Mathf.PI)));
            }
            ArcaneMats.DrawQuad(PMat.Get(PMat.StarSpike), pb + new Vector3(0, 0.006f, 0), width * 2.6f * (grow < 1f ? 1.6f : 1f), now * 4f, color2 * new Color(1, 1, 1, alpha));
        }
    }

    // ------------------------------------------------------------------------------------------------
    // The moon
    // ------------------------------------------------------------------------------------------------

    public static class Moon
    {
        public const int New = 0, Waxing = 1, Full = 2, Waning = 3;
        private static HediffDef turned;
        private static bool looked;

        /// <summary>The moon's phase for this pawn: the sky's (four phases a quadrum), unless she has turned her own moon.</summary>
        public static int Phase(Pawn p)
        {
            if (!looked)
            {
                looked = true;
                turned = DefDatabase<HediffDef>.GetNamedSilentFail("AA_MoonTurned");
            }
            var h = turned != null ? p?.health?.hediffSet?.GetFirstHediffOfDef(turned) : null;
            if (h != null) return Mathf.Clamp(Mathf.RoundToInt(h.Severity) - 1, 0, 3);
            var map = p?.MapHeld ?? Find.AnyPlayerHomeMap;
            if (map == null) return New;
            return Mathf.Clamp(GenLocalDate.DayOfQuadrum(map) * 4 / 15, 0, 3);
        }

        public static string Label(int phase) => ("AA_Moon_" + phase).Translate();
    }

    /// <summary>Different effects under each phase of the moon.</summary>
    public class Effect_StarcallerMoonPhase : SpellEffect
    {
        public List<SpellEffect> always;
        public List<SpellEffect> newMoon;
        public List<SpellEffect> waxing;
        public List<SpellEffect> full;
        public List<SpellEffect> waning;

        public Effect_StarcallerMoonPhase()
        {
            affects = TargetFilter.All;
        }

        private List<SpellEffect> For(int phase)
        {
            switch (phase)
            {
                case Moon.New: return newMoon;
                case Moon.Waxing: return waxing;
                case Moon.Full: return full;
                default: return waning;
            }
        }

        private IEnumerable<SpellEffect> Active(SpellContext ctx)
        {
            if (always != null)
                foreach (var e in always) yield return e;
            var l = For(Moon.Phase(ctx.caster));
            if (l != null)
                foreach (var e in l) yield return e;
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            foreach (var e in Active(ctx))
                if (Rand.Chance(e.chance)) e.ApplyAtCell(ctx, info);
            if (ctx.caster != null && ctx.caster.Spawned && ctx.Once("moonphase")) ArcaneFx.Text(ctx.caster, Moon.Label(Moon.Phase(ctx.caster)), ctx.Color2);
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            foreach (var e in Active(ctx))
            {
                if (!e.AppliesToPawns || p.Dead || !ctx.Passes(p, e.affects)) continue;
                e.ApplyToPawn(ctx, p, info);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (always != null)
                foreach (var e in always)
                    foreach (var s in e.Describe(ctx)) yield return s;
            int cur = ctx?.caster != null ? Moon.Phase(ctx.caster) : -1;
            for (int ph = 0; ph < 4; ph++)
            {
                var l = For(ph);
                if (l.NullOrEmpty()) continue;
                yield return (ph == cur ? "AA_Desc_MoonNow" : "AA_Desc_MoonThen").Translate(Moon.Label(ph));
                foreach (var e in l)
                    foreach (var s in e.Describe(ctx)) yield return "  • " + s;
            }
        }
    }

    /// <summary>The caster turns her own moon one phase onward for a while.</summary>
    public class Effect_StarcallerTurnMoon : SpellEffect
    {
        public HediffDef hediff;
        public int durationTicks = 30000;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (hediff == null || p != ctx.caster) return;
            int next = (Moon.Phase(p) + 1) % 4;
            var old = p.health.hediffSet.GetFirstHediffOfDef(hediff);
            if (old != null) p.health.RemoveHediff(old);
            ArcaneUtility.ApplyHediff(p, hediff, next + 1, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), ctx);
            ArcaneFx.Text(p, Moon.Label(next), ctx.Color2);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_TurnMoon".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    /// <summary>Keeps a status on the mage whose severity is the phase of the moon, so each phase can carry its own gifts.</summary>
    public class P_StarcallerMoon : ArcanePassive
    {
        public HediffDef hediff;

        public override string Describe(int rank) => "AA_P_StarcallerMoon".Translate();

        public override void Tick(CompArcane c, int rank, int elapsed)
        {
            var p = c.Pawn;
            if (hediff == null || p == null || p.Dead || !p.IsHashIntervalTick(120, Mathf.Max(1, elapsed))) return;
            var h = p.health.hediffSet.GetFirstHediffOfDef(hediff);
            if (h == null)
            {
                h = HediffMaker.MakeHediff(hediff, p);
                p.health.AddHediff(h);
            }
            float want = Moon.Phase(p) + 1;
            if (Mathf.Abs(h.Severity - want) > 0.01f) h.Severity = want;
        }
    }

    /// <summary>Stronger under the night sky.</summary>
    public class P_StarcallerNight : ArcanePassive
    {
        public float nightPerRank = 0.06f;
        public float openSkyPerRank = 0.02f;

        public override string Describe(int rank) => "AA_P_StarcallerNight".Translate((nightPerRank * rank).ToStringPercent(), (openSkyPerRank * rank).ToStringPercent());

        public override float SpellPowerFactor(CompArcane c, int rank)
        {
            var p = c.Pawn;
            var map = p?.Map;
            if (map == null) return 1f;
            float f = 1f;
            if (map.skyManager.CurSkyGlow < 0.35f) f += nightPerRank * rank;
            if (!p.Position.Roofed(map)) f += openSkyPerRank * rank;
            return f;
        }
    }

    /// <summary>A crescent of moonlight flies out to the target point and comes back to the caster, striking on both passes.</summary>
    public class Effect_StarcallerCrescent : SpellEffect
    {
        public List<SpellEffect> outEffects;
        public List<SpellEffect> backEffects;
        public float width = 1.4f;
        public float speed = 20f;

        public override bool AppliesToPawns => false;

        private void Sweep(SpellContext ctx, Vector3 start, Vector3 end, int ticks, List<SpellEffect> effects)
        {
            var map = ctx.map;
            var mgr = ArcaneVfxManager.For(map);
            float dist = (end - start).MagnitudeHorizontal();
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist));
            for (int i = 0; i <= steps; i++)
            {
                float f = i / (float)steps;
                Vector3 pt = Vector3.Lerp(start, end, f);
                mgr.Schedule(1 + Mathf.RoundToInt(ticks * f), () =>
                {
                    var c = pt.ToIntVec3();
                    if (!c.InBounds(map)) return;
                    SpellRunner.Impact(ctx, new ImpactInfo { cell = c, pos = pt, radius = width * ctx.RadiusFactor, sourcePos = start }, effects, true, false);
                    if (map == Find.CurrentMap) Fx.Burst(map, pt, ctx.Color2, 2f, PMat.Stardust, 1.2f, 0.5f, 0.18f, PFlags.Glint);
                });
            }
            if (map == Find.CurrentMap)
                mgr.Add(new VfxProjectile(start, end, ticks, ArcaneMats.Get("ArcanumAscendant/Vfx/Crescent"), ctx.Color2, Color.white, 1.5f, 0f, true, false) { style = VfxStyle.Astral });
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var caster = ctx.caster;
            if (caster == null || !ctx.Once("crescent")) return;
            var map = ctx.map;
            Vector3 start = caster.DrawPos;
            Vector3 end = info.pos;
            float dist = (end - start).MagnitudeHorizontal();
            if (dist < 1f) return;
            int ticks = Mathf.Max(6, Mathf.RoundToInt(dist / speed * 60f));
            Sweep(ctx, start, end, ticks, outEffects);
            ArcaneVfxManager.For(map).Schedule(ticks + 3, () =>
            {
                ctx.hitThisCast.Clear();
                Vector3 home = caster.Spawned && caster.Map == map ? caster.DrawPos : start;
                Sweep(ctx, end, home, ticks, backEffects.NullOrEmpty() ? outEffects : backEffects);
            });
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Crescent".Translate();
            foreach (var e in outEffects)
                foreach (var s in e.Describe(ctx)) yield return "  • " + s;
            if (backEffects.NullOrEmpty()) yield break;
            yield return "AA_Desc_CrescentBack".Translate();
            foreach (var e in backEffects)
                foreach (var s in e.Describe(ctx)) yield return "  • " + s;
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Signs of the zodiac
    // ------------------------------------------------------------------------------------------------

    /// <summary>Puts a sign on the target. Only one sign of the group can be worn at a time unless keepOthers is set.</summary>
    public class Effect_StarcallerSign : SpellEffect
    {
        public HediffDef hediff;
        public List<HediffDef> group;
        public int durationTicks = -1;
        public bool keepOthers;

        public Effect_StarcallerSign()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (hediff == null) return;
            if (!keepOthers && group != null)
                foreach (var g in group)
                {
                    if (g == hediff) continue;
                    var h = p.health.hediffSet.GetFirstHediffOfDef(g);
                    if (h != null) p.health.RemoveHediff(h);
                }
            ArcaneUtility.ApplyHediff(p, hediff, 1f, durationTicks > 0 ? Mathf.RoundToInt(durationTicks * ctx.DurationFactor) : -1, ctx);
            if (p.Spawned && p.Map == Find.CurrentMap)
            {
                Fx.Rise(p.Map, p.DrawPos, ctx.Color2, 12f, PMat.Stardust, 0.9f, 0.9f, 1.2f, 0.2f, PFlags.Glint);
                ArcaneVfxManager.For(p.Map).Add(new VfxSprite { mat = PMat.Get(PMat.StarSpike), follow = p, pos = p.DrawPos, scaleStart = 3.4f, scaleEnd = 0.8f, duration = 20, color = ctx.Color2, spin = 60f });
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (hediff == null) yield break;
            string dur = durationTicks > 0 ? " " + "AA_Desc_For".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod()) : "";
            yield return "AA_Desc_Hediff".Translate(hediff.LabelCap) + dur;
            if (!keepOthers && group != null) yield return "AA_Desc_SignExclusive".Translate();
        }
    }

    /// <summary>Reads a person's natal sign (from the day they were born) and blesses them by it.</summary>
    public class Effect_StarcallerHoroscope : SpellEffect
    {
        public List<HediffDef> signs;
        public int durationTicks = 120000;

        public Effect_StarcallerHoroscope()
        {
            affects = TargetFilter.Allies;
        }

        public static int SignIndex(Pawn p, int count) => count <= 0 ? 0 : Mathf.Clamp((p.ageTracker?.BirthDayOfYear ?? 0) * count / 60, 0, count - 1);

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (signs.NullOrEmpty() || !p.RaceProps.Humanlike) return;
            var sign = signs[SignIndex(p, signs.Count)];
            foreach (var g in signs)
            {
                var h = p.health.hediffSet.GetFirstHediffOfDef(g);
                if (h != null) p.health.RemoveHediff(h);
            }
            ArcaneUtility.ApplyHediff(p, sign, 1f, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), ctx);
            ArcaneFx.Text(p, "AA_BornUnder".Translate(sign.label), ctx.Color2);
            if (p.Spawned && p.Map == Find.CurrentMap) Fx.Rise(p.Map, p.DrawPos, ctx.Color2, 12f, PMat.Stardust, 0.9f, 0.9f, 1.4f, 0.2f, PFlags.Glint);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Horoscope".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
            if (signs == null) yield break;
            foreach (var s in signs) yield return "  • " + s.LabelCap;
        }
    }

    /// <summary>The caster's own signs are lent to every ally in the area.</summary>
    public class Effect_StarcallerShareSign : SpellEffect
    {
        public List<HediffDef> group;
        public int durationTicks = 1200;

        public Effect_StarcallerShareSign()
        {
            affects = TargetFilter.AlliesNotSelf;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var caster = ctx.caster;
            if (group == null || caster == null || p == caster) return;
            foreach (var g in group)
            {
                if (!caster.health.hediffSet.HasHediff(g)) continue;
                ArcaneUtility.ApplyHediff(p, g, 1f, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), ctx);
                if (p.Spawned && p.Map == Find.CurrentMap)
                    ArcaneVfxManager.For(p.Map).Add(new VfxStarLine(caster.DrawPos, p.DrawPos, 26, ctx.Color, ctx.Color2, 0.4f));
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_ShareSign".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }
}

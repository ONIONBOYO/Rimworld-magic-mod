using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace ArcanumAscendant
{
    // ================================================================================================
    // Launching pawns through the air
    // ================================================================================================

    public enum LaunchMode
    {
        /// <summary>Thrown directly away from the caster.</summary>
        AwayFromCaster,
        /// <summary>Yanked through the air to land beside the caster.</summary>
        TowardCaster,
        /// <summary>Tossed straight up, landing a step or two away.</summary>
        Upward,
        /// <summary>The caster flies to the target cell.</summary>
        CasterToTarget,
        /// <summary>Thrown away from the centre of the impact.</summary>
        AwayFromImpact,
        /// <summary>Thrown toward the centre of the impact.</summary>
        TowardImpact,
    }

    /// <summary>
    /// Hurls pawns through the air with a real flight arc (vanilla PawnFlyer). They land with a crash: damage scales with
    /// the distance flown, a flight cut short by a wall becomes a wall slam, and the impact can splash nearby enemies.
    /// </summary>
    public class Effect_Launch : SpellEffect
    {
        public LaunchMode mode = LaunchMode.AwayFromCaster;
        public float distance = 6f;
        public float damage = 12f;
        public DamageDef damageDef;
        public float wallSlamFactor = 1.6f;
        public float splashRadius;
        public float splashFactor = 0.5f;
        public int stunTicks = 45;
        /// <summary>Creatures bigger than this are too heavy to throw.</summary>
        public float maxBodySize = 4f;
        public List<SpellEffect> landingEffects;
        public float landingRadius = 1.5f;
        /// <summary>Downed pawns can be carried too (for lifting a fallen ally out of danger).</summary>
        public bool allowDowned;

        public override bool AppliesToPawns => mode != LaunchMode.CasterToTarget;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (mode != LaunchMode.CasterToTarget) return;
            var caster = ctx.caster;
            if (caster == null || !caster.Spawned || caster.Downed) return;
            var map = caster.Map;
            IntVec3 dest = info.cell;
            if (!dest.InBounds(map)) return;
            if (!dest.Standable(map) || dest == caster.Position)
                dest = CellFinder.StandableCellNear(info.cell, map, 3f, c => c != caster.Position);
            if (!dest.IsValid) return;
            // fly once the cast has finished, so the casting verb never sees its caster vanish mid-shot
            ArcaneVfxManager.For(map).Schedule(1, () =>
            {
                if (caster.Spawned && !caster.Downed && caster.Map == map) Launch(ctx, caster, dest, false, true);
            });
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p == ctx.caster) return;
            if (!CanLaunch(p, out float sizeFactor))
            {
                // too heavy or pinned down: the blast still lands
                if (damage > 0f && p.Spawned) LandingDamage(ctx.caster, p, damage * ctx.DamageFactor * 0.6f, damageDef, ctx.Color);
                return;
            }
            var map = p.Map;
            Vector3 origin;
            switch (mode)
            {
                case LaunchMode.AwayFromCaster:
                case LaunchMode.TowardCaster:
                    origin = ctx.caster != null && ctx.caster.Spawned ? ctx.caster.DrawPos : info.pos;
                    break;
                case LaunchMode.AwayFromImpact:
                case LaunchMode.TowardImpact:
                    origin = info.pos;
                    break;
                default:
                    origin = p.DrawPos;
                    break;
            }
            Vector3 dir = (p.DrawPos - origin).Yto0();
            if (mode == LaunchMode.TowardCaster || mode == LaunchMode.TowardImpact) dir = -dir;
            float dist = distance * sizeFactor;
            if (mode == LaunchMode.Upward)
            {
                dir = VRand.Dir(VRand.Range(0f, 360f));
                dist = VRand.Range(1.4f, 2.6f);
            }
            else if (mode == LaunchMode.TowardCaster || mode == LaunchMode.TowardImpact)
            {
                dist = Mathf.Min(dist, Mathf.Max(0f, (p.DrawPos - origin).MagnitudeHorizontal() - 1.4f));
            }
            if (dir.sqrMagnitude < 0.0001f) dir = VRand.Dir(VRand.Range(0f, 360f));
            dir.Normalize();
            IntVec3 dest = FindLanding(map, p.Position, p.DrawPos, dir, dist, out bool slam);
            if (dest == p.Position)
            {
                if (damage > 0f) LandingDamage(ctx.caster, p, damage * ctx.DamageFactor * (slam ? wallSlamFactor : 0.7f), damageDef, ctx.Color);
                if (stunTicks > 0) p.stances?.stunner?.StunFor(Mathf.RoundToInt(stunTicks * ctx.DurationFactor), ctx.caster, false, true);
                return;
            }
            Launch(ctx, p, dest, slam, false);
        }

        public static bool CanLaunch(Pawn p, out float sizeFactor, float maxBodySize = 4f, bool allowDowned = false)
        {
            sizeFactor = 1f;
            if (p == null || !p.Spawned || p.Dead || (p.Downed && !allowDowned) || p.CarriedBy != null || p.InBed() || !(p.ParentHolder is Map)) return false;
            if (p.BodySize > maxBodySize) return false;
            if (p.BodySize > 1f) sizeFactor = 1f / Mathf.Sqrt(p.BodySize);
            if (p.kindDef?.isBoss ?? false) sizeFactor *= 0.35f;
            return true;
        }

        private bool CanLaunch(Pawn p, out float sizeFactor) => CanLaunch(p, out sizeFactor, maxBodySize, allowDowned);

        /// <summary>Walks the flight path and returns the last open cell. A wall, building or the map edge ends the flight early.</summary>
        public static IntVec3 FindLanding(Map map, IntVec3 start, Vector3 from, Vector3 dir, float dist, out bool slam)
        {
            slam = false;
            IntVec3 last = start;
            if (dist < 0.6f) return start;
            int steps = Mathf.CeilToInt(dist / 0.5f);
            for (int i = 1; i <= steps; i++)
            {
                var c = (from + dir * Mathf.Min(dist, i * 0.5f)).ToIntVec3();
                if (c == last) continue;
                if (!c.InBounds(map) || c.Impassable(map) || !c.Standable(map) || c.GetEdifice(map) is Building b && b.def.passability != Traversability.Standable)
                {
                    slam = c.InBounds(map);
                    break;
                }
                last = c;
            }
            return last;
        }

        public void Launch(SpellContext ctx, Pawn p, IntVec3 dest, bool slam, bool self)
        {
            var map = p.Map;
            var def = ArcaneDefOf.AA_WindFlyer;
            if (map == null || def == null) return;
            Vector3 start = p.DrawPos;
            float flown = (dest.ToVector3Shifted() - start).MagnitudeHorizontal();
            PawnFlyer flyer;
            try
            {
                flyer = PawnFlyer.MakeFlyer(def, p, dest, null, null);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Could not launch " + p + ": " + e.Message, 0x5A1C1A0);
                return;
            }
            if (flyer == null) return;
            if (flyer is ArcanePawnFlyer af)
            {
                af.caster = ctx.caster;
                af.spell = ctx.def;
                af.power = ctx.DamageFactor;
                af.damage = self ? 0f : damage;
                af.splashDamage = damage;
                af.damageDef = damageDef;
                af.slam = slam;
                af.slamFactor = wallSlamFactor;
                af.splashRadius = splashRadius * ctx.RadiusFactor;
                af.splashFactor = self ? 1f : splashFactor;
                af.stunTicks = self ? 0 : Mathf.RoundToInt(stunTicks * ctx.DurationFactor);
                af.flown = flown;
                af.selfLaunch = self;
                af.color = ctx.Color;
                af.color2 = ctx.Color2;
                af.source = this;
                af.effectIndex = ctx.props?.effects?.IndexOf(this) ?? -1;
                af.casterList = af.effectIndex < 0 && (ctx.props?.casterEffects?.Contains(this) ?? false);
                if (af.casterList) af.effectIndex = ctx.props.casterEffects.IndexOf(this);
            }
            GenSpawn.Spawn(flyer, dest, map);
            if (map == Find.CurrentMap)
            {
                Fx.RingBurst(map, start, new Color(0.9f, 0.95f, 1f, 0.55f), 10f, PMat.Dust, 0.4f, 2.2f, 0.9f, 0.7f);
                Fx.Burst(map, start, ctx.Color2, 8f, PMat.Gust, 2.5f, 0.6f, 0.5f, PFlags.AlignVel | PFlags.Stretch, 0f, 0f, 0.3f, 0.3f);
            }
        }

        public static void LandingDamage(Pawn instigator, Pawn victim, float amount, DamageDef def, Color color)
        {
            if (victim == null || victim.Dead || amount <= 0.5f) return;
            bool wasDead = victim.Dead;
            Vector3 pos = victim.DrawPos;
            float dealt = 0f;
            CompArcane.applyingSpellDamage = true;
            try
            {
                var dinfo = new DamageInfo(def ?? ArcaneDefOf.AA_Force, amount, 0.3f, -1f, instigator);
                dealt = victim.TakeDamage(dinfo)?.totalDamageDealt ?? 0f;
            }
            finally
            {
                CompArcane.applyingSpellDamage = false;
            }
            if (ArcanumMod.Settings.damageNumbers && victim.MapHeld != null) ArcaneFx.DamageNumber(victim.MapHeld, pos, dealt, false, color);
            if (!wasDead && victim.Dead) instigator?.TryGetComp<CompArcane>()?.Notify_Kill(victim, null);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (mode == LaunchMode.CasterToTarget) yield return "AA_Desc_LaunchSelf".Translate();
            else yield return ("AA_Desc_Launch_" + mode).Translate(distance.ToString("F0"), (damage * (ctx?.DamageFactor ?? 1f)).ToString("F0"));
        }
    }

    /// <summary>A pawn in flight. Lands with a crash and resolves the launching spell's impact.</summary>
    public class ArcanePawnFlyer : PawnFlyer
    {
        public Pawn caster;
        public AbilityDef spell;
        public float power = 1f;
        public float damage;
        public float splashDamage;
        public DamageDef damageDef;
        public bool slam;
        public float slamFactor = 1.6f;
        public float splashRadius;
        public float splashFactor = 0.5f;
        public int stunTicks;
        public float flown;
        public bool selfLaunch;
        public Color color = Color.white;
        public Color color2 = Color.white;
        public int effectIndex = -1;
        public bool casterList;
        [Unsaved] public Effect_Launch source;

        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!Spawned) return;
            var map = Map;
            if (map != Find.CurrentMap || !VRand.Chance(0.55f)) return;
            Vector3 p = DrawPos;
            Fx.Burst(map, p, color2 * new Color(1, 1, 1, 0.7f), 1f, PMat.Gust, 1.2f, 0.5f, 0.55f, PFlags.Stretch | PFlags.AlignVel, 0f, 0f, 0.1f, 0.3f);
            if (VRand.Chance(0.4f)) Fx.Burst(map, p, new Color(0.9f, 0.9f, 0.9f, 0.4f), 1f, PMat.Dust, 0.6f, 0.9f, 0.5f, PFlags.Lit, 40f, 0f, 0.1f, 0.2f);
        }

        protected override void RespawnPawn()
        {
            Pawn p = FlyingPawn;
            Map map = Map;
            IntVec3 cell = Position;
            base.RespawnPawn();
            if (p == null || map == null) return;
            try
            {
                Land(p, map, cell);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Landing error: " + e, 0x5A1C1A1);
            }
        }

        private Effect_Launch FindSource()
        {
            if (source != null) return source;
            var props = spell?.comps?.OfType<CompProperties_ArcaneSpell>().FirstOrDefault();
            if (props == null || effectIndex < 0) return null;
            var list = casterList ? props.casterEffects : props.effects;
            return effectIndex < list.Count ? list[effectIndex] as Effect_Launch : null;
        }

        private void Land(Pawn p, Map map, IntVec3 cell)
        {
            Vector3 pos = cell.ToVector3Shifted();
            float mult = power * (slam ? slamFactor : 1f) * Mathf.Lerp(0.6f, 1.25f, Mathf.Clamp01(flown / 8f));
            if (!selfLaunch && damage > 0f && p.Spawned) Effect_Launch.LandingDamage(caster, p, damage * mult, damageDef, color);
            if (!selfLaunch && stunTicks > 0 && p.Spawned && !p.Dead) p.stances?.stunner?.StunFor(stunTicks, caster, false, true);
            if (splashRadius > 0.5f && splashDamage > 0f)
            {
                foreach (var t in GenRadial.RadialDistinctThingsAround(cell, map, Mathf.Min(splashRadius, 12f), true).OfType<Pawn>().ToList())
                {
                    if (t == p || t == caster || t.Dead) continue;
                    if (caster != null ? !t.HostileTo(caster) : !t.HostileTo(p)) continue;
                    Effect_Launch.LandingDamage(caster, t, splashDamage * mult * splashFactor, damageDef, color);
                    if (!t.Dead && t.Spawned) t.stances?.stunner?.StunFor(30, caster, false, true);
                }
            }
            var src = FindSource();
            if (src != null && !src.landingEffects.NullOrEmpty() && caster != null && spell != null)
            {
                var ctx = SpellContext.For(caster, spell);
                if (ctx.props != null)
                {
                    ctx.map = map;
                    var info = new ImpactInfo { cell = cell, pos = pos, primary = selfLaunch ? null : p, radius = src.landingRadius * ctx.RadiusFactor, sourcePos = pos };
                    SpellRunner.Impact(ctx, info, src.landingEffects, playVfx: false);
                }
            }
            if (map == Find.CurrentMap)
            {
                float big = slam ? 1.5f : 1f;
                Fx.RingBurst(map, pos, new Color(0.85f, 0.85f, 0.8f, 0.7f), 12f * big, PMat.Dust, 0.4f, 2.6f, 1.2f, 0.9f);
                Fx.Debris(map, pos, new Color(0.55f, 0.5f, 0.45f, 1f), 6f * big, 3.5f, 0.18f);
                Fx.Burst(map, pos, color2, 10f * big, PMat.Gust, 3.5f, 0.5f, 0.55f, PFlags.AlignVel | PFlags.Stretch, 0f, 0f, 0.2f, 0.4f);
                ArcaneVfxManager.For(map).Add(new VfxSprite { mat = ArcaneMats.Shock, pos = pos, scaleStart = 0.3f, scaleEnd = 2.6f * big, duration = 16, color = color2 });
                ArcaneVfxManager.For(map).decals.Add(pos, DecalKind.Crack, 0.9f * big, new Color(1f, 1f, 1f, 0.6f), 14f);
                ArcaneFx.Shake(map, slam ? 0.9f : 0.35f);
                if (slam && !selfLaunch && p.Spawned) ArcaneFx.Text(p, "AA_WallSlam".Translate(), new Color(1f, 0.85f, 0.6f));
            }
            SoundDefOf.Pawn_Melee_Punch_HitPawn?.PlayOneShot(new TargetInfo(cell, map));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "aaCaster");
            Scribe_Defs.Look(ref spell, "aaSpell");
            Scribe_Defs.Look(ref damageDef, "aaDamageDef");
            Scribe_Values.Look(ref power, "aaPower", 1f);
            Scribe_Values.Look(ref damage, "aaDamage");
            Scribe_Values.Look(ref splashDamage, "aaSplashDamage");
            Scribe_Values.Look(ref slam, "aaSlam");
            Scribe_Values.Look(ref slamFactor, "aaSlamFactor", 1.6f);
            Scribe_Values.Look(ref splashRadius, "aaSplashRadius");
            Scribe_Values.Look(ref splashFactor, "aaSplashFactor", 0.5f);
            Scribe_Values.Look(ref stunTicks, "aaStun");
            Scribe_Values.Look(ref flown, "aaFlown");
            Scribe_Values.Look(ref selfLaunch, "aaSelf");
            Scribe_Values.Look(ref color, "aaColor", Color.white);
            Scribe_Values.Look(ref color2, "aaColor2", Color.white);
            Scribe_Values.Look(ref effectIndex, "aaEffectIndex", -1);
            Scribe_Values.Look(ref casterList, "aaCasterList");
        }
    }

    // ================================================================================================
    // Disarm
    // ================================================================================================

    /// <summary>Knocks the weapon out of a person's hands, and can pull it to the caster's feet.</summary>
    public class Effect_Disarm : SpellEffect
    {
        public bool steal = true;
        public float destroyChance;
        /// <summary>Damage dealt instead to anything that has no weapon to lose (animals, mechanoids, the unarmed).</summary>
        public float fallbackDamage = 20f;
        public DamageDef fallbackDamageDef;
        public bool affectsMechs;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var eq = p.equipment?.Primary;
            bool can = eq != null && !(p.kindDef?.isBoss ?? false) && (p.RaceProps.Humanlike || affectsMechs && p.RaceProps.IsMechanoid);
            if (!can)
            {
                if (fallbackDamage > 0f) new Effect_Damage { damageDef = fallbackDamageDef, amount = fallbackDamage }.ApplyToPawn(ctx, p, info);
                return;
            }
            Vector3 from = p.DrawPos;
            if (!p.equipment.TryDropEquipment(eq, out ThingWithComps dropped, p.PositionHeld, false)) return;
            if (p.Spawned) ArcaneFx.Text(p, "AA_Disarmed".Translate(), ctx.Color);
            if (dropped == null || dropped.Destroyed) return;
            var map = dropped.MapHeld;
            if (Rand.Chance(destroyChance))
            {
                if (map != null)
                {
                    Fx.Shards(map, from, ctx.Color2, 12f, 4f, 0.25f);
                    ArcaneFx.Flash(map, from, ctx.Color, 2f);
                }
                dropped.Destroy();
                return;
            }
            var caster = ctx.caster;
            if (steal && caster != null && caster.Spawned && map != null && caster.Map == map)
            {
                if (dropped.Spawned) dropped.DeSpawn();
                if (GenPlace.TryPlaceThing(dropped, caster.Position, map, ThingPlaceMode.Near))
                {
                    if (caster.Faction == Faction.OfPlayer) dropped.SetForbidden(false, false);
                    ArcaneVfxManager.For(map).Add(new VfxSoulWisp(from, caster, ctx.Color, ctx.Color2));
                }
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return (steal ? "AA_Desc_DisarmSteal" : "AA_Desc_Disarm").Translate() + Chance(chance);
        }
    }

    // ================================================================================================
    // Banish out of reality ("noclip")
    // ================================================================================================

    /// <summary>
    /// Pulls a pawn out of reality for a while. It is held safely as a world pawn and dropped back where it vanished
    /// (or as close as possible), dazed and hurting. Bosses slip back much sooner. Never banishes your own people.
    /// </summary>
    public class Effect_Banish : SpellEffect
    {
        public int durationTicks = 360;
        public float bossFactor = 0.35f;
        public float maxBodySize = 99f;
        public List<SpellEffect> returnEffects;
        public float returnRadius;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.Spawned || p.Dead || p == ctx.caster) return;
            if (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony || p.IsSlaveOfColony || p.HostFaction == Faction.OfPlayer) return;
            if (p.BodySize > maxBodySize) return;
            int dur = Mathf.RoundToInt(durationTicks * ctx.DurationFactor);
            if (p.kindDef?.isBoss ?? false) dur = Mathf.RoundToInt(dur * bossFactor);
            if (dur < 30) return;
            int index = ctx.props?.effects?.IndexOf(this) ?? -1;
            BanishUtility.Banish(p, dur, ctx, index);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Banish".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod()) + Chance(chance);
        }
    }

    public class BanishedPawn : IExposable
    {
        public Pawn pawn;
        public int returnTick;
        public int mapId = -1;
        public IntVec3 cell;
        public Faction faction;
        public Pawn source;
        public AbilityDef spell;
        public int effectIndex = -1;
        public Color color = Color.white;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn", true);
            Scribe_Values.Look(ref returnTick, "returnTick");
            Scribe_Values.Look(ref mapId, "mapId", -1);
            Scribe_Values.Look(ref cell, "cell");
            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref source, "source");
            Scribe_Defs.Look(ref spell, "spell");
            Scribe_Values.Look(ref effectIndex, "effectIndex", -1);
            Scribe_Values.Look(ref color, "color", Color.white);
        }
    }

    public static class BanishUtility
    {
        public static GameComponent_Arcanum Comp => Current.Game?.GetComponent<GameComponent_Arcanum>();

        public static void Banish(Pawn p, int ticks, SpellContext ctx, int effectIndex)
        {
            var comp = Comp;
            var map = p.Map;
            if (comp == null || map == null) return;
            Vector3 pos = p.DrawPos;
            var entry = new BanishedPawn
            {
                pawn = p, returnTick = Find.TickManager.TicksGame + ticks, mapId = map.uniqueID, cell = p.Position, faction = p.Faction,
                source = ctx?.caster, spell = ctx?.def, effectIndex = effectIndex, color = ctx?.Color ?? Color.white,
            };
            try
            {
                p.GetLord()?.Notify_PawnLost(p, PawnLostCondition.ExitedMap);
                p.jobs?.StopAll();
                p.DeSpawn();
                if (!Find.WorldPawns.Contains(p)) Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Could not banish " + p + ": " + e.Message, 0x5A1C1A2);
                if (!p.Spawned && !p.Destroyed && !p.Dead && p.ParentHolder == null)
                {
                    if (Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);
                    GenSpawn.Spawn(p, entry.cell, map);
                }
                return;
            }
            comp.banished.Add(entry);
            GlitchBurst(map, pos, entry.color, 1.2f);
            SoundDefOf.Psycast_Skip_Exit?.PlayOneShot(new TargetInfo(entry.cell, map));
            if (map == Find.CurrentMap) ArcaneVfxManager.For(map).screen.AddText(pos, "AA_Noclipped".Translate(), entry.color, 1.1f, ArcaneVfxManager.For(map).Now, 80f, true);
        }

        public static void Tick(GameComponent_Arcanum comp)
        {
            var list = comp.banished;
            if (list.Count == 0) return;
            int now = Find.TickManager.TicksGame;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var b = list[i];
                if (b.pawn == null || b.pawn.Destroyed || b.pawn.Dead)
                {
                    list.RemoveAt(i);
                    continue;
                }
                if (now < b.returnTick) continue;
                list.RemoveAt(i);
                try
                {
                    Return(b);
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[Arcanum Ascendant] Could not return a banished pawn: " + e, 0x5A1C1A3);
                }
            }
        }

        private static void Return(BanishedPawn b)
        {
            var p = b.pawn;
            if (p.Spawned) return;
            var map = Find.Maps.FirstOrDefault(m => m.uniqueID == b.mapId);
            if (map == null)
            {
                // the map is gone: let the world forget them normally
                if (Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide);
                return;
            }
            if (Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);
            if (p.ParentHolder != null && !(p.ParentHolder is Map)) return;
            IntVec3 c = b.cell;
            if (!c.InBounds(map) || !c.Standable(map)) c = CellFinder.StandableCellNear(b.cell.ClampInsideMap(map), map, 8f);
            if (!c.IsValid) c = CellFinder.RandomCell(map);
            GenSpawn.Spawn(p, c, map);
            var faction = p.Faction;
            if (faction != null && faction != Faction.OfPlayer && faction.HostileTo(Faction.OfPlayer) && (p.RaceProps.Humanlike || p.RaceProps.IsMechanoid) && p.GetLord() == null)
                LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, false, false, false, false, false, false, false), map, new[] { p });
            GlitchBurst(map, p.DrawPos, b.color, 1f);
            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(c, map));
            if (b.source != null && b.spell != null && b.effectIndex >= 0 && !b.source.Destroyed)
            {
                var ctx = SpellContext.For(b.source, b.spell);
                if (ctx.props != null && b.effectIndex < ctx.props.effects.Count && ctx.props.effects[b.effectIndex] is Effect_Banish eb && !eb.returnEffects.NullOrEmpty())
                {
                    ctx.map = map;
                    ctx.forcedTarget = p;
                    var info = new ImpactInfo { cell = c, pos = p.DrawPos, primary = p, radius = eb.returnRadius * ctx.RadiusFactor, sourcePos = p.DrawPos };
                    SpellRunner.Impact(ctx, info, eb.returnEffects, playVfx: false);
                }
            }
        }

        /// <summary>Static, glitching squares and a soft implosion: something stepped in or out of reality here.</summary>
        public static void GlitchBurst(Map map, Vector3 pos, Color c, float scale)
        {
            if (map == null || map != Find.CurrentMap) return;
            Fx.Burst(map, pos, c, 14f * scale, PMat.Glitch, 2.2f, 0.45f, 0.32f, PFlags.Flicker, 0f, 0f, 0.4f, 0.5f);
            Fx.Burst(map, pos, new Color(1f, 0.95f, 0.6f, 0.9f), 8f * scale, PMat.Glitch, 1.2f, 0.6f, 0.22f, PFlags.Flicker, 0f, 0f, 0.6f, 0.6f);
            Fx.Implode(map, pos, c, 10f * scale, 1.8f, 0.4f, -1, 0.14f, 6f);
            ArcaneVfxManager.For(map).Add(new VfxSprite { mat = ArcaneMats.Ring, pos = pos, scaleStart = 2.6f * scale, scaleEnd = 0.2f, duration = 14, color = c });
            ArcaneLightFlash.Spawn(map, pos, c, 3f, 14);
        }
    }

    // ================================================================================================
    // Timed sequences
    // ================================================================================================

    public class SequenceStep
    {
        public int delayTicks = 60;
        public float radius = -1f;
        public List<SpellEffect> effects = new List<SpellEffect>();
        public List<VfxSpec> vfx = new List<VfxSpec>();
        public float shake;
        public SoundDef sound;
        public string textKey;
        public Color textColor = Color.white;
    }

    /// <summary>Unfolds a series of effects over the impact area, one step after another.</summary>
    public class Effect_Sequence : SpellEffect
    {
        public List<SequenceStep> steps = new List<SequenceStep>();

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            if (map == null) return;
            var mgr = ArcaneVfxManager.For(map);
            int t = 0;
            foreach (var step in steps)
            {
                t += Mathf.Max(0, step.delayTicks);
                var s = step;
                mgr.Schedule(t, () =>
                {
                    if (ctx.map != map || !info.cell.InBounds(map)) return;
                    float r = s.radius >= 0f ? s.radius * ctx.RadiusFactor : info.radius;
                    ArcaneFx.PlayAll(s.vfx, map, info.pos, ctx, r);
                    s.sound?.PlayOneShot(new TargetInfo(info.cell, map));
                    if (s.shake > 0f) ArcaneFx.Shake(map, s.shake);
                    if (!s.textKey.NullOrEmpty() && map == Find.CurrentMap)
                        ArcaneVfxManager.For(map).screen.AddText(info.pos + new Vector3(0f, 0f, 1.2f), s.textKey.Translate(), s.textColor, 1.3f, ArcaneVfxManager.For(map).Now, 90f, true);
                    if (!s.effects.NullOrEmpty())
                        SpellRunner.Impact(ctx, new ImpactInfo { cell = info.cell, pos = info.pos, radius = r, sourcePos = info.sourcePos }, s.effects, playVfx: false);
                });
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            int total = steps.Sum(s => Mathf.Max(0, s.delayTicks));
            yield return "AA_Desc_Sequence".Translate(steps.Count, total.ToStringTicksToPeriod());
            foreach (var s in steps)
                foreach (var e in s.effects)
                    foreach (var line in e.Describe(ctx))
                        yield return "   " + line;
        }
    }

    // ================================================================================================
    // Spending stacks the caster has built up
    // ================================================================================================

    /// <summary>Used in casterEffects: reads stacks of a status on the caster and empowers the whole spell with them.</summary>
    public class Effect_Empower : SpellEffect
    {
        public HediffDef hediff;
        public float perStack = 0.1f;
        public float maxBonus = 4f;
        public bool consume = true;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (hediff == null || p != ctx.caster) return;
            var h = p.health?.hediffSet?.GetFirstHediffOfDef(hediff);
            if (h == null) return;
            float bonus = Mathf.Min(maxBonus, perStack * h.Severity);
            ctx.basePower *= 1f + bonus;
            if (consume) p.health.RemoveHediff(h);
            if (p.Spawned && bonus > 0.01f) ArcaneFx.Text(p, "AA_EmpowerText".Translate(bonus.ToStringPercent("F0")), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (hediff != null) yield return (consume ? "AA_Desc_EmpowerConsume" : "AA_Desc_Empower").Translate(hediff.label, perStack.ToStringPercent("F0"), maxBonus.ToStringPercent("F0"));
        }
    }

    // ================================================================================================
    // Copies of enemies
    // ================================================================================================

    /// <summary>Creates a copy of the target (same kind, own gear) that fights for the caster for a while.</summary>
    public class Effect_CopyPawn : SpellEffect
    {
        public int durationTicks = 2700;
        public float maxBodySize = 4f;
        public int maxCopies = 1;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p == ctx.caster || p.kindDef == null || (p.kindDef.isBoss) || p.BodySize > maxBodySize || ctx.caster == null) return;
            var map = ctx.map;
            if (map == null) return;
            int made = ctx.hitThisCast.Count(t => t is Pawn cp && cp.health?.hediffSet?.HasHediff(ArcaneDefOf.AA_Summoned) == true && cp.kindDef == p.kindDef);
            if (made >= maxCopies) return;
            var faction = ArcaneUtility.SummonFactionFor(ctx.caster, p);
            if (faction == null) return;
            Pawn copy;
            try
            {
                copy = PawnGenerator.GeneratePawn(new PawnGenerationRequest(p.kindDef, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false, allowFood: false));
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Could not copy " + p.kindDef + ": " + e.Message, 0x5A1C1A4);
                return;
            }
            IntVec3 c = CellFinder.StandableCellNear(p.Position, map, 3f, x => x.GetFirstPawn(map) == null);
            if (!c.IsValid) c = p.Position;
            GenSpawn.Spawn(copy, c, map);
            ArcaneUtility.MakeSummon(copy, ctx, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), ArcaneDefOf.AA_Summoned);
            ArcaneUtility.JoinCasterSide(ctx.caster, map, copy);
            ctx.hitThisCast.Add(copy);
            BanishUtility.GlitchBurst(map, copy.DrawPos, ctx.Color, 1f);
            ArcaneFx.Text(copy, "AA_SkinStolen".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_CopyPawn".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    // ================================================================================================
    // Wall patterns
    // ================================================================================================

    public enum SpawnPattern
    {
        Default,
        Ring,
        Area,
        /// <summary>A closed labyrinth filling the radius.</summary>
        Maze,
        /// <summary>A sealed room around the target.</summary>
        Box,
        /// <summary>Two parallel walls from the caster to the target.</summary>
        Corridor,
    }

    public static class SpawnPatterns
    {
        public static List<IntVec3> Cells(SpawnPattern pattern, SpellContext ctx, ImpactInfo info, int boxRadius, float corridorWidth)
        {
            var map = ctx.map;
            var cells = new List<IntVec3>();
            IntVec3 center = info.primary is Pawn tp && tp.Spawned ? tp.Position : info.cell;
            switch (pattern)
            {
                case SpawnPattern.Box:
                {
                    int r = Mathf.Max(1, boxRadius);
                    for (int dx = -r; dx <= r; dx++)
                        for (int dz = -r; dz <= r; dz++)
                            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) == r) cells.Add(center + new IntVec3(dx, 0, dz));
                    break;
                }
                case SpawnPattern.Corridor:
                {
                    if (ctx.caster == null) break;
                    Vector3 a = ctx.caster.Position.ToVector3Shifted(), b = info.cell.ToVector3Shifted();
                    Vector3 d = (b - a).Yto0();
                    float len = d.magnitude;
                    if (len < 1f) break;
                    d /= len;
                    var side = new Vector3(-d.z, 0f, d.x);
                    float off = corridorWidth * 0.5f + 0.5f;
                    var set = new HashSet<IntVec3>();
                    for (float s = 1f; s <= len + 0.5f; s += 0.5f)
                    {
                        set.Add((a + d * s + side * off).ToIntVec3());
                        set.Add((a + d * s - side * off).ToIntVec3());
                    }
                    cells.AddRange(set);
                    break;
                }
                case SpawnPattern.Maze:
                    cells.AddRange(Maze(center, Mathf.Max(3f, info.radius), map));
                    break;
            }
            return cells;
        }

        /// <summary>A perfect maze on a two-cell lattice centred on the target, closed by an outer wall.</summary>
        private static IEnumerable<IntVec3> Maze(IntVec3 center, float radius, Map map)
        {
            int R = Mathf.FloorToInt(radius);
            int n = R / 2;
            var rooms = new List<IntVec2>();
            var roomSet = new HashSet<IntVec2>();
            for (int i = -n; i <= n; i++)
                for (int j = -n; j <= n; j++)
                {
                    if (new Vector2(i * 2, j * 2).magnitude > radius - 1.2f) continue;
                    var r = new IntVec2(i, j);
                    rooms.Add(r);
                    roomSet.Add(r);
                }
            var open = new HashSet<IntVec3>();
            foreach (var r in rooms) open.Add(center + new IntVec3(r.x * 2, 0, r.z * 2));
            // depth-first carve
            var visited = new HashSet<IntVec2>();
            var stack = new Stack<IntVec2>();
            var start = new IntVec2(0, 0);
            if (roomSet.Contains(start))
            {
                stack.Push(start);
                visited.Add(start);
            }
            var dirs = new[] { new IntVec2(1, 0), new IntVec2(-1, 0), new IntVec2(0, 1), new IntVec2(0, -1) };
            var rng = new System.Random(center.x * 73856093 ^ center.z * 19349663 ^ Find.TickManager.TicksGame);
            while (stack.Count > 0)
            {
                var cur = stack.Peek();
                var options = dirs.Where(d => roomSet.Contains(new IntVec2(cur.x + d.x, cur.z + d.z)) && !visited.Contains(new IntVec2(cur.x + d.x, cur.z + d.z))).ToList();
                if (options.Count == 0)
                {
                    stack.Pop();
                    continue;
                }
                var d0 = options[rng.Next(options.Count)];
                var next = new IntVec2(cur.x + d0.x, cur.z + d0.z);
                open.Add(center + new IntVec3(cur.x * 2 + d0.x, 0, cur.z * 2 + d0.z));
                visited.Add(next);
                stack.Push(next);
            }
            // walls: every cell inside the radius that is not carved open
            foreach (var c in GenRadial.RadialCellsAround(center, Mathf.Min(radius, GenRadial.MaxRadialPatternRadius - 1f), true))
                if (!open.Contains(c)) yield return c;
        }
    }
}

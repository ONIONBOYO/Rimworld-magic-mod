using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArcanumAscendant
{
    /// <summary>
    /// Autocast logic. Player mages only autocast spells the player toggled on (and only while drafted unless the
    /// spell allows undrafted use). Non-player mages use every spell intelligently in combat.
    /// </summary>
    public static class ArcaneAI
    {
        public static void TryAutocast(CompArcane c)
        {
            var p = c.Pawn;
            if (p == null || !p.Spawned || p.Dead || p.Downed || p.Map == null) return;
            if (p.stances?.stunner?.Stunned ?? false) return;
            if (p.stances?.curStance is Stance_Warmup) return;
            if (p.CurJob?.ability != null) return;
            if (p.CurJobDef == ArcaneDefOf.AA_ArcaneChannel) return;
            if (p.InMentalState && !p.MentalStateDef.IsAggro) return;
            if (ArcanumMod.Settings.vfxIntensity < 0f) return;
            bool player = p.Faction == Faction.OfPlayer;
            if (player)
            {
                if (!ArcanumMod.Settings.allowAutocast) return;
                if (p.jobs?.curDriver?.asleep ?? false) return;
            }
            else if (!ArcanumMod.Settings.enemyMagesCast) return;

            var enemy = ArcaneUtility.NearestEnemy(p, 32f);
            bool inCombat = enemy != null;
            if (!player && !inCombat) return;

            var abilities = c.ArcaneAbilities().Where(a =>
            {
                if (player && !c.IsAutocast(a.def)) return false;
                if (player && !p.Drafted && !a.Ext.aiAllowedUndrafted) return false;
                if (a.Ext.role == SpellRole.Utility || a.Ext.role == SpellRole.Mobility) return false;
                if (a.Ext.quirky && !player) return false;
                if (!a.CanCast.Accepted) return false;
                // Autocast never overchannels: taking on strain is always the player's call.
                if (c.mana + 0.01f < a.ManaCost && !a.CanPayWithHealth(out _)) return false;
                return !a.GizmoDisabled(out _);
            }).ToList();
            if (abilities.Count == 0) return;

            foreach (var a in Prioritized(abilities, enemy))
            {
                if (TryPickTarget(c, a, enemy, out var target))
                {
                    Cast(p, a, target, player);
                    return;
                }
            }
        }

        private static IEnumerable<ArcaneAbility> Prioritized(List<ArcaneAbility> list, Pawn enemy)
        {
            int Score(ArcaneAbility a)
            {
                switch (a.Ext.role)
                {
                    case SpellRole.Heal: return 100;
                    case SpellRole.Summon: return enemy != null ? 70 : 0;
                    case SpellRole.Control: return 60;
                    case SpellRole.Buff: return 50;
                    case SpellRole.Attack: return 40 + (a.Ext.ultimate ? 30 : 0) + Mathf.RoundToInt(a.Ext.manaCost * 0.1f);
                }
                return 0;
            }
            return list.OrderByDescending(a => Score(a) + Rand.Range(0, 15));
        }

        private static bool TryPickTarget(CompArcane c, ArcaneAbility a, Pawn nearestEnemy, out LocalTargetInfo target)
        {
            target = LocalTargetInfo.Invalid;
            var p = c.Pawn;
            var props = a.SpellProps;
            if (props == null) return false;
            var map = p.Map;
            float range = a.verb?.EffectiveRange ?? 0f;
            bool selfCast = props.delivery == DeliveryType.Self || props.delivery == DeliveryType.Nova || !(a.def.verbProperties?.targetable ?? true);
            var ctx = SpellContext.For(p, a.def);
            float radius = (props.delivery == DeliveryType.Barrage ? props.barrageRadius : props.radius) * ctx.RadiusFactor;

            switch (a.Ext.role)
            {
                case SpellRole.Heal:
                {
                    var allies = AlliesAround(p, selfCast ? Mathf.Max(radius, 1f) : range)
                        .Where(x => x.health.summaryHealth.SummaryHealthPercent < a.Ext.aiHealthThreshold || x.health.HasHediffsNeedingTend())
                        .OrderBy(x => x.health.summaryHealth.SummaryHealthPercent).ToList();
                    if (allies.Count == 0) return false;
                    if (props.affects == TargetFilter.Self && !allies.Contains(p)) return false;
                    target = selfCast ? p : allies[0];
                    return selfCast || CanTarget(a, target);
                }
                case SpellRole.Buff:
                {
                    if (nearestEnemy == null) return false;
                    var hediffs = props.effects.Concat(props.casterEffects).OfType<Effect_Hediff>().Select(e => e.hediff).Where(h => h != null).ToList();
                    if (selfCast || props.affects == TargetFilter.Self)
                    {
                        if (hediffs.Any(h => p.health.hediffSet.HasHediff(h))) return false;
                        if (props.effects.OfType<Effect_Ward>().Any() && c.ward > c.WardMax * 0.5f && c.WardMax > 1f) return false;
                        target = p;
                        return true;
                    }
                    var ally = AlliesAround(p, range).Where(x => x != p || props.affects != TargetFilter.AlliesNotSelf)
                        .Where(x => !hediffs.Any(h => x.health.hediffSet.HasHediff(h)))
                        .OrderBy(x => x.Position.DistanceTo(nearestEnemy.Position)).FirstOrDefault();
                    if (ally == null) return false;
                    target = ally;
                    return CanTarget(a, target);
                }
                case SpellRole.Summon:
                    if (nearestEnemy == null) return false;
                    if (selfCast)
                    {
                        target = p;
                        return true;
                    }
                    if (props.effects.OfType<Effect_RaiseDead>().Any())
                    {
                        var corpse = GenRadial.RadialDistinctThingsAround(p.Position, map, Mathf.Min(range, GenRadial.MaxRadialPatternRadius - 1f), true)
                            .OfType<Corpse>().Where(x => x.InnerPawn != null && !x.InnerPawn.RaceProps.IsMechanoid).FirstOrDefault();
                        if (corpse == null) return false;
                        target = corpse;
                        return CanTarget(a, target);
                    }
                    target = CellFinder.StandableCellNear(Vector3.Lerp(p.DrawPos, nearestEnemy.DrawPos, 0.4f).ToIntVec3(), map, 3f);
                    return target.IsValid && CanTarget(a, target);
                default:
                {
                    if (nearestEnemy == null) return false;
                    if (props.delivery == DeliveryType.Global)
                    {
                        // map-wide spells are worth it against a real attack, not a lone straggler
                        int hostiles = map.mapPawns.AllPawnsSpawned.Count(x => !x.Dead && !x.Downed && x.HostileTo(p) && !x.Fogged() && !ArcaneUtility.IsDormant(x));
                        if (hostiles < Mathf.Max(3f, a.Ext.aiMinEnemiesInRadius)) return false;
                        target = p;
                        return true;
                    }
                    if (selfCast)
                    {
                        float r = Mathf.Max(radius, 2f);
                        int count = EnemiesAround(p, p.Position, r).Count();
                        if (count < Mathf.Max(1f, a.Ext.aiMinEnemiesInRadius)) return false;
                        target = p;
                        return true;
                    }
                    var enemies = EnemiesAround(p, p.Position, range).ToList();
                    if (enemies.Count == 0) return false;
                    Pawn best = null;
                    float bestScore = -1f;
                    foreach (var e in enemies)
                    {
                        if (!CanTarget(a, e)) continue;
                        float s = 1f;
                        if (radius > 0.5f)
                        {
                            int near = EnemiesAround(p, e.Position, radius).Count();
                            int allies = AlliesAround(p, 0f, e.Position, radius).Count();
                            if (allies > 0 && props.effects.OfType<Effect_Explosion>().Any()) continue;
                            s = near * 2f;
                        }
                        if (a.Ext.ultimate && NearCount(e) < 3 && !(e.kindDef?.isBoss ?? false) && e.BodySize < 2f) s *= 0.2f;
                        s += e.BodySize * 0.5f;
                        s -= e.Position.DistanceTo(p.Position) * 0.02f;
                        if (s > bestScore)
                        {
                            bestScore = s;
                            best = e;
                        }
                    }
                    if (best == null) return false;
                    if (radius > 0.5f && a.Ext.aiMinEnemiesInRadius > 1f && EnemiesAround(p, best.Position, radius).Count() < a.Ext.aiMinEnemiesInRadius) return false;
                    target = best;
                    return true;

                    int NearCount(Pawn e) => EnemiesAround(p, e.Position, 6f).Count();
                }
            }
        }

        private static bool CanTarget(ArcaneAbility a, LocalTargetInfo t)
        {
            var p = a.pawn;
            if (!t.IsValid) return false;
            float range = a.verb?.EffectiveRange ?? 0f;
            if (range > 0f && p.Position.DistanceTo(t.Cell) > range) return false;
            if (a.def.verbProperties != null && a.def.verbProperties.requireLineOfSight && !GenSight.LineOfSight(p.Position, t.Cell, p.Map, true)) return false;
            return a.verb == null || a.verb.ValidateTarget(t, false);
        }

        private static IEnumerable<Pawn> EnemiesAround(Pawn caster, IntVec3 center, float r)
        {
            r = Mathf.Min(r, GenRadial.MaxRadialPatternRadius - 1f);
            foreach (var t in GenRadial.RadialDistinctThingsAround(center, caster.Map, r, true))
                if (t is Pawn o && !o.Dead && !o.Downed && o.HostileTo(caster)) yield return o;
        }

        private static IEnumerable<Pawn> AlliesAround(Pawn caster, float r, IntVec3? center = null, float? rr = null)
        {
            var c = center ?? caster.Position;
            float radius = Mathf.Min(rr ?? r, GenRadial.MaxRadialPatternRadius - 1f);
            if (radius < 1f)
            {
                yield return caster;
                yield break;
            }
            foreach (var t in GenRadial.RadialDistinctThingsAround(c, caster.Map, radius, true))
                if (t is Pawn o && !o.Dead && o.Faction == caster.Faction && !o.HostileTo(caster)) yield return o;
        }

        private static void Cast(Pawn p, ArcaneAbility a, LocalTargetInfo target, bool player)
        {
            var job = a.GetJob(target, target);
            if (job == null) return;
            if (player)
            {
                if (p.Drafted) p.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                else p.jobs.StartJob(job, JobCondition.InterruptForced, null, true);
            }
            else
            {
                p.jobs.StartJob(job, JobCondition.InterruptForced, null, true);
            }
        }
    }
}

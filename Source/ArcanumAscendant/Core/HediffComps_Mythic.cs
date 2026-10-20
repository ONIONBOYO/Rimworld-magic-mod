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
    /// <summary>Shared helpers for status effects that remember who applied them.</summary>
    public static class SourcedHediff
    {
        /// <summary>A spell context for effects caused by a status: the original caster and spell when known.</summary>
        public static SpellContext Context(Pawn source, AbilityDef spell, Map map, Color color)
        {
            SpellContext ctx = null;
            if (source != null && spell != null)
            {
                ctx = SpellContext.For(source, spell);
                if (ctx.props == null) ctx = null;
            }
            if (ctx == null)
            {
                ctx = new SpellContext
                {
                    caster = source, mods = new SpellModSummary(), arcane = source?.TryGetComp<CompArcane>(),
                    props = new CompProperties_ArcaneSpell { color = color, color2 = color },
                };
            }
            ctx.map = map;
            return ctx;
        }

        /// <summary>Runs an action next tick: status callbacks often fire while the game is iterating a pawn's hediffs.
        /// Off the map (a caravan, a transport pod) it runs on the next game tick instead.</summary>
        public static void Later(Map map, Action a)
        {
            if (a == null) return;
            if (map != null) ArcaneVfxManager.For(map).Schedule(1, a);
            else Current.Game?.GetComponent<GameComponent_Arcanum>()?.RunNextTick(a);
        }
    }

    // ================================================================================================
    // Stacks that detonate
    // ================================================================================================

    public class HediffCompProperties_StackTrigger : HediffCompProperties
    {
        public float threshold = 7f;
        /// <summary>Optional higher threshold for bosses.</summary>
        public float bossThreshold = -1f;
        /// <summary>Applied to the bearer when the stacks detonate.</summary>
        public List<SpellEffect> effects = new List<SpellEffect>();
        /// <summary>Applied around the bearer (never to the bearer itself).</summary>
        public List<SpellEffect> spreadEffects = new List<SpellEffect>();
        public float spreadRadius = 4f;
        public List<VfxSpec> vfx = new List<VfxSpec>();
        public Color color = Color.white;
        public string labelKey;
        public float shake;
        public SoundDef sound;

        public HediffCompProperties_StackTrigger()
        {
            compClass = typeof(HediffComp_StackTrigger);
        }
    }

    /// <summary>
    /// A status that builds in stacks (its severity) and detonates at a threshold: the stacks are spent and the effects
    /// resolve with the original caster's power. Spread effects can push stacks onto nearby enemies, so detonations chain
    /// across a crowd one tick at a time.
    /// </summary>
    public class HediffComp_StackTrigger : HediffComp, IArcaneSourced
    {
        public Pawn source;
        public AbilityDef spell;
        private bool pending;

        public HediffCompProperties_StackTrigger Props => (HediffCompProperties_StackTrigger)props;

        public float Threshold => (Pawn?.kindDef?.isBoss ?? false) && Props.bossThreshold > 0f ? Props.bossThreshold : Props.threshold;

        public override string CompLabelInBracketsExtra => Mathf.FloorToInt(parent.Severity + 0.001f) + "/" + Threshold.ToString("F0");

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster == null) return;
            source = ctx.caster;
            if (ctx.def != null) spell = ctx.def;
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Check();
        }

        public void Check()
        {
            if (pending || parent.Severity + 0.001f < Threshold) return;
            var p = Pawn;
            // a bearer in flight, being carried or in a pod waits until it is back on the ground
            if (p == null || p.Dead || !p.Spawned) return;
            var map = p.Map;
            pending = true;
            SourcedHediff.Later(map, Fire);
        }

        private void Fire()
        {
            pending = false;
            var p = Pawn;
            if (p == null || p.Dead || !p.Spawned || p.health == null || !p.health.hediffSet.hediffs.Contains(parent)) return;
            if (parent.Severity + 0.001f < Threshold) return;
            var map = p.MapHeld;
            if (map == null) return;
            var def = parent.def;
            IntVec3 cell = p.PositionHeld;
            Vector3 pos = p.SpawnedOrAnyParentSpawned ? p.DrawPos : cell.ToVector3Shifted();
            p.health.RemoveHediff(parent);

            var ctx = SourcedHediff.Context(source, spell, map, Props.color);
            ctx.forcedTarget = p;
            if (map == Find.CurrentMap)
            {
                ArcaneFx.PlayAll(Props.vfx, map, pos, ctx, Props.spreadRadius);
                if (!Props.labelKey.NullOrEmpty())
                    ArcaneVfxManager.For(map).screen.AddText(pos + new Vector3(0f, 0f, 0.9f), Props.labelKey.Translate(), Props.color, 1.35f, ArcaneVfxManager.For(map).Now, 80f, true);
                if (Props.shake > 0f) ArcaneFx.Shake(map, Props.shake);
                ArcaneLightFlash.Spawn(map, pos, Props.color, 4f, 18);
            }
            Props.sound?.PlayOneShot(new TargetInfo(cell, map));
            if (!Props.effects.NullOrEmpty())
                SpellRunner.Impact(ctx, new ImpactInfo { cell = cell, pos = pos, primary = p, radius = 0f, sourcePos = pos }, Props.effects, playVfx: false);
            if (!Props.spreadEffects.NullOrEmpty())
            {
                var info = new ImpactInfo { cell = cell, pos = pos, primary = null, radius = Props.spreadRadius * ctx.RadiusFactor, sourcePos = pos, exclude = p };
                SpellRunner.Impact(ctx, info, Props.spreadEffects, playVfx: false);
            }
            source?.TryGetComp<CompArcane>()?.Notify_StackTriggered(p, def);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "source");
            Scribe_Defs.Look(ref spell, "spell");
        }
    }

    // ================================================================================================
    // Deferred damage
    // ================================================================================================

    public class HediffCompProperties_DelayedDoom : HediffCompProperties
    {
        /// <summary>Fraction of all damage taken while marked that is dealt again when the mark ends.</summary>
        public float storedFactor = 1f;
        /// <summary>Extra damage as a fraction of the bearer's lethal damage threshold.</summary>
        public float lethalPercent;
        public HediffDef stackHediff;
        public float perStackLethal;
        public bool consumeStacks = true;
        public DamageDef damageDef;
        public float bossFactor = 0.5f;
        public Color color = new Color(1f, 0.25f, 0.3f);
        public string labelKey = "AA_DoomFalls";
        public float shake = 0.8f;

        public HediffCompProperties_DelayedDoom()
        {
            compClass = typeof(HediffComp_DelayedDoom);
        }
    }

    /// <summary>Remembers every wound the bearer takes, then delivers it all again when the mark runs out.</summary>
    public class HediffComp_DelayedDoom : HediffComp, IArcaneSourced
    {
        public float stored;
        public Pawn source;
        public float power = 1f;
        private static bool resolving;

        public HediffCompProperties_DelayedDoom Props => (HediffCompProperties_DelayedDoom)props;

        public override string CompLabelInBracketsExtra => Props.storedFactor > 0f && stored > 0.5f ? stored.ToString("F0") : null;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster != null) source = ctx.caster;
            if (ctx != null) power = Mathf.Max(power, Mathf.Sqrt(Mathf.Max(0.3f, ctx.DamageFactor)));
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (!resolving && totalDamageDealt > 0f) stored += totalDamageDealt;
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            var p = Pawn;
            if (p == null || p.Dead) return;
            // only a mark that runs its course comes due; one that is cleansed or dispelled simply fades
            var timer = parent.TryGetComp<HediffComp_Disappears>();
            if (timer != null && timer.ticksToDisappear > 0) return;
            float st = stored;
            var src = source;
            float pw = power;
            var pr = Props;
            SourcedHediff.Later(p.MapHeld, () => Resolve(p, st, src, pw, pr));
        }

        private static void Resolve(Pawn p, float stored, Pawn source, float power, HediffCompProperties_DelayedDoom pr)
        {
            if (p == null || p.Dead || p.Destroyed || p.health == null) return;
            float pct = pr.lethalPercent;
            if (pr.stackHediff != null && pr.perStackLethal > 0f)
            {
                var sh = p.health.hediffSet.GetFirstHediffOfDef(pr.stackHediff);
                if (sh != null)
                {
                    pct += pr.perStackLethal * sh.Severity;
                    if (pr.consumeStacks) p.health.RemoveHediff(sh);
                }
            }
            float amount = stored * pr.storedFactor + p.health.LethalDamageThreshold * pct * power;
            if (p.kindDef?.isBoss ?? false) amount *= pr.bossFactor;
            if (amount < 1f) return;
            Vector3 pos = p.DrawPos;
            var map = p.MapHeld;
            bool wasDead = p.Dead;
            float dealt = 0f;
            resolving = true;
            bool prevSpellDamage0 = CompArcane.applyingSpellDamage;
            CompArcane.applyingSpellDamage = true;
            try
            {
                var dinfo = new DamageInfo(pr.damageDef ?? ArcaneDefOf.AA_ArcaneTrue, amount, 999f, -1f, source);
                dinfo.SetIgnoreArmor(true);
                dealt = p.TakeDamage(dinfo)?.totalDamageDealt ?? 0f;
            }
            finally
            {
                resolving = false;
                CompArcane.applyingSpellDamage = prevSpellDamage0;
            }
            if (!wasDead && p.Dead) source?.TryGetComp<CompArcane>()?.Notify_Kill(p, null);
            if (map == null || map != Find.CurrentMap) return;
            var mgr = ArcaneVfxManager.For(map);
            mgr.Add(new VfxPillar(pos, 30, pr.color, Color.white, 1.1f));
            Fx.Implode(map, pos, pr.color, 16f, 2.4f, 0.4f, -1, 0.16f, 8f);
            Fx.Burst(map, pos, pr.color, 12f, PMat.Ember, 3.5f, 0.8f, 0.2f, PFlags.Stretch | PFlags.AlignVel);
            mgr.screen.AddText(pos + new Vector3(0f, 0f, 0.9f), pr.labelKey.Translate(), pr.color, 1.3f, mgr.Now, 80f, true);
            if (ArcanumMod.Settings.damageNumbers) ArcaneFx.DamageNumber(map, pos, dealt, true, pr.color);
            ArcaneFx.Shake(map, pr.shake);
            ArcaneLightFlash.Spawn(map, pos, pr.color, 4f, 20);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref stored, "stored");
            Scribe_References.Look(ref source, "source");
            Scribe_Values.Look(ref power, "power", 1f);
        }
    }

    // ================================================================================================
    // Something happens when the bearer dies
    // ================================================================================================

    public class HediffCompProperties_DeathTrigger : HediffCompProperties
    {
        /// <summary>Applied to the caster who placed the mark.</summary>
        public List<SpellEffect> casterEffects = new List<SpellEffect>();
        /// <summary>Applied around the place the bearer died.</summary>
        public List<SpellEffect> areaEffects = new List<SpellEffect>();
        public float areaRadius = 5f;
        public List<VfxSpec> vfx = new List<VfxSpec>();
        public Color color = Color.white;
        public string labelKey;
        public float shake;

        public HediffCompProperties_DeathTrigger()
        {
            compClass = typeof(HediffComp_DeathTrigger);
        }
    }

    public class HediffComp_DeathTrigger : HediffComp, IArcaneSourced
    {
        public Pawn source;
        public AbilityDef spell;

        public HediffCompProperties_DeathTrigger Props => (HediffCompProperties_DeathTrigger)props;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster == null) return;
            source = ctx.caster;
            if (ctx.def != null) spell = ctx.def;
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            var p = Pawn;
            var map = p.Corpse?.MapHeld ?? p.MapHeld;
            if (map == null) return;
            IntVec3 cell = p.Corpse?.PositionHeld ?? p.PositionHeld;
            var src = source;
            var sp = spell;
            var pr = Props;
            SourcedHediff.Later(map, () =>
            {
                var ctx = SourcedHediff.Context(src, sp, map, pr.color);
                Vector3 pos = cell.ToVector3Shifted();
                if (map == Find.CurrentMap)
                {
                    ArcaneFx.PlayAll(pr.vfx, map, pos, ctx, pr.areaRadius);
                    if (!pr.labelKey.NullOrEmpty())
                        ArcaneVfxManager.For(map).screen.AddText(pos + new Vector3(0f, 0f, 1f), pr.labelKey.Translate(), pr.color, 1.4f, ArcaneVfxManager.For(map).Now, 90f, true);
                    if (pr.shake > 0f) ArcaneFx.Shake(map, pr.shake);
                }
                if (!pr.areaEffects.NullOrEmpty())
                    SpellRunner.Impact(ctx, new ImpactInfo { cell = cell, pos = pos, radius = pr.areaRadius * ctx.RadiusFactor, sourcePos = pos }, pr.areaEffects, playVfx: false);
                if (src != null && !src.Dead && !pr.casterEffects.NullOrEmpty())
                {
                    var selfInfo = new ImpactInfo { cell = src.PositionHeld, pos = src.SpawnedOrAnyParentSpawned ? src.DrawPos : src.PositionHeld.ToVector3Shifted(), primary = src, sourcePos = pos };
                    foreach (var e in pr.casterEffects)
                    {
                        if (e == null || !Rand.Chance(e.chance)) continue;
                        e.ApplyAtCell(ctx, selfInfo);
                        if (e.AppliesToPawns && !src.Dead) e.ApplyToPawn(ctx, src, selfInfo);
                    }
                    if (src.Spawned && src.Map == Find.CurrentMap) ArcaneVfxManager.For(src.Map).Add(new VfxSoulWisp(pos, src, pr.color, Color.white));
                }
            });
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "source");
            Scribe_Defs.Look(ref spell, "spell");
        }
    }

    // ================================================================================================
    // Compulsion: charm, terror, disorientation
    // ================================================================================================

    public enum CompelMode
    {
        /// <summary>Walks toward the caster in a daze and does not fight.</summary>
        Approach,
        /// <summary>Runs from the caster in terror.</summary>
        Flee,
        /// <summary>Wanders aimlessly, lost.</summary>
        Wander,
        /// <summary>Stands still, transfixed.</summary>
        Hold,
        /// <summary>Can only think of attacking the caster (a paladin's challenge).</summary>
        Attack,
    }

    public class HediffCompProperties_Compel : HediffCompProperties
    {
        public CompelMode mode = CompelMode.Approach;
        public bool breakOnDamage;
        public float breakDamage = 1f;
        public bool affectsMechs;
        public int interval = 20;
        public float approachDistance = 2.4f;
        public float fleeDistance = 14f;

        public HediffCompProperties_Compel()
        {
            compClass = typeof(HediffComp_Compel);
        }
    }

    /// <summary>Overrides what the bearer is doing every fraction of a second, so it cannot get on with fighting.</summary>
    public class HediffComp_Compel : HediffComp_Throttled, IArcaneSourced
    {
        public Pawn source;
        public IntVec3 anchor = IntVec3.Invalid;
        private bool broken;
        private int acc;

        public HediffCompProperties_Compel Props => (HediffCompProperties_Compel)props;

        public override bool CompShouldRemove => broken;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster != null)
            {
                source = ctx.caster;
                if (ctx.caster.SpawnedOrAnyParentSpawned) anchor = ctx.caster.PositionHeld;
            }
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            acc = 999;
            if (Pawn != null && Pawn.RaceProps.IsMechanoid && !Props.affectsMechs) broken = true;
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (Props.breakOnDamage && totalDamageDealt >= Props.breakDamage && dinfo.Def != null && dinfo.Def.harmsHealth) broken = true;
        }

        protected override void Process(int elapsed)
        {
            acc += elapsed;
            if (acc < Props.interval) return;
            acc = 0;
            var p = Pawn;
            if (broken || p == null || !p.Spawned || p.Dead || p.Downed || p.jobs == null) return;
            if (p.stances?.stunner?.Stunned ?? false) return;
            try
            {
                Act(p);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Compel error: " + e.Message, 0x5A1C1B0);
                broken = true;
            }
        }

        private void Act(Pawn p)
        {
            var map = p.Map;
            bool srcHere = source != null && source.Spawned && source.Map == map && !source.Dead;
            IntVec3 focus = srcHere ? source.Position : anchor.IsValid ? anchor : p.Position;
            Job job = null;
            switch (Props.mode)
            {
                case CompelMode.Approach:
                    if (p.Position.DistanceTo(focus) > Props.approachDistance)
                    {
                        IntVec3 c = CellFinder.StandableCellNear(focus, map, 2f, x => x != focus);
                        if (!c.IsValid) c = focus;
                        if (p.CurJobDef == JobDefOf.Goto && p.CurJob.targetA.Cell.DistanceTo(c) < 2f) return;
                        job = JobMaker.MakeJob(JobDefOf.Goto, c, Props.interval * 3, true);
                        job.locomotionUrgency = LocomotionUrgency.Walk;
                    }
                    else
                    {
                        if (p.CurJobDef == JobDefOf.Wait) return;
                        job = JobMaker.MakeJob(JobDefOf.Wait, Props.interval * 3);
                    }
                    break;
                case CompelMode.Flee:
                {
                    if (p.CurJobDef == JobDefOf.Flee) return;
                    IntVec3 dest = IntVec3.Invalid;
                    if (srcHere) dest = CellFinderLoose.GetFleeDest(p, new List<Thing> { source }, Props.fleeDistance);
                    else RCellFinder.TryFindDirectFleeDestination(focus, Props.fleeDistance, p, out dest);
                    if (!dest.IsValid || dest == p.Position) dest = CellFinder.RandomClosewalkCellNear(p.Position, map, 8);
                    job = JobMaker.MakeJob(JobDefOf.Flee, dest, srcHere ? (LocalTargetInfo)source : LocalTargetInfo.Invalid);
                    job.locomotionUrgency = LocomotionUrgency.Sprint;
                    job.expiryInterval = Props.interval * 6;
                    break;
                }
                case CompelMode.Wander:
                {
                    if (p.CurJobDef == JobDefOf.Goto && p.pather.Moving) return;
                    IntVec3 c = CellFinder.RandomClosewalkCellNear(p.Position, map, 6);
                    job = JobMaker.MakeJob(JobDefOf.Goto, c, Props.interval * 4, true);
                    job.locomotionUrgency = LocomotionUrgency.Amble;
                    break;
                }
                case CompelMode.Hold:
                    if (p.CurJobDef == JobDefOf.Wait) return;
                    job = JobMaker.MakeJob(JobDefOf.Wait, Props.interval * 3);
                    break;
                case CompelMode.Attack:
                {
                    if (!srcHere || source.Downed)
                    {
                        broken = true;
                        return;
                    }
                    if (p.CurJob != null && p.CurJob.targetA.Thing == source && (p.CurJobDef == JobDefOf.AttackMelee || p.CurJobDef == JobDefOf.AttackStatic)) return;
                    var verb = p.TryGetAttackVerb(source, !p.IsColonist);
                    if (verb == null) return;
                    job = JobMaker.MakeJob(verb.IsMeleeAttack ? JobDefOf.AttackMelee : JobDefOf.AttackStatic, source);
                    job.expiryInterval = Props.interval * 6;
                    job.checkOverrideOnExpire = true;
                    if (p.mindState != null) p.mindState.enemyTarget = source;
                    break;
                }
            }
            if (job != null) p.jobs.StartJob(job, JobCondition.InterruptForced);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "source");
            Scribe_Values.Look(ref anchor, "anchor", IntVec3.Invalid);
            Scribe_Values.Look(ref broken, "broken");
        }
    }

    // ================================================================================================
    // Shared fate: damage spreads along a link
    // ================================================================================================

    public class HediffCompProperties_DamageLink : HediffCompProperties
    {
        public float shareFraction = 0.4f;
        public DamageDef damageDef;
        public Color color = new Color(1f, 0.3f, 0.4f);

        public HediffCompProperties_DamageLink()
        {
            compClass = typeof(HediffComp_DamageLink);
        }
    }

    /// <summary>Everyone bound by the same caster shares a part of every wound any of them takes.</summary>
    public class HediffComp_DamageLink : HediffComp, IArcaneSourced
    {
        public Pawn source;
        private static bool sharing;

        public HediffCompProperties_DamageLink Props => (HediffCompProperties_DamageLink)props;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster != null) source = ctx.caster;
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (sharing || totalDamageDealt < 0.5f) return;
            var p = Pawn;
            var map = p?.MapHeld;
            if (map == null) return;
            var others = new List<Pawn>();
            foreach (var o in map.mapPawns.AllPawnsSpawned)
            {
                if (o == p || o.Dead || o.health?.hediffSet == null) continue;
                if (!(o.health.hediffSet.GetFirstHediffOfDef(parent.def) is HediffWithComps hc)) continue;
                var link = hc.TryGetComp<HediffComp_DamageLink>();
                if (link != null && link.source == source) others.Add(o);
            }
            if (others.Count == 0) return;
            float share = totalDamageDealt * Props.shareFraction;
            Vector3 from = p.SpawnedOrAnyParentSpawned ? p.DrawPos : p.PositionHeld.ToVector3Shifted();
            sharing = true;
            bool prevSpellDamage1 = CompArcane.applyingSpellDamage;
            CompArcane.applyingSpellDamage = true;
            try
            {
                foreach (var o in others)
                {
                    bool wasDead = o.Dead;
                    var d = new DamageInfo(Props.damageDef ?? ArcaneDefOf.AA_ArcaneTrue, share, 999f, -1f, source);
                    d.SetIgnoreArmor(true);
                    o.TakeDamage(d);
                    if (!wasDead && o.Dead) source?.TryGetComp<CompArcane>()?.Notify_Kill(o, null);
                    if (map == Find.CurrentMap && o.Spawned)
                        ArcaneVfxManager.For(map).Add(new VfxBeam(from, o.DrawPos, 10, ArcaneMats.Get("ArcanumAscendant/Vfx/Beam"), Props.color, Color.white, 0.25f));
                }
            }
            finally
            {
                sharing = false;
                CompArcane.applyingSpellDamage = prevSpellDamage1;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "source");
        }
    }

    // ================================================================================================
    // Stacks that grow whenever the bearer is hurt
    // ================================================================================================

    public class HediffCompProperties_StackOnDamaged : HediffCompProperties
    {
        public HediffDef hediff;
        public float severity = 1f;
        public float maxSeverity = -1f;
        public int cooldownTicks = 20;

        public HediffCompProperties_StackOnDamaged()
        {
            compClass = typeof(HediffComp_StackOnDamaged);
        }
    }

    public class HediffComp_StackOnDamaged : HediffComp, IArcaneSourced
    {
        public Pawn source;
        public AbilityDef spell;
        private int nextTick;

        public HediffCompProperties_StackOnDamaged Props => (HediffCompProperties_StackOnDamaged)props;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster == null) return;
            source = ctx.caster;
            if (ctx.def != null) spell = ctx.def;
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (Props.hediff == null || totalDamageDealt <= 0f) return;
            int now = Find.TickManager.TicksGame;
            if (now < nextTick) return;
            nextTick = now + Props.cooldownTicks;
            var p = Pawn;
            var src = source;
            var sp = spell;
            var pr = Props;
            var map = p.MapHeld;
            SourcedHediff.Later(map, () =>
            {
                if (p.Dead) return;
                var ctx = SourcedHediff.Context(src, sp, map, Color.white);
                ArcaneUtility.ApplyHediff(p, pr.hediff, pr.severity, -1, ctx, true, pr.maxSeverity);
            });
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "source");
            Scribe_Defs.Look(ref spell, "spell");
            Scribe_Values.Look(ref nextTick, "nextTick");
        }
    }

    // ================================================================================================
    // Contracts: a boon now, a price later unless it was paid in blood
    // ================================================================================================

    public class HediffCompProperties_Contract : HediffCompProperties
    {
        public int killsRequired = 1;
        public HediffDef rewardHediff;
        public float rewardSeverity = 1f;
        public HediffDef penaltyHediff;
        public float penaltySeverity = 1f;
        public Color color = new Color(1f, 0.55f, 0.2f);

        public HediffCompProperties_Contract()
        {
            compClass = typeof(HediffComp_Contract);
        }
    }

    public class HediffComp_Contract : HediffComp, IArcaneSourced
    {
        public int kills;
        public Pawn source;
        public AbilityDef spell;

        public HediffCompProperties_Contract Props => (HediffCompProperties_Contract)props;

        public override string CompLabelInBracketsExtra => Mathf.Min(kills, Props.killsRequired) + "/" + Props.killsRequired;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster == null) return;
            source = ctx.caster;
            if (ctx.def != null) spell = ctx.def;
        }

        public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
        {
            base.Notify_KilledPawn(victim, dinfo);
            if (victim != null && victim != Pawn) kills++;
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            var p = Pawn;
            if (p == null || p.Dead) return;
            bool paid = kills >= Props.killsRequired;
            var pr = Props;
            var src = source;
            var sp = spell;
            var map = p.MapHeld;
            SourcedHediff.Later(map, () =>
            {
                if (p.Dead) return;
                var ctx = SourcedHediff.Context(src, sp, map, pr.color);
                var h = paid ? pr.rewardHediff : pr.penaltyHediff;
                if (h != null) ArcaneUtility.ApplyHediff(p, h, paid ? pr.rewardSeverity : pr.penaltySeverity, -1, ctx);
                if (p.Spawned) ArcaneFx.Text(p, (paid ? "AA_ContractPaid" : "AA_ContractDue").Translate(), paid ? pr.color : new Color(1f, 0.3f, 0.25f));
            });
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref kills, "kills");
            Scribe_References.Look(ref source, "source");
            Scribe_Defs.Look(ref spell, "spell");
        }
    }

    // ================================================================================================
    // Glitching: random micro-stuns
    // ================================================================================================

    public class HediffCompProperties_Glitch : HediffCompProperties
    {
        public float chancePerSecond = 0.35f;
        public int stunTicks = 30;
        public Color color = new Color(1f, 0.95f, 0.55f);

        public HediffCompProperties_Glitch()
        {
            compClass = typeof(HediffComp_Glitch);
        }
    }

    public class HediffComp_Glitch : HediffComp_Throttled
    {
        private int acc;

        public HediffCompProperties_Glitch Props => (HediffCompProperties_Glitch)props;

        protected override void Process(int elapsed)
        {
            acc += elapsed;
            if (acc < 60) return;
            acc -= 60;
            var p = Pawn;
            if (p == null || !p.Spawned || p.Dead || !Rand.Chance(Props.chancePerSecond * Mathf.Max(0.5f, parent.Severity))) return;
            p.stances?.stunner?.StunFor(Props.stunTicks, null, false, false);
            if (p.Map == Find.CurrentMap) Fx.Burst(p.Map, p.DrawPos, Props.color, 5f, PMat.Glitch, 1.4f, 0.35f, 0.24f, PFlags.Flicker, 0f, 0f, 0.5f, 0.4f);
        }
    }

    // ================================================================================================
    // Deflection: incoming shots are turned aside, and maybe sent back
    // ================================================================================================

    public class HediffCompProperties_Deflect : HediffCompProperties
    {
        public float chance = 0.3f;
        public bool rangedOnly = true;
        public float reflectFraction;
        public DamageDef reflectDamageDef;
        public Color color = new Color(0.7f, 1f, 0.95f);

        public HediffCompProperties_Deflect()
        {
            compClass = typeof(HediffComp_Deflect);
        }
    }

    public class HediffComp_Deflect : HediffComp
    {
        public HediffCompProperties_Deflect Props => (HediffCompProperties_Deflect)props;

        private static bool reflecting;

        public static bool IsRangedHit(DamageInfo dinfo)
        {
            if (dinfo.Def == null || dinfo.Def.isExplosive) return false;
            if (dinfo.Def.isRanged) return true;
            return dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon;
        }

        /// <summary>Called before damage lands. Returns true if a deflection status turned the hit aside.</summary>
        public static bool Apply(Pawn p, ref DamageInfo dinfo)
        {
            if (reflecting || p?.health?.hediffSet == null) return false;
            float chance = 0f, reflect = 0f;
            HediffCompProperties_Deflect best = null;
            bool ranged = IsRangedHit(dinfo);
            foreach (var h in p.health.hediffSet.hediffs)
            {
                if (!(h is HediffWithComps hc)) continue;
                foreach (var c in hc.comps)
                {
                    if (!(c is HediffComp_Deflect d)) continue;
                    if (d.Props.rangedOnly && !ranged) continue;
                    float ch = d.Props.chance * Mathf.Max(0.25f, hc.Severity);
                    if (ch > chance)
                    {
                        chance = ch;
                        best = d.Props;
                    }
                    reflect = Mathf.Max(reflect, d.Props.reflectFraction);
                }
            }
            if (best == null || !Rand.Chance(Mathf.Min(0.9f, chance))) return false;
            var map = p.MapHeld;
            if (map != null && map == Find.CurrentMap && p.Spawned)
            {
                Fx.Burst(map, p.DrawPos, best.color, 8f, PMat.Gust, 3f, 0.4f, 0.5f, PFlags.Stretch | PFlags.AlignVel, 0f, 0f, 0.3f, 0.2f);
                ArcaneVfxManager.For(map).Add(new VfxSprite { mat = ArcaneMats.Ring, pos = p.DrawPos, scaleStart = 0.6f, scaleEnd = 2f, duration = 12, color = best.color });
                ArcaneFx.Text(p, "AA_Deflected".Translate(), best.color);
            }
            if (reflect > 0f && dinfo.Instigator is Pawn attacker && attacker != p && !attacker.Dead && attacker.Spawned && attacker.Map == map)
            {
                reflecting = true;
                bool prevSpellDamage2 = CompArcane.applyingSpellDamage;
                CompArcane.applyingSpellDamage = true;
                try
                {
                    attacker.TakeDamage(new DamageInfo(best.reflectDamageDef ?? ArcaneDefOf.AA_Force, dinfo.Amount * reflect, 0.5f, -1f, p));
                    if (map == Find.CurrentMap)
                        ArcaneVfxManager.For(map).Add(new VfxProjectile(p.DrawPos, attacker.DrawPos, 8, ArcaneMats.Get("ArcanumAscendant/Vfx/Orb"), best.color, Color.white, 0.5f, 0f, false, false)
                            { style = VfxStyle.Wind });
                }
                finally
                {
                    reflecting = false;
                    CompArcane.applyingSpellDamage = prevSpellDamage2;
                }
            }
            return true;
        }
    }

    // ================================================================================================
    // Something keeps happening to the bearer
    // ================================================================================================

    public class HediffCompProperties_PeriodicEffects : HediffCompProperties
    {
        public int interval = 120;
        /// <summary>Applied to the bearer each pulse.</summary>
        public List<SpellEffect> effects = new List<SpellEffect>();
        /// <summary>Applied around the bearer each pulse (never to the bearer itself).</summary>
        public List<SpellEffect> areaEffects = new List<SpellEffect>();
        public float areaRadius = 3f;
        public List<VfxSpec> vfx = new List<VfxSpec>();
        public Color color = Color.white;

        public HediffCompProperties_PeriodicEffects()
        {
            compClass = typeof(HediffComp_PeriodicEffects);
        }
    }

    /// <summary>
    /// Pulses spell effects on (and optionally around) the bearer on a timer, with the power of the mage who applied it:
    /// interest that accrues, a swarm that keeps biting, a curse that keeps adding omens.
    /// </summary>
    public class HediffComp_PeriodicEffects : HediffComp_Throttled, IArcaneSourced
    {
        public Pawn source;
        public AbilityDef spell;
        private int acc;

        public HediffCompProperties_PeriodicEffects Props => (HediffCompProperties_PeriodicEffects)props;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster == null) return;
            source = ctx.caster;
            if (ctx.def != null) spell = ctx.def;
        }

        protected override void Process(int elapsed)
        {
            acc += elapsed;
            if (acc < Props.interval) return;
            acc = 0;
            var p = Pawn;
            if (p == null || p.Dead || !p.Spawned) return;
            var map = p.Map;
            var src = source;
            var sp = spell;
            var pr = Props;
            SourcedHediff.Later(map, () => Pulse(p, map, src, sp, pr));
        }

        private static void Pulse(Pawn p, Map map, Pawn src, AbilityDef sp, HediffCompProperties_PeriodicEffects pr)
        {
            if (p.Dead || !p.Spawned || p.Map != map) return;
            var ctx = SourcedHediff.Context(src, sp, map, pr.color);
            ctx.forcedTarget = p;
            Vector3 pos = p.DrawPos;
            if (map == Find.CurrentMap && !pr.vfx.NullOrEmpty()) ArcaneFx.PlayAll(pr.vfx, map, pos, ctx, pr.areaRadius);
            if (!pr.effects.NullOrEmpty())
                SpellRunner.Impact(ctx, new ImpactInfo { cell = p.Position, pos = pos, primary = p, radius = 0f, sourcePos = pos }, pr.effects, playVfx: false);
            if (!pr.areaEffects.NullOrEmpty() && !p.Dead && p.Spawned)
                SpellRunner.Impact(ctx, new ImpactInfo { cell = p.Position, pos = pos, radius = pr.areaRadius * ctx.RadiusFactor, sourcePos = pos, exclude = p },
                    pr.areaEffects, playVfx: false);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "source");
            Scribe_Defs.Look(ref spell, "spell");
            Scribe_Values.Look(ref acc, "acc");
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ArcanumAscendant
{
    /// <summary>Helper base: runs Process() at most once per game tick regardless of 1.6 tick throttling.</summary>
    public abstract class HediffComp_Throttled : HediffComp
    {
        private int lastTick = -1;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Run();
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Run();
        }

        private void Run()
        {
            int now = Find.TickManager.TicksGame;
            if (lastTick < 0) lastTick = now;
            int elapsed = now - lastTick;
            if (elapsed <= 0) return;
            lastTick = now;
            Process(Mathf.Min(elapsed, 250));
        }

        protected abstract void Process(int elapsed);
    }

    public class HediffCompProperties_ArcaneDot : HediffCompProperties
    {
        public DamageDef damageDef;
        public float damagePerSecond = 2f;
        public int interval = 60;
        public bool scaleWithSeverity = true;
        public FleckDef fleck;
        public Color color = Color.white;
        public float healPerSecond;

        public HediffCompProperties_ArcaneDot()
        {
            compClass = typeof(HediffComp_ArcaneDot);
        }
    }

    /// <summary>Damage or healing over time that remembers who applied it.</summary>
    public class HediffComp_ArcaneDot : HediffComp_Throttled
    {
        public Pawn instigator;
        public float power = 1f;
        private int acc;

        public HediffCompProperties_ArcaneDot Props => (HediffCompProperties_ArcaneDot)props;

        protected override void Process(int elapsed)
        {
            acc += elapsed;
            if (acc < Props.interval) return;
            int ticks = acc;
            acc = 0;
            var p = Pawn;
            if (p.Dead) return;
            float sev = Props.scaleWithSeverity ? parent.Severity : 1f;
            float seconds = ticks / 60f;
            if (Props.damagePerSecond > 0f && Props.damageDef != null)
            {
                bool wasDead = p.Dead;
                CompArcane.applyingSpellDamage = true;
                try
                {
                    p.TakeDamage(new DamageInfo(Props.damageDef, Props.damagePerSecond * seconds * sev * power, 0f, -1f, instigator));
                }
                finally
                {
                    CompArcane.applyingSpellDamage = false;
                }
                if (!wasDead && p.Dead) instigator?.TryGetComp<CompArcane>()?.Notify_Kill(p, null);
            }
            if (Props.healPerSecond > 0f) ArcaneUtility.HealPawn(p, Props.healPerSecond * seconds * sev * power);
            if (Props.fleck != null && p.Spawned) ArcaneFx.Particles(p.Map, p.DrawPos, Props.color, Props.fleck, 2, 0.7f, 0.5f);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref instigator, "instigator");
            Scribe_Values.Look(ref power, "power", 1f);
        }
    }

    public class HediffCompProperties_Summoned : HediffCompProperties
    {
        public bool vanishOnDeath = true;
        public bool killOnExpire;
        public bool controlAI = true;
        public float leashRange = 10f;

        public HediffCompProperties_Summoned()
        {
            compClass = typeof(HediffComp_Summoned);
        }
    }

    /// <summary>Lifetime and simple combat AI for summoned or raised creatures.</summary>
    public class HediffComp_Summoned : HediffComp_Throttled
    {
        public Pawn master;
        public int ticksLeft = -1;
        private int aiAcc;

        public HediffCompProperties_Summoned Props => (HediffCompProperties_Summoned)props;

        public override string CompLabelInBracketsExtra => ticksLeft > 0 ? ticksLeft.ToStringTicksToPeriod(true, true) : null;

        protected override void Process(int elapsed)
        {
            var p = Pawn;
            if (p.Dead) return;
            if (ticksLeft > 0)
            {
                ticksLeft -= elapsed;
                if (ticksLeft <= 0)
                {
                    Expire();
                    return;
                }
            }
            if (!Props.controlAI || !p.Spawned || p.Downed) return;
            aiAcc += elapsed;
            if (aiAcc < 45) return;
            aiAcc = 0;
            ControlTick(p);
        }

        private void ControlTick(Pawn p)
        {
            if (p.stances?.stunner?.Stunned ?? false) return;
            var map = p.Map;
            var curJob = p.CurJobDef;
            bool attacking = curJob == JobDefOf.AttackMelee || curJob == JobDefOf.AttackStatic;
            if (attacking && p.CurJob.targetA.Thing is Pawn tgt && !tgt.Dead && !tgt.Downed) return;
            IntVec3 anchor = master != null && master.Spawned && master.Map == map ? master.Position : p.Position;
            Pawn enemy = null;
            float best = 999f;
            foreach (var other in map.mapPawns.AllPawnsSpawned)
            {
                if (other.Dead || other.Downed || !other.HostileTo(p)) continue;
                float d = other.Position.DistanceTo(anchor);
                if (d > 22f) continue;
                float dp = other.Position.DistanceTo(p.Position);
                if (dp < best)
                {
                    best = dp;
                    enemy = other;
                }
            }
            if (enemy != null)
            {
                bool ranged = p.equipment?.Primary != null && p.equipment.Primary.def.IsRangedWeapon;
                if (ranged && p.RaceProps.Humanlike) return; // lord handles ranged humanlikes
                var job = JobMaker.MakeJob(JobDefOf.AttackMelee, enemy);
                job.expiryInterval = 600;
                job.checkOverrideOnExpire = true;
                p.jobs.StartJob(job, JobCondition.InterruptForced);
                return;
            }
            if (master != null && master.Spawned && master.Map == map && p.Position.DistanceTo(master.Position) > Props.leashRange
                && curJob != JobDefOf.Goto)
            {
                var c = CellFinder.RandomClosewalkCellNear(master.Position, map, 4);
                var job = JobMaker.MakeJob(JobDefOf.Goto, c);
                job.locomotionUrgency = LocomotionUrgency.Jog;
                p.jobs.StartJob(job, JobCondition.InterruptForced);
            }
        }

        public void Expire()
        {
            var p = Pawn;
            if (p.Spawned)
            {
                ArcaneFx.Particles(p.Map, p.DrawPos, new Color(0.7f, 0.6f, 1f), ArcaneDefOf.AA_Fleck_Smoke, 6, 0.8f, 1f);
                ArcaneFx.Particles(p.Map, p.DrawPos, new Color(0.7f, 0.6f, 1f), ArcaneDefOf.AA_Fleck_Spark, 10, 2f, 0.5f);
            }
            if (Props.killOnExpire)
            {
                p.Kill(null, parent);
                if (Props.vanishOnDeath && p.Corpse != null && !p.Corpse.Destroyed) p.Corpse.Destroy();
            }
            else if (!p.Destroyed)
            {
                p.Destroy(DestroyMode.Vanish);
            }
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (!Props.vanishOnDeath) return;
            var p = Pawn;
            var map = p.MapHeld;
            if (map == null) return;
            ArcaneVfxManager.For(map).Schedule(1, () =>
            {
                var corpse = p.Corpse;
                if (corpse != null && !corpse.Destroyed)
                {
                    if (corpse.Spawned) ArcaneFx.Particles(corpse.Map, corpse.DrawPos, new Color(0.7f, 0.6f, 1f), ArcaneDefOf.AA_Fleck_Smoke, 5, 0.6f, 1f);
                    corpse.Destroy();
                }
            });
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref master, "master");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", -1);
        }
    }

    public class HediffCompProperties_Dominated : HediffCompProperties
    {
        public HediffCompProperties_Dominated()
        {
            compClass = typeof(HediffComp_Dominated);
        }
    }

    /// <summary>Returns a mind-controlled pawn to its faction when the effect ends.</summary>
    public class HediffComp_Dominated : HediffComp_Throttled
    {
        public Faction originalFaction;
        public int ticksLeft = 2500;
        private bool restored;

        public override string CompLabelInBracketsExtra => ticksLeft.ToStringTicksToPeriod(true, true);
        public override bool CompShouldRemove => ticksLeft <= 0;

        protected override void Process(int elapsed)
        {
            ticksLeft -= elapsed;
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Restore();
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Restore();
        }

        private void Restore()
        {
            if (restored) return;
            restored = true;
            var p = Pawn;
            if (p == null || p.Destroyed) return;
            if (originalFaction != null && p.Faction != originalFaction && !originalFaction.defeated)
            {
                p.SetFaction(originalFaction);
                if (!p.Dead && p.Spawned && originalFaction.HostileTo(Faction.OfPlayer))
                    LordMaker.MakeNewLord(originalFaction, new LordJob_AssaultColony(originalFaction, false, false, false, false, false, false, false), p.Map, new[] { p });
                if (!p.Dead && p.Spawned) ArcaneFx.Text(p, "AA_DominationEnds".Translate(), Color.white);
            }
            else if (originalFaction == null && p.Faction != null && !p.Dead && p.RaceProps.Animal)
            {
                p.SetFaction(null);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref originalFaction, "originalFaction");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
            Scribe_Values.Look(ref restored, "restored");
        }
    }

    public class HediffCompProperties_DeathBurst : HediffCompProperties
    {
        public DamageDef damageDef;
        public float damage = 15f;
        public float radius = 2.5f;
        public HediffDef spreadHediff;
        public float spreadSeverity = 0.5f;
        public Color color = Color.green;

        public HediffCompProperties_DeathBurst()
        {
            compClass = typeof(HediffComp_DeathBurst);
        }
    }

    /// <summary>When the carrier dies it bursts, damaging and/or infecting enemies around it.</summary>
    public class HediffComp_DeathBurst : HediffComp
    {
        public HediffCompProperties_DeathBurst Props => (HediffCompProperties_DeathBurst)props;

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            var p = Pawn;
            var map = p.MapHeld;
            if (map == null) return;
            IntVec3 cell = p.PositionHeld;
            Pawn instigator = parent.TryGetComp<HediffComp_ArcaneDot>()?.instigator;
            var props = Props;
            ArcaneVfxManager.For(map).Schedule(1, () =>
            {
                ArcaneFx.Eruption(map, cell.ToVector3Shifted(), props.color, props.color, props.radius);
                foreach (var t in GenRadial.RadialDistinctThingsAround(cell, map, props.radius, true).OfType<Pawn>().ToList())
                {
                    if (t.Dead || (instigator != null && !t.HostileTo(instigator))) continue;
                    if (props.damageDef != null)
                    {
                        CompArcane.applyingSpellDamage = true;
                        try { t.TakeDamage(new DamageInfo(props.damageDef, props.damage, 0.3f, -1f, instigator)); }
                        finally { CompArcane.applyingSpellDamage = false; }
                    }
                    if (props.spreadHediff != null && !t.Dead)
                    {
                        var h = ArcaneUtility.ApplyHediff(t, props.spreadHediff, props.spreadSeverity, -1, null);
                        var dot = (h as HediffWithComps)?.TryGetComp<HediffComp_ArcaneDot>();
                        if (dot != null) dot.instigator = instigator;
                    }
                }
            });
        }
    }

    /// <summary>Hediff that simply shows a looping aura around the pawn while active.</summary>
    public class HediffCompProperties_AuraVisual : HediffCompProperties
    {
        public Color color = Color.white;
        public string texPath = "ArcanumAscendant/Vfx/RuneCircle";
        public float size = 1.6f;
        public FleckDef fleck;
        public int fleckInterval = 20;
        /// <summary>Particle emitter preset from <see cref="StatusFx"/> (Burning, Frozen, Shocked, Doom...).</summary>
        public string emitter;
        public int emitInterval = 12;
        /// <summary>Texture that clings to the pawn while the hediff lasts (ice crust, sigil, bubble).</summary>
        public string overlayTex;
        public bool overlayGlow = true;
        public float overlaySize = 1.6f;
        public float overlaySpin;
        public float overlayZ;
        public bool overlayPulse;
        public Color overlayColor = new Color(0f, 0f, 0f, 0f);

        public HediffCompProperties_AuraVisual()
        {
            compClass = typeof(HediffComp_AuraVisual);
        }
    }

    public class HediffComp_AuraVisual : HediffComp_Throttled
    {
        private int acc = 999;
        private int fleckAcc;
        private int emitAcc;
        [Unsaved] private VfxStatusOverlay overlay;

        public HediffCompProperties_AuraVisual Props => (HediffCompProperties_AuraVisual)props;

        protected override void Process(int elapsed)
        {
            var p = Pawn;
            if (!p.Spawned || p.Dead) return;
            if (Props.overlayTex != null && (overlay == null || overlay.finished) && p.Map == Find.CurrentMap)
            {
                overlay = new VfxStatusOverlay
                {
                    pawn = p, hediff = parent.def, mat = ArcaneMats.Get(Props.overlayTex, Props.overlayGlow),
                    color = Props.overlayColor.a > 0f ? Props.overlayColor : Props.color, size = Props.overlaySize, glow = Props.overlayGlow,
                    spin = Props.overlaySpin, zOffset = Props.overlayZ, pulse = Props.overlayPulse, bob = Props.overlayZ > 0.3f ? 0.06f : 0f,
                };
                ArcaneVfxManager.For(p.Map)?.Add(overlay);
            }
            if (Props.emitter != null)
            {
                emitAcc += elapsed;
                if (emitAcc >= Props.emitInterval)
                {
                    emitAcc = 0;
                    StatusFx.Emit(Props.emitter, p, Props.color, parent.Severity);
                }
            }
            acc += elapsed;
            if (acc >= 58)
            {
                acc = 0;
                if (!Props.texPath.NullOrEmpty()) ArcaneFx.Aura(p, Props.color, 62, Props.size, Props.texPath);
            }
            if (Props.fleck != null)
            {
                fleckAcc += elapsed;
                if (fleckAcc >= Props.fleckInterval)
                {
                    fleckAcc = 0;
                    ArcaneFx.Particles(p.Map, p.DrawPos, Props.color, Props.fleck, 1, 0.5f, 0.5f, 0f, 0.4f);
                }
            }
        }
    }
}

namespace ArcanumAscendant
{
    public class HediffCompProperties_ExpireBurst : HediffCompProperties
    {
        public DamageDef damageDef;
        public float damage = 30f;
        public float radius = 2.5f;
        public UnityEngine.Color color = UnityEngine.Color.white;
        public bool damageCarrier = true;
        public float shake = 1f;

        public HediffCompProperties_ExpireBurst()
        {
            compClass = typeof(HediffComp_ExpireBurst);
        }
    }

    /// <summary>When the hediff runs out (e.g. Living Bomb) the carrier detonates, hurting the carrier's allies.</summary>
    public class HediffComp_ExpireBurst : HediffComp
    {
        public HediffCompProperties_ExpireBurst Props => (HediffCompProperties_ExpireBurst)props;

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            var p = Pawn;
            if (p == null || p.Dead || !p.Spawned) return;
            var map = p.Map;
            var cell = p.Position;
            var dot = parent.TryGetComp<HediffComp_ArcaneDot>();
            Verse.Pawn instigator = dot?.instigator;
            float power = dot?.power ?? 1f;
            var props = Props;
            ArcaneFx.Eruption(map, p.DrawPos, props.color, UnityEngine.Color.white, props.radius);
            ArcaneVfxManager.For(map).Add(new VfxSprite { mat = ArcaneMats.Shock, pos = p.DrawPos, scaleStart = 0.5f, scaleEnd = props.radius * 2.6f, duration = 26, color = props.color, fadeIn = 0.02f, fadeOut = 0.8f });
            ArcaneFx.Shake(map, props.shake);
            CompArcane.applyingSpellDamage = true;
            try
            {
                foreach (var t in Verse.GenRadial.RadialDistinctThingsAround(cell, map, props.radius, true))
                {
                    if (!(t is Verse.Pawn other) || other.Dead) continue;
                    if (other == p && !props.damageCarrier) continue;
                    if (instigator != null && other != p && !other.HostileTo(instigator)) continue;
                    bool wasDead = other.Dead;
                    other.TakeDamage(new Verse.DamageInfo(props.damageDef ?? RimWorld.DamageDefOf.Bomb, props.damage * power, 0.4f, -1f, instigator));
                    if (!wasDead && other.Dead) instigator?.TryGetComp<CompArcane>()?.Notify_Kill(other, null);
                }
            }
            finally
            {
                CompArcane.applyingSpellDamage = false;
            }
        }
    }
}

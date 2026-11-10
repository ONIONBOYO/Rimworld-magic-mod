using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace ArcanumAscendant
{
    // ================================================================================================
    // The Wyrmcaller: a mage bonded to one dragon. The wyrm is a real colony animal (it can be zoned, trained to haul
    // and rescue, taken on caravans) that grows as its mage climbs the circles and carries out most of her spells itself.
    // ================================================================================================

    public class WyrmBreed
    {
        public ArcaneClassDef subclass;
        public PawnKindDef kind;
        public HediffDef hediff;
    }

    public class HediffCompProperties_WyrmBond : HediffCompProperties
    {
        public PawnKindDef defaultKind;
        public List<WyrmBreed> breeds = new List<WyrmBreed>();
        /// <summary>The mage's circle at which the wyrm reaches each life stage after the first.</summary>
        public List<int> stageCircles = new List<int> { 2, 4 };
        /// <summary>Biological age (years) that puts the wyrm in each life stage.</summary>
        public List<float> stageAges = new List<float> { 1f, 100f, 200f };
        public ThoughtDef griefThought;
        public Color color = new Color(0.25f, 0.84f, 0.63f);

        public HediffCompProperties_WyrmBond()
        {
            compClass = typeof(HediffComp_WyrmBond);
        }
    }

    /// <summary>
    /// Ties a wyrm to its mage. The wyrm's life stage follows the mage's circle, its strength follows her level (the
    /// hediff's severity), and its breed follows her subclass.
    /// </summary>
    public class HediffComp_WyrmBond : HediffComp_Throttled
    {
        public Pawn master;
        private int acc = 9999;

        public HediffCompProperties_WyrmBond Props => (HediffCompProperties_WyrmBond)props;

        public override string CompLabelInBracketsExtra => master != null ? master.LabelShort : null;

        protected override void Process(int elapsed)
        {
            acc += elapsed;
            if (acc < 250) return;
            acc = 0;
            Sync(false);
        }

        public int WantedStage(CompArcane c)
        {
            int stage = 0;
            for (int i = 0; i < Props.stageCircles.Count; i++)
                if (c.circle >= Props.stageCircles[i]) stage = i + 1;
            return Mathf.Min(stage, Props.stageAges.Count - 1);
        }

        public void Sync(bool silent)
        {
            var p = Pawn;
            if (p == null || p.Dead || master == null) return;
            var c = master.TryGetComp<CompArcane>();
            if (c == null || !c.IsMage) return;
            parent.Severity = Mathf.Clamp(c.level, 1, CompArcane.MaxLevel);

            int stage = WantedStage(c);
            if (p.ageTracker != null && p.ageTracker.CurLifeStageIndex < stage)
            {
                p.ageTracker.AgeBiologicalTicks = (long)(Props.stageAges[stage] * GenDate.TicksPerYear) + 1000;
                p.Drawer?.renderer?.SetAllGraphicsDirty();
                if (!silent && p.Spawned)
                {
                    ArcaneFx.LevelUpBurst(p, Props.color, true);
                    if (p.Map == Find.CurrentMap) Fx.Burst(p.Map, p.DrawPos, Props.color, 20f, PMat.Scale, 3f, 1.4f, 0.3f, PFlags.Lit | PFlags.Wobble, 300f, 1.2f, 0.6f);
                    if (master.Faction == Faction.OfPlayer)
                        Find.LetterStack.ReceiveLetter("AA_WyrmGrewLabel".Translate(p.LabelShort), "AA_WyrmGrew".Translate(p.LabelShort, master.LabelShort,
                            p.ageTracker.CurLifeStage?.label ?? "?", p.Named("WYRM"), master.Named("MAGE")), LetterDefOf.PositiveEvent, p);
                }
            }

            var breed = Props.breeds.FirstOrDefault(b => b.subclass != null && b.subclass == c.subclassDef);
            var kind = breed?.kind ?? Props.defaultKind;
            if (kind != null && p.kindDef != kind && kind.race == p.def)
            {
                p.kindDef = kind;
                p.Drawer?.renderer?.SetAllGraphicsDirty();
                if (!silent && p.Spawned)
                {
                    ArcaneFx.LevelUpBurst(p, Props.color, true);
                    if (master.Faction == Faction.OfPlayer)
                        Messages.Message("AA_WyrmBreed".Translate(p.LabelShort, kind.label, p.Named("WYRM")), p, MessageTypeDefOf.PositiveEvent);
                }
            }
            foreach (var b in Props.breeds)
            {
                if (b.hediff == null) continue;
                var h = p.health.hediffSet.GetFirstHediffOfDef(b.hediff);
                if (b == breed && h == null) p.health.AddHediff(b.hediff);
                else if (b != breed && h != null) p.health.RemoveHediff(h);
            }
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Wyrm.Forget(master);
            if (master == null || master.Dead) return;
            if (Props.griefThought != null) master.needs?.mood?.thoughts?.memories?.TryGainMemory(Props.griefThought);
            if (master.Faction == Faction.OfPlayer)
                Find.LetterStack.ReceiveLetter("AA_WyrmDiedLabel".Translate(Pawn.LabelShort), "AA_WyrmDied".Translate(Pawn.LabelShort, master.LabelShort, master.Named("MAGE")),
                    LetterDefOf.NegativeEvent, master);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref master, "master");
        }
    }

    public static class Wyrm
    {
        private struct Entry
        {
            public int tick;
            public Pawn wyrm;
        }

        private static readonly Dictionary<int, Entry> cache = new Dictionary<int, Entry>();
        private static HediffDef aspectDef;
        private static bool aspectLooked;

        public static HediffComp_WyrmBond Bond(Pawn p)
        {
            var hs = p?.health?.hediffSet?.hediffs;
            if (hs == null) return null;
            for (int i = 0; i < hs.Count; i++)
                if (hs[i] is HediffWithComps hc)
                {
                    var b = hc.TryGetComp<HediffComp_WyrmBond>();
                    if (b != null) return b;
                }
            return null;
        }

        public static bool IsWyrm(Pawn p) => Bond(p) != null;

        public static void Forget(Pawn master)
        {
            if (master != null) cache.Remove(master.thingIDNumber);
        }

        /// <summary>The caster's living wyrm on the caster's own map, if any.</summary>
        public static Pawn Of(Pawn master)
        {
            if (master == null) return null;
            int now = Find.TickManager.TicksGame;
            if (cache.TryGetValue(master.thingIDNumber, out var e) && now - e.tick < 90 && (e.wyrm == null || !e.wyrm.Dead && e.wyrm.Spawned && e.wyrm.Map == master.MapHeld))
                return e.wyrm;
            Pawn found = null;
            var map = master.MapHeld;
            if (map != null)
                foreach (var p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p.RaceProps.Humanlike || p.Dead) continue;
                    if (Bond(p)?.master == master)
                    {
                        found = p;
                        break;
                    }
                }
            cache[master.thingIDNumber] = new Entry { tick = now, wyrm = found };
            return found;
        }

        /// <summary>The caster's living wyrm wherever it is: any map, or travelling in a caravan.</summary>
        public static Pawn Anywhere(Pawn master)
        {
            var here = Of(master);
            if (here != null || master == null) return here;
            foreach (var map in Find.Maps)
                foreach (var p in map.mapPawns.AllPawns)
                    if (!p.RaceProps.Humanlike && !p.Dead && Bond(p)?.master == master) return p;
            foreach (var p in Find.WorldPawns.AllPawnsAlive)
                if (!p.RaceProps.Humanlike && Bond(p)?.master == master) return p;
            return null;
        }

        /// <summary>A mage in draconic aspect breathes fire alongside her wyrm.</summary>
        public static bool TwinBreath(Pawn caster)
        {
            if (!aspectLooked)
            {
                aspectLooked = true;
                aspectDef = DefDatabase<HediffDef>.GetNamedSilentFail("AA_DraconicAspect");
            }
            return aspectDef != null && caster?.health?.hediffSet != null && caster.health.hediffSet.HasHediff(aspectDef);
        }

        public static IEnumerable<Pawn> Brood(Pawn master, ThingDef race)
        {
            var map = master?.MapHeld;
            if (map == null) yield break;
            foreach (var p in map.mapPawns.AllPawnsSpawned.ToList())
            {
                if (p.Dead || p.Downed || race != null && p.def != race) continue;
                var hs = p.health?.hediffSet?.hediffs;
                if (hs == null) continue;
                for (int i = 0; i < hs.Count; i++)
                    if (hs[i] is HediffWithComps hc && hc.TryGetComp<HediffComp_Summoned>()?.master == master)
                    {
                        yield return p;
                        break;
                    }
            }
        }

        public static void ScaleBurst(Map map, Vector3 pos, Color c, float n)
        {
            if (map != Find.CurrentMap) return;
            Fx.Burst(map, pos, c, n, PMat.Scale, 2.6f, 1.2f, 0.26f, PFlags.Lit | PFlags.Wobble, 300f, 1.2f, 0.5f);
            Fx.Embers(map, pos, Color.Lerp(c, Color.white, 0.3f), n * 0.6f, 0.6f, 1.2f, 1.2f);
        }
    }

    /// <summary>Hatches the caster's wyrm: a bonded, fully trained colony animal that grows with its mage.</summary>
    public class Effect_WyrmHatch : SpellEffect
    {
        public PawnKindDef kind;
        public HediffDef bond;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var caster = ctx.caster;
            var map = ctx.map;
            if (kind == null || bond == null || caster == null || map == null || !ctx.Once("hatch")) return;
            if (Wyrm.Anywhere(caster) != null)
            {
                ArcaneFx.Text(caster, "AA_WyrmAlreadyBonded".Translate(), ctx.Color);
                return;
            }
            IntVec3 cell = CellFinder.StandableCellNear(info.cell, map, 3f);
            if (!cell.IsValid) cell = caster.Position;
            var p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, caster.Faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true,
                canGeneratePawnRelations: false, allowFood: false, fixedBiologicalAge: 0.5f, fixedChronologicalAge: 0.5f));
            GenSpawn.Spawn(p, cell, map);
            var h = (HediffWithComps)HediffMaker.MakeHediff(bond, p);
            p.health.AddHediff(h);
            var comp = h.TryGetComp<HediffComp_WyrmBond>();
            if (comp != null)
            {
                comp.master = caster;
                comp.Sync(true);
            }
            if (p.Faction == Faction.OfPlayer)
            {
                if (p.training != null)
                    foreach (var td in DefDatabase<TrainableDef>.AllDefsListForReading)
                    {
                        try
                        {
                            if (!p.training.CanAssignToTrain(td).Accepted) continue;
                            p.training.SetWantedRecursive(td, true);
                            p.training.Train(td, caster, true);
                        }
                        catch (Exception e)
                        {
                            Log.Warning("[Arcanum Ascendant] Could not train the wyrm in " + td + ": " + e.Message);
                        }
                    }
                if (p.playerSettings != null)
                {
                    p.playerSettings.Master = caster;
                    p.playerSettings.followDrafted = true;
                }
                if (caster.relations != null && !caster.relations.DirectRelationExists(PawnRelationDefOf.Bond, p)) caster.relations.AddDirectRelation(PawnRelationDefOf.Bond, p);
                if (p.Name == null || p.Name.Numerical) p.Name = PawnBioAndNameGenerator.GeneratePawnName(p);
                Find.LetterStack.ReceiveLetter("AA_WyrmHatchedLabel".Translate(p.LabelShort), "AA_WyrmHatched".Translate(p.LabelShort, caster.LabelShort, caster.Named("MAGE")),
                    LetterDefOf.PositiveEvent, p);
            }
            else ArcaneUtility.JoinCasterSide(caster, map, p);
            Wyrm.Forget(caster);
            if (map == Find.CurrentMap)
            {
                Vector3 pos = p.DrawPos;
                ArcaneFx.Flash(map, pos, ctx.Color, 4f);
                Fx.Burst(map, pos, new Color(0.95f, 0.92f, 0.82f, 1f), 14f, PMat.Shell, 3.2f, 1.4f, 0.24f, PFlags.Lit | PFlags.Bounce | PFlags.Shadow, 500f, 6f, 0.3f);
                Fx.Embers(map, pos, ctx.Color2, 16f, 0.8f, 1.6f, 1.8f);
                ArcaneVfxManager.For(map).Add(new VfxPillar(pos, 40, ctx.Color, ctx.Color2, 1.2f));
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_WyrmHatch".Translate();
        }
    }

    public enum WyrmCommandMode
    {
        /// <summary>The wyrm attacks the target in melee.</summary>
        Attack,
        /// <summary>The wyrm flies back to its mage's side.</summary>
        Recall,
        /// <summary>The wyrm devours a corpse and heals.</summary>
        Devour,
        /// <summary>Mage and wyrm trade places.</summary>
        Swap,
        /// <summary>Mage and wyrm fly to the target together.</summary>
        Ride,
        /// <summary>Mage and wyrm fly to the target, setting everything along the way alight.</summary>
        Strafe,
        /// <summary>Only applies wyrmHediff to the wyrm.</summary>
        Empower,
    }

    public class Effect_WyrmCommand : SpellEffect
    {
        public WyrmCommandMode mode = WyrmCommandMode.Attack;
        /// <summary>Applied to the wyrm whatever the mode.</summary>
        public HediffDef wyrmHediff;
        public float wyrmHediffSeverity = 1f;
        public int wyrmHediffTicks = -1;
        public bool stackHediff;
        public float maxSeverity = -1f;
        public float heal = 60f;
        /// <summary>Strafe: effects laid along the flight path, and how wide.</summary>
        public List<SpellEffect> pathEffects;
        public float pathRadius = 1.8f;
        /// <summary>Ride and Strafe: effects where the wyrm lands.</summary>
        public List<SpellEffect> landingEffects;
        public float landingRadius = 2.5f;

        [Unsaved] private Effect_Launch launcher;

        public override bool AppliesToPawns => mode == WyrmCommandMode.Attack || mode == WyrmCommandMode.Devour;

        public Effect_WyrmCommand()
        {
            affects = TargetFilter.All;
        }

        private Effect_Launch Launcher => launcher ??= new Effect_Launch { damage = 0f, splashRadius = 0f, landingEffects = landingEffects, landingRadius = landingRadius };

        private static readonly Effect_Launch PlainFlight = new Effect_Launch { damage = 0f, splashRadius = 0f };

        private void Buff(SpellContext ctx, Pawn w)
        {
            if (wyrmHediff == null || w == null || w.Dead) return;
            ArcaneUtility.ApplyHediff(w, wyrmHediff, wyrmHediffSeverity, wyrmHediffTicks > 0 ? Mathf.RoundToInt(wyrmHediffTicks * ctx.DurationFactor) : -1, ctx, stackHediff, maxSeverity);
        }

        private static bool Ready(SpellContext ctx, out Pawn w)
        {
            w = Wyrm.Of(ctx.caster);
            if (w != null && w.Spawned && !w.Downed && w.Map == ctx.map) return true;
            if (ctx.caster != null && ctx.Once("nowyrm")) ArcaneFx.Text(ctx.caster, "AA_NoWyrm".Translate(), ctx.Color);
            return false;
        }

        private static IntVec3 Beside(Map map, IntVec3 c, Pawn except)
        {
            var near = CellFinder.StandableCellNear(c, map, 2.5f, x => x != c && (except == null || x != except.Position));
            return near.IsValid ? near : c;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (mode != WyrmCommandMode.Attack || p != info.primary || p == ctx.caster) return;
            if (!Ready(ctx, out var w) || p == w) return;
            Buff(ctx, w);
            var job = JobMaker.MakeJob(JobDefOf.AttackMelee, p);
            job.killIncappedTarget = false;
            job.expiryInterval = 1800;
            job.checkOverrideOnExpire = true;
            w.jobs?.StartJob(job, JobCondition.InterruptForced);
            if (w.Map == Find.CurrentMap)
                ArcaneVfxManager.For(w.Map).Add(new VfxSprite { mat = ArcaneMats.Ring, follow = p, pos = p.DrawPos, scaleStart = 2.4f, scaleEnd = 0.6f, duration = 24, color = ctx.Color });
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse c, ImpactInfo info)
        {
            if (mode != WyrmCommandMode.Devour || c == null || c.Destroyed || !ctx.Once("devour")) return;
            if (!Ready(ctx, out var w)) return;
            var inner = c.InnerPawn;
            if (inner != null && inner.RaceProps.Humanlike && inner.Faction == ctx.caster?.Faction) return;
            var map = c.Map;
            IntVec3 cell = c.Position;
            Vector3 pos = c.DrawPos;
            float size = Mathf.Clamp(inner?.BodySize ?? 1f, 0.3f, 4f);
            try
            {
                c.Strip();
            }
            catch
            {
                // nothing worth stripping
            }
            c.Destroy();
            ArcaneUtility.HealPawn(w, heal * size * ctx.HealFactor);
            if (w.needs?.food != null) w.needs.food.CurLevel = w.needs.food.MaxLevel;
            Buff(ctx, w);
            IntVec3 dest = Beside(map, cell, null);
            if (dest != w.Position && w.Position.DistanceTo(cell) > 2.5f)
                ArcaneVfxManager.For(map).Schedule(1, () =>
                {
                    if (w.Spawned && !w.Downed && w.Map == map) PlainFlight.Launch(ctx, w, dest, false, true);
                });
            if (map == Find.CurrentMap)
            {
                Fx.Burst(map, pos, new Color(0.6f, 0.05f, 0.05f, 1f), 10f, PMat.BloodAlpha, 2.5f, 0.9f, 0.3f, PFlags.Lit, 200f, 3f, 0.3f);
                Fx.Burst(map, pos, new Color(0.9f, 0.88f, 0.8f, 1f), 5f, PMat.Bone, 2.2f, 1.2f, 0.2f, PFlags.Lit | PFlags.Bounce, 400f, 5f, 0.3f);
            }
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (mode == WyrmCommandMode.Attack || mode == WyrmCommandMode.Devour) return;
            if (!ctx.Once("wyrmcmd" + (int)mode) || !Ready(ctx, out var w)) return;
            var caster = ctx.caster;
            var map = ctx.map;
            Buff(ctx, w);
            switch (mode)
            {
                case WyrmCommandMode.Recall:
                {
                    IntVec3 dest = Beside(map, caster.Position, w);
                    if (dest == w.Position) return;
                    ArcaneVfxManager.For(map).Schedule(1, () =>
                    {
                        if (w.Spawned && !w.Downed && w.Map == map) Launcher.Launch(ctx, w, dest, false, true);
                    });
                    break;
                }
                case WyrmCommandMode.Swap:
                {
                    IntVec3 a = caster.Position, b = w.Position;
                    if (a == b) return;
                    Effect_Teleport.Blink(caster, b, ctx);
                    Effect_Teleport.Blink(w, a, ctx);
                    caster.jobs?.EndCurrentJob(JobCondition.InterruptForced);
                    break;
                }
                case WyrmCommandMode.Ride:
                case WyrmCommandMode.Strafe:
                {
                    IntVec3 dest = info.cell;
                    if (!dest.InBounds(map)) return;
                    if (!dest.Standable(map)) dest = CellFinder.StandableCellNear(dest, map, 3f);
                    if (!dest.IsValid) return;
                    IntVec3 riderDest = Beside(map, dest, null);
                    Vector3 start = w.DrawPos;
                    Vector3 end = dest.ToVector3Shifted();
                    float dist = (end - start).MagnitudeHorizontal();
                    var mgr = ArcaneVfxManager.For(map);
                    mgr.Schedule(1, () =>
                    {
                        if (w.Spawned && !w.Downed && w.Map == map) Launcher.Launch(ctx, w, dest, false, true);
                        if (caster.Spawned && !caster.Downed && caster.Map == map && riderDest != caster.Position) PlainFlight.Launch(ctx, caster, riderDest, false, true);
                    });
                    if (mode == WyrmCommandMode.Strafe && !pathEffects.NullOrEmpty() && dist > 1.5f)
                    {
                        // the flyer covers the distance in roughly dist / 14 seconds, never less than about half a second
                        int flight = Mathf.Max(27, Mathf.RoundToInt(dist / 14f * 60f));
                        int steps = Mathf.Max(1, Mathf.RoundToInt(dist / 2f));
                        for (int i = 1; i <= steps; i++)
                        {
                            float f = i / (float)(steps + 1);
                            Vector3 pt = Vector3.Lerp(start, end, f);
                            mgr.Schedule(2 + Mathf.RoundToInt(flight * f), () =>
                            {
                                var c = pt.ToIntVec3();
                                if (!c.InBounds(map)) return;
                                SpellRunner.Impact(ctx, new ImpactInfo { cell = c, pos = pt, radius = pathRadius * ctx.RadiusFactor, sourcePos = start }, pathEffects, true);
                            });
                        }
                    }
                    break;
                }
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return ("AA_Desc_Wyrm_" + mode).Translate((heal * (ctx?.HealFactor ?? 1f)).ToString("F0"));
            if (wyrmHediff != null) yield return "AA_Desc_WyrmHediff".Translate(wyrmHediff.LabelCap);
            if (!pathEffects.NullOrEmpty())
                foreach (var e in pathEffects)
                    foreach (var s in e.Describe(ctx)) yield return "  • " + s;
            if (!landingEffects.NullOrEmpty())
            {
                yield return "AA_Desc_WyrmLanding".Translate();
                foreach (var e in landingEffects)
                    foreach (var s in e.Describe(ctx)) yield return "  • " + s;
            }
        }
    }

    public enum WyrmBroodMode
    {
        /// <summary>Every drakeling leaps onto the target.</summary>
        Pounce,
        /// <summary>The wyrm eats its own brood to heal and grow stronger.</summary>
        Cull,
    }

    public class Effect_WyrmBrood : SpellEffect
    {
        public WyrmBroodMode mode = WyrmBroodMode.Pounce;
        public ThingDef race;
        public float damage = 14f;
        public DamageDef damageDef;
        public float healPerDrakeling = 40f;
        public HediffDef wyrmHediff;
        public float maxSeverity = 8f;
        public int wyrmHediffTicks = 1800;

        [Unsaved] private Effect_Launch launcher;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var caster = ctx.caster;
            var map = ctx.map;
            if (caster == null || map == null || !ctx.Once("brood" + (int)mode)) return;
            var brood = Wyrm.Brood(caster, race).ToList();
            if (brood.Count == 0)
            {
                ArcaneFx.Text(caster, "AA_NoBrood".Translate(), ctx.Color);
                return;
            }
            if (mode == WyrmBroodMode.Pounce)
            {
                launcher ??= new Effect_Launch { damage = damage, damageDef = damageDef, splashRadius = 1.6f, splashFactor = 1f };
                var mgr = ArcaneVfxManager.For(map);
                int i = 0;
                foreach (var d in brood)
                {
                    var drake = d;
                    IntVec3 dest = CellFinder.StandableCellNear(info.cell, map, 2.5f, c => c.GetFirstPawn(map) == null);
                    if (!dest.IsValid) dest = info.cell;
                    mgr.Schedule(1 + i * 3, () =>
                    {
                        if (drake.Spawned && !drake.Downed && drake.Map == map && dest != drake.Position) launcher.Launch(ctx, drake, dest, false, true);
                    });
                    i++;
                }
                return;
            }
            var w = Wyrm.Of(caster);
            if (w == null || !w.Spawned)
            {
                ArcaneFx.Text(caster, "AA_NoWyrm".Translate(), ctx.Color);
                return;
            }
            foreach (var d in brood)
            {
                Vector3 pos = d.DrawPos;
                if (map == Find.CurrentMap)
                {
                    ArcaneVfxManager.For(map).Add(new VfxSoulWisp(pos, w, ctx.Color, ctx.Color2));
                    Wyrm.ScaleBurst(map, pos, ctx.Color, 6f);
                }
                d.Destroy(DestroyMode.Vanish);
                ArcaneUtility.HealPawn(w, healPerDrakeling * ctx.HealFactor);
                if (wyrmHediff != null) ArcaneUtility.ApplyHediff(w, wyrmHediff, 1f, Mathf.RoundToInt(wyrmHediffTicks * ctx.DurationFactor), ctx, true, maxSeverity);
            }
            ArcaneFx.Text(w, "AA_BroodCulled".Translate(brood.Count), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (mode == WyrmBroodMode.Pounce) yield return "AA_Desc_BroodPounce".Translate((damage * (ctx?.DamageFactor ?? 1f)).ToString("F0"));
            else yield return "AA_Desc_BroodCull".Translate((healPerDrakeling * (ctx?.HealFactor ?? 1f)).ToString("F0"), wyrmHediff?.label ?? "?");
        }
    }

    /// <summary>The brood hunts: wild animals across the map are brought down and left for the colony's butchers.</summary>
    public class Effect_WyrmHunt : SpellEffect
    {
        public int count = 4;
        public float maxBodySize = 1.6f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var caster = ctx.caster;
            var map = ctx.map;
            if (caster == null || map == null || !ctx.Once("hunt")) return;
            var prey = map.mapPawns.AllPawnsSpawned.Where(p => !p.Dead && p.Faction == null && p.RaceProps.Animal && !p.Position.Fogged(map) && p.BodySize <= maxBodySize
                    && !p.InMentalState && p.GetLord() == null)
                .OrderBy(p => p.Position.DistanceToSquared(caster.Position)).Take(count + ctx.mods.extraTargets).ToList();
            if (prey.Count == 0)
            {
                ArcaneFx.Text(caster, "AA_NoPrey".Translate(), ctx.Color);
                return;
            }
            var mgr = ArcaneVfxManager.For(map);
            for (int i = 0; i < prey.Count; i++)
            {
                var p = prey[i];
                mgr.Schedule(10 + i * 14, () =>
                {
                    if (p.Dead || !p.Spawned) return;
                    Vector3 pos = p.DrawPos;
                    if (map == Find.CurrentMap)
                    {
                        mgr.Add(new VfxProjectile(pos + new Vector3(-5f, 0f, 9f), pos, 14, ArcaneMats.Get("ArcanumAscendant/Vfx/DrakeSwoop"), ctx.Color, ctx.Color2, 1.3f, 0f, false, false)
                            { style = VfxStyle.Draconic });
                        mgr.Schedule(14, () => Wyrm.ScaleBurst(map, pos, ctx.Color, 5f));
                    }
                    mgr.Schedule(14, () =>
                    {
                        if (p.Dead || !p.Spawned) return;
                        p.Kill(new DamageInfo(DamageDefOf.Bite, 500f, 1f, -1f, caster));
                        var corpse = p.Corpse;
                        if (corpse != null && caster.Faction == Faction.OfPlayer) corpse.SetForbidden(false, false);
                    });
                });
            }
            Messages.Message("AA_BroodHunted".Translate(prey.Count), new LookTargets(prey.Cast<Thing>()), MessageTypeDefOf.PositiveEvent);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_WyrmHunt".Translate(count, maxBodySize.ToString("0.#"));
        }
    }

    /// <summary>The wyrm smells treasure: the nearest undiscovered veins of valuable ore are revealed.</summary>
    public class Effect_WyrmProspect : SpellEffect
    {
        public int veins = 2;
        public float minValue = 4f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var caster = ctx.caster;
            if (map == null || caster == null || !ctx.Once("prospect")) return;
            var found = new List<IntVec3>();
            foreach (var c in map.AllCells)
            {
                if (!c.Fogged(map)) continue;
                var ed = c.GetEdifice(map);
                var ore = ed?.def?.building?.mineableThing;
                if (ore == null || ore.BaseMarketValue < minValue) continue;
                found.Add(c);
            }
            if (found.Count == 0)
            {
                Messages.Message("AA_NoTreasure".Translate(), caster, MessageTypeDefOf.NeutralEvent);
                return;
            }
            var origin = (Wyrm.Of(caster) ?? caster).Position;
            var targets = new List<IntVec3>();
            for (int i = 0; i < veins + ctx.mods.extraTargets && found.Count > 0; i++)
            {
                var best = found.MinBy(c => c.DistanceToSquared(origin));
                targets.Add(best);
                found.RemoveAll(c => c.DistanceToSquared(best) < 64);
            }
            foreach (var t in targets)
            {
                foreach (var c in GenRadial.RadialCellsAround(t, 3.5f, true))
                    if (c.InBounds(map) && c.Fogged(map)) map.fogGrid.Unfog(c);
                if (map == Find.CurrentMap)
                {
                    Vector3 pos = t.ToVector3Shifted();
                    ArcaneFx.Flash(map, pos, new Color(1f, 0.82f, 0.3f), 3f);
                    Fx.Rise(map, pos, new Color(1f, 0.82f, 0.3f), 14f, PMat.Gold, 1.5f, 0.8f, 2f, 0.2f, PFlags.Glint);
                    ArcaneVfxManager.For(map).Add(new VfxPillar(pos, 60, new Color(1f, 0.82f, 0.3f), Color.white, 0.9f));
                }
            }
            Messages.Message("AA_TreasureFound".Translate(targets.Count), new LookTargets(targets.Select(t => new TargetInfo(t, map))), MessageTypeDefOf.PositiveEvent);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_WyrmProspect".Translate(veins);
        }
    }

    /// <summary>Warms fertilized eggs: they move toward hatching, and those that are ready hatch at once.</summary>
    public class Effect_WyrmBrooding : SpellEffect
    {
        public float progress = 0.5f;

        private static readonly FieldInfo Gestate = typeof(CompHatcher).GetField("gestateProgress", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int n = 0;
            foreach (var t in ColonyFx.ThingsIn(ctx, info).ToList())
            {
                var hatcher = (t as ThingWithComps)?.GetComp<CompHatcher>();
                if (hatcher == null || t.Destroyed) continue;
                n++;
                if (Gestate != null)
                {
                    float g = (float)Gestate.GetValue(hatcher) + progress * Mathf.Sqrt(Mathf.Max(0.1f, ctx.HealFactor));
                    if (g < 1f)
                    {
                        Gestate.SetValue(hatcher, g);
                        ColonyFx.Sparkle(map, t.DrawPos, ctx.Color, 2f);
                        continue;
                    }
                }
                try
                {
                    hatcher.Hatch();
                }
                catch (Exception e)
                {
                    Log.Warning("[Arcanum Ascendant] Could not hatch " + t + ": " + e.Message);
                }
            }
            if (n > 0 && ctx.caster != null && ctx.Once("brooding")) ArcaneFx.Text(ctx.caster, "AA_EggsWarmed".Translate(n), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_WyrmBrooding".Translate(progress.ToStringPercent());
        }
    }

    /// <summary>Caster effect: the richer the colony, the harder this spell hits.</summary>
    public class Effect_WyrmWealth : SpellEffect
    {
        public float per100k = 0.12f;
        public float maxBonus = 1.2f;

        public static float Wealth(Pawn caster)
        {
            var map = caster?.MapHeld;
            if (map == null || !map.IsPlayerHome) map = Find.AnyPlayerHomeMap;
            return map?.wealthWatcher?.WealthTotal ?? 0f;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p != ctx.caster) return;
            float bonus = Mathf.Min(maxBonus, Wealth(p) / 100000f * per100k);
            ctx.basePower *= 1f + bonus;
            if (p.Spawned && bonus > 0.01f) ArcaneFx.Text(p, "AA_EmpowerText".Translate(bonus.ToStringPercent("F0")), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_WyrmWealth".Translate(per100k.ToStringPercent("F0"), maxBonus.ToStringPercent("F0"));
        }
    }

    /// <summary>A dragon's power grows with its hoard: spell power from the colony's total wealth.</summary>
    public class P_WyrmHoard : ArcanePassive
    {
        public float per100kPerRank = 0.03f;
        public float maxPerRank = 0.12f;
        private static int cacheTick = -99999;
        private static float cacheWealth;

        public override string Describe(int rank) => "AA_P_WyrmHoard".Translate((per100kPerRank * rank).ToStringPercent("F0"), (maxPerRank * rank).ToStringPercent("F0"));

        public override float SpellPowerFactor(CompArcane c, int rank)
        {
            int now = Find.TickManager.TicksGame;
            if (now - cacheTick > 2000)
            {
                cacheTick = now;
                cacheWealth = Effect_WyrmWealth.Wealth(c.Pawn);
            }
            return 1f + Mathf.Min(maxPerRank * rank, cacheWealth / 100000f * per100kPerRank * rank);
        }
    }

    public class CompProperties_WyrmHoard : CompProperties
    {
        public int intervalTicks = 60000;
        public float wealthFraction = 0.0008f;
        public int min = 15;
        public int max = 180;
        public ThingDef product;

        public CompProperties_WyrmHoard()
        {
            compClass = typeof(CompWyrmHoard);
        }
    }

    /// <summary>A dragon's hoard grows by itself: once a day it yields silver in proportion to the colony's wealth, as long as the wyrm lives.</summary>
    public class CompWyrmHoard : ThingComp
    {
        private int ticks;

        public CompProperties_WyrmHoard Props => (CompProperties_WyrmHoard)props;

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned) return;
            ticks++;
            if (parent.IsHashIntervalTick(40) && parent.Map == Find.CurrentMap && VRand.Chance(0.5f))
                Fx.Rise(parent.Map, parent.DrawPos, new Color(1f, 0.85f, 0.35f), 1f, PMat.Gold, 0.6f, 0.4f, 1.4f, 0.14f, PFlags.Glint);
            if (ticks < Props.intervalTicks) return;
            ticks = 0;
            var owner = parent.TryGetComp<CompArcaneConstruct>()?.caster;
            if (owner == null || owner.Dead || Wyrm.Anywhere(owner) == null) return;
            var def = Props.product ?? ThingDefOf.Silver;
            int n = Mathf.Clamp(Mathf.RoundToInt(parent.Map.wealthWatcher.WealthTotal * Props.wealthFraction), Props.min, Props.max);
            var t = ThingMaker.MakeThing(def);
            t.stackCount = Mathf.Min(n, def.stackLimit);
            GenPlace.TryPlaceThing(t, parent.Position, parent.Map, ThingPlaceMode.Near);
            if (parent.Faction == Faction.OfPlayer) Messages.Message("AA_HoardGrew".Translate(n, def.label), parent, MessageTypeDefOf.PositiveEvent);
        }

        public override string CompInspectStringExtra()
        {
            return "AA_HoardInspect".Translate((Mathf.Max(0, Props.intervalTicks - ticks)).ToStringTicksToPeriod());
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticks, "hoardTicks");
        }
    }
}

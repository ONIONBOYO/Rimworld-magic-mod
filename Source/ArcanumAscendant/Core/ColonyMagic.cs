using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArcanumAscendant
{
    // ================================================================================================
    // Birth watch: blessings on a pregnancy that act when the child is born.
    // ================================================================================================

    public class HediffCompProperties_BirthWatch : HediffCompProperties
    {
        /// <summary>Extra babies born with the first (twins, triplets...).</summary>
        public int extraBabies;
        /// <summary>Each full point of the blessing's severity above 1 (spell power) adds another baby, up to maxExtraBabies.</summary>
        public bool extraPerSeverity;
        public int maxExtraBabies = 2;
        public bool reviveStillborn;
        public bool cureInfantIllness;
        public bool easeRecovery;
        /// <summary>Each newborn gains this many of these genes (as heritable endogenes).</summary>
        public List<GeneDef> newbornGenes;
        public int newbornGeneCount = 1;
        public HediffDef newbornHediff;
        public ThoughtDef parentThought;
        public Color color = new Color(1f, 0.6f, 0.8f);

        public HediffCompProperties_BirthWatch()
        {
            compClass = typeof(HediffComp_BirthWatch);
        }
    }

    public class HediffComp_BirthWatch : HediffComp_Throttled
    {
        private Pawn father;
        private Pawn geneticMother;
        private List<GeneDef> genes;
        private bool laboring;
        private bool done;
        private int acc;
        private List<int> knownChildren = new List<int>();

        public HediffCompProperties_BirthWatch Props => (HediffCompProperties_BirthWatch)props;

        public override bool CompShouldRemove => done;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            knownChildren = Pawn.relations?.Children?.Select(c => c.thingIDNumber).ToList() ?? new List<int>();
        }

        protected override void Process(int elapsed)
        {
            acc += elapsed;
            if (acc < 30 || done) return;
            acc = 0;
            var p = Pawn;
            if (p == null || p.Dead)
            {
                done = true;
                return;
            }
            HediffWithParents carrying = null;
            foreach (var h in p.health.hediffSet.hediffs)
                if (h is Hediff_LaborPushing || h is Hediff_Labor || h is Hediff_Pregnant)
                {
                    carrying = (HediffWithParents)h;
                    if (h is Hediff_LaborPushing) break;
                }
            if (carrying != null)
            {
                if (carrying.Father != null) father = carrying.Father;
                if (carrying.Mother != null) geneticMother = carrying.Mother;
                if (carrying.geneSet != null) genes = carrying.geneSet.GenesListForReading.ToList();
                if (carrying is Hediff_LaborPushing || carrying is Hediff_Labor) laboring = true;
                return;
            }
            if (!laboring)
            {
                // the pregnancy ended without a birth (miscarriage, termination)
                done = true;
                return;
            }
            laboring = false;
            try
            {
                OnBirth(p);
            }
            catch (Exception e)
            {
                Log.Warning("[Arcanum Ascendant] Birth blessing error: " + e);
            }
            done = true;
        }

        private List<Pawn> NewChildren(Pawn mother)
        {
            var list = new List<Pawn>();
            var kids = mother.relations?.Children;
            if (kids == null) return list;
            foreach (var c in kids)
                if (!knownChildren.Contains(c.thingIDNumber) && c.ageTracker != null && c.ageTracker.AgeBiologicalTicks < GenDate.TicksPerDay)
                    list.Add(c);
            return list;
        }

        private void Bless(Pawn baby)
        {
            if (baby == null) return;
            if (baby.Dead && Props.reviveStillborn)
            {
                foreach (var h in baby.health.hediffSet.hediffs.Where(h => h.def == HediffDefOf.Stillborn).ToList()) baby.health.RemoveHediff(h);
                if (ResurrectionUtility.TryResurrect(baby))
                {
                    Messages.Message("AA_StillbornRevived".Translate(baby.LabelShort), baby, MessageTypeDefOf.PositiveEvent);
                    foreach (var parent in new[] { Pawn, father })
                        HeartUtil.Memories(parent)?.RemoveMemoriesOfDef(ThoughtDefOf.Stillbirth);
                }
            }
            if (baby.Dead) return;
            if (Props.cureInfantIllness)
                foreach (var h in baby.health.hediffSet.hediffs.Where(h => h.def == HediffDefOf.InfantIllness).ToList()) baby.health.RemoveHediff(h);
            if (!Props.newbornGenes.NullOrEmpty() && baby.genes != null)
                foreach (var g in Props.newbornGenes.Where(g => g != null && !baby.genes.HasActiveGene(g)).InRandomOrder().Take(Props.newbornGeneCount))
                    baby.genes.AddGene(g, false);
            if (Props.newbornHediff != null) baby.health.AddHediff(Props.newbornHediff);
            if (baby.Spawned) ArcaneFx.Particles(baby.Map, baby.DrawPos, Props.color, ArcaneDefOf.AA_Fleck_Petal, 8, 1f, 0.5f);
        }

        private static RitualOutcomePossibility HealthyOutcome()
        {
            var def = DefDatabase<RitualOutcomeEffectDef>.GetNamedSilentFail("ChildBirth");
            return def?.outcomeChances?.OrderByDescending(o => o.positivityIndex).FirstOrDefault();
        }

        private void OnBirth(Pawn mother)
        {
            var born = NewChildren(mother);
            if (born.Count == 0) return;
            var first = born[0];
            foreach (var b in born) Bless(b);
            int extra = Props.extraBabies;
            if (Props.extraPerSeverity) extra += Mathf.FloorToInt(parent.Severity - 1f + 0.001f);
            extra = Mathf.Clamp(extra, 0, Props.maxExtraBabies);
            var siblings = new List<Pawn>();
            if (extra > 0 && ModsConfig.BiotechActive)
            {
                var outcome = HealthyOutcome();
                var geneList = genes ?? first.genes?.Endogenes?.Select(g => g.def).ToList() ?? new List<GeneDef>();
                for (int i = 0; i < extra && outcome != null; i++)
                {
                    var t = PregnancyUtility.ApplyBirthOutcome(outcome, 1f, null, geneList, geneticMother ?? mother, mother, father, null, null, null, true);
                    if (t is Pawn sib)
                    {
                        Bless(sib);
                        siblings.Add(sib);
                    }
                    else if (t is Corpse c)
                    {
                        Bless(c.InnerPawn);
                        if (c.InnerPawn != null) siblings.Add(c.InnerPawn);
                    }
                }
            }
            if (Props.easeRecovery)
                foreach (var h in mother.health.hediffSet.hediffs.Where(h => h.def == HediffDefOf.PostpartumExhaustion).ToList()) mother.health.RemoveHediff(h);
            if (Props.parentThought != null)
            {
                HeartUtil.Gain(mother, Props.parentThought);
                HeartUtil.Gain(father, Props.parentThought);
            }
            if (siblings.Count > 0)
            {
                var all = new List<Pawn> { first };
                all.AddRange(siblings);
                string key = all.Count == 2 ? "AA_Twins" : "AA_Triplets";
                Find.LetterStack.ReceiveLetter((key + "Label").Translate(), (key + "Text").Translate(mother.LabelShort.Named("MOTHER"),
                    string.Join(", ", all.Select(b => b.LabelShort)).Named("BABIES")), LetterDefOf.PositiveEvent, new LookTargets(all));
            }
        }

        public override string CompLabelInBracketsExtra => laboring ? "AA_InLabor".Translate().ToString() : null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref father, "father");
            Scribe_References.Look(ref geneticMother, "geneticMother");
            Scribe_Collections.Look(ref genes, "genes", LookMode.Def);
            Scribe_Values.Look(ref laboring, "laboring");
            Scribe_Values.Look(ref done, "done");
            Scribe_Collections.Look(ref knownChildren, "knownChildren", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && knownChildren == null) knownChildren = new List<int>();
        }
    }

    // ================================================================================================
    // Dreams: something happens while the bearer sleeps, and they wake changed.
    // ================================================================================================

    public class HediffCompProperties_Dream : HediffCompProperties
    {
        public ThoughtDef wakeThought;
        /// <summary>Social memory toward the dreamer's love partner, if they have one (they dreamt of them).</summary>
        public ThoughtDef partnerThought;
        /// <summary>Skill experience per hour of sleep, in the dreamer's best skill they have a passion for.</summary>
        public float xpPerHour;
        public int minSleepTicks = 1500;
        public bool removeOnWake = true;
        public Color color = new Color(0.8f, 0.6f, 1f);

        public HediffCompProperties_Dream()
        {
            compClass = typeof(HediffComp_Dream);
        }
    }

    public class HediffComp_Dream : HediffComp_Throttled
    {
        private int slept;
        private bool wasAsleep;
        private bool done;
        private SkillDef skill;
        private int fx;

        public HediffCompProperties_Dream Props => (HediffCompProperties_Dream)props;

        public override bool CompShouldRemove => done && Props.removeOnWake;

        protected override void Process(int elapsed)
        {
            var p = Pawn;
            if (p == null || p.Dead || done) return;
            bool asleep = !RestUtility.Awake(p);
            if (asleep)
            {
                slept += elapsed;
                if (Props.xpPerHour > 0f && p.skills != null)
                {
                    if (skill == null)
                        skill = p.skills.skills.Where(s => !s.TotallyDisabled).OrderByDescending(s => (int)s.passion * 100 + s.Level).FirstOrDefault()?.def;
                    if (skill != null) p.skills.GetSkill(skill).Learn(Props.xpPerHour * elapsed / 2500f, true);
                }
                fx += elapsed;
                if (fx >= 240 && p.Spawned)
                {
                    fx = 0;
                    ArcaneFx.Particles(p.Map, p.DrawPos + new Vector3(0f, 0f, 0.5f), Props.color, ArcaneDefOf.AA_Fleck_Star, 1, 0.3f, 0.4f);
                }
            }
            else if (wasAsleep && slept >= Props.minSleepTicks)
            {
                Wake(p);
            }
            wasAsleep = asleep;
        }

        private void Wake(Pawn p)
        {
            HeartUtil.Gain(p, Props.wakeThought);
            if (Props.partnerThought != null)
            {
                var partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(p, false);
                if (partner != null) HeartUtil.Gain(p, Props.partnerThought, partner);
            }
            if (p.Spawned) ArcaneFx.Text(p, "AA_WokeFromDream".Translate(), Props.color);
            slept = 0;
            done = Props.removeOnWake;
        }

        public override string CompLabelInBracketsExtra => wasAsleep ? "AA_Dreaming".Translate().ToString() : null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref slept, "slept");
            Scribe_Values.Look(ref wasAsleep, "wasAsleep");
            Scribe_Values.Look(ref done, "done");
            Scribe_Defs.Look(ref skill, "skill");
        }
    }

    // ================================================================================================
    // Small lifecycle comps
    // ================================================================================================

    /// <summary>Holds genes given by a spell and takes them away again when the status ends.</summary>
    public class HediffComp_TemporaryGenes : HediffComp
    {
        public List<GeneDef> genes = new List<GeneDef>();

        public void Track(GeneDef g)
        {
            if (g != null && !genes.Contains(g)) genes.Add(g);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            var tracker = Pawn?.genes;
            if (tracker == null) return;
            foreach (var g in genes)
            {
                var gene = tracker.Xenogenes.FirstOrDefault(x => x.def == g);
                if (gene != null) tracker.RemoveGene(gene);
            }
        }

        public override string CompTipStringExtra => genes.Count > 0 ? "AA_BorrowedGenes".Translate(string.Join(", ", genes.Select(g => g.label))).ToString() : null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look(ref genes, "genes", LookMode.Def);
            if (genes == null) genes = new List<GeneDef>();
        }
    }

    public class HediffCompProperties_TemporaryGenes : HediffCompProperties
    {
        public HediffCompProperties_TemporaryGenes()
        {
            compClass = typeof(HediffComp_TemporaryGenes);
        }
    }

    /// <summary>The bearer does not age while this lasts.</summary>
    public class HediffComp_AgeLock : HediffComp_Throttled
    {
        private long locked = -1;

        protected override void Process(int elapsed)
        {
            var at = Pawn?.ageTracker;
            if (at == null) return;
            if (locked < 0) locked = at.AgeBiologicalTicks;
            else if (at.AgeBiologicalTicks > locked) at.AgeBiologicalTicks = locked;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref locked, "locked", -1L);
        }
    }

    public class HediffCompProperties_AgeLock : HediffCompProperties
    {
        public HediffCompProperties_AgeLock()
        {
            compClass = typeof(HediffComp_AgeLock);
        }
    }

    /// <summary>When the status ends, the bearer's needs crash: the bill for what the spell gave them.</summary>
    public class HediffCompProperties_NeedsOnExpire : HediffCompProperties
    {
        public List<NeedDef> needs = new List<NeedDef>();
        public float level = 0.05f;

        public HediffCompProperties_NeedsOnExpire()
        {
            compClass = typeof(HediffComp_NeedsOnExpire);
        }
    }

    public class HediffComp_NeedsOnExpire : HediffComp
    {
        public HediffCompProperties_NeedsOnExpire Props => (HediffCompProperties_NeedsOnExpire)props;

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            var p = Pawn;
            if (p == null || p.Dead || p.needs == null) return;
            foreach (var d in Props.needs)
            {
                var n = p.needs.TryGetNeed(d);
                if (n != null) n.CurLevelPercentage = Mathf.Min(n.CurLevelPercentage, Props.level);
            }
            if (p.Spawned) ArcaneFx.Text(p, "AA_DebtComesDue".Translate(), new Color(0.8f, 0.5f, 0.5f));
        }
    }

    /// <summary>Something happens to the bearer whenever they use a weapon: lightning, fumbles, stuns.</summary>
    public class HediffCompProperties_OnVerbUsed : HediffCompProperties
    {
        public bool rangedOnly = true;
        public float chance = 1f;
        public int cooldownTicks = 60;
        public bool lightning;
        public float dropWeaponChance;
        public int stunTicks;
        public Color color = Color.white;
        public string labelKey;

        public HediffCompProperties_OnVerbUsed()
        {
            compClass = typeof(HediffComp_OnVerbUsed);
        }
    }

    public class HediffComp_OnVerbUsed : HediffComp, IArcaneSourced
    {
        public Pawn source;
        private int lastTick = -99999;

        public HediffCompProperties_OnVerbUsed Props => (HediffCompProperties_OnVerbUsed)props;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster != null) source = ctx.caster;
        }

        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            base.Notify_PawnUsedVerb(verb, target);
            var p = Pawn;
            if (p == null || !p.Spawned || p.Dead || verb == null) return;
            if (Props.rangedOnly && verb.IsMeleeAttack) return;
            int now = Find.TickManager.TicksGame;
            if (now - lastTick < Props.cooldownTicks || !Rand.Chance(Props.chance)) return;
            lastTick = now;
            var map = p.Map;
            if (Props.lightning) map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(map, p.Position));
            if (Props.dropWeaponChance > 0f && p.equipment?.Primary != null && Rand.Chance(Props.dropWeaponChance))
                p.equipment.TryDropEquipment(p.equipment.Primary, out _, p.Position, false);
            if (Props.stunTicks > 0) p.stances?.stunner?.StunFor(Props.stunTicks, source, false, true);
            if (!Props.labelKey.NullOrEmpty()) ArcaneFx.Text(p, Props.labelKey.Translate(), Props.color);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "source");
        }
    }

    /// <summary>A sworn guardian (the caster) takes part of every wound meant for the bearer.</summary>
    public class HediffCompProperties_Guardian : HediffCompProperties
    {
        public float fraction = 0.5f;
        public float maxDistance = 30f;
        public Color color = new Color(1f, 0.9f, 0.5f);

        public HediffCompProperties_Guardian()
        {
            compClass = typeof(HediffComp_Guardian);
        }
    }

    public class HediffComp_Guardian : HediffComp, IArcaneSourced
    {
        public Pawn guardian;
        private static bool redirecting;

        public HediffCompProperties_Guardian Props => (HediffCompProperties_Guardian)props;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster != null) guardian = ctx.caster;
        }

        public override bool CompShouldRemove => guardian == null || guardian.Dead;

        public static void Apply(Pawn p, ref DamageInfo dinfo)
        {
            if (redirecting || p?.health?.hediffSet == null || dinfo.Amount <= 0f) return;
            foreach (var h in p.health.hediffSet.hediffs)
            {
                if (!(h is HediffWithComps hc)) continue;
                var c = hc.TryGetComp<HediffComp_Guardian>();
                var g = c?.guardian;
                if (g == null || g == p || g.Dead || g.Downed || !g.Spawned || !p.Spawned || g.Map != p.Map || g.Position.DistanceTo(p.Position) > c.Props.maxDistance) continue;
                float moved = dinfo.Amount * c.Props.fraction;
                redirecting = true;
                try
                {
                    g.TakeDamage(new DamageInfo(dinfo.Def, moved, dinfo.ArmorPenetrationInt, dinfo.Angle, dinfo.Instigator));
                }
                finally
                {
                    redirecting = false;
                }
                dinfo.SetAmount(dinfo.Amount - moved);
                ArcaneVfxManager.For(p.Map).Add(new VfxBeam(p.DrawPos, g.DrawPos, 12, ArcaneMats.Beam, c.Props.color, Color.white, 0.35f));
                return;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref guardian, "guardian");
        }
    }

    /// <summary>When the bearer dies, something is left behind: silver for every Debt, a soul gem, and so on.</summary>
    public class HediffCompProperties_DeathLoot : HediffCompProperties
    {
        public ThingDef thing;
        public int baseCount;
        public HediffDef stackHediff;
        public float perStack;
        public float perBodySize;
        public string labelKey;
        public Color color = Color.white;

        public HediffCompProperties_DeathLoot()
        {
            compClass = typeof(HediffComp_DeathLoot);
        }
    }

    public class HediffComp_DeathLoot : HediffComp
    {
        public HediffCompProperties_DeathLoot Props => (HediffCompProperties_DeathLoot)props;

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            var p = Pawn;
            var map = p.Corpse?.MapHeld ?? p.MapHeld;
            if (map == null || Props.thing == null) return;
            var pos = p.Corpse?.PositionHeld ?? p.PositionHeld;
            float stacks = Props.stackHediff != null ? p.health.hediffSet.GetFirstHediffOfDef(Props.stackHediff)?.Severity ?? 0f : 0f;
            int n = Mathf.RoundToInt(Props.baseCount + stacks * Props.perStack + p.BodySize * Props.perBodySize);
            if (n <= 0) return;
            HearthUtil.Spawn(Props.thing, n, pos, map);
            ArcaneFx.Particles(map, pos.ToVector3Shifted(), Props.color, ArcaneDefOf.AA_Fleck_Spark, 10, 1.5f, 0.5f);
            if (!Props.labelKey.NullOrEmpty()) MoteMaker.ThrowText(pos.ToVector3Shifted(), map, Props.labelKey.Translate(n, Props.thing.label), Props.color, 3.5f);
        }
    }

    /// <summary>An undead labourer: works for the colony until the binding ends, then returns to the grave.</summary>
    public class HediffComp_Laborer : HediffComp_Throttled
    {
        public int ticksLeft = -1;

        protected override void Process(int elapsed)
        {
            if (ticksLeft < 0) return;
            ticksLeft -= elapsed;
            if (ticksLeft <= 0) Release();
        }

        private void Release()
        {
            var p = Pawn;
            if (p == null || p.Dead) return;
            var map = p.MapHeld;
            var pos = p.PositionHeld;
            try
            {
                p.jobs?.StopAll();
                if (p.Faction != null) p.SetFaction(null);
                p.Kill(null);
                var corpse = p.Corpse;
                var rot = corpse?.TryGetComp<CompRottable>();
                if (rot != null) rot.RotProgress = rot.PropsRot.TicksToDessicated + 1;
                if (map != null)
                {
                    ArcaneFx.Particles(map, pos.ToVector3Shifted(), new Color(0.6f, 1f, 0.7f), ArcaneDefOf.AA_Fleck_Smoke, 8, 1f, 1f);
                    MoteMaker.ThrowText(pos.ToVector3Shifted(), map, "AA_ReturnsToGrave".Translate(), new Color(0.6f, 1f, 0.7f), 3.5f);
                }
            }
            catch (Exception e)
            {
                Log.Warning("[Arcanum Ascendant] Laborer release error: " + e.Message);
            }
        }

        public override string CompLabelInBracketsExtra => ticksLeft > 0 ? ticksLeft.ToStringTicksToPeriod(true, true) : null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", -1);
        }
    }

    public class HediffCompProperties_Laborer : HediffCompProperties
    {
        public HediffCompProperties_Laborer()
        {
            compClass = typeof(HediffComp_Laborer);
        }
    }

    // ================================================================================================
    // Map tracker: temporary terrain, preserved things and prophesied arrivals. Saved with the map.
    // ================================================================================================

    public class MapComponent_ArcaneColony : MapComponent
    {
        private class TerrainRevert : IExposable
        {
            public IntVec3 cell;
            public TerrainDef original;
            public int until;

            public void ExposeData()
            {
                Scribe_Values.Look(ref cell, "cell");
                Scribe_Defs.Look(ref original, "original");
                Scribe_Values.Look(ref until, "until");
            }
        }

        private class Preserved : IExposable
        {
            public Thing thing;
            public int until;

            public void ExposeData()
            {
                Scribe_References.Look(ref thing, "thing");
                Scribe_Values.Look(ref until, "until");
            }
        }

        private class ArrivalMark : IExposable
        {
            public HediffDef hediff;
            public float severity;
            public int until;
            public Faction faction;

            public void ExposeData()
            {
                Scribe_Defs.Look(ref hediff, "hediff");
                Scribe_Values.Look(ref severity, "severity");
                Scribe_Values.Look(ref until, "until");
                Scribe_References.Look(ref faction, "faction");
            }
        }

        private List<TerrainRevert> reverts = new List<TerrainRevert>();
        private List<Preserved> preserved = new List<Preserved>();
        private List<ArrivalMark> marks = new List<ArrivalMark>();
        private List<int> marked = new List<int>();

        public MapComponent_ArcaneColony(Map map) : base(map)
        {
        }

        public static MapComponent_ArcaneColony For(Map map)
        {
            var c = map.GetComponent<MapComponent_ArcaneColony>();
            if (c == null)
            {
                c = new MapComponent_ArcaneColony(map);
                map.components.Add(c);
            }
            return c;
        }

        public void AddTemporaryTerrain(IntVec3 cell, TerrainDef original, int until)
        {
            var existing = reverts.FirstOrDefault(r => r.cell == cell);
            if (existing != null)
            {
                existing.until = Mathf.Max(existing.until, until);
                return;
            }
            reverts.Add(new TerrainRevert { cell = cell, original = original, until = until });
        }

        public void AddPreserved(Thing t, int until)
        {
            var existing = preserved.FirstOrDefault(r => r.thing == t);
            if (existing != null) existing.until = Mathf.Max(existing.until, until);
            else preserved.Add(new Preserved { thing = t, until = until });
        }

        public void AddArrivalMark(HediffDef hediff, float severity, int until, Faction faction)
        {
            marks.Add(new ArrivalMark { hediff = hediff, severity = severity, until = until, faction = faction });
            // pawns already here are not "arrivals"
            foreach (var p in map.mapPawns.AllPawnsSpawned)
                if (!marked.Contains(p.thingIDNumber)) marked.Add(p.thingIDNumber);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            int now = Find.TickManager.TicksGame;
            if (now % 250 != 0) return;
            try
            {
                for (int i = reverts.Count - 1; i >= 0; i--)
                {
                    var r = reverts[i];
                    if (now < r.until) continue;
                    reverts.RemoveAt(i);
                    if (r.original == null || !r.cell.InBounds(map) || r.cell.GetEdifice(map) != null) continue;
                    map.terrainGrid.SetTerrain(r.cell, r.original);
                    if (!r.cell.Walkable(map))
                        foreach (var t in r.cell.GetThingList(map).ToList())
                        {
                            if (!(t is Pawn p) || p.Dead) continue;
                            var dest = CellFinder.StandableCellNear(r.cell, map, 6f);
                            if (!dest.IsValid) continue;
                            p.Position = dest;
                            p.Notify_Teleported(true, true);
                        }
                }
                for (int i = preserved.Count - 1; i >= 0; i--)
                {
                    var pr = preserved[i];
                    if (pr.thing == null || pr.thing.Destroyed || now >= pr.until)
                    {
                        preserved.RemoveAt(i);
                        continue;
                    }
                    var rot = pr.thing.TryGetComp<CompRottable>();
                    if (rot != null) rot.RotProgress = 0f;
                }
                marks.RemoveAll(m => m.hediff == null || now >= m.until);
                if (marks.Count > 0)
                    foreach (var p in map.mapPawns.AllPawnsSpawned)
                    {
                        if (p.Dead || marked.Contains(p.thingIDNumber)) continue;
                        marked.Add(p.thingIDNumber);
                        foreach (var m in marks)
                            if (m.faction != null && p.HostileTo(m.faction))
                            {
                                ArcaneUtility.ApplyHediff(p, m.hediff, m.severity, -1, null, true);
                                ArcaneFx.Text(p, "AA_Foretold".Translate(), new Color(0.8f, 0.2f, 0.25f));
                            }
                    }
                if (marked.Count > 4000) marked.RemoveRange(0, 2000);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Arcanum Ascendant] Colony tracker error: " + e, 0x5A1C2A1);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref reverts, "reverts", LookMode.Deep);
            Scribe_Collections.Look(ref preserved, "preserved", LookMode.Deep);
            Scribe_Collections.Look(ref marks, "marks", LookMode.Deep);
            Scribe_Collections.Look(ref marked, "marked", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                reverts ??= new List<TerrainRevert>();
                preserved ??= new List<Preserved>();
                marks ??= new List<ArrivalMark>();
                marked ??= new List<int>();
                preserved.RemoveAll(p => p.thing == null);
            }
        }
    }

    // ================================================================================================
    // Game conditions
    // ================================================================================================

    /// <summary>A strong, steady wind: wind turbines run at their best.</summary>
    public class GameCondition_SteadyGale : GameCondition
    {
        public override float MinWindSpeed() => 1.3f;
    }

    /// <summary>Halcyon days: the outdoor temperature is pulled toward a pleasant one, whatever the season.</summary>
    public class GameCondition_Halcyon : GameCondition
    {
        public float target = 21f;
        public float maxOffset = 32f;

        public override float TemperatureOffset()
        {
            var map = SingleMap;
            if (map == null) return 0f;
            float baseTemp = Find.World.tileTemperatures.GetOutdoorTemp(map.Tile);
            float off = Mathf.Clamp(target - baseTemp, -maxOffset, maxOffset);
            float ramp = Mathf.Clamp01(Mathf.Min(TicksPassed, Permanent ? 99999 : TicksLeft) / 2500f);
            return off * ramp;
        }
    }

    // ================================================================================================
    // Buildings
    // ================================================================================================

    /// <summary>An ice coffin: a cryptosleep casket that melts after a while (or as soon as it is opened).</summary>
    public class Building_IceCoffin : Building_CryptosleepCasket
    {
        public int ticksLeft = -1;
        private bool held;

        public override void TickRare()
        {
            base.TickRare();
            if (!Spawned) return;
            if (HasAnyContents) held = true;
            if (ticksLeft > 0)
            {
                ticksLeft -= 250;
                if (ticksLeft <= 0)
                {
                    Melt();
                    return;
                }
            }
            if (held && !HasAnyContents) Melt();
        }

        private void Melt()
        {
            var map = Map;
            var pos = DrawPos;
            if (HasAnyContents) EjectContents();
            if (map != null) ArcaneFx.Particles(map, pos, new Color(0.7f, 0.9f, 1f), ArcaneDefOf.AA_Fleck_Shard, 12, 2f, 0.5f);
            if (!Destroyed) Destroy(DestroyMode.Vanish);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (HasAnyContents && Spawned) EjectContents();
            base.Destroy(mode);
        }

        public override string GetInspectString()
        {
            string s = base.GetInspectString();
            if (ticksLeft > 0) s = (s.NullOrEmpty() ? "" : s + "\n") + "AA_MeltsIn".Translate(ticksLeft.ToStringTicksToPeriod());
            return s;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", -1);
            Scribe_Values.Look(ref held, "held");
        }
    }

    /// <summary>A rune trap that binds and stuns enemies who step on it, then recharges.</summary>
    public class Building_SnareRune : Building_Trap
    {
        private int readyTick;
        public const int RechargeTicks = 1800;

        protected override float SpringChance(Pawn p)
        {
            if (Find.TickManager.TicksGame < readyTick) return 0f;
            if (Faction != null && (p.Faction == Faction || !p.HostileTo(Faction))) return 0f;
            return 1f;
        }

        protected override void SpringSub(Pawn p)
        {
            readyTick = Find.TickManager.TicksGame + RechargeTicks;
            if (p == null) return;
            p.stances?.stunner?.StunFor(360, this, false, true);
            var bind = DefDatabase<HediffDef>.GetNamedSilentFail("AA_Entangled");
            if (bind != null) ArcaneUtility.ApplyHediff(p, bind, 1f, 600, null);
            if (Spawned)
            {
                ArcaneFx.Flash(Map, DrawPos, new Color(1f, 0.7f, 0.25f), 2.5f);
                ArcaneFx.Particles(Map, DrawPos, new Color(1f, 0.7f, 0.25f), ArcaneDefOf.AA_Fleck_Rune, 8, 1.5f, 0.5f);
            }
        }

        public override string GetInspectString()
        {
            string s = base.GetInspectString();
            int left = readyTick - Find.TickManager.TicksGame;
            if (left > 0) s = (s.NullOrEmpty() ? "" : s + "\n") + "AA_Recharging".Translate(left.ToStringTicksToPeriod());
            return s;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref readyTick, "readyTick");
        }
    }

    // ================================================================================================
    // Items
    // ================================================================================================

    public class CompProperties_UseEffectRestoreMana : CompProperties_UseEffect
    {
        public float percent = 0.5f;

        public CompProperties_UseEffectRestoreMana()
        {
            compClass = typeof(CompUseEffect_RestoreMana);
        }
    }

    /// <summary>Soul gems: a mage breaks one to drink the mana bound inside.</summary>
    public class CompUseEffect_RestoreMana : CompUseEffect
    {
        public CompProperties_UseEffectRestoreMana PropsMana => (CompProperties_UseEffectRestoreMana)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            var c = usedBy.Arcane();
            if (c == null || !c.IsMage) return;
            c.mana = Mathf.Min(c.ManaMax, c.mana + c.ManaMax * PropsMana.percent);
            if (usedBy.Spawned) ArcaneFx.Text(usedBy, "AA_ManaDrunk".Translate(), new Color(0.6f, 0.8f, 1f));
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            if (!p.IsMage()) return "AA_Req_Mage".Translate();
            return base.CanBeUsedBy(p);
        }
    }

    public class CompProperties_TapeRecording : CompProperties
    {
        public CompProperties_TapeRecording()
        {
            compClass = typeof(CompTapeRecording);
        }
    }

    /// <summary>A recorded tape holding someone's skill. Watched once, it teaches.</summary>
    public class CompTapeRecording : ThingComp
    {
        public SkillDef skill;
        public float xp;
        public string recordedFrom;

        public override bool AllowStackWith(Thing other) => false;

        public override string TransformLabel(string label) => skill != null ? label + " (" + skill.label + ")" : label;

        public override string CompInspectStringExtra()
        {
            if (skill == null) return "AA_TapeBlank".Translate();
            return "AA_TapeContents".Translate(skill.label, xp.ToString("F0"), recordedFrom ?? "?");
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref skill, "skill");
            Scribe_Values.Look(ref xp, "xp");
            Scribe_Values.Look(ref recordedFrom, "recordedFrom");
        }
    }

    public class CompUseEffect_WatchTape : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            var rec = parent.GetComp<CompTapeRecording>();
            if (rec?.skill == null || usedBy.skills == null) return;
            usedBy.skills.GetSkill(rec.skill)?.Learn(rec.xp, true);
            Messages.Message("AA_TapeWatched".Translate(usedBy.LabelShort, rec.skill.label), usedBy, MessageTypeDefOf.PositiveEvent);
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            var rec = parent.GetComp<CompTapeRecording>();
            if (rec?.skill == null) return "AA_TapeBlank".Translate();
            if (p.skills == null || p.skills.GetSkill(rec.skill).TotallyDisabled) return "AA_CannotLearnSkill".Translate(rec.skill.label);
            return base.CanBeUsedBy(p);
        }
    }

    public class CompProperties_UseEffectWatchTape : CompProperties_UseEffect
    {
        public CompProperties_UseEffectWatchTape()
        {
            compClass = typeof(CompUseEffect_WatchTape);
        }
    }

    /// <summary>Hexwine: anything might happen to whoever drinks it.</summary>
    public class IngestionOutcomeDoer_ArcaneRandom : IngestionOutcomeDoer
    {
        public List<HediffDef> hediffs = new List<HediffDef>();
        public List<ThoughtDef> thoughts = new List<ThoughtDef>();
        public float inspirationWeight = 1f;
        public float hediffWeight = 3f;
        public float thoughtWeight = 1f;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            float roll = Rand.Range(0f, hediffWeight + thoughtWeight + inspirationWeight);
            if (roll < hediffWeight && hediffs.Count > 0)
            {
                var h = hediffs.RandomElement();
                if (h != null)
                {
                    pawn.health.AddHediff(h);
                    if (pawn.Spawned) ArcaneFx.Text(pawn, h.LabelCap, new Color(0.7f, 0.4f, 1f));
                }
                return;
            }
            roll -= hediffWeight;
            if (roll < thoughtWeight && thoughts.Count > 0)
            {
                HeartUtil.Gain(pawn, thoughts.RandomElement());
                return;
            }
            var ih = pawn.mindState?.inspirationHandler;
            if (ih != null && !ih.Inspired)
            {
                var def = DefDatabase<InspirationDef>.AllDefsListForReading.Where(d => d.Worker.InspirationCanOccur(pawn)).RandomElementWithFallback();
                if (def != null) ih.TryStartInspiration(def, "AA_Hexwine".Translate());
            }
        }
    }

    // ================================================================================================
    // Startup: statuses that block vanilla thoughts (fearlessness, psychic wards, dark sight).
    // ================================================================================================

    [StaticConstructorOnStartup]
    public static class ColonyMagicStartup
    {
        static ColonyMagicStartup()
        {
            try
            {
                Nullify("AA_Fatalism", "WitnessedDeathAlly", "WitnessedDeathNonAlly", "WitnessedDeathFamily", "ObservedLayingCorpse", "ObservedLayingRottingCorpse", "Pain");
                Nullify("AA_Conviction", "PsychicDrone");
                Nullify("AA_NightEyes", "EnvironmentDark");
            }
            catch (Exception e)
            {
                Log.Warning("[Arcanum Ascendant] Could not register thought wards: " + e.Message);
            }
        }

        private static void Nullify(string hediff, params string[] thoughts)
        {
            var h = DefDatabase<HediffDef>.GetNamedSilentFail(hediff);
            if (h == null) return;
            foreach (var name in thoughts)
            {
                var t = DefDatabase<ThoughtDef>.GetNamedSilentFail(name);
                if (t == null) continue;
                t.nullifyingHediffs ??= new List<HediffDef>();
                if (!t.nullifyingHediffs.Contains(h)) t.nullifyingHediffs.Add(h);
            }
        }
    }
}

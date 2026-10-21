using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    public class HediffCompProperties_MultipleBirth : HediffCompProperties
    {
        public int extraMin = 1;
        public int extraMax = 1;
        public float chance = 1f;

        public HediffCompProperties_MultipleBirth()
        {
            compClass = typeof(HediffComp_MultipleBirth);
        }
    }

    /// <summary>
    /// A blessing on a mother: her next birth brings extra babies (twins, triplets) or, for an animal, extra young. It
    /// watches the pregnancy (and, for people, the labor that follows) and adds the siblings through the vanilla birth
    /// so they come with genes, parents, names and all. Spent once the birth is over.
    /// </summary>
    public class HediffComp_MultipleBirth : HediffComp_Throttled
    {
        private int stage;
        private float lastProgress;
        private Pawn father;
        private List<GeneDef> genes;
        private bool spent;
        private int acc;

        public HediffCompProperties_MultipleBirth Props => (HediffCompProperties_MultipleBirth)props;

        public override bool CompShouldRemove => spent || base.CompShouldRemove;

        public override string CompLabelInBracketsExtra => stage == 2 ? "AA_InLabor".Translate().ToString() : stage == 1 ? lastProgress.ToStringPercent() : null;

        private static bool IsLabor(Hediff h) => h != null && (h.def.defName == "PregnancyLabor" || h.def.defName == "PregnancyLaborPushing");

        protected override void Process(int elapsed)
        {
            acc += elapsed;
            if (acc < 60 || spent) return;
            acc = 0;
            var mother = Pawn;
            if (mother == null || mother.Dead) return;
            var hs = mother.health.hediffSet;
            var labor = hs.hediffs.FirstOrDefault(IsLabor);
            var preg = PregnancyUtility.GetPregnancyHediff(mother) as Hediff_Pregnant ?? hs.GetFirstHediffOfDef(HediffDefOf.Pregnant) as Hediff_Pregnant;
            if (labor != null)
            {
                Record(labor as HediffWithParents);
                stage = 2;
                return;
            }
            if (preg != null)
            {
                Record(preg);
                lastProgress = preg.GestationProgress;
                stage = 1;
                return;
            }
            // the pregnancy is over: a person who was in labor, or an animal that carried to term, has given birth
            bool born = stage == 2 || stage == 1 && lastProgress >= 0.97f && !mother.RaceProps.Humanlike;
            int was = stage;
            stage = 0;
            lastProgress = 0f;
            if (was == 0) return;
            if (!born)
            {
                father = null;
                genes = null;
                return;
            }
            spent = true;
            if (!Rand.Chance(Props.chance)) return;
            int extra = Rand.RangeInclusive(Props.extraMin, Mathf.Max(Props.extraMin, Props.extraMax));
            var m = mother;
            var f = father;
            var g = genes;
            SourcedHediff.Later(mother.MapHeld, () => ExtraBirths(m, f, g, extra));
        }

        private void Record(HediffWithParents h)
        {
            if (h == null) return;
            if (h.Father != null) father = h.Father;
            var list = h.geneSet?.GenesListForReading;
            if (list != null && list.Count > 0) genes = list.ToList();
        }

        private static void ExtraBirths(Pawn mother, Pawn father, List<GeneDef> genes, int extra)
        {
            if (mother == null || mother.Dead || extra <= 0) return;
            int made = 0;
            for (int i = 0; i < extra; i++)
            {
                try
                {
                    if (mother.RaceProps.Humanlike)
                    {
                        var def = Dlc.Def<RitualOutcomeEffectDef>("ChildBirth");
                        var outcome = def?.outcomeChances?.MaxByWithFallback(o => o.positivityIndex);
                        if (outcome == null) break;
                        var baby = PregnancyUtility.ApplyBirthOutcome(outcome, 1f, null, genes, mother, mother, father, null, null, null, true);
                        if (baby != null) made++;
                    }
                    else
                    {
                        Hediff_Pregnant.DoBirthSpawn(mother, father);
                        made++;
                    }
                }
                catch (System.Exception e)
                {
                    Log.Warning("[Arcanum Ascendant] Could not add an extra birth for " + mother + ": " + e.Message);
                    break;
                }
            }
            if (made == 0) return;
            if (mother.Spawned && mother.Map == Find.CurrentMap)
            {
                ArcaneFx.Flash(mother.Map, mother.DrawPos, new Color(1f, 0.75f, 0.85f), 3f);
                Fx.Rise(mother.Map, mother.DrawPos, new Color(1f, 0.85f, 0.95f), 12f, PMat.Star, 0.8f, 0.6f, 1.6f, 0.2f, PFlags.Glint);
                Fx.Burst(mother.Map, mother.DrawPos, new Color(1f, 0.45f, 0.7f), 8f, PMat.Heart, 1.4f, 1f, 0.22f, PFlags.Wobble);
            }
            if (mother.RaceProps.Humanlike)
                Find.LetterStack.ReceiveLetter("AA_ExtraBirthLabel".Translate(made + 1), "AA_ExtraBirthText".Translate(mother.LabelShort, made + 1, mother.Named("MOTHER")),
                    LetterDefOf.PositiveEvent, mother);
            else
                Messages.Message("AA_ExtraLitter".Translate(mother.LabelShort, mother.Named("MOTHER")), mother, MessageTypeDefOf.PositiveEvent);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref stage, "stage");
            Scribe_Values.Look(ref lastProgress, "lastProgress");
            Scribe_Values.Look(ref spent, "spent");
            Scribe_References.Look(ref father, "father", true);
            Scribe_Collections.Look(ref genes, "genes", LookMode.Def);
        }
    }

    public enum DreamSubject
    {
        None,
        /// <summary>The mage who cast the dream.</summary>
        Caster,
        /// <summary>The dreamer's own beloved.</summary>
        Lover,
    }

    public class HediffCompProperties_WakeThought : HediffCompProperties
    {
        /// <summary>Mood memory gained on waking.</summary>
        public ThoughtDef thought;
        /// <summary>Opinion memory about the dream's subject, gained on waking.</summary>
        public ThoughtDef socialThought;
        public DreamSubject subject = DreamSubject.None;
        public float minSleepHours = 4f;
        public float inspirationChance;
        public InspirationDef inspiration;
        /// <summary>Added to the rest need on waking.</summary>
        public float rest;
        /// <summary>Added to the recreation need on waking.</summary>
        public float joy;
        public string textKey;
        public Color color = new Color(0.8f, 0.7f, 1f);

        public HediffCompProperties_WakeThought()
        {
            compClass = typeof(HediffComp_WakeThought);
        }
    }

    /// <summary>
    /// A dream laid on a sleeper: once they have slept long enough and wake, they get the memory (a sweet dream, a
    /// prophecy, a nightmare...), maybe an inspiration, and the status ends.
    /// </summary>
    public class HediffComp_WakeThought : HediffComp_Throttled, IArcaneSourced
    {
        public Pawn source;
        private int slept;
        private bool done;
        private int acc;

        public HediffCompProperties_WakeThought Props => (HediffCompProperties_WakeThought)props;

        public override bool CompShouldRemove => done || base.CompShouldRemove;

        public override string CompLabelInBracketsExtra => slept > 0 ? ((float)slept / GenDate.TicksPerHour).ToString("0.#") + "/" + Props.minSleepHours.ToString("0.#") + "h" : null;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster != null) source = ctx.caster;
        }

        protected override void Process(int elapsed)
        {
            acc += elapsed;
            if (acc < 30 || done) return;
            int step = acc;
            acc = 0;
            var p = Pawn;
            if (p == null || p.Dead) return;
            if (!RestUtility.Awake(p))
            {
                slept += step;
                if (p.Spawned && p.Map == Find.CurrentMap && Rand.Chance(0.35f))
                    Fx.Rise(p.Map, p.DrawPos, Props.color, 1f, PMat.Star, 0.3f, 0.3f, 1.8f, 0.14f, PFlags.Glint);
                return;
            }
            if (slept < Props.minSleepHours * GenDate.TicksPerHour) return;
            Wake(p);
        }

        private void Wake(Pawn p)
        {
            done = true;
            Pawn about = null;
            if (Props.subject == DreamSubject.Caster) about = source != null && source != p ? source : null;
            else if (Props.subject == DreamSubject.Lover) about = LovePartnerRelationUtility.ExistingMostLikedLovePartner(p, false);
            Romance.GainMemory(p, Props.thought, about);
            if (about != null) Romance.GainMemory(p, Props.socialThought, about);
            if (Props.rest != 0f && p.needs?.rest != null) p.needs.rest.CurLevel = Mathf.Clamp01(p.needs.rest.CurLevel + Props.rest);
            if (Props.joy != 0f && p.needs?.joy != null) p.needs.joy.CurLevel = Mathf.Clamp01(p.needs.joy.CurLevel + Props.joy);
            if (Props.inspirationChance > 0f && Rand.Chance(Props.inspirationChance))
            {
                var h = p.mindState?.inspirationHandler;
                if (h != null && !h.Inspired)
                {
                    var def = Props.inspiration ?? DefDatabase<InspirationDef>.AllDefsListForReading.Where(d => d.Worker.InspirationCanOccur(p)).RandomElementWithFallback();
                    if (def != null) h.TryStartInspiration(def, "AA_DreamReason".Translate(parent.def.label));
                }
            }
            if (p.Spawned)
            {
                ArcaneFx.Text(p, (Props.textKey ?? "AA_WokeFromDream").Translate(), Props.color);
                if (p.Map == Find.CurrentMap)
                {
                    Fx.Rise(p.Map, p.DrawPos, Props.color, 8f, PMat.Star, 0.6f, 0.5f, 1.4f, 0.18f, PFlags.Glint);
                    Fx.Burst(p.Map, p.DrawPos, Color.white, 4f, PMat.Petal, 1f, 0.9f, 0.16f, PFlags.Wobble);
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref slept, "slept");
            Scribe_Values.Look(ref done, "done");
            Scribe_References.Look(ref source, "source");
        }
    }
}

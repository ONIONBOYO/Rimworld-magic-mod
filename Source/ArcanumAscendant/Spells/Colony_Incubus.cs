using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // The Incubus at home: the Dreamlord's dreamwork (a feast on a dreamer's worst memories, a rude awakening for every
    // sleeper on the map) and the Pactbinder's firstborn clause (every baby the bearer gives birth to next is gifted).
    // Pregnancy and labor are Biotech content; nothing here needs the DLC to load.
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// The Dreamlord works on what people dream. mode "Devour": the target's worst bad memories are removed and the caster
    /// regains mana for each. "Wake": a sleeping colonist is woken at once, rested a little, given a status and drafted.
    /// </summary>
    public class Effect_IncubusDreamwork : SpellEffect
    {
        public string mode = "Devour";
        // Devour
        public int maxMemories = 2;
        public int bonusIfAsleep = 1;
        public float manaPerMemory = 8f;
        // Wake
        public float rest;
        public HediffDef wokenHediff;
        public bool draft;

        public Effect_IncubusDreamwork()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p == null || p.Dead || !p.RaceProps.Humanlike) return;
            if (mode == "Wake") Wake(ctx, p);
            else Devour(ctx, p);
        }

        private void Devour(SpellContext ctx, Pawn p)
        {
            var mem = p.needs?.mood?.thoughts?.memories;
            if (mem == null) return;
            int n = maxMemories + (RestUtility.Awake(p) ? 0 : bonusIfAsleep) + (ctx.mods?.extraTargets ?? 0);
            var worst = new List<Thought_Memory>();
            foreach (var m in mem.Memories)
            {
                if (m == null || m.CurStage == null) continue;
                float off;
                try
                {
                    off = m.MoodOffset();
                }
                catch
                {
                    continue;
                }
                if (off < -0.01f) worst.Add(m);
            }
            worst = worst.OrderBy(m => m.MoodOffset()).Take(Mathf.Max(0, n)).ToList();
            if (worst.Count == 0)
            {
                ArcaneFx.Text(p, "AA_IncubusNoNightmares".Translate(), ctx.Color);
                return;
            }
            foreach (var m in worst) mem.RemoveMemory(m);
            if (ctx.arcane != null && ctx.arcane.IsMage && manaPerMemory > 0f)
                ctx.arcane.mana = Mathf.Min(ctx.arcane.ManaMax, ctx.arcane.mana + manaPerMemory * worst.Count);
            ArcaneFx.Text(p, "AA_IncubusDevoured".Translate(worst.Count), ctx.Color);
            if (p.Spawned && p.Map == Find.CurrentMap)
            {
                Fx.Rise(p.Map, p.DrawPos, ctx.Color2, 8f, PMat.Star, 0.5f, 0.5f, 1.4f, 0.16f, PFlags.Glint);
                if (ctx.caster != null && ctx.caster.Spawned && ctx.caster.Map == p.Map)
                    ArcaneVfxManager.For(p.Map).Add(new VfxBeam(p.DrawPos, ctx.caster.DrawPos, 24, ArcaneMats.Get("ArcanumAscendant/Vfx/Beam"), ctx.Color, ctx.Color2, 0.4f));
            }
        }

        private void Wake(SpellContext ctx, Pawn p)
        {
            if (!p.Spawned || p.Downed || p.Faction != Faction.OfPlayer || RestUtility.Awake(p)) return;
            RestUtility.WakeUp(p);
            if (rest != 0f && p.needs?.rest != null) p.needs.rest.CurLevel = Mathf.Clamp01(p.needs.rest.CurLevel + rest);
            if (wokenHediff != null) ArcaneUtility.ApplyHediff(p, wokenHediff, 1f, -1, ctx);
            if (draft && p.drafter != null && p.IsColonistPlayerControlled && !p.Downed && !p.InMentalState) p.drafter.Drafted = true;
            ArcaneFx.Text(p, "AA_IncubusWoken".Translate(), ctx.Color);
            if (p.Map == Find.CurrentMap) Fx.Burst(p.Map, p.DrawPos, ctx.Color, 8f, PMat.Star, 2.2f, 0.6f, 0.18f, PFlags.Glint);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (mode == "Wake")
            {
                yield return "AA_Desc_IncubusWake".Translate();
                if (rest != 0f) yield return "AA_Desc_IncubusWakeRest".Translate(rest.ToStringPercent());
                if (wokenHediff != null) yield return "AA_Desc_IncubusWakeStatus".Translate(wokenHediff.label);
                if (draft) yield return "AA_Desc_IncubusWakeDraft".Translate();
                yield break;
            }
            yield return "AA_Desc_IncubusDevour".Translate(maxMemories, bonusIfAsleep, manaPerMemory.ToString("0"));
        }
    }

    public class HediffCompProperties_IncubusPromise : HediffCompProperties
    {
        /// <summary>How many of each newborn's skills become burning passions.</summary>
        public int passions = 2;
        /// <summary>One of these (that the child does not have and that does not conflict) is granted.</summary>
        public List<TraitDef> traitOptions = new List<TraitDef>();
        /// <summary>A lasting status for each gifted child.</summary>
        public HediffDef childHediff;
        /// <summary>Void the whole status on anyone but a woman (false: female animals may keep it, they are simply never gifted).</summary>
        public bool humanOnly = true;

        public HediffCompProperties_IncubusPromise()
        {
            compClass = typeof(HediffComp_IncubusPromise);
        }
    }

    /// <summary>
    /// A firstborn clause on a mother: it watches her pregnancy and labor (Biotech) and, once she has given birth, gifts
    /// every baby of hers born in the following hour (twins from other blessings included): burning passions, a fine trait
    /// and a lasting status. Spent after that birth. It voids the status it sits on when the bearer is male (or not a person,
    /// with humanOnly), so a mother's bargain laid on the wrong partner does not linger; with no gifts set it is only that guard.
    /// </summary>
    public class HediffComp_IncubusPromise : HediffComp_Throttled
    {
        private const int Window = 2500;
        private int stage;
        private int window;
        private bool done;
        private int acc;
        private List<int> known = new List<int>();

        public HediffCompProperties_IncubusPromise Props => (HediffCompProperties_IncubusPromise)props;

        public override bool CompShouldRemove => done || base.CompShouldRemove;

        public override string CompLabelInBracketsExtra => stage == 2 ? "AA_InLabor".Translate().ToString() : null;

        private static bool IsLabor(Hediff h) => h != null && (h.def.defName == "PregnancyLabor" || h.def.defName == "PregnancyLaborPushing");

        protected override void Process(int elapsed)
        {
            acc += elapsed;
            if (acc < 60 || done) return;
            int step = acc;
            acc = 0;
            var mother = Pawn;
            if (mother == null || mother.Dead || mother.health?.hediffSet == null) return;
            bool human = mother.RaceProps.Humanlike;
            if (mother.gender != Gender.Female || (Props.humanOnly && !human) || (human && mother.relations == null))
            {
                done = true;
                if (mother.Spawned) ArcaneFx.Text(mother, "AA_IncubusPromiseVoid".Translate(), new Color(1f, 0.55f, 0.3f));
                return;
            }
            if (!human) return;
            if (stage == 3)
            {
                Gift(mother);
                window -= step;
                if (window <= 0) done = true;
                return;
            }
            bool labor = mother.health.hediffSet.hediffs.Any(IsLabor);
            bool pregnant = PregnancyUtility.GetPregnancyHediff(mother) != null;
            if (labor || pregnant)
            {
                if (stage == 0) Remember(mother);
                stage = labor ? 2 : 1;
                return;
            }
            if (stage == 2)
            {
                // labor is over: the baby (and any twins that follow within the hour) are hers to gift
                stage = 3;
                window = Window;
                Gift(mother);
                return;
            }
            stage = 0;
        }

        private void Remember(Pawn mother)
        {
            known.Clear();
            foreach (var c in mother.relations.Children)
                if (c != null) known.Add(c.thingIDNumber);
        }

        private void Gift(Pawn mother)
        {
            foreach (var child in mother.relations.Children.ToList())
            {
                if (child == null || known.Contains(child.thingIDNumber)) continue;
                known.Add(child.thingIDNumber);
                if (child.Dead || child.ageTracker == null || child.ageTracker.AgeBiologicalTicks > GenDate.TicksPerDay * 2) continue;
                GiftChild(mother, child);
            }
        }

        private void GiftChild(Pawn mother, Pawn child)
        {
            var gifts = new List<string>();
            if (child.skills != null && Props.passions > 0)
            {
                foreach (var s in child.skills.skills.Where(x => !x.PermanentlyDisabled && x.passion != Passion.Major).InRandomOrder().Take(Props.passions).ToList())
                {
                    s.passion = Passion.Major;
                    gifts.Add(s.def.label);
                }
            }
            var traits = child.story?.traits;
            if (traits != null && !Props.traitOptions.NullOrEmpty())
            {
                var pick = Props.traitOptions.Where(d => d != null && !traits.HasTrait(d) && !traits.allTraits.Any(t => t.def.ConflictsWith(d) || d.ConflictsWith(t)))
                    .RandomElementWithFallback();
                if (pick != null)
                {
                    var trait = new Trait(pick, 0);
                    traits.GainTrait(trait);
                    gifts.Add(trait.Label);
                }
            }
            bool marked = false;
            if (Props.childHediff != null && child.health != null && !child.health.hediffSet.HasHediff(Props.childHediff))
            {
                child.health.AddHediff(HediffMaker.MakeHediff(Props.childHediff, child));
                marked = true;
            }
            if (gifts.Count == 0 && !marked) return;
            var map = child.MapHeld;
            if (child.Spawned && map == Find.CurrentMap)
            {
                var gold = new Color(1f, 0.82f, 0.42f);
                ArcaneFx.Flash(map, child.DrawPos, gold, 2.5f);
                Fx.Rise(map, child.DrawPos, gold, 10f, PMat.Star, 0.6f, 0.5f, 1.6f, 0.18f, PFlags.Glint);
            }
            Find.LetterStack.ReceiveLetter("AA_IncubusPromiseKeptLabel".Translate(child.LabelShort, child.Named("CHILD")),
                "AA_IncubusPromiseKept".Translate(child.LabelShort, mother.LabelShort, gifts.Count > 0 ? gifts.ToCommaList(true) : "-", child.Named("CHILD"),
                    mother.Named("MOTHER")), LetterDefOf.PositiveEvent, child);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref stage, "stage");
            Scribe_Values.Look(ref window, "window");
            Scribe_Values.Look(ref done, "done");
            Scribe_Collections.Look(ref known, "known", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && known == null) known = new List<int>();
        }
    }
}

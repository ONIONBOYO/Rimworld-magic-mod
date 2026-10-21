using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Necromancer family colony expansion: flesh grafted from the dead onto the living, and rites performed over a body
    // (last words for the grieving, knowledge and food taken from the dead, a step through a corpse).
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Restores a part the target has lost (an arm, a leg, an eye, a kidney...) with the same part taken from a fresh corpse of
    /// the same body plan lying near them. If the donor had an added part (bionic, prosthetic) there, that is what the target
    /// gets. Each donor gives one part and then crumbles, its gear left on the ground. The target gets an aching status and a
    /// mood memory. Biggest missing parts are grafted first.
    /// </summary>
    public class Effect_NecromancerGraft : SpellEffect
    {
        public float donorRadius = 5f;
        public int maxParts = 1;
        public HediffDef graftHediff;
        public ThoughtDef thought;

        public Effect_NecromancerGraft()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p == null || p.Dead || !p.Spawned || p.health?.hediffSet == null || !p.RaceProps.IsFlesh) return;
            var map = p.Map;
            var set = p.health.hediffSet;
            var missing = set.GetMissingPartsCommonAncestors()
                .Where(m => m?.Part != null && !set.PartOrAnyAncestorHasDirectlyAddedParts(m.Part))
                .OrderByDescending(m => m.Part.coverageAbsWithChildren)
                .ToList();
            if (missing.Count == 0)
            {
                ArcaneFx.Text(p, "AA_Necromancer_GraftNothing".Translate(), ctx.Color);
                return;
            }
            var donors = GenRadial.RadialDistinctThingsAround(p.Position, map, donorRadius, true).OfType<Corpse>()
                .Where(c => Usable(c, p))
                .OrderBy(c => c.Position.DistanceToSquared(p.Position))
                .ToList();
            int cap = Mathf.Max(1, maxParts + ctx.mods.extraTargets);
            int done = 0;
            foreach (var mp in missing)
            {
                if (done >= cap) break;
                var part = mp.Part;
                if (!set.PartIsMissing(part)) continue;
                foreach (var donor in donors)
                {
                    if (donor.Destroyed || !donor.Spawned) continue;
                    if (!TryGraft(p, part, donor)) continue;
                    done++;
                    Messages.Message("AA_Necromancer_Grafted".Translate(p.LabelShort, donor.InnerPawn.LabelShort, part.Label, p.Named("PAWN")),
                        p, MessageTypeDefOf.PositiveEvent);
                    Crumble(ctx, donor);
                    break;
                }
            }
            if (done == 0)
            {
                ArcaneFx.Text(p, "AA_Necromancer_GraftNoDonor".Translate(), ctx.Color);
                return;
            }
            if (graftHediff != null) ArcaneUtility.ApplyHediff(p, graftHediff, 1f, -1, ctx);
            if (thought != null && p.needs?.mood?.thoughts?.memories != null) p.needs.mood.thoughts.memories.TryGainMemory(thought);
            if (ColonyFx.Visible(map))
            {
                ArcaneFx.Particles(map, p.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Bone, 8, 1f, 0.5f);
                ColonyFx.Sparkle(map, p.DrawPos, ctx.Color2, 6f);
            }
        }

        private static bool Usable(Corpse c, Pawn patient)
        {
            var d = c?.InnerPawn;
            if (d == null || d == patient || !c.Spawned || c.Destroyed || d.health?.hediffSet == null) return false;
            if (!d.RaceProps.IsFlesh || d.RaceProps.body != patient.RaceProps.body) return false;
            return c.GetRotStage() == RotStage.Fresh;
        }

        /// <summary>Gives the patient the donor's version of the part (same record, or its mirror twin). False when the donor has none to give.</summary>
        private static bool TryGraft(Pawn p, BodyPartRecord part, Corpse donor)
        {
            var dset = donor.InnerPawn.health.hediffSet;
            var candidates = new List<BodyPartRecord> { part };
            candidates.AddRange(donor.InnerPawn.RaceProps.body.AllParts.Where(r => r != part && r.def == part.def && r.depth == part.depth));
            foreach (var dp in candidates)
            {
                // a bionic or prosthetic installed right there travels as it is
                var added = dset.hediffs.FirstOrDefault(h => h is Hediff_AddedPart && h.Part == dp);
                if (added != null)
                {
                    try
                    {
                        p.health.RestorePart(part, null, false);
                        p.health.AddHediff(added.def, part);
                        return true;
                    }
                    catch (System.Exception e)
                    {
                        Log.Warning("[Arcanum Ascendant] Could not graft " + added.def + ": " + e.Message);
                        return false;
                    }
                }
                if (!Whole(dset, dp)) continue;
                p.health.RestorePart(part);
                return true;
            }
            return false;
        }

        /// <summary>The donor has the part and everything below it, natural and unharmed by missing pieces or added parts.</summary>
        private static bool Whole(HediffSet dset, BodyPartRecord part)
        {
            foreach (var h in dset.hediffs)
            {
                if (h.Part == null || !(h is Hediff_MissingPart || h is Hediff_AddedPart)) continue;
                for (var r = h.Part; r != null; r = r.parent)
                    if (r == part) return false;
                if (h is Hediff_MissingPart || h is Hediff_AddedPart)
                    for (var r = part.parent; r != null; r = r.parent)
                        if (r == h.Part) return false;
            }
            return true;
        }

        private static void Crumble(SpellContext ctx, Corpse c)
        {
            var map = c.Map;
            var pos = c.Position;
            var draw = c.DrawPos;
            try
            {
                if (c.AnythingToStrip()) c.Strip(false);
            }
            catch (System.Exception e)
            {
                Log.Warning("[Arcanum Ascendant] Could not strip " + c + ": " + e.Message);
            }
            if (!c.Destroyed) c.Destroy(DestroyMode.Vanish);
            FilthMaker.TryMakeFilth(pos, map, ThingDefOf.Filth_Ash, 2);
            if (ColonyFx.Visible(map)) ArcaneFx.Particles(map, draw, ctx.Color, ArcaneDefOf.AA_Fleck_Smoke, 8, 0.8f, 1f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_NecromancerGraft".Translate(maxParts + (ctx?.mods?.extraTargets ?? 0), donorRadius.ToString("0.#"));
        }
    }

    /// <summary>
    /// A rite performed over the dead. Up to maxCorpses qualifying bodies in the area (and, with includeGraves, bodies resting in
    /// graves and sarcophagi there) are each given the rite: everyone mourning that person lets go of their grief (easeGrief),
    /// the sub-effects run at the body, and the body crumbles afterwards (consume, never for buried bodies). Without a body the
    /// rite does nothing at all, which is what makes the corpse-fuelled spells honest.
    /// </summary>
    public class Effect_NecromancerRite : SpellEffect
    {
        public int maxCorpses = 1;
        public bool humanlikeOnly;
        public bool notDesiccated = true;
        public bool includeGraves;
        public bool spareColonists = true;
        public bool consume;
        public bool easeGrief;
        public ThoughtDef mournerThought;
        public float subRadius;
        public List<SpellEffect> effects = new List<SpellEffect>();

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            if (map == null) return;
            var found = new List<(Corpse corpse, bool buried)>();
            var seen = new HashSet<Corpse>();
            foreach (var c in info.Cells(map))
                foreach (var t in c.GetThingList(map))
                {
                    if (t is Corpse corpse && corpse.Spawned && seen.Add(corpse)) found.Add((corpse, false));
                    else if (includeGraves && t is Building_CorpseCasket casket && casket.HasCorpse && casket.Corpse != null && seen.Add(casket.Corpse))
                        found.Add((casket.Corpse, true));
                }
            int cap = Mathf.Max(1, maxCorpses + ctx.mods.extraTargets);
            int done = 0;
            foreach (var (corpse, buried) in found.OrderBy(f => (f.corpse.PositionHeld - info.cell).LengthHorizontalSquared))
            {
                if (done >= cap) break;
                if (!Qualifies(ctx, corpse)) continue;
                done++;
                var dead = corpse.InnerPawn;
                var at = corpse.PositionHeld;
                if (easeGrief) EaseGrief(ctx, dead, corpse);
                if (!effects.NullOrEmpty())
                    SpellRunner.Impact(ctx, new ImpactInfo { cell = at, pos = at.ToVector3Shifted(), radius = subRadius, sourcePos = info.pos }, effects,
                        playVfx: false);
                if (consume && !buried && !corpse.Destroyed) Crumble(ctx, corpse);
                else if (ColonyFx.Visible(map)) ArcaneFx.Particles(map, at.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Smoke, 5, 0.6f, 0.9f);
            }
            if (done == 0 && ctx.caster != null && ctx.caster.Spawned && ctx.Once("necroRiteNone"))
                ArcaneFx.Text(ctx.caster, "AA_Necromancer_NoBody".Translate(), ctx.Color);
        }

        private bool Qualifies(SpellContext ctx, Corpse c)
        {
            var d = c?.InnerPawn;
            if (d == null || c.Destroyed || d.RaceProps == null || !d.RaceProps.IsFlesh) return false;
            if (humanlikeOnly && !d.RaceProps.Humanlike) return false;
            if (notDesiccated && c.GetRotStage() == RotStage.Dessicated) return false;
            if (spareColonists && d.RaceProps.Humanlike && ctx.caster?.Faction != null && d.Faction == ctx.caster.Faction) return false;
            return true;
        }

        private void EaseGrief(SpellContext ctx, Pawn dead, Corpse corpse)
        {
            int mourners = 0;
            foreach (var p in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive.ToList())
            {
                if (p == null || p.Dead || p == dead || !p.RaceProps.Humanlike) continue;
                if (p.Faction != Faction.OfPlayer && !p.IsPrisonerOfColony) continue;
                var mem = p.needs?.mood?.thoughts?.memories;
                if (mem == null) continue;
                var sad = new List<Thought_Memory>();
                foreach (var m in mem.Memories)
                {
                    if (m?.otherPawn != dead) continue;
                    float mood;
                    try
                    {
                        mood = m.MoodOffset();
                    }
                    catch
                    {
                        continue;
                    }
                    if (mood < 0f) sad.Add(m);
                }
                if (sad.Count == 0) continue;
                foreach (var m in sad) mem.RemoveMemory(m);
                if (mournerThought != null) mem.TryGainMemory(mournerThought);
                mourners++;
                if (p.Spawned) ArcaneFx.Text(p, "AA_Necromancer_Farewell".Translate(), ctx.Color);
            }
            if (mourners > 0)
                Messages.Message("AA_Necromancer_LastWords".Translate(dead.LabelShort, mourners, dead.Named("PAWN")), new TargetInfo(corpse.PositionHeld, ctx.map),
                    MessageTypeDefOf.PositiveEvent);
            else if (ctx.caster != null && ctx.caster.Spawned)
                ArcaneFx.Text(ctx.caster, "AA_Necromancer_NoMourners".Translate(), ctx.Color);
        }

        private static void Crumble(SpellContext ctx, Corpse c)
        {
            var map = c.Map;
            var pos = c.Position;
            var draw = c.DrawPos;
            try
            {
                if (c.AnythingToStrip()) c.Strip(false);
            }
            catch (System.Exception e)
            {
                Log.Warning("[Arcanum Ascendant] Could not strip " + c + ": " + e.Message);
            }
            if (!c.Destroyed) c.Destroy(DestroyMode.Vanish);
            if (map == null) return;
            FilthMaker.TryMakeFilth(pos, map, ThingDefOf.Filth_Ash, 2);
            if (ColonyFx.Visible(map)) ArcaneFx.Particles(map, draw, ctx.Color, ArcaneDefOf.AA_Fleck_Smoke, 8, 0.8f, 1f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            int n = maxCorpses + (ctx?.mods?.extraTargets ?? 0);
            yield return "AA_Desc_NecromancerRite".Translate(n);
            if (humanlikeOnly) yield return "AA_Desc_NecromancerRitePeople".Translate();
            if (includeGraves) yield return "AA_Desc_NecromancerRiteGraves".Translate();
            if (spareColonists) yield return "AA_Desc_NecromancerRiteSpare".Translate();
            if (easeGrief) yield return "AA_Desc_NecromancerRiteGrief".Translate();
            if (!effects.NullOrEmpty())
            {
                yield return "AA_Desc_NecromancerRiteEach".Translate();
                foreach (var e in effects)
                    foreach (var s in e.Describe(ctx)) yield return "  • " + s;
            }
            if (consume) yield return "AA_Desc_NecromancerRiteConsume".Translate();
        }
    }
}

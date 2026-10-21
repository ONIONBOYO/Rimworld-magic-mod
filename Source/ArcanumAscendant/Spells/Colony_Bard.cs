using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Bard family colony expansion: a sing-along that knits the colony together, and a jester who busks for the guests.
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Everyone awake in the area sings together. Every pair of singers gains an opinion memory of each other, each singer
    /// regains some recreation, and each gains a mood memory whose stage grows with the size of the choir. Babies, the downed,
    /// the sleeping and pawns in a mental state do not sing. The caster always sings if they are in the area.
    /// </summary>
    public class Effect_BardSingAlong : SpellEffect
    {
        public ThoughtDef opinionThought;
        /// <summary>A memory with one stage per choir size in <see cref="stageAt"/>.</summary>
        public ThoughtDef moodThought;
        public int maxSingers = 10;
        public float joy = 0.15f;
        /// <summary>Stage i of the mood memory applies when at least stageAt[i] people sing.</summary>
        public List<int> stageAt = new List<int> { 2, 4, 6, 8 };

        public override bool AppliesToPawns => false;

        private static bool CanSing(Pawn p) =>
            p != null && p.Spawned && !p.Dead && !p.Downed && p.RaceProps.Humanlike && p.relations != null && p.needs?.mood?.thoughts?.memories != null
            && !p.InMentalState && RestUtility.Awake(p) && !p.DevelopmentalStage.Baby();

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            if (map == null || !ctx.Once("bardSingAlong")) return;
            var center = info.cell;
            var pool = new List<Pawn>();
            foreach (var c in info.Cells(map))
                foreach (var t in c.GetThingList(map))
                    if (t is Pawn p && !pool.Contains(p) && CanSing(p) && ctx.Passes(p, TargetFilter.Allies)) pool.Add(p);
            int cap = Mathf.Max(2, maxSingers + ctx.mods.extraTargets);
            var singers = pool.OrderBy(p => p == ctx.caster ? -1 : p.Position.DistanceToSquared(center)).Take(cap).ToList();
            if (singers.Count < 2)
            {
                if (ctx.caster != null && ctx.caster.Spawned) ArcaneFx.Text(ctx.caster, "AA_BardSingAlongAlone".Translate(), ctx.Color);
                return;
            }

            int stage = -1;
            for (int i = 0; i < stageAt.Count; i++)
                if (singers.Count >= stageAt[i]) stage = i;
            if (moodThought?.stages != null) stage = Mathf.Min(stage, moodThought.stages.Count - 1);

            bool visible = ColonyFx.Visible(map);
            for (int i = 0; i < singers.Count; i++)
            {
                var a = singers[i];
                if (opinionThought != null)
                    for (int j = i + 1; j < singers.Count; j++)
                    {
                        var b = singers[j];
                        Romance.GainMemory(a, opinionThought, b);
                        Romance.GainMemory(b, opinionThought, a);
                    }
                if (moodThought != null && stage >= 0)
                    a.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(moodThought, stage));
                if (joy > 0f) a.needs.joy?.GainJoy(joy, JoyKindDefOf.Social);
                if (visible)
                {
                    ArcaneFx.Particles(map, a.DrawPos + new Vector3(0f, 0f, 0.4f), ctx.Color, ArcaneDefOf.AA_Fleck_Note, 3, 0.7f, 0.55f, 0f, 0.6f);
                    if (i > 0 && i <= 6) ColonyFx.Fly(map, singers[i - 1].DrawPos, a.DrawPos, ctx.Color2);
                }
            }
            if (ctx.caster != null && ctx.caster.Spawned) ArcaneFx.Text(ctx.caster, "AA_BardSangTogether".Translate(singers.Count), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            string op = opinionThought?.stages?.FirstOrDefault()?.baseOpinionOffset.ToString("+0;-0") ?? "?";
            yield return "AA_Desc_BardSingAlong".Translate(maxSingers + (ctx?.mods?.extraTargets ?? 0), op);
            if (moodThought?.stages != null && moodThought.stages.Count > 0)
                yield return "AA_Desc_BardSingAlongChoir".Translate(moodThought.stages[0].baseMoodEffect.ToString("+0;-0"),
                    moodThought.stages[moodThought.stages.Count - 1].baseMoodEffect.ToString("+0;-0"));
            if (joy > 0f) yield return "AA_Desc_BardSingAlongJoy".Translate(joy.ToStringPercent());
        }
    }

    /// <summary>
    /// The jester performs and passes the hat. Every awake humanlike guest of a non-hostile faction in the area (visitors,
    /// traders, guests; not prisoners or slaves) tips some silver, scaled by the caster's Social skill, enjoys the show, and
    /// warms their faction to the colony a little (capped per faction, never for factions without goodwill). Colonists in
    /// the area simply enjoy the show. The silver is placed at the caster's feet.
    /// </summary>
    public class Effect_JesterPassTheHat : SpellEffect
    {
        public IntRange silverPerGuest = new IntRange(10, 25);
        public float socialBonusPerLevel = 0.05f;
        public float guestJoy = 0.2f;
        public float colonistJoy = 0.1f;
        public int goodwillPerGuest = 1;
        public int maxGoodwillPerFaction = 5;

        public override bool AppliesToPawns => false;

        private float SkillFactor(Pawn caster) => 1f + socialBonusPerLevel * (caster?.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0);

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var caster = ctx.caster;
            if (map == null || caster == null || caster.Faction == null || !ctx.Once("jesterPassTheHat")) return;
            float factor = SkillFactor(caster);
            var seen = new HashSet<Pawn>();
            var goodwill = new Dictionary<Faction, int>();
            int silver = 0, audience = 0;
            bool visible = ColonyFx.Visible(map);
            foreach (var c in info.Cells(map))
                foreach (var t in c.GetThingList(map))
                {
                    if (!(t is Pawn p) || p == caster || !seen.Add(p)) continue;
                    if (p.Dead || p.Downed || !p.RaceProps.Humanlike || p.InMentalState || !RestUtility.Awake(p)) continue;
                    if (p.Faction == caster.Faction || p.IsSlaveOfColony)
                    {
                        if (colonistJoy > 0f) p.needs?.joy?.GainJoy(colonistJoy, JoyKindDefOf.Social);
                        continue;
                    }
                    if (p.Faction == null || p.IsPrisoner || p.Faction.HostileTo(caster.Faction)) continue;
                    audience++;
                    int tip = Mathf.Max(1, Mathf.RoundToInt(silverPerGuest.RandomInRange * factor));
                    silver += tip;
                    if (guestJoy > 0f) p.needs?.joy?.GainJoy(guestJoy, JoyKindDefOf.Social);
                    var f = p.Faction;
                    if (goodwillPerGuest != 0 && !f.IsPlayer && f.HasGoodwill && !f.def.PermanentlyHostileTo(FactionDefOf.PlayerColony))
                    {
                        goodwill.TryGetValue(f, out int g);
                        goodwill[f] = Mathf.Min(maxGoodwillPerFaction, g + goodwillPerGuest);
                    }
                    if (visible)
                    {
                        ColonyFx.Fly(map, p.DrawPos, caster.DrawPos, ctx.Color);
                        ArcaneFx.Text(p, "AA_JesterTip".Translate(tip), ctx.Color);
                    }
                }

            if (audience == 0)
            {
                ArcaneFx.Text(caster, "AA_JesterNoAudience".Translate(), ctx.Color);
                return;
            }

            int left = silver;
            while (left > 0)
            {
                var stack = ThingMaker.MakeThing(ThingDefOf.Silver);
                stack.stackCount = Mathf.Min(left, ThingDefOf.Silver.stackLimit);
                left -= stack.stackCount;
                GenPlace.TryPlaceThing(stack, caster.Position, map, ThingPlaceMode.Near);
            }
            foreach (var kv in goodwill)
                if (kv.Value != 0) kv.Key.TryAffectGoodwillWith(Faction.OfPlayer, kv.Value, true, false);
            if (visible) ColonyFx.Sparkle(map, caster.DrawPos, ctx.Color2, 6f);
            ArcaneFx.Text(caster, "AA_JesterHatTotal".Translate(silver, audience), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            float factor = SkillFactor(ctx?.caster);
            yield return "AA_Desc_JesterPassTheHat".Translate(Mathf.RoundToInt(silverPerGuest.min * factor), Mathf.RoundToInt(silverPerGuest.max * factor));
            if (goodwillPerGuest != 0) yield return "AA_Desc_JesterPassTheHatGoodwill".Translate(goodwillPerGuest.ToString("+0;-0"), maxGoodwillPerFaction);
            if (colonistJoy > 0f) yield return "AA_Desc_JesterPassTheHatColonists".Translate(colonistJoy.ToStringPercent());
        }
    }
}

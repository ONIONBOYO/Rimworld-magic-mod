using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    // ------------------------------------------------------------------------------------------------
    // Succubus colony expansion: mending feuds (Kiss and Make Up) and cashing in Infatuation for an
    // outcome other than damage (Swoon, Homeward Heart).
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// The target and the person they get on worst with (on the same map, of the same faction) make up: both gain an opinion
    /// memory of each other (and a mood memory). Sometimes, when both are single adults who could love each other, the old
    /// enemies fall in love instead.
    /// </summary>
    public class Effect_SuccubusReconcile : SpellEffect
    {
        public ThoughtDef opinionThought;
        public ThoughtDef moodThought;
        /// <summary>Chance that a pair of single, mutually attracted adults become lovers.</summary>
        public float loveChance = 0.3f;

        public Effect_SuccubusReconcile()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p == null || p.Dead || !p.Spawned || p.relations == null || !p.RaceProps.Humanlike || p.IsPrisoner || p.Map == null) return;
            Pawn rival = null;
            float worst = 0f;
            foreach (var o in p.Map.mapPawns.AllPawnsSpawned)
            {
                if (o == p || o.Dead || o.relations == null || !o.RaceProps.Humanlike || o.IsPrisoner || o.Faction != p.Faction) continue;
                float s = p.relations.OpinionOf(o) + o.relations.OpinionOf(p);
                if (s < worst)
                {
                    worst = s;
                    rival = o;
                }
            }
            if (rival == null)
            {
                ArcaneFx.Text(p, "AA_SuccubusNoFeud".Translate(), ctx.Color);
                return;
            }
            if (!ctx.Once("reconcile" + Mathf.Min(p.thingIDNumber, rival.thingIDNumber) + "_" + Mathf.Max(p.thingIDNumber, rival.thingIDNumber))) return;
            Romance.GainMemory(p, opinionThought, rival);
            Romance.GainMemory(rival, opinionThought, p);
            Romance.GainMemory(p, moodThought, rival);
            Romance.GainMemory(rival, moodThought, p);
            Romance.LinkFx(p, rival, ctx.Color);
            bool lovers = false;
            if (loveChance > 0f && Romance.Bond(p, rival) == null && Romance.CanPair(p, rival, true) && Rand.Chance(loveChance))
                lovers = Romance.Raise(p, rival, PawnRelationDefOf.Lover) != null;
            if (lovers && (PawnUtility.ShouldSendNotificationAbout(p) || PawnUtility.ShouldSendNotificationAbout(rival)))
                Find.LetterStack.ReceiveLetter("AA_SuccubusEnemiesToLoversLabel".Translate(p.LabelShort, rival.LabelShort, p.Named("A"), rival.Named("B")),
                    "AA_SuccubusEnemiesToLovers".Translate(p.LabelShort, rival.LabelShort, ctx.caster?.LabelShort ?? "?", p.Named("A"), rival.Named("B")),
                    LetterDefOf.PositiveEvent, new LookTargets(p, rival));
            else ArcaneFx.Text(p, "AA_SuccubusMadeUp".Translate(rival.LabelShort), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_SuccubusReconcile".Translate();
            if (opinionThought?.stages != null && opinionThought.stages.Count > 0)
                yield return "AA_Desc_OpinionMemory".Translate(opinionThought.stages[0].label, opinionThought.stages[0].baseOpinionOffset.ToString("+0;-0"));
            if (moodThought?.stages != null && moodThought.stages.Count > 0)
                yield return "AA_Desc_MoodMemory".Translate(moodThought.stages[0].label, moodThought.stages[0].baseMoodEffect.ToString("+0;-0"));
            if (loveChance > 0f) yield return "AA_Desc_SuccubusReconcileLove".Translate(loveChance.ToStringPercent());
        }
    }

    /// <summary>
    /// Cashes in a stacking status: only targets carrying at least minStacks of it are affected, the stacks are spent, and the
    /// inner effects play on them. Bosses, the very large and (optionally) machines are unmoved.
    /// </summary>
    public class Effect_SuccubusHeartsDue : SpellEffect
    {
        public HediffDef stackHediff;
        public float minStacks = 1f;
        public bool consume = true;
        public float maxBodySize = 999f;
        public bool fleshOnly;
        public bool noBosses = true;
        /// <summary>Text shown over a target that doesn't carry enough of the status (default: not smitten enough).</summary>
        public string missingKey;
        /// <summary>No floating text over targets that don't qualify (for area spells).</summary>
        public bool quiet;
        public List<SpellEffect> effects = new List<SpellEffect>();

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (stackHediff == null || p?.health?.hediffSet == null || p.Dead || effects.NullOrEmpty()) return;
            if (noBosses && (p.kindDef?.isBoss ?? false) || p.BodySize > maxBodySize || fleshOnly && !p.RaceProps.IsFlesh)
            {
                if (p.Spawned && !quiet) ArcaneFx.Text(p, "AA_SuccubusUnmoved".Translate(), ctx.Color);
                return;
            }
            var h = p.health.hediffSet.GetFirstHediffOfDef(stackHediff);
            if (h == null || h.Severity < minStacks - 0.01f)
            {
                if (p.Spawned && !quiet)
                    ArcaneFx.Text(p, missingKey.NullOrEmpty() ? "AA_SuccubusNotSmitten".Translate(Mathf.FloorToInt(h?.Severity ?? 0f), Mathf.CeilToInt(minStacks)).ToString()
                        : missingKey.Translate().ToString(), ctx.Color);
                return;
            }
            if (consume) p.health.RemoveHediff(h);
            foreach (var e in effects)
            {
                if (e == null || p.Dead || p.Destroyed || !e.AppliesToPawns || !Rand.Chance(e.chance)) continue;
                if (!ctx.Passes(p, e.affects == TargetFilter.Inherit ? TargetFilter.All : e.affects)) continue;
                e.ApplyToPawn(ctx, p, info);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (stackHediff == null) yield break;
            yield return (consume ? "AA_Desc_SuccubusHeartsDueSpent" : "AA_Desc_SuccubusHeartsDue").Translate(Mathf.CeilToInt(minStacks), stackHediff.label);
            foreach (var e in effects)
            {
                if (e == null) continue;
                foreach (var s in e.Describe(ctx)) yield return "  " + s;
            }
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Base tree: Glamour's admirers, Fickle Heart, Last Embrace
    // ------------------------------------------------------------------------------------------------

    public class HediffCompProperties_SuccubusAdoration : HediffCompProperties
    {
        public HediffDef hediff;
        public float severity = 1f;
        public float maxSeverity = 10f;
        public int durationTicks = 1800;
        /// <summary>The same attacker gains a stack at most this often.</summary>
        public int cooldownTicks = 60;
        public Color color = new Color(1f, 0.37f, 0.64f);

        public HediffCompProperties_SuccubusAdoration()
        {
            compClass = typeof(HediffComp_SuccubusAdoration);
        }
    }

    /// <summary>Whoever hurts the bearer falls a little more in love with the one who glamoured them.</summary>
    public class HediffComp_SuccubusAdoration : HediffComp, IArcaneSourced
    {
        public Pawn source;
        public AbilityDef spell;
        private Dictionary<int, int> lastHit = new Dictionary<int, int>();

        public HediffCompProperties_SuccubusAdoration Props => (HediffCompProperties_SuccubusAdoration)props;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster == null) return;
            source = ctx.caster;
            if (ctx.def != null) spell = ctx.def;
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            var p = Pawn;
            if (Props.hediff == null || p == null || !(dinfo.Instigator is Pawn att) || att == p || att.Dead || !att.Spawned || !att.HostileTo(p)) return;
            int now = Find.TickManager.TicksGame;
            lastHit ??= new Dictionary<int, int>();
            if (lastHit.TryGetValue(att.thingIDNumber, out int last) && now - last < Props.cooldownTicks) return;
            lastHit[att.thingIDNumber] = now;
            var map = att.Map;
            var src = source;
            var sp = spell;
            var pr = Props;
            SourcedHediff.Later(map, () =>
            {
                if (att.Dead || !att.Spawned) return;
                var ctx = SourcedHediff.Context(src, sp, map, pr.color);
                ArcaneUtility.ApplyHediff(att, pr.hediff, pr.severity, pr.durationTicks, ctx, true, pr.maxSeverity);
                if (map == Find.CurrentMap) Fx.Burst(map, att.DrawPos, pr.color, 2f, PMat.Heart, 0.8f, 0.7f, 0.18f, PFlags.Wobble);
            });
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "source");
            Scribe_Defs.Look(ref spell, "spell");
        }
    }

    /// <summary>Every stack of a status on enemies around the target flies to the target and piles up there.</summary>
    public class Effect_SuccubusGatherHearts : SpellEffect
    {
        public HediffDef stackHediff;
        public float gatherRadius = 7f;
        public float bonus = 1f;
        public float maxSeverity = 10f;
        public int durationTicks = 1800;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (stackHediff == null || !p.Spawned) return;
            var map = p.Map;
            float sum = 0f;
            float r = Mathf.Min(gatherRadius * ctx.RadiusFactor, GenRadial.MaxRadialPatternRadius - 1f);
            foreach (var t in GenRadial.RadialDistinctThingsAround(p.Position, map, r, true).ToList())
            {
                if (!(t is Pawn o) || o == p || o.Dead) continue;
                var h = o.health?.hediffSet?.GetFirstHediffOfDef(stackHediff);
                if (h == null) continue;
                sum += h.Severity;
                o.health.RemoveHediff(h);
                if (map == Find.CurrentMap)
                    ArcaneVfxManager.For(map).Add(new VfxProjectile(o.DrawPos, p.DrawPos, 20, ArcaneMats.Get("ArcanumAscendant/Vfx/Heart"), ctx.Color, Color.white, 0.45f, 1.0f,
                        false, false) { style = VfxStyle.Desire });
            }
            ArcaneUtility.ApplyHediff(p, stackHediff, sum + bonus, durationTicks, ctx, true, maxSeverity);
            ArcaneFx.Text(p, "AA_SuccubusHeartsGathered".Translate(Mathf.FloorToInt(sum + bonus)), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (stackHediff != null) yield return "AA_Desc_SuccubusGather".Translate(stackHediff.label, (gatherRadius * (ctx?.RadiusFactor ?? 1f)).ToString("F0"), bonus.ToString("0"));
        }
    }

    /// <summary>
    /// A thrall's last embrace: an enthralled enemy is given back to its own side just long enough to burst, wounding and
    /// infatuating every enemy around it.
    /// </summary>
    public class Effect_SuccubusLastEmbrace : SpellEffect
    {
        public float damage = 40f;
        public float burstRadius = 4f;
        public DamageDef damageDef;
        public HediffDef spreadHediff;
        public float spreadSeverity = 2f;

        public Effect_SuccubusLastEmbrace()
        {
            affects = TargetFilter.AllButSelf;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var dom = p.health?.hediffSet?.GetFirstHediffOfDef(ArcaneDefOf.AA_Dominated);
            if (dom == null || !p.Spawned)
            {
                if (p.Spawned) ArcaneFx.Text(p, "AA_SuccubusNotThrall".Translate(), ctx.Color);
                return;
            }
            var map = p.Map;
            IntVec3 cell = p.Position;
            Vector3 pos = p.DrawPos;
            // hand it back to its own faction first, so its death is theirs to mourn
            p.health.RemoveHediff(dom);
            float r = Mathf.Min(burstRadius * ctx.RadiusFactor, GenRadial.MaxRadialPatternRadius - 1f);
            float amount = damage * ctx.DamageFactor;
            bool prevSpellDamage = CompArcane.applyingSpellDamage;
            CompArcane.applyingSpellDamage = true;
            try
            {
                foreach (var t in GenRadial.RadialDistinctThingsAround(cell, map, r, true).ToList())
                {
                    if (!(t is Pawn o) || o == p || o.Dead || !ctx.IsEnemy(o)) continue;
                    o.TakeDamage(new DamageInfo(damageDef ?? DefDatabase<DamageDef>.GetNamedSilentFail("AA_Heartbreak") ?? ArcaneDefOf.AA_Arcane, amount, 0.5f, -1f, ctx.caster));
                    if (spreadHediff != null && !o.Dead) ArcaneUtility.ApplyHediff(o, spreadHediff, spreadSeverity, 1800, ctx, true, 10f);
                }
                if (!p.Dead) p.Kill(new DamageInfo(damageDef ?? ArcaneDefOf.AA_Arcane, 9999f, 1f, -1f, ctx.caster));
            }
            finally
            {
                CompArcane.applyingSpellDamage = prevSpellDamage;
            }
            if (map == Find.CurrentMap)
            {
                ArcaneFx.Flash(map, pos, ctx.Color, r * 1.4f);
                Fx.Burst(map, pos, ctx.Color, 24f, PMat.Heart, 4f, 1.0f, 0.3f, PFlags.Wobble);
                Fx.Burst(map, pos, ctx.Color2, 16f, PMat.Petal, 3f, 1.2f, 0.25f, PFlags.Wobble);
                ArcaneVfxManager.For(map).Add(new VfxSprite { mat = ArcaneMats.Ring, pos = pos, scaleStart = 0.4f, scaleEnd = r * 2.2f, duration = 26, color = ctx.Color });
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_SuccubusLastEmbrace".Translate((damage * (ctx?.DamageFactor ?? 1f)).ToString("F0"), (burstRadius * (ctx?.RadiusFactor ?? 1f)).ToString("0.#"),
                spreadSeverity.ToString("0"));
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Temptress: the court. Favourites court her while she (or her champion) is near, and defect for good at 100%.
    // ------------------------------------------------------------------------------------------------

    public class HediffCompProperties_SuccubusCourtship : HediffCompProperties
    {
        /// <summary>She or her champion must be this close for courtship to grow.</summary>
        public float radius = 9f;
        /// <summary>Courtship gained per second near her (1 = won).</summary>
        public float perSecond = 0.03f;
        public HediffDef championHediff;
        /// <summary>A status on her that makes every favourite court twice as fast.</summary>
        public HediffDef sessionHediff;
        public float bossFactor = 0.25f;
        public int spurnStunTicks = 600;
        public Color color = new Color(1f, 0.48f, 0.72f);

        public HediffCompProperties_SuccubusCourtship()
        {
            compClass = typeof(HediffComp_SuccubusCourtship);
        }
    }

    public class HediffComp_SuccubusCourtship : HediffComp_Throttled, IArcaneSourced
    {
        public Pawn source;
        public float rate = 1f;
        private bool won;
        private int acc;

        public HediffCompProperties_SuccubusCourtship Props => (HediffCompProperties_SuccubusCourtship)props;

        public override string CompLabelInBracketsExtra => parent.Severity.ToStringPercent("F0");

        public override bool CompShouldRemove => won || base.CompShouldRemove;

        public void SetSource(SpellContext ctx)
        {
            if (ctx?.caster == null) return;
            source = ctx.caster;
            rate = Mathf.Max(rate, ctx.DurationFactor);
        }

        private bool Near(Pawn p)
        {
            if (p.Position.InHorDistOf(source.Position, Props.radius)) return true;
            if (Props.championHediff == null) return false;
            foreach (var o in p.Map.mapPawns.SpawnedPawnsInFaction(source.Faction))
                if (o != source && !o.Dead && o.health.hediffSet.HasHediff(Props.championHediff) && p.Position.InHorDistOf(o.Position, Props.radius)) return true;
            return false;
        }

        protected override void Process(int elapsed)
        {
            acc += elapsed;
            if (acc < 60 || won) return;
            float secs = acc / 60f;
            acc = 0;
            var p = Pawn;
            if (p == null || p.Dead || !p.Spawned || source == null || source.Dead || !source.Spawned || source.Map != p.Map || source.Faction == null) return;
            if (!p.HostileTo(source) || !Near(p)) return;
            float r = Props.perSecond * rate * secs;
            if (Props.sessionHediff != null && source.health.hediffSet.HasHediff(Props.sessionHediff)) r *= 2f;
            if (p.kindDef?.isBoss ?? false) r *= Props.bossFactor;
            parent.Severity = Mathf.Min(1f, parent.Severity + r);
            if (p.Map == Find.CurrentMap && Rand.Chance(0.4f)) Fx.Burst(p.Map, p.DrawPos, Props.color, 1f, PMat.Heart, 0.5f, 0.8f, 0.16f, PFlags.Wobble);
            if (parent.Severity < 0.999f) return;
            won = true;
            var src = source;
            var stun = Props.spurnStunTicks;
            SourcedHediff.Later(p.Map, () => SuccubusCourt.Win(p, src, stun));
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref source, "source");
            Scribe_Values.Look(ref rate, "rate", 1f);
            Scribe_Values.Look(ref won, "won");
        }
    }

    public static class SuccubusCourt
    {
        public static bool Unwinnable(Pawn p) => (p.kindDef?.isBoss ?? false) || p.RaceProps.IsMechanoid || !(p.RaceProps.Humanlike || p.RaceProps.Animal);

        /// <summary>A favourite whose courtship is complete changes sides for good (bosses and machines are only humbled).</summary>
        public static void Win(Pawn p, Pawn source, int spurnStunTicks)
        {
            if (p == null || p.Dead || source == null || source.Faction == null) return;
            if (Unwinnable(p))
            {
                if (p.Spawned)
                {
                    p.stances?.stunner?.StunFor(spurnStunTicks, source);
                    ArcaneFx.Text(p, "AA_SuccubusHumbled".Translate(), new Color(1f, 0.48f, 0.72f));
                }
                return;
            }
            if (p.InMentalState) p.MentalState.RecoverFromState();
            if (source.Faction == Faction.OfPlayer) InteractionWorker_RecruitAttempt.DoRecruit(source, p, true);
            else ArcaneUtility.BindToFaction(p, source.Faction);
            p.jobs?.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
            if (p.Spawned && p.Map == Find.CurrentMap)
            {
                ArcaneFx.Flash(p.Map, p.DrawPos, new Color(1f, 0.48f, 0.72f), 3f);
                Fx.Burst(p.Map, p.DrawPos, new Color(1f, 0.48f, 0.72f), 16f, PMat.Heart, 2.5f, 1f, 0.24f, PFlags.Wobble);
            }
        }

        public static IEnumerable<(Pawn pawn, Hediff favour)> Favourites(Pawn source, HediffDef favour)
        {
            if (source?.Map == null || favour == null) yield break;
            foreach (var p in source.Map.mapPawns.AllPawnsSpawned.ToList())
            {
                var h = p.health?.hediffSet?.GetFirstHediffOfDef(favour);
                if (h is HediffWithComps hc && hc.TryGetComp<HediffComp_SuccubusCourtship>()?.source == source) yield return (p, h);
            }
        }
    }

    public enum SuccubusCourtMode
    {
        /// <summary>Make an enemy a favourite (her court has only so many seats).</summary>
        Beckon,
        /// <summary>A favourite who is at least halfway won defects at once.</summary>
        Betray,
        /// <summary>Throw a favourite over: the more they adored her, the harder they fall.</summary>
        CastAside,
        /// <summary>Every favourite on the map lays down arms and courts her a little more.</summary>
        Decree,
        /// <summary>Favourites near her give up the fight and go home; their people think better of the colony.</summary>
        Dismiss,
    }

    public class Effect_SuccubusCourt : SpellEffect
    {
        public SuccubusCourtMode mode = SuccubusCourtMode.Beckon;
        public HediffDef favour;
        public int seats = 3;
        public bool ignoreSeats;
        public float startCourtship = 0.05f;
        /// <summary>Beckon: each stack of this status on the target (spent) gives a head start.</summary>
        public HediffDef headStartHediff;
        public float headStartPerStack = 0.08f;
        public float minCourtship = 0.5f;
        public float damage = 20f;
        public float damagePerCourtship = 60f;
        public int stunTicks = 120;
        public int stunPerCourtship = 480;
        public HediffDef decreeHediff;
        public float decreeCourtship = 0.15f;
        public int goodwillPerDismissed = 2;

        public Effect_SuccubusCourt()
        {
            affects = TargetFilter.Enemies;
        }

        public override bool AppliesToPawns => mode != SuccubusCourtMode.Decree;

        private Hediff Favour(Pawn p, Pawn source)
        {
            var h = p.health?.hediffSet?.GetFirstHediffOfDef(favour);
            return h is HediffWithComps hc && hc.TryGetComp<HediffComp_SuccubusCourtship>()?.source == source ? h : null;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var caster = ctx.caster;
            if (favour == null || caster == null || p == caster || p.Dead) return;
            switch (mode)
            {
                case SuccubusCourtMode.Beckon: Beckon(ctx, p); break;
                case SuccubusCourtMode.Betray:
                {
                    var h = Favour(p, caster);
                    if (h == null || h.Severity < minCourtship - 0.001f)
                    {
                        ArcaneFx.Text(p, "AA_SuccubusNotReady".Translate((h?.Severity ?? 0f).ToStringPercent("F0"), minCourtship.ToStringPercent("F0")), ctx.Color);
                        return;
                    }
                    p.health.RemoveHediff(h);
                    SuccubusCourt.Win(p, caster, 600);
                    break;
                }
                case SuccubusCourtMode.CastAside:
                {
                    var h = Favour(p, caster);
                    if (h == null)
                    {
                        ArcaneFx.Text(p, "AA_SuccubusNotFavoured".Translate(), ctx.Color);
                        return;
                    }
                    float c = h.Severity;
                    p.health.RemoveHediff(h);
                    bool prev = CompArcane.applyingSpellDamage;
                    CompArcane.applyingSpellDamage = true;
                    try
                    {
                        p.TakeDamage(new DamageInfo(DefDatabase<DamageDef>.GetNamedSilentFail("AA_Heartbreak") ?? ArcaneDefOf.AA_Arcane,
                            (damage + damagePerCourtship * c) * ctx.DamageFactor, 1f, -1f, caster));
                    }
                    finally
                    {
                        CompArcane.applyingSpellDamage = prev;
                    }
                    if (!p.Dead) p.stances?.stunner?.StunFor(Mathf.RoundToInt((stunTicks + stunPerCourtship * c) * ctx.DurationFactor), caster);
                    ArcaneFx.Text(p, "AA_SuccubusCastAside".Translate(), ctx.Color);
                    break;
                }
                case SuccubusCourtMode.Dismiss:
                {
                    var h = Favour(p, caster);
                    if (h == null) return;
                    p.health.RemoveHediff(h);
                    if (p.RaceProps.Humanlike && !(p.kindDef?.isBoss ?? false))
                    {
                        p.mindState?.mentalStateHandler?.TryStartMentalState(MentalStateDefOf.PanicFlee, null, true);
                        if (p.Faction != null && !p.Faction.IsPlayer && goodwillPerDismissed != 0 && !p.Faction.def.PermanentlyHostileTo(FactionDefOf.PlayerColony)
                            && caster.Faction == Faction.OfPlayer)
                            p.Faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwillPerDismissed, false, false);
                    }
                    else p.stances?.stunner?.StunFor(Mathf.RoundToInt(240 * ctx.DurationFactor), caster);
                    ArcaneFx.Text(p, "AA_SuccubusDismissed".Translate(), ctx.Color);
                    break;
                }
            }
        }

        private void Beckon(SpellContext ctx, Pawn p)
        {
            var caster = ctx.caster;
            if (!p.HostileTo(caster) || p.IsPrisoner) return;
            if (p.RaceProps.IsMechanoid || !(p.RaceProps.Humanlike || p.RaceProps.Animal))
            {
                ArcaneFx.Text(p, "AA_SuccubusUnmoved".Translate(), ctx.Color);
                return;
            }
            float start = startCourtship;
            if (headStartHediff != null)
            {
                var inf = p.health.hediffSet.GetFirstHediffOfDef(headStartHediff);
                if (inf != null)
                {
                    start += inf.Severity * headStartPerStack;
                    p.health.RemoveHediff(inf);
                }
            }
            var existing = Favour(p, caster);
            if (existing != null)
            {
                existing.Severity = Mathf.Min(0.99f, existing.Severity + start);
                return;
            }
            if (!ignoreSeats)
            {
                var court = SuccubusCourt.Favourites(caster, favour).Where(x => x.pawn != p).OrderBy(x => x.favour.Severity).ToList();
                int max = seats + ctx.mods.extraTargets;
                for (int i = 0; i <= court.Count - max; i++)
                {
                    court[i].pawn.health.RemoveHediff(court[i].favour);
                    ArcaneFx.Text(court[i].pawn, "AA_SuccubusLostInterest".Translate(), ctx.Color);
                }
            }
            var h = ArcaneUtility.ApplyHediff(p, favour, Mathf.Clamp(start, 0.01f, 0.99f), -1, ctx);
            if (h != null) ArcaneFx.Text(p, "AA_SuccubusBeckoned".Translate(h.Severity.ToStringPercent("F0")), ctx.Color);
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (mode != SuccubusCourtMode.Decree || favour == null || ctx.caster == null || !ctx.Once("decree")) return;
            foreach (var (p, h) in SuccubusCourt.Favourites(ctx.caster, favour).ToList())
            {
                if (decreeHediff != null) ArcaneUtility.ApplyHediff(p, decreeHediff, 1f, -1, ctx);
                h.Severity = Mathf.Min(1f, h.Severity + decreeCourtship);
                if (p.Map == Find.CurrentMap)
                    ArcaneVfxManager.For(p.Map).Add(new VfxProjectile(ctx.caster.DrawPos, p.DrawPos, 24, ArcaneMats.Get("ArcanumAscendant/Vfx/Heart"), ctx.Color, Color.white,
                        0.5f, 1.2f, false, false) { style = VfxStyle.Desire });
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            switch (mode)
            {
                case SuccubusCourtMode.Beckon:
                    yield return "AA_Desc_SuccubusBeckon".Translate(startCourtship.ToStringPercent("F0"), ignoreSeats ? "-" : (seats + (ctx?.mods?.extraTargets ?? 0)).ToString());
                    if (headStartHediff != null) yield return "AA_Desc_SuccubusHeadStart".Translate(headStartHediff.label, headStartPerStack.ToStringPercent("F0"));
                    break;
                case SuccubusCourtMode.Betray: yield return "AA_Desc_SuccubusBetray".Translate(minCourtship.ToStringPercent("F0")); break;
                case SuccubusCourtMode.CastAside:
                    yield return "AA_Desc_SuccubusCastAside".Translate((damage * (ctx?.DamageFactor ?? 1f)).ToString("F0"), ((damage + damagePerCourtship) * (ctx?.DamageFactor ?? 1f)).ToString("F0"),
                        ((stunTicks + stunPerCourtship) / 60f).ToString("0.#"));
                    break;
                case SuccubusCourtMode.Decree: yield return "AA_Desc_SuccubusDecree".Translate(decreeCourtship.ToStringPercent("F0")); break;
                case SuccubusCourtMode.Dismiss: yield return "AA_Desc_SuccubusDismiss".Translate(goodwillPerDismissed); break;
            }
        }
    }
}

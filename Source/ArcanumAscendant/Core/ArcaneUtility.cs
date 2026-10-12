using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ArcanumAscendant
{
    public static class ArcaneUtility
    {
        public static CompArcane Arcane(this Pawn p) => p?.TryGetComp<CompArcane>();

        public static string ToStringTicksToPeriod(this float ticks) => Mathf.RoundToInt(ticks).ToStringTicksToPeriod();

        public static bool IsMage(this Pawn p) => p?.TryGetComp<CompArcane>()?.IsMage ?? false;

        public static void PayHealth(Pawn p, float amount)
        {
            if (amount <= 0f || p.Dead) return;
            var part = p.health.hediffSet.GetNotMissingParts().Where(x => x.depth == BodyPartDepth.Outside && x.coverageAbs > 0f).RandomElementWithFallback();
            var dinfo = new DamageInfo(DamageDefOf.Cut, amount, 999f, -1f, null, part);
            dinfo.SetIgnoreInstantKillProtection(false);
            CompArcane.applyingSpellDamage = true;
            try { p.TakeDamage(dinfo); }
            finally { CompArcane.applyingSpellDamage = false; }
            if (p.Spawned) ArcaneFx.Particles(p.Map, p.DrawPos, new Color(0.8f, 0.05f, 0.1f), ArcaneDefOf.AA_Fleck_Mote, 5, 1.2f, 0.5f);
        }

        /// <summary>Heals injuries (most severe first). Returns the amount actually healed.</summary>
        public static float HealPawn(Pawn p, float amount, bool scars = false)
        {
            if (p?.health == null || p.Dead || amount <= 0f) return 0f;
            float left = amount;
            var injuries = p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Where(i => scars || !i.IsPermanent()).OrderByDescending(i => i.Severity).ToList();
            foreach (var inj in injuries)
            {
                if (left <= 0f) break;
                float h = Mathf.Min(left, inj.Severity);
                inj.Heal(h);
                left -= h;
            }
            return amount - left;
        }

        public static void Cleanse(Pawn p, bool diseases, bool toxins, bool drugs, bool debuffs = true)
        {
            foreach (var h in p.health.hediffSet.hediffs.ToList())
            {
                if (h is Hediff_Injury || h is Hediff_MissingPart || h is Hediff_AddedPart) continue;
                if (h.def == ArcaneDefOf.AA_ArcaneCore || h.def == ArcaneDefOf.AA_Summoned || h.def == ArcaneDefOf.AA_Undead || h.def == ArcaneDefOf.AA_Dominated) continue;
                bool remove = false;
                if (diseases && h.def.makesSickThought) remove = true;
                if (diseases && h.def.isInfection) remove = true;
                if (toxins && (h.def == HediffDefOf.ToxicBuildup || h.def == HediffDefOf.FoodPoisoning)) remove = true;
                if (drugs && h.def.IsAddiction) remove = true;
                if (debuffs && h.def.isBad && (h.def.defName.StartsWith("AA_") || h.def == HediffDefOf.Hypothermia || h.def == HediffDefOf.Heatstroke)) remove = true;
                if (debuffs && h.def.defName.StartsWith("AA_") && !h.def.isBad) remove = false;
                if (remove) p.health.RemoveHediff(h);
            }
        }

        public static Hediff ApplyHediff(Pawn p, HediffDef def, float severity, int durationTicks, SpellContext ctx, bool stack = false, float maxSeverity = -1f)
        {
            if (p == null || def == null || p.Dead || p.health == null) return null;
            var h = p.health.hediffSet.GetFirstHediffOfDef(def);
            if (h != null)
            {
                h.Severity = stack ? h.Severity + severity : Mathf.Max(h.Severity, severity);
                if (maxSeverity > 0f) h.Severity = Mathf.Min(h.Severity, maxSeverity);
            }
            else
            {
                h = HediffMaker.MakeHediff(def, p);
                h.Severity = maxSeverity > 0f ? Mathf.Min(severity, maxSeverity) : severity;
                p.health.AddHediff(h);
                if (h.pawn == null || !p.health.hediffSet.hediffs.Contains(h)) return null;
            }
            if (h is HediffWithComps hc)
            {
                var dis = hc.TryGetComp<HediffComp_Disappears>();
                if (dis != null && durationTicks > 0) dis.ticksToDisappear = Mathf.Max(dis.ticksToDisappear, durationTicks);
                else if (dis != null && ctx != null && ctx.DurationFactor != 1f && h.ageTicks < 2)
                    dis.ticksToDisappear = Mathf.RoundToInt(dis.ticksToDisappear * ctx.DurationFactor);
                if (ctx != null)
                    foreach (var comp in hc.comps)
                        if (comp is IArcaneSourced src) src.SetSource(ctx);
                var dot = hc.TryGetComp<HediffComp_ArcaneDot>();
                if (dot != null && ctx?.caster != null)
                {
                    dot.instigator = ctx.caster;
                    dot.power = Mathf.Max(dot.power, ctx.DamageFactor);
                }
            }
            return h;
        }

        public static bool BreakShields(Pawn p)
        {
            bool any = false;
            if (p.apparel != null)
                foreach (var ap in p.apparel.WornApparel.ToList())
                {
                    var sh = ap.TryGetComp<CompShield>();
                    if (sh != null && sh.Energy > 0f)
                    {
                        EmpBreak(sh);
                        any = true;
                    }
                    var ic0 = ap.TryGetComp<CompProjectileInterceptor>();
                    if (ic0 != null && ic0.Active)
                    {
                        BreakInterceptor(ic0);
                        any = true;
                    }
                }
            if (p is ThingWithComps twc)
                foreach (var comp in twc.AllComps.ToList())
                {
                    if (comp is CompShield sh2 && sh2.Energy > 0f)
                    {
                        EmpBreak(sh2);
                        any = true;
                    }
                    else if (comp is CompProjectileInterceptor ic && ic.Active)
                    {
                        BreakInterceptor(ic);
                        any = true;
                    }
                }
            return any;
        }

        private static void EmpBreak(ThingComp comp)
        {
            var dinfo = new DamageInfo(DamageDefOf.EMP, 9999f, 999f);
            comp.PostPreApplyDamage(ref dinfo, out _);
        }

        public static void BreakInterceptor(CompProjectileInterceptor ic)
        {
            EmpBreak(ic);
            ic.currentHitPoints = 0;
        }

        public static Pawn NearestEnemy(Pawn p, float radius)
        {
            if (p?.Map == null) return null;
            Pawn best = null;
            float bd = radius * radius;
            foreach (var o in p.Map.attackTargetsCache.GetPotentialTargetsFor(p))
            {
                if (!(o.Thing is Pawn op) || op.Dead || op.Downed) continue;
                float d = op.Position.DistanceToSquared(p.Position);
                if (d < bd)
                {
                    bd = d;
                    best = op;
                }
            }
            return best;
        }

        public static Pawn RandomEnemy(Pawn p, float radius)
        {
            if (p?.Map == null) return null;
            return p.Map.attackTargetsCache.GetPotentialTargetsFor(p).Select(t => t.Thing).OfType<Pawn>()
                .Where(o => !o.Dead && !o.Downed && o.Position.DistanceTo(p.Position) <= radius).RandomElementWithFallback();
        }

        #region Summons & factions

        public static Faction ThrallFaction()
        {
            var def = DefDatabase<FactionDef>.GetNamedSilentFail("AA_ArcaneThralls");
            if (def == null) return Faction.OfPlayer;
            var f = Find.FactionManager.FirstFactionOfDef(def);
            if (f == null)
            {
                f = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def));
                foreach (var other in Find.FactionManager.AllFactionsListForReading)
                    if (other != f) f.TryMakeInitialRelationsWith(other);
                Find.FactionManager.Add(f);
            }
            RefreshThrallRelations(f);
            return f;
        }

        public static void RefreshThrallRelations(Faction thralls)
        {
            var player = Faction.OfPlayer;
            foreach (var other in Find.FactionManager.AllFactionsListForReading)
            {
                if (other == thralls) continue;
                if (thralls.RelationWith(other, true) == null) thralls.TryMakeInitialRelationsWith(other);
                FactionRelationKind want = other == player ? FactionRelationKind.Ally : other.HostileTo(player) ? FactionRelationKind.Hostile : FactionRelationKind.Neutral;
                if (thralls.RelationKindWith(other) != want) thralls.SetRelationDirect(other, want, false);
            }
        }

        /// <summary>Faction that a caster's summons and dominated pawns should join.</summary>
        public static Faction SummonFactionFor(Pawn caster)
        {
            if (caster?.Faction == null) return null;
            return caster.Faction == Faction.OfPlayer ? ThrallFaction() : caster.Faction;
        }

        /// <summary>Puts the given pawn(s) into a lord that fights alongside the caster.</summary>
        public static void JoinCasterSide(Pawn caster, Map map, Pawn specific = null)
        {
            if (caster == null || map == null) return;
            var faction = SummonFactionFor(caster);
            if (faction == null) return;
            var pawns = specific != null ? new List<Pawn> { specific } :
                map.mapPawns.SpawnedPawnsInFaction(faction).Where(p => p.GetLord() == null && !p.Dead).ToList();
            if (pawns.Count == 0) return;
            Lord lord;
            if (faction != Faction.OfPlayer && caster.Faction == Faction.OfPlayer)
            {
                lord = map.lordManager.lords.FirstOrDefault(l => l.faction == faction && l.LordJob is LordJob_ArcaneThralls lj && lj.master == caster);
                if (lord == null) lord = LordMaker.MakeNewLord(faction, new LordJob_ArcaneThralls(caster), map);
            }
            else
            {
                lord = caster.GetLord();
                if (lord == null) return;
            }
            foreach (var p in pawns)
            {
                if (p.GetLord() == lord) continue;
                p.GetLord()?.Notify_PawnLost(p, PawnLostCondition.ChangedFaction);
                lord.AddPawn(p);
            }
        }

        public static void MakeSummon(Pawn p, SpellContext ctx, int durationTicks, HediffDef def)
        {
            var h = (HediffWithComps)HediffMaker.MakeHediff(def, p);
            p.health.AddHediff(h);
            var c = h.TryGetComp<HediffComp_Summoned>();
            if (c != null)
            {
                c.master = ctx.caster;
                c.ticksLeft = durationTicks;
            }
            h.Severity = Mathf.Clamp(ctx.SummonFactor, 0.1f, 10f);
        }

        public static bool RaiseUndead(Pawn dead, SpellContext ctx, int durationTicks, HediffDef undeadDef)
        {
            if (dead == null || !dead.Dead || dead.Corpse == null || !dead.Corpse.Spawned) return false;
            var corpse = dead.Corpse;
            var map = corpse.Map;
            Vector3 pos = corpse.DrawPos;
            if (!ResurrectionUtility.TryResurrect(dead, new ResurrectionParams { gettingScarsChance = 0f, restoreMissingParts = true, noLord = true, removeDiedThoughts = true }))
                return false;
            var faction = SummonFactionFor(ctx.caster);
            if (faction != null && dead.Faction != faction) dead.SetFaction(faction);
            dead.mindState?.mentalStateHandler?.Reset();
            MakeSummon(dead, ctx, durationTicks, undeadDef);
            ArcaneUtility.HealPawn(dead, 9999f);
            if (dead.Spawned)
            {
                var mgr = ArcaneVfxManager.For(map);
                mgr.Add(new VfxSprite { mat = ArcaneMats.Rune, pos = pos, scaleStart = 0.5f, scaleEnd = 2.4f, duration = 45, color = ctx.Color, layer = AltitudeLayer.MoteLow });
                mgr.Add(new VfxPillar(pos, 40, ctx.Color, ctx.Color2, 1.2f));
                ArcaneFx.Particles(map, pos, ctx.Color, ArcaneDefOf.AA_Fleck_Smoke, 4, 0.7f, 1f);
                ArcaneFx.Text(dead, "AA_Risen".Translate(), ctx.Color);
            }
            JoinCasterSide(ctx.caster, map, dead);
            return true;
        }

        #endregion

        #region Tooltips

        /// <summary>Explains the mage's circle, its level cap and what the next ascension needs.</summary>
        public static string CircleTip(CompArcane c)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("AA_CircleTipHead".Translate(CompArcane.CircleLabel(c.circle), c.LevelCap));
            if (c.circle < CompArcane.MaxCircle)
            {
                var sigil = CompArcane.SigilFor(c.circle + 1);
                sb.Append("\n");
                sb.Append((c.ReadyToAscend ? "AA_CircleTipReady" : "AA_CircleTipNext").Translate(CompArcane.CircleLabel(c.circle + 1), sigil?.label ?? "?", c.LevelCap));
            }
            else sb.Append("\n" + "AA_CircleTipMax".Translate());
            return sb.ToString();
        }

        public static string SpellTooltip(AbilityDef def, Pawn caster)
        {
            var sb = new StringBuilder();
            var ext = def.GetModExtension<SpellExtension>();
            var props = def.comps?.OfType<CompProperties_ArcaneSpell>().FirstOrDefault();
            sb.AppendLine(def.LabelCap.Colorize(props?.color ?? Color.white) + (ext != null && ext.ultimate ? " " + "AA_UltimateTag".Translate().Colorize(new Color(1f, 0.8f, 0.3f)) : ""));
            if (ext != null)
            {
                var tags = new List<string>(ext.tags);
                tags.Add(("AA_Role_" + ext.role).Translate());
                sb.AppendLine(string.Join(" · ", tags).Colorize(ColoredText.SubtleGrayColor));
            }
            sb.AppendLine();
            sb.AppendLine(def.description);
            if (!ext?.flavor.NullOrEmpty() ?? false) sb.AppendLine(("\"" + ext.flavor + "\"").Colorize(ColoredText.SubtleGrayColor));
            sb.AppendLine();
            SpellContext ctx = caster != null ? SpellContext.For(caster, def) : null;
            if (ext != null)
            {
                float cost = ext.manaCost;
                if (caster?.abilities?.GetAbility(def) is ArcaneAbility aa) cost = aa.ManaCost;
                sb.AppendLine("AA_Tip_Mana".Translate(cost.ToString("F0")));
            }
            if (def.cooldownTicksRange.max > 0)
            {
                int cd = def.cooldownTicksRange.max;
                if (caster?.abilities?.GetAbility(def) is ArcaneAbility aa2) cd = aa2.AdjustedCooldown(cd);
                sb.AppendLine("AA_Tip_Cooldown".Translate(cd.ToStringTicksToPeriod()));
            }
            if (def.verbProperties != null)
            {
                if (def.verbProperties.range > 0f) sb.AppendLine("AA_Tip_Range".Translate(def.verbProperties.range.ToString("F0")));
                if (def.verbProperties.warmupTime > 0f) sb.AppendLine("AA_Tip_Cast".Translate(def.verbProperties.warmupTime.ToString("F1")));
            }
            if (props != null)
            {
                sb.AppendLine("AA_Tip_Delivery".Translate(("AA_Delivery_" + props.delivery).Translate()));
                float r = props.radius * (ctx?.RadiusFactor ?? 1f);
                if (r > 0.5f) sb.AppendLine("AA_Tip_Radius".Translate(r.ToString("F1")));
                if (props.delivery == DeliveryType.Chain) sb.AppendLine("AA_Tip_Chains".Translate(props.chainCount + (ctx?.mods.extraTargets ?? 0)));
                if (props.delivery == DeliveryType.Barrage) sb.AppendLine("AA_Tip_Strikes".Translate(props.barrageCount + 2 * (ctx?.mods.extraTargets ?? 0)));
                if ((props.delivery == DeliveryType.Projectile || props.delivery == DeliveryType.Meteor) && props.projectileCount + (ctx?.mods.extraTargets ?? 0) > 1)
                    sb.AppendLine("AA_Tip_Projectiles".Translate(props.projectileCount + (ctx?.mods.extraTargets ?? 0)));
                sb.AppendLine();
                foreach (var e in props.casterEffects)
                    foreach (var line in e.Describe(ctx)) sb.AppendLine("• " + "AA_Tip_Self".Translate() + line);
                foreach (var e in props.effects)
                    foreach (var line in e.Describe(ctx)) sb.AppendLine("• " + line);
            }
            if (ctx?.arcane != null && ctx.arcane.IsMage)
            {
                sb.AppendLine();
                sb.AppendLine("AA_Tip_Power".Translate(ctx.DamageFactor.ToStringPercent()).Colorize(new Color(0.7f, 0.85f, 1f)));
            }
            return sb.ToString().TrimEndNewlines();
        }

        public static string NodeTooltip(ArcaneNodeDef node, CompArcane c)
        {
            var sb = new StringBuilder();
            int rank = c?.Rank(node) ?? 0;
            sb.AppendLine(node.LabelCap.Colorize(node.tree.color) + "  " + ("AA_Kind_" + node.kind).Translate().Colorize(ColoredText.SubtleGrayColor));
            if (node.maxRank > 1) sb.AppendLine("AA_RankOf".Translate(rank, node.maxRank));
            sb.AppendLine();
            if (!node.description.NullOrEmpty()) sb.AppendLine(node.description);
            if (node.unlockSpell != null)
            {
                sb.AppendLine();
                sb.AppendLine(SpellTooltip(node.unlockSpell, c?.Pawn));
            }
            int showRank = Mathf.Max(1, rank);
            var effects = NodeEffectLines(node, showRank).ToList();
            if (effects.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine((rank > 0 ? "AA_CurrentRank" : "AA_FirstRank").Translate().Colorize(new Color(0.8f, 0.8f, 0.5f)));
                foreach (var l in effects) sb.AppendLine("• " + l);
                if (rank > 0 && rank < node.maxRank)
                {
                    sb.AppendLine("AA_NextRank".Translate().Colorize(new Color(0.6f, 0.9f, 0.6f)));
                    foreach (var l in NodeEffectLines(node, rank + 1)) sb.AppendLine("• " + l);
                }
            }
            if (!node.flavor.NullOrEmpty())
            {
                sb.AppendLine();
                sb.AppendLine(("\"" + node.flavor + "\"").Colorize(ColoredText.SubtleGrayColor));
            }
            sb.AppendLine();
            var req = new List<string>();
            if (node.requiredLevel > 1) req.Add("AA_ReqLevel".Translate(node.requiredLevel));
            if (node.requiredAttribute != null) req.Add("AA_ReqAttr".Translate(node.requiredAttribute.label, node.requiredAttributeValue));
            if (!node.exclusiveGroup.NullOrEmpty()) req.Add("AA_ReqExclusive".Translate());
            if (node.costPerRank > 1) req.Add("AA_ReqCost".Translate(node.costPerRank));
            if (req.Count > 0) sb.AppendLine(string.Join("  ·  ", req).Colorize(ColoredText.SubtleGrayColor));
            if (c != null)
            {
                var can = c.CanRankUp(node);
                if (can.Accepted) sb.AppendLine("AA_ClickToLearn".Translate().Colorize(new Color(0.5f, 1f, 0.5f)));
                else if (rank < node.maxRank && !can.Reason.NullOrEmpty()) sb.AppendLine(can.Reason.Colorize(new Color(1f, 0.5f, 0.4f)));
            }
            return sb.ToString().TrimEndNewlines();
        }

        public static IEnumerable<string> NodeEffectLines(ArcaneNodeDef node, int rank)
        {
            foreach (var sm in node.statOffsetsPerRank)
                yield return sm.stat.LabelCap + ": " + sm.stat.Worker.ValueToString(sm.value * rank, false, ToStringNumberSense.Offset);
            foreach (var sm in node.statPercentPerRank)
                yield return sm.stat.LabelCap + ": " + (sm.value * rank >= 0 ? "+" : "") + (sm.value * rank).ToStringPercent();
            foreach (var co in node.capacityOffsetsPerRank)
                yield return co.capacity.LabelCap + ": " + (co.offset * rank >= 0 ? "+" : "") + (co.offset * rank).ToStringPercent();
            foreach (var m in node.spellMods) yield return m.Describe(rank);
            foreach (var p in node.passives) yield return p.Describe(rank);
        }

        #endregion

        #region Auto allocation

        public static void AutoAllocate(CompArcane c, bool attributes = true, bool perks = true)
        {
            if (!c.IsMage) return;
            if (c.CanChooseSubclass && c.Pawn.Faction != Faction.OfPlayer) c.ChooseSubclass(c.classDef.subclasses.RandomElement());
            if (attributes)
            {
                var attrs = DefDatabase<ArcaneAttributeDef>.AllDefsListForReading;
                int guard = 0;
                while (c.attributePoints > 0 && guard++ < 500)
                {
                    var pick = attrs.Where(a => c.CanRaiseAttribute(a)).RandomElementByWeightWithFallback(a =>
                        a == c.classDef.primaryAttribute ? 4f : a == c.classDef.secondaryAttribute ? 2.5f : 1f);
                    if (pick == null) break;
                    c.RaiseAttribute(pick);
                }
            }
            if (perks)
            {
                int guard = 0;
                while (c.perkPoints > 0 && guard++ < 500)
                {
                    var trees = new List<ArcaneClassDef> { c.classDef };
                    if (c.subclassDef != null) trees.Add(c.subclassDef);
                    var options = trees.SelectMany(t => t.nodes).Where(n => c.CanRankUp(n).Accepted).ToList();
                    if (options.Count == 0) break;
                    var pick = options.RandomElementByWeight(n =>
                        n.kind == NodeKind.Spell ? 5f : n.kind == NodeKind.Ultimate ? 8f : n.kind == NodeKind.Keystone ? 4f : n.kind == NodeKind.Notable ? 3f : 1.5f);
                    c.RankUp(pick);
                }
            }
        }

        #endregion
    }
}

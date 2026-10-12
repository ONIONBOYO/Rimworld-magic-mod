using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ArcanumAscendant
{
    /// <summary>Creates a persistent area (aura, storm, consecration, black hole...).</summary>
    public class Effect_Field : SpellEffect
    {
        public float fieldRadius = 4f;
        public int durationTicks = 600;
        public int pulseInterval = 60;
        public bool followCaster;
        public List<SpellEffect> pulseEffects = new List<SpellEffect>();
        public List<VfxSpec> pulseVfx = new List<VfxSpec>();
        public string groundTex = "ArcanumAscendant/Vfx/RuneCircle";
        public string groundTex2;
        public float groundSpin = 0.5f;
        public float groundAlpha = 0.55f;
        public FleckDef ambientFleck;
        public float ambientDensity = 1f;
        public float ambientSpeed = 0.5f;
        public float ambientScale = 0.6f;
        public float ambientAngle = -1f;
        public bool useColor2;
        public float pullStrength;
        public TargetFilter pullAffects = TargetFilter.Enemies;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var props = ctx.props;
            int listIndex = 0;
            int idx = props.effects.IndexOf(this);
            if (idx < 0)
            {
                idx = props.casterEffects.IndexOf(this);
                listIndex = 1;
            }
            var f = new ArcaneField
            {
                ability = ctx.def,
                listIndex = listIndex,
                effectIndex = idx,
                caster = ctx.caster,
                cell = followCaster ? ctx.caster.Position : info.cell,
                followCaster = followCaster,
                radius = Mathf.Min(fieldRadius * ctx.RadiusFactor, GenRadial.MaxRadialPatternRadius - 1f),
                ticksLeft = Mathf.RoundToInt(durationTicks * ctx.DurationFactor),
                power = ctx.basePower * Mathf.Max(0.1f, 1f + ctx.mods.damage),
                healPower = ctx.healStat * Mathf.Max(0.1f, 1f + ctx.mods.healing),
                critChance = ctx.critChance,
                effect = this,
                ctx = ctx,
            };
            f.totalTicks = f.ticksLeft;
            ArcaneVfxManager.For(ctx.map).AddField(f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Field".Translate((fieldRadius * (ctx?.RadiusFactor ?? 1f)).ToString("F1"),
                (durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod(), (pulseInterval / 60f).ToString("F1"));
            foreach (var e in pulseEffects)
                foreach (var s in e.Describe(ctx)) yield return "  • " + s;
            if (pullStrength > 0f) yield return "  • " + "AA_Desc_Vortex".Translate();
        }
    }

    public class Effect_Summon : SpellEffect
    {
        public PawnKindDef kind;
        public List<PawnKindDef> kindOptions;
        public int count = 1;
        public int durationTicks = 3600;
        public HediffDef extraHediff;
        public bool scaleCountWithPower;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int n = count + ctx.mods.extraTargets;
            if (scaleCountWithPower) n = Mathf.RoundToInt(n * Mathf.Max(1f, ctx.SummonFactor));
            var faction = ArcaneUtility.SummonFactionFor(ctx.caster);
            for (int i = 0; i < n; i++)
            {
                var k = kindOptions.NullOrEmpty() ? kind : kindOptions.RandomElement();
                if (k == null) continue;
                IntVec3 cell = CellFinder.StandableCellNear(info.cell, map, 3f);
                if (!cell.IsValid) cell = info.cell;
                var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(k, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false, allowFood: false));
                GenSpawn.Spawn(pawn, cell, map);
                ArcaneUtility.MakeSummon(pawn, ctx, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), ArcaneDefOf.AA_Summoned);
                ArcaneUtility.JoinCasterSide(ctx.caster, map, pawn);
                if (extraHediff != null) pawn.health.AddHediff(extraHediff);
                ArcaneFx.Flash(map, pawn.DrawPos, ctx.Color, 2.5f);
                ArcaneFx.Particles(map, pawn.DrawPos, ctx.Color2, ArcaneDefOf.AA_Fleck_Rune, 3, 0.8f, 0.6f);
                ArcaneVfxManager.For(map).Add(new VfxSprite { mat = ArcaneMats.Rune, pos = pawn.DrawPos, scaleStart = 0.5f, scaleEnd = 2.5f, duration = 40, color = ctx.Color, layer = AltitudeLayer.MoteLow });
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            var k = kind ?? kindOptions?.FirstOrDefault();
            yield return "AA_Desc_Summon".Translate(count, k?.label ?? "?", (durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    public class Effect_RaiseDead : SpellEffect
    {
        public int maxCount = 3;
        public int durationTicks = 30000;
        public bool allowHumanlike = true;
        public bool allowAnimals = true;
        public HediffDef undeadHediff;

        public Effect_RaiseDead()
        {
            affects = TargetFilter.Corpses;
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse corpse, ImpactInfo info)
        {
            int max = maxCount + ctx.mods.extraTargets * 2;
            if (ctx.hitThisCast.Count(t => t is Pawn p && p.health.hediffSet.HasHediff(undeadHediff ?? ArcaneDefOf.AA_Undead)) >= max) return;
            var p = corpse.InnerPawn;
            if (p == null || p.RaceProps.IsMechanoid || corpse.GetRotStage() == RotStage.Dessicated) return;
            if (p.RaceProps.Humanlike && !allowHumanlike) return;
            if (p.RaceProps.Animal && !allowAnimals) return;
            if (p.kindDef?.isBoss ?? false) return;
            if (ctx.caster != null && p.Faction == ctx.caster.Faction && p.RaceProps.Humanlike) return;
            if (!ArcaneUtility.RaiseUndead(p, ctx, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), undeadHediff ?? ArcaneDefOf.AA_Undead)) return;
            ctx.hitThisCast.Add(p);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_RaiseDead".Translate(maxCount, (durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    public class Effect_SpawnThing : SpellEffect
    {
        public ThingDef thing;
        public int count = 1;
        public bool inArea;
        public float cellChance = 1f;
        public bool ring;
        public int lifetimeTicks = -1;
        public bool onlyStandable = true;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (thing == null) return;
            var map = ctx.map;
            var cells = new List<IntVec3>();
            if (ring && info.radius > 0.5f)
                cells.AddRange(GenRadial.RadialCellsAround(info.cell, info.radius, true).Where(c => c.InBounds(map) && c.DistanceTo(info.cell) > info.radius - 1.1f));
            else if (inArea)
                cells.AddRange(info.Cells(map).Where(c => Rand.Chance(cellChance)));
            else
                for (int i = 0; i < count + (thing.category == ThingCategory.Building ? ctx.mods.extraTargets : 0); i++)
                {
                    var c = i == 0 ? info.cell : CellFinder.StandableCellNear(info.cell, map, 2.5f);
                    if (c.IsValid) cells.Add(c);
                }
            foreach (var c in cells)
            {
                if (!c.InBounds(map)) continue;
                if (thing.category == ThingCategory.Building)
                {
                    if (onlyStandable && (!c.Standable(map) || c.GetFirstPawn(map) != null || c.GetEdifice(map) != null)) continue;
                    var t = ThingMaker.MakeThing(thing, thing.MadeFromStuff ? GenStuff.DefaultStuffFor(thing) : null);
                    if (t.def.CanHaveFaction && ctx.caster?.Faction != null) t.SetFaction(ctx.caster.Faction);
                    GenSpawn.Spawn(t, c, map, WipeMode.VanishOrMoveAside);
                    var con = t.TryGetComp<CompArcaneConstruct>();
                    if (con != null) con.Setup(ctx, lifetimeTicks > 0 ? Mathf.RoundToInt(lifetimeTicks * ctx.DurationFactor) : -1);
                    ArcaneFx.Particles(map, c.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Spark, 3, 1.5f, 0.5f);
                }
                else
                {
                    var t = ThingMaker.MakeThing(thing, thing.MadeFromStuff ? GenStuff.DefaultStuffFor(thing) : null);
                    t.stackCount = Mathf.Min(Mathf.Max(1, count), thing.stackLimit);
                    GenPlace.TryPlaceThing(t, c, map, ThingPlaceMode.Near);
                    var con = t.TryGetComp<CompArcaneConstruct>();
                    if (con != null) con.Setup(ctx, lifetimeTicks > 0 ? Mathf.RoundToInt(lifetimeTicks * ctx.DurationFactor) : -1);
                    if (inArea) continue;
                    break;
                }
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (thing == null) yield break;
            string s = "AA_Desc_Spawn".Translate(thing.LabelCap);
            if (lifetimeTicks > 0) s += " " + "AA_Desc_For".Translate((lifetimeTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
            yield return s;
        }
    }

    public class Effect_SpawnItems : SpellEffect
    {
        public ThingDef thing;
        public IntRange count = new IntRange(1, 1);
        public bool scaleWithPower = true;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (thing == null) return;
            int total = count.RandomInRange;
            if (scaleWithPower) total = Mathf.RoundToInt(total * Mathf.Sqrt(Mathf.Max(0.5f, ctx.DamageFactor)));
            while (total > 0)
            {
                var t = ThingMaker.MakeThing(thing, thing.MadeFromStuff ? GenStuff.DefaultStuffFor(thing) : null);
                int n = Mathf.Min(total, thing.stackLimit);
                t.stackCount = n;
                total -= n;
                GenPlace.TryPlaceThing(t, info.cell, ctx.map, ThingPlaceMode.Near);
            }
            ArcaneFx.Particles(ctx.map, info.pos, ctx.Color2, ArcaneDefOf.AA_Fleck_Spark, 12, 2f, 0.5f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (thing != null) yield return "AA_Desc_Conjure".Translate(count.ToString(), thing.label);
        }
    }

    public enum TeleportMode
    {
        CasterToTarget,
        TargetToCaster,
        Swap,
        Banish,
        AlliesToCaster,
    }

    public class Effect_Teleport : SpellEffect
    {
        public TeleportMode mode = TeleportMode.CasterToTarget;
        public float banishDistance = 15f;
        public List<SpellEffect> pathEffects;

        public override bool AppliesToPawns => mode != TeleportMode.CasterToTarget && mode != TeleportMode.Swap;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var caster = ctx.caster;
            var map = ctx.map;
            if (caster == null || !caster.Spawned) return;
            if (mode == TeleportMode.CasterToTarget)
            {
                IntVec3 dest = info.cell;
                Vector3 from = caster.DrawPos;
                if (info.primary is Pawn victim && victim != caster && victim.Spawned)
                {
                    // land just behind the target, relative to where the caster came from
                    IntVec3 best = IntVec3.Invalid;
                    float bestDist = -1f;
                    foreach (var adj in GenAdj.AdjacentCells)
                    {
                        var c = victim.Position + adj;
                        if (!c.InBounds(map) || !c.Standable(map) || c.GetFirstPawn(map) != null) continue;
                        float d = (c.ToVector3Shifted() - from).MagnitudeHorizontalSquared();
                        if (d > bestDist)
                        {
                            bestDist = d;
                            best = c;
                        }
                    }
                    if (best.IsValid) dest = best;
                }
                if (!dest.Standable(map)) dest = CellFinder.StandableCellNear(info.cell, map, 3f);
                if (!dest.IsValid) return;
                Blink(caster, dest, ctx);
                if (!pathEffects.NullOrEmpty())
                {
                    var cells = GenSight.BresenhamCellsBetween(from.ToIntVec3(), dest).Where(c => c.InBounds(map)).ToList();
                    var pinfo = new ImpactInfo { cell = dest, pos = dest.ToVector3Shifted(), radius = 0.001f, areaCells = cells, sourcePos = from };
                    SpellRunner.Impact(ctx, pinfo, pathEffects, playVfx: false);
                    ArcaneVfxManager.For(map).Add(new VfxBeam(from, dest.ToVector3Shifted(), 14, ArcaneMats.Beam, ctx.Color, ctx.Color2, 0.6f));
                }
            }
            else if (mode == TeleportMode.Swap && info.primary is Pawn other && other.Spawned && other != caster)
            {
                IntVec3 a = caster.Position, b = other.Position;
                Blink(caster, b, ctx);
                Blink(other, a, ctx);
            }
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var caster = ctx.caster;
            var map = ctx.map;
            if (p == caster || !p.Spawned) return;
            switch (mode)
            {
                case TeleportMode.TargetToCaster:
                case TeleportMode.AlliesToCaster:
                {
                    var c = CellFinder.StandableCellNear(caster.Position, map, 3f, x => x != caster.Position && x.GetFirstPawn(map) == null);
                    if (c.IsValid) Blink(p, c, ctx);
                    break;
                }
                case TeleportMode.Banish:
                {
                    if (p.kindDef?.isBoss ?? false) return;
                    for (int i = 0; i < 20; i++)
                    {
                        var dir = Quaternion.AngleAxis(Rand.Range(0f, 360f), Vector3.up) * Vector3.forward;
                        var c = (p.DrawPos + dir * banishDistance).ToIntVec3();
                        if (c.InBounds(map) && c.Standable(map) && !c.Fogged(map))
                        {
                            Blink(p, c, ctx);
                            p.stances?.stunner?.StunFor(90, caster, false, false);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        public static void Blink(Pawn p, IntVec3 dest, SpellContext ctx)
        {
            var map = p.Map;
            Vector3 from = p.DrawPos;
            var mgr = ArcaneVfxManager.For(map);
            mgr.Add(new VfxSprite { mat = ArcaneMats.Ring, pos = from, scaleStart = 2.2f, scaleEnd = 0.2f, duration = 18, color = ctx?.Color ?? Color.white });
            ArcaneFx.Particles(map, from, ctx?.Color ?? Color.white, ArcaneDefOf.AA_Fleck_Spark, 8, 2f, 0.5f);
            p.Position = dest;
            p.Notify_Teleported(true, true);
            Vector3 to = p.DrawPos;
            mgr.Add(new VfxSprite { mat = ArcaneMats.Ring, pos = to, scaleStart = 0.2f, scaleEnd = 2.4f, duration = 18, color = ctx?.Color2 ?? Color.white });
            ArcaneFx.Flash(map, to, ctx?.Color ?? Color.white, 2f);
            ArcaneFx.Particles(map, to, ctx?.Color2 ?? Color.white, ArcaneDefOf.AA_Fleck_Spark, 8, 2f, 0.5f);
            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(dest, map));
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return ("AA_Desc_Teleport_" + mode).Translate();
        }
    }

    public class Effect_Resurrect : SpellEffect
    {
        public bool sideEffects;
        public bool onlyAllies = true;

        public Effect_Resurrect()
        {
            affects = TargetFilter.Corpses;
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse corpse, ImpactInfo info)
        {
            var p = corpse.InnerPawn;
            if (p == null) return;
            if (onlyAllies && ctx.caster != null && p.Faction != ctx.caster.Faction && !(p.Faction == null && p.RaceProps.Animal)) return;
            bool ok = sideEffects ? ResurrectionUtility.TryResurrectWithSideEffects(p) : ResurrectionUtility.TryResurrect(p);
            if (ok && p.Spawned)
            {
                ArcaneVfxManager.For(p.Map).Add(new VfxPillar(p.DrawPos, 60, ctx.Color, ctx.Color2, 2f));
                ArcaneFx.Text(p, "AA_Resurrected".Translate(), ctx.Color);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Resurrect".Translate();
        }
    }

    public class Effect_Dominate : SpellEffect
    {
        public int durationTicks = 2500;
        public float maxBodySize = 99f;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p.kindDef?.isBoss ?? false) return;
            if (p.BodySize > maxBodySize || p.Downed || p == ctx.caster) return;
            if (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony || p.IsSlaveOfColony) return;
            if (p.health.hediffSet.HasHediff(ArcaneDefOf.AA_Dominated)) return;
            var originalFaction = p.Faction;
            var newFaction = ArcaneUtility.SummonFactionFor(ctx.caster);
            if (newFaction == null || newFaction == originalFaction) return;
            if (p.InMentalState) p.MentalState.RecoverFromState();
            p.SetFaction(newFaction);
            var h = (HediffWithComps)HediffMaker.MakeHediff(ArcaneDefOf.AA_Dominated, p);
            p.health.AddHediff(h);
            var comp = h.TryGetComp<HediffComp_Dominated>();
            if (comp != null)
            {
                comp.originalFaction = originalFaction;
                comp.ticksLeft = Mathf.RoundToInt(durationTicks * ctx.DurationFactor);
            }
            ArcaneUtility.JoinCasterSide(ctx.caster, p.Map, p);
            p.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            ArcaneFx.Text(p, "AA_Dominated".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Dominate".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    public class Effect_Polymorph : SpellEffect
    {
        public PawnKindDef into;
        public List<PawnKindDef> options;
        public int durationTicks = 1500;
        public float maxBodySize = 4f;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p.kindDef?.isBoss ?? false) return;
            if (p.BodySize > maxBodySize || p == ctx.caster || !p.Spawned) return;
            if (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony || p.IsSlaveOfColony || p.carryTracker?.CarriedThing is Pawn) return;
            if (ArcaneVfxManager.For(p.Map).polymorphs.IsCritter(p)) return;
            var k = options.NullOrEmpty() ? into : options.RandomElement();
            if (k == null || p.kindDef == k) return;
            ArcaneVfxManager.For(p.Map).polymorphs.Polymorph(p, k, Mathf.RoundToInt(durationTicks * ctx.DurationFactor), ctx);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            var k = into ?? options?.FirstOrDefault();
            yield return "AA_Desc_Polymorph".Translate(k?.label ?? "?", (durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    public class Effect_GameCondition : SpellEffect
    {
        public GameConditionDef condition;
        public int durationTicks = 60000;
        public List<GameConditionDef> removeConditions;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var mgr = ctx.map.gameConditionManager;
            if (!removeConditions.NullOrEmpty())
                foreach (var c in mgr.ActiveConditions.ToList())
                    if (removeConditions.Contains(c.def)) c.End();
            if (condition != null)
            {
                var existing = mgr.ActiveConditions.FirstOrDefault(c => c.def == condition);
                int dur = Mathf.RoundToInt(durationTicks * ctx.DurationFactor);
                if (existing != null) existing.Duration = Mathf.Max(existing.Duration, existing.TicksPassed + dur);
                else mgr.RegisterCondition(GameConditionMaker.MakeCondition(condition, dur));
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (condition != null) yield return "AA_Desc_Condition".Translate(condition.label, (durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
            if (!removeConditions.NullOrEmpty()) yield return "AA_Desc_EndConditions".Translate(string.Join(", ", removeConditions.Select(c => c.label)));
        }
    }

    public class Effect_Weather : SpellEffect
    {
        public WeatherDef weather;
        public List<WeatherDef> options;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var w = options.NullOrEmpty() ? weather : options.RandomElement();
            if (w != null) ctx.map.weatherManager.TransitionTo(w);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            var w = weather ?? options?.FirstOrDefault();
            if (w != null) yield return "AA_Desc_Weather".Translate(w.label);
        }
    }

    public class Effect_LightningStrike : SpellEffect
    {
        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            ctx.map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(ctx.map, info.cell));
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Lightning".Translate();
        }
    }

    public class Effect_Need : SpellEffect
    {
        public NeedDef need;
        public float amount = 0.3f;

        public Effect_Need()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var n = need != null ? p.needs?.TryGetNeed(need) : null;
            if (n != null) n.CurLevel = Mathf.Clamp01(n.CurLevel + amount);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (need != null) yield return "AA_Desc_Need".Translate(need.label, amount.ToStringPercent());
        }
    }

    public class Effect_Thought : SpellEffect
    {
        public ThoughtDef thought;

        public Effect_Thought()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (thought == null || p.needs?.mood?.thoughts?.memories == null) return;
            p.needs.mood.thoughts.memories.TryGainMemory(thought);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (thought != null) yield return "AA_Desc_Thought".Translate(thought.stages?.FirstOrDefault()?.label ?? thought.label);
        }
    }

    public class Effect_Inspiration : SpellEffect
    {
        public InspirationDef inspiration;

        public Effect_Inspiration()
        {
            affects = TargetFilter.AlliesNotSelf;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var h = p.mindState?.inspirationHandler;
            if (h == null || h.Inspired || !p.RaceProps.Humanlike) return;
            var def = inspiration ?? DefDatabase<InspirationDef>.AllDefsListForReading.Where(d => d.Worker.InspirationCanOccur(p)).RandomElementWithFallback();
            if (def != null) h.TryStartInspiration(def, "AA_SpellReason".Translate(ctx.def.label));
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Inspire".Translate(inspiration?.label ?? "AA_RandomInspiration".Translate().ToString());
        }
    }

    public class Effect_SkillXp : SpellEffect
    {
        public SkillDef skill;
        public float xp = 1000f;

        public Effect_SkillXp()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p.skills == null) return;
            var s = skill != null ? p.skills.GetSkill(skill) : p.skills.skills.Where(x => !x.TotallyDisabled).RandomElementWithFallback();
            s?.Learn(xp * Mathf.Sqrt(ctx.HealFactor), true);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_SkillXp".Translate(xp.ToString("F0"), skill?.label ?? "AA_RandomSkill".Translate().ToString());
        }
    }

    public class Effect_PlantGrowth : SpellEffect
    {
        public float growth = 0.25f;
        public bool includeTrees = true;
        public bool sowWild;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            foreach (var c in info.Cells(map))
            {
                var plant = c.GetPlant(map);
                if (plant != null)
                {
                    if (!includeTrees && plant.def.plant.IsTree) continue;
                    plant.Growth = Mathf.Min(1f, plant.Growth + growth * Mathf.Sqrt(ctx.HealFactor));
                    plant.DirtyMapMesh(map);
                    if (Rand.Chance(0.15f)) ArcaneFx.Particles(map, c.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Leaf, 1, 0.6f, 0.5f);
                }
                else if (sowWild && Rand.Chance(0.25f) && c.GetEdifice(map) == null && c.GetFirstItem(map) == null && c.GetFertility(map) > 0.5f)
                {
                    var def = map.Biome.AllWildPlants.Where(pd => !pd.plant.IsTree && pd.CanEverPlantAt(c, map)).RandomElementWithFallback();
                    if (def != null)
                    {
                        var p = (Plant)ThingMaker.MakeThing(def);
                        p.Growth = 0.5f;
                        GenSpawn.Spawn(p, c, map);
                    }
                }
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Growth".Translate(growth.ToStringPercent());
        }
    }

    public class Effect_Repair : SpellEffect
    {
        public float amount = 50f;
        public bool items = true;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var done = new HashSet<Thing>();
            float amt = amount * ctx.HealFactor;
            foreach (var c in info.Cells(map))
                foreach (var t in c.GetThingList(map))
                {
                    if (!t.def.useHitPoints || t.HitPoints >= t.MaxHitPoints || !done.Add(t)) continue;
                    if (t is Pawn) continue;
                    if (t.def.category == ThingCategory.Item && !items) continue;
                    if (t.Faction != null && ctx.caster?.Faction != null && t.Faction.HostileTo(ctx.caster.Faction)) continue;
                    t.HitPoints = Mathf.Min(t.MaxHitPoints, t.HitPoints + Mathf.RoundToInt(amt));
                    if (Rand.Chance(0.3f)) ArcaneFx.Particles(map, t.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Spark, 2, 1f, 0.4f);
                    map.mapDrawer.MapMeshDirty(t.Position, MapMeshFlagDefOf.Things);
                }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Repair".Translate((amount * (ctx?.HealFactor ?? 1f)).ToString("F0"));
        }
    }

    public class Effect_Transmute : SpellEffect
    {
        public List<ThingDef> from;
        public bool anyStoneChunk = true;
        public ThingDef to;
        public IntRange countPerUnit = new IntRange(10, 20);

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (to == null) return;
            var map = ctx.map;
            foreach (var c in info.Cells(map))
                foreach (var t in c.GetThingList(map).ToList())
                {
                    bool match = (from != null && from.Contains(t.def)) || (anyStoneChunk && t.def.thingCategories != null && t.def.thingCategories.Contains(ThingCategoryDefOf.StoneChunks));
                    if (!match) continue;
                    int units = t.stackCount;
                    IntVec3 pos = t.Position;
                    t.Destroy();
                    int n = countPerUnit.RandomInRange * units;
                    while (n > 0)
                    {
                        var made = ThingMaker.MakeThing(to);
                        made.stackCount = Mathf.Min(n, to.stackLimit);
                        n -= made.stackCount;
                        GenPlace.TryPlaceThing(made, pos, map, ThingPlaceMode.Near);
                    }
                    ArcaneFx.Flash(map, pos.ToVector3Shifted(), ctx.Color, 1.5f);
                    ArcaneFx.Particles(map, pos.ToVector3Shifted(), ctx.Color2, ArcaneDefOf.AA_Fleck_Spark, 6, 1.6f, 0.5f);
                }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (to != null) yield return "AA_Desc_Transmute".Translate(to.label, countPerUnit.ToString());
        }
    }

    public class Effect_Temperature : SpellEffect
    {
        public float heat = 100f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            GenTemperature.PushHeat(info.cell, ctx.map, heat * ctx.basePower);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return (heat > 0 ? "AA_Desc_Heat" : "AA_Desc_Cool").Translate();
        }
    }

    public class Effect_Mine : SpellEffect
    {
        public bool yieldResources = true;
        public bool includeBuildings;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            foreach (var c in info.Cells(map))
            {
                var e = c.GetEdifice(map);
                if (e == null || e.Destroyed) continue;
                if (e is Mineable m && m.def.mineable)
                {
                    if (yieldResources && ctx.caster != null) m.DestroyMined(ctx.caster);
                    else m.Destroy(DestroyMode.KillFinalize);
                    FleckMaker.ThrowDustPuffThick(c.ToVector3Shifted(), map, 1.5f, new Color(0.55f, 0.5f, 0.45f));
                }
                else if (includeBuildings && e.Faction != ctx.caster?.Faction)
                {
                    e.TakeDamage(new DamageInfo(DamageDefOf.Bomb, 200f * ctx.DamageFactor, 1f, -1f, ctx.caster));
                }
            }
            map.roofGrid.SetRoof(info.cell, null);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Mine".Translate();
        }
    }

    public class Effect_Terraform : SpellEffect
    {
        public TerrainDef to;
        public List<TerrainDef> fromOnly;
        public bool onlyNatural = true;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (to == null) return;
            var map = ctx.map;
            foreach (var c in info.Cells(map))
            {
                var cur = c.GetTerrain(map);
                if (cur == to) continue;
                if (!fromOnly.NullOrEmpty() && !fromOnly.Contains(cur)) continue;
                if (onlyNatural && fromOnly.NullOrEmpty() && (cur.layerable || cur.BuildableByPlayer)) continue;
                if (c.GetEdifice(map) != null && !to.passability.Equals(Traversability.Standable)) continue;
                map.terrainGrid.SetTerrain(c, to);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (to != null) yield return "AA_Desc_Terraform".Translate(to.label);
        }
    }

    public class Effect_Recruit : SpellEffect
    {
        public bool animalsOnly;
        public float maxWildness = 1f;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var caster = ctx.caster;
            if (caster?.Faction != Faction.OfPlayer || p.Faction == Faction.OfPlayer) return;
            if (p.kindDef?.isBoss ?? false) return;
            if (p.RaceProps.Animal)
            {
                if (p.GetStatValue(StatDefOf.Wildness) > maxWildness) return;
            }
            else
            {
                if (animalsOnly || !p.RaceProps.Humanlike) return;
                if (!(p.Downed || p.IsPrisonerOfColony || (p.Faction != null && !p.Faction.HostileTo(Faction.OfPlayer) && p.guest != null && p.guest.Recruitable))) return;
            }
            InteractionWorker_RecruitAttempt.DoRecruit(caster, p, true);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return (animalsOnly ? "AA_Desc_Tame" : "AA_Desc_Recruit").Translate();
        }
    }

    public class Effect_Reveal : SpellEffect
    {
        public float revealRadius = 20f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            float r = Mathf.Min(revealRadius * ctx.RadiusFactor, GenRadial.MaxRadialPatternRadius - 1f);
            foreach (var c in GenRadial.RadialCellsAround(info.cell, r, true))
                if (c.InBounds(map) && c.Fogged(map)) map.fogGrid.Unfog(c);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Reveal".Translate(revealRadius.ToString("F0"));
        }
    }

    public class Effect_Age : SpellEffect
    {
        public float years = -5f;
        public float minAge = 18f;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var at = p.ageTracker;
            if (at == null) return;
            long ticks = (long)(years * GenDate.TicksPerYear);
            long min = (long)(Mathf.Min(minAge, at.AgeBiologicalYearsFloat) * GenDate.TicksPerYear);
            at.AgeBiologicalTicks = System.Math.Max(min, at.AgeBiologicalTicks + ticks);
            ArcaneFx.Text(p, (years < 0 ? "AA_Younger" : "AA_Older").Translate(Mathf.Abs(years).ToString("F0")), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return (years < 0 ? "AA_Desc_Younger" : "AA_Desc_Older").Translate(Mathf.Abs(years).ToString("F0"));
        }
    }

    public class Effect_Rewind : SpellEffect
    {
        public int windowTicks = 2500;

        public Effect_Rewind()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            int window = Mathf.RoundToInt(windowTicks * ctx.DurationFactor);
            int removed = 0;
            foreach (var h in p.health.hediffSet.hediffs.ToList())
            {
                if (h.ageTicks > window) continue;
                if (h is Hediff_Injury || h is Hediff_MissingPart && !(h.Part?.def?.tags?.NullOrEmpty() ?? true) || h.def == HediffDefOf.BloodLoss)
                {
                    if (h is Hediff_MissingPart mp) p.health.RestorePart(mp.Part);
                    else p.health.RemoveHediff(h);
                    removed++;
                }
            }
            if (removed > 0) ArcaneFx.Text(p, "AA_Rewound".Translate(removed), ctx.Color);
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse c, ImpactInfo info)
        {
            var p = c.InnerPawn;
            if (p == null || p.Faction != ctx.caster?.Faction) return;
            if (Find.TickManager.TicksGame - c.timeOfDeath > windowTicks * ctx.DurationFactor) return;
            if (ResurrectionUtility.TryResurrect(p)) ArcaneFx.Text(p, "AA_Resurrected".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Rewind".Translate((windowTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    public class Effect_CooldownReset : SpellEffect
    {
        public float fraction = 1f;
        public bool includeSelf;

        public Effect_CooldownReset()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (p.abilities == null) return;
            foreach (var ab in p.abilities.abilities)
            {
                if (ab.def == ctx.def) continue;
                if (p == ctx.caster && !includeSelf) continue;
                if (!ab.OnCooldown) continue;
                if (fraction >= 0.999f) ab.ResetCooldown();
                else ab.StartCooldown(Mathf.RoundToInt(ab.CooldownTicksRemaining * (1f - fraction)));
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_CdReset".Translate(fraction.ToStringPercent());
        }
    }

    public class Effect_Random : SpellEffect
    {
        public List<SpellEffect> options = new List<SpellEffect>();
        public bool perTarget = true;

        public override bool AppliesToPawns => perTarget;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (perTarget || options.Count == 0) return;
            var e = options.RandomElement();
            SpellRunner.Impact(ctx, info, new List<SpellEffect> { e }, playVfx: false);
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (options.Count == 0) return;
            var e = options.RandomElement();
            e.ApplyAtCell(ctx, info);
            if (e.AppliesToPawns && ctx.Passes(p, e.affects == TargetFilter.Inherit ? TargetFilter.All : e.affects)) e.ApplyToPawn(ctx, p, info);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Random".Translate();
            foreach (var o in options)
                foreach (var s in o.Describe(ctx)) yield return "  ? " + s;
        }
    }
}

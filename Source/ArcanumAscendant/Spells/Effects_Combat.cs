using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArcanumAscendant
{
    /// <summary>Base class for all spell effects. Effects are composed in XML.</summary>
    public abstract class SpellEffect
    {
        public TargetFilter affects = TargetFilter.Inherit;
        public float chance = 1f;
        public float radius = -1f;
        public List<VfxSpec> vfxOnTarget;

        public virtual bool AppliesToPawns => true;
        public virtual void ApplyAtCell(SpellContext ctx, ImpactInfo info) { }
        public virtual void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info) { }
        public virtual void ApplyToCorpse(SpellContext ctx, Corpse c, ImpactInfo info) { }
        public virtual IEnumerable<string> Describe(SpellContext ctx) { yield break; }

        protected static string Chance(float c) => c < 0.999f ? " (" + c.ToStringPercent() + ")" : "";
    }

    public class Effect_Damage : SpellEffect
    {
        public DamageDef damageDef;
        public float amount = 10f;
        public float armorPen = -1f;
        public int hits = 1;
        public float lethalPercent;
        public bool trueDamage;
        public float executeBelow;
        public float vsMechanoids = 1f;
        public float vsHumanlikes = 1f;
        public float vsAnimals = 1f;
        public float vsEntities = 1f;
        public bool damageBuildings;
        public float buildingFactor = 1f;
        public bool falloff;
        public int stunTicks;
        public bool noCrit;
        public float healCasterFraction;
        public HediffDef bonusVsHediff;
        public float bonusVsHediffFactor = 2f;
        public bool consumeHediff;

        public DamageDef Def => trueDamage ? ArcaneDefOf.AA_ArcaneTrue : damageDef ?? ArcaneDefOf.AA_Arcane;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            float mult = ctx.DamageFactor * info.scale * TypeFactor(p);
            if (ctx.arcane != null)
                foreach (var (passive, rank, _) in ctx.arcane.Passives) mult *= passive.TargetDamageFactor(ctx.arcane, rank, p, ctx);
            if (falloff && info.radius > 0.5f)
            {
                float d = (p.DrawPos - info.pos).MagnitudeHorizontal();
                mult *= Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(d / info.radius));
            }
            if (bonusVsHediff != null)
            {
                var bh = p.health.hediffSet.GetFirstHediffOfDef(bonusVsHediff);
                if (bh != null)
                {
                    mult *= bonusVsHediffFactor;
                    if (consumeHediff) p.health.RemoveHediff(bh);
                    if (p.Spawned) ArcaneFx.Particles(p.Map, p.DrawPos, ctx.Color2, ArcaneDefOf.AA_Fleck_Shard, 10, 2.5f, 0.6f);
                }
            }
            bool crit = false;
            if (!noCrit && ctx.RollCrit(out float cm))
            {
                crit = true;
                mult *= cm;
            }
            DealDamage(ctx, p, amount * mult, crit);
            if (!p.Dead) ArcaneReactions.TryTrigger(ctx, p, Def);
        }

        public void DealDamage(SpellContext ctx, Pawn p, float total, bool crit)
        {
            var def = Def;
            float pen = (armorPen >= 0f ? armorPen : def.defaultArmorPenetration) + ctx.mods.armorPen;
            bool wasDead = p.Dead;
            float dealt = 0f;
            Vector3 hitPos = p.DrawPos;
            int n = Mathf.Max(1, hits);
            float perHit = total / n;
            float angle = (p.DrawPos - (ctx.caster?.DrawPos ?? p.DrawPos)).AngleFlat();
            CompArcane.applyingSpellDamage = true;
            try
            {
                for (int i = 0; i < n && !p.Dead && !p.Destroyed; i++)
                {
                    var dinfo = new DamageInfo(def, perHit, pen, angle, ctx.caster, null, null, DamageInfo.SourceCategory.ThingOrUnknown, p);
                    if (def.armorCategory == null) dinfo.SetIgnoreArmor(true);
                    var r = p.TakeDamage(dinfo);
                    dealt += r?.totalDamageDealt ?? 0f;
                }
                if (lethalPercent > 0f && !p.Dead && !p.Destroyed)
                {
                    float extra = p.health.LethalDamageThreshold * lethalPercent * Mathf.Sqrt(Mathf.Max(0.1f, ctx.DamageFactor));
                    var dinfo = new DamageInfo(ArcaneDefOf.AA_ArcaneTrue, extra, 999f, angle, ctx.caster, null, null, DamageInfo.SourceCategory.ThingOrUnknown, p);
                    dinfo.SetIgnoreArmor(true);
                    var r = p.TakeDamage(dinfo);
                    dealt += r?.totalDamageDealt ?? 0f;
                }
                if (executeBelow > 0f && !p.Dead && !p.Destroyed && !(p.kindDef?.isBoss ?? false)
                    && p.health.summaryHealth.SummaryHealthPercent < executeBelow)
                {
                    ArcaneFx.Text(p, "AA_Executed".Translate(), new Color(1f, 0.3f, 0.3f));
                    p.Kill(new DamageInfo(ArcaneDefOf.AA_ArcaneTrue, 9999f, 999f, -1f, ctx.caster));
                }
            }
            finally
            {
                CompArcane.applyingSpellDamage = false;
            }
            if (ArcanumMod.Settings.damageNumbers) ArcaneFx.DamageNumber(ctx.map, hitPos, dealt, crit, ctx.Color);
            else if (crit && p.Spawned) ArcaneFx.Text(p, "AA_Crit".Translate(Mathf.RoundToInt(dealt)), new Color(1f, 0.85f, 0.3f));
            if (stunTicks > 0 && !p.Dead && p.stances?.stunner != null)
                p.stances.stunner.StunFor(Mathf.RoundToInt(stunTicks * ctx.DurationFactor), ctx.caster, false, true);
            ApplyOnHitMods(ctx, p);
            float ls = ctx.lifestealStat + ctx.mods.lifesteal + healCasterFraction;
            if (ls > 0f && dealt > 0f && ctx.caster != null && !ctx.caster.Dead)
            {
                ArcaneUtility.HealPawn(ctx.caster, dealt * ls);
                if (dealt > 3f && ctx.caster.Spawned && ctx.map != null && ctx.caster.Map == ctx.map && VRand.Chance(0.6f))
                    ArcaneVfxManager.For(ctx.map)?.Add(new VfxSoulWisp(hitPos, ctx.caster, ctx.Color, ctx.Color2));
            }
            ctx.totalDamage += dealt;
            ctx.arcane?.Notify_SpellHit(ctx, p, dealt);
            if (!wasDead && p.Dead)
            {
                try
                {
                    StyleFx.Death(ctx, p, hitPos);
                }
                catch (System.Exception e)
                {
                    Log.ErrorOnce("[Arcanum Ascendant] Death effect error: " + e, 0x5A1C0F1);
                }
                ctx.arcane?.Notify_Kill(p, ctx);
            }
        }

        public static void ApplyOnHitMods(SpellContext ctx, Pawn p)
        {
            if (p.Dead || ctx.mods.onHit.Count == 0) return;
            foreach (var (hd, ch, sev) in ctx.mods.onHit)
                if (Rand.Chance(ch)) ArcaneUtility.ApplyHediff(p, hd, sev, -1, ctx);
        }

        private float TypeFactor(Pawn p)
        {
            if (p.RaceProps.IsMechanoid) return vsMechanoids;
            if (p.RaceProps.Humanlike) return vsHumanlikes;
            if (p.RaceProps.Animal) return vsAnimals;
            return vsEntities;
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (!damageBuildings) return;
            var map = ctx.map;
            float dmg = amount * ctx.DamageFactor * buildingFactor * info.scale;
            var done = new HashSet<Thing>();
            foreach (var c in info.Cells(map))
                foreach (var t in c.GetThingList(map).ToList())
                {
                    if (!(t is Building b) || !done.Add(b) || b.Destroyed) continue;
                    if (b.Faction != null && ctx.caster?.Faction != null && !b.Faction.HostileTo(ctx.caster.Faction) && b.Faction == ctx.caster.Faction) continue;
                    b.TakeDamage(new DamageInfo(Def, dmg, 1f, -1f, ctx.caster));
                }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            float v = amount * (ctx?.DamageFactor ?? 1f);
            string s = "AA_Desc_Damage".Translate(v.ToString("F0"), Def.label);
            if (hits > 1) s += " " + "AA_Desc_Hits".Translate(hits);
            if (trueDamage) s += " " + "AA_Desc_IgnoresArmor".Translate();
            yield return s + Chance(chance);
            if (lethalPercent > 0f) yield return "AA_Desc_Lethal".Translate(lethalPercent.ToStringPercent());
            if (executeBelow > 0f) yield return "AA_Desc_Execute".Translate(executeBelow.ToStringPercent());
            if (vsMechanoids > 1.01f) yield return "AA_Desc_VsMech".Translate(vsMechanoids.ToStringPercent());
            if (stunTicks > 0) yield return "AA_Desc_Stun".Translate((stunTicks / 60f).ToString("F1"));
            if (healCasterFraction > 0f) yield return "AA_Desc_Drain".Translate(healCasterFraction.ToStringPercent());
            if (bonusVsHediff != null) yield return "AA_Desc_BonusVs".Translate(bonusVsHediffFactor.ToStringPercent(), bonusVsHediff.label);
            if (damageBuildings) yield return "AA_Desc_Buildings".Translate();
        }
    }

    public class Effect_Heal : SpellEffect
    {
        public float amount = 10f;
        public bool tend;
        public float regrowChance;
        public bool cureDiseases;
        public bool removeScars;
        public bool restoreBlood;

        public Effect_Heal()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            float heal = amount * ctx.HealFactor * info.scale;
            bool healCrit = ctx.RollCrit(out float cm);
            if (healCrit) heal *= cm;
            float healed = ArcaneUtility.HealPawn(p, heal, removeScars);
            if (healed >= 0.5f && p.Spawned) ArcaneFx.HealNumber(p.Map, p.DrawPos, healed, healCrit);
            if (tend)
                foreach (var h in p.health.hediffSet.hediffs.ToList())
                    if (h.TendableNow()) h.Tended(1f, 1f);
            if (restoreBlood)
            {
                var bl = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
                if (bl != null) p.health.RemoveHediff(bl);
            }
            if (cureDiseases) ArcaneUtility.Cleanse(p, diseases: true, toxins: true, drugs: false);
            if (regrowChance > 0f)
            {
                foreach (var mp in p.health.hediffSet.hediffs.OfType<Hediff_MissingPart>().ToList())
                {
                    if (!Rand.Chance(regrowChance * Mathf.Min(2f, ctx.HealFactor))) continue;
                    if (mp.Part == null || p.health.hediffSet.PartIsMissing(mp.Part.parent ?? mp.Part) && mp.Part.parent != null) continue;
                    p.health.RestorePart(mp.Part);
                    ArcaneFx.Text(p, "AA_Regrown".Translate(mp.Part.LabelCap), new Color(0.5f, 1f, 0.5f));
                    break;
                }
            }
            if (p.Spawned) FleckMaker.ThrowMetaIcon(p.Position, p.Map, FleckDefOf.HealingCross);
            ctx.arcane?.GainXp(heal * 0.25f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Heal".Translate((amount * (ctx?.HealFactor ?? 1f)).ToString("F0")) + Chance(chance);
            if (tend) yield return "AA_Desc_Tend".Translate();
            if (regrowChance > 0f) yield return "AA_Desc_Regrow".Translate(regrowChance.ToStringPercent());
            if (cureDiseases) yield return "AA_Desc_Cure".Translate();
        }
    }

    public class Effect_Hediff : SpellEffect
    {
        public HediffDef hediff;
        public float severity = 1f;
        public int durationTicks = -1;
        public bool stack;
        public bool scaleSeverityWithPower;
        public float maxSeverity = -1f;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (hediff == null) return;
            float sev = severity * info.scale;
            if (scaleSeverityWithPower) sev *= Mathf.Sqrt(ctx.DamageFactor);
            int dur = durationTicks > 0 ? Mathf.RoundToInt(durationTicks * ctx.DurationFactor) : -1;
            ArcaneUtility.ApplyHediff(p, hediff, sev, dur, ctx, stack, maxSeverity);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (hediff == null) yield break;
            string dur = durationTicks > 0 ? " " + "AA_Desc_For".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod()) : "";
            yield return "AA_Desc_Hediff".Translate(hediff.LabelCap) + dur + Chance(chance);
        }
    }

    public class Effect_Stun : SpellEffect
    {
        public int ticks = 120;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            p.stances?.stunner?.StunFor(Mathf.RoundToInt(ticks * ctx.DurationFactor * info.scale), ctx.caster, false, true);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Stun".Translate((ticks * (ctx?.DurationFactor ?? 1f) / 60f).ToString("F1")) + Chance(chance);
        }
    }

    public class Effect_Knockback : SpellEffect
    {
        public float distance = 3f;
        public bool pull;
        public float wallDamage = 8f;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.Spawned || p == ctx.caster) return;
            var map = p.Map;
            Vector3 from = info.radius > 0.5f ? info.pos : info.sourcePos;
            Vector3 dir = (p.DrawPos - from).Yto0();
            if (dir.sqrMagnitude < 0.01f) dir = (p.DrawPos - (ctx.caster?.DrawPos ?? from)).Yto0();
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();
            if (pull) dir = -dir;
            float dist = distance / Mathf.Max(1f, p.BodySize);
            IntVec3 dest = p.Position;
            bool hitWall = false;
            for (float d = 1f; d <= dist + 0.01f; d += 1f)
            {
                IntVec3 c = (p.DrawPos + dir * d).ToIntVec3();
                if (!c.InBounds(map) || !c.Standable(map)) { hitWall = true; break; }
                if (pull && from.ToIntVec3() == c) break;
                dest = c;
            }
            if (dest != p.Position)
            {
                ArcaneFx.Particles(map, p.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Mote, 4, 1f, 0.6f);
                p.Position = dest;
                p.Notify_Teleported(true, true);
                FleckMaker.ThrowDustPuffThick(p.DrawPos, map, 1.2f, new Color(0.7f, 0.7f, 0.7f));
            }
            if (hitWall && !pull && wallDamage > 0f)
            {
                CompArcane.applyingSpellDamage = true;
                try { p.TakeDamage(new DamageInfo(DamageDefOf.Blunt, wallDamage * ctx.DamageFactor, 0.2f, -1f, ctx.caster)); }
                finally { CompArcane.applyingSpellDamage = false; }
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return (pull ? "AA_Desc_Pull" : "AA_Desc_Knockback").Translate(distance.ToString("F0"));
        }
    }

    public class Effect_Explosion : SpellEffect
    {
        public DamageDef damageDef;
        public float amount = 20f;
        public float explosionRadius = 2.9f;
        public float armorPen = -1f;
        public float fireChance;
        public ThingDef postSpawn;
        public float postSpawnChance;
        public bool spareAllies = true;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            float r = Mathf.Min(explosionRadius * ctx.RadiusFactor, GenRadial.MaxRadialPatternRadius - 1f);
            var def = damageDef ?? DamageDefOf.Bomb;
            List<Thing> ignored = null;
            if (spareAllies && ctx.caster != null)
            {
                ignored = new List<Thing> { ctx.caster };
                foreach (var t in GenRadial.RadialDistinctThingsAround(info.cell, map, r, true))
                    if (t is Pawn p && ctx.IsAlly(p)) ignored.Add(p);
                    else if (t is Building b && b.Faction != null && b.Faction == ctx.caster.Faction) ignored.Add(b);
            }
            CompArcane.applyingSpellDamage = true;
            try
            {
                GenExplosion.DoExplosion(info.cell, map, r, def, ctx.caster, Mathf.RoundToInt(amount * ctx.DamageFactor * info.scale),
                    armorPen >= 0f ? armorPen + ctx.mods.armorPen : -1f, null, null, null, null, postSpawn, postSpawnChance, 1, null, null, 255, false, null, 0f, 1,
                    fireChance, false, null, ignored);
            }
            finally
            {
                CompArcane.applyingSpellDamage = false;
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Explosion".Translate((amount * (ctx?.DamageFactor ?? 1f)).ToString("F0"), (damageDef ?? DamageDefOf.Bomb).label,
                (explosionRadius * (ctx?.RadiusFactor ?? 1f)).ToString("F1"));
        }
    }

    public class Effect_Ignite : SpellEffect
    {
        public float cellChance = 0.3f;
        public float fireSize = 0.6f;
        public bool igniteTargets = true;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            foreach (var c in info.Cells(map))
                if (Rand.Chance(cellChance) && c.InBounds(map) && !c.Filled(map))
                    FireUtility.TryStartFireIn(c, map, fireSize, ctx.caster);
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (igniteTargets && p.Spawned) p.TryAttachFire(fireSize, ctx.caster);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Ignite".Translate();
        }
    }

    public class Effect_Extinguish : SpellEffect
    {
        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            foreach (var c in info.Cells(map))
                foreach (var f in c.GetThingList(map).OfType<Fire>().ToList())
                    f.Destroy();
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var fire = p.GetAttachment(ThingDefOf.Fire);
            if (fire != null && !fire.Destroyed) fire.Destroy();
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Extinguish".Translate();
        }
    }

    public class Effect_ShieldBreak : SpellEffect
    {
        public bool breakInterceptors = true;
        public int mechStunTicks;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (ArcaneUtility.BreakShields(p) && p.Spawned) ArcaneFx.Text(p, "AA_ShieldShattered".Translate(), new Color(0.6f, 0.8f, 1f));
            if (mechStunTicks > 0 && p.RaceProps.IsMechanoid)
                p.stances?.stunner?.StunFor(Mathf.RoundToInt(mechStunTicks * ctx.DurationFactor), ctx.caster, false, true);
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (!breakInterceptors) return;
            var map = ctx.map;
            float r = Mathf.Max(info.radius, 3f) + 6f;
            foreach (var t in GenRadial.RadialDistinctThingsAround(info.cell, map, Mathf.Min(r, GenRadial.MaxRadialPatternRadius - 1f), true))
            {
                var ic = t.TryGetComp<CompProjectileInterceptor>();
                if (ic != null && ic.Active && t.Faction != ctx.caster?.Faction)
                {
                    ArcaneUtility.BreakInterceptor(ic);
                    ArcaneFx.Text(t, "AA_ShieldShattered".Translate(), new Color(0.6f, 0.8f, 1f));
                }
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_ShieldBreak".Translate();
            if (mechStunTicks > 0) yield return "AA_Desc_MechStun".Translate((mechStunTicks / 60f).ToString("F1"));
        }
    }

    public class Effect_Ward : SpellEffect
    {
        public float amount = 20f;
        public bool overcap;

        public Effect_Ward()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var c = p.TryGetComp<CompArcane>();
            if (c == null) return;
            float amt = amount * ctx.HealFactor * info.scale;
            float cap = Mathf.Max(c.IsMage ? c.WardMax : 0f, amt) + (overcap ? amt : 0f);
            if (c.ward < cap) c.ward = Mathf.Min(cap, c.ward + amt);
            if (p.Spawned)
            {
                ArcaneVfxManager.For(p.Map).Add(new VfxSprite
                {
                    mat = ArcaneMats.Hex, follow = p, pos = p.DrawPos, scaleStart = 2.6f, scaleEnd = 1.9f, duration = 40, color = ctx.Color2,
                    fadeIn = 0.1f, fadeOut = 0.6f,
                });
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Ward".Translate((amount * (ctx?.HealFactor ?? 1f)).ToString("F0"));
        }
    }

    public class Effect_RestoreMana : SpellEffect
    {
        public float amount = 20f;
        public float percent;

        public Effect_RestoreMana()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var c = p.TryGetComp<CompArcane>();
            if (c == null || !c.IsMage) return;
            c.mana = Mathf.Min(c.ManaMax, c.mana + amount + c.ManaMax * percent);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return percent > 0f ? "AA_Desc_ManaPct".Translate(percent.ToStringPercent()) : "AA_Desc_Mana".Translate(amount.ToString("F0"));
        }
    }

    public class Effect_Cleanse : SpellEffect
    {
        public bool diseases = true;
        public bool toxins = true;
        public bool drugs;
        public bool mental = true;
        public bool debuffs = true;

        public Effect_Cleanse()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            ArcaneUtility.Cleanse(p, diseases, toxins, drugs, debuffs);
            if (mental && p.InMentalState && !p.MentalStateDef.IsAggro) p.MentalState.RecoverFromState();
            else if (mental && p.InMentalState && ctx.IsAlly(p)) p.MentalState.RecoverFromState();
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Cleanse".Translate();
        }
    }

    public class Effect_MentalState : SpellEffect
    {
        public MentalStateDef state;
        public bool humanlikeOnly;
        public bool resistedByBosses = true;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (state == null || p.InMentalState || p.Downed) return;
            if (humanlikeOnly && !p.RaceProps.Humanlike) return;
            if (resistedByBosses && (p.kindDef?.isBoss ?? false)) return;
            var st = state;
            if (st == MentalStateDefOf.Berserk && p.RaceProps.IsMechanoid) st = MentalStateDefOf.BerserkMechanoid;
            p.mindState?.mentalStateHandler?.TryStartMentalState(st, "AA_SpellReason".Translate(ctx.def.label), true, false, false, null, false, false, false);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (state != null) yield return "AA_Desc_Mental".Translate(state.label) + Chance(chance);
        }
    }
}

namespace ArcanumAscendant
{
    /// <summary>Detonates corpses in the area.</summary>
    public class Effect_CorpseExplosion : SpellEffect
    {
        public DamageDef damageDef;
        public float damage = 30f;
        public float explosionRadius = 2.5f;
        public int maxCorpses = 6;

        public Effect_CorpseExplosion()
        {
            affects = TargetFilter.Corpses;
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse corpse, ImpactInfo info)
        {
            if (ctx.hitThisCast.Count(t => t is Corpse) >= maxCorpses + ctx.mods.extraTargets * 2) return;
            if (corpse.Destroyed || !corpse.Spawned) return;
            ctx.hitThisCast.Add(corpse);
            var map = corpse.Map;
            IntVec3 cell = corpse.Position;
            float size = Mathf.Clamp(corpse.InnerPawn?.BodySize ?? 1f, 0.5f, 3f);
            float r = Mathf.Min(explosionRadius * ctx.RadiusFactor * Mathf.Sqrt(size), GenRadial.MaxRadialPatternRadius - 1f);
            corpse.Destroy();
            FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Blood, 3);
            var ignored = new List<Thing>();
            if (ctx.caster != null)
            {
                ignored.Add(ctx.caster);
                foreach (var t in GenRadial.RadialDistinctThingsAround(cell, map, r, true))
                    if (t is Pawn p && ctx.IsAlly(p)) ignored.Add(p);
            }
            CompArcane.applyingSpellDamage = true;
            try
            {
                GenExplosion.DoExplosion(cell, map, r, damageDef ?? DamageDefOf.Bomb, ctx.caster, Mathf.RoundToInt(damage * size * ctx.DamageFactor), 0.4f,
                    null, null, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, ignored);
            }
            finally { CompArcane.applyingSpellDamage = false; }
            ArcaneFx.Particles(map, cell.ToVector3Shifted(), new Color(0.6f, 0.05f, 0.05f), ArcaneDefOf.AA_Fleck_Bone, 8, 3f, 0.6f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_CorpseExplosion".Translate((damage * (ctx?.DamageFactor ?? 1f)).ToString("F0"), maxCorpses);
        }
    }

    /// <summary>Interrogate a corpse: the caster learns from the dead person's best skill.</summary>
    public class Effect_SpeakWithDead : SpellEffect
    {
        public float xp = 2000f;

        public Effect_SpeakWithDead()
        {
            affects = TargetFilter.Corpses;
        }

        public override void ApplyToCorpse(SpellContext ctx, Corpse corpse, ImpactInfo info)
        {
            var dead = corpse.InnerPawn;
            var caster = ctx.caster;
            if (dead?.skills == null || caster?.skills == null || ctx.hitThisCast.Contains(corpse)) return;
            ctx.hitThisCast.Add(corpse);
            var best = dead.skills.skills.Where(s => !s.TotallyDisabled).OrderByDescending(s => s.Level).FirstOrDefault();
            if (best == null) return;
            var mine = caster.skills.GetSkill(best.def);
            if (mine == null || mine.TotallyDisabled) return;
            mine.Learn(xp * Mathf.Max(1f, best.Level / 5f), true);
            ArcaneFx.Text(caster, "AA_SpokeWithDead".Translate(dead.LabelShort, best.def.label), ctx.Color);
            if (corpse.Spawned) ArcaneFx.Particles(corpse.Map, corpse.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Skull, 3, 0.6f, 0.6f);
            var rot = corpse.GetComp<CompRottable>();
            if (rot != null) rot.RotProgress += rot.PropsRot.TicksToRotStart;
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_SpeakWithDead".Translate();
        }
    }

    /// <summary>Strips magical buffs and shields from targets.</summary>
    public class Effect_Dispel : SpellEffect
    {
        public bool breakShields = true;
        public bool endDomination = true;

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            int removed = 0;
            foreach (var h in p.health.hediffSet.hediffs.ToList())
            {
                if (h.def.isBad || h.def == ArcaneDefOf.AA_ArcaneCore || h.def == ArcaneDefOf.AA_Undead || h.def == ArcaneDefOf.AA_Summoned) continue;
                if (!(h is HediffWithComps hc) || hc.TryGetComp<HediffComp_Disappears>() == null) continue;
                p.health.RemoveHediff(h);
                removed++;
            }
            var c = p.TryGetComp<CompArcane>();
            if (c != null && c.ward > 0f)
            {
                c.ward = 0f;
                removed++;
            }
            if (breakShields && ArcaneUtility.BreakShields(p)) removed++;
            if (removed > 0 && p.Spawned) ArcaneFx.Text(p, "AA_Dispelled".Translate(), ctx.Color);
            if (p.health.hediffSet.HasHediff(ArcaneDefOf.AA_Summoned) && p.Faction != ctx.caster?.Faction && ctx.IsEnemy(p))
                p.health.hediffSet.GetFirstHediffOfDef(ArcaneDefOf.AA_Summoned)?.TryGetComp<HediffComp_Summoned>()?.Expire();
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Dispel".Translate();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    public enum TargetFilter
    {
        Inherit,
        Enemies,
        Allies,
        AlliesNotSelf,
        Self,
        All,
        AllButSelf,
        Corpses,
    }

    public enum DeliveryType
    {
        Instant,
        Projectile,
        Meteor,
        Beam,
        Chain,
        Cone,
        Nova,
        Self,
        Barrage,
        Line,
    }

    /// <summary>Spell definition data, attached to an AbilityDef as its effect comp.</summary>
    public class CompProperties_ArcaneSpell : CompProperties_AbilityEffect
    {
        public DeliveryType delivery = DeliveryType.Instant;
        public TargetFilter affects = TargetFilter.Enemies;
        public float radius;
        public List<SpellEffect> effects = new List<SpellEffect>();
        public List<SpellEffect> casterEffects = new List<SpellEffect>();

        // projectile / meteor
        public string projectileTex = "ArcanumAscendant/Vfx/Orb";
        public float projectileScale = 0.9f;
        public float projectileSpeed = 22f;
        public int projectileCount = 1;
        public float projectileArc;
        public bool projectileSpin;
        public float multiTargetRadius = 6f;

        // beam
        public float beamWidth = 0.9f;
        public string beamTex = "ArcanumAscendant/Vfx/Beam";
        public int beamTicks = 22;
        public bool beamToMaxRange;

        // chain
        public int chainCount = 3;
        public float chainRange = 6f;
        public float chainFalloff = 0.85f;
        public int chainDelay = 6;

        // cone & line
        public float coneAngle = 45f;
        public float coneRange = 7f;
        public int lineStepDelay = 2;
        public float lineLength = 12f;

        // barrage
        public int barrageCount = 6;
        public int barrageInterval = 8;
        public float barrageRadius = 4f;
        public float strikeRadius = 1.5f;
        public StrikeStyle strikeStyle = StrikeStyle.Meteor;

        // visuals & audio
        public Color color = Color.white;
        public Color color2 = Color.white;
        public List<VfxSpec> castVfx = new List<VfxSpec>();
        public List<VfxSpec> impactVfx = new List<VfxSpec>();
        public List<VfxSpec> casterVfx = new List<VfxSpec>();
        public SoundDef castSound;
        public SoundDef impactSound;
        public float shake;

        public CompProperties_ArcaneSpell()
        {
            compClass = typeof(CompAbilityEffect_ArcaneSpell);
            canTargetBosses = true;
            applicableToMechs = true;
            availableWhenTargetIsWounded = true;
            canTargetBaby = true;
        }

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach (var e in base.ConfigErrors(parentDef)) yield return e;
            if (effects.Any(e => e == null)) yield return "null effect in " + parentDef.defName;
        }
    }

    public enum StrikeStyle
    {
        Meteor,
        Lightning,
        Pillar,
        Eruption,
        Shard,
    }

    /// <summary>Runtime context for a single cast.</summary>
    public class SpellContext
    {
        public Pawn caster;
        public CompArcane arcane;
        public AbilityDef def;
        public SpellExtension ext;
        public CompProperties_ArcaneSpell props;
        public SpellModSummary mods;
        public Map map;
        public float basePower = 1f;
        public float critChance;
        public float critMultiplier = 1.5f;
        public float areaStat = 1f;
        public float durationStat = 1f;
        public float summonStat = 1f;
        public float healStat = 1f;
        public float lifestealStat;
        public bool free;
        public float totalDamage;
        public Color Color => props?.color ?? Color.white;
        public Color Color2 => props?.color2 ?? Color.white;
        public HashSet<Thing> hitThisCast = new HashSet<Thing>();
        /// <summary>The pawn the caster deliberately aimed an offensive spell at. It counts as an enemy even when it isn't
        /// hostile (hunting a wild animal, punishing a colonist), the same way a forced attack works in vanilla.</summary>
        public Pawn forcedTarget;
        public Faction Faction => caster?.Faction;

        public float DamageFactor => basePower * Mathf.Max(0.1f, 1f + mods.damage);
        public float HealFactor => basePower * healStat * Mathf.Max(0.1f, 1f + mods.healing);
        public float RadiusFactor => areaStat * Mathf.Max(0.2f, 1f + mods.radius);
        public float DurationFactor => durationStat * Mathf.Max(0.2f, 1f + mods.duration);
        public float SummonFactor => summonStat * Mathf.Max(0.2f, 1f + mods.summonPower);
        public float CritChance => Mathf.Clamp01(critChance + mods.critChance);

        public static SpellContext For(Pawn caster, AbilityDef def)
        {
            var ctx = new SpellContext
            {
                caster = caster,
                def = def,
                ext = def.GetModExtension<SpellExtension>(),
                props = def.comps?.OfType<CompProperties_ArcaneSpell>().FirstOrDefault(),
                map = caster?.MapHeld,
                arcane = caster?.TryGetComp<CompArcane>(),
            };
            if (ctx.arcane != null && ctx.arcane.IsMage)
            {
                ctx.mods = ctx.arcane.ModsFor(def);
                ctx.basePower = ctx.arcane.SpellPower;
                ctx.critChance = caster.GetStatValue(ArcaneDefOf.AA_CritChance);
                ctx.critMultiplier = caster.GetStatValue(ArcaneDefOf.AA_CritDamage);
                ctx.areaStat = caster.GetStatValue(ArcaneDefOf.AA_AreaFactor);
                ctx.durationStat = caster.GetStatValue(ArcaneDefOf.AA_DurationFactor);
                ctx.summonStat = caster.GetStatValue(ArcaneDefOf.AA_SummonPower);
                ctx.healStat = caster.GetStatValue(ArcaneDefOf.AA_HealingPower);
                ctx.lifestealStat = caster.GetStatValue(ArcaneDefOf.AA_Lifesteal);
            }
            else
            {
                ctx.mods = new SpellModSummary();
                ctx.basePower = ArcanumMod.Settings.spellPowerMultiplier;
            }
            return ctx;
        }

        public bool RollCrit(out float mult)
        {
            if (Rand.Chance(CritChance))
            {
                mult = critMultiplier;
                return true;
            }
            mult = 1f;
            return false;
        }

        public bool Passes(Thing t, TargetFilter filter)
        {
            if (filter == TargetFilter.Inherit) filter = props?.affects ?? TargetFilter.Enemies;
            if (t == null) return false;
            if (filter == TargetFilter.Corpses) return t is Corpse;
            if (!(t is Pawn p)) return false;
            if (p.Dead) return false;
            switch (filter)
            {
                case TargetFilter.Self: return p == caster;
                case TargetFilter.All: return true;
                case TargetFilter.AllButSelf: return p != caster;
                case TargetFilter.Enemies: return p != caster && (p == forcedTarget || IsEnemy(p));
                case TargetFilter.Allies: return p == caster || IsAlly(p);
                case TargetFilter.AlliesNotSelf: return p != caster && IsAlly(p);
            }
            return false;
        }

        public bool IsEnemy(Pawn p)
        {
            if (caster == null) return true;
            if (p.HostileTo(caster)) return true;
            if (p.InMentalState && p.MentalState.def.IsAggro) return p.Faction != caster.Faction || p.MentalStateDef == MentalStateDefOf.Berserk;
            return false;
        }

        public bool IsAlly(Pawn p)
        {
            if (caster == null) return false;
            if (p.HostileTo(caster)) return false;
            if (p.Faction != null && p.Faction == caster.Faction) return true;
            if (caster.Faction == Faction.OfPlayer && (p.IsPrisonerOfColony || p.IsSlaveOfColony)) return true;
            if (caster.Faction == Faction.OfPlayer && p.Faction != null && p.Faction.def.defName == "AA_ArcaneThralls") return true;
            return p.Faction != null && caster.Faction != null && !p.Faction.HostileTo(caster.Faction) && p.RaceProps.Humanlike;
        }
    }

    /// <summary>Info about one impact: where it landed, which cells and pawns are in the area.</summary>
    public class ImpactInfo
    {
        public IntVec3 cell;
        public Vector3 pos;
        public Thing primary;
        public float radius;
        public float scale = 1f;
        public List<IntVec3> areaCells;
        public Vector3 sourcePos;

        public List<IntVec3> Cells(Map map)
        {
            if (areaCells == null)
            {
                areaCells = new List<IntVec3>();
                if (radius < 0.5f) areaCells.Add(cell);
                else
                    foreach (var c in GenRadial.RadialCellsAround(cell, radius, true))
                        if (c.InBounds(map)) areaCells.Add(c);
            }
            return areaCells;
        }
    }
}

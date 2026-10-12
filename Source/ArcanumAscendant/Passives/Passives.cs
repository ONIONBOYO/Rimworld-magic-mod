using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArcanumAscendant
{
    /// <summary>Base class for skill tree passives. All hooks receive the current rank.</summary>
    public abstract class ArcanePassive
    {
        public string key;
        public string tag;
        public int cooldownTicks;

        public virtual void ResolveReferences(ArcaneNodeDef node)
        {
            if (key.NullOrEmpty()) key = node.defName + "_" + GetType().Name;
        }

        public abstract string Describe(int rank);

        protected bool Ready(CompArcane c) => cooldownTicks <= 0 || c.PassiveReady(key);
        protected void Trigger(CompArcane c) { if (cooldownTicks > 0) c.StartPassiveCooldown(key, cooldownTicks); }
        protected bool TagMatch(SpellContext ctx) => tag.NullOrEmpty() || (ctx?.ext != null && ctx.ext.tags.Contains(tag));
        protected static string Pct(float v) => v.ToStringPercent();
        protected string CdText => cooldownTicks > 0 ? " " + "AA_PassiveCooldown".Translate(cooldownTicks.ToStringTicksToPeriod()) : "";
        protected string TagText => tag.NullOrEmpty() ? "" : " " + "AA_PassiveTag".Translate(tag);

        public virtual float SpellPowerFactor(CompArcane c, int rank) => 1f;
        public virtual float TargetDamageFactor(CompArcane c, int rank, Pawn target, SpellContext ctx) => 1f;
        public virtual void ModifyStats(CompArcane c, int rank, Action<StatDef, float> addOffset, Action<StatDef, float> addPercent) { }
        public virtual void ModifySpell(CompArcane c, AbilityDef def, SpellExtension ext, SpellModSummary sum, int rank) { }
        public virtual void Tick(CompArcane c, int rank, int elapsed) { }
        public virtual void PreDamageTaken(CompArcane c, int rank, ref DamageInfo dinfo, ref bool absorbed) { }
        public virtual void PostDamageTaken(CompArcane c, int rank, DamageInfo dinfo, float dealt) { }
        public virtual void WeaponHit(CompArcane c, int rank, Thing victim, DamageInfo dinfo, DamageWorker.DamageResult result) { }
        public virtual void SpellCast(CompArcane c, int rank, SpellContext ctx) { }
        public virtual void SpellHit(CompArcane c, int rank, SpellContext ctx, Pawn target, float damage) { }
        public virtual void Kill(CompArcane c, int rank, Pawn victim, SpellContext ctx) { }
        public virtual void Downed(CompArcane c, int rank) { }
        public virtual void Notify_Learned(CompArcane c, int rank) { }
    }

    public class P_ManaOnKill : ArcanePassive
    {
        public float amount = 5f;
        public override string Describe(int rank) => "AA_P_ManaOnKill".Translate((amount * rank).ToString("F0"));
        public override void Kill(CompArcane c, int rank, Pawn victim, SpellContext ctx) => c.mana = Mathf.Min(c.ManaMax, c.mana + amount * rank);
    }

    public class P_ManaOnHit : ArcanePassive
    {
        public float amount = 1f;
        public float chance = 1f;
        public bool weaponHits;
        public override string Describe(int rank) => (weaponHits ? "AA_P_ManaOnWeaponHit" : "AA_P_ManaOnHit").Translate((amount * rank).ToString("F1"), Pct(chance)) + TagText;

        public override void SpellHit(CompArcane c, int rank, SpellContext ctx, Pawn target, float damage)
        {
            if (!weaponHits && TagMatch(ctx) && Rand.Chance(chance)) c.mana = Mathf.Min(c.ManaMax, c.mana + amount * rank);
        }

        public override void WeaponHit(CompArcane c, int rank, Thing victim, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            if (weaponHits && Rand.Chance(chance)) c.mana = Mathf.Min(c.ManaMax, c.mana + amount * rank);
        }
    }

    /// <summary>Chance to refund mana and reset the cooldown of the spell just cast.</summary>
    public class P_EchoCast : ArcanePassive
    {
        public float chancePerRank = 0.05f;
        public override string Describe(int rank) => "AA_P_Echo".Translate(Pct(chancePerRank * rank)) + TagText;

        public override void SpellCast(CompArcane c, int rank, SpellContext ctx)
        {
            if (ctx.free || ctx.ext == null || ctx.ext.ultimate || !TagMatch(ctx) || !Rand.Chance(chancePerRank * rank)) return;
            var ab = c.Pawn.abilities?.GetAbility(ctx.def) as ArcaneAbility;
            if (ab == null) return;
            ArcaneVfxManager.For(c.Pawn.Map)?.Schedule(2, () =>
            {
                ab.ResetCooldown();
                c.mana = Mathf.Min(c.ManaMax, c.mana + ab.ManaCost);
                if (c.Pawn.Spawned) ArcaneFx.Text(c.Pawn, "AA_Echo".Translate(), c.DisplayClass.color);
            });
        }
    }

    public class P_KillExplosion : ArcanePassive
    {
        public DamageDef damageDef;
        public float damage = 15f;
        public float radius = 2.5f;
        public float chance = 1f;
        public bool spellKillsOnly = true;
        public override string Describe(int rank) => "AA_P_KillExplosion".Translate(Pct(chance), (damage * rank).ToString("F0"), (damageDef ?? DamageDefOf.Bomb).label, radius.ToString("F1")) + TagText;

        public override void Kill(CompArcane c, int rank, Pawn victim, SpellContext ctx)
        {
            if (spellKillsOnly && ctx == null) return;
            if (ctx != null && !TagMatch(ctx)) return;
            if (!Rand.Chance(chance)) return;
            var map = victim.MapHeld ?? c.Pawn.Map;
            if (map == null) return;
            IntVec3 cell = victim.PositionHeld;
            var caster = c.Pawn;
            float power = c.SpellPower;
            ArcaneVfxManager.For(map).Schedule(2, () =>
            {
                var ignored = GenRadial.RadialDistinctThingsAround(cell, map, radius, true).OfType<Pawn>().Where(p => !p.HostileTo(caster)).Cast<Thing>().ToList();
                CompArcane.applyingSpellDamage = true;
                try
                {
                    GenExplosion.DoExplosion(cell, map, radius, damageDef ?? DamageDefOf.Bomb, caster, Mathf.RoundToInt(damage * rank * power), -1f, null, null, null, null, null, 0f, 1, null, null, 255,
                        false, null, 0f, 1, 0f, false, null, ignored);
                }
                finally { CompArcane.applyingSpellDamage = false; }
            });
        }
    }

    /// <summary>When downed, rise again fully healed (with a cooldown).</summary>
    public class P_Rebirth : ArcanePassive
    {
        public float novaDamage = 30f;
        public DamageDef novaDamageDef;
        public float novaRadius = 4f;
        public float wardFraction = 0.5f;
        public Color color = new Color(1f, 0.6f, 0.2f);
        public string labelKey = "AA_Reborn";

        public P_Rebirth()
        {
            cooldownTicks = 120000;
        }

        public override string Describe(int rank) => "AA_P_Rebirth".Translate((novaDamage * rank).ToString("F0"), novaRadius.ToString("F1")) + CdText;

        public override void Downed(CompArcane c, int rank)
        {
            if (!Ready(c)) return;
            var p = c.Pawn;
            if (p.Dead || !p.Spawned) return;
            Trigger(c);
            var map = p.Map;
            var caster = p;
            float power = c.SpellPower;
            ArcaneVfxManager.For(map).Schedule(30, () =>
            {
                if (caster.Dead || !caster.Spawned) return;
                ArcaneUtility.HealPawn(caster, 9999f);
                foreach (var h in caster.health.hediffSet.hediffs.ToList())
                    if (h.def == HediffDefOf.BloodLoss || (h.def.isBad && h is Hediff_Injury)) caster.health.RemoveHediff(h);
                c.ward = c.WardMax * wardFraction;
                c.mana = c.ManaMax;
                ArcaneVfxManager.For(map).Add(new VfxPillar(caster.DrawPos, 80, color, Color.white, 3f));
                ArcaneVfxManager.For(map).Add(new VfxSprite { mat = ArcaneMats.Shock, pos = caster.DrawPos, scaleStart = 0.5f, scaleEnd = novaRadius * 2.5f, duration = 40, color = color, fadeIn = 0.02f, fadeOut = 0.8f });
                ArcaneFx.Particles(map, caster.DrawPos, color, ArcaneDefOf.AA_Fleck_Ember, 40, 4f, 1f);
                ArcaneFx.Shake(map, 3f);
                ArcaneFx.Text(caster, labelKey.Translate(), color);
                var ignored = GenRadial.RadialDistinctThingsAround(caster.Position, map, novaRadius, true).OfType<Pawn>().Where(x => !x.HostileTo(caster)).Cast<Thing>().ToList();
                CompArcane.applyingSpellDamage = true;
                try
                {
                    GenExplosion.DoExplosion(caster.Position, map, novaRadius, novaDamageDef ?? DamageDefOf.Flame, caster, Mathf.RoundToInt(novaDamage * rank * power), -1f, null, null, null, null, null, 0f, 1,
                        null, null, 255, false, null, 0f, 1, 0f, false, null, ignored);
                }
                finally { CompArcane.applyingSpellDamage = false; }
            });
        }
    }

    public class P_Thorns : ArcanePassive
    {
        public float fraction = 0.1f;
        public DamageDef damageDef;
        public bool meleeOnly = true;
        public override string Describe(int rank) => "AA_P_Thorns".Translate(Pct(fraction * rank), (damageDef ?? ArcaneDefOf.AA_Arcane).label) + (meleeOnly ? " " + "AA_MeleeOnly".Translate().ToString() : "");

        public override void PostDamageTaken(CompArcane c, int rank, DamageInfo dinfo, float dealt)
        {
            if (!(dinfo.Instigator is Pawn attacker) || attacker == c.Pawn || attacker.Dead || dealt <= 0f) return;
            if (meleeOnly && (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon || attacker.Position.DistanceTo(c.Pawn.Position) > 2f)) return;
            CompArcane.applyingSpellDamage = true;
            try { attacker.TakeDamage(new DamageInfo(damageDef ?? ArcaneDefOf.AA_Arcane, dealt * fraction * rank * c.SpellPower, 0.5f, -1f, c.Pawn)); }
            finally { CompArcane.applyingSpellDamage = false; }
            if (attacker.Spawned) ArcaneFx.Particles(attacker.Map, attacker.DrawPos, c.DisplayClass.color, ArcaneDefOf.AA_Fleck_Spark, 3, 1.2f, 0.4f);
        }
    }

    public class P_DamageReduction : ArcanePassive
    {
        public float fractionPerRank = 0.03f;
        public List<DamageDef> damageDefs;
        public bool rangedOnly;
        public bool meleeOnly;
        public float maxTotal = 0.75f;
        public override string Describe(int rank)
        {
            string scope = !damageDefs.NullOrEmpty() ? string.Join(", ", damageDefs.Select(d => d.label)) : rangedOnly ? "AA_Ranged".Translate().ToString() : meleeOnly ? "AA_Melee".Translate().ToString() : "AA_AllDamage".Translate().ToString();
            return "AA_P_DamageReduction".Translate(Pct(Mathf.Min(maxTotal, fractionPerRank * rank)), scope);
        }

        public override void PreDamageTaken(CompArcane c, int rank, ref DamageInfo dinfo, ref bool absorbed)
        {
            if (!damageDefs.NullOrEmpty() && !damageDefs.Contains(dinfo.Def)) return;
            bool ranged = dinfo.Def.isRanged || (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon);
            if (rangedOnly && !ranged) return;
            if (meleeOnly && ranged) return;
            dinfo.SetAmount(dinfo.Amount * (1f - Mathf.Min(maxTotal, fractionPerRank * rank)));
        }
    }

    public class P_LowHealthPower : ArcanePassive
    {
        public float threshold = 0.5f;
        public float bonusPerRank = 0.1f;
        public override string Describe(int rank) => "AA_P_LowHealthPower".Translate(Pct(bonusPerRank * rank), Pct(threshold));

        public override float SpellPowerFactor(CompArcane c, int rank)
        {
            var p = c.Pawn;
            if (p?.health?.summaryHealth == null) return 1f;
            return p.health.summaryHealth.SummaryHealthPercent < threshold ? 1f + bonusPerRank * rank : 1f;
        }
    }

    public class P_FullManaPower : ArcanePassive
    {
        public float threshold = 0.8f;
        public float bonusPerRank = 0.08f;
        public override string Describe(int rank) => "AA_P_FullManaPower".Translate(Pct(bonusPerRank * rank), Pct(threshold));
        public override float SpellPowerFactor(CompArcane c, int rank) => c.mana >= c.ManaMax * threshold ? 1f + bonusPerRank * rank : 1f;
    }

    public class P_OnHitHediff : ArcanePassive
    {
        public HediffDef hediff;
        public float chancePerRank = 0.1f;
        public float severity = 0.5f;
        public bool spells = true;
        public bool weapons;
        public bool self;
        public int durationTicks = -1;

        public override string Describe(int rank)
        {
            string src = spells && weapons ? "AA_SrcAny".Translate() : weapons ? "AA_SrcWeapon".Translate() : "AA_SrcSpell".Translate();
            return (self ? "AA_P_OnHitSelfHediff" : "AA_P_OnHitHediff").Translate(Pct(Mathf.Min(1f, chancePerRank * rank)), src, hediff?.label ?? "?") + TagText + CdText;
        }

        private void Do(CompArcane c, Pawn target, int rank)
        {
            if (hediff == null || !Ready(c) || !Rand.Chance(chancePerRank * rank)) return;
            var who = self ? c.Pawn : target;
            if (who == null || who.Dead) return;
            Trigger(c);
            var h = ArcaneUtility.ApplyHediff(who, hediff, severity, durationTicks, null, stack: self);
            var dot = (h as HediffWithComps)?.TryGetComp<HediffComp_ArcaneDot>();
            if (dot != null && !self)
            {
                dot.instigator = c.Pawn;
                dot.power = c.SpellPower;
            }
        }

        public override void SpellHit(CompArcane c, int rank, SpellContext ctx, Pawn target, float damage)
        {
            if (spells && TagMatch(ctx)) Do(c, target, rank);
        }

        public override void WeaponHit(CompArcane c, int rank, Thing victim, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            if (weapons && victim is Pawn p) Do(c, p, rank);
        }
    }

    public class P_SelfBuffOnCast : ArcanePassive
    {
        public HediffDef hediff;
        public float severityPerCast = 0.2f;
        public int durationTicks = 600;
        public override string Describe(int rank) => "AA_P_SelfBuffOnCast".Translate(hediff?.label ?? "?") + TagText;

        public override void SpellCast(CompArcane c, int rank, SpellContext ctx)
        {
            if (hediff == null || !TagMatch(ctx)) return;
            ArcaneUtility.ApplyHediff(c.Pawn, hediff, severityPerCast * rank, durationTicks, null, stack: true);
        }
    }

    public class P_Execute : ArcanePassive
    {
        public float thresholdPerRank = 0.05f;
        public override string Describe(int rank) => "AA_P_Execute".Translate(Pct(thresholdPerRank * rank)) + TagText;

        public override void SpellHit(CompArcane c, int rank, SpellContext ctx, Pawn target, float damage)
        {
            if (target.Dead || target.Destroyed || (target.kindDef?.isBoss ?? false) || !TagMatch(ctx) || damage <= 0f) return;
            if (target.health.summaryHealth.SummaryHealthPercent < thresholdPerRank * rank)
            {
                if (target.Spawned) ArcaneFx.Text(target, "AA_Executed".Translate(), new Color(1f, 0.3f, 0.3f));
                CompArcane.applyingSpellDamage = true;
                try { target.Kill(new DamageInfo(ArcaneDefOf.AA_ArcaneTrue, 9999f, 999f, -1f, c.Pawn)); }
                finally { CompArcane.applyingSpellDamage = false; }
                c.Notify_Kill(target, ctx);
            }
        }
    }

    public class P_DodgeBlink : ArcanePassive
    {
        public float chancePerRank = 0.05f;
        public bool rangedOnly = true;

        public P_DodgeBlink()
        {
            cooldownTicks = 300;
        }

        public override string Describe(int rank) => "AA_P_DodgeBlink".Translate(Pct(chancePerRank * rank)) + CdText;

        public override void PreDamageTaken(CompArcane c, int rank, ref DamageInfo dinfo, ref bool absorbed)
        {
            var p = c.Pawn;
            if (!p.Spawned || p.Downed || !Ready(c)) return;
            bool ranged = dinfo.Def.isRanged || (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon);
            if (rangedOnly && !ranged) return;
            if (!Rand.Chance(chancePerRank * rank)) return;
            Trigger(c);
            absorbed = true;
            var map = p.Map;
            var cell = CellFinder.StandableCellNear(p.Position, map, 4f, x => x != p.Position && x.GetFirstPawn(map) == null && x.DistanceTo(p.Position) >= 2f);
            ArcaneFx.Text(p, "AA_Dodged".Translate(), c.DisplayClass.color);
            if (cell.IsValid)
            {
                var pawn = p;
                ArcaneVfxManager.For(map).Schedule(0, () => Effect_Teleport.Blink(pawn, cell, null));
            }
        }
    }

    /// <summary>A portion of incoming damage is paid with mana instead of health.</summary>
    public class P_ManaShield : ArcanePassive
    {
        public float fractionPerRank = 0.1f;
        public float manaPerDamage = 1.5f;
        public override string Describe(int rank) => "AA_P_ManaShield".Translate(Pct(fractionPerRank * rank), manaPerDamage.ToString("F1"));

        public override void PreDamageTaken(CompArcane c, int rank, ref DamageInfo dinfo, ref bool absorbed)
        {
            float part = dinfo.Amount * Mathf.Min(0.9f, fractionPerRank * rank);
            float cost = part * manaPerDamage;
            if (c.mana < cost || part <= 0f) return;
            c.mana -= cost;
            dinfo.SetAmount(dinfo.Amount - part);
        }
    }

    /// <summary>Allows casting with health when out of mana.</summary>
    public class P_BloodPact : ArcanePassive
    {
        public float healthPerMana = 0.25f;
        public float reductionPerRank = 0.15f;
        public float HealthPerMana(int rank) => healthPerMana * Mathf.Max(0.2f, 1f - reductionPerRank * (rank - 1));
        public override string Describe(int rank) => "AA_P_BloodPact".Translate(HealthPerMana(rank).ToString("F2"));
    }

    public enum CreatureType
    {
        Mechanoid,
        Humanlike,
        Animal,
        Entity,
        Boss,
        Insect,
    }

    public class P_DamageVs : ArcanePassive
    {
        public CreatureType vs = CreatureType.Mechanoid;
        public float bonusPerRank = 0.1f;
        public override string Describe(int rank) => "AA_P_DamageVs".Translate(Pct(bonusPerRank * rank), ("AA_Creature_" + vs).Translate()) + TagText;

        public override float TargetDamageFactor(CompArcane c, int rank, Pawn target, SpellContext ctx)
        {
            if (!TagMatch(ctx)) return 1f;
            bool m;
            switch (vs)
            {
                case CreatureType.Mechanoid: m = target.RaceProps.IsMechanoid; break;
                case CreatureType.Humanlike: m = target.RaceProps.Humanlike; break;
                case CreatureType.Animal: m = target.RaceProps.Animal; break;
                case CreatureType.Boss: m = (target.kindDef?.isBoss ?? false) || target.BodySize >= 2.5f; break;
                case CreatureType.Insect: m = target.RaceProps.Insect; break;
                default: m = !target.RaceProps.Humanlike && !target.RaceProps.Animal && !target.RaceProps.IsMechanoid; break;
            }
            return m ? 1f + bonusPerRank * rank : 1f;
        }
    }

    public class P_ShieldBreaker : ArcanePassive
    {
        public float chancePerRank = 0.2f;
        public override string Describe(int rank) => "AA_P_ShieldBreaker".Translate(Pct(Mathf.Min(1f, chancePerRank * rank))) + TagText;

        public override void SpellHit(CompArcane c, int rank, SpellContext ctx, Pawn target, float damage)
        {
            if (!TagMatch(ctx) || !Rand.Chance(chancePerRank * rank)) return;
            if (ArcaneUtility.BreakShields(target) && target.Spawned) ArcaneFx.Text(target, "AA_ShieldShattered".Translate(), new Color(0.6f, 0.8f, 1f));
        }
    }

    public class P_RaiseOnKill : ArcanePassive
    {
        public float chancePerRank = 0.1f;
        public int durationTicks = 15000;
        public HediffDef undeadHediff;
        public override string Describe(int rank) => "AA_P_RaiseOnKill".Translate(Pct(Mathf.Min(1f, chancePerRank * rank)), durationTicks.ToStringTicksToPeriod()) + TagText;

        public override void Kill(CompArcane c, int rank, Pawn victim, SpellContext ctx)
        {
            if (ctx != null && !TagMatch(ctx)) return;
            if (victim.RaceProps.IsMechanoid || (victim.kindDef?.isBoss ?? false) || !Rand.Chance(chancePerRank * rank)) return;
            var caster = c.Pawn;
            var map = caster.Map;
            if (map == null) return;
            ArcaneVfxManager.For(map).Schedule(45, () =>
            {
                if (!victim.Dead || victim.Corpse == null || !victim.Corpse.Spawned) return;
                var sctx = new SpellContext { caster = caster, arcane = c, map = map, mods = new SpellModSummary(), basePower = c.SpellPower, durationStat = 1f };
                sctx.props = new CompProperties_ArcaneSpell { color = new Color(0.4f, 1f, 0.6f), color2 = new Color(0.2f, 0.5f, 0.3f) };
                ArcaneUtility.RaiseUndead(victim, sctx, durationTicks, undeadHediff ?? ArcaneDefOf.AA_Undead);
            });
        }
    }

    public class P_WardOnCast : ArcanePassive
    {
        public float amount = 5f;
        public override string Describe(int rank) => "AA_P_WardOnCast".Translate((amount * rank).ToString("F0")) + TagText;

        public override void SpellCast(CompArcane c, int rank, SpellContext ctx)
        {
            if (!TagMatch(ctx)) return;
            c.ward = Mathf.Min(Mathf.Max(c.WardMax, amount * rank), c.ward + amount * rank * c.SpellPower);
        }
    }

    public class P_HealOnCast : ArcanePassive
    {
        public float amount = 3f;
        public float radius;
        public override string Describe(int rank) => (radius > 0.5f ? "AA_P_HealAlliesOnCast" : "AA_P_HealOnCast").Translate((amount * rank).ToString("F0"), radius.ToString("F0")) + TagText;

        public override void SpellCast(CompArcane c, int rank, SpellContext ctx)
        {
            if (!TagMatch(ctx)) return;
            var p = c.Pawn;
            float heal = amount * rank * c.SpellPower;
            if (radius < 0.5f || !p.Spawned)
            {
                ArcaneUtility.HealPawn(p, heal);
                return;
            }
            foreach (var t in GenRadial.RadialDistinctThingsAround(p.Position, p.Map, radius, true).OfType<Pawn>())
                if (t == p || (t.Faction == p.Faction && !t.Dead)) ArcaneUtility.HealPawn(t, heal);
        }
    }

    public class P_CooldownOnKill : ArcanePassive
    {
        public int ticksPerRank = 60;
        public override string Describe(int rank) => "AA_P_CooldownOnKill".Translate((ticksPerRank * rank / 60f).ToString("F1"));

        public override void Kill(CompArcane c, int rank, Pawn victim, SpellContext ctx)
        {
            foreach (var ab in c.ArcaneAbilities())
                if (ab.OnCooldown) ab.StartCooldown(Mathf.Max(0, ab.CooldownTicksRemaining - ticksPerRank * rank));
        }
    }

    public enum PassiveTrigger
    {
        OnHitTaken,
        OnKill,
        OnSpellCast,
        OnWeaponHit,
        Periodic,
        OnSpellHit,
        OnDowned,
    }

    public enum PassiveTargetMode
    {
        Attacker,
        Victim,
        Self,
        NearestEnemy,
        RandomEnemy,
    }

    /// <summary>Casts a spell for free when something happens. Makes for very flexible keystones.</summary>
    public class P_TriggerSpell : ArcanePassive
    {
        public PassiveTrigger trigger = PassiveTrigger.OnKill;
        public AbilityDef spell;
        public float chancePerRank = 0.1f;
        public PassiveTargetMode target = PassiveTargetMode.Victim;
        public float searchRadius = 15f;
        public int interval = 600;
        public bool onlyInCombat = true;

        public override string Describe(int rank) => ("AA_Trigger_" + trigger).Translate(Pct(Mathf.Min(1f, chancePerRank * rank)), spell?.label ?? "?",
            (interval / 60f).ToString("F0")) + TagText + CdText;

        private void Fire(CompArcane c, int rank, Thing contextTarget)
        {
            if (spell == null || !Ready(c) || !Rand.Chance(chancePerRank * rank)) return;
            var p = c.Pawn;
            if (!p.Spawned || p.Downed) return;
            LocalTargetInfo t;
            switch (target)
            {
                case PassiveTargetMode.Self: t = p; break;
                case PassiveTargetMode.NearestEnemy: t = ArcaneUtility.NearestEnemy(p, searchRadius); break;
                case PassiveTargetMode.RandomEnemy: t = ArcaneUtility.RandomEnemy(p, searchRadius); break;
                default: t = contextTarget != null && (contextTarget.Spawned || contextTarget is Pawn) ? (contextTarget.Spawned ? new LocalTargetInfo(contextTarget) : new LocalTargetInfo(contextTarget.PositionHeld)) : LocalTargetInfo.Invalid; break;
            }
            if (!t.IsValid) return;
            Trigger(c);
            var cc = c;
            var tt = t;
            ArcaneVfxManager.For(p.Map).Schedule(1, () => SpellRunner.CastFree(cc, spell, tt));
        }

        public override void PostDamageTaken(CompArcane c, int rank, DamageInfo dinfo, float dealt)
        {
            if (trigger == PassiveTrigger.OnHitTaken && dinfo.Instigator != null && dinfo.Instigator != c.Pawn) Fire(c, rank, dinfo.Instigator);
        }

        public override void Kill(CompArcane c, int rank, Pawn victim, SpellContext ctx)
        {
            if (trigger == PassiveTrigger.OnKill && (ctx == null || ctx.def != spell) && (ctx == null || TagMatch(ctx))) Fire(c, rank, victim);
        }

        public override void SpellCast(CompArcane c, int rank, SpellContext ctx)
        {
            if (trigger == PassiveTrigger.OnSpellCast && !ctx.free && ctx.def != spell && TagMatch(ctx)) Fire(c, rank, null);
        }

        public override void SpellHit(CompArcane c, int rank, SpellContext ctx, Pawn t, float damage)
        {
            if (trigger == PassiveTrigger.OnSpellHit && !ctx.free && ctx.def != spell && TagMatch(ctx)) Fire(c, rank, t);
        }

        public override void WeaponHit(CompArcane c, int rank, Thing victim, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            if (trigger == PassiveTrigger.OnWeaponHit) Fire(c, rank, victim);
        }

        public override void Downed(CompArcane c, int rank)
        {
            if (trigger == PassiveTrigger.OnDowned) Fire(c, rank, c.Pawn);
        }

        public override void Tick(CompArcane c, int rank, int elapsed)
        {
            if (trigger != PassiveTrigger.Periodic) return;
            if (!c.PassiveReady(key + "_p")) return;
            c.StartPassiveCooldown(key + "_p", interval);
            if (onlyInCombat && Find.TickManager.TicksGame - c.lastCombatTick > 1200 && ArcaneUtility.NearestEnemy(c.Pawn, searchRadius) == null) return;
            Fire(c, rank, null);
        }
    }

    /// <summary>Applies a list of effects around the mage periodically (auras).</summary>
    public class P_Aura : ArcanePassive
    {
        public float radius = 5f;
        public int interval = 120;
        public TargetFilter affects = TargetFilter.Allies;
        public List<SpellEffect> effects = new List<SpellEffect>();
        public bool onlyInCombat;
        public bool onlyDrafted;
        public Color color = Color.white;
        public string descKey;

        public override string Describe(int rank)
        {
            string head = "AA_P_Aura".Translate(radius.ToString("F0"), (interval / 60f).ToString("F1"), ("AA_Filter_" + affects).Translate());
            var lines = new List<string> { head };
            foreach (var e in effects)
                foreach (var s in e.Describe(null)) lines.Add("  • " + s);
            if (!descKey.NullOrEmpty()) lines.Add(descKey.Translate());
            return string.Join("\n", lines);
        }

        public override void Tick(CompArcane c, int rank, int elapsed)
        {
            if (!c.PassiveReady(key + "_p")) return;
            c.StartPassiveCooldown(key + "_p", interval);
            var p = c.Pawn;
            if (!p.Spawned || p.Downed) return;
            if (onlyDrafted && !p.Drafted && p.Faction == Faction.OfPlayer) return;
            if (onlyInCombat && Find.TickManager.TicksGame - c.lastCombatTick > 900) return;
            var ctx = new SpellContext
            {
                caster = p, arcane = c, map = p.Map, mods = new SpellModSummary(), basePower = c.SpellPower * rank, healStat = p.GetStatValue(ArcaneDefOf.AA_HealingPower),
                durationStat = p.GetStatValue(ArcaneDefOf.AA_DurationFactor), areaStat = 1f,
                props = new CompProperties_ArcaneSpell { affects = affects, color = color, color2 = color },
            };
            var info = new ImpactInfo { cell = p.Position, pos = p.DrawPos, radius = radius, sourcePos = p.DrawPos };
            SpellRunner.Impact(ctx, info, effects, playVfx: false);
            ArcaneVfxManager.For(p.Map).Add(new VfxSprite
            {
                mat = ArcaneMats.Ring, follow = p, pos = p.DrawPos, scaleStart = 1f, scaleEnd = radius * 2f, duration = 35, color = color * new Color(1, 1, 1, 0.5f),
                fadeIn = 0.05f, fadeOut = 0.8f, layer = AltitudeLayer.MoteLow,
            });
        }
    }

    /// <summary>Flat stat bonus that only applies while a condition holds is handled elsewhere; this one converts one stat into another.</summary>
    public class P_StatScaling : ArcanePassive
    {
        public StatDef stat;
        public float perLevel;
        public float perAttributePoint;
        public ArcaneAttributeDef attribute;

        public override string Describe(int rank)
        {
            if (attribute != null) return "AA_P_StatPerAttr".Translate(stat?.label ?? "?", (perAttributePoint * rank).ToString("0.###"), attribute.label);
            return "AA_P_StatPerLevel".Translate(stat?.label ?? "?", (perLevel * rank).ToString("0.###"));
        }

        public override void ModifyStats(CompArcane c, int rank, Action<StatDef, float> addOffset, Action<StatDef, float> addPercent)
        {
            if (stat == null) return;
            float v = perLevel * c.level * rank;
            if (attribute != null) v += perAttributePoint * c.Attribute(attribute) * rank;
            addOffset(stat, v);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace ArcanumAscendant
{
    /// <summary>Tracks pawns that were turned into critters. Originals wait as world pawns.</summary>
    public class PolymorphTracker : IExposable
    {
        private List<Entry> entries = new List<Entry>();

        public class Entry : IExposable
        {
            public Pawn original;
            public Pawn critter;
            public int ticksLeft;
            public Faction originalFaction;

            public void ExposeData()
            {
                Scribe_References.Look(ref original, "original", true);
                Scribe_References.Look(ref critter, "critter");
                Scribe_Values.Look(ref ticksLeft, "ticksLeft");
                Scribe_References.Look(ref originalFaction, "originalFaction");
            }
        }

        public void Polymorph(Pawn p, PawnKindDef kind, int ticks, SpellContext ctx)
        {
            var map = p.Map;
            IntVec3 pos = p.Position;
            var critter = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, null, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true, canGeneratePawnRelations: false));
            var e = new Entry { original = p, critter = critter, ticksLeft = ticks, originalFaction = p.Faction };
            p.carryTracker?.TryDropCarriedThing(pos, ThingPlaceMode.Near, out _);
            p.DeSpawn();
            if (!Find.WorldPawns.Contains(p)) Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
            GenSpawn.Spawn(critter, pos, map);
            critter.Name = new NameSingle(p.LabelShort);
            entries.Add(e);
            ArcaneFx.Flash(map, critter.DrawPos, ctx?.Color ?? Color.magenta, 3f);
            ArcaneFx.Particles(map, critter.DrawPos, ctx?.Color2 ?? Color.white, ArcaneDefOf.AA_Fleck_Spark, 16, 2.5f, 0.5f);
            ArcaneFx.Particles(map, critter.DrawPos, Color.white, ArcaneDefOf.AA_Fleck_Smoke, 4, 0.6f, 1f);
            MoteMaker.ThrowText(critter.DrawPos, map, "AA_Polymorphed".Translate(kind.label), ctx?.Color ?? Color.magenta, 3f);
        }

        public bool IsCritter(Pawn p) => entries.Any(e => e.critter == p);

        public void Tick(Map map)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                e.ticksLeft--;
                if (e.original == null)
                {
                    entries.RemoveAt(i);
                    continue;
                }
                if (e.critter == null || e.critter.Dead || e.critter.Destroyed)
                {
                    var corpse = e.critter?.Corpse;
                    IntVec3 pos = corpse != null && corpse.Spawned ? corpse.Position : (e.critter?.PositionHeld ?? IntVec3.Invalid);
                    corpse?.Destroy();
                    RestoreOriginal(e, map, pos, kill: true);
                    entries.RemoveAt(i);
                    continue;
                }
                if (e.ticksLeft <= 0 && e.critter.Spawned)
                {
                    IntVec3 pos = e.critter.Position;
                    e.critter.Destroy(DestroyMode.Vanish);
                    RestoreOriginal(e, map, pos, kill: false);
                    entries.RemoveAt(i);
                }
            }
        }

        private void RestoreOriginal(Entry e, Map map, IntVec3 pos, bool kill)
        {
            var p = e.original;
            if (p.Spawned) return;
            if (!pos.IsValid || !pos.InBounds(map)) pos = CellFinder.RandomClosewalkCellNear(map.Center, map, 10);
            if (Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);
            if (p.Dead) return;
            GenSpawn.Spawn(p, pos, map);
            ArcaneFx.Flash(map, p.DrawPos, Color.magenta, 2.5f);
            ArcaneFx.Particles(map, p.DrawPos, Color.white, ArcaneDefOf.AA_Fleck_Smoke, 4, 0.6f, 1f);
            if (kill)
            {
                p.Kill(new DamageInfo(ArcaneDefOf.AA_ArcaneTrue, 9999f));
                return;
            }
            if (p.Faction != null && p.Faction != Faction.OfPlayer && p.Faction.HostileTo(Faction.OfPlayer))
                LordMaker.MakeNewLord(p.Faction, new LordJob_AssaultColony(p.Faction, false, false, false, false, false, false, false), map, new[] { p });
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref entries, "entries", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                entries ??= new List<Entry>();
                entries.RemoveAll(e => e == null || e.original == null);
            }
        }
    }

    public class CompProperties_ArcaneConstruct : CompProperties
    {
        public int defaultLifetimeTicks = 2500;
        public float damageScale = 1f;
        public List<SpellEffect> pulseEffects;
        public int pulseInterval = 120;
        public float pulseRadius = 4f;
        public TargetFilter pulseAffects = TargetFilter.Enemies;
        public Color glowColor = Color.white;
        public float glowSize = 2f;

        public CompProperties_ArcaneConstruct()
        {
            compClass = typeof(CompArcaneConstruct);
        }
    }

    /// <summary>Temporary conjured structure (walls, turrets, totems). Scales with caster power.</summary>
    public class CompArcaneConstruct : ThingComp
    {
        public int ticksLeft = -1;
        public float power = 1f;
        public Pawn caster;
        public AbilityDef sourceSpell;
        private int age;

        public CompProperties_ArcaneConstruct Props => (CompProperties_ArcaneConstruct)props;

        public void Setup(SpellContext ctx, int lifetime)
        {
            caster = ctx.caster;
            sourceSpell = ctx.def;
            power = ctx.basePower * Mathf.Max(0.2f, ctx.SummonFactor) * Mathf.Max(0.1f, 1f + ctx.mods.damage);
            ticksLeft = lifetime > 0 ? lifetime : Props.defaultLifetimeTicks;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && ticksLeft < 0) ticksLeft = Props.defaultLifetimeTicks;
        }

        public override void CompTick()
        {
            base.CompTick();
            Step(1);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            Step(250);
        }

        private void Step(int ticks)
        {
            if (!parent.Spawned) return;
            age += ticks;
            if (ticksLeft > 0)
            {
                ticksLeft -= ticks;
                if (ticksLeft <= 0)
                {
                    var map = parent.Map;
                    ArcaneFx.Particles(map, parent.DrawPos, Props.glowColor, ArcaneDefOf.AA_Fleck_Shard, 8, 2f, 0.5f);
                    ArcaneFx.Particles(map, parent.DrawPos, Props.glowColor, ArcaneDefOf.AA_Fleck_Smoke, 3, 0.5f, 1f);
                    parent.Destroy(DestroyMode.Vanish);
                    return;
                }
            }
            if (!Props.pulseEffects.NullOrEmpty() && age % Props.pulseInterval < ticks && sourceSpell != null)
            {
                var ctx = caster != null ? SpellContext.For(caster, sourceSpell) : new SpellContext { def = sourceSpell, mods = new SpellModSummary() };
                ctx.props ??= sourceSpell.comps?.OfType<CompProperties_ArcaneSpell>().FirstOrDefault();
                if (ctx.props == null) return;
                ctx.map = parent.Map;
                ctx.basePower = power;
                var info = new ImpactInfo { cell = parent.Position, pos = parent.DrawPos, radius = Props.pulseRadius, sourcePos = parent.DrawPos };
                foreach (var e in Props.pulseEffects) if (e.affects == TargetFilter.Inherit) e.affects = Props.pulseAffects;
                SpellRunner.Impact(ctx, info, Props.pulseEffects, playVfx: false);
                ArcaneVfxManager.For(parent.Map).Add(new VfxSprite
                {
                    mat = ArcaneMats.Shock, pos = parent.DrawPos, scaleStart = 0.5f, scaleEnd = Props.pulseRadius * 2f, duration = 30, color = Props.glowColor,
                    fadeIn = 0.02f, fadeOut = 0.8f, layer = AltitudeLayer.MoteLow,
                });
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (Props.glowSize <= 0f) return;
            Vector3 p = parent.DrawPos;
            p.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.realtimeSinceStartup * 3f + parent.thingIDNumber);
            ArcaneMats.DrawQuad(ArcaneMats.Glow, p, Props.glowSize * pulse, 0f, Props.glowColor * new Color(1, 1, 1, 0.5f));
        }

        public override string CompInspectStringExtra()
        {
            return ticksLeft > 0 ? "AA_DissipatesIn".Translate(ticksLeft.ToStringTicksToPeriod()).ToString() : null;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", -1);
            Scribe_Values.Look(ref power, "power", 1f);
            Scribe_Values.Look(ref age, "age");
            Scribe_References.Look(ref caster, "caster");
            Scribe_Defs.Look(ref sourceSpell, "sourceSpell");
        }
    }

    /// <summary>Projectile fired by conjured turrets; damage scales with the construct's power. Draws a glow.</summary>
    public class Projectile_Arcane : Bullet
    {
        public override int DamageAmount
        {
            get
            {
                float p = 1f;
                var con = (launcher as ThingWithComps)?.GetComp<CompArcaneConstruct>();
                if (con != null) p = con.power * con.Props.damageScale;
                return Mathf.Max(1, Mathf.RoundToInt(base.DamageAmount * p));
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var ext = def.GetModExtension<ProjectileGlowExtension>();
            Color c = ext?.color ?? Color.white;
            Vector3 p = drawLoc;
            p.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            ArcaneMats.DrawQuad(ArcaneMats.Glow, p, ext?.glowSize ?? 1.5f, 0f, c * new Color(1, 1, 1, 0.7f));
            base.DrawAt(drawLoc, flip);
        }

        protected override void Tick()
        {
            base.Tick();
            if (Spawned && this.IsHashIntervalTick(3))
            {
                var ext = def.GetModExtension<ProjectileGlowExtension>();
                if (ext?.trailFleck != null) ArcaneFx.Particles(Map, DrawPos, ext.color, ext.trailFleck, 1, 0.3f, 0.5f);
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            var ext = def.GetModExtension<ProjectileGlowExtension>();
            var map = Map;
            Vector3 pos = DrawPos;
            if (def.projectile.explosionRadius > 0f && map != null && !blockedByShield)
            {
                IntVec3 cell = Position;
                var ignored = new List<Thing>();
                if (launcher != null)
                {
                    ignored.Add(launcher);
                    foreach (var t in GenRadial.RadialDistinctThingsAround(cell, map, def.projectile.explosionRadius, true))
                        if (t.Faction != null && launcher.Faction != null && !t.Faction.HostileTo(launcher.Faction)) ignored.Add(t);
                }
                GenExplosion.DoExplosion(cell, map, def.projectile.explosionRadius, def.projectile.damageDef, launcher, DamageAmount, ArmorPenetration,
                    null, equipmentDef, def, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, ignored);
                base.Impact(null, false);
            }
            else base.Impact(hitThing, blockedByShield);
            if (map != null && ext != null)
            {
                ArcaneFx.Flash(map, pos, ext.color, 1.6f);
                ArcaneFx.Particles(map, pos, ext.color, ArcaneDefOf.AA_Fleck_Spark, 4, 1.5f, 0.4f);
                if (ext.impactHediff != null && hitThing is Pawn hp && !hp.Dead && Rand.Chance(ext.impactHediffChance))
                    ArcaneUtility.ApplyHediff(hp, ext.impactHediff, ext.impactHediffSeverity, -1, null);
            }
        }
    }

    public class ProjectileGlowExtension : DefModExtension
    {
        public Color color = Color.white;
        public float glowSize = 1.5f;
        public FleckDef trailFleck;
        public HediffDef impactHediff;
        public float impactHediffChance = 1f;
        public float impactHediffSeverity = 0.5f;
    }

    /// <summary>
    /// Reading a grimoire begins attunement: the reader must then study at an arcane lectern until their magic wakes.
    /// With "free awakening" on, it awakens them immediately instead.
    /// </summary>
    public class CompUseEffect_Awaken : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            var c = usedBy.TryGetComp<CompArcane>();
            if (c == null) return;
            if (usedBy.Faction != Faction.OfPlayer)
            {
                c.Awaken(DefDatabase<ArcaneClassDef>.AllDefs.Where(d => !d.IsSubclass).RandomElement());
                return;
            }
            if (ArcanumMod.Settings.freeAwakening)
            {
                Find.WindowStack.Add(new Dialog_ChooseClass(c, mustChoose: true));
                return;
            }
            c.StartAttunement();
            if (usedBy.Spawned) ArcaneFx.LevelUpBurst(usedBy, new Color(0.75f, 0.55f, 1f), big: false);
            Find.LetterStack.ReceiveLetter("AA_AttunementBegunLabel".Translate(usedBy.LabelShort, usedBy.Named("PAWN")),
                "AA_AttunementBegunText".Translate(usedBy.LabelShort, CompArcane.AttunementHours.ToString("F0"), CompArcane.StudyFactor(usedBy).ToStringPercent(), usedBy.Named("PAWN")),
                LetterDefOf.PositiveEvent, usedBy);
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            var c = p.TryGetComp<CompArcane>();
            if (c == null || !p.RaceProps.Humanlike) return "AA_CannotAwaken".Translate();
            if (c.IsMage) return "AA_AlreadyAwakened".Translate();
            if (c.attuning || c.awakenReady) return "AA_AlreadyAttuning".Translate(p.LabelShort, p.Named("PAWN"));
            var ok = CompArcane.CanAttune(p);
            if (!ok.Accepted) return ok;
            return base.CanBeUsedBy(p);
        }
    }

    /// <summary>Tome of unmaking: refunds every perk and attribute point.</summary>
    public class CompUseEffect_Respec : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            var c = usedBy.TryGetComp<CompArcane>();
            if (c == null || !c.IsMage) return;
            c.Respec();
            if (usedBy.Spawned) ArcaneFx.LevelUpBurst(usedBy, c.DisplayClass.color, false);
            Messages.Message("AA_Respecced".Translate(usedBy.LabelShort, usedBy.Named("PAWN")), usedBy, MessageTypeDefOf.PositiveEvent);
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            var c = p.TryGetComp<CompArcane>();
            if (c == null || !c.IsMage) return "AA_NotAMage".Translate();
            return base.CanBeUsedBy(p);
        }
    }

    /// <summary>Mana potion: restores mana when ingested.</summary>
    public class IngestionOutcomeDoer_RestoreMana : IngestionOutcomeDoer
    {
        public float amount = 100f;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            var c = pawn.TryGetComp<CompArcane>();
            if (c == null || !c.IsMage) return;
            c.mana = Mathf.Min(c.ManaMax, c.mana + amount * ingestedCount);
            if (pawn.Spawned)
            {
                ArcaneFx.Particles(pawn.Map, pawn.DrawPos, new Color(0.3f, 0.6f, 1f), ArcaneDefOf.AA_Fleck_Spark, 10, 1.5f, 0.5f);
                ArcaneFx.Text(pawn, "+" + amount.ToString("F0") + " " + "AA_ManaWord".Translate(), new Color(0.4f, 0.7f, 1f));
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArcanumAscendant
{
    // ================================================================================================
    // Effects that work on the colony itself: its kitchens, fires, power, buildings, stores and land.
    // All of them only ever touch the caster's own buildings (or ownerless things) unless stated otherwise.
    // ================================================================================================

    public static class HearthUtil
    {
        public static IEnumerable<Thing> ThingsIn(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var seen = new HashSet<Thing>();
            foreach (var c in info.Cells(map))
                foreach (var t in c.GetThingList(map).ToList())
                    if (seen.Add(t)) yield return t;
        }

        public static bool Ours(Thing t, SpellContext ctx) => t.Faction == null || (ctx.caster != null && t.Faction == ctx.caster.Faction);

        public static float Power(SpellContext ctx) => Mathf.Clamp(Mathf.Sqrt(Mathf.Max(0.25f, ctx.HealFactor)), 0.5f, 3f);

        public static void Note(SpellContext ctx, ImpactInfo info, string text)
        {
            if (ctx.map != null) MoteMaker.ThrowText(info.pos, ctx.map, text, ctx.Color, 4f);
        }

        public static void Puff(SpellContext ctx, Vector3 pos, FleckDef fleck = null, int n = 4)
        {
            if (ctx.map == null) return;
            ArcaneFx.Particles(ctx.map, pos, ctx.Color, fleck ?? ArcaneDefOf.AA_Fleck_Spark, n, 1.2f, 0.5f);
        }

        public static void PlaceNear(Thing t, IntVec3 c, Map map)
        {
            if (!GenPlace.TryPlaceThing(t, c, map, ThingPlaceMode.Near)) t.Destroy();
        }

        public static void Spawn(ThingDef def, int count, IntVec3 c, Map map)
        {
            while (count > 0 && def != null)
            {
                var t = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
                int n = Mathf.Min(count, def.stackLimit);
                t.stackCount = n;
                count -= n;
                PlaceNear(t, c, map);
            }
        }
    }

    /// <summary>Cooks the raw food lying in an area into meals.</summary>
    public class Effect_CookFood : SpellEffect
    {
        public int maxMeals = 10;
        public bool fineMeals = true;
        public bool lavishMeals;
        public bool allowHumanMeat;

        public override bool AppliesToPawns => false;

        private bool IsRaw(ThingDef d)
        {
            var ing = d.ingestible;
            if (ing == null || !d.IsNutritionGivingIngestible || d.IsCorpse || ing.IsMeal) return false;
            if (ing.drugCategory != DrugCategory.None) return false;
            if (ing.preferability != FoodPreferability.RawBad && ing.preferability != FoodPreferability.RawTasty) return false;
            if (!allowHumanMeat && (ing.sourceDef?.race?.Humanlike ?? false)) return false;
            return (ing.foodType & (FoodTypeFlags.Meat | FoodTypeFlags.VegetableOrFruit | FoodTypeFlags.AnimalProduct)) != 0;
        }

        private static bool IsMeatSide(ThingDef d) => (d.ingestible.foodType & (FoodTypeFlags.Meat | FoodTypeFlags.AnimalProduct)) != 0;

        private static float Take(List<Thing> pool, float need, HashSet<ThingDef> used)
        {
            float got = 0f;
            foreach (var t in pool)
            {
                if (got >= need - 0.001f) break;
                if (t.Destroyed || t.stackCount <= 0) continue;
                float per = t.def.ingestible.CachedNutrition;
                if (per <= 0f) continue;
                int units = Mathf.Min(t.stackCount, Mathf.CeilToInt((need - got) / per - 0.001f));
                if (units <= 0) continue;
                used.Add(t.def);
                got += units * per;
                if (units >= t.stackCount) t.Destroy();
                else t.SplitOff(units).Destroy();
            }
            return got;
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var meat = new List<Thing>();
            var veg = new List<Thing>();
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (t.def.category != ThingCategory.Item || !IsRaw(t.def)) continue;
                if (t.TryGetComp<CompRottable>() is CompRottable r && r.Stage != RotStage.Fresh) continue;
                (IsMeatSide(t.def) ? meat : veg).Add(t);
            }
            float Total(List<Thing> l) => l.Where(t => !t.Destroyed).Sum(t => t.stackCount * t.def.ingestible.CachedNutrition);
            int budget = Mathf.RoundToInt(maxMeals * HearthUtil.Power(ctx));
            var lavishDef = DefDatabase<ThingDef>.GetNamedSilentFail("MealLavish");
            var made = new Dictionary<ThingDef, int>();
            var used = new HashSet<ThingDef>();
            for (int guard = 0; guard < 200 && budget > 0; guard++)
            {
                float m = Total(meat), v = Total(veg);
                ThingDef meal;
                if (lavishMeals && lavishDef != null && m >= 0.5f && v >= 0.5f)
                {
                    Take(meat, 0.5f, used);
                    Take(veg, 0.5f, used);
                    meal = lavishDef;
                }
                else if (fineMeals && m >= 0.25f && v >= 0.25f)
                {
                    Take(meat, 0.25f, used);
                    Take(veg, 0.25f, used);
                    meal = ThingDefOf.MealFine;
                }
                else if (m + v >= 0.5f)
                {
                    float got = Take(m >= v ? meat : veg, 0.5f, used);
                    if (got < 0.5f) Take(m >= v ? veg : meat, 0.5f - got, used);
                    meal = ThingDefOf.MealSimple;
                }
                else break;
                made[meal] = made.TryGetValue(meal) + 1;
                budget--;
            }
            if (made.Count == 0)
            {
                HearthUtil.Note(ctx, info, "AA_NoRawFood".Translate());
                return;
            }
            foreach (var kv in made)
            {
                int n = kv.Value;
                while (n > 0)
                {
                    var t = ThingMaker.MakeThing(kv.Key);
                    t.stackCount = Mathf.Min(n, kv.Key.stackLimit);
                    n -= t.stackCount;
                    var ingr = t.TryGetComp<CompIngredients>();
                    if (ingr != null)
                        foreach (var d in used.Take(3)) ingr.RegisterIngredient(d);
                    HearthUtil.PlaceNear(t, info.cell, map);
                }
            }
            ArcaneFx.Flash(map, info.pos, ctx.Color, 3f);
            ArcaneFx.Particles(map, info.pos, ctx.Color2, ArcaneDefOf.AA_Fleck_Ember, 16, 2f, 0.6f);
            HearthUtil.Note(ctx, info, "AA_MealsCooked".Translate(made.Values.Sum()));
            ctx.arcane?.GainXp(4f * made.Values.Sum());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_CookFood".Translate(Mathf.RoundToInt(maxMeals * (ctx != null ? HearthUtil.Power(ctx) : 1f)));
            if (lavishMeals) yield return "AA_Desc_CookLavish".Translate();
        }
    }

    public enum FuelKind
    {
        /// <summary>Wood, chemfuel and other things that burn: campfires, torches, generators, stoves.</summary>
        Fire,
        /// <summary>Turret barrels and other rearmable weapons.</summary>
        Turret,
        Any,
    }

    /// <summary>Refuels the caster's buildings in an area.</summary>
    public class Effect_Refuel : SpellEffect
    {
        public FuelKind kind = FuelKind.Fire;
        public float fraction = 1f;

        public override bool AppliesToPawns => false;

        private bool Matches(CompRefuelable comp, Thing t)
        {
            var f = comp.Props.fuelFilter;
            switch (kind)
            {
                case FuelKind.Fire:
                    return f != null && (f.Allows(ThingDefOf.WoodLog) || f.Allows(ThingDefOf.Chemfuel)) && !(t is Building_Turret);
                case FuelKind.Turret:
                    return t is Building_Turret;
                default:
                    return true;
            }
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (!(t is ThingWithComps tw) || !HearthUtil.Ours(t, ctx)) continue;
                var comp = tw.GetComp<CompRefuelable>();
                if (comp == null || !Matches(comp, t)) continue;
                float target = Mathf.Max(comp.TargetFuelLevel, 1f);
                float missing = target - comp.Fuel;
                if (missing <= 0.01f) continue;
                comp.Refuel(missing * Mathf.Clamp01(fraction * HearthUtil.Power(ctx)));
                n++;
                HearthUtil.Puff(ctx, t.DrawPos, kind == FuelKind.Fire ? ArcaneDefOf.AA_Fleck_Ember : ArcaneDefOf.AA_Fleck_Gear, 6);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Refueled".Translate(n) : "AA_NothingToRefuel".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return ("AA_Desc_Refuel_" + kind).Translate();
        }
    }

    /// <summary>Melts the snow over a wide area, warms it and lifts hypothermia from the people standing in it.</summary>
    public class Effect_Thaw : SpellEffect
    {
        public float heat = 200f;
        public float hypothermiaCure = 0.35f;

        public Effect_Thaw()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int melted = 0;
            foreach (var c in info.Cells(map))
            {
                if (map.snowGrid.GetDepth(c) <= 0.001f) continue;
                map.snowGrid.SetDepth(c, 0f);
                melted++;
                if (Rand.Chance(0.05f)) FleckMaker.ThrowSmoke(c.ToVector3Shifted(), map, 1.2f);
            }
            if (heat > 0f) GenTemperature.PushHeat(info.cell, map, heat * HearthUtil.Power(ctx));
            if (melted > 0) HearthUtil.Note(ctx, info, "AA_SnowMelted".Translate());
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var h = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
            if (h == null) return;
            h.Severity -= hypothermiaCure * HearthUtil.Power(ctx);
            if (h.Severity <= 0.01f) p.health.RemoveHediff(h);
            ArcaneFx.Text(p, "AA_Warmed".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Thaw".Translate();
        }
    }

    /// <summary>Cremates the corpses in an area. The living who watch feel the dead were honoured.</summary>
    public class Effect_Cremate : SpellEffect
    {
        public ThoughtDef mournerThought;
        public float mournRadius = 14f;
        public int maxCorpses = 12;

        public override bool AppliesToPawns => false;

        private static readonly string[] CorpseThoughts = { "ObservedLayingCorpse", "ObservedLayingRottingCorpse" };

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int burned = 0;
            bool humanlike = false;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (burned >= maxCorpses) break;
                if (!(t is Corpse corpse) || corpse.InnerPawn == null) continue;
                var p = corpse.InnerPawn;
                if (p.RaceProps.IsMechanoid) continue;
                var pos = corpse.Position;
                p.equipment?.GetDirectlyHeldThings()?.TryDropAll(pos, map, ThingPlaceMode.Near);
                p.inventory?.innerContainer?.TryDropAll(pos, map, ThingPlaceMode.Near);
                if (p.RaceProps.Humanlike) humanlike = true;
                ArcaneVfxManager.For(map).Add(new VfxPillar(corpse.DrawPos, 50, ctx.Color, ctx.Color2, 1.2f));
                ArcaneFx.Particles(map, corpse.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Ember, 10, 1.5f, 0.6f);
                corpse.Destroy();
                FilthMaker.TryMakeFilth(pos, map, ThingDefOf.Filth_Ash, 2);
                burned++;
            }
            if (burned == 0)
            {
                HearthUtil.Note(ctx, info, "AA_NoCorpses".Translate());
                return;
            }
            HearthUtil.Note(ctx, info, "AA_Cremated".Translate(burned));
            if (!humanlike) return;
            var corpseDefs = CorpseThoughts.Select(n => DefDatabase<ThoughtDef>.GetNamedSilentFail(n)).Where(d => d != null).ToList();
            foreach (var p in map.mapPawns.FreeColonistsSpawned)
            {
                if (p.Position.DistanceTo(info.cell) > mournRadius) continue;
                var mem = p.needs?.mood?.thoughts?.memories;
                if (mem == null) continue;
                foreach (var d in corpseDefs) mem.RemoveMemoriesOfDef(d);
                if (mournerThought != null) mem.TryGainMemory(mournerThought);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Cremate".Translate(maxCorpses);
        }
    }

    /// <summary>Every fire in the area explodes and is consumed. Only the caster's enemies are hurt.</summary>
    public class Effect_Flashover : SpellEffect
    {
        public float baseDamage = 10f;
        public float damagePerFireSize = 22f;
        public float blastRadius = 1.6f;
        public int maxFires = 24;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var fires = new List<Fire>();
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (t is Fire f) fires.Add(f);
                else if (t is Pawn pw && pw.GetAttachment(ThingDefOf.Fire) is Fire af) fires.Add(af);
            }
            int n = 0;
            foreach (var f in fires.Distinct().OrderByDescending(f => f.fireSize))
            {
                if (n >= maxFires || f.Destroyed) continue;
                n++;
                var pos = f.parent is Pawn host && host.Spawned ? host.Position : f.Position;
                float size = f.fireSize;
                float r = blastRadius + size * 0.6f;
                f.Destroy();
                float dmg = (baseDamage + damagePerFireSize * size) * ctx.DamageFactor;
                foreach (var t in GenRadial.RadialDistinctThingsAround(pos, map, r, true).ToList())
                {
                    if (!(t is Pawn p) || p.Dead || !ctx.IsEnemy(p)) continue;
                    var dinfo = new DamageInfo(DamageDefOf.Flame, dmg, 0.2f, -1f, ctx.caster);
                    bool prev = CompArcane.applyingSpellDamage;
                    CompArcane.applyingSpellDamage = true;
                    try { p.TakeDamage(dinfo); }
                    finally { CompArcane.applyingSpellDamage = prev; }
                }
                ArcaneFx.Eruption(map, pos.ToVector3Shifted(), ctx.Color, ctx.Color2, r);
                ArcaneFx.Flash(map, pos.ToVector3Shifted(), ctx.Color, r * 1.5f);
            }
            if (n == 0) HearthUtil.Note(ctx, info, "AA_NoFires".Translate());
            else ArcaneFx.Shake(map, Mathf.Min(3f, 0.4f + n * 0.15f));
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Flashover".Translate(baseDamage.ToString("F0"), damagePerFireSize.ToString("F0"));
        }
    }

    /// <summary>Raises a steam geyser out of solid ground.</summary>
    public class Effect_SpawnGeyser : SpellEffect
    {
        public float minDistanceToOther = 9f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var def = ThingDefOf.SteamGeyser;
            if (def == null) return;
            var rect = GenAdj.OccupiedRect(info.cell, Rot4.North, def.size);
            foreach (var c in rect)
            {
                if (!c.InBounds(map) || c.GetEdifice(map) != null || !c.Walkable(map) || c.GetTerrain(map).IsWater || c.GetTerrain(map).BuildableByPlayer
                    || c.GetFirstItem(map) != null || c.GetFirstPawn(map) != null || map.roofGrid.RoofAt(c)?.isThickRoof == true)
                {
                    HearthUtil.Note(ctx, info, "AA_GeyserBlocked".Translate());
                    return;
                }
            }
            foreach (var g in map.listerThings.ThingsOfDef(def))
                if (g.Position.DistanceTo(info.cell) < minDistanceToOther)
                {
                    HearthUtil.Note(ctx, info, "AA_GeyserTooClose".Translate());
                    return;
                }
            foreach (var c in rect) map.terrainGrid.SetTerrain(c, TerrainDefOf.Gravel ?? c.GetTerrain(map));
            var geyser = GenSpawn.Spawn(ThingMaker.MakeThing(def), info.cell, map);
            ArcaneFx.Eruption(map, geyser.TrueCenter(), ctx.Color, ctx.Color2, 3f);
            ArcaneFx.Shake(map, 3f);
            for (int i = 0; i < 8; i++) FleckMaker.ThrowSmoke(geyser.TrueCenter() + Gen.RandomHorizontalVector(1.5f), map, 2.5f);
            Messages.Message("AA_GeyserRaised".Translate(), new LookTargets(geyser), MessageTypeDefOf.PositiveEvent);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Geyser".Translate();
        }
    }

    /// <summary>Burns away waste: toxic wastepacks, filth and spoiled food. Nothing is left, not even pollution.</summary>
    public class Effect_Incinerate : SpellEffect
    {
        public bool wastepacks = true;
        public bool filth = true;
        public bool rotten = true;
        public int maxThings = 200;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int n = 0;
            var waste = ModsConfig.BiotechActive ? ThingDefOf.Wastepack : null;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (n >= maxThings) break;
                bool burn = false;
                if (wastepacks && waste != null && t.def == waste) burn = true;
                else if (filth && t is Filth) burn = true;
                else if (rotten && t.def.category == ThingCategory.Item && t.TryGetComp<CompRottable>() is CompRottable r && r.Stage != RotStage.Fresh) burn = true;
                else if (rotten && t is Corpse c && c.GetRotStage() != RotStage.Fresh && !(c.InnerPawn?.IsColonist ?? false)) burn = true;
                if (!burn) continue;
                if (Rand.Chance(0.35f)) ArcaneFx.Particles(map, t.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Ember, 3, 1f, 0.5f);
                t.Destroy();
                n++;
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Incinerated".Translate(n) : "AA_NothingToBurn".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Incinerate".Translate();
        }
    }

    /// <summary>Heats an enemy's gear past its limits: flammable clothing ignites, and explosives they carry go off.</summary>
    public class Effect_CookOff : SpellEffect
    {
        public float explosionDamage = 28f;
        public float explosionRadius = 2.4f;
        public float flammabilityToIgnite = 0.5f;

        private static bool Explosive(Thing t)
        {
            if (t?.def == null) return false;
            if (t.def == ThingDefOf.Chemfuel) return true;
            if (t.def.IsShell) return true;
            if (t.def.Verbs != null)
                foreach (var v in t.def.Verbs)
                    if ((v.defaultProjectile?.projectile?.explosionRadius ?? 0f) > 0.5f) return true;
            return false;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            if (!p.Spawned) return;
            var map = p.Map;
            bool any = false;
            if (p.apparel != null && p.apparel.WornApparel.Any(a => a.GetStatValue(StatDefOf.Flammability) >= flammabilityToIgnite))
            {
                FireUtility.TryAttachFire(p, 0.6f, ctx.caster);
                any = true;
            }
            var bombs = new List<Thing>();
            if (Explosive(p.equipment?.Primary)) bombs.Add(p.equipment.Primary);
            if (p.inventory != null) bombs.AddRange(p.inventory.innerContainer.Where(Explosive));
            if (bombs.Count > 0)
            {
                var pos = p.Position;
                float dmg = explosionDamage * ctx.DamageFactor * Mathf.Min(3f, bombs.Count);
                foreach (var b in bombs)
                {
                    if (b == p.equipment?.Primary) p.equipment.DestroyEquipment((ThingWithComps)b);
                    else if (!b.Destroyed) b.Destroy();
                }
                ArcaneFx.Text(p, "AA_CookedOff".Translate(), ctx.Color);
                GenExplosion.DoExplosion(pos, map, explosionRadius, DamageDefOf.Bomb, ctx.caster, Mathf.RoundToInt(dmg), 0.3f);
                any = true;
            }
            if (!any) ArcaneFx.Particles(map, p.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Ember, 4, 1f, 0.4f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_CookOff".Translate();
        }
    }

    /// <summary>Hatches every fertilized egg in the area.</summary>
    public class Effect_HatchEggs : SpellEffect
    {
        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (!(t is ThingWithComps tw)) continue;
                var h = tw.GetComp<CompHatcher>();
                if (h == null) continue;
                int stack = t.stackCount;
                var pos = t.DrawPos;
                h.Hatch();
                n += stack;
                HearthUtil.Puff(ctx, pos, ArcaneDefOf.AA_Fleck_Feather, 4);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_EggsHatched".Translate(n) : "AA_NoEggs".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_HatchEggs".Translate();
        }
    }

    /// <summary>Smelts loose weapons and apparel back into their materials, as a perfect smelter would.</summary>
    public class Effect_Smelt : SpellEffect
    {
        public float efficiency = 1f;
        public QualityCategory maxQuality = QualityCategory.Good;
        public int maxItems = 40;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (n >= maxItems) break;
                if (t.def.category != ThingCategory.Item || !t.def.smeltable || !(t is Apparel || t.def.IsWeapon)) continue;
                if (t.TryGetComp<CompQuality>() is CompQuality q && q.Quality > maxQuality) continue;
                var pos = t.Position;
                List<Thing> products;
                try { products = t.SmeltProducts(efficiency).ToList(); }
                catch { continue; }
                t.Destroy();
                foreach (var pr in products) HearthUtil.PlaceNear(pr, pos, map);
                n++;
                ArcaneFx.Particles(map, pos.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Ember, 4, 1.2f, 0.5f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Smelted".Translate(n) : "AA_NothingToSmelt".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Smelt".Translate(maxQuality.GetLabel());
        }
    }

    /// <summary>Turns a number of one thing into another (steel into components, herbs into medicine...).</summary>
    public class Effect_Convert : SpellEffect
    {
        public List<ThingDef> from = new List<ThingDef>();
        public ThingDef to;
        public float inputPerOutput = 10f;
        public int maxOutput = 10;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (to == null || from.NullOrEmpty()) return;
            var map = ctx.map;
            var stacks = HearthUtil.ThingsIn(ctx, info).Where(t => from.Contains(t.def) && t.def.category == ThingCategory.Item).ToList();
            int total = stacks.Sum(t => t.stackCount);
            int cap = Mathf.RoundToInt(maxOutput * HearthUtil.Power(ctx));
            int outputs = Mathf.Min(cap, Mathf.FloorToInt(total / inputPerOutput));
            if (outputs <= 0)
            {
                HearthUtil.Note(ctx, info, "AA_NotEnoughToConvert".Translate(Mathf.CeilToInt(inputPerOutput), from[0].label));
                return;
            }
            int need = Mathf.CeilToInt(outputs * inputPerOutput);
            foreach (var t in stacks)
            {
                if (need <= 0) break;
                int take = Mathf.Min(need, t.stackCount);
                need -= take;
                if (take >= t.stackCount) t.Destroy();
                else t.SplitOff(take).Destroy();
            }
            HearthUtil.Spawn(to, outputs, info.cell, map);
            ArcaneFx.Flash(map, info.pos, ctx.Color, 2.5f);
            ArcaneFx.Particles(map, info.pos, ctx.Color2, ArcaneDefOf.AA_Fleck_Spark, 14, 2f, 0.5f);
            HearthUtil.Note(ctx, info, "AA_Converted".Translate(outputs, to.label));
            ctx.arcane?.GainXp(10f * outputs);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (to == null || from.NullOrEmpty()) yield break;
            yield return "AA_Desc_Convert".Translate(inputPerOutput.ToString("F0"), string.Join("/", from.Select(f => f.label)), to.label,
                Mathf.RoundToInt(maxOutput * (ctx != null ? HearthUtil.Power(ctx) : 1f)));
        }
    }

    /// <summary>Pours lightning into the caster's batteries.</summary>
    public class Effect_ChargeBatteries : SpellEffect
    {
        public float fraction = 1f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (!(t is ThingWithComps tw) || !HearthUtil.Ours(t, ctx)) continue;
                var b = tw.GetComp<CompPowerBattery>();
                if (b == null || b.AmountCanAccept <= 0.5f) continue;
                b.SetStoredEnergyPct(Mathf.Clamp01(b.StoredEnergyPct + fraction * HearthUtil.Power(ctx)));
                n++;
                ArcaneVfxManager.For(ctx.map).Add(new VfxBeam(t.DrawPos + new Vector3(0, 0, 3f), t.DrawPos, 12, ArcaneMats.Beam, ctx.Color, ctx.Color2, 0.5f));
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_BatteriesCharged".Translate(n) : "AA_NoBatteries".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_ChargeBatteries".Translate(fraction.ToStringPercent());
        }
    }

    /// <summary>Repairs broken-down machines without components and can end a solar flare.</summary>
    public class Effect_FixBreakdowns : SpellEffect
    {
        public bool endSolarFlare = true;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (!(t is ThingWithComps tw) || !HearthUtil.Ours(t, ctx)) continue;
                var b = tw.GetComp<CompBreakdownable>();
                if (b == null || !b.BrokenDown) continue;
                b.Notify_Repaired();
                n++;
                ArcaneFx.Particles(ctx.map, t.DrawPos, ctx.Color, ArcaneDefOf.AA_Fleck_Bolt, 6, 2f, 0.5f);
            }
            bool flare = false;
            var solarFlare = DefDatabase<GameConditionDef>.GetNamedSilentFail("SolarFlare");
            if (endSolarFlare && solarFlare != null)
                foreach (var c in ctx.map.gameConditionManager.ActiveConditions.ToList())
                    if (c.def == solarFlare)
                    {
                        c.End();
                        flare = true;
                    }
            if (flare) Messages.Message("AA_SolarFlareGrounded".Translate(), MessageTypeDefOf.PositiveEvent, false);
            HearthUtil.Note(ctx, info, n > 0 ? "AA_MachinesFixed".Translate(n) : "AA_NothingBroken".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_FixBreakdowns".Translate();
            if (endSolarFlare) yield return "AA_Desc_EndSolarFlare".Translate();
        }
    }

    /// <summary>Recharges colony mechanoids.</summary>
    public class Effect_MechEnergy : SpellEffect
    {
        public float amount = 0.5f;

        public Effect_MechEnergy()
        {
            affects = TargetFilter.Allies;
        }

        public override void ApplyToPawn(SpellContext ctx, Pawn p, ImpactInfo info)
        {
            var e = p.needs?.energy;
            if (e == null || !p.RaceProps.IsMechanoid) return;
            e.CurLevelPercentage = Mathf.Clamp01(e.CurLevelPercentage + amount * HearthUtil.Power(ctx));
            ArcaneFx.Text(p, "AA_Recharged".Translate(), ctx.Color);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_MechEnergy".Translate(amount.ToStringPercent());
        }
    }

    /// <summary>Freezes water into walkable ice for a while. Ice that something was built on stays frozen.</summary>
    public class Effect_FreezeWater : SpellEffect
    {
        public int durationTicks = 60000;
        public TerrainDef ice;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var iceDef = ice ?? DefDatabase<TerrainDef>.GetNamedSilentFail("Ice");
            if (iceDef == null) return;
            var tracker = MapComponent_ArcaneColony.For(map);
            int dur = Mathf.RoundToInt(durationTicks * ctx.DurationFactor);
            int n = 0;
            foreach (var c in info.Cells(map))
            {
                var cur = c.GetTerrain(map);
                if (!cur.IsWater || cur == iceDef) continue;
                tracker.AddTemporaryTerrain(c, cur, Find.TickManager.TicksGame + dur);
                map.terrainGrid.SetTerrain(c, iceDef);
                n++;
                if (Rand.Chance(0.1f)) ArcaneFx.Particles(map, c.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Snow, 2, 0.6f, 0.5f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_WaterFrozen".Translate(dur.ToStringTicksToPeriod()) : "AA_NoWater".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_FreezeWater".Translate((durationTicks * (ctx?.DurationFactor ?? 1f)).ToStringTicksToPeriod());
        }
    }

    /// <summary>Blows the filth out of an area.</summary>
    public class Effect_Clean : SpellEffect
    {
        public int maxFilth = 400;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (n >= maxFilth) break;
                if (!(t is Filth f)) continue;
                n += f.thickness;
                if (Rand.Chance(0.2f)) FleckMaker.ThrowDustPuff(t.Position, ctx.map, 1f);
                f.Destroy();
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Cleaned".Translate() : "AA_AlreadyClean".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Clean".Translate();
        }
    }

    /// <summary>Airs out a room: clears smoke, tox gas and rot stink and brings its temperature toward a pleasant one.</summary>
    public class Effect_Ventilate : SpellEffect
    {
        public ThoughtDef thought;
        public float pleasantTemperature = 21f;
        public float temperatureLerp = 0.75f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var room = info.cell.GetRoom(map);
            IEnumerable<IntVec3> cells = room != null && !room.PsychologicallyOutdoors && room.CellCount < 900 ? room.Cells : info.Cells(map);
            foreach (var c in cells)
                if (c.InBounds(map) && map.gasGrid.AnyGasAt(c)) map.gasGrid.SetDirect(c, 0, 0, 0, 0);
            if (room != null && !room.UsesOutdoorTemperature)
                room.Temperature = Mathf.Lerp(room.Temperature, pleasantTemperature, temperatureLerp);
            if (thought != null)
                foreach (var p in map.mapPawns.FreeColonistsSpawned)
                    if ((room != null && p.GetRoom() == room) || p.Position.DistanceTo(info.cell) <= info.radius)
                        p.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
            ArcaneFx.Particles(map, info.pos, ctx.Color, ArcaneDefOf.AA_Fleck_Leaf, 12, 3f, 0.5f);
            HearthUtil.Note(ctx, info, "AA_Ventilated".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Ventilate".Translate(pleasantTemperature.ToStringTemperature("F0"));
        }
    }

    /// <summary>Gathers the loose items in an area to the caster's feet.</summary>
    public class Effect_GatherItems : SpellEffect
    {
        public int maxStacks = 60;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var caster = ctx.caster;
            if (caster == null || !caster.Spawned) return;
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (n >= maxStacks) break;
                if (t.def.category != ThingCategory.Item || !t.def.EverHaulable || t is Corpse c && c.InnerPawn?.IsColonist == true) continue;
                if (t.Position.DistanceTo(caster.Position) < 1.5f) continue;
                var from = t.DrawPos;
                map.reservationManager.ReleaseAllForTarget(t);
                t.DeSpawn();
                t.SetForbidden(false, false);
                if (!GenPlace.TryPlaceThing(t, caster.Position, map, ThingPlaceMode.Near))
                {
                    GenSpawn.Spawn(t, from.ToIntVec3(), map);
                    continue;
                }
                n++;
                if (n <= 12) ArcaneVfxManager.For(map).Add(new VfxBeam(from, t.DrawPos, 14, ArcaneMats.Beam, ctx.Color, ctx.Color2, 0.3f));
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_ItemsGathered".Translate(n) : "AA_NothingToGather".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_GatherItems".Translate(maxStacks);
        }
    }

    /// <summary>Harvests crops and fells trees in an area, leaving the produce on the ground.</summary>
    public class Effect_Harvest : SpellEffect
    {
        public bool crops = true;
        public bool trees;
        /// <summary>Also cuts away grass, brush and other wild plants with no yield (clearing land).</summary>
        public bool clearWild;
        public int maxPlants = 120;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var by = ctx.caster;
            int harvested = 0, felled = 0, cleared = 0;
            var yields = new Dictionary<ThingDef, int>();
            foreach (var c in info.Cells(map))
            {
                if (harvested + felled + cleared >= maxPlants) break;
                var plant = c.GetPlant(map);
                if (plant == null || plant.Destroyed) continue;
                var pd = plant.def.plant;
                var zone = map.zoneManager.ZoneAt(c) as Zone_Growing;
                bool tree = pd.IsTree;
                if (tree && trees)
                {
                    int y = plant.YieldNow();
                    if (y > 0 && pd.harvestedThingDef != null) yields[pd.harvestedThingDef] = yields.TryGetValue(pd.harvestedThingDef) + y;
                    plant.PlantCollected(by, PlantDestructionMode.Chop);
                    felled++;
                }
                else if (!tree && crops && plant.HarvestableNow && pd.harvestedThingDef != null)
                {
                    int y = plant.YieldNow();
                    if (y > 0) yields[pd.harvestedThingDef] = yields.TryGetValue(pd.harvestedThingDef) + y;
                    plant.PlantCollected(by, PlantDestructionMode.Cut);
                    harvested++;
                }
                else if (clearWild && !tree && zone == null && !plant.sown && (pd.harvestedThingDef == null || plant.YieldNow() <= 0))
                {
                    plant.Destroy();
                    cleared++;
                }
                else continue;
                if (Rand.Chance(0.25f)) ArcaneFx.Particles(map, c.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Leaf, 2, 1.5f, 0.5f);
            }
            foreach (var kv in yields) HearthUtil.Spawn(kv.Key, kv.Value, info.cell, map);
            if (harvested + felled + cleared == 0) HearthUtil.Note(ctx, info, "AA_NothingToHarvest".Translate());
            else HearthUtil.Note(ctx, info, "AA_Harvested".Translate(yields.Values.Sum()));
            ctx.arcane?.GainXp(harvested + felled * 2);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (crops) yield return "AA_Desc_HarvestCrops".Translate();
            if (trees) yield return "AA_Desc_FellTrees".Translate();
            if (clearWild) yield return "AA_Desc_ClearWild".Translate();
        }
    }

    /// <summary>Sows every empty cell of the growing zones in an area with that zone's crop.</summary>
    public class Effect_Sow : SpellEffect
    {
        public int maxPlants = 150;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int n = 0;
            foreach (var c in info.Cells(map))
            {
                if (n >= maxPlants) break;
                if (!(map.zoneManager.ZoneAt(c) is Zone_Growing zone)) continue;
                var def = zone.GetPlantDefToGrow();
                if (def == null || c.GetPlant(map) != null || c.GetEdifice(map) != null || c.GetFirstItem(map) != null || c.GetFirstPawn(map) != null) continue;
                if (!def.CanEverPlantAt(c, map)) continue;
                var p = (Plant)ThingMaker.MakeThing(def);
                p.Growth = Plant.BaseSownGrowthPercent;
                p.sown = true;
                GenSpawn.Spawn(p, c, map);
                n++;
                if (Rand.Chance(0.2f)) ArcaneFx.Particles(map, c.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Leaf, 1, 0.6f, 0.4f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Sown".Translate(n) : "AA_NothingToSow".Translate());
            ctx.arcane?.GainXp(n * 0.5f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Sow".Translate();
        }
    }

    /// <summary>Burns the blight out of plants.</summary>
    public class Effect_CureBlight : SpellEffect
    {
        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int n = 0;
            foreach (var c in info.Cells(map))
            {
                var plant = c.GetPlant(map);
                if (plant == null || !plant.Blighted) continue;
                plant.Blight?.Destroy();
                if (plant.def.useHitPoints) plant.HitPoints = plant.MaxHitPoints;
                n++;
                ArcaneFx.Particles(map, c.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Leaf, 2, 0.8f, 0.5f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_BlightCured".Translate(n) : "AA_NoBlight".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_CureBlight".Translate();
        }
    }

    /// <summary>Grows mature plants of one kind on the free, fertile ground in an area.</summary>
    public class Effect_SpawnPlants : SpellEffect
    {
        public ThingDef plant;
        public int count = 6;
        public float growth = 1f;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (plant == null) return;
            var map = ctx.map;
            int want = Mathf.RoundToInt(count * HearthUtil.Power(ctx));
            int n = 0;
            foreach (var c in info.Cells(map).InRandomOrder())
            {
                if (n >= want) break;
                if (c.GetPlant(map) != null || c.GetEdifice(map) != null || c.GetFirstItem(map) != null || !plant.CanEverPlantAt(c, map)) continue;
                var p = (Plant)ThingMaker.MakeThing(plant);
                p.Growth = growth;
                GenSpawn.Spawn(p, c, map);
                n++;
                ArcaneFx.Particles(map, c.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Petal, 3, 0.8f, 0.5f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_PlantsGrown".Translate(n, plant.label) : "AA_NoRoomToGrow".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            if (plant != null) yield return "AA_Desc_SpawnPlants".Translate(count, plant.label);
        }
    }

    /// <summary>Draws the pollution out of the ground.</summary>
    public class Effect_Unpollute : SpellEffect
    {
        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (!ModsConfig.BiotechActive) return;
            var map = ctx.map;
            int n = 0;
            foreach (var c in info.Cells(map))
            {
                if (!map.pollutionGrid.IsPolluted(c)) continue;
                map.pollutionGrid.SetPolluted(c, false);
                n++;
                if (Rand.Chance(0.1f)) ArcaneFx.Particles(map, c.ToVector3Shifted(), ctx.Color, ArcaneDefOf.AA_Fleck_Leaf, 1, 0.8f, 0.5f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Unpolluted".Translate(n) : "AA_NoPollution".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Unpollute".Translate();
        }
    }

    /// <summary>Smooths rough natural walls and floors, as a master stonecutter would.</summary>
    public class Effect_Smooth : SpellEffect
    {
        public bool walls = true;
        public bool floors = true;
        public int maxCells = 120;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            int n = 0;
            foreach (var c in info.Cells(map))
            {
                if (n >= maxCells) break;
                bool did = false;
                var e = c.GetEdifice(map);
                if (walls && e != null && e.def.building?.smoothedThing != null && ctx.caster != null)
                {
                    SmoothableWallUtility.SmoothWall(e, ctx.caster);
                    did = true;
                }
                var terr = c.GetTerrain(map);
                if (floors && terr.smoothedTerrain != null && (e == null || e.def.passability != Traversability.Impassable))
                {
                    map.terrainGrid.SetTerrain(c, terr.smoothedTerrain);
                    did = true;
                }
                if (!did) continue;
                n++;
                if (Rand.Chance(0.2f)) FleckMaker.ThrowDustPuff(c, map, 0.8f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Smoothed".Translate(n) : "AA_NothingToSmooth".Translate());
            ctx.arcane?.GainXp(n * 0.5f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Smooth".Translate();
        }
    }

    /// <summary>Finishes every construction frame in an area whose materials have all been delivered.</summary>
    public class Effect_CompleteConstruction : SpellEffect
    {
        public int maxFrames = 20;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (ctx.caster == null) return;
            int done = 0, waiting = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (done >= maxFrames) break;
                if (!(t is Frame f) || f.Destroyed || f.Faction != ctx.caster.Faction) continue;
                if (!f.IsCompleted())
                {
                    waiting++;
                    continue;
                }
                var pos = f.DrawPos;
                f.CompleteConstruction(ctx.caster);
                done++;
                ArcaneFx.Particles(ctx.map, pos, ctx.Color, ArcaneDefOf.AA_Fleck_Gear, 6, 1.5f, 0.5f);
            }
            if (done > 0) HearthUtil.Note(ctx, info, "AA_Constructed".Translate(done));
            else HearthUtil.Note(ctx, info, waiting > 0 ? "AA_FramesNeedMaterials".Translate(waiting) : "AA_NoFrames".Translate());
            ctx.arcane?.GainXp(done * 8f);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_CompleteConstruction".Translate();
        }
    }

    /// <summary>Takes the caster's own buildings in an area apart instantly, as if deconstructed.</summary>
    public class Effect_Deconstruct : SpellEffect
    {
        public int maxBuildings = 30;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            if (ctx.caster == null) return;
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (n >= maxBuildings) break;
                if (!(t is Building b) || b.Destroyed || b.Faction != ctx.caster.Faction || !b.DeconstructibleBy(ctx.caster.Faction).Accepted) continue;
                if (b.TryGetComp<CompArcaneConstruct>() != null) continue;
                var pos = b.DrawPos;
                b.Destroy(DestroyMode.Deconstruct);
                n++;
                ArcaneFx.Particles(ctx.map, pos, ctx.Color, ArcaneDefOf.AA_Fleck_Spark, 6, 1.5f, 0.5f);
            }
            HearthUtil.Note(ctx, info, n > 0 ? "AA_Unclipped".Translate(n) : "AA_NothingToUnclip".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Deconstruct".Translate();
        }
    }

    public class LootOption
    {
        public ThingDef thing;
        public IntRange count = new IntRange(1, 1);
        public float weight = 1f;
    }

    /// <summary>Pulls a few random things out of nowhere.</summary>
    public class Effect_RandomLoot : SpellEffect
    {
        public List<LootOption> options = new List<LootOption>();
        public int picks = 3;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var valid = options.Where(o => o.thing != null).ToList();
            if (valid.Count == 0) return;
            var got = new List<string>();
            for (int i = 0; i < picks; i++)
            {
                var o = valid.RandomElementByWeight(x => x.weight);
                int n = Mathf.Max(1, Mathf.RoundToInt(o.count.RandomInRange * HearthUtil.Power(ctx)));
                HearthUtil.Spawn(o.thing, n, info.cell, ctx.map);
                got.Add(n + "x " + o.thing.label);
            }
            ArcaneFx.Particles(ctx.map, info.pos, ctx.Color2, ArcaneDefOf.AA_Fleck_Spark, 10, 1.6f, 0.5f);
            Messages.Message("AA_FoundObjects".Translate(string.Join(", ", got)), new TargetInfo(info.cell, ctx.map), MessageTypeDefOf.PositiveEvent, false);
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_RandomLoot".Translate(picks);
        }
    }

    /// <summary>Double or nothing on the silver lying in an area.</summary>
    public class Effect_Wager : SpellEffect
    {
        public ThingDef currency;
        public float winChance = 0.5f;
        public int maxStake = 1500;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var cur = currency ?? ThingDefOf.Silver;
            var map = ctx.map;
            var piles = HearthUtil.ThingsIn(ctx, info).Where(t => t.def == cur).ToList();
            int stake = 0;
            foreach (var t in piles)
            {
                if (stake >= maxStake) break;
                int take = Mathf.Min(maxStake - stake, t.stackCount);
                stake += take;
                if (take >= t.stackCount) t.Destroy();
                else t.SplitOff(take).Destroy();
            }
            if (stake == 0)
            {
                HearthUtil.Note(ctx, info, "AA_NoStake".Translate(cur.label));
                return;
            }
            float chance = Mathf.Clamp(winChance + (HearthUtil.Power(ctx) - 1f) * 0.05f, 0.05f, 0.75f);
            if (Rand.Chance(chance))
            {
                HearthUtil.Spawn(cur, stake * 2, info.cell, map);
                ArcaneFx.Flash(map, info.pos, new Color(1f, 0.85f, 0.3f), 3f);
                Messages.Message("AA_WagerWon".Translate(stake * 2, cur.label), new TargetInfo(info.cell, map), MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                ArcaneFx.Particles(map, info.pos, ctx.Color, ArcaneDefOf.AA_Fleck_Ember, 16, 2f, 0.6f);
                Messages.Message("AA_WagerLost".Translate(stake, cur.label), new TargetInfo(info.cell, map), MessageTypeDefOf.NegativeEvent);
            }
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Wager".Translate(maxStake, winChance.ToStringPercent());
        }
    }

    /// <summary>A copy of an item arrives from a timeline where you had two.</summary>
    public class Effect_DuplicateItem : SpellEffect
    {
        public float maxMarketValue = 500f;

        public override bool AppliesToPawns => false;

        private static bool Duplicable(Thing t)
        {
            if (t.def.category != ThingCategory.Item || t is Corpse || t is MinifiedThing || t.def.destroyOnDrop) return false;
            if (t.def.tradeability == Tradeability.None && !t.def.IsStuff) return false;
            if (t.def.thingCategories != null && t.def.thingCategories.Any(c => c.defName.Contains("Artifact"))) return false;
            var comps = (t as ThingWithComps)?.AllComps;
            if (comps != null && comps.Any(c => c.GetType().Name.Contains("Bladelink") || c.GetType().Name.Contains("Biocod"))) return false;
            return true;
        }

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            var map = ctx.map;
            var target = info.cell.GetThingList(map).FirstOrDefault(Duplicable);
            if (target == null)
            {
                HearthUtil.Note(ctx, info, "AA_NothingToDuplicate".Translate());
                return;
            }
            float cap = maxMarketValue * HearthUtil.Power(ctx);
            float unit = Mathf.Max(0.01f, target.MarketValue);
            int n = Mathf.Min(target.stackCount, Mathf.FloorToInt(cap / unit));
            if (n <= 0)
            {
                HearthUtil.Note(ctx, info, "AA_TooValuable".Translate(target.LabelShort, cap.ToStringMoney()));
                return;
            }
            var copy = ThingMaker.MakeThing(target.def, target.Stuff);
            copy.stackCount = n;
            if (target.TryGetComp<CompQuality>() is CompQuality q && copy.TryGetComp<CompQuality>() is CompQuality cq)
                cq.SetQuality(q.Quality, ArtGenerationContext.Colony);
            if (target.def.useHitPoints) copy.HitPoints = Mathf.Min(copy.MaxHitPoints, target.HitPoints);
            HearthUtil.PlaceNear(copy, info.cell, map);
            ArcaneFx.Flash(map, info.pos, ctx.Color, 2.5f);
            ArcaneFx.Particles(map, info.pos, ctx.Color2, ArcaneDefOf.AA_Fleck_Clock ?? ArcaneDefOf.AA_Fleck_Spark, 12, 1.5f, 0.5f);
            HearthUtil.Note(ctx, info, "AA_Duplicated".Translate(copy.LabelCap));
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Duplicate".Translate((maxMarketValue * (ctx != null ? HearthUtil.Power(ctx) : 1f)).ToStringMoney());
        }
    }

    /// <summary>Destroys a share of the hit points of the enemy buildings in an area: turrets, mortars, walls, mech cluster structures.</summary>
    public class Effect_Sabotage : SpellEffect
    {
        public float fractionOfMaxHp = 0.6f;
        public bool onlyDangerous;

        public override bool AppliesToPawns => false;

        public override void ApplyAtCell(SpellContext ctx, ImpactInfo info)
        {
            int n = 0;
            foreach (var t in HearthUtil.ThingsIn(ctx, info))
            {
                if (!(t is Building b) || b.Destroyed || !b.def.useHitPoints || b.Faction == null || ctx.caster?.Faction == null || !b.Faction.HostileTo(ctx.caster.Faction)) continue;
                if (onlyDangerous && !(b.def.building?.ai_combatDangerous ?? false) && !(b is Building_Turret)) continue;
                float dmg = b.MaxHitPoints * Mathf.Clamp01(fractionOfMaxHp * Mathf.Sqrt(Mathf.Max(0.25f, ctx.DamageFactor)));
                var pos = b.DrawPos;
                b.TakeDamage(new DamageInfo(DamageDefOf.Bomb, dmg, 1f, -1f, ctx.caster));
                n++;
                ArcaneFx.Flash(ctx.map, pos, ctx.Color, 2f);
                FleckMaker.ThrowSmoke(pos, ctx.map, 1.5f);
            }
            if (n == 0) HearthUtil.Note(ctx, info, "AA_NothingToSabotage".Translate());
        }

        public override IEnumerable<string> Describe(SpellContext ctx)
        {
            yield return "AA_Desc_Sabotage".Translate(fractionOfMaxHp.ToStringPercent());
        }
    }
}

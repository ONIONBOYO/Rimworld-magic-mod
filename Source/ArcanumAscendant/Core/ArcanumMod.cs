using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    public class ArcanumSettings : ModSettings
    {
        public float xpMultiplier = 1f;
        public float manaRegenMultiplier = 1f;
        public float spellPowerMultiplier = 1f;
        public float enemyMageChance = 0.06f;
        public float enemyLevelScale = 1f;
        public bool enemyMagesCast = true;
        public bool freeAwakening = true;
        public bool awakenStartingColonists;
        public float vfxIntensity = 1f;
        public bool screenShake = true;
        public bool allowAutocast = true;
        public bool autocastNewSpells = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref xpMultiplier, "xpMultiplier", 1f);
            Scribe_Values.Look(ref manaRegenMultiplier, "manaRegenMultiplier", 1f);
            Scribe_Values.Look(ref spellPowerMultiplier, "spellPowerMultiplier", 1f);
            Scribe_Values.Look(ref enemyMageChance, "enemyMageChance", 0.06f);
            Scribe_Values.Look(ref enemyLevelScale, "enemyLevelScale", 1f);
            Scribe_Values.Look(ref enemyMagesCast, "enemyMagesCast", true);
            Scribe_Values.Look(ref freeAwakening, "freeAwakening", true);
            Scribe_Values.Look(ref awakenStartingColonists, "awakenStartingColonists");
            Scribe_Values.Look(ref vfxIntensity, "vfxIntensity", 1f);
            Scribe_Values.Look(ref screenShake, "screenShake", true);
            Scribe_Values.Look(ref allowAutocast, "allowAutocast", true);
            Scribe_Values.Look(ref autocastNewSpells, "autocastNewSpells", true);
        }
    }

    public class ArcanumMod : Mod
    {
        private static ArcanumSettings settings;

        public static ArcanumSettings Settings => settings ??= new ArcanumSettings();

        public ArcanumMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<ArcanumSettings>();
        }

        public override string SettingsCategory() => "Arcanum Ascendant";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var s = Settings;
            var l = new Listing_Standard();
            l.Begin(inRect);
            l.Label("AA_Set_Header".Translate().Colorize(new Color(0.75f, 0.6f, 1f)));
            l.GapLine();
            l.Label("AA_Set_Xp".Translate(s.xpMultiplier.ToStringPercent()));
            s.xpMultiplier = l.Slider(s.xpMultiplier, 0.1f, 10f);
            l.Label("AA_Set_ManaRegen".Translate(s.manaRegenMultiplier.ToStringPercent()));
            s.manaRegenMultiplier = l.Slider(s.manaRegenMultiplier, 0.1f, 5f);
            l.Label("AA_Set_Power".Translate(s.spellPowerMultiplier.ToStringPercent()));
            s.spellPowerMultiplier = l.Slider(s.spellPowerMultiplier, 0.25f, 5f);
            l.GapLine();
            l.CheckboxLabeled("AA_Set_FreeAwaken".Translate(), ref s.freeAwakening, "AA_Set_FreeAwakenDesc".Translate());
            l.CheckboxLabeled("AA_Set_AwakenStart".Translate(), ref s.awakenStartingColonists, "AA_Set_AwakenStartDesc".Translate());
            l.CheckboxLabeled("AA_Set_Autocast".Translate(), ref s.allowAutocast, "AA_Set_AutocastDesc".Translate());
            l.CheckboxLabeled("AA_Set_AutocastNew".Translate(), ref s.autocastNewSpells);
            l.GapLine();
            l.Label("AA_Set_EnemyChance".Translate(s.enemyMageChance.ToStringPercent()));
            s.enemyMageChance = l.Slider(s.enemyMageChance, 0f, 1f);
            l.Label("AA_Set_EnemyLevel".Translate(s.enemyLevelScale.ToStringPercent()));
            s.enemyLevelScale = l.Slider(s.enemyLevelScale, 0.1f, 4f);
            l.CheckboxLabeled("AA_Set_EnemyCast".Translate(), ref s.enemyMagesCast);
            l.GapLine();
            l.Label("AA_Set_Vfx".Translate(s.vfxIntensity.ToStringPercent()));
            s.vfxIntensity = l.Slider(s.vfxIntensity, 0f, 2f);
            l.CheckboxLabeled("AA_Set_Shake".Translate(), ref s.screenShake);
            l.Gap();
            if (l.ButtonText("AA_Set_Reset".Translate()))
            {
                var fresh = new ArcanumSettings();
                foreach (var f in typeof(ArcanumSettings).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                    f.SetValue(s, f.GetValue(fresh));
            }
            l.End();
        }
    }

    /// <summary>Startup wiring: injects the arcane comp into every race and links classes with their trees.</summary>
    [StaticConstructorOnStartup]
    public static class ArcaneStartup
    {
        static ArcaneStartup()
        {
            int injected = 0;
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race == null || !typeof(ThingWithComps).IsAssignableFrom(def.thingClass)) continue;
                def.comps ??= new List<CompProperties>();
                if (def.comps.Any(c => c is CompProperties_Arcane)) continue;
                def.comps.Add(new CompProperties_Arcane());
                injected++;
            }

            foreach (var cls in DefDatabase<ArcaneClassDef>.AllDefsListForReading)
            {
                cls.subclasses.Clear();
                cls.nodes.Clear();
            }
            foreach (var cls in DefDatabase<ArcaneClassDef>.AllDefsListForReading)
                if (cls.parentClass != null) cls.parentClass.subclasses.Add(cls);
            foreach (var node in DefDatabase<ArcaneNodeDef>.AllDefsListForReading)
                node.tree?.nodes.Add(node);
            foreach (var cls in DefDatabase<ArcaneClassDef>.AllDefsListForReading)
            {
                cls.subclasses.SortBy(c => c.order);
                foreach (var n in cls.nodes)
                    if (n.unlockSpell?.GetModExtension<SpellExtension>() is SpellExtension ext && ext.owner == null) ext.owner = cls;
                foreach (var s in cls.startingSpells)
                    if (s.GetModExtension<SpellExtension>() is SpellExtension ext2 && ext2.owner == null) ext2.owner = cls;
            }

            int classes = DefDatabase<ArcaneClassDef>.AllDefs.Count(c => !c.IsSubclass);
            int subclasses = DefDatabase<ArcaneClassDef>.AllDefs.Count(c => c.IsSubclass);
            int spells = DefDatabase<AbilityDef>.AllDefs.Count(d => d.HasModExtension<SpellExtension>());
            Log.Message($"[Arcanum Ascendant] Ready: {classes} classes, {subclasses} subclasses, {DefDatabase<ArcaneNodeDef>.DefCount} skill nodes, {spells} spells. Arcane comp injected into {injected} races.");
        }
    }

    /// <summary>Awakens starting colonists if the setting is on.</summary>
    public class GameComponent_Arcanum : GameComponent
    {
        public GameComponent_Arcanum(Game game) { }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            if (!ArcanumMod.Settings.awakenStartingColonists) return;
            foreach (var p in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                var c = p.TryGetComp<CompArcane>();
                if (c != null && !c.IsMage) c.Awaken(DefDatabase<ArcaneClassDef>.AllDefs.Where(d => !d.IsSubclass).RandomElement(), true);
            }
        }
    }

    public static class ArcaneDebug
    {
        public static IEnumerable<FloatMenuOption> Options(CompArcane c)
        {
            if (!c.IsMage)
            {
                foreach (var cls in DefDatabase<ArcaneClassDef>.AllDefs.Where(d => !d.IsSubclass).OrderBy(d => d.order))
                {
                    var cc = cls;
                    yield return new FloatMenuOption("Awaken as " + cls.label, () => c.Awaken(cc));
                }
                yield break;
            }
            yield return new FloatMenuOption("+1 level", () => c.GainXp(c.XpToNextLevel - c.xp + 0.1f, false));
            yield return new FloatMenuOption("+10 levels", () => { for (int i = 0; i < 10; i++) c.GainXp(c.XpToNextLevel - c.xp + 0.1f, false); });
            yield return new FloatMenuOption("Max level", () => c.SetLevelDirect(CompArcane.MaxLevel));
            yield return new FloatMenuOption("+10 perk points", () => c.perkPoints += 10);
            yield return new FloatMenuOption("+10 attribute points", () => c.attributePoints += 10);
            yield return new FloatMenuOption("Auto-allocate points", () => ArcaneUtility.AutoAllocate(c));
            yield return new FloatMenuOption("Learn every node (max ranks)", () =>
            {
                foreach (var n in c.classDef.nodes) c.ForceRank(n, n.maxRank);
                if (c.subclassDef != null) foreach (var n in c.subclassDef.nodes) c.ForceRank(n, n.maxRank);
            });
            yield return new FloatMenuOption("Fill mana & ward", () => { c.mana = c.ManaMax; c.ward = c.WardMax; });
            yield return new FloatMenuOption("Reset all cooldowns", () => { foreach (var a in c.ArcaneAbilities()) a.ResetCooldown(); });
            yield return new FloatMenuOption("Respec", () => c.Respec());
            yield return new FloatMenuOption("Remove class", () => c.Unawaken());
            if (c.subclassDef == null)
                foreach (var sub in c.classDef.subclasses)
                {
                    var s = sub;
                    yield return new FloatMenuOption("Force subclass: " + sub.label, () =>
                    {
                        if (c.level < c.classDef.subclassUnlockLevel) c.SetLevelDirect(c.classDef.subclassUnlockLevel);
                        c.ChooseSubclass(s);
                    });
                }
        }
    }
}

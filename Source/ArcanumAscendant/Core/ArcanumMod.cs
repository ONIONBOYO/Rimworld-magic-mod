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
        public bool freeAwakening;
        public bool awakenStartingColonists;
        public float vfxIntensity = 1f;
        public bool screenShake = true;
        public bool allowAutocast = true;
        public bool autocastNewSpells = true;
        public bool dynamicLights = true;
        public bool screenFlashes = true;
        public bool cinematicUltimates = true;
        public bool damageNumbers = true;
        public bool decals = true;
        public bool reactions = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref xpMultiplier, "xpMultiplier", 1f);
            Scribe_Values.Look(ref manaRegenMultiplier, "manaRegenMultiplier", 1f);
            Scribe_Values.Look(ref spellPowerMultiplier, "spellPowerMultiplier", 1f);
            Scribe_Values.Look(ref enemyMageChance, "enemyMageChance", 0.06f);
            Scribe_Values.Look(ref enemyLevelScale, "enemyLevelScale", 1f);
            Scribe_Values.Look(ref enemyMagesCast, "enemyMagesCast", true);
            Scribe_Values.Look(ref freeAwakening, "freeAwakeningV2");
            Scribe_Values.Look(ref awakenStartingColonists, "awakenStartingColonists");
            Scribe_Values.Look(ref vfxIntensity, "vfxIntensity", 1f);
            Scribe_Values.Look(ref screenShake, "screenShake", true);
            Scribe_Values.Look(ref allowAutocast, "allowAutocast", true);
            Scribe_Values.Look(ref autocastNewSpells, "autocastNewSpells", true);
            Scribe_Values.Look(ref dynamicLights, "dynamicLights", true);
            Scribe_Values.Look(ref screenFlashes, "screenFlashes", true);
            Scribe_Values.Look(ref cinematicUltimates, "cinematicUltimates", true);
            Scribe_Values.Look(ref damageNumbers, "damageNumbers", true);
            Scribe_Values.Look(ref decals, "decals", true);
            Scribe_Values.Look(ref reactions, "reactions", true);
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

        private static Vector2 settingsScroll;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var s = Settings;
            var l = new Listing_Standard();
            var view = new Rect(0f, 0f, inRect.width - 20f, 980f);
            Widgets.BeginScrollView(inRect, ref settingsScroll, view);
            l.Begin(view);
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
            l.CheckboxLabeled("AA_Set_Reactions".Translate(), ref s.reactions, "AA_Set_ReactionsDesc".Translate());
            l.GapLine();
            l.Label("AA_Set_Vfx".Translate(s.vfxIntensity.ToStringPercent()));
            s.vfxIntensity = l.Slider(s.vfxIntensity, 0f, 2f);
            l.CheckboxLabeled("AA_Set_Shake".Translate(), ref s.screenShake);
            l.CheckboxLabeled("AA_Set_Lights".Translate(), ref s.dynamicLights, "AA_Set_LightsDesc".Translate());
            l.CheckboxLabeled("AA_Set_Flashes".Translate(), ref s.screenFlashes, "AA_Set_FlashesDesc".Translate());
            l.CheckboxLabeled("AA_Set_Cinematic".Translate(), ref s.cinematicUltimates, "AA_Set_CinematicDesc".Translate());
            l.CheckboxLabeled("AA_Set_Numbers".Translate(), ref s.damageNumbers, "AA_Set_NumbersDesc".Translate());
            l.CheckboxLabeled("AA_Set_Decals".Translate(), ref s.decals, "AA_Set_DecalsDesc".Translate());
            l.Gap();
            if (l.ButtonText("AA_Set_Reset".Translate()))
            {
                var fresh = new ArcanumSettings();
                foreach (var f in typeof(ArcanumSettings).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                    f.SetValue(s, f.GetValue(fresh));
            }
            l.End();
            Widgets.EndScrollView();
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
        private HashSet<ArcaneReactionDef> discovered = new HashSet<ArcaneReactionDef>();
        /// <summary>Pawns pulled out of reality, waiting to fall back in.</summary>
        public List<BanishedPawn> banished = new List<BanishedPawn>();

        public GameComponent_Arcanum(Game game)
        {
            // Static per-game caches must not survive into another save.
            ArcaneReactions.Reset();
            ArcaneLightFlash.ResetLive();
        }

        public bool IsDiscovered(ArcaneReactionDef r) => discovered.Contains(r);

        /// <summary>The first time the colony sets off a reaction, it is written into every grimoire's codex.</summary>
        public void Discover(ArcaneReactionDef r, Pawn by)
        {
            if (r == null || !discovered.Add(r)) return;
            Find.LetterStack.ReceiveLetter("AA_ReactionDiscoveredLabel".Translate(r.LabelCap),
                "AA_ReactionDiscoveredText".Translate(by.LabelShort, r.LabelCap, r.description, by.Named("PAWN")), LetterDefOf.PositiveEvent, by);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref discovered, "discoveredReactions", LookMode.Def);
            Scribe_Collections.Look(ref banished, "banishedPawns", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                discovered ??= new HashSet<ArcaneReactionDef>();
                discovered.RemoveWhere(d => d == null);
                banished ??= new List<BanishedPawn>();
                banished.RemoveAll(b => b?.pawn == null);
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (banished.Count > 0 && Find.TickManager.TicksGame % 10 == 0) BanishUtility.Tick(this);
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            if (!ArcanumMod.Settings.awakenStartingColonists) return;
            foreach (var p in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                var c = p.TryGetComp<CompArcane>();
                if (c != null && !c.IsMage) c.Awaken(ArcaneClassDef.RandomRootFor(p), true);
            }
        }
    }

    public static class ArcaneDebug
    {
        public static IEnumerable<FloatMenuOption> Options(CompArcane c)
        {
            if (!c.IsMage)
            {
                if (!c.attuning && !c.awakenReady) yield return new FloatMenuOption("Begin attunement", () => c.StartAttunement());
                if (c.attuning) yield return new FloatMenuOption("+25% attunement", () => c.AddAttunement(0.25f));
                if (!c.awakenReady) yield return new FloatMenuOption("Complete attunement", () => { c.StartAttunement(); c.CompleteAttunement(); });
                foreach (var cls in DefDatabase<ArcaneClassDef>.AllDefs.Where(d => !d.IsSubclass).OrderBy(d => d.order))
                {
                    var cc = cls;
                    yield return new FloatMenuOption("Awaken as " + cls.label, () => c.Awaken(cc));
                }
                yield break;
            }
            yield return new FloatMenuOption("+1 level (respects circle cap)", () => c.GainXp(c.XpToNextLevel - c.xp + 0.1f, false));
            yield return new FloatMenuOption("+1 level (ignores cap)", () => c.SetLevelDirect(c.level + 1));
            yield return new FloatMenuOption("+5 levels (ignores cap)", () => c.SetLevelDirect(c.level + 5));
            if (c.circle < CompArcane.MaxCircle)
                yield return new FloatMenuOption("Ascend to next circle", () =>
                {
                    if (c.level < c.LevelCap) c.SetLevelDirect(c.LevelCap);
                    c.Ascend();
                });
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
            yield return new FloatMenuOption("+40 strain", () => c.AddStrain(40f));
            yield return new FloatMenuOption("Clear strain", () => { c.strain = 0f; c.AddStrain(0.001f); });
            yield return new FloatMenuOption("Wild surge", () => ArcaneStrain.WildSurge(c));
            yield return new FloatMenuOption("Grandmaster every known spell", () => { foreach (var s in c.KnownSpells().ToList()) c.GainMastery(s, Mastery.Thresholds[Mastery.MaxRank]); });
            foreach (var fam in DefDatabase<FamiliarDef>.AllDefs)
            {
                var ff = fam;
                yield return new FloatMenuOption("Bond familiar: " + fam.label, () => Familiars.Bond(c, ff));
            }
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

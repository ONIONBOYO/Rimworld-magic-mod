using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>A playable class or subclass. Subclasses point to their parent via <see cref="parentClass"/>.</summary>
    public class ArcaneClassDef : Def
    {
        public string iconPath;
        public Color color = Color.white;
        public Color colorSecondary = Color.white;
        public ArcaneClassDef parentClass;
        public string tagline;
        public string lore;
        public string playstyle;
        public List<string> schoolTags = new List<string>();
        public ArcaneAttributeDef primaryAttribute;
        public ArcaneAttributeDef secondaryAttribute;
        public int subclassUnlockLevel = 7;
        public List<StatModifier> innateStatOffsets = new List<StatModifier>();
        public List<AbilityDef> startingSpells = new List<AbilityDef>();
        public int treeWidth = 7;
        public int treeHeight = 8;
        public int order;

        [Unsaved] public List<ArcaneClassDef> subclasses = new List<ArcaneClassDef>();
        [Unsaved] public List<ArcaneNodeDef> nodes = new List<ArcaneNodeDef>();
        [Unsaved] private Texture2D iconInt;

        public bool IsSubclass => parentClass != null;
        public ArcaneClassDef RootClass => parentClass ?? this;

        public Texture2D Icon
        {
            get
            {
                if (iconInt == null)
                    iconInt = iconPath.NullOrEmpty() ? BaseContent.BadTex : ContentFinder<Texture2D>.Get(iconPath, false) ?? BaseContent.BadTex;
                return iconInt;
            }
        }

        public IEnumerable<AbilityDef> AllSpells => nodes.Where(n => n.unlockSpell != null).Select(n => n.unlockSpell).Concat(startingSpells).Distinct();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (parentClass != null && parentClass.parentClass != null) yield return "subclass of a subclass is not supported";
        }
    }

    public enum NodeKind
    {
        Minor,
        Notable,
        Spell,
        Augment,
        Keystone,
        Ultimate,
    }

    public class CapacityOffset
    {
        public PawnCapacityDef capacity;
        public float offset;
    }

    /// <summary>A single node in a class or subclass skill tree.</summary>
    public class ArcaneNodeDef : Def
    {
        public ArcaneClassDef tree;
        public NodeKind kind = NodeKind.Minor;
        public int x;
        public int y;
        public int maxRank = 1;
        public int costPerRank = 1;
        public int requiredLevel;
        public ArcaneAttributeDef requiredAttribute;
        public int requiredAttributeValue;
        public List<ArcaneNodeDef> prerequisites = new List<ArcaneNodeDef>();
        public bool requireAllPrerequisites;
        public string exclusiveGroup;
        public AbilityDef unlockSpell;
        public List<StatModifier> statOffsetsPerRank = new List<StatModifier>();
        public List<StatModifier> statPercentPerRank = new List<StatModifier>();
        public List<CapacityOffset> capacityOffsetsPerRank = new List<CapacityOffset>();
        public List<SpellModifier> spellMods = new List<SpellModifier>();
        public List<ArcanePassive> passives = new List<ArcanePassive>();
        public string iconPath;
        public string flavor;

        [Unsaved] private Texture2D iconInt;

        public Texture2D Icon
        {
            get
            {
                if (iconInt == null)
                {
                    if (!iconPath.NullOrEmpty()) iconInt = ContentFinder<Texture2D>.Get(iconPath, false);
                    if (iconInt == null && unlockSpell != null) iconInt = unlockSpell.uiIcon;
                    if (iconInt == null) iconInt = tree?.Icon ?? BaseContent.BadTex;
                }
                return iconInt;
            }
        }

        public bool IsRoot => prerequisites.NullOrEmpty();

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            if (label.NullOrEmpty() && unlockSpell != null) label = unlockSpell.label;
            foreach (var p in passives) p.ResolveReferences(this);
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (tree == null) yield return "node has no tree";
            if (maxRank < 1) yield return "maxRank < 1";
            if (unlockSpell != null && maxRank != 1) yield return "spell nodes must have maxRank 1";
        }
    }

    /// <summary>One of the five core attributes, raised with attribute points.</summary>
    public class ArcaneAttributeDef : Def
    {
        public string abbreviation;
        public string iconPath;
        public Color color = Color.white;
        public List<StatModifier> statOffsetsPerPoint = new List<StatModifier>();
        public int order;
        [Unsaved] private Texture2D iconInt;

        public Texture2D Icon
        {
            get
            {
                if (iconInt == null)
                    iconInt = iconPath.NullOrEmpty() ? BaseContent.BadTex : ContentFinder<Texture2D>.Get(iconPath, false) ?? BaseContent.BadTex;
                return iconInt;
            }
        }
    }

    public enum SpellRole
    {
        Attack,
        Heal,
        Buff,
        Control,
        Summon,
        Utility,
        Mobility,
    }

    /// <summary>Arcane metadata attached to an <see cref="AbilityDef"/>.</summary>
    public class SpellExtension : DefModExtension
    {
        public float manaCost = 10f;
        public float healthCost;
        public List<string> tags = new List<string>();
        public int tier = 1;
        public SpellRole role = SpellRole.Attack;
        public bool ultimate;
        public bool quirky;
        public float xpFactor = 1f;
        public bool autocastByDefault;
        /// <summary>For Buff/Heal autocast: only cast when an ally is below this health fraction.</summary>
        public float aiHealthThreshold = 0.6f;
        public float aiMinEnemiesInRadius;
        public bool aiAllowedUndrafted;
        public ArcaneClassDef owner;
        public string flavor;
    }

    /// <summary>Per-rank modifications applied to matching spells.</summary>
    public class SpellModifier
    {
        public List<AbilityDef> spells;
        public string tag;
        public float damage;
        public float healing;
        public float radius;
        public float duration;
        public float manaCost;
        public float cooldown;
        public float range;
        public int extraTargets;
        public float critChance;
        public float armorPen;
        public float lifesteal;
        public float summonPower;
        public HediffDef applyHediff;
        public float applyChance;
        public float applySeverity = 1f;

        public bool Matches(AbilityDef def, SpellExtension ext)
        {
            if (spells != null && spells.Count > 0 && !spells.Contains(def)) return false;
            if (!tag.NullOrEmpty() && (ext == null || !ext.tags.Contains(tag))) return false;
            return true;
        }

        public string Describe(int rank)
        {
            var parts = new List<string>();
            string P(float v) => (v * rank >= 0 ? "+" : "") + (v * rank).ToStringPercent();
            if (damage != 0) parts.Add(P(damage) + " " + "AA_ModDamage".Translate());
            if (healing != 0) parts.Add(P(healing) + " " + "AA_ModHealing".Translate());
            if (radius != 0) parts.Add(P(radius) + " " + "AA_ModRadius".Translate());
            if (duration != 0) parts.Add(P(duration) + " " + "AA_ModDuration".Translate());
            if (manaCost != 0) parts.Add(P(manaCost) + " " + "AA_ModManaCost".Translate());
            if (cooldown != 0) parts.Add(P(cooldown) + " " + "AA_ModCooldown".Translate());
            if (range != 0) parts.Add(P(range) + " " + "AA_ModRange".Translate());
            if (critChance != 0) parts.Add(P(critChance) + " " + "AA_ModCrit".Translate());
            if (armorPen != 0) parts.Add(P(armorPen) + " " + "AA_ModArmorPen".Translate());
            if (lifesteal != 0) parts.Add(P(lifesteal) + " " + "AA_ModLifesteal".Translate());
            if (summonPower != 0) parts.Add(P(summonPower) + " " + "AA_ModSummon".Translate());
            if (extraTargets != 0) parts.Add("+" + (extraTargets * rank) + " " + "AA_ModExtraTargets".Translate());
            if (applyHediff != null) parts.Add("AA_ModApplies".Translate((applyChance * rank).ToStringPercent(), applyHediff.label));
            string scope;
            if (!spells.NullOrEmpty()) scope = string.Join(", ", spells.Select(s => s.LabelCap.ToString()));
            else if (!tag.NullOrEmpty()) scope = "AA_TaggedSpells".Translate(tag);
            else scope = "AA_AllSpells".Translate();
            return scope + ": " + string.Join(", ", parts);
        }
    }

    /// <summary>Aggregated spell modifiers for a caster + spell.</summary>
    public class SpellModSummary
    {
        public float damage;
        public float healing;
        public float radius;
        public float duration;
        public float manaCost;
        public float cooldown;
        public float range;
        public int extraTargets;
        public float critChance;
        public float armorPen;
        public float lifesteal;
        public float summonPower;
        public List<(HediffDef def, float chance, float severity)> onHit = new List<(HediffDef, float, float)>();

        public void Add(SpellModifier m, int rank)
        {
            damage += m.damage * rank;
            healing += m.healing * rank;
            radius += m.radius * rank;
            duration += m.duration * rank;
            manaCost += m.manaCost * rank;
            cooldown += m.cooldown * rank;
            range += m.range * rank;
            extraTargets += m.extraTargets * rank;
            critChance += m.critChance * rank;
            armorPen += m.armorPen * rank;
            lifesteal += m.lifesteal * rank;
            summonPower += m.summonPower * rank;
            if (m.applyHediff != null) onHit.Add((m.applyHediff, m.applyChance * rank, m.applySeverity));
        }
    }

    [DefOf]
    public static class ArcaneDefOf
    {
        public static HediffDef AA_ArcaneCore;
        public static ThingDef AA_ArcaneCrystal;
        public static JobDef AA_ArcaneStudy;
        public static HediffDef AA_Summoned;
        public static HediffDef AA_Polymorphed;
        public static HediffDef AA_Dominated;
        public static HediffDef AA_Undead;
        public static DamageDef AA_Arcane;
        public static DamageDef AA_ArcaneTrue;
        public static StatCategoryDef AA_ArcanaStats;
        public static StatDef AA_SpellPower;
        public static StatDef AA_ManaMax;
        public static StatDef AA_ManaRegen;
        public static StatDef AA_CooldownReduction;
        public static StatDef AA_CastSpeed;
        public static StatDef AA_CritChance;
        public static StatDef AA_CritDamage;
        public static StatDef AA_ManaCostFactor;
        public static StatDef AA_WardMax;
        public static StatDef AA_WardRegen;
        public static StatDef AA_AreaFactor;
        public static StatDef AA_DurationFactor;
        public static StatDef AA_SummonPower;
        public static StatDef AA_Lifesteal;
        public static StatDef AA_XpGain;
        public static StatDef AA_HealingPower;
        public static ThingDef AA_Grimoire;
        public static ThingDef AA_TomeOfUnmaking;
        public static ThingDef AA_ArcaneLight;
        public static FleckDef AA_Fleck_Spark;
        public static FleckDef AA_Fleck_Glow;
        public static FleckDef AA_Fleck_Ember;
        public static FleckDef AA_Fleck_Mote;
        public static FleckDef AA_Fleck_Smoke;
        public static FleckDef AA_Fleck_Shard;
        public static FleckDef AA_Fleck_Leaf;
        public static FleckDef AA_Fleck_Note;
        public static FleckDef AA_Fleck_Rune;
        public static FleckDef AA_Fleck_Ring;
        public static FleckDef AA_Fleck_Snow;
        public static FleckDef AA_Fleck_Bone;
        public static FleckDef AA_Fleck_Feather;
        public static FleckDef AA_Fleck_Blood;
        public static FleckDef AA_Fleck_Gear;
        public static FleckDef AA_Fleck_Star;
        public static FleckDef AA_Fleck_Skull;
        public static FleckDef AA_Fleck_Petal;
        public static FleckDef AA_Fleck_Clock;
        public static FleckDef AA_Fleck_Bolt;
        public static SoundDef AA_LevelUp;
        public static SoundDef AA_Awaken;
        public static SoundDef AA_NodeUnlock;

        static ArcaneDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ArcaneDefOf));
        }
    }
}

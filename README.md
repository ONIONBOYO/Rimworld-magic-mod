# Arcanum Ascendant: Magic & Classes for RimWorld 1.6

![Arcanum Ascendant](About/Preview.png)

A magic and class system for RimWorld with **9 classes, 27 subclasses, 252 spells and 705 skill tree nodes**. Every class and every subclass has its own skill tree, and it doesn't depend on any other mod (not even Harmony).

- **Five attributes**: Potency, Focus, Celerity, Resilience and Attunement. They power your spells and gate the deeper rows of every tree.
- **Branching skill trees**: spells, augments that change how a spell behaves, multi-rank passives, mutually exclusive keystones and a subclass ultimate.
- **Psycast-style awakening**: colonists don't get magic for free. Scribe a *grimoire of awakening* from arcane crystals, have a colonist read it, then let them study at an *arcane lectern* until their gift awakens. Psychic sensitivity and intellect speed it up, and the psychically deaf can't attune at all.
- **Five arcane circles**: levels 1-30 come from fighting, casting, healing and study. At levels 6, 12, 18 and 24 experience stops until the mage shatters an *ascension sigil* to break through to the next circle. Subclasses open at the Second Circle and ultimates need the Fifth. After level 30, paragon levels continue slowly.
- **Endgame spells for endgame threats**: true damage, damage based on a percentage of the target's vitality, executes, shield shattering, mechanoid shutdowns, black holes, time stop, mass mind control and spells that scale with "boss" size. They're built for heavily armored late-game faction mods.
- **Utility and quirky spells**: make colonists young again (each casting cures an ailment of age, fades a scar or regrows a limb), age enemies until they crumble to dust, repair and un-rot items, raise an item's quality to legendary, turn weakened enemies into solid gold, call down ore meteorites, steal skills from prisoners, make it rain, turn rock into steel or silver, grow crops, tame every animal in sight, polymorph raiders into chickens, and scatter banana peels.
- **Summons**: risen dead, wolves, bears, a thrumbo, megascarabs, turrets, a Magma Titan, a Rune Colossus, and walls of ice, bone, thorns and runes.
- **Custom visual effects and sound**: glowing projectiles with trails, beams, chain lightning, meteors, pillars of light, rune circles, shockwaves, black holes, ward bubbles and casting circles. There are 64 synthesized sound effects.
- **Grimoire UI**: a zoomable skill-tree screen, an attribute panel, a spellbook with autocast and hide toggles, a subclass picker with tree previews, and a codex.
- **Enemy mages**: raiders can be awakened too, and they cast their spells at you.
- **Fully configurable**: XP rate, mana regeneration, spell power, enemy mage frequency and level, effect intensity and screen shake.

The full list of every spell and keystone is in **[docs/Compendium.md](docs/Compendium.md)**.

---

## Installation

**Requires RimWorld 1.6.** No other mods are needed.

1. **Download** the mod. On this GitHub page click the green **Code** button, then **Download ZIP**.
2. **Extract** the ZIP into your RimWorld `Mods` folder. The extracted folder (for example `rimworld-magic-mod-main`) should sit directly inside `Mods`:
   - **Windows (Steam):** `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
   - **Mac (Steam):** `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/`
   - **Linux (Steam):** `~/.steam/steam/steamapps/common/RimWorld/Mods/`
   - **GOG/DRM-free:** the `Mods` folder next to the game executable.
3. **Enable** it. Start RimWorld, open **Mods**, tick **Arcanum Ascendant - Magic & Classes**, and restart when the game asks. Load order doesn't matter much; putting it after Core and the DLCs is fine.

That's it. You can add it to an existing save.

> Folder check: the mod is installed correctly if `Mods/<folder>/About/About.xml` and `Mods/<folder>/1.6/Assemblies/ArcanumAscendant.dll` both exist.

---

## Getting started

1. **Find arcane crystals.** Mine violet *ley crystal veins* in mountains, drill for them with a deep drill, buy them from exotic traders and outlander or tribal settlements, or take them from enemy mages you kill.
2. **Research *Arcanology*** and build an **arcane lectern**. Scribe a **grimoire of awakening** there (8 arcane crystals, 60 silver, 25 cloth, Intellectual 6).
3. **Attune a colonist.** Have them read the grimoire. They then need about 16 hours of study at the lectern; they study in their free time, or you can right-click the lectern to order it. Psychic sensitivity and Intellectual skill speed this up. When attunement completes, click **Awaken magic** and choose one of the nine classes.
   - Rarely, a colonist's gift wakes on its own.
   - If you'd rather skip all this, turn on **Free awakening** in the mod settings.
4. **Open the grimoire.** Mages get a status panel showing mana, ward and XP. Click it to open the grimoire:
   - Spend **attribute points** in the left panel. You get one per level.
   - Spend **perk points** in the skill tree. Click a glowing node to learn it. A node needs a learned node connected above it, and deeper rows need levels and your primary attribute.
5. **Cast spells.** Learned spells appear as buttons on the command bar. Combat spells show while the mage is drafted; utility, heal and buff spells show at all times. Spell buttons display their mana cost.
6. **Ascend.** At level 6 the mage's experience bar turns gold and stops filling. Etch a *sigil of the Second Circle* at the lectern and have them shatter it. Higher sigils need more crystals, gold and research (*Higher Arcana*, *Arcane Mastery*, *Archmagic*).
7. **Choose a subclass at the Second Circle.** Open the grimoire's Subclass tab, preview the three trees, and commit to one.
8. **Turn on autocast.** Right-click a spell button, or use the Spellbook tab. Drafted mages then cast those spells on their own. Healing spells can also autocast while undrafted.

### Tips
- Mages gain experience faster from real fights than from study. Killing non-hostile creatures gives almost nothing.
- **Ward** is an arcane barrier that absorbs damage before your health does. It regenerates once you've gone 5 seconds without taking damage. Resilience and level increase it.
- **Mana draughts** restore mana instantly. Brew them at the Arcane Lectern.
- A **Tome of Unmaking** (requires the *Higher Arcana* research) refunds every point you've spent, so you can rebuild.
- Keystones in the same row are mutually exclusive. They define your build.
- Hover any node or spell for an exact tooltip showing current and next-rank numbers and your real damage multiplier.
- In developer mode, a **DEV: Arcanum** button lets you test anything: awaken, add levels, learn every node, reset cooldowns.

---

## The classes

| Class | Fantasy | Subclasses |
|---|---|---|
| **Pyromancer** | Burst fire damage and burning | Inferno Lord (area devastation), Ashen Phoenix (healing fire and rebirth), Forgeheart (molten armor and fire constructs) |
| **Tempest** | Lightning chains, frost and wind | Stormcaller (anti-mech lightning), Frostwarden (freeze and shatter), Skybreaker (tornadoes and weather) |
| **Necromancer** | Drain life, raise and explode the dead | Lich (deathless caster, Finger of Death), Bonelord (undead legions), Plaguebringer (spreading plague) |
| **Paladin** | Holy tank and healer | Templar (front-line knight), Cleric (mass healing and resurrection), Inquisitor (purge, silence, final judgment) |
| **Chronomancer** | Haste, slow and stopped time | Timeweaver (squad buffs, Eternity), Voidwalker (gravity wells, Singularity), Paradox (undo deaths, erase enemies) |
| **Druid** | Nature, beasts and farming | Beastmaster (bears, wargs, thrumbo), Verdant Warden (healing groves, World Tree), Shapeshifter (bear, wolf and thrumbo forms) |
| **Runesmith** | Turrets, wards and transmutation | Golemancer (construct army), Spellforger (enchanting allies), Arcane Gunslinger (runic gunplay) |
| **Shadowmancer** | Hexes, stealth and terror | Assassin (backstab, Death Lotus), Hexer (curses and chickens), Hemomancer (blood magic) |
| **Spellsinger** | Music: buffs, sleep, mood | Skald (war songs), Illusionist (charm and recruit), Jester (chaos magic and pies) |

See the [Compendium](docs/Compendium.md) for every spell, its cost, cooldown and description.

---

## Mod settings

Options → Mod Settings → Arcanum Ascendant:

| Setting | Default |
|---|---|
| Experience gain | 100% |
| Mana regeneration | 100% |
| Spell power | 100% |
| Free awakening (no grimoire or study needed) | Off |
| Starting colonists begin awakened | Off |
| Allow autocast | On |
| Chance for hostile people to be mages | 6% |
| Enemy mage level scaling | 100% |
| Enemy mages cast spells | On |
| Visual effect intensity | 100% |
| Screen shake | On |

---

## Compatibility

- RimWorld **1.6**. Works with or without any DLC.
- It doesn't patch game code (no Harmony), so conflicts are very unlikely.
- It works with modded humanlike races: every race gets the arcane component at startup.
- It's safe to add mid-save. Mages from saves made before the circle system are converted automatically: their level is rescaled and all their points are refunded so you can rebuild. To remove it, first un-awaken your mages from the dev tools, or start a new save.

---

## Building from source (for modders)

You only need this if you want to change the mod. The repository already contains the compiled DLL, the textures and the sounds.

- **C# assembly:** `Source/ArcanumAscendant/` compiles against `Krafs.Rimworld.Ref` (NuGet) with the .NET SDK and writes to `1.6/Assemblies/`.
- **Content:** classes, spells and skill trees are written in compact Python in `Tools/gen/classes/*.py`. `python3 Tools/gen/build.py` turns them into XML in `Defs/Generated/` and regenerates `docs/Compendium.md`.
- **Art:** `python3 Tools/art/make_textures.py` renders every icon, sprite and effect texture procedurally (needs numpy and pillow).
- **Audio:** `python3 Tools/audio/make_sounds.py` synthesizes every sound effect.
- **Validation:** `Tools/validator` loads the compiled assembly and RimWorld's reference assemblies through reflection. It checks every XML field name, class, enum value and def reference.
- `Tools/build_all.sh` runs all of the above in order.

Adding a spell is about ten lines of Python: pick a delivery (projectile, beam, chain, cone, nova, barrage, line, meteor) and compose effects (damage, heal, hediff, field, summon, teleport, polymorph and dozens more).

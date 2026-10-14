# Arcanum Ascendant - Compendium

Every class, subclass, spell and keystone in the mod. Generated from `Tools/gen/classes/*.py` - do not edit by hand.

**14 classes - 42 subclasses - 424 spells - 1170 skill tree nodes**

## Contents

- [Pyromancer](#pyromancer): Inferno Lord, Ashen Phoenix, Forgeheart
- [Tempest](#tempest): Stormcaller, Frostwarden, Skybreaker
- [Necromancer](#necromancer): Lich, Bonelord, Plaguebringer
- [Paladin](#paladin): Templar, Cleric, Inquisitor
- [Chronomancer](#chronomancer): Timeweaver, Voidwalker, Paradox
- [Druid](#druid): Beastmaster, Verdant Warden, Shapeshifter
- [Runesmith](#runesmith): Golemancer, Spellforger, Arcane Gunslinger
- [Shadowmancer](#shadowmancer): Assassin, Hexer, Hemomancer
- [Spellsinger](#spellsinger): Skald, Illusionist, Jester
- [Harbinger](#harbinger): Doomsayer, Pale Rider, Cataclysm
- [Wind Mage](#wind-mage): Windblade, Pressure Sage, Skywarden
- [Succubus](#succubus): Temptress, Thief of Years, Nightmare Muse
- [Incubus](#incubus): Pactbinder, Dreamlord, Hellprince
- [Noclipper](#noclipper): Level Keeper, Entity Handler, Archivist

## Pyromancer

*Wield fire as a weapon, a shield and a forge. Raw destruction with a warm heart.*

Pyromancers command the oldest and hungriest of the elements. Every fight ends in ash, and every winter night ends in warmth.

**Playstyle:** High burst damage, area denial and burning damage over time. Pyromancers want Potency and Focus. Get Fireball early, set the battlefield alight, and finish the survivors with Meteor.

**Key attributes:** Potency, Focus

### Pyromancer spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Firebolt | 8 | 1.5s | attack | Hurl a bolt of fire that scorches a single target and may set it burning. |
| Fireball | 22 | 6s | attack | Launch a roaring sphere of flame that detonates on impact, scorching everything in the blast. |
| Cauterize | 15 | 20s | heal | Sear a companion's wounds shut. Heals and tends every injury at once. It hurts, but it works. |
| Fire Ward | 18 | 30s | buff | Wrap an ally in a mantle of tamed flame: immunity to fire and a burst of ward. |
| Kindle Hearth * | 20 | 12 h | utility | Conjure an everburning arcane hearth that heats and lights the area for a full day. Perfect for cold nights and colder tundras. |
| Flame Wave | 22 | 9s | attack | Unleash a cone of flame that burns and hurls back everyone in front of you. |
| Living Bomb | 25 | 10s | attack | Plant a seed of flame inside a target. After a few seconds it detonates, damaging the target and everyone standing near it. |
| Meteor | 60 | 1.1 h | attack | Call down a burning star. Devastates a wide area, stuns, and sets the world alight. |
| Dragon's Breath | 34 | 14s | attack | Exhale a roaring cone of dragonfire. Everything in front of you burns. |
| Wall of Fire | 40 | 30s | control | Draw a line of fire across the battlefield. It burns for half a minute and scorches anything that crosses it. |
| Orbiting Suns | 40 | 1.1 h | attack | Three miniature suns wheel around you for twenty-five seconds, scorching and igniting anything that comes close. |

**Keystones:** *Avatar of Ruin* - Abandon restraint. Fire spells deal far more damage but cost more mana.; *Eternal Flame* - A fire that never goes out. Fire spells are cheaper and sometimes don't go on cooldown at all.; *Heart of the Sun* - The pinnacle of pyromancy. Your fire kills sometimes launch a free Fireball at the next enemy.

### Inferno Lord (subclass)

*Rain ruin from the sky. The battlefield is your furnace.*

Inferno Lords forsake subtlety for scale: firestorms, pillars of flame and the dreaded Cataclysm.

**Playstyle:** The biggest area damage in the game. Stack Potency, Area and crit, and keep enemies clumped with Flame Wave before calling the sky down.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Fire Rain | 40 | 22s | attack | Call a storm of small meteors over an area. Each strike burns and scorches. |
| Pillar of Flame | 30 | 10s | attack | A column of fire erupts from the ground beneath the target, stunning and incinerating. |
| Hellfire Field | 38 | 30s | control | Curse the ground with hellfire. Enemies inside burn every second for ten seconds. |
| Infernal Pact | 0 | 1.4 h | buff | Burn your own blood for power: restore a large share of your mana and become empowered, at the cost of health. |
| Cataclysm (ultimate) | 180 | 5.8 h | attack | The Inferno Lord's final word. A storm of great meteors annihilates an enormous area. Nothing survives the center. |
| Supernova | 120 | 2.9 h | attack | Become a dying star. A blinding blast of fire tears outward from you, burning even through armor. It costs you some of your own blood. |
| Infernal Torrent | 50 | 30s | attack | Pour a continuous torrent of hellfire at a target for five seconds. Everything around the point of contact burns. |

**Keystones:** *Scorched Earth Doctrine* - You are a walking wildfire. Your fire spells frequently set targets burning, and enemies near you take fire damage.; *Ruinous Precision* - Every blast placed perfectly. Massive critical chance and armor penetration for fire spells.

### Ashen Phoenix (subclass)

*Die. Burn. Rise again. Healing fire and blazing mobility.*

Ashen Phoenixes learned that fire also cleanses and renews. They dive through enemy lines, mend their friends with flame and refuse to stay dead.

**Playstyle:** A mobile battle-healer. Dive in with Phoenix Dive, heal allies with Healing Flames, and rely on Phoenix Heart to cheat death. Wants Potency and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Phoenix Dive | 25 | 14s | mobility | Become a streak of flame: dash to the target location, burning everything along the path, then explode on landing. |
| Healing Flames | 30 | 15s | heal | A gentle golden fire washes over an area. Allies are healed and cleansed; enemies are scorched. |
| Ember Swarm | 20 | 6s | attack | Release a flock of flaming embers that seek out nearby enemies. |
| Wings of Flame | 35 | 1.4 h | buff | Unfurl wings of living fire: faster movement, immunity to fire, and more spell power. |
| Rebirth Eternal (ultimate) | 220 | 7.2 h | heal | The phoenix's song. Resurrects fallen allies nearby, fully heals and wards the living, and burns every enemy in a wide nova. |
| Rekindle | 70 | 4 h | heal | Phoenix fire burns away what is broken. Heals an ally, fades two scars and regrows a lost limb or organ with each casting. |
| Phoenix Feathers | 40 | 1.4 h | heal | Five burning feathers circle you for half a minute. Each time one brushes an ally it mends their wounds. |

**Keystones:** *Phoenix Heart* - When you are downed, you rise from the ashes fully healed in an explosion of fire. Once per day.; *Solar Flare* - Your healing spells often release a free Ember Swarm at nearby enemies.; *Ashen Covenant* - Devote yourself to renewal. Much stronger healing, and every healing spell shields you.

### Forgeheart (subclass)

*Fire that builds. Molten armor, smelting magic and blazing war-constructs.*

Forgehearts are smiths first and mages second. They armor their friends in molten steel, smelt rock into metal, and raise burning sentinels.

**Playstyle:** A durable support and summoner. Use Ember Sentinels to hold a line, Heat Metal to strip armor off mechanoids, and Molten Armor on your front line. Wants Focus and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Molten Armor | 20 | 30s | buff | Coat an ally in a shell of glowing metal: heavy armor, fire immunity and a ward. |
| Heat Metal | 22 | 8s | attack | Superheat the target's armor and plating. Brutal against mechanoids; strips armor so allies can finish the job. |
| Ember Sentinel | 35 | 40s | summon | Forge a burning sentinel that hurls bolts of fire at enemies for a minute. |
| Pyroclastic Smelt * | 25 | 1 h | utility | Melt down stone chunks in an area. The arcane forge leaves behind usable steel. |
| Forge of the Titans (ultimate) | 200 | 7.2 h | summon | Raise a towering Magma Titan and two Ember Sentinels. The Titan lobs molten slag that explodes on impact. |
| Reforge * | 90 | 1 day | utility | Pour soul-fire into a weapon or piece of gear and hammer it anew. Its quality rises by one step, up to legendary. |
| Hammerfall | 55 | 28s | attack | Five glowing forge-hammers slam down in turn around the target. Each blow crushes and staggers. Force like this shatters frozen enemies. |

**Keystones:** *Living Forge* - Allies who fight beside you are continually coated in molten armor.; *Mechbane* - You know exactly where machines are weakest. Huge bonus damage against mechanoids, and your fire shatters energy shields.

## Tempest

*Command lightning, ice and the wind itself. Chain-strike crowds and freeze them in place.*

Tempests are storms given a voice. Their lightning leaps from foe to foe, their frost locks enemies in place, and their winds scatter armies.

**Playstyle:** Fast chaining damage and powerful crowd control. Tempests want Potency and Celerity: low cooldowns let you weave Chain Lightning, Frost Lance and Gale Burst constantly.

**Key attributes:** Potency, Celerity

### Tempest spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Spark | 8 | 1.5s | attack | A crackling bolt of lightning that arcs to a second nearby enemy. |
| Frost Lance | 14 | 4s | attack | Fire a shard of black ice that pierces and chills. |
| Chain Lightning | 26 | 8s | attack | Lightning leaps between up to five enemies, losing a little power with each jump. |
| Gale Burst | 18 | 12s | control | A violent gust explodes outward from you, hurling enemies away. |
| Frost Armor | 18 | 30s | buff | Encase an ally in a lattice of enchanted ice that turns blades and soaks heat. |
| Rainmaker * | 25 | 12 h | utility | Seed the clouds. Rain begins to fall and nearby fires are snuffed out. Farmers will love you. |
| Blizzard | 40 | 30s | control | Summon a howling blizzard over an area. Enemies inside take frost damage and are chilled to the bone. |
| Thunderstorm | 70 | 1.2 h | attack | Darken the sky over an area and call down bolt after bolt of lightning. |
| Frost Nova | 30 | 18s | control | A ring of killing frost bursts from you, freezing everything close by. |
| Storm Lance | 42 | 16s | attack | Hurl a lance of lightning that splits the sky: bolts strike every few steps along its path. |
| Glacial Spike | 28 | 10s | attack | A spike of ice erupts from the ground a moment after you mark the spot, freezing whatever stands there. Frozen enemies shatter when struck with fire or force. |

**Keystones:** *Maelstrom* - Become the storm. Lightning spells hit harder, chain further, and sometimes cost nothing.; *Heart of Winter* - Winter follows you. Frost lasts longer, chills almost always, and enemies near you slow to a crawl.; *Storm Sovereign* - The sky answers you unbidden: while in combat, a free Spark strikes a random nearby enemy every second and a half.

### Stormcaller (subclass)

*Pure, overwhelming lightning. Fry machines, shatter shields, call the wrath of the sky.*

Stormcallers chase the purest form of the storm. Their lightning overloads machines, bursts energy shields and tears through armies.

**Playstyle:** The premier anti-mechanoid class. Overload shuts down mechs and breaks shields; Tesla Coils hold chokepoints; Wrath of the Sky ends fights. Wants Potency and Celerity.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Ball Lightning | 34 | 12s | attack | A slow, enormous orb of plasma drifts toward the target and detonates violently. |
| Thunderclap | 28 | 16s | control | Slam a fist of thunder into the ground, stunning and battering everyone around you. |
| Overload | 30 | 14s | control | Flood a target with current. Shatters energy shields, shuts down mechanoids for long seconds and fries their circuits. |
| Tesla Coil | 36 | 40s | summon | Conjure a crackling tesla coil that zaps enemies with chain lightning for a minute. |
| Wrath of the Sky (ultimate) | 190 | 5.8 h | attack | The storm takes notice. Two dozen lightning bolts annihilate a vast area while you become Stormborn. |
| Ion Storm | 110 | 2.2 h | control | Call down a storm of ionised air. For twenty seconds it fries electronics, collapses shields and locks up mechanoids. |
| Arc Missiles | 32 | 9s | attack | Six crackling orbs of lightning that seek out enemies. They leave targets shocked; fire turns a shocked target into an explosion. |

**Keystones:** *Storm Ascendant* - Your lightning ignores more armor and deals far more damage.; *Living Current* - Your lightning has a mind of its own. Lightning hits sometimes spawn a free Chain Lightning.

### Frostwarden (subclass)

*Freeze the battlefield. Walls of ice, flash freezes and shattering blows.*

Frostwardens are the patient side of the storm. They wall off the battlefield, freeze enemies in their tracks and shatter them.

**Playstyle:** The strongest crowd control in the game. Freeze groups with Flash Freeze, then use Shatter for triple damage. Ice Walls reshape any fight. Wants Potency and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Ice Wall | 30 | 30s | utility | Raise a ring of enchanted ice walls around a point. Trap enemies inside or shelter friends. |
| Flash Freeze | 34 | 18s | control | Instantly freeze everything in an area solid. |
| Shatter | 22 | 6s | attack | Strike a target with a hammer of ice. Frozen targets take triple damage and the ice explodes. |
| Cryostasis | 30 | 1.1 h | heal | Seal a badly wounded ally in protective ice. They cannot act or be harmed, and they regenerate while frozen. |
| Absolute Zero (ultimate) | 200 | 5.8 h | control | All motion stops. Every enemy in a vast radius around you is frozen solid and battered by cold, and a killing blizzard rises. |
| Glacial Tomb | 55 | 40s | control | Encase a target in a tomb of ice ringed by frozen walls. It can do nothing for ten seconds, and the ice makes it brittle. |
| Ice Halo | 40 | 1.1 h | attack | Six blades of ice orbit you for twenty-five seconds, slicing and chilling anyone who gets close. |

**Keystones:** *Glaciation* - Frost spells execute weakened enemies, and Shatter becomes devastating.; *Eternal Winter* - You become a fortress of ice: a huge ward that regenerates rapidly, and a quarter of all damage is drained from mana instead.

### Skybreaker (subclass)

*Wind, hail and weather. Tornadoes, hurricanes and command of the sky itself.*

Skybreakers master the air. They ride the wind, pelt enemies with hail, conjure tornadoes, and can even clear poisoned skies.

**Playstyle:** Mobile area control and utility. Tornadoes drag enemies together, Hailstorm punishes clumps, and Clear Skies ends fallout and volcanic winters. Wants Potency and Celerity.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Tornado | 38 | 26s | control | Conjure a raging tornado that drags enemies inward and batters them for eight seconds. |
| Hailstorm | 36 | 20s | attack | Fist-sized hail rains down over an area. |
| Windwalk | 18 | 16s | mobility | Ride the wind to a distant point and keep its speed for a while. |
| Clear Skies * | 60 | 2 days | utility | Tear open the clouds. Ends toxic fallout, volcanic winter, flashstorms, cold snaps and heat waves, and calls a beautiful aurora. |
| Hurricane (ultimate) | 210 | 6.2 h | control | Unleash a hurricane. For twenty seconds a colossal vortex drags everything toward its eye and shreds it. |
| Starfall * | 90 | 2 days | utility | Reach into the sky and pull down a falling star. It crashes nearby as a meteorite packed with ore. |
| Cyclone Blades | 38 | 40s | attack | Three blades of compressed wind scream around you, cutting and hurling back whatever they hit. |

**Keystones:** *Eye of God* - See the battlefield from above. Every area spell grows enormously.; *Untouchable* - Like trying to hit the wind. A quarter of all attacks simply miss as you blink away.

## Necromancer

*Command death. Drain life, explode corpses and raise the fallen to fight for you.*

Necromancers treat death as a resource. Every fallen raider is a future soldier, a bomb, or a lesson.

**Playstyle:** Snowballing attrition. Your army grows with every kill: raise corpses, detonate the ones you can't use, and drain life to stay standing. Wants Focus and Potency.

**Key attributes:** Focus, Potency

### Necromancer spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Soul Bolt | 8 | 1.5s | attack | A bolt of hungry, green soulfire. |
| Raise Dead | 30 | 30s | summon | Raise up to two corpses near the target as undead thralls for a day. They fight for you, feel nothing, and need nothing. |
| Corpse Explosion | 24 | 10s | attack | Detonate up to six corpses in an area. Bigger bodies make bigger bangs. |
| Drain Life | 20 | 8s | attack | Rip the life out of a target and take it for yourself. |
| Bone Armor | 18 | 30s | buff | Grow a cage of hardened bone around an ally. |
| Speak With the Dead * | 20 | 4 h | utility | Interrogate a corpse. The dead share their most practiced skill with you, and the body rots quickly afterward. |
| Enfeeble | 26 | 18s | control | Curse an area. Enemies weaken, their wounds wither, and they suffer more from every blow. |
| Army of the Dead | 80 | 2.9 h | summon | Raise every corpse in a wide area - up to six - as thralls for a full day. |
| Corpse Feast | 10 | 30s | utility | Draw the last life out of up to six corpses nearby. Each one heals you and refills your mana, then crumbles away. |
| Mark of the Grave | 26 | 12s | control | Brand an enemy for death. Their wounds fester, and if they die while marked they rise as your undead servant. |
| Soul Swarm | 34 | 14s | attack | Release six hungry souls that hunt down enemies and gnaw at their life, feeding a share of it back to you. |

**Keystones:** *Master of Graves* - Your army is endless. Raise far more corpses for far longer, and those you kill often rise again.; *Soul Eater* - Consume souls directly. Massive lifesteal, a flood of mana on every kill, and kills shave seconds off your cooldowns.; *Deathlord* - Death answers to you. Anything you kill may burst in a wave of necrotic energy.

### Lich (subclass)

*Shed mortality. Freezing death magic, soul theft and the Finger of Death.*

Liches trade their humanity for undying power. Their touch freezes the blood and their gaze kills.

**Playstyle:** A durable caster that refuses to die and kills anything big. Finger of Death deletes bosses; Lichform makes you nearly unkillable. Wants Focus and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Death Coil | 22 | 6s | attack | A spiralling coil of death that heals you for half the damage it deals. |
| Grave Chill | 30 | 18s | control | The cold of the tomb spills out from you, freezing enemies in place. |
| Soul Rend | 26 | 14s | attack | Tear at the souls of everyone in an area, restoring your mana for each one you touch. |
| Lichform | 60 | 4.3 h | buff | Become a lich for a minute: no pain, no hunger, halved damage, and terrible magical power. |
| Finger of Death (ultimate) | 160 | 4.3 h | attack | Point, and the target dies. Even the mightiest war machines are torn apart by a huge fraction of their own vitality. |
| Soul Reap | 60 | 40s | attack | Tear a sliver of soul from every enemy around you. You are healed for part of the damage and your mana surges. |
| Drain Soul | 40 | 20s | attack | Lock a ribbon of necrotic power onto a target and drink its soul for five seconds. You heal for most of the damage. |

**Keystones:** *Eternal Vigil* - Your phylactery holds. When you fall, you rise again in a wave of death. Once per day.; *Archlich* - Death on demand. Finger of Death recharges much faster and necromancy hits harder.; *Soulbound* - Spend twice as long in Lichform, more often, and drink the life of your foes.

### Bonelord (subclass)

*An army of the dead and fortresses of bone.*

Bonelords raise legions. Bone spires guard their flanks, bone prisons trap their prey, and their dead never stop marching.

**Playstyle:** A full summoner. Keep corpses around, keep your army up, and empower them with Dark Command. Wants Focus and Attunement.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Bone Spire | 30 | 30s | summon | Raise a spire of bone that fires splinters at enemies. |
| Bone Prison | 28 | 30s | control | Ribs of bone erupt in a ring around the target, trapping everyone inside. |
| Dark Command | 30 | 40s | buff | Drive your undead and allies into a frenzy: faster, stronger, tireless. |
| Bone Spear | 24 | 8s | attack | A line of bone spikes erupts from the ground toward the target. |
| Legion Eternal (ultimate) | 200 | 7.2 h | summon | Every corpse in a huge area rises - up to fifteen - for two full days, empowered by your will. |
| Bone Storm | 50 | 35s | buff | A whirlwind of razor-edged bone spins around you for twelve seconds, shredding every enemy that comes close. |
| Bone Shards | 30 | 12s | attack | Hurl five jagged shards of bone that curve in on enemies and leave them bleeding. |

**Keystones:** *Legion Master* - Every ally near you fights with the fury of Dark Command at all times.; *Bone Fortress* - Bone grows over you like armor, and when you are struck a spire may burst from the ground to defend you.

### Plaguebringer (subclass)

*Rot, poison and plague. Infect one enemy and watch it spread.*

Plaguebringers cultivate disease like gardeners. A single carrier, a little patience, and an entire raid melts.

**Playstyle:** Damage over time that spreads on its own. Seed the plague, keep enemies in Miasma, and let deaths burst into new infections. Wants Focus and Attunement.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Plague Bolt | 16 | 4s | attack | Infect a target with the black plague. Victims who die from it burst and infect those around them. |
| Miasma | 32 | 24s | control | A choking cloud of rot settles over an area, poisoning everyone inside. |
| Contagion | 30 | 16s | control | Every enemy near the target catches whatever it has: plague, poison and doom spread outward. |
| Swarm of Rot | 34 | 1.1 h | summon | Call forth a brood of megascarabs from the soil to swarm your enemies for a minute. |
| Pandemic (ultimate) | 200 | 6.2 h | attack | Unleash a pandemic. Every enemy in an enormous area is infected, poisoned, cursed and withered, and the air itself turns toxic. |
| Plague Rats | 40 | 1.1 h | summon | A swarm of plague-ridden rats boils out of the ground. Their bites carry the black plague. |

**Keystones:** *Black Death* - Everything you touch catches the plague, and everything you kill bursts.; *Epidemiologist* - Precise and patient: plague magic lasts far longer, spreads further, and finishes off the weak.

## Paladin

*Holy warrior and healer. Smite the wicked, shield the innocent, mend the broken.*

Paladins channel radiant light. They stand on the front line, shielding allies, healing wounds and burning away anything unholy.

**Playstyle:** The most durable class and a strong healer. Paladins want Resilience and Potency: Divine Shield, Lay on Hands and Holy Nova keep the whole squad alive.

**Key attributes:** Resilience, Potency

### Paladin spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Smite | 9 | 2s | attack | Call down a lance of light on a single foe. |
| Lay on Hands | 20 | 20s | heal | Heal an ally with a touch of pure light, tending every wound. |
| Divine Shield | 30 | 1.4 h | buff | Become almost untouchable for six seconds. |
| Consecration | 30 | 25s | control | Sanctify the ground. Enemies on it burn with holy fire; allies on it slowly heal. |
| Blessing | 15 | 30s | buff | Bless an ally: tougher armor, steadier nerves and faster healing for hours. |
| Purify * | 25 | 1 h | utility | Burn sickness out of a body. Cures infections, diseases, toxic buildup, food poisoning and even drug addictions. |
| Hammer of Justice | 20 | 12s | control | Hurl a hammer of light that stuns the target. |
| Holy Nova | 55 | 40s | heal | A blast of light: heals allies and burns enemies all around you. |
| Divine Intervention | 60 | 2.9 h | buff | The heavens reach down and hold an ally. For eight seconds nothing can harm them. |
| Heaven's Lance | 48 | 20s | attack | Call a procession of holy pillars that march out from you in a line, searing and judging everything in their path. |
| Sunstrike | 45 | 22s | attack | Mark the ground. A heartbeat later a column of sunfire slams down: heavy radiant damage, a stagger, and a mark of judgment that shadow magic can detonate. |

**Keystones:** *Wrath of Heaven* - Light that burns the unnatural: much more holy damage, especially against machines and abominations.; *Sanctity* - Become a living sanctuary. Much stronger healing, and every spell wards you.; *Exalted* - When struck, you may erupt in a free Holy Nova.

### Templar (subclass)

*Charge into the fray clad in light. The unbreakable front line.*

Templars are the sword and shield of the faith: armored, relentless, and impossible to put down.

**Playstyle:** A melee tank. Charge into the enemy, mark them with Judgment and shield the team. Pair with a melee weapon. Wants Resilience and Celerity.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Judgment | 28 | 10s | attack | A massive smite that marks the target for judgment: everyone hits them harder. |
| Shield of Faith | 34 | 30s | buff | Raise a great ward on yourself and smaller wards on every ally nearby. |
| Holy Charge | 22 | 14s | mobility | Charge to a target location in a blaze of light, stunning and knocking back enemies where you land. |
| Righteous Fury | 30 | 1.4 h | buff | Fight with holy fury: your weapon strikes hit harder and heal you. |
| Avatar of Light (ultimate) | 180 | 5.8 h | buff | Become an avatar of the divine for a minute, exploding with holy force as you ascend. |
| Unbreakable Vow | 45 | 1.4 h | buff | Swear to stand your ground. For fifteen seconds you take far less damage and a third of every blow is turned back on the attacker. |

**Keystones:** *Juggernaut* - An unstoppable wall of plate and faith. Huge armor, and melee attackers are punished severely.; *Eternal Crusade* - The fight feeds you. Weapon hits restore mana and kills slash your cooldowns.

### Cleric (subclass)

*The greatest healer on the rim. Nobody dies on your watch - or stays dead.*

Clerics pour everything into restoration: mass heals, regeneration, sanctuaries, and in the end, true resurrection.

**Playstyle:** Pure support. Keep your colonists alive through anything, regrow lost limbs and resurrect the fallen. Wants Resilience and Focus.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Greater Heal | 40 | 20s | heal | A powerful heal that can even regrow lost body parts. |
| Mass Regeneration | 35 | 30s | heal | Every ally nearby begins to regenerate rapidly. |
| Resurrection | 120 | 1 day | utility | Bring a dead ally back to life. |
| Sanctuary | 40 | 40s | heal | Raise a dome of holy light. Allies inside heal quickly and take less damage. |
| Miracle (ultimate) | 250 | 1 day | heal | A true miracle. Resurrects every fallen ally nearby, fully heals and restores every living ally, regrows lost limbs and cures all ills. |
| Greater Restoration | 140 | 1 day | heal | A long, exhausting prayer that makes the body whole: every chronic illness cured, every scar erased, every lost limb and organ regrown, every addiction broken. |
| Guardian Spirits | 50 | 1.8 h | heal | Three guardian spirits circle you for forty seconds. Whenever one passes an ally it heals them and lifts poisons and sickness. |

**Keystones:** *Ascension* - Death is a minor setback. Resurrection and Miracle recharge twice as fast.; *Overflowing Grace* - Every spell you cast becomes a heal for everyone around you.

### Inquisitor (subclass)

*Purge, silence and judge. The scourge of heretic mages and god-machines alike.*

Inquisitors hunt what should not exist: rogue mages, abominations and the war machines of fallen empires.

**Playstyle:** Anti-caster, anti-shield, anti-boss. Dispel buffs, silence mages, banish threats and finish them with Final Judgment. Wants Potency and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Purge | 22 | 12s | control | Strip away the target's magic: buffs, wards and energy shields are destroyed. |
| Silence | 26 | 18s | control | Seal the tongues of everyone in an area. They cannot cast spells for a while. |
| Holy Fire | 20 | 5s | attack | A searing bolt of righteous flame that melts armor and marks the target. |
| Banishment | 28 | 20s | control | Hurl a foe far across the battlefield and stun them. |
| Final Judgment (ultimate) | 200 | 5.8 h | attack | Heaven renders its verdict. Twelve pillars of annihilating light fall around the target. Armor means nothing. |
| Mirror of Judgment | 50 | 1.4 h | buff | For ten seconds, every blow struck against you is judged: you take half of it, and the attacker takes all of it. |
| Searing Ray | 45 | 25s | attack | Focus a ray of holy fire on a heretic for four seconds. It burns through armor and brands them for judgment. |

**Keystones:** *No Heresy* - Holy spells often purge their targets for free and execute the weakened.; *The Long Inquest* - Final Judgment recharges far faster and calls more pillars.

## Chronomancer

*Bend time and space. Haste allies, slow enemies, stop the clock entirely.*

Chronomancers see time as a river they can wade into. They hasten friends, slow foes, rewind wounds, blink across the battlefield and, at the peak of their art, stop time itself.

**Playstyle:** Control and force multiplication. Haste your best fighters, Slow and Time Stop enemies, and Rewind injuries. Wants Celerity and Attunement - cooldown reduction is king.

**Key attributes:** Celerity, Attunement

### Chronomancer spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Temporal Bolt | 9 | 1.5s | attack | A bolt of raw entropy. Ignores armor completely and may slow the target. |
| Haste | 18 | 30s | buff | Speed up an ally's personal time: faster movement, attacks, casting and work. |
| Slow | 18 | 12s | control | Thicken time in an area. Enemies inside crawl. |
| Blink | 12 | 8s | mobility | Step through space to a nearby point. |
| Rewind Wounds | 25 | 30s | heal | Turn back an ally's body by an hour. Every wound received in that time simply never happened. |
| Accelerate Growth * | 22 | 3 h | utility | Fast-forward time for the plants in an area. Crops leap toward harvest. |
| Chronal Shield | 22 | 30s | buff | Wrap an ally in slowed time; incoming harm loses much of its force. |
| Time Stop | 70 | 1.4 h | control | Stop time for every enemy around you for six seconds. |
| Rejuvenate * | 60 | 6 h | utility | Unwind an ally's personal time. Each casting takes three years off their body, cures one age-related condition (bad back, cataracts, frailty, dementia and the like) and fades one scar. Keep casting to restore them fully. |
| Senescence | 34 | 15s | attack | Force years of age onto a target in an instant. Each casting adds about eight years, wracks the body with time-ravaged weakness, and may bring on the ailments of old age. Push someone far enough past their natural lifespan and they crumble to dust. |
| Reverse Entropy * | 45 | 2 h | utility | Run time backwards for everything in an area: damaged items are repaired, spoiled food is fresh again, and clothes taken from the dead lose their taint. Allies' worn gear is restored too. |
| Temporal Bomb | 40 | 25s | attack | Plant a knot of stopped time on an enemy. It follows them for three seconds, then releases every moment it held at once: heavy damage, a long stun and lingering slowness. Void magic fractures slowed targets. |

**Keystones:** *Eternal Stasis* - When you stop time, it stays stopped. Time Stop and Slow last half again as long and reach further.; *Infinite Acceleration* - Everything you do happens a little sooner than it should.; *Master of Time* - When badly hurt, time sometimes simply undoes it: a free Rewind Wounds on yourself.

### Timeweaver (subclass)

*Mass haste, temporal prisons, youth and age. The ultimate force multiplier.*

Timeweavers stitch moments together. Their allies move at impossible speed while enemies wither into dust.

**Playstyle:** Support a whole squad: Mass Haste, Echo of Moments to reset everyone's cooldowns, and Eternity to win any fight. Wants Celerity and Focus.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Mass Haste | 40 | 1.1 h | buff | Every ally around you is hastened. |
| Ages of Dust | 30 | 12s | attack | Age everything in an area by years in a heartbeat. Enemies take damage that ignores armor, wither, suffer the ailments of old age, and the very old crumble to dust. |
| Echo of Moments | 60 | 2.9 h | utility | Rewind the cooldowns of every ally nearby. Their spells and abilities are ready again. |
| Fountain of Youth * | 80 | 8 h | utility | Open a fountain of youth around an ally. Everyone touched loses two years, sheds an age-related condition and a scar, and regrows a lost limb or organ. Repeated castings can restore even the oldest body. |
| Eternity (ultimate) | 220 | 7.2 h | control | Stop time across an enormous area for ten seconds. Your allies are hastened, their cooldowns reset, and only they move. |

**Keystones:** *Loom of Fate* - Every effect you weave lasts far longer.; *Accelerando* - Allies near you in combat are permanently hastened.

### Voidwalker (subclass)

*Walk between worlds. Portals, gravity wells and the devouring Singularity.*

Voidwalkers learned that space has holes in it. They step through, pull enemies in, and drop what remains into nothing.

**Playstyle:** Positioning and raw destruction. Gravity Well gathers enemies, Void Bolt tears through armor, and Singularity erases whatever is left. Wants Potency and Celerity.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Void Bolt | 18 | 4s | attack | A sphere of nothing that tears through armor and leaves reality frayed around its target. |
| Recall | 30 | 1.4 h | utility | Open a portal: every ally near the target point is pulled through to your side. |
| Gravity Well | 36 | 24s | control | Collapse space into a well that drags enemies to its center and crushes them. |
| Phase Shift | 28 | 1.1 h | buff | Slip partly out of reality. Attacks pass through you and you move faster. |
| Singularity (ultimate) | 210 | 6.2 h | attack | Tear open a black hole. For fifteen seconds it drags everything toward it and grinds it out of existence. |
| Transposition | 24 | 15s | mobility | Fold space so that you and a target trade places instantly. Pull an ally out of danger, or drop an enemy into it. |
| Disintegrate | 70 | 1.1 h | attack | Focus a beam of pure nothing on a target for six seconds. It ignores nearly all armor and erases a little of the target's being with every pulse. |

**Keystones:** *Devourer* - The void is always hungry. Void kills implode, hurting nearby foes and feeding you mana.; *Annihilation* - Pure erasure. Void spells deal vastly more damage and ignore much more armor.

### Paradox (subclass)

*Break causality. Undo deaths, loop enemies in time and erase them from history.*

Paradox mages exploit contradictions in time. They undo what already happened, trap foes in loops and erase the unworthy from history.

**Playstyle:** Undo mistakes and erase threats. Undo can revive the recently dead; Causality Loop shreds anything; Grandfather Paradox ends bosses. Wants Attunement and Celerity.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Causality Loop | 26 | 10s | attack | Trap a target in a loop: the same moment of pain repeats over and over. |
| Temporal Rift | 36 | 28s | control | Rip open a rift where time runs strangely: enemies inside slow and decay while allies inside are hastened. |
| Split Timeline | 40 | 2.2 h | buff | Borrow strength from a timeline where you prepared better: a ward, haste and refreshed cooldowns. |
| Undo | 120 | 1 day | heal | Undo the last six hours for an area: allies' recent wounds vanish and allies who died in that time are restored to life. |
| Grandfather Paradox (ultimate) | 180 | 4.8 h | attack | Erase the target from history. Most creatures simply cease to exist; even titans lose most of their vitality. |
| Fate Anchor | 90 | 1 day | buff | Anchor an ally's fate to this moment. If they die within the next four hours, time snaps back and they live, whole and unhurt. Works once. |

**Keystones:** *Erasure* - All your spells erase the badly wounded from history, and Grandfather Paradox returns much sooner.; *Ouroboros* - When you fall, time rewinds to just before. Once per day.

## Druid

*Speak for the wild. Roots and thorns, beasts and shapes, harvests and healing.*

Druids are the voice of every living thing on the rim. They call beasts, raise thorns, heal with sap and make barren land bloom.

**Playstyle:** A flexible all-rounder with the best farming utility in the game. Entangle and Thorn Volley in combat, Bloom and Fertile Ground at home. Wants Focus and Resilience.

**Key attributes:** Focus, Resilience

### Druid spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Thorn Volley | 9 | 2s | attack | Fling a spray of hardened thorns at nearby enemies. |
| Entangle | 20 | 14s | control | Roots burst from the ground and bind every enemy in an area. |
| Rejuvenation | 18 | 12s | heal | Fill an ally with green life: an immediate heal and lasting regeneration. |
| Barkskin | 16 | 30s | buff | Harden an ally's skin into living bark. |
| Bloom * | 24 | 3 h | utility | Life surges through the soil. Plants grow at a staggering pace and wild flowers sprout from the earth. |
| Call of the Wild | 28 | 40s | summon | Howl, and a pair of timber wolves answer, fighting for you for a minute. |
| Fertile Ground * | 60 | 2 days | utility | Enrich the earth itself. Plain soil in the area becomes rich soil, permanently. |
| Wrath of Nature | 55 | 35s | attack | A wave of thorns and roots tears through the ground toward the target. |
| Earthquake | 60 | 40s | control | Stamp, and the earth answers. The ground heaves around you, knocking enemies flat and battering them with stone. |
| Nature's Renewal | 50 | 3 h | heal | Pour the patient strength of the forest into an ally: heals, cures an age-related or chronic condition and fades two scars with each casting. |
| Hornet Swarm | 30 | 12s | attack | Loose a swarm of eight giant hornets. They chase down enemies and sting them with venom. |

**Keystones:** *Overgrowth* - The wild turns violent. Nature spells hit harder and often entangle.; *Lifewell* - Everything you cast spills life into those around you.; *Archdruid* - The wild answers you unbidden: in combat, wolves arrive every thirty seconds.

### Beastmaster (subclass)

*Bears, wargs and thrumbos fight at your side. Tame anything that walks.*

Beastmasters gather a menagerie: summoned bears, wargs and, at the peak of their power, a thrumbo. They can tame any animal with a word.

**Playstyle:** Summoner with tons of bodies. Feral Command empowers every animal ally. Tame Beast brings home anything. Wants Focus and Attunement.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Summon Bear | 34 | 1.1 h | summon | A grizzly bear lumbers out of the wilderness to fight for you for two minutes. |
| Feral Command | 28 | 40s | buff | Every ally around you - especially beasts - fights with primal ferocity. |
| Tame Beast * | 40 | 12 h | utility | Speak to an animal in its own tongue. It joins the colony. |
| Stampede | 34 | 20s | attack | A spectral herd thunders toward the target, trampling and scattering everything in its path. |
| Thrumbo Call (ultimate) | 180 | 7.2 h | summon | The great white thrumbo answers your call, flanked by two wargs, for three minutes. |
| Alpha's Call * | 100 | 1 day | utility | Let out the call of the pack leader. Every animal in a wide area that isn't too wild comes to your side for good. |

**Keystones:** *Primal Pack* - Your allies always fight with feral ferocity near you.; *King of Beasts* - Every beast-summoning spell calls one more creature, and they all stay far longer.

### Verdant Warden (subclass)

*Groves that heal, walls of thorns and the ancient World Tree.*

Verdant Wardens shape living fortresses. Their groves heal, their thorn walls bleed attackers, and their spores choke enemies.

**Playstyle:** Defensive area control and healing. Plant a Healing Grove, wall off approaches with thorns, and raise the World Tree in desperate fights. Wants Focus and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Thorn Wall | 30 | 30s | utility | Grow a thicket of thorny walls around a point. Enemies near the walls are slashed. |
| Healing Grove | 38 | 1.2 h | heal | Plant a blessed sapling that heals every ally near it for a minute. |
| Spore Cloud | 30 | 22s | control | Release a cloud of choking spores that poison and disorient. |
| Nature's Grasp | 34 | 20s | attack | The whole area erupts with grasping roots and thorns. |
| The World Tree (ultimate) | 220 | 7.2 h | heal | Raise an echo of the World Tree. For two minutes it heals every ally in a huge radius, while roots bind every enemy near you. |
| Verdant Surge | 55 | 1.4 h | heal | A wave of green life rolls over your allies, healing them, purging poisons and fading a scar from each. |

**Keystones:** *Living Bastion* - Become an ancient tree: massive ward and armor, and everything that hurts you is torn by thorns.; *The Gardener* - Your groves come quickly and heal mightily.

### Shapeshifter (subclass)

*Become the beast. Bear, wolf and the mighty thrumbo.*

Shapeshifters wear the wild like a cloak. In bear form they are walls; in wolf form they are blurs; as a thrumbo, they are gods.

**Playstyle:** A melee bruiser. Shift into Bear Form to tank or Wolf Form to chase, Maul and Roar your way through enemies, and unleash Thrumbo Avatar for boss fights. Wants Resilience and Celerity.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Bear Form | 30 | 1.4 h | buff | Become a great bear for a minute: thick hide, crushing blows. |
| Wolf Form | 24 | 1.4 h | buff | Become a swift wolf for a minute. |
| Maul | 16 | 6s | attack | Savage an adjacent enemy with a brutal swipe that stuns. |
| Primal Roar | 30 | 30s | control | A roar that shakes the bones. Nearby enemies panic and flee, weakened. |
| Thrumbo Avatar (ultimate) | 160 | 5.8 h | buff | Take the form of the thrumbo, the god-beast of the rim: nearly invulnerable, regenerating, devastating. |
| Pounce | 22 | 10s | attack | Leap like a great cat onto a distant enemy, mauling them and knocking them off their feet. |

**Keystones:** *Spirit of Ursoc* - The great bear spirit protects you: when downed, you rise again in a roar. Once per day.; *Spirit of Fenris* - The wolf spirit hunts through you: weapon hits may Maul for free and every spell finishes the weak.

## Runesmith

*Inscribe power into the world. Turrets, wards, repairs, explosive runes and gold from stone.*

Runesmiths are engineers of magic. They etch runes into stone, steel and skin - and the runes do the rest.

**Playstyle:** A builder-caster with strong economy. Arcane Turrets and Explosive Runes control the field, Rune of Warding protects allies, and Transmutation pays the bills. Wants Focus and Celerity.

**Key attributes:** Focus, Celerity

### Runesmith spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Rune Bolt | 8 | 1.5s | attack | Fire a spinning rune that cuts through armor. |
| Rune of Warding | 22 | 25s | buff | Etch a warding rune onto an ally: a strong ward and runic armor. |
| Arcane Turret | 32 | 35s | summon | Conjure an arcane turret that fires rune bolts at enemies for a minute. |
| Explosive Rune | 24 | 10s | attack | Inscribe a rune on the ground that detonates three seconds later. |
| Mend * | 20 | 1 h | utility | Runes of mending knit broken things back together: walls, turrets, weapons and armor in an area. |
| Enchant Weapon | 20 | 40s | buff | Inscribe runes on an ally's weapon and nerves: faster aim, faster firing, better accuracy. |
| Transmutation * | 40 | 1 day | utility | Turn stone chunks in an area into silver. Every runesmith's favourite party trick. |
| Runic Barrage | 60 | 40s | attack | A storm of glowing runes slams down over an area, shattering shields and armor. |
| Midas Touch * | 55 | 1.4 h | attack | Inscribe a rune of gold on a creature. If it is weakened or small, it turns to solid gold where it stands. The mighty are only gilded, slowed and burned. |
| Rune Cannonade | 65 | 40s | attack | Inscribe a firing solution in the air. A battery of runic shells rains down on the target area, each one exploding and shredding armor. |
| Seeker Glyphs | 30 | 10s | attack | Four burning glyphs peel off your hand and hunt their targets, cracking armor where they strike. |

**Keystones:** *Overcharge* - Every rune burns hotter. Much more damage and armor penetration.; *Arcane Foundry* - Every construct spell builds one more construct, and they last much longer.; *Grand Architect* - Runes guard you even unbidden: being hit may inscribe a free Explosive Rune under the attacker.

### Golemancer (subclass)

*An army of constructs. Sentries, pylons, fortress walls and the Rune Colossus.*

Golemancers build their armies. Iron sentries, arc pylons and runic walls go up in seconds, and when the war comes, the Colossus wakes.

**Playstyle:** Turret-heavy summoner and fortress builder. Drop sentries and pylons, wall the approach and keep everything repaired. Wants Focus and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Iron Sentry | 40 | 40s | summon | Assemble a heavy runic sentry gun that fires piercing rune shells. |
| Arc Pylon | 34 | 35s | summon | Raise an arc pylon that pelts enemies with chained lightning. |
| Fortify | 30 | 40s | utility | A ring of runic walls slams up around the target point. |
| Maintenance Protocol | 30 | 1.1 h | utility | Mass repair: every structure and construct in a wide area is restored. |
| Colossus Protocol (ultimate) | 220 | 7.2 h | summon | Wake the Rune Colossus - a towering artillery construct - alongside two iron sentries. |
| Overdrive Protocol | 50 | 1.4 h | buff | Broadcast an overdrive glyph. Every ally nearby moves and fights faster and is patched up. |
| Drone Swarm | 40 | 1.2 h | attack | Four runic drones whirl around you for thirty seconds, grinding into anything that comes near and sundering its armor. |

**Keystones:** *Warforged* - Your constructs are built for war: half again as powerful.; *Endless Factory* - Your constructs last twice as long and can be rebuilt far sooner.

### Spellforger (subclass)

*Enchant your allies into legends. Wards, mana and the Grand Enchantment.*

Spellforgers enchant people instead of things. A spellforger's squad hits harder, shrugs off bullets, and never runs out of mana.

**Playstyle:** The best buffer in the game. Keep allies warded, enchanted and supplied with mana, dispel enemy magic, then drop the Grand Enchantment. Wants Focus and Attunement.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Empower Allies | 34 | 40s | buff | Every ally nearby is enchanted: rune-charged weapons and empowered magic. |
| Runeshield Array | 40 | 35s | buff | Project runeshields onto every ally nearby. |
| Disenchant | 28 | 18s | control | Strip the magic from everything in an area: enemy buffs, wards and shields are destroyed. |
| Mana Battery | 45 | 1.4 h | utility | Pour your stored power into an ally mage, restoring 40% of their mana. |
| Grand Enchantment (ultimate) | 220 | 7.2 h | buff | Every ally in a huge radius receives every enchantment you know at once: ironhide, rune-charged weapons, empowerment, haste and a massive ward. |
| Masterwork Rune * | 80 | 1 day | utility | Carve a rune of perfection into an item or the weapon a pawn is holding. Its quality rises by one step, up to legendary. |
| Magic Missile | 24 | 6s | attack | The oldest spell in the book, perfected: five darts of pure force that never miss and ignore armor entirely. |

**Keystones:** *Paragon of Runes* - Allies near you are continually warded and rune-charged.; *Arcane Well* - You are a font of mana: allied mages near you regenerate mana from your reserves.

### Arcane Gunslinger (subclass)

*Runic rounds and impossible shots. Magic for people who prefer guns.*

Arcane Gunslingers carve runes into their bullets. Ricochets that hunt targets, armor-piercing deadeye shots, and one final bullet that kills gods.

**Playstyle:** Ranged weapon specialist. Enchant your own aim, spread runic procs with your gun, and line up Deadeye and Final Shot on the biggest targets. Wants Celerity and Attunement.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Quickdraw | 22 | 40s | buff | Rune-charge your reflexes: haste and faster, truer shooting. |
| Ricochet | 24 | 8s | attack | A runic bullet that bounces between six targets. |
| Deadeye | 34 | 15s | attack | Take aim, read the runes, and fire a single perfect shot. Massive armor-piercing damage. |
| Bullet Storm | 30 | 14s | attack | Fan the hammer: a cone of runic bullets shreds everything in front of you. |
| The Final Shot (ultimate) | 180 | 4.8 h | attack | One bullet, inscribed with every rune you know. It ignores armor, breaks shields and tears a huge fraction of vitality from even the mightiest target. |
| Fan the Hammer | 30 | 12s | attack | Six runic shots in the blink of an eye, spread across the nearest enemies. |

**Keystones:** *Executioner* - No target walks away. Gun spells crit often, finish the wounded and hit bosses hard.; *Runic Akimbo* - Every shot you fire may be followed by a free Rune Bolt.

## Shadowmancer

*Hexes, stealth and terror. Strike from the dark and leave nothing but fear.*

Shadowmancers deal in what can't be seen: curses, stealth and the dread that turns brave raiders into fleeing children.

**Playstyle:** A cunning debuffer and burst damage dealer. Curse priority targets with Hex of Doom, terrify crowds, slip away with Cloak of Shadows. Wants Celerity and Potency.

**Key attributes:** Celerity, Potency

### Shadowmancer spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Shadow Bolt | 8 | 1.5s | attack | A bolt of living darkness. |
| Shadowstep | 14 | 10s | mobility | Step into one shadow and out of another, moving faster and harder to hit for a while. |
| Hex of Doom | 18 | 6s | control | Curse a target to suffer more from every wound. Recasting deepens the curse. |
| Cloak of Shadows | 30 | 1.4 h | buff | Wrap yourself in shadow and vanish from enemy sight for fifteen seconds. |
| Night Terror | 22 | 14s | control | Fill a mind with nightmares. The target flees in terror, weakened. |
| Shadow Sight * | 20 | 2 h | utility | Send your shadow out across the land. Everything hidden within a huge radius is revealed. |
| Leech | 20 | 8s | attack | Siphon life from a target through a tendril of shadow. |
| Umbral Eruption | 55 | 35s | attack | Darkness bursts from the ground around you, tearing and blinding everyone nearby. |
| Eclipse * | 80 | 1 day | utility | Swallow the sun. Darkness falls across the map for six hours, and every enemy near you is blinded by the sudden night. |
| Shadow Bind | 32 | 18s | control | Shadows lash out from the ground and drag every enemy in the area toward the center, holding them in place. |
| Shadow Blades | 36 | 40s | attack | Four knives of solid shadow circle you for twenty seconds, slashing and opening wounds on everyone who comes close. |

**Keystones:** *Living Nightmare* - Enemies near you are steadily cursed and weakened.; *Silhouette* - Strike from where they least expect. Huge spell critical chance and damage.; *Shadow Lord* - When struck, you may melt into the shadows for free.

### Assassin (subclass)

*Appear, kill, vanish. Executions, poisons and the Death Lotus.*

Assassins end fights before they start. They blink behind the most dangerous enemy, kill it, and are gone before anyone reacts.

**Playstyle:** Single-target burst and executions. Mark for Death, Backstab, vanish in Smoke Bomb, repeat. Wants Celerity and Attunement.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Backstab | 26 | 10s | attack | Blink behind the target and drive a shadow blade into it. Executes the badly wounded. |
| Toxic Fan | 22 | 8s | attack | Throw a fan of poisoned daggers. |
| Smoke Bomb | 28 | 40s | control | Shatter a vial of shadow smoke: enemies inside are blinded and you vanish. |
| Mark for Death | 20 | 16s | control | Mark a single enemy. Everything hurts it more, and it cannot hide. |
| Death Lotus (ultimate) | 170 | 4.8 h | attack | Become a whirlwind of shadow blades, flickering between up to nine enemies. Each strike executes the weakened. |
| Shadow Dance | 40 | 24s | attack | Flicker from enemy to enemy, appearing behind each one for a single killing cut. |
| Whirling Daggers | 30 | 30s | attack | Five poisoned daggers spin around you for fifteen seconds. Walk through a crowd and let them do the work. |

**Keystones:** *Executioner* - Shadow spells execute anything below a quarter of its health.; *Shadowdance* - Kills flow into the next: shadow kills often trigger a free Backstab on the nearest enemy.

### Hexer (subclass)

*Curses for every occasion. Agony, silence, and turning raiders into chickens.*

Hexers collect curses the way others collect coins. Agony, silence, bad luck, and - when they're feeling playful - polymorph.

**Playstyle:** Stack curses on dangerous enemies and neutralize the rest. Polymorph removes a threat entirely. Malediction cripples whole raids. Wants Attunement and Focus.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Curse of Agony | 18 | 5s | attack | A curse of creeping agony that grows worse with each casting. |
| Hex of Silence | 26 | 18s | control | Silence and stun the minds of everyone in an area. |
| Polymorph | 30 | 25s | control | Turn an enemy into a chicken for half a minute. If the chicken dies, so does the enemy. |
| Hex Swarm | 26 | 10s | attack | A swarm of hexes that each curse a different nearby enemy with something random and unpleasant. |
| Malediction (ultimate) | 200 | 6.2 h | attack | The great curse. Every enemy in a vast area is doomed, withered, weakened, silenced and wracked with agony. |
| Stone Gaze | 40 | 30s | control | Meet their eyes with the gaze of the gorgon. They turn to stone for ten seconds: helpless, but hard to hurt. |

**Keystones:** *Lord of Curses* - Every curse spills over: curse spells affect enemies around the target too.; *Barnyard* - Polymorph strikes extra targets, and curses sometimes polymorph for free. The farm grows.

### Hemomancer (subclass)

*Blood is power. Cast with your own life, take theirs in return.*

Hemomancers spend blood like mana: their own, their allies' and especially their enemies'.

**Playstyle:** High-risk, high-reward. Your spells can cost health, and your lifesteal pays it back. Blood Pact lets you keep casting with an empty mana pool. Wants Potency and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Blood Lance | 10 | 3s | attack | Form a spear of your own blood and hurl it. Costs health. |
| Hemorrhage | 24 | 14s | attack | Every enemy in an area begins to bleed uncontrollably. |
| Blood Frenzy | 20 | 1.1 h | buff | Boil your blood: faster attacks, harder hits and lifesteal. |
| Transfusion | 15 | 20s | heal | Give your own blood to save an ally: a huge heal for them, a wound for you. |
| Crimson Tide (ultimate) | 150 | 5.3 h | attack | Your blood erupts outward in a red wave: crushing damage to every enemy around you, and you drink half of it back. |
| Blood Harvest | 45 | 20s | attack | Pull the blood out of every enemy in an area through their skin. You drink a third of what you take. |
| Blood Siphon | 30 | 20s | attack | Draw a stream of blood out of a target for five seconds. Their wounds keep bleeding and you heal for half of everything you take. |

**Keystones:** *Sanguinarch* - The blood of your enemies flows to you: massive lifesteal and blood damage.; *The Undying Blood* - Your blood refuses to stop. When downed, you rise again fully healed. Once per day.

## Spellsinger

*Music is magic. Inspire allies, enthrall enemies, and keep the colony happy.*

Spellsingers weave magic into music. A war chant makes soldiers fearless, a lullaby drops raiders where they stand, and a good tune keeps the whole colony smiling.

**Playstyle:** The ultimate support with fantastic quality-of-life utility. Buff allies, put enemies to sleep, and keep moods high between fights. Wants Attunement and Focus.

**Key attributes:** Attunement, Focus

### Spellsinger spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Discordant Note | 8 | 1.5s | attack | A note so sour it hurts. May leave the target dazed. |
| Song of Rest | 24 | 20s | heal | A gentle melody: allies nearby heal a little and feel rested. |
| War Chant | 26 | 40s | buff | A thunderous war chant: allies nearby fight braver, harder and steadier. |
| Lullaby | 28 | 20s | control | A soft, irresistible lullaby. Enemies in the area fall asleep where they stand. |
| Inspire * | 40 | 1 day | utility | Sing a colonist a song made just for them. They gain a random inspiration. |
| Joyful Tune * | 30 | 6 h | utility | Play a cheerful tune for everyone nearby. Recreation is restored and spirits soar. |
| Sonic Boom | 22 | 10s | attack | Release a deafening blast of sound in front of you. |
| Ballad of Heroes | 60 | 1.4 h | buff | The song of legends. Every ally nearby gains valor, steadiness and a strong ward. |
| Siren's Call | 34 | 22s | control | Sing a note no one can resist. Enemies in the area stumble toward its center, entranced, and stand there listening. |
| Standing Ovation * | 90 | 2 days | utility | Give the performance of a lifetime. Every colonist who hears it is struck by inspiration. |
| Echoing Notes | 26 | 10s | attack | Sing five notes that take wing and chase enemies down, ringing in their skulls. |

**Keystones:** *Requiem* - Your music becomes a weapon: sonic magic hits much harder and resonates through machines.; *Grand Symphony* - Your songs carry further and linger far longer.; *Living Legend* - Songs are already being sung about you. Allies nearby in combat always have valor.

### Skald (subclass)

*Battle hymns and thunder chords. A war bard who leads from the front.*

Skalds sing on the battlefield, and their songs shake the ground. Their allies fight like heroes; their enemies hear their own dirge.

**Playstyle:** Front-line support. Keep Battle Hymn up, stun with Thunderous Chord, and drop the Song of Ragnarok on the big fights. Wants Attunement and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Battle Hymn | 36 | 40s | buff | Sing a battle hymn as you fight: for fifteen seconds allies near you gain valor and enemies near you are battered by sound. |
| Thunderous Chord | 30 | 16s | control | Strike a chord so loud it knocks enemies off their feet. |
| Rallying Cry | 30 | 1.1 h | heal | A cry that snaps allies out of panic and breakdowns, heals them and steels their nerves. |
| Dirge | 28 | 20s | control | A funeral song for enemies who aren't dead yet. They weaken and their doom approaches. |
| Song of Ragnarok (ultimate) | 200 | 6.2 h | buff | The end-of-the-world song. Allies become legends - valor, haste, power and wards - while enemies are shattered by sound. |
| Song of the Undying | 110 | 7.2 h | buff | A saga so stirring that death itself waits for the last verse. For thirty seconds, allies around you who fall are pulled back to life. |

**Keystones:** *Einherjar* - The song is not over. When downed, rise again in a thunderous chord. Once per day.; *War Band* - Your warband never stops fighting: allies nearby always have valor and a trickle of ward.

### Illusionist (subclass)

*Charm, mesmerize and deceive. Turn enemies into friends - temporarily or permanently.*

Illusionists weave lies so beautiful the world believes them. Enemies fight for them, guards sleep, and prisoners suddenly decide to join.

**Playstyle:** Mind control and recruitment. Charm the most dangerous enemy, Mesmerize the rest, and Silver Tongue your prisoners into colonists. Wants Attunement and Focus.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Charm | 34 | 25s | control | Charm a single enemy into fighting for you for thirty seconds. |
| Mesmerize | 30 | 22s | control | A dazzling display of light and color. Enemies in the area stand transfixed. |
| Mirage | 30 | 1.2 h | buff | Fold light around yourself. You vanish, and allies nearby become harder to hit. |
| Silver Tongue * | 60 | 1 day | utility | Talk a prisoner, a downed enemy or a visitor into joining the colony on the spot. |
| Mass Domination (ultimate) | 220 | 6.7 h | control | Bend every lesser mind in an area to your will for a full minute. Bosses resist. |
| Plagiarize * | 60 | 1 day | utility | Reach into someone's mind and take what they know. A large chunk of their best skill's experience becomes yours. |

**Keystones:** *Mastermind* - Your illusions reach further and hold far longer.; *Enthrall* - Enemies who come close to you periodically freeze, transfixed.

### Jester (subclass)

*Chaos magic, custard pies and terrible luck - for your enemies, mostly.*

Jesters treat magic as a joke, and the joke is usually on the enemy. Their spells are random, ridiculous and frequently devastating.

**Playstyle:** Embrace randomness. Wild Magic and Grand Finale can do anything, Lucky Coin makes allies crit, and the pies never stop. Wants Attunement and Celerity.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Pie in the Face | 10 | 4s | attack | Conjure and throw a custard pie. Humiliating, blinding and mildly concussive. |
| Wild Magic | 24 | 8s | attack | Something happens! Each target nearby suffers - or enjoys - a random magical effect. |
| Lucky Coin | 18 | 30s | buff | Flip an ally a lucky coin. Heads: they crit more and dodge better. Tails: also that. |
| Confetti Cannon | 26 | 14s | attack | A deafening cannon of confetti. Enemies in front are stunned and concussed; it is very festive. |
| Grand Finale (ultimate) | 180 | 5.8 h | attack | The show-stopper. Twenty random magical effects explode over a huge area. Nobody, including the jester, knows what will happen. |
| Banana Peels * | 26 | 25s | control | Scatter conjured banana peels across an area. For fifteen seconds, enemies who walk through keep slipping over. |
| Juggling Knives * | 25 | 30s | attack | Juggle three knives around yourself at alarming speed. Anyone who gets close gets cut, and occasionally a custard pie flies out of the act. |

**Keystones:** *The House Always Wins* - Luck is a skill. Huge crit chance and spells often refund themselves.; *Pandemonium* - Chaos follows you everywhere: Wild Magic erupts around a random enemy every five seconds in combat.

## Harbinger

*Prophet of the end. Lay Omens on your enemies, then break the Seventh Seal and watch it chain through the crowd.*

Harbingers do not so much fight as announce. Every curse they speak is a promise, and every promise is kept.

**Playstyle:** Build Omens with crows, prophecies and your allies' own gunfire (Fated), then cash them in: Toll the Bell for a single kill, Last Rites for a crowd, or let seven of them break the Seventh Seal, which ignores armour, scales with the victim's own vitality and splashes Omens onto its friends. Wants Focus and Potency.

**Key attributes:** Focus, Potency

### Harbinger spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Ill Omen | 8 | 1.5s | attack | Send a crow of ill omen. Its beak deals doom damage that ignores armour and lays an Omen. Seven Omens break the Seventh Seal. |
| Murder of Crows | 24 | 10s | attack | Release six crows that wheel through the air and fall on the nearest enemies, each one laying an Omen. |
| Toll the Bell | 22 | 9s | attack | A single, terrible note for a single enemy: doom damage, plus 35% for every Omen it carries. The Omens are spent. |
| Prophecy of Ruin | 18 | 14s | control | Foretell an enemy's death and lay two Omens. If it dies within thirty seconds you regain a quarter of your mana, your other spells cool down by a third, and its doom spreads: three Omens on every enemy within six cells. |
| Sentence | 30 | 20s | control | Pass sentence on an enemy. In ten seconds, half of every wound it took in the meantime is dealt again, plus 3% of its vitality for each Omen it carries. |
| Fated | 20 | 16s | control | Tie an enemy's fate to its wounds for twenty seconds: every time anything hurts it, it gains an Omen. Your allies' gunfire becomes prophecy. |
| Apocalyptic Vision | 28 | 22s | control | Show everyone in front of you how the world ends. Enemies in a wide cone flee in terror for six seconds and gain two Omens. |
| Black Sun | 45 | 40s | control | Put out the sun over an area for fifteen seconds. Enemies beneath it can barely see and gain an Omen every two seconds. |
| Doomsday Clock | 40 | 26s | attack | A spectral clock appears over an area and strikes midnight three seconds later: everything beneath takes heavy doom damage, +15% per Omen, and gains another. |
| Last Rites | 50 | 35s | attack | Read the last rites over everyone around you. Every enemy within six cells takes doom damage, +30% per Omen (spent), and you are healed for a quarter of the damage. |
| Memento Mori * | 30 | 1 day | utility | Whisper to a colonist the exact day of their death. Unsettled and inspired, they throw themselves into their work. |

**Keystones:** *Prophet of the End* - Every death you cause is foretold. Kills often prophesy the ruin of the nearest enemy for free, and your doom hits harder.; *End Times* - Omens fall like rain. Half your Doom hits lay an extra Omen, and every Doom spell costs a fifth less mana.; *Herald of the Seventh Seal* - Death announces you. Anything you kill may burst in a wave of doom that ignores armour.

### Doomsayer (subclass)

*Speak the end aloud. Doom clocks, shared fates, words that unmake, and the Apocalypse itself.*

Doomsayers have stopped predicting the end and started scheduling it.

**Playstyle:** The purest Omen engine. Words of Unmaking spreads Omens down a chain, Shared Fate links a crowd so one Seventh Seal hurts them all, and Final Hour deletes a single target. The Apocalypse puts three Omens on everything on the map. Wants Focus and Attunement.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Words of Unmaking | 32 | 14s | attack | Speak a word that should not be spoken. It leaps between six enemies, searing each one and laying two Omens. |
| Final Hour | 45 | 24s | attack | Hang a doom clock over one enemy. When it strikes four seconds later the enemy takes massive doom damage, +20% per Omen (spent), plus 8% of its vitality. Nothing outruns its final hour. |
| Shared Fate | 40 | 30s | control | Bind every enemy in an area to one fate for fifteen seconds: half of any wound one of them takes is dealt to all the others. One Seventh Seal becomes a massacre. |
| Doom Spiral | 30 | 18s | attack | Stare into an enemy's soul and unravel it: a beam that searing doom along its length and lays an Omen every half second for four seconds. |
| Cheat Fate | 60 | 7.2 h | buff | Rewrite an ally's fate. For the next minute, if they would die, they rise again instead. |
| Apocalypse (ultimate) | 220 | 7.2 h | attack | Announce the end of the world. Every enemy on the map takes doom damage and gains three Omens; three seconds later the sky answers, and every Omen on the map is tolled at once for 35% extra damage each. |

**Keystones:** *Voice of the End* - Every word is final. Final Hour and Words of Unmaking recharge far faster and all your doom hits harder.; *Unwritten* - Your own fate is unwritten. Once a day, when you would die, you rise again in a wave of doom. Cheat Fate recharges twice as fast.

### Pale Rider (subclass)

*Behold, a pale horse. Reap with a scythe of pale fire, ride through the enemy line, and harvest their souls.*

The fourth horseman, or someone who borrowed his horse. Pale Riders fight up close and grow stronger with every death.

**Playstyle:** A front-line reaper. Kills store Souls (more power and speed); Soul Harvest spends them in a huge nova. Reap executes the weak, Ride Them Down tramples a path through a line, and Death Rides blinks you from enemy to enemy. Wants Potency and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Reap | 18 | 6s | attack | Swing a scythe of pale fire in a wide arc. Executes enemies below 20% health and deals triple damage to anything downed or stunned. |
| Spectral Steed | 20 | 1.4 h | buff | Mount a spectral pale horse for thirty seconds: much faster, harder to stop, and slightly harder to hurt. |
| Ride Them Down | 26 | 14s | mobility | Ride through the enemy line to the target point, trampling everything in your path for doom damage and knocking it aside. |
| Soul Harvest | 40 | 25s | attack | Spend every harvested soul in one blast of pale fire around you: +15% power per soul (up to +150%), healing you for a fifth of the damage. Some survivors flee in terror. |
| Deathly Pallor | 34 | 40s | control | Wear death like a cloak for twenty seconds. Enemies near you are slowed by the chill of the grave and gain an Omen every two seconds. |
| Death Rides (ultimate) | 200 | 5.8 h | attack | Become Death for thirty seconds and ride through the battlefield: blink between up to ten enemies, reaping each one for massive doom damage. Anything below 30% health dies outright. Spends your souls for extra power. |

**Keystones:** *Horseman of Death* - You never dismount. Spectral Steed lasts twice as long and returns twice as often, and Ride Them Down hits harder and recharges fast.; *Grim Reaper* - Every swing is a harvest. Reap recharges twice as fast and anything you hit below 15% health dies.

### Cataclysm (subclass)

*The end of days, on demand. Earthquakes, the bitter star Wormwood, plagues of locusts and the Seven Seals.*

Some Harbingers announce the end. Cataclysms bring it, one disaster at a time.

**Playstyle:** Huge, slow, map-shaking area damage that also wrecks buildings and turrets. Split the Earth flings a line of enemies into the air, Wormwood poisons the ground, the Blood Moon lays Omens on everything on the map, and the Seven Seals unfold seven different calamities over one spot. Wants Potency and Attunement.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Split the Earth | 34 | 16s | attack | Split the earth in a line. The ground erupts beneath everything in the way, flinging enemies into the air and cracking walls and turrets. |
| Locust Swarm | 42 | 30s | control | A plague of locusts descends on an area for fifteen seconds, devouring enemies, stripping their armour and sometimes laying Omens. |
| Wormwood | 60 | 40s | attack | Call down Wormwood, the bitter star. It smashes into an area and poisons the ground for twenty seconds. |
| Hailfire | 55 | 30s | attack | Hail and fire mixed with blood rain down on an area: a dozen strikes, each one burning and sometimes laying an Omen. |
| Blood Moon | 70 | 2.9 h | control | Turn the moon to blood for thirty seconds. Every enemy on the map gains an Omen every five seconds. |
| The Seven Seals (ultimate) | 240 | 7.2 h | attack | Break the seven seals over an area, one after another: conquest, war, famine, death, the martyrs' vengeance, the great quake and the final silence. Each seal is a different calamity. |

**Keystones:** *Armageddon* - Everything you call down is bigger. Much bigger.; *Extinction* - Every death is a small apocalypse: kills often explode in doom, and your calamities recharge faster.

## Wind Mage

*Throw the battlefield around. Hurl enemies through the air into walls, steal their breath and blow their bullets away.*

Wind Mages treat a fight like weather: something that happens to other people. Their enemies spend most of it in the air.

**Playstyle:** Positioning is a weapon. Gust, Hurl and Sky Toss really throw enemies: the farther they fly, the harder they land, and walls hurt most of all. Vacuum Bubble silences and suffocates, Tailwind and Wind Wall turn bullets aside, and the Cyclone sucks a whole squad in before blasting it apart. Wants Celerity and Potency.

**Key attributes:** Celerity, Potency

### Wind Mage spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Gust | 8 | 1.5s | attack | A blade of wind that cuts and shoves its target back three cells. Anything shoved into a wall takes far more damage. |
| Updraft | 18 | 10s | control | A column of wind pops everything in a small area into the air. They come down hard and stunned. |
| Hurl | 22 | 12s | attack | Grab an enemy with a fist of wind and hurl it ten cells. It hurts whatever it lands on, and walls hurt most of all. |
| Zephyr Dash | 14 | 8s | mobility | Ride a gust to the target spot. You land in a burst of wind that knocks everyone around you back. |
| Air Lance | 24 | 9s | attack | Compress the air into a lance and drive it through everything in a straight line, punching through armour and shoving targets aside. |
| Vacuum Bubble | 26 | 18s | control | Pull all the air away from an enemy's head for eight seconds. It suffocates, cannot call out, and cannot cast spells. |
| Tailwind | 18 | 25s | buff | Wrap an ally in a tailwind for thirty seconds: much faster, and a third of incoming shots are blown off course. |
| Wind Wall | 34 | 30s | control | Raise a line of standing gales for thirty seconds. Bullets, arrows and shells that cross it are torn apart. |
| Snatch | 20 | 30s | control | A cheeky gust plucks the weapon out of an enemy's hands and drops it at your feet. Mechanoids just get buffeted. |
| Sky Toss | 45 | 30s | attack | Every enemy around you is thrown high into the air and slammed back into the ground. |
| Cyclone | 60 | 1.1 h | attack | Spin up a cyclone over an area. For eight seconds it drags enemies toward its heart and batters them; then it explodes, blasting everything inside outward. |
| Dust Devil | 30 | 24s | control | Trap an enemy in its own little cyclone for seven seconds. It is thrown into the air every second and a half, and lands hard every time. |
| Cleansing Wind * | 60 | 2 days | utility | Call a great wind across the whole map. It blows away toxic fallout and volcanic ash, clears the sky and snuffs out the fires around you. |

**Keystones:** *Storm Chaser* - Ride every storm. Cyclone and Sky Toss come back far sooner and grow larger, and you move much faster.; *Eye of Calm* - You stand in the calm eye of your own storm. Ranged attacks lose a third of their force against you, and part of every blow is blown back at the attacker.; *Tempestuous* - The wind never stops. Your wind spells often loose a free Gust at a random enemy.

### Windblade (subclass)

*Become the wind with an edge. Blink from enemy to enemy, cut a hundred times a second.*

Windblades learned that air moving fast enough is a sword, and that a sword moving fast enough is everywhere at once.

**Playstyle:** A blinking duelist. Gale Step and Hundredfold Gale teleport you from enemy to enemy, cutting each one; Thousand Cuts shreds a single target; Eye of the Storm turns half of all attacks aside and throws some back. Wants Celerity and Potency.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Gale Step | 30 | 12s | attack | Become the wind and blink through up to five enemies, appearing behind each one and cutting it. Wounds may keep bleeding. |
| Thousand Cuts | 26 | 10s | attack | A storm of tiny blades descends on one enemy: twelve cuts in a heartbeat, each one hard to stop. |
| Razor Gale | 30 | 14s | attack | A fan of razor wind tears through everything in front of you, shoving enemies back and leaving them bleeding. |
| Crosswind | 28 | 12s | attack | Loose eight wind blades that curve through the air and find their own targets, each one cutting and shoving. |
| Eye of the Storm | 35 | 1.1 h | buff | Stand in the calm eye of a storm of blades for twenty seconds. Half of all attacks, ranged or melee, are turned aside and part of their force thrown back. You move and strike faster. |
| Hundredfold Gale (ultimate) | 200 | 5.8 h | attack | Dissolve into the wind and blink between up to fifteen enemies in a few seconds, cutting each one deep. You stand in the eye of the storm when it ends. |

**Keystones:** *Wind Dancer* - You never stop moving. Gale Step returns twice as fast and blinks through two more enemies.; *Honed Edge* - Every cut finds the gap in the armour. Huge critical chance and armour piercing.

### Pressure Sage (subclass)

*Weaponise the weight of the sky. Implosions, crushing pressure and the air stolen from an entire map.*

Pressure Sages understand that the atmosphere weighs a great deal, and that it can be persuaded to sit on people.

**Playstyle:** Heavy control and the best answer to giant machines: Crushing Depth deals damage scaled by body size, Implosion sucks a group together and blows it apart, Dead Calm suffocates an area, and Dead Air steals the breath of every enemy on the map. Wants Potency and Focus.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Crushing Depth | 34 | 14s | attack | Crush an enemy under the weight of the sky. Damage scales with body size: a centipede takes several times what a man does. It is slowed afterwards. |
| Eardrum Burst | 30 | 18s | control | A sudden spike of pressure around you bursts eardrums: enemies close by are concussed and stunned. |
| Implosion | 40 | 22s | attack | Collapse the air over an area: everything inside is sucked toward the centre, then the pressure snaps back and blasts it all outward. |
| Pressure Lock | 28 | 20s | control | Pin an enemy in place under crushing pressure for six seconds; every blow against it lands harder. Machines cannot be held down for long, but they stagger. |
| Dead Calm | 50 | 40s | control | Still the air over an area for twelve seconds. There is nothing to breathe: every enemy inside suffocates. |
| Dead Air (ultimate) | 220 | 7.2 h | attack | Steal the breath of every enemy on the map. They suffocate, are crushed by the weight of the sky (big ones hardest) and stagger for a moment. |

**Keystones:** *Vacuum* - Your silences last far longer, and every breath you steal for good returns mana to you.; *Crushing* - Everything you do weighs more. Much more damage, and far more against machines.

### Skywarden (subclass)

*Guardian of the open sky. Domes of wind that eat bullets, rescues through the air, and walls of gale.*

Skywardens stand between their friends and the guns, and the guns lose.

**Playstyle:** The best protector against ranged armies. Aegis of Gales raises a dome that destroys hostile shots and throws their force back; Gale Guard does the same for one ally; Rescue lifts an ally (even a downed one) through the air to your side. Heaven's Bulwark covers a whole battle line. Wants Resilience and Celerity.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Aegis of Gales | 50 | 1.4 h | buff | Raise a spinning dome of wind for forty seconds. Hostile shots fired into it from outside are torn apart, and half their force is thrown back at the shooter. |
| Rescue | 16 | 12s | utility | Lift an ally out of danger on a gust of wind and set them down beside you. Works on the fallen, too. |
| Gale Guard | 30 | 30s | buff | Wrap an ally in a guard of wind for twenty-five seconds: most bullets are turned aside and their force thrown back. Adds a ward. |
| Slipstream | 40 | 1.1 h | buff | Every ally around you rides your slipstream for thirty seconds: hastened, and a third of incoming shots blown aside. |
| Skyfall | 36 | 20s | attack | Leap into the sky and come down like a hammer: everything where you land is crushed and blasted outward. |
| Heaven's Bulwark (ultimate) | 200 | 7.2 h | buff | The sky itself stands guard. Raise a vast dome of wind for a full minute that tears apart every hostile shot fired into it and throws the whole force back at the shooter. Allies near you gain a ward and a guard of wind. |

**Keystones:** *Aegis* - Your domes and guards come back far sooner and last half again as long.; *Storm Shield* - Whoever strikes you is struck back by the wind, hard. Much larger ward.

## Succubus

*Win every fight by making the enemy fall in love. Infatuate, enthrall, and let their own army do the killing.*

Succubi are demons of desire, or simply very persuasive mages. The difference matters little to the raiders who switch sides mid-battle.

**Playstyle:** Stack Infatuation with kisses, charms and the Siren's Call. At five stacks a target is Enthralled and fights for you for twenty seconds; bosses are stunned instead. Heartbreak spends the stacks for damage, Lovers' Quarrel turns a crowd against itself, and Kiss of Youth keeps you young forever. Only women can take this path. Wants Attunement and Focus.

**Key attributes:** Attunement, Focus

### Succubus spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Blown Kiss | 8 | 1.5s | attack | Blow a kiss that flies like an arrow: it stings with heartbreak and leaves the target infatuated. |
| Captivate | 20 | 12s | control | Captivate an enemy completely: for eight seconds it walks to your side and will not fight unless badly hurt. Two stacks of Infatuation. |
| Siren Song | 26 | 20s | control | Sing a note no one can resist. Every enemy within six cells drops what it is doing and walks toward you, dazed, and gains Infatuation. A hard blow wakes them. |
| Heartbreak | 24 | 10s | attack | Break a heart that is already yours: heartbreak damage, +50% for every stack of Infatuation on the target. The stacks are spent. |
| Kiss of Youth | 30 | 30s | attack | Steal a kiss and the years that come with it. The target ages several years and is hurt; you grow a little younger. |
| Glamour | 24 | 35s | buff | Weave a glamour around yourself or an ally for thirty seconds. Enemies can hardly bring themselves to hurt the wearer: a quarter of all attacks simply fail. |
| Ardor | 22 | 30s | buff | Fill an ally with burning ardor for forty seconds: harder blows, steadier aim, no fear of pain. |
| Lovers' Quarrel | 40 | 35s | control | Whisper a terrible rumour into a crowd. Enemies in the area fly into a jealous rage and attack whoever is nearest. |
| Essence Feast | 40 | 30s | attack | Drink from every heart around you. Enemies within eight cells take heartbreak damage, two and a half times as much if they are infatuated, and you are healed for half of it. |
| Stolen Heartbeat | 50 | 40s | attack | Reach into an enemy's chest and stop its heart for a moment: heartbreak damage, a share of its vitality, more for every stack of Infatuation (spent), and a long stun. |
| Irresistible * | 80 | 3 days | utility | Turn the full force of your charm on a downed enemy or a prisoner. They fall hopelessly in love and join the colony. |

**Keystones:** *Femme Fatale* - Love kills. Heartbreak and Stolen Heartbeat come back far sooner and all your desire hits harder.; *Beloved of All* - Everyone falls for you. Most of your spells lay an extra stack of Infatuation, and your charms last longer.; *Queen of Hearts* - Hearts flutter wherever you walk: enemies near you steadily become infatuated.

### Temptress (subclass)

*Make them yours. Enthral single targets, seduce whole squads, and lead an army of the damned.*

A Temptress does not win battles; she collects them.

**Playstyle:** The domination specialist. Enthrall and Heartstrings pile on Infatuation, Mass Seduction turns an area into a trap of hearts, Kiss of Betrayal takes an enemy for your own outright, and the Harem of the Damned enthralls every enemy around you at once. Wants Attunement and Celerity.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Enthrall | 30 | 16s | control | Hold an enemy's gaze until it forgets everything else: transfixed for six seconds, and three stacks of Infatuation. |
| Heartstrings | 28 | 12s | attack | Pluck the heartstrings of seven enemies in a chain: each one is stung and grows more infatuated. |
| Jealousy | 26 | 22s | control | Plant jealousy in one enemy's heart: it flies into a rage against its own side and grows more infatuated with you. |
| Mass Seduction | 50 | 1.1 h | control | Turn an area into a garden of temptation for fifteen seconds: every enemy inside gains Infatuation every two seconds. |
| Kiss of Betrayal | 60 | 1.4 h | control | A kiss that rewrites loyalty. For fifteen seconds an enemy (not too large, and never a boss) fights for you. |
| Harem of the Damned (ultimate) | 220 | 7.2 h | attack | Reveal your true face. Every enemy within twelve cells is overwhelmed with five stacks of Infatuation: all but the mightiest are enthralled and fight for you. |

**Keystones:** *Siren Queen* - Your charms last half again as long and reach farther.; *Heartless* - When your lovers die, their broken hearts burst and wound everyone nearby.

### Thief of Years (subclass)

*Beauty is eternal if someone else pays for it. Steal years from enemies, give them to friends, never grow old.*

Thieves of Years drink time itself. Their enemies wither into grey old men in a single battle; their friends grow young again.

**Playstyle:** A drain-and-sustain mage. Withering Kiss and Time's Toll age enemies until their bodies fail, Decay keeps aging them for fifteen seconds, and the stolen years heal and rejuvenate your allies. Eternal Beauty ages a whole battlefield by a decade and makes you deathless. Wants Focus and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Withering Kiss | 34 | 20s | attack | A long, cold kiss: the target ages five years, may develop the ailments of old age, and very old enemies crumble. You grow a year younger. |
| Stolen Youth | 40 | 1.4 h | heal | Pour stolen years into an ally: they grow three years younger, an old ailment may lift, scars fade and wounds close. |
| Time's Toll | 44 | 30s | attack | Collect what time is owed from an area: everyone inside ages three years, slows to a crawl and takes heartbreak damage. |
| Decay | 36 | 30s | control | Set an enemy's years running: for fifteen seconds it ages a year every three seconds, and the years hurt. |
| Spring of Youth | 45 | 1.1 h | heal | Spend stolen youth on everyone around you: allies within four cells heal, regenerate and lose a scar. |
| Eternal Beauty (ultimate) | 220 | 7.2 h | attack | Drink a whole battlefield's worth of youth. Every enemy within ten cells ages twelve years (the old simply crumble) and takes heartbreak damage; you grow five years younger and cannot die for a minute. |

**Keystones:** *Methuselah* - You have outlived death before. Once a day, when you would die, you rise again young and whole.; *Graverobber* - You take far more than years. Heavy lifesteal and more damage.

### Nightmare Muse (subclass)

*Sweet dreams and night terrors. Sing enemies to sleep, feed on their dreams, wake their nightmares.*

A Nightmare Muse visits people in their sleep. Since she started doing it on the battlefield, nobody gets much sleep there.

**Playstyle:** Control through sleep. Sleepsong and Endless Night put enemies into a dream that only pain can break; Dream Eater deals triple damage to dreamers, Dread Dream sends them fleeing while the nightmare eats them, and Sleepwalk sets them wandering. Wants Attunement and Focus.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Sleepsong | 30 | 20s | control | Sing a lullaby over an area. Enemies inside fall into a dream for ten seconds and stand where they are until something hurts them. |
| Dream Eater | 26 | 10s | attack | Feed on an enemy's dreams: heartbreak damage, tripled against a dreamer, healing you for half and restoring mana. |
| Dread Dream | 28 | 16s | control | Wake the worst nightmare an enemy has ever had. It flees in terror for six seconds while the dream eats at it. |
| Sleepwalk | 34 | 25s | control | Enemies in an area fall into a walking sleep for eight seconds and wander off, unaware of the battle. |
| Lucid Dream | 26 | 35s | buff | Let an ally fight as if in a lucid dream for thirty seconds: time slows, aim sharpens and pain becomes just an idea. |
| Endless Night (ultimate) | 220 | 7.2 h | attack | Every enemy on the map falls into a deep dream. Five seconds later you feast on every dream at once: heartbreak damage, tripled against those still dreaming. |

**Keystones:** *Sandman's Song* - Your lullabies carry much farther and come back sooner.; *Nightmare Queen* - Dream Eater feasts on three dreamers at once, and every dream you touch bites harder.

## Incubus

*Every deal has a price. Put enemies in debt and collect, sign contracts with your friends, and steal what you covet.*

Incubi are devils of the bargain. Everything has a price, and they are very, very good at collecting.

**Playstyle:** Lay Debts with hellfire brands, chains and commands. At five Debts the Incubus collects: hellfire, a share of the victim's vitality, and the weapon out of its hands. Devil's Due calls in every Debt on the map at once. Infernal Contracts make an ally monstrously strong if they deliver two kills. Only men can take this path. Wants Potency and Attunement.

**Key attributes:** Potency, Attunement

### Incubus spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Hellfire Brand | 8 | 1.5s | attack | Fling a brand of violet hellfire. It burns, and it marks the target as owing you: one Debt. |
| Covet | 20 | 25s | control | Covet an enemy's weapon, and it is yours: it tears out of their hands and lands at your feet. The enemy owes you for the trouble. |
| Command: Kneel | 24 | 16s | control | Speak with the voice of a prince of hell. The target kneels for five seconds, takes more damage while it does, and owes you two Debts. |
| Hellchains | 30 | 18s | control | Burning chains erupt from the ground in an area, binding every enemy there for six seconds and burning them. One Debt each. |
| Collect | 24 | 10s | attack | Call in a debt early: hellfire damage, +50% for every Debt the target owes (all of them spent). |
| Infernal Contract | 30 | 40s | buff | Offer an ally a contract: for forty seconds they are monstrously strong. If they make two kills before it runs out they are rewarded; if not, the devil collects. |
| Pact of Flame | 24 | 1.1 h | buff | Make a pact with hellfire for thirty seconds: immune to burning, and your spells burn hotter. |
| Soul Bargain | 0 | 1.4 h | utility | Sell a little of your own soul: lose some health, regain two fifths of your mana at once. |
| Brimstone | 46 | 28s | attack | Brimstone erupts from the earth in an area: eight burning strikes, each one laying a Debt half the time. |
| Devil's Due | 70 | 1.4 h | attack | Call in every debt at once. Every enemy on the map takes hellfire damage, +60% for every Debt it owes (spent). |
| Deal With the Devil * | 40 | 1 day | utility | Offer a colonist a deal. They will gain something wonderful: an inspiration, years of skill, a healed body or fantastic luck. Usually. |

**Keystones:** *Loan Shark* - Payment is due now. Devil's Due and Collect come back far sooner, and everything infernal hits harder.; *Prince of Lies* - Everyone owes you, and nobody can pin you down: half your infernal hits add a Debt, and attacks often find only an illusion.; *Archdevil* - Hell follows you. Anything you kill may burst into hellfire.

### Pactbinder (subclass)

*Every soul has a price, and every price has interest. Usury, collateral, blood contracts and foreclosure.*

Pactbinders are the accountants of hell, and nothing in hell is more frightening than an accountant.

**Playstyle:** The Debt engine. Usury makes Debts accrue on their own, Soul Collateral pays you when a debtor dies, Breach of Contract cashes Debts in for enormous single-target damage, and Foreclosure collects from everyone around you at once. Blood Contracts make an ally a demigod for a minute. Wants Potency and Focus.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Usury | 26 | 18s | control | Charge interest on an enemy's soul: for twenty seconds it gains a Debt every two seconds. |
| Soul Collateral | 30 | 25s | control | Take an enemy's soul as collateral and lay two Debts. If it dies within thirty seconds, you regain mana and a ward, your other spells cool down by half, and its friends inherit the debt. |
| Breach of Contract | 40 | 20s | attack | Punish a defaulter: hellfire damage, +40% for every Debt (spent), plus a share of its vitality. |
| Garnishment | 36 | 25s | attack | Garnish the wages of everyone around you: hellfire damage, doubled against anyone in debt, healing you for two fifths. |
| Blood Contract | 40 | 1.4 h | buff | Offer an ally a contract written in a great deal of blood: overwhelming power for a minute, paid for with three kills. |
| Foreclosure (ultimate) | 220 | 7.2 h | attack | Foreclose on every soul within fifteen cells: each one is driven five Debts deeper and collected at once. |

**Keystones:** *Hostile Takeover* - Every infernal hit adds a Debt (at most twice a second).; *Bankrupt* - Cash out constantly. Breach of Contract and Garnishment come back far sooner, and everything infernal hits harder.

### Dreamlord (subclass)

*He visits in the night. Possess enemies through their dreams, plant nightmares that spread, and rule the dreaming world.*

The incubus of legend comes in dreams. The Dreamlord just does it to whole armies.

**Playstyle:** Possession and spreading nightmares. Possess takes an enemy for thirty seconds; Nightmare Seed eats its host and jumps to everyone nearby when the host dies; Sleep Paralysis pins a target in place. The Lord of Dreams possesses or paralyses every enemy on the map. Wants Attunement and Focus.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Nightmare Seed | 30 | 15s | control | Plant a nightmare in an enemy. It feeds on its host for twenty seconds, and if the host dies it seeds itself in everyone nearby. |
| Sleep Paralysis | 34 | 24s | control | Something sits on an enemy's chest: it cannot move for eight seconds and suffers. Machines do not dream. |
| Waking Nightmare | 36 | 25s | control | Pull the nightmares of everyone in an area into the waking world: they flee in terror for seven seconds. |
| Dreamshroud | 30 | 1.4 h | buff | Step halfway into the dreaming world for fifteen seconds: enemies cannot see you. |
| Possess | 70 | 2.2 h | control | Slip into an enemy's dreams and take the wheel: for thirty seconds it fights for you. Not the mightiest, and not machines too big to dream. |
| Lord of Dreams (ultimate) | 230 | 7.2 h | attack | Rule the dreaming world. Every enemy on the map is either possessed for twenty seconds or paralysed in its sleep, and all of them owe you. |

**Keystones:** *Dream Tyrant* - Possess takes two minds at once and returns sooner.; *Nightmare Lord* - Every nightmare you plant is deeper and crueller.

### Hellprince (subclass)

*Stop pretending. Take your true form, drag enemies in with a lash of hellfire, and open the gates of hell.*

Some incubi bargain. Hellprinces remember who they were before they learned to smile, and they do not bargain.

**Playstyle:** A burning brawler. Hellfire Lash drags enemies to you, Infernal Form doubles your strength and armours you in hellfire, Hellhounds and Hellgates scorch everything near them, and the Crown of Ash turns you into a walking inferno that lays Debts on everything it burns. Wants Potency and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Hellfire Lash | 22 | 10s | attack | Crack a whip of hellfire at an enemy and drag it through the air to your feet, burning. |
| Infernal Form | 60 | 2.9 h | buff | Take your true shape for forty seconds: horns and hellfire, twice the strength, heavy armour and immunity to fire. |
| Hellhounds | 50 | 2.2 h | summon | Whistle up two hellhounds for a minute. They are savage, immune to fire and scorch everything they stand near. |
| Rain of Brimstone | 70 | 1.1 h | attack | Five burning stones fall from the sky on and around the target. |
| Hellgate | 50 | 1.4 h | summon | Tear open a crack into hell for thirty seconds. It breathes hellfire on every enemy near it and lays Debts on them. |
| Crown of Ash (ultimate) | 220 | 7.2 h | control | Crown yourself in hellfire for thirty seconds: you take your infernal form, and everything within six cells of you burns and falls into debt every second and a half. |

**Keystones:** *Warlord of Hell* - You barely remember your mortal shape. Infernal Form lasts twice as long and returns much sooner.; *Pyrelord* - Everything you burn burns hotter, and the dead burst into hellfire.

## Noclipper

*You fell out of reality once and found the way back. Walk through walls, trap enemies in yellow mazes, and send them where you went.*

Noclippers have been to the Backrooms: the endless, humming, yellow levels behind the world. They came back with a few habits.

**Playstyle:** Lay Lost with static and wrong turns; at four stacks an enemy noclips out of reality for six seconds and comes back wandering. Noclip walks you through walls, Endless Hallway and Level 0 build corridors and mazes of wallpaper around the enemy, the Door to Nowhere swallows anyone who comes close, and Out of Bounds drops a whole squad out of the map. Wants Attunement and Celerity.

**Key attributes:** Attunement, Celerity

### Noclipper spells

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Static Flicker | 8 | 1.5s | attack | Throw a burst of bad signal. It hurts, and it leaves the target a little more Lost. |
| Noclip | 16 | 10s | mobility | Clip through reality to any spot in range. Walls, doors and line of sight do not matter. |
| Wrong Turn | 26 | 18s | control | Somewhere back there, the target took a wrong turn. It vanishes from reality for four seconds and comes back more Lost. |
| Buzzing Lights | 40 | 30s | control | Fluorescent lights hum into existence over an area for twelve seconds. Enemies under them glitch, losing whole moments, and grow more Lost. |
| Endless Hallway | 30 | 30s | control | Wallpapered walls rise on both sides of a line from you to the target, turning the ground in between into a hallway for half a minute. |
| Almond Water | 20 | 20s | heal | Share a sip of almond water: an ally heals, shakes off what ails them, steadies their mind and regains some mana. |
| Something Is Watching | 34 | 25s | control | Enemies in an area feel eyes on them from every corner for fifteen seconds. They aim badly and keep losing their way. |
| Door to Nowhere | 45 | 1.2 h | control | An ordinary office door appears, standing on its own, for thirty seconds. Any enemy that walks close to it goes through, and vanishes for five seconds. |
| Level 0 | 60 | 1.4 h | control | Fold the world into Level 0 around a target area: a maze of damp yellow wallpaper rises around every enemy inside and stands for half a minute. Everyone inside grows more Lost. |
| Out of Bounds | 60 | 1.1 h | control | Every enemy in an area falls through the floor and out of the map. Eight seconds later they drop back in, hard. |
| Conjure Almond Water * | 30 | 1 day | utility | Reach into a wall and pull out a few bottles of almond water. Nobody knows why this works. |

**Keystones:** *Wanderer* - You never stay anywhere long. Noclip is nearly always ready, and sometimes when you are hurt you slip away on your own.; *Keeper of Thresholds* - Everyone who meets you starts to lose their way: half your hits make them more Lost.; *The Backrooms* - You know the levels better than anyone alive. Every time someone noclips away, a flicker of static jumps to the next enemy.

### Level Keeper (subclass)

*Pull the levels through into the world: the Poolrooms, the Manila Room, Level Run For Your Life, and the rest.*

Level Keepers carry the Backrooms around with them, a room or two at a time.

**Playstyle:** Battlefield architecture. The Manila Room seals a single enemy in a box, Threshold rings an area in walls, the Poolrooms heal your side and drown theirs, and Level ! sends every enemy around you running. Bleed Through drops every enemy on the map into the Backrooms for ten seconds. Wants Attunement and Resilience.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Manila Room | 40 | 30s | control | Fold a single enemy into the Manila Room: a box of wallpaper closes around it for twenty seconds. |
| The Poolrooms | 50 | 1.2 h | control | Flood an area with the Poolrooms for twenty seconds: warm water and pale tiles. Allies inside regenerate; enemies wade slowly and lose their way. |
| Level ! | 45 | 40s | control | The lights turn red. Every enemy within seven cells runs for its life for six seconds; every ally runs faster. |
| Threshold | 45 | 40s | control | Draw a threshold around an area: a ring of wallpaper walls cuts it off from the world for fifteen seconds. |
| Sublimity | 40 | 1.1 h | buff | Open a door onto somewhere warm, quiet and impossible. Allies around you are warded, steadied and fearless for thirty seconds. |
| Bleed Through (ultimate) | 230 | 7.2 h | attack | Let the Backrooms bleed through into the world. Every enemy on the map noclips out of reality for ten seconds (bosses for less) and comes back Lost and disoriented. |

**Keystones:** *Master of Levels* - Every level you open is larger and stays open far longer.; *Safe Room* - Wherever you stand is a safe room: allies near you are steadily warded and healed while you fight.

### Entity Handler (subclass)

*Some things in the levels can be reasoned with. Hounds, Partygoers, Smilers and Skin-Stealers, on your side for once.*

Entity Handlers made friends in the Backrooms. Their friends are why nobody else did.

**Playstyle:** A summoner of Backrooms entities. Hounds hunt in a pack, Partygoers swarm and smile =), a Smiler sends enemies running, and the Skin-Stealer copies an enemy to fight on your side. Level Fun throws a party nobody survives. Wants Attunement and Focus.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Release the Hounds | 40 | 1.4 h | summon | Open a door and let two Hounds out for a minute. They run on all fours and they are very hungry. |
| Partygoers | 50 | 1.8 h | summon | Invite three Partygoers to the fight for a minute. They are so happy to be here =) |
| Smiler | 45 | 1.2 h | control | Leave a Smiler in the dark for thirty seconds. Every few seconds, enemies who see it run in terror and the static burns them. |
| Faceling Swap | 18 | 12s | mobility | Trade places with an enemy, the way the Facelings do. It finds itself where you were, a little more Lost. |
| Skin-stealer | 60 | 2.2 h | attack | Let a Skin-Stealer wear an enemy's face. A perfect copy of the target, gear and all, fights for you for forty-five seconds. |
| Level Fun (ultimate) | 230 | 7.2 h | attack | Everyone is invited to Level Fun =) Six Partygoers join the fight, and every enemy in a wide area loses its mind with joy and its way out. |

**Keystones:** *Alpha* - Lead the pack. Every summon brings an extra entity, and they last half again as long.; *Host of the Party* - Everyone comes to your parties eventually. Your guests come back sooner, and some of the people you kill come back as guests.

### Archivist (subclass)

*Everything is on tape. Record your enemies and replay their worst moments, rewind your friends, and find the exit sign.*

Archivists document the Backrooms on old camcorders. The tapes remember more than they should.

**Playstyle:** Recording is damage. Camcorder records an enemy for eight seconds; when the tape ends, every wound it took happens again. Pair it with your allies' fire. Rewind Tape undoes thirty seconds of an ally's wounds, Exit Sign teleports your whole squad, and Lost Footage records an entire battlefield. Wants Focus and Attunement.

| Spell | Mana | Cooldown | Role | Description |
|---|---|---|---|---|
| Camcorder | 30 | 20s | control | Start recording an enemy. In eight seconds the tape ends, and every wound it took in the meantime happens to it again. |
| Tracking Error | 30 | 18s | control | The picture rolls and tears over an area: enemies inside glitch for eight seconds, losing whole seconds at a time. |
| Static Burst | 36 | 22s | attack | Every screen in the world flashes to static at once: everything around you is burned by it, made more Lost and sometimes thrown out of sync. |
| Exit Sign | 40 | 1.4 h | utility | Follow the exit sign. You and every ally within six cells step through a door and come out at the target. |
| Rewind Tape | 50 | 2.2 h | heal | Rewind an ally by thirty seconds. The wounds of the last half-minute never happened. |
| Lost Footage (ultimate) | 220 | 7.2 h | attack | Record an entire battlefield on cursed tape. Every enemy within twelve cells is recorded for ten seconds; when the tape runs out, everything that happened to each of them happens again, half as bad again. |

**Keystones:** *Director's Cut* - Film everything. Camcorder returns twice as fast and records two more enemies.; *Rewinder* - Your own tape can be rewound. Once a day, when you would die, you are rewound instead.


Spells marked * are utility or quirky spells.

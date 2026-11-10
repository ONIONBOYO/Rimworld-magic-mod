"""
The Wyrmcaller. She hatches one wyrm and is bonded to it for life. The wyrm is a real colony animal: it can be zoned,
trained to haul and rescue, loaded as a pack animal, and it hunts for itself. It grows as she climbs the circles
(hatchling, then drake at the Second Circle, then great wyrm at the Fourth), it grows stronger with every level she
gains, and most of her spells are things the wyrm does: they start where the wyrm stands.

  Wyrmcaller (base)  Hatch, Wyrmfire (a cone of fire from the wyrm), Hunt, Heel, Stoop, Terrible Roar, Devour, Dragonflight,
                     and the colony: Molt (scales to tailor), Hoard Run, Skyward Scout, Dragon Rides.
  Soulbound          Spirit wyrm. Two bodies, one life: shared wounds, traded places, a draconic aspect that lets her
                     breathe fire too, and Skyfire, a strafing run on wyrmback.
  Hoardkeeper        Gold wyrm. Its power grows with the colony's wealth; it smells ore, sits on a hoard that grows by
                     itself, and breathes molten gold.
  Broodmother        Brood wyrm. Clutches of drakelings, egg bombs, a nest that lays eggs, a brood that hunts.
"""
from classes.common import *  # noqa: F401,F403
from classes import mythic_things as MT

JADE, EMBER = "#3fd6a0", "#ffe08a"
AZURE, FROSTWHITE = "#7ad7ff", "#eef9ff"
AURUM, BRONZE = "#ffcf4a", "#c8742a"
CRIMSON, CORAL = "#e0463c", "#ffb37a"
DRACONIC = School("Draconic", JADE, EMBER, fleck="flame", fleck2="scale", projectile="ArcanumAscendant/Vfx/WyrmFlame")
SOUL = School("Draconic", AZURE, FROSTWHITE, fleck="flame", fleck2="scale", projectile="ArcanumAscendant/Vfx/WyrmFlame")
HOARD = School("Draconic", AURUM, BRONZE, fleck="coin", fleck2="flame", projectile="ArcanumAscendant/Vfx/WyrmFlame")
BROOD = School("Draconic", CRIMSON, CORAL, fleck="flame", fleck2="scale", projectile="ArcanumAscendant/Vfx/WyrmFlame")
JADE_RGBA = rgb(JADE) + (1.0,)

WF = "AA_Wyrmfire"
BURN = "AA_WyrmfireBlaze"
RACE = "AA_Wyrm"
DRAKELING = "AA_Drakeling"

KEYS = {
    "AA_WyrmGoldDrop": "Solid gold",
}


def burn(n=1.0, **kw):
    return stacks(BURN, n, duration=360, cap=5, **kw)


def wyrm(mode, **kw):
    """Effect_WyrmCommand: Attack, Recall, Devour, Swap, Ride, Strafe or Empower (Classes_Wyrmcaller.cs)."""
    return O("Effect_WyrmCommand", mode=mode, **kw)


def brood(mode, **kw):
    return O("Effect_WyrmBrood", mode=mode, race=DRAKELING, **kw)


def periodic_bolts(interval, effects, rng=14.0, count=1, tex="ArcanumAscendant/Vfx/WyrmFlame", color=JADE):
    return O("ArcanumAscendant.HediffCompProperties_PeriodicEffects", interval=int(interval), targetEffects=effects, targetRange=float(rng),
             targetCount=int(count), targetTex=tex, color=rgb(color) + (1.0,))


FIRE_HIT = [flash(2.0), burst("flame", 8, 2.4, 0.7), burst("ember", 8, 2.0, 0.5), glow(1.2, 2.4, 20, useColor2=True)]
SOFT = [burst("scale", 8, 1.4, 0.5), burst("ember", 8, 1.2, 0.4), glow(1.4, 2.4, 30, useColor2=True)]

STAGES = [("Hatchling", "AA_WyrmHatchling", 1.5), ("Drake", "AA_WyrmDrake", 2.9), ("Elder", "AA_WyrmElder", 4.6)]
BREEDS = [("AA_Wyrm", "Jade", "jade wyrm", None), ("AA_Wyrm_Spirit", "Spirit", "spirit wyrm", "Soulbound"), ("AA_Wyrm_Gold", "Gold", "gold wyrm", "Hoardkeeper"),
          ("AA_Wyrm_Brood", "Brood", "brood wyrm", "Broodmother")]


def tool(label, cap, power, cd, part, chance=None, pen=None):
    return MT.tool(label, [cap], power, cd, part, chance, pen)


def race_defs():
    out = []
    for dn, label, size, health, hunger, dmgf in (("AA_WyrmHatchling", "hatchling", 0.2, 0.28, 0.35, 0.4), ("AA_WyrmDrake", "drake", 0.55, 0.62, 0.7, 0.72),
                                                 ("AA_WyrmElder", "great wyrm", 1.0, 1.0, 1.0, 1.0)):
        out.append(to_xml("LifeStageDef", O(None, defName=dn, label=label, visible=True, reproductive=False, bodySizeFactor=size, healthScaleFactor=health,
                                             hungerRateFactor=hunger, marketValueFactor=max(0.3, size), foodMaxFactor=max(0.4, size), meleeDamageFactor=dmgf), 1))
    race = O(None, body="QuadrupedAnimalWithPawsAndTail", baseBodySize=3.6, baseHealthScale=7.0, baseHungerRate=0.5, foodType="CarnivoreAnimal, OvivoreAnimal",
             lifeExpectancy=3000, intelligence="Animal", thinkTreeMain="Animal", thinkTreeConstant="AnimalConstant", predator=True, maxPreyBodySize=2.5,
             manhunterOnDamageChance=0.0, manhunterOnTameFailChance=0.0, hasGenders=False, petness=0.5, packAnimal=True, herdAnimal=False,
             trainability="Advanced", nameOnTameChance=1.0, canBePredatorPrey=False, bleedRateFactor=0.5, hasMeat=False,
             lifeStageAges=[O(None, def_="AA_WyrmHatchling", minAge=0.0), O(None, def_="AA_WyrmDrake", minAge=100.0), O(None, def_="AA_WyrmElder", minAge=200.0)],
             soundMeleeHitPawn="Pawn_Melee_BigBash_HitPawn", soundMeleeHitBuilding="Pawn_Melee_BigBash_HitBuilding", soundMeleeMiss="Pawn_Melee_BigBash_Miss")
    td = to_xml("ThingDef", O(None, defName=RACE, label="wyrm",
        description="A dragon, hatched by a Wyrmcaller and bonded to her for life. It grows as she does: a hatchling at first, a drake when she reaches "
                    "the Second Circle, a great wyrm at the Fourth, and stronger with every level she gains. It can be zoned, trained to haul and "
                    "rescue, loaded as a pack animal, and it hunts for itself. It does not breed, and it will not outlive its welcome: it lives for "
                    "thousands of years.",
        statBases={"MoveSpeed": 5.8, "MarketValue": 3000, "ComfyTemperatureMin": -70, "ComfyTemperatureMax": 140, "ArmorRating_Sharp": 0.45,
                   "ArmorRating_Blunt": 0.3, "ArmorRating_Heat": 1.3, "Wildness": 0.0, "Flammability": 0.0, "LeatherAmount": 0, "MeatAmount": 0,
                   "FilthRate": 2, "CaravanRidingSpeedFactor": 1.6, "ToxicResistance": 0.5},
        tools=[tool("fangs", "Bite", 34, 2.2, "Teeth", 0.7, 0.45), tool("left claw", "Scratch", 24, 1.7, "FrontLeftPaw", None, 0.3),
               tool("right claw", "Scratch", 24, 1.7, "FrontRightPaw", None, 0.3), tool("head", "Blunt", 18, 2.0, "HeadAttackTool", 0.2)],
        race=race, tradeability="None"), 1)
    out.append(td.replace("<ThingDef>", '<ThingDef ParentName="AnimalThingBase">', 1).replace("<def_>", "<def>").replace("</def_>", "</def>"))
    for kind, breed, label, _ in BREEDS:
        stages = [O(None, bodyGraphicData=O(None, texPath="ArcanumAscendant/Pawns/Wyrm_%s_%s" % (breed, st), graphicClass="Graphic_Multi", drawSize=float(sz),
                                            shadowData=O(None, volume=(0.25 * sz, 0.3, 0.2 * sz), offset=(0.0, 0.0, -0.08 * sz))),
                    dessicatedBodyGraphicData=O(None, texPath="Things/Pawn/Animal/Dessicated/CritterDessicatedMedium", graphicClass="Graphic_Multi",
                                                drawSize=float(sz) * 0.8)) for st, _, sz in STAGES]
        k = to_xml("PawnKindDef", O(None, defName=kind, label=label, race=RACE, combatPower=420.0, ecoSystemWeight=0.0, canArriveManhunter=False,
                                    lifeStages=stages), 1)
        out.append(k.replace("<PawnKindDef>", '<PawnKindDef ParentName="AnimalKindBase">', 1))
    return out


def item_defs():
    out = []
    scale = to_xml("ThingDef", O(None, defName="AA_WyrmScale", label="wyrm scale",
        description="Scales shed by a bonded wyrm. Worked like leather, they make armour that turns blades and shrugs off fire, and clothing that is warm "
                    "in the cold and cool in the heat.",
        graphicData=O(None, texPath="ArcanumAscendant/Items/WyrmScale", graphicClass="Graphic_Single"), stackLimit=75, healthAffectsPrice=False,
        statBases={"MaxHitPoints": 120, "MarketValue": 9.5, "Mass": 0.035, "Flammability": 0.0, "DeteriorationRate": 0.5, "StuffPower_Armor_Sharp": 1.9,
                   "StuffPower_Armor_Blunt": 0.6, "StuffPower_Armor_Heat": 3.0, "StuffPower_Insulation_Cold": 18, "StuffPower_Insulation_Heat": 26},
        thingCategories=["Leathers"],
        stuffProps=O(None, categories=["Leathery"], color=rgb(JADE) + (1.0,), commonality=0.0, stuffAdjective="wyrmscale",
                     statFactors={"MaxHitPoints": 1.6, "Beauty": 2.2, "Flammability": 0.0})), 1)
    out.append(scale.replace("<ThingDef>", '<ThingDef ParentName="AA_ArcaneItemBase">', 1))
    egg = to_xml("ThingDef", O(None, defName="AA_WyrmEgg", label="wyrm egg (unfert.)",
        description="An unfertilized egg from a brood wyrm's nest, the size of a helmet. It cooks like any egg and feeds like six.",
        graphicData=O(None, texPath="ArcanumAscendant/Items/WyrmEgg", graphicClass="Graphic_Single"), stackLimit=20, socialPropernessMatters=True,
        statBases={"MaxHitPoints": 60, "MarketValue": 28, "Mass": 0.6, "Flammability": 0.5, "DeteriorationRate": 2, "Nutrition": 0.6, "FoodPoisonChanceFixedHuman": 0.02},
        thingCategories=["EggsUnfertilized"],
        ingestible=O(None, foodType="AnimalProduct", ingestEffect="EatMeat", ingestSound="RawMeat_Eat", tasteThought="AteRawFood", preferability="RawBad"),
        comps=[O("CompProperties_Rottable", daysToRotStart=20, rotDestroys=True)]), 1)
    out.append(egg.replace("<ThingDef>", '<ThingDef ParentName="AA_ArcaneItemBase">', 1))
    return out


def statuses():
    s = []
    s += race_defs()
    s += item_defs()
    # ------------------------------------------------------------------ the bond
    s.append(status("AA_WyrmBond", "bonded wyrm", "Bonded to a Wyrmcaller. It grows when she ascends a circle, and every level she gains makes it hit "
        "harder, shrug off more and move faster.", JADE, bad=False, maxSev=30.0, initSev=1.0,
        comps=[O("ArcanumAscendant.HediffCompProperties_WyrmBond", defaultKind="AA_Wyrm", griefThought="AA_WyrmDied", color=JADE_RGBA,
                 breeds=[O(None, subclass="AA_Class_" + sub, kind=kind, hediff="AA_Breed" + breed) for kind, breed, _, sub in BREEDS if sub])],
        stages=[stage(offsets={"MeleeDamageFactor": 0.022, "ArmorRating_Sharp": 0.01, "ArmorRating_Blunt": 0.01, "IncomingDamageFactor": -0.008,
                               "MoveSpeed": 0.03}, mult=True)],
        extra={"everCurableByItem": False, "keepOnBodyPartRestoration": True}))
    s.append(status("AA_BreedSpirit", "spirit wyrm", "Half here and half elsewhere, like the one it is bound to: blows land softer and wounds close faster.",
        AZURE, bad=False, stages=[stage(factors={"IncomingDamageFactor": 0.85, "InjuryHealingFactor": 2.0})], extra={"keepOnBodyPartRestoration": True}))
    s.append(status("AA_BreedGold", "gold wyrm", "Scales like coins and a hide like a vault door.", AURUM, bad=False,
        stages=[stage(offsets={"ArmorRating_Sharp": 0.3, "ArmorRating_Blunt": 0.25}, factors={"MoveSpeed": 0.94})], extra={"keepOnBodyPartRestoration": True}))
    s.append(status("AA_BreedBrood", "brood wyrm", "A mother of drakes: fast, fierce and always hungry.", CRIMSON, bad=False,
        stages=[stage(factors={"MoveSpeed": 1.12, "MeleeCooldownFactor": 0.85})], extra={"keepOnBodyPartRestoration": True}))
    s.append(thought_def("AA_WyrmDied", "my wyrm died", "I felt it go. There is a place in my head where it used to be.", -18, days=8.0))
    s.append(thought_def("AA_RodeADragon", "rode a dragon", "I rode a dragon. I am never going to stop talking about it.", 8, days=3.0))
    s.append(thought_def("AA_UnderTheWing", "under the wing", "There is a dragon asleep outside and it is on our side. I have never slept better.", 5, days=2.0))

    # ------------------------------------------------------------------ fire and fear
    s.append(status(BURN, "wyrmfire", "Wyrmfire clings like oil and keeps burning. Stacks.", JADE, bad=True, ticks=360, maxSev=5,
        comps=[dot(WF, 3.0, JADE, "AA_Fleck_WyrmFlame")], stages=[stage(offsets={"MentalBreakThreshold": 0.05})], fx=dict(emitter="Wyrmfire", emitInterval=8)))
    s.append(status("AA_WyrmFrenzy", "set upon its prey", "Loosed on a target: faster on its feet and much faster with tooth and claw.", JADE, bad=False,
        ticks=600, stages=[stage(factors={"MoveSpeed": 1.3, "MeleeCooldownFactor": 0.6})], fx=dict(emitter="Scales", emitInterval=20)))
    s.append(status("AA_Gorged", "gorged", "Full of fresh meat: wounds close fast and it hits harder.", JADE, bad=False, ticks=1800,
        comps=[dot(None, 0.0, JADE, "AA_Fleck_Scale", heal_ps=4.0)], stages=[stage(factors={"MeleeDamageFactor": 1.25})]))
    s.append(status("AA_WyrmDread", "terrified", "A dragon roared at it. Every instinct it has says run.", JADE, bad=True, ticks=240,
        comps=[compel("Flee")], stages=[stage(factors={"ShootingAccuracyPawn": 0.7, "MeleeHitChance": 0.8})]))
    s.append(status("AA_Emboldened", "emboldened", "The dragon roared and it was on their side.", EMBER, bad=False, ticks=900,
        stages=[stage(offsets={"MentalBreakThreshold": -0.1, "PainShockThreshold": 0.15}, factors={"MeleeHitChance": 1.1, "ShootingAccuracyPawn": 1.1})]))
    s.append(status("AA_Dragonscale", "dragonscale", "A shed scale, still warm, pressed over the heart: blades skid off and fire does nothing.", JADE,
        bad=False, ticks=3600, stages=[stage(offsets={"ArmorRating_Sharp": 0.4, "ArmorRating_Blunt": 0.3, "ArmorRating_Heat": 1.0},
                                             factors={"Flammability": 0.0}, dmgf={"Flame": 0.0, "Burn": 0.1, WF: 0.2})],
        fx=dict(emitter="Scales", emitInterval=24)))

    # ------------------------------------------------------------------ Soulbound
    s.append(status("AA_SharedHeart", "shared heart", "Two bodies, one life. Each takes half of every wound, and a quarter of it is felt by the other.",
        AZURE, bad=False, ticks=3600, comps=[damage_link(0.5, None, AZURE)], stages=[stage(factors={"IncomingDamageFactor": 0.5})],
        fx=dict(emitter="Scales", emitInterval=30)))
    s.append(status("AA_DraconicAspect", "draconic aspect", "Scales over skin, claws for nails, wings of pale fire. While it lasts she breathes wyrmfire "
        "along with her wyrm, hits like a hammer and does not burn.", AZURE, bad=False, ticks=2700,
        stages=[stage(offsets={"ArmorRating_Sharp": 0.5, "ArmorRating_Blunt": 0.4, "ArmorRating_Heat": 1.2, "PainShockThreshold": 0.3},
                      factors={"MeleeDamageFactor": 1.6, "MoveSpeed": 1.15, "Flammability": 0.0}, dmgf={"Flame": 0.0, "Burn": 0.1})],
        fx=dict(emitter="Scales", emitInterval=12, overlayTex="ArcanumAscendant/Vfx/DragonWings", overlaySize=2.8, overlayPulse=True)))
    s.append(status("AA_WyrmFury", "fury of two", "Its mage's anger is its own. Every second it spits a bolt of wyrmfire at the nearest enemy.", AZURE,
        bad=False, ticks=900, comps=[periodic_bolts(60, [dmg(16, WF, armorPen=0.3), burn(1)], 16.0, 1, color=AZURE)],
        fx=dict(emitter="Wyrmfire", emitInterval=10)))

    # ------------------------------------------------------------------ Hoardkeeper
    s.append(status("AA_GoldClad", "gold-clad", "Coated in cooling gold: it burns, it weighs a great deal, and anything that dies in it leaves the gold "
        "behind.", AURUM, bad=True, ticks=600,
        comps=[dot(WF, 2.0, AURUM, "AA_Fleck_Coin"), death_trigger(area=[conjure("Gold", 4, 10)], radius=0.5, color=AURUM, label="AA_WyrmGoldDrop")],
        stages=[stage(factors={"MoveSpeed": 0.5, "MeleeCooldownFactor": 1.3})], fx=dict(emitter="Goldclad", emitInterval=10)))
    s.append(status("AA_GoldenAge", "golden age", "Everything is going well and everyone knows it: steadier work, sharper deals.", AURUM, bad=False,
        ticks=DAY, stages=[stage(factors={"WorkSpeedGlobal": 1.1}, offsets={"TradePriceImprovement": 0.08, "MentalBreakThreshold": -0.04})]))
    s.append(thought_def("AA_GoldenAgeMood", "golden age", "The colony is rich, the dragon is fat, and for once nothing is on fire that shouldn't be.", 6, days=1.0))

    # ------------------------------------------------------------------ Broodmother
    s.append(status("AA_Matriarch", "matriarch's hunger", "It has eaten its own young and is the stronger for it. Stacks.", CRIMSON, bad=False, ticks=1800,
        maxSev=8, stages=[stage(offsets={"MeleeDamageFactor": 0.12, "MoveSpeed": 0.1}, mult=True)], fx=dict(emitter="Wyrmfire", emitInterval=16)))

    # ------------------------------------------------------------------ things
    s += MT.creature(DRAKELING, "drakeling", "A dragon the size of a dog, all teeth and bad temper, hatched from a brood wyrm's clutch. It does not "
        "last: it was hatched too fast.", tex="Drakeling", size=1.35, body="QuadrupedAnimalWithPawsAndTail", body_size=0.6, health=0.9, speed=7.2,
        tools=[tool("fangs", "Bite", 11, 1.6, "Teeth", 0.7, 0.2), tool("left claw", "Scratch", 8, 1.3, "FrontLeftPaw"),
               tool("right claw", "Scratch", 8, 1.3, "FrontRightPaw")], color=CRIMSON, combat_power=60, armor=0.2, heat_armor=1.0)
    hoard = MT.construct("Construct_WyrmHoard", "dragon's hoard", "A heap of coin, plate and bright things with a dragon-shaped hollow in the middle. "
        "A hoard grows by itself: once a day it yields silver in proportion to the colony's wealth (15 to 180), for as long as the wyrm that owns it "
        "lives. It is also very beautiful, in a vulgar way.", tex="WyrmHoard", color=AURUM, hp=400, lifetime=15 * DAY, size=2.6, glow=1.6, light=5,
        comps=[O("ArcanumAscendant.CompProperties_WyrmHoard")])
    hoard = hoard.replace("<Beauty>0</Beauty>", "<Beauty>60</Beauty>")
    assert "<Beauty>60</Beauty>" in hoard
    s.append(hoard)
    s.append(MT.construct("Construct_WyrmNest", "wyrm nest", "A nest of scorched earth, bones and shed scales, warm to the touch. Twice a day a brood "
        "wyrm leaves two eggs in it: they cook like any egg and each feeds like six.", tex="WyrmNest", color=CRIMSON, hp=300, lifetime=5 * DAY, size=2.4,
        glow=1.2, comps=[O("CompProperties_Spawner", thingToSpawn="AA_WyrmEgg", spawnCount=2, spawnIntervalRange="28000~32000", spawnMaxAdjacent=30,
                           writeTimeLeftToSpawn=True, showMessageIfOwned=True, saveKeysPrefix="aaNest"),
                         O("CompProperties_HeatPusher", compClass="CompHeatPusher", heatPerSecond=6.0, heatPushMaxTemperature=24.0)]))
    s.append(MT.construct("Construct_ClutchEgg", "wyrm egg", "A brood wyrm's egg, rocking gently. Something inside wants out.", tex="ClutchEgg",
        color=CRIMSON, hp=60, lifetime=300, size=1.1, glow=1.0))
    return s


def build():
    wc = ClassSpec("Wyrmcaller", "wyrmcaller", color=JADE, color2=EMBER, icon=("dragon", "flame"), order=15, school=DRACONIC,
        tagline="Hatch a dragon. Raise it. It grows when you do, and most of your spells are things it does.",
        desc="A Wyrmcaller is one half of a pair. The other half starts the size of a cat and ends the size of a barn, and every bit of it is "
             "hers: its fire, its hunger, its temper and its absolute certainty that she is the most important thing in the world.",
        playstyle="You begin with an egg. The wyrm is a colony animal you can zone, train to haul and rescue and load for caravans, and it is a "
                  "hatchling until you reach the Second Circle (drake) and the Fourth (great wyrm); every level you gain makes it stronger. Most "
                  "spells start from the wyrm, so where it stands matters: Wyrmfire is a cone from its jaws, Stoop drops it on a target, Terrible "
                  "Roar scatters whatever is near it, Hunt sets it on one enemy and Heel calls it back. At home it sheds scales to tailor, hauls, "
                  "scouts and gives rides. If it dies you may hatch another after three days. Wants Attunement and Resilience.",
        lore="'Is it safe?' asked the trader. 'No,' she said, scratching it under the chin. 'That's rather the point.'",
        primary="ATT", secondary="RES", tags=["Draconic"], starting=["HatchWyrm"], innate={"AA_SummonPower": 0.1})
    wc.add_spells(
        Spell("HatchWyrm", "hatch wyrm", "Hatch your wyrm: a bonded dragon for life. It arrives as a hatchling, fully trained, and grows into a drake "
              "at the Second Circle and a great wyrm at the Fourth. Only one at a time; if it dies you can hatch another when this spell returns.",
              icon=("egg", "dragon"), mana=20, cd=3000, range=6, warmup=5.0, tags=["Draconic", "Summon"], role="Summon", target="cell", quirky=True,
              requiresNoCompanion=True, sound_impact="AA_WyrmRoar", impact_vfx=[ring(0.2, 1.0, 30)],
              effects=[O("Effect_WyrmHatch", kind="AA_Wyrm", bond="AA_WyrmBond")], flavor="It looked at her first. That was that."),
        Spell("Wyrmfire", "wyrmfire", "Your wyrm breathes a cone of fire toward the target: 22 wyrmfire and a clinging burn that stacks, eight cells "
              "long from wherever the wyrm is standing. It sets the ground alight.", icon=("dragon", "flame"), mana=20, cd=7, range=30, warmup=0.5,
              tags=["Draconic"], target="enemy_area", delivery="Cone", coneAngle=50.0, coneRange=8.0, fromCompanion=True, sound_cast="AA_WyrmBreath",
              sound_impact="AA_WyrmBreath", effects=[dmg(22, WF, armorPen=0.3), burn(1), ignite(0.12, 0.4, False)]),
        Spell("Hunt", "hunt", "Point. Your wyrm drops whatever it is doing and goes for that enemy, a third faster and striking far more often for ten "
              "seconds.", icon=("dragon", "claw"), mana=12, cd=6, range=40, warmup=0.2, tags=["Draconic"], role="Control", target="enemy", autocast=False,
              impact_vfx=[], effects=[wyrm("Attack", wyrmHediff="AA_WyrmFrenzy")], flavor="That one."),
        Spell("Heel", "heel", "Call your wyrm back. It takes wing and lands at your side.", icon=("dragon", "hand"), mana=8, cd=10, warmup=0.2,
              tags=["Draconic", "Mobility"], role="Utility", target="self", autocast=False, impact_vfx=[], effects=[wyrm("Recall")]),
        Spell("Stoop", "stoop", "Your wyrm takes wing and comes down on the target point like a falling wall: 45 damage within three cells, a two-second "
              "stun, and everything there set burning.", icon=("dragon", "meteor"), mana=30, cd=16, range=30, warmup=0.5, tags=["Draconic"], target="cell",
              hostile=True, tier=2, shake=0.8, impact_vfx=[], sound_cast="AA_WyrmRoar",
              effects=[launch("CompanionToTarget", 0, 0, landing=[dmg(45, WF, armorPen=0.4), stun(120), burn(1)], landingRadius=3.0)]),
        Spell("TerribleRoar", "terrible roar", "Your wyrm roars. Enemies within seven cells of it run in terror for four seconds, shooting and striking "
              "badly; allies there are emboldened for fifteen.", icon=("dragon", "burst"), mana=30, cd=30, warmup=0.4, tags=["Draconic"], role="Control",
              target="self", delivery="Nova", radius=7.0, affects="All", fromCompanion=True, ai_min=3, shake=1.2, sound_cast="AA_WyrmRoar",
              impact_vfx=[ring(0.1, 1.3, 30), burst("scale", 16, 4.0, 0.5, byRadius=True), smoke(4, 1.2)],
              effects=[hediff("AA_WyrmDread", 1.0, affects="Enemies"), hediff("AA_Emboldened", 1.0, affects="Allies")]),
        Spell("Dragonscale", "dragonscale", "Press a freshly shed scale over an ally's heart. For a minute blades skid off (+40% armour), fire does "
              "nothing to them and they cannot be set alight.", icon=("scale", "shield"), mana=22, cd=30, range=20, warmup=0.4, tags=["Draconic", "Ward"],
              role="Buff", target="ally", impact_vfx=SOFT, effects=[hediff("AA_Dragonscale", 1.0)]),
        Spell("Devour", "devour", "Feed your wyrm a corpse. It flies to it and eats it whole (gear is left behind): 60 healing for every point of the "
              "body's size, a full belly, and half a minute of fast healing and harder blows. The colony's own dead are not on the menu.",
              icon=("dragon", "bone"), mana=14, cd=20, range=30, warmup=0.4, tags=["Draconic"], role="Utility", target="corpse", affects="Corpses",
              quirky=True, impact_vfx=[], effects=[wyrm("Devour", heal=60.0, wyrmHediff="AA_Gorged")]),
        Spell("Dragonflight", "dragonflight", "Climb on. You and your wyrm fly to the target point together and land hard: 30 damage and a stagger to "
              "enemies within two and a half cells.", icon=("dragon", "wing"), mana=28, cd=25, range=34, warmup=0.4, tags=["Draconic", "Mobility"],
              role="Mobility", target="cell", los=False, shake=0.6, impact_vfx=[], sound_cast="AA_WyrmRoar",
              effects=[wyrm("Ride", landingEffects=[dmg(30, WF), stun(45)], landingRadius=2.5)]),
        Spell("Molt", "molt", "Help your wyrm shed. It leaves 12 to 20 wyrm scales where it stands: tailor them like leather into armour that turns "
              "blades, ignores fire and keeps its wearer comfortable in any weather.", icon=("scale", "hand"), mana=30, cd=4000, warmup=3.0,
              tags=["Draconic", "Utility"], role="Utility", target="self", delivery="Nova", radius=0.4, fromCompanion=True, quirky=True,
              impact_vfx=SOFT, effects=[conjure("AA_WyrmScale", 12, 20)], flavor="It itches. Everyone knows when it itches."),
        Spell("HoardRun", "hoard run", "Send your wyrm to fetch. Up to forty things lying within seven cells of the target point are snatched up and "
              "carried straight to wherever they are stored.", icon=("dragon", "coin"), mana=24, cd=500, range=40, warmup=1.0,
              tags=["Draconic", "Utility"], role="Utility", target="cell", radius=7.0, quirky=True, impact_vfx=[burst("scale", 10, 2.0, 0.5, byRadius=True)],
              effects=[haul(40), wyrm("Empower")]),
        Spell("SkywardScout", "skyward scout", "Your wyrm climbs until it is a dot and looks down. The map is revealed within forty cells of it.",
              icon=("dragon", "eye"), mana=30, cd=1000, warmup=2.0, tags=["Draconic", "Utility"], role="Utility", target="self", delivery="Nova",
              radius=0.4, fromCompanion=True, quirky=True, impact_vfx=[pillar(1.0, 50), burst("scale", 10, 3.0, 0.5)], effects=[reveal(40)]),
        Spell("DragonRides", "dragon rides", "Everyone gets a turn. Colonists within six cells of your wyrm gain 40% recreation and +8 mood for three "
              "days; with Biotech, children also gain 5 growth points and half a bar of learning.", icon=("dragon", "heart"), mana=35, cd=2000,
              warmup=3.0, tags=["Draconic", "Utility"], role="Utility", target="self", delivery="Nova", radius=6.0, affects="Allies", fromCompanion=True,
              quirky=True, impact_vfx=[ring(0.2, 1.0, 30)] + SOFT, effects=[need("Joy", 0.4), memory("AA_RodeADragon"), child_growth(5, learning=0.5)],
              flavor="Hold on to the spines. Not that one."),
    )
    wc.add_nodes(
        Node("Fledgling", "fledgling", (1, 0), ranks=3, mods=[tagmod("Draconic", damage=0.05)]),
        Node("Kinship", "kinship", (3, 0), ranks=3, stats={"AA_ManaMax": 10, "AA_ManaRegen": 0.06}),
        Node("Handler", "handler", (5, 0), ranks=3, stats={"AA_CastSpeed": 0.03, "AA_CooldownReduction": 0.01}),
        Node("Wyrmfire", None, (1, 1), kind="Spell", spell="Wyrmfire", pre=["Fledgling"]),
        Node("Hunt", None, (3, 1), kind="Spell", spell="Hunt", pre=["Kinship"]),
        Node("Heel", None, (5, 1), kind="Spell", spell="Heel", pre=["Handler"]),
        Node("DeepBreath", "deep breath", (0, 2), kind="Augment", ranks=2, pre=["Wyrmfire"], mods=[spellmod(["Wyrmfire"], radius=0.15, damage=0.15)],
             desc="Wyrmfire reaches farther and burns hotter."),
        Node("Devour", None, (2, 2), kind="Spell", spell="Devour", pre=["Wyrmfire", "Hunt"]),
        Node("Dragonscale", None, (4, 2), kind="Spell", spell="Dragonscale", pre=["Hunt", "Heel"]),
        Node("HoardRun", None, (6, 2), kind="Spell", spell="HoardRun", pre=["Heel"]),
        Node("Firebrand", "firebrand", (1, 3), kind="Notable", ranks=2, pre=["DeepBreath", "Devour"],
             passives=[P("P_OnHitHediff", hediff=BURN, chancePerRank=0.3, severity=1.0, stack=True, maxSeverity=5.0, durationTicks=360, tag="Draconic")],
             desc="Everything draconic burns: your Draconic spells often lay an extra stack of wyrmfire."),
        Node("TerribleRoar", None, (3, 3), kind="Spell", spell="TerribleRoar", pre=["Devour", "Dragonscale"]),
        Node("Molt", None, (5, 3), kind="Spell", spell="Molt", pre=["Dragonscale", "HoardRun"]),
        Node("Ironscale", "ironscale", (0, 4), ranks=3, pre=["Firebrand"], stats={"AA_WardMax": 8, "ArmorRating_Heat": 0.1}),
        Node("Stoop", None, (2, 4), kind="Spell", spell="Stoop", pre=["Firebrand", "TerribleRoar"]),
        Node("Wyrmkin", "wyrmkin", (4, 4), kind="Notable", ranks=2, pre=["TerribleRoar", "Molt"],
             passives=[P("P_HealOnCast", amount=3.0, tag="Draconic")], stats={"TameAnimalChance": 0.1, "TrainAnimalChance": 0.1},
             desc="You are a little draconic yourself. Each Draconic spell restores a little of you, and animals of every kind listen to you."),
        Node("DragonRides", None, (6, 4), kind="Spell", spell="DragonRides", pre=["Molt"]),
        Node("Thermals", "thermals", (1, 5), kind="Augment", ranks=2, pre=["Ironscale", "Stoop"], mods=[spellmod(["Stoop", "Dragonflight"], cooldown=-0.2, damage=0.15)],
             desc="Stoop and Dragonflight come back sooner and land harder."),
        Node("Dragonflight", None, (3, 5), kind="Spell", spell="Dragonflight", pre=["Stoop", "Wyrmkin"]),
        Node("SkywardScout", None, (5, 5), kind="Spell", spell="SkywardScout", pre=["Wyrmkin", "DragonRides"]),
        Node("Sinew", "sinew", (0, 6), ranks=3, pre=["Thermals"], stats={"AA_SummonPower": 0.05}),
        Node("OneMind", "one mind", (2, 6), kind="Notable", ranks=2, pre=["Thermals", "Dragonflight"], mods=[tagmod("Draconic", damage=0.08, critChance=0.03)]),
        Node("OldBlood", "old blood", (4, 6), ranks=3, pre=["Dragonflight", "SkywardScout"], pct={"AA_SpellPower": 0.03}),
        Node("LongMemory", "long memory", (6, 6), ranks=3, pre=["SkywardScout"], stats={"AA_DurationFactor": 0.05}),
        Node("Dragonlord", "dragonlord", (2, 7), kind="Keystone", excl="WyrmcallerKeystone", pre=["OneMind", "Sinew"],
             mods=[spellmod(["Wyrmfire", "Stoop"], cooldown=-0.35), tagmod("Draconic", damage=0.15)],
             desc="It fights as you think. Wyrmfire and Stoop come back far sooner and everything draconic hits harder."),
        Node("Wyrmfriend", "wyrmfriend", (4, 7), kind="Keystone", excl="WyrmcallerKeystone", pre=["OldBlood", "LongMemory"],
             mods=[spellmod(["Molt", "DragonRides", "HoardRun", "SkywardScout", "HatchWyrm"], cooldown=-0.5)], stats={"AA_SummonPower": 0.15},
             desc="More pet than weapon. Molt, Dragon Rides, Hoard Run, Skyward Scout and Hatch Wyrm come back twice as fast."),
        Node("HeartOfTheWyrm", "heart of the wyrm", (3, 8), kind="Keystone", pre=["Dragonlord", "Wyrmfriend"], pct={"AA_SpellPower": 0.15},
             passives=[P("P_TriggerSpell", trigger="OnSpellHit", spell=S("Hunt"), chancePerRank=0.25, target="Victim", cooldownTicks=300, tag="Draconic")],
             desc="It knows who you are angry at. A quarter of your Draconic hits also set the wyrm on that enemy."),
    )

    # ------------------------------------------------------------------ SOULBOUND
    so = ClassSpec("Soulbound", "soulbound", color=AZURE, color2=FROSTWHITE, icon=("dragon", "heart"), parent=wc, order=0, school=SOUL,
        tagline="Two bodies, one life. Share wounds, trade places, grow scales of your own, and burn the field from wyrmback.",
        desc="The Soulbound stopped being able to tell where she ends and the wyrm begins. Neither of them minds.",
        playstyle="Your wyrm becomes a pale spirit wyrm that shrugs off blows and heals fast. Shared Heart halves the wounds either of you takes, "
                  "Trade Places swaps you in an instant, Blood Offering spends your blood to mend it, and Rekindle brings it back from the dead. "
                  "Draconic Aspect gives you scales, claws and a breath of your own (Wyrmfire then comes from both of you), Fury of Two makes the "
                  "wyrm spit fire by itself, and Skyfire is a strafing run with you on its back. Wants Attunement and Resilience.",
        lore="When she stubs her toe, something the size of a barn looks up and growls.",
        primary="ATT", secondary="RES", tags=["Draconic"], height=8)
    so.add_spells(
        Spell("SharedHeart", "shared heart", "For a minute you and your wyrm share one life: each of you takes half of every wound, and a quarter of it "
              "is felt by the other.", icon=("heart", "dragon"), mana=30, cd=60, warmup=0.4, tags=["Draconic", "Ward"], role="Buff", target="self",
              impact_vfx=SOFT, effects=[hediff("AA_SharedHeart", 1.0, affects="Self"), wyrm("Empower", wyrmHediff="AA_SharedHeart")]),
        Spell("TradePlaces", "trade places", "You and your wyrm swap places in the blink of an eye, wherever each of you is on the map.",
              icon=("portal", "dragon"), mana=16, cd=14, warmup=0.1, tags=["Draconic", "Mobility"], role="Mobility", target="self", impact_vfx=[],
              effects=[wyrm("Swap")], flavor="It is hard to ambush someone who is suddenly a dragon."),
        Spell("BloodOffering", "blood offering", "Open a vein for your wyrm. It heals 120 at once; you pay for it with your own blood.",
              icon=("drop", "dragon"), mana=14, cd=20, warmup=0.4, tags=["Draconic", "Healing"], role="Heal", target="self", delivery="Nova", radius=0.4,
              affects="All", fromCompanion=True, health_cost=14, autocast=False, impact_vfx=SOFT + [burst("blood", 8, 1.5, 0.5)], effects=[heal(120, affects="All")]),
        Spell("DraconicAspect", "draconic aspect", "Take on the dragon for forty-five seconds: scales (+50% armour), claws (+60% melee damage), no fear of "
              "fire, and a breath of your own. While it lasts, Wyrmfire comes from you as well as from your wyrm.", icon=("dragon", "mask"), mana=40,
              cd=75, warmup=0.6, tags=["Draconic"], role="Buff", target="self", tier=2, sound_cast="AA_WyrmRoar",
              impact_vfx=[pillar(1.0, 40), burst("scale", 20, 2.5, 0.5), flash(3.0)], effects=[hediff("AA_DraconicAspect", 1.0, affects="Self")]),
        Spell("FuryOfTwo", "fury of two", "Your anger becomes its anger. For fifteen seconds your wyrm spits a bolt of wyrmfire at the nearest enemy "
              "every second (16 damage and a burn), on top of whatever else it is doing.", icon=("flame", "dragon"), mana=34, cd=40, warmup=0.3,
              tags=["Draconic"], target="self", autocast=False, tier=2, impact_vfx=[], sound_cast="AA_WyrmRoar",
              effects=[wyrm("Empower", wyrmHediff="AA_WyrmFury")]),
        Spell("RekindleBond", "rekindle", "Refuse to let it go. Target your wyrm's body: it lives again, and the bond with it.", icon=("phoenix", "dragon"),
              mana=120, cd=1000, range=12, warmup=6.0, tags=["Draconic", "Healing"], role="Utility", target="corpse", affects="Corpses", quirky=True,
              impact_vfx=[pillar(1.6, 60), flash(4.0), burst("scale", 24, 2.5, 0.6)], effects=[resurrect()],
              flavor="No. Not you. Get up."),
        Spell("UnderTheWing", "under the wing", "Let the whole colony feel what you feel: something enormous is watching over them. Every colonist on "
              "the map gains +5 mood for two days.", icon=("wing", "heart"), mana=50, cd=3000, warmup=3.0, tags=["Draconic", "Utility"], role="Utility",
              target="self", delivery="Global", affects="Allies", quirky=True, cast_vfx=[pillar(1.4, 60)], impact_vfx=[burst("scale", 6, 1.2, 0.4)],
              effects=[memory("AA_UnderTheWing")]),
        Spell("Skyfire", "skyfire", "Mount up and burn the field. You and your wyrm fly to the target point, and everything within two and a half "
              "cells of the line you fly takes 70 wyrmfire, burns hard and has the ground set alight under it. You land with a blast of 60 more.",
              icon=("dragon", "fireball"), mana=220, cd=300, range=40, warmup=2.0, tags=["Draconic"], target="cell", hostile=True, los=False,
              ultimate=True, tier=5, xp=2.0, shake=1.6, sound_cast="AA_WyrmRoar", cast_vfx=[pillar(2.6, 60), screen(AZURE, 0.3, 40)],
              impact_vfx=[flash(3.0), burst("flame", 14, 3.0, 0.8, byRadius=True)],
              effects=[wyrm("Strafe", pathEffects=[dmg(70, WF, armorPen=0.5, affects="Enemies"), burn(3, affects="Enemies"), ignite(0.35, 0.6, False)],
                            pathRadius=2.5, landingEffects=[dmg(60, WF, armorPen=0.5), stun(150)], landingRadius=4.0)]),
    )
    so.add_nodes(
        Node("SharedHeart", None, (2, 0), kind="Spell", spell="SharedHeart"),
        Node("Heartbeat", "one heartbeat", (4, 0), ranks=3, stats={"AA_WardMax": 8, "AA_HealingPower": 0.04}),
        Node("Entwined", "entwined", (1, 1), kind="Augment", ranks=2, pre=["SharedHeart"], mods=[spellmod(["SharedHeart"], duration=0.4, cooldown=-0.15)],
             desc="Shared Heart lasts longer and comes back sooner."),
        Node("TradePlaces", None, (3, 1), kind="Spell", spell="TradePlaces", pre=["SharedHeart", "Heartbeat"]),
        Node("Communion", "communion", (5, 1), ranks=3, pre=["Heartbeat"], stats={"AA_ManaRegen": 0.1}),
        Node("Scaled", "scaled", (0, 2), ranks=3, pre=["Entwined"], stats={"ArmorRating_Sharp": 0.04, "ArmorRating_Blunt": 0.04}),
        Node("BloodOffering", None, (2, 2), kind="Spell", spell="BloodOffering", pre=["Entwined", "TradePlaces"]),
        Node("DraconicAspect", None, (4, 2), kind="Spell", spell="DraconicAspect", pre=["TradePlaces", "Communion"]),
        Node("UnderTheWing", None, (6, 2), kind="Spell", spell="UnderTheWing", pre=["Communion"]),
        Node("Lifeblood", "lifeblood", (1, 3), kind="Notable", ranks=2, pre=["Scaled", "BloodOffering"], stats={"AA_Lifesteal": 0.03},
             passives=[P("P_DamageReduction", fractionPerRank=0.05)], desc="What feeds one feeds both. A little lifesteal, and you take 5% less damage per rank."),
        Node("FuryOfTwo", None, (3, 3), kind="Spell", spell="FuryOfTwo", pre=["BloodOffering", "DraconicAspect"]),
        Node("Wingspan", "wingspan", (5, 3), kind="Augment", ranks=2, pre=["DraconicAspect", "UnderTheWing"],
             mods=[spellmod(["DraconicAspect"], duration=0.3, cooldown=-0.15)], desc="Draconic Aspect lasts longer and returns sooner."),
        Node("RekindleBond", None, (2, 4), kind="Spell", spell="RekindleBond", pre=["Lifeblood", "FuryOfTwo"]),
        Node("TwinFlame", "twin flame", (4, 4), kind="Notable", ranks=2, pre=["FuryOfTwo", "Wingspan"], mods=[spellmod(["Wyrmfire", "FuryOfTwo"], damage=0.2)],
             desc="Two throats, one fire: Wyrmfire and Fury of Two burn 20% hotter per rank."),
        Node("Resolve", "resolve", (1, 5), ranks=3, pre=["RekindleBond"], stats={"AA_DurationFactor": 0.05}),
        Node("Kindred", "kindred", (3, 5), ranks=3, pre=["RekindleBond", "TwinFlame"], pct={"AA_SpellPower": 0.03}),
        Node("Updraft", "updraft", (5, 5), ranks=3, pre=["TwinFlame"], stats={"MoveSpeed": 0.08}),
        Node("Undying", "undying bond", (2, 6), kind="Keystone", excl="SoulboundKeystone", pre=["Resolve", "Kindred"],
             passives=[P("P_Rebirth", novaDamage=40.0, novaDamageDef=WF, novaRadius=5.0, wardFraction=0.6, cooldownTicks=DAY, color=rgb(AZURE) + (1.0,))],
             mods=[spellmod(["RekindleBond"], cooldown=-0.5)],
             desc="It will not let you go either. Once a day, when you would die, you rise again in a burst of wyrmfire; and Rekindle returns twice as fast."),
        Node("Dragonheart", "dragonheart", (4, 6), kind="Keystone", excl="SoulboundKeystone", pre=["Kindred", "Updraft"],
             mods=[spellmod(["DraconicAspect"], duration=1.0), tagmod("Draconic", damage=0.15)], stats={"ArmorRating_Heat": 0.5},
             desc="More dragon than mage. Draconic Aspect lasts twice as long, everything draconic hits harder, and fire barely touches you."),
        Node("Skyfire", None, (3, 7), kind="Ultimate", spell="Skyfire", pre=["Undying", "Dragonheart"]),
    )

    # ------------------------------------------------------------------ HOARDKEEPER
    ho = ClassSpec("Hoardkeeper", "hoardkeeper", color=AURUM, color2=BRONZE, icon=("dragon", "coin"), parent=wc, order=1, school=HOARD,
        tagline="A dragon is as strong as its hoard. Get rich, and let the gold wyrm show the raiders what your wealth is for.",
        desc="Hoardkeepers understand what every dragon understands: that there is no such thing as enough.",
        playstyle="Your wyrm turns gold, armoured like a vault. Its power is the colony's wealth: Dragon's Greed gives spell power for every 100,000 "
                  "silver the colony is worth. It smells out hidden ore, sits on a hoard that pays silver every day, and breathes molten gold that "
                  "slows, burns and leaves real gold behind when the victim dies. Coin Storm, Plunder and the Golden Age round out a class that "
                  "pays for itself. Wants Attunement and Focus.",
        lore="The raiders came for the silver. They stayed, in a sense. Several of them are quite valuable now.",
        primary="ATT", secondary="FOC", tags=["Draconic"], height=8)
    ho.add_spells(
        Spell("MoltenGold", "molten gold", "Your wyrm breathes a cone of molten gold seven cells long: 26 wyrmfire, and everything it coats is gold-clad "
              "for ten seconds (half speed, slow to strike, still burning). Whatever dies gold-clad leaves 4 to 10 gold behind.",
              icon=("dragon", "coin"), mana=28, cd=12, range=30, warmup=0.5, tags=["Draconic"], target="enemy_area", delivery="Cone", coneAngle=45.0,
              coneRange=7.0, fromCompanion=True, sound_cast="AA_WyrmBreath", sound_impact="AA_WyrmBreath",
              effects=[dmg(26, WF, armorPen=0.35), hediff("AA_GoldClad", 1.0)], flavor="They do make lovely statues."),
        Spell("SniffTreasure", "sniff out treasure", "Your wyrm lifts its head and smells metal. The two nearest undiscovered veins of valuable ore "
              "(gold, jade, plasteel, uranium, components) are revealed on the map.", icon=("dragon", "crystal"), mana=40, cd=2000, warmup=3.0,
              tags=["Draconic", "Utility"], role="Utility", target="self", quirky=True, impact_vfx=[ring(0.2, 1.0, 30), burst("coin", 10, 2.0, 0.5)],
              effects=[O("Effect_WyrmProspect", veins=2, minValue=4.0), wyrm("Empower")]),
        Spell("DragonsHoard", "dragon's hoard", "Heap up a hoard for your wyrm for fifteen days. Once a day it yields silver in proportion to the colony's "
              "wealth (15 to 180), as long as your wyrm lives, and it is beautiful enough to lift any room.", icon=("coin", "crown"), mana=60, cd=5000,
              range=12, warmup=4.0, tags=["Draconic", "Utility"], role="Utility", target="cell", quirky=True,
              impact_vfx=[ring(0.2, 1.0, 30), burst("coin", 24, 2.5, 0.6)], effects=[spawn("AA_Construct_WyrmHoard", lifetimeTicks=15 * DAY)]),
        Spell("CoinStorm", "coin storm", "Your wyrm shakes out its hoard over the enemy: ten strikes of flung coin and plate across the area, 22 damage "
              "each. One strike in three leaves a few silver on the ground afterwards.", icon=("coin", "burst"), mana=40, cd=24, range=30, warmup=0.7,
              tags=["Draconic"], target="enemy_area", delivery="Barrage", barrageCount=10, barrageInterval=7, barrageRadius=4.5, strikeRadius=1.4,
              strikeStyle="Shard", tier=2, ai_min=2, impact_vfx=[burst("coin", 8, 3.0, 0.5)],
              effects=[dmg(22, "AA_Force", armorPen=0.35), conjure("Silver", 2, 6, chance=0.33)]),
        Spell("Plunder", "plunder", "Your wyrm covets. Every enemy within five cells of it has its weapon torn from its hands and dropped at your feet; "
              "machines and beasts are battered instead.", icon=("dragon", "gun"), mana=36, cd=40, warmup=0.5, tags=["Draconic"], role="Control",
              target="self", delivery="Nova", radius=5.0, fromCompanion=True, tier=2, ai_min=2, impact_vfx=[ring(0.1, 1.2, 26), burst("coin", 14, 3.0, 0.5, byRadius=True)],
              effects=[disarm(True, fallback=20, fallbackDef="AA_Force")], flavor="Mine."),
        Spell("GoldenAge", "golden age", "Declare a golden age. For a day every colonist on the map works 10% faster, trades a little sharper, is "
              "steadier in mind and gains +6 mood.", icon=("coin", "sun"), mana=70, cd=6000, warmup=4.0, tags=["Draconic", "Utility"], role="Utility",
              target="self", delivery="Global", affects="Allies", quirky=True, cast_vfx=[pillar(1.6, 60), screen(AURUM, 0.22, 60)],
              impact_vfx=[burst("coin", 6, 1.4, 0.4)], effects=[hediff("AA_GoldenAge", 1.0), memory("AA_GoldenAgeMood")]),
        Spell("HoardsWrath", "hoard's wrath", "Everything the dragon owns, as fire. Your wyrm breathes a river of molten gold twelve cells long: 110 "
              "wyrmfire, harder the richer the colony (+12% for every 100,000 silver of wealth, up to +120%), and everything it touches is gold-clad.",
              icon=("dragon", "volcano"), mana=220, cd=300, range=34, warmup=2.2, tags=["Draconic"], target="enemy_area", delivery="Cone", coneAngle=60.0,
              coneRange=12.0, fromCompanion=True, ultimate=True, tier=5, xp=2.0, shake=1.8, sound_cast="AA_WyrmRoar", sound_impact="AA_WyrmBreath",
              cast_vfx=[pillar(2.6, 60), screen(AURUM, 0.35, 50)], caster_effects=[O("Effect_WyrmWealth", per100k=0.12, maxBonus=1.2)],
              effects=[dmg(110, WF, armorPen=0.6), hediff("AA_GoldClad", 1.0), ignite(0.2, 0.5, False)]),
    )
    ho.add_nodes(
        Node("MoltenGold", None, (2, 0), kind="Spell", spell="MoltenGold"),
        Node("Avarice", "avarice", (4, 0), ranks=3, stats={"TradePriceImprovement": 0.03, "AA_ManaMax": 6}),
        Node("Crucible", "crucible", (1, 1), kind="Augment", ranks=2, pre=["MoltenGold"], mods=[spellmod(["MoltenGold"], damage=0.2, cooldown=-0.1)],
             desc="Molten Gold burns hotter and comes back sooner."),
        Node("SniffTreasure", None, (3, 1), kind="Spell", spell="SniffTreasure", pre=["MoltenGold", "Avarice"]),
        Node("DragonsGreed", "dragon's greed", (5, 1), kind="Notable", ranks=3, pre=["Avarice"], passives=[P("P_WyrmHoard", per100kPerRank=0.03, maxPerRank=0.12)],
             desc="A dragon is as strong as its hoard. +3% spell power per rank for every 100,000 silver the colony is worth, up to +12% per rank."),
        Node("Gilded", "gilded", (0, 2), ranks=3, pre=["Crucible"], stats={"ArmorRating_Sharp": 0.04, "AA_WardMax": 6}),
        Node("DragonsHoard", None, (2, 2), kind="Spell", spell="DragonsHoard", pre=["Crucible", "SniffTreasure"]),
        Node("CoinStorm", None, (4, 2), kind="Spell", spell="CoinStorm", pre=["SniffTreasure", "DragonsGreed"]),
        Node("Appraiser", "appraiser", (6, 2), ranks=3, pre=["DragonsGreed"], stats={"NegotiationAbility": 0.08, "TradePriceImprovement": 0.02}),
        Node("Tithe", "tithe", (1, 3), kind="Notable", ranks=2, pre=["Gilded", "DragonsHoard"], passives=[P("P_ManaOnKill", amount=8.0)],
             desc="Everything that dies pays you something. Each kill restores mana."),
        Node("Plunder", None, (3, 3), kind="Spell", spell="Plunder", pre=["DragonsHoard", "CoinStorm"]),
        Node("Mint", "mint", (5, 3), kind="Augment", ranks=2, pre=["CoinStorm", "Appraiser"], mods=[spellmod(["CoinStorm"], extraTargets=2, damage=0.1)],
             desc="Coin Storm: four more strikes per rank."),
        Node("Nosey", "a nose for it", (2, 4), kind="Augment", pre=["Tithe", "Plunder"], mods=[spellmod(["SniffTreasure"], extraTargets=1, cooldown=-0.3)],
             desc="Sniff Out Treasure finds a third vein and comes back sooner."),
        Node("GoldenAge", None, (4, 4), kind="Spell", spell="GoldenAge", pre=["Plunder", "Mint"]),
        Node("Vault", "vault", (1, 5), ranks=3, pre=["Nosey"], stats={"AA_DurationFactor": 0.05}),
        Node("Opulence", "opulence", (3, 5), ranks=3, pre=["Nosey", "GoldenAge"], pct={"AA_SpellPower": 0.03}),
        Node("Usury", "usury", (5, 5), ranks=3, pre=["GoldenAge"], stats={"AA_ManaRegen": 0.1}),
        Node("KingUnderTheMountain", "king under the mountain", (2, 6), kind="Keystone", excl="HoardKeystone", pre=["Vault", "Opulence"],
             passives=[P("P_WyrmHoard", per100kPerRank=0.04, maxPerRank=0.2)], stats={"AA_WardMax": 20},
             desc="Sit on it. A further +4% spell power per 100,000 silver of colony wealth (up to +20%) and a stronger ward."),
        Node("Midas", "the golden touch", (4, 6), kind="Keystone", excl="HoardKeystone", pre=["Opulence", "Usury"],
             passives=[P("P_OnHitHediff", hediff="AA_GoldClad", chancePerRank=0.3, severity=1.0, tag="Draconic", cooldownTicks=120)],
             mods=[spellmod(["MoltenGold", "CoinStorm"], cooldown=-0.3)],
             desc="Everything you burn is gilded: Draconic hits often leave their target gold-clad, and Molten Gold and Coin Storm return sooner."),
        Node("HoardsWrath", None, (3, 7), kind="Ultimate", spell="HoardsWrath", pre=["KingUnderTheMountain", "Midas"]),
    )

    # ------------------------------------------------------------------ BROODMOTHER
    br = ClassSpec("Broodmother", "broodmother", color=CRIMSON, color2=CORAL, icon=("egg", "dragon"), parent=wc, order=2, school=BROOD,
        tagline="One dragon is a weapon. A brood is a problem. Clutches of drakelings, egg bombs, and a nest that feeds the colony.",
        desc="A Broodmother's wyrm lays. Constantly. What hatches is small, fast, furious and fiercely loyal to its grandmother.",
        playstyle="Your wyrm becomes a red brood wyrm, quick and savage. Clutch lays eggs that hatch into drakelings, Egg Bomb throws one that "
                  "explodes into fire and another drakeling, Pounce sends the whole brood onto one target, and Culling feeds them back to the wyrm "
                  "to heal it and make it stronger. At home a Nest lays eggs for the kitchen, the Brood Hunts for meat, and Brooding hatches every "
                  "fertilized egg in the barn. Wants Attunement and Celerity.",
        lore="She stopped naming them after the first two hundred.",
        primary="ATT", secondary="CEL", tags=["Draconic"], height=8)
    br.add_spells(
        Spell("Clutch", "clutch", "Your wyrm's brood lays three eggs at the target. Five seconds later they hatch into drakelings that fight for you "
              "for ninety seconds.", icon=("egg", "claw"), mana=34, cd=30, range=24, warmup=0.6, tags=["Draconic", "Summon"], role="Summon", target="cell",
              impact_vfx=[burst("scale", 8, 1.5, 0.5)],
              effects=[spawn("AA_Construct_ClutchEgg", count=3, lifetimeTicks=300),
                       sequence(step(300, [summon(DRAKELING, 3, 90 * SEC)], radius=0.0, vfx=[flash(2.4), burst("flame", 10, 2.5, 0.6)], sound="AA_WyrmRoar"))]),
        Spell("EggBomb", "egg bomb", "Lob an egg that was never going to hatch properly. It bursts for 34 wyrmfire within two and a half cells, leaves "
              "everything burning, and one very angry drakeling climbs out of the wreckage for a minute.", icon=("egg", "fireball"), mana=24, cd=10,
              range=28, warmup=0.5, tags=["Draconic"], target="enemy_area", delivery="Projectile", radius=2.5, projectileTex="ArcanumAscendant/Vfx/EggShot",
              projectileScale=1.0, projectileSpeed=18.0, projectileArc=1.2, projectileSpin=True, ai_min=1,
              effects=[dmg(34, WF, armorPen=0.3, falloff=True), burn(1), summon(DRAKELING, 1, 60 * SEC)]),
        Spell("BroodPounce", "pounce", "Every drakeling you have leaps onto the target point at once. Each lands for 14 damage on whatever is within a cell "
              "and a half of it.", icon=("claw", "dragon"), mana=20, cd=12, range=30, warmup=0.3, tags=["Draconic"], target="cell", hostile=True,
              autocast=False, impact_vfx=[], effects=[brood("Pounce", damage=14.0, damageDef="Scratch")]),
        Spell("Culling", "culling", "The wyrm eats its young. Every drakeling you have is consumed: each heals the wyrm for 40 and makes it 12% "
              "stronger and faster for thirty seconds (stacking eight times).", icon=("dragon", "skull"), mana=18, cd=30, warmup=0.3, tags=["Draconic"],
              role="Buff", target="self", autocast=False, impact_vfx=[], effects=[brood("Cull", healPerDrakeling=40.0, wyrmHediff="AA_Matriarch")],
              flavor="There will be more."),
        Spell("WyrmNest", "wyrm nest", "Scrape out a nest for five days. Twice a day your wyrm leaves two eggs in it, each a meal for six once cooked, "
              "and the nest keeps the room warm.", icon=("egg", "leaf"), mana=50, cd=4000, range=12, warmup=4.0, tags=["Draconic", "Utility"],
              role="Utility", target="cell", quirky=True, impact_vfx=[ring(0.2, 1.0, 30), burst("scale", 12, 1.6, 0.5)],
              effects=[spawn("AA_Construct_WyrmNest", lifetimeTicks=5 * DAY)]),
        Spell("BroodHunts", "the brood hunts", "Let them off the leash. Drakelings bring down the four nearest wild animals on the map (nothing much "
              "bigger than a deer) and leave the bodies for the butcher.", icon=("claw", "bone"), mana=45, cd=2000, warmup=3.0, tags=["Draconic", "Utility"],
              role="Utility", target="self", quirky=True, sound_cast="AA_WyrmRoar", impact_vfx=[burst("scale", 10, 2.5, 0.5)],
              effects=[O("Effect_WyrmHunt", count=4, maxBodySize=1.6)]),
        Spell("Brooding", "brooding", "Your wyrm settles over the eggs around a point and warms them. Every fertilized egg within five cells moves "
              "halfway to hatching, and any that are ready hatch on the spot: chickens, ducks, anything that lays.", icon=("egg", "heart"), mana=35,
              cd=2000, range=24, warmup=3.0, tags=["Draconic", "Utility"], role="Utility", target="cell", radius=5.0, quirky=True,
              impact_vfx=[ring(0.2, 1.0, 30)] + SOFT, effects=[O("Effect_WyrmBrooding", progress=0.5)]),
        Spell("QueensClutch", "queen's clutch", "It rains eggs. Nine egg bombs fall across the area: each bursts for 55 wyrmfire, sets everything "
              "burning and hatches a drakeling that fights for a minute.", icon=("egg", "crown"), mana=220, cd=300, range=32, warmup=2.2,
              tags=["Draconic"], target="enemy_area", delivery="Barrage", barrageCount=9, barrageInterval=10, barrageRadius=6.0, strikeRadius=2.2,
              strikeStyle="Meteor", ultimate=True, tier=5, xp=2.0, shake=1.6, ai_min=4, sound_cast="AA_WyrmRoar",
              cast_vfx=[pillar(2.6, 60), screen(CRIMSON, 0.3, 50)], impact_vfx=[flash(3.0), burst("flame", 14, 3.5, 0.8, byRadius=True)],
              effects=[dmg(55, WF, armorPen=0.45, falloff=True), burn(2), summon(DRAKELING, 1, 60 * SEC)]),
    )
    br.add_nodes(
        Node("Clutch", None, (2, 0), kind="Spell", spell="Clutch"),
        Node("Fecund", "many mouths", (4, 0), ranks=3, stats={"AA_SummonPower": 0.05}),
        Node("LargeClutch", "large clutch", (1, 1), kind="Augment", ranks=2, pre=["Clutch"], mods=[spellmod(["Clutch"], extraTargets=1, cooldown=-0.1)],
             desc="One more egg per clutch per rank."),
        Node("EggBomb", None, (3, 1), kind="Spell", spell="EggBomb", pre=["Clutch", "Fecund"]),
        Node("Motherhood", "motherhood", (5, 1), ranks=3, pre=["Fecund"], stats={"AA_ManaRegen": 0.1}),
        Node("Sharp", "sharp little teeth", (0, 2), ranks=3, pre=["LargeClutch"], mods=[tagmod("Summon", duration=0.08)]),
        Node("BroodPounce", None, (2, 2), kind="Spell", spell="BroodPounce", pre=["LargeClutch", "EggBomb"]),
        Node("WyrmNest", None, (4, 2), kind="Spell", spell="WyrmNest", pre=["EggBomb", "Motherhood"]),
        Node("Brooding", None, (6, 2), kind="Spell", spell="Brooding", pre=["Motherhood"]),
        Node("PackTactics", "pack tactics", (1, 3), kind="Augment", ranks=2, pre=["Sharp", "BroodPounce"], mods=[spellmod(["BroodPounce"], damage=0.3, cooldown=-0.15)],
             desc="Pounce lands harder and comes back sooner."),
        Node("Culling", None, (3, 3), kind="Spell", spell="Culling", pre=["BroodPounce", "WyrmNest"]),
        Node("Hatchery", "hatchery", (5, 3), kind="Notable", pre=["WyrmNest", "Brooding"],
             passives=[P("P_Aura", radius=10.0, interval=HOUR, affects="Allies", color=rgb(CRIMSON) + (1.0,), effects=[animal_produce(0.015)])],
             stats={"TameAnimalChance": 0.1}, desc="Everything lays better near a broodmother. Every hour, tame animals within ten cells of you come "
                                                   "1.5% closer to their next egg, milking or shearing."),
        Node("Swarm", "swarm", (2, 4), kind="Notable", ranks=2, pre=["PackTactics", "Culling"],
             passives=[P("P_TriggerSpell", trigger="OnKill", spell=S("EggBomb"), chancePerRank=0.2, target="NearestEnemy", cooldownTicks=240)],
             desc="Death is an opportunity. Your kills sometimes throw an Egg Bomb at the next enemy."),
        Node("BroodHunts", None, (4, 4), kind="Spell", spell="BroodHunts", pre=["Culling", "Hatchery"]),
        Node("Quick", "quick", (1, 5), ranks=3, pre=["Swarm"], stats={"AA_CastSpeed": 0.04}),
        Node("Matron", "matron", (3, 5), ranks=3, pre=["Swarm", "BroodHunts"], pct={"AA_SpellPower": 0.03}),
        Node("Provider", "provider", (5, 5), kind="Augment", pre=["BroodHunts"], mods=[spellmod(["BroodHunts"], extraTargets=2, cooldown=-0.3)],
             desc="The Brood Hunts brings down two more animals and returns sooner."),
        Node("Legion", "legion", (2, 6), kind="Keystone", excl="BroodKeystone", pre=["Quick", "Matron"],
             mods=[spellmod(["Clutch", "QueensClutch"], extraTargets=2), tagmod("Summon", duration=0.5)],
             desc="There are always more. Two more eggs in every clutch, and drakelings last half again as long."),
        Node("Devourer", "devourer", (4, 6), kind="Keystone", excl="BroodKeystone", pre=["Matron", "Provider"],
             mods=[spellmod(["Culling"], cooldown=-0.5, healing=0.5), tagmod("Draconic", damage=0.15)],
             desc="The brood exists to feed the mother. Culling returns twice as fast and heals half again as much, and everything draconic hits harder."),
        Node("QueensClutch", None, (3, 7), kind="Ultimate", spell="QueensClutch", pre=["Legion", "Devourer"]),
    )
    return wc, [so, ho, br], statuses()

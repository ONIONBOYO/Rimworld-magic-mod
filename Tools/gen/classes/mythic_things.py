"""ThingDefs for the constructs, entities and items of the Harbinger, Wind Mage, Succubus, Incubus and Noclipper.
Their textures are painted by Tools/art/mythic_art.py, so none of them go through the generic construct icon list."""
from arcgen import O, to_xml, rgb


def _construct_comp(color, lifetime, pulse=None, radius=4.0, interval=120, affects="Enemies", glow=1.6):
    return O("ArcanumAscendant.CompProperties_ArcaneConstruct", defaultLifetimeTicks=int(lifetime), glowColor=rgb(color) + (1.0,), glowSize=float(glow),
             pulseEffects=pulse, pulseInterval=int(interval) if pulse else None, pulseRadius=float(radius) if pulse else None,
             pulseAffects=affects if pulse else None)


def deflector(radius, reflect=0.0, color="#7ef0d2", dome=True, reflectDef=None):
    return O("ArcanumAscendant.CompProperties_ArcaneDeflector", radius=float(radius), reflectFraction=float(reflect) if reflect else None,
             reflectDamageDef=reflectDef, color=rgb(color) + (1.0,), drawDome=dome)


def construct(tid, label, desc, *, tex, color, hp=200, lifetime=1800, size=1.4, wall=False, pulse=None, radius=4.0, interval=120,
              affects="Enemies", glow=1.6, comps=(), light=None):
    """A temporary conjured structure. Walls block movement and sight; everything else can be walked past."""
    all_comps = [_construct_comp(color, lifetime, pulse, radius, interval, affects, glow)] + list(comps)
    if light:
        all_comps.append(O("CompProperties_Glower", glowRadius=float(light), glowColor="(%d,%d,%d,0)" % tuple(int(c * 255) for c in rgb(color))))
    f = dict(defName="AA_" + tid, label=label, description=desc, thingClass="Building", category="Building", tickerType="Normal",
             altitudeLayer="Building", drawerType="MapMeshAndRealTime", selectable=True, useHitPoints=True, rotatable=False,
             graphicData=O(None, texPath="ArcanumAscendant/Things/" + tex, graphicClass="Graphic_Single", drawSize=float(size)),
             statBases={"MaxHitPoints": int(hp), "Flammability": 0.0, "Beauty": 0},
             building=O(None, claimable=False, destroySound="AA_Construct_Break"),
             comps=all_comps)
    if wall:
        f.update(passability="Impassable", fillPercent=1.0, blockWind=True, castEdgeShadows=True, staticSunShadowHeight=0.8, holdsRoof=False,
                 blockLight=True)
    else:
        f.update(passability="PassThroughOnly", pathCost=40, fillPercent=0.25)
    return to_xml("ThingDef", O(None, **f), 1)


def creature(defName, label, desc, *, tex, size, body, body_size, health, speed, tools, color, combat_power=80, armor=0.0, heat_armor=0.0,
             flies=False):
    """A summonable creature that exists only while magic holds it together: no food, no rest, no corpse worth keeping."""
    race = O(None, body=body, baseBodySize=float(body_size), baseHealthScale=float(health), baseHungerRate=0.01, foodType="None",
             lifeExpectancy=100, intelligence="Animal", thinkTreeMain="Animal", thinkTreeConstant="AnimalConstant",
             manhunterOnDamageChance=1.0, hasGenders=False, needsRest=False, petness=0.0, canBePredatorPrey=False, herdAnimal=False,
             predator=True, maxPreyBodySize=0.0, alwaysViolent=True, isImmuneToInfections=True, bleedRateFactor=0.2, hasMeat=False,
             nameOnTameChance=0.0, trainability="None",
             lifeStageAges=[O(None, def_="AnimalAdult", minAge=0.0)],
             soundMeleeHitPawn="Pawn_Melee_BigBash_HitPawn", soundMeleeHitBuilding="Pawn_Melee_BigBash_HitBuilding", soundMeleeMiss="Pawn_Melee_BigBash_Miss")
    things = to_xml("ThingDef", O(None, defName=defName, label=label, description=desc,
        statBases={"MoveSpeed": float(speed), "MarketValue": 0, "ComfyTemperatureMin": -80, "ComfyTemperatureMax": 120,
                   "ArmorRating_Sharp": float(armor), "ArmorRating_Blunt": float(armor), "ArmorRating_Heat": float(heat_armor),
                   "Wildness": 1.0, "Flammability": 0.0, "LeatherAmount": 0, "MeatAmount": 0, "FilthRate": 0},
        tools=tools, race=race, tradeability="None"), 1)
    things = things.replace("<ThingDef>", '<ThingDef ParentName="AnimalThingBase">', 1).replace("<def_>", "<def>").replace("</def_>", "</def>")
    kind = to_xml("PawnKindDef", O(None, defName=defName, label=label, race=defName, combatPower=float(combat_power),
        ecoSystemWeight=0.0, canArriveManhunter=False,
        lifeStages=[O(None, bodyGraphicData=O(None, texPath="ArcanumAscendant/Pawns/" + tex, graphicClass="Graphic_Multi", drawSize=float(size),
                                              shadowData=O(None, volume=(0.3, 0.3, 0.3), offset=(0.0, 0.0, -0.25))),
                         dessicatedBodyGraphicData=O(None, texPath="Things/Pawn/Animal/Dessicated/CritterDessicatedMedium", graphicClass="Graphic_Multi",
                                                     drawSize=float(size)))]), 1)
    kind = kind.replace("<PawnKindDef>", '<PawnKindDef ParentName="AnimalKindBase">', 1)
    return [things, kind]


def tool(label, capacities, power, cooldown, part, chance=None, pen=None):
    return O(None, label=label, capacities=list(capacities), power=float(power), cooldownTime=float(cooldown), linkedBodyPartsGroup=part,
             chanceFactor=chance, armorPenetration=pen)


def almond_water():
    """A drink from the Backrooms: restores a little mana and steadies the mind."""
    xml = to_xml("ThingDef", O(None, defName="AA_AlmondWater", label="almond water",
        description="A plastic bottle of faintly cloudy water that smells of almonds. Found in the walls of places that do not exist. Drinking it restores "
                    "60 mana to a mage and steadies anyone's nerves for a few hours.",
        graphicData=O(None, texPath="ArcanumAscendant/Items/AlmondWater", graphicClass="Graphic_Single"), stackLimit=25,
        statBases={"MaxHitPoints": 40, "MarketValue": 20, "Mass": 0.3, "Flammability": 0.0, "DeteriorationRate": 1.0},
        ingestible=O(None, preferability="NeverForNutrition", foodType="Fluid, Processed", maxNumToIngestAtOnce=1, baseIngestTicks=80, chairSearchRadius=0,
                     joy=0.08, joyKind="Gluttonous",
                     outcomeDoers=[O("ArcanumAscendant.IngestionOutcomeDoer_RestoreMana", amount=60.0),
                                   O("IngestionOutcomeDoer_GiveHediff", hediffDef="AA_AlmondCalm", severity=1.0)])), 1)
    return xml.replace("<ThingDef>", '<ThingDef ParentName="AA_ArcaneItemBase">', 1)

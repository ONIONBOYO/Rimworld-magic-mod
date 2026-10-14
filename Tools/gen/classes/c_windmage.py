"""
The Wind Mage: the battlefield as a bowling alley. Spells throw enemies through the air for real (they fly, land and
slam into walls), suck the air out of their lungs, blow their bullets off course and cut them to ribbons.
"""
from classes.common import *  # noqa: F401,F403
from classes.expansion2 import missiles, channel  # noqa: F401
from classes import mythic_things as MT

MINT, AIR = "#7ef0d2", "#f4fffb"
WIND = School("Wind", MINT, AIR, fleck="feather", fleck2="mote", projectile="ArcanumAscendant/Vfx/WindBlade")
BLADE = School("Wind", "#b8ffe9", "#2fb38c", fleck="spark", fleck2="feather", projectile="ArcanumAscendant/Vfx/WindBlade")
SAGE = School("Wind", "#a0f0ff", "#2a6a8a", fleck="ring", fleck2="mote", projectile="ArcanumAscendant/Vfx/Orb")
WARDEN = School("Wind", "#e6fff7", "#6aa8ff", fleck="feather", fleck2="star", projectile="ArcanumAscendant/Vfx/WindBlade")

KEYS = {
    "AA_Cyclone": "Cyclone",
    "AA_Implode": "Implosion",
}

GUST_HIT = [flash(1.6), burst("feather", 6, 2.5, 0.5), glow(1.0, 2.0, 16, useColor2=True)]


def statuses():
    s = []
    s.append(status("AA_Suffocating", "suffocating", "The air has been pulled away from this creature. It can barely breathe, cannot speak, and is slowly dying.",
        "#a0f0ff", bad=True, ticks=480, maxSev=3, comps=[dot("AA_Gale", 2.5, "#a0f0ff")],
        stages=[stage(caps={"Breathing": -0.55, "Talking": -0.8}, factors={"MoveSpeed": 0.8})], fx=dict(emitter="Gale", emitInterval=12)))
    s.append(status("AA_Tailwind", "tailwind", "Wrapped in a tailwind: faster, and a third of incoming shots are blown off course.", MINT, bad=False, ticks=1800,
        comps=[deflect(0.33, True, color=MINT)], stages=[stage(factors={"MoveSpeed": 1.35})], fx=dict(emitter="Gale", emitInterval=10)))
    s.append(status("AA_EyeOfTheStorm", "eye of the storm", "Standing in the calm eye of a storm of blades. Half of all attacks, ranged or melee, are turned aside and part "
        "of their force thrown back.", "#b8ffe9", bad=False, ticks=1200, comps=[deflect(0.5, False, reflect=0.4, reflectDef="AA_Gale", color="#b8ffe9")],
        stages=[stage(factors={"MoveSpeed": 1.25, "MeleeHitChance": 1.2, "MeleeCooldownFactor": 0.8})],
        fx=dict(emitter="Gale", emitInterval=5, overlayTex="ArcanumAscendant/Vfx/Vortex", overlaySize=2.0, overlaySpin=-6.0)))
    s.append(status("AA_GaleGuard", "gale guard", "A wall of wind turns most bullets aside and hurls their force back at the shooter.", "#e6fff7", bad=False, ticks=1500,
        comps=[deflect(0.6, True, reflect=0.5, reflectDef="AA_Gale", color="#e6fff7")], fx=dict(emitter="Gale", emitInterval=8,
        overlayTex="ArcanumAscendant/Vfx/HexShield", overlaySize=1.8, overlayPulse=True)))
    s.append(status("AA_OffBalance", "off balance", "Buffeted by the wind: hard to aim, hard to swing.", MINT, bad=True, ticks=300,
        stages=[stage(factors={"ShootingAccuracyPawn": 0.7, "MeleeHitChance": 0.8, "MoveSpeed": 0.85})]))
    s.append(status("AA_Cyclone", "cyclone", "Caught in a personal cyclone and thrown about every second and a half.", MINT, bad=True, ticks=420,
        comps=[periodic(90, effects=[launch("Upward", 0, 7, "AA_Gale", stunTicks=20)], color=MINT)],
        fx=dict(emitter="Gale", emitInterval=4, overlayTex="ArcanumAscendant/Vfx/Vortex", overlaySize=1.7, overlaySpin=-9.0)))
    s.append(status("AA_PressureLocked", "pressure-locked", "Pinned in place by crushing air pressure, and the pressure makes every blow land harder.", "#a0f0ff",
        bad=True, ticks=360, comps=[compel("Hold")], stages=[stage(factors={"IncomingDamageFactor": 1.2, "MoveSpeed": 0.2})], fx=dict(emitter="Gale", emitInterval=16)))
    s.append(MT.construct("Construct_WindWall", "wind wall", "A standing gale. Bullets and arrows that cross it are torn apart. It blows itself out after half a minute.",
        tex="WindWall", color=MINT, hp=150, lifetime=1800, size=1.3, glow=1.2, comps=[MT.deflector(1.3, color=MINT, dome=False)]))
    s.append(MT.construct("Construct_AegisPylon", "aegis of gales", "A spinning knot of wind at the heart of a dome. Hostile shots from outside the dome are torn "
        "apart, and half their force is thrown back at the shooter.", tex="AegisPylon", color="#bfefff", hp=300, lifetime=2400, size=1.5, glow=2.0,
        comps=[MT.deflector(5.0, reflect=0.5, color="#bfefff", reflectDef="AA_Gale")]))
    s.append(MT.construct("Construct_HeavensBulwark", "heaven's bulwark", "The sky itself stands guard. A vast dome of wind that tears apart every hostile shot "
        "fired into it and throws the full force back at the shooter.", tex="AegisPylon", color="#e6fff7", hp=800, lifetime=3600, size=2.4, glow=3.5,
        comps=[MT.deflector(9.0, reflect=1.0, color="#e6fff7", reflectDef="AA_Gale")], light=8.0))
    return s


def build():
    w = ClassSpec("WindMage", "wind mage", color=MINT, color2=AIR, icon=("gust", "tornado"), order=11, school=WIND,
        tagline="Throw the battlefield around. Hurl enemies through the air into walls, steal their breath and blow their bullets away.",
        desc="Wind Mages treat a fight like weather: something that happens to other people. Their enemies spend most of it in the air.",
        playstyle="Positioning is a weapon. Gust, Hurl and Sky Toss really throw enemies: the farther they fly, the harder they land, and walls "
                  "hurt most of all. Vacuum Bubble silences and suffocates, Tailwind and Wind Wall turn bullets aside, and the Cyclone sucks a "
                  "whole squad in before blasting it apart. Wants Celerity and Potency.",
        lore="Her teacher told her to go fly a kite. She took it very personally, and so did the raiders.",
        primary="CEL", secondary="POT", tags=["Wind"], starting=["Gust"], innate={"MoveSpeed": 0.1})
    w.add_spells(
        Spell("Gust", "gust", "A blade of wind that cuts and shoves its target back three cells. Anything shoved into a wall takes far more damage.",
              icon=("gust", "arrow"), mana=8, cd=1.5, range=26, warmup=0.4, tags=["Wind"], delivery="Projectile", projectileSpeed=30.0, projectileScale=0.8,
              impact_vfx=GUST_HIT, effects=[dmg(9, "AA_Gale"), launch("AwayFromCaster", 3, 4, "AA_Gale", slam=2.5, stunTicks=20)],
              flavor="Mind the wall."),
        Spell("Updraft", "updraft", "A column of wind pops everything in a small area into the air. They come down hard and stunned.", icon=("tornado", "arrow"),
              mana=18, cd=10, range=26, warmup=0.5, tags=["Wind"], role="Control", target="enemy_area", radius=2.2, ai_min=2,
              impact_vfx=[ring(0.2, 1.0, 22), burst("feather", 12, 3.0, 0.6, byRadius=True)],
              effects=[dmg(6, "AA_Gale"), launch("Upward", 0, 14, "AA_Gale", stunTicks=120)]),
        Spell("Hurl", "hurl", "Grab an enemy with a fist of wind and hurl it ten cells. It hurts whatever it lands on, and walls hurt most of all.",
              icon=("hand", "gust"), mana=22, cd=12, range=24, warmup=0.5, tags=["Wind"], impact_vfx=GUST_HIT,
              effects=[launch("AwayFromCaster", 10, 18, "AA_Gale", slam=2.5, splash=2.0, splashFactor=0.6, stunTicks=60)], flavor="Fore!"),
        Spell("ZephyrDash", "zephyr dash", "Ride a gust to the target spot. You land in a burst of wind that knocks everyone around you back.",
              icon=("wing", "gust"), mana=14, cd=8, range=18, warmup=0.2, tags=["Wind", "Mobility"], role="Mobility", target="cell",
              impact_vfx=[burst("feather", 8, 2.0, 0.5)],
              effects=[launch("CasterToTarget", 0, 0, landing=[knock(2.5), dmg(10, "AA_Gale")], landingRadius=2.5)]),
        Spell("AirLance", "air lance", "Compress the air into a lance and drive it through everything in a straight line, punching through armour and shoving "
              "targets aside.", icon=("arrow", "gust"), mana=24, cd=9, range=18, warmup=0.6, tags=["Wind"], target="enemy_area", delivery="Line",
              lineLength=16.0, strikeRadius=0.9, lineStepDelay=1, tier=2, ai_min=2, impact_vfx=[burst("feather", 4, 2.0, 0.4)],
              effects=[dmg(18, "AA_Gale", armorPen=0.4), knock(1.5)]),
        Spell("VacuumBubble", "vacuum bubble", "Pull all the air away from an enemy's head for eight seconds. It suffocates, cannot call out, and cannot cast spells.",
              icon=("orb", "gust"), mana=26, cd=18, range=26, warmup=0.6, tags=["Wind"], role="Control", tier=2,
              impact_vfx=[ring(0.6, 0.1, 20, byRadius=False), glow(1.4, 0.6, 30)],
              effects=[hediff("AA_Suffocating", 1.0, stack=True, maxSeverity=3.0), hediff("AA_Silenced", 1.0, duration=480)]),
        Spell("Tailwind", "tailwind", "Wrap an ally in a tailwind for thirty seconds: much faster, and a third of incoming shots are blown off course.",
              icon=("wing", "shield"), mana=18, cd=25, range=24, warmup=0.4, tags=["Wind", "Ward"], role="Buff", target="ally",
              impact_vfx=[burst("feather", 10, 2.0, 0.5), ring(0.3, 0.9, 20, byRadius=False)], effects=[hediff("AA_Tailwind", 1.0)]),
        Spell("WindWall", "wind wall", "Raise a line of standing gales for thirty seconds. Bullets, arrows and shells that cross it are torn apart.",
              icon=("wall", "gust"), mana=34, cd=30, range=22, warmup=0.6, tags=["Wind", "Ward"], role="Control", target="cell", delivery="Line",
              lineLength=8.0, strikeRadius=0.4, lineStepDelay=1, tier=2, impact_vfx=[burst("feather", 4, 1.5, 0.4)],
              effects=[spawn("AA_Construct_WindWall", lifetimeTicks=1800)]),
        Spell("Snatch", "snatch", "A cheeky gust plucks the weapon out of an enemy's hands and drops it at your feet. Mechanoids just get buffeted.",
              icon=("hand", "gun"), mana=20, cd=30, range=22, warmup=0.4, tags=["Wind"], role="Control", tier=2, impact_vfx=GUST_HIT,
              effects=[disarm(True, fallback=18, fallbackDef="AA_Gale")], flavor="Finders keepers."),
        Spell("SkyToss", "sky toss", "Every enemy around you is thrown high into the air and slammed back into the ground.", icon=("tornado", "burst"),
              mana=45, cd=30, warmup=0.6, tags=["Wind"], target="self", delivery="Nova", radius=4.5, ai_min=3, tier=3, shake=1.2,
              impact_vfx=[ring(0.1, 1.2, 30), burst("feather", 24, 4.0, 0.6, byRadius=True), smoke(4, 1.2)],
              effects=[dmg(10, "AA_Gale"), launch("Upward", 0, 30, "AA_Gale", stunTicks=150)]),
        Spell("Cyclone", "cyclone", "Spin up a cyclone over an area. For eight seconds it drags enemies toward its heart and batters them; then it explodes, "
              "blasting everything inside outward.", icon=("tornado", "swirl"), mana=60, cd=45, range=30, warmup=1.0, tags=["Wind"], target="enemy_area",
              ai_min=2, tier=3, shake=0.8, impact_vfx=[ring(0.2, 1.0, 30), flash(2.0)], sound_cast="AA_Gust",
              effects=[field(4.0, 480, 30, [dmg(5, "AA_Gale", affects="Enemies"), hediff("AA_OffBalance", 1.0, affects="Enemies")],
                             groundTex="ArcanumAscendant/Vfx/Vortex", groundSpin=-12.0, groundAlpha=0.55, ambientFleck="AA_Fleck_Feather",
                             ambientDensity=4.0, ambientSpeed=3.0, pullStrength=2.5),
                       sequence(step(480, [dmg(15, "AA_Gale"), launch("AwayFromImpact", 7, 22, "AA_Gale", slam=2.0)], radius=4.5,
                                     vfx=[ring(0.1, 1.2, 30), burst("feather", 24, 5.0, 0.6, byRadius=True), flash(3.0)], shake=1.5, sound="AA_Gust",
                                     text="AA_Cyclone", textColor=MINT))]),
        Spell("DustDevil", "dust devil", "Trap an enemy in its own little cyclone for seven seconds. It is thrown into the air every second and a half, and "
              "lands hard every time.", icon=("tornado", "eye"), mana=30, cd=24, range=26, warmup=0.5, tags=["Wind"], role="Control", tier=2,
              impact_vfx=[ring(0.2, 0.9, 24, byRadius=False), burst("feather", 12, 3.0, 0.5)], effects=[hediff("AA_Cyclone", 1.0), dmg(6, "AA_Gale")]),
        Spell("CleansingWind", "cleansing wind", "Call a great wind across the whole map. It blows away toxic fallout and volcanic ash, clears the sky and snuffs "
              "out the fires around you.", icon=("gust", "sun"), mana=60, cd=DAY * 2 / SEC, warmup=4.0, tags=["Wind", "Utility", "Weather"],
              role="Utility", target="self", radius=10.0, affects="All", quirky=True, sound_cast="AA_Gust",
              impact_vfx=[ring(0.1, 1.2, 50), burst("feather", 30, 4.0, 0.6, byRadius=True)],
              effects=[condition(None, 0, remove=["ToxicFallout", "VolcanicWinter"]), weather("Clear"), extinguish()],
              flavor="Open a window."),
    )
    w.add_nodes(
        Node("Squall", "squall", (1, 0), ranks=3, mods=[tagmod("Wind", damage=0.05)]),
        Node("LightFooted", "light-footed", (3, 0), ranks=3, stats={"MoveSpeed": 0.08, "AA_CastSpeed": 0.02}),
        Node("OpenSky", "open sky", (5, 0), ranks=3, stats={"AA_ManaMax": 10, "AA_ManaRegen": 0.08}),
        Node("Hurl", None, (1, 1), kind="Spell", spell="Hurl", pre=["Squall"]),
        Node("ZephyrDash", None, (3, 1), kind="Spell", spell="ZephyrDash", pre=["LightFooted"]),
        Node("Updraft", None, (5, 1), kind="Spell", spell="Updraft", pre=["OpenSky"]),
        Node("Bowling", "bowling", (0, 2), kind="Augment", ranks=2, pre=["Hurl"], mods=[spellmod(["Hurl", "Gust"], damage=0.2, range=0.1)],
             desc="Thrown enemies hit harder, and so does whatever they land on."),
        Node("Crosscurrent", "crosscurrent", (2, 2), kind="Notable", pre=["Hurl", "ZephyrDash"],
             passives=[P("P_OnHitHediff", hediff="AA_OffBalance", chancePerRank=0.3, severity=1.0, tag="Wind")],
             desc="Wind spells often leave enemies off balance: worse aim, weaker swings."),
        Node("Tailwind", None, (4, 2), kind="Spell", spell="Tailwind", pre=["ZephyrDash", "Updraft"]),
        Node("Thermals", "thermals", (6, 2), kind="Augment", ranks=2, pre=["Updraft"], mods=[spellmod(["Updraft"], radius=0.2, cooldown=-0.1)]),
        Node("AirLance", None, (1, 3), kind="Spell", spell="AirLance", pre=["Bowling", "Crosscurrent"]),
        Node("DustDevil", None, (6, 3), kind="Spell", spell="DustDevil", pre=["Thermals"]),
        Node("WindRider", "wind rider", (3, 3), ranks=3, pre=["Crosscurrent", "Tailwind"], stats={"AA_CooldownReduction": 0.02, "MoveSpeed": 0.06}),
        Node("VacuumBubble", None, (5, 3), kind="Spell", spell="VacuumBubble", pre=["Tailwind", "Thermals"]),
        Node("Shear", "shear", (0, 4), kind="Augment", ranks=2, pre=["AirLance"], mods=[spellmod(["AirLance"], damage=0.2, armorPen=0.1)]),
        Node("Snatch", None, (2, 4), kind="Spell", spell="Snatch", pre=["AirLance", "WindRider"]),
        Node("WindWall", None, (4, 4), kind="Spell", spell="WindWall", pre=["WindRider", "VacuumBubble"]),
        Node("Breathless", "breathless", (6, 4), kind="Augment", ranks=2, pre=["VacuumBubble"], mods=[spellmod(["VacuumBubble"], duration=0.3, cooldown=-0.1)]),
        Node("SkyToss", None, (1, 5), kind="Spell", spell="SkyToss", pre=["Shear", "Snatch"]),
        Node("Featherweight", "featherweight", (3, 5), kind="Notable", ranks=2, pre=["Snatch", "WindWall"],
             passives=[P("P_DodgeBlink", chancePerRank=0.06, rangedOnly=True)], desc="Bullets sometimes find only the wind where you were standing."),
        Node("CleansingWind", None, (5, 5), kind="Spell", spell="CleansingWind", pre=["WindWall", "Breathless"]),
        Node("Jetstream", "jetstream", (0, 6), ranks=3, pre=["SkyToss"], stats={"AA_CritChance": 0.02, "AA_CritDamage": 0.05}),
        Node("Cyclone", None, (2, 6), kind="Spell", spell="Cyclone", pre=["SkyToss", "Featherweight"]),
        Node("HighPressure", "high pressure", (4, 6), kind="Notable", ranks=2, pre=["Featherweight", "CleansingWind"], mods=[tagmod("Wind", damage=0.08, radius=0.05)],
             desc="Every gust carries more weight."),
        Node("Doldrums", "doldrums", (6, 6), ranks=3, pre=["CleansingWind"], stats={"AA_WardMax": 8, "AA_WardRegen": 0.1}),
        Node("StormChaser", "storm chaser", (2, 7), kind="Keystone", excl="WindKeystone", pre=["Cyclone", "Jetstream"],
             mods=[spellmod(["Cyclone", "SkyToss"], cooldown=-0.3, radius=0.2)], stats={"MoveSpeed": 0.3},
             desc="Ride every storm. Cyclone and Sky Toss come back far sooner and grow larger, and you move much faster."),
        Node("EyeOfCalm", "eye of calm", (4, 7), kind="Keystone", excl="WindKeystone", pre=["HighPressure", "Doldrums"],
             passives=[P("P_DamageReduction", fractionPerRank=0.3, rangedOnly=True), P("P_Thorns", fraction=0.25, damageDef="AA_Gale", meleeOnly=False)],
             desc="You stand in the calm eye of your own storm. Ranged attacks lose a third of their force against you, and part of every blow "
                  "is blown back at the attacker."),
        Node("Tempestuous", "tempestuous", (3, 8), kind="Keystone", pre=["StormChaser", "EyeOfCalm"], pct={"AA_SpellPower": 0.15},
             passives=[P("P_TriggerSpell", trigger="OnSpellCast", spell=S("Gust"), chancePerRank=0.35, target="RandomEnemy", tag="Wind", cooldownTicks=60)],
             desc="The wind never stops. Your wind spells often loose a free Gust at a random enemy."),
    )

    # ------------------------------------------------------------------ WINDBLADE
    wb = ClassSpec("Windblade", "windblade", color="#b8ffe9", color2="#2fb38c", icon=("crescent", "gust"), parent=w, order=0, school=BLADE,
        tagline="Become the wind with an edge. Blink from enemy to enemy, cut a hundred times a second.",
        desc="Windblades learned that air moving fast enough is a sword, and that a sword moving fast enough is everywhere at once.",
        playstyle="A blinking duelist. Gale Step and Hundredfold Gale teleport you from enemy to enemy, cutting each one; Thousand Cuts shreds a "
                  "single target; Eye of the Storm turns half of all attacks aside and throws some back. Wants Celerity and Potency.",
        lore="'Where did she go?' was the last thing most of them said. The answer was always 'behind you'.",
        primary="CEL", secondary="POT", tags=["Wind"], height=8)
    wb.add_spells(
        Spell("GaleStep", "gale step", "Become the wind and blink through up to five enemies, appearing behind each one and cutting it. Wounds may keep bleeding.",
              icon=("crescent", "arrow"), mana=30, cd=12, range=22, warmup=0.3, tags=["Wind", "Mobility"], target="enemy", delivery="ChainDash",
              chainCount=4, chainRange=7.0, chainDelay=6, impact_vfx=[flash(1.6), burst("spark", 8, 3.0, 0.4)],
              effects=[dmg(22, "AA_Gale", armorPen=0.3), hediff("AA_Bleeding", 1.0, chance=0.4)]),
        Spell("ThousandCuts", "thousand cuts", "A storm of tiny blades descends on one enemy: twelve cuts in a heartbeat, each one hard to stop.",
              icon=("crescent", "burst"), mana=26, cd=10, range=20, warmup=0.4, tags=["Wind"], impact_vfx=[burst("spark", 20, 4.0, 0.4), flash(1.8)],
              effects=[dmg(36, "AA_Gale", hits=12, armorPen=0.5), hediff("AA_Bleeding", 1.5)]),
        Spell("RazorGale", "razor gale", "A fan of razor wind tears through everything in front of you, shoving enemies back and leaving them bleeding.",
              icon=("gust", "crescent"), mana=30, cd=14, range=7, warmup=0.4, tags=["Wind"], target="enemy_area", delivery="Cone", coneAngle=60.0,
              coneRange=7.0, ai_min=2, tier=2, impact_vfx=[flash(1.4)],
              effects=[dmg(20, "AA_Gale", armorPen=0.3), hediff("AA_Bleeding", 1.0), launch("AwayFromCaster", 3, 6, "AA_Gale", slam=2.0, stunTicks=20)]),
        missiles("Crosswind", "crosswind", "Loose eight wind blades that curve through the air and find their own targets, each one cutting and shoving.",
                 ("crescent", "swirl"), mana=28, cd=12, count=8, speed=22, projectile="WindBlade", scale=0.6, spread=160.0, tags=["Wind"], school=BLADE,
                 impact_vfx=GUST_HIT, effects=[dmg(9, "AA_Gale", armorPen=0.3), knock(1.0)]),
        Spell("EyeOfTheStorm", "eye of the storm", "Stand in the calm eye of a storm of blades for twenty seconds. Half of all attacks, ranged or melee, are turned "
              "aside and part of their force thrown back. You move and strike faster.", icon=("eye", "tornado"), mana=35, cd=45, warmup=0.3,
              tags=["Wind", "Ward"], role="Buff", target="self", tier=3, impact_vfx=[ring(0.2, 1.0, 24, byRadius=False), burst("spark", 16, 3.0, 0.4)],
              effects=[hediff("AA_EyeOfTheStorm", 1.0)]),
        Spell("HundredfoldGale", "hundredfold gale", "Dissolve into the wind and blink between up to fifteen enemies in a few seconds, cutting each one deep. "
              "You stand in the eye of the storm when it ends.", icon=("crescent", "tornado"), mana=200, cd=240, range=26, warmup=1.0, tags=["Wind"],
              target="enemy", delivery="ChainDash", chainCount=14, chainRange=10.0, chainDelay=5, ultimate=True, tier=5, xp=2.0, shake=1.5,
              caster_effects=[hediff("AA_EyeOfTheStorm", 1.0)], impact_vfx=[flash(2.4), burst("spark", 14, 4.0, 0.5)],
              effects=[dmg(48, "AA_Gale", armorPen=0.5), hediff("AA_Bleeding", 1.5), launch("Upward", 0, 8, "AA_Gale", stunTicks=30)]),
    )
    wb.add_nodes(
        Node("GaleStep", None, (2, 0), kind="Spell", spell="GaleStep"),
        Node("Keen", "keen", (4, 0), ranks=3, mods=[tagmod("Wind", critChance=0.03)]),
        Node("Afterimage", "afterimage", (1, 1), kind="Augment", ranks=2, pre=["GaleStep"], mods=[spellmod(["GaleStep"], extraTargets=1, cooldown=-0.1)],
             desc="One more blink per rank."),
        Node("ThousandCuts", None, (3, 1), kind="Spell", spell="ThousandCuts", pre=["GaleStep", "Keen"]),
        Node("Whetted", "whetted", (5, 1), ranks=3, pre=["Keen"], stats={"AA_CritDamage": 0.08}),
        Node("Flurry", "flurry", (0, 2), ranks=3, pre=["Afterimage"], stats={"AA_CastSpeed": 0.04}),
        Node("RazorGale", None, (2, 2), kind="Spell", spell="RazorGale", pre=["Afterimage", "ThousandCuts"]),
        Node("DeathByPaperCuts", "death by paper cuts", (4, 2), kind="Augment", ranks=2, pre=["ThousandCuts", "Whetted"],
             mods=[spellmod(["ThousandCuts"], damage=0.25)]),
        Node("Crosswind", None, (6, 2), kind="Spell", spell="Crosswind", pre=["Whetted"]),
        Node("Bladestorm", "bladestorm", (1, 3), kind="Notable", ranks=2, pre=["Flurry", "RazorGale"],
             passives=[P("P_OnHitHediff", hediff="AA_Bleeding", chancePerRank=0.2, severity=1.0, tag="Wind")], desc="Wind spells often leave deep, bleeding cuts."),
        Node("EyeOfTheStorm", None, (3, 3), kind="Spell", spell="EyeOfTheStorm", pre=["RazorGale", "DeathByPaperCuts"]),
        Node("Swarming", "swarming", (5, 3), kind="Augment", ranks=2, pre=["DeathByPaperCuts", "Crosswind"], mods=[spellmod(["Crosswind"], extraTargets=2, damage=0.1)],
             desc="Two more blades per rank."),
        Node("Untouchable", "untouchable", (2, 4), kind="Notable", ranks=2, pre=["Bladestorm", "EyeOfTheStorm"], passives=[P("P_DodgeBlink", chancePerRank=0.08)],
             desc="You are rarely where the blow lands."),
        Node("StillCenter", "still center", (4, 4), kind="Augment", ranks=2, pre=["EyeOfTheStorm", "Swarming"], mods=[spellmod(["EyeOfTheStorm"], duration=0.4, cooldown=-0.15)]),
        Node("Quicksilver", "quicksilver", (1, 5), ranks=3, pre=["Untouchable"], stats={"AA_CooldownReduction": 0.03}),
        Node("Edgewalker", "edgewalker", (3, 5), kind="Notable", ranks=2, pre=["Untouchable", "StillCenter"], passives=[P("P_CooldownOnKill", ticksPerRank=120, tag="Wind")],
             desc="Every kill brings your blades back sooner."),
        Node("Gale", "gale", (5, 5), ranks=3, pre=["StillCenter"], mods=[tagmod("Wind", damage=0.05)]),
        Node("WindDancer", "wind dancer", (2, 6), kind="Keystone", excl="WindbladeKeystone", pre=["Quicksilver", "Edgewalker"],
             mods=[spellmod(["GaleStep"], cooldown=-0.5, extraTargets=2)], stats={"MoveSpeed": 0.3},
             desc="You never stop moving. Gale Step returns twice as fast and blinks through two more enemies."),
        Node("HoneEdge", "honed edge", (4, 6), kind="Keystone", excl="WindbladeKeystone", pre=["Edgewalker", "Gale"],
             mods=[tagmod("Wind", critChance=0.15, armorPen=0.2)], desc="Every cut finds the gap in the armour. Huge critical chance and armour piercing."),
        Node("HundredfoldGale", None, (3, 7), kind="Ultimate", spell="HundredfoldGale", pre=["WindDancer", "HoneEdge"]),
    )

    # ------------------------------------------------------------------ PRESSURE SAGE
    ps = ClassSpec("PressureSage", "pressure sage", color="#a0f0ff", color2="#2a6a8a", icon=("pressure", "orb"), parent=w, order=1, school=SAGE,
        tagline="Weaponise the weight of the sky. Implosions, crushing pressure and the air stolen from an entire map.",
        desc="Pressure Sages understand that the atmosphere weighs a great deal, and that it can be persuaded to sit on people.",
        playstyle="Heavy control and the best answer to giant machines: Crushing Depth deals damage scaled by body size, Implosion sucks a group "
                  "together and blows it apart, Dead Calm suffocates an area, and Dead Air steals the breath of every enemy on the map. "
                  "Wants Potency and Focus.",
        lore="'Is it me, or is it hard to breathe in here?' It is her.", primary="POT", secondary="FOC", tags=["Wind"], height=8)
    ps.add_spells(
        Spell("CrushingDepth", "crushing depth", "Crush an enemy under the weight of the sky. Damage scales with body size: a centipede takes several times what a "
              "man does. It is slowed afterwards.", icon=("pressure", "skull"), mana=34, cd=14, range=26, warmup=0.7, tags=["Wind"],
              impact_vfx=[ring(1.0, 0.2, 20, byRadius=False), flash(2.0), burst("ring", 4, 0.5, 1.0)],
              effects=[dmg(30, "AA_Gale", bodySizeExponent=1.0, armorPen=0.6), hediff("AA_Slowed", 1.0, duration=300)], flavor="Heavier things fall harder."),
        Spell("EardrumBurst", "eardrum burst", "A sudden spike of pressure around you bursts eardrums: enemies close by are concussed and stunned.",
              icon=("pressure", "ring"), mana=30, cd=18, warmup=0.3, tags=["Wind"], role="Control", target="self", delivery="Nova", radius=4.0, ai_min=2,
              impact_vfx=[ring(0.1, 1.2, 20), ring(0.1, 0.9, 30), flash(2.5)],
              effects=[dmg(14, "AA_Gale"), hediff("AA_Concussed", 1.0), stun(90)]),
        Spell("Implosion", "implosion", "Collapse the air over an area: everything inside is sucked toward the centre, then the pressure snaps back and blasts it "
              "all outward.", icon=("pressure", "burst"), mana=40, cd=22, range=28, warmup=0.8, tags=["Wind"], target="enemy_area", radius=3.5, ai_min=2,
              tier=2, impact_vfx=[ring(1.2, 0.1, 30), glow(2.0, 0.5, 30)],
              effects=[launch("TowardImpact", 6, 8, "AA_Gale"),
                       sequence(step(45, [dmg(30, "AA_Gale"), launch("AwayFromImpact", 5, 15, "AA_Gale", slam=2.0)], radius=3.5,
                                     vfx=[flash(3.5), ring(0.1, 1.2, 26), burst("ring", 6, 1.0, 1.0)], shake=1.2, sound="AA_Gust", text="AA_Implode",
                                     textColor="#a0f0ff"))]),
        Spell("PressureLock", "pressure lock", "Pin an enemy in place under crushing pressure for six seconds; every blow against it lands harder. Machines "
              "cannot be held down for long, but they stagger.", icon=("pressure", "chain"), mana=28, cd=20, range=26, warmup=0.5, tags=["Wind"],
              role="Control", tier=2, impact_vfx=[ring(1.0, 0.3, 24, byRadius=False)], effects=[hediff("AA_PressureLocked", 1.0), stun(60), dmg(8, "AA_Gale")]),
        Spell("DeadCalm", "dead calm", "Still the air over an area for twelve seconds. There is nothing to breathe: every enemy inside suffocates.",
              icon=("orb", "ring"), mana=50, cd=40, range=28, warmup=1.0, tags=["Wind"], role="Control", target="enemy_area", ai_min=2, tier=3,
              impact_vfx=[ring(0.2, 1.0, 30), glow(2.5, 3.5, 40)],
              effects=[field(4.5, 720, 60, [hediff("AA_Suffocating", 0.5, stack=True, maxSeverity=3.0, affects="Enemies"), dmg(4, "AA_Gale", affects="Enemies")],
                             groundTex="ArcanumAscendant/Vfx/RuneCircle", groundSpin=0.0, groundAlpha=0.35)]),
        Spell("DeadAir", "dead air", "Steal the breath of every enemy on the map. They suffocate, are crushed by the weight of the sky (big ones hardest) and "
              "stagger for a moment.", icon=("pressure", "eclipse"), mana=220, cd=300, warmup=3.0, tags=["Wind"], target="self", delivery="Global",
              ultimate=True, tier=5, xp=2.0, shake=2.0, ai_min=4, cast_vfx=[ring(0.1, 2.0, 60, byRadius=False), screen("#a0f0ff", 0.3, 40, vignette=True)],
              impact_vfx=[ring(1.0, 0.2, 20, byRadius=False)],
              effects=[hediff("AA_Suffocating", 2.0, stack=True, maxSeverity=3.0), dmg(25, "AA_Gale", bodySizeExponent=0.8, armorPen=0.6), stun(60)]),
    )
    ps.add_nodes(
        Node("CrushingDepth", None, (2, 0), kind="Spell", spell="CrushingDepth"),
        Node("Barometer", "barometer", (4, 0), ranks=3, stats={"AA_ManaMax": 10, "AA_CritChance": 0.01}),
        Node("Abyssal", "abyssal", (1, 1), kind="Augment", ranks=2, pre=["CrushingDepth"], mods=[spellmod(["CrushingDepth"], damage=0.2, cooldown=-0.1)]),
        Node("EardrumBurst", None, (3, 1), kind="Spell", spell="EardrumBurst", pre=["CrushingDepth", "Barometer"]),
        Node("Density", "density", (5, 1), ranks=3, pre=["Barometer"], mods=[tagmod("Wind", armorPen=0.05)]),
        Node("Titanfall", "titanfall", (0, 2), kind="Notable", ranks=2, pre=["Abyssal"], passives=[P("P_DamageVs", vs="Mechanoid", bonusPerRank=0.12)],
             desc="The bigger and heavier the machine, the better the sky can crush it."),
        Node("Implosion", None, (2, 2), kind="Spell", spell="Implosion", pre=["Abyssal", "EardrumBurst"]),
        Node("Ringing", "ringing", (4, 2), kind="Augment", ranks=2, pre=["EardrumBurst", "Density"], mods=[spellmod(["EardrumBurst"], radius=0.2, duration=0.3)]),
        Node("PressureLock", None, (6, 2), kind="Spell", spell="PressureLock", pre=["Density"]),
        Node("Singularity", "singularity", (1, 3), kind="Augment", ranks=2, pre=["Implosion", "Titanfall"], mods=[spellmod(["Implosion"], radius=0.2, damage=0.15)]),
        Node("Compression", "compression", (3, 3), ranks=3, pre=["Implosion", "Ringing"], pct={"AA_SpellPower": 0.03}),
        Node("Vise", "vise", (5, 3), kind="Augment", ranks=2, pre=["Ringing", "PressureLock"], mods=[spellmod(["PressureLock"], duration=0.4, cooldown=-0.15)]),
        Node("DeadCalm", None, (2, 4), kind="Spell", spell="DeadCalm", pre=["Singularity", "Compression"]),
        Node("Asphyxia", "asphyxia", (4, 4), kind="Notable", ranks=2, pre=["Compression", "Vise"],
             passives=[P("P_OnHitHediff", hediff="AA_Suffocating", chancePerRank=0.12, severity=0.5, stack=True, maxSeverity=3.0, tag="Wind")],
             desc="Wind spells sometimes steal a breath along with everything else."),
        Node("Stillness", "stillness", (1, 5), kind="Augment", ranks=2, pre=["DeadCalm"], mods=[spellmod(["DeadCalm"], radius=0.25, duration=0.3)]),
        Node("Pressurised", "pressurised", (3, 5), ranks=3, pre=["DeadCalm", "Asphyxia"], stats={"AA_DurationFactor": 0.05, "AA_ManaRegen": 0.1}),
        Node("WeightOfTheSky", "weight of the sky", (5, 5), kind="Notable", pre=["Asphyxia"], mods=[spellmod(["CrushingDepth"], extraTargets=1)],
             desc="Crushing Depth falls on a second enemy near the first."),
        Node("Vacuum", "vacuum", (2, 6), kind="Keystone", excl="SageKeystone", pre=["Stillness", "Pressurised"], mods=[tagmod("Wind", duration=0.4)],
             passives=[P("P_ManaOnKill", amount=10.0)], desc="Your silences last far longer, and every breath you steal for good returns mana to you."),
        Node("Crushing", "crushing", (4, 6), kind="Keystone", excl="SageKeystone", pre=["Pressurised", "WeightOfTheSky"], mods=[tagmod("Wind", damage=0.25)],
             passives=[P("P_DamageVs", vs="Mechanoid", bonusPerRank=0.25)], desc="Everything you do weighs more. Much more damage, and far more against machines."),
        Node("DeadAir", None, (3, 7), kind="Ultimate", spell="DeadAir", pre=["Vacuum", "Crushing"]),
    )

    # ------------------------------------------------------------------ SKYWARDEN
    sw = ClassSpec("Skywarden", "skywarden", color="#e6fff7", color2="#6aa8ff", icon=("dome", "wing"), parent=w, order=2, school=WARDEN,
        tagline="Guardian of the open sky. Domes of wind that eat bullets, rescues through the air, and walls of gale.",
        desc="Skywardens stand between their friends and the guns, and the guns lose.",
        playstyle="The best protector against ranged armies. Aegis of Gales raises a dome that destroys hostile shots and throws their force back; "
                  "Gale Guard does the same for one ally; Rescue lifts an ally (even a downed one) through the air to your side. Heaven's "
                  "Bulwark covers a whole battle line. Wants Resilience and Celerity.",
        lore="The raiders brought four hundred rounds of ammunition. They took most of it home again, in themselves.",
        primary="RES", secondary="CEL", tags=["Wind", "Ward"], height=8)
    sw.add_spells(
        Spell("AegisOfGales", "aegis of gales", "Raise a spinning dome of wind for forty seconds. Hostile shots fired into it from outside are torn apart, and half "
              "their force is thrown back at the shooter.", icon=("dome", "gust"), mana=50, cd=60, range=20, warmup=0.8, tags=["Wind", "Ward"],
              role="Buff", target="cell", tier=2, impact_vfx=[ring(0.2, 1.2, 40, byRadius=False), burst("feather", 20, 3.0, 0.5)],
              effects=[spawn("AA_Construct_AegisPylon", lifetimeTicks=2400)]),
        Spell("Rescue", "rescue", "Lift an ally out of danger on a gust of wind and set them down beside you. Works on the fallen, too.", icon=("wing", "heart"),
              mana=16, cd=12, range=26, warmup=0.3, tags=["Wind", "Utility"], role="Utility", target="ally", affects="AlliesNotSelf", autocast=False,
              impact_vfx=[burst("feather", 10, 2.0, 0.5)], effects=[launch("TowardCaster", 30, 0, stunTicks=0, allowDowned=True)]),
        Spell("GaleGuard", "gale guard", "Wrap an ally in a guard of wind for twenty-five seconds: most bullets are turned aside and their force thrown back. Adds a "
              "ward.", icon=("shield", "gust"), mana=30, cd=30, range=24, warmup=0.4, tags=["Wind", "Ward"], role="Buff", target="ally",
              impact_vfx=[hexshield(2.0, 40), burst("feather", 10, 2.0, 0.5)], effects=[hediff("AA_GaleGuard", 1.0), ward(25)]),
        Spell("Slipstream", "slipstream", "Every ally around you rides your slipstream for thirty seconds: hastened, and a third of incoming shots blown aside.",
              icon=("wing", "swirl"), mana=40, cd=45, warmup=0.5, tags=["Wind"], role="Buff", target="self", delivery="Nova", radius=5.0, affects="Allies",
              tier=3, impact_vfx=[ring(0.2, 1.1, 30), burst("feather", 20, 3.0, 0.5, byRadius=True)],
              effects=[hediff("AA_Haste", 1.0, duration=1800), hediff("AA_Tailwind", 1.0)]),
        Spell("Skyfall", "skyfall", "Leap into the sky and come down like a hammer: everything where you land is crushed and blasted outward.",
              icon=("arrow", "burst"), mana=36, cd=20, range=20, warmup=0.3, tags=["Wind", "Mobility"], target="cell", tier=2, shake=1.2,
              impact_vfx=[burst("feather", 10, 2.0, 0.5)],
              effects=[launch("CasterToTarget", 0, 0, landing=[dmg(30, "AA_Gale"), launch("AwayFromImpact", 4, 12, "AA_Gale", slam=2.0)], landingRadius=3.0)]),
        Spell("HeavensBulwark", "heaven's bulwark", "The sky itself stands guard. Raise a vast dome of wind for a full minute that tears apart every hostile shot "
              "fired into it and throws the whole force back at the shooter. Allies near you gain a ward and a guard of wind.",
              icon=("dome", "star"), mana=200, cd=300, range=10, warmup=2.0, tags=["Wind", "Ward"], role="Buff", target="cell", radius=6.0, affects="Allies",
              ultimate=True, tier=5, xp=2.0, shake=1.0, cast_vfx=[pillar(3.0, 60)],
              impact_vfx=[ring(0.1, 1.4, 50), burst("feather", 40, 4.0, 0.6, byRadius=True), screen("#e6fff7", 0.25, 40)],
              effects=[spawn("AA_Construct_HeavensBulwark", lifetimeTicks=3600), ward(40), hediff("AA_GaleGuard", 1.0)]),
    )
    sw.add_nodes(
        Node("AegisOfGales", None, (2, 0), kind="Spell", spell="AegisOfGales"),
        Node("Steadfast", "steadfast", (4, 0), ranks=3, stats={"AA_WardMax": 10}),
        Node("Bulwark", "bulwark", (1, 1), kind="Augment", ranks=2, pre=["AegisOfGales"], mods=[spellmod(["AegisOfGales"], duration=0.3, cooldown=-0.15)]),
        Node("Rescue", None, (3, 1), kind="Spell", spell="Rescue", pre=["AegisOfGales", "Steadfast"]),
        Node("Windbreak", "windbreak", (5, 1), ranks=3, pre=["Steadfast"], stats={"ArmorRating_Sharp": 0.04, "ArmorRating_Blunt": 0.03}),
        Node("Sentinel", "sentinel", (0, 2), ranks=3, pre=["Bulwark"], stats={"AA_WardRegen": 0.15}),
        Node("GaleGuard", None, (2, 2), kind="Spell", spell="GaleGuard", pre=["Bulwark", "Rescue"]),
        Node("Lifeline", "lifeline", (4, 2), kind="Augment", ranks=2, pre=["Rescue", "Windbreak"], mods=[spellmod(["Rescue"], cooldown=-0.3, range=0.2)]),
        Node("Skyfall", None, (6, 2), kind="Spell", spell="Skyfall", pre=["Windbreak"]),
        Node("Shieldbearer", "shieldbearer", (1, 3), kind="Notable", ranks=2, pre=["Sentinel", "GaleGuard"], passives=[P("P_WardOnCast", amount=5.0, tag="Wind")],
             desc="Every wind spell you weave leaves you a little more warded."),
        Node("Slipstream", None, (3, 3), kind="Spell", spell="Slipstream", pre=["GaleGuard", "Lifeline"]),
        Node("Meteoric", "meteoric", (5, 3), kind="Augment", ranks=2, pre=["Lifeline", "Skyfall"], mods=[spellmod(["Skyfall"], damage=0.25, radius=0.15)]),
        Node("Guardian", "guardian", (2, 4), kind="Notable", ranks=2, pre=["Shieldbearer", "Slipstream"],
             passives=[P("P_Aura", radius=6.0, interval=240, affects="Allies", onlyInCombat=True, color=(0.9, 1.0, 0.97, 1.0), effects=[ward(6)])],
             desc="Allies near you are steadily warded while you fight."),
        Node("Updrafts", "updrafts", (4, 4), ranks=3, pre=["Slipstream", "Meteoric"], stats={"AA_CooldownReduction": 0.03}),
        Node("Unshaken", "unshaken", (1, 5), ranks=3, pre=["Guardian"], stats={"AA_WardMax": 10, "AA_HealingPower": 0.05}),
        Node("WatchfulSky", "watchful sky", (3, 5), kind="Notable", ranks=2, pre=["Guardian", "Updrafts"],
             passives=[P("P_DamageReduction", fractionPerRank=0.08, rangedOnly=True)], desc="The sky watches over you: ranged attacks lose some of their force."),
        Node("Highwind", "highwind", (5, 5), ranks=3, pre=["Updrafts"], mods=[tagmod("Wind", radius=0.05, duration=0.05)]),
        Node("Aegis", "aegis", (2, 6), kind="Keystone", excl="SkywardenKeystone", pre=["Unshaken", "WatchfulSky"],
             mods=[spellmod(["AegisOfGales", "GaleGuard"], cooldown=-0.4, duration=0.5)],
             desc="Your domes and guards come back far sooner and last half again as long."),
        Node("StormShield", "storm shield", (4, 6), kind="Keystone", excl="SkywardenKeystone", pre=["WatchfulSky", "Highwind"],
             passives=[P("P_Thorns", fraction=0.6, damageDef="AA_Gale", meleeOnly=False)], stats={"AA_WardMax": 30},
             desc="Whoever strikes you is struck back by the wind, hard. Much larger ward."),
        Node("HeavensBulwark", None, (3, 7), kind="Ultimate", spell="HeavensBulwark", pre=["Aegis", "StormShield"]),
    )
    return w, [wb, ps, sw], statuses()

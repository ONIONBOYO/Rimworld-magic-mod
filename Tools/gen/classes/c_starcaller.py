"""
The Starcaller. She hangs stars over the battlefield, and her spells are verbs on those stars: link them into a
constellation, pull one across the field, set them all off.

  Starcaller (base)  Stars. Kindle them, then: Constellation (lines of light between them, and a killing ground inside any
                     triangle), Shooting Star (move one, burning its path), Twinkle, Star's Grace and Stellar Collapse (every star
                     flares to blind, heal or explode), Starstep (walk to one).
  Constellar         Geometry. A great star, three at once, a pulsar that sweeps beams, walls of light along every line,
                     a volley from every star, and the Grand Conjunction that brings them all to one point.
  Moonsworn          The moon has four phases a quadrum and every Moonsworn spell changes with it. She can turn her own moon.
  Signbearer         Wears the constellations: five signs, one at a time, each a different way to fight. Reads horoscopes.
"""
from classes.common import *  # noqa: F401,F403
from classes import mythic_things as MT

INDIGO, STARLIGHT = "#7f8cff", "#fff3c4"
SILVER, MOONVIOLET = "#dfe6ff", "#8f7bff"
GOLD, DEEP = "#ffd76b", "#5a6cff"
ICEBLUE = "#9ad0ff"
ASTRAL = School("Astral", INDIGO, STARLIGHT, fleck="stardust", fleck2="star", projectile="ArcanumAscendant/Vfx/StarSpike")
CONST = School("Astral", ICEBLUE, "#ffffff", fleck="stardust", fleck2="star", projectile="ArcanumAscendant/Vfx/StarSpike")
LUNAR = School("Astral", SILVER, MOONVIOLET, fleck="stardust", fleck2="mote", projectile="ArcanumAscendant/Vfx/Crescent")
ZODIAC = School("Astral", GOLD, DEEP, fleck="stardust", fleck2="star", projectile="ArcanumAscendant/Vfx/StarSpike")
INDIGO_RGBA = rgb(INDIGO) + (1.0,)

STAR, POLARIS = "AA_Star", "AA_Polaris"
SF = "AA_Starfire"
SIGNS = ["AA_SignArcher", "AA_SignBull", "AA_SignScales", "AA_SignWaterBearer", "AA_SignLion"]
NATAL = ["AA_NatalSmith", "AA_NatalReaper", "AA_NatalScholar", "AA_NatalHunter", "AA_NatalLover", "AA_NatalHearth"]

KEYS = {
    "AA_Moon_0": "New moon",
    "AA_Moon_1": "Waxing moon",
    "AA_Moon_2": "Full moon",
    "AA_Moon_3": "Waning moon",
}


# ---- effect helpers (Classes_Starcaller.cs)

def kindle(thing=STAR, max_=4, lifetime=5 * MIN, triangle=0.0, **kw):
    return O("Effect_StarcallerKindle", thing=thing, max=max_, lifetimeTicks=int(lifetime), triangleRadius=float(triangle) if triangle else None, **kw)


def web(line, inside=None, linkRange=14.0, linger=0, wall=None, wallLife=480, **kw):
    return O("Effect_StarcallerWeb", linkRange=float(linkRange), lineEffects=line, insideEffects=inside, lingerTicks=int(linger) if linger else None,
             wallThing=wall, wallLifetimeTicks=int(wallLife) if wall else None, **kw)


def at_stars(effects, radius=3.0, consume=False, **kw):
    return O("Effect_StarcallerAtStars", effects=effects, starRadius=float(radius), consume=consume or None, **kw)


def shoot(path=None, arrive=None, arriveRadius=2.5, all_=False, bonus=0.5, **kw):
    return O("Effect_StarcallerShoot", all=all_ or None, pathEffects=path, arriveEffects=arrive, arriveRadius=float(arriveRadius),
             bonusPerStar=float(bonus) if all_ else None, **kw)


def starstep(arrive=None, radius=3.0):
    return O("Effect_StarcallerStep", arriveEffects=arrive, arriveRadius=float(radius))


def volley(effects, starRange=16.0):
    return O("Effect_StarcallerVolley", effects=effects, starRange=float(starRange))


def pulsar(effects, duration=600, beamRange=7.0, **kw):
    return O("Effect_StarcallerPulsar", effects=effects, durationTicks=int(duration), beamRange=float(beamRange), **kw)


def moon(always=None, new=None, waxing=None, full=None, waning=None, **kw):
    return O("Effect_StarcallerMoonPhase", always=always, newMoon=new, waxing=waxing, full=full, waning=waning, **kw)


def crescent(out, back=None, width=1.4):
    return O("Effect_StarcallerCrescent", outEffects=out, backEffects=back, width=float(width))


def sign(h, duration=MIN, keep=False):
    return O("Effect_StarcallerSign", hediff=h, group=SIGNS, durationTicks=int(duration), keepOthers=keep or None)


def periodic_bolts(interval, effects, rng=14.0, count=1, tex="ArcanumAscendant/Vfx/StarSpike", color=GOLD):
    return O("ArcanumAscendant.HediffCompProperties_PeriodicEffects", interval=int(interval), targetEffects=effects, targetRange=float(rng),
             targetCount=int(count), targetTex=tex, color=rgb(color) + (1.0,))


STAR_HIT = [flash(1.8), burst("stardust", 10, 2.4, 0.5), glow(1.0, 2.2, 18, useColor2=True)]
SOFT = [burst("stardust", 14, 1.4, 0.5), burst("star", 6, 1.0, 0.4), glow(1.4, 2.4, 30, useColor2=True)]


def sign_fx(name):
    return dict(emitter="Starlit", emitInterval=14, overlayTex="ArcanumAscendant/Vfx/Sign_" + name, overlaySize=1.7, overlayZ=1.25, overlayPulse=True)


def star_thing(defName, label, desc, *, size, great, pulse, radius, color, color2, light):
    return to_xml("ThingDef", O(None, defName=defName, label=label, description=desc, thingClass="Building", category="Building", tickerType="Normal",
        altitudeLayer="BuildingOnTop", drawerType="RealtimeOnly", selectable=True, useHitPoints=False, rotatable=False, neverMultiSelect=True,
        passability="Standable", pathCost=0, fillPercent=0.0, size=(1, 1),
        graphicData=O(None, texPath="ArcanumAscendant/Things/StarBase", graphicClass="Graphic_Single", shaderType="MoteGlow", drawSize=float(size) * 1.2),
        statBases={"Flammability": 0.0, "Beauty": 4},
        building=O(None, isEdifice=False, isInert=True, claimable=False, deconstructible=False, canPlaceOverWall=True),
        comps=[O("ArcanumAscendant.CompProperties_ArcaneConstruct", defaultLifetimeTicks=5 * MIN, glowColor=rgb(color) + (1.0,), glowSize=0.0,
                 pulseEffects=pulse, pulseInterval=60, pulseRadius=float(radius), pulseAffects="All"),
               O("ArcanumAscendant.CompProperties_ArcaneStar", size=float(size), great=great or None, color=rgb(color) + (1.0,), color2=rgb(color2) + (1.0,)),
               O("CompProperties_Glower", glowRadius=float(light), glowColor="(%d,%d,%d,0)" % tuple(int(c * 255) for c in rgb(color)))]), 1)


def statuses():
    s = []
    # ------------------------------------------------------------------ stars and what they do to those nearby
    s.append(status("AA_Starlit", "starlit", "Caught in the light of a Starcaller's star. Nothing lit this clearly can dodge well, and starfire finds it "
        "easily.", INDIGO, bad=True, ticks=150, stages=[stage(dmgf={SF: 1.25}, factors={"MeleeDodgeChance": 0.6})], fx=dict(emitter="Starlit", emitInterval=16)))
    s.append(status("AA_Dazzled", "dazzled", "Eyes full of stars. It can barely see what it is shooting at.", STARLIGHT, bad=True, ticks=360,
        stages=[stage(caps={"Sight": -0.5}, factors={"ShootingAccuracyPawn": 0.6, "MeleeHitChance": 0.75})], fx=dict(emitter="Starlit", emitInterval=10)))
    s.append(status("AA_Guided", "guided by Polaris", "Polaris shows the way: steadier aim, quicker shots.", ICEBLUE, bad=False, ticks=150,
        stages=[stage(factors={"ShootingAccuracyPawn": 1.15, "AimingDelayFactor": 0.85})]))
    s.append(status("AA_Nebulous", "lost in a nebula", "Wrapped in glowing dust: half blind and wading.", ICEBLUE, bad=True, ticks=120,
        stages=[stage(caps={"Sight": -0.4}, factors={"MoveSpeed": 0.65})], fx=dict(emitter="Starlit", emitInterval=12)))
    s.append(status("AA_Stargazing", "stargazing", "Near an orrery, a mage's mind wanders the right way: experience comes faster and mana flows back "
        "sooner.", INDIGO, bad=False, ticks=HOUR, stages=[stage(offsets={"AA_XpGain": 0.3, "AA_ManaRegen": 0.6, "MentalBreakThreshold": -0.03})]))
    s.append(thought_def("AA_FallingStars", "watched the stars fall", "The whole sky came down in silver lines. I have never seen anything like it.", 6, days=2.0))
    s.append(thought_def("AA_WishMade", "wished upon a star", "I made a wish on a star a Starcaller hung for me. I think it heard.", 4, days=3.0))

    # ------------------------------------------------------------------ the moon
    s.append(status("AA_MoonPhase", "moonsworn", "Sworn to the moon, whose gifts turn with its face. New moon: quick and deadly. Waxing: mana flows. "
        "Full: every spell and every mending is stronger. Waning: spells return sooner.", SILVER, bad=False, maxSev=4.0, initSev=1.0,
        stages=[stage(label="new moon", offsets={"AA_CritChance": 0.12}, factors={"MoveSpeed": 1.1}),
                stage(label="waxing moon", min_sev=1.5, offsets={"AA_ManaRegen": 1.2, "AA_ManaCostFactor": -0.1}),
                stage(label="full moon", min_sev=2.5, offsets={"AA_SpellPower": 0.15, "AA_HealingPower": 0.25}),
                stage(label="waning moon", min_sev=3.5, offsets={"AA_CooldownReduction": 0.12, "AA_CastSpeed": 0.1})],
        fx=dict(emitter="Moonlit", emitInterval=40)))
    s.append(status("AA_MoonTurned", "her own moon", "She has turned her own moon ahead of the sky's. Until it slips back, her magic follows her moon.",
        MOONVIOLET, bad=False, ticks=HOUR * 12, maxSev=4.0,
        stages=[stage(label="new"), stage(label="waxing", min_sev=1.5), stage(label="full", min_sev=2.5), stage(label="waning", min_sev=3.5)]))
    s.append(status("AA_NightWatch", "night watch", "Moon-touched for a day: tires half as fast and steps lighter, so the night shift is no hardship.",
        SILVER, bad=False, ticks=DAY, stages=[stage(factors={"RestFallRateFactor": 0.5, "MoveSpeed": 1.08})], fx=dict(emitter="Moonlit", emitInterval=60)))
    s.append(status("AA_Moonshade", "moonshade", "Hidden in the dark of the moon.", MOONVIOLET, bad=False, ticks=300,
        comps=[O("HediffCompProperties_Invisibility", visibleToPlayer=True)], stages=[stage(factors={"MoveSpeed": 1.15})]))

    # ------------------------------------------------------------------ signs
    s.append(status("AA_SignArcher", "sign of the Archer", "The Archer stands over her. Every two seconds a star-arrow flies at the nearest enemy within "
        "sixteen cells.", GOLD, bad=False, ticks=MIN, comps=[periodic_bolts(120, [dmg(14, SF, armorPen=0.3)], 16.0, 1)],
        stages=[stage(factors={"ShootingAccuracyPawn": 1.15})], fx=sign_fx("Archer")))
    s.append(status("AA_SignBull", "sign of the Bull", "The Bull stands over her: thick-hided and immovable. Every three seconds enemies within two and a "
        "half cells are gored and thrown back.", GOLD, bad=False, ticks=MIN,
        comps=[periodic(180, area=[knock(2.5, affects="Enemies"), dmg(10, SF, affects="Enemies")], radius=2.5, color=GOLD)],
        stages=[stage(offsets={"ArmorRating_Sharp": 0.35, "ArmorRating_Blunt": 0.35, "PainShockThreshold": 0.3}, factors={"IncomingDamageFactor": 0.85})],
        fx=sign_fx("Bull")))
    s.append(status("AA_SignScales", "sign of the Scales", "The Scales stand over her. What is done to her is weighed: she takes 15% less, and 40% of it "
        "goes back to whoever dealt it.", GOLD, bad=False, ticks=MIN, comps=[shell(0.85, reflect=0.4, color=GOLD, reflectDef=SF)], fx=sign_fx("Scales")))
    s.append(status("AA_SignWaterBearer", "sign of the Water-Bearer", "The Water-Bearer stands over her, pouring. Her own mana returns far faster, and every "
        "three seconds allied mages within six cells regain 6 mana.", GOLD, bad=False, ticks=MIN,
        comps=[periodic(180, area=[mana(6, affects="Allies")], radius=6.0, color=GOLD)], stages=[stage(offsets={"AA_ManaRegen": 2.5})],
        fx=sign_fx("WaterBearer")))
    s.append(status("AA_SignLion", "sign of the Lion", "The Lion stands over her. Every four seconds enemies within five cells may lose their nerve and "
        "run, and allies there take heart.", GOLD, bad=False, ticks=MIN,
        comps=[periodic(240, area=[hediff("AA_Cowed", 1.0, affects="Enemies", chance=0.35), hediff("AA_Lionheart", 1.0, affects="Allies")], radius=5.0,
                        color=GOLD)],
        stages=[stage(offsets={"MentalBreakThreshold": -0.15}, factors={"MeleeDamageFactor": 1.2})], fx=sign_fx("Lion")))
    s.append(status("AA_Cowed", "cowed", "Something very large roared, and it has decided to be somewhere else.", GOLD, bad=True, ticks=150,
        comps=[compel("Flee")]))
    s.append(status("AA_Lionheart", "lionheart", "Standing near the Lion: braver, and harder to put down.", GOLD, bad=False, ticks=300,
        stages=[stage(offsets={"MentalBreakThreshold": -0.1, "PainShockThreshold": 0.2})]))
    natal = [("AA_NatalSmith", "born under the Smith", "The Smith's children have sure hands.", {"GeneralLaborSpeed": 1.3, "ConstructionSpeed": 1.2}, {}),
             ("AA_NatalReaper", "born under the Reaper", "The Reaper's children know when a field is ready.", {"PlantWorkSpeed": 1.3},
              {"PlantHarvestYield": 0.1}),
             ("AA_NatalScholar", "born under the Scholar", "The Scholar's children cannot leave a question alone.", {"ResearchSpeed": 1.3},
              {"GlobalLearningFactor": 0.25}),
             ("AA_NatalHunter", "born under the Hunter", "The Hunter's children walk quietly and shoot straight.",
              {"ShootingAccuracyPawn": 1.15, "MoveSpeed": 1.08}, {"HuntingStealth": 0.2}),
             ("AA_NatalLover", "born under the Lover", "The Lover's children are easy to like and hard to refuse.", {},
              {"SocialImpact": 0.25, "NegotiationAbility": 0.2, "TradePriceImprovement": 0.05}),
             ("AA_NatalHearth", "born under the Hearth", "The Hearth's children mend, and are quick to be mended.", {"ImmunityGainSpeed": 1.25},
              {"MedicalTendQuality": 0.12, "MedicalSurgerySuccessChance": 0.1})]
    for dn, label, desc, factors, offsets in natal:
        s.append(status(dn, label, desc + " For two days after a Signbearer reads their stars, that gift is theirs.", GOLD, bad=False, ticks=2 * DAY,
                        stages=[stage(factors=factors, offsets=offsets)]))

    # ------------------------------------------------------------------ things
    s.append(star_thing(STAR, "star", "A star hung in the air by a Starcaller. It lights everything near it so that nothing there can dodge well and "
        "starfire finds it easily, and her spells act through it.", size=1.0, great=False, radius=4.0, color=INDIGO, color2=STARLIGHT, light=5,
        pulse=[hediff("AA_Starlit", 1.0, affects="Enemies")]))
    s.append(star_thing(POLARIS, "Polaris", "A great star. It counts as a star for every spell (twice over for flares and volleys), it does not burn "
        "out when new stars are kindled, and allies near it shoot straighter.", size=1.7, great=True, radius=6.0, color=ICEBLUE, color2="#ffffff", light=8,
        pulse=[hediff("AA_Starlit", 1.0, affects="Enemies"), hediff("AA_Guided", 1.0, affects="Allies")]))
    s.append(MT.construct("Construct_StarWall", "wall of heaven", "A sheet of hard starlight drawn between two stars. Nothing walks through it and "
        "nothing sees through it.", tex="StarWall", color=ICEBLUE, hp=260, lifetime=480, size=1.1, wall=True, glow=1.2))
    orrery = MT.construct("Construct_Orrery", "astral orrery", "Brass rings turning around a caught star, showing where everything in the sky is going. "
        "Mages within six cells gain experience 30% faster and recover mana sooner, it lights the room, and it is very beautiful.", tex="Orrery",
        color=INDIGO, hp=300, lifetime=3 * DAY, size=2.4, pulse=[hediff("AA_Stargazing", 1.0)], radius=6.0, interval=HOUR // 2, affects="Allies",
        glow=2.4, light=9)
    orrery = orrery.replace("<Beauty>0</Beauty>", "<Beauty>45</Beauty>")
    assert "<Beauty>45</Beauty>" in orrery
    s.append(orrery)
    return s


def build():
    sc = ClassSpec("Starcaller", "starcaller", color=INDIGO, color2=STARLIGHT, icon=("constellation", "star"), order=14, school=ASTRAL,
        tagline="Hang stars over the battlefield and draw the lines between them. Whatever stands inside your constellation is already dead.",
        desc="Starcallers read the sky and then rearrange it. Their stars are small, cold and exactly where they want them.",
        playstyle="Everything acts through your stars. Kindle up to four (Falling Star leaves one too). Each lights enemies near it so they cannot "
                  "dodge and starfire hurts them more. Constellation draws burning lines between stars and stuns everything inside a triangle; "
                  "Shooting Star drags one across the field; Twinkle, Star's Grace and Stellar Collapse make every star blind, heal or explode; Starstep "
                  "walks you to one. Stronger at night and under open sky. Wants Focus and Potency.",
        lore="'They are very far away,' she said, 'which is the only reason you are still alive.'",
        primary="FOC", secondary="POT", tags=["Astral"], starting=["StarDart"], innate={"AA_ManaMax": 10})
    sc.add_spells(
        Spell("StarDart", "star dart", "Throw a splinter of starlight. It hits half again as hard against anything lit by one of your stars.",
              icon=("star", "arrow"), mana=8, cd=1.5, range=28, warmup=0.4, tags=["Astral"], delivery="Projectile", projectileSpeed=30.0, projectileScale=0.8,
              impact_vfx=STAR_HIT, effects=[dmg(11, SF, bonusVsHediff="AA_Starlit", bonusVsHediffFactor=1.5)]),
        Spell("KindleStar", "kindle star", "Hang a star in the air for five minutes. It lights enemies within four cells (they dodge badly and take a "
              "quarter more from starfire) and your other spells act through it. You can keep four; a fifth burns out the oldest.",
              icon=("star", "hand"), mana=12, cd=5, range=30, warmup=0.3, tags=["Astral"], role="Utility", target="cell", autocast=False, hostile=False,
              sound_impact="AA_StarChime", impact_vfx=[glow(1.2, 2.4, 20, useColor2=True)], effects=[kindle()], flavor="There. And there."),
        Spell("Constellation", "constellation", "Draw lines of starlight between every pair of your stars within fourteen cells of each other. Enemies on "
              "a line take 26 starfire and are dazzled. Enemies inside a triangle of three linked stars also take 30 more and are stunned for two "
              "and a half seconds. Needs two stars; three make a killing ground.", icon=("constellation", "burst"), mana=30, cd=14, warmup=0.7,
              tags=["Astral"], target="self", autocast=False, shake=0.4, sound_cast="AA_StarChime", impact_vfx=[],
              effects=[web([dmg(26, SF, armorPen=0.3), hediff("AA_Dazzled", 1.0)], [dmg(30, SF, armorPen=0.3), stun(150)])],
              flavor="First you place the points. Then you connect them."),
        Spell("ShootingStar", "shooting star", "Your star nearest the target streaks there and stays. Everything along its path takes 22 starfire, and "
              "where it stops it bursts for 18 more and a short stun.", icon=("meteor", "star"), mana=18, cd=8, range=32, warmup=0.4, tags=["Astral"],
              target="cell", hostile=True, autocast=False, impact_vfx=STAR_HIT,
              effects=[shoot([dmg(22, SF, armorPen=0.25), hediff("AA_Starlit", 1.0)], [dmg(18, SF), stun(45)], 2.2)]),
        Spell("Twinkle", "twinkle", "Every one of your stars flares at once. Enemies within three and a half cells of any star are dazzled for six "
              "seconds and stunned for one and a half.", icon=("star", "eye"), mana=22, cd=18, warmup=0.4, tags=["Astral"], role="Control", target="self",
              autocast=False, impact_vfx=[flash(3.0), ring(0.2, 1.0, 20)],
              effects=[at_stars([hediff("AA_Dazzled", 1.0, affects="Enemies"), stun(90, affects="Enemies")], 3.5)]),
        Spell("StarsGrace", "star's grace", "Starlight is kind to its own. Allies within four cells of any of your stars heal 18 and regenerate for "
              "twenty seconds.", icon=("star", "heart"), mana=30, cd=30, warmup=0.6, tags=["Astral", "Healing"], role="Heal", target="self",
              affects="Allies", autocast=False, impact_vfx=SOFT + [ring(0.2, 1.0, 26)],
              effects=[at_stars([heal(18, affects="Allies"), hediff("AA_Regeneration", 1.0, affects="Allies")], 4.0)]),
        Spell("Starstep", "starstep", "Step through starlight to your star nearest the target point, however far. The star flares as you arrive and "
              "dazzles enemies within three cells.", icon=("star", "door"), mana=14, cd=12, range=45, warmup=0.2, tags=["Astral", "Mobility"],
              role="Mobility", target="cell", los=False, impact_vfx=[flash(2.4), burst("stardust", 16, 2.4, 0.5)],
              effects=[starstep([hediff("AA_Dazzled", 1.0, affects="Enemies")], 3.0)]),
        Spell("FallingStar", "falling star", "Call a star down out of the sky. It strikes the area for 60 starfire (less toward the edge), staggers "
              "everything there, and stays where it fell as a new star of yours.", icon=("meteor", "burst"), mana=40, cd=22, range=32, warmup=0.9,
              tags=["Astral"], target="enemy_area", delivery="Meteor", radius=2.4, projectileTex="ArcanumAscendant/Vfx/StarSpike", projectileScale=2.4,
              projectileSpeed=24.0, tier=2, ai_min=2, shake=0.8, sound_impact="AA_Impact_Astral",
              effects=[dmg(60, SF, armorPen=0.35, falloff=True), stun(60), kindle()]),
        Spell("StellarCollapse", "stellar collapse", "Every one of your stars dies at once, the way stars do. Each explodes for 70 starfire within three and a half "
              "cells (less toward the edge) and dazzles what is left. The stars are spent.", icon=("burst", "star"), mana=60, cd=40, warmup=1.0,
              tags=["Astral"], target="self", autocast=False, tier=3, shake=1.4, sound_cast="AA_StarChime",
              impact_vfx=[flash(5.0), ring(0.1, 1.3, 30), burst("stardust", 30, 5.0, 0.7, byRadius=True), pillar(1.4, 30)],
              effects=[at_stars([dmg(70, SF, armorPen=0.5, falloff=True, affects="Enemies"), hediff("AA_Dazzled", 1.0, affects="Enemies")], 3.5, consume=True)],
              flavor="They all end like this eventually. She just picks when."),
        Spell("NightOfFallingStars", "night of falling stars", "Fill the sky with falling stars for everyone to see. Every colonist on the map gains +6 "
              "mood for two days and a third of a bar of recreation.", icon=("meteor", "heart"), mana=60, cd=4000, warmup=3.0, tags=["Astral", "Utility"],
              role="Utility", target="self", delivery="Global", affects="Allies", quirky=True, cast_vfx=[pillar(1.6, 60), screen(INDIGO, 0.22, 60)],
              impact_vfx=[burst("stardust", 10, 1.4, 0.5)], effects=[memory("AA_FallingStars"), need("Joy", 0.33)],
              flavor="Everyone came out to watch. Even the prisoners were allowed a window."),
        Spell("WishUponAStar", "wish upon a star", "Hang a star for one colonist and let them wish on it. They gain +4 mood for three days, and the star "
              "grants one thing at random: an inspiration, 5000 experience in a skill, a full night's rest, or 60 healing.", icon=("star", "hand"),
              mana=40, cd=3000, range=12, warmup=2.5, tags=["Astral", "Utility"], role="Utility", target="ally", quirky=True, impact_vfx=SOFT,
              effects=[memory("AA_WishMade"), random_of([inspire(affects="Allies"), skillxp(5000), need("Rest", 1.0), heal(60)])],
              flavor="Don't tell me what you wished for."),
        Spell("AstralOrrery", "astral orrery", "Conjure an orrery of brass and starlight for three days. Mages within six cells of it gain experience "
              "30% faster and recover mana sooner; it lights the room and is beautiful enough to lift it a rank.", icon=("ring", "star"), mana=60,
              cd=3000, range=12, warmup=3.0, tags=["Astral", "Utility"], role="Utility", target="cell", quirky=True,
              impact_vfx=[ring(0.2, 1.0, 30), burst("stardust", 18, 1.6, 0.5), pillar(1.0, 40)],
              effects=[spawn("AA_Construct_Orrery", lifetimeTicks=3 * DAY)]),
        Spell("AstralSurvey", "astral survey", "Look down on the land the way the stars do. The map is revealed within thirty-five cells of the target, "
              "and what you learn of the heavens adds 200 research to the colony's current project.", icon=("eye", "star"), mana=45, cd=2000, range=60,
              warmup=2.5, tags=["Astral", "Utility"], role="Utility", target="cell", los=False, quirky=True,
              impact_vfx=[ring(0.2, 1.0, 40), burst("stardust", 20, 3.0, 0.5)], effects=[reveal(35), research(200)]),
    )
    sc.add_nodes(
        Node("StarGazer", "stargazer", (1, 0), ranks=3, mods=[tagmod("Astral", damage=0.05)]),
        Node("DeepSky", "deep sky", (3, 0), ranks=3, stats={"AA_ManaMax": 10, "AA_ManaRegen": 0.06}),
        Node("Twinkling", "twinkling", (5, 0), ranks=3, stats={"AA_CastSpeed": 0.03, "AA_CooldownReduction": 0.01}),
        Node("KindleStar", None, (1, 1), kind="Spell", spell="KindleStar", pre=["StarGazer"]),
        Node("Constellation", None, (3, 1), kind="Spell", spell="Constellation", pre=["DeepSky"]),
        Node("Starstep", None, (5, 1), kind="Spell", spell="Starstep", pre=["Twinkling"]),
        Node("SevenSisters", "seven sisters", (0, 2), kind="Augment", ranks=2, pre=["KindleStar"], mods=[spellmod(["KindleStar"], extraTargets=1, cooldown=-0.15)],
             desc="One more star at a time per rank, and Kindle Star comes back sooner."),
        Node("ShootingStar", None, (2, 2), kind="Spell", spell="ShootingStar", pre=["KindleStar", "Constellation"]),
        Node("Twinkle", None, (4, 2), kind="Spell", spell="Twinkle", pre=["Constellation", "Starstep"]),
        Node("WishUponAStar", None, (6, 2), kind="Spell", spell="WishUponAStar", pre=["Starstep"]),
        Node("Nightborn", "nightborn", (1, 3), kind="Notable", ranks=2, pre=["SevenSisters", "ShootingStar"],
             passives=[P("P_StarcallerNight", nightPerRank=0.07, openSkyPerRank=0.03)],
             desc="The sky is hers. +7% spell power per rank at night, and +3% per rank whenever she stands under open sky."),
        Node("StarsGrace", None, (3, 3), kind="Spell", spell="StarsGrace", pre=["ShootingStar", "Twinkle"]),
        Node("NightOfFallingStars", None, (5, 3), kind="Spell", spell="NightOfFallingStars", pre=["Twinkle", "WishUponAStar"]),
        Node("BrightLines", "bright lines", (0, 4), kind="Augment", ranks=2, pre=["Nightborn"], mods=[spellmod(["Constellation"], damage=0.2, cooldown=-0.1)],
             desc="Constellation hits harder and comes back sooner."),
        Node("FallingStar", None, (2, 4), kind="Spell", spell="FallingStar", pre=["Nightborn", "StarsGrace"]),
        Node("Stardust", "stardust", (4, 4), kind="Notable", ranks=2, pre=["StarsGrace", "NightOfFallingStars"],
             passives=[P("P_OnHitHediff", hediff="AA_Starlit", chancePerRank=0.5, severity=1.0, durationTicks=300, tag="Astral")],
             desc="Your starfire clings: Astral spells often leave their target starlit for five seconds even far from any star."),
        Node("AstralOrrery", None, (6, 4), kind="Spell", spell="AstralOrrery", pre=["NightOfFallingStars"]),
        Node("LongNight", "long night", (1, 5), ranks=3, pre=["BrightLines", "FallingStar"], stats={"AA_DurationFactor": 0.05}),
        Node("AstralSurvey", None, (3, 5), kind="Spell", spell="AstralSurvey", pre=["FallingStar", "Stardust"]),
        Node("Zenith", "zenith", (5, 5), ranks=3, pre=["Stardust", "AstralOrrery"], stats={"AA_CritChance": 0.02, "AA_CritDamage": 0.05}),
        Node("StellarCollapse", None, (2, 6), kind="Spell", spell="StellarCollapse", pre=["LongNight", "AstralSurvey"]),
        Node("ColdLight", "cold light", (4, 6), kind="Notable", ranks=2, pre=["AstralSurvey", "Zenith"], mods=[tagmod("Astral", damage=0.08, critChance=0.03)]),
        Node("FarTraveller", "far traveller", (6, 6), ranks=3, pre=["Zenith"], pct={"AA_SpellPower": 0.03}),
        Node("Firmament", "firmament", (2, 7), kind="Keystone", excl="StarcallerKeystone", pre=["StellarCollapse"],
             mods=[spellmod(["KindleStar", "FallingStar"], extraTargets=2), spellmod(["KindleStar"], duration=1.0)],
             desc="A sky of your own: two more stars at a time, and kindled stars last twice as long."),
        Node("DyingLight", "dying light", (4, 7), kind="Keystone", excl="StarcallerKeystone", pre=["ColdLight", "FarTraveller"],
             mods=[spellmod(["StellarCollapse", "ShootingStar"], cooldown=-0.35), tagmod("Astral", damage=0.15)],
             desc="Stars burn brightest at the end. Stellar Collapse and Shooting Star come back far sooner and all starfire hits harder."),
        Node("MusicOfTheSpheres", "music of the spheres", (3, 8), kind="Keystone", pre=["Firmament", "DyingLight"], pct={"AA_SpellPower": 0.15},
             passives=[P("P_TriggerSpell", trigger="Periodic", spell=S("KindleStar"), chancePerRank=1.0, target="RandomEnemy", interval=480,
                         searchRadius=24.0)],
             desc="The sky arranges itself for you: in battle, every eight seconds a star kindles by itself beside a nearby enemy."),
    )

    # ------------------------------------------------------------------ CONSTELLAR
    co = ClassSpec("Constellar", "constellar", color=ICEBLUE, color2="#ffffff", icon=("constellation", "ring"), parent=sc, order=0, school=CONST,
        tagline="Geometry is a weapon. More stars, a great star, walls of light along every line, and a pulsar that sweeps the field.",
        desc="A Constellar thinks in angles. Give her three points and she will give you a place nobody can stand.",
        playstyle="The star architect. Summer Triangle drops three stars at once, Polaris is a great star that never burns out, Pulsar turns a star "
                  "into two sweeping beams, Wall of Heaven builds hard light along every line of your constellation, Convergence fires a lance "
                  "from every star at one target, and the Grand Conjunction brings them all to one point. Wants Focus and Celerity.",
        lore="She drew the battle on a napkin beforehand. The raiders were kind enough to stand on the dots.",
        primary="FOC", secondary="CEL", tags=["Astral"], height=8)
    co.add_spells(
        Spell("Polaris", "polaris", "Kindle Polaris, a great star, for ten minutes. It counts as a star for every spell (flaring half again as strong "
              "and firing two lances), never burns out to make room, lights enemies within six cells and steadies the aim of allies there. Only one "
              "at a time.", icon=("star", "crown"), mana=40, cd=60, range=30, warmup=0.8, tags=["Astral"], role="Utility", target="cell", autocast=False,
              hostile=False, sound_impact="AA_StarChime", impact_vfx=[pillar(1.2, 40), glow(1.6, 3.0, 30, useColor2=True)],
              effects=[kindle(POLARIS, lifetime=10 * MIN)]),
        Spell("SummerTriangle", "summer triangle", "Three stars at once, in a triangle four cells wide around the target: a constellation waiting to be "
              "drawn.", icon=("constellation", "hand"), mana=34, cd=30, range=30, warmup=0.6, tags=["Astral"], role="Utility", target="cell",
              autocast=False, hostile=False, sound_impact="AA_StarChime", impact_vfx=[ring(0.2, 1.0, 24)], effects=[kindle(triangle=4.0)]),
        Spell("Pulsar", "pulsar", "Your star nearest the target becomes a pulsar for ten seconds: two beams sweep around it out to seven cells, and "
              "every enemy a beam passes over takes 20 starfire and is left starlit.", icon=("star", "swirl"), mana=36, cd=28, range=32, warmup=0.6,
              tags=["Astral"], target="cell", hostile=True, autocast=False, tier=2, impact_vfx=[flash(2.4)],
              effects=[pulsar([dmg(20, SF, armorPen=0.35), hediff("AA_Starlit", 1.0, duration=300)], 600, 7.0)]),
        Spell("WallOfHeaven", "wall of heaven", "Draw your constellation in hard light. Every line between stars within fourteen cells becomes a wall for "
              "eight seconds: nothing walks or sees through it. Enemies caught on a line take 12 starfire. Cut a raid in half, or shut a door.",
              icon=("wall", "star"), mana=40, cd=35, warmup=0.7, tags=["Astral"], role="Control", target="self", autocast=False, tier=2,
              sound_cast="AA_StarChime", impact_vfx=[], effects=[web([dmg(12, SF)], wall="AA_Construct_StarWall", wallLife=480)]),
        Spell("Convergence", "convergence", "Every star within sixteen cells of the target fires a lance of light at it: 24 starfire each, piercing armour. "
              "Six stars around a boss is a very bad place to be a boss.", icon=("arrow", "constellation"), mana=34, cd=14, range=32, warmup=0.6,
              tags=["Astral"], target="enemy", tier=2, autocast=False, impact_vfx=STAR_HIT, effects=[volley([dmg(24, SF, armorPen=0.6)], 16.0)]),
        Spell("Nebula", "nebula", "Every one of your stars breathes out a cloud of glowing dust three cells wide for twelve seconds. Enemies inside are "
              "half blind and slowed by a third.", icon=("cloud", "star"), mana=30, cd=30, warmup=0.6, tags=["Astral"], role="Control", target="self",
              autocast=False, impact_vfx=[burst("stardust", 20, 1.6, 0.6, byRadius=True)],
              effects=[at_stars([field(3.0, 720, 60, [hediff("AA_Nebulous", 1.0, affects="Enemies")], groundTex="ArcanumAscendant/Vfx/Nebula",
                                       groundSpin=3.0, groundAlpha=0.5, ambientFleck="AA_Fleck_Stardust", ambientDensity=3.0, ambientSpeed=0.5)], 3.0)]),
        Spell("StarMetal", "star metal", "Let your stars fall the rest of the way. Every star you have burns out and leaves behind 3 to 5 plasteel: "
              "what is left when a small star cools.", icon=("star", "anvil"), mana=50, cd=3000, warmup=3.0, tags=["Astral", "Utility"], role="Utility",
              target="self", quirky=True, impact_vfx=[flash(2.0), burst("stardust", 14, 2.0, 0.5), smoke(2, 0.8)],
              effects=[at_stars([conjure("Plasteel", 3, 5)], 0.5, consume=True)], flavor="Still warm."),
        Spell("GrandConjunction", "grand conjunction", "Every star you have streaks to one point, burning its path (40 starfire). Where they meet they "
              "become one in a blast of 90 starfire within five cells, half again as strong for each star beyond the first, and a four-second stun. "
              "One star remains.", icon=("constellation", "burst"), mana=220, cd=300, range=36, warmup=2.5, tags=["Astral"], target="cell", hostile=True,
              ultimate=True, tier=5, xp=2.0, shake=2.0, autocast=False, cast_vfx=[pillar(2.6, 70), screen(ICEBLUE, 0.3, 50)],
              impact_vfx=[flash(8.0), ring(0.1, 1.4, 44), burst("stardust", 60, 6.0, 0.8, byRadius=True), pillar(2.4, 50), screen("#ffffff", 0.5, 20)],
              effects=[shoot([dmg(40, SF, armorPen=0.4)], [dmg(90, SF, armorPen=0.6, falloff=True), stun(240)], 5.0, all_=True, bonus=0.5)]),
    )
    co.add_nodes(
        Node("SummerTriangle", None, (2, 0), kind="Spell", spell="SummerTriangle"),
        Node("Geometer", "geometer", (4, 0), ranks=3, mods=[tagmod("Astral", radius=0.05)]),
        Node("Acute", "acute", (1, 1), kind="Augment", ranks=2, pre=["SummerTriangle"], mods=[spellmod(["SummerTriangle"], cooldown=-0.2)],
             desc="Summer Triangle comes back 20% sooner per rank."),
        Node("Polaris", None, (3, 1), kind="Spell", spell="Polaris", pre=["SummerTriangle", "Geometer"]),
        Node("Sextant", "sextant", (5, 1), ranks=3, pre=["Geometer"], stats={"AA_ManaRegen": 0.1}),
        Node("Compass", "compass", (0, 2), ranks=3, pre=["Acute"], stats={"AA_DurationFactor": 0.05}),
        Node("Pulsar", None, (2, 2), kind="Spell", spell="Pulsar", pre=["Acute", "Polaris"]),
        Node("Convergence", None, (4, 2), kind="Spell", spell="Convergence", pre=["Polaris", "Sextant"]),
        Node("StarMetal", None, (6, 2), kind="Spell", spell="StarMetal", pre=["Sextant"]),
        Node("FixedStars", "fixed stars", (1, 3), kind="Notable", pre=["Compass", "Pulsar"],
             passives=[P("P_OnHitHediff", hediff="AA_Dazzled", chancePerRank=0.25, severity=1.0, tag="Astral", cooldownTicks=120)],
             desc="Your lines burn longer in the eye: starfire has a one-in-four chance to dazzle whatever it hits."),
        Node("WallOfHeaven", None, (3, 3), kind="Spell", spell="WallOfHeaven", pre=["Pulsar", "Convergence"]),
        Node("Parallax", "parallax", (5, 3), kind="Augment", ranks=2, pre=["Convergence", "StarMetal"], mods=[spellmod(["Convergence"], damage=0.2, cooldown=-0.1)],
             desc="Convergence hits harder and comes back sooner."),
        Node("Lighthouse", "lighthouse", (2, 4), kind="Augment", ranks=2, pre=["FixedStars", "WallOfHeaven"],
             mods=[spellmod(["Pulsar"], duration=0.3, radius=0.15)], desc="Pulsars burn longer and reach farther."),
        Node("Nebula", None, (4, 4), kind="Spell", spell="Nebula", pre=["WallOfHeaven", "Parallax"]),
        Node("Meridian", "meridian", (1, 5), ranks=3, pre=["Lighthouse"], stats={"AA_CritChance": 0.02}),
        Node("Architect", "architect of heaven", (3, 5), ranks=3, pre=["Lighthouse", "Nebula"], pct={"AA_SpellPower": 0.03}),
        Node("Dust", "dust to dust", (5, 5), kind="Augment", pre=["Nebula"], mods=[spellmod(["Nebula", "WallOfHeaven"], duration=0.4)],
             desc="Nebulae and Walls of Heaven last 40% longer."),
        Node("Zodiacal", "great square", (2, 6), kind="Keystone", excl="ConstellarKeystone", pre=["Meridian", "Architect"],
             mods=[spellmod(["KindleStar", "SummerTriangle", "FallingStar"], extraTargets=2), spellmod(["Constellation"], radius=0.3)],
             desc="A bigger sky: two more stars at a time, and Constellation links stars 30% farther apart."),
        Node("Occultation", "occultation", (4, 6), kind="Keystone", excl="ConstellarKeystone", pre=["Architect", "Dust"],
             mods=[spellmod(["Pulsar", "Convergence"], cooldown=-0.4), tagmod("Astral", armorPen=0.2)],
             desc="Sharper light. Pulsar and Convergence come back much sooner and all starfire bites deeper into armour."),
        Node("GrandConjunction", None, (3, 7), kind="Ultimate", spell="GrandConjunction", pre=["Zodiacal", "Occultation"]),
    )

    # ------------------------------------------------------------------ MOONSWORN
    mo = ClassSpec("Moonsworn", "moonsworn", color=SILVER, color2=MOONVIOLET, icon=("crescent", "star"), parent=sc, order=1, school=LUNAR,
        tagline="The moon has four faces and so does every one of her spells. Learn the calendar, or turn the moon yourself.",
        desc="The Moonsworn made a promise to the moon, which keeps its promises on a schedule.",
        playstyle="The moon runs through four phases every quadrum (new, waxing, full, waning: about four days each) and every Moonsworn spell does "
                  "something different under each. New moon is stealth and silence, waxing feeds you, the full moon is raw power and healing, and "
                  "waning slows and weakens. Take the Moon Oath to wear the phase's own gifts, and Turn the Moon to run a phase ahead of the sky "
                  "for half a day. Wants Focus and Attunement.",
        lore="'Come back in four days,' she told the raid's envoy. He thought it was a negotiation. It was a forecast.",
        primary="FOC", secondary="ATT", tags=["Astral", "Moon"], height=8)
    mo.add_spells(
        Spell("Moonbeam", "moonbeam", "Hold a beam of moonlight on a target for four seconds, striking about three times a second. New moon: it "
              "also silences. Waxing: you are healed for 40% of the damage. Full: it hits half again as hard and dazzles. Waning: it slows to a "
              "crawl.", icon=("crescent", "arrow"), mana=34, cd=16, range=22, warmup=0.5, tags=["Astral", "Moon"], delivery="Channel", channelTicks=240,
              channelInterval=18, beamWidth=0.8, impact_vfx=[burst("stardust", 4, 1.6, 0.4)],
              effects=[moon(new=[dmg(9, SF, armorPen=0.4), hediff("AA_Silenced", 1.0, duration=180)],
                            waxing=[dmg(9, SF, armorPen=0.4, healCasterFraction=0.4)],
                            full=[dmg(14, SF, armorPen=0.4), hediff("AA_Dazzled", 1.0)],
                            waning=[dmg(9, SF, armorPen=0.4), hediff("AA_Slowed", 1.0, duration=240)])]),
        Spell("LunarTide", "lunar tide", "The moon pulls on everything. Under a waxing or full moon the tide comes in: enemies within six cells are "
              "thrown seven cells away from you. Under a waning or new moon it goes out: they are dragged to your feet and stunned. Either way they "
              "take 14 damage.", icon=("wave", "crescent"), mana=32, cd=20, warmup=0.5, tags=["Astral", "Moon"], role="Control", target="self",
              delivery="Nova", radius=6.0, ai_min=2, shake=0.6, impact_vfx=[ring(0.1, 1.2, 30), burst("stardust", 24, 4.0, 0.6, byRadius=True)],
              effects=[moon(always=[dmg(14, SF)], new=[launch("TowardCaster", 8, 6, SF, stunTicks=90)], waxing=[launch("AwayFromCaster", 7, 12, SF, slam=2.0)],
                            full=[launch("AwayFromCaster", 7, 12, SF, slam=2.0)], waning=[launch("TowardCaster", 8, 6, SF, stunTicks=90)])]),
        Spell("Crescent", "crescent", "Throw a crescent of moonlight out to the target point. It cuts everything in its path for 24, then comes back to "
              "your hand and cuts them again for 30.", icon=("crescent", "sword"), mana=24, cd=9, range=20, warmup=0.4, tags=["Astral", "Moon"],
              target="cell", hostile=True, impact_vfx=[], effects=[crescent([dmg(24, SF, armorPen=0.3)], [dmg(30, SF, armorPen=0.3)], 1.4)],
              flavor="It always comes back."),
        Spell("TurnTheMoon", "turn the moon", "Turn your own moon one phase onward for twelve hours. Your Moonsworn spells and your Moon Oath follow "
              "your moon, not the sky's, until it slips back. Cast it again to turn it further.", icon=("crescent", "clock"), mana=30, cd=250,
              warmup=1.0, tags=["Astral", "Moon"], role="Buff", target="self", quirky=True, impact_vfx=SOFT,
              effects=[O("Effect_StarcallerTurnMoon", hediff="AA_MoonTurned", durationTicks=HOUR * 12)], flavor="It is only a matter of perspective."),
        Spell("SilverLight", "silver light", "Bathe an ally in moonlight: 30 healing. New moon: they also vanish from sight for five seconds. Waxing: "
              "they regenerate for twenty seconds. Full: twice the healing and poisons are lifted. Waning: a ward of 25 as well.",
              icon=("crescent", "heart"), mana=30, cd=20, range=22, warmup=0.6, tags=["Astral", "Moon", "Healing"], role="Heal", target="ally", ai_hp=0.6,
              impact_vfx=SOFT + [pillar(0.8, 30)],
              effects=[moon(always=[heal(30)], new=[hediff("AA_Moonshade", 1.0)], waxing=[hediff("AA_Regeneration", 1.0)], full=[heal(30), cleanse()],
                            waning=[ward(25)])]),
        Spell("NightWatch", "night watch", "Touch the colonists around a point with moonlight. For a day they tire half as fast and move 8% quicker: "
              "a night shift that costs nobody their sleep.", icon=("crescent", "eye"), mana=45, cd=2000, range=24, warmup=2.0, tags=["Astral", "Moon"],
              role="Utility", target="ally_area", radius=4.0, quirky=True, impact_vfx=[ring(0.2, 1.0, 30)] + SOFT, effects=[hediff("AA_NightWatch", 1.0)]),
        Spell("BlueMoon", "once in a blue moon", "Ask the moon for a favour it grants very rarely. Something good arrives: resource pods, a wanderer, a "
              "passing thrumbo herd, ambrosia, a ship chunk or an orbital trader, whichever the world can manage.", icon=("crescent", "dice"), mana=90,
              cd=15000, warmup=5.0, tags=["Astral", "Moon"], role="Utility", target="self", quirky=True,
              cast_vfx=[pillar(1.8, 80), screen("#6f8cff", 0.25, 60)], impact_vfx=SOFT,
              effects=[incident(["ResourcePodCrash", "WandererJoin", "ThrumboPasses", "AmbrosiaSprout", "ShipChunkDrop", "OrbitalTraderArrival"])],
              flavor="The moon owed her one."),
        Spell("Moonfall", "moonfall", "Ask the moon to come closer. After a long breath it does: 120 starfire within seven cells (less toward the edge) "
              "and a five-second stun. Under a full moon it hits half again as hard; under a new moon it also blinds for ten seconds.",
              icon=("crescent", "burst"), mana=220, cd=300, range=34, warmup=3.0, tags=["Astral", "Moon"], target="enemy_area", delivery="Meteor",
              radius=7.0, projectileTex="ArcanumAscendant/Vfx/Moon", projectileScale=9.0, projectileSpeed=9.0, ultimate=True, tier=5, xp=2.0, shake=2.5,
              ai_min=4, cast_vfx=[pillar(2.6, 70), screen("#3a3a8a", 0.4, 90, vignette=True)],
              impact_vfx=[flash(9.0), ring(0.1, 1.3, 50), burst("stardust", 60, 7.0, 0.8, byRadius=True), smoke(10, 2.0), screen("#ffffff", 0.5, 24)],
              effects=[moon(always=[stun(300)], new=[dmg(120, SF, armorPen=0.5, falloff=True, damageBuildings=True), hediff("AA_Blinded", 1.0, duration=600)],
                            waxing=[dmg(120, SF, armorPen=0.5, falloff=True, damageBuildings=True)],
                            full=[dmg(180, SF, armorPen=0.5, falloff=True, damageBuildings=True)],
                            waning=[dmg(120, SF, armorPen=0.5, falloff=True, damageBuildings=True), hediff("AA_Slowed", 1.0)])]),
    )
    mo.add_nodes(
        Node("Crescent", None, (2, 0), kind="Spell", spell="Crescent"),
        Node("MoonOath", "moon oath", (4, 0), kind="Notable", passives=[P("P_StarcallerMoon", hediff="AA_MoonPhase")],
             desc="Swear to the moon and wear its phase. New moon: +12% crit chance, +10% speed. Waxing: +1.2 mana a second, spells cost 10% less. "
                  "Full: +15% spell power, +25% healing. Waning: +12% cooldown reduction, +10% cast speed."),
        Node("Keen", "keen edge", (1, 1), kind="Augment", ranks=2, pre=["Crescent"], mods=[spellmod(["Crescent"], damage=0.2, range=0.15)]),
        Node("Moonbeam", None, (3, 1), kind="Spell", spell="Moonbeam", pre=["Crescent", "MoonOath"]),
        Node("Tidal", "tidal", (5, 1), ranks=3, pre=["MoonOath"], stats={"AA_ManaMax": 12}),
        Node("Mare", "mare", (0, 2), ranks=3, pre=["Keen"], mods=[tagmod("Moon", damage=0.05)]),
        Node("LunarTide", None, (2, 2), kind="Spell", spell="LunarTide", pre=["Keen", "Moonbeam"]),
        Node("SilverLight", None, (4, 2), kind="Spell", spell="SilverLight", pre=["Moonbeam", "Tidal"]),
        Node("TurnTheMoon", None, (6, 2), kind="Spell", spell="TurnTheMoon", pre=["Tidal"]),
        Node("Undertow", "undertow", (1, 3), kind="Augment", ranks=2, pre=["Mare", "LunarTide"], mods=[spellmod(["LunarTide"], radius=0.2, cooldown=-0.15)],
             desc="Lunar Tide reaches farther and comes back sooner."),
        Node("Moonlit", "moonlit", (3, 3), kind="Notable", ranks=2, pre=["LunarTide", "SilverLight"], passives=[P("P_HealOnCast", amount=3.0, radius=4.0, tag="Moon")],
             desc="Every Moon spell sheds a little silver light on the allies around you."),
        Node("NightWatch", None, (5, 3), kind="Spell", spell="NightWatch", pre=["SilverLight", "TurnTheMoon"]),
        Node("Lunatic", "lunatic", (6, 3), kind="Augment", pre=["TurnTheMoon"], mods=[spellmod(["TurnTheMoon"], cooldown=-0.5, duration=0.5)],
             desc="Turn the Moon comes back twice as fast and holds half again as long."),
        Node("Selene", "selene's favour", (2, 4), ranks=3, pre=["Undertow", "Moonlit"], stats={"AA_HealingPower": 0.05, "AA_WardMax": 8}),
        Node("BlueMoon", None, (4, 4), kind="Spell", spell="BlueMoon", pre=["Moonlit", "NightWatch"]),
        Node("Gibbous", "gibbous", (1, 5), ranks=3, pre=["Selene"], stats={"AA_CritChance": 0.02}),
        Node("Perigee", "perigee", (3, 5), ranks=3, pre=["Selene", "BlueMoon"], pct={"AA_SpellPower": 0.03}),
        Node("Earthshine", "earthshine", (5, 5), kind="Augment", ranks=2, pre=["BlueMoon"], mods=[spellmod(["Moonbeam"], damage=0.2, duration=0.25)],
             desc="Moonbeam burns hotter and holds longer."),
        Node("Syzygy", "syzygy", (2, 6), kind="Keystone", excl="MoonswornKeystone", pre=["Gibbous", "Perigee"],
             mods=[tagmod("Moon", damage=0.2, cooldown=-0.15)], desc="Everything lines up. All Moon spells hit 20% harder and come back 15% sooner."),
        Node("DarkSide", "dark side", (4, 6), kind="Keystone", excl="MoonswornKeystone", pre=["Perigee", "Earthshine"],
             passives=[P("P_DodgeBlink", chancePerRank=0.12)], mods=[tagmod("Moon", critChance=0.1)],
             desc="There is a side of the moon nobody sees. One blow in eight finds only moonlight where you stood, and Moon spells crit far more often."),
        Node("Moonfall", None, (3, 7), kind="Ultimate", spell="Moonfall", pre=["Syzygy", "DarkSide"]),
    )

    # ------------------------------------------------------------------ SIGNBEARER
    sb = ClassSpec("Signbearer", "signbearer", color=GOLD, color2=DEEP, icon=("constellation", "crown"), parent=sc, order=2, school=ZODIAC,
        tagline="Wear the constellations. The Archer, the Bull, the Scales, the Water-Bearer and the Lion: one at a time, each a different fight.",
        desc="A Signbearer does not look up at the zodiac. It looks down at her.",
        playstyle="A stance fighter. Each Sign lasts a minute and only one can be worn: the Archer shoots star-arrows by itself, the Bull makes you a "
                  "wall that gores, the Scales send damage back, the Water-Bearer pours mana into your whole party and the Lion breaks enemy nerve. "
                  "Ascendant lends your sign to every ally near you, and the Wheel of Heaven wears all five. At home she reads horoscopes: every "
                  "colonist was born under one of six stars, and she can wake its gift. Wants Attunement and Resilience.",
        lore="'What's your sign?' is, from her, a tactical question.",
        primary="ATT", secondary="RES", tags=["Astral"], height=8)
    sb.add_spells(
        Spell("SignArcher", "sign of the archer", "Take the sign of the Archer for a minute. Every two seconds a star-arrow flies from you to the nearest "
              "enemy within sixteen cells for 14 starfire, and your own aim is 15% better. Replaces any other sign.", icon=("arrow", "constellation"),
              mana=28, cd=20, warmup=0.4, tags=["Astral"], role="Buff", target="self", impact_vfx=SOFT, effects=[sign("AA_SignArcher")]),
        Spell("SignBull", "sign of the bull", "Take the sign of the Bull for a minute: +35% armour, far more pain before you fall, 15% less damage "
              "taken, and every three seconds enemies within two and a half cells are gored for 10 and thrown back. Replaces any other sign.",
              icon=("horns", "constellation"), mana=28, cd=20, warmup=0.4, tags=["Astral"], role="Buff", target="self", impact_vfx=SOFT,
              effects=[sign("AA_SignBull")]),
        Spell("SignScales", "sign of the scales", "Take the sign of the Scales for a minute. You take 15% less damage, and 40% of every blow is dealt "
              "back to whoever struck it as starfire. Replaces any other sign.", icon=("coin", "constellation"), mana=28, cd=20, warmup=0.4,
              tags=["Astral"], role="Buff", target="self", impact_vfx=SOFT, effects=[sign("AA_SignScales")]),
        Spell("SignWaterBearer", "sign of the water-bearer", "Take the sign of the Water-Bearer for a minute. Your mana returns 2.5 a second faster, and "
              "every three seconds allied mages within six cells regain 6 mana. Replaces any other sign.", icon=("drop", "constellation"), mana=10,
              cd=20, warmup=0.4, tags=["Astral"], role="Buff", target="self", impact_vfx=SOFT, effects=[sign("AA_SignWaterBearer")]),
        Spell("SignLion", "sign of the lion", "Take the sign of the Lion for a minute: +20% melee damage and a steadier mind. Every four seconds each "
              "enemy within five cells has a one-in-three chance to lose its nerve and run, and allies there take heart. Replaces any other sign.",
              icon=("claw", "constellation"), mana=28, cd=20, warmup=0.4, tags=["Astral"], role="Buff", target="self", impact_vfx=SOFT,
              effects=[sign("AA_SignLion")]),
        Spell("Ascendant", "ascendant", "Your sign rises over everyone. Allies within eight cells take on whatever sign you are wearing for twenty "
              "seconds.", icon=("constellation", "wing"), mana=40, cd=45, warmup=0.6, tags=["Astral"], role="Buff", target="self", delivery="Nova",
              radius=8.0, affects="Allies", autocast=False, impact_vfx=[ring(0.1, 1.2, 34), burst("stardust", 30, 3.0, 0.6, byRadius=True)],
              effects=[O("Effect_StarcallerShareSign", group=SIGNS, durationTicks=1200)]),
        Spell("Horoscope", "horoscope", "Read a colonist's stars. Everyone was born under one of six signs (it depends on their birthday): the Smith "
              "(labour and construction), the Reaper (plant work and yield), the Scholar (research and learning), the Hunter (aim, stealth and speed), "
              "the Lover (social, negotiation and trade) or the Hearth (immunity, tending and surgery). For two days their sign's gift is awake.",
              icon=("scroll", "constellation"), mana=30, cd=500, range=12, warmup=2.0, tags=["Astral", "Utility"], role="Utility", target="ally", quirky=True,
              impact_vfx=SOFT, effects=[O("Effect_StarcallerHoroscope", signs=NATAL, durationTicks=2 * DAY)],
              flavor="You will meet a tall, dark stranger. He will have a charge rifle. Duck."),
        Spell("WheelOfHeaven", "wheel of heaven", "The whole zodiac turns over you. For thirty seconds you wear all five signs at once: the Archer's "
              "arrows, the Bull's hide, the Scales' justice, the Water-Bearer's flood and the Lion's roar.", icon=("constellation", "sun"), mana=220,
              cd=300, warmup=2.0, tags=["Astral"], role="Buff", target="self", ultimate=True, tier=5, xp=2.0, shake=1.0,
              cast_vfx=[pillar(2.6, 70), screen(GOLD, 0.3, 50)], impact_vfx=[ring(0.1, 1.3, 40), burst("stardust", 50, 4.0, 0.7), flash(5.0)],
              effects=[sign(h, 1800, keep=True) for h in SIGNS]),
    )
    sb.add_nodes(
        Node("SignArcher", None, (2, 0), kind="Spell", spell="SignArcher"),
        Node("Ecliptic", "ecliptic", (4, 0), ranks=3, stats={"AA_DurationFactor": 0.05}),
        Node("Fletcher", "fletcher", (1, 1), kind="Augment", ranks=2, pre=["SignArcher"], mods=[spellmod(["SignArcher"], damage=0.25, duration=0.2)],
             desc="The Archer's arrows hit harder and the sign lasts longer."),
        Node("SignBull", None, (3, 1), kind="Spell", spell="SignBull", pre=["SignArcher", "Ecliptic"]),
        Node("Horoscope", None, (5, 1), kind="Spell", spell="Horoscope", pre=["Ecliptic"]),
        Node("Houses", "twelve houses", (0, 2), ranks=3, pre=["Fletcher"], stats={"AA_ManaMax": 12}),
        Node("SignScales", None, (2, 2), kind="Spell", spell="SignScales", pre=["Fletcher", "SignBull"]),
        Node("SignWaterBearer", None, (4, 2), kind="Spell", spell="SignWaterBearer", pre=["SignBull", "Horoscope"]),
        Node("Almanac", "almanac", (6, 2), kind="Augment", ranks=2, pre=["Horoscope"], mods=[spellmod(["Horoscope"], cooldown=-0.3, duration=0.5)],
             desc="Horoscopes come back sooner and their gifts last half again as long per rank."),
        Node("Hide", "thick hide", (1, 3), ranks=3, pre=["Houses", "SignScales"], stats={"ArmorRating_Sharp": 0.04, "ArmorRating_Blunt": 0.04}),
        Node("SignLion", None, (3, 3), kind="Spell", spell="SignLion", pre=["SignScales", "SignWaterBearer"]),
        Node("Stargiver", "stargiver", (5, 3), kind="Notable", pre=["SignWaterBearer", "Almanac"],
             passives=[P("P_Aura", radius=9.0, interval=HOUR, affects="Allies", color=rgb(GOLD) + (1.0,), effects=[hediff("AA_Stargazing", 1.0)])],
             desc="Where she is, the stars are near. Mages within nine cells of her gain experience 30% faster and recover mana sooner, as if an "
                  "orrery stood beside them."),
        Node("Cusp", "on the cusp", (2, 4), kind="Notable", ranks=2, pre=["Hide", "SignLion"],
             mods=[spellmod(["SignArcher", "SignBull", "SignScales", "SignWaterBearer", "SignLion"], cooldown=-0.25)],
             desc="Change signs faster: every sign comes back 25% sooner per rank."),
        Node("Ascendant", None, (4, 4), kind="Spell", spell="Ascendant", pre=["SignLion", "Stargiver"]),
        Node("Fortune", "fortune", (1, 5), ranks=3, pre=["Cusp"], stats={"AA_CritChance": 0.02}),
        Node("Destiny", "destiny", (3, 5), ranks=3, pre=["Cusp", "Ascendant"], pct={"AA_SpellPower": 0.03}),
        Node("Rising", "rising sign", (5, 5), kind="Augment", pre=["Ascendant"], mods=[spellmod(["Ascendant"], duration=0.5, radius=0.25, cooldown=-0.2)],
             desc="Ascendant reaches farther, lasts half again as long and comes back sooner."),
        Node("FixedSign", "fixed sign", (2, 6), kind="Keystone", excl="SignbearerKeystone", pre=["Fortune", "Destiny"],
             mods=[spellmod(["SignArcher", "SignBull", "SignScales", "SignWaterBearer", "SignLion"], duration=1.0)],
             stats={"AA_WardMax": 20}, desc="Hold your sign. Every sign lasts twice as long, and you carry a stronger ward."),
        Node("MutableSign", "mutable sign", (4, 6), kind="Keystone", excl="SignbearerKeystone", pre=["Destiny", "Rising"],
             mods=[spellmod(["SignArcher", "SignBull", "SignScales", "SignWaterBearer", "SignLion"], cooldown=-0.5, manaCost=-0.3)],
             desc="Flow between signs. Every sign comes back twice as fast and costs 30% less."),
        Node("WheelOfHeaven", None, (3, 7), kind="Ultimate", spell="WheelOfHeaven", pre=["FixedSign", "MutableSign"]),
    )
    return sc, [co, mo, sb], statuses()

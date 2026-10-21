"""Colony expansion of the Druid: farms, soil, beasts, groves and shapes that matter at home as much as in the field."""
from classes.common import *  # noqa: F401,F403
from classes.prims_family import *  # noqa: F401,F403
from classes.prims_colony import *  # noqa: F401,F403
from classes import c_druid as M

KEYS = {
    "AA_DruidLongSleepWoke": "awake and whole",
}

HOUR_S = HOUR / SEC  # one in-game hour in seconds of game time at 1x
DAY_S = DAY / SEC

GREEN = (0.5, 0.88, 0.35, 1.0)
BEASTC = (0.85, 0.63, 0.31, 1.0)
GROVEC = (0.25, 0.85, 0.56, 1.0)


def return_to_earth(maxCorpses=8, heal=6.0, growth=0.2, **kw):
    """Corpses sink into the soil, enriching it (Colony_Druid.cs)."""
    return O("Effect_DruidReturnToEarth", maxCorpses=maxCorpses, healPerCorpse=float(heal), growth=float(growth), **kw)


def unleash_pack(callRadius=20.0, attackTicks=1500, animalEffects=None, requireRelease=True, **kw):
    """The colony's own trained animals charge the target (Colony_Druid.cs)."""
    return O("Effect_DruidUnleashPack", callRadius=float(callRadius), attackTicks=int(attackTicks), animalEffects=animalEffects,
             requireRelease=None if requireRelease else False, **kw)


# ------------------------------------------------------------------------------------------------ base tree

def _base(t, extra):
    d = t["Druid"]
    d.add_spells(
        Spell("GatherSimples", "gather simples",
              "Walk the wild margins in your mind and come back with an armful of healroot: 3 to 5 herbal medicine appear at the target spot. "
              "Once a day.", icon=("leaf", "cross"),
              mana=30, cd=DAY_S, range=12, warmup=4.0, tags=["Nature", "Healing", "Utility"], role="Utility", target="cell", quirky=True,
              impact_vfx=[burst("leaf", 14, 1.5, 0.6), burst("petal", 8, 1.0, 0.5), glow(1.2, 2.0, 30)],
              effects=[conjure("MedicineHerbal", 3, 5)]),
        Spell("HarvestHome", "harvest home",
              "Sing the harvest in. Every ripe crop in your growing zones within 7 cells is reaped at once with 25% more yield, and every bare "
              "cell of those zones is sown again with the zone's crop, already 15% grown. Bring the fields in before the cold snap. Once a day.",
              icon=("scythe", "leaf"), mana=45, cd=DAY_S, range=30, warmup=5.0, tags=["Nature", "Utility"], role="Utility", target="cell",
              radius=7.0, quirky=True,
              impact_vfx=[ring(0.1, 1.0, 60), burst("leaf", 30, 1.8, 0.6, byRadius=True), burst("petal", 16, 1.2, 0.5, byRadius=True)],
              effects=[harvest(1.25), sow(0.15)]),
        Spell("GreeningRite", "greening rite",
              "Walk barefoot in the soil with someone until something green wakes up in them. Their Plants passion is kindled (none becomes "
              "interested, interested becomes burning), they gain 2000 Plants experience and a good memory (+5 mood for 3 days). With Biotech, "
              "a child also gains 6 growth points and learning. Once every five days.",
              icon=("leaf", "hand"), mana=60, cd=DAY_S * 5, range=4, warmup=6.0, tags=["Nature", "Utility"], role="Buff", target="ally", quirky=True,
              impact_vfx=[pillar(1.0, 50), burst("leaf", 20, 1.2, 0.6), burst("petal", 10, 1.0, 0.5)],
              effects=[passion("Upgrade", skill="Plants"), skillxp(2000, "Plants"), child_growth(6.0, 0.5), memory("AA_DruidGreeningRite")]),
        Spell("ForestBreath", "breath of the forest",
              "Bring the smell of pine and rain indoors. Everyone within 6 cells of the target has their need for the outdoors filled completely "
              "(an end to cabin fever in mountain halls, prison cells and long winters) and feels refreshed (+6 mood for a day).",
              icon=("tree", "gust"), mana=30, cd=DAY_S / 2, range=24, warmup=3.0, tags=["Nature", "Utility"], role="Buff", target="ally_area",
              radius=6.0, quirky=True,
              impact_vfx=[ring(0.1, 1.0, 50), burst("leaf", 24, 2.0, 0.6, byRadius=True), glow(2.0, 4.0, 40)],
              effects=[need("Outdoors", 1.0), memory("AA_DruidForestBreath")]),
        Spell("FloweringArbor", "flowering arbor",
              "Grow a living arbor heavy with blossom for three days. It is a thing of great beauty (+40 beauty, enough to lift a whole room's "
              "impressiveness) and everyone within 4 cells of it regains 3% recreation every hour. Grow it in the dining hall before a "
              "wedding.", icon=("rose", "tree"),
              mana=50, cd=DAY_S * 3, range=20, warmup=5.0, tags=["Nature", "Utility"], role="Utility", target="cell", quirky=True,
              impact_vfx=[pillar(1.2, 50), burst("petal", 30, 1.6, 0.6), ring(0.2, 0.8, 30, byRadius=False)],
              effects=[spawn("AA_Construct_DruidArbor", lifetimeTicks=DAY * 3)]),
        Spell("ReturnToEarth", "return to the earth",
              "Give the dead back to the land. Up to 8 corpses within 5 cells sink into the ground; whatever they wore or carried stays on the "
              "surface. Natural soil within 1.5 cells of each becomes rich soil (wider for big animals), plants there grow 20%, and every ally "
              "within 4 cells of a corpse is healed for 6. Your own people's bodies are left for a proper grave; mechanoids have nothing to give.",
              icon=("bone", "flower"), mana=40, cd=DAY_S / 2, range=26, warmup=3.0, tags=["Nature", "Healing", "Utility"], role="Utility",
              target="cell", radius=5.0, quirky=True, affects="Corpses",
              impact_vfx=[rune(1.0, 80, tex="ArcanumAscendant/Vfx/Roots"), burst("leaf", 24, 1.5, 0.6, byRadius=True), smoke(3, 1.0)],
              effects=[return_to_earth(8, 6.0, 0.2)]),
    )
    d.add_nodes(
        Node("GatherSimples", None, (6, 1), kind="Spell", spell="GatherSimples", pre=["Greenward"]),
        Node("HarvestHome", None, (4, 3), kind="Spell", spell="HarvestHome", pre=["Bloom"]),
        Node("GreenThumb", "green thumb", (3, 4), kind="Notable", pre=["Oakheart", "HarvestHome"],
             passives=[P("P_Aura", radius=8.0, interval=HOUR, affects="Allies", color=GREEN, effects=[grow(0.012)])],
             desc="Things grow where you walk. Every hour, every plant within 8 cells of you grows 1.2% of the way to maturity "
                  "(a little more with healing power), drafted or not."),
        Node("GreeningRite", None, (5, 4), kind="Spell", spell="GreeningRite", pre=["FertileGround"]),
        Node("ForestBreath", None, (6, 5), kind="Spell", spell="ForestBreath", pre=["Wildheart"]),
        Node("FloweringArbor", None, (5, 6), kind="Spell", spell="FloweringArbor", pre=["SpiritBond"]),
        Node("ReturnToEarth", None, (1, 6), kind="Spell", spell="ReturnToEarth", pre=["Verdance"]),
    )
    extra.append(thought_def("AA_DruidGreeningRite", "greening rite",
                             "I walked barefoot in the soil with the druid, and something green woke up in me.", 5, days=3))
    extra.append(thought_def("AA_DruidForestBreath", "breath of the forest",
                             "The druid brought the smell of pine and rain inside. I can breathe again.", 6, days=1))
    arbor = things.totem("Construct_DruidArbor", "flowering arbor",
                         "A living arbor heavy with blossom, grown by a druid. It is beautiful to behold, and resting near it is a small joy.",
                         color="#ff9fd0", color2="#7fe05a", hp=300, lifetime=DAY * 3, shape="tree", radius=4.0, interval=HOUR,
                         affects="Allies", pulse=[need("Joy", 0.03)], light=4.0, glow=1.6)
    arbor = [x.replace("<Beauty>5</Beauty>", "<Beauty>40</Beauty>") for x in arbor]
    assert any("<Beauty>40</Beauty>" in x for x in arbor), "flowering arbor beauty not set"
    extra += arbor


# ------------------------------------------------------------------------------------------------ Beastmaster

def _beastmaster(t, extra):
    bm = t["Beastmaster"]
    bm.add_spells(
        Spell("BeastLore", "beast lore",
              "Teach a tame animal in its own tongue. It advances three steps of training at once (whatever its training settings allow next) "
              "and becomes bonded to you, with you as its master. Twice a day.",
              icon=("paw", "book"), mana=30, cd=DAY_S / 2, range=8, warmup=4.0, tags=["Nature", "Beast", "Utility"], role="Utility",
              target="ally", quirky=True, impact_vfx=[burst("leaf", 12, 1.2, 0.5), glow(1.2, 2.0, 30), ring(0.2, 0.6, 24, byRadius=False)],
              effects=[train(3), bond_animal()]),
        Spell("CallOfTheHerds", "call of the herds",
              "Sound the old herding call past the edge of the map. Something answers: a migrating herd crosses your land (good hunting), a few "
              "farm animals wander in to join you, or a thrumbo passes by. Once every three days.",
              icon=("paw", "trumpet"), mana=70, cd=DAY_S * 3, warmup=5.0, tags=["Nature", "Beast", "Utility"], role="Utility", target="self",
              quirky=True, impact_vfx=[ring(0.1, 2.0, 60, byRadius=False), burst("leaf", 24, 3.0, 0.6), flash(2.5)],
              effects=[incident(["HerdMigration", "FarmAnimalsWanderIn", "ThrumboPasses"])]),
        Spell("UnleashThePack", "unleash the pack",
              "Point, and your own animals answer. Every tame animal of yours within 20 cells that has learned Release (or is bonded to you) "
              "turns feral and charges the target, attacking it for up to 25 seconds or until it falls. Babies, the downed, roped animals and conjured "
              "beasts stay put. Train your huskies and bears for this.",
              icon=("hound", "claw"), mana=30, cd=30, range=30, warmup=0.4, tags=["Nature", "Beast"], role="Attack", target="enemy",
              impact_vfx=[ring(0.1, 1.0, 24, byRadius=False), burst("leaf", 10, 2.0, 0.5)],
              effects=[unleash_pack(20.0, 1500, [hediff("AA_Feral", 1.0, duration=1500)])]),
    )
    bm.add_nodes(
        Node("BeastLore", None, (4, 1), kind="Spell", spell="BeastLore", pre=["Packbond"]),
        Node("UnleashThePack", None, (2, 4), kind="Spell", spell="UnleashThePack", pre=["Grizzly"]),
        Node("Herdsong", "herdsong", (4, 4), kind="Notable", pre=["Whisperer"],
             passives=[P("P_Aura", radius=10.0, interval=HOUR, affects="Allies", color=BEASTC, effects=[animal_produce(0.005)])],
             desc="The herd settles when you hum. Every hour, tame animals within 10 cells of you fill an extra 0.5% of their milk and wool "
                  "and come 0.5% closer to their next egg (12% a day), drafted or not."),
        Node("CallOfTheHerds", None, (6, 3), kind="Spell", spell="CallOfTheHerds", pre=["AlphasCall"]),
    )


# ------------------------------------------------------------------------------------------------ Verdant Warden

def _warden(t, extra):
    vw = t["VerdantWarden"]
    vw.add_spells(
        Spell("WardensMercy", "warden's mercy",
              "Roots bind the wounds of the fallen, friend and foe. Everyone within 8 cells of the target (raiders, strangers, prisoners, "
              "colonists and animals) has every wound and illness that needs it tended at 70% quality, without medicine, so the downed live "
              "long enough to be rescued or captured. Enemies are also held by the roots (90% slower) for an hour.",
              icon=("vine", "cross"), mana=40, cd=DAY_S / 4, range=30, warmup=3.0, tags=["Nature", "Healing", "Utility"], role="Utility",
              target="cell", radius=8.0, quirky=True, affects="AllButSelf",
              impact_vfx=[rune(1.0, 80, tex="ArcanumAscendant/Vfx/Roots"), burst("leaf", 24, 1.5, 0.6, byRadius=True)],
              effects=[tend(0.7, affects="AllButSelf"), hediff("AA_Entangled", 1.0, duration=HOUR, affects="Enemies")]),
        Spell("Mossheart", "mossheart",
              "Plant a mossheart: a squat green stump that eats filth. When it lands, and every hour for the next two days, all filth within "
              "5.5 cells is consumed (a spotless infirmary or kitchen) and, with Biotech, polluted ground there is purified.",
              icon=("tree", "drop"), mana=45, cd=DAY_S * 2, range=20, warmup=3.0, tags=["Nature", "Utility"], role="Utility", target="cell",
              radius=5.5, quirky=True,
              impact_vfx=[ring(0.1, 1.0, 50), burst("leaf", 20, 1.5, 0.6, byRadius=True), glow(1.5, 3.0, 40)],
              effects=[clean(filth=True, pollution=True), spawn("AA_Construct_DruidMossheart", lifetimeTicks=DAY * 2)]),
        Spell("StranglerFig", "strangler fig",
              "Seed a strangler fig among the enemy's works. For 20 seconds its roots tear through a 3.5-cell circle: every structure there that "
              "your colony did not build (turrets, mortars, barricades, walls, even bare rock) takes 40 damage a second, and enemies inside "
              "take 5 thorn damage a second and are often rooted in place. Break a siege camp before the shells fly.",
              icon=("vine", "wall"), mana=45, cd=45, range=30, warmup=1.2, tags=["Nature"], role="Attack", target="cell", radius=3.5,
              hostile=True, ai_min=2,
              impact_vfx=[rune(1.0, 60, tex="ArcanumAscendant/Vfx/Roots"), burst("leaf", 20, 2.0, 0.6, byRadius=True), smoke(4, 1.2)],
              effects=[field(3.5, 1200, 60, [dmg(5, "AA_Thorn", damageBuildings=True, buildingFactor=8.0),
                                             hediff("AA_Entangled", 1.0, duration=90, chance=0.3)],
                             groundTex="ArcanumAscendant/Vfx/Roots", groundAlpha=0.5, ambientFleck="AA_Fleck_Leaf", ambientDensity=1.5,
                             ambientSpeed=0.3, ambientScale=0.7)]),
    )
    vw.add_nodes(
        Node("StranglerFig", None, (0, 3), kind="Spell", spell="StranglerFig", pre=["Briar"]),
        Node("WardensMercy", None, (4, 2), kind="Spell", spell="WardensMercy", pre=["Canopy"]),
        Node("Mossheart", None, (5, 3), kind="Spell", spell="Mossheart", pre=["Sporecap"]),
        Node("Sickroom", "green infirmary", (4, 4), kind="Notable", pre=["Heartwood", "Mossheart"],
             passives=[P("P_Aura", radius=8.0, interval=HOUR, affects="Allies", color=GROVEC, effects=[immunize(0.012)])],
             desc="The sick mend in your shade. Every hour, allies within 8 cells of you (prisoners and animals too) gain 1.2% immunity "
                  "against every disease they are fighting (a little more with healing power), drafted or not."),
    )
    extra += things.totem("Construct_DruidMossheart", "mossheart",
                          "A squat, glowing green stump grown by a verdant warden. It slowly eats the filth (and, with Biotech, the pollution) around it.",
                          color="#3fd890", color2="#a0ff70", hp=250, lifetime=DAY * 2, shape="tree", radius=5.5, interval=HOUR,
                          affects="Allies", pulse=[clean(filth=True, pollution=True)], glow=1.4, size=1.1)


# ------------------------------------------------------------------------------------------------ Shapeshifter

def _shifter(t, extra):
    ss = t["Shapeshifter"]
    ss.add_spells(
        Spell("TheLongSleep", "the long sleep",
              "Sing someone into a bear's winter sleep. They lie down in their bed at once; until they wake after at least 6 hours asleep (12 "
              "hours at most) they need a quarter of the food, heal wounds three times as fast, fight off disease 50% faster and shrug off 20 C "
              "of cold. They wake whole (+20% rest, +5 mood for a day). Works on colonists, prisoners and tame animals.",
              icon=("moon", "snowflake"), mana=25, cd=HOUR_S * 2, range=12, warmup=3.0, tags=["Nature", "Shapeshift", "Healing", "Utility"],
              role="Utility", target="ally", quirky=True,
              impact_vfx=[burst("star", 10, 0.6, 0.4), glow(1.2, 2.0, 40), burst("leaf", 8, 0.8, 0.5)],
              effects=[hediff("AA_DruidLongSleep", 1.0), sleep_now(inBed=True)]),
        Spell("OxForm", "ox form",
              "Become a great musk ox for 6 hours: carry three times as much when hauling, move 10% faster and shrug off 15 C of cold. "
              "Haul the steel home, then the harvest.",
              icon=("horns", "paw"), mana=20, cd=HOUR_S * 3, warmup=1.0, tags=["Nature", "Shapeshift", "Utility"], role="Buff", target="self",
              quirky=True, impact_vfx=[smoke(3, 1.0), flash(2.0), burst("leaf", 10, 1.5, 0.5)],
              effects=[hediff("AA_DruidOxForm", 1.0)]),
        Spell("CubsPlay", "cub's play",
              "Turn into a great clumsy cub and romp with everyone within 7 cells: they regain 30% recreation and laugh about it for a day "
              "(+5 mood). With Biotech, children gain 4 growth points and 40% learning, and babies get to play.",
              icon=("paw", "smile"), mana=35, cd=DAY_S / 2, warmup=2.0, tags=["Nature", "Shapeshift", "Utility"], role="Buff", target="self",
              delivery="Nova", radius=7.0, affects="AlliesNotSelf", quirky=True,
              impact_vfx=[smoke(3, 1.0), ring(0.1, 1.0, 40), burst("petal", 20, 2.0, 0.5, byRadius=True), burst("leaf", 10, 1.5, 0.5)],
              effects=[need("Joy", 0.3), child_growth(4.0, 0.4), memory("AA_DruidCubsPlay")]),
    )
    ss.add_nodes(
        Node("TheLongSleep", None, (2, 1), kind="Spell", spell="TheLongSleep", pre=["BearForm"]),
        Node("OxForm", None, (4, 1), kind="Spell", spell="OxForm", pre=["Hide"]),
        Node("CubsPlay", None, (6, 2), kind="Spell", spell="CubsPlay", pre=["WolfForm"]),
    )
    extra.append(status("AA_DruidLongSleep", "the long sleep",
                        "Sunk into a bear's winter sleep: hunger barely stirs, wounds knit three times as fast and the cold matters less. "
                        "Ends on waking after six hours of sleep.", "#c8a070", bad=False, ticks=HOUR * 12,
                        stages=[stage(factors={"InjuryHealingFactor": 3.0, "ImmunityGainSpeed": 1.5}, offsets={"ComfyTemperatureMin": -20},
                                      extra={"hungerRateFactor": 0.25})],
                        comps=[wake_thought(thought="AA_DruidWokeFromLongSleep", minSleepHours=6.0, rest=0.2, text="AA_DruidLongSleepWoke",
                                            color="#e0c090")],
                        fx=dict(emitter="Dream", emitInterval=45)))
    extra.append(thought_def("AA_DruidWokeFromLongSleep", "woke from the long sleep",
                             "I slept like a bear in winter. I feel whole again.", 5, days=1))
    extra.append(status("AA_DruidOxForm", "ox form", "Shaped like a great musk ox: built to haul, hard to chill.", "#c88a5a", bad=False,
                        ticks=HOUR * 6, stages=[stage(factors={"CarryingCapacity": 3.0, "MoveSpeed": 1.1}, offsets={"ComfyTemperatureMin": -15})],
                        fx=dict(emitter="Stone", emitInterval=60)))
    extra.append(thought_def("AA_DruidCubsPlay", "played with the cub",
                             "A shapeshifter turned into a great clumsy cub and played with us. I haven't laughed like that in years.", 5, days=1))


def extend(t, extra):
    _base(t, extra)
    _beastmaster(t, extra)
    _warden(t, extra)
    _shifter(t, extra)

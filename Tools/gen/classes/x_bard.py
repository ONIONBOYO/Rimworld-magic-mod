"""Colony expansion of the Bard: parties, sing-alongs and catchy tunes at home; sagas, horns, illusions and a jester who busks."""
import re

from classes.common import *  # noqa: F401,F403
from classes.prims_family import *  # noqa: F401,F403
from classes.prims_colony import *  # noqa: F401,F403
from classes import c_bard as M

KEYS = {
    "AA_SkaldVerseLabel": "a verse for the saga!",
}

HOUR_S = HOUR / SEC  # one in-game hour in seconds of game time at 1x
DAY_S = DAY / SEC

SONGC = (1.0, 0.61, 0.88, 1.0)
SKALDC = (1.0, 0.54, 0.29, 1.0)
JESTERC = (1.0, 0.85, 0.29, 1.0)


# ------------------------------------------------------------------------------------------------ local primitives (Colony_Bard.cs)

def sing_along(opinion, mood, maxSingers=10, joy=0.15, **kw):
    """Everyone awake in the area sings together: pairwise opinion memories, a mood memory staged by choir size, recreation."""
    return O("Effect_BardSingAlong", opinionThought=opinion, moodThought=mood, maxSingers=maxSingers, joy=float(joy), **kw)


def pass_the_hat(lo=10, hi=25, goodwill=1, maxGoodwill=5, guestJoy=0.2, colonistJoy=0.1, **kw):
    """Guests of friendly factions in the area tip silver (scaled by the caster's Social) and warm their factions to the colony."""
    return O("Effect_JesterPassTheHat", silverPerGuest="%d~%d" % (lo, hi), goodwillPerGuest=goodwill, maxGoodwillPerFaction=maxGoodwill,
             guestJoy=float(guestJoy), colonistJoy=float(colonistJoy), **kw)


# ------------------------------------------------------------------------------------------------ base tree

def _base(t, extra):
    b = t["Bard"]
    b.add_spells(
        Spell("Revelry", "revelry",
              "Strike up a dance tune and call the colony to a party. A real party gathers around you at a party spot, table or campfire "
              "(attending lifts the mood as any good party does), and everyone within 12 cells of you is festive for four hours: their "
              "recreation fills 50% faster. Needs a free gathering spot and a colony that is not busy fighting; the festive mood lands "
              "even if the party cannot start. Once every two days.",
              icon=("note", "balloon"), mana=50, cd=DAY_S * 2, warmup=3.0, tags=["Song", "Utility"], role="Utility", target="self",
              delivery="Nova", radius=12.0, affects="Allies", quirky=True,
              impact_vfx=M.SONG_NOVA + [burst("petal", 24, 2.5, 0.6, byRadius=True), burst("star", 12, 2.0, 0.5, byRadius=True)],
              effects=[gathering("Party"), hediff("AA_BardFestive", 1.0)],
              flavor="Somebody has to start the dancing."),
        Spell("Singalong", "fireside sing-along",
              "Lead everyone within 7 cells in a song they all know. Up to 10 people who are awake (colonists, slaves, prisoners and guests "
              "alike, children too) sing together: every pair of them gains +10 opinion of each other for six days, each regains 15% "
              "recreation, and the bigger the choir the better they all feel for a day (+3 mood for two or three voices, +5 for four, +7 "
              "for six, +9 for eight or more). Old grudges are hard to keep through the chorus. Once a day.",
              icon=("brazier", "note"), mana=40, cd=DAY_S, warmup=4.0, tags=["Song", "Utility"], role="Utility", target="self",
              delivery="Nova", radius=7.0, affects="Allies", quirky=True,
              impact_vfx=[ring(0.1, 1.0, 50), burst("note", 26, 1.6, 0.6, byRadius=True), glow(2.0, 3.5, 50)],
              effects=[sing_along("AA_BardSangTogether", "AA_BardChoir", maxSingers=10, joy=0.15)]),
        Spell("SurgeonsSonata", "surgeon's sonata",
              "Play a slow, steady sonata for a doctor. For six hours their hands do not shake: their surgeries succeed 30% more often "
              "(relative to their own odds), and they tend wounds 20% better and 30% faster. Play it before the risky operation, not "
              "after. Twice a day.",
              icon=("cross", "hand"), mana=30, cd=DAY_S / 2, range=12, warmup=3.0, tags=["Song", "Healing", "Utility"], role="Buff",
              target="ally", quirky=True,
              impact_vfx=[glow(1.4, 2.4, 40), burst("note", 10, 1.0, 0.5), ring(0.2, 0.7, 30, byRadius=False)],
              effects=[hediff("AA_BardSonata", 1.0)]),
        Spell("CatchyTune", "catchy tune",
              "Teach someone a tune they cannot stop humming. For a day they hum it wherever they go, and anyone who comes within 4 cells "
              "of them catches it (but does not pass it on). Hummer and listeners alike find their recreation draining 40% slower for a "
              "day. Send it round the colony with your busiest hauler or cook. Once a day.",
              icon=("note", "swirl"), mana=35, cd=DAY_S, range=12, warmup=2.0, tags=["Song", "Utility"], role="Buff", target="ally",
              quirky=True, impact_vfx=[burst("note", 14, 1.4, 0.5), spiral("note", 10, 1.5, 0.5), glow(1.2, 2.0, 30)],
              effects=[hediff("AA_BardHumming", 1.0)], flavor="It goes da-da-DEE-da. No, the other one."),
        Spell("SongOnTheWind", "song on the wind",
              "Sing a song meant for someone you have never met, and let the wind carry it past the edge of the map. Someone out there hears "
              "it: a wanderer arrives soon after, asking to join the colony. Once a season.",
              icon=("gust", "feather"), mana=80, cd=DAY_S * 15, warmup=6.0, tags=["Song", "Utility"], role="Utility", target="self",
              quirky=True, tier=3, impact_vfx=[ring(0.2, 2.0, 60, byRadius=False), burst("note", 30, 3.5, 0.6), burst("feather", 12, 3.0, 0.5)],
              effects=[incident(["WandererJoin"])], flavor="Nobody walks the rim alone for long once they've heard that tune."),
        Spell("TauntingElegy", "taunting elegy",
              "Stand over a fallen body and sing its elegy, mockingly, for its friends to hear. Every enemy within 8 cells of the corpse is "
              "disheartened for 20 seconds (25% worse aim, 20% worse in melee, 15% slower), and each person among them has one chance in "
              "three to lose heart altogether and run from the battle. Bosses are unmoved. Bring their champion down first, then sing over him.",
              icon=("skull", "trumpet"), mana=35, cd=30, range=26, warmup=1.0, tags=["Song", "Curse"], role="Control", target="corpse",
              radius=8.0, affects="Enemies", autocast=False,
              impact_vfx=[ring(0.1, 1.0, 40), burst("note", 18, 2.5, 0.6, byRadius=True), burst("skull", 6, 1.5, 0.5), smoke(3, 0.8)],
              effects=[hediff("AA_BardDisheartened", 1.0),
                       random_of([mental("PanicFlee", humanlikeOnly=True), hediff("AA_BardDisheartened", 1.0), hediff("AA_BardDisheartened", 1.0)])]),
    )
    b.add_nodes(
        Node("CatchyTune", None, (2, 1), kind="Spell", spell="CatchyTune", pre=["PerfectPitch"]),
        Node("SurgeonsSonata", None, (6, 1), kind="Spell", spell="SurgeonsSonata", pre=["Harmony"]),
        Node("TauntingElegy", None, (0, 3), kind="Spell", spell="TauntingElegy", pre=["Crescendo"]),
        Node("Singalong", None, (2, 3), kind="Spell", spell="Singalong", pre=["Lullaby"]),
        Node("Revelry", None, (3, 4), kind="Spell", spell="Revelry", pre=["Showman"]),
        Node("Nightsong", "nightsong", (5, 4), kind="Notable", pre=["Inspire"],
             passives=[P("P_Aura", radius=9.0, interval=HOUR, affects="Allies", color=SONGC, effects=[hediff("AA_BardNightsong", 1.0)])],
             desc="You hum in your sleep, and everyone sleeps better for it. Every hour, everyone within 9 cells of you (drafted or not, "
                  "prisoners and animals too) rests 20% faster for the next hour. Put your bed in the barracks."),
        Node("SongOnTheWind", None, (2, 6), kind="Spell", spell="SongOnTheWind", pre=["Chorus"]),
    )
    extra.append(status("AA_BardFestive", "festive", "In a festive mood: recreation fills half again as fast.", "#ff9ce0", bad=False,
                        ticks=HOUR * 4, stages=[stage(factors={"JoyGainFactor": 1.5})], fx=dict(emitter="Song", emitInterval=90)))
    extra.append(opinion_def("AA_BardSangTogether", "sang together", 10, days=6.0,
                             desc="We sang the same song around the fire. It's hard to stay cross with someone after that."))
    extra.append(thought_def("AA_BardChoir", "sang along", "", 3, days=1.0, stages=[
        ("sang in a small group", "A few of us sang together. It was nice.", 3),
        ("sang in a choir", "We sang together, all of us at once. I still feel it.", 5),
        ("sang in a big choir", "So many voices in one song. I felt like part of something.", 7),
        ("sang in a great choir", "The whole colony sang as one. I'll remember that night for a long time.", 9),
    ]))
    extra.append(status("AA_BardSonata", "surgeon's sonata", "A slow, steady sonata keeps this doctor's hands from shaking.", "#ff9ce0",
                        bad=False, ticks=HOUR * 6,
                        stages=[stage(factors={"MedicalSurgerySuccessChance": 1.3, "MedicalTendQuality": 1.2, "MedicalTendSpeed": 1.3})],
                        fx=dict(emitter="Song", emitInterval=120)))
    extra.append(status("AA_BardHumming", "humming a catchy tune",
                        "Humming a tune that will not leave. Anyone who comes close catches it too, and recreation drains slower.", "#ff9ce0",
                        bad=False, ticks=DAY, stages=[stage(factors={"JoyFallRateFactor": 0.6})],
                        comps=[periodic(250, area=[hediff("AA_BardEarworm", 1.0, affects="Allies")], radius=4.0, color=SONGC)],
                        fx=dict(emitter="Song", emitInterval=60)))
    extra.append(status("AA_BardEarworm", "catchy tune", "Caught a catchy tune from someone humming it. Recreation drains slower.", "#ff9ce0",
                        bad=False, ticks=DAY, stages=[stage(factors={"JoyFallRateFactor": 0.6})], fx=dict(emitter="Song", emitInterval=180)))
    extra.append(status("AA_BardDisheartened", "disheartened", "Heard their fallen comrade mocked in song. The fight has gone out of them.",
                        "#b08ab0", bad=True, ticks=1200,
                        stages=[stage(factors={"ShootingAccuracyPawn": 0.75, "MeleeHitChance": 0.8, "MoveSpeed": 0.85})],
                        fx=dict(emitter="Lost", emitInterval=24)))
    extra.append(status("AA_BardNightsong", "nightsong", "Sleeping near a bard who hums in their sleep. Rest comes faster.", "#ff9ce0",
                        bad=False, ticks=HOUR + 200, stages=[stage(factors={"RestRateMultiplier": 1.2})]))


# ------------------------------------------------------------------------------------------------ Skald

def _skald(t, extra):
    sk = t["Skald"]
    sk.add_spells(
        Spell("NamedInSaga", "named in the saga",
              "Name an enemy in your saga. For 30 seconds it takes 30% more damage from everything. If it dies while its name still rings, "
              "its death becomes a verse: every ally within 12 cells of the body gains valor, every colonist among them remembers being "
              "there (+6 mood for three days), and you regain 20 mana.",
              icon=("scroll", "sword"), mana=30, cd=30, range=28, warmup=0.6, tags=["Song", "Curse"], role="Control", target="enemy",
              autocast=False, school=M.SKALD,
              impact_vfx=[flash(2.5), burst("note", 14, 2.0, 0.6), ring(0.2, 0.8, 26, byRadius=False)],
              effects=[hediff("AA_SkaldNamed", 1.0)], flavor="'And then came Bjorn, whose axe was hungry...'"),
        Spell("HornOfTheHall", "horn of the hall",
              "Blow the great horn. Everyone of the colony on the map (and any guests) drops what they are doing, even sleep, and hurries to "
              "you at double speed for 20 seconds; getting hurt breaks the call. For two hours after, their tiredness stops growing and they "
              "walk 10% faster, and they wake with 20% more rest. Draft them as they arrive. Prisoners cannot leave their cells; the colony's "
              "animals come running too. Once a day.",
              icon=("trumpet", "horns"), mana=50, cd=DAY_S, warmup=0.5, tags=["Song", "Utility"], role="Utility", target="self",
              delivery="Global", affects="AlliesNotSelf", quirky=True, tier=2, school=M.SKALD, shake=1.0,
              cast_vfx=[ring(0.1, 2.5, 50, byRadius=False), flash(3.0)], impact_vfx=[burst("note", 6, 1.5, 0.5)],
              effects=[hediff("AA_SkaldHornCall", 1.0), hediff("AA_SkaldRoused", 1.0), need("Rest", 0.2)],
              flavor="Three blasts: raiders. Nobody remembers what four blasts means."),
        Spell("AncestralSaga", "saga of the old ones",
              "Recite the saga of the old ones, who built the great machines before the fall. Somewhere in its four hundred verses is something "
              "your researchers did not know: 400 research points (more with spell power) go into the current project, and a project with "
              "fewer than 150 points left is finished outright. Once a day.",
              icon=("book", "rune"), mana=45, cd=DAY_S, warmup=6.0, tags=["Song", "Utility"], role="Utility", target="self", quirky=True,
              school=M.SKALD, impact_vfx=[pillar(1.0, 50), burst("note", 16, 1.5, 0.5), burst("rune", 10, 1.2, 0.5)],
              effects=[research(400, finishBelow=150)]),
    )
    sk.add_nodes(
        Node("NamedInSaga", None, (2, 2), kind="Spell", spell="NamedInSaga", pre=["Drummer"]),
        Node("HornOfTheHall", None, (5, 3), kind="Spell", spell="HornOfTheHall", pre=["Bannerman"]),
        Node("AncestralSaga", None, (1, 4), kind="Spell", spell="AncestralSaga", pre=["Hymnal"]),
        Node("Frith", "frith", (4, 4), kind="Notable", pre=["Shieldsinger"],
             passives=[P("P_Aura", radius=10.0, interval=HOUR, affects="Allies", color=SKALDC, effects=[hediff("AA_SkaldFrith", 1.0)])],
             desc="Where the skald sits, there is peace. Every hour, everyone within 10 cells of you (drafted or not) is under frith for the "
                  "next hour: no social fights start, and their mental break threshold is 4% lower."),
    )
    extra.append(status("AA_SkaldNamed", "named in the saga", "Named in a skald's saga. Every blow lands harder, and its death will be sung.",
                        "#ff8a4a", bad=True, ticks=1800, stages=[stage(factors={"IncomingDamageFactor": 1.3})],
                        comps=[death_trigger(caster=[mana(20)], area=[hediff("AA_Valor", 1.0, affects="Allies"), memory("AA_SkaldSagaVerse", affects="Allies")],
                                             radius=12.0, color="#ff8a4a", label="AA_SkaldVerseLabel",
                                             vfx=[ring(0.1, 1.0, 40), burst("note", 24, 3.0, 0.6, byRadius=True), flash(3.0)])],
                        fx=dict(emitter="Watched", emitInterval=20)))
    extra.append(thought_def("AA_SkaldSagaVerse", "a verse for the saga",
                             "I was there when the one named in the skald's saga fell. They will sing of it for years.", 6, days=3))
    extra.append(status("AA_SkaldHornCall", "answering the horn", "The great horn is calling. Hurrying to the skald.", "#ff8a4a", bad=False,
                        ticks=1200, stages=[stage(factors={"MoveSpeed": 2.0})],
                        comps=[compel("Approach", breakOnDamage=True, breakDamage=1.0, approach=3.0)], fx=dict(emitter="Song", emitInterval=30)))
    extra.append(status("AA_SkaldRoused", "roused by the horn", "Woken by the horn and ready for anything. Tiredness can wait.", "#ff8a4a",
                        bad=False, ticks=HOUR * 2, stages=[stage(factors={"MoveSpeed": 1.1}, extra={"restFallFactor": 0.0})]))
    extra.append(status("AA_SkaldFrith", "frith", "Under the skald's frith: peace among kin. No fights here.", "#ff8a4a", bad=False,
                        ticks=HOUR + 200, stages=[stage(offsets={"MentalBreakThreshold": -0.04}, extra={"socialFightChanceFactor": 0.0})]))


# ------------------------------------------------------------------------------------------------ Illusionist

def _illusionist(t, extra):
    il = t["Illusionist"]
    il.add_spells(
        Spell("PhantomFeast", "phantom feast",
              "Lay an illusory banquet before everyone within 6 cells of the target. Nobody is fed, but everyone believes they have eaten "
              "a lavish meal: they get the mood of one (as from a real lavish meal) and for a day their hunger grows half as fast. The "
              "illusion that gets a colony through the last week of winter. Once a day.",
              icon=("pie", "mask"), mana=40, cd=DAY_S, range=20, warmup=3.0, tags=["Song", "Illusion", "Utility"], role="Utility",
              target="ally_area", radius=6.0, affects="Allies", quirky=True, school=M.ILLUSION,
              impact_vfx=[ring(0.1, 1.0, 40), burst("star", 20, 1.5, 0.6, byRadius=True), spiral("star", 12, 1.5, 0.5), glow(2.0, 3.5, 40)],
              effects=[memory("AteLavishMeal"), hediff("AA_IllusionistFed", 1.0)], flavor="'Was the swan always this... crunchy?'"),
        Spell("Hypnotherapy", "hypnotherapy",
              "Put a colonist into a trance and talk them out of their cravings. Every addiction they have is gone at once, withdrawal and "
              "all (even luciferium), and they come out of it calm and clear-headed (+5 mood for two days). It cannot stop them from starting "
              "again. Once every five days.",
              icon=("swirl", "bottle"), mana=80, cd=DAY_S * 5, range=4, warmup=6.0, tags=["Song", "Illusion", "Healing", "Utility"],
              role="Utility", target="ally", quirky=True, tier=3, school=M.ILLUSION,
              impact_vfx=[spiral("star", 18, -2.0, 0.5), glow(1.4, 0.6, 40, useColor2=True), ring(0.6, 0.1, 30, byRadius=False)],
              effects=[restore(addictions=True), memory("AA_IllusionistTrance")]),
        Spell("FalseSun", "false sun",
              "Hang an illusion of the noonday sun in the air for three days. It is so convincing that the plants believe it: it lights the "
              "ground like a sun lamp, so crops within about 6 cells grow indoors, underground or through an eclipse (in the daytime hours; "
              "plants still rest at night), and nobody near it works in the dark. It needs no power and burns no fuel. Once every three days.",
              icon=("sun", "mask"), mana=60, cd=DAY_S * 3, range=20, warmup=4.0, tags=["Song", "Illusion", "Utility"], role="Utility",
              target="cell", quirky=True, school=M.ILLUSION,
              impact_vfx=[pillar(1.2, 50), flash(4.0, color="#fff4c8"), burst("star", 20, 2.0, 0.5)],
              effects=[spawn("AA_Construct_IllusionistFalseSun", lifetimeTicks=DAY * 3)]),
    )
    il.add_nodes(
        Node("PhantomFeast", None, (4, 1), kind="Spell", spell="PhantomFeast", pre=["Glamour"]),
        Node("Hypnotherapy", None, (1, 3), kind="Spell", spell="Hypnotherapy", pre=["Hypnosis"]),
        Node("FalseSun", None, (5, 3), kind="Spell", spell="FalseSun", pre=["Phantasm"]),
    )
    extra.append(status("AA_IllusionistFed", "phantom feast", "Convinced by an illusion that they ate a banquet. Hunger grows half as fast.",
                        "#c49cff", bad=False, ticks=DAY, stages=[stage(extra={"hungerRateFactor": 0.5})], fx=dict(emitter="Charm", emitInterval=240)))
    extra.append(thought_def("AA_IllusionistTrance", "free of cravings",
                             "The illusionist talked me out of it while I was under. The craving is just... gone.", 5, days=2))
    sun = things.totem("Construct_IllusionistFalseSun", "false sun",
                       "An illusion of the noonday sun, hung in the air by an illusionist. It is so convincing that plants grow under it as if under "
                       "a sun lamp, and it needs no power at all.",
                       color="#fff4c8", color2="#ffd84a", hp=200, lifetime=DAY * 3, shape="brazier", pulse=None, light=14.0, glow=3.0, size=1.2)
    glower = re.compile(r"(<li Class=\"CompProperties_Glower\">.*?<glowColor>)\([^)]*\)(</glowColor>)", re.S)
    out = [glower.sub(r"\1(370,370,370,0)\2", x) for x in sun]
    assert any("(370,370,370,0)" in x for x in out), "false sun glower not set: %s" % sun
    extra += out


# ------------------------------------------------------------------------------------------------ Jester

def _jester(t, extra):
    js = t["Jester"]
    js.add_spells(
        Spell("PassTheHat", "pass the hat",
              "Juggle, tumble and fall over for everyone within 9 cells, then pass the hat. Every visitor, trader and guest of a friendly "
              "faction watching tosses in 10 to 25 silver (5% more per level of your Social skill), regains 20% recreation and leaves their "
              "faction a little fonder of the colony (+1 goodwill per guest, at most +5 per faction). Colonists just enjoy the show (+10% "
              "recreation). The silver lands at your feet. Twice a day.",
              icon=("coin", "smile"), mana=30, cd=DAY_S / 2, warmup=5.0, tags=["Song", "Chaos", "Utility"], role="Utility", target="self",
              delivery="Nova", radius=9.0, affects="Allies", quirky=True, school=M.JESTER,
              impact_vfx=[ring(0.1, 1.0, 40), burst("star", 16, 2.0, 0.5, byRadius=True), burst("note", 10, 1.5, 0.5, byRadius=True)],
              effects=[pass_the_hat(10, 25, goodwill=1, maxGoodwill=5, guestJoy=0.2, colonistJoy=0.1)],
              flavor="Tips appreciated. Pies, less so."),
        Spell("WheelOfFortune", "wheel of fortune",
              "Spin the jester's wheel and let fate pick something to happen to the colony. Most slices are good: cargo pods, a falling "
              "ship chunk, a meteorite full of ore, a wild animal deciding to join you, a psychic soothe, an ambrosia sprout or a refugee pod. "
              "Some are not: a short circuit, a solar flare, an eclipse or a family of alphabeavers. Slices that cannot happen right now "
              "are skipped. Once every three days.",
              icon=("dice", "ring"), mana=50, cd=DAY_S * 3, warmup=3.0, tags=["Song", "Chaos", "Utility"], role="Utility", target="self",
              quirky=True, tier=2, school=M.JESTER,
              impact_vfx=[spiral("star", 24, 3.0, 0.6), burst("star", 16, 2.5, 0.5), flash(2.5, color="#ffd84a")],
              effects=[incident(["ResourcePodCrash", "ShipChunkDrop", "MeteoriteImpact", "SelfTame", "PsychicSoothe", "AmbrosiaSprout",
                                 "RefugeePodCrash", "ShortCircuit", "SolarFlare", "Eclipse", "Alphabeavers"])],
              flavor="Round and round she goes."),
        Spell("SeltzerBottle", "seltzer bottle",
              "Spray a fizzing blast of conjured soda water in a 50-degree cone 7 cells long. Every fire it touches goes out (on the ground, "
              "on buildings and on anyone burning, friend or foe), and enemies caught in it are knocked back 2 cells and left sputtering for "
              "10 seconds (15% slower, 20% worse aim). Put out the kitchen, then the raiders.",
              icon=("bottle", "wave"), mana=20, cd=12, range=7, warmup=0.3, tags=["Song", "Chaos"], role="Control", target="cell",
              delivery="Cone", coneAngle=50.0, coneRange=7.0, quirky=True, school=M.JESTER,
              impact_vfx=[burst("glow", 10, 2.5, 0.5), burst("snow", 14, 3.0, 0.4)],
              effects=[extinguish(affects="All"), knock(2.0, affects="Enemies"), hediff("AA_JesterSoaked", 1.0, affects="Enemies")],
              flavor="The oldest trick in the book. The book is very wet."),
    )
    js.add_nodes(
        Node("PassTheHat", None, (4, 1), kind="Spell", spell="PassTheHat", pre=["Pratfall"]),
        Node("SeltzerBottle", None, (0, 2), kind="Spell", spell="SeltzerBottle", pre=["WildMagic"]),
        Node("ClassClown", "class clown", (2, 4), kind="Notable", pre=["ConfettiCannon"],
             passives=[P("P_Aura", radius=8.0, interval=HOUR, affects="AlliesNotSelf", color=JESTERC,
                         effects=[random_of([memory("AA_JesterLaughed")] + [need("Joy", 0.0)] * 5)])],
             desc="You can't help being funny. Every hour, each person within 8 cells of you (drafted or not) has one chance in six to crack "
                  "up at something you did: +4 mood for six hours."),
        Node("WheelOfFortune", None, (4, 4), kind="Spell", spell="WheelOfFortune", pre=["Gambler"]),
    )
    extra.append(status("AA_JesterSoaked", "soaked", "Drenched in conjured soda water. Sputtering, sticky and slow.", "#bfe8ff", bad=True,
                        ticks=600, stages=[stage(factors={"MoveSpeed": 0.85, "ShootingAccuracyPawn": 0.8})], fx=dict(emitter="Chill", emitInterval=30)))
    extra.append(thought_def("AA_JesterLaughed", "the jester made me laugh", "I couldn't help it. That jester is ridiculous.", 4, days=0.25))


def extend(t, extra):
    _base(t, extra)
    _skald(t, extra)
    _illusionist(t, extra)
    _jester(t, extra)

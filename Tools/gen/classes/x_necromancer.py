"""Colony expansion of the Necromancer: death as a resource at home too - the dead work, give their flesh and their knowledge,
say goodbye to the grieving, and the plague, the bone and the tomb's cold all find a use in the colony."""
from classes.common import *  # noqa: F401,F403
from classes.prims_family import *  # noqa: F401,F403
from classes.prims_colony import *  # noqa: F401,F403
from classes import c_necromancer as M

HOUR_S = HOUR / SEC  # one in-game hour in seconds of game time at 1x
DAY_S = DAY / SEC

NECROC = (0.49, 1.0, 0.6, 1.0)
LICHC = (0.6, 0.85, 1.0, 1.0)
BONEC = (0.91, 0.89, 0.78, 1.0)
PLAGUEC = (0.66, 0.85, 0.29, 1.0)

WATER = ["WaterShallow", "WaterDeep", "WaterMovingShallow", "WaterMovingChestDeep", "WaterOceanShallow", "WaterOceanDeep", "Marsh"]
UNDEAD_NEEDS = ["Food", "Rest", "Joy", "Comfort", "Beauty", "Outdoors", "RoomSize"]


# ------------------------------------------------------------------------------------------------ local primitives
# (C# in Source/ArcanumAscendant/Spells/Colony_Necromancer.cs)

def graft(donorRadius=5.0, maxParts=1, status=None, thought=None, **kw):
    """A missing part of the target (limb, eye, organ...) is taken from a fresh corpse of the same body plan within donorRadius
    of them (a bionic there transfers as it is); the donor crumbles, its gear left behind."""
    return O("Effect_NecromancerGraft", donorRadius=float(donorRadius), maxParts=maxParts if maxParts != 1 else None,
             graftHediff=status, thought=thought, **kw)


def rite(effects=None, maxCorpses=1, humanlikeOnly=False, notDesiccated=True, includeGraves=False, spareColonists=True, consume=False,
         easeGrief=False, mournerThought=None, subRadius=0.0, **kw):
    """A rite over the dead: up to maxCorpses qualifying bodies in the area (or, with includeGraves, inside graves and sarcophagi)
    each run the sub-effects at the body (radius subRadius); with easeGrief everyone mourning the dead person lets go; with
    consume the body crumbles afterwards. Nothing happens at all without a body, so these spells really need one."""
    return O("Effect_NecromancerRite", effects=effects, maxCorpses=maxCorpses if maxCorpses != 1 else None,
             humanlikeOnly=humanlikeOnly or None, notDesiccated=None if notDesiccated else False, includeGraves=includeGraves or None,
             spareColonists=None if spareColonists else False, consume=consume or None, easeGrief=easeGrief or None,
             mournerThought=mournerThought, subRadius=float(subRadius) if subRadius else None, **kw)


def node(sid, pos, pre):
    return Node(sid, None, pos, kind="Spell", spell=sid, pre=pre)


# ------------------------------------------------------------------------------------------------ base tree

def _base(t, extra):
    n = t["Necromancer"]
    n.add_spells(
        Spell("GraveLabor", "grave labor",
              "Bind a dead person back into their body to work for the colony. For three days the servant hauls, cleans, cooks, plants, mines "
              "and builds (20% slower than the living) without food, sleep, recreation or complaint. It will not fight, and it lies down dead "
              "again when the binding ends. Raiders and the colony's own fallen both answer, but not dried-out husks, beasts or machines. "
              "Once a day.",
              icon=("hammer", "skull"), mana=45, cd=DAY_S, range=20, warmup=3.0, tags=["Necromancy", "Summon", "Utility"], role="Utility",
              target="corpse", radius=1.5, quirky=True,
              impact_vfx=[rune(1.0, 80), pillar(1.0, 40), burst("smoke", 10, 1.0, 0.8, byRadius=True), burst("skull", 4, 1.0, 0.6)],
              effects=[raise_servant(3.0, 1)],
              flavor="The harvest will not wait for the living."),
        Spell("FleshGraft", "flesh graft",
              "Stitch a part from a fresh corpse onto a living ally who has lost one: an arm, a leg, an eye, a jaw, a kidney, a lung. The donor "
              "must lie within 5 cells of the patient and share their body plan (people for people, dogs for dogs), and if the donor had a "
              "bionic or prosthetic there, the patient gets that instead. The rest of the donor crumbles to dust, leaving its gear behind. "
              "The graft aches for three days (+10% pain, -5% moving and manipulation), and most people are not thrilled to wear a dead "
              "stranger's flesh (-3 mood for 6 days). One part per casting, once a day.",
              icon=("hand", "bone"), mana=55, cd=DAY_S, range=3, warmup=6.0, tags=["Necromancy", "Utility"], role="Heal", target="ally",
              quirky=True,
              impact_vfx=[glow(1.2, 2.0, 30, useColor2=True), burst("bone", 8, 1.0, 0.5), burst("smoke", 6, 0.8, 0.7)],
              effects=[graft(5.0, 1, "AA_NecroGraft", "AA_NecroDeadMansGraft")],
              flavor="He won't be needing it."),
        Spell("LastWords", "last words",
              "Call a soul back for one last conversation. Target a body, or a grave or sarcophagus that holds one: everyone in the colony who "
              "mourns that person (a lost spouse, a child, a friend, a bonded animal) can finally let go. Every sad memory they have of that "
              "death ends, and each mourner gains 'heard their last words' (+8 mood for 5 days). The body is not disturbed. "
              "Once every two days.",
              icon=("lips", "skull"), mana=40, cd=DAY_S * 2, range=8, warmup=5.0, tags=["Necromancy", "Utility"], role="Utility",
              target="cell", radius=1.0, quirky=True,
              impact_vfx=[pillar(0.8, 60), spiral("skull", 8, 1.0, 0.5), glow(1.5, 2.5, 50)],
              effects=[rite(maxCorpses=1, notDesiccated=False, includeGraves=True, spareColonists=False, easeGrief=True,
                            mournerThought="AA_NecroLastWords")],
              flavor="'Tell her I'm not angry.'"),
        Spell("UnlivingVigil", "unliving vigil",
              "Lend an ally the stillness of the grave for twelve hours. Their hunger and tiredness stop right where they are and pain reaches "
              "them only by half, so they can tend the sick, finish the harvest or stand watch through the night without a break. Feeling "
              "dead inside is unsettling (-3 mood for half a day). Once a day.",
              icon=("eye", "moon"), mana=30, cd=DAY_S, range=20, warmup=1.5, tags=["Necromancy", "Utility"], role="Buff", target="ally",
              quirky=True,
              impact_vfx=[ring(0.6, 0.1, 30, byRadius=False), burst("smoke", 8, 0.6, 0.7), glow(1.0, 1.8, 30)],
              effects=[hediff("AA_NecroVigil", 1.0), memory("AA_NecroGraveStill")]),
        Spell("GraveOath", "oath beyond the grave",
              "Bind an ally's soul to their body for a day. If they die before the oath fades (and you are on the same map), they stand back "
              "up a moment later, fully mended, as your deathless thrall, and fight on for six hours before the binding ends and they fall "
              "for good. The oath holds only once.",
              icon=("seal", "skull"), mana=35, cd=120, range=20, warmup=1.0, tags=["Necromancy"], role="Buff", target="ally", quirky=True,
              impact_vfx=[rune(0.8, 60, byRadius=False), spiral("skull", 6, 1.0, 0.5)],
              effects=[hediff("AA_NecroGraveOath", 1.0)],
              flavor="Not yet."),
        Spell("CorpseWalk", "corpse walk",
              "Step into a corpse and out of it. You vanish and reappear where a body lies, up to 30 cells away, and the body bursts into "
              "grave-mist that weakens every enemy within 3 cells. No body, no step: it needs a corpse that is not a dried-out husk, and it "
              "spares the colony's own dead.",
              icon=("portal", "skull"), mana=24, cd=12, range=30, warmup=0.5, tags=["Necromancy"], role="Mobility", target="corpse",
              radius=1.0,
              impact_vfx=[flash(2.0), burst("smoke", 16, 2.0, 0.9), burst("skull", 5, 1.5, 0.6)],
              effects=[rite(maxCorpses=1, consume=True, subRadius=3.0,
                            effects=[teleport("CasterToTarget"), hediff("AA_Weakened", 1.0, affects="Enemies")])]),
    )
    n.add_nodes(
        node("UnlivingVigil", (1, 4), ["Enfeeble"]),
        node("GraveLabor", (6, 3), ["ShamblingHorde"]),
        node("FleshGraft", (3, 4), ["SoulHarvest"]),
        node("LastWords", (5, 4), ["SpeakWithDead"]),
        node("CorpseWalk", (2, 5), ["Deathless"]),
        node("GraveOath", (4, 6), ["CorpseLord"]),
        Node("KeeperOfTheDead", "keeper of the dead", (6, 5), kind="Notable", pre=["NecroticMastery"],
             passives=[P("P_Aura", radius=7.0, interval=1250, affects="Allies", color=NECROC, effects=[preserve()])],
             desc="Rot answers to you. Every half hour, food and corpses within 7 cells of you are made fresh again, so nothing near you ever "
                  "spoils: make your bed beside the larder or the morgue."),
    )
    extra.append(status("AA_NecroGraft", "fresh graft", "A part taken from the dead and stitched on by necromancy. It aches while it takes.",
        "#c89a8a", bad=True, ticks=DAY * 3, fx=dict(emitter="Undead", emitInterval=60),
        stages=[stage(caps={"Moving": -0.05, "Manipulation": -0.05}, extra={"painOffset": 0.1})]))
    extra.append(status("AA_NecroVigil", "unliving vigil", "The stillness of the grave: hunger and tiredness have stopped, and pain is dulled.",
        "#7dff9a", bad=False, ticks=HOUR * 12, fx=dict(emitter="Undead", emitInterval=45),
        stages=[stage(factors={"MoveSpeed": 0.95}, extra={"hungerRateFactor": 0.0, "restFallFactor": 0.0, "painFactor": 0.5})]))
    extra.append(status("AA_NecroGraveOath", "oath beyond the grave", "Bound to this body by a necromancer's oath. Death will not keep them down, once.",
        "#7dff9a", bad=False, ticks=DAY, fx=dict(emitter="Undead", emitInterval=50),
        comps=[raise_on_death(HOUR * 6), O("HediffCompProperties_DisappearsOnDeath")]))
    extra.append(thought_def("AA_NecroLastWords", "heard their last words",
        "The necromancer let them speak one last time. I said goodbye, and I can let go now.", 8, days=5))
    extra.append(thought_def("AA_NecroDeadMansGraft", "wearing a dead stranger's flesh",
        "Part of me used to belong to someone else. It still twitches sometimes.", -3, days=6, nullifying=["Psychopath"]))
    extra.append(thought_def("AA_NecroGraveStill", "grave-still",
        "The necromancer stilled my body. I feel like a corpse that forgot to lie down.", -3, days=0.5))


# ------------------------------------------------------------------------------------------------ Lich

def _lich(t, extra):
    li = t["Lich"]
    li.add_spells(
        Spell("MindHarvest", "harvest of minds",
              "Drink what a dead mind knew. The body of a person lying within a cell of the target gives up its knowledge: 450 research "
              "points go into the colony's current project (more with spell power), and the husk crumbles to dust, leaving its gear behind. "
              "It needs a body that is not dried out, and it spares the colony's own dead. Once a day.",
              icon=("book", "eye"), mana=50, cd=DAY_S, range=6, warmup=5.0, tags=["Necromancy", "Utility"], role="Utility", target="corpse",
              radius=1.0, quirky=True,
              impact_vfx=[rune(1.0, 90, byRadius=False), spiral("skull", 10, 1.2, 0.5), glow(1.5, 2.5, 50, useColor2=True)],
              effects=[rite(maxCorpses=1, humanlikeOnly=True, consume=True, effects=[research(450)])],
              flavor="Every scholar who dies takes a library with them. Not any more."),
        Spell("HollowHeart", "hollow heart",
              "Lock an ally's feelings away in your phylactery for two days. A mental break that is not violent ends at once, and no new break "
              "can begin while the heart is hollow. The price: they care half as much about anyone (their opinions of others count for half), "
              "recreation does them half as much good, and their words carry 30% less weight. Once a day.",
              icon=("heartcrack", "snowflake"), mana=40, cd=DAY_S, range=20, warmup=1.5, tags=["Necromancy", "Frost"], role="Utility",
              target="ally", quirky=True,
              impact_vfx=[ring(0.6, 0.1, 30, byRadius=False), burst("snow", 12, 1.0, 0.5), glow(1.0, 1.6, 30)],
              effects=[calm(), hediff("AA_NecroHollowHeart", 1.0)],
              flavor="It's easier this way. You'll see."),
        Spell("RimeBridge", "rime bridge",
              "Breathe the cold of the tomb over water. Every cell of water or marsh within 3.5 cells of the target freezes into thick ice "
              "that never thaws, firm enough to walk and haul across: ford a river, reach an island, cross a lake or close a moat.",
              icon=("snowflake", "wave"), mana=40, cd=DAY_S / 2, range=26, warmup=2.0, tags=["Necromancy", "Frost", "Utility"],
              role="Utility", target="cell", radius=3.5, quirky=True,
              impact_vfx=[ring(0.2, 1.0, 40), burst("snow", 30, 2.0, 0.6, byRadius=True), glow(2.0, 3.5, 40)],
              effects=[terraform("Ice", fromOnly=WATER)]),
    )
    li.add_nodes(
        node("HollowHeart", (0, 2), ["GraveChill"]),
        node("RimeBridge", (2, 2), ["ColdBlood"]),
        node("MindHarvest", (5, 3), ["SoulFeast"]),
        Node("WhispersOfTheDead", "whispers of the dead", (4, 2), kind="Notable", pre=["ColdBlood"],
             passives=[P("P_Aura", radius=9.0, interval=600, affects="Allies", color=LICHC,
                         effects=[hediff("AA_NecroWhispers", 1.0, duration=900)])],
             desc="The dead whisper what they once knew. Everyone within 9 cells of you researches 25% faster."),
    )
    extra.append(status("AA_NecroHollowHeart", "hollow heart", "Their feelings are locked away in a lich's phylactery. No break can reach them, "
        "but neither can much else.", "#9ad8ff", bad=False, ticks=DAY * 2, fx=dict(emitter="Chill", emitInterval=40),
        stages=[stage(factors={"JoyGainFactor": 0.5, "SocialImpact": 0.7}, extra={"blocksMentalBreaks": True, "opinionOfOthersFactor": 0.5})]))
    extra.append(status("AA_NecroWhispers", "whispers of the dead", "The dead whisper what they once knew.", "#9ad8ff", bad=False, ticks=900,
        stages=[stage(factors={"ResearchSpeed": 1.25})]))


# ------------------------------------------------------------------------------------------------ Bonelord

def _bonelord(t, extra):
    bl = t["Bonelord"]
    bl.add_spells(
        Spell("BoneScaffold", "bone scaffold",
              "Raise a scaffold of living bone for a day. Every hour its many hands put 300 work (more with healing power) into every "
              "construction frame within 7 cells that already has all its materials, finishing them one after another with your own "
              "Construction skill. Once a day.",
              icon=("pillar", "bone"), mana=45, cd=DAY_S, range=20, warmup=2.0, tags=["Necromancy", "Construct", "Utility"], role="Utility",
              target="cell", quirky=True,
              impact_vfx=[burst("bone", 16, 2.0, 0.6), ring(0.3, 0.8, 24, byRadius=False), smoke(3, 1.0)],
              effects=[spawn("AA_Construct_BoneScaffold", lifetimeTicks=DAY)],
              flavor="Many hands make light work. These have a great many hands."),
        Spell("GraveDiggers", "grave diggers",
              "Skeletal hands claw up through the rock. Every natural rock wall and ore vein within 2.5 cells of the target is dug out as if "
              "you had mined it yourself: stone chunks and ore drop where it stood. Twice a day.",
              icon=("hand", "rock"), mana=30, cd=DAY_S / 2, range=24, warmup=2.0, tags=["Necromancy", "Utility"], role="Utility", target="cell",
              radius=2.5, quirky=True,
              impact_vfx=[burst("bone", 18, 2.0, 0.6, byRadius=True), smoke(6, 1.2), ring(0.2, 1.0, 30)],
              effects=[mine()]),
        Spell("MarrowBombs", "marrow bombs",
              "Raise up to three corpses as hollow shamblers packed with splintered bone. For twenty seconds they run at your enemies; when "
              "they are cut down, or the binding ends, they burst in a storm of shards that hits everyone within 2.5 cells, friend or foe, for "
              "30 damage. Nothing is left to raise again.",
              icon=("bone", "burst"), mana=36, cd=25, range=26, warmup=1.0, tags=["Necromancy", "Summon"], role="Summon", target="corpse",
              radius=5.0,
              impact_vfx=[rune(1.0, 50), burst("bone", 14, 2.0, 0.6, byRadius=True), smoke(4, 1.0)],
              effects=[raise_dead(3, 1200, undeadHediff="AA_NecroMarrowBomb")]),
    )
    bl.add_nodes(
        node("BoneScaffold", (4, 1), ["Ossify"]),
        node("GraveDiggers", (6, 2), ["DarkCommand"]),
        node("MarrowBombs", (2, 4), ["BonePrison"]),
    )
    extra += things.totem("Construct_BoneScaffold", "bone scaffold", "A lattice of living bone with far too many hands. It builds.",
                          color="#e8e2c8", color2="#6a5f40", hp=300, lifetime=DAY, pulse=[construct(300)], radius=7.0, interval=HOUR,
                          affects="Allies", glow=1.2, size=1.4)
    extra.append(status("AA_NecroMarrowBomb", "marrow bomb", "A hollow shambler packed with splintered bone. It will burst when it falls.",
        "#e8e2c8", bad=False, fx=dict(emitter="Undead", emitInterval=20),
        comps=[O("ArcanumAscendant.HediffCompProperties_DeathBurst", damageDef="AA_Necrotic", damage=30.0, radius=2.5, color=rgb("#e8e2c8") + (1.0,)),
               O("ArcanumAscendant.HediffCompProperties_Summoned", vanishOnDeath=True, killOnExpire=True),
               aura("#e8e2c8", size=1.2, fleck="AA_Fleck_Bone", interval=30)],
        stages=[stage(factors={"MoveSpeed": 1.4}, offsets={"PainShockThreshold": 0.9, "MentalBreakThreshold": -1.0},
                      extra={"painFactor": 0.0, "disablesNeeds": UNDEAD_NEEDS, "blocksMentalBreaks": True})]))


# ------------------------------------------------------------------------------------------------ Plaguebringer

def _plague(t, extra):
    pb = t["Plaguebringer"]
    pb.add_spells(
        Spell("Variolation", "variolation",
              "Give an ally a measured dose of plague. They run a fever for a day (-15% consciousness, -10% moving), and then their body "
              "remembers: for the next 19 days they build immunity 50% faster and resist toxins 25% better. With Biotech the lesson goes into "
              "their blood for good: they gain the strong immunity gene. Once every two days.",
              icon=("tube", "drop"), mana=45, cd=DAY_S * 2, range=6, warmup=3.0, tags=["Necromancy", "Plague", "Utility"], role="Buff",
              target="ally", quirky=True,
              impact_vfx=[spiral("smoke", 8, 1.0, 0.5), burst("mote", 10, 1.0, 0.4), glow(1.0, 1.8, 30)],
              effects=[hediff("AA_NecroVariolated", 1.0), gene("Immunity_Strong")],
              flavor="A little plague now, so the big one finds the door locked."),
        Spell("CarrionBrood", "carrion brood",
              "Loose your carrion beetles on the dead. Up to four bodies within 3 cells of the target are eaten down to nothing, and the "
              "beetles leave behind what they make of them: 10 to 16 insect jelly per body, a sweet food that never spoils. The gear is left "
              "behind. It spares the colony's own dead, and dried-out husks have nothing left to give. Twice a day.",
              icon=("bug", "drop"), mana=35, cd=DAY_S / 2, range=24, warmup=2.0, tags=["Necromancy", "Plague", "Utility"], role="Utility",
              target="corpse", radius=3.0, quirky=True,
              impact_vfx=[burst("smoke", 14, 1.0, 1.0, byRadius=True), ring(0.3, 0.8, 24, byRadius=False)],
              effects=[rite(maxCorpses=4, consume=True, effects=[conjure("InsectJelly", 10, 16)])],
              flavor="Waste not, want not. The beetles certainly don't."),
        Spell("PlagueVessel", "plague vessel",
              "Make an ally the plague's vessel for thirty seconds. Every two seconds every enemy within 3 cells of them catches the black "
              "plague, while the vessel itself is spared. An undead thrall makes a perfect vessel: send it into the thick of them.",
              icon=("drop", "hand"), mana=30, cd=30, range=24, warmup=0.6, tags=["Necromancy", "Plague"], role="Buff", target="ally",
              impact_vfx=[ring(0.2, 1.0, 26, byRadius=False), spiral("smoke", 12, 2.0, 0.8)],
              effects=[hediff("AA_NecroPlagueVessel", 1.0)]),
    )
    pb.add_nodes(
        node("CarrionBrood", (0, 2), ["Miasma"]),
        node("Variolation", (4, 1), ["Virulence"]),
        node("PlagueVessel", (1, 4), ["Carrier"]),
        Node("PlagueDoctor", "plague doctor", (5, 3), kind="Notable", pre=["Outbreak"],
             passives=[P("P_Aura", radius=9.0, interval=600, affects="Allies", color=PLAGUEC,
                         effects=[hediff("AA_NecroPlagueDoctor", 1.0, duration=900)])],
             desc="A plague doctor at the bedside. Allies within 9 cells of you (colonists, prisoners and animals) build immunity 35% faster: "
                  "sleep in the hospital during an epidemic."),
    )
    extra.append(status("AA_NecroVariolated", "variolated", "A measured dose of plague: first a fever, then a body that knows how to fight.",
        "#a8d84a", bad=False, initSev=1.0, maxSev=1.0, comps=[O("HediffCompProperties_SeverityPerDay", severityPerDay=-0.05)],
        stages=[stage(label="hardened", factors={"ImmunityGainSpeed": 1.5}, offsets={"ToxicResistance": 0.25}),
                stage(label="fever", min_sev=0.95, factors={"ImmunityGainSpeed": 1.5}, offsets={"ToxicResistance": 0.25},
                      caps={"Consciousness": -0.15, "Moving": -0.1})]))
    extra.append(status("AA_NecroPlagueVessel", "plague vessel", "The black plague rides this body and leaps to every enemy nearby.",
        "#a8d84a", bad=False, ticks=1800, fx=dict(emitter="Plague", emitInterval=12),
        comps=[periodic(120, area=[hediff("AA_Plague", 0.5, stack=True, maxSeverity=5.0, affects="Enemies")], radius=3.0, color="#a8d84a")]))
    extra.append(status("AA_NecroPlagueDoctor", "plague doctor's care", "A plaguebringer is near, and the body fights sickness harder.",
        "#a8d84a", bad=False, ticks=900, stages=[stage(factors={"ImmunityGainSpeed": 1.35})]))


def extend(t, extra):
    _base(t, extra)
    _lich(t, extra)
    _bonelord(t, extra)
    _plague(t, extra)

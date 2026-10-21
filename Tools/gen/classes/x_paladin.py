"""Colony expansion of the Paladin: mercy and judgment over the fallen, a light to grow by, bread for everyone, oaths and
banners at home, a cleric's sacraments and an inquisitor who keeps the colony's minds and fires in order."""
import re

from classes.common import *  # noqa: F401,F403
from classes.prims_family import *  # noqa: F401,F403
from classes.prims_colony import *  # noqa: F401,F403
from classes import c_paladin as M  # noqa: F401  (the class module: schools and visuals)

KEYS = {}

HOUR_S = HOUR / SEC  # one in-game hour in seconds of game time at 1x
DAY_S = DAY / SEC

GOLD = (1.0, 0.89, 0.54, 1.0)
TEMPLARC = (1.0, 0.81, 0.35, 1.0)
CLERICC = (0.72, 1.0, 0.85, 1.0)


def verdict(kind, **kw):
    """A paladin's verdict over a downed enemy (Colony_Paladin.cs): "Quarter" (spared, released, goodwill) or "Pyre" (burned
    where they lie, the pyre bursting over the enemies around them)."""
    return O("Effect_PaladinVerdict", verdict=kind, **kw)


def guardian_oath(factor=0.5, color="#ffcf5a"):
    """Status comp: a damage shell whose turned-aside part strikes the paladin who swore the oath (Colony_Paladin.cs)."""
    return O("ArcanumAscendant.HediffCompProperties_DamageShell", compClass="ArcanumAscendant.HediffComp_PaladinGuardianOath",
             damageFactor=float(factor), color=rgb(color) + (1.0,))


# ------------------------------------------------------------------------------------------------ base tree

def _base(t, extra):
    p = t["Paladin"]
    p.add_spells(
        Spell("Quarter", "quarter",
              "Kneel beside a downed enemy and spare them. Their wounds are tended and healed for 30, their pain is dulled to 40% for "
              "twelve hours, and they are taken captive and released on the spot: no longer hostile, they walk home as soon as they can "
              "stand. Their faction warms to you (+12 goodwill, though factions that can never make peace do not care), and you feel the "
              "better for it (+4 mood for two days). Only the downed are judged; anyone still standing is left alone.",
              icon=("hand", "feather"), mana=30, cd=HOUR_S * 6, range=4, warmup=3.0, tags=["Holy", "Utility"], role="Utility",
              target="enemy", affects="Enemies", quirky=True, hostile=False, autocast=False,
              impact_vfx=[pillar(1.0, 40), burst("feather", 14, 1.2, 0.6), glow(1.2, 2.2, 30, useColor2=True)],
              effects=[verdict("Quarter", goodwill=12, casterThought="AA_PaladinMercyShown",
                               spareEffects=[heal(30, tend=True), hediff("AA_PaladinQuartered", 1.0)])]),
        Spell("LanternOfDawn", "lantern of dawn",
              "Hang a small sun over the target for three days. It lights the ground like a sun lamp: crops within about 6 cells grow as "
              "in full daylight, at night and under a roof alike (they still need warmth), and nobody there works in the dark. Allies "
              "within 7 cells of it also work 10% faster. Grow food in a sealed hall through the winter, or keep the harvest going all "
              "night.", icon=("sun", "brazier"),
              mana=60, cd=DAY_S * 3, range=24, warmup=4.0, tags=["Holy", "Utility"], role="Utility", target="cell", quirky=True,
              impact_vfx=[pillar(1.4, 60), flash(4.0), ring(0.2, 1.0, 40, byRadius=False), burst("star", 20, 2.0, 0.6)],
              effects=[spawn("AA_Construct_PaladinDawnLantern", lifetimeTicks=DAY * 3)]),
        Spell("BeaconForTheLost", "beacon for the lost",
              "Light a beacon that only the lost can see. Within moments someone answers: a wanderer walks in asking to join the colony, "
              "or an escape pod falls nearby with a wounded refugee inside. Once every ten days.", icon=("star", "door"),
              mana=90, cd=DAY_S * 10, warmup=6.0, tags=["Holy", "Utility"], role="Utility", target="self", quirky=True,
              impact_vfx=[pillar(2.5, 120), flash(5.0), ring(0.1, 2.0, 60, byRadius=False), burst("star", 30, 3.0, 0.7)],
              effects=[incident(["WandererJoin", "RefugeePodCrash"])]),
        Spell("BreakingOfBread", "breaking of bread",
              "Bless a single loaf and break it among everyone within 5 cells, and there is always enough. Every person there, prisoners "
              "too, eats until full without a crumb leaving the larder, and animals get a hearty meal. They remember sharing it (+4 mood "
              "for a day). Once a day: a famine can be survived on this.", icon=("hand", "pie"),
              mana=50, cd=DAY_S, warmup=5.0, tags=["Holy", "Utility"], role="Utility", target="self", delivery="Nova", radius=5.0,
              affects="Allies", quirky=True,
              impact_vfx=[ring(0.1, 1.0, 40), glow(1.6, 3.0, 40, useColor2=True), burst("star", 16, 1.6, 0.5, byRadius=True)],
              effects=[need("Food", 1.0), memory("AA_PaladinBrokeBread")]),
        Spell("BearTheBurden", "bear the burden",
              "Take another's weariness onto your own shoulders. They regain 50% rest at once and feel only half their pain for 12 hours; "
              "you lose 35% rest and ache in their place for those 12 hours (+10% pain, 5% slower). They think better of you for it (+12 "
              "opinion for six days). Let the doctor finish the surgery, or the builder close the wall before the cold.",
              icon=("hand", "shield"), mana=20, cd=HOUR_S * 4, range=3, warmup=2.0, tags=["Holy", "Utility"], role="Buff", target="ally",
              affects="AlliesNotSelf", quirky=True,
              impact_vfx=[glow(1.2, 2.0, 30, useColor2=True), burst("feather", 8, 1.0, 0.5)],
              effects=[need("Rest", 0.5), hediff("AA_PaladinBurdenLifted", 1.0), memory("AA_PaladinCarriedMyBurden", toCaster=True)],
              caster_effects=[need("Rest", -0.35), hediff("AA_PaladinBurdenBorne", 1.0)]),
        Spell("RebukeTheUnhallowed", "rebuke the unhallowed",
              "Raise your holy symbol and let the light judge what should not walk. Every enemy within 7 cells is seared: the undead and "
              "anything summoned take about 78 holy damage, the creatures of the dark (Anomaly's entities) about 75 and mechanoids 15, "
              "while living people and beasts take only a stinging 12. Break an enemy necromancer's army without burning its prisoners.",
              icon=("cross", "skull"), mana=40, cd=30, warmup=0.8, tags=["Holy"], role="Attack", target="self", delivery="Nova",
              radius=7.0, affects="Enemies", tier=3, ai_min=1,
              impact_vfx=[flash(5.0), ring(0.1, 1.2, 36), pillar(1.6, 40), burst("star", 24, 4.0, 0.6, byRadius=True)],
              effects=[dmg(30, "AA_Holy", vsHumanlikes=0.2, vsAnimals=0.2, vsMechanoids=0.5, vsEntities=2.5,
                           bonusVsHediff="AA_Undead", bonusVsHediffFactor=12.0),
                       dmg(30, "AA_Holy", vsHumanlikes=0.2, vsAnimals=0.2, vsMechanoids=0.0, vsEntities=0.0,
                           bonusVsHediff="AA_Summoned", bonusVsHediffFactor=12.0, noCrit=True)]),
    )
    p.add_nodes(
        Node("BearTheBurden", None, (4, 1), kind="Spell", spell="BearTheBurden", pre=["Fortitude", "Mercy"]),
        Node("Quarter", None, (5, 2), kind="Spell", spell="Quarter", pre=["LayOnHands"]),
        Node("BreakingOfBread", None, (6, 3), kind="Spell", spell="BreakingOfBread", pre=["HealingHands"]),
        Node("LanternOfDawn", None, (3, 4), kind="Spell", spell="LanternOfDawn", pre=["Bulwark"]),
        Node("RebukeTheUnhallowed", None, (1, 4), kind="Spell", spell="RebukeTheUnhallowed", pre=["Zealot"]),
        Node("Hearthguard", "hearthguard", (5, 4), kind="Notable", pre=["Purify"],
             passives=[P("P_Aura", radius=10.0, interval=HOUR // 2, affects="Allies", color=GOLD,
                         effects=[memory("AA_PaladinHearthguard"), hediff("AA_PaladinSteadied", 1.0)])],
             desc="Home feels safe while you are in it. Every half hour, colonists, guests and prisoners within 10 cells of you feel "
                  "sheltered (+3 mood for a few hours) and steadier (mental break threshold 4% lower for the next hour), drafted or not."),
        Node("BeaconForTheLost", None, (6, 6), kind="Spell", spell="BeaconForTheLost", pre=["Martyr"]),
    )
    extra.append(status("AA_PaladinQuartered", "granted quarter",
                        "Spared by a paladin. The pain is dulled enough to walk home.", "#ffe28a", bad=False, ticks=HOUR * 12,
                        stages=[stage(pain=0.4)], fx=dict(emitter="Holy", emitInterval=60)))
    extra.append(thought_def("AA_PaladinMercyShown", "showed mercy",
                             "I spared a fallen enemy and sent them home. The light asks nothing less.", 4, days=2))
    extra.append(status("AA_PaladinDawnlit", "dawnlit", "Working in the light of a paladin's lantern.", "#ffe9a8", bad=False,
                        ticks=400, stages=[stage(factors={"WorkSpeedGlobal": 1.1})]))
    lantern = things.totem("Construct_PaladinDawnLantern", "lantern of dawn",
                           "A small sun hung in the air by a paladin. It lights the ground like a sun lamp, so crops grow beneath it at "
                           "night and under a roof, and everyone working in its light works a little faster.",
                           color="#ffe9a8", color2="#ffffff", hp=300, lifetime=DAY * 3, shape="brazier", radius=7.0, interval=250,
                           affects="Allies", pulse=[hediff("AA_PaladinDawnlit", 1.0)], light=14.0, glow=2.4, size=1.2)
    sun = "<overlightRadius>7</overlightRadius>\n        <glowRadius>14</glowRadius>\n        <glowColor>(370,370,370,0)</glowColor>"
    out = []
    for x in lantern:
        if "<glowRadius>14</glowRadius>" in x:
            x = re.sub(r"<glowRadius>14</glowRadius>\s*<glowColor>[^<]*</glowColor>", sun, x, count=1)
        out.append(x)
    assert any("(370,370,370,0)" in x for x in out), "dawn lantern glower not set"
    extra += out
    extra.append(thought_def("AA_PaladinBrokeBread", "broke bread together",
                             "The paladin blessed one loaf and there was enough for all of us.", 4, days=1))
    extra.append(status("AA_PaladinBurdenLifted", "burden lifted",
                        "A paladin took this weariness and pain onto their own shoulders.", "#ffe28a", bad=False, ticks=HOUR * 12,
                        stages=[stage(pain=0.5)], fx=dict(emitter="Holy", emitInterval=60)))
    extra.append(status("AA_PaladinBurdenBorne", "bearing a burden",
                        "Carrying someone else's weariness and pain.", "#c8b070", bad=True, ticks=HOUR * 12,
                        stages=[stage(factors={"MoveSpeed": 0.95}, extra={"painOffset": 0.1})]))
    extra.append(opinion_def("AA_PaladinCarriedMyBurden", "carried my burden", 12, days=6))
    extra.append(thought_def("AA_PaladinHearthguard", "sheltered by the light",
                             "The paladin is home. Nothing will get past them.", 3, days=0.12))
    extra.append(status("AA_PaladinSteadied", "steadied", "A paladin is near. It is easier to hold together.", "#ffe28a", bad=False,
                        ticks=HOUR, stages=[stage(offsets={"MentalBreakThreshold": -0.04})]))


# ------------------------------------------------------------------------------------------------ Templar

def _templar(t, extra):
    tp = t["Templar"]
    tp.add_spells(
        Spell("GuardiansOath", "guardian's oath",
              "Swear to stand between an ally and harm for a day. Every blow that lands on them is halved, and the other half strikes "
              "you instead, wherever you are on the map, through your own armour and ward. The oath breaks if you are downed, die or "
              "leave the map. Swear it over the children before the raid, or over the only doctor.",
              icon=("shield", "hand"), mana=35, cd=60, range=20, warmup=0.5, tags=["Holy", "Ward"], role="Buff", target="ally",
              affects="AlliesNotSelf", quirky=True,
              impact_vfx=[hexshield(2.4, 40), flash(2.5), burst("star", 12, 1.6, 0.6)],
              effects=[hediff("AA_PaladinGuardianOath", 1.0)]),
        Spell("BannerOfTheOrder", "banner of the order",
              "Plant the order's banner for a day. Everyone of yours within 8 cells of it stands taller: +3 mood, a mental break "
              "threshold 8% lower, pain has to be 10% worse to drop them and they aim 10% quicker. Plant it in the dining hall in a hard "
              "winter, or behind the barricade when the raid comes.", icon=("crown", "pillar"),
              mana=45, cd=DAY_S, range=12, warmup=3.0, tags=["Holy", "Utility"], role="Buff", target="cell", quirky=True,
              impact_vfx=[pillar(1.2, 50), flash(3.0), ring(0.2, 1.0, 30, byRadius=False), burst("star", 16, 2.0, 0.6)],
              effects=[spawn("AA_Construct_PaladinBanner", lifetimeTicks=DAY)]),
        Spell("PilgrimsBlessing", "pilgrim's blessing",
              "Bless travellers before the road. For three days everyone of yours within 4 cells walks 10% faster, gets hungry 30% "
              "slower, tires 20% slower and stays comfortable 8 C further into the cold and the heat. Bless a caravan before it leaves, "
              "or the hunting party.", icon=("wing", "star"),
              mana=40, cd=DAY_S, range=12, warmup=3.0, tags=["Holy", "Utility"], role="Buff", target="ally_area", radius=4.0, quirky=True,
              impact_vfx=[ring(0.1, 1.0, 40), burst("feather", 16, 1.6, 0.6, byRadius=True), glow(1.4, 2.6, 30, useColor2=True)],
              effects=[hediff("AA_PaladinPilgrim", 1.0)]),
    )
    tp.add_nodes(
        Node("PilgrimsBlessing", None, (2, 1), kind="Spell", spell="PilgrimsBlessing", pre=["Judgment"]),
        Node("Squires", "squires", (2, 2), kind="Notable", pre=["Vanguard"],
             passives=[P("P_Aura", radius=8.0, interval=HOUR // 2, affects="Allies", color=TEMPLARC,
                         effects=[hediff("AA_PaladinSquire", 1.0)])],
             desc="Everyone near a templar learns by watching. Allies within 8 cells of you learn every skill 25% faster, drafted or not."),
        Node("GuardiansOath", None, (6, 2), kind="Spell", spell="GuardiansOath", pre=["ShieldOfFaith"]),
        Node("BannerOfTheOrder", None, (4, 4), kind="Spell", spell="BannerOfTheOrder", pre=["Unbreakable"]),
    )
    extra.append(status("AA_PaladinGuardianOath", "guardian's oath",
                        "A templar has sworn to stand between this one and harm: half of every blow is turned aside onto the templar.",
                        "#ffcf5a", bad=False, ticks=DAY, comps=[guardian_oath(0.5, "#ffcf5a")], fx=dict(emitter="Ward", emitInterval=50)))
    extra += things.totem("Construct_PaladinBanner", "banner of the order",
                          "A templar's banner, planted and blessed. Those who stand near it stand taller.",
                          color="#ffcf5a", color2="#fff6d8", hp=250, lifetime=DAY, shape="totem", radius=8.0, interval=250,
                          affects="Allies", pulse=[hediff("AA_PaladinUnderBanner", 1.0), memory("AA_PaladinBannerPride")], glow=1.6)
    extra.append(status("AA_PaladinUnderBanner", "under the banner", "Standing near the order's banner.", "#ffcf5a", bad=False,
                        ticks=400, stages=[stage(offsets={"MentalBreakThreshold": -0.08, "PainShockThreshold": 0.1},
                                                 factors={"AimingDelayFactor": 0.9})]))
    extra.append(thought_def("AA_PaladinBannerPride", "under the order's banner",
                             "The templar's banner is flying over us. We will not break.", 3, days=0.25))
    extra.append(status("AA_PaladinPilgrim", "pilgrim's blessing", "Blessed for the road: lighter on the feet, slower to hunger and tire.",
                        "#ffcf5a", bad=False, ticks=DAY * 3,
                        stages=[stage(factors={"MoveSpeed": 1.1}, offsets={"ComfyTemperatureMin": -8, "ComfyTemperatureMax": 8},
                                      extra={"hungerRateFactor": 0.7, "restFallFactor": 0.8})]))
    extra.append(status("AA_PaladinSquire", "squire", "Learning by watching a templar.", "#ffcf5a", bad=False, ticks=HOUR,
                        stages=[stage(offsets={"GlobalLearningFactor": 0.25})]))


# ------------------------------------------------------------------------------------------------ Cleric

def _cleric(t, extra):
    cl = t["Cleric"]
    cl.add_spells(
        Spell("Communion", "communion",
              "Share the sacrament of your own blood. Whoever receives it feels blessed (+6 mood for two days). A sanguophage (Biotech) is "
              "also fed 40 hemogen, and nobody has to be bitten. You are a little pale for 8 hours afterwards (5% less blood pumping, "
              "tiring 20% faster). Once a day.", icon=("drop", "cross"),
              mana=30, cd=DAY_S, range=3, warmup=4.0, tags=["Holy", "Healing", "Utility"], role="Buff", target="ally",
              affects="AlliesNotSelf", quirky=True,
              impact_vfx=[glow(1.2, 2.2, 40, useColor2=True), burst("feather", 10, 1.0, 0.5), burst("star", 8, 1.0, 0.4)],
              effects=[hemogen(0.4), memory("AA_PaladinCommunion")], caster_effects=[hediff("AA_PaladinGaveBlood", 1.0)]),
        Spell("PeaceOfTheWild", "peace of the wild",
              "Walk into the madness with open hands. Every creature within 9 cells that has been driven mad (a manhunting pack, an animal "
              "out for revenge, a psychic insanity pulse) or anyone in a berserk rage comes back to itself at once. Wild animals then "
              "wander off in peace. Once every two days.", icon=("paw", "feather"),
              mana=50, cd=DAY_S * 2, range=24, warmup=2.0, tags=["Holy", "Utility"], role="Utility", target="cell", radius=9.0,
              affects="Enemies", quirky=True, hostile=False,
              impact_vfx=[ring(0.1, 1.0, 50), glow(2.0, 4.0, 40, useColor2=True), burst("feather", 24, 2.0, 0.6, byRadius=True)],
              effects=[calm(violent=True, affects="Enemies")]),
        Spell("Christening", "christening",
              "Bless someone in water and light before their family. It is meant for babies and children (Biotech), but anyone can be "
              "blessed. For thirty days they fight off disease 30% faster and stay comfortable 10 C further into the cold and the heat, "
              "enough to carry a newborn through its first winter. Everyone within 5 cells who sees it is moved (+5 mood for two days).",
              icon=("drop", "star"), mana=35, cd=DAY_S * 3, range=3, warmup=5.0, tags=["Holy", "Healing", "Utility"], role="Buff",
              target="ally", quirky=True,
              impact_vfx=[pillar(1.0, 50), glow(1.4, 2.6, 40, useColor2=True), burst("feather", 14, 1.2, 0.5)],
              effects=[hediff("AA_PaladinChristened", 1.0), memory("AA_PaladinChristening", radius=5.0)]),
    )
    cl.add_nodes(
        Node("Christening", None, (4, 1), kind="Spell", spell="Christening", pre=["Grace"]),
        Node("Communion", None, (2, 2), kind="Spell", spell="Communion", pre=["MassRegeneration", "Serenity"]),
        Node("PeaceOfTheWild", None, (6, 2), kind="Spell", spell="PeaceOfTheWild", pre=["Sanctuary"]),
        Node("GuidingHands", "guiding hands", (4, 3), kind="Notable", pre=["Restoration", "HallowedDome"],
             passives=[P("P_Aura", radius=8.0, interval=HOUR // 4, affects="Allies", color=CLERICC,
                         effects=[hediff("AA_PaladinGuidedHands", 1.0)])],
             desc="Healers work better with a cleric at their shoulder. Allies within 8 cells of you tend 15% better and 15% faster, and "
                  "their surgeries are 15% more likely to succeed, drafted or not."),
    )
    extra.append(thought_def("AA_PaladinCommunion", "took communion", "The cleric shared the sacrament with me. I feel clean and held.", 6,
                             days=2))
    extra.append(status("AA_PaladinGaveBlood", "gave of their blood", "Pale after sharing the sacrament of their own blood.", "#d0a0a0",
                        bad=True, ticks=HOUR * 8, stages=[stage(caps={"BloodPumping": -0.05}, extra={"restFallFactor": 1.2})]))
    extra.append(status("AA_PaladinChristened", "christened", "Blessed in water and light: hardier against sickness and the weather.",
                        "#b8ffd8", bad=False, ticks=DAY * 30,
                        stages=[stage(factors={"ImmunityGainSpeed": 1.3}, offsets={"ComfyTemperatureMin": -10, "ComfyTemperatureMax": 10})]))
    extra.append(thought_def("AA_PaladinChristening", "witnessed a christening",
                             "The cleric blessed one of us in water and light. It was beautiful.", 5, days=2))
    extra.append(status("AA_PaladinGuidedHands", "guided hands", "A cleric is close by, steadying the healer's hands.", "#b8ffd8",
                        bad=False, ticks=HOUR // 2 + 200,
                        stages=[stage(offsets={"MedicalTendQuality": 0.15, "MedicalTendSpeed": 0.15, "MedicalSurgerySuccessChance": 0.15})]))


# ------------------------------------------------------------------------------------------------ Inquisitor

def _inquisitor(t, extra):
    iq = t["Inquisitor"]
    iq.add_spells(
        Spell("SealOfTheMind", "seal of the mind",
              "Seal the minds of everyone of yours within 5 cells against the whispers. For three days they are as if psychically deaf: "
              "a psychic drone, a psychic ship's wail or a hostile psycaster's touch cannot reach them (nor can a psychic soothe, and "
              "psycasters among them cannot cast). Gather the colony at dinner when the drone begins.", icon=("eye", "seal"),
              mana=50, cd=DAY_S * 3, range=20, warmup=3.0, tags=["Holy", "Utility"], role="Buff", target="ally_area", radius=5.0,
              quirky=True,
              impact_vfx=[rune(1.0, 60), ring(0.1, 1.0, 40), burst("star", 16, 1.6, 0.5, byRadius=True)],
              effects=[hediff("AA_PaladinSealedMind", 1.0)]),
        Spell("TheFireObeys", "the fire obeys",
              "Command the flames. Every fire within 8 cells goes out at once, and any ally burning there is put out; then the fire leaps "
              "onto every enemy in the area instead and sets them ablaze. You drink the heat (+12 mana). Save the storeroom from a "
              "short-circuit fire, or hand the raiders back their own molotovs.", icon=("flame", "hand"),
              mana=35, cd=HOUR_S, range=30, warmup=1.0, tags=["Holy", "Fire", "Utility"], role="Utility", target="cell", radius=8.0,
              affects="Enemies", quirky=True, hostile=True,
              impact_vfx=[ring(0.1, 1.0, 40), burst("ember", 30, 2.5, 0.6, byRadius=True), smoke(4, 1.0)],
              effects=[extinguish(affects="Allies"), ignite(0.0, 0.6, True, affects="Enemies")], caster_effects=[mana(12)]),
        Spell("AutoDaFe", "auto-da-fe",
              "Pass sentence on a fallen foe. A downed enemy is burned at the stake where they lie, killed outright, and the pyre bursts "
              "over every enemy within 5 cells: 30 holy damage, a mark of judgment, a stagger and even odds of catching fire. Anyone still "
              "standing is not judged. The other face of quarter.", icon=("flame", "cross"),
              mana=40, cd=30, range=20, warmup=1.5, tags=["Holy", "Fire"], role="Attack", target="enemy", affects="Enemies", tier=3,
              autocast=False,
              impact_vfx=[pillar(1.6, 50), flash(3.5)],
              effects=[verdict("Pyre", killDamageDef="AA_Holy", burstRadius=5.0,
                               burstVfx=[flash(5.0), ring(0.1, 1.1, 30), burst("ember", 30, 3.5, 0.7, byRadius=True),
                                         burst("star", 16, 3.0, 0.6, byRadius=True)],
                               burstEffects=[dmg(30, "AA_Holy", affects="Enemies"), hediff("AA_Marked", 1.0, affects="Enemies"),
                                             stun(45, affects="Enemies"), hediff("AA_Burning", 1.0, chance=0.5, affects="Enemies")])]),
    )
    iq.add_nodes(
        Node("TheFireObeys", None, (4, 2), kind="Spell", spell="TheFireObeys", pre=["Interrogator"]),
        Node("SealOfTheMind", None, (6, 2), kind="Spell", spell="SealOfTheMind", pre=["Silence"]),
        Node("AutoDaFe", None, (2, 4), kind="Spell", spell="AutoDaFe", pre=["Banishment"]),
    )
    extra.append(status("AA_PaladinSealedMind", "sealed mind", "An inquisitor's seal: no psychic whisper reaches this mind, good or ill.",
                        "#ff9a3c", bad=False, ticks=DAY * 3, stages=[stage(factors={"PsychicSensitivity": 0.0})],
                        fx=dict(emitter="Watched", emitInterval=90)))


def extend(t, extra):
    _base(t, extra)
    _templar(t, extra)
    _cleric(t, extra)
    _inquisitor(t, extra)

"""
The Harbinger: a prophet of doom. Every spell lays Omens on its victims; at seven Omens the Seventh Seal breaks, dealing
damage that ignores armour, scales with the victim's own vitality and scatters Omens onto everyone nearby, so one
detonation can set off the next across a whole raid.
"""
from classes.common import *  # noqa: F401,F403
from classes.expansion2 import missiles, channel, delayed  # noqa: F401

CRIMSON, BONE = "#c8323c", "#f0e6d2"
DOOM = School("Doom", CRIMSON, BONE, fleck="ember", fleck2="smoke", projectile="ArcanumAscendant/Vfx/CrowBolt")
SAYER = School("Doom", "#e0284a", "#ffd7a8", fleck="ember", fleck2="star", projectile="ArcanumAscendant/Vfx/CrowBolt")
RIDER = School("Doom", "#cfe8d6", "#4f6f62", fleck="smoke", fleck2="mote", projectile="ArcanumAscendant/Vfx/CrowBolt")
CATA = School("Doom", "#ff6a28", "#52100a", fleck="ember", fleck2="smoke", projectile="ArcanumAscendant/Vfx/Meteor")

OMEN = "AA_Omen"
SEAL = "ArcanumAscendant/Vfx/OmenSeal"

KEYS = {
    "AA_SeventhSeal": "The Seventh Seal breaks",
    "AA_ProphecyFulfilled": "As it was foretold",
    "AA_SentenceCarriedOut": "Sentence carried out",
    "AA_FateRewritten": "Fate rewritten",
    "AA_Seal1": "The First Seal: Conquest",
    "AA_Seal2": "The Second Seal: War",
    "AA_Seal3": "The Third Seal: Famine",
    "AA_Seal4": "The Fourth Seal: Death",
    "AA_Seal5": "The Fifth Seal: Martyrs",
    "AA_Seal6": "The Sixth Seal: The Great Quake",
    "AA_Seal7": "The Seventh Seal: Silence",
    "AA_Doomsday": "Midnight",
}


def omen(n=1, **kw):
    """n Omens on the target. Each new Omen refreshes the others for 25 seconds."""
    return stacks(OMEN, n, duration=1500, cap=12, **kw)


DOOM_HIT = [flash(2.2), burst("ember", 8, 2.0, 0.5), glow(1.2, 2.2, 20, useColor2=True)]
SEAL_RUNE = rune(1.0, 50, tex=SEAL)


def statuses():
    s = []
    s.append(status(OMEN, "omen",
        "A portent of doom hangs over this creature. Each Omen makes it a little more vulnerable; at seven, the Seventh Seal breaks: "
        "damage that ignores armour and scales with the victim's own vitality, and the Omens scatter to everyone nearby.",
        CRIMSON, bad=True, ticks=1500, maxSev=12,
        comps=[stack_trigger(7, boss=10, radius=4.5, color=CRIMSON, label="AA_SeventhSeal", shake=1.2, sound="AA_Bell",
                             effects=[dmg(45, "AA_Doom", lethalPercent=0.12, noCrit=True), stun(90)],
                             spread=[omen(2)],
                             vfx=[rune(1.2, 60, tex=SEAL, byRadius=False), pillar(1.6, 40), flash(4.0), burst("ember", 20, 3.0, 0.6),
                                  screen(CRIMSON, 0.25, 20)])],
        stages=[stage(offsets={"IncomingDamageFactor": 0.03}, mult=True)],
        fx=dict(emitter="Omen", emitInterval=20, overlayTex="ArcanumAscendant/Vfx/OmenEye", overlaySize=0.55, overlayZ=1.05, overlayPulse=True)))
    s.append(status("AA_Prophesied", "prophesied ruin",
        "Its death has been foretold. If it dies while the prophecy stands, the Harbinger is rewarded and its Omens fly to its friends.",
        CRIMSON, bad=True, ticks=1800,
        comps=[death_trigger(caster=[mana(percent=0.25), cdreset(0.35, includeSelf=True)], area=[omen(3)], radius=6.0, color=CRIMSON,
                             label="AA_ProphecyFulfilled", shake=0.8, vfx=[rune(1.0, 50, tex=SEAL), burst("ember", 16, 3.0, 0.6, byRadius=True)])],
        stages=[stage(factors={"IncomingDamageFactor": 1.1})],
        fx=dict(emitter="Omen", emitInterval=26, overlayTex="ArcanumAscendant/Vfx/Skull", overlaySize=0.5, overlayZ=1.05, overlayPulse=True)))
    s.append(status("AA_Sentenced", "sentenced",
        "Sentence has been passed. When it is carried out, every wound taken since is dealt again in part, and each Omen adds to the toll.",
        "#ff4050", bad=True, ticks=600,
        comps=[delayed_doom(stored=0.5, stackHediff=OMEN, perStack=0.03, damageDef="AA_Doom", boss=0.6, color="#ff4050", label="AA_SentenceCarriedOut", shake=1.0)],
        fx=dict(emitter="Omen", emitInterval=24, overlayTex=SEAL, overlaySize=0.7, overlayZ=1.1, overlaySpin=3.0, overlayPulse=True)))
    s.append(status("AA_Fated", "fated",
        "This creature's fate is bound to its wounds: whenever anything hurts it, it gains an Omen.", CRIMSON, bad=True, ticks=1200,
        comps=[stack_on_damaged(OMEN, 1.0, cap=12, cooldown=45)],
        fx=dict(emitter="Omen", emitInterval=18, overlayTex="ArcanumAscendant/Vfx/Chains", overlaySize=1.3, overlaySpin=1.0)))
    s.append(status("AA_Terrified", "terror", "It has seen how the world ends and can think of nothing but running.", "#a01828", bad=True, ticks=360,
        comps=[compel("Flee")], stages=[stage(factors={"ShootingAccuracyPawn": 0.4, "MeleeHitChance": 0.5})],
        fx=dict(emitter="Watched", emitInterval=12)))
    s.append(status("AA_Eclipsed", "eclipsed", "Beneath the black sun nothing can be seen clearly.", "#5a0a14", bad=True, ticks=180,
        stages=[stage(caps={"Sight": -0.45}, factors={"ShootingAccuracyPawn": 0.6})]))
    s.append(status("AA_SharedFate", "shared fate", "Bound to the same fate as its companions: part of every wound any of them takes is dealt to all the others.",
        CRIMSON, bad=True, ticks=900, comps=[damage_link(0.5, "AA_Doom", CRIMSON)],
        fx=dict(emitter="Omen", emitInterval=24, overlayTex="ArcanumAscendant/Vfx/Chains", overlaySize=1.2, overlaySpin=-1.5)))
    s.append(status("AA_FateRewritten", "fate rewritten", "The Doomsayer has rewritten this person's fate. If they die, they will rise again.", "#ffd7a8", bad=False,
        ticks=3600, comps=[death_ward("#ffd7a8", "AA_FateRewritten")],
        fx=dict(overlayTex=SEAL, overlaySize=1.6, overlaySpin=2.0, overlayPulse=True)))
    s.append(status("AA_Certainty", "certainty", "Every prophecy fulfilled makes the next one more certain. Stacks.", "#ffd7a8", bad=False, ticks=1200, maxSev=5,
        stages=[stage(offsets={"AA_SpellPower": 0.06, "AA_CritChance": 0.02}, mult=True)], fx=dict(emitter="Omen", emitInterval=40)))
    s.append(status("AA_Souls", "harvested souls", "Souls reaped by the Pale Rider, waiting to be spent. Each one sharpens the scythe.", "#cfe8d6", bad=False,
        ticks=3600, maxSev=12, stages=[stage(offsets={"AA_SpellPower": 0.03, "MoveSpeed": 0.03}, mult=True)],
        fx=dict(emitter="Undead", emitInterval=28)))
    s.append(status("AA_GraveChill", "grave chill", "The cold of the grave slows every step.", "#cfe8d6", bad=True, ticks=240,
        stages=[stage(factors={"MoveSpeed": 0.6, "MeleeCooldownFactor": 1.25, "AimingDelayFactor": 1.25})], fx=dict(emitter="Chill", emitInterval=24)))
    s.append(status("AA_PaleSteed", "pale steed", "Riding a spectral pale horse: faster than the living, harder to stop.", "#cfe8d6", bad=False, ticks=1800,
        stages=[stage(factors={"MoveSpeed": 1.6, "IncomingDamageFactor": 0.85}, offsets={"PainShockThreshold": 0.3})],
        fx=dict(emitter="Haste", emitInterval=5)))
    s.append(status("AA_DeathIncarnate", "death incarnate", "Death itself rides in this body: nearly untouchable, terribly strong.", "#cfe8d6", bad=False, ticks=1800,
        stages=[stage(offsets={"AA_SpellPower": 0.3, "PainShockThreshold": 1.0}, factors={"IncomingDamageFactor": 0.5, "MoveSpeed": 1.3},
                      extra={"painFactor": 0.0})],
        fx=dict(emitter="Undead", emitInterval=8, overlayTex="ArcanumAscendant/Vfx/RuneCircle2", overlaySize=2.2, overlaySpin=2.0)))
    s.append(status("AA_BloodMoon", "blood moon", "The moon has turned to blood: an Omen falls on this creature every few seconds.", "#ff2a2a", bad=True, ticks=1800,
        comps=[periodic(300, effects=[omen(1)], color="#ff2a2a")], fx=dict(emitter="Blood", emitInterval=30)))
    s.append(status("AA_Wormwood", "wormwood", "The waters have turned bitter: a slow poison from the fallen star.", "#9ac24a", bad=True, ticks=600, maxSev=3,
        comps=[dot("AA_Doom", 2.0, "#9ac24a", "AA_Fleck_Smoke")], stages=[stage(factors={"ImmunityGainSpeed": 0.6}, caps={"BloodFiltration": -0.15})],
        fx=dict(emitter="Poison", emitInterval=18)))
    return s


def build():
    h = ClassSpec("Harbinger", "harbinger", color=CRIMSON, color2=BONE, icon=("crow", "eye"), order=10, school=DOOM,
        tagline="Prophet of the end. Lay Omens on your enemies, then break the Seventh Seal and watch it chain through the crowd.",
        desc="Harbingers do not so much fight as announce. Every curse they speak is a promise, and every promise is kept.",
        playstyle="Build Omens with crows, prophecies and your allies' own gunfire (Fated), then cash them in: Toll the Bell for a single "
                  "kill, Last Rites for a crowd, or let seven of them break the Seventh Seal, which ignores armour, scales with the "
                  "victim's own vitality and splashes Omens onto its friends. Wants Focus and Potency.",
        lore="'I saw seven crows on the roof this morning,' said the trader. The Harbinger smiled. 'You will see one more.'",
        primary="FOC", secondary="POT", tags=["Doom", "Curse"], starting=["IllOmen"], innate={"AA_CritDamage": 0.1})
    h.add_spells(
        Spell("IllOmen", "ill omen", "Send a crow of ill omen. Its beak deals doom damage that ignores armour and lays an Omen. Seven Omens break the Seventh Seal.",
              icon=("crow", "eye"), mana=8, cd=1.5, range=28, warmup=0.4, tags=["Doom"], delivery="Projectile", projectileSpeed=24.0, projectileScale=0.85,
              impact_vfx=DOOM_HIT, sound_cast="AA_Caw", effects=[dmg(10, "AA_Doom"), omen(1)], flavor="Count them. One."),
        missiles("MurderOfCrows", "murder of crows", "Release six crows that wheel through the air and fall on the nearest enemies, each one laying an Omen.",
                 ("crow", "burst"), mana=24, cd=10, count=6, speed=16, projectile="CrowBolt", scale=0.6, spread=150.0, tags=["Doom"], school=DOOM,
                 impact_vfx=DOOM_HIT, sound_cast="AA_Caw", effects=[dmg(7, "AA_Doom"), omen(1)], flavor="A gathering of crows is called a murder for a reason."),
        Spell("TollTheBell", "toll the bell", "A single, terrible note for a single enemy: doom damage, plus 35% for every Omen it carries. The Omens are spent.",
              icon=("bell", "eye"), mana=22, cd=9, range=26, warmup=0.6, tags=["Doom"], sound_cast="AA_Bell",
              impact_vfx=[ring(0.3, 1.0, 30, byRadius=False), flash(3.0), burst("ember", 12, 2.5, 0.6), SEAL_RUNE],
              effects=[dmg(26, "AA_Doom", stackHediff=OMEN, bonusPerStack=0.35, consumeStacks=True)], flavor="Ask not for whom it tolls."),
        Spell("ProphecyOfRuin", "prophecy of ruin", "Foretell an enemy's death and lay two Omens. If it dies within thirty seconds you regain a quarter of your mana, "
              "your other spells cool down by a third, and its doom spreads: three Omens on every enemy within six cells.", icon=("scroll", "skull"),
              mana=18, cd=14, range=30, warmup=0.5, tags=["Doom", "Curse"], role="Control", impact_vfx=DOOM_HIT + [SEAL_RUNE],
              effects=[hediff("AA_Prophesied", 1.0), omen(2)]),
        Spell("Sentence", "sentence", "Pass sentence on an enemy. In ten seconds, half of every wound it took in the meantime is dealt again, plus 3% of its "
              "vitality for each Omen it carries.", icon=("hourglass", "skull"), mana=30, cd=20, range=28, warmup=0.7, tags=["Doom", "Curse"], role="Control",
              tier=2, impact_vfx=[rune(0.9, 60, tex=SEAL, byRadius=False), flash(2.5), burst("ember", 10, 2.0, 0.5)], effects=[hediff("AA_Sentenced", 1.0)],
              flavor="The court of last things is now in session."),
        Spell("Fated", "fated", "Tie an enemy's fate to its wounds for twenty seconds: every time anything hurts it, it gains an Omen. Your allies' gunfire "
              "becomes prophecy.", icon=("eye", "chain"), mana=20, cd=16, range=30, warmup=0.5, tags=["Doom", "Curse"], role="Control", tier=2,
              impact_vfx=DOOM_HIT, effects=[hediff("AA_Fated", 1.0), omen(1)]),
        Spell("ApocalypticVision", "apocalyptic vision", "Show everyone in front of you how the world ends. Enemies in a wide cone flee in terror for six "
              "seconds and gain two Omens.", icon=("eye", "burst"), mana=28, cd=22, range=9, warmup=0.5, tags=["Doom"], role="Control", target="enemy_area",
              delivery="Cone", coneAngle=70.0, coneRange=9.0, ai_min=2, tier=2, impact_vfx=[flash(1.6)],
              effects=[hediff("AA_Terrified", 1.0), omen(2), dmg(8, "AA_Doom")]),
        Spell("BlackSun", "black sun", "Put out the sun over an area for fifteen seconds. Enemies beneath it can barely see and gain an Omen every two seconds.",
              icon=("eclipse", "eye"), mana=45, cd=40, range=30, warmup=1.0, tags=["Doom"], role="Control", target="enemy_area", ai_min=2, tier=3,
              impact_vfx=[flash(3.0, color="#2a0006"), ring(0.2, 1.0, 30)],
              effects=[field(5.0, 900, 120, [omen(1, affects="Enemies"), hediff("AA_Eclipsed", 1.0, affects="Enemies"), dmg(4, "AA_Doom", affects="Enemies")],
                             groundTex="ArcanumAscendant/Vfx/BlackSun", groundSpin=1.0, groundAlpha=0.75, ambientFleck="AA_Fleck_Ember", ambientDensity=2.0,
                             ambientSpeed=0.8)]),
        delayed("DoomsdayClock", "doomsday clock", "A spectral clock appears over an area and strikes midnight three seconds later: everything beneath takes heavy "
                "doom damage, +15% per Omen, and gains another.", ("clock", "skull"), mana=40, cd=26, delay=180, radius=2.8, strike="Pillar", tags=["Doom"],
                tier=3, school=DOOM, ai_min=2, sound_impact="AA_Bell", impact_vfx=[flash(3.5), ring(0.2, 1.1, 30), SEAL_RUNE],
                effects=[dmg(40, "AA_Doom", stackHediff=OMEN, bonusPerStack=0.15), omen(1)]),
        Spell("LastRites", "last rites", "Read the last rites over everyone around you. Every enemy within six cells takes doom damage, +30% per Omen (spent), "
              "and you are healed for a quarter of the damage.", icon=("bell", "skull"), mana=50, cd=35, warmup=0.8, tags=["Doom"], target="self",
              delivery="Nova", radius=6.0, ai_min=3, tier=3, shake=1.2, sound_cast="AA_Bell",
              impact_vfx=[ring(1.1, 0.1, 34), rune(1.0, 60, tex=SEAL), burst("ember", 30, 3.0, 0.7, byRadius=True), flash(4.0)],
              effects=[dmg(30, "AA_Doom", stackHediff=OMEN, bonusPerStack=0.3, consumeStacks=True, healCasterFraction=0.25)]),
        Spell("MementoMori", "memento mori", "Whisper to a colonist the exact day of their death. Unsettled and inspired, they throw themselves into their work.",
              icon=("hourglass", "heart"), mana=30, cd=DAY / SEC, range=4, warmup=3.0, tags=["Doom", "Utility"], role="Utility", target="ally", quirky=True,
              impact_vfx=[rune(0.8, 60, tex=SEAL, byRadius=False), burst("ember", 6, 1.0, 0.5)], effects=[inspire("Frenzy_Work")],
              flavor="It is not today. Get back to work."),
    )
    h.add_nodes(
        Node("GrimPortents", "grim portents", (1, 0), ranks=3, mods=[tagmod("Doom", damage=0.05)]),
        Node("Foreknowledge", "foreknowledge", (3, 0), ranks=3, stats={"AA_ManaMax": 10, "AA_CritChance": 0.01}),
        Node("CarrionCall", "carrion call", (5, 0), ranks=3, stats={"AA_CastSpeed": 0.03}, mods=[spellmod(["IllOmen"], damage=0.08)]),
        Node("TollTheBell", None, (1, 1), kind="Spell", spell="TollTheBell", pre=["GrimPortents"]),
        Node("ProphecyOfRuin", None, (3, 1), kind="Spell", spell="ProphecyOfRuin", pre=["Foreknowledge"]),
        Node("MurderOfCrows", None, (5, 1), kind="Spell", spell="MurderOfCrows", pre=["CarrionCall"]),
        Node("FuneralKnell", "funeral knell", (0, 2), kind="Augment", ranks=2, pre=["TollTheBell"], mods=[spellmod(["TollTheBell"], damage=0.15, cooldown=-0.1)]),
        Node("BadTidings", "bad tidings", (2, 2), kind="Notable", ranks=2, pre=["TollTheBell", "ProphecyOfRuin"],
             passives=[P("P_OnHitHediff", hediff=OMEN, chancePerRank=0.15, severity=1.0, stack=True, maxSeverity=12.0, durationTicks=1500, tag="Doom")],
             desc="Doom spells sometimes lay an extra Omen."),
        Node("Fated", None, (4, 2), kind="Spell", spell="Fated", pre=["ProphecyOfRuin", "MurderOfCrows"]),
        Node("Unkindness", "unkindness", (6, 2), kind="Augment", ranks=2, pre=["MurderOfCrows"], mods=[spellmod(["MurderOfCrows"], extraTargets=2, damage=0.05)],
             desc="Two more crows per rank."),
        Node("Sentence", None, (1, 3), kind="Spell", spell="Sentence", pre=["FuneralKnell", "BadTidings"]),
        Node("MorbidFocus", "morbid focus", (3, 3), ranks=3, pre=["BadTidings", "Fated"], stats={"AA_CooldownReduction": 0.02, "AA_CastSpeed": 0.03}),
        Node("ApocalypticVision", None, (5, 3), kind="Spell", spell="ApocalypticVision", pre=["Fated", "Unkindness"]),
        Node("MementoMori", None, (6, 4), kind="Spell", spell="MementoMori", pre=["ApocalypticVision"]),
        Node("DeathWarrant", "death warrant", (0, 4), kind="Augment", ranks=2, pre=["Sentence"], mods=[spellmod(["Sentence"], damage=0.2, cooldown=-0.1)]),
        Node("HarbingerOfRuin", "harbinger of ruin", (2, 4), kind="Notable", ranks=2, pre=["Sentence", "MorbidFocus"],
             passives=[P("P_TriggerSpell", trigger="OnStackTrigger", spell=S("IllOmen"), chancePerRank=0.5, target="NearestEnemy", stackHediff=OMEN)],
             desc="When a Seventh Seal breaks, a crow of ill omen flies at the nearest enemy."),
        Node("BlackSun", None, (4, 4), kind="Spell", spell="BlackSun", pre=["MorbidFocus", "ApocalypticVision"]),
        Node("DoomsdayClock", None, (1, 5), kind="Spell", spell="DoomsdayClock", pre=["DeathWarrant", "HarbingerOfRuin"]),
        Node("CarrionFeast", "carrion feast", (3, 5), kind="Notable", ranks=2, pre=["HarbingerOfRuin", "BlackSun"],
             passives=[P("P_ManaOnKill", amount=6.0)], stats={"AA_Lifesteal": 0.02}, desc="The crows bring you a share of every death."),
        Node("LongNight", "long night", (5, 5), ranks=3, pre=["BlackSun", "MementoMori"], mods=[tagmod("Doom", duration=0.08, radius=0.04)]),
        Node("Omenwright", "omenwright", (0, 6), ranks=3, pre=["DoomsdayClock"], stats={"AA_CritChance": 0.02, "AA_CritDamage": 0.06}),
        Node("LastRites", None, (2, 6), kind="Spell", spell="LastRites", pre=["DoomsdayClock", "CarrionFeast"]),
        Node("BearerOfTheSeal", "bearer of the seal", (4, 6), kind="Notable", ranks=2, pre=["CarrionFeast", "LongNight"],
             mods=[tagmod("Doom", damage=0.08, critChance=0.03)], desc="Everything you foretell lands harder, the Seventh Seal included."),
        Node("GraveVigil", "grave vigil", (6, 6), ranks=3, pre=["LongNight"], stats={"AA_WardMax": 8, "AA_ManaRegen": 0.1}),
        Node("ProphetOfTheEnd", "prophet of the end", (2, 7), kind="Keystone", excl="HarbingerKeystone", pre=["LastRites", "Omenwright"],
             mods=[tagmod("Doom", damage=0.15)],
             passives=[P("P_TriggerSpell", trigger="OnKill", spell=S("ProphecyOfRuin"), chancePerRank=0.5, target="NearestEnemy", cooldownTicks=120)],
             desc="Every death you cause is foretold. Kills often prophesy the ruin of the nearest enemy for free, and your doom hits harder."),
        Node("EndTimes", "end times", (4, 7), kind="Keystone", excl="HarbingerKeystone", pre=["BearerOfTheSeal", "GraveVigil"],
             mods=[tagmod("Doom", manaCost=-0.2)],
             passives=[P("P_OnHitHediff", hediff=OMEN, chancePerRank=0.5, severity=1.0, stack=True, maxSeverity=12.0, durationTicks=1500, tag="Doom")],
             desc="Omens fall like rain. Half your Doom hits lay an extra Omen, and every Doom spell costs a fifth less mana."),
        Node("HeraldOfTheSeventhSeal", "herald of the seventh seal", (3, 8), kind="Keystone", pre=["ProphetOfTheEnd", "EndTimes"], pct={"AA_SpellPower": 0.15},
             passives=[P("P_KillExplosion", damageDef="AA_Doom", damage=30.0, radius=3.5, chance=0.5, spellKillsOnly=False)],
             desc="Death announces you. Anything you kill may burst in a wave of doom that ignores armour."),
    )

    # ------------------------------------------------------------------ DOOMSAYER
    ds = ClassSpec("Doomsayer", "doomsayer", color="#e0284a", color2="#ffd7a8", icon=("seal", "eye"), parent=h, order=0, school=SAYER,
        tagline="Speak the end aloud. Doom clocks, shared fates, words that unmake, and the Apocalypse itself.",
        desc="Doomsayers have stopped predicting the end and started scheduling it.",
        playstyle="The purest Omen engine. Words of Unmaking spreads Omens down a chain, Shared Fate links a crowd so one Seventh Seal "
                  "hurts them all, and Final Hour deletes a single target. The Apocalypse puts three Omens on everything on the map. "
                  "Wants Focus and Attunement.",
        lore="She published the date. Most people did not take it seriously, which is how she knew it was right.",
        primary="FOC", secondary="ATT", tags=["Doom", "Curse"], height=8)
    ds.add_spells(
        Spell("WordsOfUnmaking", "words of unmaking", "Speak a word that should not be spoken. It leaps between six enemies, searing each one and laying two Omens.",
              icon=("scroll", "bolt"), mana=32, cd=14, range=26, warmup=0.6, tags=["Doom", "Curse"], delivery="Chain", chainCount=5, chainRange=7.0,
              chainFalloff=0.95, tier=2, impact_vfx=DOOM_HIT, effects=[dmg(16, "AA_Doom"), omen(2)]),
        delayed("FinalHour", "final hour", "Hang a doom clock over one enemy. When it strikes four seconds later the enemy takes massive doom damage, +20% per "
                "Omen (spent), plus 8% of its vitality. Nothing outruns its final hour.", ("clock", "eye"), mana=45, cd=24, delay=240, radius=0.9,
                strike="Pillar", tags=["Doom"], tier=3, target="enemy", school=SAYER, sound_impact="AA_Bell",
                impact_vfx=[flash(4.0), rune(1.0, 50, tex=SEAL, byRadius=False), burst("ember", 16, 3.0, 0.6)],
                effects=[dmg(70, "AA_Doom", stackHediff=OMEN, bonusPerStack=0.2, consumeStacks=True, lethalPercent=0.08)]),
        Spell("SharedFate", "shared fate", "Bind every enemy in an area to one fate for fifteen seconds: half of any wound one of them takes is dealt to all the "
              "others. One Seventh Seal becomes a massacre.", icon=("chain", "eye"), mana=40, cd=30, range=28, warmup=0.8, tags=["Doom", "Curse"],
              role="Control", target="enemy_area", radius=4.0, ai_min=3, tier=3, impact_vfx=[rune(1.0, 60, tex=SEAL), ring(0.3, 1.0, 26)],
              effects=[hediff("AA_SharedFate", 1.0), omen(1)]),
        channel("DoomSpiral", "doom spiral", "Stare into an enemy's soul and unravel it: a beam that searing doom along its length and lays an Omen every half "
                "second for four seconds.", ("eye", "swirl"), mana=30, cd=18, ticks=240, interval=30, width=0.8, tags=["Doom"], tier=2, school=SAYER,
                effects=[dmg(7, "AA_Doom"), omen(1)]),
        Spell("CheatFate", "cheat fate", "Rewrite an ally's fate. For the next minute, if they would die, they rise again instead.", icon=("seal", "heart"),
              mana=60, cd=300, range=24, warmup=1.0, tags=["Doom", "Ward"], role="Buff", target="ally", tier=3, autocast=False,
              impact_vfx=[pillar(1.2, 40), rune(1.0, 60, tex=SEAL, byRadius=False), flash(3.0, color="#ffd7a8")], effects=[hediff("AA_FateRewritten", 1.0)]),
        Spell("Apocalypse", "apocalypse", "Announce the end of the world. Every enemy on the map takes doom damage and gains three Omens; three seconds later "
              "the sky answers, and every Omen on the map is tolled at once for 35% extra damage each.", icon=("eclipse", "seal"), mana=220, cd=300,
              warmup=3.0, tags=["Doom"], target="self", delivery="Global", ultimate=True, tier=5, xp=2.0, shake=2.5, ai_min=4, sound_cast="AA_Bell",
              cast_vfx=[pillar(3.0, 80), screen("#ff2040", 0.4, 60)],
              impact_vfx=[pillar(1.0, 40), burst("ember", 10, 2.0, 0.6)],
              effects=[dmg(30, "AA_Doom"), omen(3),
                       sequence(step(180, [dmg(20, "AA_Doom", stackHediff=OMEN, bonusPerStack=0.35, consumeStacks=True)], radius=0.0,
                                     vfx=[rune(0.9, 50, tex=SEAL, byRadius=False), flash(3.0)], sound="AA_Bell", text="AA_Doomsday", textColor="#ff4060"))]),
    )
    ds.add_nodes(
        Node("WordsOfUnmaking", None, (2, 0), kind="Spell", spell="WordsOfUnmaking"),
        Node("SilverTongue", "silver tongue", (4, 0), ranks=3, stats={"AA_ManaCostFactor": -0.03, "AA_ManaRegen": 0.08}),
        Node("Babel", "babel", (1, 1), kind="Augment", ranks=2, pre=["WordsOfUnmaking"], mods=[spellmod(["WordsOfUnmaking"], extraTargets=1, damage=0.1)],
             desc="The word leaps to one more enemy per rank."),
        Node("Certainty", "certainty", (3, 1), kind="Notable", pre=["WordsOfUnmaking", "SilverTongue"],
             passives=[P("P_SelfBuffOnCast", hediff="AA_Certainty", severityPerCast=0.25, durationTicks=1200, tag="Doom")],
             desc="Every doom you speak makes you surer of the next. Casting Doom spells builds Certainty: more power and more criticals."),
        Node("DoomSpiral", None, (5, 1), kind="Spell", spell="DoomSpiral", pre=["SilverTongue"]),
        Node("Litany", "litany", (0, 2), ranks=3, pre=["Babel"], mods=[tagmod("Doom", cooldown=-0.03)]),
        Node("FinalHour", None, (2, 2), kind="Spell", spell="FinalHour", pre=["Babel", "Certainty"]),
        Node("Unblinking", "unblinking", (4, 2), kind="Augment", ranks=2, pre=["Certainty", "DoomSpiral"], mods=[spellmod(["DoomSpiral"], damage=0.2, range=0.2)]),
        Node("Dread", "dread", (6, 2), ranks=3, pre=["DoomSpiral"], mods=[tagmod("Doom", critChance=0.03)]),
        Node("MidnightStrikes", "midnight strikes", (1, 3), kind="Augment", ranks=2, pre=["FinalHour"], mods=[spellmod(["FinalHour"], cooldown=-0.15, damage=0.15)]),
        Node("SharedFate", None, (3, 3), kind="Spell", spell="SharedFate", pre=["FinalHour", "Unblinking"]),
        Node("PropheticShield", "prophetic shield", (5, 3), kind="Notable", ranks=2, pre=["Unblinking", "Dread"], stats={"AA_WardMax": 15},
             passives=[P("P_DamageReduction", fractionPerRank=0.04)], desc="You saw that blow coming."),
        Node("OmenWeaver", "omen weaver", (0, 4), ranks=3, pre=["MidnightStrikes"], pct={"AA_SpellPower": 0.03}),
        Node("CollectiveDoom", "collective doom", (2, 4), kind="Augment", ranks=2, pre=["SharedFate"], mods=[spellmod(["SharedFate"], radius=0.25, duration=0.3)]),
        Node("CheatFate", None, (4, 4), kind="Spell", spell="CheatFate", pre=["SharedFate", "PropheticShield"]),
        Node("EchoingDoom", "echoing doom", (1, 5), kind="Notable", ranks=2, pre=["OmenWeaver", "CollectiveDoom"],
             passives=[P("P_EchoCast", chancePerRank=0.08, tag="Doom")], desc="Your prophecies sometimes repeat themselves for free."),
        Node("HeraldsVoice", "herald's voice", (3, 5), ranks=3, pre=["CollectiveDoom", "CheatFate"], stats={"AA_CastSpeed": 0.04, "AA_CooldownReduction": 0.02}),
        Node("FatesFavor", "fate's favor", (5, 5), kind="Notable", pre=["CheatFate"], passives=[P("P_ManaShield", fractionPerRank=0.15, manaPerDamage=1.2)],
             desc="Fate itself turns some blows aside, at a cost in mana."),
        Node("VoiceOfTheEnd", "voice of the end", (2, 6), kind="Keystone", excl="DoomsayerKeystone", pre=["EchoingDoom", "HeraldsVoice"],
             mods=[spellmod(["FinalHour", "WordsOfUnmaking"], cooldown=-0.3), tagmod("Doom", damage=0.2)],
             desc="Every word is final. Final Hour and Words of Unmaking recharge far faster and all your doom hits harder."),
        Node("Unwritten", "unwritten", (4, 6), kind="Keystone", excl="DoomsayerKeystone", pre=["HeraldsVoice", "FatesFavor"],
             mods=[spellmod(["CheatFate"], cooldown=-0.5)],
             passives=[P("P_Rebirth", novaDamage=40.0, novaDamageDef="AA_Doom", novaRadius=5.0, wardFraction=0.5, cooldownTicks=DAY, color=(1.0, 0.85, 0.66, 1.0),
                         labelKey="AA_FateRewritten")],
             desc="Your own fate is unwritten. Once a day, when you would die, you rise again in a wave of doom. Cheat Fate recharges twice as fast."),
        Node("Apocalypse", None, (3, 7), kind="Ultimate", spell="Apocalypse", pre=["VoiceOfTheEnd", "Unwritten"]),
    )

    # ------------------------------------------------------------------ PALE RIDER
    pr = ClassSpec("PaleRider", "pale rider", color="#cfe8d6", color2="#4f6f62", icon=("scythe", "crow"), parent=h, order=1, school=RIDER,
        tagline="Behold, a pale horse. Reap with a scythe of pale fire, ride through the enemy line, and harvest their souls.",
        desc="The fourth horseman, or someone who borrowed his horse. Pale Riders fight up close and grow stronger with every death.",
        playstyle="A front-line reaper. Kills store Souls (more power and speed); Soul Harvest spends them in a huge nova. Reap executes the "
                  "weak, Ride Them Down tramples a path through a line, and Death Rides blinks you from enemy to enemy. Wants Potency and Resilience.",
        lore="'And his name that sat on him was Death.' Her name is actually Marta, but the horse is very convincing.",
        primary="POT", secondary="RES", tags=["Doom"], height=8)
    pr.add_spells(
        Spell("Reap", "reap", "Swing a scythe of pale fire in a wide arc. Executes enemies below 20% health and deals triple damage to anything downed or stunned.",
              icon=("scythe", "burst"), mana=18, cd=6, range=4.5, warmup=0.3, tags=["Doom"], target="enemy_area", delivery="Cone", coneAngle=110.0, coneRange=4.5,
              impact_vfx=[flash(1.6)], effects=[dmg(24, "AA_Doom", executeBelow=0.2, bonusVsHelpless=3.0), omen(1)], flavor="Every harvest begins with a swing."),
        Spell("SpectralSteed", "spectral steed", "Mount a spectral pale horse for thirty seconds: much faster, harder to stop, and slightly harder to hurt.",
              icon=("scythe", "wing"), mana=20, cd=60, warmup=0.5, tags=["Doom", "Mobility"], role="Buff", target="self",
              impact_vfx=[smoke(4, 1.0), burst("smoke", 10, 2.0, 0.6), flash(2.0)], effects=[hediff("AA_PaleSteed", 1.0)]),
        Spell("RideThemDown", "ride them down", "Ride through the enemy line to the target point, trampling everything in your path for doom damage and knocking it aside.",
              icon=("scythe", "arrow"), mana=26, cd=14, range=16, warmup=0.3, tags=["Doom", "Mobility"], role="Mobility", target="cell", tier=2,
              impact_vfx=[flash(2.0), smoke(3, 1.0)],
              effects=[teleport("CasterToTarget", pathEffects=[dmg(22, "AA_Doom"), knock(2.0), omen(1)])]),
        Spell("SoulHarvest", "soul harvest", "Spend every harvested soul in one blast of pale fire around you: +15% power per soul (up to +150%), healing you for a "
              "fifth of the damage. Some survivors flee in terror.", icon=("skull", "swirl"), mana=40, cd=25, warmup=0.6, tags=["Doom"], target="self",
              delivery="Nova", radius=5.0, ai_min=2, tier=2, shake=1.0, caster_effects=[empower("AA_Souls", 0.15, 1.5)],
              impact_vfx=[ring(0.1, 1.1, 30), burst("smoke", 20, 3.0, 0.8, byRadius=True), spiral("skull", 12, 2.5, 0.6), flash(3.0)],
              effects=[dmg(34, "AA_Doom", healCasterFraction=0.2), hediff("AA_Terrified", 1.0, chance=0.4)]),
        Spell("DeathlyPallor", "deathly pallor", "Wear death like a cloak for twenty seconds. Enemies near you are slowed by the chill of the grave and gain an "
              "Omen every two seconds.", icon=("skull", "snowflake"), mana=34, cd=40, warmup=0.5, tags=["Doom"], role="Control", target="self", tier=3,
              ai_min=2, radius=3.5, impact_vfx=[ring(0.2, 1.0, 26), smoke(4, 1.2)],
              effects=[field(3.5, 1200, 120, [omen(1, affects="Enemies"), hediff("AA_GraveChill", 1.0, affects="Enemies")], followCaster=True,
                             groundTex="ArcanumAscendant/Vfx/RuneCircle2", groundSpin=-1.0, groundAlpha=0.4, ambientFleck="AA_Fleck_Smoke",
                             ambientDensity=1.5, ambientSpeed=0.4)]),
        Spell("DeathRides", "death rides", "Become Death for thirty seconds and ride through the battlefield: blink between up to ten enemies, reaping each one "
              "for massive doom damage. Anything below 30% health dies outright. Spends your souls for extra power.", icon=("scythe", "skull"),
              mana=200, cd=240, range=24, warmup=1.5, tags=["Doom"], target="enemy", delivery="ChainDash", chainCount=9, chainRange=9.0, chainDelay=7,
              ultimate=True, tier=5, xp=2.0, shake=2.0, caster_effects=[empower("AA_Souls", 0.1, 1.0), hediff("AA_DeathIncarnate", 1.0)],
              impact_vfx=[flash(3.0), burst("smoke", 10, 2.5, 0.7), spiral("skull", 6, 2.0, 0.5)],
              effects=[dmg(60, "AA_Doom", executeBelow=0.3), omen(2)]),
    )
    pr.add_nodes(
        Node("Reap", None, (1, 0), kind="Spell", spell="Reap"),
        Node("TollOfSouls", "toll of souls", (3, 0), kind="Notable",
             passives=[P("P_StacksOnKill", hediff="AA_Souls", perKill=1.0, maxStacks=12.0, durationTicks=3600)],
             desc="Every death you cause adds a harvested soul: more spell power and speed, until you spend them."),
        Node("PaleFire", "pale fire", (5, 0), ranks=3, mods=[tagmod("Doom", damage=0.04)]),
        Node("WideArc", "wide arc", (0, 1), kind="Augment", ranks=2, pre=["Reap"], mods=[spellmod(["Reap"], damage=0.15, range=0.15)]),
        Node("SpectralSteed", None, (2, 1), kind="Spell", spell="SpectralSteed", pre=["Reap", "TollOfSouls"]),
        Node("RideThemDown", None, (4, 1), kind="Spell", spell="RideThemDown", pre=["TollOfSouls", "PaleFire"]),
        Node("Gravedigger", "gravedigger", (6, 1), ranks=3, pre=["PaleFire"], stats={"AA_Lifesteal": 0.02}),
        Node("ReapersDue", "reaper's due", (1, 2), kind="Notable", ranks=2, pre=["WideArc", "SpectralSteed"], passives=[P("P_Execute", thresholdPerRank=0.05, tag="Doom")],
             desc="Doom spells finish off enemies left at low health."),
        Node("SoulHarvest", None, (3, 2), kind="Spell", spell="SoulHarvest", pre=["SpectralSteed", "RideThemDown"]),
        Node("Stampede", "stampede", (5, 2), kind="Augment", ranks=2, pre=["RideThemDown", "Gravedigger"], mods=[spellmod(["RideThemDown"], damage=0.25, cooldown=-0.15)]),
        Node("HollowEyes", "hollow eyes", (0, 3), ranks=3, pre=["ReapersDue"], stats={"AA_CritChance": 0.02}),
        Node("BountifulHarvest", "bountiful harvest", (2, 3), kind="Augment", ranks=2, pre=["SoulHarvest", "ReapersDue"],
             mods=[spellmod(["SoulHarvest"], radius=0.2, damage=0.15)]),
        Node("DeathlyPallor", None, (4, 3), kind="Spell", spell="DeathlyPallor", pre=["SoulHarvest", "Stampede"]),
        Node("IronSaddle", "iron saddle", (6, 3), ranks=3, pre=["Stampede"], stats={"ArmorRating_Sharp": 0.04, "ArmorRating_Blunt": 0.04}),
        Node("SoulGlutton", "soul glutton", (1, 4), kind="Notable", ranks=2, pre=["HollowEyes", "BountifulHarvest"], passives=[P("P_ManaOnKill", amount=8.0)],
             stats={"AA_Lifesteal": 0.03}, desc="Each soul feeds you mana and life."),
        Node("ShroudOfDust", "shroud of dust", (3, 4), kind="Augment", ranks=2, pre=["DeathlyPallor"], mods=[spellmod(["DeathlyPallor"], radius=0.3, duration=0.3)]),
        Node("NoRest", "no rest", (5, 4), kind="Notable", ranks=2, pre=["DeathlyPallor", "IronSaddle"], passives=[P("P_CooldownOnKill", ticksPerRank=90)],
             desc="Kills hurry your cooldowns along."),
        Node("ColdHands", "cold hands", (2, 5), ranks=3, pre=["SoulGlutton", "ShroudOfDust"], stats={"AA_CritDamage": 0.06}),
        Node("EndlessRide", "endless ride", (4, 5), ranks=3, pre=["ShroudOfDust", "NoRest"], stats={"AA_CooldownReduction": 0.03}),
        Node("HorsemanOfDeath", "horseman of death", (2, 6), kind="Keystone", excl="RiderKeystone", pre=["ColdHands", "EndlessRide"],
             mods=[spellmod(["SpectralSteed"], duration=1.0, cooldown=-0.5), spellmod(["RideThemDown"], cooldown=-0.4, damage=0.3)],
             stats={"MoveSpeed": 0.4},
             desc="You never dismount. Spectral Steed lasts twice as long and returns twice as often, and Ride Them Down hits harder and recharges fast."),
        Node("GrimReaper", "grim reaper", (4, 6), kind="Keystone", excl="RiderKeystone", pre=["ColdHands", "EndlessRide"],
             mods=[spellmod(["Reap"], cooldown=-0.5, damage=0.3)], passives=[P("P_Execute", thresholdPerRank=0.15, tag="Doom")],
             desc="Every swing is a harvest. Reap recharges twice as fast and anything you hit below 15% health dies."),
        Node("DeathRides", None, (3, 7), kind="Ultimate", spell="DeathRides", pre=["HorsemanOfDeath", "GrimReaper"]),
    )

    # ------------------------------------------------------------------ CATACLYSM
    cy = ClassSpec("Cataclysm", "cataclysm", color="#ff6a28", color2="#52100a", icon=("eclipse", "meteor"), parent=h, order=2, school=CATA,
        tagline="The end of days, on demand. Earthquakes, the bitter star Wormwood, plagues of locusts and the Seven Seals.",
        desc="Some Harbingers announce the end. Cataclysms bring it, one disaster at a time.",
        playstyle="Huge, slow, map-shaking area damage that also wrecks buildings and turrets. Split the Earth flings a line of enemies into the "
                  "air, Wormwood poisons the ground, the Blood Moon lays Omens on everything on the map, and the Seven Seals unfold seven "
                  "different calamities over one spot. Wants Potency and Attunement.",
        lore="She keeps a list of disasters on her wall and crosses them off. The list is getting short.",
        primary="POT", secondary="ATT", tags=["Doom"], height=8)
    cy.add_spells(
        Spell("SplitTheEarth", "split the earth", "Split the earth in a line. The ground erupts beneath everything in the way, flinging enemies into the air and "
              "cracking walls and turrets.", icon=("rock", "burst"), mana=34, cd=16, range=18, warmup=0.8, tags=["Doom", "Earth"], target="enemy_area",
              delivery="Line", lineLength=14.0, strikeRadius=1.4, lineStepDelay=2, ai_min=2, tier=2, shake=1.2,
              impact_vfx=[flash(1.8), smoke(2, 1.0), burst("smoke", 6, 2.5, 0.6)],
              effects=[dmg(20, "AA_Doom", damageBuildings=True, buildingFactor=2.5), omen(1), launch("Upward", 0, 12, stunTicks=60)]),
        Spell("LocustSwarm", "locust swarm", "A plague of locusts descends on an area for fifteen seconds, devouring enemies, stripping their armour and sometimes "
              "laying Omens.", icon=("locust", "cloud"), mana=42, cd=30, range=30, warmup=1.0, tags=["Doom", "Plague"], role="Control",
              target="enemy_area", ai_min=2, tier=2, style="Doom", impact_vfx=[flash(2.0), smoke(4, 1.2)],
              effects=[field(4.0, 900, 60, [dmg(8, "AA_Doom", affects="Enemies"), hediff("AA_ArmorShred", 1.0, stack=True, maxSeverity=4.0, affects="Enemies"),
                                            omen(1, chance=0.35, affects="Enemies")],
                             groundTex="ArcanumAscendant/Vfx/Glow", groundAlpha=0.25, ambientFleck="AA_Fleck_Smoke", ambientDensity=3.0, ambientSpeed=2.5,
                             ambientScale=0.4)]),
        Spell("Wormwood", "wormwood", "Call down Wormwood, the bitter star. It smashes into an area and poisons the ground for twenty seconds.",
              icon=("meteor", "drop"), mana=60, cd=40, range=34, warmup=1.6, tags=["Doom"], target="enemy_area", delivery="Meteor", radius=3.0, ai_min=2,
              tier=3, shake=1.8, projectileTex="ArcanumAscendant/Vfx/Meteor", projectileScale=1.4,
              impact_vfx=[flash(5.0), ring(0.1, 1.2, 34), burst("ember", 30, 4.0, 0.8, byRadius=True), smoke(8, 1.6)],
              effects=[dmg(55, "AA_Doom", falloff=True, damageBuildings=True, buildingFactor=2.0), omen(2),
                       field(3.0, 1200, 60, [hediff("AA_Wormwood", 1.0, stack=True, maxSeverity=3.0, affects="Enemies")], groundTex="ArcanumAscendant/Vfx/Glow",
                             groundAlpha=0.35, useColor2=True, ambientFleck="AA_Fleck_Smoke", ambientDensity=1.5, ambientSpeed=0.4)]),
        Spell("Hailfire", "hailfire", "Hail and fire mixed with blood rain down on an area: a dozen strikes, each one burning and sometimes laying an Omen.",
              icon=("cloud", "flame"), mana=55, cd=30, range=32, warmup=1.2, tags=["Doom"], target="enemy_area", delivery="Barrage", barrageCount=12,
              barrageInterval=6, barrageRadius=5.0, strikeRadius=1.6, strikeStyle="Meteor", ai_min=2, tier=3, shake=1.2,
              impact_vfx=[flash(2.0), burst("ember", 8, 3.0, 0.6)], effects=[dmg(24, "AA_Doom"), ignite(0.25), omen(1, chance=0.5)]),
        Spell("BloodMoon", "blood moon", "Turn the moon to blood for thirty seconds. Every enemy on the map gains an Omen every five seconds.",
              icon=("moon", "drop"), mana=70, cd=120, warmup=2.0, tags=["Doom"], role="Control", target="self", delivery="Global", tier=4, ai_min=4,
              cast_vfx=[pillar(2.0, 60), screen("#ff1010", 0.35, 50)], impact_vfx=[burst("blood", 4, 1.0, 0.5)],
              effects=[hediff("AA_BloodMoon", 1.0), omen(1)]),
        Spell("SevenSeals", "the seven seals", "Break the seven seals over an area, one after another: conquest, war, famine, death, the martyrs' vengeance, the "
              "great quake and the final silence. Each seal is a different calamity.", icon=("seal", "eclipse"), mana=240, cd=300, range=34, warmup=3.0,
              tags=["Doom"], target="enemy_area", radius=9.0, ultimate=True, tier=5, xp=2.0, shake=1.5, ai_min=4,
              cast_vfx=[pillar(3.0, 80)], impact_vfx=[rune(1.0, 360, tex=SEAL), ring(0.2, 1.0, 40)],
              effects=[sequence(
                  step(20, [hediff("AA_Terrified", 1.0, affects="Enemies"), omen(1, affects="Enemies")], radius=9.0,
                       vfx=[flash(4.0, color="#ffffff"), ring(0.1, 1.0, 30)], text="AA_Seal1", textColor="#ffffff", sound="AA_Bell"),
                  step(50, [mental("Berserk", chance=0.5, affects="Enemies"), dmg(20, "AA_Doom", affects="Enemies")], radius=9.0,
                       vfx=[flash(4.0, color="#ff2020"), burst("ember", 30, 4.0, 0.7, byRadius=True)], text="AA_Seal2", textColor="#ff3030", sound="AA_Bell"),
                  step(50, [hediff("AA_Weakened", 1.0, affects="Enemies"), need("Food", -0.5, affects="Enemies")], radius=9.0,
                       vfx=[flash(4.0, color="#202020"), smoke(10, 2.0)], text="AA_Seal3", textColor="#bbbbbb", sound="AA_Bell"),
                  step(50, [dmg(40, "AA_Doom", executeBelow=0.25, affects="Enemies")], radius=9.0,
                       vfx=[flash(4.0, color="#cfe8d6"), spiral("skull", 20, 3.0, 0.7)], text="AA_Seal4", textColor="#cfe8d6", sound="AA_Bell"),
                  step(50, [heal(40, affects="Allies"), ward(30, affects="Allies")], radius=9.0,
                       vfx=[flash(4.0, color="#ffe28a"), burst("feather", 20, 2.0, 0.6, byRadius=True)], text="AA_Seal5", textColor="#ffe28a", sound="AA_Bell"),
                  step(50, [dmg(25, "AA_Doom", damageBuildings=True, buildingFactor=3.0, affects="Enemies"), launch("Upward", 0, 25, stunTicks=120, affects="Enemies")],
                       radius=9.0, vfx=[ring(0.1, 1.2, 40), smoke(12, 2.0)], shake=3.0, text="AA_Seal6", textColor="#ff8a40", sound="AA_Bell"),
                  step(90, [dmg(80, "AA_Doom", lethalPercent=0.1, affects="Enemies"), omen(3, affects="Enemies")], radius=9.0,
                       vfx=[pillar(4.0, 60), flash(7.0, color="#ff2040"), screen("#ff2040", 0.4, 40)], shake=3.0,
                       text="AA_Seal7", textColor="#ff2040", sound="AA_Bell"))]),
    )
    cy.add_nodes(
        Node("SplitTheEarth", None, (2, 0), kind="Spell", spell="SplitTheEarth"),
        Node("Tectonic", "tectonic", (4, 0), ranks=3, mods=[tagmod("Doom", radius=0.04, damage=0.03)]),
        Node("Faultline", "faultline", (1, 1), kind="Augment", ranks=2, pre=["SplitTheEarth"], mods=[spellmod(["SplitTheEarth"], damage=0.2, range=0.15)]),
        Node("SiegeOfHeaven", "siege of heaven", (3, 1), kind="Notable", pre=["SplitTheEarth", "Tectonic"],
             passives=[P("P_ShieldBreaker", chancePerRank=0.5, tag="Doom")], desc="Your calamities often collapse energy shields outright."),
        Node("LocustSwarm", None, (5, 1), kind="Spell", spell="LocustSwarm", pre=["Tectonic"]),
        Node("Aftershock", "aftershock", (0, 2), ranks=3, pre=["Faultline"], stats={"AA_AreaFactor": 0.04}),
        Node("Wormwood", None, (2, 2), kind="Spell", spell="Wormwood", pre=["Faultline", "SiegeOfHeaven"]),
        Node("Pestilence", "pestilence", (4, 2), kind="Augment", ranks=2, pre=["SiegeOfHeaven", "LocustSwarm"], mods=[spellmod(["LocustSwarm"], radius=0.2, duration=0.3)]),
        Node("Ashfall", "ashfall", (6, 2), ranks=3, pre=["LocustSwarm"], stats={"AA_ManaMax": 15}),
        Node("BitterWaters", "bitter waters", (1, 3), kind="Augment", ranks=2, pre=["Wormwood"], mods=[spellmod(["Wormwood"], damage=0.2, radius=0.15)]),
        Node("Hailfire", None, (3, 3), kind="Spell", spell="Hailfire", pre=["Wormwood", "Pestilence"]),
        Node("WorldBreaker", "world breaker", (5, 3), kind="Notable", ranks=2, pre=["Pestilence", "Ashfall"], passives=[P("P_DamageVs", vs="Mechanoid", bonusPerRank=0.12)],
             desc="Machines break as easily as men when the world itself is against them."),
        Node("Calamitous", "calamitous", (0, 4), ranks=3, pre=["BitterWaters"], pct={"AA_SpellPower": 0.03}),
        Node("RainOfFire", "rain of fire", (2, 4), kind="Augment", ranks=2, pre=["Hailfire"], mods=[spellmod(["Hailfire"], extraTargets=2, damage=0.1)],
             desc="Four more strikes per rank."),
        Node("BloodMoon", None, (4, 4), kind="Spell", spell="BloodMoon", pre=["Hailfire", "WorldBreaker"]),
        Node("Doomsday", "doomsday", (1, 5), kind="Notable", ranks=2, pre=["Calamitous", "RainOfFire"], mods=[tagmod("Doom", critChance=0.04, damage=0.06)]),
        Node("EndlessNight", "endless night", (3, 5), ranks=3, pre=["RainOfFire", "BloodMoon"], stats={"AA_DurationFactor": 0.05, "AA_ManaRegen": 0.1}),
        Node("CrimsonTide", "crimson tide", (5, 5), kind="Augment", pre=["BloodMoon"], mods=[spellmod(["BloodMoon"], duration=0.5, cooldown=-0.25)]),
        Node("Armageddon", "armageddon", (2, 6), kind="Keystone", excl="CataclysmKeystone", pre=["Doomsday", "EndlessNight"],
             mods=[tagmod("Doom", radius=0.3, damage=0.15)], desc="Everything you call down is bigger. Much bigger."),
        Node("Extinction", "extinction", (4, 6), kind="Keystone", excl="CataclysmKeystone", pre=["EndlessNight", "CrimsonTide"],
             passives=[P("P_KillExplosion", damageDef="AA_Doom", damage=35.0, radius=3.0, chance=0.6, spellKillsOnly=False)], mods=[tagmod("Doom", cooldown=-0.15)],
             desc="Every death is a small apocalypse: kills often explode in doom, and your calamities recharge faster."),
        Node("SevenSeals", None, (3, 7), kind="Ultimate", spell="SevenSeals", pre=["Armageddon", "Extinction"]),
    )
    return h, [ds, pr, cy], statuses()

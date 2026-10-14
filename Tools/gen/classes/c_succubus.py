"""
The Succubus (women only; men walk the Incubus path). Her spells lay Infatuation; at five stacks the target is Enthralled
and fights for her for twenty seconds, so a good Succubus turns the enemy's own army against it. She also drinks years and
dreams.
"""
from classes.common import *  # noqa: F401,F403
from classes.expansion2 import missiles, channel, delayed  # noqa: F401

ROSE, BLUSH = "#ff5fa2", "#ffd1e8"
DESIRE = School("Desire", ROSE, BLUSH, fleck="petal", fleck2="star", projectile="ArcanumAscendant/Vfx/Orb")
TEMPT = School("Desire", "#ff7ab8", "#fff0f6", fleck="petal", fleck2="star", projectile="ArcanumAscendant/Vfx/Orb")
YEARS = School("Desire", "#ffb0d0", "#8a2050", fleck="clock", fleck2="petal", projectile="ArcanumAscendant/Vfx/Orb")
MUSE = School("Desire", "#b07aff", "#ff9ce0", fleck="star", fleck2="mote", projectile="ArcanumAscendant/Vfx/Orb")

INF = "AA_Infatuation"

KEYS = {
    "AA_Enthralled": "Enthralled",
    "AA_ForeverYoung": "Forever young",
}


def infatuate(n=1, **kw):
    return stacks(INF, n, duration=1800, cap=10, **kw)


KISS_HIT = [flash(1.8), burst("petal", 6, 2.0, 0.5), glow(1.0, 2.0, 18, useColor2=True)]


def statuses():
    s = []
    s.append(status(INF, "infatuation", "Hopelessly smitten. Infatuated creatures aim worse and strike weaker; at five stacks they are Enthralled and fight for "
        "the one they adore for twenty seconds. The very strongest are merely stunned by it.", ROSE, bad=True, ticks=1800, maxSev=10,
        comps=[stack_trigger(5, boss=9, radius=3.0, color=ROSE, label="AA_Enthralled", shake=0.4, sound="AA_Heartbeat",
                             effects=[dominate(1200, maxBodySize=4.0), stun(150)], spread=[infatuate(1)],
                             vfx=[flash(3.0, color=BLUSH), burst("petal", 20, 3.0, 0.6), ring(0.2, 1.0, 24, byRadius=False)])],
        stages=[stage(offsets={"ShootingAccuracyPawn": -0.05, "MeleeHitChance": -0.05}, mult=True)],
        fx=dict(emitter="Charm", emitInterval=14, overlayTex="ArcanumAscendant/Vfx/Heart", overlaySize=0.45, overlayZ=1.05, overlayPulse=True)))
    s.append(status("AA_Beguiled", "beguiled", "Dazed by a siren's call, it wanders toward her without a thought of fighting. A hard enough blow will break the spell.",
        ROSE, bad=True, ticks=300, comps=[compel("Approach", breakOnDamage=True, breakDamage=12)], fx=dict(emitter="Charm", emitInterval=10)))
    s.append(status("AA_Charmed", "charmed", "Utterly charmed: it walks to her side and will not fight unless badly hurt.", ROSE, bad=True, ticks=480,
        comps=[compel("Approach", breakOnDamage=True, breakDamage=25)], fx=dict(emitter="Charm", emitInterval=8)))
    s.append(status("AA_Transfixed", "transfixed", "Transfixed by impossible beauty. It cannot look away, let alone move.", "#ff7ab8", bad=True, ticks=360,
        comps=[compel("Hold")], stages=[stage(factors={"IncomingDamageFactor": 1.1})], fx=dict(emitter="Charm", emitInterval=8)))
    s.append(status("AA_Glamour", "glamour", "Wrapped in a glamour: enemies can hardly bring themselves to hurt her, and everyone else can hardly look away.", BLUSH,
        bad=False, ticks=1800, comps=[deflect(0.25, False, color=BLUSH)], stages=[stage(offsets={"SocialImpact": 0.5, "PawnBeauty": 2.0})],
        fx=dict(emitter="Charm", emitInterval=20)))
    s.append(status("AA_Ardor", "ardor", "Burning with ardor: stronger, steadier and nearly fearless.", ROSE, bad=False, ticks=2400,
        stages=[stage(factors={"MeleeDamageFactor": 1.3, "ShootingAccuracyPawn": 1.15, "MoveSpeed": 1.1}, offsets={"PainShockThreshold": 0.3})],
        fx=dict(emitter="Charm", emitInterval=24)))
    s.append(status("AA_Decaying", "decaying", "Years drip away from this body: it ages a little more every few seconds.", "#ffb0d0", bad=True, ticks=900,
        comps=[periodic(180, effects=[agesh(1.0, ailmentChance=0.15, lethalAgeFactor=1.5)], color="#ffb0d0"), dot("AA_Heartbreak", 1.0, "#ffb0d0")],
        fx=dict(emitter="Wither", emitInterval=16)))
    s.append(status("AA_EternalBeauty", "eternal beauty", "She has drunk so much youth that death itself cannot keep her.", "#ffb0d0", bad=False, ticks=3600,
        comps=[death_ward("#ffb0d0", "AA_ForeverYoung")], fx=dict(emitter="Charm", emitInterval=12, overlayTex="ArcanumAscendant/Vfx/HexShield",
        overlaySize=1.8, overlayPulse=True)))
    s.append(status("AA_Dreaming", "dreaming", "Lost in a deep, sweet dream. It stands where it is, and only pain will wake it.", "#b07aff", bad=True, ticks=600,
        comps=[compel("Hold", breakOnDamage=True, breakDamage=1)], stages=[stage(caps={"Sight": -0.8})],
        fx=dict(emitter="Dream", emitInterval=12)))
    s.append(status("AA_DreadDream", "dread dream", "Trapped in a waking nightmare: it flees from shadows, and the dream is eating it alive.", "#b07aff", bad=True,
        ticks=360, comps=[compel("Flee"), dot("AA_Heartbreak", 3.0, "#b07aff")], fx=dict(emitter="Dream", emitInterval=8)))
    s.append(status("AA_Sleepwalking", "sleepwalking", "Wandering in its sleep, unaware of the battle around it.", "#b07aff", bad=True, ticks=480,
        comps=[compel("Wander")], fx=dict(emitter="Dream", emitInterval=12)))
    s.append(status("AA_LucidDream", "lucid dream", "Living in a lucid dream: time slows down, aim sharpens, pain is just an idea.", "#ff9ce0", bad=False, ticks=1800,
        stages=[stage(factors={"AimingDelayFactor": 0.75, "ShootingAccuracyPawn": 1.2, "MeleeHitChance": 1.15}, offsets={"PainShockThreshold": 0.4})],
        fx=dict(emitter="Dream", emitInterval=20)))
    return s


def build():
    sc = ClassSpec("Succubus", "succubus", color=ROSE, color2=BLUSH, icon=("lips", "heart"), order=12, school=DESIRE, gender="Female", gender_group="Seduction",
        tagline="Win every fight by making the enemy fall in love. Infatuate, enthrall, and let their own army do the killing.",
        desc="Succubi are demons of desire, or simply very persuasive mages. The difference matters little to the raiders who switch sides mid-battle.",
        playstyle="Stack Infatuation with kisses, charms and the Siren's Call. At five stacks a target is Enthralled and fights for you for twenty "
                  "seconds; bosses are stunned instead. Heartbreak spends the stacks for damage, Lovers' Quarrel turns a crowd against itself, "
                  "and Kiss of Youth keeps you young forever. Only women can take this path. Wants Attunement and Focus.",
        lore="'Lay down your weapons,' she said, and most of them did, and then they picked them up again and pointed them the other way.",
        primary="ATT", secondary="FOC", tags=["Desire", "Charm"], starting=["BlownKiss"], innate={"AA_ManaRegen": 0.1})
    sc.add_spells(
        Spell("BlownKiss", "blown kiss", "Blow a kiss that flies like an arrow: it stings with heartbreak and leaves the target infatuated.", icon=("lips", "heart"),
              mana=8, cd=1.5, range=26, warmup=0.4, tags=["Desire"], delivery="Projectile", projectileSpeed=22.0, projectileScale=0.7, impact_vfx=KISS_HIT,
              effects=[dmg(10, "AA_Heartbreak"), infatuate(1)], flavor="Mwah."),
        Spell("Captivate", "captivate", "Captivate an enemy completely: for eight seconds it walks to your side and will not fight unless badly hurt. Two stacks of "
              "Infatuation.", icon=("heart", "eye"), mana=20, cd=12, range=24, warmup=0.5, tags=["Desire", "Charm"], role="Control", impact_vfx=KISS_HIT,
              effects=[hediff("AA_Charmed", 1.0), infatuate(2)]),
        Spell("SirenSong", "siren song", "Sing a note no one can resist. Every enemy within six cells drops what it is doing and walks toward you, dazed, "
              "and gains Infatuation. A hard blow wakes them.", icon=("note", "heart"), mana=26, cd=20, warmup=0.6, tags=["Desire", "Charm"], role="Control",
              target="self", delivery="Nova", radius=6.0, ai_min=2, sound_cast="AA_Heartbeat",
              impact_vfx=[ring(0.1, 1.2, 34), burst("petal", 20, 3.0, 0.6, byRadius=True), burst("note", 10, 2.0, 0.6, byRadius=True)],
              effects=[hediff("AA_Beguiled", 1.0), infatuate(1)]),
        Spell("Heartbreak", "heartbreak", "Break a heart that is already yours: heartbreak damage, +50% for every stack of Infatuation on the target. The stacks are "
              "spent.", icon=("heartcrack", "burst"), mana=24, cd=10, range=26, warmup=0.5, tags=["Desire"], sound_impact="AA_Heartbeat",
              impact_vfx=KISS_HIT + [burst("blood", 6, 2.0, 0.5)],
              effects=[dmg(22, "AA_Heartbreak", stackHediff=INF, bonusPerStack=0.5, consumeStacks=True)], flavor="It was never going to work out."),
        Spell("KissOfYouth", "kiss of youth", "Steal a kiss and the years that come with it. The target ages several years and is hurt; you grow a little younger.",
              icon=("lips", "hourglass"), mana=30, cd=30, range=6, warmup=0.8, tags=["Desire"], tier=2, impact_vfx=KISS_HIT + [burst("clock", 6, 1.5, 0.5)],
              effects=[agesh(3.0, ailmentChance=0.2), dmg(12, "AA_Heartbreak"), infatuate(1)], caster_effects=[agesh(-0.5)]),
        Spell("Glamour", "glamour", "Weave a glamour around yourself or an ally for thirty seconds. Enemies can hardly bring themselves to hurt the wearer: a "
              "quarter of all attacks simply fail.", icon=("mask", "star"), mana=24, cd=35, range=20, warmup=0.4, tags=["Desire", "Ward"], role="Buff",
              target="ally", impact_vfx=[burst("star", 12, 1.5, 0.5), glow(1.6, 2.4, 30)], effects=[hediff("AA_Glamour", 1.0)]),
        Spell("Ardor", "ardor", "Fill an ally with burning ardor for forty seconds: harder blows, steadier aim, no fear of pain.", icon=("heart", "flame"),
              mana=22, cd=30, range=20, warmup=0.4, tags=["Desire"], role="Buff", target="ally", impact_vfx=[burst("petal", 10, 2.0, 0.5), flash(1.6)],
              effects=[hediff("AA_Ardor", 1.0)]),
        Spell("LoversQuarrel", "lovers' quarrel", "Whisper a terrible rumour into a crowd. Enemies in the area fly into a jealous rage and attack whoever is nearest.",
              icon=("heartcrack", "dagger"), mana=40, cd=35, range=28, warmup=0.8, tags=["Desire", "Charm"], role="Control", target="enemy_area", radius=3.0,
              ai_min=3, tier=2, impact_vfx=[flash(2.0), burst("petal", 16, 2.5, 0.6, byRadius=True), ring(0.2, 1.0, 24)],
              effects=[mental("Berserk", chance=0.6, humanlikeOnly=False), infatuate(1)], flavor="She said WHAT about you?"),
        Spell("EssenceFeast", "essence feast", "Drink from every heart around you. Enemies within eight cells take heartbreak damage, two and a half times as much "
              "if they are infatuated, and you are healed for half of it.", icon=("heart", "swirl"), mana=40, cd=30, warmup=0.7, tags=["Desire"],
              target="self", delivery="Nova", radius=8.0, ai_min=3, tier=3,
              impact_vfx=[ring(1.1, 0.1, 34), spiral("petal", 16, -2.5, 0.6), flash(2.5)],
              effects=[dmg(16, "AA_Heartbreak", bonusVsHediff=INF, bonusVsHediffFactor=2.5, healCasterFraction=0.5)], caster_effects=[mana(15)]),
        Spell("StolenHeartbeat", "stolen heartbeat", "Reach into an enemy's chest and stop its heart for a moment: heartbreak damage, a share of its vitality, more "
              "for every stack of Infatuation (spent), and a long stun.", icon=("heart", "hand"), mana=50, cd=40, range=24, warmup=0.8, tags=["Desire"],
              tier=3, sound_impact="AA_Heartbeat", impact_vfx=[flash(3.0), burst("blood", 10, 2.5, 0.5), ring(0.6, 0.1, 20, byRadius=False)],
              effects=[dmg(30, "AA_Heartbreak", lethalPercent=0.15, stackHediff=INF, bonusPerStack=0.25, consumeStacks=True), stun(120)]),
        Spell("Irresistible", "irresistible", "Turn the full force of your charm on a downed enemy or a prisoner. They fall hopelessly in love and join the colony.",
              icon=("heart", "crown"), mana=80, cd=DAY * 3 / SEC, range=4, warmup=6.0, tags=["Desire", "Utility"], role="Utility", target="any_pawn",
              affects="AllButSelf", quirky=True, impact_vfx=[pillar(1.0, 40), burst("petal", 20, 2.0, 0.6), flash(2.5)], effects=[recruit()],
              flavor="Nobody has ever said no. Nobody has ever wanted to."),
    )
    sc.add_nodes(
        Node("Allure", "allure", (1, 0), ranks=3, mods=[tagmod("Desire", damage=0.05)]),
        Node("SweetNothings", "sweet nothings", (3, 0), ranks=3, stats={"AA_ManaMax": 10, "AA_ManaRegen": 0.06}),
        Node("Coquette", "coquette", (5, 0), ranks=3, stats={"AA_CastSpeed": 0.03, "AA_CooldownReduction": 0.01}),
        Node("Captivate", None, (1, 1), kind="Spell", spell="Captivate", pre=["Allure"]),
        Node("Heartbreak", None, (3, 1), kind="Spell", spell="Heartbreak", pre=["SweetNothings"]),
        Node("Glamour", None, (5, 1), kind="Spell", spell="Glamour", pre=["Coquette"]),
        Node("Smitten", "smitten", (0, 2), kind="Augment", ranks=2, pre=["Captivate"], mods=[spellmod(["Captivate", "BlownKiss"], duration=0.25, range=0.1)]),
        Node("Lovestruck", "lovestruck", (2, 2), kind="Notable", ranks=2, pre=["Captivate", "Heartbreak"],
             passives=[P("P_OnHitHediff", hediff=INF, chancePerRank=0.15, severity=1.0, stack=True, maxSeverity=10.0, durationTicks=1800, tag="Desire")],
             desc="Desire spells sometimes leave an extra stack of Infatuation."),
        Node("SirenSong", None, (4, 2), kind="Spell", spell="SirenSong", pre=["Heartbreak", "Glamour"]),
        Node("Ardor", None, (6, 2), kind="Spell", spell="Ardor", pre=["Glamour"]),
        Node("Shattered", "shattered", (1, 3), kind="Augment", ranks=2, pre=["Smitten", "Lovestruck"], mods=[spellmod(["Heartbreak"], damage=0.2, cooldown=-0.1)]),
        Node("KissOfYouth", None, (3, 3), kind="Spell", spell="KissOfYouth", pre=["Lovestruck", "SirenSong"]),
        Node("Serenade", "serenade", (5, 3), kind="Augment", ranks=2, pre=["SirenSong", "Ardor"], mods=[spellmod(["SirenSong"], radius=0.2, duration=0.3)]),
        Node("HeartOfStone", "heart of stone", (0, 4), ranks=3, pre=["Shattered"], stats={"AA_WardMax": 8, "ArmorRating_Sharp": 0.03}),
        Node("LoversQuarrel", None, (2, 4), kind="Spell", spell="LoversQuarrel", pre=["Shattered", "KissOfYouth"]),
        Node("AgelessBeauty", "ageless beauty", (4, 4), kind="Notable", ranks=2, pre=["KissOfYouth", "Serenade"],
             passives=[P("P_HealOnCast", amount=3.0, tag="Desire")], stats={"AA_Lifesteal": 0.02}, desc="Each spell of desire restores a little of you."),
        Node("Admirers", "admirers", (6, 4), ranks=3, pre=["Serenade"], stats={"AA_DurationFactor": 0.05}),
        Node("EssenceFeast", None, (1, 5), kind="Spell", spell="EssenceFeast", pre=["HeartOfStone", "LoversQuarrel"]),
        Node("Heartstopper", "heartstopper", (3, 5), kind="Notable", ranks=2, pre=["LoversQuarrel", "AgelessBeauty"],
             passives=[P("P_TriggerSpell", trigger="OnStackTrigger", spell=S("BlownKiss"), chancePerRank=0.5, target="NearestEnemy", stackHediff=INF)],
             desc="Whenever someone falls under your spell, you blow a kiss at the next one."),
        Node("Irresistible", None, (5, 5), kind="Spell", spell="Irresistible", pre=["AgelessBeauty", "Admirers"]),
        Node("Flirt", "flirt", (0, 6), ranks=3, pre=["EssenceFeast"], stats={"AA_CritChance": 0.02, "AA_CritDamage": 0.05}),
        Node("StolenHeartbeat", None, (2, 6), kind="Spell", spell="StolenHeartbeat", pre=["EssenceFeast", "Heartstopper"]),
        Node("Devotion", "devotion", (4, 6), kind="Notable", ranks=2, pre=["Heartstopper", "Irresistible"], mods=[tagmod("Desire", damage=0.08, critChance=0.03)]),
        Node("Infatuated", "infatuated", (6, 6), ranks=3, pre=["Irresistible"], pct={"AA_SpellPower": 0.03}),
        Node("FemmeFatale", "femme fatale", (2, 7), kind="Keystone", excl="SuccubusKeystone", pre=["StolenHeartbeat", "Flirt"],
             mods=[spellmod(["Heartbreak", "StolenHeartbeat"], cooldown=-0.35), tagmod("Desire", damage=0.15)],
             desc="Love kills. Heartbreak and Stolen Heartbeat come back far sooner and all your desire hits harder."),
        Node("BelovedOfAll", "beloved of all", (4, 7), kind="Keystone", excl="SuccubusKeystone", pre=["Devotion", "Infatuated"],
             passives=[P("P_OnHitHediff", hediff=INF, chancePerRank=0.6, severity=1.0, stack=True, maxSeverity=10.0, durationTicks=1800, tag="Desire")],
             mods=[tagmod("Desire", duration=0.3)], desc="Everyone falls for you. Most of your spells lay an extra stack of Infatuation, and your charms last longer."),
        Node("QueenOfHearts", "queen of hearts", (3, 8), kind="Keystone", pre=["FemmeFatale", "BelovedOfAll"], pct={"AA_SpellPower": 0.15},
             passives=[P("P_Aura", radius=6.0, interval=180, affects="Enemies", onlyInCombat=True, color=(1.0, 0.37, 0.64, 1.0), effects=[infatuate(1)])],
             desc="Hearts flutter wherever you walk: enemies near you steadily become infatuated."),
    )

    # ------------------------------------------------------------------ TEMPTRESS
    tp = ClassSpec("Temptress", "temptress", color="#ff7ab8", color2="#fff0f6", icon=("lips", "crown"), parent=sc, order=0, school=TEMPT,
        tagline="Make them yours. Enthral single targets, seduce whole squads, and lead an army of the damned.",
        desc="A Temptress does not win battles; she collects them.",
        playstyle="The domination specialist. Enthrall and Heartstrings pile on Infatuation, Mass Seduction turns an area into a trap of hearts, Kiss "
                  "of Betrayal takes an enemy for your own outright, and the Harem of the Damned enthralls every enemy around you at once. "
                  "Wants Attunement and Celerity.",
        lore="Her bodyguards are all ex-raiders. None of them can remember why they switched sides, only that it was worth it.",
        primary="ATT", secondary="CEL", tags=["Desire", "Charm"], height=8)
    tp.add_spells(
        Spell("Enthrall", "enthrall", "Hold an enemy's gaze until it forgets everything else: transfixed for six seconds, and three stacks of Infatuation.",
              icon=("eye", "heart"), mana=30, cd=16, range=24, warmup=0.6, tags=["Desire", "Charm"], role="Control", impact_vfx=KISS_HIT,
              effects=[hediff("AA_Transfixed", 1.0), infatuate(3)]),
        Spell("Heartstrings", "heartstrings", "Pluck the heartstrings of seven enemies in a chain: each one is stung and grows more infatuated.",
              icon=("heart", "chain"), mana=28, cd=12, range=26, warmup=0.5, tags=["Desire", "Charm"], delivery="Chain", chainCount=6, chainRange=7.0,
              chainFalloff=0.95, impact_vfx=KISS_HIT, effects=[dmg(10, "AA_Heartbreak"), infatuate(1)]),
        Spell("Jealousy", "jealousy", "Plant jealousy in one enemy's heart: it flies into a rage against its own side and grows more infatuated with you.",
              icon=("heartcrack", "eye"), mana=26, cd=22, range=26, warmup=0.5, tags=["Desire", "Charm"], role="Control", tier=2, impact_vfx=KISS_HIT,
              effects=[mental("Berserk", humanlikeOnly=False), infatuate(2)]),
        Spell("MassSeduction", "mass seduction", "Turn an area into a garden of temptation for fifteen seconds: every enemy inside gains Infatuation every two "
              "seconds.", icon=("rose", "heart"), mana=50, cd=45, range=28, warmup=1.0, tags=["Desire", "Charm"], role="Control", target="enemy_area",
              ai_min=3, tier=3, impact_vfx=[ring(0.2, 1.0, 30), burst("petal", 24, 2.0, 0.6, byRadius=True)],
              effects=[field(5.0, 900, 120, [infatuate(1, affects="Enemies")], groundTex="ArcanumAscendant/Vfx/Circle_Desire", groundSpin=2.0, groundAlpha=0.5,
                             ambientFleck="AA_Fleck_Petal", ambientDensity=3.0, ambientSpeed=0.8)]),
        Spell("KissOfBetrayal", "kiss of betrayal", "A kiss that rewrites loyalty. For fifteen seconds an enemy (not too large, and never a boss) fights for you.",
              icon=("lips", "dagger"), mana=60, cd=60, range=6, warmup=1.0, tags=["Desire", "Charm"], role="Control", tier=3,
              impact_vfx=KISS_HIT + [ring(0.2, 0.9, 24, byRadius=False)], effects=[dominate(900, maxBodySize=2.5), infatuate(2)]),
        Spell("HaremOfTheDamned", "harem of the damned", "Reveal your true face. Every enemy within twelve cells is overwhelmed with five stacks of Infatuation: "
              "all but the mightiest are enthralled and fight for you.", icon=("crown", "heart"), mana=220, cd=300, warmup=2.5, tags=["Desire", "Charm"],
              target="self", delivery="Nova", radius=12.0, ultimate=True, tier=5, xp=2.0, shake=1.5, ai_min=4, sound_cast="AA_Heartbeat",
              cast_vfx=[pillar(2.6, 70), screen(ROSE, 0.35, 50)],
              impact_vfx=[ring(0.1, 1.2, 50), burst("petal", 50, 4.0, 0.7, byRadius=True), flash(6.0, color=BLUSH)],
              effects=[infatuate(5)]),
    )
    tp.add_nodes(
        Node("Enthrall", None, (2, 0), kind="Spell", spell="Enthrall"),
        Node("Captivating", "captivating", (4, 0), ranks=3, mods=[tagmod("Charm", duration=0.06)]),
        Node("DeepGaze", "deep gaze", (1, 1), kind="Augment", ranks=2, pre=["Enthrall"], mods=[spellmod(["Enthrall"], duration=0.3, cooldown=-0.1)]),
        Node("Heartstrings", None, (3, 1), kind="Spell", spell="Heartstrings", pre=["Enthrall", "Captivating"]),
        Node("Temptation", "temptation", (5, 1), ranks=3, pre=["Captivating"], stats={"AA_ManaRegen": 0.1}),
        Node("Puppeteer", "puppeteer", (0, 2), ranks=3, pre=["DeepGaze"], stats={"AA_DurationFactor": 0.05}),
        Node("Jealousy", None, (2, 2), kind="Spell", spell="Jealousy", pre=["DeepGaze", "Heartstrings"]),
        Node("Tangled", "tangled", (4, 2), kind="Augment", ranks=2, pre=["Heartstrings", "Temptation"], mods=[spellmod(["Heartstrings"], extraTargets=2, damage=0.1)],
             desc="Two more hearts per rank."),
        Node("Siren", "siren", (6, 2), ranks=3, pre=["Temptation"], stats={"AA_CastSpeed": 0.04}),
        Node("GreenEyed", "green-eyed", (1, 3), kind="Augment", pre=["Jealousy"], mods=[spellmod(["Jealousy"], cooldown=-0.3, extraTargets=1)],
             desc="Jealousy spreads to a second enemy."),
        Node("MassSeduction", None, (3, 3), kind="Spell", spell="MassSeduction", pre=["Jealousy", "Tangled"]),
        Node("Adoration", "adoration", (5, 3), kind="Notable", ranks=2, pre=["Tangled", "Siren"], passives=[P("P_ManaOnHit", amount=2.0, chance=0.5, tag="Charm")],
             desc="Their adoration feeds you."),
        Node("Paramour", "paramour", (2, 4), kind="Notable", ranks=2, pre=["GreenEyed", "MassSeduction"],
             passives=[P("P_TriggerSpell", trigger="OnStackTrigger", spell=S("Heartstrings"), chancePerRank=0.4, target="NearestEnemy", stackHediff=INF)],
             desc="Each enthralment sends your heartstrings leaping to the next enemy."),
        Node("KissOfBetrayal", None, (4, 4), kind="Spell", spell="KissOfBetrayal", pre=["MassSeduction", "Adoration"]),
        Node("Garden", "garden of delights", (1, 5), kind="Augment", ranks=2, pre=["Paramour"], mods=[spellmod(["MassSeduction"], radius=0.25, duration=0.3)]),
        Node("Mistress", "mistress", (3, 5), ranks=3, pre=["Paramour", "KissOfBetrayal"], pct={"AA_SpellPower": 0.03}),
        Node("Faithless", "faithless", (5, 5), kind="Augment", pre=["KissOfBetrayal"], mods=[spellmod(["KissOfBetrayal"], duration=0.6, cooldown=-0.25)]),
        Node("SirenQueen", "siren queen", (2, 6), kind="Keystone", excl="TemptressKeystone", pre=["Garden", "Mistress"],
             mods=[tagmod("Charm", duration=0.5, radius=0.2)], desc="Your charms last half again as long and reach farther."),
        Node("Heartless", "heartless", (4, 6), kind="Keystone", excl="TemptressKeystone", pre=["Mistress", "Faithless"],
             passives=[P("P_KillExplosion", damageDef="AA_Heartbreak", damage=30.0, radius=3.0, chance=0.6, spellKillsOnly=False)],
             mods=[tagmod("Desire", damage=0.2)], desc="When your lovers die, their broken hearts burst and wound everyone nearby."),
        Node("HaremOfTheDamned", None, (3, 7), kind="Ultimate", spell="HaremOfTheDamned", pre=["SirenQueen", "Heartless"]),
    )

    # ------------------------------------------------------------------ THIEF OF YEARS
    ty = ClassSpec("ThiefOfYears", "thief of years", color="#ffb0d0", color2="#8a2050", icon=("lips", "hourglass"), parent=sc, order=1, school=YEARS,
        tagline="Beauty is eternal if someone else pays for it. Steal years from enemies, give them to friends, never grow old.",
        desc="Thieves of Years drink time itself. Their enemies wither into grey old men in a single battle; their friends grow young again.",
        playstyle="A drain-and-sustain mage. Withering Kiss and Time's Toll age enemies until their bodies fail, Decay keeps aging them for "
                  "fifteen seconds, and the stolen years heal and rejuvenate your allies. Eternal Beauty ages a whole battlefield by a decade "
                  "and makes you deathless. Wants Focus and Resilience.",
        lore="She is older than the colony, older than the ship it crashed in. She looks twenty-three.",
        primary="FOC", secondary="RES", tags=["Desire", "Time"], height=8)
    ty.add_spells(
        Spell("WitheringKiss", "withering kiss", "A long, cold kiss: the target ages five years, may develop the ailments of old age, and very old enemies crumble. "
              "You grow a year younger.", icon=("lips", "skull"), mana=34, cd=20, range=6, warmup=0.8, tags=["Desire", "Time"], impact_vfx=KISS_HIT,
              effects=[agesh(5.0, ailmentChance=0.35, lethalAgeFactor=1.4), dmg(16, "AA_Heartbreak"), infatuate(1)], caster_effects=[agesh(-1.0)]),
        Spell("StolenYouth", "stolen youth", "Pour stolen years into an ally: they grow three years younger, an old ailment may lift, scars fade and wounds close.",
              icon=("hourglass", "heart"), mana=40, cd=60, range=20, warmup=1.0, tags=["Desire", "Healing"], role="Heal", target="ally", ai_hp=0.5,
              impact_vfx=[pillar(1.0, 40), burst("petal", 12, 1.5, 0.5)], effects=[agesh(-3.0, cureConditions=1, healScars=1), heal(25)]),
        Spell("TimesToll", "time's toll", "Collect what time is owed from an area: everyone inside ages three years, slows to a crawl and takes heartbreak damage.",
              icon=("clock", "lips"), mana=44, cd=30, range=26, warmup=0.8, tags=["Desire", "Time"], target="enemy_area", radius=3.5, ai_min=2, tier=2,
              impact_vfx=[ring(0.2, 1.0, 26), burst("clock", 12, 1.5, 0.6, byRadius=True)],
              effects=[agesh(3.0, ailmentChance=0.2, lethalAgeFactor=1.5), hediff("AA_Slowed", 1.0), dmg(12, "AA_Heartbreak")]),
        Spell("Decay", "decay", "Set an enemy's years running: for fifteen seconds it ages a year every three seconds, and the years hurt.", icon=("hourglass", "skull"),
              mana=36, cd=30, range=26, warmup=0.6, tags=["Desire", "Time"], role="Control", tier=2, impact_vfx=KISS_HIT + [burst("clock", 6, 1.0, 0.5)],
              effects=[hediff("AA_Decaying", 1.0)]),
        Spell("SpringOfYouth", "spring of youth", "Spend stolen youth on everyone around you: allies within four cells heal, regenerate and lose a scar.", icon=("rose", "heart"),
              mana=45, cd=45, warmup=0.8, tags=["Desire", "Healing"], role="Heal", target="self", delivery="Nova", radius=4.0, affects="Allies", ai_hp=0.6,
              tier=3, impact_vfx=[ring(0.2, 1.0, 30), burst("petal", 24, 2.0, 0.6, byRadius=True)],
              effects=[heal(20), restore(scars=1), hediff("AA_Regeneration", 1.0)]),
        Spell("EternalBeauty", "eternal beauty", "Drink a whole battlefield's worth of youth. Every enemy within ten cells ages twelve years (the old simply crumble) "
              "and takes heartbreak damage; you grow five years younger and cannot die for a minute.", icon=("crown", "hourglass"), mana=220, cd=300,
              warmup=2.5, tags=["Desire", "Time"], target="self", delivery="Nova", radius=10.0, ultimate=True, tier=5, xp=2.0, shake=1.5, ai_min=3,
              cast_vfx=[pillar(2.6, 70)], caster_effects=[agesh(-5.0, cureConditions=2, healScars=2), hediff("AA_EternalBeauty", 1.0)],
              impact_vfx=[ring(1.2, 0.1, 50), spiral("clock", 20, -3.0, 0.6), burst("petal", 40, 4.0, 0.6, byRadius=True)],
              effects=[agesh(12.0, ailmentChance=0.6, lethalAgeFactor=1.2), dmg(40, "AA_Heartbreak")]),
    )
    ty.add_nodes(
        Node("WitheringKiss", None, (2, 0), kind="Spell", spell="WitheringKiss"),
        Node("Timeless", "timeless", (4, 0), ranks=3, stats={"AA_HealingPower": 0.05, "AA_Lifesteal": 0.01}),
        Node("LongKiss", "long kiss", (1, 1), kind="Augment", ranks=2, pre=["WitheringKiss"], mods=[spellmod(["WitheringKiss"], damage=0.2, range=0.3)]),
        Node("StolenYouth", None, (3, 1), kind="Spell", spell="StolenYouth", pre=["WitheringKiss", "Timeless"]),
        Node("Vintage", "vintage", (5, 1), ranks=3, pre=["Timeless"], stats={"AA_ManaMax": 12}),
        Node("Crowsfeet", "crow's feet", (0, 2), ranks=3, pre=["LongKiss"], mods=[tagmod("Time", damage=0.05)]),
        Node("TimesToll", None, (2, 2), kind="Spell", spell="TimesToll", pre=["LongKiss", "StolenYouth"]),
        Node("FountainOfYouth", "fountain of youth", (4, 2), kind="Augment", ranks=2, pre=["StolenYouth", "Vintage"],
             mods=[spellmod(["StolenYouth"], cooldown=-0.25, healing=0.2)]),
        Node("Decay", None, (6, 2), kind="Spell", spell="Decay", pre=["Vintage"]),
        Node("Hourglass", "hourglass", (1, 3), kind="Augment", ranks=2, pre=["Crowsfeet", "TimesToll"], mods=[spellmod(["TimesToll"], radius=0.2, damage=0.15)]),
        Node("SpringOfYouth", None, (3, 3), kind="Spell", spell="SpringOfYouth", pre=["TimesToll", "FountainOfYouth"]),
        Node("Rot", "rot", (5, 3), kind="Augment", ranks=2, pre=["FountainOfYouth", "Decay"], mods=[spellmod(["Decay"], duration=0.3, cooldown=-0.15)]),
        Node("TimeThief", "time thief", (2, 4), kind="Notable", ranks=2, pre=["Hourglass", "SpringOfYouth"], passives=[P("P_CooldownOnKill", ticksPerRank=120)],
             desc="Each life you end hands you a few of its seconds."),
        Node("Evergreen", "evergreen", (4, 4), kind="Notable", ranks=2, pre=["SpringOfYouth", "Rot"], passives=[P("P_HealOnCast", amount=4.0, radius=4.0)],
             desc="Every spell you cast sheds a little youth on the allies around you."),
        Node("BorrowedTime", "borrowed time", (1, 5), ranks=3, pre=["TimeThief"], stats={"AA_DurationFactor": 0.05}),
        Node("Agelessness", "agelessness", (3, 5), ranks=3, pre=["TimeThief", "Evergreen"], stats={"AA_WardMax": 10, "AA_HealingPower": 0.05}),
        Node("Senescence", "senescence", (5, 5), kind="Notable", pre=["Evergreen"], mods=[tagmod("Time", damage=0.15)],
             passives=[P("P_OnHitHediff", hediff="AA_Slowed", chancePerRank=0.2, severity=1.0, tag="Time")], desc="Your touch leaves enemies slow and tired."),
        Node("Methuselah", "methuselah", (2, 6), kind="Keystone", excl="ThiefKeystone", pre=["BorrowedTime", "Agelessness"],
             passives=[P("P_Rebirth", novaDamage=40.0, novaDamageDef="AA_Heartbreak", novaRadius=5.0, wardFraction=0.6, cooldownTicks=DAY,
                         color=(1.0, 0.69, 0.82, 1.0), labelKey="AA_ForeverYoung")],
             desc="You have outlived death before. Once a day, when you would die, you rise again young and whole."),
        Node("Graverobber", "graverobber", (4, 6), kind="Keystone", excl="ThiefKeystone", pre=["Agelessness", "Senescence"], stats={"AA_Lifesteal": 0.12},
             mods=[tagmod("Desire", damage=0.15)], desc="You take far more than years. Heavy lifesteal and more damage."),
        Node("EternalBeauty", None, (3, 7), kind="Ultimate", spell="EternalBeauty", pre=["Methuselah", "Graverobber"]),
    )

    # ------------------------------------------------------------------ NIGHTMARE MUSE
    nm = ClassSpec("NightmareMuse", "nightmare muse", color="#b07aff", color2="#ff9ce0", icon=("moon", "lips"), parent=sc, order=2, school=MUSE,
        tagline="Sweet dreams and night terrors. Sing enemies to sleep, feed on their dreams, wake their nightmares.",
        desc="A Nightmare Muse visits people in their sleep. Since she started doing it on the battlefield, nobody gets much sleep there.",
        playstyle="Control through sleep. Sleepsong and Endless Night put enemies into a dream that only pain can break; Dream Eater deals triple "
                  "damage to dreamers, Dread Dream sends them fleeing while the nightmare eats them, and Sleepwalk sets them wandering. "
                  "Wants Attunement and Focus.",
        lore="'I dreamt of her,' said the last raider standing. 'She was beautiful. Then she was hungry.'",
        primary="ATT", secondary="FOC", tags=["Desire", "Dream"], height=8)
    nm.add_spells(
        Spell("Sleepsong", "sleepsong", "Sing a lullaby over an area. Enemies inside fall into a dream for ten seconds and stand where they are until something hurts "
              "them.", icon=("moon", "note"), mana=30, cd=20, range=26, warmup=0.8, tags=["Desire", "Dream"], role="Control", target="enemy_area", radius=3.0,
              ai_min=2, impact_vfx=[ring(0.2, 1.0, 30), burst("star", 16, 1.0, 0.5, byRadius=True), glow(2.0, 3.0, 40)],
              effects=[hediff("AA_Dreaming", 1.0), infatuate(1)]),
        Spell("DreamEater", "dream eater", "Feed on an enemy's dreams: heartbreak damage, tripled against a dreamer, healing you for half and restoring mana.",
              icon=("moon", "lips"), mana=26, cd=10, range=26, warmup=0.5, tags=["Desire", "Dream"], impact_vfx=KISS_HIT,
              effects=[dmg(26, "AA_Heartbreak", bonusVsHediff="AA_Dreaming", bonusVsHediffFactor=3.0, healCasterFraction=0.5)], caster_effects=[mana(10)]),
        Spell("DreadDream", "dread dream", "Wake the worst nightmare an enemy has ever had. It flees in terror for six seconds while the dream eats at it.",
              icon=("moon", "skull"), mana=28, cd=16, range=26, warmup=0.5, tags=["Desire", "Dream"], role="Control", impact_vfx=KISS_HIT,
              effects=[hediff("AA_DreadDream", 1.0), infatuate(1)]),
        Spell("Sleepwalk", "sleepwalk", "Enemies in an area fall into a walking sleep for eight seconds and wander off, unaware of the battle.", icon=("moon", "arrow"),
              mana=34, cd=25, range=26, warmup=0.8, tags=["Desire", "Dream"], role="Control", target="enemy_area", radius=3.0, ai_min=2, tier=2,
              impact_vfx=[ring(0.2, 1.0, 26), burst("star", 12, 1.0, 0.5, byRadius=True)], effects=[hediff("AA_Sleepwalking", 1.0)]),
        Spell("LucidDream", "lucid dream", "Let an ally fight as if in a lucid dream for thirty seconds: time slows, aim sharpens and pain becomes just an idea.",
              icon=("star", "eye"), mana=26, cd=35, range=22, warmup=0.4, tags=["Desire", "Dream"], role="Buff", target="ally", tier=2,
              impact_vfx=[burst("star", 12, 1.5, 0.5), glow(1.6, 2.4, 30)], effects=[hediff("AA_LucidDream", 1.0)]),
        Spell("EndlessNight", "endless night", "Every enemy on the map falls into a deep dream. Five seconds later you feast on every dream at once: heartbreak "
              "damage, tripled against those still dreaming.", icon=("moon", "eclipse"), mana=220, cd=300, warmup=3.0, tags=["Desire", "Dream"],
              target="self", delivery="Global", ultimate=True, tier=5, xp=2.0, shake=1.0, ai_min=4,
              cast_vfx=[pillar(2.4, 70), screen("#3a1a6a", 0.4, 80, vignette=True)], impact_vfx=[burst("star", 6, 1.0, 0.5)],
              effects=[hediff("AA_Dreaming", 1.0, duration=900), infatuate(2),
                       sequence(step(300, [dmg(30, "AA_Heartbreak", bonusVsHediff="AA_Dreaming", bonusVsHediffFactor=3.0)], radius=0.0,
                                     vfx=[burst("petal", 8, 2.0, 0.5), flash(2.0)], sound="AA_Heartbeat"))]),
    )
    nm.add_nodes(
        Node("Sleepsong", None, (2, 0), kind="Spell", spell="Sleepsong"),
        Node("Moonlit", "moonlit", (4, 0), ranks=3, stats={"AA_ManaRegen": 0.08, "AA_CastSpeed": 0.02}),
        Node("Hush", "hush", (1, 1), kind="Augment", ranks=2, pre=["Sleepsong"], mods=[spellmod(["Sleepsong"], radius=0.2, duration=0.3)]),
        Node("DreamEater", None, (3, 1), kind="Spell", spell="DreamEater", pre=["Sleepsong", "Moonlit"]),
        Node("Reverie", "reverie", (5, 1), ranks=3, pre=["Moonlit"], mods=[tagmod("Dream", damage=0.05)]),
        Node("Drowsy", "drowsy", (0, 2), ranks=3, pre=["Hush"], stats={"AA_DurationFactor": 0.04}),
        Node("DreadDream", None, (2, 2), kind="Spell", spell="DreadDream", pre=["Hush", "DreamEater"]),
        Node("Gluttony", "gluttony", (4, 2), kind="Augment", ranks=2, pre=["DreamEater", "Reverie"], mods=[spellmod(["DreamEater"], damage=0.2, cooldown=-0.1)]),
        Node("Somnolence", "somnolence", (6, 2), kind="Notable", pre=["Reverie"],
             passives=[P("P_OnHitHediff", hediff="AA_Dreaming", chancePerRank=0.12, severity=1.0, tag="Dream", cooldownTicks=300)],
             desc="Dream spells sometimes send their target straight to sleep."),
        Node("Sleepwalk", None, (1, 3), kind="Spell", spell="Sleepwalk", pre=["Drowsy", "DreadDream"]),
        Node("Incubation", "incubation", (3, 3), ranks=3, pre=["DreadDream", "Gluttony"], pct={"AA_SpellPower": 0.03}),
        Node("LucidDream", None, (5, 3), kind="Spell", spell="LucidDream", pre=["Gluttony", "Somnolence"]),
        Node("Nightmarish", "nightmarish", (2, 4), kind="Augment", ranks=2, pre=["Sleepwalk", "Incubation"],
             mods=[spellmod(["DreadDream", "Sleepwalk"], duration=0.3, cooldown=-0.1)]),
        Node("DreamWalker", "dream walker", (4, 4), kind="Notable", ranks=2, pre=["Incubation", "LucidDream"], passives=[P("P_DodgeBlink", chancePerRank=0.06)],
             desc="Half of you is always somewhere in a dream; blows sometimes find only that half."),
        Node("Lucidity", "lucidity", (1, 5), ranks=3, pre=["Nightmarish"], stats={"AA_CritChance": 0.02}),
        Node("Insomnia", "insomnia", (3, 5), kind="Notable", ranks=2, pre=["Nightmarish", "DreamWalker"], passives=[P("P_ManaOnKill", amount=10.0)],
             desc="Every dream that ends for good feeds you."),
        Node("Starlight", "starlight", (5, 5), ranks=3, pre=["DreamWalker"], stats={"AA_WardMax": 10}),
        Node("SandmansSong", "sandman's song", (2, 6), kind="Keystone", excl="MuseKeystone", pre=["Lucidity", "Insomnia"],
             mods=[spellmod(["Sleepsong", "Sleepwalk"], radius=0.4, cooldown=-0.3)], desc="Your lullabies carry much farther and come back sooner."),
        Node("NightmareQueen", "nightmare queen", (4, 6), kind="Keystone", excl="MuseKeystone", pre=["Insomnia", "Starlight"],
             mods=[spellmod(["DreamEater"], extraTargets=2), tagmod("Dream", damage=0.2)], desc="Dream Eater feasts on three dreamers at once, and every "
             "dream you touch bites harder."),
        Node("EndlessNight", None, (3, 7), kind="Ultimate", spell="EndlessNight", pre=["SandmansSong", "NightmareQueen"]),
    )
    return sc, [tp, ty, nm], statuses()

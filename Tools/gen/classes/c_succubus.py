"""
The Succubus (women only; men walk the Incubus path).

Each tree plays its own game and none repeats another:
  Succubus (base)  Infatuation: lay it, grow it, gather it, spend it (damage, capture, rage) and blow up its payoff, the
                   Enthralled thrall. At home: love and family, from the first spark to twins.
  Temptress        The court: a few chosen enemies become her favourites, court her while she or her champion is near and
                   defect for good when the courtship is won. She lays no Infatuation at all.
  Thief of Years   The only one who moves years: she banks a Hoard of Years from the living and the dead and spends it on
                   youth, mothers, children and time itself.
  Nightmare Muse   The only one who touches sleep: she puts enemies to sleep and works on the sleepers, and keeps the
                   colony's nights sweet.
No spell anywhere boosts fertility: Quickening simply makes it happen.
"""
from classes.common import *  # noqa: F401,F403
from classes.expansion2 import missiles, channel, delayed  # noqa: F401

ROSE, BLUSH = "#ff5fa2", "#ffd1e8"
DESIRE = School("Desire", ROSE, BLUSH, fleck="petal", fleck2="star", projectile="ArcanumAscendant/Vfx/Orb")
TEMPT = School("Desire", "#ff7ab8", "#fff0f6", fleck="petal", fleck2="star", projectile="ArcanumAscendant/Vfx/Orb")
YEARS = School("Desire", "#ffb0d0", "#8a2050", fleck="clock", fleck2="petal", projectile="ArcanumAscendant/Vfx/Orb")
MUSE = School("Desire", "#b07aff", "#ff9ce0", fleck="star", fleck2="mote", projectile="ArcanumAscendant/Vfx/Orb")
MUSE_C, MUSE_C2 = "#b07aff", "#ff9ce0"
ROSE_RGBA = (1.0, 0.37, 0.64, 1.0)
MUSE_RGBA = (0.69, 0.48, 1.0, 1.0)

INF = "AA_Infatuation"
FAVOUR = "AA_Favoured"
CHAMPION = "AA_Champion"
HOARD = "AA_HoardOfYears"
DREAM = "AA_Dreaming"

KEYS = {
    "AA_Enthralled": "Enthralled",
    "AA_ForeverYoung": "Forever young",
    "AA_SuccubusWokeInLove": "Dreamt of love",
    "AA_SuccubusNotDreaming": "Not dreaming",
}


def infatuate(n=1, **kw):
    return stacks(INF, n, duration=1800, cap=10, **kw)


def bank(years):
    """Caster effect: add stolen years to the Thief's hoard (at most thirty)."""
    return stacks(HOARD, years, cap=30)


def spend(perYear, maxYears, without=0.5):
    """Caster effect: spend up to maxYears from the hoard, each adding perYear power; with an empty hoard the spell works at `without`."""
    return empower(HOARD, perStack=perYear, maxBonus=perYear * maxYears, maxSpend=maxYears, withoutFactor=without)


def hearts_due(minStacks, effects, hediff_=INF, **kw):
    """Effect_SuccubusHeartsDue: only targets carrying at least minStacks of the status are affected; it is spent and the inner effects play."""
    return O("Effect_SuccubusHeartsDue", stackHediff=hediff_, minStacks=float(minStacks), effects=list(effects), **kw)


def reconcile(opinion, mood=None, loveChance=0.3, **kw):
    """Effect_SuccubusReconcile: the target and the person they get on worst with make up."""
    return O("Effect_SuccubusReconcile", opinionThought=opinion, moodThought=mood, loveChance=float(loveChance), **kw)


def court(mode, **kw):
    """Effect_SuccubusCourt: Beckon, Betray, CastAside, Decree or Dismiss (see Colony_Succubus.cs)."""
    return O("Effect_SuccubusCourt", mode=mode, favour=FAVOUR, **kw)


KISS_HIT = [flash(1.8), burst("petal", 6, 2.0, 0.5), glow(1.0, 2.0, 18, useColor2=True)]
SOFT_HIT = [burst("petal", 12, 1.4, 0.5), glow(1.4, 2.2, 30, useColor2=True)]


def statuses():
    s = []
    # ------------------------------------------------------------------ base: Infatuation and love
    s.append(status(INF, "infatuation", "Hopelessly smitten. Infatuated creatures aim worse and strike weaker; at five stacks they are Enthralled and fight for "
        "the one they adore for twenty seconds. The very strongest need nine stacks and are merely stunned by it.", ROSE, bad=True, ticks=1800, maxSev=10,
        comps=[stack_trigger(5, boss=9, radius=3.0, color=ROSE, label="AA_Enthralled", shake=0.4, sound="AA_Heartbeat",
                             effects=[dominate(1200, maxBodySize=4.0), stun(150)], spread=[infatuate(1)],
                             vfx=[flash(3.0, color=BLUSH), burst("petal", 20, 3.0, 0.6), ring(0.2, 1.0, 24, byRadius=False)])],
        stages=[stage(offsets={"ShootingAccuracyPawn": -0.05, "MeleeHitChance": -0.05}, mult=True)],
        fx=dict(emitter="Charm", emitInterval=14, overlayTex="ArcanumAscendant/Vfx/Heart", overlaySize=0.45, overlayZ=1.05, overlayPulse=True)))
    s.append(status("AA_Beguiled", "beguiled", "Dazed by a siren's call, it wanders toward her without a thought of fighting. A hard enough blow will break the spell.",
        ROSE, bad=True, ticks=300, comps=[compel("Approach", breakOnDamage=True, breakDamage=12)], fx=dict(emitter="Charm", emitInterval=10)))
    s.append(status("AA_Charmed", "charmed", "Utterly charmed: it walks to her side and will not fight unless badly hurt.", ROSE, bad=True, ticks=480,
        comps=[compel("Approach", breakOnDamage=True, breakDamage=25)], fx=dict(emitter="Charm", emitInterval=8)))
    s.append(status("AA_Glamour", "glamour", "Wrapped in a glamour: some attacks simply fail, and whoever lands a blow anyway falls a little in love with the "
        "succubus who wove it.", BLUSH, bad=False, ticks=1800,
        comps=[deflect(0.15, False, color=BLUSH), O("ArcanumAscendant.HediffCompProperties_SuccubusAdoration", hediff=INF, cooldownTicks=60,
                                                    color=rgb(ROSE) + (1.0,))],
        stages=[stage(offsets={"SocialImpact": 0.5, "PawnBeauty": 2.0})], fx=dict(emitter="Charm", emitInterval=20)))
    s.append(status("AA_Swooning", "swooning", "Fainted dead away from sheer longing. It lies where it fell, unhurt and quite unable to fight, until the "
        "swoon passes: long enough to be carried off and taken prisoner.", ROSE, bad=True, ticks=5400,
        stages=[stage(extra={"capMods": [O(None, capacity="Consciousness", setMax=0.1)]})],
        fx=dict(emitter="Charm", emitInterval=16, overlayTex="ArcanumAscendant/Vfx/Heart", overlaySize=0.4, overlayZ=0.9, overlayPulse=True)))
    s.append(status("AA_KissOfLife", "kiss of life", "Kissed back onto their feet: for half a minute pain simply does not reach them. When the kiss "
        "fades, the pain comes back.", BLUSH, bad=False, ticks=1800, stages=[stage(pain=0.0, offsets={"PainShockThreshold": 0.5})],
        fx=dict(emitter="Charm", emitInterval=12)))
    s.append(status("AA_TwinBlessing", "blessing of twins", "A succubus has blessed this womb. The next birth within thirty days brings twins or "
        "triplets (for an animal, one or two extra litters).", ROSE, bad=False, ticks=30 * DAY, comps=[multi_birth(1, 2)],
        fx=dict(emitter="Charm", emitInterval=90)))
    s.append(opinion_def("AA_RedThreadOpinion", "red thread of fate", 20, days=10.0, desc="A red thread ties my heart to theirs."))
    s.append(thought_def("AA_RedThreadMood", "red thread of fate", "A succubus tied a red thread between my heart and someone else's. I can feel it tug.",
                         6, days=2.0))
    s.append(opinion_def("AA_MadeUpOpinion", "kissed and made up", 35, days=10.0, desc="We fought for so long. I can't even remember why any more."))
    s.append(thought_def("AA_MadeUpMood", "made up with an old rival", "An old feud is over. It feels like putting down something heavy.", 4, days=1.0))
    s.append(opinion_def("AA_KissOfLifeOpinion", "kissed me back to life", 12, days=10.0, desc="I was down, and she kissed me back onto my feet."))

    # ------------------------------------------------------------------ Temptress: the court
    s.append(status(FAVOUR, "favoured", "Chosen for a succubus's court. While she or her champion is near, this one courts her a little more every "
        "second, fighting ever more half-heartedly; when the courtship is complete it changes sides for good. Bosses and machines can only be humbled.",
        "#ff7ab8", bad=True, maxSev=1.0, initSev=0.05,
        comps=[O("ArcanumAscendant.HediffCompProperties_SuccubusCourtship", radius=9.0, perSecond=0.03, championHediff=CHAMPION,
                 sessionHediff="AA_CourtInSession", color=rgb("#ff7ab8") + (1.0,)),
               disappears(DAY // 2)],
        stages=[stage(label="smitten", factors={"MeleeDamageFactor": 0.85, "ShootingAccuracyPawn": 0.9}),
                stage(label="besotted", min_sev=0.5, factors={"MeleeDamageFactor": 0.6, "ShootingAccuracyPawn": 0.7, "AimingDelayFactor": 1.25})],
        fx=dict(emitter="Charm", emitInterval=12, overlayTex="ArcanumAscendant/Vfx/Heart", overlaySize=0.5, overlayZ=1.1, overlaySpin=1.0, overlayPulse=True)))
    s.append(status(CHAMPION, "her champion", "Chosen as a succubus's champion: proud, fierce and steady. Her favourites court her just as fast when they are "
        "near her champion as when they are near her.", "#ff7ab8", bad=False, ticks=HOUR * 12,
        stages=[stage(factors={"MeleeDamageFactor": 1.15, "ShootingAccuracyPawn": 1.1, "WorkSpeedGlobal": 1.05},
                      offsets={"MentalBreakThreshold": -0.08, "SocialImpact": 0.15})],
        fx=dict(emitter="Charm", emitInterval=40)))
    s.append(status("AA_CourtInSession", "court in session", "The whole battlefield is her court: every favourite courts her twice as fast.", "#ff7ab8",
        bad=False, ticks=3600, fx=dict(emitter="Charm", emitInterval=6)))
    s.append(status("AA_Decreed", "under decree", "Ordered by the one it adores to lay down its arms, and it does.", "#ff7ab8", bad=True, ticks=480,
        comps=[compel("Hold")], fx=dict(emitter="Charm", emitInterval=10)))
    s.append(status("AA_SweetTalking", "sweet-talking", "Still glowing from a shameless flirt: a harder bargain, a sweeter tongue.", BLUSH, bad=False,
        ticks=15000, stages=[stage(offsets={"TradePriceImprovement": 0.12, "NegotiationAbility": 0.3, "SocialImpact": 0.2})],
        fx=dict(emitter="Charm", emitInterval=60)))

    # ------------------------------------------------------------------ Thief of Years: the hoard
    s.append(status(HOARD, "hoard of years", "Years stolen from others, banked and waiting to be spent. Withering kisses, twilight years, the unlived "
        "years of the dead and every life she ends add to it; her gifts of youth, months and summers spend it. She can hold thirty.",
        "#ffb0d0", bad=False, maxSev=30.0, initSev=1.0,
        stages=[stage(label="a few years"), stage(label="a decade", min_sev=10.0), stage(label="two decades", min_sev=20.0),
                stage(label="a lifetime", min_sev=29.5)],
        fx=dict(emitter="Charm", emitInterval=120)))
    s.append(status("AA_Dotage", "twilight years", "Lent thirty years for twenty seconds: hands shake, knees creak and the eyes will not focus. The years "
        "go back to where they came from when it ends.", "#ffb0d0", bad=True, ticks=1200,
        stages=[stage(factors={"MoveSpeed": 0.6, "ShootingAccuracyPawn": 0.7, "MeleeHitChance": 0.75, "AimingDelayFactor": 1.4},
                      caps={"Consciousness": -0.1})],
        fx=dict(emitter="Wither", emitInterval=14)))
    s.append(status("AA_EternalBeauty", "eternal beauty", "She has drunk so much youth that death itself cannot keep her.", "#ffb0d0", bad=False, ticks=3600,
        comps=[death_ward("#ffb0d0", "AA_ForeverYoung")], fx=dict(emitter="Charm", emitInterval=12, overlayTex="ArcanumAscendant/Vfx/HexShield",
        overlaySize=1.8, overlayPulse=True)))

    # ------------------------------------------------------------------ Nightmare Muse: sleep and dreams
    s.append(status(DREAM, "dreaming", "Lost in a deep, sweet dream. It stands where it is, and only pain will wake it.", MUSE_C, bad=True, ticks=600,
        comps=[compel("Hold", breakOnDamage=True, breakDamage=1)], stages=[stage(caps={"Sight": -0.8})],
        fx=dict(emitter="Dream", emitInterval=12)))
    s.append(status("AA_DreadDream", "dread dream", "Trapped in a waking nightmare: it flees from shadows, and the dream is eating it alive.", MUSE_C, bad=True,
        ticks=360, comps=[compel("Flee"), dot("AA_Heartbreak", 3.0, MUSE_C)], fx=dict(emitter="Dream", emitInterval=8)))
    s.append(status("AA_Sleepwalking", "sleepwalking", "Wandering in its sleep, unaware of the battle around it. Pain does not wake it.", MUSE_C, bad=True,
        ticks=720, comps=[compel("Wander")], fx=dict(emitter="Dream", emitInterval=12)))
    s.append(status("AA_BelovedDream", "dream of the beloved", "Tonight this sleeper will dream of the one they love. After at least four hours of "
        "sleep they wake glowing, rested and thinking only of their beloved.", MUSE_C, bad=False, ticks=DAY,
        comps=[wake_thought(thought="AA_BelovedDreamMood", social="AA_BelovedDreamOpinion", subject="Lover", minSleepHours=4.0, rest=0.15, joy=0.25,
                            text="AA_SuccubusWokeInLove", color=MUSE_C2)],
        fx=dict(emitter="Dream", emitInterval=45)))
    s.append(status("AA_Drowsy", "drowsy", "Its sleep has been stolen: eyelids drooping, it moves and aims like a sleepwalker.", MUSE_C, bad=True, ticks=1800,
        stages=[stage(factors={"MoveSpeed": 0.75, "AimingDelayFactor": 1.3}, caps={"Consciousness": -0.15})], fx=dict(emitter="Dream", emitInterval=14)))
    s.append(status("AA_MoonlitRest", "moonlit rest", "Sleeping near a Nightmare Muse, who keeps the bad dreams away: rest comes faster and the mind is "
        "steadier.", MUSE_C, bad=False, ticks=2500, stages=[stage(factors={"RestRateMultiplier": 1.3}, offsets={"MentalBreakThreshold": -0.03})]))
    s.append(status("AA_Untroubled", "untroubled", "A dreamcatcher hangs nearby. The nights are quiet and the days are a little easier to bear.", MUSE_C,
        bad=False, ticks=HOUR * 2, stages=[stage(factors={"RestRateMultiplier": 1.15}, offsets={"MentalBreakThreshold": -0.05})]))
    s.append(thought_def("AA_BelovedDreamMood", "dreamt of love", "I dreamt of love all night long, and I woke up smiling.", 8, days=1.0))
    s.append(opinion_def("AA_BelovedDreamOpinion", "dreamt of them", 15, days=5.0, desc="I dreamt of them all night. I can't stop thinking about them."))
    s.append(thought_def("AA_SweetOblivionMood", "slept it off", "Whatever came over me, I slept it off. I feel strangely light.", 5, days=1.0))
    s += things.totem("Construct_SuccubusDreamcatcher", "dreamcatcher",
        "A hoop of moonlit thread and stolen dreams, hung by a Nightmare Muse. Every hour it calms anyone near it who is having a (non-violent) "
        "breakdown and keeps everyone's nights quiet.", color=MUSE_C, color2=MUSE_C2, hp=200, lifetime=DAY * 3, radius=6.0, interval=HOUR,
        affects="Allies", glow=1.6, light=3.0, pulse=[calm(), hediff("AA_Untroubled", 1.0)])
    return s


def build():
    sc = ClassSpec("Succubus", "succubus", color=ROSE, color2=BLUSH, icon=("lips", "heart"), order=12, school=DESIRE, gender="Female", gender_group="Seduction",
        tagline="Make the enemy fall in love, then decide what their hearts are worth. At home, make the colony fall in love with each other.",
        desc="Succubi are demons of desire, or simply very persuasive mages. The difference matters little to the raiders who switch sides mid-battle, "
             "or to the colonists who suddenly find themselves married with twins on the way.",
        playstyle="Everything turns on Infatuation. Kisses, charms, songs and a glamour that makes attackers fall for your bodyguard lay it; at five "
                  "stacks a target is Enthralled and fights for you. Fickle Heart pulls a crowd's stacks onto one target, Heartbreak, Swoon and "
                  "Lovers' Quarrel spend them three different ways, and Last Embrace detonates a thrall into a burst of new hearts. At home she "
                  "ties couples, mends feuds, makes babies happen and doubles them. Only women can take this path. Wants Attunement and Focus.",
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
        Spell("Glamour", "glamour", "Weave a glamour around yourself or an ally for thirty seconds. One attack in seven simply fails, and every enemy that lands a "
              "blow on the wearer anyway gains a stack of Infatuation (once a second each). Glamour your toughest fighter and let the raid fall in love "
              "while it shoots.", icon=("mask", "star"), mana=24, cd=35, range=20, warmup=0.4, tags=["Desire", "Ward"], role="Buff",
              target="ally", impact_vfx=[burst("star", 12, 1.5, 0.5), glow(1.6, 2.4, 30)], effects=[hediff("AA_Glamour", 1.0)]),
        Spell("FickleHeart", "fickle heart", "Every heart is fickle. All the Infatuation on enemies within seven cells of the target flies to it and piles up "
              "there, plus one more stack: enthrall the captain of a crowd you have been flirting with, or build the nine stacks a boss needs.",
              icon=("heart", "swirl"), mana=30, cd=25, range=26, warmup=0.6, tags=["Desire", "Charm"], role="Control", tier=2, ai_min=3,
              impact_vfx=KISS_HIT + [ring(1.2, 0.1, 26, byRadius=False)],
              effects=[O("Effect_SuccubusGatherHearts", stackHediff=INF, gatherRadius=7.0, bonus=1.0)], flavor="You said you loved me! - I did. Then I saw her."),
        Spell("LoversQuarrel", "lovers' quarrel", "Whisper a terrible rumour into a crowd. Every enemy in the area carrying at least two stacks of Infatuation "
              "spends them and flies into a jealous rage, attacking whoever is nearest.", icon=("heartcrack", "dagger"), mana=40, cd=35, range=28,
              warmup=0.8, tags=["Desire", "Charm"], role="Control", target="enemy_area", radius=4.0, ai_min=3, tier=2,
              impact_vfx=[flash(2.0), burst("petal", 16, 2.5, 0.6, byRadius=True), ring(0.2, 1.0, 24)],
              effects=[hearts_due(2, [mental("Berserk", humanlikeOnly=False)], quiet=True)], flavor="She said WHAT about you?"),
        Spell("LastEmbrace", "last embrace", "Take an Enthralled thrall in your arms one last time. It is given back to its own side for a heartbeat and then "
              "bursts: 40 heartbreak damage and two stacks of Infatuation to every enemy within four cells, enough to enthrall the next ones.",
              icon=("lips", "burst"), mana=25, cd=15, range=24, warmup=0.4, tags=["Desire"], target="any_pawn", affects="AllButSelf", tier=3, autocast=False,
              shake=0.6, sound_impact="AA_Heartbeat", impact_vfx=[flash(3.0)],
              effects=[O("Effect_SuccubusLastEmbrace", damage=40.0, burstRadius=4.0, damageDef="AA_Heartbreak", spreadHediff=INF, spreadSeverity=2.0)],
              flavor="Goodbye, darling."),
        Spell("Swoon", "swoon", "Whisper something to an enemy that is already yours. With at least three stacks of Infatuation (all spent) it swoons, "
              "fainting dead away for a minute and a half, unhurt and helpless: long enough to carry it off as a prisoner. Bosses, machines and "
              "creatures much bigger than a person are unmoved.", icon=("feather", "heart"), mana=30, cd=25, range=24, warmup=0.6,
              tags=["Desire", "Charm"], role="Control", target="enemy", tier=2, autocast=False,
              impact_vfx=KISS_HIT + [burst("feather", 8, 1.0, 0.5)],
              effects=[hearts_due(3, [hediff("AA_Swooning", 1.0)], maxBodySize=2.5, fleshOnly=True)], flavor="Oh. Oh my."),
        Spell("KissOfLife", "kiss of life", "Kiss a fallen ally back onto their feet. For thirty seconds pain does not reach them at all, which is enough "
              "to stand up and get clear if it was only the pain that dropped them; they also heal 12 and think fondly of you (+12 opinion for ten "
              "days). When the kiss fades, the pain returns.", icon=("lips", "cross"), mana=30, cd=60, range=3.9, warmup=1.0,
              tags=["Desire", "Healing"], role="Heal", target="ally", tier=2, ai_hp=0.3,
              impact_vfx=[burst("petal", 12, 1.5, 0.5), glow(1.6, 2.4, 30), flash(1.8)],
              effects=[hediff("AA_KissOfLife", 1.0), heal(12), memory("AA_KissOfLifeOpinion", toCaster=True)]),
        Spell("Irresistible", "irresistible", "Turn the full force of your charm on a downed enemy or a prisoner. They fall hopelessly in love and join the colony.",
              icon=("heart", "crown"), mana=80, cd=DAY * 3 / SEC, range=4, warmup=6.0, tags=["Desire", "Utility"], role="Utility", target="any_pawn",
              affects="AllButSelf", quirky=True, impact_vfx=[pillar(1.0, 40), burst("petal", 20, 2.0, 0.6), flash(2.5)], effects=[recruit()],
              flavor="Nobody has ever said no. Nobody has ever wanted to."),
        Spell("RedThread", "red thread of fate", "Tie a red thread between a colonist and the one fate meant for them: their partner if they have one, "
              "otherwise their best match on the map. Both gain +20 opinion of each other for ten days and a +6 mood memory for two; if both are "
              "single adults who could love each other, they become lovers.", icon=("heart", "ring"), mana=50, cd=2000, range=30, warmup=2.0,
              tags=["Desire", "Utility"], role="Utility", target="ally", quirky=True,
              impact_vfx=SOFT_HIT + [ring(0.2, 0.9, 26, byRadius=False)],
              effects=[matchmake("BestMatch", relation="Lover", opinion="AA_RedThreadOpinion", mood="AA_RedThreadMood")],
              flavor="Some threads were always there. She just pulls them tight."),
        Spell("KissAndMakeUp", "kiss and make up", "Kiss a colonist on the cheek and the feud they nurse against their worst rival in the colony is "
              "over: both gain +35 opinion of each other for ten days and +4 mood for a day. One time in three, if both are single adults who could "
              "love each other, the old enemies fall in love instead.", icon=("lips", "heartcrack"), mana=40, cd=1000, range=24, warmup=1.5,
              tags=["Desire", "Utility"], role="Utility", target="ally", quirky=True, impact_vfx=SOFT_HIT,
              effects=[reconcile("AA_MadeUpOpinion", "AA_MadeUpMood", loveChance=0.33)],
              flavor="'I hate him.' 'I know, darling. Kiss him anyway.'"),
        Spell("Quickening", "quickening", "Lay a hand on a woman, her partner or a tame animal and bless them with a child. The woman and her partner "
              "conceive at once (Biotech), or the animal and the nearest mate of its kind within twenty cells do; an egg-layer's next eggs are fertile. "
              "The sterile, the too young and the too old cannot be helped.", icon=("flower", "heart"), mana=60, cd=2000,
              range=6, warmup=3.0, tags=["Desire", "Utility"], role="Utility", target="ally", quirky=True,
              impact_vfx=[burst("petal", 16, 1.2, 0.5), burst("star", 8, 1.0, 0.4), glow(1.6, 2.6, 40, useColor2=True)],
              effects=[conceive(mateRadius=20.0)]),
        Spell("TwinBlessing", "blessing of twins", "Bless a woman or a female animal, pregnant or not: her next birth within thirty days brings twins or "
              "triplets (even odds), or for an animal one or two extra litters. Pairs beautifully with Quickening.",
              icon=("star", "flower"), mana=80, cd=5000, range=6, warmup=3.0, tags=["Desire", "Utility"], role="Utility", target="ally", quirky=True,
              impact_vfx=[pillar(1.0, 40), burst("star", 14, 1.4, 0.5), burst("petal", 14, 1.4, 0.5)],
              effects=[hediff("AA_TwinBlessing", 1.0)], flavor="Two cradles. Just in case. Make it three."),
    )
    sc.add_nodes(
        Node("Allure", "allure", (1, 0), ranks=3, mods=[tagmod("Desire", damage=0.05)]),
        Node("SweetNothings", "sweet nothings", (3, 0), ranks=3, stats={"AA_ManaMax": 10, "AA_ManaRegen": 0.06}),
        Node("Coquette", "coquette", (5, 0), ranks=3, stats={"AA_CastSpeed": 0.03, "AA_CooldownReduction": 0.01}),
        Node("Captivate", None, (1, 1), kind="Spell", spell="Captivate", pre=["Allure"]),
        Node("RedThread", None, (2, 1), kind="Spell", spell="RedThread", pre=["SweetNothings"]),
        Node("Heartbreak", None, (3, 1), kind="Spell", spell="Heartbreak", pre=["SweetNothings"]),
        Node("Glamour", None, (5, 1), kind="Spell", spell="Glamour", pre=["Coquette"]),
        Node("Smitten", "smitten", (0, 2), kind="Augment", ranks=2, pre=["Captivate"], mods=[spellmod(["Captivate", "BlownKiss"], duration=0.25, range=0.1)]),
        Node("Swoon", None, (1, 2), kind="Spell", spell="Swoon", pre=["Captivate"]),
        Node("Lovestruck", "lovestruck", (2, 2), kind="Notable", ranks=2, pre=["Captivate", "Heartbreak"],
             passives=[P("P_OnHitHediff", hediff=INF, chancePerRank=0.15, severity=1.0, stack=True, maxSeverity=10.0, durationTicks=1800, tag="Desire")],
             desc="Desire spells sometimes leave an extra stack of Infatuation."),
        Node("KissAndMakeUp", None, (3, 2), kind="Spell", spell="KissAndMakeUp", pre=["RedThread", "Heartbreak"]),
        Node("SirenSong", None, (4, 2), kind="Spell", spell="SirenSong", pre=["Heartbreak", "Glamour"]),
        Node("KissOfLife", None, (5, 2), kind="Spell", spell="KissOfLife", pre=["Glamour"]),
        Node("Adored", "adored", (6, 2), kind="Augment", ranks=2, pre=["Glamour"], mods=[spellmod(["Glamour"], duration=0.3, cooldown=-0.15)],
             desc="Glamours last longer and come back sooner."),
        Node("Shattered", "shattered", (1, 3), kind="Augment", ranks=2, pre=["Smitten", "Lovestruck"], mods=[spellmod(["Heartbreak"], damage=0.2, cooldown=-0.1)]),
        Node("Quickening", None, (2, 3), kind="Spell", spell="Quickening", pre=["KissAndMakeUp"]),
        Node("FickleHeart", None, (3, 3), kind="Spell", spell="FickleHeart", pre=["Lovestruck", "SirenSong"]),
        Node("Serenade", "serenade", (5, 3), kind="Augment", ranks=2, pre=["SirenSong"], mods=[spellmod(["SirenSong"], radius=0.2, duration=0.3)]),
        Node("HeartOfStone", "heart of stone", (0, 4), ranks=3, pre=["Shattered"], stats={"AA_WardMax": 8, "ArmorRating_Sharp": 0.03}),
        Node("LoversQuarrel", None, (2, 4), kind="Spell", spell="LoversQuarrel", pre=["Shattered", "FickleHeart"]),
        Node("Matchmaker", "matchmaker", (3, 4), kind="Notable", ranks=2, pre=["Quickening", "FickleHeart"],
             mods=[spellmod(["RedThread", "KissAndMakeUp", "Quickening", "TwinBlessing"], cooldown=-0.2)],
             desc="Love is her trade. Red Thread, Kiss and Make Up, Quickening and the Blessing of Twins come back 20% sooner per rank."),
        Node("AgelessBeauty", "heart's ease", (4, 4), kind="Notable", ranks=2, pre=["FickleHeart", "Serenade"],
             passives=[P("P_HealOnCast", amount=3.0, tag="Desire")], stats={"AA_Lifesteal": 0.02}, desc="Each spell of desire restores a little of you."),
        Node("Admirers", "admirers", (6, 4), ranks=3, pre=["Serenade"], stats={"AA_DurationFactor": 0.05}),
        Node("LastEmbrace", None, (1, 5), kind="Spell", spell="LastEmbrace", pre=["HeartOfStone", "LoversQuarrel"]),
        Node("TwinBlessing", None, (2, 5), kind="Spell", spell="TwinBlessing", pre=["Matchmaker"]),
        Node("Heartstopper", "heartstopper", (3, 5), kind="Notable", ranks=2, pre=["LoversQuarrel", "AgelessBeauty"],
             passives=[P("P_TriggerSpell", trigger="OnStackTrigger", spell=S("BlownKiss"), chancePerRank=0.5, target="NearestEnemy", stackHediff=INF)],
             desc="Whenever someone falls under your spell, you blow a kiss at the next one."),
        Node("Irresistible", None, (5, 5), kind="Spell", spell="Irresistible", pre=["AgelessBeauty", "Admirers"]),
        Node("Flirt", "flirt", (0, 6), ranks=3, pre=["LastEmbrace"], stats={"AA_CritChance": 0.02, "AA_CritDamage": 0.05}),
        Node("SweetRelease", "sweet release", (2, 6), kind="Augment", ranks=2, pre=["LastEmbrace", "Heartstopper"],
             mods=[spellmod(["LastEmbrace"], damage=0.2, radius=0.15)], desc="Last Embrace bursts harder and wider."),
        Node("Devotion", "devotion", (4, 6), kind="Notable", ranks=2, pre=["Heartstopper", "Irresistible"], mods=[tagmod("Desire", damage=0.08, critChance=0.03)]),
        Node("Infatuated", "infatuated", (6, 6), ranks=3, pre=["Irresistible"], pct={"AA_SpellPower": 0.03}),
        Node("FemmeFatale", "femme fatale", (2, 7), kind="Keystone", excl="SuccubusKeystone", pre=["SweetRelease", "Flirt"],
             mods=[spellmod(["Heartbreak", "LastEmbrace"], cooldown=-0.35), tagmod("Desire", damage=0.15)],
             desc="Love kills. Heartbreak and Last Embrace come back far sooner and all your desire hits harder."),
        Node("BelovedOfAll", "beloved of all", (4, 7), kind="Keystone", excl="SuccubusKeystone", pre=["Devotion", "Infatuated"],
             passives=[P("P_OnHitHediff", hediff=INF, chancePerRank=0.6, severity=1.0, stack=True, maxSeverity=10.0, durationTicks=1800, tag="Desire")],
             mods=[tagmod("Desire", duration=0.3)], desc="Everyone falls for you. Most of your spells lay an extra stack of Infatuation, and your charms last longer."),
        Node("QueenOfHearts", "queen of hearts", (3, 8), kind="Keystone", pre=["FemmeFatale", "BelovedOfAll"], pct={"AA_SpellPower": 0.15},
             passives=[P("P_Aura", radius=6.0, interval=180, affects="Enemies", onlyInCombat=True, color=ROSE_RGBA, effects=[infatuate(1)])],
             desc="Hearts flutter wherever you walk: enemies near you steadily become infatuated."),
    )

    # ------------------------------------------------------------------ TEMPTRESS: the court
    tp = ClassSpec("Temptress", "temptress", color="#ff7ab8", color2="#fff0f6", icon=("lips", "crown"), parent=sc, order=0, school=TEMPT,
        tagline="Not for twenty seconds: for good. Hold court on the battlefield and take your favourites home.",
        desc="A Temptress does not win battles; she collects them. The base succubus borrows an enemy's heart for a moment. The Temptress keeps it.",
        playstyle="A court of three. Beckon makes an enemy your favourite (Infatuation it carried is a head start). Favourites fight half-heartedly "
                  "and court you every second they are near you or your Champion; at 100% they defect for good. Betray the ones who are halfway "
                  "there, Cast Aside the ones you are done with (the more they adored you, the harder they fall), Decree that the whole court "
                  "lays down arms, or send them home to warm their faction to the colony. At home she plays the diplomat: harder bargains, callers "
                  "at the gate, softer prisoners and polished manners. Wants Attunement and Celerity.",
        lore="Her bodyguards are all ex-raiders. None of them can remember why they switched sides, only that it was worth it.",
        primary="ATT", secondary="CEL", tags=["Desire", "Charm"], height=8)
    tp.add_spells(
        Spell("Beckon", "beckon", "Crook a finger at an enemy: it becomes one of your favourites (5% courted, plus 8% for every stack of Infatuation it "
              "carried, which is spent). Favourites fight half-heartedly and court you every second they are within nine cells of you or your "
              "Champion; at 100% they change sides for good. Your court has three seats: beckon a fourth and the least devoted loses interest. "
              "Machines cannot love; bosses can only be humbled.", icon=("hand", "heart"), mana=30, cd=8, range=26, warmup=0.5,
              tags=["Desire", "Charm"], role="Control", autocast=False, impact_vfx=KISS_HIT + [ring(0.2, 0.8, 20, byRadius=False)],
              effects=[court("Beckon", seats=3, startCourtship=0.05, headStartHediff=INF, headStartPerStack=0.08)],
              flavor="Come here. No, closer."),
        Spell("KissOfBetrayal", "kiss of betrayal", "Kiss a favourite who is at least halfway won. It does not wait for the rest of the courtship: it "
              "changes sides on the spot.", icon=("lips", "dagger"), mana=40, cd=30, range=8, warmup=0.8, tags=["Desire", "Charm"], role="Control",
              tier=2, autocast=False, impact_vfx=KISS_HIT + [ring(0.2, 0.9, 24, byRadius=False)], effects=[court("Betray", minCourtship=0.5)]),
        Spell("CastAside", "cast aside", "Throw a favourite over. It takes heartbreak damage (20, up to 80 for one who was fully won) and collapses for two to "
              "ten seconds, longer the more it adored you, and its seat is free again.", icon=("heartcrack", "hand"), mana=25, cd=12, range=26,
              warmup=0.4, tags=["Desire", "Charm"], tier=2, autocast=False, impact_vfx=KISS_HIT + [burst("blood", 6, 2.0, 0.5)],
              effects=[court("CastAside", damage=20.0, damagePerCourtship=60.0, stunTicks=120, stunPerCourtship=480)], flavor="It's not you. It's me. No, it's you."),
        Spell("ChampionsFavour", "champion's favour", "Name a colonist your champion for twelve hours: they fight a little harder, work a little faster, "
              "break less easily and speak more sweetly, and your favourites court you just as fast when they are near your champion as when "
              "they are near you, so you can hold court from the back line.", icon=("sword", "heart"), mana=30, cd=300, range=24, warmup=1.0,
              tags=["Desire", "Charm"], role="Buff", target="ally", impact_vfx=[pillar(0.8, 30), burst("petal", 14, 1.6, 0.5)],
              effects=[hediff(CHAMPION, 1.0)], flavor="Wear my colours."),
        Spell("RoyalDecree", "royal decree", "Speak, and your court obeys. Every favourite on the map lays down its arms and stands still for eight seconds, "
              "and courts you 15% more on the spot.", icon=("crown", "scroll"), mana=50, cd=45, warmup=1.0, tags=["Desire", "Charm"], role="Control",
              target="self", tier=3, autocast=False, sound_cast="AA_Heartbeat", cast_vfx=[pillar(1.6, 40), screen("#ff7ab8", 0.18, 30)],
              effects=[court("Decree", decreeHediff="AA_Decreed", decreeCourtship=0.15)]),
        Spell("HomewardHeart", "homeward heart", "Remind every favourite within twelve cells of the sweetheart waiting at home. They leave your court, give "
              "up the raid and flee the map, and their people think a little better of your colony for it (+2 goodwill each). Beasts just freeze "
              "for four seconds.", icon=("heart", "door"), mana=40, cd=60, warmup=1.0, tags=["Desire", "Charm"], role="Control", target="self",
              delivery="Nova", radius=12.0, tier=3, autocast=False, sound_cast="AA_Heartbeat",
              impact_vfx=[ring(0.1, 1.2, 40), burst("petal", 30, 3.0, 0.6, byRadius=True)], effects=[court("Dismiss", goodwillPerDismissed=2)],
              flavor="Go home. She's waiting. She always was."),
        Spell("HoneyedWords", "honeyed words", "Flirt shamelessly with a visitor, a trader or even a captive: their faction warms to the colony (+8 "
              "goodwill, except with factions that will never make peace), and for six hours you drive a harder bargain: +12% trade prices, +30% "
              "negotiation ability and +20% social impact.", icon=("coin", "lips"), mana=40, cd=1000, range=12, warmup=2.0,
              tags=["Desire", "Charm"], role="Utility", target="any_pawn", affects="AllButSelf", quirky=True,
              impact_vfx=SOFT_HIT, effects=[goodwill(8)], caster_effects=[hediff("AA_SweetTalking", 1.0)],
              flavor="'For you? Half price.' He has no idea why he said it."),
        Spell("GentlemenCallers", "gentlemen callers", "Let word of your beauty slip beyond the colony. Within moments admirers come calling: a trade "
              "caravan or a group of friendly visitors arrives at the edge of the map, if any faction is willing to send one.", icon=("rose", "bell"),
              mana=70, cd=3000, warmup=4.0, tags=["Desire", "Charm"], role="Utility", target="self", quirky=True,
              impact_vfx=[ring(0.2, 1.2, 40), burst("petal", 24, 2.5, 0.6), pillar(1.2, 40)],
              effects=[incident(["TraderCaravanArrival", "VisitorGroup"])], flavor="They always bring flowers. And silver."),
        Spell("FinishingSchool", "finishing school", "Take a colonist in hand for an afternoon of posture, conversation and devastating smiles. They gain "
              "3000 Social experience and their passion for Social deepens one step (none to interested, interested to burning).",
              icon=("book", "lips"), mana=50, cd=2000, range=6, warmup=4.0, tags=["Desire", "Charm"], role="Utility", target="ally", quirky=True,
              impact_vfx=SOFT_HIT + [burst("star", 8, 1.0, 0.4)], effects=[skillxp(3000, "Social"), passion("Upgrade", skill="Social")],
              flavor="Chin up. Shoulders back. Now: laugh at his joke."),
        Spell("HaremOfTheDamned", "queen's court", "Hold court over the whole battlefield. Every enemy within twelve cells becomes your favourite at 40% "
              "courted, seats or no seats, and for a minute every favourite courts you twice as fast.", icon=("crown", "heart"), mana=220, cd=300,
              warmup=2.5, tags=["Desire", "Charm"], target="self", delivery="Nova", radius=12.0, ultimate=True, tier=5, xp=2.0, shake=1.5, ai_min=4,
              sound_cast="AA_Heartbeat", cast_vfx=[pillar(2.6, 70), screen(ROSE, 0.35, 50)],
              impact_vfx=[ring(0.1, 1.2, 50), burst("petal", 50, 4.0, 0.7, byRadius=True), flash(6.0, color=BLUSH)],
              effects=[court("Beckon", ignoreSeats=True, startCourtship=0.4)], caster_effects=[hediff("AA_CourtInSession", 1.0)]),
    )
    tp.add_nodes(
        Node("Beckon", None, (2, 0), kind="Spell", spell="Beckon"),
        Node("Captivating", "captivating", (4, 0), ranks=3, mods=[tagmod("Charm", duration=0.06)], desc="Favourites court you a little faster, and charms last longer."),
        Node("HoneyedWords", None, (6, 0), kind="Spell", spell="HoneyedWords"),
        Node("ArdentSuitors", "ardent suitors", (1, 1), kind="Augment", ranks=2, pre=["Beckon"], mods=[spellmod(["Beckon"], duration=0.25)],
             desc="Favourites court you 25% faster per rank."),
        Node("KissOfBetrayal", None, (3, 1), kind="Spell", spell="KissOfBetrayal", pre=["Beckon", "Captivating"]),
        Node("Temptation", "temptation", (5, 1), ranks=3, pre=["Captivating"], stats={"AA_ManaRegen": 0.1}),
        Node("Puppeteer", "puppeteer", (0, 2), ranks=3, pre=["ArdentSuitors"], stats={"AA_DurationFactor": 0.05}),
        Node("CastAside", None, (2, 2), kind="Spell", spell="CastAside", pre=["ArdentSuitors", "KissOfBetrayal"]),
        Node("ChampionsFavour", None, (4, 2), kind="Spell", spell="ChampionsFavour", pre=["KissOfBetrayal", "Temptation"]),
        Node("Siren", "siren", (6, 2), ranks=3, pre=["Temptation"], stats={"AA_CastSpeed": 0.04}),
        Node("SweetCaptivity", "sweet captivity", (0, 3), kind="Notable", pre=["Puppeteer"],
             passives=[P("P_Aura", radius=9.0, interval=HOUR, affects="Allies", color=ROSE_RGBA,
                         effects=[prisoner(resistance=0.5, will=0.1, suppression=0.02)])],
             desc="Prisoners can't help falling for her. Every hour, prisoners within nine cells lose 0.5 resistance and 0.1 will, and slaves grow 2% "
                  "more suppressed (Ideology)."),
        Node("LargerCourt", "a larger court", (1, 3), kind="Augment", pre=["CastAside"], mods=[spellmod(["Beckon"], extraTargets=1)],
             desc="One more seat in your court."),
        Node("RoyalDecree", None, (3, 3), kind="Spell", spell="RoyalDecree", pre=["CastAside", "ChampionsFavour"]),
        Node("Adoration", "adoration", (5, 3), kind="Notable", ranks=2, pre=["ChampionsFavour", "Siren"], passives=[P("P_ManaOnHit", amount=2.0, chance=0.5, tag="Charm")],
             desc="Their adoration feeds you."),
        Node("GentlemenCallers", None, (6, 3), kind="Spell", spell="GentlemenCallers", pre=["Siren"]),
        Node("Paramour", "heartbreaker", (2, 4), kind="Notable", ranks=2, pre=["LargerCourt", "RoyalDecree"],
             mods=[spellmod(["CastAside"], damage=0.3, cooldown=-0.2)], desc="Cast Aside hits 30% harder and comes back 20% sooner per rank."),
        Node("FinishingSchool", None, (4, 4), kind="Spell", spell="FinishingSchool", pre=["RoyalDecree", "Adoration"]),
        Node("HomewardHeart", None, (5, 4), kind="Spell", spell="HomewardHeart", pre=["Adoration"]),
        Node("Garden", "courtly love", (1, 5), kind="Augment", ranks=2, pre=["Paramour"], mods=[spellmod(["RoyalDecree"], cooldown=-0.3, duration=0.3)],
             desc="Royal Decree comes back sooner and holds longer."),
        Node("Mistress", "mistress", (3, 5), ranks=3, pre=["Paramour", "FinishingSchool"], pct={"AA_SpellPower": 0.03}),
        Node("Faithless", "faithless", (5, 5), kind="Augment", pre=["HomewardHeart"], mods=[spellmod(["KissOfBetrayal"], cooldown=-0.4)],
             desc="Kiss of Betrayal comes back 40% sooner."),
        Node("SirenQueen", "siren queen", (2, 6), kind="Keystone", excl="TemptressKeystone", pre=["Garden", "Mistress"],
             mods=[tagmod("Charm", duration=0.5), spellmod(["Beckon"], extraTargets=1)],
             desc="Your court grows: one more seat, and every favourite courts you half again as fast."),
        Node("Heartless", "heartless", (4, 6), kind="Keystone", excl="TemptressKeystone", pre=["Mistress", "Faithless"],
             passives=[P("P_KillExplosion", damageDef="AA_Heartbreak", damage=30.0, radius=3.0, chance=0.6, spellKillsOnly=False)],
             mods=[spellmod(["CastAside", "KissOfBetrayal"], cooldown=-0.5), tagmod("Desire", damage=0.2)],
             desc="Love is a weapon. Cast Aside and Kiss of Betrayal come back twice as fast, and the hearts of those you kill burst and wound everyone nearby."),
        Node("HaremOfTheDamned", None, (3, 7), kind="Ultimate", spell="HaremOfTheDamned", pre=["SirenQueen", "Heartless"]),
    )

    # ------------------------------------------------------------------ THIEF OF YEARS: the hoard
    ty = ClassSpec("ThiefOfYears", "thief of years", color="#ffb0d0", color2="#8a2050", icon=("lips", "hourglass"), parent=sc, order=1, school=YEARS,
        tagline="Every year she gives away was stolen from someone. Bank the years of your enemies and spend them on the people you love.",
        desc="Thieves of Years drink time itself. Only they can take years from one body and put them in another, and they keep the difference.",
        playstyle="A banker of time. Withering Kiss and Twilight Years take years from enemies and Unlived Years harvests them from the fresh dead, "
                  "filling your Hoard of Years (thirty at most; every kill adds one with Hoarder). Everything else spends it: Stolen Youth, Borrowed "
                  "Months for mothers, Stolen Summers for children, Fast Forward over the workshop and fields, Time's Ruin over enemy walls, and "
                  "Eternal Beauty drinks the whole hoard at once. Spells cast on an empty hoard are much weaker. Wants Focus and Resilience.",
        lore="She is older than the colony, older than the ship it crashed in. She looks twenty-three.",
        primary="FOC", secondary="RES", tags=["Desire", "Time"], height=8)
    ty.add_spells(
        Spell("WitheringKiss", "withering kiss", "A long, cold kiss: the target ages five years, may develop the ailments of old age, takes heartbreak "
              "damage, and very old enemies crumble. You bank three years in your hoard.", icon=("lips", "skull"), mana=34, cd=20, range=6, warmup=0.8,
              tags=["Desire", "Time"], impact_vfx=KISS_HIT + [burst("clock", 6, 1.0, 0.5)],
              effects=[agesh(5.0, ailmentChance=0.35, lethalAgeFactor=1.4), dmg(16, "AA_Heartbreak")], caster_effects=[bank(3)]),
        Spell("UnlivedYears", "unlived years", "Every body that died young still owes the years it never lived. Collect them: the fresh corpse at the target "
              "crumbles to dust (any gear stays behind) and you bank four years. The colony's grave diggers will thank you.", icon=("hourglass", "skull"),
              mana=15, cd=10, range=12, warmup=1.0, tags=["Desire", "Time"], role="Utility", target="corpse", affects="Corpses", quirky=True,
              impact_vfx=[burst("clock", 10, 1.2, 0.5), smoke(3, 1.0)], effects=[devour(heal=0.0, mana=0.0, maxCorpses=1)], caster_effects=[bank(4)]),
        Spell("TimesToll", "twilight years", "Lend everyone in an area thirty years for twenty seconds: they move 40% slower, shake as they aim and stumble "
              "in melee, then get their youth back. You bank two years for the trouble.", icon=("clock", "lips"), mana=40, cd=30, range=26, warmup=0.8,
              tags=["Desire", "Time"], role="Control", target="enemy_area", radius=3.5, ai_min=2, tier=2,
              impact_vfx=[ring(0.2, 1.0, 26), burst("clock", 12, 1.5, 0.6, byRadius=True)], effects=[hediff("AA_Dotage", 1.0)], caster_effects=[bank(2)]),
        Spell("StolenYouth", "stolen youth", "Pour stolen years into an ally. Spends up to ten years from your hoard: they grow younger (two years, up to six "
              "with a full spend), an old ailment may lift, scars fade and 25 damage heals. On an empty hoard it barely works.", icon=("hourglass", "heart"),
              mana=40, cd=60, range=20, warmup=1.0, tags=["Desire", "Healing"], role="Heal", target="ally", ai_hp=0.5,
              impact_vfx=[pillar(1.0, 40), burst("petal", 12, 1.5, 0.5)], caster_effects=[spend(0.2, 10, without=0.3)],
              effects=[agesh(-2.0, cureConditions=1, healScars=1, scaleWithPower=True), heal(25)]),
        Spell("BorrowedMonths", "borrowed months", "Lend an expectant mother, person or animal some of your hoard. Spends up to three years: her pregnancy "
              "jumps four days ahead, up to about six with a full spend (never past the birth itself).", icon=("hourglass", "flower"), mana=45, cd=1000,
              range=6, warmup=2.0, tags=["Desire", "Time"], role="Utility", target="ally", quirky=True,
              impact_vfx=[burst("clock", 10, 1.2, 0.5), burst("petal", 8, 1.2, 0.5), glow(1.4, 2.2, 30)],
              caster_effects=[spend(0.45, 3, without=0.25)], effects=[gestate(4)]),
        Spell("StolenSummers", "stolen summers", "Pour summers stolen from your enemies into the children around a point. Spends up to four years: every child "
              "within four cells gains six growth points (more with a bigger spend) and half a bar of learning, and babies get to play (Biotech).",
              icon=("sun", "hourglass"), mana=50, cd=1000, range=24, warmup=2.0, tags=["Desire", "Time"], role="Utility", target="ally_area",
              radius=4.0, quirky=True, caster_effects=[spend(0.35, 4, without=0.25)],
              impact_vfx=[ring(0.2, 1.0, 30), burst("clock", 14, 1.5, 0.5, byRadius=True), burst("star", 10, 1.2, 0.5, byRadius=True)],
              effects=[child_growth(6, learning=0.5)]),
        Spell("FastForward", "fast forward", "Let a week pass in a heartbeat over one corner of the colony. Spends up to five years: within four cells, "
              "crops grow, bills in progress lose 800 work and construction frames with their materials gain 400 work, all more with a bigger spend.",
              icon=("clock", "hammer"), mana=60, cd=1500, range=24, warmup=2.0, tags=["Desire", "Time"], role="Utility", target="cell", radius=4.0,
              quirky=True, caster_effects=[spend(0.3, 5, without=0.25)],
              impact_vfx=[ring(0.2, 1.0, 30), spiral("clock", 16, 2.0, 0.5), burst("leaf", 10, 1.2, 0.5, byRadius=True)],
              effects=[grow(0.25), craft(800), construct(400)], flavor="Nobody saw the seasons turn. The barn just finished itself."),
        Spell("TimesRuin", "time's ruin", "Let a century pass over an area. Spends up to four years: walls, sandbags, turrets and every other building there "
              "that is not your colony's take 180 damage (up to about 360 with a full spend) as they crumble with age. Cover does not last forever.",
              icon=("hourglass", "wall"), mana=50, cd=35, range=28, warmup=1.0, tags=["Desire", "Time"], target="enemy_area", radius=3.0, tier=3, ai_min=2,
              caster_effects=[spend(0.25, 4, without=0.5)],
              impact_vfx=[ring(0.2, 1.0, 30), burst("clock", 18, 1.6, 0.6, byRadius=True), smoke(5, 1.2)],
              effects=[dmg(6, damageBuildings=True, buildingFactor=30.0)]),
        Spell("EternalBeauty", "eternal beauty", "Drink a whole battlefield's worth of youth and spend your entire hoard on it. Every enemy within ten cells "
              "ages twelve years (the old simply crumble) and takes heartbreak damage, 4% more for every banked year spent; you grow five years "
              "younger and cannot die for a minute.", icon=("crown", "hourglass"), mana=220, cd=300,
              warmup=2.5, tags=["Desire", "Time"], target="self", delivery="Nova", radius=10.0, ultimate=True, tier=5, xp=2.0, shake=1.5, ai_min=3,
              cast_vfx=[pillar(2.6, 70)], caster_effects=[empower(HOARD, perStack=0.04, maxBonus=1.2), agesh(-5.0, cureConditions=2, healScars=2),
                                                          hediff("AA_EternalBeauty", 1.0)],
              impact_vfx=[ring(1.2, 0.1, 50), spiral("clock", 20, -3.0, 0.6), burst("petal", 40, 4.0, 0.6, byRadius=True)],
              effects=[agesh(12.0, ailmentChance=0.6, lethalAgeFactor=1.2), dmg(40, "AA_Heartbreak")]),
    )
    ty.add_nodes(
        Node("WitheringKiss", None, (2, 0), kind="Spell", spell="WitheringKiss"),
        Node("Timeless", "timeless", (4, 0), ranks=3, stats={"AA_HealingPower": 0.05, "AA_Lifesteal": 0.01}),
        Node("LongKiss", "long kiss", (1, 1), kind="Augment", ranks=2, pre=["WitheringKiss"], mods=[spellmod(["WitheringKiss"], damage=0.2, range=0.3)]),
        Node("UnlivedYears", None, (3, 1), kind="Spell", spell="UnlivedYears", pre=["WitheringKiss", "Timeless"]),
        Node("Vintage", "vintage", (5, 1), ranks=3, pre=["Timeless"], stats={"AA_ManaMax": 12}),
        Node("Crowsfeet", "crow's feet", (0, 2), ranks=3, pre=["LongKiss"], mods=[tagmod("Time", damage=0.05)]),
        Node("TimesToll", None, (2, 2), kind="Spell", spell="TimesToll", pre=["LongKiss", "UnlivedYears"]),
        Node("StolenYouth", None, (4, 2), kind="Spell", spell="StolenYouth", pre=["UnlivedYears", "Vintage"]),
        Node("BorrowedMonths", None, (6, 2), kind="Spell", spell="BorrowedMonths", pre=["Vintage"]),
        Node("BorrowedTime", "borrowed time", (0, 3), ranks=3, pre=["Crowsfeet"], stats={"AA_DurationFactor": 0.05}),
        Node("Hourglass", "hourglass", (1, 3), kind="Augment", ranks=2, pre=["Crowsfeet", "TimesToll"], mods=[spellmod(["TimesToll"], radius=0.2, duration=0.3)],
             desc="Twilight Years reaches farther and lasts longer."),
        Node("Hoarder", "hoarder", (3, 3), kind="Notable", ranks=2, pre=["TimesToll", "StolenYouth"],
             passives=[P("P_StacksOnKill", hediff=HOARD, perKill=1.0, maxStacks=30.0)],
             desc="Nothing goes to waste. Every life you end adds a year (two at rank 2) to your hoard."),
        Node("FountainOfYouth", "fountain of youth", (5, 3), kind="Augment", ranks=2, pre=["StolenYouth", "BorrowedMonths"],
             mods=[spellmod(["StolenYouth"], cooldown=-0.25, healing=0.2)]),
        Node("Agelessness", "agelessness", (6, 3), ranks=3, pre=["BorrowedMonths"], stats={"AA_WardMax": 10, "AA_HealingPower": 0.05}),
        Node("TimesRuin", None, (2, 4), kind="Spell", spell="TimesRuin", pre=["Hourglass", "Hoarder"]),
        Node("StolenSummers", None, (4, 4), kind="Spell", spell="StolenSummers", pre=["Hoarder", "FountainOfYouth"]),
        Node("TimeThief", "time thief", (1, 5), kind="Notable", ranks=2, pre=["TimesRuin"], passives=[P("P_CooldownOnKill", ticksPerRank=120)],
             desc="Each life you end hands you a few of its seconds."),
        Node("FastForward", None, (3, 5), kind="Spell", spell="FastForward", pre=["TimesRuin", "StolenSummers"]),
        Node("Evergreen", "evergreen", (5, 5), kind="Notable", ranks=2, pre=["StolenSummers"], passives=[P("P_HealOnCast", amount=4.0, radius=4.0)],
             desc="Every spell you cast sheds a little youth on the allies around you."),
        Node("Methuselah", "methuselah", (2, 6), kind="Keystone", excl="ThiefKeystone", pre=["TimeThief", "FastForward"],
             passives=[P("P_Rebirth", novaDamage=40.0, novaDamageDef="AA_Heartbreak", novaRadius=5.0, wardFraction=0.6, cooldownTicks=DAY,
                         color=(1.0, 0.69, 0.82, 1.0), labelKey="AA_ForeverYoung")],
             desc="You have outlived death before. Once a day, when you would die, you rise again young and whole."),
        Node("Graverobber", "graverobber", (4, 6), kind="Keystone", excl="ThiefKeystone", pre=["FastForward", "Evergreen"], stats={"AA_Lifesteal": 0.12},
             mods=[tagmod("Desire", damage=0.15)], desc="You take far more than years. Heavy lifesteal and more damage."),
        Node("EternalBeauty", None, (3, 7), kind="Ultimate", spell="EternalBeauty", pre=["Methuselah", "Graverobber"]),
    )

    # ------------------------------------------------------------------ NIGHTMARE MUSE: sleep and dreams
    nm = ClassSpec("NightmareMuse", "nightmare muse", color=MUSE_C, color2=MUSE_C2, icon=("moon", "lips"), parent=sc, order=2, school=MUSE,
        tagline="Sleep is her battlefield. Put them under, then decide what they dream.",
        desc="A Nightmare Muse visits people in their sleep. Since she started doing it on the battlefield, nobody gets much sleep there. At home she is "
             "the one who keeps the bad dreams away.",
        playstyle="Sleep first, then work on the sleepers. Sleepsong puts an area to sleep (pain wakes them). Sleepwalk turns sleepers into wanderers who "
                  "do not wake when shot, Dream Eater wakes every sleeper in a small area screaming for heavy damage, Dread Dream and Stolen Sleep "
                  "work on the waking, and Endless Night does it to the whole map. At home: dreams of the beloved, singing a breakdown to sleep, "
                  "and a dreamcatcher over the barracks. Wants Attunement and Focus.",
        lore="'I dreamt of her,' said the last raider standing. 'She was beautiful. Then she was hungry.'",
        primary="ATT", secondary="FOC", tags=["Desire", "Dream"], height=8)
    nm.add_spells(
        Spell("Sleepsong", "sleepsong", "Sing a lullaby over an area. Enemies inside fall into a dream for ten seconds and stand where they are until something "
              "hurts them.", icon=("moon", "note"), mana=30, cd=20, range=26, warmup=0.8, tags=["Desire", "Dream"], role="Control", target="enemy_area",
              radius=3.0, ai_min=2, impact_vfx=[ring(0.2, 1.0, 30), burst("star", 16, 1.0, 0.5, byRadius=True), glow(2.0, 3.0, 40)],
              effects=[hediff(DREAM, 1.0)]),
        Spell("DreamEater", "dream eater", "Feast on every dream in a small area. Each sleeper within two and a half cells wakes screaming: 45 heartbreak "
              "damage and a second and a half of stun. Those already awake are ignored. You regain 10 mana.", icon=("moon", "lips"), mana=30, cd=12,
              range=26, warmup=0.5, tags=["Desire", "Dream"], target="enemy_area", radius=2.5, impact_vfx=KISS_HIT + [ring(0.2, 0.9, 20)],
              effects=[hearts_due(1, [dmg(45, "AA_Heartbreak"), stun(90)], hediff_=DREAM, noBosses=False, quiet=True)], caster_effects=[mana(10)]),
        Spell("Sleepwalk", "sleepwalk", "Get the sleepers in an area up without waking them: every dreaming enemy there wanders off in its sleep for twelve "
              "seconds, and pain no longer wakes it. Shoot them in peace.", icon=("moon", "arrow"), mana=30, cd=25, range=26, warmup=0.8,
              tags=["Desire", "Dream"], role="Control", target="enemy_area", radius=3.5, ai_min=2, tier=2,
              impact_vfx=[ring(0.2, 1.0, 26), burst("star", 12, 1.0, 0.5, byRadius=True)],
              effects=[hearts_due(1, [hediff("AA_Sleepwalking", 1.0)], hediff_=DREAM, noBosses=False, quiet=True)]),
        Spell("DreadDream", "dread dream", "Wake the worst nightmare a waking enemy has ever had. It flees in terror for six seconds while the dream eats at it.",
              icon=("moon", "skull"), mana=28, cd=16, range=26, warmup=0.5, tags=["Desire", "Dream"], role="Control", impact_vfx=KISS_HIT,
              effects=[hediff("AA_DreadDream", 1.0)]),
        Spell("StolenSleep", "stolen sleep", "Steal every last scrap of sleep out of an enemy: its rest is emptied and it turns drowsy for thirty seconds "
              "(25% slower, 30% slower to aim, a little dazed). An enemy left on empty for long collapses asleep where it stands. The stolen sleep "
              "refreshes every ally within five cells of it (+25% rest).", icon=("moon", "hand"), mana=30, cd=20, range=26, warmup=0.6,
              tags=["Desire", "Dream"], role="Control", target="enemy", tier=2, impact_vfx=KISS_HIT + [burst("star", 10, 1.2, 0.5)],
              effects=[need("Rest", -1.0, affects="Enemies"), hediff("AA_Drowsy", 1.0, affects="Enemies"), need("Rest", 0.25, affects="Allies", radius=5.0)]),
        Spell("BelovedDream", "dream of the beloved", "Breathe a sweet dream over everyone within three cells who sleeps in the next day. After at least "
              "four hours of sleep they wake glowing: +8 mood for a day, +15% rest and +25% recreation, and if they have a lover, +15 opinion of "
              "them for five days. Cast it over a couple's bed and they wake up thinking only of each other.", icon=("moon", "heart"), mana=35,
              cd=1000, range=30, warmup=1.5, tags=["Desire", "Dream"], role="Utility", target="ally_area", radius=3.0, quirky=True,
              impact_vfx=[ring(0.2, 1.0, 30), burst("star", 14, 1.0, 0.5, byRadius=True), burst("petal", 10, 1.0, 0.5, byRadius=True)],
              effects=[hediff("AA_BelovedDream", 1.0)], flavor="Sleep, sweethearts. I'll keep the bad dreams for myself."),
        Spell("SweetOblivion", "sweet oblivion", "Sing someone out of a mental break, even a violent one: the break ends at once, they lie down where "
              "they stand and fall asleep, and wake up feeling lighter (+5 mood for a day). It works just as well on a colonist who simply needs to "
              "sleep right now.", icon=("cloud", "moon"), mana=40, cd=500, range=24, warmup=1.0, tags=["Desire", "Dream"], role="Utility",
              target="any_pawn", affects="All", quirky=True,
              impact_vfx=[burst("star", 12, 1.0, 0.5), glow(1.6, 2.6, 40), burst("note", 6, 1.0, 0.5)],
              effects=[calm(violent=True, thought="AA_SweetOblivionMood"), sleep_now(inBed=False)]),
        Spell("Dreamcatcher", "dreamcatcher", "Hang a dreamcatcher of moonlit thread for three days. Every hour it ends any non-violent breakdown within six "
              "cells and leaves everyone there untroubled for two hours (rest 15% faster, mental break threshold 5% lower). Hang it in the barracks "
              "before a hard winter.", icon=("ring", "moon"), mana=50, cd=2000, range=20, warmup=3.0, tags=["Desire", "Dream"], role="Utility",
              target="cell", quirky=True, impact_vfx=[ring(0.2, 1.0, 30), burst("star", 12, 1.0, 0.5)],
              effects=[spawn("AA_Construct_SuccubusDreamcatcher", lifetimeTicks=DAY * 3)]),
        Spell("EndlessNight", "endless night", "Every enemy on the map falls into a deep dream. Five seconds later you feast on every dream at once: heartbreak "
              "damage, tripled against those still dreaming.", icon=("moon", "eclipse"), mana=220, cd=300, warmup=3.0, tags=["Desire", "Dream"],
              target="self", delivery="Global", ultimate=True, tier=5, xp=2.0, shake=1.0, ai_min=4,
              cast_vfx=[pillar(2.4, 70), screen("#3a1a6a", 0.4, 80, vignette=True)], impact_vfx=[burst("star", 6, 1.0, 0.5)],
              effects=[hediff(DREAM, 1.0, duration=900),
                       sequence(step(300, [dmg(30, "AA_Heartbreak", bonusVsHediff=DREAM, bonusVsHediffFactor=3.0)], radius=0.0,
                                     vfx=[burst("petal", 8, 2.0, 0.5), flash(2.0)], sound="AA_Heartbeat"))]),
    )
    nm.add_nodes(
        Node("Sleepsong", None, (2, 0), kind="Spell", spell="Sleepsong"),
        Node("Moonlit", "moonlit", (4, 0), ranks=3, stats={"AA_ManaRegen": 0.08, "AA_CastSpeed": 0.02}),
        Node("Hush", "hush", (1, 1), kind="Augment", ranks=2, pre=["Sleepsong"], mods=[spellmod(["Sleepsong"], radius=0.2, duration=0.3)]),
        Node("DreamEater", None, (3, 1), kind="Spell", spell="DreamEater", pre=["Sleepsong", "Moonlit"]),
        Node("BelovedDream", None, (4, 1), kind="Spell", spell="BelovedDream", pre=["Moonlit"]),
        Node("Reverie", "reverie", (5, 1), ranks=3, pre=["Moonlit"], mods=[tagmod("Dream", damage=0.05)]),
        Node("Drowsy", "drowsy", (0, 2), ranks=3, pre=["Hush"], stats={"AA_DurationFactor": 0.04}),
        Node("DreadDream", None, (2, 2), kind="Spell", spell="DreadDream", pre=["Hush", "DreamEater"]),
        Node("Gluttony", "gluttony", (4, 2), kind="Augment", ranks=2, pre=["DreamEater", "Reverie"], mods=[spellmod(["DreamEater"], damage=0.2, cooldown=-0.1)]),
        Node("Somnolence", "somnolence", (6, 2), kind="Notable", pre=["Reverie"],
             passives=[P("P_OnHitHediff", hediff=DREAM, chancePerRank=0.12, severity=1.0, tag="Dream", cooldownTicks=300)],
             desc="Dream spells sometimes send their target straight to sleep."),
        Node("SweetOblivion", None, (0, 3), kind="Spell", spell="SweetOblivion", pre=["Drowsy"]),
        Node("Sleepwalk", None, (1, 3), kind="Spell", spell="Sleepwalk", pre=["Drowsy", "DreadDream"]),
        Node("StolenSleep", None, (2, 3), kind="Spell", spell="StolenSleep", pre=["DreadDream"]),
        Node("Incubation", "incubation", (3, 3), ranks=3, pre=["DreadDream", "Gluttony"], pct={"AA_SpellPower": 0.03}),
        Node("Dreamcatcher", None, (5, 3), kind="Spell", spell="Dreamcatcher", pre=["Gluttony", "Somnolence"]),
        Node("MoonlitVigil", "moonlit vigil", (6, 3), kind="Notable", pre=["Somnolence"],
             passives=[P("P_Aura", radius=10.0, interval=HOUR // 2, affects="Allies", color=MUSE_RGBA, effects=[hediff("AA_MoonlitRest", 1.0)])],
             desc="She keeps watch over sleepers. Allies within ten cells of her rest 30% faster in their sleep and are a little harder to break "
                  "(-3% mental break threshold)."),
        Node("Nightmarish", "nightmarish", (2, 4), kind="Augment", ranks=2, pre=["Sleepwalk", "Incubation"],
             mods=[spellmod(["DreadDream", "Sleepwalk"], duration=0.3, cooldown=-0.1)]),
        Node("DreamWalker", "dream walker", (4, 4), kind="Notable", ranks=2, pre=["Incubation", "Dreamcatcher"], passives=[P("P_DodgeBlink", chancePerRank=0.06)],
             desc="Half of you is always somewhere in a dream; blows sometimes find only that half."),
        Node("Lucidity", "lucidity", (1, 5), ranks=3, pre=["Nightmarish"], stats={"AA_CritChance": 0.02}),
        Node("Insomnia", "insomnia", (3, 5), kind="Notable", ranks=2, pre=["Nightmarish", "DreamWalker"], passives=[P("P_ManaOnKill", amount=10.0)],
             desc="Every dream that ends for good feeds you."),
        Node("Starlight", "starlight", (5, 5), ranks=3, pre=["DreamWalker"], stats={"AA_WardMax": 10}),
        Node("SandmansSong", "sandman's song", (2, 6), kind="Keystone", excl="MuseKeystone", pre=["Lucidity", "Insomnia"],
             mods=[spellmod(["Sleepsong", "Sleepwalk"], radius=0.4, cooldown=-0.3)], desc="Your lullabies carry much farther and come back sooner."),
        Node("NightmareQueen", "nightmare queen", (4, 6), kind="Keystone", excl="MuseKeystone", pre=["Insomnia", "Starlight"],
             mods=[spellmod(["DreamEater"], radius=0.5, cooldown=-0.3), tagmod("Dream", damage=0.2)],
             desc="Dream Eater feasts on a much wider area and comes back sooner, and every dream you touch bites harder."),
        Node("EndlessNight", None, (3, 7), kind="Ultimate", spell="EndlessNight", pre=["SandmansSong", "NightmareQueen"]),
    )
    return sc, [tp, ty, nm], statuses()

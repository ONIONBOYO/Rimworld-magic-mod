"""Colony expansion of the Succubus: the matchmaker and fertility goddess. She ties couples together, mends feuds, blesses
wombs and herds with children and twins, swoons enemies into captivity, charms traders and prisoners, lends stolen time to
mothers and children, and sends sweet dreams of the beloved."""
from classes.common import *  # noqa: F401,F403
from classes import c_succubus as M

ROSE, BLUSH = M.ROSE, M.BLUSH
MUSE_C, MUSE_C2 = "#b07aff", "#ff9ce0"
ROSE_RGBA = (1.0, 0.37, 0.64, 1.0)
MUSE_RGBA = (0.69, 0.48, 1.0, 1.0)

KEYS = {
    "AA_SuccubusWokeInLove": "Dreamt of love",
}


def hearts_due(minStacks, effects, **kw):
    """Effect_SuccubusHeartsDue (Colony_Succubus.cs): only targets with at least minStacks of Infatuation are affected; the
    stacks are spent and the inner effects play on them."""
    return O("Effect_SuccubusHeartsDue", stackHediff=M.INF, minStacks=float(minStacks), effects=list(effects), **kw)


def reconcile(opinion, mood=None, loveChance=0.3, **kw):
    """Effect_SuccubusReconcile (Colony_Succubus.cs): the target and the person they get on worst with make up."""
    return O("Effect_SuccubusReconcile", opinionThought=opinion, moodThought=mood, loveChance=float(loveChance), **kw)


SOFT_HIT = [burst("petal", 12, 1.4, 0.5), glow(1.4, 2.2, 30, useColor2=True)]


def statuses():
    s = []
    s.append(status("AA_Swooning", "swooning", "Fainted dead away from sheer longing. It lies where it fell, unhurt and quite unable to fight, until the "
        "swoon passes: long enough to be carried off and taken prisoner.", ROSE, bad=True, ticks=5400,
        stages=[stage(extra={"capMods": [O(None, capacity="Consciousness", setMax=0.1)]})],
        fx=dict(emitter="Charm", emitInterval=16, overlayTex="ArcanumAscendant/Vfx/Heart", overlaySize=0.4, overlayZ=0.9, overlayPulse=True)))
    s.append(status("AA_KissOfLife", "kiss of life", "Kissed back onto their feet: for half a minute pain simply does not reach them. When the kiss "
        "fades, the pain comes back.", BLUSH, bad=False, ticks=1800, stages=[stage(pain=0.0, offsets={"PainShockThreshold": 0.5})],
        fx=dict(emitter="Charm", emitInterval=12)))
    s.append(status("AA_TwinBlessing", "blessing of twins", "A succubus has blessed this womb. The next birth brings twins or triplets (for an "
        "animal, one or two extra litters), and until then she is more fertile.", ROSE, bad=False, ticks=30 * DAY, comps=[multi_birth(1, 2)],
        dlc_stats={"Fertility": ("factor", 1.5)}, fx=dict(emitter="Charm", emitInterval=90)))
    s.append(status("AA_Fecund", "fecund", "Near a succubus, life wants to make more of itself: more fertile for a while (Biotech).", BLUSH, bad=False,
        ticks=5000, dlc_stats={"Fertility": ("factor", 1.5)}))
    s.append(status("AA_SweetTalking", "sweet-talking", "Still glowing from a shameless flirt: a harder bargain, a sweeter tongue.", BLUSH, bad=False,
        ticks=15000, stages=[stage(offsets={"TradePriceImprovement": 0.12, "NegotiationAbility": 0.3, "SocialImpact": 0.2})],
        fx=dict(emitter="Charm", emitInterval=60)))
    s.append(status("AA_Heartsick", "heartsick", "Thinking only of the one waiting at home. It wants nothing more to do with this fight.", ROSE, bad=True,
        ticks=480, comps=[compel("Flee", mechs=True)], fx=dict(emitter="Charm", emitInterval=10)))
    s.append(status("AA_BelovedDream", "dream of the beloved", "Tonight this sleeper will dream of the one they love. After at least four hours of "
        "sleep they wake glowing, rested and thinking only of their beloved.", MUSE_C, bad=False, ticks=DAY,
        comps=[wake_thought(thought="AA_BelovedDreamMood", social="AA_BelovedDreamOpinion", subject="Lover", minSleepHours=4.0, rest=0.15, joy=0.25,
                            text="AA_SuccubusWokeInLove", color=MUSE_C2)],
        fx=dict(emitter="Dream", emitInterval=45)))
    s.append(status("AA_Drowsy", "drowsy", "Its sleep has been stolen: eyelids drooping, it moves and aims like a sleepwalker.", MUSE_C, bad=True, ticks=1800,
        stages=[stage(factors={"MoveSpeed": 0.75, "AimingDelayFactor": 1.3}, caps={"Consciousness": -0.15})], fx=dict(emitter="Dream", emitInterval=14)))
    s.append(status("AA_MoonlitRest", "moonlit rest", "Sleeping near a Nightmare Muse, who keeps the bad dreams away: rest comes faster and the mind is "
        "steadier.", MUSE_C, bad=False, ticks=2500, stages=[stage(factors={"RestRateMultiplier": 1.3}, offsets={"MentalBreakThreshold": -0.03})]))
    # memories
    s.append(opinion_def("AA_RedThreadOpinion", "red thread of fate", 20, days=10.0, desc="A red thread ties my heart to theirs."))
    s.append(thought_def("AA_RedThreadMood", "red thread of fate", "A succubus tied a red thread between my heart and someone else's. I can feel it tug.",
                         6, days=2.0))
    s.append(opinion_def("AA_MadeUpOpinion", "kissed and made up", 35, days=10.0, desc="We fought for so long. I can't even remember why any more."))
    s.append(thought_def("AA_MadeUpMood", "made up with an old rival", "An old feud is over. It feels like putting down something heavy.", 4, days=1.0))
    s.append(opinion_def("AA_KissOfLifeOpinion", "kissed me back to life", 12, days=10.0, desc="I was down, and she kissed me back onto my feet."))
    s.append(thought_def("AA_BelovedDreamMood", "dreamt of love", "I dreamt of love all night long, and I woke up smiling.", 8, days=1.0))
    s.append(opinion_def("AA_BelovedDreamOpinion", "dreamt of them", 15, days=5.0, desc="I dreamt of them all night. I can't stop thinking about them."))
    s.append(thought_def("AA_SweetOblivionMood", "slept it off", "Whatever came over me, I slept it off. I feel strangely light.", 5, days=1.0))
    return s


def extend(t, extra):
    base, tp, ty, nm = t["Succubus"], t["Temptress"], t["ThiefOfYears"], t["NightmareMuse"]
    extra.extend(statuses())

    # ------------------------------------------------------------------ SUCCUBUS: matchmaker and fertility goddess
    base.add_spells(
        Spell("RedThread", "red thread of fate", "Tie a red thread between a colonist and the one fate meant for them: their partner if they have one, "
              "otherwise their best match on the map. Both gain +20 opinion of each other for ten days and a +6 mood memory for two; if both are "
              "single adults who could love each other, they become lovers.", icon=("heart", "ring"), mana=50, cd=2000, range=30, warmup=2.0,
              tags=["Desire", "Utility"], role="Utility", target="ally", quirky=True, school=M.DESIRE,
              impact_vfx=SOFT_HIT + [ring(0.2, 0.9, 26, byRadius=False)],
              effects=[matchmake("BestMatch", relation="Lover", opinion="AA_RedThreadOpinion", mood="AA_RedThreadMood")],
              flavor="Some threads were always there. She just pulls them tight."),
        Spell("KissAndMakeUp", "kiss and make up", "Kiss a colonist on the cheek and the feud they nurse against their worst rival in the colony is "
              "over: both gain +35 opinion of each other for ten days and +4 mood for a day. One time in three, if both are single adults who could "
              "love each other, the old enemies fall in love instead.", icon=("lips", "heartcrack"), mana=40, cd=1000, range=24, warmup=1.5,
              tags=["Desire", "Utility"], role="Utility", target="ally", quirky=True, school=M.DESIRE, impact_vfx=SOFT_HIT,
              effects=[reconcile("AA_MadeUpOpinion", "AA_MadeUpMood", loveChance=0.33)],
              flavor="'I hate him.' 'I know, darling. Kiss him anyway.'"),
        Spell("Quickening", "quickening", "Lay a hand on a woman, her partner or a tame animal and bless them with a child. The woman and her partner "
              "conceive at once (Biotech), or the animal and the nearest mate of its kind within twenty cells do; an egg-layer's next eggs are fertile. "
              "The usual rules still apply: the sterile, the too young and the too old cannot be helped.", icon=("flower", "heart"), mana=60, cd=2000,
              range=6, warmup=3.0, tags=["Desire", "Utility"], role="Utility", target="ally", quirky=True, school=M.DESIRE,
              impact_vfx=[burst("petal", 16, 1.2, 0.5), burst("star", 8, 1.0, 0.4), glow(1.6, 2.6, 40, useColor2=True)],
              effects=[conceive(mateRadius=20.0)]),
        Spell("TwinBlessing", "blessing of twins", "Bless a woman or a female animal, pregnant or not: her next birth within thirty days brings twins or "
              "triplets (even odds), or for an animal one or two extra litters. Until then she is half again as fertile (Biotech).",
              icon=("star", "flower"), mana=80, cd=5000, range=6, warmup=3.0, tags=["Desire", "Utility"], role="Utility", target="ally", quirky=True,
              school=M.DESIRE, impact_vfx=[pillar(1.0, 40), burst("star", 14, 1.4, 0.5), burst("petal", 14, 1.4, 0.5)],
              effects=[hediff("AA_TwinBlessing", 1.0)], flavor="Two cradles. Just in case. Make it three."),
        Spell("Swoon", "swoon", "Whisper something to an enemy that is already yours. With at least three stacks of Infatuation (all spent) it swoons, "
              "fainting dead away for a minute and a half, unhurt and helpless: long enough to carry it off as a prisoner. Bosses, machines and "
              "creatures much bigger than a person are unmoved.", icon=("feather", "heart"), mana=30, cd=25, range=24, warmup=0.6,
              tags=["Desire", "Charm"], role="Control", target="enemy", tier=2, autocast=False, school=M.DESIRE,
              impact_vfx=M.KISS_HIT + [burst("feather", 8, 1.0, 0.5)],
              effects=[hearts_due(3, [hediff("AA_Swooning", 1.0)], maxBodySize=2.5, fleshOnly=True)], flavor="Oh. Oh my."),
        Spell("KissOfLife", "kiss of life", "Kiss a fallen ally back onto their feet. For thirty seconds pain does not reach them at all, which is enough "
              "to stand up and get clear if it was only the pain that dropped them; they also heal 12 and think fondly of you (+12 opinion for ten "
              "days). When the kiss fades, the pain returns.", icon=("lips", "cross"), mana=30, cd=60, range=3.9, warmup=1.0,
              tags=["Desire", "Healing"], role="Heal", target="ally", tier=2, ai_hp=0.3, school=M.DESIRE,
              impact_vfx=[burst("petal", 12, 1.5, 0.5), glow(1.6, 2.4, 30), flash(1.8)],
              effects=[hediff("AA_KissOfLife", 1.0), heal(12), memory("AA_KissOfLifeOpinion", toCaster=True)]),
    )
    base.add_nodes(
        Node("RedThread", None, (2, 1), kind="Spell", spell="RedThread", pre=["SweetNothings"]),
        Node("Swoon", None, (1, 2), kind="Spell", spell="Swoon", pre=["Captivate"]),
        Node("KissAndMakeUp", None, (3, 2), kind="Spell", spell="KissAndMakeUp", pre=["RedThread", "Heartbreak"]),
        Node("KissOfLife", None, (5, 2), kind="Spell", spell="KissOfLife", pre=["Glamour"]),
        Node("Quickening", None, (2, 3), kind="Spell", spell="Quickening", pre=["KissAndMakeUp"]),
        Node("MotherOfHerds", "mother of herds", (3, 4), kind="Notable", pre=["Quickening", "KissOfYouth"],
             passives=[P("P_Aura", radius=10.0, interval=HOUR, affects="Allies", color=ROSE_RGBA,
                         effects=[hediff("AA_Fecund", 1.0), animal_produce(0.02)])],
             desc="Life wants to make more of itself around her. Every hour, colonists and tame animals within ten cells grow more fertile for two "
                  "hours (Biotech: fertility x1.5), and tame animals' milk, wool and eggs come 2% sooner."),
        Node("TwinBlessing", None, (2, 5), kind="Spell", spell="TwinBlessing", pre=["MotherOfHerds"]),
    )

    # ------------------------------------------------------------------ TEMPTRESS: traders, visitors and captives
    tp.add_spells(
        Spell("HoneyedWords", "honeyed words", "Flirt shamelessly with a visitor, a trader or even a captive: their faction warms to the colony (+8 "
              "goodwill, except with factions that will never make peace), and for six hours you drive a harder bargain: +12% trade prices, +30% "
              "negotiation ability and +20% social impact.", icon=("coin", "lips"), mana=40, cd=1000, range=12, warmup=2.0,
              tags=["Desire", "Charm"], role="Utility", target="any_pawn", affects="AllButSelf", quirky=True, school=M.TEMPT,
              impact_vfx=SOFT_HIT, effects=[goodwill(8)], caster_effects=[hediff("AA_SweetTalking", 1.0)],
              flavor="'For you? Half price.' He has no idea why he said it."),
        Spell("GentlemenCallers", "gentlemen callers", "Let word of your beauty slip beyond the colony. Within moments admirers come calling: a trade "
              "caravan or a group of friendly visitors arrives at the edge of the map, if any faction is willing to send one.", icon=("rose", "bell"),
              mana=70, cd=3000, warmup=4.0, tags=["Desire", "Charm"], role="Utility", target="self", quirky=True, school=M.TEMPT,
              impact_vfx=[ring(0.2, 1.2, 40), burst("petal", 24, 2.5, 0.6), pillar(1.2, 40)],
              effects=[incident(["TraderCaravanArrival", "VisitorGroup"])], flavor="They always bring flowers. And silver."),
        Spell("HomewardHeart", "homeward heart", "Remind every smitten enemy within nine cells of the sweetheart waiting at home. Each one with at least "
              "two stacks of Infatuation (all spent) loses the will to fight: people give up the raid and flee the map, while beasts and machines "
              "just run from you for eight seconds. Bosses are unmoved.", icon=("heart", "door"), mana=60, cd=60, warmup=1.2,
              tags=["Desire", "Charm"], role="Control", target="self", delivery="Nova", radius=9.0, tier=3, autocast=False, school=M.TEMPT,
              sound_cast="AA_Heartbeat", impact_vfx=[ring(0.1, 1.2, 40), burst("petal", 30, 3.0, 0.6, byRadius=True)],
              effects=[hearts_due(2, [mental("PanicFlee", humanlikeOnly=True), hediff("AA_Heartsick", 1.0)])],
              flavor="Go home. She's waiting. She always was."),
    )
    tp.add_nodes(
        Node("HoneyedWords", None, (6, 0), kind="Spell", spell="HoneyedWords"),
        Node("SweetCaptivity", "sweet captivity", (0, 3), kind="Notable", pre=["Puppeteer"],
             passives=[P("P_Aura", radius=9.0, interval=HOUR, affects="Allies", color=ROSE_RGBA,
                         effects=[prisoner(resistance=0.5, will=0.1, suppression=0.02)])],
             desc="Prisoners can't help falling for her. Every hour, prisoners within nine cells lose 0.5 resistance and 0.1 will, and slaves grow 2% "
                  "more suppressed (Ideology)."),
        Node("GentlemenCallers", None, (6, 3), kind="Spell", spell="GentlemenCallers", pre=["Siren"]),
        Node("HomewardHeart", None, (5, 4), kind="Spell", spell="HomewardHeart", pre=["Adoration"]),
    )

    # ------------------------------------------------------------------ THIEF OF YEARS: time lent to mothers and children
    ty.add_spells(
        Spell("BorrowedMonths", "borrowed months", "Spend some of your stolen time on an expectant mother, person or animal: her pregnancy jumps five "
              "days ahead (more with greater healing power, never past the birth itself). The time comes out of your own hoard: you age half a year.",
              icon=("hourglass", "flower"), mana=45, cd=1000, range=6, warmup=2.0, tags=["Desire", "Time"], role="Utility", target="ally",
              quirky=True, school=M.YEARS, impact_vfx=[burst("clock", 10, 1.2, 0.5), burst("petal", 8, 1.2, 0.5), glow(1.4, 2.2, 30)],
              effects=[gestate(5)], caster_effects=[agesh(0.5, scaleWithPower=False)]),
        Spell("StolenSummers", "stolen summers", "Pour summers stolen from your enemies into the children around a point: every child within four cells "
              "gains 8 growth points (more with greater healing power) and half a bar of learning, and babies get to play (Biotech).",
              icon=("sun", "hourglass"), mana=50, cd=1000, range=24, warmup=2.0, tags=["Desire", "Time"], role="Utility", target="ally_area",
              radius=4.0, quirky=True, school=M.YEARS,
              impact_vfx=[ring(0.2, 1.0, 30), burst("clock", 14, 1.5, 0.5, byRadius=True), burst("star", 10, 1.2, 0.5, byRadius=True)],
              effects=[child_growth(8, learning=0.5)]),
        Spell("TimesRuin", "time's ruin", "Let a century pass over an area in a heartbeat. Walls, sandbags, turrets and every other building there that "
              "is not your colony's take heavy damage (180) as they crumble with age, and every enemy inside ages two years and takes a little "
              "damage. Cover does not last forever.", icon=("hourglass", "wall"), mana=50, cd=35, range=28, warmup=1.0, tags=["Desire", "Time"],
              target="enemy_area", radius=3.0, tier=3, ai_min=2, school=M.YEARS,
              impact_vfx=[ring(0.2, 1.0, 30), burst("clock", 18, 1.6, 0.6, byRadius=True), smoke(5, 1.2)],
              effects=[dmg(6, damageBuildings=True, buildingFactor=30.0), agesh(2.0, ailmentChance=0.1)]),
    )
    ty.add_nodes(
        Node("BorrowedMonths", None, (4, 1), kind="Spell", spell="BorrowedMonths", pre=["Timeless"]),
        Node("TimesRuin", None, (2, 3), kind="Spell", spell="TimesRuin", pre=["TimesToll"]),
        Node("StolenSummers", None, (4, 3), kind="Spell", spell="StolenSummers", pre=["FountainOfYouth"]),
    )

    # ------------------------------------------------------------------ NIGHTMARE MUSE: sweet dreams and stolen sleep
    nm.add_spells(
        Spell("BelovedDream", "dream of the beloved", "Breathe a sweet dream over everyone within three cells who sleeps in the next day. After at least "
              "four hours of sleep they wake glowing: +8 mood for a day, +15% rest and +25% recreation, and if they have a lover, +15 opinion of "
              "them for five days. Cast it over a couple's bed and they wake up thinking only of each other.", icon=("moon", "heart"), mana=35,
              cd=1000, range=30, warmup=1.5, tags=["Desire", "Dream"], role="Utility", target="ally_area", radius=3.0, quirky=True, school=M.MUSE,
              impact_vfx=[ring(0.2, 1.0, 30), burst("star", 14, 1.0, 0.5, byRadius=True), burst("petal", 10, 1.0, 0.5, byRadius=True)],
              effects=[hediff("AA_BelovedDream", 1.0)], flavor="Sleep, sweethearts. I'll keep the bad dreams for myself."),
        Spell("SweetOblivion", "sweet oblivion", "Sing someone out of a mental break, even a violent one: the break ends at once, they lie down where "
              "they stand and fall asleep, and wake up feeling lighter (+5 mood for a day). It works just as well on a colonist who simply needs to "
              "sleep right now.", icon=("cloud", "moon"), mana=40, cd=500, range=24, warmup=1.0, tags=["Desire", "Dream"], role="Utility",
              target="any_pawn", affects="All", quirky=True, school=M.MUSE,
              impact_vfx=[burst("star", 12, 1.0, 0.5), glow(1.6, 2.6, 40), burst("note", 6, 1.0, 0.5)],
              effects=[calm(violent=True, thought="AA_SweetOblivionMood"), sleep_now(inBed=False)]),
        Spell("StolenSleep", "stolen sleep", "Steal every last scrap of sleep out of an enemy: its rest is emptied and it turns drowsy for thirty seconds "
              "(25% slower, 30% slower to aim, a little dazed). An enemy left on empty for long collapses asleep where it stands. The stolen sleep "
              "refreshes every ally within five cells of it (+25% rest).", icon=("moon", "hand"), mana=30, cd=20, range=26, warmup=0.6,
              tags=["Desire", "Dream"], role="Control", target="enemy", tier=2, school=M.MUSE, impact_vfx=M.KISS_HIT + [burst("star", 10, 1.2, 0.5)],
              effects=[need("Rest", -1.0, affects="Enemies"), hediff("AA_Drowsy", 1.0, affects="Enemies"), need("Rest", 0.25, affects="Allies", radius=5.0)]),
    )
    nm.add_nodes(
        Node("BelovedDream", None, (4, 1), kind="Spell", spell="BelovedDream", pre=["Moonlit"]),
        Node("SweetOblivion", None, (0, 3), kind="Spell", spell="SweetOblivion", pre=["Drowsy"]),
        Node("StolenSleep", None, (2, 3), kind="Spell", spell="StolenSleep", pre=["DreadDream"]),
        Node("MoonlitVigil", "moonlit vigil", (6, 3), kind="Notable", pre=["Somnolence"],
             passives=[P("P_Aura", radius=10.0, interval=HOUR // 2, affects="Allies", color=MUSE_RGBA, effects=[hediff("AA_MoonlitRest", 1.0)])],
             desc="She keeps watch over sleepers. Allies within ten cells of her rest 30% faster in their sleep and are a little harder to break "
                  "(-3% mental break threshold)."),
    )

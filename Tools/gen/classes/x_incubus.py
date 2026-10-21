"""Colony expansion of the Incubus: the dream-lover and pact-maker at home. He visits sleepers' dreams himself, witnesses marriage
contracts, sells children (a child bought with a hunter's price, twins at a cost, a gifted firstborn), buys vices and sells talents,
indentures prisoners at interest, teaches in dreams, devours nightmares, wakes the colony for the night raid, and as a prince of hell
drills, drives and taxes the rim."""
from classes.common import *  # noqa: F401,F403
from classes import c_incubus as M

PINK, ROSE, GOLD = "#ff7ab8", "#ff9cd0", "#ffcf6a"
LOVE = School("Shadow", PINK, "#a64dff", fleck="star", fleck2="petal", projectile="ArcanumAscendant/Vfx/VoidOrb")

KEYS = {
    "AA_IncubusVisitWake": "What a dream...",
    "AA_IncubusHauntWake": "A pleasant dream",
    "AA_NoteComesDue": "The note comes due",
}

# Vices the devil will buy (base-game traits without degrees, so nothing good is taken by mistake).
VICES = ["Pyromaniac", "Abrasive", "Greedy", "Jealous", "Wimp", "AnnoyingVoice", "CreepyBreathing", "Gourmand"]
# Gifts for a promised child.
GIFTS = ["Kind", "Tough", "TooSmart", "FastLearner", "GreatMemory", "QuickSleeper"]

LOVE_HIT = [burst("petal", 10, 1.2, 0.5), glow(1.0, 1.8, 30, useColor2=True), spiral("star", 8, 1.0, 0.4)]
PACT_SEAL = [pillar(0.9, 40), burst("ember", 12, 1.5, 0.5), ring(0.6, 0.1, 24, byRadius=False)]


def dreamwork(mode, **kw):
    """Effect_IncubusDreamwork (Source/ArcanumAscendant/Spells/Colony_Incubus.cs): mode Devour or Wake."""
    return O("Effect_IncubusDreamwork", mode=mode, **kw)


def promise(passions=2, traits=None, child=None, humanOnly=True):
    """HediffComp_IncubusPromise: every baby the bearer gives birth to next is gifted; the status is voided on a man (and on a
    beast when humanOnly). With no gifts it is only that guard."""
    return O("ArcanumAscendant.HediffCompProperties_IncubusPromise", passions=int(passions), traitOptions=list(traits or []) or None,
             childHediff=child, humanOnly=None if humanOnly else False)


def statuses():
    s = []
    # ---------------------------------------------------------------- base: dreams, vows, bargains
    s.append(status("AA_NightVisit", "night visit", "The incubus means to visit this sleeper's dreams himself. After four hours of sleep they will "
        "wake from a dream of a dark and charming stranger who looked a great deal like him.", PINK, bad=False, ticks=90000,
        comps=[wake_thought("AA_NightVisitDream", social="AA_NightVisitOpinion", subject="Caster", minSleepHours=4.0, inspirationChance=0.25,
                            rest=0.15, joy=0.3, text="AA_IncubusVisitWake", color=ROSE)],
        fx=dict(emitter="Dream", emitInterval=40)))
    s.append(thought_def("AA_NightVisitDream", "visited in a dream", "A charming stranger walked through my dreams all night. I woke up blushing.", 12,
                         days=1.0))
    s.append(opinion_def("AA_NightVisitOpinion", "visited my dreams", 15, days=10.0))
    s.append(status("AA_DreamHaunt", "haunted by sweet dreams", "Time spent near the incubus lingers. The next long sleep will bring a pleasant dream.",
        ROSE, bad=False, ticks=60000, comps=[wake_thought("AA_DreamHauntDream", minSleepHours=4.0, text="AA_IncubusHauntWake", color=ROSE)]))
    s.append(thought_def("AA_DreamHauntDream", "pleasant dream", "I had a warm, lovely dream. I can't remember it, but I feel good.", 4, days=1.0))
    s.append(thought_def("AA_RenewedVows", "renewed our vows", "We signed our names together again, and I remembered why I fell in love.", 8, days=3.0))
    s.append(opinion_def("AA_RenewedVowsOpinion", "fell for them all over again", 20, days=15.0))
    s.append(status("AA_HeirsPrice", "heir's bargain", "Signed for a child. The price is one life, any life: a hunted beast or a slaughtered one will do. "
        "When the day is out the bargain is settled: paid, and the couple conceive; unpaid, and the devil collects.", GOLD, bad=False, ticks=60000,
        comps=[contract(1, reward="AA_HeirGranted", penalty="AA_Defaulted", color=GOLD)],
        fx=dict(emitter="Debt", emitInterval=40, overlayTex="ArcanumAscendant/Vfx/ContractMark", overlaySize=0.4, overlayZ=1.05)))
    s.append(status("AA_HeirGranted", "heir granted", "The price was paid. The heir is on the way.", PINK, bad=False, ticks=90,
        comps=[periodic(60, effects=[conceive(animals=False)], color=PINK, vfx=[burst("petal", 12, 1.5, 0.5), glow(1.2, 2.4, 30)])]))
    s.append(status("AA_Defaulted", "defaulted", "Defaulted on a devil's bargain. Hell is collecting, slowly: everything is harder for a day.", "#ff3020",
        bad=True, ticks=60000, stages=[stage(factors={"WorkSpeedGlobal": 0.8, "MoveSpeed": 0.9}, offsets={"MentalBreakThreshold": 0.05})],
        fx=dict(emitter="Debt", emitInterval=30)))
    s.append(status("AA_TwoForOne", "two for one", "A bargain with an incubus: her next birth brings one more baby than nature intended. Until then she "
        "carries the weight of both.", PINK, bad=False, ticks=1200000, comps=[multi_birth(1, 1), promise(0, humanOnly=False)],
        stages=[stage(factors={"MoveSpeed": 0.85, "WorkSpeedGlobal": 0.9})], fx=dict(emitter="Charm", emitInterval=60)))
    s.append(thought_def("AA_SoldAVice", "sold a vice", "The devil bought my worst habit. I feel lighter, and a little hollow where it used to be.", -5, days=4.0))
    # ---------------------------------------------------------------- pactbinder
    s.append(status("AA_Indentured", "indentured", "Signed to terms with the Pactbinder. Interest accrues: every six hours their resolve wears a little "
        "thinner.", GOLD, bad=True, ticks=120000, comps=[periodic(15000, effects=[prisoner(2.0, suppression=0.05)], color=GOLD)],
        fx=dict(emitter="Debt", emitInterval=40, overlayTex="ArcanumAscendant/Vfx/ContractMark", overlaySize=0.4, overlayZ=1.05)))
    s.append(status("AA_PromisedChild", "firstborn clause", "A clause in her future, written by the Pactbinder: every baby she bears next will be "
        "gifted.", GOLD, bad=False, ticks=1200000, comps=[promise(2, GIFTS, "AA_Godchild")], fx=dict(emitter="Gold", emitInterval=60)))
    s.append(status("AA_Godchild", "promised child", "Born under a firstborn clause: gifted from the cradle, and quick to learn all its life.", GOLD,
        bad=False, stages=[stage(offsets={"GlobalLearningFactor": 0.15})]))
    s.append(status("AA_Underwritten", "underwritten", "The Pactbinder has pledged his own power as surety for a clause: less mana, slower to recover.",
        "#ff6a28", bad=True, ticks=300000, stages=[stage(offsets={"AA_ManaMax": -25, "AA_ManaRegen": -0.3})]))
    s.append(status("AA_PromissoryNote", "promissory note", "Signed for credit: only a quarter of every blow lands now. When the note comes due, "
        "twice what landed is collected at once, through any armour.", GOLD, bad=False, ticks=900,
        comps=[delayed_doom(stored=2.0, damageDef="AA_Hellfire", boss=1.0, color=GOLD, label="AA_NoteComesDue", shake=0.6)],
        stages=[stage(factors={"IncomingDamageFactor": 0.25})],
        fx=dict(emitter="Debt", emitInterval=10, overlayTex="ArcanumAscendant/Vfx/ContractMark", overlaySize=0.5, overlayZ=1.05, overlayPulse=True)))
    s.append(opinion_def("AA_HeldToTerms", "held me to my contract", -3, days=2.0))
    # ---------------------------------------------------------------- dreamlord
    s.append(status("AA_LucidLesson", "lucid lesson", "The Dreamlord is teaching this dreamer: every hour the lesson lasts, they learn something.",
        "#7a4dff", bad=False, ticks=20000, comps=[periodic(2500, effects=[skillxp(300)], color="#b49cff")], fx=dict(emitter="Dream", emitInterval=30)))
    s.append(status("AA_RudeAwakening", "rude awakening", "Torn out of sleep by the Dreamlord, every sense sharpened by the nightmare he used to do it.",
        "#7a4dff", bad=False, ticks=3600, stages=[stage(factors={"MoveSpeed": 1.15, "AimingDelayFactor": 0.85}, offsets={"PainShockThreshold": 0.1})],
        fx=dict(emitter="Dream", emitInterval=12)))
    # ---------------------------------------------------------------- hellprince
    s.append(status("AA_TrialSeared", "seared", "Came through the Hellprince's drill. Everything hurts, but the lessons stuck.", "#ff5a1e", bad=True, ticks=15000,
        stages=[stage(factors={"MoveSpeed": 0.9}, extra={"painOffset": 0.15})], fx=dict(emitter="Fire", emitInterval=50)))
    s.append(status("AA_WhipDriven", "driven", "The Hellprince cracked the whip: working faster than is good for anyone.", "#ff5a1e", bad=False, ticks=20000,
        stages=[stage(factors={"WorkSpeedGlobal": 1.3, "MoveSpeed": 1.1})], fx=dict(emitter="Infernal", emitInterval=45)))
    s.append(thought_def("AA_WhipDrivenHard", "driven like the damned", "The Hellprince cracked his whip over us. We worked, but nobody enjoyed it.", -5,
                         days=1.0))
    s.append(status("AA_InfernalWarmth", "infernal warmth", "Standing near a prince of hell is like standing near a furnace: the cold does not bite.",
        "#ff5a1e", bad=False, ticks=1500, stages=[stage(offsets={"ComfyTemperatureMin": -10})]))
    return s


def extend(t, extra):
    extra.extend(statuses())

    # ================================================================== BASE: the dream-lover and pact-maker at home
    base = t["Incubus"]
    base.add_spells(
        Spell("NightVisit", "night visit", "Promise a colonist a visit tonight. The next time they sleep for four hours or more, the incubus walks "
              "through their dreams himself, and they wake with +12 mood for a day, +15 opinion of him for ten days, 30% recreation, 15% rest and "
              "a one in four chance of an inspiration. The promise fades if they do not sleep within a day and a half. The married may get ideas.",
              icon=("lips", "moon"), mana=25, cd=250, range=30, warmup=1.5, tags=["Love", "Colony"], school=LOVE, role="Utility", target="ally",
              quirky=True, impact_vfx=LOVE_HIT, effects=[hediff("AA_NightVisit", 1.0)], flavor="He visits in the night. He always has."),
        Spell("FaustianTalent", "faustian talent", "Sell a colonist a talent: their best skill without a burning passion gains a step of passion (none "
              "becomes interested, interested becomes burning). The devil is paid in knowledge: about 3000 experience drains out of a random skill.",
              icon=("star", "contract"), mana=60, cd=10000, range=4, warmup=4.0, tags=["Pact", "Colony"], role="Utility", target="ally", quirky=True,
              tier=2, impact_vfx=PACT_SEAL + [burst("star", 10, 1.4, 0.5)], effects=[passion("Upgrade"), skillxp(-3000)],
              flavor="You will be brilliant. You just won't remember how to cook."),
        Spell("MarriageContract", "marriage contract", "Draw up a marriage contract for a colonist and their partner, and witness it. They fall in love "
              "all over again: +20 opinion of each other for fifteen days and +8 mood for three. Lovers sign an engagement; the engaged are married "
              "on the spot.", icon=("ring", "contract"), mana=50, cd=3000, range=12, warmup=3.0, tags=["Love", "Pact", "Colony"], school=LOVE,
              role="Utility", target="ally", quirky=True, tier=2, impact_vfx=LOVE_HIT + [pillar(0.8, 40)],
              effects=[rekindle(opinion="AA_RenewedVowsOpinion", mood="AA_RenewedVows", raise_=True)],
              flavor="'Till death do you part. Death has been notified.'"),
        Spell("HeirsBargain", "heir's bargain", "(Biotech) Sell a couple the child they long for. The colonist you target signs, and the price is one life "
              "within a day: a hunted beast or a slaughtered one will do. When the day is out the bargain is settled. Paid, and the signer and their "
              "partner conceive at once (the sterile, the too young and the too old cannot be helped). Unpaid, and the signer defaults: 20% slower "
              "at work, 10% slower on their feet and quicker to break for a day.", icon=("seal", "heart"), mana=60, cd=5000, range=6, warmup=3.0,
              tags=["Love", "Pact", "Colony"], school=LOVE, role="Utility", target="ally", quirky=True, tier=3, impact_vfx=PACT_SEAL + LOVE_HIT[:1],
              effects=[hediff("AA_HeirsPrice", 1.0)], flavor="Every child is a gift. This one is a purchase."),
        Spell("SellAVice", "sell a vice", "Buy a colonist's worst habit off them: the first of pyromaniac, abrasive, greedy, jealous, wimp, annoying "
              "voice, creepy breathing or gourmand that they have is gone for good. The devil's fee is a sliver of soul, -5 mood for four days, and "
              "he takes it whether or not there was a vice to buy.", icon=("coin", "mask"), mana=80, cd=10000, range=4, warmup=4.0,
              tags=["Pact", "Colony"], role="Utility", target="ally", quirky=True, tier=3, impact_vfx=PACT_SEAL,
              effects=[trait(remove=VICES), memory("AA_SoldAVice")], flavor="He pays in full. He just never says in what."),
        Spell("TwoForOne", "two for one", "Strike a bargain with an expectant mother, woman or beast: her next birth brings one more baby than nature "
              "intended (twins, or a second litter). She pays as she carries: 15% slower on her feet and 10% slower at work until the birth. The "
              "bargain lapses unclaimed after twenty days, and a man cannot take it. Human pregnancy needs Biotech.", icon=("heart", "coin"), mana=50, cd=3000, range=10,
              warmup=2.0, tags=["Love", "Pact", "Colony"], school=LOVE, role="Utility", target="ally", quirky=True, tier=4,
              impact_vfx=LOVE_HIT + [ring(0.2, 0.8, 30, byRadius=False)], effects=[hediff("AA_TwoForOne", 1.0)],
              flavor="Limited time offer. Terms apply. Terms always apply."),
    )
    base.add_nodes(
        Node("NightVisit", None, (0, 0), kind="Spell", spell="NightVisit"),
        Node("DreamHaunt", "dream haunt", (0, 1), kind="Notable", pre=["NightVisit"],
             passives=[P("P_Aura", radius=8.0, interval=2500, affects="AlliesNotSelf", color=rgb(ROSE) + (1.0,),
                         effects=[hediff("AA_DreamHaunt", 1.0, duration=60000)])],
             desc="Colonists who spend time within eight cells of the incubus dream sweetly: their next long sleep ends in a pleasant dream (+4 mood "
                  "for a day)."),
        Node("FaustianTalent", None, (6, 1), kind="Spell", spell="FaustianTalent", pre=["Brimfire"]),
        Node("MarriageContract", None, (0, 3), kind="Spell", spell="MarriageContract", pre=["DreamHaunt", "Greed"]),
        Node("HeirsBargain", None, (0, 5), kind="Spell", spell="HeirsBargain", pre=["MarriageContract", "FinePrint"]),
        Node("SellAVice", None, (6, 5), kind="Spell", spell="SellAVice", pre=["Sulphur"]),
        Node("TwoForOne", None, (0, 7), kind="Spell", spell="TwoForOne", pre=["HeirsBargain", "Hellbound"]),
    )

    # ================================================================== PACTBINDER: indentures, clauses and credit
    pb = t["Pactbinder"]
    pb.add_spells(
        Spell("Indenture", "indenture", "Offer a prisoner terms they cannot refuse: resistance falls by 8 and will by 3 at once, then interest accrues, "
              "another 2 resistance every six hours for two days. A slave instead gains 30% suppression at once and 5% every six hours (Ideology). "
              "Spell power raises every figure a little.", icon=("chain", "coin"), mana=35, cd=1000, range=20, warmup=2.0, tags=["Pact", "Colony"],
              school=M.BINDER, role="Utility", target="any_pawn", affects="AllButSelf", quirky=True, impact_vfx=PACT_SEAL,
              effects=[prisoner(8.0, will=3.0, suppression=0.3), hediff("AA_Indentured", 1.0)], flavor="Sign here. And here. And for the rest of your life."),
        Spell("PromissoryNote", "promissory note", "Sign an ally a promissory note: for fifteen seconds they take only a quarter of every blow. Then the note "
              "comes due and twice what landed is collected at once, through any armour: half the damage, paid later. Buy the time you need, and "
              "be ready to pay.", icon=("scroll", "shield"), mana=30, cd=40, range=24, warmup=0.4, tags=["Pact"], school=M.BINDER, role="Buff",
              target="ally", tier=2, impact_vfx=[burst("ember", 12, 1.5, 0.5), hexshield(1.6, 30)], effects=[hediff("AA_PromissoryNote", 1.0)],
              flavor="Payable on demand. He will demand."),
        Spell("FirstbornClause", "firstborn clause", "(Biotech) Write a clause into an expectant mother's future: every baby she bears next is born "
              "gifted, with burning passions in two skills, one fine trait (kind, tough, too smart, fast learner, great memory or quick sleeper) "
              "and a lifelong 15% learning bonus (void on anyone but a woman). The Pactbinder underwrites the clause himself: -25 maximum mana and -0.3 mana per second for five "
              "days. The clause lapses after twenty days.", icon=("contract", "star"), mana=70, cd=5000, range=6, warmup=3.0,
              tags=["Pact", "Colony"], school=M.BINDER, role="Utility", target="ally", quirky=True, tier=3, impact_vfx=PACT_SEAL + LOVE_HIT[:1],
              effects=[hediff("AA_PromisedChild", 1.0)], caster_effects=[hediff("AA_Underwritten", 1.0)],
              flavor="The child will owe him nothing. Read clause four."),
    )
    pb.add_nodes(
        Node("Indenture", None, (0, 0), kind="Spell", spell="Indenture"),
        Node("PromissoryNote", None, (0, 4), kind="Spell", spell="PromissoryNote", pre=["Penalty"]),
        Node("FirstbornClause", None, (6, 4), kind="Spell", spell="FirstbornClause", pre=["Skim"]),
        Node("ContractLaw", "contract law", (6, 6), kind="Notable", pre=["Escrow"],
             passives=[P("P_Aura", radius=10.0, interval=1250, affects="AlliesNotSelf", color=rgb(GOLD) + (1.0,),
                         effects=[calm(thought="AA_HeldToTerms", affects="AlliesNotSelf")])],
             desc="Nobody breaks down in front of the Pactbinder: any non-violent mental break within ten cells of him ends within half an hour, "
                  "and the one who broke thinks a little less of him for holding them to their contract."),
    )

    # ================================================================== DREAMLORD: the dreams of the colony
    dl = t["Dreamlord"]
    dl.add_spells(
        Spell("LucidLesson", "lucid lesson", "Send a colonist to bed at once (their own, if they can reach it) and teach them in their dreams: for "
              "the next eight hours they gain 300 experience in a random skill every hour, 2400 in all.", icon=("moon", "book"), mana=30, cd=1000,
              range=30, warmup=1.5, tags=["Colony"], school=M.DREAM, role="Utility", target="ally", quirky=True,
              impact_vfx=[spiral("star", 10, 1.2, 0.4), glow(1.2, 2.0, 30, useColor2=True)], effects=[sleep_now(), hediff("AA_LucidLesson", 1.0)],
              flavor="In the dream he was a master smith. In the morning he was a better one."),
        Spell("RudeAwakening", "rude awakening", "Tear every sleeping colonist on the map out of their dreams at once. They wake drafted, with 15% rest "
              "restored and a minute of nightmare-sharpened senses: 15% faster, 15% quicker to aim and steadier against pain. For the raid at three "
              "in the morning.", icon=("bell", "moon"), mana=50, cd=600, warmup=1.0, tags=["Colony"], school=M.DREAM, role="Utility", target="self",
              delivery="Global", affects="Allies", quirky=True, tier=2, cast_vfx=[pillar(1.6, 40), screen("#3a1a6a", 0.25, 30, vignette=True)],
              impact_vfx=[burst("star", 6, 1.5, 0.4)],
              effects=[dreamwork("Wake", rest=0.15, wokenHediff="AA_RudeAwakening", draft=True)], flavor="Wake up. Wake up. WAKE UP."),
        Spell("DevourNightmares", "devour nightmares", "Feast on a colonist's nightmares: their two worst bad memories (grief, horror, humiliation, "
              "whatever weighs most) are eaten away, three if they are asleep when you feed, and you regain 8 mana for each.", icon=("moon", "claw"),
              mana=30, cd=500, range=30, warmup=1.5, tags=["Colony"], school=M.DREAM, role="Utility", target="ally", quirky=True, tier=3,
              impact_vfx=[spiral("star", 14, -2.0, 0.5), glow(1.4, 0.4, 30, useColor2=True), smoke(3, 0.8)],
              effects=[dreamwork("Devour", maxMemories=2, bonusIfAsleep=1, manaPerMemory=8.0)], flavor="Nightmares are delicious. Grief is better."),
    )
    dl.add_nodes(
        Node("LucidLesson", None, (0, 0), kind="Spell", spell="LucidLesson"),
        Node("RudeAwakening", None, (0, 4), kind="Spell", spell="RudeAwakening", pre=["Terrors"]),
        Node("DevourNightmares", None, (6, 4), kind="Spell", spell="DevourNightmares", pre=["Phantasm"]),
    )

    # ================================================================== HELLPRINCE: drill, whip and court
    hp = t["Hellprince"]
    hp.add_spells(
        Spell("TrialByHellfire", "trial by hellfire", "Put the colonists around the target through a drill in hellfire: each gains 1500 melee and 1500 "
              "shooting experience, and comes out seared, with 15% more pain and 10% slower feet for six hours.", icon=("flame", "sword"), mana=50,
              cd=1000, range=20, warmup=2.0, tags=["Colony"], school=M.PRINCE, role="Utility", target="ally_area", radius=4.0, quirky=True,
              impact_vfx=[flash(2.0), ring(0.2, 1.0, 28), burst("ember", 16, 2.0, 0.6, byRadius=True)],
              effects=[skillxp(1500, "Melee"), skillxp(1500, "Shooting"), hediff("AA_TrialSeared", 1.0)], flavor="Again. Faster. Hotter."),
        Spell("CrackTheWhip", "crack the whip", "Crack a lash of hellfire over the colonists around the target. For eight hours they work 30% faster and "
              "move 10% faster, but nobody enjoys it: their recreation drops by a quarter at once and they suffer -5 mood for a day.",
              icon=("chain", "bolt"), mana=40, cd=1000, range=20, warmup=1.0, tags=["Colony"], school=M.PRINCE, role="Utility", target="ally_area",
              radius=6.0, quirky=True, tier=2, impact_vfx=[flash(1.6), ring(0.2, 1.0, 22), burst("ember", 12, 2.5, 0.5, byRadius=True)],
              effects=[hediff("AA_WhipDriven", 1.0), need("Joy", -0.25), memory("AA_WhipDrivenHard")], flavor="The harvest will be in by nightfall."),
        Spell("DemandTribute", "demand tribute", "A prince of hell does not trade; he is paid. Demand tribute from the lords of the rim, and 150 to 250 "
              "silver (more with spell power) appears in brimstone at your feet. Every faction that is not at war with you resents it: -8 goodwill "
              "with each.", icon=("crown", "coin"), mana=80, cd=5000, warmup=4.0, tags=["Colony"], school=M.PRINCE, role="Utility", target="self",
              quirky=True, tier=3, cast_vfx=[pillar(2.0, 50), screen("#ff5a1e", 0.15, 30)],
              impact_vfx=[ring(0.2, 1.4, 40, byRadius=False), burst("ember", 24, 3.0, 0.6), smoke(4, 1.0)],
              effects=[conjure("Silver", 150, 250), goodwill(-8, mode="AllNonHostile")], flavor="Tithes are due. Hell does not send reminders."),
    )
    hp.add_nodes(
        Node("TrialByHellfire", None, (0, 0), kind="Spell", spell="TrialByHellfire"),
        Node("InfernalWarmth", "infernal warmth", (0, 4), kind="Notable", pre=["PackLeader"],
             passives=[P("P_Aura", radius=8.0, interval=1250, affects="Allies", color=rgb("#ff5a1e") + (1.0,),
                         effects=[hediff("AA_InfernalWarmth", 1.0, duration=1500)])],
             desc="A walking furnace: allies within eight cells of the Hellprince are comfortable 10 degrees colder. Winter work, winter caravans "
                  "and winter sieges go easier at his side."),
        Node("CrackTheWhip", None, (6, 4), kind="Spell", spell="CrackTheWhip", pre=["Wrath"]),
        Node("DemandTribute", None, (6, 6), kind="Spell", spell="DemandTribute", pre=["GatesOfHell"]),
    )

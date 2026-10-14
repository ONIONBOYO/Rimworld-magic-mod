"""
The Incubus (men only; women walk the Succubus path). A devil of bargains: his spells put enemies in Debt, and at five
Debts he comes to Collect: hellfire, a share of their vitality, and their weapon. He signs contracts with his friends
(great power now, if they deliver), possesses enemies in their dreams, and wears hellfire as a crown.
"""
from classes.common import *  # noqa: F401,F403
from classes.expansion2 import missiles, channel, delayed  # noqa: F401
from classes import mythic_things as MT

VIOLET, EMBER = "#a64dff", "#ffb347"
PACT = School("Shadow", VIOLET, EMBER, fleck="ember", fleck2="smoke", projectile="ArcanumAscendant/Vfx/Fireball")
HELL = School("Fire", "#c060ff", "#ffb347", fleck="ember", fleck2="smoke", projectile="ArcanumAscendant/Vfx/Fireball")
BINDER = School("Shadow", "#ffb347", "#5a1a00", fleck="ember", fleck2="star", projectile="ArcanumAscendant/Vfx/Fireball")
DREAM = School("Shadow", "#7a4dff", "#ff7af0", fleck="star", fleck2="smoke", projectile="ArcanumAscendant/Vfx/VoidOrb")
PRINCE = School("Fire", "#ff5a1e", "#ffd27a", fleck="ember", fleck2="smoke", projectile="ArcanumAscendant/Vfx/Fireball")

DEBT = "AA_Debt"

KEYS = {
    "AA_Collected": "Debt collected",
    "AA_Foreclosed": "Foreclosed",
}


def debt(n=1, **kw):
    return stacks(DEBT, n, duration=2400, cap=10, **kw)


HELL_HIT = [flash(2.0), burst("ember", 10, 2.5, 0.5), smoke(2, 0.8)]


def statuses():
    s = []
    s.append(status(DEBT, "debt", "This creature owes the Incubus. Debts make it hesitate and fumble; at five he comes to collect: hellfire, a share of its "
        "very vitality, and the weapon from its hands.", VIOLET, bad=True, ticks=2400, maxSev=10,
        comps=[stack_trigger(5, boss=8, radius=3.0, color=EMBER, label="AA_Collected", shake=0.8,
                             effects=[dmg(40, "AA_Hellfire", lethalPercent=0.1, noCrit=True), disarm(True, fallback=0)],
                             vfx=[flash(3.0, color=EMBER), burst("ember", 24, 3.0, 0.6), ring(0.2, 1.0, 24, byRadius=False), pillar(1.0, 30)])],
        stages=[stage(offsets={"AimingDelayFactor": 0.06, "WorkSpeedGlobal": -0.04}, mult=True)],
        fx=dict(emitter="Debt", emitInterval=18, overlayTex="ArcanumAscendant/Vfx/ContractMark", overlaySize=0.45, overlayZ=1.05, overlayPulse=True)))
    s.append(status("AA_Contracted", "infernal contract", "Signed in blood: tremendous strength for forty seconds, but the contract must be paid with two "
        "kills, or the devil collects.", EMBER, bad=False, ticks=2400,
        comps=[contract(2, reward="AA_ContractReward", penalty="AA_ContractPenalty", color=EMBER)],
        stages=[stage(factors={"MeleeDamageFactor": 1.5, "AimingDelayFactor": 0.7, "MoveSpeed": 1.2, "IncomingDamageFactor": 0.85},
                      offsets={"AA_SpellPower": 0.3, "PainShockThreshold": 0.3})],
        fx=dict(emitter="Infernal", emitInterval=10, overlayTex="ArcanumAscendant/Vfx/ContractMark", overlaySize=0.45, overlayZ=1.05)))
    s.append(status("AA_BloodContract", "blood contract", "A greater contract, written in a great deal of blood: overwhelming power for a minute, paid for "
        "with three kills.", "#ff6a28", bad=False, ticks=3600,
        comps=[contract(3, reward="AA_ContractReward", penalty="AA_ContractPenalty", rewardSev=1.0, penaltySev=1.0, color="#ff6a28")],
        stages=[stage(factors={"MeleeDamageFactor": 1.9, "AimingDelayFactor": 0.55, "MoveSpeed": 1.3, "IncomingDamageFactor": 0.7},
                      offsets={"AA_SpellPower": 0.5, "PainShockThreshold": 0.6})],
        fx=dict(emitter="Infernal", emitInterval=6, overlayTex="ArcanumAscendant/Vfx/ContractMark", overlaySize=0.55, overlayZ=1.05, overlayPulse=True)))
    s.append(status("AA_ContractReward", "contract fulfilled", "The contract was honoured. The devil pays his debts too.", EMBER, bad=False, ticks=3600,
        comps=[dot(None, 0.0, EMBER, "AA_Fleck_Ember", heal_ps=2.0)], stages=[stage(offsets={"AA_SpellPower": 0.15, "AA_ManaRegen": 0.5})],
        fx=dict(emitter="Infernal", emitInterval=30)))
    s.append(status("AA_ContractPenalty", "collected", "The contract was broken, and the devil has collected his due: weak, aching and shaken.", "#ff3020",
        bad=True, ticks=5000, stages=[stage(factors={"MoveSpeed": 0.75, "WorkSpeedGlobal": 0.7, "MeleeDamageFactor": 0.7},
                                            offsets={"MentalBreakThreshold": 0.08}, caps={"Consciousness": -0.1})]))
    s.append(status("AA_Hellchained", "hellchained", "Bound by burning chains from below.", VIOLET, bad=True, ticks=360,
        comps=[dot("AA_Hellfire", 2.0, VIOLET, "AA_Fleck_Ember")], stages=[stage(factors={"MoveSpeed": 0.1, "MeleeCooldownFactor": 1.3})],
        fx=dict(emitter="Infernal", emitInterval=14, overlayTex="ArcanumAscendant/Vfx/Chains", overlaySize=1.3, overlaySpin=1.0)))
    s.append(status("AA_Kneeling", "kneeling", "Commanded to kneel, and unable to refuse.", VIOLET, bad=True, ticks=300,
        comps=[compel("Hold")], stages=[stage(factors={"IncomingDamageFactor": 1.25})], fx=dict(emitter="Debt", emitInterval=16)))
    s.append(status("AA_PactOfFlame", "pact of flame", "Bound to hellfire: immune to burning, and every spell burns hotter.", "#ff5a1e", bad=False, ticks=1800,
        stages=[stage(factors={"Flammability": 0.0}, offsets={"AA_SpellPower": 0.2, "ArmorRating_Heat": 0.8}, dmgf={"Flame": 0.0, "Burn": 0.0})],
        fx=dict(emitter="Fire", emitInterval=12)))
    s.append(status("AA_Usury", "usury", "Interest accrues: another Debt every two seconds.", EMBER, bad=True, ticks=1200,
        comps=[periodic(120, effects=[debt(1)], color=EMBER)], fx=dict(emitter="Debt", emitInterval=10)))
    s.append(status("AA_Collateral", "soul collateral", "Its soul is pledged as collateral. If it dies, the Incubus collects the pledge.", EMBER, bad=True,
        ticks=1800, comps=[death_trigger(caster=[mana(percent=0.3), cdreset(0.5, includeSelf=True), ward(30)], area=[debt(2)], radius=5.0, color=EMBER,
                                         label="AA_Foreclosed", shake=0.6, vfx=[burst("ember", 20, 3.0, 0.6, byRadius=True), flash(3.0, color=EMBER)])],
        fx=dict(emitter="Debt", emitInterval=20, overlayTex="ArcanumAscendant/Vfx/ContractMark", overlaySize=0.55, overlayZ=1.05, overlayPulse=True)))
    s.append(status("AA_NightmareSeed", "nightmare seed", "A nightmare has taken root. It feeds on its host and, if the host dies, it seeds itself in everyone "
        "nearby.", "#7a4dff", bad=True, ticks=1200,
        comps=[dot("AA_Heartbreak", 2.0, "#7a4dff", "AA_Fleck_Smoke"),
               death_trigger(area=[hediff("AA_NightmareSeed", 1.0), debt(1)], radius=4.0, color="#7a4dff", vfx=[burst("star", 16, 2.5, 0.5, byRadius=True)])],
        stages=[stage(offsets={"MentalBreakThreshold": 0.1}, factors={"ShootingAccuracyPawn": 0.85})], fx=dict(emitter="Dream", emitInterval=10)))
    s.append(status("AA_SleepParalysis", "sleep paralysis", "Awake but unable to move, with something sitting on its chest.", "#7a4dff", bad=True, ticks=480,
        comps=[compel("Hold"), dot("AA_Heartbreak", 1.5, "#7a4dff")], stages=[stage(factors={"IncomingDamageFactor": 1.15})],
        fx=dict(emitter="Dream", emitInterval=8, overlayTex="ArcanumAscendant/Vfx/Smile", overlaySize=0.5, overlayZ=1.1)))
    s.append(status("AA_WakingNightmare", "waking nightmare", "Fleeing from something no one else can see.", "#7a4dff", bad=True, ticks=420,
        comps=[compel("Flee")], fx=dict(emitter="Dream", emitInterval=8)))
    s.append(status("AA_InfernalForm", "infernal form", "Wearing his true, infernal shape: horns, hellfire and terrible strength.", "#ff5a1e", bad=False, ticks=2400,
        stages=[stage(factors={"MeleeDamageFactor": 2.0, "MoveSpeed": 1.15, "IncomingDamageFactor": 0.65, "Flammability": 0.0},
                      offsets={"AA_SpellPower": 0.35, "ArmorRating_Sharp": 0.3, "ArmorRating_Blunt": 0.3, "ArmorRating_Heat": 1.0, "PainShockThreshold": 0.5},
                      dmgf={"Flame": 0.0, "Burn": 0.0})],
        fx=dict(emitter="Infernal", emitInterval=4, overlayTex="ArcanumAscendant/Vfx/Circle_Fire", overlaySize=2.4, overlaySpin=3.0)))
    s.append(status("AA_Hellspawn", "hellspawn", "Born of hellfire: immune to burning, savage in a fight, and scorching to stand near.", "#ff5a1e", bad=False,
        comps=[periodic(120, area=[dmg(6, "AA_Hellfire"), hediff("AA_Burning", 1.0)], radius=2.0, color="#ff5a1e")],
        stages=[stage(factors={"MeleeDamageFactor": 1.4, "MoveSpeed": 1.15, "Flammability": 0.0}, offsets={"ArmorRating_Heat": 1.0}, dmgf={"Flame": 0.0, "Burn": 0.0})],
        fx=dict(emitter="Fire", emitInterval=6)))
    s.append(MT.construct("Construct_Hellgate", "hellgate", "A crack into somewhere very hot. It breathes hellfire on every enemy nearby and lays debts on them. "
        "It closes after half a minute.", tex="Hellgate", color="#ff5a1e", hp=400, lifetime=1800, size=2.0, glow=2.8, light=6.0,
        pulse=[dmg(10, "AA_Hellfire"), hediff("AA_Burning", 1.0), debt(1)], radius=5.0, interval=120))
    return s


def build():
    ic = ClassSpec("Incubus", "incubus", color=VIOLET, color2=EMBER, icon=("horns", "scroll"), order=13, school=PACT, gender="Male", gender_group="Seduction",
        tagline="Every deal has a price. Put enemies in debt and collect, sign contracts with your friends, and steal what you covet.",
        desc="Incubi are devils of the bargain. Everything has a price, and they are very, very good at collecting.",
        playstyle="Lay Debts with hellfire brands, chains and commands. At five Debts the Incubus collects: hellfire, a share of the victim's "
                  "vitality, and the weapon out of its hands. Devil's Due calls in every Debt on the map at once. Infernal Contracts make an "
                  "ally monstrously strong if they deliver two kills. Only men can take this path. Wants Potency and Attunement.",
        lore="'Sign here,' he said. 'And here. And initial here. This one is your soul. Lovely.'",
        primary="POT", secondary="ATT", tags=["Desire", "Infernal"], starting=["HellfireBrand"], innate={"AA_CritChance": 0.03})
    ic.add_spells(
        Spell("HellfireBrand", "hellfire brand", "Fling a brand of violet hellfire. It burns, and it marks the target as owing you: one Debt.", icon=("flame", "scroll"),
              mana=8, cd=1.5, range=26, warmup=0.4, tags=["Infernal", "Fire"], school=HELL, delivery="Projectile", projectileSpeed=24.0, projectileScale=0.75,
              impact_vfx=HELL_HIT, effects=[dmg(11, "AA_Hellfire"), debt(1)], flavor="Payment is due upon receipt."),
        Spell("Covet", "covet", "Covet an enemy's weapon, and it is yours: it tears out of their hands and lands at your feet. The enemy owes you for the trouble.",
              icon=("hand", "gun"), mana=20, cd=25, range=24, warmup=0.4, tags=["Infernal"], role="Control", impact_vfx=HELL_HIT,
              effects=[disarm(True, fallback=16, fallbackDef="AA_Hellfire"), debt(1)], flavor="Thou shalt not. He does anyway."),
        Spell("CommandKneel", "command: kneel", "Speak with the voice of a prince of hell. The target kneels for five seconds, takes more damage while it does, and "
              "owes you two Debts.", icon=("crown", "hand"), mana=24, cd=16, range=24, warmup=0.4, tags=["Infernal"], role="Control", impact_vfx=HELL_HIT,
              effects=[hediff("AA_Kneeling", 1.0), debt(2), stun(30)]),
        Spell("Hellchains", "hellchains", "Burning chains erupt from the ground in an area, binding every enemy there for six seconds and burning them. One Debt "
              "each.", icon=("chain", "flame"), mana=30, cd=18, range=26, warmup=0.6, tags=["Infernal", "Fire"], school=HELL, role="Control",
              target="enemy_area", radius=2.5, ai_min=2, impact_vfx=[flash(2.0), burst("ember", 14, 2.0, 0.6, byRadius=True), ring(0.2, 0.9, 24)],
              effects=[hediff("AA_Hellchained", 1.0), debt(1)]),
        Spell("Collect", "collect", "Call in a debt early: hellfire damage, +50% for every Debt the target owes (all of them spent).", icon=("coin", "hand"),
              mana=24, cd=10, range=26, warmup=0.5, tags=["Infernal"], impact_vfx=HELL_HIT + [burst("star", 8, 2.0, 0.5)],
              effects=[dmg(22, "AA_Hellfire", stackHediff=DEBT, bonusPerStack=0.5, consumeStacks=True)]),
        Spell("InfernalContract", "infernal contract", "Offer an ally a contract: for forty seconds they are monstrously strong. If they make two kills before it "
              "runs out they are rewarded; if not, the devil collects.", icon=("contract", "flame"), mana=30, cd=40, range=20, warmup=0.8,
              tags=["Infernal"], role="Buff", target="ally", impact_vfx=[burst("ember", 16, 2.0, 0.5), pillar(0.8, 30)], effects=[hediff("AA_Contracted", 1.0)],
              flavor="Read the fine print. Actually, don't."),
        Spell("PactOfFlame", "pact of flame", "Make a pact with hellfire for thirty seconds: immune to burning, and your spells burn hotter.", icon=("flame", "horns"),
              mana=24, cd=45, warmup=0.4, tags=["Infernal", "Fire"], school=HELL, role="Buff", target="self", impact_vfx=[burst("ember", 16, 2.0, 0.5), flash(2.0)],
              effects=[hediff("AA_PactOfFlame", 1.0)]),
        Spell("SoulBargain", "soul bargain", "Sell a little of your own soul: lose some health, regain two fifths of your mana at once.", icon=("heart", "orb"),
              mana=0, cd=60, warmup=0.3, tags=["Infernal", "Utility"], role="Utility", target="self", health_cost=18, autocast=False,
              impact_vfx=[burst("ember", 10, 1.5, 0.5)], effects=[mana(percent=0.4)]),
        Spell("Brimstone", "brimstone", "Brimstone erupts from the earth in an area: eight burning strikes, each one laying a Debt half the time.",
              icon=("volcano", "flame"), mana=46, cd=28, range=30, warmup=1.0, tags=["Infernal", "Fire"], school=HELL, target="enemy_area", delivery="Barrage",
              barrageCount=8, barrageInterval=7, barrageRadius=4.0, strikeRadius=1.5, strikeStyle="Eruption", ai_min=2, tier=2, shake=1.0,
              impact_vfx=[flash(2.0), burst("ember", 10, 3.0, 0.6)], effects=[dmg(22, "AA_Hellfire"), ignite(0.3), debt(1, chance=0.5)]),
        Spell("DevilsDue", "devil's due", "Call in every debt at once. Every enemy on the map takes hellfire damage, +60% for every Debt it owes (spent).",
              icon=("scroll", "burst"), mana=70, cd=60, warmup=1.5, tags=["Infernal"], target="self", delivery="Global", tier=3, ai_min=3,
              cast_vfx=[pillar(2.0, 50), screen(EMBER, 0.2, 30)], impact_vfx=[flash(2.0), burst("ember", 8, 2.0, 0.5)],
              effects=[dmg(10, "AA_Hellfire", stackHediff=DEBT, bonusPerStack=0.6, consumeStacks=True)]),
        Spell("DealWithTheDevil", "deal with the devil", "Offer a colonist a deal. They will gain something wonderful: an inspiration, years of skill, a healed "
              "body or fantastic luck. Usually.", icon=("contract", "dice"), mana=40, cd=DAY / SEC, range=4, warmup=4.0, tags=["Infernal", "Utility"],
              role="Utility", target="ally", quirky=True, impact_vfx=[pillar(1.0, 40), burst("ember", 12, 1.5, 0.5)],
              effects=[random_of([inspire(), skillxp(4000), heal(60), hediff("AA_Lucky", 1.0, duration=10000), hediff("AA_ContractPenalty", 1.0)])],
              flavor="Terms and conditions apply."),
    )
    ic.add_nodes(
        Node("Avarice", "avarice", (1, 0), ranks=3, mods=[tagmod("Infernal", damage=0.05)]),
        Node("SmallPrint", "small print", (3, 0), ranks=3, stats={"AA_ManaMax": 10, "AA_ManaCostFactor": -0.02}),
        Node("Brimfire", "brimfire", (5, 0), ranks=3, mods=[tagmod("Fire", damage=0.05)]),
        Node("Covet", None, (1, 1), kind="Spell", spell="Covet", pre=["Avarice"]),
        Node("Collect", None, (3, 1), kind="Spell", spell="Collect", pre=["SmallPrint"]),
        Node("Hellchains", None, (5, 1), kind="Spell", spell="Hellchains", pre=["Brimfire"]),
        Node("Greed", "greed", (0, 2), kind="Augment", ranks=2, pre=["Covet"], mods=[spellmod(["Covet"], cooldown=-0.2, range=0.2)]),
        Node("Compound", "compound interest", (2, 2), kind="Notable", ranks=2, pre=["Covet", "Collect"],
             passives=[P("P_OnHitHediff", hediff=DEBT, chancePerRank=0.15, severity=1.0, stack=True, maxSeverity=10.0, durationTicks=2400, tag="Infernal")],
             desc="Infernal spells sometimes add an extra Debt."),
        Node("CommandKneel", None, (4, 2), kind="Spell", spell="CommandKneel", pre=["Collect", "Hellchains"]),
        Node("Shackles", "shackles", (6, 2), kind="Augment", ranks=2, pre=["Hellchains"], mods=[spellmod(["Hellchains"], radius=0.2, duration=0.3)]),
        Node("InfernalContract", None, (1, 3), kind="Spell", spell="InfernalContract", pre=["Greed", "Compound"]),
        Node("Usurer", "usurer", (3, 3), ranks=3, pre=["Compound", "CommandKneel"], pct={"AA_SpellPower": 0.03}),
        Node("PactOfFlame", None, (5, 3), kind="Spell", spell="PactOfFlame", pre=["CommandKneel", "Shackles"]),
        Node("FinePrint", "fine print", (0, 4), kind="Augment", ranks=2, pre=["InfernalContract"], mods=[spellmod(["InfernalContract"], duration=0.3, cooldown=-0.15)]),
        Node("SoulBargain", None, (2, 4), kind="Spell", spell="SoulBargain", pre=["InfernalContract", "Usurer"]),
        Node("Interest", "interest", (4, 4), kind="Notable", ranks=2, pre=["Usurer", "PactOfFlame"],
             passives=[P("P_TriggerSpell", trigger="OnStackTrigger", spell=S("HellfireBrand"), chancePerRank=0.5, target="NearestEnemy", stackHediff=DEBT)],
             desc="Whenever you collect, you open a new account: a free Hellfire Brand at the nearest enemy."),
        Node("Sulphur", "sulphur", (6, 4), ranks=3, pre=["PactOfFlame"], stats={"ArmorRating_Heat": 0.08, "AA_WardMax": 6}),
        Node("Brimstone", None, (1, 5), kind="Spell", spell="Brimstone", pre=["FinePrint", "SoulBargain"]),
        Node("Ledger", "ledger", (3, 5), kind="Notable", ranks=2, pre=["SoulBargain", "Interest"], passives=[P("P_ManaOnKill", amount=7.0)],
             desc="Every closed account pays out in mana."),
        Node("DealWithTheDevil", None, (5, 5), kind="Spell", spell="DealWithTheDevil", pre=["Interest", "Sulphur"]),
        Node("Hellbound", "hellbound", (0, 6), ranks=3, pre=["Brimstone"], stats={"AA_CritDamage": 0.06}),
        Node("DevilsDue", None, (2, 6), kind="Spell", spell="DevilsDue", pre=["Brimstone", "Ledger"]),
        Node("SilverTongued", "silver-tongued", (4, 6), kind="Notable", ranks=2, pre=["Ledger", "DealWithTheDevil"],
             mods=[tagmod("Infernal", damage=0.08, critChance=0.03)], desc="You always get the better end of the deal."),
        Node("Ownership", "ownership", (6, 6), ranks=3, pre=["DealWithTheDevil"], stats={"AA_CooldownReduction": 0.02, "AA_ManaRegen": 0.08}),
        Node("Loanshark", "loan shark", (2, 7), kind="Keystone", excl="IncubusKeystone", pre=["DevilsDue", "Hellbound"],
             mods=[spellmod(["DevilsDue", "Collect"], cooldown=-0.35), tagmod("Infernal", damage=0.15)],
             desc="Payment is due now. Devil's Due and Collect come back far sooner, and everything infernal hits harder."),
        Node("PrinceOfLies", "prince of lies", (4, 7), kind="Keystone", excl="IncubusKeystone", pre=["SilverTongued", "Ownership"],
             passives=[P("P_OnHitHediff", hediff=DEBT, chancePerRank=0.5, severity=1.0, stack=True, maxSeverity=10.0, durationTicks=2400, tag="Infernal"),
                       P("P_DodgeBlink", chancePerRank=0.12)],
             desc="Everyone owes you, and nobody can pin you down: half your infernal hits add a Debt, and attacks often find only an illusion."),
        Node("ArchDevil", "archdevil", (3, 8), kind="Keystone", pre=["Loanshark", "PrinceOfLies"], pct={"AA_SpellPower": 0.15},
             passives=[P("P_KillExplosion", damageDef="AA_Hellfire", damage=30.0, radius=3.0, chance=0.5, spellKillsOnly=False)],
             desc="Hell follows you. Anything you kill may burst into hellfire."),
    )

    # ------------------------------------------------------------------ PACTBINDER
    pb = ClassSpec("Pactbinder", "pactbinder", color="#ffb347", color2="#5a1a00", icon=("contract", "chain"), parent=ic, order=0, school=BINDER,
        tagline="Every soul has a price, and every price has interest. Usury, collateral, blood contracts and foreclosure.",
        desc="Pactbinders are the accountants of hell, and nothing in hell is more frightening than an accountant.",
        playstyle="The Debt engine. Usury makes Debts accrue on their own, Soul Collateral pays you when a debtor dies, Breach of Contract "
                  "cashes Debts in for enormous single-target damage, and Foreclosure collects from everyone around you at once. Blood "
                  "Contracts make an ally a demigod for a minute. Wants Potency and Focus.",
        lore="He does not need to fight. Everyone already owes him.", primary="POT", secondary="FOC", tags=["Infernal"], height=8)
    pb.add_spells(
        Spell("Usury", "usury", "Charge interest on an enemy's soul: for twenty seconds it gains a Debt every two seconds.", icon=("coin", "hourglass"),
              mana=26, cd=18, range=26, warmup=0.5, tags=["Infernal"], role="Control", impact_vfx=HELL_HIT, effects=[hediff("AA_Usury", 1.0), debt(1)]),
        Spell("SoulCollateral", "soul collateral", "Take an enemy's soul as collateral and lay two Debts. If it dies within thirty seconds, you regain mana and a ward, "
              "your other spells cool down by half, and its friends inherit the debt.", icon=("contract", "skull"), mana=30, cd=25, range=26, warmup=0.5,
              tags=["Infernal"], role="Control", impact_vfx=HELL_HIT, effects=[hediff("AA_Collateral", 1.0), debt(2)]),
        Spell("BreachOfContract", "breach of contract", "Punish a defaulter: hellfire damage, +40% for every Debt (spent), plus a share of its vitality.",
              icon=("contract", "burst"), mana=40, cd=20, range=26, warmup=0.6, tags=["Infernal"], tier=2,
              impact_vfx=HELL_HIT + [pillar(1.0, 30), ring(0.3, 1.0, 24, byRadius=False)],
              effects=[dmg(40, "AA_Hellfire", stackHediff=DEBT, bonusPerStack=0.4, consumeStacks=True, lethalPercent=0.05)]),
        Spell("Garnishment", "garnishment", "Garnish the wages of everyone around you: hellfire damage, doubled against anyone in debt, healing you for two fifths.",
              icon=("coin", "swirl"), mana=36, cd=25, warmup=0.6, tags=["Infernal"], target="self", delivery="Nova", radius=6.0, ai_min=2, tier=2,
              impact_vfx=[ring(1.1, 0.1, 30), spiral("star", 14, -2.5, 0.6)],
              effects=[dmg(14, "AA_Hellfire", bonusVsHediff=DEBT, bonusVsHediffFactor=2.0, healCasterFraction=0.4)], caster_effects=[mana(10)]),
        Spell("BloodContract", "blood contract", "Offer an ally a contract written in a great deal of blood: overwhelming power for a minute, paid for with three "
              "kills.", icon=("contract", "drop"), mana=40, cd=60, range=20, warmup=1.0, tags=["Infernal"], role="Buff", target="ally", tier=3,
              impact_vfx=[pillar(1.2, 40), burst("ember", 20, 2.5, 0.6), burst("blood", 8, 1.5, 0.5)], effects=[hediff("AA_BloodContract", 1.0)]),
        Spell("Foreclosure", "foreclosure", "Foreclose on every soul within fifteen cells: each one is driven five Debts deeper and collected at once.",
              icon=("scroll", "crown"), mana=220, cd=300, warmup=2.5, tags=["Infernal"], target="self", delivery="Nova", radius=15.0, ultimate=True, tier=5,
              xp=2.0, shake=2.0, ai_min=4, cast_vfx=[pillar(2.6, 70), screen(EMBER, 0.3, 50)],
              impact_vfx=[ring(0.1, 1.2, 50), burst("ember", 40, 4.0, 0.6, byRadius=True)], effects=[dmg(30, "AA_Hellfire"), debt(5)]),
    )
    pb.add_nodes(
        Node("Usury", None, (2, 0), kind="Spell", spell="Usury"),
        Node("Accountant", "accountant", (4, 0), ranks=3, stats={"AA_ManaMax": 12}),
        Node("Extortion", "extortion", (1, 1), kind="Augment", ranks=2, pre=["Usury"], mods=[spellmod(["Usury"], duration=0.3, cooldown=-0.15)]),
        Node("SoulCollateral", None, (3, 1), kind="Spell", spell="SoulCollateral", pre=["Usury", "Accountant"]),
        Node("Ledgers", "ledgers", (5, 1), ranks=3, pre=["Accountant"], stats={"AA_ManaRegen": 0.1}),
        Node("Arrears", "arrears", (0, 2), ranks=3, pre=["Extortion"], mods=[tagmod("Infernal", duration=0.06)]),
        Node("BreachOfContract", None, (2, 2), kind="Spell", spell="BreachOfContract", pre=["Extortion", "SoulCollateral"]),
        Node("Lien", "lien", (4, 2), kind="Augment", ranks=2, pre=["SoulCollateral", "Ledgers"], mods=[spellmod(["SoulCollateral"], cooldown=-0.2, range=0.2)]),
        Node("Garnishment", None, (6, 2), kind="Spell", spell="Garnishment", pre=["Ledgers"]),
        Node("Penalty", "penalty clause", (1, 3), kind="Augment", ranks=2, pre=["Arrears", "BreachOfContract"], mods=[spellmod(["BreachOfContract"], damage=0.25)]),
        Node("BloodContract", None, (3, 3), kind="Spell", spell="BloodContract", pre=["BreachOfContract", "Lien"]),
        Node("Skim", "skim", (5, 3), kind="Augment", ranks=2, pre=["Lien", "Garnishment"], mods=[spellmod(["Garnishment"], radius=0.2, damage=0.15)]),
        Node("Creditor", "creditor", (2, 4), kind="Notable", ranks=2, pre=["Penalty", "BloodContract"], passives=[P("P_CooldownOnKill", ticksPerRank=120)],
             desc="Every debtor who dies frees up your schedule."),
        Node("Witnessed", "witnessed", (4, 4), kind="Notable", ranks=2, pre=["BloodContract", "Skim"], mods=[spellmod(["BloodContract", "InfernalContract"], duration=0.3)],
             stats={"AA_HealingPower": 0.05}, desc="Contracts you witness last longer."),
        Node("Repossess", "repossess", (1, 5), ranks=3, pre=["Creditor"], stats={"AA_CritChance": 0.02}),
        Node("BadDebt", "bad debt", (3, 5), ranks=3, pre=["Creditor", "Witnessed"], pct={"AA_SpellPower": 0.03}),
        Node("Escrow", "escrow", (5, 5), ranks=3, pre=["Witnessed"], stats={"AA_WardMax": 10}),
        Node("HostileTakeover", "hostile takeover", (2, 6), kind="Keystone", excl="PactbinderKeystone", pre=["Repossess", "BadDebt"],
             passives=[P("P_OnHitHediff", hediff=DEBT, chancePerRank=1.0, severity=1.0, stack=True, maxSeverity=10.0, durationTicks=2400, tag="Infernal",
                         cooldownTicks=30)], desc="Every infernal hit adds a Debt (at most twice a second)."),
        Node("Bankrupt", "bankrupt", (4, 6), kind="Keystone", excl="PactbinderKeystone", pre=["BadDebt", "Escrow"],
             mods=[spellmod(["BreachOfContract", "Garnishment"], cooldown=-0.4), tagmod("Infernal", damage=0.2)],
             desc="Cash out constantly. Breach of Contract and Garnishment come back far sooner, and everything infernal hits harder."),
        Node("Foreclosure", None, (3, 7), kind="Ultimate", spell="Foreclosure", pre=["HostileTakeover", "Bankrupt"]),
    )

    # ------------------------------------------------------------------ DREAMLORD
    dl = ClassSpec("Dreamlord", "dreamlord", color="#7a4dff", color2="#ff7af0", icon=("moon", "horns"), parent=ic, order=1, school=DREAM,
        tagline="He visits in the night. Possess enemies through their dreams, plant nightmares that spread, and rule the dreaming world.",
        desc="The incubus of legend comes in dreams. The Dreamlord just does it to whole armies.",
        playstyle="Possession and spreading nightmares. Possess takes an enemy for thirty seconds; Nightmare Seed eats its host and jumps to "
                  "everyone nearby when the host dies; Sleep Paralysis pins a target in place. The Lord of Dreams possesses or paralyses every "
                  "enemy on the map. Wants Attunement and Focus.",
        lore="They found the guards asleep at their posts, smiling, and the gate wide open.", primary="ATT", secondary="FOC", tags=["Infernal", "Dream"], height=8)
    dl.add_spells(
        Spell("NightmareSeed", "nightmare seed", "Plant a nightmare in an enemy. It feeds on its host for twenty seconds, and if the host dies it seeds itself in "
              "everyone nearby.", icon=("moon", "skull"), mana=30, cd=15, range=26, warmup=0.5, tags=["Infernal", "Dream"], role="Control",
              impact_vfx=[burst("star", 10, 2.0, 0.5), glow(1.2, 2.2, 20, useColor2=True)], effects=[hediff("AA_NightmareSeed", 1.0), debt(1)]),
        Spell("SleepParalysis", "sleep paralysis", "Something sits on an enemy's chest: it cannot move for eight seconds and suffers. Machines do not dream.",
              icon=("moon", "smile"), mana=34, cd=24, range=26, warmup=0.6, tags=["Infernal", "Dream"], role="Control", impact_vfx=[burst("star", 10, 1.5, 0.5)],
              effects=[hediff("AA_SleepParalysis", 1.0), debt(1)]),
        Spell("WakingNightmare", "waking nightmare", "Pull the nightmares of everyone in an area into the waking world: they flee in terror for seven seconds.",
              icon=("eye", "moon"), mana=36, cd=25, range=26, warmup=0.7, tags=["Infernal", "Dream"], role="Control", target="enemy_area", radius=3.5,
              ai_min=2, tier=2, impact_vfx=[ring(0.2, 1.0, 26), burst("star", 16, 2.5, 0.5, byRadius=True)],
              effects=[hediff("AA_WakingNightmare", 1.0), debt(1)]),
        Spell("Dreamshroud", "dreamshroud", "Step halfway into the dreaming world for fifteen seconds: enemies cannot see you.", icon=("moon", "mask"),
              mana=30, cd=60, warmup=0.3, tags=["Infernal", "Dream"], role="Buff", target="self", tier=2, impact_vfx=[smoke(4, 1.0), burst("star", 10, 1.5, 0.5)],
              effects=[hediff("AA_Invisible", 1.0)]),
        Spell("Possess", "possess", "Slip into an enemy's dreams and take the wheel: for thirty seconds it fights for you. Not the mightiest, and not machines too big "
              "to dream.", icon=("horns", "eye"), mana=70, cd=90, range=24, warmup=1.2, tags=["Infernal", "Dream"], role="Control", tier=3,
              impact_vfx=[pillar(1.0, 40), burst("star", 20, 2.0, 0.6), ring(0.6, 0.1, 24, byRadius=False)], effects=[dominate(1800, maxBodySize=3.0), debt(2)]),
        Spell("LordOfDreams", "lord of dreams", "Rule the dreaming world. Every enemy on the map is either possessed for twenty seconds or paralysed in its sleep, "
              "and all of them owe you.", icon=("crown", "moon"), mana=230, cd=300, warmup=3.0, tags=["Infernal", "Dream"], target="self", delivery="Global",
              ultimate=True, tier=5, xp=2.0, shake=1.0, ai_min=4, cast_vfx=[pillar(2.6, 70), screen("#3a1a6a", 0.4, 80, vignette=True)],
              impact_vfx=[burst("star", 8, 1.5, 0.5)],
              effects=[random_of([dominate(1200, maxBodySize=3.0), hediff("AA_SleepParalysis", 1.0)]), debt(2)]),
    )
    dl.add_nodes(
        Node("NightmareSeed", None, (2, 0), kind="Spell", spell="NightmareSeed"),
        Node("Nocturne", "nocturne", (4, 0), ranks=3, stats={"AA_ManaRegen": 0.08, "AA_CastSpeed": 0.02}),
        Node("Fester", "fester", (1, 1), kind="Augment", ranks=2, pre=["NightmareSeed"], mods=[spellmod(["NightmareSeed"], damage=0.25, duration=0.2)]),
        Node("SleepParalysis", None, (3, 1), kind="Spell", spell="SleepParalysis", pre=["NightmareSeed", "Nocturne"]),
        Node("Midnight", "midnight", (5, 1), ranks=3, pre=["Nocturne"], mods=[tagmod("Dream", damage=0.05)]),
        Node("Dreamer", "dreamer", (0, 2), ranks=3, pre=["Fester"], stats={"AA_DurationFactor": 0.05}),
        Node("WakingNightmare", None, (2, 2), kind="Spell", spell="WakingNightmare", pre=["Fester", "SleepParalysis"]),
        Node("HeavyChest", "heavy chest", (4, 2), kind="Augment", ranks=2, pre=["SleepParalysis", "Midnight"], mods=[spellmod(["SleepParalysis"], duration=0.3, cooldown=-0.15)]),
        Node("Dreamshroud", None, (6, 2), kind="Spell", spell="Dreamshroud", pre=["Midnight"]),
        Node("Terrors", "terrors", (1, 3), kind="Augment", ranks=2, pre=["Dreamer", "WakingNightmare"], mods=[spellmod(["WakingNightmare"], radius=0.25, duration=0.2)]),
        Node("Possess", None, (3, 3), kind="Spell", spell="Possess", pre=["WakingNightmare", "HeavyChest"]),
        Node("Phantasm", "phantasm", (5, 3), kind="Notable", ranks=2, pre=["HeavyChest", "Dreamshroud"], passives=[P("P_DodgeBlink", chancePerRank=0.08)],
             desc="Blows aimed at you sometimes strike a dream."),
        Node("Puppetmaster", "puppetmaster", (2, 4), kind="Notable", ranks=2, pre=["Terrors", "Possess"], mods=[spellmod(["Possess"], duration=0.3, cooldown=-0.15)],
             desc="Possessions last longer and come back sooner."),
        Node("Incubate", "incubate", (4, 4), kind="Notable", ranks=2, pre=["Possess", "Phantasm"],
             passives=[P("P_OnHitHediff", hediff="AA_NightmareSeed", chancePerRank=0.12, severity=1.0, tag="Dream")], desc="Dream spells sometimes plant a nightmare."),
        Node("DeepSleep", "deep sleep", (1, 5), ranks=3, pre=["Puppetmaster"], pct={"AA_SpellPower": 0.03}),
        Node("NightTerrors", "night terrors", (3, 5), ranks=3, pre=["Puppetmaster", "Incubate"], stats={"AA_CritChance": 0.02, "AA_CritDamage": 0.05}),
        Node("Somnambulist", "somnambulist", (5, 5), ranks=3, pre=["Incubate"], stats={"AA_WardMax": 10}),
        Node("DreamTyrant", "dream tyrant", (2, 6), kind="Keystone", excl="DreamlordKeystone", pre=["DeepSleep", "NightTerrors"],
             mods=[spellmod(["Possess"], extraTargets=1, cooldown=-0.3)], desc="Possess takes two minds at once and returns sooner."),
        Node("Nightmarelord", "nightmare lord", (4, 6), kind="Keystone", excl="DreamlordKeystone", pre=["NightTerrors", "Somnambulist"],
             mods=[tagmod("Dream", damage=0.25, duration=0.3)], desc="Every nightmare you plant is deeper and crueller."),
        Node("LordOfDreams", None, (3, 7), kind="Ultimate", spell="LordOfDreams", pre=["DreamTyrant", "Nightmarelord"]),
    )

    # ------------------------------------------------------------------ HELLPRINCE
    hp = ClassSpec("Hellprince", "hellprince", color="#ff5a1e", color2="#ffd27a", icon=("horns", "flame"), parent=ic, order=2, school=PRINCE,
        tagline="Stop pretending. Take your true form, drag enemies in with a lash of hellfire, and open the gates of hell.",
        desc="Some incubi bargain. Hellprinces remember who they were before they learned to smile, and they do not bargain.",
        playstyle="A burning brawler. Hellfire Lash drags enemies to you, Infernal Form doubles your strength and armours you in hellfire, "
                  "Hellhounds and Hellgates scorch everything near them, and the Crown of Ash turns you into a walking inferno that lays Debts "
                  "on everything it burns. Wants Potency and Resilience.",
        lore="The horns were always there. He just stopped hiding them.", primary="POT", secondary="RES", tags=["Infernal", "Fire"], height=8)
    hp.add_spells(
        Spell("HellfireLash", "hellfire lash", "Crack a whip of hellfire at an enemy and drag it through the air to your feet, burning.", icon=("chain", "flame"),
              mana=22, cd=10, range=16, warmup=0.3, tags=["Infernal", "Fire"], impact_vfx=HELL_HIT,
              effects=[dmg(18, "AA_Hellfire"), hediff("AA_Burning", 1.0), debt(1), launch("TowardCaster", 12, 8, "AA_Hellfire", stunTicks=40)]),
        Spell("InfernalForm", "infernal form", "Take your true shape for forty seconds: horns and hellfire, twice the strength, heavy armour and immunity to fire.",
              icon=("horns", "burst"), mana=60, cd=120, warmup=1.0, tags=["Infernal", "Fire"], role="Buff", target="self",
              impact_vfx=[pillar(1.6, 40), burst("ember", 30, 3.0, 0.6), flash(3.0, color="#ff5a1e")], effects=[hediff("AA_InfernalForm", 1.0)]),
        Spell("Hellhounds", "hellhounds", "Whistle up two hellhounds for a minute. They are savage, immune to fire and scorch everything they stand near.",
              icon=("paw", "flame"), mana=50, cd=90, range=20, warmup=1.2, tags=["Infernal", "Fire", "Summon"], role="Summon", target="cell", tier=2,
              impact_vfx=[flash(2.5), burst("ember", 20, 2.0, 0.6), smoke(4, 1.0)], effects=[summon("Warg", 2, MIN, extraHediff="AA_Hellspawn")]),
        Spell("RainOfBrimstone", "rain of brimstone", "Five burning stones fall from the sky on and around the target.", icon=("meteor", "flame"),
              mana=70, cd=45, range=32, warmup=1.4, tags=["Infernal", "Fire"], target="enemy_area", delivery="Meteor", projectileCount=5,
              multiTargetRadius=6.0, radius=2.5, ai_min=2, tier=3, shake=1.5, projectileTex="ArcanumAscendant/Vfx/Meteor", projectileScale=1.1,
              impact_vfx=[flash(3.0), burst("ember", 20, 3.5, 0.6, byRadius=True), smoke(5, 1.4)],
              effects=[dmg(40, "AA_Hellfire", falloff=True), ignite(0.4), debt(1)]),
        Spell("Hellgate", "hellgate", "Tear open a crack into hell for thirty seconds. It breathes hellfire on every enemy near it and lays Debts on them.",
              icon=("portal", "flame"), mana=50, cd=60, range=20, warmup=1.0, tags=["Infernal", "Fire"], role="Summon", target="cell", tier=3,
              impact_vfx=[pillar(1.6, 40), flash(3.0, color="#ff5a1e"), burst("ember", 20, 3.0, 0.6)], effects=[spawn("AA_Construct_Hellgate", lifetimeTicks=1800)]),
        Spell("CrownOfAsh", "crown of ash", "Crown yourself in hellfire for thirty seconds: you take your infernal form, and everything within six cells of you "
              "burns and falls into debt every second and a half.", icon=("crown", "flame"), mana=220, cd=300, warmup=2.0, tags=["Infernal", "Fire"],
              role="Control", target="self", radius=6.0, ultimate=True, tier=5, xp=2.0, shake=1.5, ai_min=3,
              cast_vfx=[pillar(2.6, 70), screen("#ff5a1e", 0.3, 40)], caster_effects=[hediff("AA_InfernalForm", 1.0)],
              impact_vfx=[ring(0.1, 1.2, 40), burst("ember", 40, 4.0, 0.6, byRadius=True)],
              effects=[field(6.0, 1800, 90, [dmg(10, "AA_Hellfire", affects="Enemies"), hediff("AA_Burning", 1.0, affects="Enemies"), debt(1, affects="Enemies")],
                             followCaster=True, groundTex="ArcanumAscendant/Vfx/Circle_Fire", groundSpin=2.0, groundAlpha=0.55, ambientFleck="AA_Fleck_Ember",
                             ambientDensity=4.0, ambientSpeed=1.5)]),
    )
    hp.add_nodes(
        Node("HellfireLash", None, (2, 0), kind="Spell", spell="HellfireLash"),
        Node("Horned", "horned", (4, 0), ranks=3, stats={"ArmorRating_Sharp": 0.04, "ArmorRating_Blunt": 0.04}),
        Node("Barbed", "barbed", (1, 1), kind="Augment", ranks=2, pre=["HellfireLash"], mods=[spellmod(["HellfireLash"], damage=0.2, range=0.15)]),
        Node("InfernalForm", None, (3, 1), kind="Spell", spell="InfernalForm", pre=["HellfireLash", "Horned"]),
        Node("Smoulder", "smoulder", (5, 1), ranks=3, pre=["Horned"], mods=[tagmod("Fire", damage=0.05)]),
        Node("Scorch", "scorch", (0, 2), ranks=3, pre=["Barbed"], stats={"AA_CritChance": 0.02}),
        Node("Hellhounds", None, (2, 2), kind="Spell", spell="Hellhounds", pre=["Barbed", "InfernalForm"]),
        Node("TrueShape", "true shape", (4, 2), kind="Augment", ranks=2, pre=["InfernalForm", "Smoulder"], mods=[spellmod(["InfernalForm"], duration=0.3, cooldown=-0.15)]),
        Node("Cinders", "cinders", (6, 2), ranks=3, pre=["Smoulder"], stats={"AA_ManaMax": 12}),
        Node("PackLeader", "pack leader", (1, 3), kind="Augment", ranks=2, pre=["Scorch", "Hellhounds"], mods=[spellmod(["Hellhounds"], extraTargets=1, duration=0.3)],
             desc="One more hellhound per rank."),
        Node("RainOfBrimstone", None, (3, 3), kind="Spell", spell="RainOfBrimstone", pre=["Hellhounds", "TrueShape"]),
        Node("Wrath", "wrath", (5, 3), kind="Notable", ranks=2, pre=["TrueShape", "Cinders"], passives=[P("P_LowHealthPower", threshold=0.5, bonusPerRank=0.25)],
             desc="Wounding a prince of hell only makes him angrier."),
        Node("Firestorm", "firestorm", (2, 4), kind="Augment", ranks=2, pre=["PackLeader", "RainOfBrimstone"], mods=[spellmod(["RainOfBrimstone"], extraTargets=1, damage=0.15)],
             desc="One more falling stone per rank."),
        Node("Hellgate", None, (4, 4), kind="Spell", spell="Hellgate", pre=["RainOfBrimstone", "Wrath"]),
        Node("Ashen", "ashen", (1, 5), ranks=3, pre=["Firestorm"], pct={"AA_SpellPower": 0.03}),
        Node("Infernal", "infernal", (3, 5), kind="Notable", ranks=2, pre=["Firestorm", "Hellgate"], passives=[P("P_Thorns", fraction=0.3, damageDef="AA_Hellfire", meleeOnly=True)],
             desc="Whoever strikes you in melee is burned by hellfire."),
        Node("GatesOfHell", "gates of hell", (5, 5), kind="Augment", ranks=2, pre=["Hellgate"], mods=[spellmod(["Hellgate"], duration=0.4, extraTargets=1)],
             desc="Open an extra gate per rank, and keep them open longer."),
        Node("Warlord", "warlord of hell", (2, 6), kind="Keystone", excl="HellprinceKeystone", pre=["Ashen", "Infernal"],
             mods=[spellmod(["InfernalForm"], duration=1.0, cooldown=-0.4)], stats={"MoveSpeed": 0.2},
             desc="You barely remember your mortal shape. Infernal Form lasts twice as long and returns much sooner."),
        Node("Pyrelord", "pyrelord", (4, 6), kind="Keystone", excl="HellprinceKeystone", pre=["Infernal", "GatesOfHell"],
             mods=[tagmod("Fire", damage=0.25)], passives=[P("P_KillExplosion", damageDef="AA_Hellfire", damage=25.0, radius=2.5, chance=0.5, spellKillsOnly=False)],
             desc="Everything you burn burns hotter, and the dead burst into hellfire."),
        Node("CrownOfAsh", None, (3, 7), kind="Ultimate", spell="CrownOfAsh", pre=["Warlord", "Pyrelord"]),
    )
    return ic, [pb, dl, hp], statuses()

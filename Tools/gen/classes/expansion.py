"""
Expansion spells: signature spells added to every class and subclass.

Each class module builds its own trees; build.py then calls extend() here, which adds the new spells, the skill tree
nodes that unlock them, and any status effects they need. Positions are free cells in each tree; prerequisites are
always above the node.
"""
from classes.common import *  # noqa: F401,F403
from classes import c_pyromancer as PY, c_tempest as TE, c_necromancer as NE, c_paladin as PA, c_chronomancer as CH
from classes import c_druid as DR, c_runesmith as RU, c_shadow as SH, c_bard as BA
from core_content import FIRE, FROST, NECRO, HOLY, SHADOW, NATURE, TIME, BLOOD  # noqa: F401


def spell_node(sid, pos, pre):
    return Node(sid, None, pos, kind="Spell", spell=sid, pre=pre)


def by_cid(root, subs):
    return {t.cid: t for t in [root] + subs}


# ------------------------------------------------------------------------------------------------
# Pyromancer
# ------------------------------------------------------------------------------------------------

def pyromancer(t, extra):
    t["Pyromancer"].add_spells(
        Spell("DragonsBreath", "dragon's breath", "Exhale a roaring cone of dragonfire. Everything in front of you burns.", icon=("flame", "wing"),
              mana=34, cd=14, range=8, warmup=0.6, tags=["Fire"], target="enemy_area", delivery="Cone", coneAngle=55.0, coneRange=8.5, ai_min=2, tier=2, shake=0.8,
              impact_vfx=[flash(2.0), burst("ember", 10, 3.5, 0.8, byRadius=True), smoke(3, 1.2)],
              effects=[dmg(22, "Flame", armorPen=0.25), hediff("AA_Burning", 1.5, stack=True, maxSeverity=5.0), ignite(0.5)]),
        Spell("Firewall", "wall of fire", "Draw a line of fire across the battlefield. It burns for half a minute and scorches anything that crosses it.",
              icon=("wall", "flame"), mana=40, cd=30, range=22, warmup=0.8, tags=["Fire"], role="Control", target="enemy_area", delivery="Line", lineLength=12.0,
              strikeRadius=1.0, lineStepDelay=1, tier=3, ai_min=2,
              impact_vfx=[burst("ember", 6, 2.0, 0.7), glow(1.4, 2.2, 30)],
              effects=[field(0.9, 1800, 60, [dmg(9, "Flame", affects="Enemies"), hediff("AA_Burning", 1.0, affects="Enemies")], groundTex="ArcanumAscendant/Vfx/Glow",
                             groundSpin=0.0, groundAlpha=0.65, ambientFleck="AA_Fleck_Ember", ambientDensity=2.0, ambientSpeed=1.5, ambientAngle=0.0)]),
    )
    t["Pyromancer"].add_nodes(
        spell_node("DragonsBreath", (4, 3), ["ScorchedEarth", "KindleHearth"]),
        Node("Wyrmfire", "wyrmfire", (4, 5), kind="Augment", ranks=2, pre=["DragonsBreath"], mods=[spellmod(["DragonsBreath"], damage=0.2, radius=0.15)]),
        spell_node("Firewall", (6, 5), ["EmberShield"]),
    )
    t["InfernoLord"].add_spells(
        Spell("Supernova", "supernova", "Become a dying star. A blinding blast of fire tears outward from you, burning even through armor. It costs you some of your own blood.",
              icon=("sun", "burst"), mana=120, cd=120, warmup=2.0, tags=["Fire"], target="self", delivery="Nova", radius=9.0, tier=4, health_cost=12, ai_min=4, shake=3.0,
              impact_vfx=[flash(9.0, 24, color="#fff4d0"), ring(0.05, 1.15, 50), burst("ember", 50, 6.0, 1.0, byRadius=True), smoke(10, 2.0), pillar(3.0, 40)],
              effects=[dmg(70, "Flame", armorPen=0.6, falloff=True), hediff("AA_Burning", 3.0, stack=True, maxSeverity=5.0), ignite(0.4), knock(3.0)]),
    )
    t["InfernoLord"].add_nodes(spell_node("Supernova", (1, 5), ["Annihilator"]))
    t["AshenPhoenix"].add_spells(
        Spell("Rekindle", "rekindle", "Phoenix fire burns away what is broken. Heals an ally, fades two scars and regrows a lost limb or organ with each casting.",
              icon=("phoenix", "heart"), mana=70, cd=HOUR * 4 / SEC, range=6, warmup=4.0, tags=["Fire", "Healing"], role="Heal", target="ally", ai_hp=0.0, autocast=False,
              impact_vfx=[pillar(1.6, 60), burst("feather", 18, 2.0, 0.6), burst("ember", 14, 2.5, 0.6), flash(3.0)],
              effects=[heal(40), restore(scars=2, regrowParts=1)]),
    )
    t["AshenPhoenix"].add_nodes(spell_node("Rekindle", (0, 2), ["HealingFlames"]))
    t["Forgeheart"].add_spells(
        Spell("Reforge", "reforge", "Pour soul-fire into a weapon or piece of gear and hammer it anew. Its quality rises by one step, up to legendary.",
              icon=("anvil", "star"), mana=90, cd=DAY / SEC, range=3, warmup=10.0, tags=["Fire", "Utility"], role="Utility", target="item", quirky=True,
              impact_vfx=[flash(3.0), burst("spark", 30, 4.0, 0.5), burst("ember", 12, 2.0, 0.6), pillar(1.0, 40)],
              effects=[improve_quality(1, "Legendary")]),
    )
    t["Forgeheart"].add_nodes(spell_node("Reforge", (6, 3), ["Bellows"]))


# ------------------------------------------------------------------------------------------------
# Tempest
# ------------------------------------------------------------------------------------------------

def tempest(t, extra):
    t["Tempest"].add_spells(
        Spell("FrostNova", "frost nova", "A ring of killing frost bursts from you, freezing everything close by.", icon=("snowflake", "ring"),
              mana=30, cd=18, warmup=0.3, tags=["Frost"], role="Control", target="self", delivery="Nova", radius=4.5, tier=2, ai_min=2, school=TE.FROST,
              impact_vfx=[flash(4.0, color="#e8fbff"), ring(0.1, 1.1, 30), burst("shard", 30, 4.0, 0.7, byRadius=True), burst("snow", 20, 1.5, 0.6, byRadius=True)],
              effects=[dmg(16, "AA_Frost"), hediff("AA_Frozen", 1.0, duration=240), hediff("AA_Chilled", 2.0, stack=True, maxSeverity=3.0)]),
        Spell("StormLance", "storm lance", "Hurl a lance of lightning that splits the sky: bolts strike every few steps along its path.", icon=("bolt", "arrow"),
              mana=42, cd=16, range=20, warmup=0.7, tags=["Storm"], target="enemy_area", delivery="Line", lineLength=18.0, strikeRadius=1.4, lineStepDelay=2, tier=3,
              ai_min=2, shake=1.2, impact_vfx=[bolt_down(1.4, 14), flash(2.5), burst("spark", 10, 4.0, 0.4)],
              effects=[dmg(26, "AA_Lightning", armorPen=0.4, vsMechanoids=1.5), hediff("AA_Shocked", 1.0, stack=True, maxSeverity=3.0)]),
    )
    t["Tempest"].add_nodes(
        spell_node("FrostNova", (6, 3), ["FrostArmor"]),
        spell_node("StormLance", (0, 5), ["Lightningrod"]),
    )
    t["Stormcaller"].add_spells(
        Spell("IonStorm", "ion storm", "Call down a storm of ionised air. For twenty seconds it fries electronics, collapses shields and locks up mechanoids.",
              icon=("cloud", "bolt"), mana=110, cd=90, range=30, warmup=1.5, tags=["Storm"], role="Control", target="enemy_area", tier=4, ai_min=2,
              impact_vfx=[flash(5.0), ring(0.1, 1.1, 40), bolt_down(2.0, 18)],
              effects=[shieldbreak(mechStun=300),
                       field(6.5, 1200, 60, [dmg(10, "EMP", affects="Enemies"), dmg(8, "AA_Lightning", vsMechanoids=2.5, affects="Enemies"), shieldbreak(mechStun=90, affects="Enemies")],
                             groundTex="ArcanumAscendant/Vfx/RuneCircle", groundSpin=3.0, groundAlpha=0.5, ambientFleck="AA_Fleck_Bolt", ambientDensity=3.0, ambientSpeed=2.0)]),
    )
    t["Stormcaller"].add_nodes(spell_node("IonStorm", (6, 4), ["GridLink"]))
    t["Frostwarden"].add_spells(
        Spell("GlacialTomb", "glacial tomb", "Encase a target in a tomb of ice ringed by frozen walls. It can do nothing for ten seconds, and the ice makes it brittle.",
              icon=("snowflake", "wall"), mana=55, cd=40, range=24, warmup=0.8, tags=["Frost"], role="Control", target="enemy", radius=2.2, tier=3,
              impact_vfx=[flash(3.0, color="#e8fbff"), burst("shard", 24, 3.0, 0.7), ring(0.2, 0.8, 26, byRadius=False)],
              effects=[stun(600, radius=0.0), hediff("AA_Frozen", 1.0, duration=600, radius=0.0), spawn("AA_Construct_IceWall", ring=True, lifetimeTicks=900)]),
    )
    t["Frostwarden"].add_nodes(spell_node("GlacialTomb", (6, 3), ["Glacier"]))
    t["Skybreaker"].add_spells(
        Spell("Starfall", "starfall", "Reach into the sky and pull down a falling star. It crashes nearby as a meteorite packed with ore.",
              icon=("meteor", "star"), mana=90, cd=DAY * 2 / SEC, range=40, warmup=6.0, tags=["Storm", "Utility"], role="Utility", target="cell", quirky=True, los=False,
              impact_vfx=[ring(0.3, 1.0, 40, byRadius=False), burst("star", 20, 3.0, 0.6)],
              effects=[meteorite()]),
    )
    t["Skybreaker"].add_nodes(spell_node("Starfall", (6, 3), ["Zephyr"]))


# ------------------------------------------------------------------------------------------------
# Necromancer
# ------------------------------------------------------------------------------------------------

def necromancer(t, extra):
    t["Necromancer"].add_spells(
        Spell("CorpseFeast", "corpse feast", "Draw the last life out of up to six corpses nearby. Each one heals you and refills your mana, then crumbles away.",
              icon=("skull", "drop"), mana=10, cd=30, range=20, warmup=1.0, tags=["Necromancy"], role="Utility", target="cell", radius=5.0,
              impact_vfx=[ring(0.3, 1.0, 30), spiral("smoke", 14, -2.0, 0.8), burst("skull", 6, 1.5, 0.6, byRadius=True)],
              effects=[devour(25, 20, 6)]),
        Spell("GraveMark", "mark of the grave", "Brand an enemy for death. Their wounds fester, and if they die while marked they rise as your undead servant.",
              icon=("skull", "rune"), mana=26, cd=12, range=28, warmup=0.6, tags=["Necromancy"], role="Control", target="enemy", tier=2,
              impact_vfx=NE.NECRO_HIT + [rune(1.0, 40, byRadius=False)],
              effects=[hediff("AA_GraveMark", 1.0), dmg(12, "AA_Necrotic")]),
    )
    t["Necromancer"].add_nodes(
        spell_node("CorpseFeast", (2, 3), ["BoneArmor"]),
        spell_node("GraveMark", (0, 5), ["Withering"]),
    )
    extra.append(status("AA_GraveMark", "mark of the grave", "Marked for death by a necromancer. Wounds fester, and if the bearer dies they will rise again as the necromancer's servant.",
        NECRO, bad=True, ticks=1800, comps=[raise_on_death(MIN * 2), aura(NECRO, "ArcanumAscendant/Vfx/Hex", 1.4, "AA_Fleck_Skull", 45)],
        stages=[stage(factors={"InjuryHealingFactor": 0.3, "IncomingDamageFactor": 1.1})]))
    t["Lich"].add_spells(
        Spell("SoulReap", "soul reap", "Tear a sliver of soul from every enemy around you. You are healed for part of the damage and your mana surges.",
              icon=("skull", "swirl"), mana=60, cd=40, warmup=0.8, tags=["Necromancy"], target="self", delivery="Nova", radius=6.5, tier=3, ai_min=3,
              impact_vfx=[ring(1.1, 0.1, 34), spiral("skull", 16, -3.0, 0.6), flash(3.0, color="#c8e8ff")],
              effects=[dmg(26, "AA_Necrotic", armorPen=0.5, healCasterFraction=0.3), hediff("AA_Weakened", 1.0)],
              caster_effects=[mana(percent=0.15)]),
    )
    t["Lich"].add_nodes(spell_node("SoulReap", (6, 2), ["SoulRend"]))
    t["Bonelord"].add_spells(
        Spell("BoneStorm", "bone storm", "A whirlwind of razor-edged bone spins around you for twelve seconds, shredding every enemy that comes close.",
              icon=("bone", "tornado"), mana=50, cd=35, warmup=0.5, tags=["Necromancy"], role="Buff", target="self", tier=3, affects="Self",
              impact_vfx=[burst("bone", 24, 3.0, 0.7), ring(0.2, 1.0, 24, byRadius=False)],
              effects=[field(3.5, 720, 30, [dmg(8, "Cut", armorPen=0.3, affects="Enemies")], followCaster=True, groundTex="ArcanumAscendant/Vfx/Vortex", groundSpin=-8.0,
                             groundAlpha=0.5, ambientFleck="AA_Fleck_Bone", ambientDensity=3.0, ambientSpeed=3.0)]),
    )
    t["Bonelord"].add_nodes(spell_node("BoneStorm", (0, 3), ["Impaler"]))
    t["Plaguebringer"].add_spells(
        Spell("PlagueRats", "plague rats", "A swarm of plague-ridden rats boils out of the ground. Their bites carry the black plague.", icon=("paw", "skull"),
              mana=40, cd=45, range=20, warmup=1.0, tags=["Necromancy", "Plague"], role="Summon", target="cell",
              impact_vfx=[smoke(5, 1.2), burst("smoke", 12, 1.5, 0.8), ring(0.3, 0.9, 24, byRadius=False)],
              effects=[summon("Rat", 6, MIN)]),
    )
    t["Plaguebringer"].add_nodes(spell_node("PlagueRats", (6, 2), ["Contagion"]))


# ------------------------------------------------------------------------------------------------
# Paladin
# ------------------------------------------------------------------------------------------------

def paladin(t, extra):
    t["Paladin"].add_spells(
        Spell("DivineIntervention", "divine intervention", "The heavens reach down and hold an ally. For eight seconds nothing can harm them.",
              icon=("hand", "sun"), mana=60, cd=120, range=24, warmup=0.0, tags=["Holy"], role="Buff", target="ally", tier=3, ai_hp=0.3,
              impact_vfx=[pillar(1.8, 50), flash(3.5, color="#fff8d0"), burst("feather", 16, 2.0, 0.6)],
              effects=[hediff("AA_DivineIntervention", 1.0)]),
    )
    t["Paladin"].add_spells(
        Spell("HeavensLance", "heaven's lance", "Call a procession of holy pillars that march out from you in a line, searing and judging everything in their path.",
              icon=("pillar", "sun"), mana=48, cd=20, range=20, warmup=0.8, tags=["Holy"], target="enemy_area", delivery="Line", lineLength=16.0, strikeRadius=1.3,
              lineStepDelay=3, tier=3, ai_min=2, shake=1.0, impact_vfx=PA.SMITE,
              effects=[dmg(28, "AA_Holy", armorPen=0.4), hediff("AA_Marked", 1.0)]),
    )
    t["Paladin"].add_nodes(
        spell_node("DivineIntervention", (4, 5), ["Radiance"]),
        spell_node("HeavensLance", (0, 5), ["Steadfast"]),
    )
    extra.append(status("AA_DivineIntervention", "divine intervention", "Held in the hand of the divine. Immune to all harm.", HOLY, bad=False, ticks=480,
        comps=[shell(0.0, color=HOLY), aura(HOLY, "ArcanumAscendant/Vfx/Shield", 2.0, "AA_Fleck_Feather", 20)]))
    t["Templar"].add_spells(
        Spell("UnbreakableVow", "unbreakable vow", "Swear to stand your ground. For fifteen seconds you take far less damage and a third of every blow is turned back on the attacker.",
              icon=("shield", "crown"), mana=45, cd=60, warmup=0.0, tags=["Holy"], role="Buff", target="self", tier=3,
              impact_vfx=[hexshield(2.6, 50), flash(2.5), burst("star", 12, 2.0, 0.6)],
              effects=[hediff("AA_UnbreakableVow", 1.0)]),
    )
    t["Templar"].add_nodes(spell_node("UnbreakableVow", (6, 3), ["Bastion"]))
    extra.append(status("AA_UnbreakableVow", "unbreakable vow", "Sworn not to fall. Takes 60% less damage and reflects a third of every blow.", HOLY, bad=False, ticks=900,
        comps=[shell(0.4, 0.33, color="#ffcf5a", reflectDef="AA_Holy"), aura("#ffcf5a", "ArcanumAscendant/Vfx/Shield", 1.8)]))
    t["Cleric"].add_spells(
        Spell("GreaterRestoration", "greater restoration", "A long, exhausting prayer that makes the body whole: every chronic illness cured, every scar erased, every lost limb and organ regrown, every addiction broken.",
              icon=("cross", "star"), mana=140, cd=DAY / SEC, range=3, warmup=12.0, tags=["Holy", "Healing"], role="Heal", target="ally", tier=4, ai_hp=0.0, autocast=False,
              impact_vfx=[pillar(2.2, 90), flash(4.0, color="#ffffff"), burst("feather", 30, 2.5, 0.7), burst("star", 20, 2.0, 0.5)],
              effects=[heal(60), restore(chronic=-1, scars=-1, regrowParts=-1, addictions=True), cleanse()]),
    )
    t["Cleric"].add_nodes(spell_node("GreaterRestoration", (0, 4), ["Triage"]))
    t["Inquisitor"].add_spells(
        Spell("MirrorOfJudgment", "mirror of judgment", "For ten seconds, every blow struck against you is judged: you take half of it, and the attacker takes all of it.",
              icon=("eye", "shield"), mana=50, cd=60, warmup=0.0, tags=["Holy"], role="Buff", target="self", tier=3,
              impact_vfx=[hexshield(2.4, 40), flash(2.5, color="#ffd0a0"), burst("star", 10, 2.0, 0.6)],
              effects=[hediff("AA_MirrorOfJudgment", 1.0)]),
    )
    t["Inquisitor"].add_nodes(spell_node("MirrorOfJudgment", (6, 3), ["Gag"]))
    extra.append(status("AA_MirrorOfJudgment", "mirror of judgment", "Every attack is judged. Takes half damage and reflects the full blow at the attacker.", "#ff9a3c", bad=False,
        ticks=600, comps=[shell(0.5, 1.0, color="#ff9a3c", reflectDef="AA_Holy"), aura("#ff9a3c", "ArcanumAscendant/Vfx/Hex", 1.8)]))


# ------------------------------------------------------------------------------------------------
# Chronomancer
# ------------------------------------------------------------------------------------------------

def chronomancer(t, extra):
    t["Chronomancer"].add_spells(
        Spell("Rejuvenate", "rejuvenate", "Unwind an ally's personal time. Each casting takes three years off their body, cures one age-related condition (bad back, cataracts, "
              "frailty, dementia and the like) and fades one scar. Keep casting to restore them fully.", icon=("hourglass", "heart"),
              mana=60, cd=HOUR * 6 / SEC, range=3, warmup=6.0, tags=["Time", "Healing"], role="Utility", target="ally", quirky=True,
              impact_vfx=[rune(1.4, 90, byRadius=False, tex="ArcanumAscendant/Vfx/Clock"), spiral("clock", 18, -2.5, 0.6), pillar(1.2, 50)],
              effects=[agesh(-3, cureConditions=1, healScars=1)]),
        Spell("Senescence", "senescence", "Force years of age onto a target in an instant. Each casting adds about eight years, wracks the body with time-ravaged weakness, "
              "and may bring on the ailments of old age. Push someone far enough past their natural lifespan and they crumble to dust.", icon=("hourglass", "skull"),
              mana=34, cd=15, range=24, warmup=0.8, tags=["Time"], target="enemy", tier=2,
              impact_vfx=CH.TIME_HIT + [smoke(3, 1.0), spiral("clock", 10, 2.0, 0.5)],
              effects=[agesh(8, ailmentChance=0.5, lethalAgeFactor=1.6), hediff("AA_TimeRavaged", 1.0, stack=True, maxSeverity=5.0), dmg(10, "AA_Temporal")]),
        Spell("ReverseEntropy", "reverse entropy", "Run time backwards for everything in an area: damaged items are repaired, spoiled food is fresh again, "
              "and clothes taken from the dead lose their taint. Allies' worn gear is restored too.", icon=("clock", "anvil"),
              mana=45, cd=HOUR * 2 / SEC, range=20, warmup=3.0, tags=["Time", "Utility"], role="Utility", target="cell", radius=4.0, quirky=True, affects="Allies",
              impact_vfx=[ring(1.0, 0.1, 40), spiral("clock", 20, -3.0, 0.6, ), rune(1.0, 60, tex="ArcanumAscendant/Vfx/Clock")],
              effects=[restore_items(1.0)]),
    )
    t["Chronomancer"].add_nodes(
        spell_node("Rejuvenate", (2, 3), ["RewindWounds"]),
        spell_node("Senescence", (4, 3), ["Viscosity"]),
        Node("Centenarian", "centenarian", (6, 5), kind="Augment", ranks=2, pre=["LongNow"], mods=[spellmod(["Senescence", "Rejuvenate"], cooldown=-0.2, healing=0.25, damage=0.25)],
             desc="Senescence ages harder and Rejuvenate restores more years; both return sooner."),
        spell_node("ReverseEntropy", (0, 5), ["Paradigm"]),
    )
    extra.append(status("AA_TimeRavaged", "time-ravaged", "Decades forced onto the body in moments. Muscles waste, joints seize and the mind fogs. Stacks with each casting.",
        "#a8a090", bad=True, ticks=DAY, maxSev=5,
        comps=[aura("#a8a090", "ArcanumAscendant/Vfx/Clock", 1.4, "AA_Fleck_Clock", 50)],
        stages=[stage(label="aged", factors={"MoveSpeed": 0.9, "MeleeDamageFactor": 0.9, "WorkSpeedGlobal": 0.9}, caps={"Manipulation": -0.05}),
                stage(label="withered", min_sev=2.0, factors={"MoveSpeed": 0.8, "MeleeDamageFactor": 0.75, "WorkSpeedGlobal": 0.8, "ImmunityGainSpeed": 0.8},
                      caps={"Manipulation": -0.12, "Consciousness": -0.05}),
                stage(label="decrepit", min_sev=3.0, factors={"MoveSpeed": 0.65, "MeleeDamageFactor": 0.6, "WorkSpeedGlobal": 0.65, "ImmunityGainSpeed": 0.6},
                      caps={"Manipulation": -0.2, "Consciousness": -0.1, "Sight": -0.1}),
                stage(label="ancient", min_sev=4.0, factors={"MoveSpeed": 0.5, "MeleeDamageFactor": 0.45, "WorkSpeedGlobal": 0.5, "ImmunityGainSpeed": 0.4, "IncomingDamageFactor": 1.15},
                      caps={"Manipulation": -0.3, "Consciousness": -0.15, "Sight": -0.2, "Hearing": -0.2}),
                stage(label="dust-dry", min_sev=5.0, factors={"MoveSpeed": 0.35, "MeleeDamageFactor": 0.3, "WorkSpeedGlobal": 0.35, "ImmunityGainSpeed": 0.25, "IncomingDamageFactor": 1.3},
                      caps={"Manipulation": -0.4, "Consciousness": -0.2, "Sight": -0.3, "Hearing": -0.3, "Breathing": -0.15})]))

    tw = t["Timeweaver"]
    for s in tw.spells:
        if s.sid == "AgesOfDust":
            s.desc = ("Age everything in an area by years in a heartbeat. Enemies take damage that ignores armor, wither, suffer the ailments of old age, and "
                      "the very old crumble to dust.")
            s.target = "enemy_area"
            s.radius = 3.0
            s.ai_min = 2
            s.effects = [agesh(6, ailmentChance=0.35, lethalAgeFactor=1.8), dmg(30, "AA_Temporal"), hediff("AA_TimeRavaged", 1.0, stack=True, maxSeverity=5.0),
                         hediff("AA_Withering", 1.0)]
            s.impact_vfx = CH.TIME_HIT + [smoke(5, 1.4), ring(0.2, 1.0, 30), spiral("clock", 14, 2.5, 0.6)]
        elif s.sid == "FountainOfYouth":
            s.desc = ("Open a fountain of youth around an ally. Everyone touched loses two years, sheds an age-related condition and a scar, and regrows a lost limb "
                      "or organ. Repeated castings can restore even the oldest body.")
            s.target = "ally_area"
            s.radius = 3.0
            s.affects = "Allies"
            s.cd = HOUR * 8 / SEC
            s.effects = [agesh(-2, cureConditions=1, healScars=1, regrowParts=1)]
            s.impact_vfx = [pillar(1.5, 60), spiral("clock", 24, -2.0, 0.6), ring(0.2, 1.0, 40), burst("glow", 16, 1.5, 0.6, byRadius=True)]
    t["Voidwalker"].add_spells(
        Spell("Transposition", "transposition", "Fold space so that you and a target trade places instantly. Pull an ally out of danger, or drop an enemy into it.",
              icon=("portal", "chain"), mana=24, cd=15, range=30, warmup=0.0, tags=["Space", "Mobility"], role="Mobility", target="any_pawn", los=False,
              impact_vfx=CH.VOID_HIT, effects=[teleport("Swap")]),
    )
    t["Voidwalker"].add_nodes(spell_node("Transposition", (0, 2), ["GravityWell"]))
    t["Paradox"].add_spells(
        Spell("FateAnchor", "fate anchor", "Anchor an ally's fate to this moment. If they die within the next four hours, time snaps back and they live, whole and unhurt. Works once.",
              icon=("chain", "heart"), mana=90, cd=DAY / SEC, range=20, warmup=2.0, tags=["Time"], role="Buff", target="ally", tier=4, autocast=False,
              impact_vfx=[rune(1.2, 80, byRadius=False, tex="ArcanumAscendant/Vfx/Clock"), spiral("clock", 16, -2.5, 0.6), flash(3.0)],
              effects=[hediff("AA_FateAnchor", 1.0)]),
    )
    t["Paradox"].add_nodes(spell_node("FateAnchor", (6, 2), ["SplitTimeline"]))
    extra.append(status("AA_FateAnchor", "fate anchor", "This person's fate is anchored. If they die while it lasts, time snaps back and they live.", "#ff7ad9", bad=False,
        ticks=HOUR * 4, comps=[death_ward("#ff7ad9", "AA_FateDenied"), aura("#ff7ad9", "ArcanumAscendant/Vfx/Clock", 1.3)]))


# ------------------------------------------------------------------------------------------------
# Druid
# ------------------------------------------------------------------------------------------------

def druid(t, extra):
    t["Druid"].add_spells(
        Spell("Earthquake", "earthquake", "Stamp, and the earth answers. The ground heaves around you, knocking enemies flat and battering them with stone.",
              icon=("rock", "burst"), mana=60, cd=40, warmup=1.0, tags=["Nature"], role="Control", target="self", delivery="Nova", radius=7.0, tier=3, ai_min=3, shake=3.0,
              impact_vfx=[ring(0.1, 1.1, 40), burst("leaf", 10, 2.0, 0.6, byRadius=True), smoke(10, 2.0), flash(3.0, color="#c8a878")],
              effects=[dmg(20, "Blunt", falloff=True), stun(150), knock(2.0)]),
        Spell("NaturesRenewal", "nature's renewal", "Pour the patient strength of the forest into an ally: heals, cures an age-related or chronic condition and fades two scars with each casting.",
              icon=("leaf", "heart"), mana=50, cd=HOUR * 3 / SEC, range=6, warmup=4.0, tags=["Nature", "Healing"], role="Heal", target="ally", ai_hp=0.0, autocast=False,
              impact_vfx=[pillar(1.2, 50), burst("leaf", 20, 1.5, 0.6), burst("petal", 12, 1.5, 0.5)],
              effects=[heal(30), restore(chronic=1, scars=2), cleanse()]),
    )
    t["Druid"].add_nodes(
        spell_node("Earthquake", (2, 5), ["Thornmail"]),
        spell_node("NaturesRenewal", (6, 3), ["Lifebloom"]),
    )
    t["Beastmaster"].add_spells(
        Spell("AlphasCall", "alpha's call", "Let out the call of the pack leader. Every animal in a wide area that isn't too wild comes to your side for good.",
              icon=("paw", "crown"), mana=100, cd=DAY / SEC, warmup=3.0, tags=["Nature", "Beast"], role="Utility", target="self", delivery="Nova", radius=12.0, quirky=True,
              impact_vfx=[ring(0.05, 1.0, 50), burst("leaf", 20, 2.5, 0.6, byRadius=True), flash(3.0)],
              effects=[recruit(animalsOnly=True, maxWildness=0.85, affects="AllButSelf")]),
    )
    t["Beastmaster"].add_nodes(spell_node("AlphasCall", (6, 2), ["FeralCommand"]))
    t["VerdantWarden"].add_spells(
        Spell("VerdantSurge", "verdant surge", "A wave of green life rolls over your allies, healing them, purging poisons and fading a scar from each.",
              icon=("tree", "heart"), mana=55, cd=60, range=20, warmup=1.5, tags=["Nature", "Healing"], role="Heal", target="ally_area", radius=4.5, ai_hp=0.6,
              impact_vfx=[ring(0.2, 1.0, 34), burst("leaf", 24, 2.0, 0.6, byRadius=True), burst("petal", 12, 1.5, 0.5, byRadius=True)],
              effects=[heal(30), cleanse(), restore(scars=1)]),
    )
    t["VerdantWarden"].add_nodes(spell_node("VerdantSurge", (6, 2), ["SporeCloud"]))
    t["Shapeshifter"].add_spells(
        Spell("Pounce", "pounce", "Leap like a great cat onto a distant enemy, mauling them and knocking them off their feet.", icon=("claw", "wing"),
              mana=22, cd=10, range=14, warmup=0.2, tags=["Nature", "Beast"], target="enemy", tier=2,
              impact_vfx=[flash(2.0), burst("blood", 10, 3.0, 0.5), smoke(2, 0.8)],
              effects=[teleport("CasterToTarget"), dmg(26, "Scratch", armorPen=0.3), stun(90), knock(2.0)]),
    )
    t["Shapeshifter"].add_nodes(spell_node("Pounce", (0, 2), ["Maul"]))


# ------------------------------------------------------------------------------------------------
# Runesmith
# ------------------------------------------------------------------------------------------------

def runesmith(t, extra):
    t["Runesmith"].add_spells(
        Spell("MidasTouch", "midas touch", "Inscribe a rune of gold on a creature. If it is weakened or small, it turns to solid gold where it stands. "
              "The mighty are only gilded, slowed and burned.", icon=("hand", "coin"),
              mana=55, cd=60, range=6, warmup=1.5, tags=["Rune"], target="enemy", tier=3, quirky=True,
              impact_vfx=[flash(3.0, color="#ffe080"), burst("spark", 24, 3.0, 0.5), glow(1.6, 2.6, 30, color="#ffd040")],
              effects=[midas(0.5, 40.0, 30.0)]),
    )
    t["Runesmith"].add_spells(
        Spell("RuneCannonade", "rune cannonade", "Inscribe a firing solution in the air. A battery of runic shells rains down on the target area, each one exploding and shredding armor.",
              icon=("rune", "meteor"), mana=65, cd=40, range=34, warmup=1.2, tags=["Rune"], target="enemy_area", delivery="Barrage", barrageCount=10, barrageInterval=6,
              barrageRadius=4.5, strikeRadius=1.8, strikeStyle="Meteor", tier=3, ai_min=2, shake=0.6, projectileScale=0.6,
              impact_vfx=[flash(2.0), burst("rune", 4, 2.0, 0.6), burst("spark", 10, 4.0, 0.4), smoke(2, 0.8)],
              effects=[dmg(20, "AA_Rune", armorPen=0.5, damageBuildings=True, buildingFactor=0.5), hediff("AA_ArmorShred", 1.0, stack=True, maxSeverity=4.0)]),
    )
    t["Runesmith"].add_nodes(
        spell_node("MidasTouch", (6, 3), ["Battery"]),
        spell_node("RuneCannonade", (0, 5), ["Runeblade"]),
    )
    extra.append(status("AA_Gilded", "gilded", "Partly turned to gold. Heavy, stiff and slow.", "#ffd040", bad=True, ticks=600,
        comps=[aura("#ffd040", "ArcanumAscendant/Vfx/RuneCircle", 1.3, "AA_Fleck_Spark", 30)],
        stages=[stage(factors={"MoveSpeed": 0.5, "MeleeCooldownFactor": 1.4, "AimingDelayFactor": 1.4})]))
    t["Golemancer"].add_spells(
        Spell("OverdriveProtocol", "overdrive protocol", "Broadcast an overdrive glyph. Every ally nearby moves and fights faster and is patched up.", icon=("gear", "bolt"),
              mana=50, cd=60, warmup=0.5, tags=["Rune"], role="Buff", target="self", delivery="Nova", radius=8.0, affects="Allies", tier=3,
              impact_vfx=[ring(0.1, 1.0, 36), burst("gear", 20, 3.0, 0.6, byRadius=True), flash(2.5)],
              effects=[hediff("AA_Haste", 1.0, duration=900), heal(15)]),
    )
    t["Golemancer"].add_nodes(spell_node("OverdriveProtocol", (6, 2), ["ArcPylon"]))
    t["Spellforger"].add_spells(
        Spell("MasterworkRune", "masterwork rune", "Carve a rune of perfection into an item or the weapon a pawn is holding. Its quality rises by one step, up to legendary.",
              icon=("rune", "star"), mana=80, cd=DAY / SEC, range=3, warmup=8.0, tags=["Rune", "Utility"], role="Utility", target="item", quirky=True,
              impact_vfx=[flash(3.0), burst("rune", 16, 2.0, 0.6), pillar(1.0, 40)],
              effects=[improve_quality(1, "Legendary")]),
    )
    t["Spellforger"].add_nodes(spell_node("MasterworkRune", (0, 2), ["Disenchant"]))
    t["Gunslinger"].add_spells(
        Spell("FanTheHammer", "fan the hammer", "Six runic shots in the blink of an eye, spread across the nearest enemies.", icon=("gun", "burst"),
              mana=30, cd=12, range=26, warmup=0.3, tags=["Rune", "Gun"], delivery="Projectile", projectileCount=6, projectileSpeed=60.0, projectileScale=0.5,
              multiTargetRadius=6.0, ai_min=1, impact_vfx=RU.RUNE_HIT,
              effects=[dmg(13, "Bullet", armorPen=0.35)]),
    )
    t["Gunslinger"].add_nodes(spell_node("FanTheHammer", (6, 2), ["Deadeye"]))


# ------------------------------------------------------------------------------------------------
# Shadowmancer
# ------------------------------------------------------------------------------------------------

def shadow(t, extra):
    t["Shadowmancer"].add_spells(
        Spell("Eclipse", "eclipse", "Swallow the sun. Darkness falls across the map for six hours, and every enemy near you is blinded by the sudden night.",
              icon=("moon", "sun"), mana=80, cd=DAY / SEC, warmup=3.0, tags=["Shadow"], role="Utility", target="self", delivery="Nova", radius=12.0, quirky=True,
              impact_vfx=[flash(6.0, color="#3a2a6a"), ring(0.05, 1.0, 50), spiral("mote", 30, 3.0, 0.7)],
              effects=[condition("Eclipse", HOUR * 6), hediff("AA_Blinded", 1.0, duration=600, affects="Enemies")]),
        Spell("ShadowBind", "shadow bind", "Shadows lash out from the ground and drag every enemy in the area toward the center, holding them in place.",
              icon=("chain", "moon"), mana=32, cd=18, range=26, warmup=0.6, tags=["Shadow"], role="Control", target="enemy_area", radius=4.0, ai_min=2, tier=2,
              impact_vfx=SH.SHADOW_HIT + [ring(1.0, 0.1, 26)],
              effects=[knock(3.0, pull=True), stun(150), hediff("AA_Entangled", 1.0, duration=300)]),
    )
    t["Shadowmancer"].add_nodes(
        spell_node("Eclipse", (6, 3), ["Phantom"]),
        spell_node("ShadowBind", (0, 3), ["Malice"]),
    )
    t["Assassin"].add_spells(
        Spell("ShadowDance", "shadow dance", "Flicker from enemy to enemy, appearing behind each one for a single killing cut.", icon=("dagger", "portal"),
              mana=40, cd=24, range=20, warmup=0.2, tags=["Shadow"], target="enemy", delivery="Chain", chainCount=5, chainRange=8.0, chainFalloff=0.95, chainDelay=10, tier=3,
              ai_min=2, impact_vfx=[flash(1.6, color="#c0a8ff"), burst("blood", 8, 3.0, 0.5), spiral("mote", 6, 2.0, 0.4)],
              effects=[teleport("CasterToTarget"), dmg(24, "Cut", armorPen=0.45)]),
    )
    t["Assassin"].add_nodes(spell_node("ShadowDance", (0, 2), ["ToxicFan"]))
    t["Hexer"].add_spells(
        Spell("StoneGaze", "stone gaze", "Meet their eyes with the gaze of the gorgon. They turn to stone for ten seconds: helpless, but hard to hurt.", icon=("eye", "rock"),
              mana=40, cd=30, range=24, warmup=0.6, tags=["Shadow", "Hex"], role="Control", target="enemy", tier=3,
              impact_vfx=[flash(2.5, color="#d8d0c0"), smoke(4, 1.0), burst("shard", 12, 2.0, 0.6)],
              effects=[stun(600), hediff("AA_Petrified", 1.0)]),
    )
    t["Hexer"].add_nodes(spell_node("StoneGaze", (6, 2), ["Polymorph"]))
    extra.append(status("AA_Petrified", "petrified", "Turned to stone. Cannot move, but stone is hard to break.", "#b8b0a0", bad=True, ticks=600,
        comps=[aura("#b8b0a0", "ArcanumAscendant/Vfx/RuneCircle", 1.4)],
        stages=[stage(factors={"MoveSpeed": 0.05, "IncomingDamageFactor": 0.5})]))
    t["Hemomancer"].add_spells(
        Spell("BloodHarvest", "blood harvest", "Pull the blood out of every enemy in an area through their skin. You drink a third of what you take.", icon=("drop", "hand"),
              mana=45, cd=20, range=24, warmup=0.7, tags=["Blood"], target="enemy_area", radius=3.5, tier=3, ai_min=2, health_cost=4,
              impact_vfx=SH.BLOOD_HIT + [ring(1.0, 0.1, 26), spiral("blood", 14, -2.5, 0.5)],
              effects=[dmg(24, "AA_Blood", armorPen=0.4, healCasterFraction=0.33), hediff("AA_Bleeding", 1.0, stack=True, maxSeverity=5.0)]),
    )
    t["Hemomancer"].add_nodes(spell_node("BloodHarvest", (6, 2), ["BloodFrenzy"]))


# ------------------------------------------------------------------------------------------------
# Spellsinger
# ------------------------------------------------------------------------------------------------

def bard(t, extra):
    t["Bard"].add_spells(
        Spell("SirensCall", "siren's call", "Sing a note no one can resist. Enemies in the area stumble toward its center, entranced, and stand there listening.",
              icon=("note", "wave"), mana=34, cd=22, range=26, warmup=0.8, tags=["Song"], role="Control", target="enemy_area", radius=5.0, tier=2, ai_min=2,
              impact_vfx=BA.SONG_NOVA, effects=[knock(3.5, pull=True), stun(180), hediff("AA_Concussed", 1.0)]),
    )
    t["Bard"].add_spells(
        Spell("StandingOvation", "standing ovation", "Give the performance of a lifetime. Every colonist who hears it is struck by inspiration.",
              icon=("note", "star"), mana=90, cd=DAY * 2 / SEC, warmup=8.0, tags=["Song", "Utility"], role="Utility", target="self", delivery="Nova", radius=14.0,
              affects="AlliesNotSelf", quirky=True,
              impact_vfx=[ring(0.05, 1.0, 50), burst("note", 30, 2.5, 0.7, byRadius=True), burst("star", 20, 2.0, 0.5, byRadius=True), flash(3.0)],
              effects=[inspire(chance=0.8)]),
    )
    t["Bard"].add_nodes(
        spell_node("SirensCall", (6, 3), ["Soothing"]),
        spell_node("StandingOvation", (0, 5), ["Earworm"]),
    )
    t["Skald"].add_spells(
        Spell("SongOfUndying", "song of the undying", "A saga so stirring that death itself waits for the last verse. For thirty seconds, allies around you who fall are pulled back to life.",
              icon=("note", "crown"), mana=110, cd=300, warmup=1.0, tags=["Song"], role="Buff", target="self", delivery="Nova", radius=8.0, affects="Allies", tier=4,
              impact_vfx=[ring(0.05, 1.0, 50), burst("note", 24, 2.5, 0.7, byRadius=True), pillar(2.0, 50), flash(3.0)],
              effects=[hediff("AA_UndyingSong", 1.0)]),
    )
    t["Skald"].add_nodes(spell_node("SongOfUndying", (6, 2), ["RallyingCry"]))
    extra.append(status("AA_UndyingSong", "song of the undying", "Death waits for the song to end. If this person dies while it plays, they rise again.", "#ff8a4a", bad=False,
        ticks=1800, comps=[death_ward("#ff8a4a", "AA_NotYet"), aura("#ff8a4a", "ArcanumAscendant/Vfx/RuneCircle", 1.3, "AA_Fleck_Note", 40)]))
    t["Illusionist"].add_spells(
        Spell("Plagiarize", "plagiarize", "Reach into someone's mind and take what they know. A large chunk of their best skill's experience becomes yours.",
              icon=("mask", "book"), mana=60, cd=DAY / SEC, range=12, warmup=3.0, tags=["Song", "Mind"], role="Utility", target="enemy", quirky=True,
              impact_vfx=[spiral("note", 14, -2.5, 0.6), flash(2.0)], effects=[steal_skill(4000)]),
    )
    t["Illusionist"].add_nodes(spell_node("Plagiarize", (6, 2), ["Mirage"]))
    t["Jester"].add_spells(
        Spell("BananaPeel", "banana peels", "Scatter conjured banana peels across an area. For fifteen seconds, enemies who walk through keep slipping over.",
              icon=("pie", "wave"), mana=26, cd=25, range=24, warmup=0.4, tags=["Song", "Chaos"], role="Control", target="enemy_area", quirky=True, ai_min=2,
              impact_vfx=[burst("star", 14, 2.0, 0.5, byRadius=True), ring(0.3, 1.0, 24)],
              effects=[field(3.5, 900, 45, [stun(50, chance=0.4, affects="Enemies")], groundTex="ArcanumAscendant/Vfx/RuneCircle", groundSpin=1.0, groundAlpha=0.35,
                             ambientFleck="AA_Fleck_Star", ambientDensity=1.0, ambientSpeed=0.5)]),
    )
    t["Jester"].add_nodes(spell_node("BananaPeel", (6, 2), ["LuckyCoin"]))


EXTENDERS = {
    "c_pyromancer": pyromancer, "c_tempest": tempest, "c_necromancer": necromancer, "c_paladin": paladin, "c_chronomancer": chronomancer,
    "c_druid": druid, "c_runesmith": runesmith, "c_shadow": shadow, "c_bard": bard,
}


def extend(modname, root, subs, extra):
    fn = EXTENDERS.get(modname)
    if fn:
        fn(by_cid(root, subs), extra)

"""Defs for the deeper systems: arcane strain, elemental reactions and familiars."""
from arcgen import O, to_xml, rgb, hediff, stun, ignite
from core_content import status, stage

NS = "ArcanumAscendant."

FIRE_DMG = ["Flame", "Burn"]


def strain_xml():
    out = []
    out.append(status("AA_ArcaneStrain", "arcane strain",
        "The mind burns from channelling more magic than the body holds. Strain fades with rest. At its peak the mage burns out.",
        "#ff5040", bad=True, maxSev=1.2, initSev=0.01,
        stages=[stage(label="strained", caps={"Consciousness": -0.03}, extra={"painOffset": 0.03}),
                stage(label="taxed", min_sev=0.35, caps={"Consciousness": -0.08}, offsets={"MentalBreakThreshold": 0.03}, extra={"painOffset": 0.08}),
                stage(label="searing", min_sev=0.6, caps={"Consciousness": -0.15, "Manipulation": -0.05}, offsets={"MentalBreakThreshold": 0.06},
                      extra={"painOffset": 0.15}),
                stage(label="breaking", min_sev=0.85, caps={"Consciousness": -0.25, "Moving": -0.1}, offsets={"MentalBreakThreshold": 0.1},
                      extra={"painOffset": 0.25})]))
    out.append(status("AA_ArcaneBurnout", "arcane burnout",
        "This mage pushed the weave too far and it burned back. No mana regenerates and spells are weak until it passes.",
        "#ff7a50", bad=True, ticks=60000,
        stages=[stage(caps={"Consciousness": -0.1}, factors={"AA_SpellPower": 0.5}, extra={"painOffset": 0.05})]))
    return out


def reaction(defName, label, desc, hint, trigger, required, *, color, color2, vfx, callout, bonus=0, bonusDef=None, true_=False, radius=0.0,
             effects=None, heal=0.0, mana_=0.0, style="Arcane", shake=0.7, sound=None, consume=True):
    return to_xml(NS + "ArcaneReactionDef", O(None,
        defName=defName, label=label, description=desc, descriptionShort=hint, triggerDamage=trigger, requiredHediffs=required,
        consume=consume if not consume else None, bonusDamage=float(bonus) if bonus else None, bonusDamageDef=bonusDef, bonusTrue=true_ or None,
        radius=float(radius) if radius else None, effects=effects or None, healCaster=float(heal) if heal else None, manaCaster=float(mana_) if mana_ else None,
        style=style, color=rgb(color) + (1.0,), color2=rgb(color2) + (1.0,), vfx=vfx, shake=float(shake), sound=sound, callout=callout), 1)


def reactions_xml():
    r = []
    r.append(reaction("AA_Reaction_Shatter", "shatter",
        "Fire or crushing force striking a frozen target shatters the ice and the flesh beneath: heavy damage that ignores armor, and the target's armor is sundered.",
        "What happens when something frozen is struck hard, or suddenly heated?",
        FIRE_DMG + ["AA_Force", "Blunt"], ["AA_Frozen"], color="#bfe8ff", color2="#ffffff", vfx="Shatter", callout="SHATTER!",
        bonus=40, true_=True, effects=[hediff("AA_ArmorShred", 2.0, stack=True, maxSeverity=4.0)], style="Frost", shake=1.0, sound="AA_Impact_Frost"))
    r.append(reaction("AA_Reaction_Steam", "steam burst",
        "Frost magic hitting a burning target flashes the flames into scalding steam: it burns everyone nearby and blinds them for a moment.",
        "Ice meets flame.", ["AA_Frost"], ["AA_Burning", "AA_LivingBomb"], color="#e8f4ff", color2="#ffb070", vfx="Steam", callout="STEAM BURST!",
        bonus=16, bonusDef="Burn", radius=2.5, effects=[hediff("AA_Blinded", 1.0, duration=180)], style="Frost", sound="AA_Impact_Frost"))
    r.append(reaction("AA_Reaction_Overload", "overload",
        "Fire touching a target crackling with lightning detonates the charge: an explosion that burns and stuns everyone around it.",
        "Lightning lingers in the flesh. What would a spark of flame do?", FIRE_DMG, ["AA_Shocked", "AA_Stormborn"], color="#ffb040", color2="#a8c8ff",
        vfx="Overload", callout="OVERLOAD!", bonus=30, bonusDef="Flame", radius=2.5, effects=[stun(60)], style="Fire", shake=1.4, sound="AA_Impact_Fire"))
    r.append(reaction("AA_Reaction_Superconduct", "superconduct",
        "Lightning running through frozen or chilled flesh superconducts: it arcs to everyone nearby, shocking them and stripping their armor.",
        "Cold makes some things conduct far better.", ["AA_Lightning", "EMP"], ["AA_Chilled", "AA_Frozen"], color="#9fc4ff", color2="#e6f6ff",
        vfx="Superconduct", callout="SUPERCONDUCT!", bonus=18, bonusDef="AA_Lightning", radius=3.0,
        effects=[hediff("AA_ArmorShred", 2.0, stack=True, maxSeverity=4.0), hediff("AA_Shocked", 1.0)], style="Storm", sound="AA_Impact_Storm"))
    r.append(reaction("AA_Reaction_Wildfire", "wildfire",
        "Fire catching in conjured roots and thorns spreads like a forest blaze: everyone caught in the tangle starts burning.",
        "Living wood burns well.", FIRE_DMG, ["AA_Entangled"], color="#ff8a2a", color2="#c8e070", vfx="Wildfire", callout="WILDFIRE!",
        bonus=12, bonusDef="Flame", radius=3.0, effects=[hediff("AA_Burning", 2.0, stack=True, maxSeverity=5.0), ignite(0.5)], style="Fire", sound="AA_Impact_Fire"))
    r.append(reaction("AA_Reaction_Purge", "purge",
        "Holy light burning through a curse, plague or undeath purges it violently: the curse is torn out, the target takes heavy radiant damage and reels.",
        "Light abhors what has been cursed.", ["AA_Holy"], ["AA_Doom", "AA_Voidtouched", "AA_Undead", "AA_Plague", "AA_Agony", "AA_GraveMark", "AA_Withering"],
        color="#ffe28a", color2="#ffffff", vfx="Purge", callout="PURGED!", bonus=45, bonusDef="AA_Holy", effects=[stun(90)], style="Holy", shake=1.0,
        sound="AA_Impact_Holy"))
    r.append(reaction("AA_Reaction_Fracture", "time fracture",
        "Void magic tearing into a target caught in slowed or looping time fractures the moment itself: enormous damage that ignores armor, and a long stun.",
        "Time and space are one fabric. Pull hard on both at once.", ["AA_Void"], ["AA_Slowed", "AA_CausalityLoop", "AA_Timelocked"],
        color="#c060ff", color2="#7ff5e4", vfx="Fracture", callout="TIME FRACTURE!", bonus=50, true_=True, effects=[stun(120)], style="Void", shake=1.3,
        sound="AA_Impact_Void"))
    r.append(reaction("AA_Reaction_Resonance", "resonance",
        "Sound or force striking something turned to ice, stone or gold finds its resonant frequency, and the target shatters from within.",
        "Every brittle thing has a note that breaks it.", ["AA_Sonic", "AA_Force"], ["AA_Frozen", "AA_Petrified", "AA_Gilded"],
        color="#ff9ce0", color2="#e0f0ff", vfx="Resonance", callout="RESONANCE!", bonus=70, true_=True, style="Song", shake=1.2, sound="AA_Impact_Song"))
    r.append(reaction("AA_Reaction_Bloom", "necrotic bloom",
        "Necrotic magic seeping into open wounds blooms into rot: poison bursts out over everyone nearby and the caster drinks the life it frees.",
        "Rot loves an open wound.", ["AA_Necrotic"], ["AA_Bleeding"], color="#7dff9a", color2="#ff3048", vfx="Bloom", callout="NECROTIC BLOOM!",
        bonus=15, bonusDef="AA_Necrotic", radius=2.5, effects=[hediff("AA_Poisoned", 1.0, stack=True, maxSeverity=4.0)], heal=15, style="Necro",
        sound="AA_Impact_Necro"))
    r.append(reaction("AA_Reaction_Boil", "boiling blood",
        "Fire reaching blood that a hex keeps flowing boils it in the veins: brutal damage and a stagger.",
        "A hex keeps the blood flowing. Heat it.", FIRE_DMG, ["AA_Bleeding"], color="#ff3048", color2="#ffb040", vfx="Boil", callout="BOILING BLOOD!",
        bonus=35, bonusDef="AA_Blood", effects=[stun(60)], style="Blood", sound="AA_Impact_Blood"))
    r.append(reaction("AA_Reaction_Twilight", "twilight",
        "Shadow striking a target marked by holy judgment collides light and dark: the mark collapses in a burst that ignores armor and feeds the caster mana.",
        "Light marks. Shadow answers.", ["AA_Shadow"], ["AA_Marked"], color="#8a6cff", color2="#ffe28a", vfx="Twilight", callout="TWILIGHT!",
        bonus=45, true_=True, mana_=15, style="Shadow", sound="AA_Impact_Shadow"))
    r.append(reaction("AA_Reaction_Entropy", "entropy cascade",
        "Temporal magic hitting a target already withering speeds the decay all at once: heavy damage that ignores armor, and years of ruin on the body.",
        "Decay that is already happening can be hurried along.", ["AA_Temporal"], ["AA_Withering"], color="#7ff5e4", color2="#a8a090",
        vfx="Fracture", callout="ENTROPY CASCADE!", bonus=30, true_=True, effects=[hediff("AA_TimeRavaged", 1.0, stack=True, maxSeverity=5.0)], style="Time",
        sound="AA_Impact_Time"))
    return r


def familiar(defName, label, desc, flavor, tex, *, color, color2, ability, interval, rng, power, style, stats, glowing=True, dmg=None, hd=None, sev=1.0,
             size=0.8, orbit=1.1, speed=1.2):
    return to_xml(NS + "FamiliarDef", O(None,
        defName=defName, label=label, description=desc, flavor=flavor, texPath="ArcanumAscendant/Familiars/" + tex, glowing=glowing,
        color=rgb(color) + (1.0,), color2=rgb(color2) + (1.0,), drawSize=float(size), orbitRadius=float(orbit), orbitSpeed=float(speed),
        ability=ability, abilityInterval=int(interval), abilityRange=float(rng), abilityPower=float(power), damageDef=dmg, hediff=hd,
        hediffSeverity=float(sev) if hd else None, style=style, statOffsets={k: float(v) for k, v in stats.items()}), 1)


def familiars_xml():
    f = []
    f.append(familiar("AA_Familiar_EmberSprite", "ember sprite",
        "A mischievous spark of living flame. In a fight it flits about its mage and spits fire at the nearest enemy, sometimes setting them alight.",
        "It keeps trying to sit in the cookfire.", "EmberSprite", color="#ff8a2a", color2="#ffd36b", ability="Bolt", interval=150, rng=12, power=8,
        style="Fire", stats={"AA_SpellPower": 0.05}, dmg="Flame", hd="AA_Burning", sev=0.5))
    f.append(familiar("AA_Familiar_FrostWisp", "frost wisp",
        "A drifting knot of winter air. It chills enemies with darts of frost and thickens its mage's ward.",
        "Wherever it sleeps, the windows fog.", "FrostWisp", color="#9be3ff", color2="#ffffff", ability="Bolt", interval=170, rng=12, power=6,
        style="Frost", stats={"AA_WardMax": 15}, dmg="AA_Frost", hd="AA_Chilled"))
    f.append(familiar("AA_Familiar_Raven", "shadow raven",
        "A raven with eyes like coals. It dives at enemies, pecking at their eyes, and its watchfulness sharpens its mage's aim at weak points.",
        "It says one word, over and over. Nobody will repeat it.", "Raven", color="#8a6cff", color2="#2a1a4a", ability="Blind", interval=240, rng=14, power=5,
        style="Shadow", stats={"AA_CritChance": 0.04}, glowing=False, dmg="Scratch", hd="AA_Blinded", size=0.9))
    f.append(familiar("AA_Familiar_Lumen", "lumen",
        "A tiny winged light. It seeks out the most injured ally nearby and mends their wounds, and makes its mage's healing stronger.",
        "Children can see it. Adults mostly see a moth.", "Lumen", color="#ffe28a", color2="#ffffff", ability="Heal", interval=300, rng=10, power=6,
        style="Holy", stats={"AA_HealingPower": 0.1}))
    f.append(familiar("AA_Familiar_ClockworkOwl", "clockwork owl",
        "A brass owl that ticks softly. In combat it rebuilds its mage's ward, and its perfect timing quickens every spell.",
        "It hoots exactly on the hour. Every hour.", "ClockworkOwl", color="#ffaa3c", color2="#fff0c8", ability="Ward", interval=360, rng=0, power=12,
        style="Rune", stats={"AA_CooldownReduction": 0.05, "AA_CastSpeed": 0.08}, glowing=False, size=0.85))
    f.append(familiar("AA_Familiar_VoidEye", "void eye",
        "A floating eye from somewhere else. It fires beams of nothing that leave enemies voidtouched, and it shows its mage where reality is thin.",
        "Do not look into it for long. It looks back.", "VoidEye", color="#c060ff", color2="#2a0a4a", ability="Bolt", interval=200, rng=14, power=10,
        style="Void", stats={"AA_SpellPower": 0.06, "AA_CritDamage": 0.08}, dmg="AA_Void", hd="AA_Voidtouched"))
    f.append(familiar("AA_Familiar_PocketDragon", "pocket dragon",
        "A dragon the size of a cat, and twice as proud. Every few seconds in combat it breathes a gout of fire over a cluster of enemies.",
        "It has started a hoard. It is mostly spoons.", "PocketDragon", color="#ff5a3a", color2="#ffd36b", ability="Breath", interval=300, rng=10, power=10,
        style="Fire", stats={"AA_ManaMax": 15}, glowing=False, dmg="Flame", hd="AA_Burning", size=1.0))
    return f

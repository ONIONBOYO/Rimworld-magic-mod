"""Core shared defs: damage types, injuries and status-effect hediffs."""
from arcgen import O, to_xml, rgb

# ------------------------------------------------------------------------------------------------
# Damage types
# ------------------------------------------------------------------------------------------------

# defName, label, injury defName, injury label, armor category (None = ignores armor), pen, color, sound, bleed, death message
DAMAGES = [
    ("AA_Arcane", "arcane", "AA_ArcaneBurn", "arcane burn", "Heat", 0.25, "#b38cff", "Arcane", 0.0, "{0} was unmade by arcane fire."),
    ("AA_ArcaneTrue", "annihilation", "AA_Unmaking", "unmade flesh", None, 1.0, "#ffffff", "Arcane", 0.0, "{0} was annihilated."),
    ("AA_Frost", "frost", "AA_FrostWound", "frost wound", "Heat", 0.2, "#8fd8ff", "Frost", 0.0, "{0} froze solid and shattered."),
    ("AA_Lightning", "lightning", "AA_Electroburn", "electrical burn", "Heat", 0.35, "#a8c8ff", "Storm", 0.0, "{0} was struck down by lightning."),
    ("AA_Holy", "radiance", "AA_Searing", "radiant searing", "Heat", 0.3, "#ffe28a", "Holy", 0.0, "{0} was purged by holy light."),
    ("AA_Shadow", "shadow", "AA_ShadowRend", "shadow rend", "Sharp", 0.35, "#7a5cff", "Shadow", 0.04, "{0} was torn apart by shadow."),
    ("AA_Necrotic", "necrotic", "AA_Necrosis", "necrosis", "Heat", 0.3, "#7dff9a", "Necro", 0.0, "{0} rotted away."),
    ("AA_Force", "force", "AA_ForceBruise", "arcane bruise", "Blunt", 0.25, "#d0d8ff", "Arcane", 0.0, "{0} was crushed by force."),
    ("AA_Blood", "hemorrhage", "AA_Hemorrhage", "hemorrhage", "Sharp", 0.3, "#ff3048", "Blood", 0.12, "{0} bled out from a hex."),
    ("AA_Thorn", "thorns", "AA_ThornWound", "thorn wound", "Sharp", 0.2, "#7fe05a", "Nature", 0.05, "{0} was impaled by thorns."),
    ("AA_Sonic", "sonic", "AA_Concussion", "sonic concussion", "Blunt", 0.3, "#ffb3e6", "Song", 0.0, "{0} was shaken apart by sound."),
    ("AA_Void", "void", "AA_VoidScar", "void scar", "Heat", 0.8, "#c060ff", "Void", 0.0, "{0} was consumed by the void."),
    ("AA_Temporal", "entropy", "AA_Withering", "withering", None, 0.0, "#7ff5e4", "Time", 0.0, "{0} aged to dust."),
    ("AA_Rune", "runic", "AA_RuneBurn", "rune burn", "Sharp", 0.45, "#ffaa3c", "Rune", 0.0, "{0} was dismantled by runes."),
]


def damage_xml():
    out = []
    for d, label, inj, injlabel, armor, pen, color, snd, bleed, death in DAMAGES:
        c = rgb(color)
        out.append(to_xml("HediffDef", O(None,
            defName=inj, label=injlabel, labelNoun="a " + injlabel if injlabel[0] not in "aeiou" else "an " + injlabel,
            description="A wound left by %s magic." % label, hediffClass="Hediff_Injury", tendable=True, displayWound=True,
            labelNounPretty="{0} in the {1}", defaultLabelColor=c + (1.0,),
            comps=[O("HediffCompProperties_TendDuration", labelTendedWell="bandaged", labelTendedWellInner="tended", labelSolidTendedWell="set"),
                   O("HediffCompProperties_Infecter", infectionChance=0.1),
                   O("HediffCompProperties_GetsPermanent", permanentLabel=injlabel + " scar")],
            injuryProps=O(None, painPerSeverity=0.0125, averagePainPerSeverityPermanent=0.00625, bleedRate=bleed if bleed else None, canMerge=True,
                          destroyedLabel="Destroyed by " + label, destroyedOutLabel="Destroyed by " + label)), 1))
        out.append(to_xml("DamageDef", O(None,
            defName=d, label=label, workerClass="DamageWorker_AddInjury", externalViolence=True, deathMessage=death,
            hediff=inj, harmAllLayersUntilOutside=True, armorCategory=armor, defaultDamage=10, defaultArmorPenetration=pen,
            defaultStoppingPower=0.5, minDamageToFragment=12, makesBlood=bleed > 0,
            explosionCellFleck="AA_Fleck_Glow", explosionColorCenter=tuple(min(1.0, x * 1.2 + 0.1) for x in c) + (1.0,),
            explosionColorEdge=c + (1.0,), soundExplosion="AA_Impact_" + snd, combatLogRules="Damage_Bomb",
            buildingDamageFactor=1.5 if d == "AA_Rune" else 1.0), 1))
    return out


# ------------------------------------------------------------------------------------------------
# Status effects
# ------------------------------------------------------------------------------------------------

def disappears(ticks):
    return O("HediffCompProperties_Disappears", disappearsAfterTicks="%d~%d" % (ticks, ticks), showRemainingTime=True)


def aura(color, tex="ArcanumAscendant/Vfx/RuneCircle", size=1.6, fleck=None, interval=20):
    return O("ArcanumAscendant.HediffCompProperties_AuraVisual", color=rgb(color) + (0.9,), texPath=tex, size=size, fleck=fleck, fleckInterval=interval)


def dot(damageDef, dps, color, fleck=None, heal_ps=None):
    return O("ArcanumAscendant.HediffCompProperties_ArcaneDot", damageDef=damageDef, damagePerSecond=dps, color=rgb(color) + (1.0,), fleck=fleck, healPerSecond=heal_ps)


def stage(offsets=None, factors=None, caps=None, dmgf=None, mult=None, label=None, min_sev=None, pain=None, disables=None, extra=None):
    f = dict(
        minSeverity=min_sev, label=label,
        statOffsets={k: float(v) for k, v in (offsets or {}).items()} or None,
        statFactors={k: float(v) for k, v in (factors or {}).items()} or None,
        capMods=[O(None, capacity=k, offset=float(v)) for k, v in (caps or {}).items()] or None,
        damageFactors={k: float(v) for k, v in (dmgf or {}).items()} or None,
        multiplyStatChangesBySeverity=mult,
        painFactor=pain,
        disablesNeeds=disables,
    )
    if extra:
        f.update(extra)
    return O(None, **f)


def status(defName, label, desc, color, *, bad, ticks=None, stages=None, comps=None, maxSev=None, initSev=1.0, cls="HediffWithComps"):
    comps = list(comps or [])
    if ticks:
        comps.insert(0, disappears(ticks))
    return to_xml("HediffDef", O(None,
        defName=defName, label=label, description=desc, hediffClass=cls, isBad=bad, defaultLabelColor=rgb(color) + (1.0,),
        initialSeverity=initSev, maxSeverity=maxSev, everCurableByItem=False if not bad else None,
        comps=comps or None, stages=stages or [stage()]), 1)


FIRE, FROST, STORM, NECRO, HOLY, SHADOW, NATURE, RUNE, SONG, TIME, BLOOD, VOID = (
    "#ff7a2a", "#8fd8ff", "#9fb8ff", "#7dff9a", "#ffe28a", "#8a6cff", "#7fe05a", "#ffaa3c", "#ff9ce0", "#7ff5e4", "#ff3048", "#c060ff")


def status_xml():
    s = []
    # ------------------------------------------------------------- core system hediffs
    s.append(to_xml("HediffDef", O(None, defName="AA_ArcaneCore", label="arcane attunement",
        description="This person has awakened to the arcane. Their attributes and skill tree shape their power.",
        hediffClass="ArcanumAscendant.Hediff_ArcaneCore", isBad=False, everCurableByItem=False, keepOnBodyPartRestoration=True,
        defaultLabelColor=(0.75, 0.6, 1.0, 1.0), stages=[stage()]), 1))
    s.append(status("AA_Summoned", "conjured", "A creature called into being by magic. It will dissipate when the spell ends.", "#c8b8ff", bad=False,
        comps=[O("ArcanumAscendant.HediffCompProperties_Summoned", vanishOnDeath=True), aura("#b49cff", size=1.4)],
        stages=[stage(factors={"MoveSpeed": 1.15}, offsets={"PainShockThreshold": 0.5}, extra={"painFactor": 0.2})]))
    s.append(status("AA_Undead", "risen", "Dragged back from death by necromancy. Feels no pain, needs nothing, and will collapse when the binding ends.", NECRO, bad=False,
        comps=[O("ArcanumAscendant.HediffCompProperties_Summoned", vanishOnDeath=False, killOnExpire=True), aura(NECRO, size=1.3, fleck="AA_Fleck_Smoke", interval=40)],
        stages=[stage(factors={"MoveSpeed": 0.9, "IncomingDamageFactor": 0.85}, offsets={"PainShockThreshold": 0.9, "MentalBreakThreshold": -1.0},
                      extra={"painFactor": 0.0, "disablesNeeds": ["Food", "Rest", "Joy", "Comfort", "Beauty", "Outdoors", "RoomSize"], "blocksMentalBreaks": True})]))
    s.append(status("AA_Dominated", "dominated", "This mind is bound to another's will.", "#ff7af0", bad=True,
        comps=[O("ArcanumAscendant.HediffCompProperties_Dominated"), aura("#ff7af0", size=1.3)], stages=[stage(extra={"blocksMentalBreaks": True})]))
    s.append(status("AA_Polymorphed", "polymorphed", "Temporarily transformed into a harmless creature.", "#ff7af0", bad=True, ticks=600))

    # ------------------------------------------------------------- debuffs
    s.append(status("AA_Burning", "arcane burning", "Clinging arcane flames sear the flesh.", FIRE, bad=True, ticks=360, maxSev=5,
        comps=[dot("Burn", 3.0, FIRE, "AA_Fleck_Ember")], stages=[stage(offsets={"MentalBreakThreshold": 0.05})]))
    s.append(status("AA_Chilled", "chilled", "Biting cold slows every movement.", FROST, bad=True, ticks=420, maxSev=3,
        comps=[aura(FROST, "ArcanumAscendant/Vfx/Frost", 1.4, "AA_Fleck_Snow", 25)],
        stages=[stage(factors={"MoveSpeed": 0.6, "AimingDelayFactor": 1.3, "MeleeCooldownFactor": 1.3})]))
    s.append(status("AA_Frozen", "frozen", "Encased in ice. Barely able to move, and the ice makes the flesh beneath brittle.", FROST, bad=True, ticks=180,
        comps=[aura("#d8f4ff", "ArcanumAscendant/Vfx/Frost", 1.8, "AA_Fleck_Shard", 15)],
        stages=[stage(factors={"MoveSpeed": 0.08, "AimingDelayFactor": 3.0, "MeleeCooldownFactor": 3.0, "IncomingDamageFactor": 1.3})]))
    s.append(status("AA_Shocked", "shocked", "Muscles spasm with lingering current.", STORM, bad=True, ticks=300, maxSev=3,
        comps=[aura(STORM, None, 1.0, "AA_Fleck_Bolt", 18)],
        stages=[stage(factors={"AimingDelayFactor": 1.5, "MeleeHitChance": 0.8, "ShootingAccuracyPawn": 0.85}, caps={"Manipulation": -0.15})]))
    s.append(status("AA_Poisoned", "arcane venom", "A magical toxin eats away at the body.", NECRO, bad=True, ticks=600, maxSev=4,
        comps=[dot("AA_Necrotic", 2.0, NECRO, "AA_Fleck_Mote")], stages=[stage(caps={"BloodFiltration": -0.2})]))
    s.append(status("AA_Plague", "black plague", "A necromantic plague. Victims who die from it burst and spread the sickness to those nearby.", "#5fbf5a", bad=True, ticks=1800, maxSev=5,
        comps=[dot("AA_Necrotic", 1.6, "#5fbf5a", "AA_Fleck_Smoke"),
               O("ArcanumAscendant.HediffCompProperties_DeathBurst", damageDef="AA_Necrotic", damage=18, radius=3.0, spreadHediff="AA_Plague", spreadSeverity=1.0, color=rgb("#5fbf5a") + (1.0,))],
        stages=[stage(caps={"Consciousness": -0.05, "Moving": -0.1}, factors={"ImmunityGainSpeed": 0.5})]))
    s.append(status("AA_Bleeding", "hemorrhage hex", "A blood curse keeps wounds from closing.", BLOOD, bad=True, ticks=480, maxSev=5,
        comps=[dot("AA_Blood", 2.2, BLOOD, "AA_Fleck_Blood")], stages=[stage(factors={"InjuryHealingFactor": 0.2})]))
    s.append(status("AA_Weakened", "enfeebled", "Strength drains from the limbs and focus from the eyes.", "#9a9a9a", bad=True, ticks=900,
        stages=[stage(factors={"MeleeDamageFactor": 0.7, "ShootingAccuracyPawn": 0.8, "WorkSpeedGlobal": 0.8})]))
    s.append(status("AA_Doom", "doomed", "A hex that makes the victim suffer more from every wound. Severity deepens the curse.", SHADOW, bad=True, ticks=900, maxSev=3,
        comps=[aura(SHADOW, "ArcanumAscendant/Vfx/Hex", 1.6)],
        stages=[stage(factors={"IncomingDamageFactor": 1.2}, label="cursed"), stage(factors={"IncomingDamageFactor": 1.4}, min_sev=1.5, label="doomed"),
                stage(factors={"IncomingDamageFactor": 1.7}, min_sev=2.5, label="damned")]))
    s.append(status("AA_ArmorShred", "sundered", "Armor and hide are torn by magic. Stacks.", RUNE, bad=True, ticks=600, maxSev=4,
        stages=[stage(offsets={"ArmorRating_Sharp": -0.15, "ArmorRating_Blunt": -0.15, "ArmorRating_Heat": -0.15}, mult=True)]))
    s.append(status("AA_Slowed", "time-slowed", "Caught in a slowed pocket of time.", TIME, bad=True, ticks=480,
        comps=[aura(TIME, "ArcanumAscendant/Vfx/Clock", 1.6)],
        stages=[stage(factors={"MoveSpeed": 0.4, "AimingDelayFactor": 1.7, "MeleeCooldownFactor": 1.7, "WorkSpeedGlobal": 0.5})]))
    s.append(status("AA_Silenced", "silenced", "Unable to weave spells.", "#cccccc", bad=True, ticks=360, comps=[aura("#ffffff", "ArcanumAscendant/Vfx/Hex", 1.2)]))
    s.append(status("AA_Marked", "judged", "Marked for judgment. Holy light and allied weapons find this target more easily.", HOLY, bad=True, ticks=1200,
        comps=[aura(HOLY, "ArcanumAscendant/Vfx/RuneCircle", 1.4)],
        stages=[stage(factors={"IncomingDamageFactor": 1.15}, dmgf={"AA_Holy": 1.5})]))
    s.append(status("AA_Entangled", "entangled", "Bound by roots and vines.", NATURE, bad=True, ticks=300,
        comps=[aura(NATURE, "ArcanumAscendant/Vfx/Roots", 1.6, "AA_Fleck_Leaf", 30)],
        stages=[stage(factors={"MoveSpeed": 0.1, "MeleeCooldownFactor": 1.3})]))
    s.append(status("AA_Blinded", "blinded", "Searing light or shadow blots out the eyes.", "#ffffff", bad=True, ticks=360,
        stages=[stage(caps={"Sight": -0.6}, factors={"ShootingAccuracyPawn": 0.5})]))
    s.append(status("AA_Withering", "withering curse", "The victim's healing fails and their flesh ages.", "#8fa07a", bad=True, ticks=1200,
        comps=[dot("AA_Temporal", 1.0, TIME, "AA_Fleck_Clock")], stages=[stage(factors={"InjuryHealingFactor": 0.1, "ImmunityGainSpeed": 0.5})]))
    s.append(status("AA_Pied", "pied", "Hit square in the face with a conjured custard pie. Humiliating, and hard to see through.", "#fff2c8", bad=True, ticks=600,
        stages=[stage(caps={"Sight": -0.4}, factors={"ShootingAccuracyPawn": 0.7, "SocialImpact": 0.5})]))
    s.append(status("AA_Concussed", "concussed", "Ears ringing, vision swimming.", SONG, bad=True, ticks=420,
        stages=[stage(caps={"Consciousness": -0.2, "Hearing": -0.5})]))
    s.append(status("AA_Voidtouched", "voidtouched", "Reality frays around the victim. Void damage is amplified and armor fails.", VOID, bad=True, ticks=900,
        comps=[aura(VOID, "ArcanumAscendant/Vfx/Vortex", 1.4)],
        stages=[stage(dmgf={"AA_Void": 1.5}, offsets={"ArmorRating_Sharp": -0.25, "ArmorRating_Heat": -0.25})]))

    # ------------------------------------------------------------- buffs
    s.append(status("AA_Haste", "hastened", "Time flows faster around this person.", TIME, bad=False, ticks=1200,
        comps=[aura(TIME, "ArcanumAscendant/Vfx/Clock", 1.3)],
        stages=[stage(factors={"MoveSpeed": 1.5, "AimingDelayFactor": 0.7, "MeleeCooldownFactor": 0.75, "WorkSpeedGlobal": 1.35}, offsets={"AA_CastSpeed": 0.3})]))
    s.append(status("AA_Empowered", "empowered", "Brimming with arcane energy. Stacks.", "#c9a0ff", bad=False, ticks=1200, maxSev=5,
        comps=[aura("#c9a0ff", None, 1.0, "AA_Fleck_Rune", 30)],
        stages=[stage(offsets={"AA_SpellPower": 0.15, "AA_CritChance": 0.03}, mult=True)]))
    s.append(status("AA_Stoneskin", "stoneskin", "Skin hardened to living stone.", "#b8a890", bad=False, ticks=3600,
        stages=[stage(offsets={"ArmorRating_Sharp": 0.45, "ArmorRating_Blunt": 0.45}, factors={"MoveSpeed": 0.85})]))
    s.append(status("AA_Blessed", "blessed", "Divine favor wards body and mind.", HOLY, bad=False, ticks=5000,
        comps=[aura(HOLY, None, 1.0, "AA_Fleck_Star", 45)],
        stages=[stage(offsets={"ArmorRating_Sharp": 0.15, "ArmorRating_Blunt": 0.15, "PainShockThreshold": 0.2, "MentalBreakThreshold": -0.08},
                      factors={"InjuryHealingFactor": 2.0, "ImmunityGainSpeed": 1.5})]))
    s.append(status("AA_Regeneration", "regenerating", "Wounds knit closed before your eyes.", "#7dffb0", bad=False, ticks=1200, maxSev=5,
        comps=[dot(None, 0.0, "#7dffb0", "AA_Fleck_Leaf", heal_ps=3.0)]))
    s.append(status("AA_Valor", "valor", "Stirred by a war song: braver, stronger, steadier.", SONG, bad=False, ticks=2400,
        comps=[aura(SONG, None, 1.0, "AA_Fleck_Note", 35)],
        stages=[stage(offsets={"PainShockThreshold": 0.3, "MentalBreakThreshold": -0.1}, factors={"MeleeDamageFactor": 1.25, "ShootingAccuracyPawn": 1.15, "MeleeHitChance": 1.15})]))
    s.append(status("AA_Focused", "arcane focus", "Mind sharpened to a razor's edge.", "#9fd0ff", bad=False, ticks=30000,
        stages=[stage(factors={"WorkSpeedGlobal": 1.25, "GlobalLearningFactor": 1.5, "ResearchSpeed": 1.3})]))
    s.append(status("AA_FireWard", "fire ward", "Immune to flame and wrapped in heat.", FIRE, bad=False, ticks=3600,
        comps=[aura(FIRE, None, 1.0, "AA_Fleck_Ember", 20)],
        stages=[stage(offsets={"ArmorRating_Heat": 1.0}, factors={"Flammability": 0.0}, dmgf={"Flame": 0.0, "Burn": 0.1})]))
    s.append(status("AA_BearForm", "ursine form", "Shaped into the might of a great bear: thick hide, crushing blows.", "#b07a4a", bad=False, ticks=3600,
        comps=[aura("#b07a4a", "ArcanumAscendant/Vfx/Claw", 1.8)],
        stages=[stage(offsets={"ArmorRating_Sharp": 0.45, "ArmorRating_Blunt": 0.45, "PainShockThreshold": 0.5}, factors={"MeleeDamageFactor": 2.0, "MoveSpeed": 0.9, "IncomingDamageFactor": 0.8})]))
    s.append(status("AA_WolfForm", "lupine form", "Shaped into a swift, savage wolf.", "#c8c8d8", bad=False, ticks=3600,
        comps=[aura("#c8c8d8", "ArcanumAscendant/Vfx/Claw", 1.4)],
        stages=[stage(factors={"MoveSpeed": 1.6, "MeleeDamageFactor": 1.4, "MeleeCooldownFactor": 0.7, "MeleeHitChance": 1.2})]))
    s.append(status("AA_ThrumboForm", "thrumbo avatar", "The primal shape of the thrumbo: near-invulnerable, unstoppable.", "#f0e8ff", bad=False, ticks=2400,
        comps=[aura("#f0e8ff", "ArcanumAscendant/Vfx/Claw", 2.4), dot(None, 0.0, "#ffffff", "AA_Fleck_Leaf", heal_ps=6.0)],
        stages=[stage(offsets={"ArmorRating_Sharp": 0.9, "ArmorRating_Blunt": 0.9, "PainShockThreshold": 0.8}, factors={"MeleeDamageFactor": 3.0, "MoveSpeed": 1.1, "IncomingDamageFactor": 0.5})]))
    s.append(status("AA_Invisible", "shrouded", "Wrapped in shadow, unseen by enemies.", SHADOW, bad=False, ticks=900,
        comps=[O("HediffCompProperties_Invisibility", visibleToPlayer=True)],
        stages=[stage(factors={"MoveSpeed": 1.2})]))
    s.append(status("AA_BloodFrenzy", "blood frenzy", "Driven by the taste of blood.", BLOOD, bad=False, ticks=1200,
        comps=[aura(BLOOD, None, 1.0, "AA_Fleck_Blood", 25)],
        stages=[stage(factors={"MeleeCooldownFactor": 0.6, "MeleeDamageFactor": 1.3, "AimingDelayFactor": 0.75}, offsets={"AA_Lifesteal": 0.15, "PainShockThreshold": 0.5})]))
    s.append(status("AA_DivineShield", "divine shield", "Untouchable for a few precious moments.", HOLY, bad=False, ticks=360,
        comps=[aura(HOLY, "ArcanumAscendant/Vfx/HexShield", 2.2)], stages=[stage(factors={"IncomingDamageFactor": 0.1})]))
    s.append(status("AA_Lichform", "lichform", "A body half-dead, all power. No pain, no hunger, terrible magic.", NECRO, bad=False, ticks=3600,
        comps=[aura(NECRO, "ArcanumAscendant/Vfx/RuneCircle2", 2.2, "AA_Fleck_Skull", 40)],
        stages=[stage(offsets={"AA_SpellPower": 0.5, "AA_CooldownReduction": 0.15, "PainShockThreshold": 1.0, "ToxicResistance": 1.0}, factors={"IncomingDamageFactor": 0.5},
                      extra={"painFactor": 0.0, "disablesNeeds": ["Food", "Rest"]})]))
    s.append(status("AA_Avatar", "avatar of light", "A blazing vessel of the divine.", HOLY, bad=False, ticks=3600,
        comps=[aura(HOLY, "ArcanumAscendant/Vfx/RuneCircle2", 2.6, "AA_Fleck_Feather", 20)],
        stages=[stage(offsets={"AA_SpellPower": 0.4, "ArmorRating_Sharp": 0.6, "ArmorRating_Blunt": 0.6, "ArmorRating_Heat": 0.6}, factors={"MoveSpeed": 1.2, "IncomingDamageFactor": 0.5, "MeleeDamageFactor": 1.6})]))
    s.append(status("AA_Stormborn", "stormborn", "Riding the lightning.", STORM, bad=False, ticks=2400,
        comps=[aura(STORM, None, 1.0, "AA_Fleck_Bolt", 10)],
        stages=[stage(offsets={"AA_CastSpeed": 0.5, "AA_CooldownReduction": 0.25, "AA_SpellPower": 0.2}, factors={"MoveSpeed": 1.35})]))
    s.append(status("AA_ManaSurge", "mana surge", "Mana pours in faster than it can be spent.", "#5aa0ff", bad=False, ticks=1200,
        comps=[aura("#5aa0ff", None, 1.0, "AA_Fleck_Glow", 20)], stages=[stage(offsets={"AA_ManaRegen": 4.0, "AA_ManaCostFactor": -0.3})]))
    s.append(status("AA_Overcharged", "rune-charged", "Weapons and reflexes hum with runic power.", RUNE, bad=False, ticks=1800,
        comps=[aura(RUNE, None, 1.0, "AA_Fleck_Gear", 30)],
        stages=[stage(factors={"AimingDelayFactor": 0.5, "RangedCooldownFactor": 0.6, "ShootingAccuracyPawn": 1.25})]))
    s.append(status("AA_Barkskin", "barkskin", "Skin hardened into living bark.", NATURE, bad=False, ticks=3600,
        stages=[stage(offsets={"ArmorRating_Sharp": 0.35, "ArmorRating_Blunt": 0.35}, factors={"IncomingDamageFactor": 0.9, "Flammability": 1.5})]))
    s.append(status("AA_Rallied", "rallied", "Spirits lifted by a rousing song.", SONG, bad=False, ticks=5000,
        stages=[stage(offsets={"MentalBreakThreshold": -0.15, "PainShockThreshold": 0.25}, factors={"WorkSpeedGlobal": 1.1})]))
    s.append(status("AA_Lucky", "lucky", "Fortune smiles. For now.", "#ffd84a", bad=False, ticks=2400,
        comps=[aura("#ffd84a", None, 1.0, "AA_Fleck_Star", 30)],
        stages=[stage(offsets={"AA_CritChance": 0.25}, factors={"ShootingAccuracyPawn": 1.2, "MeleeHitChance": 1.2})]))
    s.append(status("AA_Ironhide", "ironhide", "Runes of warding etched into the skin.", RUNE, bad=False, ticks=3600,
        comps=[aura(RUNE, "ArcanumAscendant/Vfx/Hex", 1.6)],
        stages=[stage(offsets={"ArmorRating_Sharp": 0.6, "ArmorRating_Blunt": 0.4, "ArmorRating_Heat": 0.4})]))
    s.append(status("AA_PhoenixWings", "phoenix wings", "Wings of living flame lift and shield the caster.", FIRE, bad=False, ticks=1800,
        comps=[aura(FIRE, "ArcanumAscendant/Vfx/Wings", 2.6, "AA_Fleck_Ember", 6)],
        stages=[stage(factors={"MoveSpeed": 1.45, "Flammability": 0.0}, offsets={"AA_SpellPower": 0.2, "ArmorRating_Heat": 1.0}, dmgf={"Flame": 0.0, "Burn": 0.0})]))
    s.append(status("AA_Timelocked", "timelocked", "Frozen outside the flow of time. Cannot be harmed, cannot act.", TIME, bad=False, ticks=300,
        comps=[aura(TIME, "ArcanumAscendant/Vfx/Clock", 2.0)], stages=[stage(factors={"IncomingDamageFactor": 0.0, "MoveSpeed": 0.05})]))
    s.append(status("AA_VoidShift", "phase-shifted", "Partly slipped out of reality; attacks pass through.", VOID, bad=False, ticks=600,
        comps=[aura(VOID, "ArcanumAscendant/Vfx/Vortex", 1.5)], stages=[stage(factors={"IncomingDamageFactor": 0.4, "MoveSpeed": 1.25})]))
    s.append(status("AA_Sanctified", "sanctified", "Holy ground strengthens the faithful.", HOLY, bad=False, ticks=240,
        stages=[stage(factors={"IncomingDamageFactor": 0.8, "InjuryHealingFactor": 3.0})]))
    s.append(status("AA_Chronoshield", "chronal shield", "Incoming harm is slowed to a crawl.", TIME, bad=False, ticks=1500,
        comps=[aura(TIME, "ArcanumAscendant/Vfx/HexShield", 1.9)], stages=[stage(factors={"IncomingDamageFactor": 0.65})]))
    s.append(status("AA_Shadowstep", "shadowstep", "Moving between shadows.", SHADOW, bad=False, ticks=600,
        stages=[stage(factors={"MoveSpeed": 1.6, "IncomingDamageFactor": 0.85})]))
    return s

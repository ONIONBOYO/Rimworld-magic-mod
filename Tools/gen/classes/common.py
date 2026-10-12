"""Shared shorthand for class modules."""
from arcgen import (O, Spell, Node, ClassSpec, School, mod, P, rgb, to_xml,  # noqa: F401
                    dmg, heal, hediff, stun, knock, explode, ignite, extinguish, shieldbreak, ward, mana, cleanse, mental, field,
                    summon, raise_dead, spawn, conjure, teleport, resurrect, dominate, polymorph, condition, weather, lightning,
                    need, thought, inspire, skillxp, grow, repair, transmute, heat, mine, terraform, recruit, reveal, age, rewind,
                    cdreset, random_of, vfx, burst, glow, flash, ring, rune, pillar, bolt_down, smoke, spiral, hexshield, orbit, FLECK)
from core_content import status, stage, disappears, aura, dot  # noqa: F401
import things  # noqa: F401

SEC = 60
MIN = 3600
HOUR = 2500
DAY = 60000


def S(spell_id):
    return "AA_Spell_" + spell_id


def burst_on(h, chance, sev=1.0, tag=None, spells=None):
    """Spell modifier: matching spells have a chance to apply hediff h on hit."""
    return mod(tag=tag, spells=[S(s) for s in spells] if spells else None, applyHediff=h, applyChance=chance, applySeverity=sev)


def spellmod(spells, **kw):
    return mod(spells=[S(s) for s in spells], **kw)


def tagmod(tag, **kw):
    return mod(tag=tag, **kw)


def allmod(**kw):
    return mod(**kw)


def expire_burst(damageDef, damage, radius, color, shake=1.0):
    return O("ArcanumAscendant.HediffCompProperties_ExpireBurst", damageDef=damageDef, damage=float(damage), radius=float(radius),
             color=rgb(color) + (1.0,), shake=float(shake))


def corpse_explode(damage, radius, damageDef=None, maxCorpses=6, **kw):
    return O("Effect_CorpseExplosion", damage=float(damage), explosionRadius=float(radius), damageDef=damageDef, maxCorpses=maxCorpses, **kw)


def speak_dead(xp=2000, **kw):
    return O("Effect_SpeakWithDead", xp=float(xp), **kw)


def dispel(**kw):
    return O("Effect_Dispel", **kw)


# ------------------------------------------------------------------------------------------------
# Newer mechanics
# ------------------------------------------------------------------------------------------------

def agesh(years, **kw):
    """Shift biological age. Negative years can also cure age conditions, fade scars and regrow parts; positive years can
    inflict old-age ailments and kill past lethalAgeFactor x life expectancy."""
    return O("Effect_AgeShift", years=float(years), **kw)


def restore(chronic=0, scars=0, regrowParts=0, addictions=False, **kw):
    return O("Effect_Restore", chronic=chronic or None, scars=scars or None, regrowParts=regrowParts or None, addictions=addictions or None, **kw)


def restore_items(repair=1.0, **kw):
    return O("Effect_RestoreItems", repairFraction=float(repair), **kw)


def improve_quality(steps=1, maxQuality="Legendary", **kw):
    return O("Effect_ImproveQuality", steps=steps, maxQuality=maxQuality, **kw)


def midas(threshold=0.5, gold=40.0, damage=30.0, gilded="AA_Gilded", **kw):
    return O("Effect_Midas", healthThreshold=float(threshold), goldPerBodySize=float(gold), damage=float(damage), gildedHediff=gilded, **kw)


def meteorite(mineable=None, **kw):
    return O("Effect_CallMeteorite", mineable=mineable, **kw)


def devour(heal=25.0, mana=20.0, maxCorpses=6, **kw):
    return O("Effect_ConsumeCorpses", healPerBodySize=float(heal), manaPerBodySize=float(mana), maxCorpses=maxCorpses, **kw)


def steal_skill(xp=4000, **kw):
    return O("Effect_StealSkill", xp=float(xp), **kw)


def shell(factor=1.0, reflect=0.0, color="#ffffff", reflectDef=None):
    """Hediff comp: multiplies incoming damage and optionally reflects part of it."""
    return O("ArcanumAscendant.HediffCompProperties_DamageShell", damageFactor=float(factor), reflectFraction=float(reflect) if reflect else None,
             reflectDamageDef=reflectDef, color=rgb(color) + (1.0,))


def death_ward(color, labelKey=None):
    """Hediff comp: if the bearer dies while it lasts, they come back to life."""
    return O("ArcanumAscendant.HediffCompProperties_DeathWard", color=rgb(color) + (1.0,), labelKey=labelKey)


def raise_on_death(duration=DAY, undead=None):
    """Hediff comp: if the bearer dies, they rise as the caster's undead."""
    return O("ArcanumAscendant.HediffCompProperties_RaiseOnDeath", durationTicks=int(duration), undeadHediff=undead)

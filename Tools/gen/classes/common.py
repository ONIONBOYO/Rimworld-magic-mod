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

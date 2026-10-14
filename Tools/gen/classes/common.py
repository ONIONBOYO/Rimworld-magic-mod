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


# ------------------------------------------------------------------------------------------------
# Mythic mechanics (Harbinger, Wind Mage, Succubus, Incubus, Noclipper)
# ------------------------------------------------------------------------------------------------

def _c(color):
    return rgb(color) + (1.0,) if isinstance(color, str) else color


def launch(mode="AwayFromCaster", distance=6.0, damage=12.0, damageDef=None, slam=None, splash=0.0, splashFactor=None, stunTicks=None,
           maxBodySize=None, landing=None, landingRadius=None, **kw):
    """Throw pawns through the air with a real flight arc. Landing hurts; walls hurt more (slam x damage)."""
    return O("Effect_Launch", mode=mode, distance=float(distance), damage=float(damage), damageDef=damageDef, wallSlamFactor=slam,
             splashRadius=float(splash) if splash else None, splashFactor=splashFactor, stunTicks=stunTicks, maxBodySize=maxBodySize,
             landingEffects=landing, landingRadius=float(landingRadius) if landingRadius else None, **kw)


def disarm(steal=True, destroyChance=0.0, fallback=20.0, fallbackDef=None, mechs=False, **kw):
    """Tear the weapon out of a humanlike's hands (and bring it to the caster). Anything else takes fallback damage."""
    return O("Effect_Disarm", steal=steal, destroyChance=destroyChance or None, fallbackDamage=float(fallback), fallbackDamageDef=fallbackDef,
             affectsMechs=mechs or None, **kw)


def banish(duration, bossFactor=None, maxBodySize=None, ret=None, retRadius=None, **kw):
    """Remove targets from reality for a while. They come back where they were (ret effects play on their return)."""
    return O("Effect_Banish", durationTicks=int(duration), bossFactor=bossFactor, maxBodySize=maxBodySize, returnEffects=ret,
             returnRadius=float(retRadius) if retRadius else None, **kw)


def step(delay, effects=None, radius=None, vfx=None, shake=None, sound=None, text=None, textColor=None):
    """One stage of a sequence, delay ticks after the previous one."""
    return O(None, delayTicks=int(delay), radius=float(radius) if radius is not None else None, effects=effects, vfx=vfx,
             shake=float(shake) if shake else None, sound=sound, textKey=text, textColor=_c(textColor) if textColor else None)


def sequence(*steps, **kw):
    return O("Effect_Sequence", steps=list(steps), **kw)


def empower(h, perStack=0.1, maxBonus=4.0, consume=True, **kw):
    """Caster effect: this cast gains power per stack of h on the caster (spending them)."""
    return O("Effect_Empower", hediff=h, perStack=float(perStack), maxBonus=float(maxBonus), consume=consume, **kw)


def copy_pawn(duration, maxBodySize=None, maxCopies=None, **kw):
    return O("Effect_CopyPawn", durationTicks=int(duration), maxBodySize=maxBodySize, maxCopies=maxCopies, **kw)


def stacks(h, n=1.0, duration=None, cap=None, **kw):
    """Add n stacks of a stacking status (refreshing its timer)."""
    return hediff(h, float(n), duration=duration, stack=True, maxSeverity=float(cap) if cap else None, **kw)


# ---- status comps

def stack_trigger(threshold, effects=None, spread=None, radius=4.0, boss=None, vfx=None, color="#ffffff", label=None, shake=None, sound=None):
    return O("ArcanumAscendant.HediffCompProperties_StackTrigger", threshold=float(threshold), bossThreshold=float(boss) if boss else None,
             effects=effects, spreadEffects=spread, spreadRadius=float(radius), vfx=vfx, color=_c(color), labelKey=label,
             shake=float(shake) if shake else None, sound=sound)


def delayed_doom(stored=1.0, lethal=0.0, stackHediff=None, perStack=0.0, consume=True, damageDef=None, boss=0.5, color="#ff4050", label=None, shake=None):
    return O("ArcanumAscendant.HediffCompProperties_DelayedDoom", storedFactor=float(stored), lethalPercent=float(lethal) if lethal else None,
             stackHediff=stackHediff, perStackLethal=float(perStack) if perStack else None, consumeStacks=consume if stackHediff else None,
             damageDef=damageDef, bossFactor=float(boss), color=_c(color), labelKey=label, shake=float(shake) if shake else None)


def death_trigger(caster=None, area=None, radius=5.0, vfx=None, color="#ffffff", label=None, shake=None):
    return O("ArcanumAscendant.HediffCompProperties_DeathTrigger", casterEffects=caster, areaEffects=area, areaRadius=float(radius), vfx=vfx,
             color=_c(color), labelKey=label, shake=float(shake) if shake else None)


def compel(mode="Approach", breakOnDamage=False, breakDamage=None, mechs=False, interval=None, approach=None, flee=None):
    return O("ArcanumAscendant.HediffCompProperties_Compel", mode=mode, breakOnDamage=breakOnDamage or None,
             breakDamage=float(breakDamage) if breakDamage else None, affectsMechs=mechs or None, interval=interval,
             approachDistance=float(approach) if approach else None, fleeDistance=float(flee) if flee else None)


def damage_link(share=0.4, damageDef=None, color="#ff4060"):
    return O("ArcanumAscendant.HediffCompProperties_DamageLink", shareFraction=float(share), damageDef=damageDef, color=_c(color))


def stack_on_damaged(h, severity=1.0, cap=None, cooldown=20):
    return O("ArcanumAscendant.HediffCompProperties_StackOnDamaged", hediff=h, severity=float(severity), maxSeverity=float(cap) if cap else None,
             cooldownTicks=int(cooldown))


def contract(kills, reward=None, penalty=None, rewardSev=1.0, penaltySev=1.0, color="#ffb347"):
    return O("ArcanumAscendant.HediffCompProperties_Contract", killsRequired=int(kills), rewardHediff=reward, rewardSeverity=float(rewardSev),
             penaltyHediff=penalty, penaltySeverity=float(penaltySev), color=_c(color))


def glitching(chance=0.35, stun=30, color="#f2e27a"):
    return O("ArcanumAscendant.HediffCompProperties_Glitch", chancePerSecond=float(chance), stunTicks=int(stun), color=_c(color))


def deflect(chance=0.3, rangedOnly=True, reflect=0.0, reflectDef=None, color="#7ef0d2"):
    return O("ArcanumAscendant.HediffCompProperties_Deflect", chance=float(chance), rangedOnly=rangedOnly, reflectFraction=float(reflect) if reflect else None,
             reflectDamageDef=reflectDef, color=_c(color))


def periodic(interval, effects=None, area=None, radius=3.0, vfx=None, color="#ffffff"):
    return O("ArcanumAscendant.HediffCompProperties_PeriodicEffects", interval=int(interval), effects=effects, areaEffects=area,
             areaRadius=float(radius), vfx=vfx, color=_c(color))


def screen(color, alpha=0.3, ticks=30, vignette=False):
    """A brief full-screen colour wash (or a darkening vignette) for the biggest moments."""
    return vfx("ScreenFlash", color=color, alpha=float(alpha), ticks=int(ticks), ground=vignette or None)

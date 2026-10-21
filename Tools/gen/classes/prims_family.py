"""Generator helpers for the love, family and sleep primitives (Source/ArcanumAscendant/Spells/Effects_Family.cs and
Core/HediffComps_Family.cs)."""
from arcgen import O, rgb


def _c(color):
    return rgb(color) + (1.0,) if isinstance(color, str) else color


def memory(thought, toCaster=False, **kw):
    """Give a ThoughtDef memory. Social (opinion) memories need someone to be about: toCaster makes it the caster."""
    return O("Effect_Thought", thought=thought, toCaster=toCaster or None, **kw)


def matchmake(mode="BestMatch", relation=None, opinion=None, mood=None, requireAttraction=True, maxPairs=3, **kw):
    """Kindle love. mode BestMatch (target + their best match on the map), InArea (pairs inside the area) or Caster (target +
    caster). relation Lover/Fiance/Spouse makes it official when both are adults, attracted and single or already together.
    opinion/mood are ThoughtDefs (an opinion_def and a thought_def) both partners gain about each other."""
    return O("Effect_Matchmake", mode=mode, relation=relation, opinionThought=opinion, moodThought=mood,
             requireAttraction=None if requireAttraction else False, maxPairs=maxPairs if maxPairs != 3 else None, **kw)


def rekindle(opinion=None, mood=None, raise_=False, **kw):
    """The target and their partner fall in love all over again (memories both ways); raise_ also gets lovers engaged and
    fiances married."""
    extra = {"raise": True} if raise_ else {}
    return O("Effect_Rekindle", opinionThought=opinion, moodThought=mood, **extra, **kw)


def conceive(humans=True, animals=True, mateRadius=15.0, **kw):
    """The target (or the target's partner) conceives now: women with a male partner (Biotech) and female animals with a male
    of their kind within mateRadius (0 = anywhere). Vanilla fertility rules still apply."""
    return O("Effect_Conceive", humans=None if humans else False, animals=None if animals else False,
             mateRadius=float(mateRadius) if mateRadius != 15.0 else None, **kw)


def gestate(days, **kw):
    """Advance pregnancies by this many days (scaled a little by healing power), never past the birth itself."""
    return O("Effect_Gestate", days=float(days), **kw)


def sleep_now(inBed=True, **kw):
    return O("Effect_SleepNow", inBed=None if inBed else False, **kw)


def calm(violent=False, thought=None, **kw):
    """End mental breaks (violent ones too if asked); thought is an optional memory about the caster afterwards."""
    return O("Effect_Calm", violent=violent or None, thought=thought, **kw)


# ---- status comps

def multi_birth(extraMin=1, extraMax=1, chance=1.0):
    """The bearer's next birth brings extra babies (people, through the vanilla birth) or extra litters (animals)."""
    return O("ArcanumAscendant.HediffCompProperties_MultipleBirth", extraMin=int(extraMin), extraMax=int(extraMax),
             chance=float(chance) if chance != 1.0 else None)


def wake_thought(thought=None, social=None, subject="None", minSleepHours=4.0, inspirationChance=0.0, inspiration=None, rest=0.0, joy=0.0,
                 text=None, color="#c8b4ff"):
    """When the bearer has slept long enough and wakes, they gain the thought (and an opinion memory about the subject:
    Caster or Lover), maybe an inspiration, rest and recreation, and the status ends."""
    return O("ArcanumAscendant.HediffCompProperties_WakeThought", thought=thought, socialThought=social, subject=subject if subject != "None" else None,
             minSleepHours=float(minSleepHours), inspirationChance=float(inspirationChance) if inspirationChance else None, inspiration=inspiration,
             rest=float(rest) if rest else None, joy=float(joy) if joy else None, textKey=text, color=_c(color))

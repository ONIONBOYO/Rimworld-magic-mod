"""Generator helpers for the colony primitives (Source/ArcanumAscendant/Spells/Effects_Colony.cs)."""
from arcgen import O


def _opt(v, default):
    return None if v == default else v


def research(points, finishBelow=0.0, **kw):
    """Add research progress to the current project (scaled by spell power); finish it outright when less than finishBelow is left."""
    return O("Effect_Research", points=float(points), finishBelow=float(finishBelow) if finishBelow else None, **kw)


def construct(work, **kw):
    """Add work to construction frames in the area that have all their materials (completing them)."""
    return O("Effect_Construct", work=float(work), **kw)


def craft(work, **kw):
    """Take work off unfinished things (bills in progress) in the area."""
    return O("Effect_Craft", work=float(work), **kw)


def harvest(yieldFactor=1.0, wild=False, trees=False, **kw):
    """Harvest every ripe plant in the area (crops in growing zones only, unless wild)."""
    return O("Effect_Harvest", yieldFactor=float(yieldFactor), wild=wild or None, trees=trees or None, **kw)


def sow(growth=0.05, **kw):
    """Sow each growing zone's crop into its empty cells in the area."""
    return O("Effect_Sow", growth=_opt(float(growth), 0.05), **kw)


def animal_produce(fraction=0.5, **kw):
    """Tame animals gain milk/wool fullness and egg progress."""
    return O("Effect_AnimalProduce", fraction=float(fraction), **kw)


def train(steps=2, **kw):
    """Advance a tame animal's training by this many steps."""
    return O("Effect_TrainAnimal", steps=int(steps), **kw)


def bond_animal(**kw):
    return O("Effect_BondAnimal", **kw)


def prisoner(resistance=5.0, will=0.0, suppression=0.0, **kw):
    """Soften prisoners (resistance, will) and, with Ideology, raise slaves' suppression."""
    return O("Effect_Prisoner", resistance=float(resistance), will=float(will) if will else None, suppression=float(suppression) if suppression else None, **kw)


def clean(filth=True, snow=False, pollution=False, **kw):
    return O("Effect_Clean", filth=None if filth else False, snow=snow or None, pollution=pollution or None, **kw)


def charge(energy=200.0, fuel=0.0, **kw):
    """Add watt-days to batteries and a fraction of capacity to refuelable buildings in the area."""
    return O("Effect_Charge", energy=float(energy), fuel=float(fuel) if fuel else None, **kw)


def incident(options, points=0.0, descKey=None, **kw):
    """Fire one of these incidents (defNames, resolved at runtime; DLC ones are skipped when missing)."""
    return O("Effect_Incident", options=list(options), points=float(points) if points else None, descKey=descKey, **kw)


def goodwill(amount, mode="TargetFaction", **kw):
    return O("Effect_Goodwill", amount=int(amount), mode=_opt(mode, "TargetFaction"), **kw)


def tend(quality=0.8, **kw):
    return O("Effect_Tend", quality=float(quality), **kw)


def immunize(amount=0.25, **kw):
    return O("Effect_Immunize", amount=float(amount), **kw)


def passion(mode="AddMinor", skill=None, **kw):
    return O("Effect_Passion", mode=_opt(mode, "AddMinor"), skill=skill, **kw)


def trait(add=None, degree=0, remove=None, **kw):
    return O("Effect_Trait", add=add, degree=degree or None, remove=list(remove) if remove else None, **kw)


def gathering(kind="Party", **kw):
    return O("Effect_Gathering", kind=_opt(kind, "Party"), **kw)


def haul(maxItems=30, **kw):
    return O("Effect_Haul", maxItems=_opt(int(maxItems), 30), **kw)


def smooth(floors=True, walls=True, **kw):
    return O("Effect_Smooth", floors=None if floors else False, walls=None if walls else False, **kw)


def preserve(**kw):
    return O("Effect_Preserve", **kw)

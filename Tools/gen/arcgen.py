"""
Arcanum Ascendant content generator.

Classes, subclasses, spells and skill tree nodes are authored compactly in Python (see classes/*.py) and
emitted as RimWorld XML defs into Defs/Generated. An icon manifest (Tools/gen/out/icons.json) is written too;
Tools/art/make_textures.py renders every icon from it.

Run:  python3 Tools/gen/build.py
"""
from xml.sax.saxutils import escape
import json
import os

NS = "ArcanumAscendant."


class O:
    """An XML object node. cls (optional) becomes the Class="..." attribute."""

    def __init__(self, cls=None, **fields):
        self.cls = cls
        self.fields = fields


MOD_CLASSES = {"CompProperties_ArcaneSpell", "SpellExtension"}


def qualify(cls):
    if "." in cls:
        return cls
    if cls.startswith(("Effect_", "P_")) or cls in MOD_CLASSES:
        return NS + cls
    return cls


def fmt_scalar(v):
    if isinstance(v, bool):
        return "true" if v else "false"
    if isinstance(v, float):
        s = ("%.4f" % v).rstrip("0").rstrip(".")
        return s if s not in ("", "-0") else "0"
    if isinstance(v, tuple):
        return "(" + ", ".join(fmt_scalar(x) for x in v) + ")"
    return escape(str(v))


def to_xml(tag, value, ind=0):
    pad = "  " * ind
    if value is None:
        return ""
    if isinstance(value, O):
        attr = ' Class="%s"' % qualify(value.cls) if value.cls else ""
        inner = "".join(to_xml(k, v, ind + 1) for k, v in value.fields.items() if v is not None)
        if not inner:
            return "%s<%s%s />\n" % (pad, tag, attr)
        return "%s<%s%s>\n%s%s</%s>\n" % (pad, tag, attr, inner, pad, tag)
    if isinstance(value, list):
        if not value:
            return ""
        inner = "".join(to_xml("li", v, ind + 1) for v in value)
        return "%s<%s>\n%s%s</%s>\n" % (pad, tag, inner, pad, tag)
    if isinstance(value, dict):
        if not value:
            return ""
        inner = "".join(to_xml(k, v, ind + 1) for k, v in value.items())
        return "%s<%s>\n%s%s</%s>\n" % (pad, tag, inner, pad, tag)
    return "%s<%s>%s</%s>\n" % (pad, tag, fmt_scalar(value), tag)


def rgb(h):
    h = h.lstrip("#")
    return tuple(round(int(h[i:i + 2], 16) / 255.0, 3) for i in (0, 2, 4))


# --------------------------------------------------------------------------------------------
# Effects
# --------------------------------------------------------------------------------------------

def dmg(amount, damageDef=None, **kw):
    return O("Effect_Damage", amount=float(amount), damageDef=damageDef, **kw)


def heal(amount, **kw):
    return O("Effect_Heal", amount=float(amount), **kw)


def hediff(h, severity=1.0, duration=None, **kw):
    return O("Effect_Hediff", hediff=h, severity=float(severity), durationTicks=duration, **kw)


def stun(ticks, **kw):
    return O("Effect_Stun", ticks=int(ticks), **kw)


def knock(distance, pull=False, **kw):
    return O("Effect_Knockback", distance=float(distance), pull=pull or None, **kw)


def explode(amount, radius, damageDef=None, **kw):
    return O("Effect_Explosion", amount=float(amount), explosionRadius=float(radius), damageDef=damageDef, **kw)


def ignite(cellChance=0.3, size=0.6, targets=True, **kw):
    return O("Effect_Ignite", cellChance=cellChance, fireSize=size, igniteTargets=targets, **kw)


def extinguish(**kw):
    return O("Effect_Extinguish", **kw)


def shieldbreak(mechStun=0, **kw):
    return O("Effect_ShieldBreak", mechStunTicks=mechStun or None, **kw)


def ward(amount, **kw):
    return O("Effect_Ward", amount=float(amount), **kw)


def mana(amount=0, percent=0, **kw):
    return O("Effect_RestoreMana", amount=float(amount), percent=percent or None, **kw)


def cleanse(**kw):
    return O("Effect_Cleanse", **kw)


def mental(state, **kw):
    return O("Effect_MentalState", state=state, **kw)


def field(radius, duration, interval, pulse, **kw):
    return O("Effect_Field", fieldRadius=float(radius), durationTicks=int(duration), pulseInterval=int(interval), pulseEffects=pulse, **kw)


def summon(kind, count, duration, **kw):
    if isinstance(kind, list):
        return O("Effect_Summon", kindOptions=kind, count=count, durationTicks=int(duration), **kw)
    return O("Effect_Summon", kind=kind, count=count, durationTicks=int(duration), **kw)


def raise_dead(maxCount, duration, **kw):
    return O("Effect_RaiseDead", maxCount=maxCount, durationTicks=int(duration), **kw)


def spawn(thing, count=1, **kw):
    return O("Effect_SpawnThing", thing=thing, count=count, **kw)


def conjure(thing, lo, hi, **kw):
    return O("Effect_SpawnItems", thing=thing, count="%d~%d" % (lo, hi), **kw)


def teleport(mode="CasterToTarget", **kw):
    return O("Effect_Teleport", mode=mode, **kw)


def resurrect(**kw):
    return O("Effect_Resurrect", **kw)


def dominate(duration, **kw):
    return O("Effect_Dominate", durationTicks=int(duration), **kw)


def polymorph(into, duration, **kw):
    if isinstance(into, list):
        return O("Effect_Polymorph", options=into, durationTicks=int(duration), **kw)
    return O("Effect_Polymorph", into=into, durationTicks=int(duration), **kw)


def condition(cond, duration, remove=None, **kw):
    return O("Effect_GameCondition", condition=cond, durationTicks=int(duration), removeConditions=remove, **kw)


def weather(w=None, options=None, **kw):
    return O("Effect_Weather", weather=w, options=options, **kw)


def lightning(**kw):
    return O("Effect_LightningStrike", **kw)


def need(n, amount, **kw):
    return O("Effect_Need", need=n, amount=amount, **kw)


def thought(t, **kw):
    return O("Effect_Thought", thought=t, **kw)


def inspire(insp=None, **kw):
    return O("Effect_Inspiration", inspiration=insp, **kw)


def skillxp(xp, skill=None, **kw):
    return O("Effect_SkillXp", skill=skill, xp=float(xp), **kw)


def grow(amount, **kw):
    return O("Effect_PlantGrowth", growth=amount, **kw)


def repair(amount, **kw):
    return O("Effect_Repair", amount=float(amount), **kw)


def transmute(to, lo, hi, **kw):
    return O("Effect_Transmute", to=to, countPerUnit="%d~%d" % (lo, hi), **kw)


def heat(amount, **kw):
    return O("Effect_Temperature", heat=float(amount), **kw)


def mine(**kw):
    return O("Effect_Mine", **kw)


def terraform(to, fromOnly=None, **kw):
    return O("Effect_Terraform", to=to, fromOnly=fromOnly, **kw)


def recruit(animalsOnly=False, **kw):
    return O("Effect_Recruit", animalsOnly=animalsOnly or None, **kw)


def reveal(radius, **kw):
    return O("Effect_Reveal", revealRadius=float(radius), **kw)


def age(years, **kw):
    return O("Effect_Age", years=float(years), **kw)


def rewind(window, **kw):
    return O("Effect_Rewind", windowTicks=int(window), **kw)


def cdreset(fraction=1.0, **kw):
    return O("Effect_CooldownReset", fraction=fraction, **kw)


def random_of(options, perTarget=True, **kw):
    return O("Effect_Random", options=options, perTarget=perTarget, **kw)


# --------------------------------------------------------------------------------------------
# VFX
# --------------------------------------------------------------------------------------------

def vfx(type_, **kw):
    if "color" in kw and isinstance(kw["color"], str):
        kw["color"] = rgb(kw["color"]) + (1.0,)
    return O(None, type=type_, **kw)


FLECK = {
    "spark": "AA_Fleck_Spark", "glow": "AA_Fleck_Glow", "ember": "AA_Fleck_Ember", "mote": "AA_Fleck_Mote",
    "smoke": "AA_Fleck_Smoke", "shard": "AA_Fleck_Shard", "leaf": "AA_Fleck_Leaf", "note": "AA_Fleck_Note",
    "rune": "AA_Fleck_Rune", "ring": "AA_Fleck_Ring", "snow": "AA_Fleck_Snow", "bone": "AA_Fleck_Bone",
    "feather": "AA_Fleck_Feather", "blood": "AA_Fleck_Blood", "gear": "AA_Fleck_Gear", "star": "AA_Fleck_Star",
    "skull": "AA_Fleck_Skull", "petal": "AA_Fleck_Petal", "clock": "AA_Fleck_Clock", "bolt": "AA_Fleck_Bolt",
}


def burst(fleck="spark", count=12, speed=2.5, scale=0.6, byRadius=False, **kw):
    return vfx("Particles", fleck=FLECK[fleck], count=count, speed=speed, scale=scale, scaleByRadius=byRadius or None, **kw)


def glow(scale=2.0, end=None, ticks=24, **kw):
    return vfx("Glow", scale=scale, scaleEnd=end if end is not None else scale * 1.4, ticks=ticks, **kw)


def flash(scale=3.0, ticks=12, **kw):
    return vfx("Flash", scale=scale, scaleEnd=scale * 1.2, ticks=ticks, **kw)


def ring(start=0.4, end=1.0, ticks=26, byRadius=True, **kw):
    return vfx("Shockwave", scale=start, scaleEnd=end, ticks=ticks, scaleByRadius=byRadius or None, **kw)


def rune(scale=1.0, ticks=60, byRadius=True, tex=None, **kw):
    return vfx("Rune", scale=scale, scaleEnd=scale * 1.05, ticks=ticks, scaleByRadius=byRadius or None, tex=tex, **kw)


def pillar(width=1.5, ticks=30, **kw):
    return vfx("Pillar", scale=width, ticks=ticks, **kw)


def bolt_down(width=1.2, ticks=14, **kw):
    return vfx("Lightning", scale=width, ticks=ticks, **kw)


def smoke(count=4, scale=1.0, **kw):
    return vfx("Smoke", count=count, scale=scale, speed=1.0, **kw)


def spiral(fleck="mote", count=12, speed=2.0, scale=0.5, **kw):
    return vfx("Spiral", fleck=FLECK[fleck], count=count, speed=speed, scale=scale, **kw)


def hexshield(scale=2.2, ticks=40, **kw):
    return vfx("Hex", scale=scale, scaleEnd=scale * 0.9, ticks=ticks, **kw)


def orbit(tex="ArcanumAscendant/Vfx/Spark", count=5, radius=0.9, ticks=90, speed=6.0, **kw):
    return vfx("Orbit", tex=tex, count=count, scale=radius, ticks=ticks, speed=speed, attach=True, **kw)


# --------------------------------------------------------------------------------------------
# Spells
# --------------------------------------------------------------------------------------------

TARGETS = {
    "enemy": dict(canTargetPawns=True, canTargetLocations=False, canTargetBuildings=False, canTargetAnimals=True, canTargetMechs=True, canTargetHumans=True),
    "enemy_area": dict(canTargetPawns=True, canTargetLocations=True, canTargetBuildings=True, canTargetAnimals=True, canTargetMechs=True, canTargetHumans=True),
    "ally": dict(canTargetPawns=True, canTargetSelf=True, canTargetLocations=False, canTargetBuildings=False, canTargetAnimals=True, canTargetMechs=True, canTargetHumans=True),
    "ally_area": dict(canTargetPawns=True, canTargetSelf=True, canTargetLocations=True, canTargetBuildings=False, canTargetAnimals=True, canTargetMechs=True, canTargetHumans=True),
    "cell": dict(canTargetPawns=True, canTargetLocations=True, canTargetBuildings=True, canTargetItems=True, canTargetSelf=True, canTargetAnimals=True, canTargetMechs=True, canTargetHumans=True),
    "corpse": dict(canTargetPawns=False, canTargetLocations=True, canTargetItems=True, canTargetCorpses=True, canTargetBuildings=False),
    "any_pawn": dict(canTargetPawns=True, canTargetSelf=True, canTargetLocations=False, canTargetAnimals=True, canTargetMechs=True, canTargetHumans=True),
}

ROLE_UNDRAFTED = {"Heal", "Buff", "Utility", "Summon", "Mobility"}


class Spell:
    def __init__(self, sid, label, desc, *, icon, mana, cd, range=24.0, warmup=0.8, tags=(), role="Attack",
                 target="enemy", delivery="Instant", radius=0.0, effects=(), caster_effects=(), affects=None,
                 cast_vfx=None, impact_vfx=None, caster_vfx=None, school=None, sound_cast=None, sound_impact=None,
                 shake=0.0, ultimate=False, quirky=False, flavor=None, tier=1, xp=1.0, autocast=None,
                 ai_min=0.0, ai_hp=0.6, los=True, health_cost=0.0, hostile=None, **delivery_kw):
        self.sid = sid
        self.defName = "AA_Spell_" + sid
        self.label = label
        self.desc = desc
        self.icon = icon
        self.mana = mana
        self.cd = cd
        self.range = range
        self.warmup = warmup
        self.tags = list(tags)
        self.role = role
        self.target = target
        self.delivery = delivery
        self.radius = radius
        self.effects = list(effects)
        self.caster_effects = list(caster_effects)
        self.affects = affects
        self.cast_vfx = cast_vfx
        self.impact_vfx = impact_vfx
        self.caster_vfx = caster_vfx
        self.school = school
        self.sound_cast = sound_cast
        self.sound_impact = sound_impact
        self.shake = shake
        self.ultimate = ultimate
        self.quirky = quirky
        self.flavor = flavor
        self.tier = tier
        self.xp = xp
        self.autocast = autocast
        self.ai_min = ai_min
        self.ai_hp = ai_hp
        self.los = los
        self.health_cost = health_cost
        self.hostile = hostile
        self.delivery_kw = delivery_kw
        self.owner = None

    def xml(self):
        sc = self.school
        tp = TARGETS[self.target] if self.target != "self" else None
        verb = {
            "verbClass": NS + "Verb_ArcaneCast",
            "range": float(self.range) if self.target != "self" else 0.0,
            "warmupTime": float(self.warmup),
            "requireLineOfSight": self.los if self.target != "self" else False,
            "drawAimPie": self.target != "self",
        }
        if self.target == "self":
            verb["targetable"] = False
            verb["nonInterruptingSelfCast"] = self.warmup <= 0.01
            verb["targetParams"] = O(None, canTargetSelf=True)
        else:
            verb["targetParams"] = O(None, **tp)
        hostile = self.hostile if self.hostile is not None else self.role in ("Attack", "Control")
        undrafted = self.role in ROLE_UNDRAFTED or self.quirky
        affects = self.affects
        if affects is None:
            affects = {"Heal": "Allies", "Buff": "Allies"}.get(self.role, "Enemies")
            if self.target == "self" and self.role in ("Heal", "Buff", "Utility", "Mobility") and self.radius <= 0:
                affects = "Self"
        comp = O("CompProperties_ArcaneSpell",
                 delivery=self.delivery,
                 affects=affects,
                 radius=float(self.radius) if self.radius else None,
                 effects=self.effects or None,
                 casterEffects=self.caster_effects or None,
                 color=sc.c1 + (1.0,), color2=sc.c2 + (1.0,),
                 castVfx=self.cast_vfx if self.cast_vfx is not None else sc.cast_vfx(),
                 impactVfx=self.impact_vfx if self.impact_vfx is not None else sc.impact_vfx(self.radius),
                 casterVfx=self.caster_vfx,
                 castSound=self.sound_cast or sc.sound_cast,
                 impactSound=self.sound_impact or sc.sound_impact,
                 shake=float(self.shake) if self.shake else None,
                 **self.delivery_kw)
        if self.delivery in ("Projectile", "Meteor", "Barrage") and "projectileTex" not in self.delivery_kw:
            comp.fields["projectileTex"] = sc.projectile
        ext = O(NS + "SpellExtension",
                manaCost=float(self.mana),
                healthCost=float(self.health_cost) if self.health_cost else None,
                tags=self.tags,
                tier=self.tier,
                role=self.role,
                ultimate=self.ultimate or None,
                quirky=self.quirky or None,
                xpFactor=self.xp if self.xp != 1.0 else None,
                autocastByDefault=(self.autocast if self.autocast is not None else self.role in ("Attack", "Heal", "Control") and not self.quirky) or None,
                aiHealthThreshold=self.ai_hp if self.role == "Heal" else None,
                aiMinEnemiesInRadius=float(self.ai_min) if self.ai_min else None,
                aiAllowedUndrafted=(self.role == "Heal") or None,
                flavor=self.flavor)
        fields = {
            "defName": self.defName,
            "label": self.label,
            "description": self.desc,
            "iconPath": "ArcanumAscendant/Spells/" + self.sid,
            "cooldownTicksRange": int(self.cd * 60),
            "hostile": hostile,
            "casterMustBeCapableOfViolence": hostile,
            "displayGizmoWhileUndrafted": undrafted,
            "verbProperties": O(None, **verb),
            "modExtensions": [ext],
            "comps": [comp],
        }
        if sc.warmup_sound:
            fields["warmupStartSound"] = sc.warmup_sound
        if self.target == "self":
            fields["targetRequired"] = False
        body = "".join(to_xml(k, v, 2) for k, v in fields.items() if v is not None)
        return '  <AbilityDef ParentName="AA_SpellBase">\n%s  </AbilityDef>\n' % body


# --------------------------------------------------------------------------------------------
# Schools (colors, sounds, default visuals)
# --------------------------------------------------------------------------------------------

class School:
    def __init__(self, name, c1, c2, fleck="spark", fleck2="mote", projectile="ArcanumAscendant/Vfx/Orb", sound=None, warmup_sound="AA_Charge"):
        self.name = name
        self.c1 = rgb(c1)
        self.c2 = rgb(c2)
        self.hex1 = c1
        self.hex2 = c2
        self.fleck = fleck
        self.fleck2 = fleck2
        self.projectile = projectile
        snd = sound or name
        self.sound_cast = "AA_Cast_" + snd
        self.sound_impact = "AA_Impact_" + snd
        self.warmup_sound = warmup_sound

    def cast_vfx(self):
        return [glow(1.6, 2.2, 18), burst(self.fleck2, 6, 1.4, 0.5)]

    def impact_vfx(self, radius):
        if radius and radius > 0.5:
            return [flash(2.5), ring(0.3, 1.1, 28), burst(self.fleck, 10, 3.0, 0.7, byRadius=True), burst(self.fleck2, 6, 1.5, 0.8, byRadius=True)]
        return [flash(1.8), burst(self.fleck, 8, 2.2, 0.55), glow(1.2, 2.0, 20, useColor2=True)]


# --------------------------------------------------------------------------------------------
# Nodes
# --------------------------------------------------------------------------------------------

class Node:
    def __init__(self, nid, label, pos, *, kind="Minor", ranks=1, cost=1, pre=(), all_pre=False, level=None,
                 attr=None, attr_val=None, excl=None, spell=None, stats=None, pct=None, caps=None, mods=(), passives=(),
                 desc=None, flavor=None, icon=None):
        self.nid = nid
        self.label = label
        self.x, self.y = pos
        self.kind = kind
        self.ranks = ranks
        self.cost = cost
        self.pre = list(pre)
        self.all_pre = all_pre
        self.level = level
        self.attr = attr
        self.attr_val = attr_val
        self.excl = excl
        self.spell = spell
        self.stats = stats or {}
        self.pct = pct or {}
        self.caps = caps or {}
        self.mods = list(mods)
        self.passives = list(passives)
        self.desc = desc
        self.flavor = flavor
        self.icon = icon
        self.tree = None
        self.defName = None


def mod(tag=None, spells=None, **kw):
    return O(None, tag=tag, spells=spells, **{k: (float(v) if isinstance(v, (int, float)) and not isinstance(v, bool) and k != "extraTargets" else v) for k, v in kw.items()})


def P(cls, **kw):
    return O(cls, **kw)


ATTR = {"POT": "AA_Potency", "FOC": "AA_Focus", "CEL": "AA_Celerity", "RES": "AA_Resilience", "ATT": "AA_Attunement"}


class ClassSpec:
    def __init__(self, cid, label, *, color, color2, icon, tagline, desc, playstyle, lore, primary, secondary, tags,
                 starting=(), innate=None, order=0, parent=None, school=None, width=7, height=9, unlock=10):
        self.cid = cid
        self.defName = "AA_Class_" + cid
        self.label = label
        self.color = rgb(color)
        self.color2 = rgb(color2)
        self.hex1 = color
        self.hex2 = color2
        self.icon = icon
        self.tagline = tagline
        self.desc = desc
        self.playstyle = playstyle
        self.lore = lore
        self.primary = primary
        self.secondary = secondary
        self.tags = tags
        self.starting = list(starting)
        self.innate = innate or {}
        self.order = order
        self.parent = parent
        self.school = school
        self.width = width
        self.height = height
        self.unlock = unlock
        self.nodes = []
        self.spells = []

    def add_spells(self, *spells):
        for s in spells:
            s.owner = self
            if s.school is None:
                s.school = self.school
            self.spells.append(s)

    def add_nodes(self, *nodes):
        for n in nodes:
            n.tree = self
            n.defName = "AA_Node_%s_%s" % (self.cid, n.nid)
            self.nodes.append(n)

    def xml(self):
        f = {
            "defName": self.defName,
            "label": self.label,
            "description": self.desc,
            "iconPath": "ArcanumAscendant/Classes/" + self.cid,
            "color": self.color + (1.0,),
            "colorSecondary": self.color2 + (1.0,),
            "parentClass": self.parent.defName if self.parent else None,
            "tagline": self.tagline,
            "lore": self.lore,
            "playstyle": self.playstyle,
            "schoolTags": self.tags,
            "primaryAttribute": ATTR[self.primary],
            "secondaryAttribute": ATTR[self.secondary],
            "subclassUnlockLevel": self.unlock if not self.parent else None,
            "innateStatOffsets": {k: float(v) for k, v in self.innate.items()} or None,
            "startingSpells": ["AA_Spell_" + s for s in self.starting] or None,
            "treeWidth": self.width,
            "treeHeight": self.height,
            "order": self.order,
        }
        body = "".join(to_xml(k, v, 2) for k, v in f.items() if v is not None)
        return "  <%sArcaneClassDef>\n%s  </%sArcaneClassDef>\n" % (NS, body, NS)


def gate_for(cls, node):
    """Default level / attribute requirement by row."""
    if cls.parent is None:
        rows = {0: (1, None), 1: (1, None), 2: (3, None), 3: (6, 6), 4: (9, 6), 5: (13, 10), 6: (17, 10), 7: (22, 14), 8: (30, 18)}
    else:
        rows = {0: (10, None), 1: (12, None), 2: (15, 8), 3: (18, 10), 4: (22, 12), 5: (26, 14), 6: (32, 16), 7: (40, 18)}
    return rows.get(node.y, (1, None))


def node_xml(cls, n, by_id):
    lvl, aval = gate_for(cls, n)
    level = n.level if n.level is not None else lvl
    attr = n.attr or (cls.primary if aval else None)
    attr_val = n.attr_val if n.attr_val is not None else aval
    pres = []
    for p in n.pre:
        if p not in by_id:
            raise KeyError("Unknown prerequisite %s for node %s in %s" % (p, n.nid, cls.cid))
        pres.append(by_id[p].defName)
    f = {
        "defName": n.defName,
        "label": n.label,
        "description": n.desc,
        "tree": cls.defName,
        "kind": n.kind,
        "x": n.x,
        "y": n.y,
        "maxRank": n.ranks if n.ranks != 1 else None,
        "costPerRank": n.cost if n.cost != 1 else None,
        "requiredLevel": level if level > 1 else None,
        "requiredAttribute": ATTR[attr] if attr and attr_val else None,
        "requiredAttributeValue": attr_val if attr and attr_val else None,
        "prerequisites": pres or None,
        "requireAllPrerequisites": n.all_pre or None,
        "exclusiveGroup": n.excl,
        "unlockSpell": ("AA_Spell_" + n.spell) if n.spell else None,
        "statOffsetsPerRank": {k: float(v) for k, v in n.stats.items()} or None,
        "statPercentPerRank": {k: float(v) for k, v in n.pct.items()} or None,
        "capacityOffsetsPerRank": [O(None, capacity=k, offset=float(v)) for k, v in n.caps.items()] or None,
        "spellMods": n.mods or None,
        "passives": n.passives or None,
        "iconPath": ("ArcanumAscendant/Nodes/%s_%s" % (cls.cid, n.nid)) if not n.spell else None,
        "flavor": n.flavor,
    }
    body = "".join(to_xml(k, v, 2) for k, v in f.items() if v is not None)
    return "  <%sArcaneNodeDef>\n%s  </%sArcaneNodeDef>\n" % (NS, body, NS)


def write_defs(path, chunks):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as fh:
        fh.write('<?xml version="1.0" encoding="utf-8"?>\n<!-- Generated by Tools/gen/build.py. Edit the Python sources, not this file. -->\n<Defs>\n')
        for c in chunks:
            fh.write(c)
        fh.write("</Defs>\n")


def write_json(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=1)

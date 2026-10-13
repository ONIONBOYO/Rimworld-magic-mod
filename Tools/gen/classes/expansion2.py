"""
Second expansion: spells built on the newer deliveries.

  Missiles  - homing bolts launched in a fan that curve in on their targets
  Orbitals  - orbs that circle the caster and strike whatever they touch
  Channel   - a beam the caster sustains, pulsing its effects until interrupted
  Delayed   - a warning circle that detonates after a delay (and can stick to a target)

Several are written as primers for elemental reactions (Glacial Spike freezes for Shatter, Arc Missiles shock for
Overload, Sunstrike marks for Twilight, and so on).
"""
from classes.common import *  # noqa: F401,F403
from classes import c_pyromancer as PY, c_tempest as TE, c_necromancer as NE, c_paladin as PA, c_chronomancer as CH
from classes import c_druid as DR, c_runesmith as RU, c_shadow as SH, c_bard as BA


def spell_node(sid, pos, pre):
    return Node(sid, None, pos, kind="Spell", spell=sid, pre=pre)


def tex(name):
    return "ArcanumAscendant/Vfx/" + name


def missiles(sid, label, desc, icon, *, mana, cd, count, speed, projectile, scale, effects, tags, range=26, spread=110.0, tier=2, radius=0.0, **kw):
    return Spell(sid, label, desc, icon=icon, mana=mana, cd=cd, range=range, warmup=0.5, tags=tags, delivery="Missiles", projectileCount=count,
                 projectileSpeed=float(speed), projectileTex=tex(projectile), projectileScale=float(scale), missileSpread=float(spread),
                 multiTargetRadius=7.0, tier=tier, radius=radius, effects=effects, **kw)


def orbitals(sid, label, desc, icon, *, mana, cd, count, orb_radius, duration, speed, projectile, scale, effects, tags, hit_cd=40, hit_radius=0.9, tier=2,
             role="Attack", **kw):
    return Spell(sid, label, desc, icon=icon, mana=mana, cd=cd, warmup=0.4, tags=tags, role=role, target="self", delivery="Orbitals",
                 orbitalCount=count, orbitalRadius=float(orb_radius), orbitalDuration=int(duration), orbitalSpeed=float(speed),
                 orbitalHitRadius=float(hit_radius), orbitalHitCooldown=int(hit_cd), projectileTex=tex(projectile), projectileScale=float(scale),
                 radius=float(orb_radius) + 1.0, ai_min=1 if role == "Attack" else 0, tier=tier, effects=effects,
                 affects="Allies" if role == "Heal" else None, **kw)


def channel(sid, label, desc, icon, *, mana, cd, ticks, interval, width, effects, tags, range=18, tier=3, radius=0.0, **kw):
    return Spell(sid, label, desc, icon=icon, mana=mana, cd=cd, range=range, warmup=0.6, tags=tags, delivery="Channel", channelTicks=int(ticks),
                 channelInterval=int(interval), beamWidth=float(width), tier=tier, radius=radius, effects=effects, **kw)


def delayed(sid, label, desc, icon, *, mana, cd, delay, radius, strike, effects, tags, count=1, spread=0.0, stagger=12, range=28, tier=2,
            target="enemy_area", **kw):
    return Spell(sid, label, desc, icon=icon, mana=mana, cd=cd, range=range, warmup=0.6, tags=tags, target=target, delivery="Delayed",
                 delayTicks=int(delay), delayedCount=count, delayedSpread=float(spread), delayedStagger=int(stagger), strikeStyle=strike,
                 radius=float(radius), tier=tier, effects=effects, **kw)


def by_cid(root, subs):
    return {t.cid: t for t in [root] + subs}


def pyromancer(t, extra):
    t["Pyromancer"].add_spells(orbitals("OrbitingSuns", "orbiting suns",
        "Three miniature suns wheel around you for twenty-five seconds, scorching and igniting anything that comes close.", ("sun", "ring"),
        mana=40, cd=45, count=3, orb_radius=1.7, duration=1500, speed=4.5, projectile="Fireball", scale=0.7, tags=["Fire"],
        effects=[dmg(12, "Flame", armorPen=0.2), hediff("AA_Burning", 1.0, stack=True, maxSeverity=5.0)]))
    t["Pyromancer"].add_nodes(spell_node("OrbitingSuns", (2, 5), ["LivingBomb"]))
    t["InfernoLord"].add_spells(channel("InfernalTorrent", "infernal torrent",
        "Pour a continuous torrent of hellfire at a target for five seconds. Everything around the point of contact burns.", ("flame", "wave"),
        mana=50, cd=30, ticks=300, interval=15, width=1.1, range=14, radius=1.2, tags=["Fire"],
        effects=[dmg(9, "Flame", armorPen=0.3), hediff("AA_Burning", 0.5, stack=True, maxSeverity=5.0), ignite(0.2)]))
    t["InfernoLord"].add_nodes(spell_node("InfernalTorrent", (6, 3), ["Brimstone"]))
    t["AshenPhoenix"].add_spells(orbitals("PhoenixFeathers", "phoenix feathers",
        "Five burning feathers circle you for half a minute. Each time one brushes an ally it mends their wounds.", ("feather", "heart"),
        mana=40, cd=60, count=5, orb_radius=1.6, duration=1800, speed=4.0, projectile="Feather", scale=0.5, tags=["Fire", "Healing"], role="Heal",
        hit_cd=120, effects=[heal(8)]))
    t["AshenPhoenix"].add_nodes(spell_node("PhoenixFeathers", (0, 3), ["Rekindle"]))
    t["Forgeheart"].add_spells(delayed("Hammerfall", "hammerfall",
        "Five glowing forge-hammers slam down in turn around the target. Each blow crushes and staggers. Force like this shatters frozen enemies.",
        ("hammer", "meteor"), mana=55, cd=28, delay=50, radius=1.3, strike="Eruption", count=5, spread=3.5, stagger=10, tags=["Fire"], tier=3, shake=0.8,
        effects=[dmg(30, "AA_Force", armorPen=0.3), stun(20)]))
    t["Forgeheart"].add_nodes(spell_node("Hammerfall", (1, 3), ["Slagbreaker"]))


def tempest(t, extra):
    t["Tempest"].add_spells(delayed("GlacialSpike", "glacial spike",
        "A spike of ice erupts from the ground a moment after you mark the spot, freezing whatever stands there. Frozen enemies shatter when struck "
        "with fire or force.", ("shard", "snowflake"), mana=28, cd=10, delay=60, radius=1.2, strike="Eruption", tags=["Frost"], school=TE.FROST,
        effects=[dmg(30, "AA_Frost", armorPen=0.3), hediff("AA_Frozen", 1.0, duration=180), stun(30)]))
    t["Tempest"].add_nodes(spell_node("GlacialSpike", (2, 3), ["Overcurrent"]))
    t["Stormcaller"].add_spells(missiles("ArcMissiles", "arc missiles",
        "Six crackling orbs of lightning that seek out enemies. They leave targets shocked; fire turns a shocked target into an explosion.", ("bolt", "arrow"),
        mana=32, cd=9, count=6, speed=20, projectile="Orb", scale=0.45, tags=["Storm"],
        effects=[dmg(10, "AA_Lightning", vsMechanoids=1.5), hediff("AA_Shocked", 1.0)]))
    t["Stormcaller"].add_nodes(spell_node("ArcMissiles", (5, 3), ["Blackout"]))
    t["Frostwarden"].add_spells(orbitals("IceHalo", "ice halo",
        "Six blades of ice orbit you for twenty-five seconds, slicing and chilling anyone who gets close.", ("shard", "ring"),
        mana=40, cd=45, count=6, orb_radius=1.8, duration=1500, speed=5, projectile="Shard", scale=0.5, tags=["Frost"], hit_cd=45,
        effects=[dmg(8, "AA_Frost"), hediff("AA_Chilled", 1.0, stack=True, maxSeverity=3.0)]))
    t["Frostwarden"].add_nodes(spell_node("IceHalo", (1, 4), ["Cryostasis"]))
    t["Skybreaker"].add_spells(orbitals("CycloneBlades", "cyclone blades",
        "Three blades of compressed wind scream around you, cutting and hurling back whatever they hit.", ("tornado", "sword"),
        mana=38, cd=40, count=3, orb_radius=2.2, duration=1200, speed=8, projectile="Shard", scale=0.6, tags=["Storm"], hit_radius=1.0,
        effects=[dmg(12, "Cut", armorPen=0.25), knock(1.5)]))
    t["Skybreaker"].add_nodes(spell_node("CycloneBlades", (1, 4), ["ClearSkies"]))


def necromancer(t, extra):
    t["Necromancer"].add_spells(missiles("SoulSwarm", "soul swarm",
        "Release six hungry souls that hunt down enemies and gnaw at their life, feeding a share of it back to you.", ("skull", "swirl"),
        mana=34, cd=14, count=6, speed=14, projectile="Skull", scale=0.55, spread=140, tags=["Necromancy"], tier=3,
        effects=[dmg(9, "AA_Necrotic", healCasterFraction=0.25)]))
    t["Necromancer"].add_nodes(spell_node("SoulSwarm", (4, 5), ["Reaper"]))
    t["Lich"].add_spells(channel("DrainSoul", "drain soul",
        "Lock a ribbon of necrotic power onto a target and drink its soul for five seconds. You heal for most of the damage.", ("skull", "drop"),
        mana=40, cd=20, ticks=300, interval=20, width=0.8, range=18, tags=["Necromancy"],
        effects=[dmg(10, "AA_Necrotic", armorPen=0.5, healCasterFraction=0.6)]))
    t["Lich"].add_nodes(spell_node("DrainSoul", (6, 3), ["SoulReap"]))
    t["Bonelord"].add_spells(missiles("BoneShards", "bone shards",
        "Hurl five jagged shards of bone that curve in on enemies and leave them bleeding.", ("bone", "arrow"),
        mana=30, cd=12, count=5, speed=16, projectile="Bone", scale=0.5, tags=["Necromancy"],
        effects=[dmg(12, "Cut", armorPen=0.3), hediff("AA_Bleeding", 1.0, chance=0.4, stack=True, maxSeverity=5.0)]))
    t["Bonelord"].add_nodes(spell_node("BoneShards", (1, 4), ["BonePrison"]))


def paladin(t, extra):
    t["Paladin"].add_spells(delayed("Sunstrike", "sunstrike",
        "Mark the ground. A heartbeat later a column of sunfire slams down: heavy radiant damage, a stagger, and a mark of judgment that shadow magic "
        "can detonate.", ("sun", "pillar"), mana=45, cd=22, delay=75, radius=2.2, strike="Pillar", tags=["Holy"], tier=3, shake=1.0,
        effects=[dmg(55, "AA_Holy", armorPen=0.5), hediff("AA_Marked", 1.0), stun(45)]))
    t["Paladin"].add_nodes(spell_node("Sunstrike", (2, 3), ["Consecration"]))
    t["Cleric"].add_spells(orbitals("GuardianSpirits", "guardian spirits",
        "Three guardian spirits circle you for forty seconds. Whenever one passes an ally it heals them and lifts poisons and sickness.", ("wing", "heart"),
        mana=50, cd=75, count=3, orb_radius=1.6, duration=2400, speed=3.5, projectile="Star", scale=0.55, tags=["Holy", "Healing"], role="Heal", hit_cd=150,
        effects=[heal(10), cleanse()]))
    t["Cleric"].add_nodes(spell_node("GuardianSpirits", (0, 3), ["Rejuvenate"]))
    t["Inquisitor"].add_spells(channel("SearingRay", "searing ray",
        "Focus a ray of holy fire on a heretic for four seconds. It burns through armor and brands them for judgment.", ("eye", "sun"),
        mana=45, cd=25, ticks=240, interval=15, width=0.9, range=20, tags=["Holy"],
        effects=[dmg(10, "AA_Holy", armorPen=0.4), hediff("AA_Marked", 1.0)]))
    t["Inquisitor"].add_nodes(spell_node("SearingRay", (1, 4), ["Banishment"]))


def chronomancer(t, extra):
    t["Chronomancer"].add_spells(delayed("TemporalBomb", "temporal bomb",
        "Plant a knot of stopped time on an enemy. It follows them for three seconds, then releases every moment it held at once: heavy damage, a long "
        "stun and lingering slowness. Void magic fractures slowed targets.", ("hourglass", "burst"),
        mana=40, cd=25, delay=180, radius=2.5, strike="Pillar", tags=["Time"], target="enemy", tier=3, shake=1.0,
        effects=[dmg(60, "AA_Temporal"), stun(120), hediff("AA_Slowed", 1.0)]))
    t["Chronomancer"].add_nodes(spell_node("TemporalBomb", (1, 6), ["Dilation"]))
    t["Voidwalker"].add_spells(channel("Disintegrate", "disintegrate",
        "Focus a beam of pure nothing on a target for six seconds. It ignores nearly all armor and erases a little of the target's being with every pulse.",
        ("portal", "burst"), mana=70, cd=45, ticks=360, interval=20, width=1.2, range=22, tags=["Void"], tier=4,
        effects=[dmg(14, "AA_Void", armorPen=0.9, lethalPercent=0.01), hediff("AA_Voidtouched", 1.0)]))
    t["Voidwalker"].add_nodes(spell_node("Disintegrate", (6, 3), ["Ghostwalk"]))


def druid(t, extra):
    t["Druid"].add_spells(missiles("HornetSwarm", "hornet swarm",
        "Loose a swarm of eight giant hornets. They chase down enemies and sting them with venom.", ("bug", "thorn"),
        mana=30, cd=12, count=8, speed=12, projectile="Thorn", scale=0.35, spread=150, tags=["Nature"],
        effects=[dmg(5, "AA_Thorn"), hediff("AA_Poisoned", 0.5, stack=True, maxSeverity=4.0)]))
    t["Druid"].add_nodes(spell_node("HornetSwarm", (4, 5), ["Harvestmoon"]))


def runesmith(t, extra):
    t["Runesmith"].add_spells(missiles("SeekerGlyphs", "seeker glyphs",
        "Four burning glyphs peel off your hand and hunt their targets, cracking armor where they strike.", ("rune", "arrow"),
        mana=30, cd=10, count=4, speed=18, projectile="RuneBolt", scale=0.6, tags=["Rune"],
        effects=[dmg(16, "AA_Rune", armorPen=0.4), hediff("AA_ArmorShred", 1.0, stack=True, maxSeverity=4.0)]))
    t["Runesmith"].add_nodes(spell_node("SeekerGlyphs", (2, 5), ["Artificer"]))
    t["Golemancer"].add_spells(orbitals("DroneSwarm", "drone swarm",
        "Four runic drones whirl around you for thirty seconds, grinding into anything that comes near and sundering its armor.", ("gear", "ring"),
        mana=40, cd=50, count=4, orb_radius=2.0, duration=1800, speed=5.5, projectile="Gear", scale=0.5, tags=["Rune"],
        effects=[dmg(9, "AA_Rune"), hediff("AA_ArmorShred", 1.0, stack=True, maxSeverity=4.0)]))
    t["Golemancer"].add_nodes(spell_node("DroneSwarm", (1, 4), ["MaintenanceProtocol"]))
    t["Spellforger"].add_spells(missiles("MagicMissile", "magic missile",
        "The oldest spell in the book, perfected: five darts of pure force that never miss and ignore armor entirely.", ("orb", "arrow"),
        mana=24, cd=6, count=5, speed=22, projectile="Orb", scale=0.4, spread=90, tags=["Rune"], tier=1,
        effects=[dmg(11, trueDamage=True)]))
    t["Spellforger"].add_nodes(spell_node("MagicMissile", (0, 3), ["MasterworkRune"]))


def shadow(t, extra):
    t["Shadowmancer"].add_spells(orbitals("ShadowBlades", "shadow blades",
        "Four knives of solid shadow circle you for twenty seconds, slashing and opening wounds on everyone who comes close.", ("dagger", "ring"),
        mana=36, cd=40, count=4, orb_radius=1.4, duration=1200, speed=7, projectile="Dagger", scale=0.55, tags=["Shadow"], hit_radius=0.8, hit_cd=30,
        effects=[dmg(10, "AA_Shadow", armorPen=0.4), hediff("AA_Bleeding", 1.0, stack=True, maxSeverity=5.0)]))
    t["Shadowmancer"].add_nodes(spell_node("ShadowBlades", (2, 5), ["Assassinate"]))
    t["Assassin"].add_spells(orbitals("WhirlingDaggers", "whirling daggers",
        "Five poisoned daggers spin around you for fifteen seconds. Walk through a crowd and let them do the work.", ("dagger", "swirl"),
        mana=30, cd=30, count=5, orb_radius=1.2, duration=900, speed=9, projectile="Dagger", scale=0.5, tags=["Shadow"], hit_radius=0.8, hit_cd=25,
        effects=[dmg(9, "Cut", armorPen=0.5), hediff("AA_Poisoned", 0.5, stack=True, maxSeverity=4.0)]))
    t["Assassin"].add_nodes(spell_node("WhirlingDaggers", (0, 3), ["ShadowDance"]))
    t["Hemomancer"].add_spells(channel("BloodSiphon", "blood siphon",
        "Draw a stream of blood out of a target for five seconds. Their wounds keep bleeding and you heal for half of everything you take.", ("drop", "chain"),
        mana=30, cd=20, ticks=300, interval=20, width=0.7, range=16, tags=["Blood"],
        effects=[dmg(9, "AA_Blood", armorPen=0.3, healCasterFraction=0.5), hediff("AA_Bleeding", 1.0, stack=True, maxSeverity=5.0)]))
    t["Hemomancer"].add_nodes(spell_node("BloodSiphon", (6, 3), ["BloodHarvest"]))


def bard(t, extra):
    t["Bard"].add_spells(missiles("EchoingNotes", "echoing notes",
        "Sing five notes that take wing and chase enemies down, ringing in their skulls.", ("note", "arrow"),
        mana=26, cd=10, count=5, speed=13, projectile="Note", scale=0.5, spread=160, tags=["Song"],
        effects=[dmg(8, "AA_Sonic"), hediff("AA_Concussed", 1.0, chance=0.3)]))
    t["Bard"].add_nodes(spell_node("EchoingNotes", (4, 5), ["Encore"]))
    t["Jester"].add_spells(orbitals("JugglingKnives", "juggling knives",
        "Juggle three knives around yourself at alarming speed. Anyone who gets close gets cut, and occasionally a custard pie flies out of the act.",
        ("dagger", "dice"), mana=25, cd=30, count=3, orb_radius=1.3, duration=1200, speed=10, projectile="Dagger", scale=0.5, tags=["Song", "Chaos"],
        quirky=True, effects=[dmg(7, "Cut"), hediff("AA_Pied", 1.0, chance=0.2)]))
    t["Jester"].add_nodes(spell_node("JugglingKnives", (6, 3), ["BananaPeel"]))


EXTENDERS = {
    "c_pyromancer": pyromancer, "c_tempest": tempest, "c_necromancer": necromancer, "c_paladin": paladin, "c_chronomancer": chronomancer,
    "c_druid": druid, "c_runesmith": runesmith, "c_shadow": shadow, "c_bard": bard,
}


def extend(modname, root, subs, extra):
    fn = EXTENDERS.get(modname)
    if fn:
        fn(by_cid(root, subs), extra)

"""
The Noclipper: a mage who fell out of reality once and learned the way back. Spells lay Lost; at four stacks the victim
noclips out of the world for six seconds and comes back disoriented. Noclippers walk through walls, build mazes of yellow
wallpaper around their enemies, open doors to nowhere and call up things from the levels below.
"""
from classes.common import *  # noqa: F401,F403
from classes.expansion2 import missiles, channel, delayed  # noqa: F401
from classes import mythic_things as MT

YELLOW, MONO = "#f2e27a", "#fff8d0"
LIMINAL = School("Liminal", YELLOW, MONO, fleck="spark", fleck2="mote", projectile="ArcanumAscendant/Vfx/Glitch")
KEEPER = School("Liminal", "#e8dc8a", "#7ab8c8", fleck="spark", fleck2="mote", projectile="ArcanumAscendant/Vfx/Glitch")
HANDLER = School("Liminal", "#d8d0a0", "#3a3a3a", fleck="smoke", fleck2="spark", projectile="ArcanumAscendant/Vfx/Glitch")
ARCHIVE = School("Liminal", "#c8c0a8", "#ff3a3a", fleck="spark", fleck2="ring", projectile="ArcanumAscendant/Vfx/Glitch")

LOST = "AA_Lost"
WALL = "AA_Construct_YellowWall"

KEYS = {
    "AA_LostInBackrooms": "Lost in the Backrooms",
    "AA_ReplayingFootage": "Replaying footage",
    "AA_LevelFun": "=)",
}


def lost(n=1, **kw):
    return stacks(LOST, n, duration=1500, cap=8, **kw)


STATIC_HIT = [flash(1.6), burst("spark", 8, 2.5, 0.4)]


def statuses():
    s = []
    s.append(status(LOST, "lost", "This creature is losing its grip on where it is. At four stacks it noclips clean out of reality for six seconds and comes "
        "back disoriented. The very strongest only slip away for a moment.", YELLOW, bad=True, ticks=1500, maxSev=8,
        comps=[stack_trigger(4, boss=8, color=YELLOW, label="AA_LostInBackrooms", shake=0.5, sound="AA_Buzz",
                             effects=[banish(360, bossFactor=0.35, retHediff="AA_Disoriented")],
                             vfx=[flash(3.0, color=MONO), ring(1.2, 0.1, 20, byRadius=False)])],
        stages=[stage(offsets={"ShootingAccuracyPawn": -0.04, "MoveSpeed": -0.03}, mult=True)],
        fx=dict(emitter="Lost", emitInterval=10, overlayTex="ArcanumAscendant/Vfx/Glitch", overlaySize=0.6, overlayZ=1.0, overlayPulse=True)))
    s.append(status("AA_Disoriented", "disoriented", "Just came back from somewhere that does not exist. It wanders, flickers and keeps losing its footing.",
        YELLOW, bad=True, ticks=300, comps=[compel("Wander"), glitching(0.4, 30, YELLOW)], fx=dict(emitter="Lost", emitInterval=8)))
    s.append(status("AA_Glitching", "glitching", "Flickering in and out of the frame, losing whole moments at a time.", YELLOW, bad=True, ticks=480,
        comps=[glitching(0.5, 30, YELLOW)], stages=[stage(factors={"ShootingAccuracyPawn": 0.85, "AimingDelayFactor": 1.15})], fx=dict(emitter="Lost", emitInterval=8)))
    s.append(status("AA_Watched", "watched", "Something is watching from the corners. It cannot shake the feeling, aims badly, and keeps losing its way.", "#d8d0a0",
        bad=True, ticks=900, comps=[periodic(150, effects=[lost(1)], color=YELLOW)],
        stages=[stage(factors={"ShootingAccuracyPawn": 0.8}, offsets={"MentalBreakThreshold": 0.05})], fx=dict(emitter="Watched", emitInterval=10)))
    s.append(status("AA_AlmondCalm", "almond water", "Refreshed by almond water. The world feels a little more solid.", MONO, bad=False, ticks=10000,
        stages=[stage(offsets={"MentalBreakThreshold": -0.08, "PainShockThreshold": 0.1})]))
    s.append(status("AA_Recorded", "recorded", "REC. Everything that happens to it is on tape, and when the tape ends it will all happen again.", "#ff3a3a",
        bad=True, ticks=480, comps=[delayed_doom(stored=1.0, damageDef="AA_Static", boss=0.6, color="#ff3a3a", label="AA_ReplayingFootage", shake=0.8)],
        fx=dict(emitter="Lost", emitInterval=16, overlayTex="ArcanumAscendant/Vfx/RecMark", overlaySize=0.55, overlayZ=1.05, overlayPulse=True)))
    s.append(status("AA_LostFootage", "lost footage", "Caught on cursed tape. When the tape runs out, everything that happened to it happens again, worse.",
        "#ff3a3a", bad=True, ticks=600, comps=[delayed_doom(stored=1.5, damageDef="AA_Static", boss=0.6, color="#ff3a3a", label="AA_ReplayingFootage", shake=1.2)],
        fx=dict(emitter="Lost", emitInterval=12, overlayTex="ArcanumAscendant/Vfx/RecMark", overlaySize=0.65, overlayZ=1.05, overlayPulse=True)))
    s.append(status("AA_TrackingError", "tracking error", "The picture rolls and tears. It loses whole seconds at a time.", "#ff3a3a", bad=True, ticks=480,
        comps=[glitching(0.6, 40, "#ff6a6a")], stages=[stage(factors={"AimingDelayFactor": 1.3, "MeleeCooldownFactor": 1.2})], fx=dict(emitter="Lost", emitInterval=6)))
    s.append(status("AA_RunForYourLife", "run for your life", "The lights went red. It knows what that means.", "#ff3030", bad=True, ticks=360,
        comps=[compel("Flee")], stages=[stage(factors={"ShootingAccuracyPawn": 0.4})], fx=dict(emitter="Watched", emitInterval=8)))
    s.append(status("AA_SmilerDread", "dread", "It saw the smile in the dark and could not stop running.", "#e8e8e8", bad=True, ticks=300,
        comps=[compel("Flee")], fx=dict(emitter="Watched", emitInterval=6)))
    s.append(status("AA_Sublime", "sublime", "Standing somewhere warm and quiet that does not exist. Steady, fearless, at peace.", "#7ab8c8", bad=False, ticks=1800,
        stages=[stage(offsets={"MentalBreakThreshold": -0.2, "PainShockThreshold": 0.2}, factors={"IncomingDamageFactor": 0.85})],
        fx=dict(emitter="Holy", emitInterval=30)))
    s.append(MT.construct("Construct_YellowWall", "wallpaper wall", "A wall of damp, mono-yellow wallpaper that should not be here. It will not be here for long.",
        tex="WallpaperWall", color=YELLOW, hp=220, lifetime=1800, size=1.0, wall=True, glow=0.0))
    s.append(MT.construct("Construct_NowhereDoor", "door to nowhere", "An ordinary office door, standing on its own, slightly open. Anything hostile that walks too "
        "close goes through it.", tex="NowhereDoor", color=YELLOW, hp=300, lifetime=1800, size=1.4, glow=1.4, light=4.0,
        pulse=[lost(1), banish(300, retHediff="AA_Disoriented")], radius=1.8, interval=60))
    s.append(MT.construct("Construct_Smiler", "smiler", "A darkness with a smile in it. Enemies who see it run, and the static hurts.", tex="Smiler",
        color="#f0f0e8", hp=250, lifetime=1800, size=1.6, glow=0.0, pulse=[hediff("AA_SmilerDread", 1.0), dmg(6, "AA_Static"), lost(1, chance=0.5)],
        radius=5.0, interval=120))
    s += MT.creature("AA_Hound", "hound", "Something from the levels below that runs on all fours, pale and hairless, with too many teeth. It exists only "
        "as long as the Noclipper holds the door open.", tex="Hound", size=1.6, body="QuadrupedAnimalWithPawsAndTail", body_size=1.0, health=1.6, speed=6.2,
        tools=[MT.tool("claws", ["Scratch"], 14, 1.1, "FrontLeftPaw"), MT.tool("claws", ["Scratch"], 14, 1.1, "FrontRightPaw"),
               MT.tool("teeth", ["Bite"], 20, 1.6, "Teeth", chance=0.8, pen=0.25)],
        color=YELLOW, combat_power=90, armor=0.15)
    s += MT.creature("AA_Partygoer", "partygoer", "A smiling yellow thing that loves parties and wants you to come to one =). It exists only as long as the "
        "Noclipper holds the door open.", tex="Partygoer", size=1.5, body="Human", body_size=1.0, health=1.4, speed=4.8,
        tools=[MT.tool("left hand", ["Blunt"], 12, 1.4, "LeftHand"), MT.tool("right hand", ["Blunt"], 12, 1.4, "RightHand"),
               MT.tool("teeth", ["Bite"], 16, 1.8, "Teeth", chance=0.6, pen=0.2)],
        color=YELLOW, combat_power=70, armor=0.1)
    s.append(MT.almond_water())
    return s


def build():
    nc = ClassSpec("Noclipper", "noclipper", color=YELLOW, color2=MONO, icon=("door", "smile"), order=14, school=LIMINAL,
        tagline="You fell out of reality once and found the way back. Walk through walls, trap enemies in yellow mazes, and send them where you went.",
        desc="Noclippers have been to the Backrooms: the endless, humming, yellow levels behind the world. They came back with a few habits.",
        playstyle="Lay Lost with static and wrong turns; at four stacks an enemy noclips out of reality for six seconds and comes back wandering. "
                  "Noclip walks you through walls, Endless Hallway and Level 0 build corridors and mazes of wallpaper around the enemy, the Door "
                  "to Nowhere swallows anyone who comes close, and Out of Bounds drops a whole squad out of the map. Wants Attunement and Celerity.",
        lore="If you are not careful and you noclip out of reality in the wrong areas, you will end up in the Backrooms. She was not careful.",
        primary="ATT", secondary="CEL", tags=["Liminal"], starting=["StaticFlicker"], innate={"AA_CooldownReduction": 0.03})
    nc.add_spells(
        Spell("StaticFlicker", "static flicker", "Throw a burst of bad signal. It hurts, and it leaves the target a little more Lost.", icon=("tube", "burst"),
              mana=8, cd=1.5, range=26, warmup=0.4, tags=["Liminal"], delivery="Projectile", projectileSpeed=26.0, projectileScale=0.7, impact_vfx=STATIC_HIT,
              effects=[dmg(9, "AA_Static"), lost(1)], flavor="Bzzzt."),
        Spell("Noclip", "noclip", "Clip through reality to any spot in range. Walls, doors and line of sight do not matter.", icon=("door", "arrow"),
              mana=16, cd=10, range=22, warmup=0.3, tags=["Liminal", "Mobility"], role="Mobility", target="cell", los=False,
              impact_vfx=[flash(2.0), burst("spark", 10, 2.0, 0.4)], effects=[teleport("CasterToTarget")], flavor="Wrong place, right time."),
        Spell("WrongTurn", "wrong turn", "Somewhere back there, the target took a wrong turn. It vanishes from reality for four seconds and comes back more Lost.",
              icon=("door", "eye"), mana=26, cd=18, range=26, warmup=0.5, tags=["Liminal"], role="Control", impact_vfx=STATIC_HIT,
              effects=[lost(1), banish(240, retHediff="AA_Disoriented")]),
        Spell("BuzzingLights", "buzzing lights", "Fluorescent lights hum into existence over an area for twelve seconds. Enemies under them glitch, losing whole "
              "moments, and grow more Lost.", icon=("tube", "eye"), mana=40, cd=30, range=28, warmup=0.8, tags=["Liminal"], role="Control", target="enemy_area",
              ai_min=2, tier=2, sound_cast="AA_Buzz", impact_vfx=[flash(2.0, color=MONO), ring(0.2, 1.0, 26)],
              effects=[field(4.0, 720, 90, [hediff("AA_Glitching", 1.0, affects="Enemies"), lost(1, affects="Enemies"), dmg(3, "AA_Static", affects="Enemies")],
                             groundTex="ArcanumAscendant/Vfx/LiminalFloor", groundSpin=0.0, groundAlpha=0.45)]),
        Spell("EndlessHallway", "endless hallway", "Wallpapered walls rise on both sides of a line from you to the target, turning the ground in between into a "
              "hallway for half a minute.", icon=("wall", "door"), mana=30, cd=30, range=16, warmup=0.6, tags=["Liminal"], role="Control", target="cell",
              tier=2, impact_vfx=[flash(1.6)], effects=[spawn(WALL, pattern="Corridor", corridorWidth=3.0, lifetimeTicks=1800)]),
        Spell("AlmondWater", "almond water", "Share a sip of almond water: an ally heals, shakes off what ails them, steadies their mind and regains some mana.",
              icon=("bottle", "heart"), mana=20, cd=20, range=20, warmup=0.6, tags=["Liminal", "Healing"], role="Heal", target="ally", ai_hp=0.6,
              impact_vfx=[burst("mote", 12, 1.5, 0.5), glow(1.4, 2.2, 24)], effects=[heal(25), cleanse(), mana(20), hediff("AA_AlmondCalm", 1.0)]),
        Spell("SomethingIsWatching", "something is watching", "Enemies in an area feel eyes on them from every corner for fifteen seconds. They aim badly and "
              "keep losing their way.", icon=("eye", "smile"), mana=34, cd=25, range=28, warmup=0.6, tags=["Liminal"], role="Control", target="enemy_area",
              radius=4.0, ai_min=2, tier=2, impact_vfx=[flash(1.4, color="#202020"), ring(0.2, 1.0, 24)], effects=[hediff("AA_Watched", 1.0), lost(1)]),
        Spell("DoorToNowhere", "door to nowhere", "An ordinary office door appears, standing on its own, for thirty seconds. Any enemy that walks close to it "
              "goes through, and vanishes for five seconds.", icon=("door", "portal"), mana=45, cd=50, range=20, warmup=1.0, tags=["Liminal"], role="Control",
              target="cell", tier=2, impact_vfx=[flash(2.5, color=MONO), burst("spark", 12, 2.0, 0.4)], effects=[spawn("AA_Construct_NowhereDoor", lifetimeTicks=1800)]),
        Spell("LevelZero", "level 0", "Fold the world into Level 0 around a target area: a maze of damp yellow wallpaper rises around every enemy inside and "
              "stands for half a minute. Everyone inside grows more Lost.", icon=("maze", "tube"), mana=60, cd=60, range=28, warmup=1.2, tags=["Liminal"],
              role="Control", target="enemy_area", radius=6.0, ai_min=3, tier=3, shake=0.6, sound_cast="AA_Buzz",
              impact_vfx=[flash(3.0, color=MONO), ring(0.1, 1.0, 30)], effects=[lost(1), spawn(WALL, pattern="Maze", lifetimeTicks=1800)],
              flavor="It smells of old, moist carpet."),
        Spell("OutOfBounds", "out of bounds", "Every enemy in an area falls through the floor and out of the map. Eight seconds later they drop back in, hard.",
              icon=("door", "skull"), mana=60, cd=45, range=28, warmup=1.0, tags=["Liminal"], role="Control", target="enemy_area", radius=3.0, ai_min=2,
              tier=3, shake=0.8, impact_vfx=[ring(1.2, 0.1, 24), flash(3.0, color=MONO)],
              effects=[lost(1), banish(480, ret=[dmg(25, "AA_Static"), stun(90)], retRadius=1.5)]),
        Spell("ConjureAlmondWater", "conjure almond water", "Reach into a wall and pull out a few bottles of almond water. Nobody knows why this works.",
              icon=("bottle", "hand"), mana=30, cd=DAY / SEC, warmup=4.0, tags=["Liminal", "Utility"], role="Utility", target="self", quirky=True,
              impact_vfx=[burst("spark", 10, 1.5, 0.4)], effects=[conjure("AA_AlmondWater", 2, 4)], flavor="Do not drink anything that does not smell of almonds."),
    )
    nc.add_nodes(
        Node("BadSignal", "bad signal", (1, 0), ranks=3, mods=[tagmod("Liminal", damage=0.05)]),
        Node("Hum", "the hum", (3, 0), ranks=3, stats={"AA_ManaMax": 10, "AA_ManaRegen": 0.06}),
        Node("Slippage", "slippage", (5, 0), ranks=3, stats={"MoveSpeed": 0.06, "AA_CastSpeed": 0.02}),
        Node("WrongTurn", None, (1, 1), kind="Spell", spell="WrongTurn", pre=["BadSignal"]),
        Node("AlmondWater", None, (3, 1), kind="Spell", spell="AlmondWater", pre=["Hum"]),
        Node("Noclip", None, (5, 1), kind="Spell", spell="Noclip", pre=["Slippage"]),
        Node("DeadEnd", "dead end", (0, 2), kind="Augment", ranks=2, pre=["WrongTurn"], mods=[spellmod(["WrongTurn"], duration=0.3, cooldown=-0.1)]),
        Node("Disorientation", "disorientation", (2, 2), kind="Notable", ranks=2, pre=["WrongTurn", "AlmondWater"],
             passives=[P("P_OnHitHediff", hediff=LOST, chancePerRank=0.15, severity=1.0, stack=True, maxSeverity=8.0, durationTicks=1500, tag="Liminal")],
             desc="Liminal spells sometimes leave the target an extra step more Lost."),
        Node("BuzzingLights", None, (4, 2), kind="Spell", spell="BuzzingLights", pre=["AlmondWater", "Noclip"]),
        Node("PhaseShift", "phase shift", (6, 2), kind="Augment", ranks=2, pre=["Noclip"], mods=[spellmod(["Noclip"], cooldown=-0.25, range=0.2)]),
        Node("EndlessHallway", None, (1, 3), kind="Spell", spell="EndlessHallway", pre=["DeadEnd", "Disorientation"]),
        Node("Liminality", "liminality", (3, 3), ranks=3, pre=["Disorientation", "BuzzingLights"], stats={"AA_CooldownReduction": 0.02, "AA_DurationFactor": 0.03}),
        Node("SomethingIsWatching", None, (5, 3), kind="Spell", spell="SomethingIsWatching", pre=["BuzzingLights", "PhaseShift"]),
        Node("Fluorescence", "fluorescence", (0, 4), kind="Augment", ranks=2, pre=["EndlessHallway"], mods=[spellmod(["BuzzingLights"], radius=0.2, duration=0.3)]),
        Node("DoorToNowhere", None, (2, 4), kind="Spell", spell="DoorToNowhere", pre=["EndlessHallway", "Liminality"]),
        Node("Paranoia", "paranoia", (4, 4), kind="Notable", ranks=2, pre=["Liminality", "SomethingIsWatching"], passives=[P("P_DodgeBlink", chancePerRank=0.07)],
             desc="You are never quite where they think you are."),
        Node("ConjureAlmondWater", None, (6, 4), kind="Spell", spell="ConjureAlmondWater", pre=["SomethingIsWatching"]),
        Node("WetCarpet", "wet carpet", (1, 5), ranks=3, pre=["Fluorescence", "DoorToNowhere"], stats={"AA_WardMax": 8, "AA_HealingPower": 0.04}),
        Node("LevelZero", None, (3, 5), kind="Spell", spell="LevelZero", pre=["DoorToNowhere", "Paranoia"]),
        Node("Monoyellow", "mono-yellow", (5, 5), ranks=3, pre=["Paranoia", "ConjureAlmondWater"], mods=[tagmod("Liminal", damage=0.05, duration=0.04)]),
        Node("Revolving", "revolving door", (0, 6), kind="Augment", ranks=2, pre=["WetCarpet"], mods=[spellmod(["DoorToNowhere"], duration=0.4, extraTargets=1)],
             desc="Open an extra door per rank, and keep them open longer."),
        Node("OutOfBounds", None, (2, 6), kind="Spell", spell="OutOfBounds", pre=["WetCarpet", "LevelZero"]),
        Node("Labyrinthine", "labyrinthine", (4, 6), kind="Notable", ranks=2, pre=["LevelZero", "Monoyellow"], mods=[spellmod(["LevelZero", "EndlessHallway"], radius=0.15, duration=0.3)],
             desc="Your mazes and hallways are bigger and stand longer."),
        Node("Frameskip", "frameskip", (6, 6), ranks=3, pre=["Monoyellow"], stats={"AA_CritChance": 0.02, "AA_CastSpeed": 0.03}),
        Node("Wanderer", "wanderer", (2, 7), kind="Keystone", excl="NoclipperKeystone", pre=["OutOfBounds", "Revolving"],
             mods=[spellmod(["Noclip"], cooldown=-0.6), tagmod("Liminal", damage=0.15)],
             passives=[P("P_TriggerSpell", trigger="OnHitTaken", spell=S("Noclip"), chancePerRank=0.2, target="Self", cooldownTicks=600)],
             desc="You never stay anywhere long. Noclip is nearly always ready, and sometimes when you are hurt you slip away on your own."),
        Node("KeeperOfThresholds", "keeper of thresholds", (4, 7), kind="Keystone", excl="NoclipperKeystone", pre=["Labyrinthine", "Frameskip"],
             passives=[P("P_OnHitHediff", hediff=LOST, chancePerRank=0.5, severity=1.0, stack=True, maxSeverity=8.0, durationTicks=1500, tag="Liminal")],
             mods=[tagmod("Liminal", duration=0.25)], desc="Everyone who meets you starts to lose their way: half your hits make them more Lost."),
        Node("Backrooms", "the backrooms", (3, 8), kind="Keystone", pre=["Wanderer", "KeeperOfThresholds"], pct={"AA_SpellPower": 0.15},
             passives=[P("P_TriggerSpell", trigger="OnStackTrigger", spell=S("StaticFlicker"), chancePerRank=1.0, target="NearestEnemy", stackHediff=LOST)],
             desc="You know the levels better than anyone alive. Every time someone noclips away, a flicker of static jumps to the next enemy."),
    )

    # ------------------------------------------------------------------ LEVEL KEEPER
    lk = ClassSpec("LevelKeeper", "level keeper", color="#e8dc8a", color2="#7ab8c8", icon=("maze", "door"), parent=nc, order=0, school=KEEPER,
        tagline="Pull the levels through into the world: the Poolrooms, the Manila Room, Level Run For Your Life, and the rest.",
        desc="Level Keepers carry the Backrooms around with them, a room or two at a time.",
        playstyle="Battlefield architecture. The Manila Room seals a single enemy in a box, Threshold rings an area in walls, the Poolrooms heal "
                  "your side and drown theirs, and Level ! sends every enemy around you running. Bleed Through drops every enemy on the map into "
                  "the Backrooms for ten seconds. Wants Attunement and Resilience.",
        lore="She keeps the keys to every level. Some of the doors should not be opened. She opens them anyway.", primary="ATT", secondary="RES",
        tags=["Liminal"], height=8)
    lk.add_spells(
        Spell("ManilaRoom", "manila room", "Fold a single enemy into the Manila Room: a box of wallpaper closes around it for twenty seconds.", icon=("wall", "eye"),
              mana=40, cd=30, range=24, warmup=0.6, tags=["Liminal"], role="Control", impact_vfx=STATIC_HIT,
              effects=[lost(1), spawn(WALL, pattern="Box", boxRadius=1, lifetimeTicks=1200)]),
        Spell("Poolrooms", "the poolrooms", "Flood an area with the Poolrooms for twenty seconds: warm water and pale tiles. Allies inside regenerate; enemies "
              "wade slowly and lose their way.", icon=("drop", "maze"), mana=50, cd=50, range=26, warmup=1.0, tags=["Liminal", "Healing"], target="cell",
              role="Control", tier=2, impact_vfx=[ring(0.2, 1.0, 30), burst("mote", 16, 1.5, 0.5, byRadius=True)],
              effects=[field(5.0, 1200, 120, [hediff("AA_Regeneration", 1.0, affects="Allies"), hediff("AA_Slowed", 1.0, affects="Enemies"),
                                              lost(1, chance=0.5, affects="Enemies")],
                             groundTex="ArcanumAscendant/Vfx/Poolrooms", groundSpin=0.0, groundAlpha=0.55, useColor2=True)]),
        Spell("LevelRun", "level !", "The lights turn red. Every enemy within seven cells runs for its life for six seconds; every ally runs faster.",
              icon=("exitsign", "skull"), mana=45, cd=40, warmup=0.5, tags=["Liminal"], role="Control", target="self", delivery="Nova", radius=7.0, ai_min=3,
              tier=2, cast_vfx=[screen("#ff2020", 0.3, 40)], impact_vfx=[ring(0.1, 1.2, 30), flash(3.0, color="#ff3030")],
              effects=[hediff("AA_RunForYourLife", 1.0, affects="Enemies"), lost(1, affects="Enemies"), hediff("AA_Haste", 1.0, duration=600, affects="Allies")],
              flavor="RUN FOR YOUR LIFE."),
        Spell("Threshold", "threshold", "Draw a threshold around an area: a ring of wallpaper walls cuts it off from the world for fifteen seconds.",
              icon=("ring", "wall"), mana=45, cd=40, range=26, warmup=0.8, tags=["Liminal"], role="Control", target="cell", radius=4.0, tier=3,
              impact_vfx=[flash(2.0, color=MONO), ring(0.2, 1.0, 24)], effects=[spawn(WALL, ring=True, lifetimeTicks=900)]),
        Spell("Sublimity", "sublimity", "Open a door onto somewhere warm, quiet and impossible. Allies around you are warded, steadied and fearless for thirty "
              "seconds.", icon=("door", "sun"), mana=40, cd=45, warmup=0.6, tags=["Liminal", "Ward"], role="Buff", target="self", delivery="Nova", radius=6.0,
              affects="Allies", tier=3, impact_vfx=[ring(0.2, 1.0, 30), burst("mote", 20, 1.5, 0.5, byRadius=True)],
              effects=[ward(25), hediff("AA_Sublime", 1.0)]),
        Spell("BleedThrough", "bleed through", "Let the Backrooms bleed through into the world. Every enemy on the map noclips out of reality for ten seconds "
              "(bosses for less) and comes back Lost and disoriented.", icon=("maze", "eclipse"), mana=230, cd=300, warmup=3.0, tags=["Liminal"],
              target="self", delivery="Global", ultimate=True, tier=5, xp=2.0, shake=1.0, ai_min=4, sound_cast="AA_Buzz",
              cast_vfx=[pillar(2.6, 70), screen("#f2e27a", 0.3, 50)], impact_vfx=[burst("spark", 8, 2.0, 0.4)],
              effects=[dmg(15, "AA_Static"), banish(600, ret=[lost(2)], retHediff="AA_Disoriented")]),
    )
    lk.add_nodes(
        Node("ManilaRoom", None, (2, 0), kind="Spell", spell="ManilaRoom"),
        Node("Architect", "architect", (4, 0), ranks=3, stats={"AA_WardMax": 8, "AA_DurationFactor": 0.03}),
        Node("NoWayOut", "no way out", (1, 1), kind="Augment", ranks=2, pre=["ManilaRoom"], mods=[spellmod(["ManilaRoom"], duration=0.3, cooldown=-0.15)]),
        Node("Poolrooms", None, (3, 1), kind="Spell", spell="Poolrooms", pre=["ManilaRoom", "Architect"]),
        Node("Chlorine", "chlorine", (5, 1), ranks=3, pre=["Architect"], stats={"AA_HealingPower": 0.06}),
        Node("Cartographer", "cartographer", (0, 2), ranks=3, pre=["NoWayOut"], mods=[tagmod("Liminal", radius=0.05)]),
        Node("LevelRun", None, (2, 2), kind="Spell", spell="LevelRun", pre=["NoWayOut", "Poolrooms"]),
        Node("DeepEnd", "deep end", (4, 2), kind="Augment", ranks=2, pre=["Poolrooms", "Chlorine"], mods=[spellmod(["Poolrooms"], radius=0.2, duration=0.3)]),
        Node("Lifeguard", "lifeguard", (6, 2), kind="Notable", ranks=2, pre=["Chlorine"], passives=[P("P_HealOnCast", amount=3.0, radius=5.0)],
             desc="Every spell you cast eases the wounds of allies around you."),
        Node("RedLights", "red lights", (1, 3), kind="Augment", ranks=2, pre=["Cartographer", "LevelRun"], mods=[spellmod(["LevelRun"], radius=0.2, duration=0.3)]),
        Node("Threshold", None, (3, 3), kind="Spell", spell="Threshold", pre=["LevelRun", "DeepEnd"]),
        Node("Levels", "levels upon levels", (5, 3), ranks=3, pre=["DeepEnd", "Lifeguard"], pct={"AA_SpellPower": 0.03}),
        Node("Lockdown", "lockdown", (2, 4), kind="Notable", ranks=2, pre=["RedLights", "Threshold"], mods=[spellmod(["Threshold", "ManilaRoom"], duration=0.3)],
             passives=[P("P_ManaOnKill", amount=6.0)], desc="Your rooms stand longer, and whatever dies in them feeds you."),
        Node("Sublimity", None, (4, 4), kind="Spell", spell="Sublimity", pre=["Threshold", "Levels"]),
        Node("Corridors", "corridors", (1, 5), ranks=3, pre=["Lockdown"], stats={"AA_CooldownReduction": 0.03}),
        Node("Quiet", "the quiet", (3, 5), kind="Notable", ranks=2, pre=["Lockdown", "Sublimity"], passives=[P("P_DamageReduction", fractionPerRank=0.05)],
             desc="Somewhere far away, it is very quiet. Some of every blow never reaches you."),
        Node("Serenity", "serenity", (5, 5), ranks=3, pre=["Sublimity"], stats={"AA_WardMax": 10, "AA_WardRegen": 0.1}),
        Node("MasterOfLevels", "master of levels", (2, 6), kind="Keystone", excl="KeeperKeystone", pre=["Corridors", "Quiet"],
             mods=[tagmod("Liminal", duration=0.4, radius=0.2)], desc="Every level you open is larger and stays open far longer."),
        Node("SafeRoom", "safe room", (4, 6), kind="Keystone", excl="KeeperKeystone", pre=["Quiet", "Serenity"],
             passives=[P("P_Aura", radius=6.0, interval=240, affects="Allies", onlyInCombat=True, color=(0.48, 0.72, 0.78, 1.0), effects=[ward(8), heal(4)])],
             desc="Wherever you stand is a safe room: allies near you are steadily warded and healed while you fight."),
        Node("BleedThrough", None, (3, 7), kind="Ultimate", spell="BleedThrough", pre=["MasterOfLevels", "SafeRoom"]),
    )

    # ------------------------------------------------------------------ ENTITY HANDLER
    eh = ClassSpec("EntityHandler", "entity handler", color="#d8d0a0", color2="#3a3a3a", icon=("hound", "smile"), parent=nc, order=1, school=HANDLER,
        tagline="Some things in the levels can be reasoned with. Hounds, Partygoers, Smilers and Skin-Stealers, on your side for once.",
        desc="Entity Handlers made friends in the Backrooms. Their friends are why nobody else did.",
        playstyle="A summoner of Backrooms entities. Hounds hunt in a pack, Partygoers swarm and smile =), a Smiler sends enemies running, and the "
                  "Skin-Stealer copies an enemy to fight on your side. Level Fun throws a party nobody survives. Wants Attunement and Focus.",
        lore="'Do you want to come to a party? =)' Yes. You do.", primary="ATT", secondary="FOC", tags=["Liminal", "Summon"], height=8)
    eh.add_spells(
        Spell("ReleaseTheHounds", "release the hounds", "Open a door and let two Hounds out for a minute. They run on all fours and they are very hungry.",
              icon=("hound", "door"), mana=40, cd=60, range=20, warmup=1.0, tags=["Liminal", "Summon"], role="Summon", target="cell",
              impact_vfx=[flash(2.0, color=MONO), burst("smoke", 10, 1.5, 0.6)], effects=[summon("AA_Hound", 2, MIN)]),
        Spell("Partygoers", "partygoers", "Invite three Partygoers to the fight for a minute. They are so happy to be here =)", icon=("balloon", "smile"),
              mana=50, cd=75, range=20, warmup=1.2, tags=["Liminal", "Summon"], role="Summon", target="cell", tier=2,
              impact_vfx=[flash(2.0, color="#f0d840"), burst("star", 16, 2.5, 0.5), burst("petal", 10, 2.0, 0.5)], effects=[summon("AA_Partygoer", 3, MIN)],
              flavor="=)"),
        Spell("Smiler", "smiler", "Leave a Smiler in the dark for thirty seconds. Every few seconds, enemies who see it run in terror and the static burns them.",
              icon=("smile", "eye"), mana=45, cd=50, range=22, warmup=1.0, tags=["Liminal", "Summon"], role="Control", target="cell", tier=2,
              impact_vfx=[flash(1.6, color="#101010"), smoke(4, 1.2)], effects=[spawn("AA_Construct_Smiler", lifetimeTicks=1800)]),
        Spell("FacelingSwap", "faceling swap", "Trade places with an enemy, the way the Facelings do. It finds itself where you were, a little more Lost.",
              icon=("mask", "swirl"), mana=18, cd=12, range=24, warmup=0.3, tags=["Liminal", "Mobility"], role="Mobility", target="enemy",
              impact_vfx=STATIC_HIT, effects=[lost(1), teleport("Swap")]),
        Spell("SkinStealer", "skin-stealer", "Let a Skin-Stealer wear an enemy's face. A perfect copy of the target, gear and all, fights for you for forty-five "
              "seconds.", icon=("mask", "hand"), mana=60, cd=90, range=24, warmup=1.2, tags=["Liminal", "Summon"], tier=3,
              impact_vfx=[flash(2.5, color=MONO), burst("smoke", 10, 1.5, 0.6)], effects=[copy_pawn(2700, maxBodySize=3.0)],
              flavor="It is not him. It knows that you know."),
        Spell("LevelFun", "level fun", "Everyone is invited to Level Fun =) Six Partygoers join the fight, and every enemy in a wide area loses its mind with joy "
              "and its way out.", icon=("balloon", "crown"), mana=230, cd=300, range=28, warmup=2.5, tags=["Liminal", "Summon"], target="enemy_area",
              radius=8.0, ultimate=True, tier=5, xp=2.0, shake=1.0, ai_min=3, cast_vfx=[screen("#f0d840", 0.3, 50)],
              impact_vfx=[ring(0.1, 1.1, 40), burst("star", 40, 4.0, 0.6, byRadius=True), flash(5.0, color="#f0d840")],
              effects=[summon("AA_Partygoer", 6, MIN * 1.5), mental("Berserk", chance=0.4, humanlikeOnly=False), lost(2)]),
    )
    eh.add_nodes(
        Node("ReleaseTheHounds", None, (2, 0), kind="Spell", spell="ReleaseTheHounds"),
        Node("Kennel", "kennel master", (4, 0), ranks=3, pct={"AA_SummonPower": 0.06}),
        Node("Pack", "the pack", (1, 1), kind="Augment", ranks=2, pre=["ReleaseTheHounds"], mods=[spellmod(["ReleaseTheHounds"], extraTargets=1, duration=0.25)],
             desc="One more Hound per rank."),
        Node("FacelingSwap", None, (3, 1), kind="Spell", spell="FacelingSwap", pre=["ReleaseTheHounds", "Kennel"]),
        Node("Handler", "handler", (5, 1), ranks=3, pre=["Kennel"], stats={"AA_DurationFactor": 0.04}),
        Node("Feral", "feral", (0, 2), ranks=3, pre=["Pack"], pct={"AA_SummonPower": 0.05}),
        Node("Partygoers", None, (2, 2), kind="Spell", spell="Partygoers", pre=["Pack", "FacelingSwap"]),
        Node("Masks", "many faces", (4, 2), kind="Augment", ranks=2, pre=["FacelingSwap", "Handler"], mods=[spellmod(["FacelingSwap"], cooldown=-0.3, range=0.2)]),
        Node("Smiler", None, (6, 2), kind="Spell", spell="Smiler", pre=["Handler"]),
        Node("Balloons", "balloons", (1, 3), kind="Augment", ranks=2, pre=["Feral", "Partygoers"], mods=[spellmod(["Partygoers"], extraTargets=1, duration=0.25)],
             desc="One more Partygoer per rank =)"),
        Node("SkinStealer", None, (3, 3), kind="Spell", spell="SkinStealer", pre=["Partygoers", "Masks"]),
        Node("WiderGrin", "wider grin", (5, 3), kind="Augment", ranks=2, pre=["Masks", "Smiler"], mods=[spellmod(["Smiler"], duration=0.4, extraTargets=1)],
             desc="An extra Smiler per rank, and they linger."),
        Node("Rapport", "rapport", (2, 4), kind="Notable", ranks=2, pre=["Balloons", "SkinStealer"], pct={"AA_SummonPower": 0.1}, stats={"AA_HealingPower": 0.05},
             desc="The entities like you. They hit harder and last longer at your side."),
        Node("Doppelganger", "doppelganger", (4, 4), kind="Augment", pre=["SkinStealer", "WiderGrin"], mods=[spellmod(["SkinStealer"], cooldown=-0.3, duration=0.4)]),
        Node("Bait", "bait", (1, 5), ranks=3, pre=["Rapport"], stats={"AA_WardMax": 8, "ArmorRating_Sharp": 0.03}),
        Node("PackTactics", "pack tactics", (3, 5), kind="Notable", ranks=2, pre=["Rapport", "Doppelganger"], passives=[P("P_ManaOnKill", amount=6.0)],
             desc="Whatever your entities kill pays you in mana."),
        Node("Grinning", "grinning", (5, 5), ranks=3, pre=["Doppelganger"], mods=[tagmod("Liminal", damage=0.05)]),
        Node("Alpha", "alpha", (2, 6), kind="Keystone", excl="HandlerKeystone", pre=["Bait", "PackTactics"], pct={"AA_SummonPower": 0.3},
             mods=[tagmod("Summon", extraTargets=1, duration=0.5)], desc="Lead the pack. Every summon brings an extra entity, and they last half again as long."),
        Node("HostOfTheParty", "host of the party", (4, 6), kind="Keystone", excl="HandlerKeystone", pre=["PackTactics", "Grinning"],
             mods=[spellmod(["Partygoers", "SkinStealer"], cooldown=-0.4)], passives=[P("P_RaiseOnKill", chancePerRank=0.2, durationTicks=HOUR * 2)],
             desc="Everyone comes to your parties eventually. Your guests come back sooner, and some of the people you kill come back as guests."),
        Node("LevelFun", None, (3, 7), kind="Ultimate", spell="LevelFun", pre=["Alpha", "HostOfTheParty"]),
    )

    # ------------------------------------------------------------------ ARCHIVIST
    ar = ClassSpec("Archivist", "archivist", color="#c8c0a8", color2="#ff3a3a", icon=("camera", "vhs"), parent=nc, order=2, school=ARCHIVE,
        tagline="Everything is on tape. Record your enemies and replay their worst moments, rewind your friends, and find the exit sign.",
        desc="Archivists document the Backrooms on old camcorders. The tapes remember more than they should.",
        playstyle="Recording is damage. Camcorder records an enemy for eight seconds; when the tape ends, every wound it took happens again. Pair "
                  "it with your allies' fire. Rewind Tape undoes thirty seconds of an ally's wounds, Exit Sign teleports your whole squad, and "
                  "Lost Footage records an entire battlefield. Wants Focus and Attunement.",
        lore="The tape was found in a wall. It shows the person who found it, finding it.", primary="FOC", secondary="ATT", tags=["Liminal"], height=8)
    ar.add_spells(
        Spell("Camcorder", "camcorder", "Start recording an enemy. In eight seconds the tape ends, and every wound it took in the meantime happens to it again.",
              icon=("camera", "eye"), mana=30, cd=20, range=28, warmup=0.5, tags=["Liminal"], role="Control", impact_vfx=STATIC_HIT + [ring(0.5, 0.1, 16, byRadius=False)],
              effects=[hediff("AA_Recorded", 1.0), lost(1)], flavor="REC"),
        Spell("TrackingError", "tracking error", "The picture rolls and tears over an area: enemies inside glitch for eight seconds, losing whole seconds at a "
              "time.", icon=("vhs", "burst"), mana=30, cd=18, range=28, warmup=0.6, tags=["Liminal"], role="Control", target="enemy_area", radius=3.0,
              ai_min=2, impact_vfx=[flash(2.0), burst("spark", 14, 3.0, 0.4, byRadius=True)],
              effects=[dmg(8, "AA_Static"), hediff("AA_TrackingError", 1.0), lost(1)]),
        Spell("StaticBurst", "static burst", "Every screen in the world flashes to static at once: everything around you is burned by it, made more Lost and "
              "sometimes thrown out of sync.", icon=("tube", "burst"), mana=36, cd=22, warmup=0.4, tags=["Liminal"], target="self", delivery="Nova",
              radius=5.0, ai_min=2, tier=2, impact_vfx=[ring(0.1, 1.2, 24), burst("spark", 20, 4.0, 0.4, byRadius=True), flash(3.0, color=MONO)],
              effects=[dmg(20, "AA_Static"), lost(1), hediff("AA_TrackingError", 1.0, chance=0.5)]),
        Spell("ExitSign", "exit sign", "Follow the exit sign. You and every ally within six cells step through a door and come out at the target.",
              icon=("exitsign", "arrow"), mana=40, cd=60, range=30, warmup=1.0, tags=["Liminal", "Mobility"], role="Utility", target="cell", los=False,
              tier=2, autocast=False, impact_vfx=[flash(2.5, color="#40ff60"), burst("spark", 12, 2.0, 0.4)],
              effects=[teleport("AlliesToTarget", gatherRadius=6.0)]),
        Spell("RewindTape", "rewind tape", "Rewind an ally by thirty seconds. The wounds of the last half-minute never happened.", icon=("vhs", "heart"),
              mana=50, cd=90, range=20, warmup=1.0, tags=["Liminal", "Healing"], role="Heal", target="ally", ai_hp=0.4, tier=3,
              impact_vfx=[burst("clock", 10, 1.5, 0.5), glow(1.6, 2.4, 30)], effects=[rewind(1800)]),
        Spell("LostFootage", "lost footage", "Record an entire battlefield on cursed tape. Every enemy within twelve cells is recorded for ten seconds; when the "
              "tape runs out, everything that happened to each of them happens again, half as bad again.", icon=("camera", "skull"), mana=220, cd=300,
              range=30, warmup=2.0, tags=["Liminal"], target="enemy_area", radius=12.0, ultimate=True, tier=5, xp=2.0, shake=1.0, ai_min=4,
              cast_vfx=[screen("#ff3a3a", 0.25, 40)], impact_vfx=[ring(0.1, 1.1, 40), burst("spark", 30, 3.0, 0.4, byRadius=True)],
              effects=[dmg(20, "AA_Static"), hediff("AA_LostFootage", 1.0), lost(2)]),
    )
    ar.add_nodes(
        Node("Camcorder", None, (2, 0), kind="Spell", spell="Camcorder"),
        Node("Analog", "analog", (4, 0), ranks=3, stats={"AA_ManaMax": 10, "AA_CritChance": 0.01}),
        Node("LongPlay", "long play", (1, 1), kind="Augment", ranks=2, pre=["Camcorder"], mods=[spellmod(["Camcorder"], cooldown=-0.15, extraTargets=1)],
             desc="Record one more enemy per rank."),
        Node("TrackingError", None, (3, 1), kind="Spell", spell="TrackingError", pre=["Camcorder", "Analog"]),
        Node("Grain", "film grain", (5, 1), ranks=3, pre=["Analog"], mods=[tagmod("Liminal", damage=0.05)]),
        Node("Archive", "the archive", (0, 2), ranks=3, pre=["LongPlay"], stats={"AA_DurationFactor": 0.04}),
        Node("StaticBurst", None, (2, 2), kind="Spell", spell="StaticBurst", pre=["LongPlay", "TrackingError"]),
        Node("Interference", "interference", (4, 2), kind="Augment", ranks=2, pre=["TrackingError", "Grain"], mods=[spellmod(["TrackingError"], radius=0.25, duration=0.3)]),
        Node("ExitSign", None, (6, 2), kind="Spell", spell="ExitSign", pre=["Grain"]),
        Node("Overexposure", "overexposure", (1, 3), kind="Augment", ranks=2, pre=["Archive", "StaticBurst"], mods=[spellmod(["StaticBurst"], damage=0.25, radius=0.15)]),
        Node("RewindTape", None, (3, 3), kind="Spell", spell="RewindTape", pre=["StaticBurst", "Interference"]),
        Node("FastForward", "fast forward", (5, 3), kind="Notable", ranks=2, pre=["Interference", "ExitSign"], stats={"AA_CooldownReduction": 0.04, "MoveSpeed": 0.06},
             desc="Skip the boring parts."),
        Node("Playback", "playback", (2, 4), kind="Notable", ranks=2, pre=["Overexposure", "RewindTape"],
             passives=[P("P_OnHitHediff", hediff="AA_Recorded", chancePerRank=0.12, severity=1.0, tag="Liminal", cooldownTicks=300)],
             desc="Liminal spells sometimes start the tape rolling on their target."),
        Node("Pause", "pause", (4, 4), kind="Augment", ranks=2, pre=["RewindTape", "FastForward"], mods=[spellmod(["RewindTape"], cooldown=-0.25)]),
        Node("Degradation", "generation loss", (1, 5), ranks=3, pre=["Playback"], pct={"AA_SpellPower": 0.03}),
        Node("Rewatch", "rewatch", (3, 5), kind="Notable", ranks=2, pre=["Playback", "Pause"], passives=[P("P_EchoCast", chancePerRank=0.08, tag="Liminal")],
             desc="Some of your spells play twice."),
        Node("Bootleg", "bootleg", (5, 5), ranks=3, pre=["Pause"], stats={"AA_WardMax": 10}),
        Node("DirectorsCut", "director's cut", (2, 6), kind="Keystone", excl="ArchivistKeystone", pre=["Degradation", "Rewatch"],
             mods=[spellmod(["Camcorder"], cooldown=-0.5, extraTargets=2), tagmod("Liminal", damage=0.15)],
             desc="Film everything. Camcorder returns twice as fast and records two more enemies."),
        Node("Rewinder", "rewinder", (4, 6), kind="Keystone", excl="ArchivistKeystone", pre=["Rewatch", "Bootleg"],
             passives=[P("P_Rebirth", novaDamage=30.0, novaDamageDef="AA_Static", novaRadius=4.0, wardFraction=0.8, cooldownTicks=DAY,
                         color=(1.0, 0.23, 0.23, 1.0), labelKey="AA_ReplayingFootage")],
             mods=[spellmod(["RewindTape"], cooldown=-0.4)], desc="Your own tape can be rewound. Once a day, when you would die, you are rewound instead."),
        Node("LostFootage", None, (3, 7), kind="Ultimate", spell="LostFootage", pre=["DirectorsCut", "Rewinder"]),
    )
    return nc, [lk, eh, ar], statuses()

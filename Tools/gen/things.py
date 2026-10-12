"""Helpers that emit ThingDefs for conjured constructs (turrets, walls, totems, lights)."""
from arcgen import O, to_xml, rgb

THING_ICONS = []  # (texPath, kind, color hex, color2 hex)


def _construct_comp(color, lifetime, pulse=None, radius=4.0, interval=120, affects="Enemies", glow=1.8, damage_scale=1.0):
    return O("ArcanumAscendant.CompProperties_ArcaneConstruct", defaultLifetimeTicks=int(lifetime), glowColor=rgb(color) + (1.0,), glowSize=glow,
             pulseEffects=pulse, pulseInterval=interval if pulse else None, pulseRadius=float(radius) if pulse else None,
             pulseAffects=affects if pulse else None, damageScale=damage_scale if damage_scale != 1.0 else None)


def turret(tid, label, desc, *, color, color2=None, damage_def, damage, range_=24.9, cooldown=1.5, burst=1, burst_ticks=8, speed=55,
           hp=300, lifetime=3600, glow=1.8, size=1.4, top_size=1.3, pen=0.3, hediff=None, hediff_chance=0.3, hediff_sev=0.5,
           trail="AA_Fleck_Mote", proj_tex="ArcanumAscendant/Vfx/Orb", proj_size=0.7, sound="AA_Turret_Shot", shape="crystal",
           explosion_radius=None, pulse=None, pulse_radius=4.0, pulse_interval=120):
    color2 = color2 or color
    base = "ArcanumAscendant/Things/" + tid
    top = "ArcanumAscendant/Things/" + tid + "_Top"
    THING_ICONS.append((base, shape + "_base", color, color2))
    THING_ICONS.append((top, shape + "_top", color, color2))
    proj = "AA_Bolt_" + tid.replace("Construct_", "")
    gun = "AA_Gun_" + tid.replace("Construct_", "")
    out = []
    pp = dict(damageDef=damage_def, damageAmountBase=int(damage), speed=float(speed), armorPenetrationBase=float(pen))
    if explosion_radius:
        pp["explosionRadius"] = float(explosion_radius)
    out.append(to_xml("ThingDef", O(None, defName=proj, label=label + " bolt", thingClass="ArcanumAscendant.Projectile_Arcane",
        category="Projectile", tickerType="Normal", altitudeLayer="Projectile", useHitPoints=False, neverMultiSelect=True,
        graphicData=O(None, texPath=proj_tex, graphicClass="Graphic_Single", shaderType="MoteGlow", drawSize=proj_size, color=rgb(color2) + (1.0,)),
        projectile=O(None, **pp),
        modExtensions=[O("ArcanumAscendant.ProjectileGlowExtension", color=rgb(color) + (1.0,), glowSize=proj_size * 2.2, trailFleck=trail,
                         impactHediff=hediff, impactHediffChance=hediff_chance if hediff else None, impactHediffSeverity=hediff_sev if hediff else None)]), 1))
    out.append(to_xml("ThingDef", O(None, defName=gun, label=label + " focus", description="The focusing crystal of a conjured construct.",
        thingClass="ThingWithComps", category="Item", equipmentType="Primary", tradeability="None", destroyOnDrop=True, useHitPoints=False,
        graphicData=O(None, texPath=top, graphicClass="Graphic_Single", drawSize=top_size),
        statBases={"AccuracyTouch": 0.9, "AccuracyShort": 0.85, "AccuracyMedium": 0.8, "AccuracyLong": 0.7, "RangedWeapon_Cooldown": float(cooldown), "Mass": 1.0, "Flammability": 0.0},
        comps=[O(None, compClass="CompEquippable")],
        verbs=[O(None, verbClass="Verb_Shoot", hasStandardCommand=True, defaultProjectile=proj, warmupTime=0.0, range=float(range_),
                 burstShotCount=burst, ticksBetweenBurstShots=burst_ticks, soundCast=sound, muzzleFlashScale=6.0)]), 1))
    out.append(to_xml("ThingDef", O(None, defName="AA_" + tid, label=label, description=desc, thingClass="Building_TurretGun", category="Building",
        tickerType="Normal", altitudeLayer="Building", drawerType="MapMeshAndRealTime", selectable=True, useHitPoints=True, rotatable=False,
        passability="PassThroughOnly", pathCost=50, fillPercent=0.5, hasTooltip=True, castEdgeShadows=True,
        graphicData=O(None, texPath=base, graphicClass="Graphic_Single", drawSize=size, shadowData=O(None, volume=(0.3, 0.5, 0.3), offset=(0, 0, -0.2))),
        statBases={"MaxHitPoints": int(hp), "Flammability": 0.0, "ShootingAccuracyTurret": 0.97, "Beauty": 0},
        building=O(None, turretGunDef=gun, turretBurstCooldownTime=float(cooldown), turretTopDrawSize=top_size, ai_combatDangerous=True, claimable=False,
                   destroySound="AA_Construct_Break"),
        comps=[_construct_comp(color, lifetime, pulse, pulse_radius, pulse_interval, glow=glow)]), 1))
    return out


def wall(tid, label, desc, *, color, color2=None, hp=400, lifetime=1800, shape="icewall", pulse=None, pulse_radius=1.5, pulse_interval=60, glow=1.2):
    color2 = color2 or color
    base = "ArcanumAscendant/Things/" + tid
    THING_ICONS.append((base, shape, color, color2))
    return [to_xml("ThingDef", O(None, defName="AA_" + tid, label=label, description=desc, thingClass="Building", category="Building",
        tickerType="Normal", altitudeLayer="Building", drawerType="MapMeshAndRealTime", selectable=True, useHitPoints=True, rotatable=False,
        passability="Impassable", fillPercent=1.0, blockWind=True, castEdgeShadows=True, staticSunShadowHeight=0.8, holdsRoof=False,
        graphicData=O(None, texPath=base, graphicClass="Graphic_Single", drawSize=1.15),
        statBases={"MaxHitPoints": int(hp), "Flammability": 0.0, "Beauty": 2},
        building=O(None, claimable=False, destroySound="AA_Construct_Break"),
        comps=[_construct_comp(color, lifetime, pulse, pulse_radius, pulse_interval, affects="Enemies", glow=glow)]), 1)]


def totem(tid, label, desc, *, color, color2=None, hp=250, lifetime=3600, shape="totem", pulse, radius=5.0, interval=120, affects="Allies",
          glow=2.2, light=None, heat=None, passable=True, size=1.4):
    color2 = color2 or color
    base = "ArcanumAscendant/Things/" + tid
    THING_ICONS.append((base, shape, color, color2))
    comps = [_construct_comp(color, lifetime, pulse, radius, interval, affects, glow)]
    if light:
        comps.append(O("CompProperties_Glower", glowRadius=float(light), glowColor="(%d,%d,%d,0)" % tuple(int(c * 255) for c in rgb(color))))
    if heat:
        comps.append(O("CompProperties_HeatPusher", compClass="CompHeatPusher", heatPerSecond=float(heat), heatPushMaxTemperature=26.0))
    return [to_xml("ThingDef", O(None, defName="AA_" + tid, label=label, description=desc, thingClass="Building", category="Building",
        tickerType="Normal", altitudeLayer="Building", drawerType="MapMeshAndRealTime", selectable=True, useHitPoints=True, rotatable=False,
        passability="PassThroughOnly" if passable else "Impassable", pathCost=30, fillPercent=0.35,
        graphicData=O(None, texPath=base, graphicClass="Graphic_Single", drawSize=size),
        statBases={"MaxHitPoints": int(hp), "Flammability": 0.0, "Beauty": 5},
        building=O(None, claimable=False, destroySound="AA_Construct_Break"),
        comps=comps), 1)]

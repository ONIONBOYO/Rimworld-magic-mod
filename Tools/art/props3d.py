"""
Modelled objects for the Starcaller and Wyrmcaller: the astral orrery, the dragon's hoard, the wyrm nest, eggs, a handful of scales
and the diving drakeling used as an effect. Same raymarcher as the wyrm (sdf3d.py).

    python3 Tools/art/props3d.py            # everything
    python3 Tools/art/props3d.py Orrery     # one
"""
import math
import os
import random
import sys

import numpy as np
from PIL import Image

import sdf3d as S
from sdf3d import F, v3, unit

HERE = os.path.dirname(os.path.abspath(__file__))
TEX = os.path.normpath(os.path.join(HERE, "..", "..", "Textures", "ArcanumAscendant"))


def rgb(*c):
    return np.array(c, F) / 255.0


def shade(buf, mats, ss, outline=(0.06, 0.05, 0.07), bias=None, glow=None):
    """Generic painter. mats: group name -> dict(col=rgb or fn(P, N) -> (M, 3), spec, shin, metal, emit).
    glow: (rgb, radius_px, strength) adds a soft halo around emissive parts."""
    hit = buf["hit"]
    n = buf["n"]
    f, r, u = buf["cam"]
    names = buf["names"]
    idx = np.nonzero(hit.reshape(-1))[0]
    P = buf["P"].reshape(-1, 3)[idx]
    N = buf["N"].reshape(-1, 3)[idx]
    ao = buf["ao"].reshape(-1)[idx]
    sh = buf["shadow"].reshape(-1)[idx]
    gd = buf["gd"].reshape(len(names), -1)[:, idx].copy()
    for k, b in (bias or {}).items():
        gd[names.index(k)] -= b
    mat = np.argmin(gd, axis=0)
    V = -f
    L = unit(-0.55 * r + 0.75 * u - 0.5 * f)
    Hh = unit(L + V)
    ndl = np.clip(N @ L, 0, 1)
    ndv = np.clip(N @ V, 0, 1)
    ndh = np.clip(N @ Hh, 0, 1)
    col = np.zeros_like(P)
    emit_mask = np.zeros(len(P), F)
    toon = 0.55 * ndl + 0.45 * (0.5 * S.smoothstep(0.06, 0.16, ndl) + 0.5 * S.smoothstep(0.48, 0.6, ndl))
    shadow = 0.3 + 0.7 * sh
    amb = (0.40 + 0.16 * N[:, 1]) * (0.30 + 0.70 * ao)
    for gi, name in enumerate(names):
        m = mat == gi
        if not m.any():
            continue
        md = mats[name]
        c = md["col"](P[m], N[m]) if callable(md["col"]) else np.asarray(md["col"], F)[None] * np.ones((int(m.sum()), 3), F)
        lit = c * (amb[m, None] * np.array([0.82, 0.9, 1.0], F) + (0.92 * toon[m] * shadow[m])[:, None] * np.array([1.0, 0.97, 0.9], F))
        spec = md.get("spec", 0.2) * ndh[m] ** md.get("shin", 24) * shadow[m]
        if md.get("metal"):
            scr = N[m] @ u
            env = 0.28 + 0.6 * S.smoothstep(-0.3, 0.9, scr) + 0.55 * ndh[m] ** 5 + 0.22 * S.smoothstep(0.5, 0.75, np.abs(np.sin(scr * 7.0 + N[m] @ r * 3.0)))
            lit = 0.2 * lit + 0.95 * c * env[:, None] * (0.35 + 0.65 * ao[m])[:, None] * (0.45 + 0.55 * shadow[m])[:, None]
            lit += spec[:, None] * (0.5 + 0.5 * c)
        else:
            lit += spec[:, None]
        rim = ((1.0 - ndv[m]) ** 2.6) * np.clip(N[m] @ u * 0.7 + 0.5, 0, 1) * 0.35
        lit += rim[:, None] * (0.5 + 0.5 * c)
        e = md.get("emit", 0.0)
        if callable(e):
            ev = e(P[m], N[m])
            lit = lit * (1 - ev[:, None] * 0.6) + ev[:, None] * np.asarray(md["emit_col"], F)[None]
            emit_mask[m] = ev
        elif e:
            lit = c * (e + 0.35 * ndv[m, None]) + lit * (1 - e) * 0.3
            emit_mask[m] = e
        col[m] = lit

    img = np.zeros((n * n, 3), F)
    img[idx] = np.clip(col, 0, 1)
    img = img.reshape(n, n, 3)
    line = S.inner_lines(buf, buf["extent"] * 0.02)
    line = np.maximum(line, S.dilate_mask(line, max(0, ss - 1)) * 0.85)
    img = img * (1.0 - 0.6 * line[..., None])
    hitf = hit.astype(F)
    grown = S.dilate_mask(hitf, max(1, int(round(ss * 1.25))))
    soft = np.maximum(grown, S.blur_mask(grown, ss * 0.6))
    out_rgb = np.where(hit[..., None], img, np.asarray(outline, F)[None, None] * np.ones_like(img))
    out_a = np.where(hit, 1.0, soft * 0.95)
    if glow is not None:
        gcol, rad, strength = glow
        em = np.zeros(n * n, F)
        em[idx] = emit_mask
        halo = S.blur_mask(em.reshape(n, n), rad * ss) * strength
        out_rgb = np.clip(out_rgb + halo[..., None] * np.asarray(gcol, F)[None, None], 0, 1)
        extra = np.clip(halo, 0, 1) * (1 - out_a)
        out_rgb = np.where((out_a < 0.5)[..., None], np.asarray(gcol, F)[None, None] * np.ones_like(out_rgb), out_rgb)
        out_a = np.clip(out_a + extra * 0.8, 0, 1)
    pre = np.concatenate([out_rgb * out_a[..., None], out_a[..., None]], -1)
    small = S.box_down(pre, ss)
    al = small[..., 3:4]
    return np.concatenate([np.where(al > 1e-4, small[..., :3] / np.maximum(al, 1e-4), 0), al], -1)


def to_image(rgba):
    return Image.fromarray((np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA")


def write(img, rel):
    path = os.path.join(TEX, rel + ".png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, optimize=True)
    print("  wrote", rel)


def march(sc, px, ss, extent, center, pitch=56.0, facing="south", ao=0.05):
    cam = S.camera(facing, pitch)
    f, r, u = cam
    L = unit(-0.55 * r + 0.75 * u - 0.5 * f)
    return S.render(sc, px * ss, cam, center, extent, shadow_dir=L, ao_scale=ao * extent)


# ------------------------------------------------------------------------------------------------ materials

BRASS = dict(col=rgb(214, 160, 62), spec=0.9, shin=30, metal=True)
GOLD = dict(col=rgb(255, 205, 70), spec=1.0, shin=34, metal=True)
STEEL = dict(col=rgb(225, 232, 245), spec=1.0, shin=40, metal=True, emit=0.25)


def stone(P, N):
    n = S.value_noise(P, 9.0)
    return (rgb(70, 72, 92)[None] * (0.8 + 0.4 * n)[:, None]).astype(F)


def wood(P, N):
    n = S.value_noise(P * np.array([3.0, 14.0, 3.0], F), 2.0)
    return (rgb(58, 40, 34)[None] * (0.65 + 0.7 * n)[:, None]).astype(F)


def ash(P, N):
    n = S.value_noise(P, 14.0)
    f1, f2, _ = S.worley(P, 9.0)
    crack = S.smoothstep(0.1, 0.0, f2 - f1)
    base = rgb(44, 34, 34)[None] * (0.7 + 0.6 * n)[:, None]
    return (base + crack[:, None] * rgb(255, 110, 40)[None] * 0.8).astype(F)


def egg_col(base, speck, freq=7.0, scales=True):
    def fn(P, N):
        f1, f2, cell = S.worley(P, freq)
        n = S.value_noise(P, 20.0)
        c = base[None] * (0.86 + 0.2 * n)[:, None]
        if scales:
            edge = S.smoothstep(0.02, 0.2, f2 - f1)
            c = c * (0.72 + 0.28 * edge)[:, None] * (0.9 + 0.2 * cell)[:, None]
        s1, _, _ = S.worley(P + 3.1, freq * 1.7)
        dots = S.smoothstep(0.2, 0.1, s1)
        return (c * (1 - dots[:, None]) + speck[None] * dots[:, None]).astype(F)
    return fn


def egg_shape(c, h, w, basis=None):
    """Fat at the bottom, narrower at the top."""
    c = v3(c)
    up = v3(0, 1, 0) if basis is None else np.asarray(basis, F)[1]
    return S.Blend([S.Ellipsoid(c - up * h * 0.12, v3(w, h * 0.86, w), basis), S.Ellipsoid(c + up * h * 0.22, v3(w * 0.78, h * 0.78, w * 0.78), basis)], w * 0.35)


# ------------------------------------------------------------------------------------------------ the orrery

def orrery():
    sc = S.Scene()
    base = sc.group("stone", 0.02)
    brass = sc.group("brass", 0.012, 0.01)
    star = sc.group("star", 0.0, 0.0)
    p1 = sc.group("planet1", 0, 0)
    p2 = sc.group("planet2", 0, 0)
    p3 = sc.group("planet3", 0, 0)
    base.add(S.Cylinder((0, 0.06, 0), (0, 1, 0), 0.62, 0.06, 0.02), S.Cylinder((0, 0.17, 0), (0, 1, 0), 0.46, 0.05, 0.02))
    brass.add(S.Torus((0, 0.13, 0), (0, 1, 0), 0.62, 0.025), S.Torus((0, 0.23, 0), (0, 1, 0), 0.46, 0.022))
    brass.cone((0, 0.2, 0), (0, 0.62, 0), 0.085, 0.045)
    brass.add(S.Sphere((0, 0.42, 0), 0.085))
    c = v3(0, 1.05, 0)
    # the yoke that carries the rings
    brass.cone((0, 0.62, 0), c - v3(0, 0.26, 0), 0.04, 0.03)
    star.add(S.Sphere(c, 0.17))
    rings = [(0.36, unit((0.25, 1, 0.1)), 0.022), (0.56, unit((-0.5, 1, 0.35)), 0.024), (0.76, unit((0.35, 1, -0.6)), 0.026)]
    planets = [(p1, 0.065, 1.1), (p2, 0.085, 3.4), (p3, 0.075, 5.2)]
    for (R, ax, rr), (grp, pr, ang) in zip(rings, planets):
        brass.add(S.Torus(c, ax, R, rr))
        a = unit(np.cross(ax, v3(0, 0, 1)))
        b = np.cross(ax, a)
        grp.add(S.Sphere(c + (a * math.cos(ang) + b * math.sin(ang)) * R, pr))
        # little collars where the planet rides the ring
        brass.add(S.Sphere(c + (a * math.cos(ang + math.pi) + b * math.sin(ang + math.pi)) * R, rr * 1.7))
    # an outer meridian ring standing upright, with pointers
    brass.add(S.Torus(c, (0, 0, 1), 0.93, 0.02))
    for ang in range(0, 360, 45):
        t = math.radians(ang)
        d = v3(math.cos(t), math.sin(t), 0)
        brass.cone(c + d * 0.93, c + d * 1.02, 0.022, 0.004)
    buf = march(sc, 256, 2, 1.32, (0, 0.72, 0), pitch=48.0)
    mats = {"stone": dict(col=stone, spec=0.15), "brass": BRASS,
            "star": dict(col=rgb(255, 246, 200), emit=1.0), "planet1": dict(col=rgb(120, 170, 255), emit=0.55, spec=0.6),
            "planet2": dict(col=rgb(255, 150, 110), emit=0.5, spec=0.6), "planet3": dict(col=rgb(190, 255, 220), emit=0.5, spec=0.6)}
    write(to_image(shade(buf, mats, 2, outline=(0.05, 0.05, 0.12), glow=(rgb(160, 180, 255), 7, 0.9))), "Things/Orrery")


# ------------------------------------------------------------------------------------------------ the hoard

def hoard():
    rng = random.Random(4)
    sc = S.Scene()
    gold = sc.group("gold", 0.03)
    coins = sc.group("coins", 0.0, 0.004)
    steel = sc.group("steel", 0.004, 0.0)
    ruby = sc.group("ruby", 0, 0)
    emerald = sc.group("emerald", 0, 0)
    sapphire = sc.group("sapphire", 0, 0)
    # a ring-shaped heap with the hollow where the wyrm sleeps
    heap = S.Blend([S.Ellipsoid((0, 0.0, 0), v3(0.98, 0.36, 0.86)), S.Ellipsoid((-0.45, 0.05, -0.3), v3(0.42, 0.4, 0.4)),
                    S.Ellipsoid((0.5, 0.02, -0.25), v3(0.4, 0.34, 0.42)), S.Ellipsoid((0.1, 0.0, 0.45), v3(0.5, 0.26, 0.36))], 0.18)
    gold.add(S.Cut(heap, S.Ellipsoid((0.02, 0.5, 0.08), v3(0.42, 0.34, 0.36))))

    def surface(x, z):
        """Rough height of the heap at (x, z)."""
        h = 0.36 * math.sqrt(max(0.0, 1 - (x / 0.98) ** 2 - (z / 0.86) ** 2))
        h = max(h, 0.05 + 0.4 * math.sqrt(max(0.0, 1 - ((x + 0.45) / 0.42) ** 2 - ((z + 0.3) / 0.4) ** 2)))
        h = max(h, 0.02 + 0.34 * math.sqrt(max(0.0, 1 - ((x - 0.5) / 0.4) ** 2 - ((z + 0.25) / 0.42) ** 2)))
        hollow = 0.5 - 0.34 * math.sqrt(max(0.0, 1 - ((x - 0.02) / 0.42) ** 2 - ((z - 0.08) / 0.36) ** 2))
        return min(h, hollow) if ((x - 0.02) / 0.42) ** 2 + ((z - 0.08) / 0.36) ** 2 < 1 else h

    for _ in range(95):
        ang, rad = rng.uniform(0, 6.283), 1.12 * math.sqrt(rng.random())
        x, z = rad * math.cos(ang), rad * 0.9 * math.sin(ang)
        y = surface(x, z)
        tilt = unit((rng.uniform(-0.5, 0.5), 1.0, rng.uniform(-0.5, 0.5))) if y > 0.02 else unit((rng.uniform(-0.12, 0.12), 1, rng.uniform(-0.12, 0.12)))
        coins.add(S.Cylinder((x, y + 0.012, z), tilt, rng.uniform(0.05, 0.075), 0.011, 0.004))
    for grp, n in ((ruby, 5), (emerald, 4), (sapphire, 4)):
        for _ in range(n):
            ang, rad = rng.uniform(0, 6.283), rng.uniform(0.3, 1.0)
            x, z = rad * math.cos(ang), rad * 0.85 * math.sin(ang)
            y = surface(x, z)
            sz = rng.uniform(0.04, 0.062)
            grp.add(S.Box((x, y + sz * 0.7, z), v3(sz, sz, sz) * 0.62, S.rot_y(rng.uniform(0, 3)) @ S.rot_x(0.9553) @ S.rot_z(math.pi / 4), 0.006))
    # a sword driven into the heap
    sb = S.rot_z(-0.32) @ S.rot_x(0.2)
    top = v3(-0.52, 0.42, -0.28)
    axis = sb @ v3(0, 1, 0)
    steel.add(S.Box(top + axis * 0.3, v3(0.035, 0.32, 0.008), sb, 0.004))
    gold.add(S.Box(top + axis * 0.64, v3(0.14, 0.022, 0.025), sb, 0.008), S.Cone(top + axis * 0.66, top + axis * 0.82, 0.024, 0.022), S.Sphere(top + axis * 0.85, 0.042))
    ruby.add(S.Sphere(top + axis * 0.64 + (sb @ v3(0, 0, 0.03)), 0.028))
    # a crown and a goblet
    cc = v3(0.52, 0.36, -0.22)
    gold.add(S.Torus(cc, unit((0.2, 1, 0.1)), 0.13, 0.03))
    for i in range(6):
        t = i * math.pi / 3
        base_p = cc + v3(math.cos(t) * 0.13, 0.02, math.sin(t) * 0.13)
        gold.cone(base_p, base_p + v3(0, 0.1, 0), 0.028, 0.006)
        if i % 2 == 0:
            sapphire.add(S.Sphere(base_p + v3(0, 0.1, 0), 0.022))
    gc = v3(0.2, 0.3, 0.5)
    gold.cone(gc, gc + v3(0.03, 0.14, 0), 0.07, 0.022)
    gold.add(S.Cut(S.Ellipsoid(gc + v3(0.045, 0.24, 0), v3(0.085, 0.1, 0.085)), S.Ellipsoid(gc + v3(0.055, 0.3, 0), v3(0.068, 0.09, 0.068))))
    buf = march(sc, 256, 2, 1.36, (0, 0.2, 0.0), pitch=56.0)

    def heap_col(P, N):
        f1, f2, cell = S.worley(P * np.array([1.0, 1.7, 1.0], F), 13.0)
        edge = S.smoothstep(0.03, 0.2, f2 - f1)
        return (rgb(250, 190, 56)[None] * (0.76 + 0.24 * edge)[:, None] * (0.88 + 0.24 * cell)[:, None]).astype(F)

    mats = {"gold": dict(col=heap_col, spec=1.0, shin=30, metal=True), "coins": GOLD, "steel": STEEL,
            "ruby": dict(col=rgb(255, 50, 70), emit=0.3, spec=1.0, shin=50), "emerald": dict(col=rgb(60, 240, 140), emit=0.3, spec=1.0, shin=50),
            "sapphire": dict(col=rgb(80, 150, 255), emit=0.3, spec=1.0, shin=50)}
    write(to_image(shade(buf, mats, 2, outline=(0.22, 0.11, 0.02), bias={"coins": 0.004}, glow=(rgb(255, 220, 140), 4, 0.5))), "Things/WyrmHoard")


# ------------------------------------------------------------------------------------------------ the nest and the eggs

def nest():
    rng = random.Random(11)
    sc = S.Scene()
    bed = sc.group("ash", 0.05)
    twig = sc.group("wood", 0.012, 0.02)
    bone = sc.group("bone", 0.01, 0.006)
    eggs = sc.group("egg", 0.0, 0.01)
    scale = sc.group("scale", 0.0, 0.004)
    bed.add(S.Cut(S.Ellipsoid((0, 0.02, 0), v3(1.0, 0.22, 0.92)), S.Ellipsoid((0, 0.36, 0), v3(0.7, 0.3, 0.64))))
    for i in range(70):
        a0 = rng.uniform(0, 6.283)
        span = rng.uniform(0.5, 1.0)
        R0 = rng.uniform(0.74, 0.98)
        h = rng.uniform(0.1, 0.36) - (R0 - 0.74) * 0.3
        p = v3(R0 * math.cos(a0), h, R0 * 0.92 * math.sin(a0))
        q = v3(R0 * math.cos(a0 + span), h + rng.uniform(-0.08, 0.1), R0 * 0.92 * math.sin(a0 + span))
        twig.cone(p, q, rng.uniform(0.03, 0.05), rng.uniform(0.015, 0.03))
    for i in range(7):
        a0 = rng.uniform(0, 6.283)
        R0 = rng.uniform(0.8, 1.0)
        p = v3(R0 * math.cos(a0), 0.3, R0 * 0.9 * math.sin(a0))
        d = unit((rng.uniform(-1, 1), rng.uniform(0.0, 0.5), rng.uniform(-1, 1))) * rng.uniform(0.22, 0.34)
        bone.cone(p, p + d, 0.022, 0.018)
        bone.add(S.Sphere(p, 0.035), S.Sphere(p + d, 0.035))
    for (x, z, tilt) in ((-0.2, -0.1, 0.25), (0.22, -0.02, -0.3), (0.0, 0.26, 0.1)):
        b = S.rot_z(tilt) @ S.rot_x(tilt * 0.6)
        eggs.add(egg_shape((x, 0.3, z), 0.3, 0.2, b))
    for i in range(14):
        a0 = rng.uniform(0, 6.283)
        R0 = rng.uniform(0.3, 0.68)
        p = v3(R0 * math.cos(a0), 0.12 + 0.2 * (R0 / 0.68) ** 2, R0 * 0.9 * math.sin(a0))
        scale.add(S.Ellipsoid(p, v3(0.07, 0.012, 0.09), S.rot_y(rng.uniform(0, 3)) @ S.rot_x(rng.uniform(-0.4, 0.4))))
    buf = march(sc, 256, 2, 1.3, (0, 0.2, 0), pitch=58.0)
    mats = {"ash": dict(col=ash, spec=0.05), "wood": dict(col=wood, spec=0.1), "bone": dict(col=rgb(236, 226, 204), spec=0.25),
            "egg": dict(col=egg_col(rgb(190, 60, 52), rgb(255, 214, 150)), spec=0.6, shin=30),
            "scale": dict(col=rgb(170, 40, 44), spec=0.9, shin=30, metal=True)}
    write(to_image(shade(buf, mats, 2, outline=(0.08, 0.04, 0.04))), "Things/WyrmNest")


def crack_glow(P, N):
    f1, f2, _ = S.worley(P, 3.6)
    n = S.value_noise(P, 9.0)
    return S.smoothstep(0.09, 0.02, f2 - f1 + 0.05 * (n - 0.5)).astype(F)


def single_egg(rel, px, base, speck, cracked=False, glow=None, extent=0.56):
    sc = S.Scene()
    egg = sc.group("egg", 0.0)
    egg.add(egg_shape((0, 0.42, 0), 0.4, 0.27, S.rot_z(0.14)))
    buf = march(sc, px, 2, extent, (0, 0.4, 0), pitch=50.0, ao=0.08)
    mat = dict(col=egg_col(base, speck, 6.0), spec=0.7, shin=28)
    if cracked:
        mat.update(emit=crack_glow, emit_col=rgb(255, 190, 90))
    write(to_image(shade(buf, {"egg": mat}, 2, outline=(0.1, 0.04, 0.04), glow=glow)), rel)
    return buf


def eggs():
    single_egg("Things/ClutchEgg", 128, rgb(178, 44, 44), rgb(255, 205, 140), cracked=True, glow=(rgb(255, 170, 80), 5, 0.8))
    single_egg("Items/WyrmEgg", 128, rgb(60, 170, 130), rgb(250, 228, 160), extent=0.66)
    # the thrown egg: a white-hot silhouette, tinted and lit in game
    sc = S.Scene()
    sc.group("egg", 0.0).add(egg_shape((0, 0, 0), 0.4, 0.27))
    cam = S.camera("north", 89.0)
    buf = S.render(sc, 256, cam, (0, 0, 0.0), 0.9, shadow_dir=None, ao_scale=0.05)
    hit = buf["hit"].astype(F)
    f, r, u = cam
    lit = np.clip(buf["N"] @ unit(-0.4 * r + 0.5 * u - 0.75 * f), 0, 1)
    f1, f2, _ = S.worley(buf["P"].reshape(-1, 3), 6.0)
    edge = S.smoothstep(0.02, 0.2, (f2 - f1)).reshape(256, 256)
    a = hit * (0.45 + 0.55 * lit) * (0.7 + 0.3 * edge)
    a = np.clip(a + S.blur_mask(hit, 9) * 0.55, 0, 1)
    a = S.box_down(a[..., None], 2)[..., 0]
    img = np.concatenate([np.ones((128, 128, 3), F), a[..., None]], -1)
    write(to_image(img), "Vfx/EggShot")


def scales():
    sc = S.Scene()
    g = sc.group("scale", 0.0)
    for (x, z, rot, tilt, s) in ((-0.2, 0.1, 0.55, 0.22, 1.0), (0.24, 0.08, -0.45, 0.28, 0.92), (0.02, -0.14, 0.05, 0.18, 1.1)):
        b = S.rot_y(rot) @ S.rot_x(tilt)
        c = v3(x, 0.07 + 0.05 * s, z)

        def W(lx, ly, lz):
            return c + b.T @ (v3(lx, ly, lz) * s)
        # a shield: broad shoulders, a point, and a keel down the middle
        A, B, C, D = W(-0.21, 0, -0.1), W(0.21, 0, -0.1), W(0, 0, 0.4), W(0, 0, -0.27)
        g.add(S.Blend([S.Tri(A, B, C, 0.028 * s), S.Tri(A, D, B, 0.028 * s), S.Cone(W(0, 0.02, -0.2), W(0, 0.03, 0.36), 0.035 * s, 0.01 * s)], 0.04))
    buf = march(sc, 128, 2, 0.62, (0, 0.08, 0), pitch=62.0, ao=0.06)

    def col(P, N):
        n = S.value_noise(P, 26.0)
        f1, f2, cell = S.worley(P, 30.0)
        return (rgb(48, 190, 140)[None] * (0.8 + 0.12 * S.smoothstep(0.02, 0.2, f2 - f1) + 0.12 * n)[:, None]).astype(F)
    write(to_image(shade(buf, {"scale": dict(col=col, spec=1.0, shin=24, metal=True)}, 2, outline=(0.02, 0.12, 0.09))), "Items/WyrmScale")


def swoop():
    """A drakeling seen from straight above, diving: used as a streak of light when the brood hunts."""
    import dragon3d
    sc, info = dragon3d.build(dict(dragon3d.STAGES["Drakeling"], wing=0.8), "spread")
    cam = S.camera("north", 89.0)
    n = 512
    buf = S.render(sc, n, cam, (0, 0.3, 0.05), 1.75, shadow_dir=None, ao_scale=0.04)
    hit = buf["hit"].astype(F)
    f, r, u = cam
    lit = np.clip(buf["N"] @ unit(-0.4 * r + 0.5 * u - 0.75 * f), 0, 1)
    a = hit * (0.5 + 0.5 * lit) * (0.5 + 0.5 * buf["ao"])
    a = np.clip(a + S.blur_mask(hit, 12) * 0.5, 0, 1)
    a = S.box_down(a[..., None], 2)[..., 0]
    img = np.concatenate([np.ones((256, 256, 3), F), a[..., None]], -1)
    write(to_image(img), "Vfx/DrakeSwoop")


ALL = {"Orrery": orrery, "Hoard": hoard, "Nest": nest, "Eggs": eggs, "Scales": scales, "Swoop": swoop}


def make_all():
    for fn in ALL.values():
        fn()


if __name__ == "__main__":
    if len(sys.argv) > 1:
        for a in sys.argv[1:]:
            ALL[a]()
    else:
        make_all()
        print("Props done.")

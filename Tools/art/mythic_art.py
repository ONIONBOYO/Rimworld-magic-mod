"""
Textures for the Harbinger, Wind Mage, Succubus, Incubus and Noclipper: particles, projectiles, casting circles,
ground textures, status overlays, constructs, the two Backrooms entities and Almond Water.

Called from make_textures.main(); can also be run on its own:  python3 Tools/art/mythic_art.py
"""
import math
import random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

import make_vfx2
import motifs
from make_textures import (BLACK, WHITE, arr, blur, dilate, glyph, hexc, mix, noise, over, save, solid, sprite, styled_glyph,
                           to_rgba, white)
from make_vfx2 import coords, down, edge, rgba


def pen(size, fn, k=4):
    p = motifs.Pen(size * k)
    fn(p)
    return p.img.resize((size, size), Image.LANCZOS)


def glowy(mask, size, core=1.0, halo=0.8, r=None):
    """White additive sprite: crisp core plus a soft halo."""
    r = r if r is not None else size * 0.05
    a = arr(mask) * core + arr(blur(mask, r)) * halo
    return white(np.clip(a, 0, 1), size)


# ------------------------------------------------------------------------------------------------ particles

def particles():
    # gust: a curved, tapering wind streak (vertical = direction of travel)
    S = 64
    def gust_fn(p):
        for off, wmax, bend in ((0.0, 0.07, 0.16), (0.2, 0.035, 0.1), (-0.17, 0.03, 0.08)):
            left, right = [], []
            for i in range(31):
                t = i / 30
                y = -0.86 + 1.72 * t
                x = off + bend * math.sin(math.pi * t) - bend * 0.5
                w = wmax * math.sin(math.pi * t) ** 0.8 + 0.004
                left.append((x - w, y))
                right.append((x + w, y))
            p.poly(left + list(reversed(right)))
    m = pen(S, gust_fn)
    save(glowy(m, S, 0.9, 0.6, 2), "Vfx/Gust")

    # glitch: hard-edged bars and blocks of corrupted picture
    S = 64
    rng = random.Random(77)
    img = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(img)
    for _ in range(9):
        h = rng.choice([2, 3, 4, 6])
        w = rng.randint(8, 40)
        x = rng.randint(4, S - w - 4)
        y = rng.randint(6, S - h - 6)
        d.rectangle([x, y, x + w, y + h], fill=rng.choice([255, 200, 140]))
    for _ in range(6):
        x, y = rng.randint(8, S - 12), rng.randint(8, S - 12)
        sz = rng.choice([3, 4, 5])
        d.rectangle([x, y, x + sz, y + sz], fill=255)
    a = arr(img) * np.clip(1.2 - coords(S)[2] * 0.8, 0, 1)
    save(white(a, S), "Vfx/Glitch")

    # omen eye: an almond eye with a slit pupil, ringed by rays
    S = 128
    def eye_fn(p):
        ps = [(0.62 * math.cos(t), 0.32 * math.sin(t) * abs(math.sin(t)) ** 0.25) for t in [i * math.pi / 30 for i in range(61)]]
        p.poly(ps)
        inner = [(0.5 * math.cos(t), 0.22 * math.sin(t) * abs(math.sin(t)) ** 0.25) for t in [i * math.pi / 30 for i in range(61)]]
        p.poly(inner, 0)
        p.circle(0, 0, 0.2)
        p.ellipse(0, 0, 0.05, 0.17, 0)
        for i in range(12):
            a = math.radians(i * 30)
            r0 = 0.72 if i % 3 else 0.68
            p.line([(r0 * math.cos(a), r0 * 0.75 * math.sin(a)), (0.92 * math.cos(a), 0.92 * 0.75 * math.sin(a))], 0.025)
    m = pen(S, eye_fn)
    save(glowy(m, S, 1.0, 0.9, 4), "Vfx/OmenEye")

    # omen seal: seven seals around a heptagram, an eye at the heart
    S = 256
    def seal_fn(p):
        p.ring(0, 0, 0.95, 0.035)
        p.ring(0, 0, 0.84, 0.012)
        for i in range(84):
            a = math.radians(i * 360 / 84)
            r0 = 0.865 if i % 2 else 0.88
            p.line([(r0 * math.cos(a), r0 * math.sin(a)), (0.93 * math.cos(a), 0.93 * math.sin(a))], 0.008, 255, False)
        for i in range(7):
            a = math.radians(90 + i * 360 / 7)
            cx, cy = 0.72 * math.cos(a), 0.72 * math.sin(a)
            p.circle(cx, cy, 0.1)
            p.circle(cx, cy, 0.075, 0)
            p.circle(cx, cy, 0.035)
        pts = [(0.6 * math.cos(math.radians(90 + i * 360 * 3 / 7)), 0.6 * math.sin(math.radians(90 + i * 360 * 3 / 7))) for i in range(8)]
        p.line(pts, 0.018, 255, False)
        p.ring(0, 0, 0.3, 0.02)
        ps = [(0.22 * math.cos(t), 0.12 * math.sin(t) * abs(math.sin(t)) ** 0.25) for t in [i * math.pi / 30 for i in range(61)]]
        p.poly(ps)
        p.circle(0, 0, 0.07, 0)
        p.circle(0, 0, 0.035)
    m = pen(S, seal_fn, 3)
    save(glowy(m, S, 1.0, 0.7, 5), "Vfx/OmenSeal")

    # crow: a dark silhouette in flight (alpha blended, tinted black)
    S = 64
    m = blur(motifs.mask("crow", S, 0.92), 0.5)
    save(white(arr(m), S), "Vfx/Crow")

    # kiss: glowing lips
    S = 64
    m = motifs.mask("lips", S, 0.85)
    save(glowy(m, S, 1.0, 0.7, 3), "Vfx/Kiss")

    # smile: two eyes and a grin full of teeth, floating in the dark
    S = 128
    def smile_fn(p):
        p.ellipse(-0.3, 0.3, 0.11, 0.17)
        p.ellipse(0.3, 0.3, 0.11, 0.17)
        top = [(0.75 * t, -0.08 - 0.12 * (1 - t * t)) for t in [-1 + 2 * i / 40 for i in range(41)]]
        bot = [(0.75 * t, -0.12 - 0.5 * (1 - t * t) ** 0.9) for t in [-1 + 2 * i / 40 for i in range(41)]]
        p.poly(top + list(reversed(bot)))
        for i in range(1, 10):
            x = -0.75 + i * 0.15
            p.line([(x, 0.05), (x, -0.7)], 0.022, 0, False)
        p.line([(-0.72, -0.27), (0.72, -0.27)], 0.02, 0, False)
    m = pen(S, smile_fn)
    save(glowy(m, S, 1.0, 0.9, 5), "Vfx/Smile")

    # fluorescent tube: a bright bar in a soft halo
    S = 64
    x, y, r = coords(S)
    core = np.clip(1 - np.abs(y) / 0.05, 0, 1) * np.clip((0.44 - np.abs(x)) / 0.04, 0, 1)
    halo = np.exp(-(y / 0.16) ** 2) * np.clip((0.5 - np.abs(x)) / 0.1, 0, 1) * 0.55
    save(white(np.clip(core + halo, 0, 1) * edge(r, 0.1), S), "Vfx/Tube")

    # confetti: a little paper rectangle (alpha, tinted)
    S = 16
    img = Image.new("L", (S, S), 0)
    ImageDraw.Draw(img).rectangle([3, 5, 12, 10], fill=255)
    save(white(arr(img), S), "Vfx/Confetti")


# ------------------------------------------------------------------------------------------------ projectiles

def projectiles():
    # crow seen from above, flying "up" (forward), wrapped in red light once tinted
    S = 128
    def crow_top(p):
        p.ellipse(0, 0.0, 0.13, 0.34)
        p.circle(0, 0.4, 0.12)
        p.poly([(-0.045, 0.48), (0, 0.66), (0.045, 0.48)])
        for s in (-1, 1):
            p.poly([(s * 0.08, 0.16), (s * 0.45, 0.24), (s * 0.8, 0.14), (s * 0.97, 0.0), (s * 0.86, -0.04), (s * 0.88, -0.13), (s * 0.73, -0.1),
                    (s * 0.74, -0.21), (s * 0.58, -0.15), (s * 0.57, -0.26), (s * 0.41, -0.17), (s * 0.31, -0.24), (s * 0.12, -0.12)])
        p.poly([(-0.1, -0.28), (-0.24, -0.76), (0, -0.66), (0.24, -0.76), (0.1, -0.28)])
    m = pen(S, crow_top)
    save(glowy(m, S, 1.0, 0.9, 5), "Vfx/CrowBolt")

    # wind blade: a crescent cutting forward
    S = 128
    def blade(p):
        outer = [(0.8 * math.cos(a), -0.25 + 0.75 * math.sin(a)) for a in [math.radians(10 + i * 4) for i in range(41)]]
        inner = [(0.62 * math.cos(a), -0.42 + 0.62 * math.sin(a)) for a in [math.radians(14 + i * 3.8) for i in range(41)]]
        p.poly(outer + list(reversed(inner)))
    m = pen(S, blade)
    save(glowy(m, S, 1.0, 0.8, 4), "Vfx/WindBlade")


# ------------------------------------------------------------------------------------------------ casting circles

def circles():
    order = list(make_vfx2.CIRCLES.items())
    for i, (style, (motif, inner)) in enumerate(order):
        if style in ("Wind", "Doom", "Desire", "Liminal"):
            save(make_vfx2.cast_circle(256, motif, inner, 100 + i), "Vfx/Circle_" + style)


# ------------------------------------------------------------------------------------------------ ground textures and overlays

def grounds():
    # the Poolrooms: pale square tiles under rippling light
    S = 256
    x, y, r = coords(S)
    u, v = (x + 0.5) * 8, (y + 0.5) * 8
    grout = np.maximum(np.exp(-((u - np.round(u)) / 0.05) ** 2), np.exp(-((v - np.round(v)) / 0.05) ** 2))
    caust = 0.5 + 0.5 * np.sin(u * 2.1 + np.sin(v * 1.7) * 2) * np.sin(v * 2.3 + np.sin(u * 1.3) * 2)
    a = (0.25 + 0.35 * caust ** 3 + 0.4 * grout) * np.clip((1 - r) / 0.25, 0, 1)
    save(white(np.clip(a, 0, 1), S), "Vfx/Poolrooms")

    # liminal floor: damp carpet in an endless grid of chevrons
    S = 256
    x, y, r = coords(S)
    u, v = (x + 0.5) * 10, (y + 0.5) * 10
    chev = np.abs(((u + np.abs((v % 1) - 0.5) * 1.2) % 1) - 0.5) < 0.12
    n = noise(S, 6, 3, 515)
    a = (0.18 + 0.3 * chev + 0.2 * (n - 0.5)) * np.clip((1 - r) / 0.3, 0, 1)
    save(white(np.clip(a, 0, 1), S), "Vfx/LiminalFloor")

    # black sun: a corona of rays around a dark disc
    S = 256
    x, y, r = coords(S)
    ang = np.arctan2(y, x)
    rays = (0.5 + 0.5 * np.cos(ang * 18)) ** 6 * np.exp(-((r - 0.62) / 0.22) ** 2)
    ring = np.exp(-((r - 0.45) / 0.03) ** 2)
    a = np.clip(rays * 0.8 + ring + 0.15 * np.exp(-((r - 0.55) / 0.25) ** 2), 0, 1) * edge(r, 0.05)
    save(white(a, S), "Vfx/BlackSun")

    # chains: a loop of chain links around the feet
    S = 128
    def chains(p):
        for i in range(14):
            a = i * 360 / 14
            cx, cy = 0.72 * math.cos(math.radians(a)), 0.72 * math.sin(math.radians(a))
            ps = [(cx + 0.11 * math.cos(t) * (1.6 if i % 2 else 0.9), cy + 0.11 * math.sin(t) * (0.9 if i % 2 else 1.6)) for t in [k * math.pi / 12 for k in range(25)]]
            p.line(motifs.rot(ps, a, cx, cy), 0.05)
    m = pen(S, chains)
    save(glowy(m, S, 1.0, 0.5, 3), "Vfx/Chains")

    # REC: the dot and the letters of an old camcorder's viewfinder
    S = 128
    def rec(p):
        p.circle(-0.62, 0.0, 0.2)
        # R
        p.rect(-0.3, -0.25, -0.24, 0.25)
        p.arc(-0.2, 0.12, 0.12, -90, 90, 0.06)
        p.line([(-0.26, 0.0), (-0.1, -0.25)], 0.06)
        # E
        p.rect(0.02, -0.25, 0.08, 0.25)
        p.rect(0.02, 0.19, 0.24, 0.25)
        p.rect(0.02, -0.03, 0.2, 0.03)
        p.rect(0.02, -0.25, 0.24, -0.19)
        # C
        p.arc(0.48, 0.0, 0.2, 60, 300, 0.06)
    m = pen(S, rec)
    save(glowy(m, S, 1.0, 0.6, 3), "Vfx/RecMark")

    # contract mark: a glowing scroll that floats over the debtor's head
    S = 64
    m = motifs.mask("contract", S, 0.85)
    save(glowy(m, S, 1.0, 0.7, 3), "Vfx/ContractMark")


# ------------------------------------------------------------------------------------------------ constructs and items

def things():
    # backrooms wall: a block of mono-yellow wallpaper, water-stained
    S = 128
    rng = random.Random(4)
    block = Image.new("L", (S, S), 0)
    ImageDraw.Draw(block).rectangle([2, 2, S - 3, S - 3], fill=255)
    base = np.array([214, 196, 120], np.float32)
    img = sprite(block, base, S, outline=2)
    x, y, r = coords(S)
    u, v = (x + 0.5) * 6, (y + 0.5) * 6
    chev = (np.abs(((u + np.abs((v % 1) - 0.5) * 1.3) % 1) - 0.5) < 0.1).astype(np.float32)
    stain = np.clip(noise(S, 4, 3, 9) - 0.55, 0, 1) * 2.2
    pattern = Image.fromarray((np.clip(chev * 0.5, 0, 1) * arr(block) * 255).astype(np.uint8))
    img = over(img, solid(S, np.array([180, 160, 85], np.float32), pattern))
    img = over(img, solid(S, np.array([120, 100, 60], np.float32), Image.fromarray((np.clip(stain, 0, 0.6) * arr(block) * 255).astype(np.uint8))))
    edge_m = Image.new("L", (S, S), 0)
    ImageDraw.Draw(edge_m).rectangle([2, 2, S - 3, 14], fill=255)
    img = over(img, solid(S, np.array([235, 225, 170], np.float32), edge_m.point(lambda q: int(q * 0.7))))
    save(img, "Things/WallpaperWall")

    # wind pylon: a spinning knot of air above a small stone
    for name, col in (("WindWall", hexc("#7ef0d2")), ("AegisPylon", hexc("#bfefff"))):
        S = 128
        stone = pen(S, lambda p: p.poly([(0.32 * math.cos(math.radians(a)), 0.32 * math.sin(math.radians(a))) for a in range(0, 360, 60)]))
        img = sprite(stone, np.array([150, 160, 165], np.float32), S, outline=2)
        def swirl(p):
            for k in range(4):
                pts = []
                for i in range(40):
                    t = i / 39
                    a = math.radians(k * 90 + t * 260)
                    rr = 0.2 + 0.68 * t
                    pts.append((rr * math.cos(a), rr * math.sin(a)))
                p.line(pts, 0.05)
        sw = pen(S, swirl)
        img = over(img, solid(S, col, blur(sw, 4).point(lambda q: int(q * 0.8))))
        img = over(img, solid(S, mix(col, WHITE, 0.6), sw.point(lambda q: int(q * 0.85))))
        if name == "AegisPylon":
            ring_m = pen(S, lambda p: p.ring(0, 0, 0.9, 0.04))
            img = over(img, solid(S, mix(col, WHITE, 0.4), ring_m))
        save(img, "Things/" + name)

    # the smiler: a shape of darkness with a grin in it
    S = 128
    x, y, r = coords(S)
    n = noise(S, 5, 3, 31)
    a = np.clip((1 - r / (0.82 + 0.15 * (n - 0.5))) * 3, 0, 1) * 0.92
    img = to_rgba(np.dstack([np.full((S, S), 12.0), np.full((S, S), 10.0), np.full((S, S), 14.0)]), a * 255)
    face = Image.open(make_textures_path("Vfx/Smile")).resize((S, S), Image.LANCZOS)
    fa = np.asarray(face, np.float32)[..., 3] / 255.0
    img = over(img, to_rgba(np.dstack([np.full((S, S), 255.0), np.full((S, S), 250.0), np.full((S, S), 225.0)]), fa * 255))
    save(img, "Things/Smiler")

    # a door to nowhere: an ordinary office door standing on its own, light spilling out
    S = 128
    frame = pen(S, lambda p: p.rect(-0.5, -0.9, 0.5, 0.82))
    img = sprite(frame, np.array([160, 140, 95], np.float32), S, outline=3)
    gap = pen(S, lambda p: p.rect(-0.4, -0.85, 0.4, 0.72))
    img = over(img, solid(S, np.array([255, 245, 190], np.float32), blur(gap, 3)))
    leaf = pen(S, lambda p: p.poly([(-0.4, -0.85), (0.18, -0.8), (0.18, 0.66), (-0.4, 0.72)]))
    img = over(img, sprite(leaf, np.array([205, 185, 120], np.float32), S, outline=1))
    knob = pen(S, lambda p: p.circle(0.08, -0.1, 0.05))
    img = over(img, solid(S, np.array([240, 220, 140], np.float32), knob))
    save(img, "Things/NowhereDoor")

    # hellgate: a ring of black stone around a whirl of hellfire, a pentagram burning in the middle
    S = 256
    x, y, r = coords(S)
    ang = np.arctan2(y, x)
    swirl = 0.5 + 0.5 * np.sin(ang * 3 + r * 14)
    n = noise(S, 6, 3, 66)
    inner = np.clip((0.78 - r) / 0.05, 0, 1)
    fire = np.clip(1.0 - r / 0.78, 0, 1) ** 0.6
    rgb = np.dstack([255 * np.clip(0.6 + 0.4 * swirl, 0, 1), 90 + 120 * swirl * fire, 40 + 180 * (1 - fire) * (0.5 + 0.5 * n)])
    rgb = rgb * (0.55 + 0.45 * fire[..., None]) + np.dstack([120 * (1 - fire), 30 * (1 - fire), 200 * (1 - fire)])
    img = to_rgba(rgb, inner * 255)
    star = pen(S, lambda p: p.line([(0.5 * math.cos(math.radians(90 + i * 144)), 0.5 * math.sin(math.radians(90 + i * 144))) for i in range(6)], 0.025), 3)
    img = over(img, solid(S, np.array([255, 230, 160], np.float32), blur(star, 3)))
    img = over(img, solid(S, WHITE, star))
    stone = pen(S, lambda p: p.ring(0, 0, 0.84, 0.13), 3)
    img = over(img, sprite(stone, np.array([62, 52, 60], np.float32), S, outline=3))
    runes = pen(S, lambda p: [p.line(motifs.rot([(0, 0.79), (0, 0.89)], k * 30), 0.03) for k in range(12)], 3)
    img = over(img, solid(S, np.array([255, 120, 40], np.float32), blur(runes, 2)))
    save(img, "Things/Hellgate")

    # almond water: a plastic bottle of faintly cloudy water
    S = 128
    bottle = pen(S, lambda p: [p.rect(-0.12, 0.45, 0.12, 0.8), p.rect(-0.16, 0.74, 0.16, 0.9),
                               p.poly([(-0.12, 0.45), (-0.36, 0.22), (-0.36, -0.86), (0.36, -0.86), (0.36, 0.22), (0.12, 0.45)])])
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    img = over(img, solid(S, np.array([20, 20, 30], np.float32), blur(dilate(bottle, 2), 0.8)))
    water = np.array([220, 236, 230], np.float32)
    rgbv = np.ones((S, S, 3), np.float32) * water * (0.85 + 0.25 * (0.5 - coords(S)[0])[..., None])
    img = over(img, to_rgba(rgbv, arr(bottle) * 220))
    cap = pen(S, lambda p: p.rect(-0.16, 0.74, 0.16, 0.9))
    img = over(img, solid(S, np.array([70, 120, 200], np.float32), cap))
    label = pen(S, lambda p: p.rect(-0.36, -0.45, 0.36, 0.0))
    img = over(img, solid(S, np.array([240, 230, 205], np.float32), label))
    nut = pen(S, lambda p: [p.ellipse(0, -0.22, 0.1, 0.14)])
    img = over(img, solid(S, np.array([180, 140, 90], np.float32), nut))
    shine = pen(S, lambda p: p.rect(-0.28, -0.8, -0.2, 0.15))
    img = over(img, solid(S, WHITE, shine.point(lambda q: int(q * 0.45))))
    save(img, "Items/AlmondWater")


def make_textures_path(rel):
    import make_textures
    import os
    return os.path.join(make_textures.TEX, rel + ".png")


# ------------------------------------------------------------------------------------------------ the Backrooms entities

def _creature_sheet(draw_fn, name, size=256):
    for facing in ("south", "east", "north"):
        img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        draw_fn(img, facing, size)
        save(img, "Pawns/%s_%s" % (name, facing))


def _shape(size, fn, k=2):
    return pen(size, fn, k)


def _paint(img, mask, rgb, shade=True, outline=3):
    S = img.size[0]
    if outline:
        img = over(img, solid(S, np.array([14, 12, 16], np.float32), blur(dilate(mask, outline), 0.7)))
    x, y, r = coords(S)
    base = np.ones((S, S, 3), np.float32) * np.asarray(rgb, np.float32)
    if shade:
        light = np.clip(1.05 + 0.6 * (-x * 0.5 - y), 0.55, 1.2)
        base = base * light[..., None]
    return over(img, to_rgba(base, arr(mask) * 255))


def hound(img, facing, S):
    skin = (196, 190, 176)
    if facing == "east":
        body = _shape(S, lambda p: [p.ellipse(-0.08, 0.02, 0.44, 0.17), p.circle(0.46, -0.05, 0.15),
                                    p.line([(-0.36, 0.0), (-0.48, -0.32), (-0.52, -0.62)], 0.07), p.line([(-0.2, 0.0), (-0.12, -0.3), (-0.2, -0.62)], 0.065),
                                    p.line([(0.2, 0.05), (0.34, -0.25), (0.28, -0.62)], 0.065), p.line([(0.3, 0.02), (0.5, -0.3), (0.48, -0.62)], 0.07),
                                    p.line([(-0.48, 0.08), (-0.7, 0.2), (-0.78, 0.1)], 0.04)])
        img2 = _paint(img, body, skin)
        mouth = _shape(S, lambda p: p.poly([(0.42, -0.1), (0.66, -0.16), (0.42, -0.2)]))
        img2 = over(img2, solid(S, np.array([30, 8, 10], np.float32), mouth))
        teeth = _shape(S, lambda p: [p.line([(0.47 + i * 0.04, -0.12), (0.48 + i * 0.04, -0.17)], 0.012) for i in range(4)])
        img2 = over(img2, solid(S, np.array([245, 240, 225], np.float32), teeth))
        eye = _shape(S, lambda p: p.circle(0.5, 0.02, 0.025))
        img2 = over(img2, solid(S, np.array([10, 10, 10], np.float32), eye))
        spine = _shape(S, lambda p: [p.circle(-0.35 + i * 0.12, 0.17, 0.025) for i in range(6)])
        img2 = over(img2, solid(S, np.array([150, 140, 125], np.float32), spine))
    elif facing == "south":
        body = _shape(S, lambda p: [p.ellipse(0, 0.08, 0.3, 0.26), p.circle(0, -0.08, 0.17),
                                    p.line([(-0.22, 0.05), (-0.42, -0.25), (-0.38, -0.6)], 0.07), p.line([(0.22, 0.05), (0.42, -0.25), (0.38, -0.6)], 0.07),
                                    p.line([(-0.12, 0.0), (-0.18, -0.35), (-0.15, -0.6)], 0.06), p.line([(0.12, 0.0), (0.18, -0.35), (0.15, -0.6)], 0.06)])
        img2 = _paint(img, body, skin)
        face = _shape(S, lambda p: [p.ellipse(0, -0.16, 0.11, 0.06), p.circle(-0.07, -0.02, 0.025), p.circle(0.07, -0.02, 0.025)])
        img2 = over(img2, solid(S, np.array([25, 8, 10], np.float32), face))
        teeth = _shape(S, lambda p: [p.line([(-0.08 + i * 0.04, -0.13), (-0.08 + i * 0.04, -0.18)], 0.012) for i in range(5)])
        img2 = over(img2, solid(S, np.array([245, 240, 225], np.float32), teeth))
    else:
        body = _shape(S, lambda p: [p.ellipse(0, 0.0, 0.3, 0.34), p.circle(0, 0.28, 0.15),
                                    p.line([(-0.22, -0.05), (-0.42, -0.3), (-0.38, -0.62)], 0.07), p.line([(0.22, -0.05), (0.42, -0.3), (0.38, -0.62)], 0.07),
                                    p.line([(-0.12, 0.1), (-0.3, 0.3), (-0.28, 0.45)], 0.06), p.line([(0.12, 0.1), (0.3, 0.3), (0.28, 0.45)], 0.06)])
        img2 = _paint(img, body, skin)
        spine = _shape(S, lambda p: [p.circle(0, 0.2 - i * 0.09, 0.03) for i in range(7)])
        img2 = over(img2, solid(S, np.array([150, 140, 125], np.float32), spine))
    img.paste(img2)


def partygoer(img, facing, S):
    yellow = (240, 214, 64)
    if facing == "east":
        body = _shape(S, lambda p: [p.ellipse(0, 0.0, 0.2, 0.4), p.circle(0.02, 0.48, 0.2),
                                    p.line([(0.05, 0.25), (0.2, -0.05), (0.26, -0.42)], 0.06), p.line([(-0.02, -0.35), (0.04, -0.85)], 0.08),
                                    p.line([(-0.08, -0.35), (-0.12, -0.85)], 0.08)])
        img2 = _paint(img, body, yellow)
        mouth = _shape(S, lambda p: p.pie(0.16, 0.44, 0.1, 200, 340))
        img2 = over(img2, solid(S, np.array([20, 16, 12], np.float32), mouth))
        eye = _shape(S, lambda p: p.ellipse(0.13, 0.55, 0.025, 0.045))
        img2 = over(img2, solid(S, np.array([20, 16, 12], np.float32), eye))
    elif facing == "south":
        body = _shape(S, lambda p: [p.ellipse(0, 0.0, 0.3, 0.4), p.circle(0, 0.48, 0.24),
                                    p.line([(-0.26, 0.2), (-0.42, -0.1), (-0.46, -0.45)], 0.065), p.line([(0.26, 0.2), (0.42, -0.1), (0.46, -0.45)], 0.065),
                                    p.line([(-0.12, -0.35), (-0.14, -0.85)], 0.09), p.line([(0.12, -0.35), (0.14, -0.85)], 0.09)])
        img2 = _paint(img, body, yellow)
        face = _shape(S, lambda p: [p.ellipse(-0.09, 0.56, 0.035, 0.06), p.ellipse(0.09, 0.56, 0.035, 0.06),
                                    p.arc(0, 0.5, 0.15, 200, 340, 0.04)])
        img2 = over(img2, solid(S, np.array([20, 16, 12], np.float32), face))
        drips = _shape(S, lambda p: [p.line([(x, 0.3), (x, 0.3 - h)], 0.025) for x, h in ((-0.12, 0.1), (0.05, 0.16), (0.15, 0.07))])
        img2 = over(img2, solid(S, np.array([200, 30, 30], np.float32), drips))
    else:
        body = _shape(S, lambda p: [p.ellipse(0, 0.0, 0.3, 0.4), p.circle(0, 0.48, 0.24),
                                    p.line([(-0.26, 0.2), (-0.42, -0.1), (-0.46, -0.45)], 0.065), p.line([(0.26, 0.2), (0.42, -0.1), (0.46, -0.45)], 0.065),
                                    p.line([(-0.12, -0.35), (-0.14, -0.85)], 0.09), p.line([(0.12, -0.35), (0.14, -0.85)], 0.09)])
        img2 = _paint(img, body, (225, 198, 55))
    img.paste(img2)


def creatures():
    _creature_sheet(hound, "Hound")
    _creature_sheet(partygoer, "Partygoer")


def make_all():
    particles()
    projectiles()
    circles()
    grounds()
    things()
    creatures()


if __name__ == "__main__":
    make_all()
    print("Mythic textures done.")

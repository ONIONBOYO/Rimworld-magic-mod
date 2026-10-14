"""
Textures for the particle / decal / lighting effect system: spark streaks, smoke, debris, decals, flares, god rays,
telegraphs, per-school casting circles, status overlays and the screen vignette.

Called from make_textures.main(); can also be run on its own:  python3 Tools/art/make_vfx2.py
"""
import math
import random

import numpy as np
from PIL import Image, ImageDraw

import make_textures as mt
import motifs
from make_textures import arr, blur, glyph, noise, save, to_rgba, vfx_radial, white


def coords(size):
    y, x = np.mgrid[0:size, 0:size].astype(np.float32)
    x = (x + 0.5) / size - 0.5
    y = (y + 0.5) / size - 0.5
    r = np.sqrt(x * x + y * y) * 2
    return x, y, r


def edge(r, w=0.06):
    return np.clip((1.0 - r) / w, 0, 1)


def rgba(rgb, alpha, size):
    if np.ndim(rgb) == 1:
        rgb = np.ones((size, size, 3), np.float32) * np.asarray(rgb, np.float32)
    return to_rgba(rgb, np.clip(alpha, 0, 1) * 255)


def big_draw(size, k=2):
    img = Image.new("L", (size * k, size * k), 0)
    return img, ImageDraw.Draw(img)


def down(img, size):
    return img.resize((size, size), Image.LANCZOS)


# ------------------------------------------------------------------------------------------------ particles

def particles():
    # velocity-stretched spark: a vertical line, brightest in the middle
    S = 64
    x, y, r = coords(S)
    a = np.exp(-(x / 0.06) ** 2) * np.clip(1 - np.abs(y) * 2, 0, 1) ** 0.7 + 0.8 * np.exp(-(x * x + y * y) / 0.004)
    save(white(np.clip(a, 0, 1) * edge(r, 0.1), S), "Vfx/Streak")

    # smoke puffs (alpha blended, tinted at runtime)
    S = 128
    x, y, r = coords(S)
    for name, rad, dens, seed in [("SmokePuff", 0.62, 1.0, 101), ("SmokePuff2", 0.52, 1.25, 202)]:
        n1 = noise(S, 4, 4, seed)
        n2 = noise(S, 8, 3, seed + 7)
        base = np.exp(-(r / rad) ** 2)
        a = np.clip((base * (0.5 + 0.8 * n1) - 0.12) * dens, 0, 1) ** 1.1
        shade = np.clip(0.78 + 0.25 * (-x - y) + 0.12 * (n2 - 0.5), 0.55, 1.0)
        rgb = np.dstack([shade * 245, shade * 242, shade * 238])
        save(rgba(rgb, a * edge(r), S), "Vfx/" + name)
    n = noise(S, 3, 3, 303)
    a = np.exp(-(r / 0.55) ** 2) * (0.45 + 0.6 * n) * 0.85
    save(rgba([235, 225, 210], a * edge(r), S), "Vfx/Dust")

    # soft ground shadow
    S = 64
    x, y, r = coords(S)
    save(rgba([0, 0, 0], np.exp(-(r / 0.55) ** 2) * 0.95 * edge(r), S), "Vfx/Shadow")

    # rock debris: irregular faceted chunks
    rng = random.Random(42)
    for name in ("Debris", "Debris2"):
        S = 64
        img, d = big_draw(S, 4)
        c = S * 2
        pts = []
        n = rng.randint(6, 9)
        for i in range(n):
            ang = i * 2 * math.pi / n + rng.uniform(-0.25, 0.25)
            rad = S * 4 * 0.5 * rng.uniform(0.45, 0.82)
            pts.append((c + rad * math.cos(ang), c + rad * math.sin(ang)))
        d.polygon(pts, fill=255)
        m = arr(down(img, S))
        x, y, r = coords(S)
        nn = noise(S, 6, 3, rng.randint(0, 999))
        shade = np.clip(0.65 + 0.45 * (-x - y) * 1.6 + 0.25 * (nn - 0.5), 0.35, 1.1)
        rgb = np.dstack([shade * 210, shade * 205, shade * 200])
        save(rgba(rgb, m, S), "Vfx/" + name)

    # bone chip
    S = 64
    img, d = big_draw(S, 4)
    c = S * 2
    d.polygon([(c - 70, c + 20), (c - 40, c - 30), (c + 60, c - 50), (c + 80, c - 10), (c + 20, c + 40)], fill=255)
    d.ellipse([c - 95, c + 0, c - 45, c + 50], fill=255)
    m = arr(down(img, S))
    x, y, r = coords(S)
    shade = np.clip(0.8 + 0.4 * (-x - y), 0.5, 1.1)
    save(rgba(np.dstack([shade * 235, shade * 225, shade * 200]), m, S), "Vfx/BoneChip")

    # ash flakes
    S = 32
    img, d = big_draw(S, 4)
    c = S * 2
    d.polygon([(c - 40, c - 10), (c - 5, c - 45), (c + 45, c - 20), (c + 30, c + 35), (c - 20, c + 40)], fill=255)
    m = arr(blur(down(img, S), 0.6))
    save(rgba([200, 195, 190], m * 0.9, S), "Vfx/Ash")

    # ice chunk: angular with facets
    S = 64
    img, d = big_draw(S, 4)
    c = S * 2
    pts = [(c, c - 110), (c + 70, c - 30), (c + 50, c + 90), (c - 40, c + 100), (c - 80, c + 10)]
    d.polygon(pts, fill=255)
    m = arr(down(img, S))
    fac = Image.new("L", (S * 4, S * 4), 0)
    fd = ImageDraw.Draw(fac)
    fd.line([pts[0], (c + 5, c + 20), pts[2]], fill=255, width=6)
    fd.line([(c + 5, c + 20), pts[4]], fill=255, width=6)
    fa = arr(down(fac, S))
    x, y, r = coords(S)
    shade = np.clip(0.75 + 0.35 * (-x - y) + 0.35 * fa, 0.5, 1.2)
    save(rgba(np.dstack([shade * 215, shade * 235, shade * 255]), m * 0.92, S), "Vfx/IceChunk")

    # flame tongue (upright, tip at top)
    S = 128
    pen = motifs.Pen(S * 4)
    pen.poly(motifs.flame_pts(0, -0.15, 0.55, 1.55))
    shape = arr(down(pen.img, S))
    x, y, r = coords(S)
    n = noise(S, 5, 3, 404)
    vert = np.clip(0.55 - y * 1.4, 0, 1)
    core = np.exp(-((x / 0.14) ** 2 + ((y - 0.18) / 0.22) ** 2))
    a = np.clip(arr(blur(Image.fromarray((shape * 255).astype(np.uint8)), 3)) * (0.55 + 0.45 * n) * (0.45 + 0.55 * vert) + core * 0.7, 0, 1)
    save(white(a, S), "Vfx/Flame")

    # wisp: a curling comet
    S = 64
    pen = motifs.Pen(S * 4)
    pts = []
    for i in range(40):
        t = i / 39
        ang = math.radians(-40 + t * 300)
        rad = 0.15 + 0.55 * t
        pts.append((rad * math.cos(ang) * 0.9, rad * math.sin(ang) * 0.9))
    for i in range(len(pts) - 1):
        w = 0.16 * (1 - i / len(pts)) + 0.02
        pen.line([pts[i], pts[i + 1]], w)
    m = arr(blur(down(pen.img, S), 1.5))
    x, y, r = coords(S)
    a = np.clip(m * 0.8 + np.exp(-(r / 0.22) ** 2) * 0.9, 0, 1) * edge(r)
    save(white(a, S), "Vfx/Wisp")

    # bubble
    S = 64
    x, y, r = coords(S)
    a = np.exp(-((r - 0.78) / 0.08) ** 2) * 0.85 + 0.12 * np.exp(-(r / 0.7) ** 2) + 0.9 * np.exp(-(((x + 0.16) ** 2 + (y + 0.16) ** 2) / 0.003))
    save(white(np.clip(a, 0, 1) * edge(r), S), "Vfx/Bubble")

    # lens flare / starburst
    S = 256
    x, y, r = coords(S)
    ang = np.arctan2(y, x)
    a = np.exp(-(r / 0.13) ** 2) * 1.2 + 0.25 * np.exp(-(r / 0.45) ** 2)
    for k, (length, width) in enumerate([(1.0, 0.010), (0.55, 0.008)]):
        for i in range(4):
            th = i * math.pi / 2 + k * math.pi / 4
            px = -np.sin(th) * x + np.cos(th) * y
            along = np.cos(th) * x + np.sin(th) * y
            a += np.exp(-(px / width) ** 2) * np.clip(1 - np.abs(along) * 2 / length, 0, 1) ** 2.2
    a += 0.18 * np.exp(-((r - 0.42) / 0.015) ** 2)
    save(white(np.clip(a, 0, 1) * edge(r), S), "Vfx/Flare")

    # small items
    S = 32
    x, y, r = coords(S)
    a = np.zeros_like(r)
    for th in (0, math.pi / 2):
        px = -np.sin(th) * x + np.cos(th) * y
        along = np.cos(th) * x + np.sin(th) * y
        a += np.exp(-(px / 0.04) ** 2) * np.clip(1 - np.abs(along) * 2, 0, 1) ** 2
    a += np.exp(-(r / 0.2) ** 2)
    save(white(np.clip(a, 0, 1) * edge(r, 0.15), S), "Vfx/GoldFleck")
    for name, motif in [("Heart", "heart"), ("Crystal", "crystal")]:
        m = glyph(motif, 64, 0.85)
        save(white(np.clip(arr(m) + arr(blur(m, 3)) * 0.6, 0, 1), 64), "Vfx/" + name)


# ------------------------------------------------------------------------------------------------ decals

def decals():
    S = 256
    x, y, r = coords(S)
    rng = random.Random(7)

    # scorch: charred blotch with radial streaks
    n = noise(S, 5, 4, 11)
    ang = np.arctan2(y, x)
    streaks = np.zeros_like(r)
    for _ in range(14):
        th = rng.uniform(-math.pi, math.pi)
        width = rng.uniform(0.06, 0.16)
        length = rng.uniform(0.6, 0.95)
        dth = np.angle(np.exp(1j * (ang - th)))
        streaks = np.maximum(streaks, np.exp(-(dth / width) ** 2) * np.clip(1 - r / length, 0, 1))
    a = np.clip(np.exp(-(r / 0.62) ** 2) * (0.9 + 0.45 * n) + streaks * 0.6 * (0.6 + 0.6 * n) - 0.05, 0, 0.93) * edge(r, 0.15)
    rgb = np.dstack([22 + 34 * r, 18 + 28 * r, 14 + 20 * r])
    save(rgba(rgb, a, S), "Vfx/Decal_Scorch")

    # crater: rim and darkened bowl
    a = np.clip(0.45 * np.exp(-(r / 0.55) ** 2) + 0.55 * np.exp(-((r - 0.68) / 0.1) ** 2) * (0.6 + 0.6 * n), 0, 0.85) * edge(r, 0.2)
    shade = np.clip(0.5 + 0.8 * (x + y) * (r < 0.62) - 0.6 * (x + y) * (r >= 0.62), 0.2, 1.0)
    rgb = np.dstack([shade * 70, shade * 62, shade * 55])
    save(rgba(rgb, a, S), "Vfx/Decal_Crater")

    # cracks
    img, d = big_draw(S, 2)
    c = S

    def crack(x0, y0, ang0, length, w, depth):
        if depth == 0 or length < 8:
            return
        pts = [(x0, y0)]
        ang_ = ang0
        steps = 6
        for i in range(steps):
            ang_ += rng.uniform(-0.35, 0.35)
            x0 += math.cos(ang_) * length / steps
            y0 += math.sin(ang_) * length / steps
            pts.append((x0, y0))
        d.line(pts, fill=255, width=max(1, int(w)))
        for _ in range(2):
            k = rng.randint(1, steps - 1)
            crack(pts[k][0], pts[k][1], ang_ + rng.uniform(-1.0, 1.0), length * 0.5, w * 0.6, depth - 1)

    for i in range(9):
        crack(c, c, i * 2 * math.pi / 9 + rng.uniform(-0.2, 0.2), S * 0.85 * rng.uniform(0.6, 1.0), 7, 3)
    m = arr(down(img, S))
    a = np.clip(m * 0.9 + 0.35 * np.exp(-(r / 0.18) ** 2), 0, 0.95) * edge(r, 0.2)
    save(rgba([22, 20, 18], a, S), "Vfx/Decal_Crack")

    # frost: crystalline branches
    img, d = big_draw(S, 2)

    def branch(x0, y0, ang0, length, w, depth):
        if depth == 0 or length < 6:
            return
        x1, y1 = x0 + length * math.cos(ang0), y0 + length * math.sin(ang0)
        d.line([x0, y0, x1, y1], fill=255, width=max(1, int(w)))
        for sgn in (-1, 1):
            k = rng.uniform(0.3, 0.7)
            branch(x0 + (x1 - x0) * k, y0 + (y1 - y0) * k, ang0 + sgn * math.radians(rng.uniform(50, 65)), length * 0.42, w * 0.65, depth - 1)

    for i in range(7):
        branch(c, c, i * 2 * math.pi / 7 + rng.uniform(-0.2, 0.2), S * 0.8 * rng.uniform(0.6, 1.0), 6, 4)
    m = arr(down(img, S))
    a = np.clip(m * 0.85 + 0.3 * np.exp(-(r / 0.55) ** 2) * (0.6 + 0.6 * n), 0, 0.9) * edge(r, 0.2)
    save(rgba([235, 248, 255], a, S), "Vfx/Decal_Frost")

    # blood splatter
    nb = noise(S, 6, 4, 23)
    blob = np.clip((np.exp(-(r / 0.35) ** 2) * (0.6 + 0.8 * nb) - 0.35) * 3, 0, 1)
    img, d = big_draw(S, 2)
    for _ in range(26):
        th = rng.uniform(0, 2 * math.pi)
        dist = rng.uniform(0.25, 0.85) * S
        rad = rng.uniform(2, 9) * (1.2 - dist / S)
        cx, cy = c + math.cos(th) * dist, c + math.sin(th) * dist
        d.ellipse([cx - rad, cy - rad, cx + rad, cy + rad], fill=255)
        d.line([c + math.cos(th) * dist * 0.5, c + math.sin(th) * dist * 0.5, cx, cy], fill=255, width=max(1, int(rad * 0.5)))
    drops = arr(down(img, S))
    a = np.clip(blob + drops, 0, 0.92) * edge(r, 0.1)
    rgb = np.dstack([110 + 40 * nb, 6 + 8 * nb, 10 + 8 * nb])
    save(rgba(rgb, a, S), "Vfx/Decal_Blood")

    # rot: organic blotch with veins
    nr = noise(S, 5, 4, 31)
    img, d = big_draw(S, 2)
    for i in range(10):
        th = i * 2 * math.pi / 10 + rng.uniform(-0.3, 0.3)
        pts = [(c, c)]
        ang_ = th
        px, py = c, c
        for k in range(8):
            ang_ += rng.uniform(-0.5, 0.5)
            px += math.cos(ang_) * S * 0.09
            py += math.sin(ang_) * S * 0.09
            pts.append((px, py))
        d.line(pts, fill=255, width=4)
    veins = arr(down(img, S))
    a = np.clip(np.exp(-(r / 0.5) ** 2) * (0.6 + 0.6 * nr) - 0.1, 0, 0.85) * edge(r, 0.2)
    a = np.maximum(a, veins * 0.8 * edge(r, 0.3))
    rgb = np.dstack([30 + 30 * veins, 40 + 70 * veins, 22 + 10 * veins])
    save(rgba(rgb, a, S), "Vfx/Decal_Rot")

    # ash
    na = noise(S, 6, 4, 41)
    a = np.clip(np.exp(-(r / 0.5) ** 2) * (0.5 + 0.7 * na) - 0.15, 0, 0.8) * edge(r, 0.2)
    save(rgba(np.dstack([95 + 30 * na, 92 + 28 * na, 88 + 26 * na]), a, S), "Vfx/Decal_Ash")

    # void stain: spiral arms
    ang = np.arctan2(y, x)
    spiral = 0.5 + 0.5 * np.sin(ang * 3 + r * 9)
    nv = noise(S, 5, 3, 51)
    a = np.clip(np.exp(-(r / 0.55) ** 2) * (0.45 + 0.55 * spiral) * (0.7 + 0.5 * nv), 0, 0.85) * edge(r, 0.2)
    save(rgba(np.dstack([45 + 20 * spiral, 12 + 6 * spiral, 70 + 30 * spiral]), a, S), "Vfx/Decal_Void")

    # scattered leaves (baked colours)
    base = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    leaf = glyph("leaf", 40, 0.9)
    for _ in range(26):
        th = rng.uniform(0, 2 * math.pi)
        dist = abs(rng.gauss(0, 0.28)) * S
        cx, cy = S / 2 + math.cos(th) * dist, S / 2 + math.sin(th) * dist
        rot = leaf.rotate(rng.uniform(0, 360), expand=True)
        col = rng.choice([(90, 150, 50), (120, 170, 60), (150, 120, 50), (80, 120, 40), (170, 150, 60)])
        tint = Image.new("RGBA", rot.size, col + (0,))
        tint.putalpha(rot.point(lambda v: int(v * 0.9)))
        base.alpha_composite(tint, (int(cx - rot.size[0] / 2), int(cy - rot.size[1] / 2)))
    save(base, "Vfx/Decal_Leaves")

    # holy sigil (glow): rune circle plus radiant lines
    rc = mt.rune_circle(S, 1, 61)
    ra = arr(rc.split()[3])
    ang = np.arctan2(y, x)
    rays = np.clip(np.cos(ang * 12) ** 24, 0, 1) * np.clip(1 - r, 0, 1) * (r > 0.3)
    save(white(np.clip(ra * 0.9 + rays * 0.6 + 0.15 * np.exp(-(r / 0.5) ** 2), 0, 1) * edge(r), S), "Vfx/Decal_Sigil")

    # glyph ring (glow): clock ticks and runes
    img, d = big_draw(S, 2)
    R = S * 0.9
    d.ellipse([c - R, c - R, c + R, c + R], outline=255, width=6)
    d.ellipse([c - R * 0.82, c - R * 0.82, c + R * 0.82, c + R * 0.82], outline=255, width=3)
    for i in range(60):
        th = math.radians(i * 6)
        l0 = R * (0.84 if i % 5 else 0.74)
        d.line([c + math.cos(th) * l0, c + math.sin(th) * l0, c + math.cos(th) * R * 0.97, c + math.sin(th) * R * 0.97], fill=255, width=4 if i % 5 == 0 else 2)
    d.line([c, c, c + R * 0.5, c - R * 0.25], fill=255, width=6)
    d.line([c, c, c - R * 0.2, c - R * 0.62], fill=255, width=4)
    m = down(img, S)
    save(white(np.clip(arr(m) + arr(blur(m, 3)) * 0.7, 0, 1), S), "Vfx/Decal_Glyph")


# ------------------------------------------------------------------------------------------------ beams, rays, trails

def trails_and_rays():
    S = 128
    x, y, r = coords(S)
    u = x + 0.5
    a = (u ** 1.6) * np.exp(-(y / 0.22) ** 2) + 0.6 * (u ** 2.4) * np.exp(-(y / 0.07) ** 2)
    save(white(np.clip(a, 0, 1), S), "Vfx/Trail")
    n = noise(S, 4, 3, 71)
    a = (u ** 1.3) * np.exp(-(y / 0.3) ** 2) * (0.45 + 0.7 * n)
    save(rgba([215, 210, 205], np.clip(a, 0, 0.9), S), "Vfx/TrailSmoke")

    # god ray: brightest at the bottom (ground), fading upward; soft across
    v = 0.5 + y  # image bottom -> v = 1
    n = noise(S, 3, 2, 72)
    a = np.exp(-(x / 0.2) ** 2) * np.clip(v, 0, 1) ** 1.6 * (0.75 + 0.35 * n)
    save(white(np.clip(a, 0, 1), S), "Vfx/GodRay")

    # telegraph: soft fill that brightens toward a crisp rim
    S = 256
    x, y, r = coords(S)
    a = (0.18 + 0.55 * np.clip(r, 0, 1) ** 3) * (r < 0.95) + 0.9 * np.exp(-((r - 0.93) / 0.025) ** 2)
    save(white(np.clip(a, 0, 1) * edge(r, 0.04), S), "Vfx/Telegraph")


# ------------------------------------------------------------------------------------------------ casting circles

CIRCLES = {
    "Arcane": ("star", "hexagram"), "Auto": ("star", "hexagram"), "Fire": ("flame", "fire"), "Frost": ("snowflake", "hex"),
    "Storm": ("bolt", "zigzag"), "Necro": ("skull", "pentagram"), "Holy": ("cross", "rays"), "Time": ("hourglass", "clock"),
    "Void": ("portal", "spiral"), "Nature": ("leaf", "petals"), "Rune": ("rune", "squares"), "Shadow": ("moon", "eye"),
    "Blood": ("drop", "thorns"), "Song": ("note", "waves"), "Plague": ("mushroom", "bubbles"),
    "Wind": ("gust", "gusts"), "Doom": ("eye", "heptagram"), "Desire": ("heart", "hearts"), "Liminal": ("smile", "grid"),
}


def cast_circle(size, motif, inner, seed):
    rng = random.Random(seed)
    k = 2
    S = size * k
    img = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(img)
    c = S / 2
    R = S * 0.47

    def circ(rr, w):
        d.ellipse([c - rr, c - rr, c + rr, c + rr], outline=255, width=max(1, int(w)))

    circ(R, S * 0.012)
    circ(R * 0.95, S * 0.004)
    circ(R * 0.74, S * 0.008)
    circ(R * 0.70, S * 0.003)
    # glyph band between the rings
    n = 10
    gs = int(S * 0.085)
    g = glyph(motif, gs, 0.9)
    for i in range(n):
        th = i * 360 / n
        gr = g.rotate(-th, expand=True)
        cx = c + R * 0.845 * math.cos(math.radians(th - 90))
        cy = c + R * 0.845 * math.sin(math.radians(th - 90))
        img.paste(255, (int(cx - gr.size[0] / 2), int(cy - gr.size[1] / 2)), gr)
    # tick marks on the outer ring
    for i in range(72):
        th = math.radians(i * 5)
        l0 = R * (0.955 if i % 2 else 0.93)
        d.line([c + math.cos(th) * l0, c + math.sin(th) * l0, c + math.cos(th) * R * 0.995, c + math.sin(th) * R * 0.995], fill=255, width=max(1, int(S * 0.003)))
    Ri = R * 0.66
    w = max(1, int(S * 0.006))

    def poly(npts, rot=90, step=1, rr=Ri):
        pts = [(c + rr * math.cos(math.radians(rot + i * 360 * step / npts)), c - rr * math.sin(math.radians(rot + i * 360 * step / npts))) for i in range(npts + 1)]
        d.line(pts, fill=255, width=w)

    if inner == "hexagram":
        poly(3)
        poly(3, rot=-90)
        circ(Ri * 0.5, w)
    elif inner == "fire":
        poly(3)
        poly(3, rot=-90)
        for i in range(12):
            th = math.radians(i * 30)
            d.line([c + math.cos(th) * Ri * 0.2, c + math.sin(th) * Ri * 0.2, c + math.cos(th) * Ri * 0.5, c + math.sin(th) * Ri * 0.5], fill=255, width=w)
    elif inner == "hex":
        poly(6)
        poly(6, rot=0, rr=Ri * 0.62)
        for i in range(6):
            th = math.radians(90 + i * 60)
            d.line([c, c, c + math.cos(th) * Ri, c - math.sin(th) * Ri], fill=255, width=w)
    elif inner == "zigzag":
        for i in range(6):
            th = math.radians(i * 60)
            pts = [(c, c)]
            for j in range(1, 5):
                rr = Ri * j / 4
                off = math.radians(12 if j % 2 else -12)
                pts.append((c + math.cos(th + off) * rr, c + math.sin(th + off) * rr))
            d.line(pts, fill=255, width=w)
        circ(Ri * 0.3, w)
    elif inner == "pentagram":
        poly(5, step=2)
        circ(Ri, w)
    elif inner == "rays":
        for i in range(16):
            th = math.radians(i * 22.5)
            d.line([c + math.cos(th) * Ri * 0.25, c + math.sin(th) * Ri * 0.25, c + math.cos(th) * Ri * (1.0 if i % 2 == 0 else 0.7),
                    c + math.sin(th) * Ri * (1.0 if i % 2 == 0 else 0.7)], fill=255, width=w)
        circ(Ri * 0.25, w)
    elif inner == "clock":
        for i in range(12):
            th = math.radians(i * 30)
            d.line([c + math.cos(th) * Ri * 0.8, c + math.sin(th) * Ri * 0.8, c + math.cos(th) * Ri, c + math.sin(th) * Ri], fill=255, width=w * 2)
        d.line([c, c, c + Ri * 0.55, c - Ri * 0.3], fill=255, width=w * 2)
        d.line([c, c, c - Ri * 0.25, c - Ri * 0.75], fill=255, width=w)
        circ(Ri * 0.08, w)
    elif inner == "spiral":
        pts = []
        for i in range(160):
            t = i / 159
            th = math.radians(t * 900)
            rr = Ri * t
            pts.append((c + math.cos(th) * rr, c + math.sin(th) * rr))
        d.line(pts, fill=255, width=w)
        circ(Ri, w)
    elif inner == "petals":
        for i in range(8):
            th = math.radians(i * 45)
            px, py = c + math.cos(th) * Ri * 0.5, c + math.sin(th) * Ri * 0.5
            rr = Ri * 0.5
            d.ellipse([px - rr, py - rr, px + rr, py + rr], outline=255, width=w)
    elif inner == "squares":
        poly(4, rot=45)
        poly(4, rot=0)
        poly(4, rot=45, rr=Ri * 0.5)
    elif inner == "eye":
        d.arc([c - Ri, c - Ri * 0.55, c + Ri, c + Ri * 1.45], 200, 340, fill=255, width=w)
        d.arc([c - Ri, c - Ri * 1.45, c + Ri, c + Ri * 0.55], 20, 160, fill=255, width=w)
        circ(Ri * 0.28, w * 2)
        circ(Ri * 0.1, w * 3)
    elif inner == "thorns":
        circ(Ri * 0.7, w)
        for i in range(16):
            th = math.radians(i * 22.5)
            p0 = (c + math.cos(th - 0.08) * Ri * 0.7, c + math.sin(th - 0.08) * Ri * 0.7)
            p1 = (c + math.cos(th) * Ri, c + math.sin(th) * Ri)
            p2 = (c + math.cos(th + 0.08) * Ri * 0.7, c + math.sin(th + 0.08) * Ri * 0.7)
            d.line([p0, p1, p2], fill=255, width=w)
    elif inner == "waves":
        for k2, rr in enumerate([Ri * 0.35, Ri * 0.6, Ri * 0.85]):
            pts = []
            for i in range(181):
                th = math.radians(i * 2)
                r2 = rr * (1 + 0.06 * math.sin(th * (6 + k2 * 2)))
                pts.append((c + math.cos(th) * r2, c + math.sin(th) * r2))
            d.line(pts, fill=255, width=w)
    elif inner == "bubbles":
        for i in range(14):
            th = rng.uniform(0, 2 * math.pi)
            dist = rng.uniform(0.1, 0.85) * Ri
            rr = rng.uniform(0.06, 0.16) * Ri
            px, py = c + math.cos(th) * dist, c + math.sin(th) * dist
            d.ellipse([px - rr, py - rr, px + rr, py + rr], outline=255, width=w)
    elif inner == "heptagram":
        pts = [(c + Ri * math.cos(math.radians(90 + i * 360 * 3 / 7)), c - Ri * math.sin(math.radians(90 + i * 360 * 3 / 7))) for i in range(8)]
        d.line(pts, fill=255, width=w)
        for i in range(7):
            th = math.radians(90 + i * 360 / 7)
            px, py = c + Ri * math.cos(th), c - Ri * math.sin(th)
            rr = Ri * 0.09
            d.ellipse([px - rr, py - rr, px + rr, py + rr], outline=255, width=w)
        circ(Ri * 0.42, w)
    elif inner == "gusts":
        for k in range(5):
            pts = []
            for i in range(60):
                t = i / 59
                th = math.radians(k * 72 + t * 220)
                rr = Ri * (0.25 + 0.75 * t)
                pts.append((c + math.cos(th) * rr, c + math.sin(th) * rr))
            d.line(pts, fill=255, width=w)
        circ(Ri * 0.22, w)
    elif inner == "hearts":
        hg = glyph("heart", int(S * 0.07), 0.9)
        for i in range(12):
            th = i * 30
            gr = hg.rotate(-th, expand=True)
            px = c + Ri * 0.78 * math.cos(math.radians(th - 90))
            py = c + Ri * 0.78 * math.sin(math.radians(th - 90))
            img.paste(255, (int(px - gr.size[0] / 2), int(py - gr.size[1] / 2)), gr)
        for i in range(4):
            th = math.radians(45 + i * 90)
            d.arc([c - Ri * 0.6, c - Ri * 0.6, c + Ri * 0.6, c + Ri * 0.6], i * 90 + 10, i * 90 + 80, fill=255, width=w)
        circ(Ri * 0.35, w)
    elif inner == "grid":
        step = Ri * 0.34
        for k in range(-2, 3):
            off = k * step
            half = (max(0.0, Ri ** 2 - off ** 2)) ** 0.5
            d.line([c + off, c - half, c + off, c + half], fill=255, width=w)
            d.line([c - half, c + off, c + half, c + off], fill=255, width=w)
        poly(4, rot=45, rr=Ri * 0.98)
    cg = glyph(motif, int(S * 0.16), 0.9)
    img.paste(255, (int(c - cg.size[0] / 2), int(c - cg.size[1] / 2)), cg)
    m = img.resize((size, size), Image.LANCZOS)
    a = arr(m)
    gl = arr(blur(m, size * 0.012))
    return white(np.clip(a + gl * 0.8, 0, 1), size)


def circles():
    for i, (style, (motif, inner)) in enumerate(CIRCLES.items()):
        save(cast_circle(256, motif, inner, 100 + i), "Vfx/Circle_" + style)


# ------------------------------------------------------------------------------------------------ status overlays

def overlays():
    S = 128
    x, y, r = coords(S)
    rng = random.Random(91)
    # ice crust: translucent shell with crystalline edges
    img, d = big_draw(S, 2)
    c = S
    for i in range(14):
        th = i * 2 * math.pi / 14 + rng.uniform(-0.1, 0.1)
        rr = S * rng.uniform(0.55, 0.8)
        tip = (c + math.cos(th) * rr, c + math.sin(th) * rr)
        b1 = (c + math.cos(th - 0.18) * rr * 0.55, c + math.sin(th - 0.18) * rr * 0.55)
        b2 = (c + math.cos(th + 0.18) * rr * 0.55, c + math.sin(th + 0.18) * rr * 0.55)
        d.polygon([b1, tip, b2], fill=200)
    crystals = arr(down(img, S))
    n = noise(S, 4, 3, 93)
    a = np.clip(0.38 * np.exp(-(r / 0.62) ** 2) * (0.7 + 0.5 * n) + crystals * 0.55, 0, 0.75) * edge(r)
    shade = np.clip(0.8 + 0.3 * (-x - y) + 0.2 * crystals, 0.6, 1.1)
    save(rgba(np.dstack([shade * 205, shade * 232, shade * 255]), a, S), "Vfx/IceCrust")
    # stone crust
    n = noise(S, 6, 4, 94)
    img, d = big_draw(S, 2)
    for i in range(7):
        th = rng.uniform(0, 2 * math.pi)
        pts = [(c + math.cos(th) * S * 0.1, c + math.sin(th) * S * 0.1)]
        for k in range(5):
            th += rng.uniform(-0.6, 0.6)
            pts.append((pts[-1][0] + math.cos(th) * S * 0.12, pts[-1][1] + math.sin(th) * S * 0.12))
        d.line(pts, fill=255, width=3)
    cracks = arr(down(img, S))
    a = np.clip(0.62 * np.exp(-(r / 0.6) ** 3), 0, 0.68) * edge(r)
    shade = (0.55 + 0.45 * n) * (1 - 0.5 * cracks)
    save(rgba(np.dstack([shade * 175, shade * 170, shade * 160]), a, S), "Vfx/StoneCrust")
    # gold crust
    n = noise(S, 5, 3, 95)
    a = np.clip(0.55 * np.exp(-(r / 0.6) ** 3), 0, 0.62) * edge(r)
    shine = np.clip(0.6 + 0.6 * (-x - y) + 0.4 * (n - 0.5), 0.3, 1.3)
    save(rgba(np.dstack([np.clip(shine * 255, 0, 255), shine * 195, shine * 70]), a, S), "Vfx/GoldCrust")
    # doom sigil: an eye inside a broken ring with spikes
    img, d = big_draw(S, 4)
    c = S * 2
    R = S * 1.7
    d.ellipse([c - R, c - R, c + R, c + R], outline=255, width=10)
    for i in range(8):
        th = math.radians(i * 45 + 22.5)
        d.line([c + math.cos(th) * R, c + math.sin(th) * R, c + math.cos(th) * R * 1.15, c + math.sin(th) * R * 1.15], fill=255, width=12)
    eye = glyph("eye", int(S * 2.4), 0.9)
    img.paste(255, (int(c - eye.size[0] / 2), int(c - eye.size[1] / 2)), eye)
    m = down(img, S)
    save(white(np.clip(arr(m) + arr(blur(m, 3)) * 0.6, 0, 1), S), "Vfx/DoomSigil")
    # judgment mark: reticle with a star
    img, d = big_draw(S, 4)
    d.ellipse([c - R, c - R, c + R, c + R], outline=255, width=10)
    d.ellipse([c - R * 0.6, c - R * 0.6, c + R * 0.6, c + R * 0.6], outline=255, width=6)
    for th in (0, 90, 180, 270):
        t = math.radians(th)
        d.line([c + math.cos(t) * R * 0.7, c + math.sin(t) * R * 0.7, c + math.cos(t) * R * 1.2, c + math.sin(t) * R * 1.2], fill=255, width=10)
    st = glyph("star", int(S * 1.4), 0.9)
    img.paste(255, (int(c - st.size[0] / 2), int(c - st.size[1] / 2)), st)
    m = down(img, S)
    save(white(np.clip(arr(m) + arr(blur(m, 3)) * 0.6, 0, 1), S), "Vfx/MarkSigil")


def ui():
    S = 512
    y, x = np.mgrid[0:S, 0:S].astype(np.float32)
    xx = (x / S - 0.5) * 2
    yy = (y / S - 0.5) * 2
    r = np.sqrt(xx * xx * 0.85 + yy * yy * 0.85)
    a = np.clip((r - 0.55) / 0.75, 0, 1) ** 1.6
    save(white(a, S), "UI/Vignette")


# ------------------------------------------------------------------------------------------------ familiars and buildings

def glowify(img, size, color, r=6, a=0.8):
    """Adds a soft coloured halo behind an RGBA sprite."""
    alpha = img.split()[3]
    halo = mt.solid(size, color, blur(alpha, r).point(lambda v: int(v * a)))
    base = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    return mt.over(mt.over(base, halo), img)


def familiars():
    S = 128
    hexc, mix, WHITE = mt.hexc, mt.mix, mt.WHITE
    x, y, r = coords(S)
    eyes = lambda ex, ey, er: mt.pen_mask(S, lambda p: [p.circle(-ex, ey, er), p.circle(ex, ey, er)])

    # ember sprite: a living flame with bright eyes
    body = mt.pen_mask(S, lambda p: p.poly(motifs.flame_pts(0, -0.1, 0.62, 1.55)))
    n = noise(S, 5, 3, 501)
    core = np.exp(-((x / 0.18) ** 2 + ((y - 0.12) / 0.25) ** 2))
    col = np.dstack([np.full((S, S), 255.0), 120 + 120 * core + 30 * n, 40 + 150 * core])
    img = to_rgba(col, arr(body) * 255)
    img = mt.over(img, mt.solid(S, WHITE, eyes(0.16, -0.12, 0.07)))
    save(glowify(img, S, hexc("#ff8a2a"), 8, 0.9), "Familiars/EmberSprite")

    # frost wisp: a snowflake heart in a cold glow
    m = glyph("snowflake", S, 0.75)
    img = mt.styled_glyph(m, S, mix(hexc("#9be3ff"), WHITE, 0.7), hexc("#7fc8ff"), hexc("#9be3ff"), glow_r=7, outline_px=0)
    img = mt.over(img, white(np.exp(-(r / 0.22) ** 2) * 0.9, S))
    save(img, "Familiars/FrostWisp")

    # shadow raven: black silhouette with a violet rim and a glowing eye
    def raven(p):
        p.ellipse(0.0, -0.05, 0.42, 0.26)
        p.circle(0.38, 0.2, 0.17)
        p.poly([(0.52, 0.24), (0.8, 0.16), (0.52, 0.12)])
        p.poly([(-0.35, -0.05), (-0.9, 0.05), (-0.85, -0.25), (-0.3, -0.2)])
        p.poly([(-0.1, 0.1), (-0.55, 0.75), (-0.2, 0.55), (0.15, 0.15)])
        p.poly([(0.05, 0.05), (0.2, 0.8), (0.35, 0.5), (0.25, 0.05)])
        p.poly([(0.0, -0.3), (-0.05, -0.55), (0.05, -0.55), (0.12, -0.3)])
    m = mt.pen_mask(S, raven)
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    img = mt.over(img, mt.solid(S, hexc("#8a6cff"), blur(mt.dilate(m, 2), 2)))
    rgb = mt.bevel(m, S, np.array([28, 22, 40], np.float32), light=2.2, dark=0.6)
    img = mt.over(img, to_rgba(rgb, arr(m) * 255))
    eye = mt.pen_mask(S, lambda p: p.circle(0.42, 0.24, 0.045))
    img = mt.over(img, mt.solid(S, hexc("#d8b0ff"), blur(eye, 2)))
    img = mt.over(img, mt.solid(S, WHITE, eye))
    save(img, "Familiars/Raven")

    # lumen: a little winged light
    wings = mt.pen_mask(S, lambda p: [p.poly([(0.08, 0.05), (0.75, 0.55), (0.85, 0.15), (0.6, -0.15), (0.1, -0.08)]),
                                      p.poly([(-0.08, 0.05), (-0.75, 0.55), (-0.85, 0.15), (-0.6, -0.15), (-0.1, -0.08)])])
    img = to_rgba(np.ones((S, S, 3), np.float32) * np.array([255, 245, 200], np.float32), arr(blur(wings, 1.5)) * 170)
    img = mt.over(img, white(np.clip(np.exp(-(r / 0.24) ** 2) * 1.3, 0, 1), S))
    save(glowify(img, S, hexc("#ffe28a"), 9, 0.8), "Familiars/Lumen")

    # clockwork owl: brass body, gear chest, big round eyes, ear tufts
    def owl(p):
        p.ellipse(0, -0.1, 0.5, 0.6)
        p.circle(0, 0.38, 0.38)
        p.poly([(-0.32, 0.6), (-0.42, 0.9), (-0.12, 0.7)])
        p.poly([(0.32, 0.6), (0.42, 0.9), (0.12, 0.7)])
    m = mt.pen_mask(S, owl)
    img = mt.sprite(m, np.array([200, 150, 70], np.float32), S, outline=2)
    gear = glyph("gear", S, 0.42, dy=int(S * 0.12))
    img = mt.over(img, mt.solid(S, np.array([150, 105, 45], np.float32), gear))
    eyes_m = eyes(0.17, 0.38, 0.13)
    img = mt.over(img, mt.solid(S, np.array([245, 235, 210], np.float32), eyes_m))
    pupils = eyes(0.17, 0.38, 0.06)
    img = mt.over(img, mt.solid(S, hexc("#ffaa3c"), pupils))
    save(img, "Familiars/ClockworkOwl")

    # void eye
    m = glyph("eye", S, 0.85)
    img = mt.styled_glyph(m, S, mix(hexc("#c060ff"), WHITE, 0.5), hexc("#7a2aff"), hexc("#c060ff"), glow_r=8, outline_px=0)
    ring = white(np.exp(-((r - 0.85) / 0.05) ** 2) * 0.8, S)
    img = mt.over(ring, img)
    save(img, "Familiars/VoidEye")

    # pocket dragon
    def dragon(p):
        p.ellipse(-0.05, -0.1, 0.38, 0.24)
        p.circle(0.38, 0.12, 0.17)
        p.poly([(0.48, 0.18), (0.75, 0.12), (0.5, 0.02)])
        p.poly([(0.32, 0.25), (0.28, 0.45), (0.4, 0.27)])
        p.poly([(-0.35, -0.12), (-0.75, 0.0), (-0.95, -0.25), (-0.7, -0.15), (-0.3, -0.25)])
        p.poly([(-0.15, 0.05), (-0.45, 0.85), (-0.25, 0.6), (-0.05, 0.75), (0.0, 0.5), (0.15, 0.7), (0.15, 0.1)])
    m = mt.pen_mask(S, dragon)
    img = mt.sprite(m, np.array([200, 60, 45], np.float32), S, outline=2)
    belly = mt.pen_mask(S, lambda p: p.ellipse(0.0, -0.18, 0.26, 0.1))
    img = mt.over(img, mt.solid(S, np.array([240, 190, 90], np.float32), belly))
    eye = mt.pen_mask(S, lambda p: p.circle(0.42, 0.15, 0.04))
    img = mt.over(img, mt.solid(S, hexc("#ffe080"), eye))
    save(img, "Familiars/PocketDragon")


def buildings():
    hexc, mix, WHITE = mt.hexc, mt.mix, mt.WHITE
    S = 256
    # the ward pylon and mana font are modelled in 3D: see buildings3d.py

    # familiar vessel: round crystal flask with a bright spirit inside
    S = 128
    x, y, r = coords(S)
    flask = mt.pen_mask(S, lambda p: [p.circle(0, -0.2, 0.55), p.rect(-0.14, 0.2, 0.14, 0.62)])
    img = to_rgba(np.ones((S, S, 3), np.float32) * np.array([200, 225, 255], np.float32), arr(flask) * 120)
    spirit = np.exp(-(((x) ** 2 + (y - 0.1) ** 2) / 0.012))
    img = mt.over(img, white(np.clip(spirit * 1.4, 0, 1), S))
    img = mt.over(img, to_rgba(np.ones((S, S, 3), np.float32) * np.array([255, 220, 150], np.float32), np.clip(np.exp(-(((x) ** 2 + (y - 0.1) ** 2) / 0.05)) * 180, 0, 255)))
    rim = mt.pen_mask(S, lambda p: [p.ring(0, -0.2, 0.55, 0.03)])
    img = mt.over(img, mt.solid(S, np.array([235, 245, 255], np.float32), rim))
    cork = mt.pen_mask(S, lambda p: p.rect(-0.17, 0.6, 0.17, 0.8))
    img = mt.over(img, mt.sprite(cork, np.array([215, 170, 70], np.float32), S, outline=1))
    save(img, "Items/FamiliarVessel")

    save(mt.attr_icon("burst", "#ff5a6a"), "UI/Overchannel")


def make_all():
    particles()
    decals()
    trails_and_rays()
    circles()
    overlays()
    ui()
    familiars()
    buildings()


if __name__ == "__main__":
    make_all()
    print("VFX textures done.")

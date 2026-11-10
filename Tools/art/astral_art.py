"""
Painted textures for the Starcaller and the Wyrmcaller: starlight, moons, constellations, flame, scales, coins, the two casting
circles and the zodiac signs. The solid objects (the wyrm, its eggs, hoard and nest, the orrery) are modelled in dragon3d.py and
props3d.py instead.

    python3 Tools/art/astral_art.py
"""
import math
import random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from make_textures import arr, blur, noise, save, to_rgba, white

F = np.float32


def grid(S, W=None):
    """x right, y up, both -1..1."""
    W = W or S
    y, x = np.mgrid[0:S, 0:W].astype(F)
    return (x + 0.5) / W * 2 - 1, 1 - (y + 0.5) / S * 2


def sstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def grey(value, alpha):
    """A shaded sprite for alpha-blended particles: `value` 0..1 brightness, tinted in game."""
    v = np.clip(value, 0, 1)[..., None] * np.ones(3, F) * 255
    return to_rgba(v, np.clip(alpha, 0, 1) * 255)


def canvas(S, k=4):
    img = Image.new("L", (S * k, S * k), 0)
    return img, ImageDraw.Draw(img), S * k


def shrink(img, S):
    return arr(img.resize((S, S), Image.LANCZOS))


def spike(x, y, ang, length, width):
    """A tapering ray through the origin."""
    c, s = math.cos(ang), math.sin(ang)
    u = x * c + y * s
    v = -x * s + y * c
    return np.exp(-np.abs(u) / length) / (1.0 + (v / width) ** 2 * (1.0 + 6.0 * np.abs(u)))


def star_field(x, y, long=0.5, short=0.3, diag=0.12, core=0.012, width=0.022):
    a = spike(x, y, math.pi / 2, long, width) + spike(x, y, 0, short, width)
    a += 0.45 * (spike(x, y, math.pi / 4, diag, width * 0.8) + spike(x, y, -math.pi / 4, diag, width * 0.8))
    r2 = x * x + y * y
    a += 1.4 * np.exp(-r2 / core) + 0.22 * np.exp(-r2 / 0.12)
    return a


def fade(x, y, w=0.14):
    r = np.sqrt(x * x + y * y)
    return np.clip((1 - r) / w, 0, 1)


# ------------------------------------------------------------------------------------------------ starlight

def stars():
    S = 128
    x, y = grid(S)
    save(white(np.clip(star_field(x, y) * fade(x, y), 0, 1), S), "Vfx/StarSpike")

    S = 64
    x, y = grid(S)
    rng = random.Random(12)
    a = np.zeros((S, S), F)
    for i in range(9):
        cx, cy = rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6)
        sc = rng.uniform(0.18, 0.5) if i else 0.62
        if i == 0:
            cx, cy = 0.05, -0.02
        a += star_field((x - cx) / sc, (y - cy) / sc, 0.4, 0.4, 0.1, 0.02, 0.05) * (0.5 + 0.5 * sc)
    save(white(np.clip(a * fade(x, y, 0.2), 0, 1), S), "Vfx/Stardust")

    # nebula: warped cloud with a few bright stars in it
    S = 256
    x, y = grid(S)
    n1 = noise(S, 4, 5, 31)
    n2 = noise(S, 3, 4, 32)
    wx = x + (n1 - 0.5) * 0.7
    wy = y + (n2 - 0.5) * 0.7
    ang = np.arctan2(wy, wx)
    r = np.sqrt(wx * wx + wy * wy)
    arms = 0.5 + 0.5 * np.sin(ang * 2.0 + r * 5.5)
    cloud = np.clip(noise(S, 5, 5, 33) * 1.5 - 0.3, 0, 1)
    a = (0.25 + 0.75 * arms) * cloud * np.exp(-(r / 0.72) ** 2.2)
    rng = random.Random(5)
    for _ in range(26):
        rr, th = rng.uniform(0, 0.75), rng.uniform(0, 6.283)
        cx, cy = rr * math.cos(th), rr * math.sin(th)
        sc = rng.uniform(0.04, 0.13)
        a += star_field((x - cx) / sc, (y - cy) / sc, 0.4, 0.4, 0.1, 0.03, 0.06) * 0.8
    save(white(np.clip(a * fade(x, y, 0.2), 0, 1), S), "Vfx/Nebula")

    # a line of light between two stars (drawn stretched: the long way is the length)
    S, W = 256, 64
    x, y = grid(S, W)
    a = np.exp(-(x / 0.13) ** 2) + 0.38 * np.exp(-(x / 0.5) ** 2)
    a *= 0.82 + 0.18 * np.sin(y * 28.0) * np.sin(y * 9.0 + 1.0)
    rng = random.Random(8)
    for i in range(7):
        cy = -0.85 + i * 0.283 + rng.uniform(-0.05, 0.05)
        a += star_field(x / 0.55, (y - cy) / (0.55 * W / S), 0.35, 0.5, 0.1, 0.03, 0.06) * 0.55
    a *= np.clip((1 - np.abs(y)) / 0.04, 0, 1) * np.clip((1 - np.abs(x)) / 0.2, 0, 1)
    save(to_rgba(np.ones((S, W, 3), F) * 255, np.clip(a, 0, 1) * 255), "Vfx/StarLine")

    # what a star stands on: a faint ring and a compass of rays, tinted and glowing in game
    S = 128
    x, y = grid(S)
    r = np.sqrt(x * x + y * y)
    ang = np.arctan2(y, x)
    a = 0.55 * np.exp(-((r - 0.62) / 0.025) ** 2) + 0.3 * np.exp(-((r - 0.74) / 0.012) ** 2)
    a += 0.35 * np.exp(-(r / 0.42) ** 2)
    ticks = (np.cos(ang * 8) > 0.985) * sstep(0.62, 0.66, r) * sstep(0.9, 0.8, r)
    ticks4 = (np.cos(ang * 4) > 0.996) * sstep(0.3, 0.4, r) * sstep(0.98, 0.7, r)
    a += 0.6 * ticks + 0.7 * ticks4
    for i in range(12):
        th = i * math.pi / 6 + math.pi / 12
        cx, cy = 0.74 * math.cos(th), 0.74 * math.sin(th)
        a += np.exp(-(((x - cx) ** 2 + (y - cy) ** 2) / 0.0012))
    save(white(np.clip(a * fade(x, y, 0.08), 0, 1), S), "Things/StarBase")


def moons():
    # crescent blade, flying upward
    S = 128
    x, y = grid(S)
    outer = np.sqrt(x * x + (y + 0.25) ** 2)
    inner = np.sqrt(x * x + (y + 0.52) ** 2)
    blade = sstep(0.82, 0.79, outer) * sstep(0.72, 0.76, inner)
    edge_glow = np.exp(-((outer - 0.8) / 0.05) ** 2) * sstep(0.70, 0.78, inner) * (y > -0.35)
    a = blade * (0.75 + 0.25 * sstep(0.74, 0.8, outer)) + 0.5 * edge_glow
    soft = arr(blur(Image.fromarray((np.clip(blade, 0, 1) * 255).astype(np.uint8)), 5))
    a = np.clip(a + soft * 0.6, 0, 1)
    rng = random.Random(3)
    for _ in range(6):
        cx, cy, sc = rng.uniform(-0.5, 0.5), rng.uniform(-0.75, -0.1), rng.uniform(0.06, 0.12)
        a += star_field((x - cx) / sc, (y - cy) / sc, 0.4, 0.4, 0.1, 0.03, 0.06) * 0.5
    save(white(np.clip(a * fade(x, y, 0.1), 0, 1), S), "Vfx/Crescent")

    # the moon itself: limb shading, seas, craters
    S = 256
    x, y = grid(S)
    r = np.sqrt(x * x + y * y)
    R = 0.8
    disc = sstep(R, R - 0.012, r)
    z = np.sqrt(np.clip(1 - (r / R) ** 2, 0, 1))
    light = np.clip(0.35 + 0.65 * (z * 0.75 + (-x * 0.35 + y * 0.45) / R), 0, 1)
    seas = sstep(0.52, 0.66, noise(S, 3, 4, 71)) * 0.34
    v = light * (1.0 - seas) * (0.9 + 0.2 * noise(S, 24, 2, 72))
    rng = random.Random(21)
    for _ in range(22):
        rr, th = R * 0.88 * math.sqrt(rng.random()), rng.uniform(0, 6.283)
        cx, cy, cr = rr * math.cos(th), rr * math.sin(th), rng.uniform(0.025, 0.085)
        d = np.sqrt((x - cx) ** 2 + (y - cy) ** 2) / cr
        bowl = sstep(1.0, 0.7, d)
        rim_ = np.exp(-((d - 1.0) / 0.16) ** 2)
        side = ((x - cx) * 0.6 - (y - cy) * 0.8) / cr
        v = v * (1 - 0.25 * bowl) + 0.2 * rim_ * np.clip(-side, 0, 1) - 0.14 * rim_ * np.clip(side, 0, 1)
    halo = 0.5 * np.exp(-((r - R) / 0.09) ** 2) * (r > R - 0.02) + 0.2 * np.exp(-((r - R) / 0.22) ** 2) * (r > R)
    alpha = np.clip(disc + halo * (1 - disc), 0, 1) * fade(x, y, 0.06)
    val = np.clip(v, 0, 1) * disc + (1 - disc)
    save(grey(val, alpha), "Vfx/Moon")


# ------------------------------------------------------------------------------------------------ the zodiac

SIGNS = {
    # points (x, y) and the lines between them, sketched after the real constellations
    "Archer": ([(-0.55, -0.5), (-0.2, -0.15), (0.15, 0.2), (0.6, 0.62), (0.28, 0.62), (0.6, 0.3), (-0.5, 0.25), (-0.2, 0.55), (0.3, -0.25), (0.05, -0.55)],
               [(0, 1), (1, 2), (2, 3), (3, 4), (3, 5), (6, 7), (7, 2), (2, 8), (8, 9), (6, 1), (1, 9)]),
    "Bull": ([(0.0, -0.55), (-0.2, -0.15), (0.2, -0.15), (-0.32, 0.1), (0.32, 0.1), (-0.62, 0.62), (0.62, 0.62), (0.0, -0.1), (-0.45, 0.38), (0.45, 0.38)],
             [(0, 1), (0, 2), (1, 3), (2, 4), (3, 8), (8, 5), (4, 9), (9, 6), (1, 7), (7, 2)]),
    "Scales": ([(0.0, 0.62), (-0.55, 0.3), (0.55, 0.3), (-0.72, -0.35), (-0.38, -0.35), (0.38, -0.35), (0.72, -0.35), (0.0, 0.3), (0.0, -0.6), (-0.55, -0.5), (0.55, -0.5)],
               [(0, 7), (1, 7), (7, 2), (1, 3), (1, 4), (3, 9), (9, 4), (2, 5), (2, 6), (5, 10), (10, 6), (7, 8)]),
    "WaterBearer": ([(-0.7, 0.35), (-0.42, 0.55), (-0.14, 0.3), (0.14, 0.55), (0.42, 0.3), (0.7, 0.55),
                     (-0.7, -0.35), (-0.42, -0.12), (-0.14, -0.4), (0.14, -0.12), (0.42, -0.4), (0.7, -0.12)],
                    [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (6, 7), (7, 8), (8, 9), (9, 10), (10, 11)]),
    "Lion": ([(0.5, -0.5), (0.1, -0.42), (-0.55, -0.48), (-0.62, 0.0), (-0.05, 0.02), (0.28, 0.2), (0.22, 0.52), (0.5, 0.66), (0.7, 0.44), (0.62, 0.18)],
             [(0, 1), (1, 2), (2, 3), (3, 4), (4, 1), (4, 5), (5, 6), (6, 7), (7, 8), (8, 9)]),
}


def signs():
    S = 128
    x, y = grid(S)
    for name, (pts, lines) in SIGNS.items():
        img, d, n = canvas(S)

        def P(p):
            return (n / 2 + p[0] * n * 0.42, n / 2 - p[1] * n * 0.42)
        for i, j in lines:
            d.line([P(pts[i]), P(pts[j])], fill=150, width=max(1, int(n * 0.012)))
        line = shrink(img, S)
        a = line * 0.75 + arr(blur(Image.fromarray((line * 255).astype(np.uint8)), 2.5)) * 0.5
        rng = random.Random(hash(name) % 1000)
        for k, p in enumerate(pts):
            sc = 0.2 if k % 3 == 0 else 0.13
            a += star_field((x - p[0] * 0.84) / sc, (y - p[1] * 0.84) / sc, 0.42, 0.42, 0.1, 0.035, 0.06)
        # a thin broken ring: the wheel the sign sits on
        r = np.sqrt(x * x + y * y)
        ang = np.arctan2(y, x)
        a += 0.4 * np.exp(-((r - 0.93) / 0.012) ** 2) * (np.cos(ang * 6 + rng.uniform(0, 6)) > -0.5)
        save(white(np.clip(a, 0, 1) * fade(x, y, 0.04), S), "Vfx/Sign_" + name)


# ------------------------------------------------------------------------------------------------ casting circles

def circle_astral():
    S = 512
    img, d, n = canvas(S, 2)
    c = n / 2
    R = n * 0.47

    def circ(rr, w, v=255):
        d.ellipse([c - rr, c - rr, c + rr, c + rr], outline=v, width=max(1, int(w)))

    def pol(rr, th):
        return (c + rr * math.cos(th), c - rr * math.sin(th))

    circ(R, n * 0.008)
    circ(R * 0.965, n * 0.003)
    circ(R * 0.80, n * 0.006)
    circ(R * 0.50, n * 0.004)
    circ(R * 0.47, n * 0.002)
    # twelve houses
    for i in range(12):
        th = i * math.pi / 6
        d.line([pol(R * 0.80, th), pol(R * 0.965, th)], fill=255, width=max(1, int(n * 0.004)))
    # degree ticks
    for i in range(72):
        th = i * math.pi / 36
        d.line([pol(R * 0.965, th), pol(R * (0.985 if i % 6 else 1.0), th)], fill=220, width=max(1, int(n * 0.002)))
    # the phases of the moon in four of the houses, small constellations in the rest
    rng = random.Random(44)
    for i in range(12):
        th = i * math.pi / 6 + math.pi / 12
        px, py = pol(R * 0.883, th)
        rr = R * 0.05
        if i % 3 == 0:
            ph = i // 3
            d.ellipse([px - rr, py - rr, px + rr, py + rr], outline=255, width=max(1, int(n * 0.003)), fill=255 if ph == 2 else 0)
            if ph in (1, 3):
                off = rr * (0.9 if ph == 1 else -0.9)
                d.pieslice([px - rr, py - rr, px + rr, py + rr], -90 if ph == 1 else 90, 90 if ph == 1 else 270, fill=255)
                d.ellipse([px - abs(off) * 0.45, py - rr, px + abs(off) * 0.45, py + rr], fill=0)
                d.ellipse([px - rr, py - rr, px + rr, py + rr], outline=255, width=max(1, int(n * 0.003)))
        else:
            ps = [(px + rng.uniform(-1.3, 1.3) * rr, py + rng.uniform(-1.0, 1.0) * rr) for _ in range(4)]
            d.line(ps, fill=170, width=max(1, int(n * 0.002)))
            for q in ps:
                d.ellipse([q[0] - n * 0.004, q[1] - n * 0.004, q[0] + n * 0.004, q[1] + n * 0.004], fill=255)
    # a twelve-pointed star woven from two hexagrams and a square pair
    for k, rot in ((3, 0), (3, math.pi / 3), (4, math.pi / 12), (4, math.pi / 12 + math.pi / 4)):
        ps = [pol(R * 0.80, rot + j * 2 * math.pi / k + math.pi / 2) for j in range(k)]
        d.polygon(ps, outline=215, width=max(1, int(n * 0.003)))
    # eight long rays from the centre star
    for i in range(8):
        th = i * math.pi / 4
        ln = R * (0.47 if i % 2 == 0 else 0.3)
        w = n * 0.012
        tip = pol(ln, th)
        a1, a2 = pol(w, th + math.pi / 2), pol(w, th - math.pi / 2)
        d.polygon([tip, a1, a2], fill=255)
    circ(R * 0.07, n * 0.004)
    m = img.resize((S, S), Image.LANCZOS)
    a = arr(m)
    gl = arr(blur(m, S * 0.008))
    x, y = grid(S)
    # stars strewn over the whole circle
    rng = random.Random(9)
    st = np.zeros((S, S), F)
    for _ in range(40):
        rr, th = rng.uniform(0.1, 0.9), rng.uniform(0, 6.283)
        cx, cy, sc = rr * math.cos(th), rr * math.sin(th), rng.uniform(0.012, 0.035)
        st += star_field((x - cx) / sc, (y - cy) / sc, 0.4, 0.4, 0.1, 0.04, 0.07) * 0.7
    save(white(np.clip(a + gl * 0.8 + st, 0, 1), S), "Vfx/Circle_Astral")


def circle_draconic():
    S = 512
    img, d, n = canvas(S, 2)
    c = n / 2
    R = n * 0.47

    def circ(rr, w, v=255):
        d.ellipse([c - rr, c - rr, c + rr, c + rr], outline=v, width=max(1, int(w)))

    def pol(rr, th):
        return (c + rr * math.cos(th), c - rr * math.sin(th))

    # the outer ring is a wyrm biting its own tail: overlapping scales all the way round
    count = 46
    for ring_r, phase, size in ((R * 0.955, 0.0, 0.058), (R * 0.905, 0.5, 0.05)):
        for i in range(count):
            th = (i + phase) * 2 * math.pi / count
            px, py = pol(ring_r, th)
            rr = R * size
            box = [px - rr, py - rr, px + rr, py + rr]
            start = -math.degrees(th) - 115
            d.arc(box, start, start + 230, fill=255, width=max(1, int(n * 0.004)))
    circ(R, n * 0.006)
    circ(R * 0.845, n * 0.007)
    # teeth pointing inward
    for i in range(24):
        th = i * math.pi / 12
        tip = pol(R * (0.70 if i % 2 == 0 else 0.76), th)
        a1, a2 = pol(R * 0.845, th + 0.045), pol(R * 0.845, th - 0.045)
        d.polygon([tip, a1, a2], fill=235)
    circ(R * 0.60, n * 0.005)
    circ(R * 0.57, n * 0.002)
    # three claw marks, raked across the middle in a spiral
    for k in range(3):
        base = k * 2 * math.pi / 3 + 0.4
        for j, off in enumerate((-0.16, 0.0, 0.16)):
            pts = []
            for t in range(13):
                f = t / 12
                rr = R * (0.12 + 0.44 * f)
                th = base + off * (1.0 - 0.3 * f) + f * 0.9
                pts.append(pol(rr, th))
            for t in range(12):
                f = t / 12
                w = n * (0.004 + 0.012 * math.sin(math.pi * min(1.0, f * 1.15)))
                d.line([pts[t], pts[t + 1]], fill=255, width=max(1, int(w)))
    # the slit eye in the centre
    er = R * 0.11
    d.ellipse([c - er, c - er, c + er, c + er], outline=255, width=max(1, int(n * 0.005)))
    d.polygon([pol(er * 0.86, math.pi / 2), pol(er * 0.26, 0), pol(er * 0.86, -math.pi / 2), pol(er * 0.26, math.pi)], fill=255)
    m = img.resize((S, S), Image.LANCZOS)
    a = arr(m)
    gl = arr(blur(m, S * 0.008))
    save(white(np.clip(a + gl * 0.8, 0, 1), S), "Vfx/Circle_Draconic")


# ------------------------------------------------------------------------------------------------ fire, scale, shell, coin

def flame_field(x, y, seed, tongues=((0.0, 1.0, 0.44), (-0.26, 0.62, 0.24), (0.27, 0.7, 0.26), (0.1, 0.45, 0.16))):
    S = x.shape[0]
    wob = (noise(S, 5, 3, seed) - 0.5)
    a = np.zeros_like(x)
    for cx, height, width in tongues:
        t = np.clip((y + 0.85) / (1.7 * height), 0, 1.2)           # 0 at the base, 1 at the tip
        w = width * (np.clip(np.sin(np.clip(t, 0, 1) * math.pi), 0, 1) ** 0.55) * (1.0 - 0.72 * np.clip(t, 0, 1)) + 0.004
        sway = 0.16 * t * np.sin(t * 6.0 + cx * 9.0) + wob * 0.42 * t
        dx = np.abs(x - cx * (1 - 0.4 * t) - sway)
        body = sstep(1.0, 0.45, dx / w) * (t < 1.0) * (y > -0.9)
        a = np.maximum(a, body * (1.0 - 0.3 * t))
    return a


def fire():
    S = 128
    x, y = grid(S)
    outer = flame_field(x, y, 61)
    inner = flame_field(x * 1.75, y * 1.35 + 0.3, 62)
    a = 0.62 * outer + 0.55 * inner
    soft = arr(blur(Image.fromarray((np.clip(outer, 0, 1) * 255).astype(np.uint8)), 4))
    a = np.clip(a + soft * 0.35, 0, 1) * fade(x, y, 0.1)
    save(white(a, S), "Vfx/WyrmFlame")

    # a scale: a shield shape with a ridge down the middle, lit from the upper left
    S = 64
    x, y = grid(S)
    w = 0.62 * np.sqrt(np.clip(1 - ((y - 0.12) / 0.78) ** 2, 0, 1)) * (1.0 - 0.55 * (np.clip(0.12 - y, 0, 2) / 0.9) ** 1.6)
    inside = sstep(0.0, 0.07, w - np.abs(x)) * (np.abs(y - 0.0) < 0.92)
    ridge = np.exp(-(x / 0.07) ** 2)
    slope = np.clip(-x / np.maximum(w, 0.05), -1, 1)
    v = 0.62 + 0.24 * slope + 0.22 * ridge + 0.12 * y
    rim_ = 1.0 - sstep(0.0, 0.16, w - np.abs(x))
    v = v * (1.0 - 0.45 * rim_)
    save(grey(v, inside), "Vfx/WyrmScale")

    # a shard of eggshell
    S = 64
    img, d, n = canvas(S)

    def P(px, py):
        return (n / 2 + px * n / 2, n / 2 - py * n / 2)
    d.polygon([P(-0.62, -0.3), (P(-0.4, 0.2)), P(-0.5, 0.45), P(-0.1, 0.72), P(0.12, 0.4), P(0.45, 0.62), P(0.66, 0.05), P(0.38, -0.2), P(0.5, -0.55),
               P(0.02, -0.4), P(-0.25, -0.7)], fill=255)
    m = shrink(img, S)
    x, y = grid(S)
    v = 0.86 + 0.14 * (-x * 0.6 + y * 0.6) - 0.3 * (1 - sstep(0.0, 0.9, m)) * 0
    speck = sstep(0.62, 0.7, noise(S, 9, 2, 14))
    v = v * (1.0 - 0.4 * speck)
    edge = m - arr(blur(Image.fromarray((m * 255).astype(np.uint8)), 2))
    v = np.clip(v - 0.6 * np.clip(edge, 0, 1), 0, 1)
    save(grey(v, m), "Vfx/EggShell")

    # a coin, seen a little from the side, with a wyrm's eye struck into it
    S = 64
    x, y = grid(S)
    yy = y / 0.9
    r = np.sqrt(x * x + yy * yy)
    disc = sstep(0.86, 0.82, r)
    thick = sstep(0.86, 0.82, np.sqrt(x * x + ((y + 0.1) / 0.9) ** 2))
    alpha = np.maximum(disc, thick)
    light = 0.72 + 0.2 * (-x * 0.6 + y * 0.8)
    rim_ = np.exp(-((r - 0.74) / 0.05) ** 2)
    v = light + 0.2 * rim_ * (-x * 0.6 + y * 0.8 > 0) - 0.18 * rim_ * (-x * 0.6 + y * 0.8 <= 0)
    eye = sstep(0.36, 0.32, np.sqrt(x * x + (yy / 0.62) ** 2)) - sstep(0.1, 0.07, np.sqrt((x / 0.32) ** 2 + (yy / 1.0) ** 2) * 0.4)
    v = v - 0.2 * np.clip(eye, 0, 1) + 0.25 * np.exp(-(((x + 0.3) ** 2 + (yy - 0.38) ** 2) / 0.02))
    v = np.where(disc > 0.5, v, 0.5 + 0.12 * np.sin(x * 60))
    save(grey(v, alpha), "Vfx/Coin")


# ------------------------------------------------------------------------------------------------ spectral wings

def wings():
    """Draconic Aspect: a pair of wings of light standing behind the caster."""
    S = 256
    img, d, n = canvas(S, 3)
    bones, d2, _ = canvas(S, 3)

    def P(px, py):
        return (n / 2 + px * n / 2, n / 2 - py * n / 2)

    for s in (-1, 1):
        root = (s * 0.05, -0.06)
        elbow = (s * 0.2, 0.26)
        wrist = (s * 0.34, 0.74)
        tips = [(s * 0.98, 0.56), (s * 0.96, 0.02), (s * 0.76, -0.42), (s * 0.46, -0.64)]
        outline = [root, elbow, wrist]
        for a, b in zip(tips[:-1], tips[1:]):
            outline.append(a)
            mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
            outline.append((mx * 0.84 + wrist[0] * 0.16, my * 0.84 + wrist[1] * 0.16))
        outline.append(tips[-1])
        outline.append((s * 0.09, -0.34))
        d.polygon([P(*p) for p in outline], fill=255)
        w = max(1, int(n * 0.012))
        d2.line([P(*root), P(*elbow), P(*wrist)], fill=255, width=int(w * 1.6), joint="curve")
        for t in tips:
            d2.line([P(*wrist), P(*t)], fill=255, width=w)
        d2.line([P(*wrist), P(s * 0.3, 0.93)], fill=255, width=w)
    m = shrink(img, S)
    b = shrink(bones, S)
    x, y = grid(S)
    near_bone = arr(blur(Image.fromarray((b * 255).astype(np.uint8)), 7))
    skin = m * (0.16 + 0.5 * np.clip(near_bone * 2.2, 0, 1)) * (0.75 + 0.25 * noise(S, 10, 3, 5))
    edge = np.clip(m - arr(blur(Image.fromarray((m * 255).astype(np.uint8)), 3)), 0, 1) * 2.0
    a = skin + 0.95 * b + 0.5 * arr(blur(Image.fromarray((b * 255).astype(np.uint8)), 2.5)) + 0.6 * edge
    save(white(np.clip(a, 0, 1) * fade(x, y, 0.03), S), "Vfx/DragonWings")


# ------------------------------------------------------------------------------------------------ the wall of heaven

def star_wall():
    """A pane of night sky standing on end: dark glass with stars in it and a bright rim."""
    S = 128
    x, y = grid(S)
    hw, hh = 0.86, 0.86
    d_box = np.maximum(np.abs(x) - hw, np.abs(y) - hh)
    inside = sstep(0.0, -0.03, d_box)
    rim_ = np.exp(-(d_box / 0.07) ** 2) * (d_box < 0.02)
    n1 = noise(S, 4, 4, 91)
    deep = np.array([18, 22, 64], F)
    violet = np.array([70, 60, 150], F)
    pale = np.array([200, 225, 255], F)
    rgb = deep[None, None] * (1 - n1[..., None]) + violet[None, None] * n1[..., None]
    band = np.exp(-((x * 0.6 + y * 0.8 - (n1 - 0.5) * 0.6) / 0.28) ** 2)          # a milky way across the pane
    rgb = rgb + band[..., None] * np.array([70, 80, 120], F)
    st = np.zeros((S, S), F)
    rng = random.Random(17)
    for _ in range(34):
        cx, cy, sc = rng.uniform(-0.78, 0.78), rng.uniform(-0.78, 0.78), rng.uniform(0.03, 0.11)
        st += star_field((x - cx) / sc, (y - cy) / sc, 0.4, 0.4, 0.1, 0.04, 0.07)
    rgb = rgb + np.clip(st, 0, 1.2)[..., None] * 255
    rgb = rgb * (1 - rim_[..., None]) + pale[None, None] * rim_[..., None]
    # bevel: lighter top-left, darker bottom-right
    bev = np.clip((-x + y) * 0.12, -0.12, 0.12)
    rgb = rgb * (1 + bev[..., None])
    alpha = np.clip(inside * 0.94 + 0.35 * np.exp(-(np.maximum(d_box, 0) / 0.05) ** 2) * (d_box >= 0), 0, 1)
    save(to_rgba(np.clip(rgb, 0, 255), alpha * 255), "Things/StarWall")


def make_all():
    stars()
    moons()
    signs()
    circle_astral()
    circle_draconic()
    fire()
    wings()
    star_wall()


if __name__ == "__main__":
    make_all()
    print("Astral and draconic textures done.")

"""
Procedurally renders every texture the mod needs.
Usage: python3 Tools/art/make_textures.py   (after Tools/gen/build.py has produced the icon manifest)
"""
import json
import math
import os
import random
import sys
import zlib

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
TEX = os.path.join(ROOT, "Textures", "ArcanumAscendant")
sys.path.insert(0, HERE)
import motifs  # noqa: E402

random.seed(1337)
np.random.seed(1337)


# ------------------------------------------------------------------------------------------ helpers

def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], dtype=np.float32)


def mix(a, b, t):
    return a * (1 - t) + b * t


WHITE = np.array([255, 255, 255], np.float32)
BLACK = np.array([0, 0, 0], np.float32)


def save(img, rel):
    path = os.path.join(TEX, rel + ".png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, optimize=True)


def to_rgba(rgb, alpha):
    rgb = np.clip(rgb, 0, 255).astype(np.uint8)
    a = np.clip(alpha, 0, 255).astype(np.uint8)
    return Image.fromarray(np.dstack([rgb, a]), "RGBA")


def arr(mask):
    return np.asarray(mask, dtype=np.float32) / 255.0


def over(dst, src):
    return Image.alpha_composite(dst, src)


def solid(size, color, alpha_mask):
    rgb = np.ones((size, size, 3), np.float32) * color
    return to_rgba(rgb, arr(alpha_mask) * 255)


def vgrad(size, top, bottom):
    t = np.linspace(0, 1, size, dtype=np.float32)[:, None, None]
    g = top[None, None, :] * (1 - t) + bottom[None, None, :] * t
    return np.repeat(g, size, axis=1)


def radial(size, inner, outer, power=1.0, cx=0.5, cy=0.5):
    y, x = np.mgrid[0:size, 0:size].astype(np.float32)
    r = np.sqrt((x / size - cx) ** 2 + (y / size - cy) ** 2) / 0.7071
    r = np.clip(r, 0, 1) ** power
    return inner[None, None, :] * (1 - r[..., None]) + outer[None, None, :] * r[..., None]


def blur(mask, r):
    return mask.filter(ImageFilter.GaussianBlur(r))


def dilate(mask, px):
    k = px * 2 + 1
    return mask.filter(ImageFilter.MaxFilter(k if k % 2 else k + 1))


def scale_mask(mask, s, dx=0, dy=0):
    size = mask.size[0]
    n = max(1, int(size * s))
    small = mask.resize((n, n), Image.LANCZOS)
    out = Image.new("L", (size, size), 0)
    out.paste(small, ((size - n) // 2 + dx, (size - n) // 2 + dy))
    return out


def glyph(name, size, s=1.0, dx=0, dy=0):
    m = motifs.mask(name, size)
    return scale_mask(m, s, dx, dy) if (s != 1.0 or dx or dy) else m


def bevel(mask, size, base, light=1.35, dark=0.55, d=2):
    """Fake bevel lighting for a flat silhouette: lighter top-left edges, darker bottom-right."""
    m = arr(mask)
    sh1 = np.roll(np.roll(m, d, 0), d, 1)
    sh2 = np.roll(np.roll(m, -d, 0), -d, 1)
    hi = np.clip(m - sh1, 0, 1)
    lo = np.clip(m - sh2, 0, 1)
    rgb = np.ones((size, size, 3), np.float32) * base
    rgb = rgb * (1 + (light - 1) * hi[..., None]) * (1 - (1 - dark) * lo[..., None])
    return rgb


def styled_glyph(mask, size, c_top, c_bot, glow_color, glow_r=5, glow_a=0.95, outline_px=3, outline_a=0.9):
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    g = blur(dilate(mask, 2), glow_r)
    layer = over(layer, solid(size, glow_color, g.point(lambda v: int(v * glow_a))))
    if outline_px:
        ol = blur(dilate(mask, outline_px), 1)
        layer = over(layer, solid(size, np.array([12, 8, 18], np.float32), ol.point(lambda v: int(v * outline_a))))
    fill = vgrad(size, c_top, c_bot)
    m = arr(mask)
    sh = np.clip(m - np.roll(np.roll(m, -3, 0), -3, 1), 0, 1)
    fill = fill * (1 - 0.35 * sh[..., None])
    hi = np.clip(m - np.roll(np.roll(m, 2, 0), 2, 1), 0, 1)
    fill = fill + (255 - fill) * 0.45 * hi[..., None]
    layer = over(layer, to_rgba(fill, m * 255))
    return layer


def noise(size, scale=8, octaves=3, seed=None):
    rng = np.random.RandomState(seed if seed is not None else random.randint(0, 99999))
    out = np.zeros((size, size), np.float32)
    amp = 1.0
    tot = 0
    for o in range(octaves):
        n = max(2, scale * (2 ** o))
        base = rng.rand(n, n).astype(np.float32)
        img = Image.fromarray((base * 255).astype(np.uint8)).resize((size, size), Image.BICUBIC)
        out += arr(img) * amp
        tot += amp
        amp *= 0.5
    return out / tot


# ------------------------------------------------------------------------------------------ VFX textures

def vfx_radial(size, fn):
    y, x = np.mgrid[0:size, 0:size].astype(np.float32)
    r = np.sqrt(((x + 0.5) / size - 0.5) ** 2 + ((y + 0.5) / size - 0.5) ** 2) * 2
    return fn(r, x / size - 0.5, y / size - 0.5) * np.clip((1.0 - r) / 0.06, 0, 1)


def white(alpha, size):
    return to_rgba(np.ones((size, size, 3), np.float32) * 255, np.clip(alpha, 0, 1) * 255)


def rune_circle(size, variant=0, seed=7):
    rng = random.Random(seed)
    big = size * 2
    img = Image.new("L", (big, big), 0)
    d = ImageDraw.Draw(img)
    c = big / 2

    def circ(r, w):
        d.ellipse([c - r, c - r, c + r, c + r], outline=255, width=w)

    R = big * 0.47
    if variant == 0:
        circ(R, int(big * 0.012))
        circ(R * 0.86, int(big * 0.007))
        circ(R * 0.55, int(big * 0.006))
        circ(R * 0.5, int(big * 0.004))
        for k in range(2):
            pts = [(c + R * 0.84 * math.cos(math.radians(90 + k * 60 + i * 120)), c - R * 0.84 * math.sin(math.radians(90 + k * 60 + i * 120))) for i in range(3)]
            d.polygon(pts, outline=255, width=int(big * 0.006))
        n = 24
        for i in range(n):
            a = math.radians(i * 360 / n)
            cx, cy = c + R * 0.93 * math.cos(a), c + R * 0.93 * math.sin(a)
            s = big * 0.022
            for _ in range(3):
                x0, y0 = cx + rng.uniform(-s, s), cy + rng.uniform(-s, s)
                x1, y1 = cx + rng.uniform(-s, s), cy + rng.uniform(-s, s)
                d.line([x0, y0, x1, y1], fill=255, width=int(big * 0.005))
        for i in range(6):
            a = math.radians(i * 60 + 30)
            cx, cy = c + R * 0.7 * math.cos(a), c + R * 0.7 * math.sin(a)
            r = big * 0.03
            d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=255, width=int(big * 0.005))
    else:
        circ(R, int(big * 0.008))
        circ(R * 0.92, int(big * 0.004))
        circ(R * 0.7, int(big * 0.009))
        pts = [(c + R * 0.9 * math.cos(math.radians(90 + i * 135)), c - R * 0.9 * math.sin(math.radians(90 + i * 135))) for i in range(8)]
        d.line(pts + [pts[0]], fill=255, width=int(big * 0.005))
        for i in range(16):
            a = math.radians(i * 22.5)
            d.line([c + R * 0.7 * math.cos(a), c + R * 0.7 * math.sin(a), c + R * 0.92 * math.cos(a), c + R * 0.92 * math.sin(a)], fill=255, width=int(big * 0.004))
        for i in range(8):
            a = math.radians(i * 45 + 22.5)
            cx, cy = c + R * 0.81 * math.cos(a), c + R * 0.81 * math.sin(a)
            r = big * 0.035
            d.arc([cx - r, cy - r, cx + r, cy + r], 0, 300, fill=255, width=int(big * 0.005))
        circ(R * 0.3, int(big * 0.006))
    img = img.resize((size, size), Image.LANCZOS)
    a = arr(img)
    g = arr(blur(img, size * 0.012))
    return white(np.clip(a + g * 0.8, 0, 1), size)


def make_vfx():
    S = 256
    save(white(vfx_radial(S, lambda r, x, y: np.exp(-(r / 0.42) ** 2) * (r < 1)), S), "Vfx/Glow")
    save(white(vfx_radial(S, lambda r, x, y: np.exp(-((r - 0.86) / 0.05) ** 2)), S), "Vfx/Ring")
    save(white(vfx_radial(S, lambda r, x, y: np.exp(-((r - 0.84) / 0.07) ** 2) + 0.25 * np.clip(r, 0, 0.84) ** 4 * (r < 0.9)), S), "Vfx/Shockwave")
    save(rune_circle(S, 0, 7), "Vfx/RuneCircle")
    save(rune_circle(S, 1, 11), "Vfx/RuneCircle2")
    # beam: varies across width (x), constant along length
    y, x = np.mgrid[0:S, 0:S].astype(np.float32)
    u = (x / S - 0.5) * 2
    streak = 0.85 + 0.15 * noise(S, 4, 2, 3)
    save(white(np.exp(-(u / 0.35) ** 2) * streak + 0.6 * np.exp(-(u / 0.08) ** 2), S), "Vfx/Beam")
    v = 1 - y / S  # PIL row 0 is top; Unity v=0 is bottom -> pillar base at image bottom
    save(white((np.exp(-(u / 0.35) ** 2) + 0.5 * np.exp(-(u / 0.08) ** 2)) * np.clip(1 - v, 0, 1) ** 0.6, S), "Vfx/Pillar")
    # hex shield
    hexmask = Image.new("L", (S * 2, S * 2), 0)
    d = ImageDraw.Draw(hexmask)
    hr = S * 2 * 0.06
    for row in range(-12, 13):
        for col in range(-12, 13):
            cx = S + col * hr * 1.5
            cy = S + row * hr * math.sqrt(3) + (hr * math.sqrt(3) / 2 if col % 2 else 0)
            pts = [(cx + hr * math.cos(math.radians(a)), cy + hr * math.sin(math.radians(a))) for a in range(0, 360, 60)]
            d.polygon(pts, outline=255, width=3)
    hexa = arr(hexmask.resize((S, S), Image.LANCZOS))
    save(white(vfx_radial(S, lambda r, x, yy: (0.15 + 0.85 * hexa * 0.6) * np.clip(r, 0, 1) ** 2.5 * (r < 0.97) + np.exp(-((r - 0.93) / 0.04) ** 2)), S), "Vfx/HexShield")
    # small particles
    s = 64
    save(white(vfx_radial(s, lambda r, x, y: np.clip(np.exp(-(np.abs(x) * np.abs(y) * 600)) * np.exp(-(r / 0.8) ** 2) + np.exp(-(r / 0.18) ** 2), 0, 1)), s), "Vfx/Spark")
    save(white(vfx_radial(s, lambda r, x, y: np.exp(-(r / 0.55) ** 2) * (0.6 + 0.4 * noise(s, 3, 2, 5)) + 0.6 * np.exp(-(r / 0.2) ** 2)), s), "Vfx/Ember")
    save(white(vfx_radial(s, lambda r, x, y: np.exp(-(r / 0.5) ** 2)), s), "Vfx/Mote")
    sm = np.clip(vfx_radial(128, lambda r, x, y: np.exp(-(r / 0.62) ** 2)) * (0.45 + 0.75 * noise(128, 3, 3, 9)) - 0.1, 0, 1)
    save(to_rgba(np.ones((128, 128, 3), np.float32) * 200, sm * 255), "Vfx/Smoke")
    for name, motif, sz in [("Shard", "shard", 64), ("Leaf", "leaf", 64), ("Note", "note", 64), ("Rune", "rune", 64), ("Snow", "snowflake", 64), ("Bone", "bone", 64),
                            ("Feather", "feather", 64), ("BloodDrop", "drop", 64), ("Gear", "gear", 64), ("Star", "star", 64), ("Skull", "skull", 64),
                            ("Petal", "flower", 64), ("Clock", "clock", 128), ("Bolt", "bolt", 64)]:
        m = glyph(motif, sz, 0.9)
        a = np.clip(arr(m) + arr(blur(m, sz * 0.05)) * 0.6, 0, 1)
        save(white(a, sz), "Vfx/" + name)
    # projectiles (pointing up)
    P = 128
    save(white(vfx_radial(P, lambda r, x, y: np.clip(np.exp(-(r / 0.35) ** 2) * 1.2 + np.exp(-(r / 0.75) ** 2) * 0.4, 0, 1)), P), "Vfx/Orb")
    fb = Image.new("L", (P, P), 0)
    fp = motifs.Pen(P * 4)
    fp.poly([(x * 0.9, y * 0.9 - 0.1) for x, y in motifs.rot(motifs.flame_pts(0, 0.1, 0.55, 1.5), 180)])
    fb = fp.img.resize((P, P), Image.LANCZOS)
    core = vfx_radial(P, lambda r, x, y: np.exp(-((x) ** 2 + (y + 0.12) ** 2) / 0.02))
    save(white(np.clip(arr(fb) * (0.55 + 0.45 * noise(P, 4, 2, 13)) + core, 0, 1), P), "Vfx/Fireball")
    mt = arr(glyph("meteor", P, 1.0))
    save(white(np.clip(mt + arr(blur(Image.fromarray((mt * 255).astype(np.uint8)), 4)) * 0.5, 0, 1), P), "Vfx/Meteor")
    for name, motif in [("Skull", "skull"), ("Hammer", "hammer"), ("Thorn", "thorn"), ("Bullet", "bullet"), ("Pie", "pie"), ("Dagger", "dagger"), ("Bone", "bone")]:
        m = glyph(motif, P, 0.85)
        save(white(np.clip(arr(m) + arr(blur(m, 5)) * 0.5, 0, 1), P), "Vfx/" + name)
    save(white(np.clip(arr(glyph("rune", P, 0.8)) + vfx_radial(P, lambda r, x, y: np.exp(-(r / 0.5) ** 2) * 0.5), 0, 1), P), "Vfx/RuneBolt")
    save(white(vfx_radial(P, lambda r, x, y: np.exp(-((r - 0.42) / 0.14) ** 2) + 0.3 * np.exp(-(r / 0.8) ** 2)), P), "Vfx/VoidOrb")
    save(white(vfx_radial(P, lambda r, x, y: np.clip(np.exp(-(r / 0.4) ** 2) * (0.7 + 0.5 * noise(P, 3, 2, 21)), 0, 1)), P), "Vfx/BloodOrb")
    # ground textures for fields and auras
    G = 256
    fr = rune_circle(G, 0, 23)
    flames = Image.new("L", (G, G), 0)
    for k in range(8):
        a = k * 45
        fm = glyph("flame", 48, 1.0)
        fm = fm.rotate(-a, expand=False)
        cx = G / 2 + G * 0.36 * math.cos(math.radians(a - 90))
        cy = G / 2 + G * 0.36 * math.sin(math.radians(a - 90))
        flames.paste(fm, (int(cx - 24), int(cy - 24)), fm)
    fa = np.clip(arr(fr.split()[3]) + arr(flames) * 0.8 + vfx_radial(G, lambda r, x, y: 0.25 * np.exp(-(r / 0.7) ** 2)), 0, 1)
    save(white(fa, G), "Vfx/FireRune")
    # frost pattern
    fz = Image.new("L", (G * 2, G * 2), 0)
    d = ImageDraw.Draw(fz)
    rng = random.Random(3)

    def branch(x, y, ang, length, w, depth):
        if depth == 0 or length < 6:
            return
        x2, y2 = x + length * math.cos(ang), y + length * math.sin(ang)
        d.line([x, y, x2, y2], fill=255, width=max(1, int(w)))
        for s in (-1, 1):
            branch(x + (x2 - x) * 0.5, y + (y2 - y) * 0.5, ang + s * math.radians(rng.uniform(45, 65)), length * 0.45, w * 0.7, depth - 1)
        branch(x2, y2, ang + math.radians(rng.uniform(-10, 10)), length * 0.6, w * 0.8, depth - 1)

    for k in range(12):
        branch(G, G, math.radians(k * 30 + rng.uniform(-5, 5)), G * 0.45, 6, 4)
    fza = arr(fz.resize((G, G), Image.LANCZOS))
    save(white(np.clip(fza + vfx_radial(G, lambda r, x, y: 0.3 * np.exp(-(r / 0.75) ** 2) + np.exp(-((r - 0.93) / 0.03) ** 2) * 0.8), 0, 1), G), "Vfx/Frost")
    # holy rune: rune circle + sun rays
    hr_ = rune_circle(G, 1, 31)
    rays = vfx_radial(G, lambda r, x, y: (0.5 + 0.5 * np.cos(np.arctan2(y, x) * 12)) ** 6 * np.exp(-(r / 0.6) ** 2) * 0.7)
    save(white(np.clip(arr(hr_.split()[3]) + rays + vfx_radial(G, lambda r, x, y: 0.25 * np.exp(-(r / 0.6) ** 2)), 0, 1), G), "Vfx/HolyRune")
    # roots
    rt = Image.new("L", (G * 2, G * 2), 0)
    d = ImageDraw.Draw(rt)
    rng = random.Random(5)

    def root(x, y, ang, length, w, depth):
        if depth == 0:
            return
        pts = [(x, y)]
        for i in range(6):
            ang += math.radians(rng.uniform(-25, 25))
            x += length / 6 * math.cos(ang)
            y += length / 6 * math.sin(ang)
            pts.append((x, y))
        d.line(pts, fill=255, width=max(1, int(w)), joint="curve")
        for _ in range(2):
            px, py = pts[rng.randint(2, 5)]
            root(px, py, ang + math.radians(rng.choice([-1, 1]) * rng.uniform(30, 70)), length * 0.55, w * 0.6, depth - 1)

    for k in range(9):
        root(G, G, math.radians(k * 40), G * 0.6, 12, 3)
    save(white(np.clip(arr(rt.resize((G, G), Image.LANCZOS)) * vfx_radial(G, lambda r, x, y: np.clip(1.2 - r, 0, 1)), 0, 1), G), "Vfx/Roots")
    # vortex
    save(white(vfx_radial(G, lambda r, x, y: np.clip((0.5 + 0.5 * np.sin(np.arctan2(y, x) * 4 + r * 14)) ** 3 * np.exp(-(r / 0.7) ** 2) * 1.2 + np.exp(-(r / 0.15) ** 2) * 0.5, 0, 1)), G), "Vfx/Vortex")
    bh = vfx_radial(G, lambda r, x, y: np.clip(1.15 - (r / 0.55) ** 3, 0, 1))
    save(to_rgba(np.zeros((G, G, 3), np.float32), bh * 255), "Vfx/BlackHole")
    # clock face
    ck = glyph("clock", G, 1.0)
    save(white(np.clip(arr(ck) * 0.9 + arr(blur(ck, 6)) * 0.5, 0, 1), G), "Vfx/Clock")
    # hex sigil (pentagram)
    hx = motifs.Pen(G * 2)
    hx.ring(0, 0, 0.92, 0.03)
    hx.ring(0, 0, 0.8, 0.015)
    pts = motifs.star_pts(5, 0.8, 0.8)[::2]
    pent = [pts[i] for i in (0, 2, 4, 1, 3, 0)]
    hx.line(pent, 0.025)
    hxa = hx.img.resize((G, G), Image.LANCZOS)
    save(white(np.clip(arr(hxa) + arr(blur(hxa, 5)) * 0.6, 0, 1), G), "Vfx/Hex")
    cl = glyph("claw", G, 0.8)
    save(white(np.clip(arr(cl) * 0.7 + arr(blur(cl, 8)) * 0.6, 0, 1), G), "Vfx/Claw")
    wg = Image.new("L", (G, G), 0)
    left = glyph("wing", G, 0.7, dx=-int(G * 0.2), dy=int(G * 0.02))
    right = left.transpose(Image.FLIP_LEFT_RIGHT)
    wg = ImageChops.lighter(left, right)
    save(white(np.clip(arr(wg) * 0.8 + arr(blur(wg, 8)) * 0.6, 0, 1), G), "Vfx/Wings")


# ------------------------------------------------------------------------------------------ icons

def spell_icon(motif, motif2, c1h, c2h, ultimate):
    S = 128
    c1, c2 = hexc(c1h), hexc(c2h)
    bg = radial(S, mix(c1, BLACK, 0.45), mix(c1, BLACK, 0.92), 1.2)
    img = to_rgba(bg, np.ones((S, S)) * 255)
    rc = rune_circle(S, 0, zlib.crc32(motif.encode()) % 100)
    img = over(img, to_rgba(np.ones((S, S, 3)) * mix(c1, WHITE, 0.2), arr(rc.split()[3]) * 0.16 * 255))
    n = noise(S, 6, 3, zlib.crc32(c1h.encode()) % 1000)
    img = over(img, to_rgba(np.ones((S, S, 3)) * c1, (n - 0.5).clip(0, 1) * 60))
    main = glyph(motif, S, 0.74 if not motif2 else 0.7, dx=0 if not motif2 else -5, dy=0 if not motif2 else -5)
    img = over(img, styled_glyph(main, S, mix(c2, WHITE, 0.55), mix(c1, WHITE, 0.15), mix(c1, WHITE, 0.25), glow_r=6))
    if motif2:
        sec = glyph(motif2, S, 0.4, dx=int(S * 0.27), dy=int(S * 0.27))
        disc = Image.new("L", (S, S), 0)
        dd = ImageDraw.Draw(disc)
        cx, cy, r = S * 0.77, S * 0.77, S * 0.21
        dd.ellipse([cx - r, cy - r, cx + r, cy + r], fill=255)
        img = over(img, solid(S, mix(c1, BLACK, 0.7), blur(disc, 1).point(lambda v: int(v * 0.85))))
        ring_ = Image.new("L", (S, S), 0)
        ImageDraw.Draw(ring_).ellipse([cx - r, cy - r, cx + r, cy + r], outline=255, width=2)
        img = over(img, solid(S, mix(c2, WHITE, 0.3), ring_))
        img = over(img, styled_glyph(sec, S, WHITE, mix(c2, WHITE, 0.3), c2, glow_r=3, glow_a=0.7, outline_px=2))
    vig = vfx_radial(S, lambda r, x, y: np.clip((r - 0.7) / 0.6, 0, 1) ** 1.5)
    img = over(img, to_rgba(np.zeros((S, S, 3)), vig * 160))
    b = Image.new("L", (S, S), 0)
    bd = ImageDraw.Draw(b)
    if ultimate:
        gold = hexc("#ffd36b")
        bd.rectangle([1, 1, S - 2, S - 2], outline=255, width=5)
        img = over(img, solid(S, gold, b))
        corners = Image.new("L", (S, S), 0)
        cd = ImageDraw.Draw(corners)
        for (x, y) in [(6, 6), (S - 7, 6), (6, S - 7), (S - 7, S - 7)]:
            cd.polygon([(x, y - 9), (x + 9, y), (x, y + 9), (x - 9, y)], fill=255)
        img = over(img, solid(S, mix(gold, WHITE, 0.4), corners))
        inner = Image.new("L", (S, S), 0)
        ImageDraw.Draw(inner).rectangle([7, 7, S - 8, S - 8], outline=255, width=1)
        img = over(img, solid(S, mix(c1, WHITE, 0.4), inner))
    else:
        bd.rectangle([0, 0, S - 1, S - 1], outline=255, width=3)
        img = over(img, solid(S, mix(c1, BLACK, 0.35), b))
        inner = Image.new("L", (S, S), 0)
        ImageDraw.Draw(inner).rectangle([3, 3, S - 4, S - 4], outline=255, width=1)
        img = over(img, solid(S, mix(c1, WHITE, 0.25), inner.point(lambda v: int(v * 0.7))))
    return img


def node_icon(motif, c1h, c2h):
    S = 128
    c1, c2 = hexc(c1h), hexc(c2h)
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    m = glyph(motif, S, 0.78)
    return over(img, styled_glyph(m, S, mix(c2, WHITE, 0.5), mix(c1, WHITE, 0.1), mix(c1, WHITE, 0.2), glow_r=5))


def class_icon(motif, motif2, c1h, c2h, sub):
    S = 256
    c1, c2 = hexc(c1h), hexc(c2h)
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    disc = Image.new("L", (S, S), 0)
    ImageDraw.Draw(disc).ellipse([10, 10, S - 11, S - 11], fill=255)
    bg = radial(S, mix(c1, BLACK, 0.4), mix(c1, BLACK, 0.9), 1.0)
    img = over(img, to_rgba(bg, arr(disc) * 255))
    rc = rune_circle(S, 1 if sub else 0, zlib.crc32(motif.encode()) % 50)
    img = over(img, to_rgba(np.ones((S, S, 3)) * mix(c1, WHITE, 0.3), arr(rc.split()[3]) * arr(disc) * 0.3 * 255))
    ring_ = Image.new("L", (S, S), 0)
    rd = ImageDraw.Draw(ring_)
    rd.ellipse([6, 6, S - 7, S - 7], outline=255, width=10)
    if sub:
        rd.ellipse([22, 22, S - 23, S - 23], outline=255, width=3)
    ringc = vgrad(S, mix(c2, WHITE, 0.4), mix(c1, BLACK, 0.2))
    img = over(img, solid(S, np.array([10, 8, 16], np.float32), blur(dilate(ring_, 2), 1)))
    img = over(img, to_rgba(ringc, arr(ring_) * 255))
    ticks = Image.new("L", (S, S), 0)
    td = ImageDraw.Draw(ticks)
    for k in range(16 if not sub else 8):
        a = math.radians(k * (360 / (16 if not sub else 8)))
        x, y = S / 2 + (S / 2 - 11) * math.cos(a), S / 2 + (S / 2 - 11) * math.sin(a)
        td.ellipse([x - 3, y - 3, x + 3, y + 3], fill=255)
    img = over(img, solid(S, WHITE, ticks))
    main = glyph(motif, S, 0.56, dy=-6 if motif2 else 0)
    img = over(img, styled_glyph(main, S, WHITE, mix(c2, WHITE, 0.2), c1, glow_r=10, outline_px=4))
    if motif2:
        sec = glyph(motif2, S, 0.26, dy=int(S * 0.3))
        img = over(img, styled_glyph(sec, S, WHITE, c2, c2, glow_r=5, outline_px=3))
    return img


def attr_icon(motif, colh):
    S = 128
    c = hexc(colh)
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    m = glyph(motif, S, 0.8)
    return over(img, styled_glyph(m, S, mix(c, WHITE, 0.6), c, c, glow_r=6))


# ------------------------------------------------------------------------------------------ things

def sprite(mask, base, size, glow_color=None, glow_mask=None, outline=3):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    if outline:
        img = over(img, solid(size, np.array([16, 12, 20], np.float32), blur(dilate(mask, outline), 0.8)))
    rgb = bevel(mask, size, base)
    tex = noise(size, 8, 3)
    rgb = rgb * (0.85 + 0.3 * tex[..., None])
    img = over(img, to_rgba(rgb, arr(mask) * 255))
    if glow_mask is not None and glow_color is not None:
        img = over(img, solid(size, glow_color, blur(glow_mask, size * 0.02).point(lambda v: int(v * 0.9))))
        img = over(img, solid(size, mix(glow_color, WHITE, 0.6), glow_mask))
    return img


def pen_mask(size, fn):
    p = motifs.Pen(size * 4)
    fn(p)
    return p.img.resize((size, size), Image.LANCZOS)


def make_thing(path, kind, c1h, c2h):
    c1, c2 = hexc(c1h), hexc(c2h)
    stone = np.array([78, 74, 88], np.float32)
    metal = np.array([120, 118, 128], np.float32)
    if kind == "brazier":
        S = 128
        bowl = pen_mask(S, lambda p: p.circle(0, 0, 0.82))
        inner = pen_mask(S, lambda p: p.circle(0, 0, 0.6))
        img = sprite(bowl, stone, S)
        fire = radial(S, mix(c2, WHITE, 0.4), c1, 1.0)
        img = over(img, to_rgba(fire, arr(inner) * 255))
        fl = pen_mask(S, lambda p: [p.poly(motifs.rot(motifs.flame_pts(0, 0.0, 0.18, 0.7), k * 45)) for k in range(8)])
        img = over(img, solid(S, mix(c2, WHITE, 0.5), fl.point(lambda v: int(v * 0.8))))
    elif kind in ("crystal_base", "coil_base", "turret_base", "sentry_base", "spire_base"):
        S = 128
        if kind == "spire_base":
            base = pen_mask(S, lambda p: [p.ellipse(0.6 * math.cos(math.radians(a)), 0.6 * math.sin(math.radians(a)), 0.28, 0.13) for a in range(0, 360, 40)] + [p.circle(0, 0, 0.5)])
            img = sprite(base, np.array([215, 205, 175], np.float32), S)
        else:
            base = pen_mask(S, lambda p: p.poly([(0.85 * math.cos(math.radians(a)), 0.85 * math.sin(math.radians(a))) for a in range(0, 360, 60 if kind != "sentry_base" else 90)]))
            img = sprite(base, stone if kind == "crystal_base" else metal, S)
            glyphs = pen_mask(S, lambda p: [p.ring(0, 0, 0.55, 0.04)] + [p.line(motifs.rot([(0, 0.62), (0, 0.78)], k * 45), 0.04) for k in range(8)])
            img = over(img, solid(S, c1, blur(glyphs, 2)))
            img = over(img, solid(S, mix(c1, WHITE, 0.5), glyphs))
    elif kind == "crystal_top":
        S = 128
        cm = glyph("crystal", S, 0.75)
        img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        img = over(img, solid(S, c1, blur(cm, 8)))
        img = over(img, styled_glyph(cm, S, mix(c2, WHITE, 0.6), c1, c1, glow_r=6, outline_px=2))
    elif kind == "coil_top":
        S = 128
        img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        coil = pen_mask(S, lambda p: [p.ring(0, 0, r, 0.06) for r in (0.7, 0.55, 0.4)] + [p.circle(0, 0, 0.25)])
        img = over(img, sprite(coil, metal, S, outline=2))
        orb = pen_mask(S, lambda p: p.circle(0, 0, 0.22))
        img = over(img, solid(S, c1, blur(orb, 8)))
        img = over(img, solid(S, mix(c2, WHITE, 0.5), orb))
        bolts = pen_mask(S, lambda p: [p.line(motifs.rot([(0, 0.25), (0.08, 0.45), (-0.05, 0.6), (0.05, 0.8)], k * 90 + 20), 0.03) for k in range(4)])
        img = over(img, solid(S, mix(c2, WHITE, 0.6), bolts))
    elif kind == "turret_top":
        S = 128
        g = pen_mask(S, lambda p: [p.rect(-0.12, 0.0, 0.12, 0.85), p.circle(0, 0, 0.38)])
        img = sprite(g, np.array([170, 140, 90], np.float32), S, glow_color=c1, glow_mask=pen_mask(S, lambda p: [p.circle(0, 0, 0.18), p.rect(-0.04, 0.3, 0.04, 0.8)]))
    elif kind == "sentry_top":
        S = 128
        g = pen_mask(S, lambda p: [p.rect(-0.3, 0.0, -0.12, 0.9), p.rect(0.12, 0.0, 0.3, 0.9), p.rect(-0.42, -0.45, 0.42, 0.25)])
        img = sprite(g, metal, S, glow_color=c1, glow_mask=pen_mask(S, lambda p: [p.rect(-0.3, -0.25, 0.3, -0.12), p.circle(0, 0.05, 0.08)]))
    elif kind == "spire_top":
        S = 128
        g = pen_mask(S, lambda p: [p.poly(motifs.rot([(-0.12, 0.0), (0.12, 0.0), (0.0, 0.9)], a)) for a in (-25, 0, 25)] + [p.circle(0, 0, 0.25)])
        img = sprite(g, np.array([228, 220, 190], np.float32), S, glow_color=c1, glow_mask=pen_mask(S, lambda p: p.circle(0, 0, 0.1)))
    elif kind in ("titan_base", "titan_top"):
        S = 256
        if kind == "titan_base":
            body = pen_mask(S, lambda p: p.poly([(0.85 * math.cos(math.radians(a)) * (1 + 0.1 * math.sin(a * 0.2)), 0.85 * math.sin(math.radians(a)) * (1 + 0.1 * math.cos(a * 0.13))) for a in range(0, 360, 15)]))
            img = sprite(body, np.array([60, 48, 46], np.float32), S, outline=4)
            cracks = Image.new("L", (S, S), 0)
            cd = ImageDraw.Draw(cracks)
            rng = random.Random(zlib.crc32(path.encode()) % 1000)
            for _ in range(14):
                x, y = S / 2, S / 2
                a = rng.uniform(0, math.pi * 2)
                pts = [(x, y)]
                for _ in range(6):
                    a += rng.uniform(-0.6, 0.6)
                    x += math.cos(a) * S * 0.06
                    y += math.sin(a) * S * 0.06
                    pts.append((x, y))
                cd.line(pts, fill=255, width=3)
            cracks = ImageChops.multiply(cracks, body)
            img = over(img, solid(S, c1, blur(cracks, 4)))
            img = over(img, solid(S, mix(c2, WHITE, 0.4), cracks))
        else:
            core = pen_mask(S, lambda p: [p.circle(0, 0, 0.42), p.rect(-0.14, 0.2, 0.14, 0.85)])
            img = sprite(core, np.array([70, 58, 52], np.float32), S, glow_color=c1, glow_mask=pen_mask(S, lambda p: [p.circle(0, 0, 0.26), p.rect(-0.06, 0.4, 0.06, 0.82)]), outline=4)
    elif kind in ("icewall", "bonewall", "thornwall", "runewall"):
        S = 128
        block = pen_mask(S, lambda p: p.rect(-0.95, -0.95, 0.95, 0.95))
        base = {"icewall": mix(c1, WHITE, 0.3), "bonewall": np.array([220, 212, 185], np.float32), "thornwall": np.array([70, 110, 50], np.float32),
                "runewall": np.array([110, 108, 118], np.float32)}[kind]
        img = sprite(block, base, S, outline=2)
        detail = Image.new("L", (S, S), 0)
        dd = ImageDraw.Draw(detail)
        rng = random.Random(zlib.crc32(kind.encode()) % 100)
        if kind == "icewall":
            for _ in range(10):
                dd.line([rng.uniform(0, S), rng.uniform(0, S), rng.uniform(0, S), rng.uniform(0, S)], fill=255, width=2)
            img = over(img, solid(S, WHITE, detail.point(lambda v: int(v * 0.6))))
        elif kind == "bonewall":
            for i in range(5):
                y = 14 + i * 24
                dd.line([6, y, S - 6, y + rng.uniform(-6, 6)], fill=255, width=5)
            img = over(img, solid(S, np.array([150, 140, 115], np.float32), detail))
        elif kind == "thornwall":
            for _ in range(26):
                x, y = rng.uniform(5, S - 5), rng.uniform(5, S - 5)
                a = rng.uniform(0, math.pi * 2)
                dd.polygon([(x, y), (x + 14 * math.cos(a) + 3, y + 14 * math.sin(a)), (x + 14 * math.cos(a) - 3, y + 14 * math.sin(a))], fill=255)
            img = over(img, solid(S, np.array([150, 200, 90], np.float32), detail))
        else:
            dd.rectangle([18, 18, S - 19, S - 19], outline=255, width=3)
            dd.line([S / 2, 30, S / 2, S - 30], fill=255, width=4)
            dd.line([S / 2, 50, S / 2 + 22, 70, S / 2, 88], fill=255, width=4)
            img = over(img, solid(S, c1, blur(detail, 3)))
            img = over(img, solid(S, mix(c1, WHITE, 0.5), detail))
    elif kind in ("tree", "worldtree"):
        S = 128 if kind == "tree" else 256
        can = pen_mask(S, lambda p: [p.circle(0.55 * math.cos(math.radians(a)), 0.55 * math.sin(math.radians(a)), 0.38) for a in range(0, 360, 60)] + [p.circle(0, 0, 0.55)])
        img = sprite(can, mix(c1, BLACK, 0.35), S, outline=3)
        leaves = Image.new("L", (S, S), 0)
        ld = ImageDraw.Draw(leaves)
        rng = random.Random(9)
        for _ in range(60 if kind == "tree" else 160):
            x, y = rng.gauss(S / 2, S * 0.2), rng.gauss(S / 2, S * 0.2)
            r = rng.uniform(S * 0.02, S * 0.05)
            ld.ellipse([x - r, y - r, x + r, y + r], fill=255)
        leaves = ImageChops.multiply(leaves, can)
        img = over(img, solid(S, mix(c1, WHITE, 0.15), leaves.point(lambda v: int(v * 0.6))))
        glow_ = pen_mask(S, lambda p: [p.circle(0.35 * math.cos(math.radians(a)), 0.35 * math.sin(math.radians(a)), 0.05) for a in range(0, 360, 72)] + [p.circle(0, 0, 0.08)])
        img = over(img, solid(S, c2, blur(glow_, S * 0.03)))
        img = over(img, solid(S, WHITE, glow_))
    else:
        raise KeyError(kind)
    save(img, path.replace("ArcanumAscendant/", ""))


def make_items_ui():
    # items
    S = 128

    def book(cover, accent, gem, cracked=False):
        m = pen_mask(S, lambda p: p.rect(-0.62, -0.78, 0.62, 0.78))
        img = sprite(m, cover, S)
        pages = pen_mask(S, lambda p: p.rect(0.5, -0.72, 0.62, 0.72))
        img = over(img, solid(S, np.array([235, 225, 200], np.float32), pages))
        trim = pen_mask(S, lambda p: [p.poly([(-0.62, 0.78), (-0.35, 0.78), (-0.62, 0.5)]), p.poly([(0.5, 0.78), (0.25, 0.78), (0.5, 0.5)]),
                                       p.poly([(-0.62, -0.78), (-0.35, -0.78), (-0.62, -0.5)]), p.poly([(0.5, -0.78), (0.25, -0.78), (0.5, -0.5)]),
                                       p.ring(-0.06, 0.0, 0.32, 0.04)])
        img = over(img, solid(S, accent, trim))
        gm = glyph("rune" if not cracked else "skull", S, 0.32, dx=-4)
        img = over(img, solid(S, gem, blur(gm, 4)))
        img = over(img, solid(S, mix(gem, WHITE, 0.5), gm))
        if cracked:
            cr = pen_mask(S, lambda p: p.line([(-0.3, 0.6), (-0.1, 0.2), (-0.25, -0.1), (0.05, -0.5)], 0.04))
            img = over(img, solid(S, np.array([20, 10, 20], np.float32), cr))
        return img

    save(book(np.array([90, 50, 140], np.float32), hexc("#ffd36b"), hexc("#c9a0ff")), "Items/Grimoire")
    save(book(np.array([45, 40, 55], np.float32), np.array([150, 150, 160], np.float32), hexc("#ff5a5a"), cracked=True), "Items/TomeOfUnmaking")

    def potion(liquid, tall=False):
        if tall:
            m = pen_mask(S, lambda p: [p.ellipse(0, -0.35, 0.45, 0.45), p.rect(-0.18, -0.1, 0.18, 0.65), p.rect(-0.24, 0.55, 0.24, 0.72)])
        else:
            m = pen_mask(S, lambda p: [p.circle(0, -0.25, 0.55), p.rect(-0.16, 0.1, 0.16, 0.6), p.rect(-0.22, 0.5, 0.22, 0.68)])
        img = sprite(m, np.array([190, 210, 230], np.float32), S)
        liq = pen_mask(S, lambda p: p.circle(0, -0.3 if not tall else -0.38, 0.45 if not tall else 0.38))
        img = over(img, to_rgba(radial(S, mix(liquid, WHITE, 0.5), liquid, 1.0, 0.5, 0.62), arr(liq) * 255))
        img = over(img, solid(S, liquid, blur(liq, 6).point(lambda v: int(v * 0.5))))
        cork = pen_mask(S, lambda p: p.rect(-0.14, 0.6 if not tall else 0.66, 0.14, 0.82 if not tall else 0.88))
        img = over(img, sprite(cork, np.array([150, 105, 60], np.float32), S, outline=1))
        hl = pen_mask(S, lambda p: p.ellipse(-0.2, -0.05 if not tall else -0.15, 0.08, 0.16))
        img = over(img, solid(S, WHITE, hl.point(lambda v: int(v * 0.7))))
        return img

    def crystal(c):
        shards = [(-0.32, -0.15, 0.2, 0.62, -12), (0.08, -0.05, 0.24, 0.85, 4), (0.4, -0.2, 0.18, 0.55, 16)]
        img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        for cx, cy, w, h, tilt in shards:
            t = math.radians(tilt)

            def rot(x, y):
                return (cx + x * math.cos(t) - y * math.sin(t), cy + x * math.sin(t) + y * math.cos(t))
            body = pen_mask(S, lambda p: p.poly([rot(-w, -0.55), rot(w, -0.55), rot(w, h - 0.75), rot(0, h - 0.5), rot(-w, h - 0.75)]))
            img = over(img, solid(S, c, blur(body, 8).point(lambda v: int(v * 0.6))))
            img = over(img, to_rgba(radial(S, mix(c, WHITE, 0.6), mix(c, BLACK, 0.25), 1.0, 0.5, 0.8), arr(body) * 255))
            facet = pen_mask(S, lambda p: p.poly([rot(-w * 0.15, -0.5), rot(w * 0.7, -0.5), rot(w * 0.7, h - 0.78), rot(0, h - 0.55)]))
            img = over(img, solid(S, mix(c, WHITE, 0.55), facet.point(lambda v: int(v * 0.55))))
        return img

    save(crystal(hexc("#a77bff")), "Items/ArcaneCrystal")

    def sigil(c, n):
        disc = pen_mask(S, lambda p: p.circle(0, 0, 0.82))
        img = sprite(disc, mix(c, BLACK, 0.7), S)
        ring = pen_mask(S, lambda p: [p.ring(0, 0, 0.72, 0.05), p.ring(0, 0, 0.5, 0.03)] +
                        [p.circle(0.61 * math.cos(math.radians(90 + k * 360 / (n + 1))), 0.61 * math.sin(math.radians(90 + k * 360 / (n + 1))), 0.07) for k in range(n + 1)])
        img = over(img, solid(S, c, blur(ring, 4)))
        img = over(img, solid(S, mix(c, WHITE, 0.4), ring))
        pts = [(0.42 * math.cos(math.radians(90 + k * 360 / (n + 1))), 0.42 * math.sin(math.radians(90 + k * 360 / (n + 1)))) for k in range(n + 1)]
        star = pen_mask(S, lambda p: p.line([pts[(k * 2) % len(pts)] for k in range(len(pts) + 1)] if len(pts) % 2 else pts + [pts[0]], 0.03))
        img = over(img, solid(S, mix(c, WHITE, 0.3), star))
        core = pen_mask(S, lambda p: p.circle(0, 0, 0.14))
        img = over(img, solid(S, c, blur(core, 8)))
        img = over(img, solid(S, WHITE, core))
        return img

    for n, col in [(2, "#5ab0ff"), (3, "#4fffc0"), (4, "#ffd35a"), (5, "#ff4fa8")]:
        save(sigil(hexc(col), n), "Items/Sigil%d" % n)

    save(potion(hexc("#3a7bff")), "Items/ManaPotion")
    save(potion(hexc("#9a5aff"), True), "Items/GreaterManaPotion")
    # arcane lectern
    S2 = 256
    lec = pen_mask(S2, lambda p: [p.poly([(-0.7, 0.1), (0.7, 0.1), (0.55, 0.55), (-0.55, 0.55)]), p.rect(-0.12, -0.75, 0.12, 0.1), p.rect(-0.5, -0.85, 0.5, -0.7)])
    img = sprite(lec, np.array([110, 80, 55], np.float32), S2)
    pages = pen_mask(S2, lambda p: [p.poly([(-0.55, 0.18), (-0.04, 0.22), (-0.04, 0.5), (-0.48, 0.46)]), p.poly([(0.55, 0.18), (0.04, 0.22), (0.04, 0.5), (0.48, 0.46)])])
    img = over(img, solid(S2, np.array([240, 232, 210], np.float32), pages))
    runes = pen_mask(S2, lambda p: [p.line([(x, 0.28), (x + 0.08, 0.42)], 0.02) for x in (-0.45, -0.3, -0.15, 0.12, 0.27, 0.4)])
    img = over(img, solid(S2, hexc("#b48cff"), blur(runes, 3)))
    img = over(img, solid(S2, hexc("#e8d8ff"), runes))
    orb = pen_mask(S2, lambda p: p.circle(0, 0.72, 0.1))
    img = over(img, solid(S2, hexc("#b48cff"), blur(orb, 10)))
    img = over(img, solid(S2, WHITE, orb))
    save(img, "Things/ArcaneAltar")
    lt = vfx_radial(64, lambda r, x, y: np.clip(np.exp(-(r / 0.35) ** 2) * 1.3 + 0.3 * np.exp(-(r / 0.9) ** 2), 0, 1))
    save(white(lt, 64), "Things/ArcaneLight")
    # UI
    save(attr_icon("eye", "#c49cff"), "UI/Awaken")
    au = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    am = pen_mask(64, lambda p: [p.arc(0, 0, 0.6, 20, 160, 0.16), p.arc(0, 0, 0.6, 200, 340, 0.16), p.poly([(0.75, 0.05), (0.35, 0.05), (0.55, 0.4)]), p.poly([(-0.75, -0.05), (-0.35, -0.05), (-0.55, -0.4)])])
    au = over(au, solid(64, np.array([10, 30, 20], np.float32), dilate(am, 2)))
    au = over(au, solid(64, hexc("#7dffb0"), am))
    save(au, "UI/Autocast")
    save(attr_icon("book", "#c49cff"), "UI/Grimoire")
    S3 = 128
    fr = pen_mask(S3, lambda p: [p.ring(0, 0, 0.9, 0.08), p.ring(0, 0, 0.78, 0.025)] + [p.circle(0.84 * math.cos(math.radians(a)), 0.84 * math.sin(math.radians(a)), 0.05) for a in range(0, 360, 45)])
    save(white(np.clip(arr(fr), 0, 1), S3), "UI/NodeFrame")
    fk = pen_mask(S3, lambda p: [p.line([(0.9 * math.cos(math.radians(a)), 0.9 * math.sin(math.radians(a))) for a in range(30, 391, 60)], 0.08),
                                 p.line([(0.76 * math.cos(math.radians(a)), 0.76 * math.sin(math.radians(a))) for a in range(30, 391, 60)], 0.025)] +
                 [p.circle(0.95 * math.cos(math.radians(a)), 0.95 * math.sin(math.radians(a)), 0.07) for a in range(30, 391, 60)])
    save(white(arr(fk), S3), "UI/NodeFrameKeystone")
    fs = pen_mask(S3, lambda p: [p.line([(0, 0.92), (0.92, 0), (0, -0.92), (-0.92, 0), (0, 0.92)], 0.08), p.line([(0, 0.78), (0.78, 0), (0, -0.78), (-0.78, 0), (0, 0.78)], 0.025)])
    save(white(arr(fs), S3), "UI/NodeFrameSpell")
    save(white(np.ones((8, 8)), 8), "UI/Line")
    save(to_rgba(np.ones((64, 64, 3)) * np.array([10, 8, 16]), np.ones((64, 64)) * 255), "UI/TreeBackground")
    save(class_icon("skull", "crown", "#b49cff", "#ffffff", False), "UI/ThrallFaction")
    for name, motif, col in [("Potency", "burst", "#ff6b52"), ("Focus", "eye", "#5a9aff"), ("Celerity", "wing", "#66ffa6"), ("Resilience", "shield", "#ffd159"),
                             ("Attunement", "star", "#c773ff")]:
        save(attr_icon(motif, col), "UI/Attr_" + name)


def make_preview(classes):
    W, H = 1280, 720
    bg = np.zeros((H, W, 3), np.float32)
    y, x = np.mgrid[0:H, 0:W].astype(np.float32)
    r = np.sqrt(((x - W / 2) / W) ** 2 + ((y - H / 2) / H) ** 2)
    bg += np.array([40, 22, 70], np.float32) * np.clip(1 - r * 1.6, 0, 1)[..., None]
    bg += np.array([8, 6, 14], np.float32)
    img = to_rgba(bg, np.ones((H, W)) * 255)
    rng = random.Random(4)
    stars = Image.new("L", (W, H), 0)
    sd = ImageDraw.Draw(stars)
    for _ in range(500):
        sx, sy, sr = rng.uniform(0, W), rng.uniform(0, H), rng.uniform(0.4, 1.6)
        sd.ellipse([sx - sr, sy - sr, sx + sr, sy + sr], fill=rng.randint(80, 255))
    img = over(img, solid_wh(W, H, WHITE, stars))
    rc = rune_circle(640, 0, 77).resize((640, 640), Image.LANCZOS)
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    a = rc.split()[3].point(lambda v: int(v * 0.35))
    tint = Image.new("RGBA", (640, 640), (190, 150, 255, 0))
    tint.putalpha(a)
    layer.paste(tint, ((W - 640) // 2, (H - 640) // 2), tint)
    img = over(img, layer)
    n = len(classes)
    for i, (icon_path) in enumerate(classes):
        ic = Image.open(icon_path).convert("RGBA").resize((128, 128), Image.LANCZOS)
        ang = math.radians(-90 + i * 360 / n)
        cx, cy = W / 2 + 470 * math.cos(ang) * 1.0, H / 2 + 250 * math.sin(ang)
        img.paste(ic, (int(cx - 64), int(cy - 64)), ic)
    font_big = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf", 96)
    font_small = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf", 30)
    txt = Image.new("L", (W, H), 0)
    td = ImageDraw.Draw(txt)
    title = "ARCANUM"
    title2 = "ASCENDANT"
    for t, yy, f in [(title, H / 2 - 130, font_big), (title2, H / 2 - 30, font_big)]:
        w = td.textlength(t, font=f)
        td.text(((W - w) / 2, yy), t, font=f, fill=255)
    sub = "9 classes  ·  27 subclasses  ·  207 spells  ·  658 skill nodes"
    w = td.textlength(sub, font=font_small)
    td.text(((W - w) / 2, H - 62), sub, font=font_small, fill=255)
    img = over(img, solid_wh(W, H, np.array([170, 110, 255], np.float32), blur(txt, 12)))
    img = over(img, solid_wh(W, H, np.array([40, 20, 70], np.float32), dilate(txt, 3)))
    grad = np.zeros((H, W, 3), np.float32)
    grad[:] = mix(np.array([255, 236, 190], np.float32), np.array([255, 190, 120], np.float32), (y / H)[..., None])
    img = over(img, to_rgba(grad, arr(txt) * 255))
    img = img.convert("RGB")
    img.save(os.path.join(ROOT, "About", "Preview.png"), optimize=True)
    icon = Image.open(classes[0]).convert("RGBA")
    mod_icon = class_icon("rune", "star", "#9a6cff", "#ffd36b", False)
    mod_icon.save(os.path.join(ROOT, "About", "ModIcon.png"), optimize=True)


def solid_wh(w, h, color, mask):
    rgb = np.ones((h, w, 3), np.float32) * color
    return to_rgba(rgb, arr(mask) * 255)


def main():
    manifest = json.load(open(os.path.join(ROOT, "Tools", "gen", "out", "icons.json")))
    print("Rendering VFX...")
    make_vfx()
    print("Rendering items and UI...")
    make_items_ui()
    print("Rendering effect textures...")
    import make_vfx2
    make_vfx2.make_all()
    print("Rendering %d icons/things..." % len(manifest))
    class_paths = []
    for i, e in enumerate(manifest):
        k = e["kind"]
        rel = e["path"].replace("ArcanumAscendant/", "")
        if k == "spell":
            save(spell_icon(e["motif"], e.get("motif2"), e["c1"], e["c2"], e["frame"] == "ultimate"), rel)
        elif k == "node":
            save(node_icon(e["motif"], e["c1"], e["c2"]), rel)
        elif k == "class":
            save(class_icon(e["motif"], e.get("motif2"), e["c1"], e["c2"], e["frame"] == "subclass"), rel)
            if e["frame"] == "class":
                class_paths.append(os.path.join(TEX, rel + ".png"))
        elif k == "thing":
            make_thing(e["path"], e["motif"], e["c1"], e["c2"])
    print("Rendering preview...")
    make_preview(class_paths)
    print("Done.")


if __name__ == "__main__":
    main()

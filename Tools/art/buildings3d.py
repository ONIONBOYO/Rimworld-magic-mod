"""
3D-modelled building sprites (see render3d.py). Directional buildings get _north/_east/_south/_west views for Graphic_Multi.

Run on its own with:  python3 Tools/art/buildings3d.py
"""
import math
import os
import sys

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import render3d as r3  # noqa: E402
from render3d import Model, flat, wood, stone, metal, decal  # noqa: E402

TEX = os.path.join(os.path.abspath(os.path.join(HERE, "..", "..")), "Textures", "ArcanumAscendant")


def save(img, rel):
    path = os.path.join(TEX, rel + ".png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, optimize=True)


WOOD = (132, 88, 56)
WOOD_DARK = (96, 62, 42)
STONE = (118, 112, 130)
STONE_DARK = (92, 88, 104)
GOLD = (222, 178, 86)
LEATHER = (82, 44, 104)
PAPER = (240, 231, 206)
RUNE = (186, 140, 255)
RIBBON = (190, 50, 70)


def _hash2(a, b):
    return (np.sin(a * 12.9898 + b * 78.233) * 43758.5453) % 1.0


def rune_text(u0, u1, v0, v1, row=0.042, col=0.026):
    """Rows of small glowing glyph strokes, like lines of arcane script on a page."""
    def fn(u, v):
        inside = (u > u0) & (u < u1) & (v > v0) & (v < v1)
        ru = (u - u0) / col
        rv = (v - v0) / row
        iu = np.floor(ru)
        iv = np.floor(rv)
        fu = ru - iu
        fv = rv - iv
        h = _hash2(iu, iv)
        vert = (np.abs(fu - 0.5) < 0.13) & (np.abs(fv - 0.5) < 0.32)
        diag = (np.abs((fu - 0.5) - (fv - 0.5) * 0.7) < 0.12) & (np.abs(fv - 0.5) < 0.3)
        bar = (np.abs(fv - 0.32) < 0.09) & (np.abs(fu - 0.5) < 0.38)
        shape = np.where(h < 0.35, vert, np.where(h < 0.62, diag, np.where(h < 0.8, bar | vert, False)))
        gap = _hash2(iv, 3.0) * 0.35 + 0.55          # ragged line ends
        line_ok = ru < (u1 - u0) / col * gap + 2
        return (inside & shape & line_ok).astype(np.float32), RUNE
    return fn


def border(w, length, width, col):
    def fn(u, v):
        m = (u < w) | (u > width - w) | (v < w) | (v > length - w)
        return m.astype(np.float32), col
    return fn


def lectern():
    m = Model()
    # stone plinth with a stepped top
    m.box(-0.30, 0.30, 0.0, 0.06, -0.24, 0.24, stone(STONE))
    def plinth_runes(u, v):
        x, z = u - 0.22, v - 0.17
        r = np.sqrt(x * x + z * z)
        ring = np.abs(r - 0.135) < 0.008
        a = np.arctan2(z, x)
        ticks = (np.abs(r - 0.112) < 0.014) & (np.abs(((a / (2 * np.pi) * 12) % 1.0) - 0.5) < 0.12)
        return (ring | ticks).astype(np.float32), RUNE
    m.box(-0.22, 0.22, 0.06, 0.10, -0.17, 0.17, {"top": decal(stone(STONE_DARK, seed=24), plinth_runes), "bottom": stone(STONE_DARK),
          "south": stone(STONE_DARK, seed=25), "north": stone(STONE_DARK), "west": stone(STONE_DARK), "east": stone(STONE_DARK)},
          uv={"top": (np.array([-0.22, 0.10, -0.17], np.float32), np.array([1, 0, 0], np.float32), np.array([0, 0, 1], np.float32))})
    # carved column with two gold bands
    m.prism(0.0, 0.01, 0.075, 0.10, 0.52, 8, wood(WOOD_DARK, axis="y"), rot=math.pi / 8)
    m.prism(0.0, 0.01, 0.087, 0.28, 0.315, 8, metal(GOLD), rot=math.pi / 8)
    m.prism(0.0, 0.01, 0.087, 0.47, 0.505, 8, metal(GOLD), rot=math.pi / 8)
    # bracket under the desk
    m.box(-0.13, 0.13, 0.50, 0.575, -0.10, 0.12, wood(WOOD_DARK, axis="x", seed=13))

    # slanted reading desk: low edge toward the reader (-z)
    xL, xR = -0.36, 0.36
    zF, zB = -0.21, 0.15
    yF, yB = 0.56, 0.76
    th = 0.05
    tFL = np.array([xL, yF, zF], np.float32)
    tFR = np.array([xR, yF, zF], np.float32)
    tBR = np.array([xR, yB, zB], np.float32)
    tBL = np.array([xL, yB, zB], np.float32)
    down = np.array([0, th, 0], np.float32)
    slope = r3._n(tBL - tFL)
    length = float(np.linalg.norm(tBL - tFL))
    width = xR - xL
    top_mat = decal(wood(WOOD, axis="x", seed=17), border(0.028, length, width, GOLD))
    m.hexahedron([tFL - down, tFR - down, tBR - down, tBL - down, tFL, tFR, tBR, tBL],
                 {"top": top_mat, "bottom": wood(WOOD_DARK), "south": metal(GOLD), "north": wood(WOOD_DARK, seed=19),
                  "west": wood(WOOD_DARK, axis="z", seed=23), "east": wood(WOOD_DARK, axis="z", seed=29)},
                 uv={"top": (tFL, np.array([1, 0, 0], np.float32), slope)})
    # lip that holds the book
    m.box(xL + 0.02, xR - 0.02, yF - 0.005, yF + 0.04, zF - 0.025, zF + 0.005, metal(GOLD))

    # open book: leather cover, then two slightly domed pages covered in glowing script
    nrm = r3._n(np.cross(slope, np.array([1, 0, 0], np.float32)))
    if nrm[1] < 0:
        nrm = -nrm

    def on_desk(u, v, lift):
        return tFL + np.array([1, 0, 0], np.float32) * u + slope * v + nrm * lift

    def slab(u0, u1, v0, v1, lift0, lift1, mats, uvtop=None, emissive=False):
        b = [on_desk(u0, v0, lift0), on_desk(u1, v0, lift0), on_desk(u1, v1, lift0), on_desk(u0, v1, lift0)]
        t = [on_desk(u0, v0, lift1), on_desk(u1, v0, lift1), on_desk(u1, v1, lift1), on_desk(u0, v1, lift1)]
        faces = m.hexahedron(b + t, mats, uv={"top": uvtop} if uvtop else None)
        if emissive:
            for f in faces:
                if f.mat is mats.get("top"):
                    f.emissive = True
        return faces

    cover = flat(LEATHER, 0.08, seed=41)
    slab(0.06, width - 0.06, 0.05, length - 0.06, 0.0, 0.014, {k: cover for k in ("top", "bottom", "south", "north", "west", "east")})
    page_side = flat((214, 204, 178), 0.04)
    for (u0, u1) in ((0.085, width / 2 - 0.012), (width / 2 + 0.012, width - 0.085)):
        o = on_desk(u0, 0.075, 0.03)
        pw = u1 - u0
        top = decal(flat(PAPER, 0.05, seed=43), rune_text(0.04, pw - 0.04, 0.05, length - 0.17))
        slab(u0, u1, 0.075, length - 0.085, 0.014, 0.03,
             {"top": top, "bottom": page_side, "south": page_side, "north": page_side, "west": page_side, "east": page_side},
             uvtop=(o, np.array([1, 0, 0], np.float32), slope), emissive=True)
    # silk bookmark draped over the lip
    m.box(0.04, 0.075, yF - 0.10, yF + 0.035, zF - 0.032, zF - 0.026, flat(RIBBON, 0.05))
    # floating rune orb above the desk
    m.discs.append((np.array([0.0, 0.92, 0.10], np.float32), 0.058, (176, 128, 255), (252, 246, 255)))
    return m


def ring_basin(m, r_out, r_in, y0, y1, sides, mat_top, mat_side):
    """A round stone rim: outer wall plus a flat ring on top, open in the middle."""
    ang = [2 * math.pi * (i + 0.5) / sides for i in range(sides)]
    for i in range(sides):
        a0, a1 = ang[i], ang[(i + 1) % sides]
        o0 = np.array([r_out * math.cos(a0), y1, r_out * math.sin(a0)], np.float32)
        o1 = np.array([r_out * math.cos(a1), y1, r_out * math.sin(a1)], np.float32)
        i0 = np.array([r_in * math.cos(a0), y1, r_in * math.sin(a0)], np.float32)
        i1 = np.array([r_in * math.cos(a1), y1, r_in * math.sin(a1)], np.float32)
        m.add(r3.Face([o0, o1, i1, i0], mat_top))
        b0 = o0.copy()
        b1 = o1.copy()
        b0[1] = b1[1] = y0
        m.add(r3.Face([b0, b1, o1, o0], mat_side))
        # inner wall, seen from above on the far side of the pool
        j0 = i0.copy()
        j1 = i1.copy()
        j0[1] = j1[1] = y1 - 0.08
        m.add(r3.Face([i0, i1, j1, j0], mat_side))


def crystal(m, cx, cz, r, y0, h, tilt_x, tilt_z, col, seed):
    """A six-sided crystal: a short prism capped with a pyramid, leaning a little."""
    sides = 6
    rot = seed * 0.7
    base = [np.array([cx + r * math.cos(rot + 2 * math.pi * k / sides), y0, cz + r * math.sin(rot + 2 * math.pi * k / sides)], np.float32) for k in range(sides)]
    lean = np.array([tilt_x, 0, tilt_z], np.float32)
    shoulder = [b + np.array([0, h * 0.72, 0], np.float32) + lean * 0.72 for b in base]
    tip = np.array([cx, y0 + h, cz], np.float32) + lean
    body = r3.flat(col, 0.05, seed=seed)
    facet = r3.flat(tuple(min(255, c * 1.25) for c in col), 0.04, seed=seed + 1)
    for k in range(sides):
        j = (k + 1) % sides
        m.add(r3.Face([base[k], base[j], shoulder[j], shoulder[k]], body if k % 2 else facet))
        m.add(r3.Face([shoulder[k], shoulder[j], tip], facet if k % 2 else body))


def mana_font():
    m = Model()
    m.prism(0, 0, 0.98, 0.0, 0.08, 12, {"top": stone(STONE_DARK, seed=51), "side": stone(STONE_DARK, seed=52)}, rot=math.pi / 12)
    ring_basin(m, 0.86, 0.66, 0.08, 0.34, 12, stone(STONE, seed=53), stone(STONE_DARK, seed=54))

    def pool(p, u, v, n):
        r = np.sqrt(p[..., 0] ** 2 + p[..., 2] ** 2)
        ripple = 0.5 + 0.5 * np.sin(r * 38.0 - r3._noise3(p, 3.0, 61) * 4)
        deep = np.array([46, 92, 214], np.float32)
        light = np.array([170, 214, 255], np.float32)
        t = np.clip(0.25 + 0.55 * ripple * (1 - r / 0.7), 0, 1)[..., None]
        return deep * (1 - t) + light * t
    m.prism(0, 0, 0.67, 0.08, 0.27, 12, {"top": pool, "side": stone(STONE_DARK)}, rot=math.pi / 12)
    for f in m.faces[-13:]:
        if f.mat is pool:
            f.emissive = True
    cols = [(126, 176, 255), (150, 196, 255), (108, 156, 240)]
    spots = [(0.0, 0.05, 0.13, 0.78, 0.0, 0.0), (-0.2, -0.05, 0.09, 0.5, -0.12, -0.02), (0.21, -0.02, 0.1, 0.56, 0.12, 0.0),
             (-0.06, 0.24, 0.08, 0.44, -0.04, 0.1), (0.1, 0.22, 0.07, 0.36, 0.08, 0.08), (0.05, -0.22, 0.07, 0.34, 0.02, -0.1)]
    for i, (cx, cz, r, h, tx, tz) in enumerate(sorted(spots, key=lambda s: -s[1])):
        crystal(m, cx, cz, r, 0.2, h, tx, tz, cols[i % 3], 70 + i)
    return m


def ward_pylon():
    m = Model()
    m.prism(0, 0, 0.36, 0.0, 0.07, 8, {"top": stone(STONE_DARK, seed=81), "side": stone(STONE_DARK, seed=82)}, rot=math.pi / 8)
    m.prism(0, 0, 0.27, 0.07, 0.14, 8, {"top": stone(STONE, seed=83), "side": stone(STONE, seed=84)}, rot=math.pi / 8)

    def obelisk(p, u, v, n):
        base = np.array([150, 108, 236], np.float32)
        y = p[..., 1]
        shade = 0.9 + 0.15 * r3._noise3(p, 10.0, 85)
        rune = (np.abs(((y - 0.2) / 0.11) % 1.0 - 0.5) < 0.09) & (y > 0.22) & (y < 0.9)
        c = base * shade[..., None]
        c[rune] = np.array([236, 220, 255], np.float32)
        return c
    m.prism(0, 0, 0.13, 0.14, 0.96, 4, {"top": obelisk, "side": obelisk}, rot=math.pi / 4, r_top=0.085)
    tipb = [np.array([0.085 * math.cos(math.pi / 4 + k * math.pi / 2), 0.96, 0.085 * math.sin(math.pi / 4 + k * math.pi / 2)], np.float32) for k in range(4)]
    tip = np.array([0, 1.06, 0], np.float32)
    for k in range(4):
        m.add(r3.Face([tipb[k], tipb[(k + 1) % 4], tip], r3.flat((176, 136, 250), 0.04, seed=86)))
    m.discs.append((np.array([0.0, 1.2, 0.0], np.float32), 0.06, (190, 150, 255), (255, 248, 255)))
    return m


def make_all():
    r3.save_views(lectern, "Things/ArcaneAltar", save, size=256, extent=1.6)
    save(r3.render(mana_font(), size=256, extent=2.2), "Things/ManaFont")
    save(r3.render(ward_pylon(), size=256, extent=1.6), "Things/WardPylon")


if __name__ == "__main__":
    make_all()
    print("3D building sprites done.")

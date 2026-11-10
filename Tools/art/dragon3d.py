"""
The wyrm, modelled and rendered rather than painted.

One model, three life stages (proportions change: a hatchling is mostly head), three facings, four breeds. The geometry is marched
once per stage and facing and then painted once per breed, so the breeds cost almost nothing extra.

    python3 Tools/art/dragon3d.py                 # everything
    python3 Tools/art/dragon3d.py Drake south     # one sheet (all breeds), for looking at
"""
import math
import os
import sys

import numpy as np
from PIL import Image

import sdf3d as S
from sdf3d import F, v3, unit

HERE = os.path.dirname(os.path.abspath(__file__))
TEX = os.path.normpath(os.path.join(HERE, "..", "..", "Textures", "ArcanumAscendant"))

STAGES = {
    "Hatchling": dict(snout=0.62, bulk=1.14, torso=0.70, neck=0.55, head=1.40, tail=0.62, leg=0.70, wing=0.56, horn=0.32, spine=0.45, eye=1.55,
                      extent=1.45, px=256),
    "Drake": dict(bulk=1.0, torso=1.0, neck=1.0, head=1.12, tail=1.0, leg=1.0, wing=0.94, horn=1.15, spine=1.15, eye=1.0,
                  extent=2.1, px=384),
    "Elder": dict(snout=1.05, bulk=1.34, torso=1.1, neck=1.22, head=1.16, tail=1.2, leg=1.06, wing=1.2, horn=1.6, spine=1.55, eye=0.85,
                  crown=True, extent=2.72, px=512),
    # the brood's drakelings: all leg and tail, wings not yet worth the name
    "Drakeling": dict(snout=0.8, bulk=0.92, torso=0.82, neck=0.7, head=1.2, tail=0.9, leg=0.9, wing=0.42, horn=0.5, spine=0.8, eye=1.3,
                      extent=1.5, px=256),
}

PITCH = {"south": 66.0, "north": 66.0, "east": 30.0}


def build(st, pose):
    B, TL, NL, HS, TS, LS, WS, HN, SP, ES = (st[k] for k in ("bulk", "torso", "neck", "head", "tail", "leg", "wing", "horn", "spine", "eye"))
    sc = S.Scene()
    body = sc.group("body", k=0.075 * B)
    belly = sc.group("belly", k=0.05, join=0.05)
    arm = sc.group("arm", k=0.02, join=0.03)
    bone = sc.group("bone", k=0.012, join=0.016)
    memb = sc.group("membrane", k=0.004, join=0.012)
    eye = sc.group("eye", 0.0, 0.004)

    yh = 0.52 * LS
    hip = v3(0, yh, -0.45 * TL)
    mid = v3(0, yh + 0.04, 0)
    chest = v3(0, yh + 0.08, 0.38 * TL)

    def P(base, off, scale=1.0):
        return base + v3(off) * scale

    # ---- the spine, head end to tail tip: (point, radius)
    neck = [(P(chest, (0, 0.60, 0.60), NL), 0.135 * B), (P(chest, (0, 0.43, 0.45), NL), 0.16 * B), (P(chest, (0, 0.20, 0.26), NL), 0.215 * B)]
    torso = [(chest, 0.34 * B), (mid, 0.33 * B), (hip, 0.29 * B)]
    tail_off = [((0, -0.03, -0.16), 0.245), ((0.02, -0.12, -0.45), 0.18), ((0.12, -0.21, -0.74), 0.13), ((0.34, -0.29, -0.96), 0.09),
                ((0.62, -0.345, -1.0), 0.06), ((0.85, -0.375, -0.85), 0.038), ((0.93, -0.385, -0.62), 0.022)]
    tail = [(P(hip, o, TS), r * B) for o, r in tail_off]
    spine = neck + torso + tail
    body.chain([tuple(p) + (r,) for p, r in spine])
    # chest and haunches
    body.add(S.Ellipsoid(P(chest, (0, -0.03, -0.02)), v3(0.34, 0.31, 0.32) * B))

    # belly: the same chain, lower and thinner, so it shows only underneath
    under = spine[0:-3]
    belly.chain([(p[0], p[1] - 0.22 * r, p[2], 0.94 * r) for p, r in under])

    # ---- head
    hd = P(chest, (0, 0.64, 0.74), NL)

    SN = st.get("snout", 1.0)

    def H(x, y, z):
        if z > 0.08:
            z = 0.08 + (z - 0.08) * SN
        return hd + v3(x, y, z) * HS

    skull = [S.Ellipsoid(H(0, 0, 0), v3(0.15, 0.125, 0.17) * HS),
             S.Cone(H(0, -0.01, 0.08), H(0, -0.065, 0.40), 0.108 * HS, 0.06 * HS),          # snout
             S.Sphere(H(0, -0.05, 0.435), 0.056 * HS),                                       # nose
             S.Cone(H(0, -0.10, -0.02), H(0, -0.155, 0.37), 0.074 * HS, 0.04 * HS)]          # jaw
    # the mouth: a thin wedge cut back from the lips to the hinge of the jaw
    slope = math.atan2(-0.075, 0.42)
    mouth = S.Box(H(0, -0.112, 0.27), v3(0.3, 0.007, 0.21 * SN + 0.02) * HS, S.rot_x(-slope))
    body.add(S.Cut(S.Blend(skull, 0.05 * HS), mouth))
    belly.add(S.Cut(S.Cone(H(0, -0.135, 0.0), H(0, -0.175, 0.34), 0.06 * HS, 0.032 * HS), mouth))
    for s in (-1, 1):                                                                        # fangs
        for z, ln in ((0.40, 0.045), (0.33, 0.03), (0.26, 0.025)):
            w = 0.052 + (0.40 - z) * 0.14
            bone.cone(H(s * w, -0.085 - (z - 0.27) * -0.18 - 0.02, z), H(s * w, -0.085 - (z - 0.27) * -0.18 - 0.02 - ln, z - 0.008), 0.011 * HS, 0.002)
    for s in (-1, 1):
        body.add(S.Sphere(H(s * 0.088, 0.085, 0.075), 0.046 * HS))                      # brow
        body.add(S.Sphere(H(s * 0.036, -0.03, 0.40), 0.021 * HS))                       # nostril
        body.add(S.Sphere(H(s * 0.11, -0.04, -0.03), 0.07 * HS))                        # jaw muscle
        eye.add(S.Sphere(H(s * 0.108, 0.04, 0.115), 0.034 * HS * ES))
        hb = H(s * 0.085, 0.09, -0.09)
        horn = [(hb, 0.046), (hb + v3(s * 0.06, 0.11, -0.15) * HS * HN, 0.036), (hb + v3(s * 0.10, 0.19, -0.33) * HS * HN, 0.022),
                (hb + v3(s * 0.11, 0.22, -0.50) * HS * HN, 0.005)]
        bone.chain([tuple(p) + (r * HS,) for p, r in horn])
        cb = H(s * 0.13, -0.03, -0.09)
        bone.cone(cb, cb + v3(s * 0.11, -0.01, -0.20) * HS * HN, 0.03 * HS, 0.005)
        if st.get("crown"):
            c2 = H(s * 0.125, 0.035, -0.1)
            bone.cone(c2, c2 + v3(s * 0.15, 0.07, -0.27) * HS * HN, 0.034 * HS, 0.004)
            c3 = H(s * 0.05, -0.16, 0.1)
            bone.cone(c3, c3 + v3(s * 0.02, -0.12, -0.08) * HS, 0.02 * HS, 0.003)
    if st.get("crown"):
        bone.cone(H(0, 0.05, 0.36), H(0, 0.13, 0.31), 0.022 * HS, 0.004)                # nose horn

    # ---- legs
    feet = []
    for s in (-1, 1):
        sh = v3(s * 0.25, yh, 0.34 * TL)
        el = sh + v3(s * 0.15, -0.20, -0.10) * LS
        wr = el + v3(0, -0.20, 0.18) * LS
        paw = v3(wr[0] + s * 0.01, 0.055, wr[2] + 0.09)
        body.chain([tuple(sh) + (0.135 * B,), tuple(el) + (0.095 * B,), tuple(wr) + (0.068 * B,)])
        body.cone(wr, paw, 0.065 * B, 0.05 * B)
        body.add(S.Ellipsoid(paw, v3(0.085, 0.05, 0.115) * B))
        feet.append(paw)
        hp = v3(s * 0.26, yh, -0.42 * TL)
        kn = hp + v3(s * 0.16, -0.15, 0.22) * LS
        hk = kn + v3(-s * 0.02, -0.17, -0.30) * LS
        ft = v3(hk[0] + s * 0.02, 0.055, hk[2] + 0.14 * LS)
        body.add(S.Ellipsoid(hp + v3(s * 0.03, -0.02, 0.02), v3(0.15, 0.21, 0.23) * B))
        body.chain([tuple(hp) + (0.17 * B,), tuple(kn) + (0.115 * B,), tuple(hk) + (0.07 * B,)])
        body.cone(hk, ft, 0.065 * B, 0.05 * B)
        body.add(S.Ellipsoid(ft, v3(0.09, 0.05, 0.13) * B))
        feet.append(ft)
    for ft in feet:
        for dx in (-0.055, 0.0, 0.055):
            a = ft + v3(dx, -0.015, 0.09) * B
            bone.cone(a, a + v3(dx * 0.4, -0.03, 0.10) * B, 0.025 * B, 0.004)

    # ---- wings
    for s in (-1, 1):
        if pose == "raised":
            # seen from the side: both wings up, the near one (the camera is on the -x side) steeper so that it shows its face
            theta, psi = math.radians(80 if s < 0 else 54), math.radians(14)
        else:
            theta, psi = math.radians(16), 0.0
        root = v3(s * 0.15, chest[1] + 0.21 * B, 0.28 * TL)
        R = S.rot_y(-s * psi) @ S.rot_z(s * theta)

        def W(lx, ly, lz):
            return root + R @ (v3(s * lx, ly, lz) * WS)

        el, wr = W(0.42, 0.10, -0.12), W(0.82, 0.08, 0.30)
        f1, f2, f3, f4 = W(1.75, -0.05, 0.44), W(1.80, -0.10, -0.22), W(1.46, -0.13, -0.80), W(0.95, -0.11, -1.14)
        th = W(0.90, 0.09, 0.52)
        att = v3(s * 0.2, yh + 0.14, -0.42 * TL)
        ws = WS ** 0.5
        arm.cone(root, el, 0.062 * ws, 0.05 * ws)
        arm.cone(el, wr, 0.05 * ws, 0.04 * ws)
        for tip, r0 in ((f1, 0.034), (f2, 0.03), (f3, 0.028), (f4, 0.026)):
            arm.cone(wr, tip, r0 * ws, 0.009)
        bone.cone(wr, th, 0.028 * ws, 0.004)

        def scallop(a, b, depth=0.42):
            m = (a + b) * 0.5
            out = unit(m - wr)
            r = float(np.linalg.norm(a - b)) * depth
            return (m + out * r * 0.62, r)

        t = 0.009
        memb.add(S.Tri(wr, f1, f2, t, [scallop(f1, f2)]), S.Tri(wr, f2, f3, t, [scallop(f2, f3)]), S.Tri(wr, f3, f4, t, [scallop(f3, f4)]),
                 S.Tri(wr, f4, att, t, [scallop(f4, att, 0.34)]), S.Tri(el, wr, att, t), S.Tri(root, el, att, t))

    # ---- tail fin
    tdir = unit(tail[-1][0] - tail[-2][0])
    memb.add(S.Ellipsoid(tail[-1][0] + tdir * 0.11 * TS, v3(0.09, 0.02, 0.15) * TS, S.basis_from(tdir)))

    # ---- dorsal spines, evenly spaced along the back
    pts = np.array([p for p, _ in spine[:-1]], F)
    rad = np.array([r for _, r in spine[:-1]], F)
    seg = np.linalg.norm(pts[1:] - pts[:-1], axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)])
    total = float(cum[-1])
    step = 0.135
    n = int(total / step)
    for i in range(n):
        d = (i + 0.5) * step
        j = min(len(seg) - 1, int(np.searchsorted(cum, d) - 1))
        f = (d - cum[j]) / max(seg[j], 1e-6)
        p = pts[j] * (1 - f) + pts[j + 1] * f
        r = rad[j] * (1 - f) + rad[j + 1] * f
        back = unit(pts[j + 1] - pts[j])
        u = d / total
        h = SP * (0.07 + 0.075 * math.exp(-((u - 0.3) / 0.22) ** 2)) * (1.0 if i % 2 == 0 else 0.68) * (1.0 - 0.55 * max(0.0, u - 0.55) / 0.45)
        top = p + v3(0, r * 0.9, 0)
        bone.cone(top - v3(0, 0.03, 0), top + v3(0, h, 0) + back * h * 0.7, max(0.012, h * 0.34), 0.004)
    return sc, dict(yh=yh, chest=chest)


# ------------------------------------------------------------------------------------------------ painting

def rgb(*c):
    return np.array(c, F) / 255.0


BREEDS = {
    # the wyrm every Wyrmcaller starts with: jade scales, a pale gold belly, wings that glow like embers against the light
    "Jade": dict(back=rgb(22, 122, 88), flank=rgb(70, 205, 150), belly=rgb(244, 222, 150), bone=rgb(246, 238, 212),
                 memb=rgb(30, 128, 112), memb_far=rgb(96, 210, 150), glow=rgb(255, 200, 96), eye=rgb(255, 214, 70),
                 rim=rgb(190, 255, 225), spec=0.38, outline=rgb(8, 34, 28)),
    # Soulbound: not altogether here
    "Spirit": dict(back=rgb(120, 170, 255), flank=rgb(190, 230, 255), belly=rgb(235, 250, 255), bone=rgb(255, 255, 255),
                   memb=rgb(90, 140, 255), memb_far=rgb(170, 220, 255), glow=rgb(200, 240, 255), eye=rgb(255, 255, 255),
                   rim=rgb(210, 240, 255), spec=0.3, outline=rgb(60, 110, 230), ghost=True),
    # Hoardkeeper: looks minted
    "Gold": dict(back=rgb(214, 150, 30), flank=rgb(255, 214, 90), belly=rgb(255, 240, 180), bone=rgb(255, 250, 235),
                 memb=rgb(150, 30, 36), memb_far=rgb(226, 80, 60), glow=rgb(255, 170, 80), eye=rgb(120, 255, 190),
                 rim=rgb(255, 250, 200), spec=0.95, outline=rgb(60, 30, 4), metal=True),
    # Broodmother: old blood and black stripes
    "Brood": dict(back=rgb(122, 20, 30), flank=rgb(205, 60, 52), belly=rgb(238, 190, 150), bone=rgb(232, 222, 200),
                  memb=rgb(52, 18, 40), memb_far=rgb(150, 40, 60), glow=rgb(255, 120, 70), eye=rgb(255, 170, 40),
                  rim=rgb(255, 190, 170), spec=0.3, outline=rgb(26, 6, 10), stripes=rgb(26, 10, 16)),
    # the brood itself
    "Drakeling": dict(back=rgb(160, 44, 30), flank=rgb(236, 110, 60), belly=rgb(250, 214, 150), bone=rgb(240, 230, 205),
                      memb=rgb(96, 28, 40), memb_far=rgb(210, 80, 60), glow=rgb(255, 150, 80), eye=rgb(255, 220, 80),
                      rim=rgb(255, 210, 170), spec=0.3, outline=rgb(34, 8, 8), stripes=rgb(60, 14, 16)),
}


def lerp(a, b, t):
    return a + (b - a) * t[..., None]


def paint(buf, pal, ss, scale_freq=24.0):
    """Turn render buffers into an RGBA float image (0..1) at 1/ss the size."""
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
    gi = {nm: i for i, nm in enumerate(names)}
    gd[gi["eye"]] -= 0.012
    gd[gi["bone"]] -= 0.004
    gd[gi["belly"]] -= 0.02 * buf["extent"] / 2.1
    mat = np.argmin(gd, axis=0)
    is_body, is_belly, is_bone, is_memb, is_eye, is_arm = (mat == gi[k] for k in ("body", "belly", "bone", "membrane", "eye", "arm"))

    V = -f
    L = unit(-0.55 * r + 0.75 * u - 0.5 * f)
    Hh = unit(L + V)
    # a sheet has two sides: light the one we can see
    flip = np.where(is_memb & (N @ V < 0), -1.0, 1.0).astype(F)
    Nn = N * flip[:, None]
    ndl = Nn @ L
    ndv = np.clip(Nn @ V, 0, 1)
    ndh = np.clip(Nn @ Hh, 0, 1)
    up = Nn[:, 1]

    f1, f2, cell = S.worley(P, scale_freq)
    edge = S.smoothstep(0.03, 0.26, f2 - f1)
    dome = 1.0 - S.smoothstep(0.0, 0.75, f1)            # each scale is a little dome: brighter in the middle
    nz = S.value_noise(P, 5.0)

    top = S.smoothstep(-0.15, 0.75, up)
    body_c = lerp(pal["flank"], pal["back"], top)
    body_c = body_c * (0.80 + 0.20 * edge)[:, None] * (0.94 + 0.12 * cell)[:, None] * (0.95 + 0.10 * dome)[:, None]
    if "stripes" in pal:
        band = np.sin((P[:, 2] + 0.35 * P[:, 0]) * 13.0 + 1.6 * np.sin(P[:, 0] * 5.0) + 2.4 * nz)
        stripe = S.smoothstep(0.3, 0.55, band) * S.smoothstep(-0.3, 0.35, up)
        body_c = lerp(body_c, pal["stripes"][None] * (0.8 + 0.2 * edge)[:, None], stripe * 0.85)

    p1, p2, _ = S.worley(P * np.array([1.0, 0.6, 1.9], F), 11.0)
    plate = S.smoothstep(0.02, 0.2, p2 - p1)
    belly_c = pal["belly"][None] * (0.78 + 0.22 * plate)[:, None]

    bone_c = pal["bone"][None] * (0.88 + 0.12 * nz)[:, None]

    d_arm = gd[gi["arm"]]
    vein = S.smoothstep(0.0, 0.3, d_arm)
    memb_c = lerp(pal["memb"], pal["memb_far"], vein * (0.75 + 0.25 * nz))
    memb_c = memb_c * (0.93 + 0.14 * S.value_noise(P, 16.0))[:, None]

    alb = np.zeros_like(P)
    alb[is_body] = body_c[is_body]
    alb[is_belly] = belly_c[is_belly]
    alb[is_bone] = bone_c[is_bone]
    alb[is_memb] = memb_c[is_memb]
    alb[is_arm] = (pal["back"][None] * (0.8 + 0.2 * nz)[:, None])[is_arm]

    diff = np.clip(ndl, 0, 1)
    toon = 0.55 * diff + 0.45 * (0.5 * S.smoothstep(0.06, 0.16, diff) + 0.5 * S.smoothstep(0.48, 0.6, diff))
    toon = np.where(is_memb, 0.30 + 0.70 * (0.5 + 0.5 * ndl), toon)      # skin lets the light through: never fully dark
    shade = 0.3 + 0.7 * sh
    shade = np.where(is_memb, 0.55 + 0.45 * sh, shade)
    amb = (0.40 + 0.16 * up) * (0.30 + 0.70 * ao)
    key_col = np.array([1.0, 0.97, 0.9], F)
    amb_col = np.array([0.82, 0.9, 1.0], F)
    col = alb * (amb[:, None] * amb_col + (0.92 * toon * shade)[:, None] * key_col)

    # wings against the light glow from inside
    back_lit = np.clip(-(N * flip[:, None]) @ L, 0, 1)
    col[is_memb] += (pal["glow"][None] * (0.10 + 0.34 * back_lit[:, None]) * vein[:, None] * (0.4 + 0.6 * ao[:, None]))[is_memb]

    spec = pal["spec"] * (ndh ** 26) * shade * (0.45 + 0.55 * edge)
    spec = np.where(is_memb, spec * 0.35, np.where(is_bone, spec * 0.8, spec))
    col += spec[:, None] * np.array([1.0, 0.98, 0.92], F)
    if pal.get("metal"):
        scr_up = Nn @ u
        env = 0.30 + 0.55 * S.smoothstep(-0.3, 0.9, scr_up) + 0.5 * ndh ** 5 + 0.25 * S.smoothstep(0.55, 0.75, np.abs(np.sin(scr_up * 7.0 + Nn @ r * 3.0)))
        metal = alb * env[:, None] * (0.35 + 0.65 * ao)[:, None] * (0.45 + 0.55 * shade)[:, None]
        m = (is_body | is_belly)
        col[m] = (0.25 * col + 0.9 * metal)[m] + (spec[:, None] * pal["flank"][None])[m]

    rim = ((1.0 - ndv) ** 2.6) * np.clip(Nn @ u * 0.7 + 0.5, 0, 1) * 0.55
    col += rim[:, None] * pal["rim"][None] * (0.4 + 0.6 * ao)[:, None]

    # eyes burn
    eye_c = pal["eye"][None] * (0.85 + 0.4 * ndv[:, None])
    col[is_eye] = eye_c[is_eye]

    ghost = pal.get("ghost")
    alpha_hit = np.ones(len(P), F)
    if ghost:
        fres = (1.0 - ndv) ** 1.4
        s1, _, _ = S.worley(P, 9.0)
        stars = S.smoothstep(0.11, 0.03, s1)
        col = col * 0.55 + pal["glow"][None] * (0.22 + 0.85 * fres[:, None]) + stars[:, None] * 0.9
        alpha_hit = np.clip(0.50 + 0.5 * fres + 0.5 * stars + is_eye * 1.0 + is_bone * 0.25, 0, 1) * np.where(is_memb, 0.72, 1.0)

    img = np.zeros((n * n, 3), F)
    img[idx] = np.clip(col, 0, 1)
    img = img.reshape(n, n, 3)
    a = np.zeros(n * n, F)
    a[idx] = alpha_hit
    a = a.reshape(n, n)

    line = S.inner_lines(buf, buf["extent"] * 0.02)
    line = np.maximum(line, S.dilate_mask(line, max(0, ss - 1)) * 0.85)
    hitf = hit.astype(F)
    if ghost:
        img = img + line[..., None] * pal["glow"][None, None] * 0.5
        halo = S.blur_mask(hitf, ss * 5.0)
        out_rgb = np.where(hit[..., None], np.clip(img, 0, 1), pal["glow"][None, None] * np.ones_like(img))
        out_a = np.where(hit, a, halo * 0.55)
    else:
        img = img * (1.0 - 0.62 * line[..., None])
        grown = S.dilate_mask(hitf, max(1, int(round(ss * 1.25))))
        soft = np.maximum(grown, S.blur_mask(grown, ss * 0.6))
        out_rgb = np.where(hit[..., None], img, pal["outline"][None, None] * np.ones_like(img))
        out_a = np.where(hit, 1.0, soft * 0.95)
    pre = np.concatenate([out_rgb * out_a[..., None], out_a[..., None]], -1)
    small = S.box_down(pre, ss)
    al = small[..., 3:4]
    return np.concatenate([np.where(al > 1e-4, small[..., :3] / np.maximum(al, 1e-4), 0), al], -1)


def to_image(rgba):
    return Image.fromarray((np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA")


def geometry(stage, facing, ss=2, px=None):
    st = STAGES[stage]
    sc, info = build(st, "raised" if facing == "east" else "spread")
    cam = S.camera(facing, PITCH[facing])
    f, r, u = cam
    px = px or st["px"]
    cy = info["yh"] * (1.75 if facing == "east" else 0.95)
    L = unit(-0.55 * r + 0.75 * u - 0.5 * f)
    return S.render(sc, px * ss, cam, (0, cy, 0.05), st["extent"], shadow_dir=L, ao_scale=0.05 * st["extent"] / 2.3)


def sheet(job):
    stage, facing, breeds, px, ss = job
    buf = geometry(stage, facing, ss, px)
    out = []
    for breed in breeds:
        img = to_image(paint(buf, BREEDS[breed], ss, scale_freq=24.0 * 2.3 / STAGES[stage]["extent"] * (0.8 if stage == "Elder" else 1.0)))
        name = "Drakeling_%s" % facing if stage == "Drakeling" else "Wyrm_%s_%s_%s" % (breed, stage, facing)
        path = os.path.join(TEX, "Pawns", name + ".png")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        img.save(path, optimize=True)
        out.append(path)
    return out


def jobs():
    js = []
    for stage in ("Hatchling", "Drake", "Elder"):
        for facing in ("south", "east", "north"):
            js.append((stage, facing, ["Jade", "Spirit", "Gold", "Brood"], None, 2))
    for facing in ("south", "east", "north"):
        js.append(("Drakeling", facing, ["Drakeling"], None, 2))
    return js


def make_all(procs=4):
    import multiprocessing as mp
    js = sorted(jobs(), key=lambda j: -STAGES[j[0]]["px"])
    with mp.Pool(procs) as pool:
        for paths in pool.imap_unordered(sheet, js):
            for p in paths:
                print("  wrote", os.path.relpath(p, TEX))


if __name__ == "__main__":
    if len(sys.argv) >= 3:
        import time
        t0 = time.time()
        stage, facing = sys.argv[1], sys.argv[2]
        px = int(sys.argv[3]) if len(sys.argv) > 3 else None
        breeds = ["Drakeling"] if stage == "Drakeling" else ["Jade", "Spirit", "Gold", "Brood"]
        for p in sheet((stage, facing, breeds, px, 2)):
            print(p)
        print("%.1fs" % (time.time() - t0))
    else:
        make_all()
        print("Wyrm sheets done.")

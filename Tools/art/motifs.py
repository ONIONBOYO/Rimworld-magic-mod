"""
Vector motif library. Every motif draws a white silhouette into an 'L' mask using normalized coordinates
in [-1, 1] (y points up). Masks are drawn supersampled and downsampled by the caller.
"""
import math
from PIL import Image, ImageDraw

N = 512  # mask resolution


class Pen:
    def __init__(self, size=N):
        self.size = size
        self.img = Image.new("L", (size, size), 0)
        self.d = ImageDraw.Draw(self.img)

    def P(self, x, y):
        s = self.size
        return (s / 2 + x * s / 2, s / 2 - y * s / 2)

    def pts(self, ps):
        return [self.P(x, y) for x, y in ps]

    def poly(self, ps, v=255):
        self.d.polygon(self.pts(ps), fill=v)

    def circle(self, x, y, r, v=255):
        a = self.P(x - r, y + r)
        b = self.P(x + r, y - r)
        self.d.ellipse([a, b], fill=v)

    def ellipse(self, x, y, rx, ry, v=255):
        a = self.P(x - rx, y + ry)
        b = self.P(x + rx, y - ry)
        self.d.ellipse([a, b], fill=v)

    def ring(self, x, y, r, w, v=255):
        a = self.P(x - r, y + r)
        b = self.P(x + r, y - r)
        self.d.ellipse([a, b], outline=v, width=max(1, int(w * self.size / 2)))

    def line(self, ps, w, v=255, round_=True):
        pp = self.pts(ps)
        width = max(1, int(w * self.size / 2))
        self.d.line(pp, fill=v, width=width, joint="curve")
        if round_:
            r = width / 2
            for p in (pp[0], pp[-1]):
                self.d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=v)

    def rect(self, x0, y0, x1, y1, v=255):
        a = self.P(x0, y1)
        b = self.P(x1, y0)
        self.d.rectangle([a, b], fill=v)

    def arc(self, x, y, r, a0, a1, w, v=255, steps=48):
        ps = [(x + r * math.cos(math.radians(a0 + (a1 - a0) * i / steps)), y + r * math.sin(math.radians(a0 + (a1 - a0) * i / steps))) for i in range(steps + 1)]
        self.line(ps, w, v)

    def pie(self, x, y, r, a0, a1, v=255, steps=48):
        ps = [(x, y)] + [(x + r * math.cos(math.radians(a0 + (a1 - a0) * i / steps)), y + r * math.sin(math.radians(a0 + (a1 - a0) * i / steps))) for i in range(steps + 1)]
        self.poly(ps, v)


def rot(ps, deg, cx=0.0, cy=0.0):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return [(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c) for x, y in ps]


def star_pts(n, r_out, r_in, cx=0, cy=0, phase=90):
    ps = []
    for i in range(n * 2):
        r = r_out if i % 2 == 0 else r_in
        a = math.radians(phase + i * 180 / n)
        ps.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return ps


def flame_pts(cx=0, cy=0, w=0.55, h=0.9, lean=0.0, steps=40):
    ps = []
    for i in range(steps + 1):
        t = i / steps
        ang = math.pi * 2 * t
        # teardrop: bottom round, top pointed
        x = math.sin(ang) * w * (1 - 0.15 * math.cos(ang))
        y = -math.cos(ang) * h * 0.5
        if y > 0:
            k = y / (h * 0.5)
            x *= (1 - k) ** 0.9
            y = y * 1.9
        ps.append((cx + x + lean * max(0, y), cy + y - h * 0.15))
    return ps


# --------------------------------------------------------------------------------------------- motifs

def m_flame(p):
    p.poly(flame_pts(0, -0.05, 0.6, 1.25, 0.12))
    p.poly(flame_pts(0.05, -0.2, 0.3, 0.7, 0.08), 0)
    p.poly(flame_pts(0.04, -0.3, 0.17, 0.42, 0.05), 200)


def m_fireball(p):
    tail = flame_pts(0, 0, 0.55, 1.6, 0.0)
    p.poly(rot([(x, y + 0.2) for x, y in tail], 45))
    p.poly(rot([(x * 0.5, y * 0.75 + 0.1) for x, y in tail], 45), 0)
    p.circle(0.22, -0.22, 0.42)
    p.circle(0.3, -0.3, 0.2, 200)


def m_meteor(p):
    tail = [(-0.3, 0.0), (0.3, 0.0), (0.12, 1.25), (0.0, 1.45), (-0.12, 1.25)]
    for ang, sc in [(45, 1.0), (30, 0.7), (60, 0.7)]:
        p.poly(rot([(x * sc, y * sc) for x, y in tail], ang))
    rock = [(0.25 + 0.42 * math.cos(math.radians(a)) * (1 + 0.12 * math.sin(a * 3)), -0.25 + 0.42 * math.sin(math.radians(a)) * (1 + 0.1 * math.cos(a * 5))) for a in range(0, 360, 20)]
    p.poly([(x - 0.05, y + 0.05) for x, y in rock], 0)
    p.poly(rock)
    p.circle(0.35, -0.35, 0.1, 0)
    p.circle(0.12, -0.12, 0.06, 0)


def m_volcano(p):
    p.poly([(-0.9, -0.8), (-0.25, 0.2), (0.25, 0.2), (0.9, -0.8)])
    p.poly([(-0.25, 0.2), (0.0, 0.0), (0.25, 0.2)], 0)
    for x, h in [(-0.3, 0.75), (0.0, 0.95), (0.3, 0.7)]:
        p.circle(x, h, 0.17)
    p.line([(0, 0.15), (0, 0.6)], 0.14)
    p.line([(-0.15, -0.3), (0.05, -0.6)], 0.05, 0)


def m_bolt(p):
    p.poly([(0.2, 0.95), (-0.45, 0.0), (-0.05, 0.0), (-0.3, -0.95), (0.5, 0.15), (0.08, 0.15), (0.4, 0.95)])


def m_snowflake(p):
    for k in range(6):
        a = k * 60
        arm = rot([(0, 0), (0, 0.9)], a)
        p.line(arm, 0.1)
        for t, l in [(0.45, 0.25), (0.7, 0.18)]:
            b = rot([(0, t), (l, t + l)], a)
            c = rot([(0, t), (-l, t + l)], a)
            p.line(b, 0.07)
            p.line(c, 0.07)
    p.circle(0, 0, 0.14)


def m_shard(p):
    p.poly([(0.0, 0.95), (0.22, 0.1), (0.0, -0.9), (-0.22, 0.1)])
    p.poly(rot([(0.0, 0.5), (0.1, 0.0), (0.0, -0.5), (-0.1, 0.0)], -35, 0, 0), 255)
    p.poly([(0.42, 0.45), (0.55, 0.05), (0.42, -0.45), (0.3, 0.05)])
    p.poly([(-0.42, 0.45), (-0.3, 0.05), (-0.42, -0.45), (-0.55, 0.05)])
    p.line([(0, 0.85), (0, -0.8)], 0.025, 120)


def m_cloud(p):
    for x, y, r in [(-0.45, -0.05, 0.32), (0.0, 0.15, 0.42), (0.45, -0.02, 0.33), (0.22, -0.2, 0.28), (-0.2, -0.22, 0.28)]:
        p.circle(x, y, r)
    p.rect(-0.75, -0.48, 0.75, -0.2)


def m_tornado(p):
    for i, (y, w) in enumerate([(0.75, 0.85), (0.45, 0.65), (0.15, 0.48), (-0.15, 0.33), (-0.45, 0.2), (-0.72, 0.1)]):
        off = 0.08 * math.sin(i * 1.3)
        p.ellipse(off, y, w, 0.11)
        p.ellipse(off, y + 0.02, w * 0.75, 0.05, 0)


def m_wave(p):
    pts = []
    for i in range(41):
        t = i / 40
        x = -0.9 + 1.8 * t
        y = -0.55 + 0.25 * math.sin(t * math.pi * 2)
        pts.append((x, y))
    p.poly([(-0.9, -0.9)] + pts + [(0.9, -0.9)])
    p.arc(0.15, 0.05, 0.55, 200, 420, 0.24)
    p.circle(0.12, 0.08, 0.25, 0)


def m_skull(p):
    p.ellipse(0, 0.18, 0.62, 0.62)
    p.rect(-0.36, -0.55, 0.36, -0.1)
    p.ellipse(-0.24, 0.05, 0.17, 0.2, 0)
    p.ellipse(0.24, 0.05, 0.17, 0.2, 0)
    p.poly([(0, -0.08), (-0.08, -0.25), (0.08, -0.25)], 0)
    for x in (-0.22, -0.07, 0.07, 0.22):
        p.line([(x, -0.38), (x, -0.56)], 0.04, 0, False)


def m_bone(p):
    p.line([(-0.5, -0.5), (0.5, 0.5)], 0.2)
    for x, y in [(-0.5, -0.5), (0.5, 0.5)]:
        p.circle(x + (0.13 if x > 0 else -0.13), y - (0.05 if x > 0 else -0.05), 0.15)
        p.circle(x - (0.05 if x > 0 else -0.05), y + (0.13 if x > 0 else -0.13), 0.15)


def m_hand(p):
    p.ellipse(0, -0.25, 0.42, 0.45)
    for x, top in [(-0.3, 0.45), (-0.1, 0.62), (0.1, 0.65), (0.3, 0.5)]:
        p.line([(x, -0.1), (x, top)], 0.17)
    p.line([(-0.35, -0.3), (-0.65, 0.05)], 0.17)
    p.rect(-0.3, -0.85, 0.3, -0.55)


def m_eye(p):
    ps = []
    for i in range(41):
        t = i / 40 * math.pi * 2
        ps.append((0.9 * math.cos(t), 0.48 * math.sin(t) * abs(math.sin(t)) ** 0.3))
    p.poly(ps)
    p.circle(0, 0, 0.36, 0)
    p.circle(0, 0, 0.27)
    p.circle(0, 0, 0.12, 0)
    p.circle(0.09, 0.09, 0.06, 255)


def m_heart(p):
    p.circle(-0.3, 0.2, 0.36)
    p.circle(0.3, 0.2, 0.36)
    p.poly([(-0.64, 0.08), (0.64, 0.08), (0, -0.8)])


def m_cross(p):
    p.rect(-0.14, -0.9, 0.14, 0.9)
    p.rect(-0.7, 0.18, 0.7, 0.44)
    for x, y in [(0, 0.9), (0, -0.9), (-0.7, 0.31), (0.7, 0.31)]:
        p.circle(x, y, 0.16)
    p.ring(0, 0.31, 0.38, 0.06)


def m_shield(p):
    ps = [(-0.7, 0.75), (0.7, 0.75), (0.7, 0.1)] + [(0.7 * math.cos(a) , -0.0 - 0.85 * math.sin(a)) for a in [i * math.pi / 20 for i in range(1, 20)]] + [(-0.7, 0.1)]
    p.poly(ps)
    inner = [(x * 0.75, y * 0.78 + 0.03) for x, y in ps]
    p.poly(inner, 0)
    p.poly([(x * 0.6, y * 0.62 + 0.05) for x, y in ps], 255)
    p.rect(-0.05, -0.55, 0.05, 0.6, 0)
    p.rect(-0.45, 0.2, 0.45, 0.3, 0)


def m_sword(p):
    p.poly([(0, 0.95), (0.11, 0.75), (0.11, -0.25), (-0.11, -0.25), (-0.11, 0.75)])
    p.rect(-0.45, -0.37, 0.45, -0.25)
    p.rect(-0.07, -0.75, 0.07, -0.37)
    p.circle(0, -0.82, 0.1)
    p.line([(0, 0.8), (0, -0.2)], 0.025, 140, False)


def m_hammer(p):
    p.rect(-0.08, -0.9, 0.08, 0.35)
    p.rect(-0.55, 0.25, 0.55, 0.7)
    p.rect(-0.6, 0.3, -0.5, 0.65, 0)
    p.rect(0.5, 0.3, 0.6, 0.65, 0)


def m_anvil(p):
    p.poly([(-0.85, 0.35), (0.6, 0.35), (0.6, 0.05), (0.3, -0.1), (0.3, -0.35), (0.55, -0.6), (-0.55, -0.6), (-0.3, -0.35), (-0.3, -0.1), (-0.5, 0.05), (-0.85, 0.2)])
    p.rect(-0.7, -0.8, 0.7, -0.6)


def m_gear(p):
    for k in range(10):
        p.poly(rot([(-0.13, 0.55), (0.13, 0.55), (0.1, 0.85), (-0.1, 0.85)], k * 36))
    p.circle(0, 0, 0.62)
    p.circle(0, 0, 0.28, 0)


def m_rune(p):
    p.ring(0, 0, 0.82, 0.08)
    p.line([(0, 0.6), (0, -0.6)], 0.12)
    p.line([(0, 0.6), (0.35, 0.25), (0, -0.05)], 0.1)
    p.line([(0, -0.05), (-0.35, -0.35)], 0.1)
    p.line([(-0.25, 0.4), (0.25, 0.4)], 0.06)


def m_hourglass(p):
    p.rect(-0.6, 0.72, 0.6, 0.85)
    p.rect(-0.6, -0.85, 0.6, -0.72)
    p.poly([(-0.45, 0.72), (0.45, 0.72), (0.06, 0.0), (0.45, -0.72), (-0.45, -0.72), (-0.06, 0.0)])
    p.poly([(-0.32, 0.62), (0.32, 0.62), (0.05, 0.1), (-0.05, 0.1)], 0)
    p.poly([(-0.05, -0.1), (0.05, -0.1), (0.3, -0.62), (-0.3, -0.62)], 0)
    p.poly([(-0.18, -0.62), (0.18, -0.62), (0.0, -0.32)], 255)
    p.rect(-0.55, -0.72, -0.48, 0.72)
    p.rect(0.48, -0.72, 0.55, 0.72)


def m_clock(p):
    p.circle(0, 0, 0.82)
    p.circle(0, 0, 0.7, 0)
    for k in range(12):
        a = math.radians(k * 30)
        r0 = 0.52 if k % 3 == 0 else 0.6
        p.line([(r0 * math.cos(a), r0 * math.sin(a)), (0.66 * math.cos(a), 0.66 * math.sin(a))], 0.05, 255, False)
    p.line([(0, 0), (0, 0.45)], 0.08)
    p.line([(0, 0), (0.32, -0.12)], 0.08)
    p.circle(0, 0, 0.08)


def m_portal(p):
    for r, w in [(0.85, 0.07), (0.62, 0.06), (0.4, 0.05)]:
        p.ring(0, 0, r, w)
    p.circle(0, 0, 0.2)
    for k in range(6):
        p.arc(0, 0, 0.73, k * 60, k * 60 + 30, 0.05)


def m_swirl(p):
    ps = []
    for i in range(160):
        t = i / 160
        a = t * math.pi * 4.2
        r = 0.08 + 0.8 * t
        ps.append((r * math.cos(a), r * math.sin(a)))
    p.line(ps, 0.12)


def m_star(p):
    p.poly(star_pts(5, 0.92, 0.38))


def m_sun(p):
    for k in range(12):
        p.poly(rot([(-0.08, 0.55), (0.08, 0.55), (0, 0.95)], k * 30))
    p.circle(0, 0, 0.5)
    p.circle(0, 0, 0.36, 0)
    p.circle(0, 0, 0.28)


def m_moon(p):
    p.circle(0, 0, 0.75)
    p.circle(0.32, 0.18, 0.62, 0)
    for x, y, r in [(0.5, -0.45, 0.07), (0.65, 0.6, 0.05)]:
        p.poly(star_pts(4, r * 2, r * 0.5, x, y))


def m_leaf(p):
    ps = []
    for i in range(41):
        t = i / 40
        a = t * math.pi
        ps.append((0.45 * math.sin(a), -0.85 + 1.7 * t))
    ps2 = [(-x, y) for x, y in reversed(ps)]
    p.poly(rot(ps + ps2, -30))
    p.line(rot([(0, -0.95), (0, 0.75)], -30), 0.05, 0, False)
    for t in (-0.4, -0.05, 0.3):
        p.line(rot([(0, t), (0.22, t + 0.2)], -30), 0.035, 0, False)
        p.line(rot([(0, t), (-0.22, t + 0.2)], -30), 0.035, 0, False)


def m_tree(p):
    p.rect(-0.1, -0.85, 0.1, -0.1)
    p.poly([(-0.1, -0.6), (-0.35, -0.85), (0.35, -0.85), (0.1, -0.6)])
    for x, y, r in [(0, 0.35, 0.45), (-0.4, 0.1, 0.32), (0.4, 0.1, 0.32), (-0.2, 0.6, 0.3), (0.22, 0.62, 0.3)]:
        p.circle(x, y, r)


def m_vine(p):
    ps = []
    for i in range(80):
        t = i / 79
        ps.append((0.45 * math.sin(t * math.pi * 2.5), -0.9 + 1.8 * t))
    p.line(ps, 0.09)
    for t in (0.2, 0.45, 0.7, 0.9):
        x = 0.45 * math.sin(t * math.pi * 2.5)
        y = -0.9 + 1.8 * t
        side = 1 if math.cos(t * math.pi * 2.5) > 0 else -1
        p.ellipse(x + side * 0.2, y, 0.17, 0.09)


def m_flower(p):
    for k in range(6):
        a = math.radians(k * 60 + 90)
        p.ellipse(0.42 * math.cos(a), 0.42 * math.sin(a), 0.26, 0.26)
    p.circle(0, 0, 0.26, 0)
    p.circle(0, 0, 0.2)


def m_mushroom(p):
    p.pie(0, 0.0, 0.8, 0, 180)
    p.rect(-0.18, -0.8, 0.18, 0.0)
    for x, y, r in [(-0.35, 0.3, 0.1), (0.2, 0.45, 0.12), (0.45, 0.15, 0.08)]:
        p.circle(x, y, r, 0)


def m_paw(p):
    p.ellipse(0, -0.3, 0.42, 0.36)
    for x, y in [(-0.52, 0.18), (-0.2, 0.45), (0.2, 0.45), (0.52, 0.18)]:
        p.ellipse(x, y, 0.17, 0.21)


def m_claw(p):
    for dx in (-0.4, 0.0, 0.4):
        ps = [(dx - 0.25 + 0.5 * t + 0.15 * math.sin(t * math.pi), 0.85 - 1.7 * t) for t in [i / 30 for i in range(31)]]
        p.line(ps, 0.13)


def m_feather(p):
    ps = []
    for i in range(41):
        t = i / 40
        ps.append((0.3 * math.sin(t * math.pi) ** 0.8, -0.75 + 1.65 * t))
    p.poly(rot(ps + [(-x * 0.9, y) for x, y in reversed(ps)], -25))
    p.line(rot([(0, -0.95), (0, 0.85)], -25), 0.04, 0, False)
    for t in (-0.3, 0.0, 0.3):
        p.line(rot([(0.0, t), (0.3, t + 0.15)], -25), 0.025, 0, False)


def m_wing(p):
    for i in range(6):
        l = 1.45 - i * 0.18
        ps = rot([(0, 0.06), (l, 0.12), (l * 0.97, -0.06), (0, -0.1)], 70 - i * 21, 0, 0)
        p.poly([(x - 0.55, y - 0.35) for x, y in ps])
    p.ellipse(-0.55, -0.35, 0.22, 0.18)


def m_phoenix(p):
    p.poly(flame_pts(0, -0.05, 0.22, 0.85, 0.0))
    for s in (-1, 1):
        for i in range(4):
            l = 0.85 - i * 0.12
            ps = rot([(0, 0), (l, 0.1), (l * 0.95, -0.03), (0, -0.1)], 40 - i * 18, 0, 0)
            p.poly([(s * x, y + 0.1) for x, y in ps])
    p.poly([(-0.18, -0.4), (0, -0.95), (0.18, -0.4)])
    p.circle(0, 0.45, 0.13)
    p.poly([(0, 0.45), (0.22, 0.4), (0, 0.38)])


def m_note(p):
    p.ellipse(-0.42, -0.5, 0.22, 0.16)
    p.ellipse(0.38, -0.32, 0.22, 0.16)
    p.rect(-0.26, -0.5, -0.17, 0.6)
    p.rect(0.54, -0.32, 0.63, 0.78)
    p.poly([(-0.26, 0.45), (0.63, 0.63), (0.63, 0.83), (-0.26, 0.65)])


def m_mask(p):
    ps = [(0.65 * math.cos(a), 0.15 + 0.75 * math.sin(a) * (1 if math.sin(a) > 0 else 1.1)) for a in [i * math.pi / 30 for i in range(61)]]
    p.poly(ps)
    p.ellipse(-0.27, 0.25, 0.16, 0.1, 0)
    p.ellipse(0.27, 0.25, 0.16, 0.1, 0)
    p.arc(0, -0.15, 0.35, 200, 340, 0.08, 0)


def m_dagger(p):
    p.poly(rot([(0, 0.95), (0.12, 0.6), (0.1, -0.15), (-0.1, -0.15), (-0.12, 0.6)], -35))
    p.poly(rot([(-0.3, -0.15), (0.3, -0.15), (0.3, -0.25), (-0.3, -0.25)], -35))
    p.poly(rot([(-0.06, -0.25), (0.06, -0.25), (0.06, -0.65), (-0.06, -0.65)], -35))
    p.circle(*rot([(0, -0.72)], -35)[0], 0.09)


def m_drop(p):
    ps = []
    for i in range(41):
        a = math.pi * 2 * i / 40
        x = 0.5 * math.sin(a)
        y = -0.25 - 0.5 * math.cos(a)
        if y > -0.25:
            x *= max(0.0, 1 - (y + 0.25) / 1.15)
            y = -0.25 + (y + 0.25) * 2.2
        ps.append((x, y))
    p.poly(ps)
    p.ellipse(-0.18, -0.35, 0.08, 0.15, 0)


def m_chain(p):
    p.line([(-0.55, -0.15), (-0.05, 0.35)], 0.1)
    for (cx, cy) in [(-0.3, -0.3), (0.3, 0.3)]:
        ps = [(cx + 0.35 * math.cos(a) * 0.75, cy + 0.35 * math.sin(a) * 1.25) for a in [i * math.pi / 20 for i in range(41)]]
        p.line(rot(ps, -45, cx, cy), 0.12)


def m_crown(p):
    p.poly([(-0.75, -0.45), (-0.8, 0.45), (-0.4, 0.05), (0, 0.6), (0.4, 0.05), (0.8, 0.45), (0.75, -0.45)])
    p.rect(-0.78, -0.65, 0.78, -0.45)
    for x, y in [(-0.8, 0.5), (0, 0.67), (0.8, 0.5)]:
        p.circle(x, y, 0.1)
    p.circle(0, -0.2, 0.1, 0)


def m_crystal(p):
    p.poly([(0, 0.95), (0.3, 0.55), (0.3, -0.5), (0, -0.85), (-0.3, -0.5), (-0.3, 0.55)])
    p.poly([(0.5, 0.45), (0.68, 0.15), (0.68, -0.5), (0.5, -0.7), (0.35, -0.5), (0.35, 0.15)])
    p.poly([(-0.5, 0.35), (-0.35, 0.05), (-0.35, -0.5), (-0.5, -0.7), (-0.68, -0.5), (-0.68, 0.05)])
    p.line([(0, 0.95), (0, -0.85)], 0.03, 110, False)


def m_orb(p):
    p.circle(0, 0, 0.7)
    p.ring(0, 0, 0.85, 0.05)
    p.circle(-0.22, 0.22, 0.2, 140)


def m_arrow(p):
    p.line([(-0.7, -0.7), (0.45, 0.45)], 0.12)
    p.poly([(0.8, 0.8), (0.25, 0.55), (0.55, 0.25)])
    p.poly([(-0.7, -0.7), (-0.85, -0.35), (-0.55, -0.45)])
    p.poly([(-0.7, -0.7), (-0.35, -0.85), (-0.45, -0.55)])


def m_bullet(p):
    p.poly(rot([(0, 0.9), (0.2, 0.55), (0.2, -0.75), (-0.2, -0.75), (-0.2, 0.55)], -35))
    p.poly(rot([(-0.2, -0.45), (0.2, -0.45), (0.2, -0.5), (-0.2, -0.5)], -35), 0)


def m_gun(p):
    p.rect(-0.75, 0.05, 0.75, 0.28)
    p.rect(-0.3, -0.05, 0.25, 0.32)
    p.poly([(-0.3, 0.0), (-0.05, 0.0), (-0.2, -0.75), (-0.55, -0.7)])
    p.circle(0.05, -0.08, 0.13)
    p.circle(0.05, -0.08, 0.07, 0)
    p.rect(0.6, 0.28, 0.7, 0.38)


def m_pillar(p):
    p.rect(-0.25, -0.9, 0.25, 0.9)
    p.rect(-0.38, -0.9, 0.38, -0.75)
    p.rect(-0.38, 0.75, 0.38, 0.9)
    p.rect(-0.08, -0.7, 0.08, 0.7, 120)


def m_wall(p):
    for row in range(5):
        y0 = -0.8 + row * 0.33
        off = 0.25 if row % 2 else 0.0
        for col in range(-2, 3):
            x0 = -0.85 + col * 0.5 + off + 0.5
            x0 = max(-0.85, x0 - 0.5)
            p.rect(x0 + 0.03, y0 + 0.03, min(0.85, x0 + 0.47), y0 + 0.3)


def m_burst(p):
    p.poly(star_pts(12, 0.95, 0.42, phase=7))
    p.circle(0, 0, 0.3, 0)
    p.circle(0, 0, 0.18)


def m_ring(p):
    p.ring(0, 0, 0.75, 0.16)
    p.ring(0, 0, 0.45, 0.06)


def m_rock(p):
    ps = [(0.8 * math.cos(math.radians(a)) * (1 + 0.15 * math.sin(a * 0.11)), -0.1 + 0.6 * math.sin(math.radians(a)) * (1 + 0.12 * math.cos(a * 0.07))) for a in range(0, 360, 24)]
    p.poly(ps)
    p.line([(-0.3, 0.1), (0.0, -0.05), (0.25, 0.2)], 0.05, 0)


def m_book(p):
    p.poly([(-0.85, 0.55), (-0.05, 0.45), (-0.05, -0.6), (-0.85, -0.5)])
    p.poly([(0.85, 0.55), (0.05, 0.45), (0.05, -0.6), (0.85, -0.5)])
    for i in range(4):
        y = 0.3 - i * 0.2
        p.line([(-0.7, y + 0.02), (-0.2, y - 0.03)], 0.03, 0, False)
        p.line([(0.7, y + 0.02), (0.2, y - 0.03)], 0.03, 0, False)


def m_coin(p):
    p.circle(0, 0, 0.82)
    p.ring(0, 0, 0.66, 0.05, 0)
    p.poly(star_pts(5, 0.42, 0.18), 0)


def m_dice(p):
    p.poly([(0, 0.85), (0.75, 0.45), (0, 0.05), (-0.75, 0.45)])
    p.poly([(-0.75, 0.38), (-0.04, -0.02), (-0.04, -0.9), (-0.75, -0.48)], 210)
    p.poly([(0.75, 0.38), (0.04, -0.02), (0.04, -0.9), (0.75, -0.48)], 170)
    for x, y in [(0, 0.45)]:
        p.ellipse(x, y, 0.1, 0.06, 0)
    for x, y in [(-0.55, 0.1), (-0.25, -0.45)]:
        p.ellipse(x, y, 0.07, 0.09, 0)
    for x, y in [(0.25, 0.05), (0.4, -0.2), (0.55, -0.45)]:
        p.ellipse(x, y, 0.07, 0.09, 0)


def m_pie(p):
    p.ellipse(0, -0.2, 0.85, 0.42)
    p.ellipse(0, -0.05, 0.78, 0.36, 0)
    pts = [(-0.78 + 1.56 * i / 30, 0.05 + 0.12 * math.sin(i * 0.9)) for i in range(31)]
    p.poly(pts + [(0.78, -0.25), (-0.78, -0.25)], 220)
    for x in (-0.4, 0.0, 0.4):
        p.circle(x, 0.22, 0.12)


def m_chicken(p):
    p.ellipse(0.0, -0.15, 0.55, 0.45)
    p.circle(0.42, 0.35, 0.25)
    p.poly([(0.62, 0.38), (0.88, 0.3), (0.62, 0.25)])
    for x in (0.3, 0.42, 0.54):
        p.circle(x, 0.62, 0.08)
    p.poly([(-0.5, 0.0), (-0.85, 0.45), (-0.75, -0.05)])
    p.line([(-0.05, -0.55), (-0.1, -0.9)], 0.05)
    p.line([(0.15, -0.55), (0.2, -0.9)], 0.05)
    p.circle(0.48, 0.4, 0.05, 0)


def m_snail(p):
    p.poly([(-0.85, -0.55), (0.8, -0.55), (0.85, -0.4), (0.5, -0.3), (-0.85, -0.38)])
    p.circle(-0.05, 0.05, 0.5)
    p.arc(-0.05, 0.05, 0.3, 0, 300, 0.05, 0)
    p.line([(0.6, -0.35), (0.8, 0.15)], 0.05)
    p.circle(0.8, 0.15, 0.06)


def m_bug(p):
    p.ellipse(0, -0.1, 0.38, 0.55)
    p.circle(0, 0.55, 0.2)
    p.line([(0, 0.45), (0, -0.62)], 0.03, 0, False)
    for s in (-1, 1):
        for y in (0.15, -0.1, -0.35):
            p.line([(0.3 * s, y), (0.75 * s, y + 0.15)], 0.06)
        p.line([(0.1 * s, 0.68), (0.35 * s, 0.9)], 0.04)


def m_thrumbo(p):
    p.ellipse(0, -0.15, 0.55, 0.6)
    p.poly([(-0.08, 0.35), (0.08, 0.35), (0.0, 0.98)])
    p.ellipse(-0.6, 0.15, 0.2, 0.1)
    p.ellipse(0.6, 0.15, 0.2, 0.1)
    p.circle(-0.22, 0.0, 0.08, 0)
    p.circle(0.22, 0.0, 0.08, 0)
    p.ellipse(0, -0.55, 0.22, 0.12, 180)


def m_thorn(p):
    for dx, sc in [(-0.35, 0.8), (0.1, 1.0), (0.5, 0.7)]:
        ps = [(dx + sc * (0.22 - 0.22 * t + 0.25 * t * t), -0.85 + sc * 1.6 * t) for t in [i / 20 for i in range(21)]]
        ps2 = [(dx - sc * 0.18 * (1 - t) + sc * 0.25 * t * t, -0.85 + sc * 1.6 * t) for t in [i / 20 for i in range(20, -1, -1)]]
        p.poly(ps + ps2)


def m_axe(p):
    p.rect(-0.07, -0.9, 0.07, 0.8)
    p.poly([(0.07, 0.65), (0.75, 0.85), (0.85, 0.25), (0.75, -0.25), (0.07, 0.05)])
    p.poly([(-0.07, 0.6), (-0.45, 0.7), (-0.45, 0.2), (-0.07, 0.25)])


def m_brazier(p):
    p.poly([(-0.65, 0.0), (0.65, 0.0), (0.4, -0.45), (-0.4, -0.45)])
    p.rect(-0.08, -0.8, 0.08, -0.45)
    p.rect(-0.45, -0.88, 0.45, -0.78)
    p.poly(flame_pts(0, 0.35, 0.45, 0.9))
    p.poly(flame_pts(0, 0.25, 0.2, 0.5), 0)


MOTIFS = {k[2:]: v for k, v in globals().items() if k.startswith("m_") and callable(v)}


def mask(name, size=128, scale=1.0):
    fn = MOTIFS.get(name)
    if fn is None:
        raise KeyError("unknown motif " + name)
    p = Pen()
    fn(p)
    img = p.img
    if scale != 1.0:
        s = int(N * scale)
        small = img.resize((s, s), Image.LANCZOS)
        img = Image.new("L", (N, N), 0)
        img.paste(small, ((N - s) // 2, (N - s) // 2))
    return img.resize((size, size), Image.LANCZOS)

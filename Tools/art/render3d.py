"""
A tiny software renderer for building sprites.

RimWorld draws buildings top-down with a "lifted" oblique projection: the ground footprint fills its cells and anything with
height is drawn further up the screen. Painting a flat front-on picture and letting the game rotate it looks wrong, so
directional buildings are modelled here in 3D and rendered once per facing.

  world axes: x = east, y = up, z = north (one unit = one cell)
  screen:     sx = x, sy = z + K * y          (K = how far up the screen one unit of height is drawn)

Along a projection ray the visible surface is simply the highest one, so the depth buffer stores world height.
Faces are convex polygons; materials are small functions of the world point and of a face-local (u, v) frame.
"""
import math

import numpy as np
from PIL import Image, ImageFilter

K = 0.62
LIGHT = np.array([-0.45, 0.8, -0.4], np.float32)
LIGHT /= np.linalg.norm(LIGHT)
VIEW = np.array([0.0, 1.0, -K], np.float32)
VIEW /= np.linalg.norm(VIEW)


def _n(v):
    v = np.asarray(v, np.float32)
    n = np.linalg.norm(v)
    return v / n if n > 1e-9 else v


class Face:
    def __init__(self, pts, mat, uv=None, emissive=False, tag=None):
        self.pts = [np.asarray(p, np.float32) for p in pts]
        self.mat = mat
        self.uv = uv            # (origin, u axis, v axis) in world space, or None
        self.emissive = emissive
        self.tag = tag
        a, b, c = self.pts[0], self.pts[1], self.pts[2]
        n = np.cross(b - a, c - a)
        # orient normals outward/upward consistently: callers give any winding, we flip toward the viewer later
        self.normal = _n(n)


class Model:
    def __init__(self):
        self.faces = []
        self.discs = []    # floating emissive orbs: (center, radius, color, core)

    def add(self, face):
        self.faces.append(face)
        return face

    # -------------------------------------------------------------------------------- primitives

    def hexahedron(self, v, mat, uv=None, skip=(), emissive=False, tag=None):
        """Eight corners: v[0..3] bottom (SW, SE, NE, NW), v[4..7] top in the same order. Faces point outward."""
        v = [np.asarray(p, np.float32) for p in v]
        quads = {
            "bottom": [v[0], v[3], v[2], v[1]],
            "top": [v[4], v[5], v[6], v[7]],
            "south": [v[0], v[1], v[5], v[4]],
            "north": [v[2], v[3], v[7], v[6]],
            "west": [v[3], v[0], v[4], v[7]],
            "east": [v[1], v[2], v[6], v[5]],
        }
        out = []
        for name, q in quads.items():
            if name in skip:
                continue
            m = mat[name] if isinstance(mat, dict) else mat
            out.append(self.add(Face(q, m, uv=(uv or {}).get(name) if isinstance(uv, dict) else uv, emissive=emissive, tag=tag)))
        return out

    def box(self, x0, x1, y0, y1, z0, z1, mat, **kw):
        return self.hexahedron([(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1),
                                (x0, y1, z0), (x1, y1, z0), (x1, y1, z1), (x0, y1, z1)], mat, **kw)

    def prism(self, cx, cz, r, y0, y1, sides, mat, rot=0.0, r_top=None, tag=None):
        """Upright n-gon prism (or frustum when r_top is given)."""
        r_top = r if r_top is None else r_top
        ang = [rot + 2 * math.pi * i / sides for i in range(sides)]
        bot = [np.array([cx + r * math.cos(a), y0, cz + r * math.sin(a)], np.float32) for a in ang]
        top = [np.array([cx + r_top * math.cos(a), y1, cz + r_top * math.sin(a)], np.float32) for a in ang]
        self.add(Face(top, mat["top"] if isinstance(mat, dict) else mat, tag=tag))
        for i in range(sides):
            j = (i + 1) % sides
            self.add(Face([bot[i], bot[j], top[j], top[i]], mat["side"] if isinstance(mat, dict) else mat, tag=tag))

    def transformed(self, rot_deg):
        """A copy rotated about the vertical axis (positive = counter-clockwise seen from above)."""
        t = math.radians(rot_deg)
        c, s = math.cos(t), math.sin(t)

        def r(p):
            return np.array([p[0] * c - p[2] * s, p[1], p[0] * s + p[2] * c], np.float32)

        m = Model()
        for f in self.faces:
            uv = None
            if f.uv is not None:
                o, eu, ev = f.uv
                uv = (r(o), r(eu), r(ev))
            nf = Face([r(p) for p in f.pts], f.mat, uv=uv, emissive=f.emissive, tag=f.tag)
            m.faces.append(nf)
        for (cen, rad, col, core) in self.discs:
            m.discs.append((r(np.asarray(cen, np.float32)), rad, col, core))
        return m


# ------------------------------------------------------------------------------------------------ rendering

def render(model, size=256, extent=1.5, ss=2, outline=(18, 13, 24), outline_px=2, edge_dark=0.72, ambient=0.42):
    """Render to an RGBA image. extent = world units covered by the image (the def's drawSize)."""
    S = size * ss
    ppu = S / extent
    depth = np.full((S, S), -1e9, np.float32)
    color = np.zeros((S, S, 3), np.float32)
    fid = np.full((S, S), -1, np.int32)
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    sx_all = (xx + 0.5 - S / 2) / ppu
    sy_all = (S / 2 - (yy + 0.5)) / ppu

    for idx, f in enumerate(model.faces):
        n = f.normal
        if float(np.dot(n, VIEW)) < 0:
            n = -n
        facing = float(n[1] - K * n[2])
        if facing <= 1e-4:
            continue   # edge-on or back-facing
        P = np.array([[p[0], p[2] + K * p[1]] for p in f.pts], np.float32)
        x0, y0 = P.min(0)
        x1, y1 = P.max(0)
        px0 = max(0, int((x0 * ppu) + S / 2) - 1)
        px1 = min(S, int((x1 * ppu) + S / 2) + 2)
        py0 = max(0, int(S / 2 - y1 * ppu) - 1)
        py1 = min(S, int(S / 2 - y0 * ppu) + 2)
        if px1 <= px0 or py1 <= py0:
            continue
        sx = sx_all[py0:py1, px0:px1]
        sy = sy_all[py0:py1, px0:px1]
        # convex point-in-polygon, either winding
        inside_pos = np.ones(sx.shape, bool)
        inside_neg = np.ones(sx.shape, bool)
        for i in range(len(P)):
            ax, ay = P[i]
            bx, by = P[(i + 1) % len(P)]
            cr = (bx - ax) * (sy - ay) - (by - ay) * (sx - ax)
            inside_pos &= cr >= -1e-6
            inside_neg &= cr <= 1e-6
        inside = inside_pos | inside_neg
        if not inside.any():
            continue
        c = float(np.dot(n, f.pts[0]))
        wy = (c - n[0] * sx - n[2] * sy) / facing
        wz = sy - K * wy
        closer = inside & (wy > depth[py0:py1, px0:px1] + 1e-5)
        if not closer.any():
            continue
        wpts = np.stack([sx, wy, wz], -1)
        if f.uv is not None:
            o, eu, ev = f.uv
            d = wpts - o
            u = d @ eu
            v = d @ ev
        else:
            u = v = None
        base = f.mat(wpts, u, v, n)
        if f.emissive:
            shaded = base
        else:
            lam = max(0.0, float(np.dot(n, LIGHT)))
            shaded = base * (ambient + (1 - ambient) * lam * 1.15)
        region_d = depth[py0:py1, px0:px1]
        region_c = color[py0:py1, px0:px1]
        region_f = fid[py0:py1, px0:px1]
        region_d[closer] = wy[closer]
        region_c[closer] = shaded[closer] if shaded.ndim == 3 else shaded
        region_f[closer] = idx

    alpha = (fid >= 0).astype(np.float32)
    # darken edges between faces so the forms read clearly
    edge = np.zeros_like(alpha, bool)
    for dy, dx in ((0, 1), (1, 0)):
        a = fid
        b = np.roll(np.roll(fid, dy, 0), dx, 1)
        edge |= (a != b) & (a >= 0) & (b >= 0)
    color[edge] *= edge_dark

    # floating emissive orbs, drawn on top
    for cen, rad, col, core in model.discs:
        cx = cen[0] * ppu + S / 2
        cy = S / 2 - (cen[2] + K * cen[1]) * ppu
        r = np.sqrt((xx + 0.5 - cx) ** 2 + (yy + 0.5 - cy) ** 2) / (rad * ppu)
        m = r <= 1.0
        t = np.clip(r, 0, 1)[..., None]
        cc = np.asarray(core, np.float32) * (1 - t) + np.asarray(col, np.float32) * t
        color[m] = cc[m]
        alpha[m] = 1.0

    rgb = np.clip(color, 0, 255)
    img = Image.fromarray(np.dstack([rgb, alpha * 255]).astype(np.uint8), "RGBA")
    if outline_px:
        a = img.split()[3]
        k = outline_px * ss * 2 + 1
        grown = a.filter(ImageFilter.MaxFilter(k))
        bg = Image.new("RGBA", img.size, tuple(outline) + (0,))
        bg.putalpha(grown)
        img = Image.alpha_composite(bg, img)
    return img.resize((size, size), Image.LANCZOS)


# ------------------------------------------------------------------------------------------------ materials

_GRIDS = {}


def _noise3(p, scale, seed):
    """Cheap smooth-ish value noise on world positions."""
    g = _GRIDS.get(seed)
    if g is None:
        g = _GRIDS[seed] = np.random.RandomState(seed).rand(64, 64, 64).astype(np.float32)
    q = (p * scale) % 63.0
    i = np.floor(q).astype(np.int32)
    f = q - i
    f = f * f * (3 - 2 * f)
    i1 = (i + 1) % 64

    def G(a, b, c):
        return g[a[..., 0], b[..., 1], c[..., 2]]
    x0 = G(i, i, i) * (1 - f[..., 0]) + G(i1, i, i) * f[..., 0]
    x1 = G(i, i1, i) * (1 - f[..., 0]) + G(i1, i1, i) * f[..., 0]
    x2 = G(i, i, i1) * (1 - f[..., 0]) + G(i1, i, i1) * f[..., 0]
    x3 = G(i, i1, i1) * (1 - f[..., 0]) + G(i1, i1, i1) * f[..., 0]
    y0 = x0 * (1 - f[..., 1]) + x1 * f[..., 1]
    y1 = x2 * (1 - f[..., 1]) + x3 * f[..., 1]
    return y0 * (1 - f[..., 2]) + y1 * f[..., 2]


def flat(rgb, jitter=0.06, seed=3):
    base = np.asarray(rgb, np.float32)

    def m(p, u, v, n):
        k = 1.0 + jitter * (_noise3(p, 9.0, seed) - 0.5) * 2
        return base * k[..., None]
    return m


def wood(rgb, axis="x", seed=11, rings=46.0):
    base = np.asarray(rgb, np.float32)
    ai = {"x": 0, "y": 1, "z": 2}[axis]

    def m(p, u, v, n):
        warp = _noise3(p, 4.0, seed) * 2.5
        coord = p[..., (ai + 1) % 3] * rings + p[..., (ai + 2) % 3] * rings * 0.35 + warp * 6
        grain = 0.5 + 0.5 * np.sin(coord)
        k = 0.86 + 0.16 * grain + 0.08 * (_noise3(p, 18.0, seed + 1) - 0.5)
        return base * k[..., None]
    return m


def stone(rgb, seed=21):
    base = np.asarray(rgb, np.float32)

    def m(p, u, v, n):
        k = 0.82 + 0.3 * _noise3(p, 7.0, seed) + 0.1 * (_noise3(p, 24.0, seed + 5) - 0.5)
        return base * k[..., None]
    return m


def metal(rgb, seed=31):
    base = np.asarray(rgb, np.float32)

    def m(p, u, v, n):
        spec = np.clip(np.dot(n, _n([0.3, 1.0, -0.6])), 0, 1) ** 6
        k = 0.85 + 0.25 * _noise3(p, 12.0, seed) + 0.6 * spec
        return base * k[..., None]
    return m


def decal(base_mat, fn):
    """Paint onto a material using the face's (u, v) frame: fn(u, v) -> (mask 0..1, rgb)."""
    def m(p, u, v, n):
        c = base_mat(p, u, v, n)
        if u is None:
            return c
        mask, col = fn(u, v)
        mask = np.clip(mask, 0, 1)[..., None]
        return c * (1 - mask) + np.asarray(col, np.float32) * mask
    return m


def save_views(model_fn, rel_base, save, size=256, extent=1.5, west=True):
    """Render a model built facing south (its front toward -z) for each Graphic_Multi direction."""
    base = model_fn()
    views = {"north": 0, "east": -90, "south": 180}
    if west:
        views["west"] = 90
    for name, rot in views.items():
        save(render(base.transformed(rot), size=size, extent=extent), rel_base + "_" + name)

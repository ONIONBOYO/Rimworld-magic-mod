"""
A small signed-distance-field raymarcher in numpy.

The flat painted sprites elsewhere in this folder are fine for most things. The wyrm and the handful of objects that go with it
are modelled instead: shapes are built from round cones, ellipsoids, tori and thin triangles, blended smoothly into each other, and
rendered with an orthographic camera into buffers (position, normal, occlusion, shadow, which part was hit). The caller turns
those buffers into colour, so one render of the geometry can be painted as several breeds.

Model space: x right, y up, z forward.
"""
import math

import numpy as np

F = np.float32


def v3(*a):
    if len(a) == 1:
        a = a[0]
    return np.array(a, F)


def unit(v):
    v = np.asarray(v, F)
    return v / max(1e-9, float(np.linalg.norm(v)))


def dot(a, b):
    return np.einsum("ij,ij->i", a, b)


def smin(a, b, k):
    if k <= 0:
        return np.minimum(a, b)
    h = np.maximum(k - np.abs(a - b), 0) / k
    return np.minimum(a, b) - h * h * k * 0.25


def rot_x(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]], F)


def rot_y(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]], F)


def rot_z(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]], F)


def basis_from(forward, up=(0, 1, 0)):
    """Rows are the local x, y, z axes; z points along `forward`."""
    z = unit(forward)
    x = np.cross(np.asarray(up, F), z)
    if np.linalg.norm(x) < 1e-5:
        x = np.cross(v3(1, 0, 0), z)
    x = unit(x)
    y = np.cross(z, x)
    return np.stack([x, y, z]).astype(F)


# ------------------------------------------------------------------------------------------------ primitives

class Cone:
    """A capsule whose radius changes from one end to the other."""

    def __init__(self, a, b, ra, rb=None):
        self.a = v3(a)
        self.ba = v3(b) - self.a
        self.l2 = max(float(self.ba @ self.ba), 1e-9)
        self.ra = float(ra)
        self.dr = float(ra if rb is None else rb) - float(ra)
        self.bc = self.a + self.ba * 0.5
        self.bR = math.sqrt(self.l2) * 0.5 + max(self.ra, self.ra + self.dr)

    def d(self, P):
        pa = P - self.a
        h = np.clip(pa @ self.ba / self.l2, 0, 1)
        q = pa - h[:, None] * self.ba
        return np.sqrt(dot(q, q)) - (self.ra + self.dr * h)


class Ellipsoid:
    def __init__(self, c, r, basis=None):
        self.c = v3(c)
        self.r = v3(r)
        self.basis = None if basis is None else np.asarray(basis, F)
        self.bc, self.bR = self.c, float(self.r.max())

    def d(self, P):
        q = P - self.c
        if self.basis is not None:
            q = q @ self.basis.T
        a = q / self.r
        b = q / (self.r * self.r)
        k0 = np.sqrt(dot(a, a))
        k1 = np.sqrt(dot(b, b))
        return np.where(k1 > 1e-6, k0 * (k0 - 1.0) / np.maximum(k1, 1e-6), -float(self.r.min()))


class Sphere:
    def __init__(self, c, r):
        self.c = v3(c)
        self.r = float(r)
        self.bc, self.bR = self.c, self.r

    def d(self, P):
        q = P - self.c
        return np.sqrt(dot(q, q)) - self.r


class Torus:
    def __init__(self, c, axis, R, r):
        self.c = v3(c)
        self.n = unit(axis)
        self.R = float(R)
        self.r = float(r)
        self.bc, self.bR = self.c, self.R + self.r

    def d(self, P):
        q = P - self.c
        h = q @ self.n
        rad = np.sqrt(np.maximum(dot(q, q) - h * h, 0))
        return np.sqrt((rad - self.R) ** 2 + h * h) - self.r


class Box:
    def __init__(self, c, half, basis=None, round=0.0):
        self.c = v3(c)
        self.b = v3(half)
        self.basis = None if basis is None else np.asarray(basis, F)
        self.round = float(round)
        self.bc, self.bR = self.c, float(np.linalg.norm(self.b)) + self.round

    def d(self, P):
        q = P - self.c
        if self.basis is not None:
            q = q @ self.basis.T
        q = np.abs(q) - self.b
        outside = np.sqrt(dot(np.maximum(q, 0), np.maximum(q, 0)))
        return outside + np.minimum(q.max(1), 0) - self.round


class Cylinder:
    """A disc: axis `n`, radius R, half height h, with rounded rims."""

    def __init__(self, c, axis, R, h, round=0.0):
        self.c = v3(c)
        self.n = unit(axis)
        self.R = float(R)
        self.h = float(h)
        self.round = float(round)
        self.bc, self.bR = self.c, math.hypot(self.R, self.h) + self.round

    def d(self, P):
        q = P - self.c
        y = q @ self.n
        rad = np.sqrt(np.maximum(dot(q, q) - y * y, 0))
        dx = rad - self.R
        dy = np.abs(y) - self.h
        return np.minimum(np.maximum(dx, dy), 0) + np.sqrt(np.maximum(dx, 0) ** 2 + np.maximum(dy, 0) ** 2) - self.round


class Tri:
    """A thin sheet. `cuts` are spheres bitten out of it, for scalloped edges."""

    def __init__(self, a, b, c, thick=0.01, cuts=()):
        self.a, self.b, self.c = v3(a), v3(b), v3(c)
        self.ba = self.b - self.a
        self.cb = self.c - self.b
        self.ac = self.a - self.c
        self.nor = np.cross(self.ba, self.ac)
        self.e1 = np.cross(self.ba, self.nor)
        self.e2 = np.cross(self.cb, self.nor)
        self.e3 = np.cross(self.ac, self.nor)
        self.n2 = max(float(self.nor @ self.nor), 1e-12)
        self.thick = float(thick)
        self.cuts = [(v3(c0), float(r0)) for c0, r0 in cuts]
        self.bc = (self.a + self.b + self.c) / 3.0
        self.bR = max(float(np.linalg.norm(p - self.bc)) for p in (self.a, self.b, self.c)) + self.thick

    @staticmethod
    def _edge(e, p):
        h = np.clip(p @ e / max(float(e @ e), 1e-12), 0, 1)
        q = h[:, None] * e - p
        return dot(q, q)

    def d(self, P):
        pa, pb, pc = P - self.a, P - self.b, P - self.c
        outside = (np.sign(pa @ self.e1) + np.sign(pb @ self.e2) + np.sign(pc @ self.e3)) < 2.0
        edge = np.minimum(np.minimum(self._edge(self.ba, pa), self._edge(self.cb, pb)), self._edge(self.ac, pc))
        plane = (pa @ self.nor) ** 2 / self.n2
        d = np.sqrt(np.where(outside, edge, plane)) - self.thick
        for c0, r0 in self.cuts:
            q = P - c0
            d = np.maximum(d, r0 - np.sqrt(dot(q, q)))
        return d


class Cut:
    """`prim` with `cutter` carved out of it."""

    def __init__(self, prim, cutter):
        self.prim, self.cutter = prim, cutter
        self.bc, self.bR = prim.bc, prim.bR

    def d(self, P):
        return np.maximum(self.prim.d(P), -self.cutter.d(P))


class Blend:
    """Several parts melted together and treated as one."""

    def __init__(self, prims, k):
        self.prims, self.k = list(prims), float(k)
        cs = np.array([p.bc for p in self.prims], F)
        self.bc = cs.mean(0)
        self.bR = max(float(np.linalg.norm(p.bc - self.bc)) + p.bR for p in self.prims)

    def d(self, P):
        out = None
        for p in self.prims:
            d = p.d(P)
            out = d if out is None else smin(out, d, self.k)
        return out


# ------------------------------------------------------------------------------------------------ scene

class Group:
    def __init__(self, name, k, join):
        self.name = name
        self.k = k          # how softly the parts of this group melt into each other
        self.join = join    # how softly the group melts into the groups before it
        self.prims = []

    def add(self, *prims):
        self.prims.extend(prims)
        return self

    def cone(self, a, b, ra, rb=None):
        self.prims.append(Cone(a, b, ra, rb))
        return self

    def chain(self, pts):
        """pts: (x, y, z, radius) control points joined by round cones."""
        for p, q in zip(pts[:-1], pts[1:]):
            self.prims.append(Cone(p[:3], q[:3], p[3], q[3]))
        return self

    def d(self, P, cut=None):
        """Distance to the group. `cut`, if given, is a distance beyond which the answer does not matter to the caller, so parts
        that are certainly farther than that are not evaluated at all."""
        n = len(P)
        out = np.full(n, 1e3, F)
        for pr in self.prims:
            q = P - pr.bc
            lb = np.sqrt(dot(q, q)) - pr.bR
            m = lb < out + self.k
            if cut is not None:
                m &= lb < cut
            cnt = int(np.count_nonzero(m))
            if cnt == 0:
                continue
            if cnt > 0.7 * n:
                out = smin(out, pr.d(P), self.k).astype(F)
            else:
                ix = np.nonzero(m)[0]
                out[ix] = smin(out[ix], pr.d(P[ix]), self.k)
        return out


class Scene:
    def __init__(self):
        self.groups = []

    def group(self, name, k=0.0, join=0.0):
        g = Group(name, k, join)
        self.groups.append(g)
        return g

    def names(self):
        return [g.name for g in self.groups]

    def dist(self, P):
        out = None
        for g in self.groups:
            if not g.prims:
                continue
            if out is None:
                out = g.d(P)
            else:
                out = smin(out, g.d(P, out + g.join + g.k), g.join)
        return out

    def group_dists(self, P):
        return np.stack([g.d(P) for g in self.groups])


# ------------------------------------------------------------------------------------------------ camera and marching

def camera(facing, pitch_deg):
    """Orthographic camera looking down at `pitch_deg` above the horizon.
    south: the model's +z points down the screen (it faces the viewer); north: up the screen; east: to the right."""
    s, c = math.sin(math.radians(pitch_deg)), math.cos(math.radians(pitch_deg))
    if facing == "south":
        f = v3(0, -s, -c)
    elif facing == "north":
        f = v3(0, -s, c)
    elif facing == "east":
        f = v3(c, -s, 0)
    else:
        f = v3(-c, -s, 0)
    r = unit(np.cross(f, v3(0, 1, 0)))
    u = unit(np.cross(r, f))
    return f, r, u


def render(scene, n, cam, center, extent, bound=None, max_steps=110, shadow_dir=None, ao_scale=0.05, verbose=False):
    """March an n x n image. Returns a dict of (n, n[, k]) buffers: hit, P, N, depth, ao, shadow, gd (per-group distance)."""
    f, r, u = cam
    center = v3(center)
    bound = bound or extent * 1.6
    xs = (((np.arange(n) + 0.5) / n) * 2 - 1) * extent
    U, V = np.meshgrid(xs, -xs)
    O = (center[None, None] + U[..., None] * r + V[..., None] * u - f * bound).reshape(-1, 3).astype(F)
    t = np.zeros(n * n, F)
    hit = np.zeros(n * n, bool)
    act = np.arange(n * n)
    eps = extent / n * 0.6
    far = bound * 2
    for i in range(max_steps):
        P = O[act] + t[act, None] * f
        d = scene.dist(P)
        h = d < eps
        hit[act[h]] = True
        t[act] += np.where(h, 0, np.maximum(d, eps * 0.5) * 0.92).astype(F)
        act = act[~h & (t[act] < far)]
        if verbose and i % 10 == 0:
            print("  step", i, "active", len(act))
        if not len(act):
            break
    # anything still marching after the step budget is grazing the silhouette: count it as a hit if it is close
    if len(act):
        P = O[act] + t[act, None] * f
        hit[act[scene.dist(P) < eps * 4]] = True

    idx = np.nonzero(hit)[0]
    P = O[idx] + t[idx, None] * f
    e = eps
    k = [v3(1, -1, -1), v3(-1, -1, 1), v3(-1, 1, -1), v3(1, 1, 1)]
    N = np.zeros_like(P)
    for kv in k:
        N += kv[None] * scene.dist(P + kv * e)[:, None]
    N /= np.maximum(np.linalg.norm(N, axis=1, keepdims=True), 1e-9)

    occ = np.zeros(len(P), F)
    w = 1.0
    for i in range(1, 6):
        hh = ao_scale * i
        occ += w * np.maximum(hh - scene.dist(P + N * hh), 0) / hh
        w *= 0.62
    ao = np.clip(1.0 - occ * 0.55, 0, 1)

    sh = np.ones(len(P), F)
    if shadow_dir is not None:
        L = unit(shadow_dir)
        tt = np.full(len(P), eps * 6, F)
        Q = P + N * eps * 3
        for i in range(18):
            d = scene.dist(Q + tt[:, None] * L)
            sh = np.minimum(sh, np.clip(9.0 * d / tt, 0, 1))
            tt += np.clip(d, eps * 2, extent * 0.12)
    gd = scene.group_dists(P)

    def full(vals, shape=()):
        out = np.zeros((n * n,) + shape, F)
        out[idx] = vals
        return out.reshape((n, n) + shape)

    return dict(hit=hit.reshape(n, n), P=full(P, (3,)), N=full(N, (3,)), depth=full(t[idx]), ao=full(ao), shadow=full(sh),
                gd=np.stack([full(g) for g in gd]), names=scene.names(), cam=cam, eps=eps, n=n, extent=extent)


# ------------------------------------------------------------------------------------------------ shading helpers

def _hash3(c):
    d = c.astype(np.float64) @ np.array([[127.1, 269.5, 113.5], [311.7, 183.3, 271.9], [74.7, 246.1, 124.6]])
    return np.modf(np.sin(d) * 43758.5453123)[0] % 1.0


def worley(P, freq):
    """Cellular noise on (N, 3) points: (distance to nearest centre, distance to the next, a random number per cell)."""
    q = P.astype(np.float64) * freq
    base = np.floor(q)
    fr = q - base
    f1 = np.full(len(q), 9.0)
    f2 = np.full(len(q), 9.0)
    cell = np.zeros(len(q))
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for dz in (-1, 0, 1):
                off = np.array([dx, dy, dz], np.float64)
                h = _hash3(base + off)
                v = off + 0.15 + 0.7 * h - fr
                d = np.sqrt((v * v).sum(1))
                closer = d < f1
                f2 = np.where(closer, f1, np.minimum(f2, d))
                cell = np.where(closer, h[:, 0] * 0.731 + h[:, 1] * 0.269, cell)
                f1 = np.where(closer, d, f1)
    return f1.astype(F), f2.astype(F), cell.astype(F)


def value_noise(P, freq):
    q = P.astype(np.float64) * freq
    base = np.floor(q)
    fr = q - base
    fr = fr * fr * (3 - 2 * fr)
    out = np.zeros(len(q))
    for dx in (0, 1):
        for dy in (0, 1):
            for dz in (0, 1):
                h = _hash3(base + np.array([dx, dy, dz], np.float64))[:, 0]
                wgt = (fr[:, 0] if dx else 1 - fr[:, 0]) * (fr[:, 1] if dy else 1 - fr[:, 1]) * (fr[:, 2] if dz else 1 - fr[:, 2])
                out += h * wgt
    return out.astype(F)


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def box_down(img, k):
    """Average k x k blocks."""
    if k == 1:
        return img
    n = img.shape[0] // k
    return img[:n * k, :n * k].reshape(n, k, n, k, -1).mean((1, 3))


def dilate_mask(m, r):
    """Grow a float mask by r pixels (max filter, separable)."""
    out = m
    for _ in range(int(r)):
        p = np.pad(out, 1)
        out = np.maximum.reduce([p[1:-1, 1:-1], p[:-2, 1:-1], p[2:, 1:-1], p[1:-1, :-2], p[1:-1, 2:]])
    return out


def blur_mask(m, r):
    from PIL import Image, ImageFilter
    im = Image.fromarray(np.clip(m * 255, 0, 255).astype(np.uint8))
    return np.asarray(im.filter(ImageFilter.GaussianBlur(r)), F) / 255.0


def inner_lines(buf, depth_thr):
    """Dark lines where the surface folds over itself or one part passes in front of another."""
    t = buf["depth"]
    hit = buf["hit"]
    big = np.where(hit, t, 0)
    p = np.pad(big, 1, mode="edge")
    hp = np.pad(hit, 1, mode="edge")
    line = np.zeros_like(t)
    for sl in ((slice(0, -2), slice(1, -1)), (slice(2, None), slice(1, -1)), (slice(1, -1), slice(0, -2)), (slice(1, -1), slice(2, None))):
        diff = (p[sl] - big)
        line = np.maximum(line, np.where(hit & hp[sl], smoothstep(depth_thr, depth_thr * 2.2, diff), 0))
    return line

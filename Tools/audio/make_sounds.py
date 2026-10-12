"""
Synthesizes every sound effect used by the mod (pure numpy, no samples).
Usage: python3 Tools/audio/make_sounds.py
Writes Sounds/ArcanumAscendant/<Name>/<n>.wav (22.05 kHz, 16-bit mono).
"""
import os
import wave
import zlib

import numpy as np

SR = 22050
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Sounds", "ArcanumAscendant")


class Sig(np.ndarray):
    """1-D signal whose addition pads the shorter operand instead of failing."""

    def __add__(self, o):
        if isinstance(o, np.ndarray) and o.ndim == 1 and len(o) != len(self):
            n = max(len(self), len(o))
            a = np.pad(np.asarray(self), (0, n - len(self)))
            b = np.pad(np.asarray(o), (0, n - len(o)))
            return (a + b).view(Sig)
        return np.ndarray.__add__(self, o)

    __radd__ = __add__


def S(x):
    return np.asarray(x, dtype=np.float64).view(Sig)


def t_(dur):
    return np.arange(int(SR * dur)) / SR


def env(n, a=0.01, d=0.2, s=0.6, r=0.3, dur=None):
    total = n / SR
    a_n, d_n, r_n = int(a * SR), int(d * SR), int(r * SR)
    s_n = max(0, n - a_n - d_n - r_n)
    e = np.concatenate([np.linspace(0, 1, max(1, a_n)), np.linspace(1, s, max(1, d_n)), np.full(s_n, s), np.linspace(s, 0, max(1, r_n))])
    return S(np.pad(e, (0, max(0, n - len(e))))[:n])


def expdecay(n, tau):
    return S(np.exp(-np.arange(n) / SR / tau))


def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * np.asarray(cutoff) / SR)
    y = S(np.zeros(len(x)))
    acc = 0.0
    if np.ndim(a) == 0:
        for i in range(len(x)):
            acc = (1 - a) * x[i] + a * acc
            y[i] = acc
    else:
        for i in range(len(x)):
            acc = (1 - a[i]) * x[i] + a[i] * acc
            y[i] = acc
    return y


def fft_filter(x, lo=0, hi=SR / 2):
    X = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    X[(f < lo) | (f > hi)] = 0
    return S(np.fft.irfft(X, len(x)))


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def osc(freq, dur, kind="sine", phase=0.0):
    n = int(SR * dur)
    f = np.broadcast_to(np.asarray(freq, dtype=np.float64), (n,)) if np.ndim(freq) else np.full(n, float(freq))
    ph = 2 * np.pi * np.cumsum(f) / SR + phase
    if kind == "sine":
        return S(np.sin(ph))
    if kind == "saw":
        return S(2 * ((ph / (2 * np.pi)) % 1) - 1)
    if kind == "square":
        return S(np.sign(np.sin(ph)))
    if kind == "tri":
        return S(2 * np.abs(2 * ((ph / (2 * np.pi)) % 1) - 1) - 1)
    raise ValueError(kind)


def sweep(f0, f1, dur, curve=1.0):
    n = int(SR * dur)
    t = np.linspace(0, 1, n) ** curve
    return f0 * (f1 / f0) ** t


def bell(freq, dur, rng, partials=((1, 1.0), (2.76, 0.5), (5.4, 0.3), (8.93, 0.15)), tau=0.8):
    n = int(SR * dur)
    y = S(np.zeros(n))
    for ratio, amp in partials:
        y += amp * osc(freq * ratio * rng.uniform(0.997, 1.003), dur) * expdecay(n, tau / ratio ** 0.5)
    return y


def karplus(freq, dur, rng, damping=0.996):
    n = int(SR * dur)
    period = int(SR / freq)
    buf = rng.uniform(-1, 1, period)
    y = np.zeros(n)
    for i in range(n):
        y[i] = buf[i % period]
        buf[i % period] = damping * 0.5 * (buf[i % period] + buf[(i + 1) % period])
    return y


def noise(dur, rng):
    return S(rng.uniform(-1, 1, int(SR * dur)))


def reverb(x, rng, length=1.2, mix=0.3, tau=0.35):
    n = int(SR * length)
    ir = rng.normal(0, 1, n) * expdecay(n, tau)
    ir = lowpass(ir, 4000)
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    wet = np.convolve(x, ir)[: len(x) + n]
    dry = np.pad(x, (0, n))
    m = min(len(wet), len(dry))
    return S((1 - mix) * dry[:m] + mix * wet[:m] * 0.5)


def crackle(dur, rng, density=60):
    n = int(SR * dur)
    y = np.zeros(n)
    for _ in range(int(density * dur)):
        p = rng.randint(0, n - 200)
        L = rng.randint(30, 200)
        y[p:p + L] += rng.uniform(-1, 1, L) * expdecay(L, 0.002) * rng.uniform(0.3, 1)
    return y


def mixdown(*parts):
    n = max(len(p) for p in parts)
    y = np.zeros(n)
    for p in parts:
        y[: len(p)] += np.asarray(p)
    return S(y)


def fade(x, ms=10):
    n = int(SR * ms / 1000)
    x = x.copy()
    x[:n] *= np.linspace(0, 1, n)
    x[-n:] *= np.linspace(1, 0, n)
    return x


def write(name, idx, y):
    y = fade(y)
    peak = np.max(np.abs(y)) + 1e-9
    y = y / peak * 0.89
    d = os.path.join(OUT, name)
    os.makedirs(d, exist_ok=True)
    with wave.open(os.path.join(d, "%d.wav" % idx), "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((y * 32767).astype(np.int16).tobytes())


# ------------------------------------------------------------------------------------------ recipes

def whoosh(dur, rng, f0=300, f1=2500, q=1.0):
    n = noise(dur, rng)
    cut = sweep(f0, f1, dur, 0.8)
    y = lowpass(n, cut) - lowpass(n, cut * 0.25)
    return y * np.sin(np.linspace(0, np.pi, len(y))) ** 1.5


def boom(dur, rng, f0=90, f1=35):
    body = osc(sweep(f0, f1, dur, 0.5), dur) * expdecay(int(SR * dur), dur * 0.35)
    nz = lowpass(noise(dur, rng), 900) * expdecay(int(SR * dur), dur * 0.2)
    return body * 1.2 + nz * 1.5


def cast_fire(rng):
    d = 0.8
    return reverb(mixdown(whoosh(d, rng, 200, 3000) * 1.5, crackle(d, rng, 80) * 0.5 * env(int(SR * d), 0.05, 0.3, 0.6, 0.3)), rng, 0.6, 0.2)


def impact_fire(rng):
    d = 1.1
    return reverb(mixdown(boom(d, rng, 110, 40), crackle(d, rng, 120) * 0.6 * expdecay(int(SR * d), 0.4), whoosh(0.5, rng, 2000, 300) * 0.6), rng, 0.8, 0.25)


def cast_holy(rng):
    d = 1.3
    base = rng.choice([523.25, 587.33, 659.25])
    y = sum(bell(base * r, d, rng, tau=0.9) * a for r, a in [(1, 1), (1.25, 0.7), (1.5, 0.6), (2, 0.4)])
    shimmer = highpass(noise(d, rng), 5000) * 0.08 * env(int(SR * d), 0.3, 0.3, 0.5, 0.6)
    return reverb(y * env(int(SR * d), 0.08, 0.3, 0.7, 0.6) + shimmer, rng, 1.6, 0.45, 0.6)


def impact_holy(rng):
    d = 1.4
    base = rng.choice([392.0, 440.0])
    chord = sum(osc(base * r, d, "tri") * a for r, a in [(1, 1), (1.26, 0.6), (1.5, 0.6), (2, 0.5)])
    y = chord * expdecay(int(SR * d), 0.5) + boom(d, rng, 140, 60) * 0.5 + whoosh(0.6, rng, 400, 6000) * 0.4
    return reverb(y, rng, 1.6, 0.4, 0.5)


def cast_rune(rng):
    d = 0.7
    ping = bell(rng.choice([880, 990, 1046]), d, rng, partials=((1, 1), (2.0, 0.5), (3.01, 0.3)), tau=0.25)
    hum = osc(sweep(120, 240, d), d, "saw") * 0.15 * env(int(SR * d), 0.05, 0.2, 0.6, 0.3)
    return reverb(ping + lowpass(hum, 1500), rng, 0.6, 0.25)


def impact_rune(rng):
    d = 0.8
    clank = bell(rng.choice([300, 340]), d, rng, partials=((1, 1), (2.32, 0.7), (4.25, 0.5), (6.1, 0.3)), tau=0.18)
    zap = osc(sweep(2000, 200, 0.25), 0.25, "square") * 0.2 * expdecay(int(SR * 0.25), 0.08)
    return reverb(mixdown(clank, zap, boom(d, rng, 120, 50) * 0.5), rng, 0.6, 0.2)


def cast_frost(rng):
    d = 0.9
    sw = osc(sweep(2000, 4500, d), d) * 0.2 * env(int(SR * d), 0.2, 0.3, 0.5, 0.3)
    tinkles = np.zeros(int(SR * d))
    for _ in range(12):
        p = rng.randint(0, len(tinkles) - 4000)
        b = bell(rng.uniform(2500, 5000), 0.18, rng, partials=((1, 1), (2.7, 0.3)), tau=0.06)
        tinkles[p:p + len(b)] += b * rng.uniform(0.2, 0.6)
    return reverb(sw + tinkles + whoosh(d, rng, 3000, 8000) * 0.3, rng, 1.0, 0.35)


def impact_frost(rng):
    d = 1.0
    shatter = np.zeros(int(SR * d))
    for _ in range(40):
        p = rng.randint(0, int(SR * 0.25))
        b = bell(rng.uniform(1800, 6000), 0.25, rng, partials=((1, 1), (2.4, 0.4)), tau=0.05)
        shatter[p:p + len(b)] += b * rng.uniform(0.2, 0.8)
    crack = highpass(noise(d, rng), 2000) * expdecay(int(SR * d), 0.06)
    return reverb(mixdown(shatter, crack * 0.8, boom(d, rng, 90, 50) * 0.4), rng, 1.0, 0.3)


def cast_storm(rng):
    d = 0.7
    buzz = osc(sweep(60, 120, d) + 8 * np.sin(np.linspace(0, 60, int(SR * d))), d, "saw")
    buzz = highpass(buzz, 300) * env(int(SR * d), 0.05, 0.2, 0.7, 0.2) * 0.5
    zaps = crackle(d, rng, 50) * 0.6
    return reverb(buzz + highpass(zaps, 1500), rng, 0.6, 0.25)


def impact_storm(rng):
    d = 1.6
    crack = highpass(noise(0.12, rng), 800) * expdecay(int(SR * 0.12), 0.03) * 2
    rumble = lowpass(noise(d, rng), 180) * expdecay(int(SR * d), 0.6) * 4
    return reverb(mixdown(crack, rumble, crackle(0.4, rng, 120) * 0.5), rng, 1.4, 0.35, 0.5)


def cast_necro(rng):
    d = 1.1
    f = rng.choice([110, 98])
    moan = sum(osc(f * r * (1 + 0.01 * np.sin(np.linspace(0, 18, int(SR * d)) + k)), d, "saw") for k, r in enumerate([1, 1.006, 1.5, 0.5]))
    moan = lowpass(moan, sweep(300, 1200, d)) * env(int(SR * d), 0.25, 0.3, 0.6, 0.4) * 0.4
    whisper = fft_filter(noise(d, rng), 2000, 6000) * 0.15 * env(int(SR * d), 0.2, 0.3, 0.6, 0.3)
    return reverb(moan + whisper, rng, 1.2, 0.4, 0.5)


def impact_necro(rng):
    d = 1.0
    thud = boom(d, rng, 70, 30)
    hiss = fft_filter(noise(d, rng), 1500, 7000) * expdecay(int(SR * d), 0.3) * 0.5
    return reverb(thud + hiss, rng, 1.0, 0.35)


def cast_shadow(rng):
    d = 0.9
    w = whoosh(d, rng, 3000, 200)[::-1]
    sub = osc(sweep(80, 40, d), d) * 0.4 * env(int(SR * d), 0.3, 0.2, 0.6, 0.2)
    return reverb(w + sub, rng, 1.0, 0.4)


def impact_shadow(rng):
    d = 0.9
    thump = boom(d, rng, 60, 35) * 0.9
    breath = fft_filter(noise(d, rng), 300, 2500) * expdecay(int(SR * d), 0.25) * 0.6
    return reverb(thump + breath, rng, 1.0, 0.4)


def cast_arcane(rng):
    d = 0.8
    sparkle = np.zeros(int(SR * d))
    for k in range(10):
        p = int(k * SR * d / 12)
        b = bell(800 * 2 ** (k / 6) * rng.uniform(0.98, 1.02), 0.3, rng, partials=((1, 1), (2, 0.3)), tau=0.12)
        sparkle[p:p + len(b)] += b[: len(sparkle) - p] * 0.5
    return reverb(sparkle + whoosh(d, rng, 500, 5000) * 0.4, rng, 1.0, 0.35)


def impact_arcane(rng):
    d = 0.9
    pop = osc(sweep(900, 150, 0.2), 0.2) * expdecay(int(SR * 0.2), 0.06)
    shimmer = sum(bell(f, d, rng, tau=0.4) for f in (660, 880, 1320)) * 0.4
    return reverb(mixdown(pop, shimmer, boom(d, rng, 120, 60) * 0.4), rng, 1.0, 0.35)


def cast_blood(rng):
    d = 0.9
    beats = np.zeros(int(SR * d))
    for p in (0.0, 0.18, 0.5, 0.68):
        b = boom(0.2, rng, 70, 40) * 0.8
        i = int(p * SR)
        beats[i:i + len(b)] += b[: len(beats) - i]
    wet = lowpass(noise(d, rng), 1200) * env(int(SR * d), 0.2, 0.3, 0.5, 0.3) * 0.3
    return reverb(beats + wet, rng, 0.8, 0.3)


def impact_blood(rng):
    d = 0.8
    splash = fft_filter(noise(d, rng), 400, 4000) * expdecay(int(SR * d), 0.15)
    return reverb(mixdown(boom(d, rng, 80, 40), splash * 0.8), rng, 0.8, 0.25)


def cast_nature(rng):
    d = 0.9
    rustle = fft_filter(noise(d, rng), 1500, 8000) * (0.5 + 0.5 * np.abs(np.sin(np.linspace(0, 25, int(SR * d))))) * env(int(SR * d), 0.1, 0.2, 0.6, 0.3) * 0.5
    knock = bell(rng.choice([220, 250]), 0.5, rng, partials=((1, 1), (2.9, 0.4)), tau=0.08)
    chirp = osc(sweep(2400, 3600, 0.12), 0.12) * expdecay(int(SR * 0.12), 0.04) * 0.3
    return reverb(mixdown(rustle, knock, np.pad(chirp, (int(SR * 0.4), 0))), rng, 1.0, 0.35)


def impact_nature(rng):
    d = 0.9
    thud = boom(d, rng, 100, 45) * 0.8
    crunch = crackle(0.5, rng, 160) * 0.4
    return reverb(mixdown(thud, crunch, fft_filter(noise(d, rng), 1500, 7000) * expdecay(int(SR * d), 0.3) * 0.3), rng, 0.9, 0.3)


def cast_song(rng):
    d = 1.2
    base = rng.choice([261.63, 293.66, 329.63])
    y = np.zeros(int(SR * d))
    for k, r in enumerate([1, 1.25, 1.5, 2.0]):
        note = karplus(base * r, d - k * 0.09, rng)
        i = int(k * 0.09 * SR)
        y[i:i + len(note)] += note * 0.6
    return reverb(y, rng, 1.2, 0.35)


def impact_song(rng):
    d = 1.0
    base = rng.choice([196.0, 220.0])
    chord = sum(karplus(base * r, d, rng, 0.993) for r in (1, 1.25, 1.5, 2.0)) * 0.5
    return reverb(chord + boom(d, rng, 130, 70) * 0.4, rng, 1.0, 0.3)


def cast_void(rng):
    d = 1.0
    war = osc(sweep(900, 120, d, 0.7) * (1 + 0.05 * np.sin(np.linspace(0, 70, int(SR * d)))), d, "tri") * env(int(SR * d), 0.05, 0.3, 0.6, 0.3) * 0.5
    return reverb(war + whoosh(d, rng, 4000, 300) * 0.3, rng, 1.2, 0.45)


def impact_void(rng):
    d = 1.3
    implode = osc(sweep(400, 25, d, 0.4), d) * expdecay(int(SR * d), 0.5)
    suck = whoosh(0.6, rng, 6000, 200) * 0.6
    return reverb(mixdown(suck, np.pad(implode, (int(SR * 0.2), 0))), rng, 1.4, 0.4, 0.5)


def cast_time(rng):
    d = 1.0
    ticks = np.zeros(int(SR * d))
    for k in range(6):
        b = bell(3200, 0.05, rng, partials=((1, 1),), tau=0.01)
        i = int(k * 0.15 * SR)
        ticks[i:i + len(b)] += b * 0.6
    chime = bell(rng.choice([660, 740]), d, rng, tau=0.5)[::-1] * 0.5
    return reverb(ticks + chime, rng, 1.0, 0.35)


def impact_time(rng):
    d = 1.1
    f = sweep(880, 440, d, 0.5)
    y = osc(f, d) * expdecay(int(SR * d), 0.4) + osc(f * 1.5, d) * expdecay(int(SR * d), 0.3) * 0.4
    return reverb(y + whoosh(0.5, rng, 200, 4000) * 0.3, rng, 1.2, 0.4)


def charge(rng):
    d = 0.9
    sw = osc(sweep(200, 900, d, 1.5), d, "tri") * env(int(SR * d), 0.3, 0.2, 0.8, 0.2) * 0.35
    return reverb(sw + whoosh(d, rng, 300, 3000) * 0.4, rng, 0.8, 0.3)


def turret_shot(rng):
    d = 0.25
    z = osc(sweep(1800, 300, d, 0.5), d, "square") * expdecay(int(SR * d), 0.05)
    return reverb(lowpass(z, 4000) + highpass(noise(d, rng), 3000) * expdecay(int(SR * d), 0.02) * 0.5, rng, 0.3, 0.2)


def construct_break(rng):
    d = 0.9
    crumble = crackle(d, rng, 200) * expdecay(int(SR * d), 0.3)
    return reverb(mixdown(boom(d, rng, 90, 40) * 0.6, crumble), rng, 0.8, 0.25)


def level_up(rng):
    d = 1.8
    y = np.zeros(int(SR * d))
    for k, f in enumerate([523.25, 659.25, 783.99, 1046.5, 1318.5]):
        b = bell(f, 1.0, rng, tau=0.5)
        i = int(k * 0.09 * SR)
        y[i:i + len(b)] += b[: len(y) - i] * 0.6
    return reverb(y + whoosh(d, rng, 500, 8000) * 0.2, rng, 1.6, 0.4, 0.6)


def awaken(rng):
    d = 2.8
    chord = sum(osc(f * (1 + 0.003 * np.sin(np.linspace(0, 12, int(SR * d)) + i)), d, "saw") for i, f in enumerate([130.81, 196.0, 261.63, 329.63, 392.0]))
    chord = lowpass(chord, sweep(200, 3000, d, 0.7)) * env(int(SR * d), 0.8, 0.5, 0.7, 1.0) * 0.3
    bells = mixdown(*[np.pad(bell(f, 1.8, rng, tau=0.8), (int(0.8 * SR), 0))[: int(SR * d)] for f in (1046.5, 1318.5, 1568.0)]) * 0.4
    return reverb(mixdown(chord, bells, boom(d, rng, 60, 30) * 0.5), rng, 2.0, 0.5, 0.8)


def node_unlock(rng):
    d = 0.7
    y = mixdown(bell(rng.choice([1046.5, 1174.7]), d, rng, tau=0.3), np.pad(bell(1568.0, 0.5, rng, tau=0.25), (int(0.06 * SR), 0))[: int(SR * d)] * 0.6)
    return reverb(y, rng, 0.8, 0.3)


RECIPES = {
    "Charge": charge, "Turret_Shot": turret_shot, "Construct_Break": construct_break, "LevelUp": level_up, "Awaken": awaken, "NodeUnlock": node_unlock,
    "Cast_Fire": cast_fire, "Impact_Fire": impact_fire, "Cast_Holy": cast_holy, "Impact_Holy": impact_holy, "Cast_Rune": cast_rune, "Impact_Rune": impact_rune,
    "Cast_Frost": cast_frost, "Impact_Frost": impact_frost, "Cast_Storm": cast_storm, "Impact_Storm": impact_storm, "Cast_Necro": cast_necro,
    "Impact_Necro": impact_necro, "Cast_Shadow": cast_shadow, "Impact_Shadow": impact_shadow, "Cast_Arcane": cast_arcane, "Impact_Arcane": impact_arcane,
    "Cast_Blood": cast_blood, "Impact_Blood": impact_blood, "Cast_Nature": cast_nature, "Impact_Nature": impact_nature, "Cast_Song": cast_song,
    "Impact_Song": impact_song, "Cast_Void": cast_void, "Impact_Void": impact_void, "Cast_Time": cast_time, "Impact_Time": impact_time,
}


def main():
    total = 0
    for name, fn in RECIPES.items():
        for v in range(2):
            rng = np.random.RandomState(zlib.crc32(name.encode()) % 10000 + v * 7919)
            y = fn(rng)
            write(name, v, y)
            total += 1
    print("Wrote %d sound files" % total)


if __name__ == "__main__":
    main()

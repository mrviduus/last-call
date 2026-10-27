"""Synthesised sound effects (our own, no recordings) -> Assets/Resources/Sfx/*.wav.

Run:  python3 Tools/make_sfx.py   (standard library only)
Each sound is built from sine sweeps and filtered noise with a short envelope, 22050 Hz mono 16-bit.
Replace any file with your own recording of the same name and the game picks it up.
"""
import math
import os
import random
import struct
import wave

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Sfx")
RATE = 22050
rng = random.Random(1)


def save(name, samples):
    peak = max(1e-6, max(abs(s) for s in samples))
    data = b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s / peak * 0.9)) * 32767)) for s in samples)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data)
    print("wrote", name + ".wav", "%.2fs" % (len(samples) / RATE))


def noise(n, cutoff):
    """White noise through a one-pole low-pass (cutoff 0..1: higher = brighter)."""
    out, y = [], 0.0
    for _ in range(n):
        y += cutoff * (rng.uniform(-1, 1) - y)
        out.append(y)
    return out


def sweep(n, f0, f1):
    """Sine whose pitch glides from f0 to f1 Hz."""
    out, phase = [], 0.0
    for i in range(n):
        f = f0 + (f1 - f0) * i / n
        phase += 2 * math.pi * f / RATE
        out.append(math.sin(phase))
    return out


def env(n, attack, decay_power=2.0):
    """Fast attack, then a curved decay to silence."""
    a = max(1, int(attack * RATE))
    return [i / a if i < a else (1 - (i - a) / max(1, n - a)) ** decay_power for i in range(n)]


def mix(*layers):
    n = max(len(l) for l in layers)
    return [sum(l[i] for l in layers if i < len(l)) for i in range(n)]


def scale(xs, k):
    return [x * k for x in xs]


def times(a, b):
    return [x * y for x, y in zip(a, b)]


def whoosh():
    n = int(0.14 * RATE)
    e = [math.sin(math.pi * i / n) ** 2 for i in range(n)]       # swells and fades
    save("whoosh", times(noise(n, 0.12), e))


def hit():
    n = int(0.12 * RATE)
    body = times(sweep(n, 170, 60), env(n, 0.002, 3))
    click = times(noise(int(0.012 * RATE), 0.8), env(int(0.012 * RATE), 0.0005, 1))
    save("hit", mix(body, scale(click, 0.6)))


def hit_heavy():
    n = int(0.26 * RATE)
    body = times(sweep(n, 120, 38), env(n, 0.002, 2.5))
    crunch = times(noise(int(0.08 * RATE), 0.35), env(int(0.08 * RATE), 0.001, 2))
    s = mix(body, scale(crunch, 0.8))
    save("hit_heavy", [math.tanh(x * 2.2) for x in s])               # a little grit


def glass():
    n = int(0.45 * RATE)
    layers = [scale(times(noise(int(0.05 * RATE), 0.9), env(int(0.05 * RATE), 0.0005, 2)), 0.7)]
    for _ in range(9):                                               # tinkling pieces
        start = rng.randint(0, int(0.25 * RATE))
        m = int(rng.uniform(0.05, 0.15) * RATE)
        f = rng.uniform(2200, 5200)
        ping = times(sweep(m, f, f * 0.97), env(m, 0.0005, 3))
        layers.append([0.0] * start + scale(ping, rng.uniform(0.2, 0.45)))
    save("glass", mix(*layers))


def wood():
    n = int(0.3 * RATE)
    layers = [times(sweep(n, 140, 70), scale(env(n, 0.002, 3), 0.6))]
    for k in range(5):                                               # splintering cracks
        start = int(k * 0.035 * RATE + rng.randint(0, 300))
        m = int(0.03 * RATE)
        layers.append([0.0] * start + scale(times(noise(m, 0.6), env(m, 0.0005, 2)), 0.8 - k * 0.12))
    save("wood", mix(*layers))


def thud():
    n = int(0.2 * RATE)
    body = times(sweep(n, 90, 40), env(n, 0.003, 2))
    dust = times(noise(n, 0.08), env(n, 0.005, 3))
    save("thud", mix(body, scale(dust, 0.5)))


def ball():
    n = int(0.07 * RATE)
    save("ball", times(sweep(n, 1400, 1100), env(n, 0.0005, 4)))


def splat():
    """Wet gore: a dull low thump under dark gurgling noise that wobbles."""
    n = int(0.45 * RATE)
    body = times(sweep(n, 120, 45), env(n, 0.002, 2))
    wet = times(noise(n, 0.05), env(n, 0.004, 1.5))
    wobble = [w * (0.6 + 0.4 * math.sin(i / RATE * 2 * math.pi * 23)) for i, w in enumerate(wet)]
    save("splat", [math.tanh(x * 2.5) for x in mix(scale(body, 0.8), wobble)])


def crunch():
    """Bone snapping: a few sharp bright cracks close together."""
    n = int(0.25 * RATE)
    layers = [times(sweep(n, 200, 60), scale(env(n, 0.001, 3), 0.5))]
    for k in range(4):
        start = int(k * 0.018 * RATE + rng.randint(0, 200))
        m = int(0.015 * RATE)
        layers.append([0.0] * start + scale(times(noise(m, 0.9), env(m, 0.0002, 3)), 1.0 - k * 0.15))
    save("crunch", mix(*layers))


def bell():
    """The last-call bell over the bar: a brass ring, a few inharmonic partials dying away slowly."""
    n = int(1.6 * RATE)
    layers = [scale(times(sweep(n, f, f), env(n, 0.002, 1.5)), a) for f, a in ((880, 1.0), (2420, 0.5), (4250, 0.25))]
    save("bell", mix(*layers))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for make in (whoosh, hit, hit_heavy, glass, wood, thud, ball, splat, crunch, bell):
        make()

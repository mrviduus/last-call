"""ORBIT 9 space bar: pixel-art layers and props (our own design) -> Assets/Resources/Art/Bar.

Run:  python3 Tools/make_bar_art.py   (needs Pillow: python3 -m pip install pillow)
The PNGs are the real assets; redraw them by hand freely, this script is just the first pass.

Art rules used here:
- one limited palette (PALETTE): every pixel is snapped to it, so all layers match;
- light sources (lamps, neon, LED strips, window, spotlights) are painted additively, then
  ordered (Bayer) dithering turns the soft gradients into crisp pixel steps;
- light comes from the top-left: highlights top/left, shadows bottom/right, dark outlines on props;
- the background stays lower in contrast than the fighters so the fight reads clearly.
Coordinates are pixels, y from the top. Screen 384x240; the floor (walkable) starts at row 112.
"""
import math
import os
import random
from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Art", "Bar")
LEVEL_W = 1152  # three screens

PALETTE = [
    # space
    0x05040e, 0x0d0b22, 0x1c1640, 0x2e2260, 0x8a90b8, 0xe8f0ff,
    # planet
    0x0c3a4a, 0x15625e, 0x2a9c8c, 0x6ad8c0,
    # wall (cool, low contrast)
    0x0b0d15, 0x10131e, 0x161a28, 0x1f2436, 0x2a3046, 0x3a425c,
    # metal
    0x4a5068, 0x6a7290, 0x9aa2bc, 0xd0d6e8,
    # neon pink
    0x3a1030, 0x7a1a5e, 0xd23aa8, 0xff7ae0, 0xffd0f4,
    # cyan
    0x0c3a48, 0x147a90, 0x2ad0f0, 0xa8f4ff,
    # amber
    0x5a3410, 0xb06a1a, 0xf0b040, 0xfff0b0,
    # red
    0x3a0e16, 0x6a1a26, 0xa02a3a, 0xd84a5a,
    # green glass
    0x1a4a2a, 0x2a8a4a, 0x7ad89a,
    # floor
    0x12141e, 0x1a1d2a, 0x232838, 0x2e3448, 0x3c4460,
    # mixed tones the lighting lands on
    0x2a1a3a, 0x3a2a4a, 0x1a3040, 0x24404a, 0x4a3a2a,
]
PAL = [((c >> 16) & 255, (c >> 8) & 255, c & 255) for c in PALETTE]
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]

# 5x7 pixel font, only the letters the signs use
FONT = {
    "O": ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    "R": ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
    "B": ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
    "I": ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
    "T": ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
    "9": ["01110", "10001", "10001", "01111", "00001", "00010", "01100"],
    "L": ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
    "V": ["10001", "10001", "10001", "10001", "01010", "01010", "00100"],
    "E": ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
    " ": ["00000"] * 7,
}


def c3(h):
    return [(h >> 16) & 255, (h >> 8) & 255, h & 255]


def lerp(a, b, t):
    a, b = c3(a), c3(b)
    return [a[i] + (b[i] - a[i]) * t for i in range(3)]


class Canvas:
    """Float RGB buffer (None = transparent); lights are added on top, then snapped to the palette."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = [None] * (w * h)

    def inside(self, x, y):
        return 0 <= x < self.w and 0 <= y < self.h

    def set(self, x, y, c):
        if self.inside(x, y):
            self.px[y * self.w + x] = list(c) if isinstance(c, list) else c3(c)

    def get(self, x, y):
        return self.px[y * self.w + x] if self.inside(x, y) else None

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1):
            for x in range(x0, x1):
                self.set(x, y, c)

    def vgrad(self, x0, y0, x1, y1, top, bottom):
        for y in range(y0, y1):
            col = lerp(top, bottom, (y - y0) / max(1, y1 - y0 - 1))
            for x in range(x0, x1):
                self.set(x, y, col)

    def bevel(self, x0, y0, x1, y1, face, light, dark):
        """Raised panel: lit top/left edges, shadowed bottom/right edges."""
        self.rect(x0, y0, x1, y1, face)
        self.rect(x0, y0, x1, y0 + 1, light)
        self.rect(x0, y0, x0 + 1, y1, light)
        self.rect(x0, y1 - 1, x1, y1, dark)
        self.rect(x1 - 1, y0, x1, y1, dark)

    def light(self, cx, cy, rx, ry, color, strength):
        """Additive elliptical light with a soft quadratic falloff."""
        col = c3(color)
        for y in range(max(0, int(cy - ry)), min(self.h, int(cy + ry) + 1)):
            for x in range(max(0, int(cx - rx)), min(self.w, int(cx + rx) + 1)):
                p = self.px[y * self.w + x]
                if p is None:
                    continue
                d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if d < 1:
                    f = (1 - math.sqrt(d)) ** 2 * strength
                    for i in range(3):
                        p[i] += col[i] * f

    def cone(self, x, y0, y1, spread, color, strength):
        """Spotlight beam widening downwards."""
        col = c3(color)
        for y in range(y0, min(self.h, y1)):
            half = (y - y0) * spread
            fade = 1 - (y - y0) / (y1 - y0)
            for xx in range(int(x - half), int(x + half) + 1):
                p = self.get(xx, y)
                if p is not None:
                    edge = 1 - abs(xx - x) / (half + 1)
                    for i in range(3):
                        p[i] += col[i] * strength * fade * edge

    def save(self, name, spread=26):
        cache = {}
        out = []
        for i, p in enumerate(self.px):
            if p is None:
                out.append((0, 0, 0, 0))
                continue
            x, y = i % self.w, i // self.w
            t = ((BAYER[y % 4][x % 4] + 0.5) / 16 - 0.5) * spread
            key = (int(p[0] + t) >> 2, int(p[1] + t) >> 2, int(p[2] + t) >> 2)
            if key not in cache:
                r, g, b = (k * 4 for k in key)
                cache[key] = min(PAL, key=lambda q: (q[0] - r) ** 2 + (q[1] - g) ** 2 + (q[2] - b) ** 2)
            out.append(cache[key] + (255,))
        im = Image.new("RGBA", (self.w, self.h))
        im.putdata(out)
        im.save(os.path.join(OUT, name + ".png"))
        print("wrote", name + ".png", im.size)


def neon_text(cv, s, x, y, scale, tube, core):
    """Neon letters: each font pixel is a scale x scale tube block with a bright core."""
    for i, ch in enumerate(s):
        for gy, row in enumerate(FONT[ch]):
            for gx, bit in enumerate(row):
                if bit != "1":
                    continue
                bx, by = x + (i * 6 + gx) * scale, y + gy * scale
                cv.rect(bx, by, bx + scale, by + scale, tube)
                cv.set(bx + scale // 2, by + scale // 2, core)


def space():
    """Far layer seen through the window (scrolls at half speed): nebula, stars, planet, moon."""
    cv = Canvas(LEVEL_W, 56)
    rng = random.Random(7)
    for y in range(56):
        for x in range(LEVEL_W):
            n = math.sin(x * 0.021 + y * 0.09) + math.sin(x * 0.007 - y * 0.05) * 1.4
            t = max(0.0, min(1.0, 0.25 + n * 0.18 + y / 120))
            cv.set(x, y, lerp(0x05040e, 0x2e2260, t))
    for _ in range(420):
        x, y = rng.randrange(LEVEL_W), rng.randrange(56)
        cv.set(x, y, 0x8a90b8 if rng.random() < 0.75 else 0xe8f0ff)
    for _ in range(14):  # bright stars with a small cross
        x, y = rng.randrange(2, LEVEL_W - 2), rng.randrange(2, 54)
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            cv.set(x + dx, y + dy, 0xe8f0ff if (dx, dy) == (0, 0) else 0x8a90b8)
    # planet lit from the upper left: shaded sphere, cloud bands, rim light
    px_, py_, r = 700, 78, 62
    for y in range(56):
        for x in range(px_ - r, px_ + r):
            dx, dy = (x - px_) / r, (y - py_) / r
            if dx * dx + dy * dy >= 1:
                continue
            nz = math.sqrt(1 - dx * dx - dy * dy)
            lit = max(0.0, -dx * 0.6 - dy * 0.5 + nz * 0.62)
            band = 0.12 * math.sin((dy + dx * 0.25) * 22)
            ramp = [0x0c3a4a, 0x15625e, 0x2a9c8c, 0x6ad8c0]
            v = max(0.0, min(0.999, lit + band)) * (len(ramp) - 1)
            cv.set(x, y, lerp(ramp[int(v)], ramp[min(3, int(v) + 1)], v - int(v)))
            if dx * dx + dy * dy > 0.9 and dx < 0.1:
                cv.set(x, y, 0x6ad8c0)
    # small moon
    for y in range(8, 24):
        for x in range(420, 436):
            dx, dy = (x - 428) / 8, (y - 16) / 8
            if dx * dx + dy * dy < 1:
                cv.set(x, y, 0xd0d6e8 if dx + dy < -0.3 else 0x9aa2bc if dx + dy < 0.5 else 0x6a7290)
    cv.set(426, 14, 0x6a7290)
    cv.set(431, 19, 0x6a7290)
    cv.save("bar_space")


def back():
    """Back wall 1152x112: door, neon sign, window opening, bottle shelf, holo screen, stage."""
    cv = Canvas(LEVEL_W, 112)
    cv.vgrad(0, 0, LEVEL_W, 70, 0x10131e, 0x1f2436)            # upper wall, darker under the ceiling
    for x in range(0, LEVEL_W, 64):                              # panel seams with a bevel
        cv.rect(x, 6, x + 1, 70, 0x0b0d15)
        cv.rect(x + 1, 6, x + 2, 70, 0x2a3046)
    cv.rect(0, 38, LEVEL_W, 39, 0x0b0d15)
    cv.rect(0, 39, LEVEL_W, 40, 0x2a3046)
    # lower wall: slats, metal rail on top, skirting board with an LED strip
    cv.rect(0, 70, LEVEL_W, 104, 0x161a28)
    for x in range(0, LEVEL_W, 8):
        cv.rect(x, 74, x + 1, 104, 0x1f2436)
    cv.bevel(0, 70, LEVEL_W, 74, 0x6a7290, 0x9aa2bc, 0x4a5068)
    cv.rect(0, 104, LEVEL_W, 112, 0x0b0d15)
    cv.rect(0, 106, LEVEL_W, 107, 0x2ad0f0)
    # ceiling trim with lamps
    cv.rect(0, 0, LEVEL_W, 6, 0x0b0d15)
    lamps = list(range(40, LEVEL_W, 96))
    for x in lamps:
        cv.rect(x - 12, 3, x + 12, 6, 0x4a5068)
        cv.rect(x - 10, 5, x + 10, 6, 0xfff0b0)

    # entrance door
    cv.bevel(26, 20, 98, 104, 0x4a5068, 0x9aa2bc, 0x10131e)
    cv.rect(30, 24, 94, 104, 0x1f2436)
    for x0 in (31, 63):
        cv.bevel(x0, 25, x0 + 30, 104, 0x2a3046, 0x3a425c, 0x10131e)
        cv.rect(x0 + 12, 40, x0 + 18, 80, 0x161a28)
    cv.rect(30, 24, 94, 25, 0x2ad0f0)

    # window opening (transparent: the space layer shows through), framed
    wx0, wy0, wx1, wy1 = 336, 8, 864, 50
    cv.bevel(wx0 - 4, wy0 - 4, wx1 + 4, wy1 + 4, 0x6a7290, 0x9aa2bc, 0x4a5068)
    for y in range(wy0, wy1):
        for x in range(wx0, wx1):
            cv.px[y * LEVEL_W + x] = None
    for x in range(wx0 + 128, wx1, 132):
        cv.bevel(x, wy0, x + 4, wy1, 0x6a7290, 0x9aa2bc, 0x4a5068)

    # bottle shelf above the counter, warm backlight
    cv.rect(320, 52, 880, 64, 0x0b0d15)
    glass = [0x7ad89a, 0xf0b040, 0xd23aa8, 0x2ad0f0, 0xd84a5a]
    rng = random.Random(9)
    for i, x in enumerate(range(326, 874, 9)):
        h = 7 + rng.randint(0, 3)
        top = 63 - h
        col = glass[i % len(glass)]
        cv.rect(x + 3, top, x + 4, top + 3, col)
        cv.rect(x + 1, top + 3, x + 6, 63, col)
        cv.rect(x + 5, top + 3, x + 6, 63, 0x10131e)              # shaded side
        cv.set(x + 2, top + 4, 0xffffff if i % 2 else 0xfff0b0)   # glint
    cv.bevel(318, 63, 882, 66, 0x6a7290, 0x9aa2bc, 0x4a5068)

    # neon sign on a backing board
    cv.bevel(110, 10, 286, 50, 0x10131e, 0x1f2436, 0x0b0d15)
    for bx, by in ((114, 14), (281, 14), (114, 45), (281, 45)):
        cv.set(bx, by, 0x6a7290)
    neon_text(cv, "ORBIT 9", 120, 17, 3, 0xff7ae0, 0xffd0f4)

    # holo screen
    cv.bevel(898, 14, 970, 56, 0x0c3a48, 0x2ad0f0, 0x147a90)
    for y in range(16, 55, 2):
        cv.rect(900, y, 968, y + 1, 0x147a90)
    for x in range(902, 966):
        y = 44 - int(9 * abs(math.sin(x * 0.31) * math.cos(x * 0.07)))
        cv.set(x, y, 0xa8f4ff)

    # stage at the far end: platform, speakers, LIVE sign
    cv.bevel(984, 84, 1148, 104, 0x3a1030, 0xff7ae0, 0x2a1a3a)
    for sx in (990, 1118):
        cv.bevel(sx, 48, sx + 24, 84, 0x10131e, 0x2a3046, 0x0b0d15)
        for cy in (58, 74):
            cv.rect(sx + 7, cy - 4, sx + 17, cy + 4, 0x0b0d15)
            cv.rect(sx + 10, cy - 1, sx + 14, cy + 1, 0x3a425c)
    neon_text(cv, "LIVE", 1042, 26, 2, 0x2ad0f0, 0xa8f4ff)

    # lighting pass
    for x in lamps:
        cv.light(x, 8, 46, 70, 0xb06a1a, 0.22)
    cv.light(198, 30, 120, 60, 0xd23aa8, 0.55)        # sign glow on the wall
    cv.light(600, 58, 300, 18, 0xb06a1a, 0.35)        # shelf backlight
    cv.light(934, 35, 60, 40, 0x2ad0f0, 0.3)          # holo screen
    cv.light(1066, 30, 50, 24, 0x2ad0f0, 0.35)        # LIVE sign
    cv.cone(1030, 6, 104, 0.4, 0xd23aa8, 0.32)        # stage spotlights
    cv.cone(1100, 6, 104, 0.4, 0x2ad0f0, 0.32)
    cv.light(576, 108, 700, 8, 0x2ad0f0, 0.25)        # skirting LED strip
    cv.save("bar_back")


def counter():
    """Bar counter 600x48: bevelled top with lamp glints, slatted front, LED strip."""
    cv = Canvas(600, 48)
    cv.rect(0, 0, 600, 1, 0xd0d6e8)
    cv.rect(0, 1, 600, 4, 0x9aa2bc)
    cv.rect(0, 4, 600, 6, 0x6a7290)
    for gx in range(24, 600, 96):                       # reflections of the ceiling lamps
        cv.rect(gx, 1, gx + 14, 3, 0xd0d6e8)
    cv.rect(0, 6, 600, 8, 0x4a5068)
    for x0 in range(0, 600, 24):
        cv.vgrad(x0, 8, x0 + 24, 40, 0x2a3046, 0x161a28)
        cv.rect(x0, 8, x0 + 1, 40, 0x0b0d15)
        cv.rect(x0 + 1, 8, x0 + 2, 40, 0x3a425c)
    cv.rect(0, 29, 600, 30, 0x2ad0f0)
    cv.rect(0, 30, 600, 31, 0xa8f4ff)
    cv.rect(0, 31, 600, 32, 0x2ad0f0)
    cv.rect(0, 40, 600, 48, 0x0b0d15)
    for x in (0, 1, 598, 599):
        cv.rect(x, 0, x + 1, 48, 0x0b0d15)
    cv.light(300, 31, 320, 10, 0x2ad0f0, 0.35)
    cv.save("bar_counter")


def floor():
    """Floor 1152x128: slanted plates in perspective, rug, light pools and neon reflections."""
    cv = Canvas(LEVEL_W, 128)
    rows = [0]
    k = 0
    while rows[-1] < 128:                                # rows get wider towards the camera
        k += 1
        rows.append(rows[-1] + 8 + k * 3)
    for y in range(128):
        band = max(i for i, r in enumerate(rows) if r <= y)
        for x in range(LEVEL_W):
            sx = (x - y * 0.45) % 64                     # seams slanted into the depth
            col = lerp(0x232838, 0x1a1d2a, y / 128)
            if y in rows:
                col = c3(0x12141e)
            elif y - 1 in rows:
                col = c3(0x2e3448)                       # bevel under each seam
            elif sx < 1:
                col = c3(0x12141e)
            elif sx < 2:
                col = c3(0x2e3448)
            elif band % 2 and int(x - y * 0.45) // 64 % 2:
                col = lerp(0x232838, 0x2e3448, 0.3)      # alternating plate tone
            cv.set(x, y, col)
    # rug in front of the counter with an amber trim
    for y in range(2, 22):
        for x in range(296, 904):
            edge = y in (2, 21) or x in (296, 903)
            inner = y in (4, 19) or x in (299, 900)
            col = 0xb06a1a if edge else 0x6a1a26 if inner else (0x3a0e16 if (x // 3 + y // 3) % 2 else 0x6a1a26)
            cv.set(x, y, col)
    # lights
    cv.light(600, 0, 320, 26, 0x2ad0f0, 0.4)         # counter LED underglow
    cv.light(198, 0, 70, 70, 0xd23aa8, 0.22)         # sign reflection
    for x in range(40, LEVEL_W, 96):
        cv.light(x, 30, 50, 22, 0xb06a1a, 0.12)      # lamp pools
    cv.light(1030, 30, 60, 26, 0xd23aa8, 0.35)       # stage spotlight pools
    cv.light(1100, 30, 60, 26, 0x2ad0f0, 0.35)
    for y in range(96, 128):                         # darken towards the bottom edge to frame the scene
        for x in range(LEVEL_W):
            p = cv.get(x, y)
            f = 1 - (y - 96) / 32 * 0.35
            cv.set(x, y, [v * f for v in p])
    cv.save("bar_floor")


def pillar():
    """Foreground pillar 28x240, drawn in front of the fighters: dark, with a cyan rim on the right."""
    cv = Canvas(28, 240)
    cv.rect(0, 0, 28, 240, 0x0b0d15)
    cv.rect(2, 0, 4, 240, 0x161a28)
    cv.rect(25, 0, 26, 240, 0x147a90)
    cv.rect(26, 0, 27, 240, 0x2ad0f0)
    for y in range(20, 240, 44):
        cv.rect(0, y, 28, y + 2, 0x10131e)
    cv.save("bar_pillar")


def outlined(cv):
    """Dark outline around every opaque pixel cluster (props read clearly against the floor)."""
    edge = []
    for y in range(cv.h):
        for x in range(cv.w):
            if cv.get(x, y) is None and any(cv.get(x + dx, y + dy) is not None for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                edge.append((x, y))
    for x, y in edge:
        cv.set(x, y, 0x0b0d15)


def chair():
    """Chair 22x30: red cushions with 3-step shading, chrome frame."""
    cv = Canvas(22, 30)
    cv.rect(4, 1, 18, 5, 0xa02a3a)
    cv.rect(4, 1, 18, 2, 0xd84a5a)
    cv.rect(4, 4, 18, 5, 0x6a1a26)
    for x in (4, 17):
        cv.rect(x, 5, x + 1, 13, 0x9aa2bc)
    cv.rect(1, 13, 21, 17, 0xa02a3a)
    cv.rect(1, 13, 21, 14, 0xd84a5a)
    cv.rect(1, 16, 21, 17, 0x6a1a26)
    for x in (3, 18):
        cv.rect(x, 17, x + 1, 29, 0x9aa2bc)
        cv.rect(x + 1, 17, x + 2, 29, 0x4a5068)
    cv.rect(3, 23, 19, 24, 0x6a7290)
    outlined(cv)
    cv.save("chair", spread=0)


def stool():
    """Bar stool 16x28."""
    cv = Canvas(16, 28)
    cv.rect(1, 1, 15, 5, 0xa02a3a)
    cv.rect(1, 1, 15, 2, 0xd84a5a)
    cv.rect(2, 5, 14, 6, 0x6a1a26)
    cv.rect(7, 6, 8, 23, 0x9aa2bc)
    cv.rect(8, 6, 9, 23, 0x4a5068)
    cv.rect(4, 15, 12, 16, 0x6a7290)
    cv.rect(3, 23, 13, 27, 0x4a5068)
    cv.rect(3, 23, 13, 24, 0x9aa2bc)
    outlined(cv)
    cv.save("stool", spread=0)


def bottle():
    """Bottle 8x18: green glass, highlight, amber label, cork."""
    cv = Canvas(8, 18)
    cv.rect(3, 1, 5, 2, 0xb06a1a)
    cv.rect(3, 2, 5, 6, 0x2a8a4a)
    cv.rect(1, 6, 7, 17, 0x2a8a4a)
    cv.rect(6, 6, 7, 17, 0x1a4a2a)
    cv.rect(2, 7, 3, 15, 0x7ad89a)
    cv.rect(1, 10, 7, 13, 0xf0b040)
    cv.rect(6, 10, 7, 13, 0xb06a1a)
    outlined(cv)
    cv.save("bottle", spread=0)


def table():
    """Round table 42x32: metal top with a cyan rim light, pedestal, base."""
    cv = Canvas(42, 32)
    for y in range(1, 8):
        for x in range(1, 41):
            dx, dy = (x - 20.5) / 20, (y - 4) / 3.4
            if dx * dx + dy * dy < 1:
                cv.set(x, y, 0xd0d6e8 if dy < -0.4 else 0x9aa2bc if dy < 0.3 else 0x6a7290)
    cv.rect(2, 7, 40, 8, 0x2ad0f0)
    cv.rect(19, 8, 21, 27, 0x6a7290)
    cv.rect(21, 8, 23, 27, 0x4a5068)
    for y in range(27, 31):
        for x in range(8, 34):
            dx, dy = (x - 20.5) / 12.5, (y - 29) / 2
            if dx * dx + dy * dy < 1:
                cv.set(x, y, 0x4a5068 if dy < 0 else 0x2a3046)
    outlined(cv)
    cv.save("table", spread=0)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for draw in (space, back, counter, floor, pillar, chair, stool, bottle, table):
        draw()

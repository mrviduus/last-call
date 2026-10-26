"""OLD LAMP pub: warm pixel-art layers and props (our own design) -> Assets/Resources/Art/Bar.

Run:  python3 Tools/make_bar_art.py   (needs Pillow: python3 -m pip install pillow)
The PNGs are the real assets; redraw them by hand freely, this script is just the first pass.

Art rules used here:
- one limited warm palette (PALETTE): every pixel is snapped to it, so all layers match;
- light sources (pendant lamps, wall lamps, the BEER neon, windows) are painted additively, then
  ordered (Bayer) dithering turns the soft gradients into crisp pixel steps;
- light comes from the top-left: highlights top/left, shadows bottom/right, dark outlines on props;
- the background stays lower in contrast than the fighters so the fight reads clearly.
Coordinates are pixels, y from the top. Screen 384x240; the floor (walkable) starts at row 112.
Layout the game relies on: counter x 300-900; pool tables at x 620 and 1010 (floor light pools under them).
"""
import math
import os
import random
from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Art", "Bar")
LEVEL_W = 1152  # three screens
POOL_TABLES = [(620, -82), (1010, -60)]  # (x, depth) as placed by Game.SpawnBarProps

PALETTE = [
    # night outside
    0x0a0c1c, 0x141a33, 0x22284a, 0x3a3f66, 0x1a1428,
    # brick
    0x2a140f, 0x3a1c16, 0x5a2a1e, 0x7a3a26, 0x9a4e30, 0xb8653c,
    # wood, dark to light
    0x1a0e08, 0x2a160c, 0x3e2212, 0x5a3218, 0x7a4622, 0x9a5e2e, 0xc08040, 0xd8a060, 0xf0c890,
    # brass and gold
    0x4a3010, 0x6a4a14, 0xa87a24, 0xe0b040, 0xfff0b0,
    # warm light
    0xffd27a, 0xffb347, 0xffe9c0,
    # beer neon
    0x5a140c, 0xa0301a, 0xff6a2a, 0xffc890,
    # felt and lamp-shade green
    0x0a2a18, 0x0e3a22, 0x17582f, 0x22783e, 0x3a9a58,
    # red leather and rug
    0x3a0e10, 0x6a1a1c, 0xa02a28, 0xd04a3a,
    # paper, chalk, cream
    0xc8b48a, 0xead8b0, 0xfff4dc,
    # dark and metal
    0x0c0806, 0x16100c, 0x241a12, 0x4a4a50, 0x8a8a90, 0xc8c8cc,
    # glass
    0x4a2a10, 0x8a5a20, 0x2a5a2a, 0x6aa05a,
]
PAL = [((c >> 16) & 255, (c >> 8) & 255, c & 255) for c in PALETTE]
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]

# 5x7 pixel font, only the letters the signs use
FONT = {
    "O": ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    "L": ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
    "D": ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
    "A": ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
    "M": ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
    "P": ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
    "B": ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
    "E": ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
    "R": ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
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

    def disc(self, cx, cy, r, color):
        for y in range(int(cy - r), int(cy + r) + 1):
            for x in range(int(cx - r), int(cx + r) + 1):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                    self.set(x, y, color)

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
        """Light beam widening downwards."""
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


def letters(cv, s, x, y, scale, color, shade=None):
    """Pixel-font text; an optional shade one pixel down-right gives painted or carved letters some depth."""
    for i, ch in enumerate(s):
        for gy, row in enumerate(FONT[ch]):
            for gx, bit in enumerate(row):
                if bit != "1":
                    continue
                bx, by = x + (i * 6 + gx) * scale, y + gy * scale
                if shade is not None:
                    cv.rect(bx + 1, by + 1, bx + scale + 1, by + scale + 1, shade)
                cv.rect(bx, by, bx + scale, by + scale, color)


def view():
    """Far layer seen through the windows (scrolls at half speed): night sky and a warm-lit city."""
    cv = Canvas(LEVEL_W, 48)
    rng = random.Random(5)
    cv.vgrad(0, 0, LEVEL_W, 48, 0x0a0c1c, 0x3a3f66)
    for _ in range(160):
        cv.set(rng.randrange(LEVEL_W), rng.randrange(20), 0xc8c8cc)
    cv.disc(250, 10, 5, 0xfff4dc)
    cv.disc(252, 9, 5, 0xead8b0)
    x = 0
    while x < LEVEL_W:                                     # rooftops of different heights
        w, h = rng.randint(18, 40), rng.randint(14, 34)
        cv.rect(x, 48 - h, x + w, 48, 0x141a33)
        for wy in range(48 - h + 3, 46, 5):
            for wx in range(x + 3, x + w - 3, 5):
                if rng.random() < 0.35:
                    cv.rect(wx, wy, wx + 2, wy + 2, 0xffd27a if rng.random() < 0.7 else 0xffb347)
        x += w + rng.randint(0, 4)
    cv.save("bar_view")


def back():
    """Back wall 1152x112: brick above wood panelling, door, pub sign, windows, BEER neon,
    back-bar shelves and taps, pendant lamps over the pool tables, darts, chalkboard, clock, cue rack."""
    cv = Canvas(LEVEL_W, 112)
    rng = random.Random(11)
    # brick wall
    for y in range(6, 66):
        row = (y - 6) // 8
        for x in range(LEVEL_W):
            bx = x + (row % 2) * 8
            if (y - 6) % 8 == 7 or bx % 16 == 15:
                col = c3(0x2a140f)                                        # mortar
            else:
                tone = (bx // 16 * 7 + row * 3) % 5
                col = c3([0x5a2a1e, 0x7a3a26, 0x7a3a26, 0x9a4e30, 0x5a2a1e][tone])
                if (y - 6) % 8 == 0:
                    col = lerp(0x9a4e30, 0xb8653c, 0.3)                   # lit top edge of each brick
            cv.set(x, y, col)
    # ceiling beam
    cv.rect(0, 0, LEVEL_W, 6, 0x2a160c)
    cv.rect(0, 5, LEVEL_W, 6, 0x1a0e08)
    cv.rect(0, 0, LEVEL_W, 1, 0x5a3218)
    # wood panelling below, with a cap rail and skirting
    for x in range(LEVEL_W):
        plank = x // 12
        for y in range(70, 104):
            col = 0x3e2212 if plank % 2 else 0x5a3218
            if x % 12 == 0:
                col = 0x1a0e08
            elif x % 12 == 1:
                col = 0x7a4622
            elif (x * 7 + y * 3 + plank * 13) % 29 == 0:
                col = 0x2a160c                                            # grain
            cv.set(x, y, col)
    cv.bevel(0, 66, LEVEL_W, 70, 0x9a5e2e, 0xd8a060, 0x3e2212)
    cv.rect(0, 104, LEVEL_W, 112, 0x1a0e08)
    cv.rect(0, 104, LEVEL_W, 105, 0x5a3218)

    # entrance door with a lit window
    cv.bevel(22, 18, 98, 104, 0x3e2212, 0x7a4622, 0x1a0e08)
    cv.bevel(28, 24, 92, 104, 0x5a3218, 0x9a5e2e, 0x2a160c)
    cv.rect(44, 30, 76, 50, 0xffd27a)
    cv.rect(59, 30, 61, 50, 0x3e2212)
    cv.rect(44, 39, 76, 41, 0x3e2212)
    for y0 in (56, 78):
        cv.bevel(36, y0, 84, y0 + 18, 0x5a3218, 0x7a4622, 0x2a160c)
    cv.disc(82, 66, 2, 0xe0b040)

    # pub sign: wooden board, gold carved letters
    cv.bevel(112, 8, 280, 40, 0x3e2212, 0x7a4622, 0x1a0e08)
    cv.rect(116, 12, 276, 36, 0x2a160c)
    letters(cv, "OLD LAMP", 125, 14, 3, 0xe0b040, shade=0x4a3010)
    for bx in (116, 275):
        cv.rect(bx, 0, bx + 1, 8, 0x4a4a50)                              # hanging chains

    # windows onto the street (transparent: the view layer shows through), wooden frames
    for wx0, wx1 in ((330, 498), (702, 870)):
        cv.bevel(wx0 - 4, 4, wx1 + 4, 44, 0x5a3218, 0x9a5e2e, 0x2a160c)
        for y in range(8, 40):
            for x in range(wx0, wx1):
                cv.px[y * LEVEL_W + x] = None
        for x in range(wx0 + 40, wx1, 42):
            cv.rect(x, 8, x + 3, 40, 0x3e2212)
        cv.rect(wx0, 23, wx1, 25, 0x3e2212)
    # BEER neon between the windows
    cv.rect(548, 10, 652, 34, 0x16100c)
    letters(cv, "BEER", 557, 13, 3, 0xff6a2a)
    for i, ch in enumerate("BEER"):  # bright tube cores
        for gy, row in enumerate(FONT[ch]):
            for gx, bit in enumerate(row):
                if bit == "1":
                    cv.set(557 + (i * 6 + gx) * 3 + 1, 13 + gy * 3 + 1, 0xffc890)

    # back bar: shelves of bottles over a mirror strip, beer taps on the counter
    cv.rect(320, 44, 880, 64, 0x16100c)
    cv.rect(320, 52, 880, 54, 0x5a3218)
    cv.rect(320, 62, 880, 64, 0x5a3218)
    glass = [0x8a5a20, 0x2a5a2a, 0x4a2a10, 0x6aa05a, 0xa0301a]
    for shelf_top in (44, 54):
        for i, x in enumerate(range(326, 874, 8)):
            h = 5 + rng.randint(0, 3)
            col = glass[(i + shelf_top) % len(glass)]
            bottom = shelf_top + 8
            cv.rect(x + 3, bottom - h, x + 4, bottom - h + 2, col)
            cv.rect(x + 1, bottom - h + 2, x + 6, bottom, col)
            cv.set(x + 2, bottom - h + 3, 0xffe9c0)
    for tx in (480, 500, 520):
        cv.rect(tx, 54, tx + 4, 64, 0xa87a24)
        cv.rect(tx, 54, tx + 1, 64, 0xe0b040)
        cv.rect(tx - 1, 50, tx + 5, 54, 0x16100c)

    # pool corner: dartboard, chalkboard, clock, cue rack (the lamps over the tables are their own sprite)
    cv.disc(940, 26, 12, 0x16100c)
    for r, col in ((11, 0xead8b0), (9, 0x6a1a1c), (7, 0xead8b0), (5, 0x17582f), (3, 0xead8b0), (1, 0xa02a28)):
        cv.disc(940, 26, r, col)
    cv.bevel(970, 12, 1050, 52, 0x5a3218, 0x9a5e2e, 0x2a160c)
    cv.rect(974, 16, 1046, 48, 0x241a12)
    for i, y in enumerate(range(20, 46, 6)):                              # chalk scribbles
        for x in range(978, 978 + 20 + (i * 13) % 40):
            if (x + i) % 4 != 3:
                cv.set(x, y, 0xead8b0 if i else 0xfff4dc)
    cv.disc(1090, 24, 10, 0x2a160c)
    cv.disc(1090, 24, 8, 0xfff4dc)
    cv.rect(1090, 18, 1091, 25, 0x16100c)
    cv.rect(1090, 24, 1095, 25, 0x16100c)
    cv.bevel(1112, 46, 1142, 100, 0x3e2212, 0x7a4622, 0x1a0e08)
    for cx in range(1117, 1140, 6):
        cv.rect(cx, 48, cx + 2, 98, 0xc08040)
        cv.rect(cx, 48, cx + 2, 51, 0x22783e)

    # lighting pass
    cv.light(196, 24, 110, 40, 0xffb347, 0.25)                          # warm wash on the sign
    cv.light(600, 22, 90, 40, 0xff6a2a, 0.45)                           # BEER neon glow
    cv.light(600, 58, 300, 16, 0xffd27a, 0.3)                           # back-bar lights
    cv.light(60, 40, 40, 30, 0xffd27a, 0.3)                             # door window
    for x in range(40, LEVEL_W, 160):
        cv.light(x, 66, 70, 40, 0xffb347, 0.12)                         # general warm ambience
    cv.save("bar_back")


def counter():
    """Counter 600x48: polished wood top, raised wooden panels, brass foot rail."""
    cv = Canvas(600, 48)
    cv.rect(0, 0, 600, 1, 0xf0c890)
    cv.rect(0, 1, 600, 4, 0xc08040)
    cv.rect(0, 4, 600, 6, 0x7a4622)
    for gx in range(30, 600, 120):
        cv.rect(gx, 1, gx + 18, 2, 0xf0c890)                             # lamp reflections on the varnish
    cv.rect(0, 6, 600, 8, 0x2a160c)
    for x0 in range(0, 600, 50):
        cv.rect(x0, 8, x0 + 50, 40, 0x3e2212)
        cv.bevel(x0 + 6, 12, x0 + 44, 34, 0x5a3218, 0x9a5e2e, 0x1a0e08)
    cv.rect(0, 40, 600, 48, 0x1a0e08)
    cv.rect(0, 37, 600, 39, 0xa87a24)                                    # brass foot rail
    cv.rect(0, 37, 600, 38, 0xe0b040)
    for bx in range(20, 600, 100):
        cv.rect(bx, 39, bx + 2, 44, 0x6a4a14)
    for x in (0, 1, 598, 599):
        cv.rect(x, 0, x + 1, 48, 0x1a0e08)
    cv.light(300, 0, 320, 14, 0xffd27a, 0.25)
    cv.save("bar_counter")


def floor():
    """Floor 1152x128: wooden boards in perspective, a rug at the counter, warm pools under the lamps."""
    cv = Canvas(LEVEL_W, 128)
    rows = [0]
    k = 0
    while rows[-1] < 128:                                # boards get wider towards the camera
        k += 1
        rows.append(rows[-1] + 6 + k * 2)
    for y in range(128):
        band = max(i for i, r in enumerate(rows) if r <= y)
        board_len = 48 + band * 10
        for x in range(LEVEL_W):
            off = (band * 29) % board_len
            col = lerp(0x5a3218, 0x3e2212, y / 128)
            if band % 3 == 1:
                col = lerp(0x7a4622, 0x5a3218, y / 128)
            if y in rows:
                col = c3(0x1a0e08)
            elif y - 1 in rows:
                col = lerp(0x9a5e2e, 0x7a4622, y / 128)                   # lit edge of each board
            elif (x + off) % board_len == 0:
                col = c3(0x1a0e08)                                         # board ends
            elif (x * 3 + y * 11 + band * 7) % 37 == 0:
                col = c3(0x2a160c)                                         # grain
            cv.set(x, y, col)
    # rug at the counter: deep red with a gold trim
    for y in range(2, 22):
        for x in range(296, 904):
            edge = y in (2, 21) or x in (296, 903)
            inner = y in (4, 19) or x in (299, 900)
            col = 0xe0b040 if edge else 0xa87a24 if inner else (0x6a1a1c if (x // 4 + y // 4) % 2 else 0xa02a28)
            cv.set(x, y, col)
    # light
    for px_, depth in POOL_TABLES:
        cv.light(px_, 8 - depth, 90, 34, 0xffb347, 0.45)               # pool lamp pools (floor row = 8 - depth)
    cv.light(600, 0, 320, 22, 0xffd27a, 0.22)
    cv.light(60, 6, 50, 20, 0xffd27a, 0.2)
    for y in range(96, 128):                                           # darker towards the bottom edge
        for x in range(LEVEL_W):
            p = cv.get(x, y)
            f = 1 - (y - 96) / 32 * 0.35
            cv.set(x, y, [v * f for v in p])
    cv.save("bar_floor")


def pillar():
    """Foreground wooden post 28x240 with brass rings, drawn in front of the fighters."""
    cv = Canvas(28, 240)
    for x in range(28):
        col = 0x241a12 if x < 4 else 0x3e2212 if x < 22 else 0x1a0e08
        cv.rect(x, 0, x + 1, 240, col)
    cv.rect(4, 0, 6, 240, 0x5a3218)
    for y in (30, 120, 210):
        cv.rect(0, y, 28, y + 4, 0xa87a24)
        cv.rect(0, y, 28, y + 1, 0xe0b040)
    cv.save("bar_pillar")


def pool_lamp():
    """Lamp hanging over a pool table, 40x90: cord from the ceiling, green shade, warm light below it.
    Its bottom edge is the shade's rim; the game hangs it a fixed height above the felt."""
    cv = Canvas(40, 90)
    cv.rect(19, 0, 21, 72, 0x16100c)                                     # cord
    for y in range(72, 84):
        half = 5 + (y - 72)
        cv.rect(20 - half, y, 20 + half, y + 1, 0x17582f if y < 82 else 0x0e3a22)
        cv.set(20 - half, y, 0x3a9a58)                                   # lit left edge of the shade
    cv.rect(2, 84, 38, 86, 0x22783e)
    cv.rect(2, 84, 38, 85, 0x3a9a58)
    cv.rect(12, 86, 28, 88, 0xffe9c0)                                    # bulbs glowing under the rim
    cv.rect(16, 88, 24, 89, 0xffd27a)
    cv.save("pool_lamp", spread=0)


def outlined(cv):
    """Dark outline around every opaque pixel cluster (props read clearly against the floor)."""
    edge = []
    for y in range(cv.h):
        for x in range(cv.w):
            if cv.get(x, y) is None and any(cv.get(x + dx, y + dy) is not None for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                edge.append((x, y))
    for x, y in edge:
        cv.set(x, y, 0x0c0806)


def chair():
    """Wooden chair 22x30 with a green seat cushion."""
    cv = Canvas(22, 30)
    cv.rect(4, 1, 18, 4, 0x9a5e2e)
    cv.rect(4, 1, 18, 2, 0xc08040)
    cv.rect(4, 6, 18, 8, 0x7a4622)
    for x in (4, 17):
        cv.rect(x, 1, x + 1, 14, 0x7a4622)
    cv.rect(1, 13, 21, 16, 0x17582f)
    cv.rect(1, 13, 21, 14, 0x3a9a58)
    cv.rect(1, 16, 21, 17, 0x5a3218)
    for x in (3, 18):
        cv.rect(x, 17, x + 1, 29, 0x7a4622)
        cv.rect(x + 1, 17, x + 2, 29, 0x3e2212)
    cv.rect(3, 23, 19, 24, 0x5a3218)
    outlined(cv)
    cv.save("chair", spread=0)


def stool():
    """Bar stool 16x28: red leather seat, wooden legs, brass footrest."""
    cv = Canvas(16, 28)
    cv.rect(1, 1, 15, 5, 0xa02a28)
    cv.rect(1, 1, 15, 2, 0xd04a3a)
    cv.rect(2, 5, 14, 6, 0x6a1a1c)
    for x in (3, 11):
        cv.rect(x, 6, x + 2, 27, 0x7a4622)
        cv.rect(x + 1, 6, x + 2, 27, 0x3e2212)
    cv.rect(3, 16, 13, 17, 0xa87a24)
    outlined(cv)
    cv.save("stool", spread=0)


def bottle():
    """Brown beer bottle 8x18 with a cream label."""
    cv = Canvas(8, 18)
    cv.rect(3, 1, 5, 2, 0xa87a24)
    cv.rect(3, 2, 5, 6, 0x4a2a10)
    cv.rect(1, 6, 7, 17, 0x4a2a10)
    cv.rect(6, 6, 7, 17, 0x241a12)
    cv.rect(2, 7, 3, 15, 0x8a5a20)
    cv.rect(1, 10, 7, 13, 0xead8b0)
    cv.rect(6, 10, 7, 13, 0xc8b48a)
    outlined(cv)
    cv.save("bottle", spread=0)


def table():
    """Round wooden pub table 42x32."""
    cv = Canvas(42, 32)
    for y in range(1, 8):
        for x in range(1, 41):
            dx, dy = (x - 20.5) / 20, (y - 4) / 3.4
            if dx * dx + dy * dy < 1:
                cv.set(x, y, 0xc08040 if dy < -0.4 else 0x9a5e2e if dy < 0.3 else 0x7a4622)
    cv.rect(2, 7, 40, 8, 0x3e2212)
    cv.rect(19, 8, 21, 27, 0x7a4622)
    cv.rect(21, 8, 23, 27, 0x3e2212)
    for y in range(27, 31):
        for x in range(8, 34):
            dx, dy = (x - 20.5) / 12.5, (y - 29) / 2
            if dx * dx + dy * dy < 1:
                cv.set(x, y, 0x5a3218 if dy < 0 else 0x2a160c)
    outlined(cv)
    cv.save("table", spread=0)


def table_flipped():
    """The pub table knocked onto its side, 34x32."""
    cv = Canvas(34, 32)
    for y in range(1, 31):
        for x in range(1, 12):
            dx, dy = (x - 6) / 5, (y - 16) / 14.5
            if dx * dx + dy * dy < 1:
                cv.set(x, y, 0xc08040 if dx < -0.4 else 0x9a5e2e if dx < 0.3 else 0x7a4622)
    cv.rect(11, 2, 12, 30, 0x3e2212)
    cv.rect(12, 14, 28, 16, 0x7a4622)
    cv.rect(12, 16, 28, 18, 0x3e2212)
    for y in range(8, 26):
        for x in range(27, 33):
            dx, dy = (x - 29.5) / 3, (y - 17) / 9
            if dx * dx + dy * dy < 1:
                cv.set(x, y, 0x5a3218 if dx < 0 else 0x2a160c)
    outlined(cv)
    cv.save("table_flipped", spread=0)


def pool_table():
    """Pool table 108x40 seen from the front and a little above: wooden rails, green felt with pockets,
    a dark wooden apron and turned legs. Its felt is 28-30 px above the floor (balls rest there)."""
    cv = Canvas(108, 40)
    cv.rect(0, 0, 108, 20, 0x5a3218)                                     # rail frame
    cv.rect(0, 0, 108, 1, 0x9a5e2e)
    for y in range(3, 17):                                               # felt, lit from the lamp above
        for x in range(4, 104):
            d = abs(x - 54) / 54
            cv.set(x, y, lerp(0x3a9a58, 0x17582f, min(1.0, d * 1.2 + (y - 3) / 30)))
    for px_ in (4, 54, 103):                                             # pockets
        cv.rect(px_ - 2, 2, px_ + 2, 5, 0x0c0806)
        cv.rect(px_ - 2, 15, px_ + 2, 18, 0x0c0806)
    cv.rect(0, 17, 108, 20, 0x7a4622)                                    # front rail
    cv.rect(0, 17, 108, 18, 0xc08040)
    for x in (27, 81):                                                   # diamond sights
        cv.set(x, 18, 0xead8b0)
    cv.rect(2, 20, 106, 29, 0x3e2212)                                    # apron
    cv.rect(2, 20, 106, 21, 0x5a3218)
    cv.rect(46, 22, 62, 27, 0x2a160c)
    for lx in (6, 50, 96):                                               # turned legs
        cv.rect(lx, 29, lx + 6, 39, 0x5a3218)
        cv.rect(lx, 33, lx + 6, 35, 0x7a4622)
        cv.rect(lx + 4, 29, lx + 6, 39, 0x2a160c)
    outlined(cv)
    cv.save("pool_table", spread=0)


def cue():
    """Pool cue 60x3: chalked tip, light shaft, brass joint, dark butt."""
    cv = Canvas(60, 3)
    cv.rect(0, 1, 58, 2, 0xf0c890)
    cv.rect(58, 1, 60, 2, 0x3a3f66)
    cv.rect(0, 0, 22, 3, 0x2a160c)
    cv.rect(0, 0, 22, 1, 0x5a3218)
    cv.rect(22, 0, 24, 3, 0xe0b040)
    cv.rect(24, 1, 58, 2, 0xd8a060)
    cv.save("cue", spread=0)


def ball():
    """Pool ball 6x6, white with shading; the game tints it per ball."""
    cv = Canvas(6, 6)
    for y in range(6):
        for x in range(6):
            dx, dy = x - 2.5, y - 2.5
            if dx * dx + dy * dy <= 7:
                cv.set(x, y, 0xfff4dc if dx + dy < -1.5 else 0xc8c8cc if dx + dy < 1.5 else 0x8a8a90)
    cv.save("ball", spread=0)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for draw in (view, back, counter, floor, pillar, chair, stool, bottle, table, table_flipped, pool_table, pool_lamp, cue, ball):
        draw()

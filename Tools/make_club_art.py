"""OLD LAMP as a strip club: neon layers (our own design) -> Assets/Resources/Art/Club.

Run from Game/:  /tmp/venv/bin/python Tools/make_club_art.py   (needs Pillow)
Same layers, sizes and layout as the pub (Tools/make_bar_art.py, whose Canvas this reuses) so the game logic
doesn't change: counter x 300-900, pool tables at x 620 and 1010. The stage with the poles is the back wall's
right end (x 920-1150), where nothing stands in front of it; the dancers on it are sprites (Game.BuildBar).
Props (tables, chairs, bottles, pool tables) stay the pub's; only the lamps are redrawn.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(__file__))
import make_bar_art as bar  # noqa: E402
from make_bar_art import Canvas, LEVEL_W, POOL_TABLES, c3, lerp  # noqa: E402

bar.OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Art", "Club")
PALETTE = [
    # dark, purple
    0x000000, 0x07050c, 0x0e0a18, 0x161026, 0x221838, 0x2e2048, 0x3c2a5c, 0x4a2a6a, 0x5e3a80, 0x7a4ea0,
    # pink neon
    0x3a0828, 0x5a1040, 0xa01a6a, 0xff2a9a, 0xff7ac8, 0xffd0ec,
    # cyan neon
    0x0a2a3a, 0x147a8a, 0x2ae0ff, 0xb0f4ff,
    # chrome
    0x2a2a34, 0x4a4a58, 0x8a8a98, 0xc8c8d4, 0xffffff,
    # gold
    0x6a4a14, 0xa87a24, 0xe0b040, 0xfff0b0,
    # red velvet
    0x2a0610, 0x4a0a1c, 0x6a1028, 0xa01a3a,
    # bottles
    0x8a5a20, 0x2a5a2a, 0x6aa05a, 0xa0301a, 0x4a2a10, 0xffd27a,
]
bar.PAL = [((c >> 16) & 255, (c >> 8) & 255, c & 255) for c in PALETTE]
bar.FONT.update({
    "I": ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
    "N": ["10001", "11001", "10101", "10101", "10011", "10001", "10001"],
    "K": ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
    "X": ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
    "T": ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
    "G": ["01110", "10001", "10000", "10111", "10001", "10001", "01111"],
    "S": ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
    "H": ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
    "W": ["10001", "10001", "10001", "10101", "10101", "11011", "10001"],
})
STAGE_X0, STAGE_X1, STAGE_TOP = 920, 1150, 88
POLES = (985, 1085)


def neon(cv, s, x, y, scale, tube, core):
    """Neon letters: coloured tube with a bright core pixel in each cell."""
    bar.letters(cv, s, x, y, scale, tube)
    for i, ch in enumerate(s):
        for gy, row in enumerate(bar.FONT[ch]):
            for gx, bit in enumerate(row):
                if bit == "1":
                    cv.set(x + (i * 6 + gx) * scale + scale // 2, y + gy * scale + scale // 2, core)


def view():
    """No windows in a club: a plain dark layer (the wall covers it)."""
    cv = Canvas(LEVEL_W, 48)
    cv.rect(0, 0, LEVEL_W, 48, 0x07050c)
    cv.save("bar_view")


def back():
    """Back wall 1152x112: tufted purple padding, neon OLD LAMP sign, mirror panels, back bar,
    the stage with poles and a red curtain at the right end."""
    cv = Canvas(LEVEL_W, 112)
    rng = random.Random(7)
    # tufted padding: diamonds with a button at each corner, lit from the top-left
    for y in range(6, 104):
        for x in range(LEVEL_W):
            u, v = (x + y) % 16, (x - y) % 16
            col = lerp(0x2e2048, 0x161026, (u + v) / 32)
            if u == 0 or v == 0:
                col = c3(0x0e0a18)                                           # seams
            if u == 0 and v == 0:
                col = c3(0x5e3a80)                                           # buttons
            cv.set(x, y, col)
    cv.rect(0, 0, LEVEL_W, 6, 0x07050c)                                      # ceiling with a chrome truss
    for x in range(0, LEVEL_W, 8):
        cv.set(x, 2, 0x4a4a58)
        cv.set(x + 4, 4, 0x4a4a58)
    cv.rect(0, 5, LEVEL_W, 6, 0x2a2a34)
    cv.rect(0, 104, LEVEL_W, 112, 0x07050c)                                  # skirting with a pink LED strip
    cv.rect(0, 103, LEVEL_W, 104, 0xff2a9a)

    # entrance door: black, chrome frame, cyan EXIT sign
    cv.bevel(22, 18, 98, 104, 0x0e0a18, 0x4a4a58, 0x000000)
    cv.bevel(28, 24, 92, 104, 0x161026, 0x2e2048, 0x07050c)
    cv.disc(82, 66, 2, 0xc8c8d4)
    cv.rect(44, 8, 76, 16, 0x07050c)
    neon(cv, "EXIT", 47, 9, 1, 0x2ae0ff, 0xb0f4ff)

    # sign: OLD LAMP in pink neon on a black board, a cyan underline
    cv.rect(112, 8, 280, 40, 0x07050c)
    neon(cv, "OLD LAMP", 125, 13, 3, 0xff2a9a, 0xffd0ec)
    cv.rect(124, 36, 268, 37, 0x2ae0ff)

    # where the pub had windows: mirror panels in cyan neon frames
    for wx0, wx1 in ((330, 498), (702, 870)):
        cv.rect(wx0 - 2, 6, wx1 + 2, 44, 0x2ae0ff)
        cv.vgrad(wx0, 8, wx1, 42, 0x221838, 0x0e0a18)
        for x in range(wx0 + 6, wx1, 23):                                    # streaks on the glass
            for k in range(12):
                cv.set(x + k, 10 + k * 2, 0x3c2a5c)
    cv.rect(548, 10, 652, 34, 0x07050c)                                      # BEER neon, cyan now
    neon(cv, "BEER", 557, 13, 3, 0x2ae0ff, 0xb0f4ff)

    # back bar: black shelves of bottles, pink LED under each shelf
    cv.rect(320, 44, 880, 64, 0x07050c)
    cv.rect(320, 52, 880, 54, 0x2a2a34)
    cv.rect(320, 62, 880, 64, 0x2a2a34)
    cv.rect(320, 54, 880, 55, 0xff2a9a)
    glass = [0x8a5a20, 0x2a5a2a, 0x4a2a10, 0x6aa05a, 0xa0301a]
    for shelf_top in (44, 54):
        for i, x in enumerate(range(326, 874, 8)):
            h = 5 + rng.randint(0, 3)
            col = glass[(i + shelf_top) % len(glass)]
            bottom = shelf_top + 8
            cv.rect(x + 3, bottom - h, x + 4, bottom - h + 2, col)
            cv.rect(x + 1, bottom - h + 2, x + 6, bottom, col)
            cv.set(x + 2, bottom - h + 3, 0xffd0ec)

    # the stage: red velvet curtain in folds, a raised platform with bulbs, two chrome poles
    for x in range(STAGE_X0, STAGE_X1):
        fold = (x - STAGE_X0) % 12
        for y in range(6, STAGE_TOP):
            col = lerp(0xa01a3a, 0x4a0a1c, abs(fold - 6) / 6)
            if fold == 0:
                col = c3(0x2a0610)
            cv.set(x, y, [v * (1 - (y - 6) / 200) for v in col])                # darker towards the floor
    cv.rect(STAGE_X0 - 4, 6, STAGE_X1 + 4, 12, 0x6a1028)                      # pelmet with gold fringe
    for x in range(STAGE_X0 - 4, STAGE_X1 + 4, 2):
        cv.set(x, 12, 0xe0b040)
    cv.bevel(STAGE_X0 - 6, STAGE_TOP, STAGE_X1 + 6, 104, 0x161026, 0x4a4a58, 0x000000)
    cv.rect(STAGE_X0 - 6, STAGE_TOP, STAGE_X1 + 6, STAGE_TOP + 2, 0xc8c8d4)    # chrome edge
    for x in range(STAGE_X0, STAGE_X1, 8):                                    # bulbs along the front
        cv.set(x, STAGE_TOP + 8, 0xfff0b0)
    for px_ in POLES:
        cv.rect(px_ - 1, 6, px_ + 2, STAGE_TOP, 0x8a8a98)
        cv.rect(px_ - 1, 6, px_, STAGE_TOP, 0xffffff)
        cv.rect(px_ + 1, 6, px_ + 2, STAGE_TOP, 0x4a4a58)
        cv.rect(px_ - 4, STAGE_TOP - 1, px_ + 5, STAGE_TOP + 1, 0xc8c8d4)    # base plate

    # lighting: neon glows, spotlights on the stage, a cold wash over the bar
    cv.light(196, 24, 110, 40, 0xff2a9a, 0.35)
    cv.light(600, 22, 90, 40, 0x2ae0ff, 0.3)
    cv.light(600, 58, 300, 14, 0xff2a9a, 0.2)
    cv.light(60, 12, 40, 20, 0x2ae0ff, 0.3)
    for px_, col in zip(POLES, (0xff2a9a, 0x2ae0ff)):
        cv.cone(px_, 6, STAGE_TOP, 0.4, col, 0.35)
    for x in range(40, LEVEL_W, 160):
        cv.light(x, 100, 70, 14, 0xff2a9a, 0.15)                             # LED strip glow on the wall
    cv.save("bar_back")


def counter():
    """Counter 600x48: black lacquer top with a chrome edge, tufted purple front, pink LED under the top."""
    cv = Canvas(600, 48)
    cv.rect(0, 0, 600, 1, 0xc8c8d4)
    cv.rect(0, 1, 600, 4, 0x161026)
    cv.rect(0, 4, 600, 6, 0x0e0a18)
    for gx in range(30, 600, 120):
        cv.rect(gx, 1, gx + 18, 2, 0xff7ac8)                                  # neon reflected in the lacquer
    cv.rect(0, 6, 600, 7, 0xff2a9a)                                          # LED strip
    for y in range(7, 40):
        for x in range(600):
            u, v = (x + y) % 12, (x - y) % 12
            col = lerp(0x4a2a6a, 0x221838, (u + v) / 24)
            if u == 0 or v == 0:
                col = c3(0x161026)
            if u == 0 and v == 0:
                col = c3(0x7a4ea0)
            cv.set(x, y, col)
    cv.rect(0, 40, 600, 48, 0x07050c)
    cv.rect(0, 37, 600, 39, 0x8a8a98)                                        # chrome foot rail
    cv.rect(0, 37, 600, 38, 0xffffff)
    for bx in range(20, 600, 100):
        cv.rect(bx, 39, bx + 2, 44, 0x4a4a58)
    for x in (0, 1, 598, 599):
        cv.rect(x, 0, x + 1, 48, 0x000000)
    cv.light(300, 7, 320, 10, 0xff2a9a, 0.3)
    cv.save("bar_counter")


def floor():
    """Floor 1152x128: black glossy tiles in perspective, a purple carpet at the counter, a lit dance floor
    by the door, neon reflections and pink light under the stage and the lamps."""
    cv = Canvas(LEVEL_W, 128)
    rng = random.Random(3)
    rows = [0]
    k = 0
    while rows[-1] < 128:
        k += 1
        rows.append(rows[-1] + 6 + k * 2)
    for y in range(128):
        band = max(i for i, r in enumerate(rows) if r <= y)
        tile = 24 + band * 8
        for x in range(LEVEL_W):
            col = lerp(0x161026, 0x07050c, y / 128)
            if (x // tile + band) % 2:
                col = lerp(0x221838, 0x0e0a18, y / 128)
            if y in rows or x % tile == 0:
                col = c3(0x000000)
            cv.set(x, y, col)
    # dance floor by the door: lit tiles in neon colours
    lit = [0xff2a9a, 0x2ae0ff, 0x7a4ea0, 0xa01a6a, 0x147a8a]
    for ty in range(4, 60, 12):
        for tx in range(100, 290, 16):
            col = lit[rng.randrange(len(lit))] if rng.random() < 0.7 else 0x221838
            cv.rect(tx + 1, ty + 1, tx + 15, ty + 11, col)
            cv.rect(tx + 1, ty + 1, tx + 15, ty + 2, 0xffffff if col != 0x221838 else 0x2e2048)
    # carpet at the counter: purple with a gold trim
    for y in range(2, 22):
        for x in range(296, 904):
            edge = y in (2, 21) or x in (296, 903)
            inner = y in (4, 19) or x in (299, 900)
            col = 0xe0b040 if edge else 0xa87a24 if inner else (0x3c2a5c if (x // 4 + y // 4) % 2 else 0x4a2a6a)
            cv.set(x, y, col)
    # light
    for px_, depth in POOL_TABLES:
        cv.light(px_, 8 - depth, 90, 34, 0xff2a9a, 0.4)
    cv.light(1035, 4, 140, 24, 0xff2a9a, 0.45)                              # under the stage
    cv.light(195, 30, 120, 40, 0x7a4ea0, 0.3)                               # dance floor glow
    cv.light(600, 0, 320, 18, 0x2ae0ff, 0.15)
    for x in range(30, LEVEL_W, 70):                                        # neon streaks on the gloss
        for k in range(10):
            cv.light(x + k * 2, 40 + k * 6, 4, 2, 0xff7ac8, 0.25)
    for y in range(96, 128):
        for x in range(LEVEL_W):
            p = cv.get(x, y)
            f = 1 - (y - 96) / 32 * 0.35
            cv.set(x, y, [v * f for v in p])
    cv.save("bar_floor")


def pillar():
    """Foreground column 28x240: black with pink neon tubes."""
    cv = Canvas(28, 240)
    for x in range(28):
        cv.rect(x, 0, x + 1, 240, 0x0e0a18 if x < 4 else 0x161026 if x < 22 else 0x07050c)
    cv.rect(13, 0, 15, 240, 0xff2a9a)
    cv.rect(13, 0, 14, 240, 0xffd0ec)
    for y in (30, 120, 210):
        cv.rect(0, y, 28, y + 3, 0x8a8a98)
        cv.rect(0, y, 28, y + 1, 0xffffff)
    cv.save("bar_pillar")


def pool_lamp():
    """Lamp over a pool table, 40x90: chrome cord, purple shade, pink glow under it."""
    cv = Canvas(40, 90)
    cv.rect(19, 0, 21, 72, 0x2a2a34)
    for y in range(72, 84):
        half = 5 + (y - 72)
        cv.rect(20 - half, y, 20 + half, y + 1, 0x4a2a6a if y < 82 else 0x2e2048)
        cv.set(20 - half, y, 0x7a4ea0)
    cv.rect(2, 84, 38, 86, 0x5e3a80)
    cv.rect(2, 84, 38, 85, 0x7a4ea0)
    cv.rect(12, 86, 28, 88, 0xffd0ec)
    cv.rect(16, 88, 24, 89, 0xff2a9a)
    cv.save("pool_lamp", spread=0)


def lamp_shade():
    """The lamp ripped off its cord, 36x14: purple shade, chrome rim, glowing bulbs."""
    cv = Canvas(36, 14)
    for y in range(0, 10):
        half = 5 + y * 1.3
        cv.rect(int(18 - half), y, int(18 + half), y + 1, 0x4a2a6a if y < 8 else 0x2e2048)
        cv.set(int(18 - half), y, 0x7a4ea0)
    cv.rect(1, 10, 35, 12, 0x8a8a98)
    cv.rect(1, 10, 35, 11, 0xffffff)
    cv.rect(11, 12, 25, 14, 0xffd0ec)
    cv.rect(17, 0, 19, 1, 0x000000)
    bar.outlined(cv)
    cv.save("lamp_shade", spread=0)


if __name__ == "__main__":
    os.makedirs(bar.OUT, exist_ok=True)
    for f in (view, back, counter, floor, pillar, pool_lamp, lamp_shade):
        f()

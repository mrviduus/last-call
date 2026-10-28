"""Every girl (RUBY, SKYE, WHIP, BOTTLES) from Lekha's whip clip -> Assets/Resources/Art/Latex/*.png

Cut-out animation like the toad: one standing frame, the whip erased, split into back leg, front leg and body,
plus a copy of the arm that the poses swing up to punch. Sheets are in the girl's layout (frame order matches
Art.Girl), black latex; the game tints the outfit per look. WHIP's own strike stays Tools/make_whip_frames.sh.

Run from Game/:  /tmp/venv/bin/python Tools/make_girl_frames.py   (needs Pillow and ffmpeg)
"""
import os
import subprocess
import sys
import tempfile

from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from make_toad_frames import part, turn  # noqa: E402

SRC = "Tools/src/whip_lash.mp4"
OUT = "Assets/Resources/Art/Latex"
STAND = 14                     # the clip's last frame: standing, whip hanging
FRAME_W, FRAME_H = 96, 72      # taller than the body: room for an arm thrown up
BODY_X, FEET_Y = 315, 720      # source pixels (after the flip)
SCALE = 58 / 508               # the source body is 508 px tall -> 58 px, a head shorter than Brick
HIP_Y = 455
BACK_HIP, FRONT_HIP, MID_HIP = (305, HIP_Y), (340, HIP_Y), (322, HIP_Y)
LEG_SPLIT_X = 328
SHOULDER = (300, 300)
PAD = 300


def frame(i):
    tmp = tempfile.mkdtemp()
    subprocess.run(["ffmpeg", "-v", "error", "-i", SRC, "-vf", "select=eq(n\\,%d)" % i, "-frames:v", "1",
                    os.path.join(tmp, "f.png")], check=True)
    return Image.open(os.path.join(tmp, "f.png")).convert("RGB").transpose(Image.FLIP_LEFT_RIGHT)


def key_out(im):
    """Flood-fills the near-white, grey background (and the floor shadow) from the edges."""
    w, h = im.size
    px = im.load()

    def bg_colour(c):
        return sum(c) > 640 and max(c) - min(c) < 18

    for y in range(440, 641):  # the whip hangs right of the front leg: paint it background first
        for x in range(int(370 + (y - 450) * 20 / 190), w):
            if sum(px[x, y]) < 300:
                px[x, y] = (250, 250, 250)
    bg = bytearray(w * h)
    stack = [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while stack:
        x, y = stack.pop()
        if 0 <= x < w and 0 <= y < h and not bg[y * w + x] and bg_colour(px[x, y]):
            bg[y * w + x] = 1
            stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    out = im.convert("RGBA")
    opx = out.load()
    for y in range(h):
        for x in range(w):
            if bg[y * w + x]:
                opx[x, y] = (0, 0, 0, 0)
    return out


ELBOW, HAND = (305, 385), (368, 440)


def near_arm(x, y):
    return seg_dist((x, y), SHOULDER, ELBOW) < 13 or seg_dist((x, y), ELBOW, HAND) < 12


def seg_dist(p, a, b):
    ax, ay = b[0] - a[0], b[1] - a[1]
    t = max(0.0, min(1.0, ((p[0] - a[0]) * ax + (p[1] - a[1]) * ay) / (ax * ax + ay * ay)))
    return ((p[0] - a[0] - t * ax) ** 2 + (p[1] - a[1] - t * ay) ** 2) ** 0.5


def skin(c):
    r, g, b, a = c
    return a and r > g > b and r - b > 40


class Girl:
    def __init__(self, im):
        w, h = im.size
        self.size = (w + 2 * PAD, h + 2 * PAD)
        big = Image.new("RGBA", self.size)
        big.paste(im, (PAD, PAD))
        self.P = lambda p: (p[0] + PAD, p[1] + PAD)
        src = big.load()
        self.back_leg = part(big, lambda x, y: y - PAD >= HIP_Y and x - PAD < LEG_SPLIT_X)
        self.front_leg = part(big, lambda x, y: y - PAD >= HIP_Y and x - PAD >= LEG_SPLIT_X)
        self.upper = part(big, lambda x, y: y - PAD < HIP_Y)
        # the arm, shoulder to elbow to fist: skin and the armband near those two bones
        self.arm = part(big, lambda x, y: near_arm(x - PAD, y - PAD) and (skin(src[x, y]) or src[x, y][3]))

    def pose(self, lean=0, front=0, back=0, arm=None, bob=0, whole=0):
        """lean: upper body back (+) or forward (-); front/back: legs swung forward (+) at the hip;
        arm: a second arm swung up from hanging by this many degrees (None = no second arm);
        bob: down (+) or up (-); whole: the whole body tipped back around the feet."""
        P = self.P
        img = Image.new("RGBA", self.size)
        img.alpha_composite(turn(self.back_leg, back, P(BACK_HIP)))
        img.alpha_composite(turn(self.front_leg, front, P(FRONT_HIP)))
        upper = self.upper.copy()
        if arm is not None:
            upper.alpha_composite(turn(self.arm, arm, P(SHOULDER)))
        img.alpha_composite(turn(upper, lean, P(MID_HIP)))
        img = turn(img, whole, P((BODY_X, FEET_Y)))
        if bob:
            img = img.transform(img.size, Image.AFFINE, (1, 0, 0, 0, 1, -bob))
        return to_frame(img)


def to_frame(img):
    w, h = FRAME_W / SCALE, FRAME_H / SCALE
    x0 = BODY_X + PAD - w / 2
    y0 = FEET_Y + PAD + 4 - h
    crop = img.crop((round(x0), round(y0), round(x0 + w), round(y0 + h)))
    small = crop.convert("RGBa").resize((FRAME_W, FRAME_H), Image.LANCZOS).convert("RGBA")
    px = small.load()
    for x in range(FRAME_W):
        for y in range(FRAME_H):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, 255) if a >= 110 else (0, 0, 0, 0)
    return small


def sheet(name, frames):
    out = Image.new("RGBA", (FRAME_W * len(frames), FRAME_H))
    for i, f in enumerate(frames):
        out.paste(f, (i * FRAME_W, 0))
    out.save(os.path.join(OUT, name + ".png"))


def main():
    os.makedirs(OUT, exist_ok=True)
    g = Girl(key_out(frame(STAND)))
    stand = g.pose()
    jab, punch = g.pose(arm=62, lean=-4), g.pose(arm=70, lean=-10, front=10)
    # frame order as Art.Girl reads them (the unused slots repeat a neighbour)
    sheet("idle", [stand, g.pose(bob=-1), g.pose(bob=-1), stand])
    sheet("walk", [g.pose(front=20, back=-16), g.pose(bob=-2), g.pose(front=-16, back=20), g.pose(bob=-2)])
    sheet("jab", [stand, jab, g.pose(lean=-14, arm=20)])                     # [1] jab, [2] headbutt
    sheet("punch", [g.pose(lean=8, arm=-15), punch, g.pose(lean=-16, arm=45)])  # wind-up, punch, throw
    sheet("kick", [stand, g.pose(front=45, lean=4), g.pose(front=70, lean=8), g.pose(front=95, lean=14),
                   g.pose(front=30, lean=4)])                                # [1] wind, [3] kick, [4] recover
    sheet("jump", [g.pose(bob=8, front=25, back=15, lean=-6), g.pose(arm=160, back=20), g.pose(front=45, back=35)])
    sheet("jump_kick", [stand, stand, g.pose(front=70, back=40, lean=10)])
    sheet("dive_kick", [stand, stand, stand, g.pose(front=105, back=30, lean=18)])
    sheet("hurt", [g.pose(whole=10, lean=8), g.pose(whole=28, lean=10, front=20)])  # [0] hurt, [1] falling
    sheet("sit", [g.pose(front=85, back=85, bob=36)])


if __name__ == "__main__":
    main()

"""Brick as the toad (by Lekha, AnimateDiff idle clip) -> Assets/Resources/Art/Toad/*.png

Only an idle clip exists, so every other move is cut-out animation: one idle frame is split into
back leg, front leg, upper body and front arm, and each pose turns or stretches those parts.
A first pass to try him out: replace any sheet with real frames of the same layout and the game picks it up.

Run from Game/:  /tmp/venv/bin/python Tools/make_toad_frames.py   (needs Pillow and ffmpeg)
Sheets are rows of 96x63 frames facing right, feet on the bottom row, like the other fighters.
"""
import os
import subprocess
import tempfile

from PIL import Image, ImageEnhance

SRC = "Tools/src/toad_idle.mp4"
OUT = "Assets/Resources/Art/Toad"
FRAME_W, FRAME_H = 96, 63
BODY_X, FEET_Y = 95, 305       # source pixels (after the flip): middle of the body, under the feet
SCALE = 62 / 175               # the source body is 175 px tall -> 62 px in game
HIP_Y = 212                    # legs below this line
BACK_HIP, FRONT_HIP, MID_HIP = (88, HIP_Y), (108, HIP_Y), (98, HIP_Y)
LEG_SPLIT_X = 97               # back leg left of this, front leg right
ARM_X, ARM_Y0, ARM_Y1 = 124, 165, 208  # the front arm (the fist) sits right of the jacket
SHOULDER = (122, 178)
BRIGHTEN = 1.3                 # the clip is dark; he has to read on the club's black floor
PAD = 120                      # working canvas margin, so turned parts never clip


def frames_from_video(indices):
    tmp = tempfile.mkdtemp()
    subprocess.run(["ffmpeg", "-v", "error", "-i", SRC, os.path.join(tmp, "%03d.png")], check=True)
    return [key_out(Image.open(os.path.join(tmp, "%03d.png" % (i + 1))).convert("RGB")) for i in indices]


def key_out(im):
    """Flood-fills the black background from the edges, so dark outlines inside the body stay."""
    im = im.transpose(Image.FLIP_LEFT_RIGHT)  # he looks left in the clip; every sheet faces right
    w, h = im.size
    px = im.load()
    bg = [[False] * h for _ in range(w)]
    stack = [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while stack:
        x, y = stack.pop()
        if 0 <= x < w and 0 <= y < h and not bg[x][y] and sum(px[x, y]) < 60:
            bg[x][y] = True
            stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    out = im.convert("RGBA")
    opx = out.load()
    for x in range(w):
        for y in range(h):
            if bg[x][y]:
                opx[x, y] = (0, 0, 0, 0)
    return out


def part(im, test):
    """The pixels of im where test(x, y), on a transparent canvas of the same size."""
    out = Image.new("RGBA", im.size)
    src, dst = im.load(), out.load()
    for x in range(im.width):
        for y in range(im.height):
            if test(x, y):
                dst[x, y] = src[x, y]
    return out


def turn(layer, deg, pivot):
    """Counter-clockwise on screen by deg around pivot (source coords)."""
    if not deg:
        return layer
    return layer.rotate(deg, resample=Image.BICUBIC, center=pivot)


class Toad:
    def __init__(self, im):
        w, h = im.size
        self.size = (w + 2 * PAD, h + 2 * PAD)
        big = Image.new("RGBA", self.size)
        big.paste(im, (PAD, PAD))
        P = lambda p: (p[0] + PAD, p[1] + PAD)
        self.P = P
        arm = lambda x, y: x - PAD >= ARM_X and ARM_Y0 <= y - PAD <= ARM_Y1
        self.back_leg = part(big, lambda x, y: y - PAD >= HIP_Y and x - PAD < LEG_SPLIT_X)
        self.front_leg = part(big, lambda x, y: y - PAD >= HIP_Y and x - PAD >= LEG_SPLIT_X)
        self.upper = part(big, lambda x, y: y - PAD < HIP_Y and not arm(x, y))
        self.arm = part(big, arm)

    def pose(self, lean=0, front=0, back=0, stretch=1.0, raise_arm=0, bob=0, whole=0, lie=False):
        """lean: upper body back (+) or forward (-); front/back: legs swung forward (+) at the hip;
        stretch: the front arm reaching out (>1) or pulled in (<1); raise_arm: the arm turned up; bob: down (+) or up (-);
        whole: the whole body tipped back around the feet; lie: tipped flat and laid on the floor."""
        P = self.P
        img = Image.new("RGBA", self.size)
        img.alpha_composite(turn(self.back_leg, back, P(BACK_HIP)))
        img.alpha_composite(turn(self.front_leg, front, P(FRONT_HIP)))
        arm = self.arm
        if stretch != 1.0:  # stretched out from the shoulder end
            box = (P((ARM_X, 0))[0], 0, self.size[0], self.size[1])
            piece = arm.crop(box)
            piece = piece.resize((max(1, int(piece.width * stretch)), piece.height), Image.BICUBIC)
            arm = Image.new("RGBA", self.size)
            arm.paste(piece, (box[0], 0))
        arm = turn(arm, raise_arm, P(SHOULDER))
        upper = Image.new("RGBA", self.size)
        upper.alpha_composite(self.upper)
        upper.alpha_composite(arm)
        img.alpha_composite(turn(upper, lean, P(MID_HIP)))
        img = turn(img, whole, P((BODY_X, FEET_Y)))
        if bob:
            img = img.transform(img.size, Image.AFFINE, (1, 0, 0, 0, 1, -bob))
        if lie:
            img = lay_flat(img, whole)
        return to_frame(img)


def lay_flat(img, whole):
    """A tipped body moved so its lowest pixel is on the floor and it's centred on the body line."""
    bbox = img.getbbox()
    cx = (bbox[0] + bbox[2]) / 2
    dx = BODY_X + PAD - cx
    dy = FEET_Y + PAD - bbox[3]
    return img.transform(img.size, Image.AFFINE, (1, 0, -dx, 0, 1, -dy))


def to_frame(img):
    """Working canvas -> one 96x63 game frame: body centred, feet on the bottom row, hard alpha."""
    w, h = FRAME_W / SCALE, FRAME_H / SCALE
    x0 = BODY_X + PAD - w / 2
    y0 = FEET_Y + PAD + 1.5 - h
    crop = img.crop((round(x0), round(y0), round(x0 + w), round(y0 + h)))
    small = crop.convert("RGBa").resize((FRAME_W, FRAME_H), Image.LANCZOS).convert("RGBA")
    small = ImageEnhance.Brightness(small).enhance(BRIGHTEN)
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
    idle = frames_from_video([0, 5, 10, 15])
    t = Toad(idle[0])
    sheet("idle", [Toad(f).pose() for f in idle])
    sheet("walk", [t.pose(front=18, back=-14), t.pose(bob=-2), t.pose(front=-14, back=18), t.pose(bob=-2)])
    # same layout as the punk's: [1] wind-up, [2] the punch
    sheet("punch", [t.pose(), t.pose(lean=6, stretch=0.75), t.pose(lean=-6, stretch=1.9)])
    # [1] hurt, [2] falling, [3] down
    sheet("hurt", [t.pose(whole=6), t.pose(whole=14, lean=6), t.pose(whole=45, front=20, lie=True),
                   t.pose(whole=90, front=10, back=5, lie=True)])
    # Brick's kick sheet: knee up, kick, high kick, jump kick
    sheet("kick", [t.pose(front=50, lean=4), t.pose(front=88, lean=12, stretch=0.8), t.pose(front=115, lean=20),
                   t.pose(front=92, back=35, lean=16)])
    # elbow drop: up with the arm raised, then flat on the floor face-first
    sheet("elbow", [t.pose(raise_arm=80, back=20), t.pose(whole=-90, raise_arm=-30, lie=True)])
    sheet("sit", [t.pose(front=80, back=80, bob=34)])


if __name__ == "__main__":
    main()

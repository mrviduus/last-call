"""Vasyl's own frames -> Assets/Resources/Art/Vasyl/*.png (rows of 96x63 frames, facing right).

Run:  python3 Tools/make_vasyl_frames.py   (needs Pillow)
Until Vasyl has a full hand-drawn set, his body is the placeholder fighter (Resources/Art/Punk, CC0)
recoloured blond with a leather vest. This script keeps that upper body and paints new legs for each pose:
  kick.png: 0 knee up (wind-up), 1 straight kick, 2 high kick (second leg of the bicycle), 3 jump kick;
  elbow.png: 0 rising with the elbow raised, 1 falling flat elbow-first (the wrestling elbow drop).
"""
import os
from PIL import Image, ImageOps

ROOT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Art")
FW, FH = 96, 63
HIP_ROW = 41            # rows below this are legs
HIP = (51, 42)          # where the kicking leg starts
JEANS, JEANS_LIGHT, OUTLINE, BOOT, BOOT_DARK = (0x40, 0x60, 0xa0), (0x60, 0x80, 0xe0), (0x24, 0x18, 0x18), (0x80, 0x60, 0x60), (0x45, 0x3c, 0x3c)

# same recolour the game applies to the placeholder at runtime (Art.RecolorAsVasyl)
BLOND = {(0xce, 0x20, 0x38): (0xf0, 0xd0, 0x70), (0xed, 0x49, 0x24): (0xff, 0xf0, 0xa0), (0x8a, 0x0b, 0x41): (0xb0, 0x88, 0x30)}
LEATHER = {(0xce, 0x20, 0x38): (0x4a, 0x3a, 0x32), (0xed, 0x49, 0x24): (0x6a, 0x52, 0x48), (0x8a, 0x0b, 0x41): (0x2a, 0x20, 0x1c)}


def vasyl_idle():
    im = ImageOps.mirror(Image.open(os.path.join(ROOT, "Punk", "idle.png")).convert("RGBA").crop((0, 0, FW, FH)))
    top = im.getbbox()[1]
    px = im.load()
    for y in range(FH):
        for x in range(FW):
            r, g, b, a = px[x, y]
            if a:
                m = BLOND if y < top + 10 else LEATHER
                if (r, g, b) in m:
                    px[x, y] = m[(r, g, b)] + (255,)
    return im


def brush(px, x, y, r, color):
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r + 1 and 0 <= x + dx < FW and 0 <= y + dy < FH:
                px[x + dx, y + dy] = color + (255,)


def limb(px, a, b, r, color):
    """Thick segment from a to b."""
    steps = max(abs(b[0] - a[0]), abs(b[1] - a[1]), 1)
    for i in range(steps + 1):
        t = i / steps
        brush(px, round(a[0] + (b[0] - a[0]) * t), round(a[1] + (b[1] - a[1]) * t), r, color)


def boot(px, at, toward_right=True):
    x, y = at
    for dy in range(-3, 4):
        for dx in range(-2, 5):
            if 0 <= x + dx < FW and 0 <= y + dy < FH:
                px[x + dx, y + dy] = (BOOT_DARK if dy > 1 else BOOT) + (255,)


def outline(im, painted):
    px = im.load()
    for (x, y) in list(painted):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < FW and 0 <= ny < FH and px[nx, ny][3] == 0:
                px[nx, ny] = OUTLINE + (255,)


def pose(base, legs):
    """base upper body; front leg erased; legs = list of (points, with_boot) polylines drawn as jeans."""
    im = base.copy()
    px = im.load()
    for y in range(HIP_ROW, FH):          # remove the front (right-hand) leg, keep the standing one
        for x in range(HIP[0] - 1, FW):
            px[x, y] = (0, 0, 0, 0)
    before = {(x, y) for y in range(FH) for x in range(FW) if px[x, y][3]}
    for points in legs:
        for a, b in zip(points, points[1:]):
            limb(px, a, b, 3, JEANS)
            limb(px, (a[0], a[1] - 2), (b[0], b[1] - 2), 1, JEANS_LIGHT)  # light along the top edge
        boot(px, points[-1])
    painted = {(x, y) for y in range(FH) for x in range(FW) if px[x, y][3]} - before
    outline(im, painted)
    return im


SKIN, SKIN_LIGHT = (0xd7, 0x7e, 0x4b), (0xff, 0xb1, 0x64)
FEET_ROW = 62  # lowest row of the idle frame: the sprite pivot sits here


def arm(im, points):
    px = im.load()
    before = {(x, y) for y in range(FH) for x in range(FW) if px[x, y][3]}
    for a, b in zip(points, points[1:]):
        limb(px, a, b, 2, SKIN)
        limb(px, (a[0] - 1, a[1] - 1), (b[0] - 1, b[1] - 1), 0, SKIN_LIGHT)
    outline(im, {(x, y) for y in range(FH) for x in range(FW) if px[x, y][3]} - before)
    return im


def elbow_rise(base):
    """Jumping up for the elbow drop: elbow raised high above the head."""
    return arm(base.copy(), [(55, 28), (61, 15), (54, 10)])


def elbow_drop(base):
    """Falling flat, elbow first: the body turned horizontal (head forward), the elbow pointing at the floor.
    The elbow tip lands on the pivot row, so the move hits where the feet would stand."""
    body = base.crop(base.getbbox()).rotate(-90, expand=True)   # head now points right, the way he faces
    im = Image.new("RGBA", (FW, FH))
    im.paste(body, (FW // 2 - body.width // 2 - 4, FEET_ROW - 9 - body.height), body)
    return arm(im, [(58, FEET_ROW - 12), (54, FEET_ROW - 3), (49, FEET_ROW - 6)])


def save_sheet(name, frames):
    sheet = Image.new("RGBA", (FW * len(frames), FH))
    for i, f in enumerate(frames):
        sheet.paste(f, (i * FW, 0), f)
    out = os.path.join(ROOT, "Vasyl")
    os.makedirs(out, exist_ok=True)
    sheet.save(os.path.join(out, name + ".png"))
    print("wrote Vasyl/%s.png" % name, sheet.size)


def main():
    base = vasyl_idle()
    save_sheet("kick", [
        pose(base, [[HIP, (60, 34), (63, 46)]]),        # knee up
        pose(base, [[HIP, (66, 40), (80, 38)]]),        # straight kick
        pose(base, [[HIP, (64, 34), (78, 26)]]),        # high kick
        pose(base, [[HIP, (66, 40), (82, 40)]]),        # jump kick (the standing leg stays tucked under)
    ])
    save_sheet("elbow", [elbow_rise(base), elbow_drop(base)])


if __name__ == "__main__":
    main()

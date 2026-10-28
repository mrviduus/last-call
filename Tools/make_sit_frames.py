"""Patrons sitting at the tables -> Assets/Resources/Art/Punk/sit.png (one 96x63 frame, facing left like the sheet).

Run:  python3 Tools/make_sit_frames.py   (needs Pillow)
Keeps the punk's upper body from Punk/idle.png and paints a seated thigh, shin and boot, in the sheet's own
colours, so the game's hair/clothes recolours apply to it like to any other frame.
"""
import os
from PIL import Image, ImageOps

ROOT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Art")
FW, FH = 96, 63
FEET_ROW = 62  # lowest row of the idle frame: the sprite pivot sits here
JEANS, JEANS_LIGHT, OUTLINE, BOOT_DARK = (0x40, 0x60, 0xa0), (0x60, 0x80, 0xe0), (0x24, 0x18, 0x18), (0x45, 0x3c, 0x3c)


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


def outline(im, painted):
    px = im.load()
    for (x, y) in list(painted):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < FW and 0 <= ny < FH and px[nx, ny][3] == 0:
                px[nx, ny] = OUTLINE + (255,)


def save_sheet(name, frames, folder):
    sheet = Image.new("RGBA", (FW * len(frames), FH))
    for i, f in enumerate(frames):
        sheet.paste(f, (i * FW, 0), f)
    out = os.path.join(ROOT, folder)
    os.makedirs(out, exist_ok=True)
    sheet.save(os.path.join(out, name + ".png"))
    print("wrote %s/%s.png" % (folder, name), sheet.size)


def sitting(idle, thigh, thigh_light, shin, boot_color):
    """Seated on a chair, facing right: the upper body lowered to seat height, thigh forward, shin down.
    Kept in the source sheet's own colours, so the game's recolours apply to it like to any other frame."""
    left, top, right, bottom = idle.getbbox()
    hip_row = top + int((bottom - top) * 0.56)
    hip_x = (left + right) // 2
    drop = 8                                              # seat is lower than standing hips
    im = Image.new("RGBA", (FW, FH))
    upper = idle.crop((0, 0, FW, hip_row))
    im.paste(upper, (0, drop), upper)
    px = im.load()
    before = {(x, y) for y in range(FH) for x in range(FW) if px[x, y][3]}
    hy = hip_row + drop
    knee = (hip_x + 13, hy)
    limb(px, (hip_x - 3, hy), knee, 3, thigh)             # thigh along the seat
    limb(px, (hip_x - 3, hy - 2), (knee[0], hy - 2), 0, thigh_light)
    limb(px, knee, (knee[0], FEET_ROW - 3), 2, shin)      # shin down to the floor
    for y in range(FEET_ROW - 4, FEET_ROW + 1):
        for x in range(knee[0] - 2, knee[0] + 5):
            px[x, y] = boot_color + (255,)
    outline(im, {(x, y) for y in range(FH) for x in range(FW) if px[x, y][3]} - before)
    return im


def main():
    punk = ImageOps.mirror(Image.open(os.path.join(ROOT, "Punk", "idle.png")).convert("RGBA").crop((0, 0, FW, FH)))
    save_sheet("sit", [ImageOps.mirror(sitting(punk, JEANS, JEANS_LIGHT, JEANS, BOOT_DARK))], "Punk")


if __name__ == "__main__":
    main()

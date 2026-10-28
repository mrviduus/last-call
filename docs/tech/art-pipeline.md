# Art pipeline

**Status:** Current. For style rules see [Art direction](../design/08-art-direction.md).

## Where art lives

All runtime art is loaded by path from `Assets/Resources/Art/`:

| Folder | Contents |
|---|---|
| `Bar/` | Level layers (`bar_view`, `bar_back`, `bar_counter`, `bar_floor`, `bar_pillar`) and props (`chair`, `stool`, `bottle`, `table`, `table_flipped`, `pool_table`, `pool_lamp`, `lamp_shade`, `cue`, `ball`) |
| `Punk/`, `Girl/` | Fighter sheets (CC0 "Streets of Fight"): `idle`, `walk`, `punch`, `hurt`, `sit`, and more for the girl |
| `Toad/` | Brick (the toad): `idle`, `walk`, `punch`, `hurt`, `kick`, `elbow`, `sit` |
| `Brick/` | The old punk Brick's `kick`, `elbow` (unused since the toad) |
| `Latex/` | Every girl: `idle`, `walk`, `jab`, `punch`, `kick`, `jump`, `jump_kick`, `dive_kick`, `hurt`, `sit` (72 px tall frames, black latex) |
| `Whip/` | `lash`: WHIP's whip strike, 15 frames 96x79, from Lekha's AnimateDiff clip |
| `shadow.png` | The shadow under every body |

## Import settings

`Assets/Editor/PixelImport.cs` applies these automatically to everything under `Resources/Art/`:
- point filtering;
- no compression and no mipmaps;
- clamp wrapping;
- **readable** textures, which runtime recolouring needs.

Don't change these per file.

## Sprite sheets

- **Layout:** one row of **96 px wide** frames per sheet.
- **Facing:** frames face right. The Punk sheets face left, so `Art` mirrors them.
- **Pivot:** at the feet, computed from the lowest opaque row of the first frame (`FeetPivot`).
- **Scale:** 1 texel = 1 world unit = 1 screen pixel.

## Looks (recolours)

`Art.Looks` maps a look name to:
- the base sheet (girl or punk);
- new colours for the three source reds (`0xce2038`, `0xed4924`, `0x8a0b41`).

Hair and clothes share those reds, so they are split by height: the top rows of each frame's body are hair.

The girls are tinted instead: their look's `clothes[0]` colours the black latex and hair (`Art.Tint`); skin, outlines and white shine stay.

**Adding a look:**
1. Add an entry to `Art.Looks`.
2. Add a name constant in `Art`.
3. Add its HUD name in `Art.Name`.

## Derived sprites

`Art` builds these in code from existing frames, so they need no files:

| Sprite | Made from |
|---|---|
| `Down` | Hurt frame turned 90° (`LyingDown`) |
| `Head`, `HeadlessHurt`, `HeadlessDown` | The top 15 rows of the hurt frame cut off |
| `HalfStays`, `HalfFlies` | The lying sprite split in two |
| `Sinking[]` | The hurt frame cut 3 rows higher each frame |
| `Doubled` | The hurt frame folded at the waist |
| Spark, dust, shard, whip, arm, puddle, flame (3 frames) | Painted pixel by pixel (`Art.Paint`) |

## Generator scripts

All scripts live in `Tools/` and write into `Assets/Resources/`. **The output PNGs and WAVs are the real assets:** the scripts are a first pass, and the files can be edited or replaced by hand.

| Script | Writes | Needs |
|---|---|---|
| `make_bar_art.py` | `Art/Bar/*.png` | Pillow |
| `make_brick_frames.py` | `Art/Brick/*.png`, `Art/Punk/sit.png`, `Art/Girl/sit.png` | Pillow |
| `make_sfx.py` | `Sfx/*.wav` | Standard library only |
| `make_toad_frames.py` | `Art/Toad/*.png` from `Tools/src/toad_idle.mp4` | Pillow, ffmpeg |
| `make_girl_frames.py` | `Art/Latex/*.png` from `Tools/src/whip_lash.mp4` | Pillow, ffmpeg |
| `make_whip_frames.sh` | `Art/Whip/lash.png` from `Tools/src/whip_lash.mp4` | ffmpeg |

Pillow isn't installed system-wide, so run the art scripts in a virtual environment:

```sh
python3 -m venv /tmp/venv && /tmp/venv/bin/pip install pillow
/tmp/venv/bin/python Tools/make_bar_art.py
```

`make_sfx.py` uses a fixed random seed, so re-running it reproduces the same files.

## Replacing placeholder art

To swap in hand-drawn art for a character:
1. Draw sheets with the same frame width (96 px), the same facing (right) and the same frame order.
2. Put them in a new folder under `Resources/Art/`.
3. Point a look at them in `Art`.

Gore and the doubled-over pose are derived from the **hurt** frame, so draw that one with care.

## Animations from video (AnimateDiff)

`make_whip_frames.sh` is the recipe: crop a fixed box (the camera must not move), `hflip` if the character faces left, `colorkey` the flat background out, scale so the body is ~63 px tall, `tile` the frames into one row. A flat background that isn't black or white keys out cleanest. `Fighter.LashFrame` maps the attack's startup, active and recovery frames onto the sheet.

## Cut-out poses (the toad)

Lekha's clip only has the toad standing. `make_toad_frames.py` keys out the black background, splits one frame into back leg, front leg, upper body and front arm (joint coordinates at the top of the script), and builds every other pose by turning or stretching those parts. When real frames arrive for a move, drop them in as that sheet (same frame order) and delete its line from the script.

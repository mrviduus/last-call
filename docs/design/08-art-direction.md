# Art direction

**Status:** Draft. How to make art files the game can load is in [Art pipeline (technical)](../tech/art-pipeline.md).

## Style

- **Resolution:** 16-bit-era pixel art on a 384×240 screen; 1 texel = 1 screen pixel, no smoothing.
- **Palette:** one limited neon palette for the club (black, deep purple, pink and cyan neon, chrome, red velvet), so every layer matches. The old pub had a warm one (wood, brick, brass).
- **Light:** comes from the top left, so highlights are on the top and left and shadows on the bottom and right. Props have dark outlines.
- **Contrast:** the background has less contrast than the fighters, so the fight always reads clearly.
- **Fighters:** about 63 px tall, drawn facing right.

## What exists

| Asset | State |
|---|---|
| Club background, counter, floor, pillars, lamps | Drawn for this project by a script (`make_club_art.py`); can be redrawn by hand |
| Props (tables, chairs, bottles, pool tables) | Still the pub's wooden ones: to be redrawn for the club |
| Fighters | Placeholders: free CC0 sprites ("Streets of Fight" by ansimuz), recoloured per character |
| Brick | The toad in a spiked leather jacket, by Lekha. Idle is his animation; walk, punch, kick, hurt, fall, elbow drop are cut-out poses of it (rough, to be replaced) |
| Sitting poses | Painted over the placeholder by a script |
| Lying down, headless, torn in half, buried, doubled over | Cut and bent from existing frames in code |
| The girls | Lekha's girl in latex, one colour per enemy (RUBY red, SKYE blue, WHIP black, BOTTLES green). The whip strike is his animation; the other moves are cut-out poses of one frame (rough) |
| Sparks, dust, blood, beer puddles, flames, the torn arm | Drawn in code |

## Needs drawing

Priority order, top first:

1. **Brick (the toad):** real frames for walk, run, punches, kicks, jump, hurt, lying, grab, throw, special, to replace the cut-out poses.
2. **THE CLOSER:** his own look, bigger than everyone.
3. **Dirty moves:** knee to the groin; a guy holding his groin, doubled over; grabbing a girl by the hair, the lift, the slam.
4. **OLD TOM and BOTTLES:** their own looks, not recolours.
5. **The rest of the cast:** each enemy their own body, not a recolour.

## To fill
- [ ] Final palette for the fighters
- [ ] Character sheets: front, side, colours for Brick and THE CLOSER
- [ ] Frame counts per animation
- [ ] Title screen and logo

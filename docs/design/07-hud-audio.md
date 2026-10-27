# HUD and audio

**Status:** Current.

## HUD

| Element | Where | Shows |
|---|---|---|
| BRICK health bar | Top left | Yellow fill. A green part = health the special cost, still to be won back |
| Clock, score, combo | Below the health bar | `11:59:SS PM`, then `12:00 AM`; `SCORE`; `N HITS` from 2 hits on |
| Enemy name and bar | Top right | The last enemy Brick hit. The bar's length matches the enemy's max health |
| Intro card | Centre | The first 6 s (text in [Lore](01-lore.md#premise-in-game)) |
| Controls panel | Bottom | The first 8 s, then while H is held |
| Prop hint | Bottom | `J - SWING / L - THROW` while holding a prop |
| End cards | Centre | The win card, or `GAME OVER - PRESS J` |

All text is bold white with a black shadow, sized in game pixels so it scales with the picture.

## Audio

All sounds are synthesised for this project. A recorded file with the same name replaces one.

| Sound | Plays on |
|---|---|
| whoosh | Swings, slips |
| hit / hit_heavy | Light and heavy hits |
| glass | Bottles, lamps |
| wood | Furniture |
| thud | Bodies landing, wall splats |
| ball | Pool balls |
| splat / crunch | Gore |
| bell | Midnight |

## To fill
- [ ] Music: style, when it plays, a change at midnight?
- [ ] Voices: Brick's shouts, enemy grunts (the plan is for you to record your own)
- [ ] A pixel font instead of the default font

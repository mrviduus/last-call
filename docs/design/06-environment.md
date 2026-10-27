# Environment

**Status:** Current.

## The level: the OLD LAMP

The pub is three screens wide (1152 × 240 px). The walkable floor is the lower part of the screen.

| Position (x, px) | What's there |
|---|---|
| ~60 | Entrance door: enemies come in here |
| 170 | Round table, 2 chairs, a couple sitting |
| 250, 900 | Pillars in front of everything (fade when someone stands behind them) |
| 300–900 | The bar counter: 6 stools, bottles on top, a couple on two stools |
| 400 | Round table, 2 chairs, a couple |
| 620 | Pool table with balls, cue and a pendant lamp |
| 790 | Round table, 2 chairs, a couple |
| 1010 | Pool table with balls, cue and a pendant lamp |
| ~1112 | Stage end: enemies come in here too |

The level's ends are walls: bodies knocked hard into them bounce off (see [Physics](#physics)).

## Props

Almost everything can be used as a weapon.

| Prop | Hits before breaking | Damage | Notes |
|---|---|---|---|
| Chair, stool | 3 | 18 | A swing knocks flying |
| Bottle | 1 | 12 | Shatters on landing and leaves a beer puddle |
| Pool cue | 4 | 12 | Longest reach in the game |
| Pool ball | Never | 12 | Kick it along the floor: it bounces and rolls |
| Lamp shade | 2 | 18 | Ripped down from a pendant lamp |
| Round table | 3, once tipped over | 18 when sliding | The 1st hit tips it over and knocks off what stands on it; later hits send it sliding into people |
| Pool table | Never | — | Never moves; hitting it knocks the balls and cue onto the floor |
| A head, an arm | 2 / 3 | 12 / 18 | Left by gore: pick up, swing, throw; a head can be kicked like a ball |

Carrying a prop slows Brick to 75% speed.

## Pendant lamps

Green lamps hang over the pool tables, high enough to need a jump.

| Action | Input |
|---|---|
| Hang on | Jump into the lamp |
| Swing | Pump with A/D, like a swing |
| Kick | J or L, from the lamp |
| Let go | K: you drop with the swing's momentum |
| Rip it down | S+J: you land holding the shade as a weapon |

## Spilled beer

A bottle that breaks leaves a beer puddle on the floor for 20 seconds.
- **Slipping:** running, landing from a jump or being knocked across a puddle makes anyone slip and fall. It does no damage.
- **Brick** can walk over a puddle safely.
- **Enemies** are past walking carefully: they may slip even while walking.

## Physics

How bodies and things move, in plain terms:
- **Knocked flying:** a body flies, bounces once on the floor, then lies down.
- **Weight:** heavier fighters fly less far and push lighter ones aside. Only THE CLOSER is heavy (twice the normal weight).
- **Wall splat:** a body knocked hard into a level end bounces back off the wall, and Brick gets one more hit on it before it lands.
- **Bowling:** a body flying fast knocks down its own teammates in the way.
- **Tables:** solid. Walk around them, or jump over them (pool tables are a bit higher).
- **Balls and heads:** bounce and roll a long way.

## To fill
- [ ] More levels, or only the OLD LAMP?
- [ ] Planned props: a pint that heals, a jukebox, a dartboard (see [Backlog](09-backlog.md))
- [ ] What is behind the door and the stage end?

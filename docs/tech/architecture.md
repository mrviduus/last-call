# Architecture

**Status:** Current.

## Code map

There are no prefabs and no gameplay objects in the scene: everything is built in code at startup. `Assets/Main.unity` holds only Unity's default objects.

| File | Role |
|---|---|
| `Assets/Brawler/Game.cs` | Boot, the world (static lists), the 60 Hz loop, the level, waves and clock, camera, HUD |
| `Assets/Brawler/Fighter.cs` | One body, hero or enemy: state machine, moves, hits, physics, gore, drawing |
| `Assets/Brawler/Player.cs` | The hero's brain: keyboard input, buffering, which move to start |
| `Assets/Brawler/Enemy.cs` | The enemy brain: ring AI, attack tokens, the drunk, the bottle thrower, the boss grab |
| `Assets/Brawler/Prop.cs` | Furniture and weapons: carry, swing, throw, break; `Shard` (debris) |
| `Assets/Brawler/Lamp.cs` | Pendulum lamps you can hang on |
| `Assets/Brawler/Fx.cs` | Cosmetic effects: sparks, dust, afterimages; `Drop` (blood and stains); `Puddle` (beer, which does affect gameplay) |
| `Assets/Brawler/Art.cs` | Loads, recolours and derives sprites; paints procedural sprites |
| `Assets/Brawler/Sfx.cs` | Plays `Resources/Sfx/<name>.wav` by name |
| `Assets/Brawler/Feel.cs` | Every tuning constant (see [Tuning](tuning.md)) |
| `Assets/Editor/Builds.cs` | WebGL build entry point |
| `Assets/Editor/PixelImport.cs` | Import settings for pixel art |
| `Assets/Tests/` | EditMode and PlayMode tests (see [Build, test, deploy](build-test-deploy.md)) |

All game code is in one assembly, `Brawler`; the tests reference it.

## Boot

1. `Game.Boot()`, a `[RuntimeInitializeOnLoadMethod]`, creates the `Game` object in any scene.
2. `Game.Awake()`:
   - sets a fixed 60 Hz tick;
   - creates the camera, which renders into a 384×240 `RenderTexture` (`Game.PixelScreen`);
   - builds the bar's parallax layers;
   - calls `Restart()`.
3. `Restart()`:
   - destroys every fighter, prop and lamp;
   - calls `Game.Reset()` to clear all static state;
   - spawns the hero with a `Player` brain, the furniture, and the seated couples with `Enemy` brains.

`OnGUI` draws `PixelScreen` scaled by an integer factor, then the HUD on top.

## The world

The world is held in static lists, so any code can reach it without lookups:
- `Game.Fighters`, `Game.Props`, `Game.Lamps`;
- `Game.Hero`.

Game state lives in static fields too: `Brawl`, `Wave`, `Clock`, `Midnight`, `Won`, `Score`, `HeroCombo`.

`Game.Reset()` must clear every one of them; tests depend on it. **Any new static state must be added to `Reset()`.**

## The 60 Hz loop

`Game.FixedUpdate()`, once per tick:

```
restart if requested
Simulate()                     ← the whole fight, one tick
remove corpses after 60 frames
clock: 60 s from the first punch → CallMidnight()
bar clear → SpawnWave(), or Won once it's midnight
spawn the next queued enemy (every 40 frames)
camera follows the hero
```

`Game.Simulate()` is the deterministic fight tick. Tests call it directly.

```
Frame++  (shake and flash timers count down)
hitstop? → stop here (SimFrame doesn't advance)
lamps.Step()                    ← before fighters, so someone hanging moves with the lamp
fighters.Step()                 ← brain.Think(), then Tick(): physics, then the state machine
props.Step()                    ← over a snapshot of the list: a prop may break others
Puddle.StepAll(); combo timer
Separate()                      ← bodies on one lane push apart, weighted by mass
BlockByTables()                 ← tables push bodies out, clamped to the level
Hero.ResolveStrike()            ← the hero's strike first: he wins ties
others.ResolveStrike()
```

Strikes are **two-phase**:
1. During `Tick()` an attack sets `strikePending` on its active frame.
2. After every fighter has moved, `ResolveStrike()` applies it.

This makes the result independent of list order, and lets a hit cancel the victim's strike from the same frame.

## Fighter

### Coordinates
- `ground` = (x, depth); `height` = off the floor.
- Screen position = (x, depth + height).
- Sort order = −depth × 2, so nearer is drawn on top.

### State machine
`State` enum: `Idle, Walk, Run, Jump, Attack, Hurt, Launched, Down, GetUp, Dead, Grab, Held, Buried, Sit, Hang, Headless`.
- `Enter(state)` resets `stateFrames`.
- `Tick()` holds one `switch` with the rules for each state.
- `CanAct` = Idle / Walk / Run: the only states a brain can start a move from.

### Moves
`Move` enum. Per-move data comes from switch expressions on `move`:
- frame data: `Startup`, `Active`, `Recovery`;
- `Reach`, `Push`, `Damage`;
- whether it `Launches`.

`Attack(m)` starts a move: from `CanAct`, or from `Grab` for hold moves.

### Getting hit
| Method | Does |
|---|---|
| `TakeHit(damage, dir, launch, push)` | The one way to be hit. Handles armour, release from holds, launch vs stagger, mass |
| `Damaged(n)` | Applies combo scaling, flashes, blood, the HUD target and score |
| `Bury` | The stomp |
| `Slip` | A puddle slip |
| `Decapitate`, `BurstHead`, `TearInHalf`, `RipArm`, `Punt` | Gore |

### Strike resolvers
| Method | For |
|---|---|
| `Strike()` | Normal hits, in front or all round |
| `StrikeDowned()` | The elbow drop |
| `StrikeHeld()` | Headbutt, throw, arm rip, low blow, hair slam |

### Per-type flags
| Field | Default | Used by |
|---|---|---|
| `whip` | false | WHIP |
| `drunk` | false | OLD TOM: hits teammates |
| `armored` | false | THE CLOSER: light hits don't stagger |
| `mass` | 1 | THE CLOSER: 2 |
| `size` | 1 | THE CLOSER: drawn 1.25× |

### Drawing
`LateUpdate()` picks the sprite from `state` and `move`, then applies:
- hit flash (the `Silhouette` material);
- squash and stretch, lunge, jolt;
- the hair-slam `lift` and `tilt`;
- invulnerability blink;
- dizzy stars.

It never changes gameplay state.

## Brains

A brain is a `MonoBehaviour` that sets `Fighter.Think` (a delegate run at the start of each `Step()`). It only writes `moveInput` and `facing`, and calls `Attack` / `Jump` / `Grab` and the like.

- **`Player`:**
  - reads keys in `Update` into flags;
  - buffers them for 8 frames in `Think`;
  - picks one action per tick in priority order: special, hanging, holding, prop, air, run, elbow drop, stomp, pick up, punch combo, kick, jump, grab.
- **`Enemy`:**
  - ring AI with attack tokens (`Engaged() < MaxAttackers`);
  - `Chuck()` for the bottle thrower;
  - the boss hold timer.

## Props

`Prop.Kind` covers furniture, weapons and gore. A prop is in one of these situations:
- held: follows its `holder`;
- resting on furniture;
- flying: has a `thrower`, and hits the first enemy of the thrower or a table;
- sliding or rolling on the floor.

`Break()` removes the prop. It spawns shards and, for a bottle, a `Puddle`. Pool tables never move.

## Effects

- **`Fx` sprites** are cosmetic and count real ticks, so they keep animating through hitstop.
- **`Drop`:** blood drops land as permanent stains, up to 500.
- **`Puddle`:** stepped by `Simulate()`, because it affects gameplay.

## Rendering

| Layer | Sort order |
|---|---|
| Parallax background layers | −20004 … −20001 |
| Blood stains | −19000 |
| Puddles | −18999 |
| Shadows | −10000 |
| Bodies and props | −depth × 2 |
| Front pillars | 30000 (fade to 35% when someone is behind them) |

The camera follows the hero, smoothed, clamped to the level, and snapped to whole pixels.

# Tuning

**Status:** Current.

Every gameplay number is a `const` in [`Assets/Brawler/Feel.cs`](../../Assets/Brawler/Feel.cs). **No magic numbers in game code:** add a constant instead.

## Units

| Quantity | Unit |
|---|---|
| Distance | Pixels. Fighters are about 63 px tall; the screen is 384×240 |
| Speed | Pixels per frame |
| Time | Frames at 60 Hz (60 = 1 second) |
| Angles | Radians, except `Fighter.tilt`, which is in degrees and only used for drawing |

## Comment tags

- `// guess`: a placeholder value, not yet tuned by playing.
- `// measured`: taken from a reference recording.

When you tune a value by playtesting, drop the `guess` tag.

## Groups in the file

| Group | Examples |
|---|---|
| Movement | `WalkX`, `RunX`, `JumpVel`, `Gravity`, `Friction` |
| Hero frame data | `PunchStartup/Active/Recovery`, `KickStartup`, `FlyingKick*`, `BicycleKick*` |
| Lanes | `DepthBand`, `HeightBand`, `BodyGap` |
| Health and damage | `HeroHealth`, `*Damage`, `ComboScaleFrom`, `JuggleCap` |
| Hit feel | `Hitstop`, `SmashShake`, `Finisher*`, `HitJolt`, `*Lunge`, `SlowMoScale` |
| Reactions | `Hitstun`, `LaunchX/Up`, `DownFrames`, `GetUp*`, `*Invuln` |
| Props and lamps | `Chair*`, `Cue*`, `Table*`, `Pool*`, `Lamp*` |
| Special | `Special*`, `RecoverPerHit` |
| Gore | `NeckHeight`, `Head*`, `Bleed*`, `ArmRipDamage` |
| Holds and dirty moves | `GrabFrames`, `Knee*`, `Throw*`, `LowBlow*`, `HairSlam*` |
| Enemy AI | `EnemyWalkX`, `MaxAttackers`, `RingX/Depth`, `EngageGiveUp` |
| Physics extras | `WallSplat*`, `WallBounceFrames`, `BossMass` |
| Enemy types | `Drunk*`, `Chucker*`, `Boss*` |
| Puddles | `Puddle*`, `SlipCooldown`, `EnemySlipChance` |
| Session | `LastCallFrames`, `IntroFrames`, `ComboFrames`, `SpawnGap` |

## Changing a number

1. Edit `Feel.cs`.
2. Run the EditMode tests: several assert behaviour that depends on timings.
3. If the number appears in the [design docs](../design/README.md), update them in the same commit.

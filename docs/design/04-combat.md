# Combat

**Status:** Current. Every number here is a constant in `Feel.cs`; the code wins if they differ.

## Space

The floor is 2.5D. Every fighter has three coordinates:
- **x**: position along the bar.
- **depth**: into or out of the screen.
- **height**: off the floor, when jumping or flying.

Two fighters can hit each other only when they are on the same **lane**: their depths are within 10 px and their heights within 36 px. On the same lane, two bodies can't stand closer than 24 px.

## Brick's moves

| Move | Damage | Startup (frames) | Reach (px) | What it does |
|---|---|---|---|---|
| Punch | 7 | 3 | 36 | Small push |
| Smash (3rd punch) | 14 | 3 | 44 | Knocks flying |
| Body kick (2nd L) | 10 | 5 | 48 | Staggers |
| Roundhouse (3rd L) | 16 | 10 | 50 | Knocks flying; back turned for the 2nd half of the wind-up |
| Sweep (J-J-L) | 8 | 6 | 44 | Down flat on the spot (not the boss) |
| Kick (1st L, at the knee) | 12 | 6 | 46 | Bigger push |
| Jump kick | 14 | — | 40 | Knocks flying; hits from anywhere in the jump |
| Flying kick (run + J) | 24 | 4 | 40 | Knocks flying; crosses the screen fast. Also kicks bodies lying on the floor it passes, once each |
| Bicycle kick (run + L) | 6 per kick | 4 | 40 | A kick every 6 frames; the last one knocks flying. Also kicks bodies lying on the floor, once each |
| Hammer stomp (S + L) | 14 | 8 | 40 | Buries the enemy for 100 frames |
| Elbow drop | 20 | 30 (in the air) | starts up to 80 | Only on enemies lying down. Homes in: Brick steers onto the body, and it can't get up until the elbow lands |
| Headbutt (in a grab) | 8 | 4 | — | Keeps the hold; the 3rd one throws |
| Throw (in a grab) | 18 | 10 | — | Thrown behind Brick |
| Arm rip (in a grab) | 20 | 8 | — | Knocks flying; Brick gets the arm as a club |
| Low blow (J + L, guy) | 10 | 4 | 36 | Grabs him and knees him; he stays doubled over for 90 frames, open to anything |
| Hair slam (J + L, girl) | 22 | 18 | 36 | Grabs her by the hair, lifts her and slams her face-first right in front |
| Special (J + K) | 15 | 4 | 40 both sides | Untouchable for 20 frames; works even while being hit; costs 8 health |
| Lamp kick | 14 | 4 | 46 | Knocks flying |

For prop and weapon damage see [Environment](06-environment.md#props).

## Health and damage

- **Health:** Brick 100. Each enemy's health is in [Enemies](05-enemies.md).
- **Combo scaling:** from the 4th hit of a combo on, each hit does 10% less, down to half. Every hit does at least 1 damage.
- **Juggle cap:** after 8 hits in one combo the victim falls, whatever hit them. No endless locks.
- **Special cost:** 8 health, shown in green on Brick's bar. Each hit he lands afterwards wins back 2. Getting hit loses whatever is still green.
- **Ties:** if Brick and an enemy swing in the same frame, Brick wins.

## What happens to whoever is hit

| Reaction | How long | Notes |
|---|---|---|
| Staggered | 16 frames | Their own blow is cancelled |
| Doubled over | 90 frames | After a low blow; more hits keep him doubled over |
| Knocked flying | Until landing | Bounces once on the floor, then lies down |
| Lying down | 70 frames | The window for an elbow drop |
| Getting up | 20 frames | Enemies can be hit the moment they stand |
| Buried | 100 frames | Then pops out |
| Burning | While in the fire, then 120 frames | 3 damage every 20 frames; enemies panic; burnt to death = charred black body |
| Brick hurt | 30 frames invulnerable | So a crowd can't hit him nonstop; he also blinks for 30 frames after getting up |

## Hit feel

Every hit must be felt:
- **Hitstop:** the game freezes for a moment on every hit:
  - 6 frames on a normal hit;
  - 10 frames plus screen shake on a heavy hit;
  - 20 frames, a bigger shake and a white screen flash on a killing blow.
- **The victim:** shakes during the freeze, flashes white, then red.
- **The attacker:** leans back winding up, then lunges into the blow.
- **Landing and takeoff:** squash and stretch, with dust.
- **Flying and bicycle kicks:** leave blue afterimages.
- **The last enemy in the bar:** their killing blow plays in slow motion.
- **Enemy telegraph:** every enemy blow flashes yellow while winding up, so you can see it coming.

## Gore

Only Brick's **killing** blows maim:

| Killing blow | Result |
|---|---|
| Kick, smash, jump, flying or lamp kick; a heavy prop swing | The head flies off. The body stands a second, fountaining blood, then drops |
| The last headbutt in a grab | The head bursts |
| Elbow drop | The body is torn in two |
| Stomp | Hammered right through the floor |
| Kick on a buried enemy | The head flies off like a football and hurts whoever it hits |

Arm rip (in a grab, L) works any time, not only as a kill.

Every hit draws blood, and harder hits draw more. Blood stays on the floor as stains.

## To fill
- [ ] Should dirty moves work on THE CLOSER? J+L skips him now.
- [ ] Hand-drawn frames for the dirty moves (see [Art direction](08-art-direction.md))

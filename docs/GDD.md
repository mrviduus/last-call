# Last Call: Game Design Document

| | |
|---|---|
| Status | Playable vertical slice: one level, one boss, win/lose |
| Updated | 2026-10-27 (commit `fd998f4`) |
| Engine | Unity 6 (6000.6.7f1), WebGL build |
| Play | https://mrviduus.github.io/last-call/ (may lag behind `main`) |

How to use this document:
- **Design intent lives here, numbers live in code.** Every tuning value is a constant in [`Assets/Brawler/Feel.cs`](../Assets/Brawler/Feel.cs). The tables below quote them for reading convenience; if they disagree, `Feel.cs` is right and this file needs updating.
- **Units:** pixels and frames at a fixed 60 Hz; 60 frames = 1 second.
- **Status tags:** `[in game]`, `[proposed]`, `[declined]`. Anything without a tag is in the game.
- For build, test and code structure see [`CLAUDE.md`](../CLAUDE.md).

---

## 1. Overview

**Pitch.** A 16-bit-style belt-scroller beat-'em-up in the spirit of Battletoads & Double Dragon. One man, one pub, one minute to midnight.

**Design pillars.**
1. **Every hit feels heavy.** Hitstop, white flash, screen shake, sparks, lunge, squash and stretch. A hit you can't feel is a bug.
2. **The room is a weapon.** Chairs, bottles, cues, pool balls, tables and the pendant lamps can all be picked up, kicked, swung on or thrown.
3. **Brutal and funny at once.** Over-the-top gore and dirty moves, played for slapstick: drunks punching their friends, people slipping on beer.
4. **Short and replayable.** One real minute of waves, then the boss. Score and combo give a reason to replay.

**Platform and controls.** Browser (WebGL), keyboard. Pixel screen 384×240, scaled up with integer scaling.

---

## 2. World and story

### Setting
**The OLD LAMP**: a warm wooden pub, three screens wide (1152 px). From left to right: the entrance door, round tables, the long bar counter with stools and bottles, two pool tables with green pendant lamps above them, and the stage end. Through the windows: the night city.

### Premise (as shown in game)
> 11:59 PM. THE OLD LAMP.
> BRICK CAME FOR ONE LAST PINT.
> AT MIDNIGHT THE GANG CLOSES IT.

At midnight the bell over the bar rings and **THE CLOSER** walks in to shut the pub down. Beat him and everyone else in the bar and the win card reads: *"MIDNIGHT. LAST CALL. THE CLOSER IS DOWN. YOU STAYED."*

`[proposed]` Back story: the landlord sold the OLD LAMP to the gang, and tonight is its last night. Not yet shown in game.

### Cast

| Name (HUD) | Who | Look |
|---|---|---|
| **BRICK** | The hero, a regular who won't leave | Blond, leather vest, jeans |
| **MOHAWK** / **BIKER** | Bar guys | Red mohawk / black hair and denim vest |
| **RUBY** / **SKYE** | Bar girls | Red outfit / brown hair, blue outfit |
| **WHIP** | Girl with a whip | Purple hair, black leather |
| **OLD TOM** | The drunk | Grey hair, brown cardigan |
| **BOTTLES** | Bottle thrower | Green hair, white top |
| **THE CLOSER** | Boss, the man closing the pub | Slicked black hair, burgundy suit, drawn 1.25× bigger |

### Content rules
- All art and sound is original or CC0 (see [`CREDITS.txt`](../CREDITS.txt)). Never copy characters, sprites or sounds from Battletoads, Mortal Kombat or other games. Taking inspiration from how they *play* is fine.
- Gore is allowed and goes to the maximum: heads off, arms ripped off, bodies torn in half.
- No sexual content. `[declined]`: nudity, BDSM characters, Liu Kang-style copies, the giant red boot, the spinning tornado.

---

## 3. Session structure (core loop)

1. **Calm bar.** The game starts quiet. Six couples sit at the tables and the counter. The clock shows `11:59:00 PM`. An intro card shows for 6 s and the controls panel for 8 s.
2. **First punch starts the brawl.** Brick's first J or L sets the fight off: the seated patrons stand up one by one (20–140 frames each) and the clock starts.
3. **Waves until midnight.** The clock runs one real minute. Whenever the bar is clear, a new wave walks in from the door or the stage end, whichever is further from Brick. Wave *n* has `min(n + 1, 5)` enemies, arriving 40 frames apart.
4. **Midnight.** The bell rings, the clock shows `12:00 AM`, and THE CLOSER comes in with two of his own, whoever is still standing. No more waves after that.
5. **End.**
   - **Win:** it is midnight and the bar is clear. The win card shows; J restarts.
   - **Lose:** Brick's health reaches 0. "GAME OVER - PRESS J" shows; J restarts.

Restart rebuilds the whole bar from scratch: furniture, couples and score.

### Wave composition
Each wave, enemies are counted down as they spawn (5, 4, 3, 2, 1):

| Slot | Rule |
|---|---|
| Last one in (1) | OLD TOM from wave 2 on |
| 4th from last (4) | BOTTLES from wave 3 on |
| Every 3rd (3) | WHIP |
| Even slots | A guy (MOHAWK or BIKER) |
| Odd slots | A girl (RUBY or SKYE) |
| Midnight wave | THE CLOSER first, then two by the rules above |

### Score
- Every hit Brick lands adds `10 × combo`.
- **Combo** = Brick's hits in a row. It resets when Brick is hit, or after 90 frames without a hit. It shows in the HUD from 2 hits on.

---

## 4. Controls

| Input | Action |
|---|---|
| WASD | Walk (A/D left/right, W/S into and out of the screen) |
| A A / D D (double tap) | Run |
| J | Punch; the 3rd landed punch in a row is a **Smash** (high kick) |
| L | Kick |
| K or Space | Jump |
| J + K together | **Special**: spinning kick all round |
| S + L | Hammer stomp: drives the enemy into the floor |
| H (hold) | Show the controls |

Context actions:

| Situation | Input | Action |
|---|---|---|
| In the air | J | Jump kick |
| Running | J / L | Flying kick / bicycle kick |
| Enemy lying in front | J | Wrestling elbow drop |
| Enemy buried | L | Kick the head off (punt) |
| Prop in reach, no enemy in reach | J | Pick the prop up |
| Holding a prop | J / L | Swing it / throw it |
| Walking into a dazed (hurt) enemy | (walk) | Grab |
| In a grab | J | Headbutt; the 3rd one throws |
| In a grab | K | Throw |
| In a grab | L | Rip an arm off (it becomes a club) |
| In a grab | S + J | **Dirty move**: a low blow on a guy, a hair slam on a girl |
| Jumping into a pool lamp | (jump) | Hang on it |
| Hanging | A/D, J/L, K, S+J | Swing / kick / let go / rip the lamp down as a weapon |

Inputs are buffered for 8 frames. For the special, J and K may be up to 3 frames apart.

---

## 5. Combat

### Space model
The floor is a 2.5D plane. A fighter has `x` (along the bar), `depth` (into the screen, −105 to 10) and `height` (jump). Screen y = depth + height. Two fighters can hit each other only when:
- their depths are within 10 px (`DepthBand`), and
- their heights are within 36 px (`HeightBand`).

Bodies standing on the same lane can't overlap closer than 24 px (`BodyGap`). The lighter body gives way more.

### Hero moves

| Move | Damage | Startup (f) | Reach (px) | Effect |
|---|---|---|---|---|
| Punch | 7 | 3 | 36 | Small push |
| Smash (3rd punch) | 14 | 3 | 44 | Launch |
| Kick | 12 | 6 | 46 | Bigger push |
| Jump kick | 14 | — | 40 | Launch; hits grounded targets from anywhere in the jump |
| Flying kick (run+J) | 24 | 4 | 40 | Launch; flies 7 px/f for 24 f |
| Bicycle kick (run+L) | 6 per kick | 4 | 40 | A hit every 6 f, the last one launches |
| Hammer stomp (S+L) | 14 | 8 | 40 | Buries the enemy for 100 f |
| Elbow drop | 20 | 30 (airtime) | 40 | Only on enemies lying down |
| Headbutt (grab J) | 8 | 4 | — | Keeps the hold; the 3rd one throws |
| Throw (grab K) | 18 | 10 | — | Launch backwards |
| Arm rip (grab L) | 20 | 8 | — | Launch; Brick gets an arm club |
| Low blow (grab S+J, guy) | 10 | 4 | — | Lets go; he stays doubled over for 90 f |
| Hair slam (grab S+J, girl) | 22 | 18 | — | Lifted 28 px, slammed face-first right in front |
| Special (J+K) | 15 | 4 | 40 both sides | Untouchable for 20 f; usable from hitstun; costs 8 HP |
| Lamp kick | 14 | 4 | 46 | Launch |

### Health and damage
- **Health:** Brick 100; guys and WHIP 40; girls and BOTTLES 30; OLD TOM 50; THE CLOSER 200.
- **Enemy damage:** jab 5 (THE CLOSER 10), whip 8, boss throw 18.
- **Combo scaling:** from the 4th hit of a combo on, each hit does 10% less, down to 50%. Every hit does at least 1.
- **Juggle cap:** after 8 hits in one combo the victim falls whatever hit them, so no endless locks.
- **Special cost:** 8 HP, shown in green. Each hit Brick lands afterwards wins back 2 HP of it. Getting hit loses the rest.

### Hit reactions and timing

| Reaction | Duration | Notes |
|---|---|---|
| Hitstun | 16 f | The victim's own pending strike is cancelled |
| Doubled over | 90 f | After a low blow; further hits keep him doubled |
| Launched | Until landing | One ground bounce, then Down |
| Down | 70 f | Elbow-drop window |
| Get up | 20 f | Enemies are hittable the moment they stand; Brick gets 30 f of invulnerability (blinks) |
| Buried | 100 f | Then pops out |
| Brick hurt | 30 f invulnerable | So a crowd can't stun-lock him |

**Ties:** the hero's strike resolves first each frame. If both swing at once, Brick wins.

### Hit feel
| Effect | When | Size |
|---|---|---|
| Hitstop | Every hit | 6 f; heavy hits 10 f + shake 8; finishers 20 f + shake 14 + 6 f white screen flash |
| Victim | Every hit | Shakes 2 px during hitstop; flashes white, then red |
| Attacker | Every hit | Leans back 2 px winding up; lunges 3 px on jabs, 6 px on heavy moves |
| Movement | Takeoff and landing | Squash and stretch for 5 f; dust |
| Afterimages | Flying and bicycle kicks | Blue ghosts trail the kick |
| Last kill of the bar | Last enemy finished | 40 rendered frames of slow motion at 0.3× |
| Enemy telegraph | Every enemy blow | Flashes yellow while winding up |

---

## 6. Physics

| Rule | Detail |
|---|---|
| Gravity | 0.3 px/f² for fighters, props and blood drops |
| Friction | 0.3 px/f on the floor; balls roll with 15% of it |
| Launch | 6 px/f across and 5 px/f up, divided by **mass** (up by √mass); finishers ×1.8 |
| Ground bounce | Once, at 2.5 px/f up and half the speed across |
| Wall splat | A launched body faster than 3 px/f hitting an end wall bounces back at half speed and takes 4 damage. For 20 f it can be hit once more (until the juggle cap) |
| Bowling | A body flying faster than 2.5 px/f knocks down its own teammates for 8 damage |
| Weight | Mass 1 for everyone, THE CLOSER 2: he flies half as far and pushes others aside |
| Tables | Solid; walk around them or jump higher than 26 px (pool tables 30 px). Fighters cornered between a table and a wall are pushed out sideways |
| Spilled drinks | A broken bottle leaves a beer puddle for 20 s. Running, landing from a jump or being knocked across it makes anyone slip and fall (no damage). Brick can walk over it safely; enemies walking through slip 2% of frames. 120 f before the same fighter can slip again |

---

## 7. Enemies and AI

### The ring and attack tokens
Enemies wait on a ring around Brick, 60–110 px away along the bar and 13–35 px off his lane, drifting slowly, on whichever side is less crowded.
- **Attack tokens:** at most 2 enemies take their turn at once. On its turn an enemy walks into Brick's lane, strikes once, then backs off.
- **Give up:** an enemy that can't reach Brick within 180 f passes its turn on.
- **Cooldown:** 70 f after each strike (whip 90), plus up to 30 f at random.
- **Tables:** enemies step around tables, and won't pick a waiting spot inside one.

### Roster

| Enemy | HP | Attack | Behaviour |
|---|---|---|---|
| MOHAWK, BIKER | 40 | Jab 5 | Walk 15% slower |
| RUBY, SKYE | 30 | Jab 5 | Standard ring AI |
| WHIP | 40 | Whip 8, 80 px reach | Long 18 f wind-up: punch her out of it and she flies |
| OLD TOM | 50 | Jab 5 | Sways in depth as he walks; his punch hits **anyone** in front of him, his mates too |
| BOTTLES | 30 | Thrown bottle 12 | Never queues for melee. Keeps 110 px away in Brick's lane and throws a bottle every 120–160 f. Broken bottles leave puddles |
| THE CLOSER | 200 | Jab 10; grab + throw 18 | Mass 2. **Armoured:** punches and kicks hurt but don't stagger him, so he needs launchers. 35% of his turns he grabs Brick, holds him 30 f, then throws him. The J+K special breaks the hold |

---

## 8. The bar: props and environment

| Prop | Hits before breaking | Damage | Notes |
|---|---|---|---|
| Chair / stool | 3 | 18 | Swing launches; reach 46 |
| Bottle | 1 | 12 | Shatters on landing → puddle |
| Cue | 4 | 12 | Longest reach in the game (62 px) |
| Pool ball | Never | 12 | Kick it along the floor; it bounces and rolls |
| Lamp shade (ripped down) | 2 | 18 | From a pendant lamp |
| Round table | 3 once tipped | 18 when sliding | 1st hit tips it over and knocks off what stands on it; next hits send it sliding into people |
| Pool table | Never | — | Never moves; hitting it knocks the balls and cue onto the floor |
| Head / arm (gore) | 2 / 3 | 12 / 18 | Pick up and swing or throw; a head can be kicked like a ball |

**Carrying** a prop slows walking to 75%.

**Pendant lamps** hang over the pool tables, rim 74 px high.
- **Hang:** jump into the lamp.
- **Swing:** pump with A/D like a pendulum, max 0.4 rad.
- **Kick:** J or L kicks from the lamp.
- **Let go:** K drops you with the swing's momentum.
- **Rip it down:** S+J tears the lamp off; you land holding the shade as a weapon.

---

## 9. Gore

Only the **hero's killing blows** maim:

| Killing blow | Result |
|---|---|
| Kick, smash, jump, flying or lamp kick; a heavy swing | Head flies off. The body stands 60 f fountaining blood, then drops |
| Last headbutt in a grab | Head bursts |
| Elbow drop | Body torn in two, one half flies off |
| Stomp | Hammered right through the floor |
| L on a buried enemy | Head punted off like a football; it hurts whoever it hits |
| L in a grab (any time, not only a kill) | Arm ripped off; Brick clubs people with it |

Every hit draws blood, and harder hits draw more. Drops land and stay as stains, up to 500 at once. At most 12 flung body parts exist at once; the oldest disappear first.

---

## 10. HUD and feedback

| Element | Where | Shows |
|---|---|---|
| BRICK health bar | Top left | Yellow fill; green part = special cost you can still win back |
| Clock, score, combo | Below the health bar | `11:59:SS PM` / `12:00 AM`, `SCORE`, `N HITS` |
| Enemy name and bar | Top right | The last enemy Brick hit, Streets of Rage style. Bar length scales with its max HP |
| Intro card | Centre | First 6 s |
| Controls panel | Bottom | First 8 s, then while H is held |
| Prop hint | Bottom | `J - SWING / L - THROW` while holding a prop |
| End cards | Centre | Win card or `GAME OVER - PRESS J` |

---

## 11. Audio

All sound effects are synthesised by [`Tools/make_sfx.py`](../Tools/make_sfx.py) into `Assets/Resources/Sfx`. Drop in a recording with the same file name to replace one.

| Sound | Used for |
|---|---|
| `whoosh` | Swings, slips |
| `hit`, `hit_heavy` | Light and heavy hits |
| `glass` | Bottles, lamps |
| `wood` | Furniture |
| `thud` | Bodies landing, wall splats |
| `ball` | Pool balls |
| `splat`, `crunch` | Gore |
| `bell` | Midnight |

No music yet.

---

## 12. Art pipeline

| Asset | Source |
|---|---|
| Fighters | "Streets of Fight" by ansimuz (CC0). Each look recolours hair and clothes at runtime (`Art.Looks`) |
| Brick's kick and elbow frames; sitting frames | [`Tools/make_brick_frames.py`](../Tools/make_brick_frames.py) |
| Bar layers and props | [`Tools/make_bar_art.py`](../Tools/make_bar_art.py): one warm palette, light from the top left, Bayer dithering |
| Poses derived in code | Lying down, headless, halves, sinking, doubled over; plus sparks, dust, the whip, the arm, puddles |

The PNGs are the real assets: the scripts are only a first pass, and the PNGs can be redrawn by hand. 1 texel = 1 screen pixel.

---

## 13. Backlog

From the design council of 2026-10-27, not yet built:

| Area | Idea |
|---|---|
| Combat | Elbow drop that dives to a lying enemy further away; 1 hit on a downed enemy per combo; launched bodies sliding tables and knocking bottles off |
| Enemies | Cue hustler (long poke, low sweep you jump over); bouncer (heavy, grabs); barmaid mini-boss; knife girl; rugby player |
| Enemy behaviour | Enemies pick up props; one holds Brick from behind while a mate punches (mash to break free); enemies flee at low HP and come back; taunts when Brick is down; enemies fighting each other after a drunk's stray punch |
| Funny mechanics | A pint on the counter heals but makes the controls drunk for 5 s; kicking the jukebox makes everyone dance for 2 s; darts as a 3-shot ranged weapon |
| Structure | 3 lives with a checkpoint per wave; an intro cutscene (drink, push, FIGHT!); music |
| Art | A hand-drawn set for Brick and frames for the dirty moves (a friend, Leha, may draw them) |

## 14. Open questions
- Should the clock pause during hitstop and slow motion? It counts real seconds now.
- Should there be a cap on how many enemies are on screen when midnight lands on top of a wave?
- Should dirty moves work on THE CLOSER? He can't be grabbed now, because he never staggers.

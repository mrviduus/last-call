# Enemies

**Status:** Current. For who they are in the story, see [Lore](01-lore.md#cast).

## How the crowd fights

The crowd surrounds Brick and takes turns, so it's never a queue and never a mob all hitting at once.

- **Waiting:** enemies wait on a ring around Brick, about 60–110 px away along the bar and a little off his lane, drifting slowly. They pick the less crowded side.
- **Turns:** at most **2** enemies take their turn at once. On its turn an enemy walks into Brick's lane, strikes once, then backs off to the ring.
- **Giving up:** an enemy that can't reach Brick within 3 seconds lets someone else try.
- **Rest:** after a strike an enemy waits about 1.2–1.7 s before its next turn.
- **Tables:** enemies walk around tables and don't stand inside them.
- **Telegraph:** every enemy blow flashes yellow while winding up. Hit them during the wind-up and their blow is cancelled.
- **On fire:** a burning enemy forgets the fight and runs about flailing (see [Fire](06-environment.md#fire-molotov)).
- **Friendly fire:** an enemy's blow lands on whoever is in front, mates included.
- **Grudges:** an enemy hurt by a mate turns on them 60% of the time. A red mark blinks over its head, and it goes straight for that mate for up to 5 s, waiting for them to get up if they're down. Two enemies can end up trading blows while Brick watches. THE CLOSER never holds a grudge.

Ways to start a fight between them:

| How | What happens |
|---|---|
| **Bowling** | Knock or throw an enemy into others. Whoever gets hit blames the one who flew into them |
| **Dodging** | Step out of the way while an enemy winds up (it flashes yellow). The blow carries on up to 30 px further and lands on a mate behind Brick, and the enemy lurches forward, pitched over |
| **OLD TOM** | About every 8 s the drunk picks a fight with the nearest mate, for no reason |
| **Fire** | A burning enemy sets alight the enemies he touches (one step, no chain reactions), and they blame him once the flames are out |


## Roster

| Enemy | Health | Attack | Behaviour |
|---|---|---|---|
| MOHAWK, BIKER | 40 | Jab, 5 | A bit slower than the girls |
| RUBY, SKYE | 30 | Jab, 5 | The basic enemy |
| WHIP | 40 | Whip, 8, long reach (80 px) | Long wind-up: punch her during it and she goes flying |
| OLD TOM | 50 | Jab, 5 | Sways as he walks; picks fights with his own mates about every 8 s |
| BOTTLES | 30 | Thrown bottle, 12 | Never comes close. Keeps about 110 px away on Brick's lane and throws a bottle every 2–2.7 s. Broken bottles leave beer puddles |
| THE CLOSER (boss) | 200 | Jab, 10; grab and throw, 18 | Twice as heavy: flies half as far. **Armoured:** punches and kicks hurt but don't stagger him, so knock him flying instead. On about a third of his turns he grabs Brick, holds him half a second, then throws him. Brick's special breaks the hold |

The couples sitting in the bar at the start are MOHAWK, BIKER, RUBY and SKYE. They stand up one by one once the fight starts.

## Who comes when

See [Core loop](02-core-loop.md#who-comes-in-each-wave).

## To fill
- [ ] Planned enemies: cue hustler, bouncer, barmaid mini-boss, knife girl, rugby player (see [Backlog](09-backlog.md))
- [ ] Behaviour: picking up props, holding Brick from behind, fleeing at low health, taunts
- [ ] A line of personality per enemy (see [Lore](01-lore.md))

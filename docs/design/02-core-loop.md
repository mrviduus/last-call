# Core loop

**Status:** Current.

One session lasts about 1–2 minutes: a quiet bar, one real minute of fighting to midnight, then the boss.

## Session, start to finish

1. **Calm bar.**
   - Six couples sit at the tables and the counter.
   - The clock shows `11:59:00 PM`.
   - The intro card shows for 6 s and the controls panel for 8 s.
2. **The first punch starts the brawl.** Brick's first punch or kick starts the fight.
   - The seated patrons stand up one by one, each after 20–140 frames.
   - The clock starts.
3. **Waves until midnight.** The clock runs one real minute.
   - Whenever the bar is clear, a new wave walks in from the door or the stage end, whichever is further from Brick.
   - Wave *n* has `min(n + 1, 5)` enemies, arriving 40 frames apart.
4. **Midnight.**
   - The bell rings and the clock shows `12:00 AM`.
   - THE CLOSER walks in with two of his own, while whoever is still standing keeps fighting.
   - No more waves come after that.
5. **End.**
   - **Win:** it's midnight and the bar is clear. The win card shows; J plays again.
   - **Lose:** Brick's health reaches 0. `GAME OVER - PRESS J` shows; J plays again.

Playing again rebuilds the bar from scratch: furniture, couples and score all reset.

## Who comes in each wave

Enemies in a wave are counted down as they arrive (5, 4, 3, 2, 1):

| Slot | Who |
|---|---|
| The last one in (1) | OLD TOM, from wave 2 on |
| 4th from last (4) | BOTTLES, from wave 3 on |
| Every 3rd (3) | WHIP |
| Even slots | A guy (MOHAWK or BIKER) |
| Odd slots | A girl (RUBY or SKYE) |
| Midnight | THE CLOSER first, then two more by the rules above |

## Score and combo

- **Score:** every hit Brick lands scores `10 × combo`.
- **Combo:** Brick's hits in a row.
  - It resets when Brick is hit, or after 1.5 s (90 frames) without a hit.
  - It shows on screen from 2 hits on.

## To fill
- [ ] Should the clock pause during hitstop and slow motion? It counts real seconds now.
- [ ] Should there be a cap on enemies on screen when midnight lands on top of a wave?
- [ ] Lives and checkpoints, or one life? (see [Backlog](09-backlog.md))

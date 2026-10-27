# Decisions

**Status:** Current. A short record of why things are the way they are, newest first. Add a row when you make a choice someone might later question.

| Date | Decision | Why | Alternatives rejected |
|---|---|---|---|
| 2026-10-27 | Only bottles Brick throws are Molotovs; enemy bottles stay beer | A power for the player, not a hazard spammed at him | Every bottle burns |
| 2026-10-27 | Dirty move on J+L, with no grab first | Easy to do mid-fight | S+J inside a grab (too many steps) |
| 2026-10-27 | The midnight clock counts real seconds from the first punch, not waves | One tense minute is short and replayable | 5 waves at a minute each (too long) |
| 2026-10-27 | Enemies have no invulnerability after getting up; they lie down for 70 frames | So downed enemies can be finished with the elbow drop | 40 frames lying plus 30 invulnerable frames (impossible to finish) |
| 2026-10-27 | The new poses are derived from existing frames in code | Placeholder art until hand-drawn sets exist | Waiting for an artist |
| 2026-10-27 | Docs split into a design track and a technical track | Designers and programmers read different things | One big design document |
| 2026-10-26 | Every number lives in `Feel.cs` | One place to tune; docs quote it | Numbers inline in code |
| 2026-10-26 | Strikes resolve in two phases (`strikePending`, then `ResolveStrike`), hero first | Order-independent results; the hero wins ties; no trades | Hitting during each fighter's own tick |
| 2026-10-26 | Enemy AI uses a ring plus 2 attack tokens | Readable crowds, never a mob hitting at once (as in Streets of Rage) | Every enemy chasing the hero |
| 2026-10-26 | GitHub Pages from a force-pushed `gh-pages` branch, no CI | Unity needs a licence to build on CI | Building in GitHub Actions |
| 2026-10-26 | Everything is built in code; no prefabs and no scene objects | Small, diff-friendly, testable | Unity scenes and prefabs |
| 2026-10-26 | All art and sound is original or CC0 | Legal; our own game | Ripped sprites and sounds |
| 2026-10-26 | Fixed 60 Hz tick, everything in pixels and frames | Frame data like the classic beat-'em-ups; deterministic tests | Delta-time physics |

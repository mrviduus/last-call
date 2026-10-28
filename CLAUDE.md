# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**Last Call**: original 16-bit beat-'em-up in Unity 6 (6000.6.7f1), plays like Battletoads & Double Dragon. Hobby, non-commercial, two friends (Vasyl + Lekha). One level: the OLD LAMP strip club (3 screens, 384x240; was a pub). Hero: Brick, a toad (Lekha's AnimateDiff art). All art/sound is ours, Lekha's or CC0 (see CREDITS.txt): never rip sprites or sounds from BT/MK/other games.

Level art: `Art.Level` = "Club/" (`Tools/make_club_art.py`); props still from `Bar/`. Brick and the girls are cut-out poses from Lekha's clips (`Tools/make_toad_frames.py`, `make_girl_frames.py`, sources in `Tools/src/`).

Docs: `docs/design/` (lore, mechanics, enemies, level: for designers) and `docs/tech/` (architecture, tuning, pipeline, build). Update the relevant page in the same commit as a mechanic change; numbers there quote `Feel.cs`, which wins on conflict. Conventions in `docs/README.md`.

## Commands

Run from `~/projects/last-call` (parent of this repo). `UNITY=/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity`. Batchmode fails if the editor has the project open.

```sh
# tests (EditMode = Assets/Tests/EditMode, PlayMode = Assets/Tests/PlayMode)
$UNITY -batchmode -projectPath Game -runTests -testPlatform EditMode -testResults /tmp/edit.xml
$UNITY -batchmode -projectPath Game -runTests -testPlatform PlayMode -testResults /tmp/play.xml
# single test: add  -testFilter SmokeTest.GoreFinishers
# PlayMode tests write screenshots to Game/Logs/*.png (smoke.png, gore_*.png): look at them

# WebGL build -> Game/Build/WebGL (add -dev for readable stack traces)
$UNITY -batchmode -quit -projectPath Game -buildTarget WebGL -executeMethod Builds.WebGL
python3 Game/Tools/serve.py Game/Build/WebGL 8765   # no-cache server
```

Verify in a browser at plain `http://127.0.0.1:8765` (no `?v=` cache busting), check console, before calling a change done.

Asset generators (`Tools/`, write into `Assets/Resources/`): `make_bar_art.py`, `make_brick_frames.py` need Pillow (make a venv); `make_sfx.py` is stdlib only.

Deploy: no CI (Unity can't build on Actions). GitHub Pages serves branch `gh-pages` = copy of `Build/WebGL` + `.nojekyll`, force-pushed from a scratch dir with `git -c http.postBuffer=524288000` (else 403 on the big .wasm). Live: https://mrviduus.github.io/last-call/

## Architecture

All game code in `Assets/Brawler/` (asmdef `Brawler`); no prefabs, no scene setup: everything is built in code.

- **Game.cs**: boots itself via `RuntimeInitializeOnLoadMethod` in any scene. Owns static world lists (`Fighters`, `Props`, `Lamps`, `Hero`), builds the bar (parallax layers, props, seated couples), waves, camera, HUD (`OnGUI`). `Game.Simulate()` = one deterministic 60 Hz tick of the whole fight; hitstop (`Freeze`) skips it. Strikes resolve after movement, hero first (hero wins ties). `Game.Brawl` flips when the hero first attacks: patrons stand up, waves start.
- **Fighter.cs**: shared body for hero and enemies: `State` / `Move` enums state machine, `Step()`, `ResolveStrike()`, hits, grabs, bury, gore. **Player.cs** / **Enemy.cs** are the brains (input / ring AI with attack tokens) driving a Fighter.
- **Feel.cs**: all tuning constants (pixels, px/frame, frames at 60 fps). Change numbers here, not inline.
- **Prop.cs** (pick-up/throwable furniture, `Shard` gibs), **Lamp.cs** (swinging pool lamps to hang on), **Fx.cs** (sparks, dust, blood, afterimages), **Art.cs** (loads/recolours sprite sheets from `Resources/Art`, paints procedural sprites), **Sfx.cs** (plays `Resources/Sfx/*.wav` by name).
- Depth model: 2D x + `ground.y` depth lane + height; sort order from depth. Floor is `Game.MinDepth..MaxDepth`.
- Input: Input System with a legacy-Input fallback (`#if` in Player.cs). Keys: A/D/W/S move, J punch, L kick, K/Space jump, H help.
- Tests: EditMode `FeelTests` call `Game.Reset()` + `Game.Simulate()` directly (no scene); PlayMode `SmokeTest` boots `Game` and runs frames.
- `Editor/Builds.cs` (WebGL build, uncompressed + hashed names), `Editor/PixelImport.cs` (pixel-art import settings).

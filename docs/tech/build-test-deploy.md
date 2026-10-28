# Build, test, deploy

**Status:** Current.

Run all commands from the folder **above** the repo (`~/projects/last-call`), with:

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity
```

Batchmode fails if the Unity editor has the project open.

## Tests

| Suite | Folder | Runs | Speed |
|---|---|---|---|
| EditMode | `Assets/Tests/EditMode` | Calls `Game.Reset()` and `Game.Simulate()` directly, with no scene | Fast |
| PlayMode | `Assets/Tests/PlayMode` | Boots the real `Game` and runs real frames; writes screenshots to `Game/Logs/*.png` | ~2 min; the win test plays the full minute |

```sh
$UNITY -batchmode -projectPath Game -runTests -testPlatform EditMode -testResults /tmp/edit.xml
$UNITY -batchmode -projectPath Game -runTests -testPlatform PlayMode -testResults /tmp/play.xml
# one test:
$UNITY -batchmode -projectPath Game -runTests -testPlatform PlayMode -testFilter SmokeTest.DirtyMoves -testResults /tmp/one.xml
```

**Screenshots** written by the PlayMode tests: `smoke`, `gore_*`, `lowblow_*`, `hairslam_*`, `win`. They show the game picture only, without the HUD. Look at them after changing visuals.

**Conventions:**
- Every mechanic with logic gets one EditMode test asserting its behaviour.
- Visual changes get a PlayMode screenshot.

## WebGL build

```sh
$UNITY -batchmode -quit -projectPath Game -buildTarget WebGL -executeMethod Brawler.EditorTools.Builds.WebGL   # add -dev for stack traces
python3 Game/Tools/serve.py Game/Build/WebGL 8765                                           # no-cache server
```

Then play at **plain** `http://127.0.0.1:8765`, without `?v=` cache busting, and check the browser console for errors. Browsers, including the VS Code one, cache aggressively; `serve.py` sends `Cache-Control: no-store`.

What `Builds.WebGL` does:
- turns compression off, so any static server can host the build;
- uses hashed file names, so a browser can never mix an old `.wasm` with new data;
- restores both settings afterwards, so a build leaves no settings changes behind.

## Deploy

There is no CI: Unity can't build on GitHub Actions without a licence.

GitHub Pages serves branch `gh-pages`, which is a copy of `Game/Build/WebGL` plus an empty `.nojekyll` file, force-pushed from a scratch folder. Push with `git -c http.postBuffer=524288000`, or the big `.wasm` fails with a 403.

Live: https://mrviduus.github.io/last-call/

## Checklist for a change

1. EditMode and PlayMode tests pass.
2. The WebGL build plays at the plain local URL, with no console errors.
3. The docs are updated in the same commit.
4. Commit, then push, then deploy.

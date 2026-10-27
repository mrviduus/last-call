# Technical track

**How** the game is built. For programmers. For what the game *is*, see the [Design track](../design/README.md).

| Page | Status | What it covers |
|---|---|---|
| [Architecture](architecture.md) | Current | Code map, the 60 Hz loop, the fighter state machine, brains, props, rendering |
| [Tuning](tuning.md) | Current | `Feel.cs`: where every gameplay number lives and how to change it |
| [Art pipeline](art-pipeline.md) | Current | Sprite sheets, recolours, generator scripts, import settings |
| [Build, test, deploy](build-test-deploy.md) | Current | Unity commands, tests, the WebGL build, GitHub Pages |
| [How-to recipes](how-to.md) | Current | Adding an enemy, a move, a prop, a sound |
| [Decisions](decisions.md) | Current | Why things are the way they are |

## Stack

| | |
|---|---|
| Engine | Unity 6 (6000.6.7f1) |
| Packages | 2D Sprite, Input System (with a legacy Input fallback), Test Framework |
| Target | WebGL, uncompressed, hashed file names |
| Language | C# |
| Asset scripts | Python 3 (Pillow for the art scripts; the sound script needs only the standard library) |

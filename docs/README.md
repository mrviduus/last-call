# Last Call documentation

The docs come in two tracks. Read the one for your role.

| Track | For | What's in it |
|---|---|---|
| [**Design**](design/README.md) | Game designers, artists, writers | The world, the story, the characters, how the game plays, the level, enemies. No code knowledge needed. |
| [**Technical**](tech/README.md) | Programmers | How the game is built: architecture, physics code, tuning, art pipeline, build and deploy. |

New here? Start with [Lore](design/01-lore.md), then the [Core loop](design/02-core-loop.md).

## Conventions

- **One topic per page.** Pages stay short and link to each other instead of repeating.
- **Every page shows its status at the top:**
  - **Stub**: only a skeleton; needs writing.
  - **Draft**: written, but open questions remain.
  - **Current**: matches the game today.
- **Every item has a tag:**
  - `[in game]`: playable now. This is the default when there is no tag.
  - `[proposed]`: an idea, not built yet.
  - `[declined]`: decided against; kept so we don't re-propose it.
- **To fill** lists at the end of a page are the open work for that page. Answer them in the page, then delete the line.
- **Numbers:** all tuning values live in code, in [`Assets/Brawler/Feel.cs`](../Assets/Brawler/Feel.cs). Docs quote them for reading. If they disagree, the code is right and the doc needs updating.
- **Units:** pixels and frames at a fixed 60 Hz (60 frames = 1 second). The screen is 384×240 pixels.
- **Keep docs in step with the game:** update the doc in the same commit as the change it describes.

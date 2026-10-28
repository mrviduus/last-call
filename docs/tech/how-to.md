# How-to recipes

**Status:** Current. Short recipes for common changes. Each one ends with: add a test, update the design docs.

## Add an enemy type

1. **Look:** add a look in `Art.Looks`, a name constant in `Art`, and its HUD name in `Art.Name`. See [Art pipeline](art-pipeline.md#looks-recolours).
2. **Numbers:** add `XxxHealth` and the like to `Feel.cs`.
3. **Behaviour:**
   - A flag on `Fighter` if it changes how it **is hit** or **hits**: see `armored`, `drunk`, `mass`.
   - A flag on `Enemy` if it changes how it **thinks**: see `chucker` and `Chuck()`.
4. **Spawning:** add its slot rule in `Game.SpawnNext()`.
5. **Test:** add an EditMode test in `FeelTests` that sets the flags and asserts the behaviour. Example: `TheDrunkPunchesHisOwnMate`.
6. **Docs:** update [Enemies](../design/05-enemies.md) and [Core loop](../design/02-core-loop.md#who-comes-in-each-wave).

## Add a hero move

1. Add it to the `Move` enum in `Move.cs`.
2. Add its frame data and stats to the switches at the top of `Fighter`: `Startup`, `Active`, `Recovery`, `Reach`, `Damage`, `Push`, and `Launches` if it knocks flying.
3. If it needs special resolution, route it in `ResolveStrike()` (as with holds and `StrikeHeld()`).
4. Pick its sprite in `LateUpdate()`'s `move switch`.
5. Add its input in `Player.Think()`, keeping the priority order.
6. Test it, then update [Controls](../design/03-controls.md) and [Combat](../design/04-combat.md).

## Add a prop

1. Add it to `Prop.Kind`.
2. Set its sprite and `hitsLeft` in `Prop.Init`.
3. Set `Damage` and `Reach` if it's a weapon.
4. Spawn it in `Game.SpawnBarProps()`.
5. Art goes in `Resources/Art/Bar/`.

## Add a sound

1. Add a function to `Tools/make_sfx.py` and **append** it to the list in `__main__`. Appending keeps the fixed random seed producing the same output for the existing sounds.
2. Run the script.
3. Call `Sfx.Play("name")` where it should play.
4. Or skip the script and drop a recorded `.wav` with that name into `Resources/Sfx/`.

## Add game state

Static state on `Game` or a class must be cleared in `Game.Reset()`. Otherwise tests and restarts leak it.

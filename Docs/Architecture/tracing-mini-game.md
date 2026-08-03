# Class detail: shared tracing mini-game

Last updated: 2026-08-03  
Files: `Assets/Scripts/MiniGameS/Tracing/TracingPathDatabase.cs`, `TracingPathMath.cs`, `TracingMiniGame.cs`, `TracingMiniGameLauncher.cs`

## Responsibility

The feature draws a normalized 2D guide path in `MiniGameHost` and evaluates mouse tracing without physics or colliders. It uses the shared instantiate-on-start / destroy-on-complete lifecycle.

## Contract

- `TracingPathDatabase` supplies a path for levels 1-4.
- `TracingMiniGame` starts only when the left mouse button is pressed in the start marker.
- Releasing before the end restarts the current attempt; leaving the permitted deviation twice resolves `MISSED`; reaching the end resolves `COMPLETE`.
- `TracingMiniGameLauncher` implements `IPlayerMiniGameLauncher` for `TaskKind.Tracing`.
- The launcher instantiates `Assets/Prefabs/MiniGames/TracingMiniGame.prefab` and destroys that instance on completion. Its final authored child view remains an M6 UI task.

## Verification and TODO

- EditMode tests cover nearest-segment distance calculation.
- Runtime verification covers the Game task to Host construction path and Console errors.
- TODO: manually play-test successful trace, release/restart, one/two deviations, and timeout with the intended mouse sensitivity and final M6 UI dimensions.

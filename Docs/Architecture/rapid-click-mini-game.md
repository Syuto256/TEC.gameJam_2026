# Class detail: shared rapid-click mini-game

Last updated: 2026-08-04  
Files: `Assets/Scripts/MiniGameS/RapidClick/RapidClickMiniGame.cs`, `RapidClickMiniGameLauncher.cs`

## Responsibility

This feature connects the existing rapid-click sample to `TaskKind.RapidClick` through the shared launcher contract. The original sample remains in `Assets/Scripts/MiniGameSample/` as reference material.

## Rules and lifecycle

- One visible button receives uGUI click events until the required count is reached.
- The temporary requirement is `8 + level * 4` clicks, preserving the sample's formula for levels 1-4.
- The base `MiniGameBase` owns the time limit and emits a single timeout result; a completed count emits `COMPLETE`.
- `RapidClickMiniGameLauncher` owns construction and destruction of the temporary child of `MiniGameHost`, then forwards the result to `MainGameController`.

## TODO

- The click requirement, button presentation, and per-level balancing are M6 tuning items, not fixed game specifications.
- `Assets/Prefabs/MiniGames/RapidClickMiniGame.prefab` is the approved shared root and is instantiated/destroyed by the launcher. Replace the code-generated inner controls with the authored Prefab view while retaining the same launcher contract.

## Verification

- Unity compiled with no Console errors.
- In the Game scene, a `RapidClick` task was created and assigned through `MainGameController`; the launcher started successfully.

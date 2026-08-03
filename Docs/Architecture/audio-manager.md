# Class detail: shared audio manager

Last updated: 2026-08-04  
Files: `Assets/Scripts/Core/AudioCatalog.cs`, `AudioManager.cs`, `Assets/Resources/AudioCatalog.asset`

## Responsibility

`AudioManager` is a single persistent audio owner. It changes BGM when the active shared scene changes and exposes a small named SFX API for UI confirmation and player-mini-game completion. It intentionally does nothing when a cue has no clip.

## Configuration

- `Assets/Resources/AudioCatalog.asset` is the single initial catalog asset. Assign an `AudioClip` and volume to each desired `AudioCue` entry in the Inspector.
- BGM cues: `TitleBgm`, `GameBgm`, `ClearBgm`, `GameOverBgm`.
- SFX cues: `UiConfirm`, `MiniGameSuccess`, `MiniGameFailure`.
- The manager loads the catalog from Resources once and survives scene changes via `DontDestroyOnLoad`.

## Contract and limits

- `AudioManager.EnsureInstance()` is safe to call from every scene bootstrap; only one manager remains.
- `AudioManager.PlaySfx(cue)` is a safe no-op until the corresponding catalog entry contains a clip.
- This is intentionally a simple BGM switch, not a cross-fade or mixer-volume implementation. BGM fade, master/BGM/SFX options, and final cue selection are M6 tuning TODOs.

## Verification

- A Game-scene play session created the persistent manager successfully.
- Unity compilation completed, EditMode tests passed (11/11), and the Console had no errors with the empty catalog.

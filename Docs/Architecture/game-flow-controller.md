# クラス詳細: GameFlowController と SceneUiBootstrap

最終更新: 2026-08-03  
実装: `Assets/Scripts/Core/GameFlowController.cs`, `Assets/Scripts/Core/SceneUiBootstrap.cs`  
状態: M2 実装済み

## 責務

`GameFlowController` は `DontDestroyOnLoad` で 1 個だけ存在し、選択中の難易度と終了結果を保持して 5 シーン間を遷移する。`SceneUiBootstrap` は各共有シーンに置かれ、開始時に必要な Canvas、Canvas Scaler、Graphic Raycaster、EventSystem と P0 用の操作 UI を生成する。

## 遷移 API

| API | 動作 |
| --- | --- |
| `OpenDifficultySelect()` | DifficultySelect を開く |
| `SelectDifficulty(difficulty)` | 選択難易度を保存し、Game を開く |
| `Retry()` | 保存済みの難易度を保持したまま Game を開く |
| `PresentResult(result)` | 終了結果を保存し、Clear または GameOver を開く |

## UI 基盤

- Canvas は `Screen Space - Overlay`、基準解像度 1920x1080、`Scale With Screen Size`。
- EventSystem は Input System の `InputSystemUIInputModule` を使用する。
- Game では `HudPanel`、`PcTaskPanel`、`PadTaskPanel`、`MiniGameHost`、`PausePanel`、`OptionPanel` を生成する。
- `MiniGameHost` / Pause / Option は M2 では初期非表示。M3 以降の実装が既存の名前を起点として連携できる。

## M6 で置き換える範囲

現在の UI は機能確認用のアンカー配置と色だけで構成する。フォント、画像、余白、最終的なボタン設計、アニメーション、オプション画面の実装は M6 で Prefab / 専用 View に置き換える。`Assets/Personal/` の資産は直接参照・変更しない。

## 検証

- Play Mode で Title の Canvas と EventSystem、Game の各領域を確認する。
- `GameFlowController` 経由で Title → DifficultySelect → Game の遷移を確認する。

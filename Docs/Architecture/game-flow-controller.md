# クラス詳細: シーン遷移と各シーンの入口

最終更新: 2026-08-04  
実装: `Assets/Scripts/Core/GameFlowController.cs`, `AppServices.cs`, `TitleManager.cs`, `DifficultySelectManager.cs`, `ResultManager.cs`  
状態: 実装済み

かつてここには `SceneUiBootstrap` の説明があった。実行時 UI 生成の全廃にともない削除し、シーンごとの Manager に置き換えている（[決定記録](../Decisions/2026-08-04-remove-runtime-ui-construction.md)）。

## 責務

`GameFlowController` は `DontDestroyOnLoad` で 1 個だけ存在し、選択中の難易度と終了結果を保持して 5 シーン間を遷移する。UI は一切持たない。

各シーンの UI 側は、そのシーンの Manager が担当する。Manager は「Scene 上の実体を参照して、ボタンを `GameFlowController` の遷移 API へつなぐ」だけである。

## 遷移 API

| API | 動作 |
| --- | --- |
| `OpenDifficultySelect()` | DifficultySelect を開く |
| `SelectDifficulty(difficulty)` | 選択難易度を保存し、直前の結果を消して Game を開く |
| `Retry()` | 保存済みの難易度を保持したまま Game を開く |
| `PresentResult(result)` | 終了結果を保存し、Clear または GameOver を開く |

## シーンごとの入口

| シーン | Manager | Inspector で持つもの |
| --- | --- | --- |
| Title | `TitleManager` | `startButton` |
| DifficultySelect | `DifficultySelectManager` | `choices`（`GameDifficulty` と `Button` の対） |
| Game | `GameManager` | 設定・各 View・ワークスペース配列・2 つの Controller |
| Clear / GameOver | `ResultManager` | `summaryText`、`backToDifficultyButton`、`retryButton`（GameOver のみ）|

`Clear` と `GameOver` は同じ `ResultManager` を使う。違いは Inspector の参照だけで、`Clear` では `retryButton` を未設定にする。

難易度を増減する場合は、シーンにボタンを置いて `choices` へ 1 行足す。コードは変更しない。

## 常駐サービスの用意

各 Manager は `Start` の先頭で `AppServices.Ensure()` を呼ぶ。これが `GameFlowController` と `AudioManager` を用意するため、**どのシーンから再生を始めても動く**。

`EventSystem` は常駐させず、各シーンに実体として置く。Hierarchy から見えることを優先した判断である。

## UI 基盤

- Canvas は `Screen Space - Overlay`、基準解像度 1920x1080、`Scale With Screen Size`、`matchWidthOrHeight = 0.5`。
- EventSystem は Input System の `InputSystemUIInputModule` を使用する。
- Title / DifficultySelect / Clear / GameOver は `MainCanvas/ScreenRoot` の下に文字とボタンを実体で持つ。実行時に生成する UI は無い。

## 検証

- Play Mode で Title → DifficultySelect → Game → Clear / GameOver の遷移を確認する。
- GameOver の Retry で難易度を保ったまま Game へ戻ることを確認する。
- Game 以外のシーンから直接再生しても、`AppServices.Ensure()` により常駐サービスがそろうことを確認する。結果が無い状態の Clear / GameOver は `emptyResultText` を表示する。

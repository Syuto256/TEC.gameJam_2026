# クラス詳細: MainGameController と TaskBubbleView

最終更新: 2026-08-03  
実装: `Assets/Scripts/Core/MainGameController.cs`, `Assets/Scripts/Core/TaskBubbleView.cs`  
状態: M3 実装済み

## 責務

`MainGameController` は Game シーンだけで生成され、`GameTuningSettings` と選択難易度から `TaskManager` / `GameSession` を生成する。一定間隔のタスク生成、AI 処理、期限切れ、HP/スコア/HUD 更新、Clear/GameOver への遷移を接続する。

`TaskBubbleView` は一件の `TaskInstance` を PC または Pad の `TaskSpawnArea` に表示する UI である。Collider は使わず、EventSystem の `IPointerClickHandler` を通じて操作する。

## 操作と状態

| 入力 | 処理 | 状態 |
| --- | --- | --- |
| 左クリック | `TryAssignPlayer` | `PlayerPlaying`。期限を停止し、MiniGameHost を表示する。 |
| 右クリック | `TryAssignAi` | `AiProcessing`。設定時間後、成功率に従って一度だけ解決する。 |
| 未操作 | `TaskManager.Tick` | 寿命が尽きると `Expired`。HP を減らす。 |

初期値 `ai.cooldownSec = 0` では、複数タスクへ連続して AI を依頼できる。正の値に変更すれば M1 の全体 AI 依頼クールタイムが有効になる。

## M4/M5 への接続点

M3 の左クリックは、タスクを自力担当へ移して `MiniGameHost` に「実装待ち」を表示する。成功/失敗の完了通知はまだ発生させない。M4/M5 はここへ本物のミニゲーム Prefab を表示し、`MiniGameBase.OnCompleted` を `TaskManager.CompletePlayer` へ一度だけ渡す。

## 実行時 View の扱い

M3 では `TaskBubbleView` をコードで生成する。最終的な Task Bubble Prefab、画像、細かな位置・サイズは M6 の常設 Canvas/UI View 移行時に置き換える。状態モデルと `MainGameController` の API は維持する。

## 検証

- 5 秒間隔で PC / Pad の `TaskSpawnArea` に TaskBubble が生成される。
- AI 依頼後に対象 View が解決時に除去される。
- 自力担当で MiniGameHost が表示され、タスクの期限が停止する。
- タスク期限切れによる HP 減少が GameOver 遷移へ反映される。

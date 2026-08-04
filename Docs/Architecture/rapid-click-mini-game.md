# クラス詳細: 連打ミニゲーム

最終更新: 2026-08-04  
実装: `Assets/Scripts/MiniGameS/RapidClick/RapidClickMiniGame.cs`  
Prefab: `Assets/Prefabs/MiniGames/RapidClickMiniGame.prefab`

## 責務

規定回数に達するまでの連打を uGUI のクリックイベントで数える。

## ルールとライフサイクル

- Prefab のルートに付いた `Image` がクリックを受ける。`Raycast Target` を無効にすると反応しなくなる。
- 必要クリック数は `baseClicks + (レベル - 1) * clicksPerLevel` で決まる。既定値は 12 と 4 で、レベル 1 で 12 回である。
- 制限時間と時間切れの通知は `MiniGameBase` が持つ。必要数に達すると `COMPLETE` を通知する。
- 生成と破棄は `MiniGameHostView` が行う。このクラスは自分を破棄しない。

## 設定

| 項目 | 場所 |
| --- | --- |
| 制限時間 | `Assets/Data/MiniGameCatalog.asset` の `RapidClick` 行 |
| レベル 1 の必要数、レベルごとの増加数 | Prefab 上の `RapidClickMiniGame` |
| 背景色、文字サイズ・配置 | Prefab のルートと `Status` |

## 検証と TODO

- Play モードで Game シーンの `RapidClick` タスクから起動できることを確認済み。Console エラー 0 件。
- TODO: 必要クリック数とレベル別のバランスはプレイテストで調整する。現在の値は暫定である。

かつて `Assets/Scripts/MiniGameSample/` に、本編と同名の `RapidClickMiniGame` をグローバル名前空間で定義したサンプルがあった。同名クラスが 2 つ存在することが混乱の原因になっていたため 2026-08-04 に削除している。

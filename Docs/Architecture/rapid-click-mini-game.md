# クラス詳細: 連打ミニゲーム

最終更新: 2026-08-06  
実装: `Assets/Scripts/MiniGameS/RapidClick/RapidClickMiniGame.cs`  
Prefab: `Assets/Prefabs/MiniGames/RapidClickMiniGame.prefab`

## 責務

規定回数に達するまでの連打を uGUI のクリックイベントで数え、クリック回数に応じたプレビュー画像とファイル情報を表示する。

## ルールとライフサイクル

- Prefab のルートに付いた `Image` がクリックを受ける。`Raycast Target` を無効にすると反応しなくなる。
- 必要クリック数は `baseClicks + (レベル - 1) * clicksPerLevel` で決まる。既定値は 12 と 4 で、レベル1は12回である。
- `previewSprites` は `Assets/Sprites/InGameUI/MiniGameUI/RapidClick/Preview_01.png`〜`Preview_08.png` を使う。`switchEveryNClicks` ごとに次のプレビューへ切り替える。
- `previewImage`、ファイル名、解像度、インデックス、残り回数は `Refresh` で更新する。残り回数は大きく表示し、既存の `ProgressText` も維持する。
- 制限時間と時間切れの通知は `MiniGameBase` が持つ。必要数に達すると `COMPLETE` を通知する。
- 生成と破棄は `MiniGameHostView` が行う。このクラスは自分を破棄しない。

## 設定

| 項目 | 場所 |
| --- | --- |
| 制限時間 | `Assets/Data/MiniGameCatalog.asset` の `RapidClick` 行 |
| クリック数 | Prefab 上の `RapidClickMiniGame.baseClicks` / `clicksPerLevel` |
| プレビュー切り替え | Prefab 上の `previewSprites` / `switchEveryNClicks` |
| 画像・情報・残り回数の配置 | `Assets/Prefabs/MiniGames/RapidClickMiniGame.prefab` |

## 検証と TODO

- EditMode でコードのコンパイルと既存の連打判定テストを確認する。
- Play モードで Game シーンの `RapidClick` タスクから起動し、プレビュー切り替えと残り回数表示を確認する。
- TODO: 必要クリック数とレベル別のバランスはプレイテストで調整する。

かつて `Assets/Scripts/MiniGameSample/` に本編と同名の `RapidClickMiniGame` があったが、同名クラスの混乱を避けるため 2026-08-04 に削除している。

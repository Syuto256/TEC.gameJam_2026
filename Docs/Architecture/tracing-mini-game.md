# クラス詳細: なぞりミニゲーム

最終更新: 2026-08-04  
実装: `Assets/Scripts/MiniGameS/Tracing/TracingPathDatabase.cs`, `TracingPathMath.cs`, `TracingMiniGame.cs`  
Prefab: `Assets/Prefabs/MiniGames/TracingMiniGame.prefab`

## 責務

正規化された 2D の経路を盤面に描き、マウスのなぞりを物理演算・コライダーなしで判定する。

## 契約

- `TracingPathDatabase` がレベル 1〜4 の経路を供給する。
- 始点マーカーの上で左ボタンを押したときだけ開始する。
- 終点に着く前にボタンを離すと、その試行を始点からやり直す（ミスにはしない）。
- 許容逸脱量を超えると 1 ミスとして試行をリセットし、許容ミス数に達すると `MISSED` で終了する。
- 終点の判定半径に入ると `COMPLETE` で終了する。精度や所要時間は成功条件に含めない。

## 実行時に生成する唯一のもの

ガイド線は経路データの点の数で本数が変わるため、Prefab 上の複製元 `TracingArea/GuideSegment`（非アクティブ）を複製して並べる。コードが決めるのは位置・長さ・角度だけで、**太さと色は複製元を編集して調整する**。

始点・終点・現在位置のマーカーは Prefab 上の実体であり、コードは `anchoredPosition` を経路データから設定するだけである。

## 設定

| 項目 | 場所 |
| --- | --- |
| 経路データ | Prefab 上の `TracingMiniGame.database`（実体は `Assets/Data/MiniGames/Tracing/TracingPathDatabase.asset`） |
| 制限時間 | `Assets/Data/MiniGameCatalog.asset` の `Tracing` 行 |
| 始点・終点の判定半径、許容ミス数 | Prefab 上の `TracingMiniGame` |
| 盤面の大きさ・配色、マーカーの大きさ・色、ガイド線の太さ・色 | Prefab の子 |

## 検証と TODO

- EditMode テストが点と折れ線の最短距離を検証している。
- Play モードで Host への生成と、経路の点数どおりのガイド線生成（3 点の経路で 3 本）を確認済み。
- TODO: 成功、離して再開、1 回／2 回の逸脱、時間切れを実機のマウス感度で手動確認する。

# クラス詳細: 仕分けミニゲーム

最終更新: 2026-08-04  
実装: `Assets/Scripts/MiniGameS/DragDrop/SortingMiniGame.cs`, `SortingDraggable.cs`, `SortingDropBox.cs`  
Prefab: `Assets/Prefabs/MiniGames/SortingMiniGame.prefab`

## 責務

Motonaga の仕分け試作を本編の `TaskKind.DragDrop` へ取り込んだもの。uGUI のドラッグイベントだけを使い、2D / 3D の物理演算は使わない。試作元は変更していない。

## 公開契約

| API / イベント | 意味 |
| --- | --- |
| `SortingDraggable` | カード 1 枚を掴んで動かす。どの箱にも入らなければ元の位置へ戻す。 |
| `SortingDropBox.OnDrop` | 落とされたカードを受け、正誤の判断を `SortingMiniGame` へ渡す。 |
| `SortingMiniGame.Drop` | 正解のカードを取り除く。許容ミス数に達すると `MISSED` で終了し、全部片付くと `COMPLETE` で終了する。 |

正誤は、カードと箱が同じ `categoryId`（文字列）を持つかどうかで決まる。`SortingMiniGame` の `levelLayouts` は、各レベルのレイアウトルート、箱、カード、許容ミス数を 1 組として持つ。開始時には該当レベルのルートだけを有効にする。

## ライフサイクル

```mermaid
sequenceDiagram
    participant Core as MainGameController
    participant Host as MiniGameHostView
    participant Game as SortingMiniGame
    participant Tasks as TaskManager
    Core->>Core: MiniGameCatalog から Prefab を引く
    Core->>Host: Spawn(prefab)
    Core->>Game: Initialize(level, timeLimit)
    Game->>Game: 各 SortingDropBox に自分を Bind
    Game->>Game: uGUI の drag / drop
    Game-->>Core: OnCompleted(success, reason)
    Core->>Tasks: CompletePlayer を 1 回
    Core->>Host: Hide()（生成物を破棄）
```

## 設定

| 項目 | 場所 |
| --- | --- |
| 制限時間 | `Assets/Data/MiniGameCatalog.asset` の `DragDrop` 行 |
| 箱とカードの配置・枚数・見た目 | `Level1Layout`〜`Level4Layout` の子に置いた Card / Box Prefab インスタンス |
| 正解の対応 | 各 `SortingDropBox` と `SortingDraggable` の `categoryId` |
| 使用する箱・カード・許容ミス数 | Prefab 上の `SortingMiniGame.levelLayouts` の各行 |

## 現在のルールと TODO

- Lv.1: A の箱 1 個とカード 1 枚。ドラッグ＆ドロップ操作を学ぶ段階。
- Lv.2: A の箱 1 個と A のカード 3 枚。
- Lv.3: A / B の箱を各 1 個、A / B のカードを各 2 枚。
- Lv.4: A / B / C の箱を各 1 個、カードは A×1・B×2・C×1。
- 各レベルのカード・箱は独立した UI Prefab インスタンスで、レイアウトルートごとに配置を調整できる。
- TODO: 現在の `GameTuningSettings` は最大タスクレベル 1 のため、Lv.2〜4 を通常プレイで出すには難易度プロファイルの `maxTaskLevel` と上昇間隔を設定する。

## 検証

- Play モードで Game シーンの `DragDrop` タスクから起動できることを確認済み。Console エラー 0 件。
- ドラッグ量は Canvas の拡大率で補正している。1920x1080 以外の解像度でもカードがポインタからずれない。
- TODO: 実機のポインタ操作でドラッグ＆ドロップを手動確認する。

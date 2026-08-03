# ミニゲームPrefabの引き継ぎ運用

日付: 2026-08-04  
状態: 採用

## ワークスペースと共有版

- 改善担当者は `Assets/Personal/<member>/MiniGameWork/` に作業用Prefab・データを置き、自由に変更する。
- 本編は `Assets/Prefabs/MiniGames/` の承認済み共有Prefabだけを参照する。`Assets/Personal/` を本編シーンや `MiniGameCatalog` から直接参照しない。
- 改善完了後、Unity Pipeline経由で作業用Prefabを共有フォルダへコピーまたは置換し、共有版として検証する。

## 使い分け

- 見た目・軽微な設定だけを変える場合はPrefab Variantを使える。
- UI構造や操作ロジックを大きく変える場合は、Personalでは通常Prefabとして自由に改善し、採用時に共有Prefabへ昇格する。

## 本編側の契約

共有 Prefab への参照は `Assets/Data/MiniGameCatalog.asset` が 1 行 1 種別で持つ。`MainGameController` がそこから引いた Prefab を `MiniGameHost` の子として生成し、結果確定後に `MiniGameHostView.Hide()` が破棄する。将来的にプールへ置き換える場合も、この契約と参照先を維持する。

共有 Prefab を差し替えた場合、カタログの参照先はパスではなくアセット参照なので、同じ Prefab ファイルを置換すれば登録の変更は要らない。別ファイルとして作った場合はカタログの `prefab` を差し替える。手順は [ミニゲームの追加・改造手順](../Architecture/mini-game-catalog.md) を参照。

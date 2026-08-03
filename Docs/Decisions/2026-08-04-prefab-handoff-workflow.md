# ミニゲームPrefabの引き継ぎ運用

日付: 2026-08-04  
状態: 採用

## ワークスペースと共有版

- 改善担当者は `Assets/Personal/<member>/MiniGameWork/` に作業用Prefab・データを置き、自由に変更する。
- 本編は `Assets/Prefabs/MiniGames/` の承認済み共有Prefabだけを参照する。`Assets/Personal/` を本編シーンや共有Launcherから直接参照しない。
- 改善完了後、Unity Pipeline経由で作業用Prefabを共有フォルダへコピーまたは置換し、共有版として検証する。

## 使い分け

- 見た目・軽微な設定だけを変える場合はPrefab Variantを使える。
- UI構造や操作ロジックを大きく変える場合は、Personalでは通常Prefabとして自由に改善し、採用時に共有Prefabへ昇格する。

## 本編側の契約

M6で各 `IPlayerMiniGameLauncher` に共有Prefabの参照を持たせる。Launcherは `MiniGameHost` の子としてPrefabを生成し、完了時に破棄する。将来的にプールへ置き換える場合も、この契約と参照先を維持する。

# Personal試作の本編採用方針

日付: 2026-08-04  
状態: 採用

## 採用対象

- Motonagaさんの仕分け試作を `TaskKind.DragDrop` の共有実装へ移植する。`DraggableItem` / `DropBox` のUIイベント設計、正誤判定、2ミス制を引き継ぐ。
- 既存の `MiniGameSample/RepidClickMiniGame` を `TaskKind.RapidClick` の共有実装へ接続する。`MiniGameBase` による制限時間・完了通知の設計を引き継ぐ。

## 非採用（置換しない）対象

Motonagaさんのタイトル遷移試作はEnter入力でシーンを開く最小実装である。本編には難易度・結果保持を扱う `GameFlowController` があるため、これを置換しない。タイトルの操作導線・演出をM6 UI調整時の参照とする。

## 取り込み方法

`Assets/Personal/` の原本は変更しない。本編用の独立クラスへ複製・再設計し、`IPlayerMiniGameLauncher`、`MiniGameBase`、`MiniGameHost` の契約に接続する。

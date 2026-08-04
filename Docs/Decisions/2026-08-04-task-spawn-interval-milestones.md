# タスク出現間隔の時刻表

記録日: 2026-08-04  
状態: 実装済み（値は調整待ち）

## 決定

`GameTuningSettings.DifficultyProfile.spawnIntervalMilestones` に、ゲーム開始からの時刻とタスク出現間隔（秒）を設定できるようにする。到達済みの最新時刻の値を使い、時刻表が空なら従来の `spawnIntervalSec` を使う。

## TODO

- Easy を含む各難易度について、出現間隔を変える時刻と秒数をプレイテストで決める。

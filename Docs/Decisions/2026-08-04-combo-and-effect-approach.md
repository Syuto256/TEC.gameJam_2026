# コンボの採用と、演出の実現方式

日付: 2026-08-04
状態: 採用

## 背景

外部の統合仕様書（`20260804_タスク管理ゲーム_AI向け統合仕様書_v1_0.md`）と実装のあいだに、次の食い違いがあった。

| 論点 | 仕様書 | 実装 |
| --- | --- | --- |
| コンボ | 8.3「コンボボーナスは付けない」 | `GameSession.ComboCount` として実装済み |
| AI 成功スコア | 9.1「自力成功と同じ扱い」 | 0.60 倍（`ai.scoreMultiplier`） |
| AI クールダウン | 22.4 で 0 秒と 2 秒が並立 | 0 秒 |
| 時間切れダメージ | 22.9 で 7〜8 が並立 | 8 |

あわせて、ライティング・パーティクル・DOTween の導入方針を決める必要があった。

## 決定 1: コンボは残す

コンボを採用する。仕様書 8.3 の懸念（コンボが途切れるとやる気を失う）は、**加算方向だけのボーナスにする**ことで回避する。

- コンボ倍率は `1 + 加算値 × (コンボ数 - 1)` を上限でクランプした値。
- 1 コンボ目は等倍。コンボが切れても基準点に戻るだけで、罰は発生しない。
- 調整値 3 つを `GameTuningSettings.score` に外出しし、プランナーが Inspector から変えられるようにした。0 を入れればコンボの効果を実質的に消せる。

`GameSessionComboTests`（4 件）で、加算・上限・リセット・無効化の各挙動を固定した。

**未決:** AI 成功はコンボを伸ばさないが、その時点のコンボ倍率は受け取る。自力と AI の評価差（仕様書 22.5）が決まるまで現状の挙動を維持し、コードに TODO を残す。

## 決定 2: パーティクルは DOTween + スプライトで作る

`ParticleSystem` は使わない。

理由は描画方式にある。`MainCanvas` は `Screen Space - Overlay`（`m_RenderMode: 0`）であり、Overlay Canvas は他のすべての描画の後に合成されるため、**`ParticleSystem` は常に UI の背面に隠れる。** 前面に出すには `UIParticle` 相当の仕組みを入れるか、Canvas を `Screen Space - Camera` へ変えるしかない。どちらもジャム終盤に入れる改修としては影響が大きい。

したがって、成功の紙吹雪・失敗の衝撃波は **Image + DOTween のトゥイーン**で表現する。数が必要な場合はスプライトを複数枚置き、乱数で角度と距離を散らす。

## 決定 3: ライティングは Image / CanvasGroup の合成で行う

[疑似 2D ライティングの技術検証を後工程へ延期](2026-08-04-deferred-ui-lighting-prototype.md) の方針を維持する。保留の前提条件（UI 構成の確定、素材差し替え対応）は達成済みのため、検証に着手してよい。

URP 2D Light は使えない。本プロジェクトの URP は 3D の Universal Renderer であり、かつ Canvas が `Screen Space - Overlay` のため、**2D Light は UI に一切影響しない。** 2D Renderer へ切り替えるには Canvas を Camera 方式へ変える必要があり、対象外とする。

## 決定 4: DOTween を導入する

トゥイーンは自前のコルーチンではなく DOTween に寄せる。現時点で未導入である。

**無料版を使う。** 有料版（DOTween Pro）は所持しているが、チーム全員が同じ状態で作業できることを優先する。

導入時の確認事項として、本プロジェクトは asmdef を 8 つに分割している（`Overwork.Core` ほか）。DOTween の DLL が各アセンブリから参照できることを、最初に 1 箇所（`HudView`）で確かめてから他へ広げる。

## 併せて実施したコード整理

| 対象 | 内容 |
| --- | --- |
| `GameSession.CalculateScore(int, float, float)` | コンボ対応の 4 引数版に置き換わった後も残っていた死にコード。削除。 |
| `GameSession.ScoreChanged` | どこからも購読されていないイベント。削除。加算スコアは `Apply` の戻り値で受け取る。 |
| `MainGameController.IsComboMilestone` | 節目の間隔 10 がコードに直書きされていた。`comboMilestoneInterval` から読むよう変更。 |
| `HudView` のスコア表示 | 補間文字列に対する冗長な `.ToString()` を削除。 |

## 検証結果（2026-08-04）

- Unity Pipeline 経由の `recompile` でコンパイルエラー 0 件。
- EditMode テスト 48 件すべて成功（新規 `GameSessionComboTests` 4 件を含む）。

## 関連資料

- [コアゲームプレイ仕様](../Specifications/gameplay-core.md)
- [演出強化・仕上げ計画](../Archive/2026-08-04-effects-and-polish.md)

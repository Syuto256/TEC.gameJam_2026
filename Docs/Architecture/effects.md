# 演出の置き場と、時間の扱い

最終更新: 2026-08-07

**演出を足す・直すときに最初に読む 1 枚です。** どこに書くか、時間をどう扱うかがここで決まります。

## 結論

**演出は 1 箇所に集約されていません。** 効果ごとに専用の View が持っています。
統合管理する仕組み（イベント配信など）は**ありません**。次に足すときも、その方式に合わせます。

そのため守るべき規則は 1 つだけです。

> **ポーズ中（`Time.timeScale = 0`）に動いてほしい演出は、自分で対処すること。**
> 何もしなければ止まります。

## どこに何があるか

| クラス | 何の演出か | 時間の扱い |
| --- | --- | --- |
| `FocusLightingView` | ミニゲーム中に背後を落とし、窓のまわりを光らせる | DOTween（既定） |
| `ResultEffectLayerView` | タスクの決着を吹き出しの位置に出す | DOTween（既定） |
| `MiniGameResultView` | ミニゲームの結果表示 | DOTween（既定） |
| `SlideMotionLinesView` | デバイス切替のスピード線 | DOTween + **`SetUpdate(true)`** |
| `RapidMashTextEffect` | 連打中の文字揺れ | DOTween + **`SetUpdate(true)`** |
| `DeviceScreenController` | デバイス面のスライド切替 | DOTween + **`SetUpdate(true)`** |
| `ButtonHoverScale` | ボタンのホバー・押下 | コルーチン + **`unscaledDeltaTime`** |
| `FadeOverlayView` | シーン間の暗転・明転 | **コルーチン** |
| `PcLidView` | PC の蓋の開閉 | コルーチン + **`unscaledDeltaTime`** |
| `SelectionCornerHighlight` | 選択中の四隅表示 | なし（静的） |
| `TaskBubbleView` | 吹き出しの出現・消滅 | DOTween（既定） |
| `HudView` | HP バー・スコア・コンボ | DOTween（既定） |

## 時間の扱いの選び方

| 状況 | 使うもの |
| --- | --- |
| ポーズ中は止まってよい（ゲーム内の演出） | DOTween をそのまま |
| ポーズ中も動いてほしい（UI・メニュー） | DOTween に **`.SetUpdate(true)`** |
| **シーン読み込みをまたぐ** | **コルーチン + `unscaledDeltaTime`** |

### シーン読み込みをまたぐ演出だけは DOTween を使わないこと

`SceneManager.LoadScene` は同期で、**そのフレームだけ 0.35 秒前後かかります。**
DOTween は実時間で進むため、**待ちと本編を 1 フレームでまとめて消化してしまいます。**

ジャム最終日に実測した値です。

| | 直す前 | 直した後 |
| --- | --- | --- |
| 明転 | **0 秒（飛んでいた）** | **0.297 秒**（設定 0.30） |
| 蓋のコマ | 最初の数コマが飛ぶ | 各 0.20 / 0.27 / 0.20 秒 |

`FadeOverlayView` と `PcLidView` は、この理由で **DOTween からコルーチンへ戻してあります。**
`action()` を呼んだあとに `yield return null` を挟み、読み込みが終わってから明転を始めます。
**同じことをする演出を足すときは、この 2 つを写してください。**

## 足すときの注意

- **`SetActive(false)` される可能性のある対象にツイーンを掛けるときは `SetLink(gameObject)` を付ける。**
  付けないと、対象が消えたあともツイーンが生き残ります。
- **ハイライトのために `Canvas` を足したら、外すときに破棄する。** 無効にして残すだけでは
  描画順が元に戻らないことがあります（実例: HP バーが赤いまま残った。
  [チュートリアル統合 §7-2](../Decisions/2026-08-07-tutorial-into-game-scene.md)）。
- 座標・大きさ・色は Scene / Prefab が持ちます。コードが持つのは**進行だけ**です。

## 既知の弱点

**同じ趣旨の対処が 7 箇所に分散しています。**「ポーズ中でも動くように」という注意書きが
7 回書かれている状態です。統合管理を置くなら、ここが最初の対象になります
（[振り返り §1-4](../Retrospective/2026-08-06-gamejam.md)）。

**ただし、今のところ実害は出ていません。** 直すかどうかは「できない」が出てから判断してください。

## 関連資料

- [シーン構造](scene-structure.md)
- [クラスカタログ](class-catalog.md)
- [DeviceScreenController](device-screen-controller.md)

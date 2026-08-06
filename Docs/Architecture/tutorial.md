# チュートリアル

最終更新: 2026-08-07  
実装: `Assets/Scripts/Core/TutorialSequenceController.cs`（479 行）  
画面: `Assets/Prefabs/UI/TutorialOverlay.prefab`

**チュートリアルを直すときに最初に読む 1 枚です。**

## 結論

**チュートリアルは専用シーンではありません。`Game` シーンの 1 モードです。**

```
DifficultySelect の「チュートリアルをあそぶ」
  → GameFlowController.StartTutorial()   … IsTutorial = true にして Game シーンを開く
    → GameManager.Start() の最後で TutorialOverlay を有効化
      → TutorialSequenceController が 34 ステップを進める
```

**以前は `Tutorial.unity` という `Game.unity` の複製がありましたが、廃止しました。**
複製した時点から追随されず、`Game.unity` が 18 回変更される間に 4 回しか同期されなかったためです
（[統合の決定](../Decisions/2026-08-07-tutorial-into-game-scene.md)）。

## 本編との違いは 5 つの値だけ

`GameTuningSettings.tutorial`（Inspector の【チュートリアルの設定】）が持ちます。

| 項目 | 既定 | 意味 |
| --- | --- | --- |
| `enableAutoSpawn` | false | 自動でタスクを出さない（案内の順に出題するため） |
| `enableTaskRush` | false | 一斉飛来を起こさない |
| `taskLifetimeSec` | 99 | タスクの制限時間。案内を読む間に期限切れにしない |
| `miniGameTimeLimitSec` | 99 | ミニゲームの制限時間。同じ理由 |
| `aiSuccessRate` | 1 | AI に任せた分を必ず成功させる |

**難易度は選ばれたものをそのまま使います。** チュートリアルは難易度の 1 つではないため、
`GameDifficulty` には追加していません（追加すると `FindMissingDifficultyProfiles()` が
「行が無い」と警告し、難易度として扱われてしまいます）。

## 画面の構造

```
Game.unity
└ MainCanvas
  └ TutorialOverlay        ← 最後の子。TutorialSequenceController を持つ
    ├ Content              ← ここが要（下記）
    │ ├ TutorialPanel
    │ ├ InstructionText    … 案内文
    │ └ ScreenAdvanceBotton … 画面クリックで進める透明ボタン
    ├ FocusMaskPanel       … 暗幕。クリックを受ける Button 付き
    └ ArrowPointer         … 対象を指す矢印
```

### `Content` を挟んである理由

`ShowInstruction` は `screenAdvanceButton.transform.SetAsLastSibling()` を呼びます。
**`Content` が無いと、この並べ替えで `ScreenAdvanceBotton` が `FocusMaskPanel` より前面に来て、
暗幕のクリックを奪います。** `Content` に閉じ込めることで前後関係が保たれます。

**この階層を崩さないでください。**

### シーン側で配線する 3 つ

Prefab はシーンオブジェクトを参照できないため、インスタンス側のオーバーライドで繋ぎます。

| | 配線先 |
| --- | --- |
| `mainGameController` | `GameManager` |
| `hudView` | `Hud` |
| `tabletSwitchButton` | `TabletTab` |

## ステップを足す・直すときの規則

### ハイライトは「待つステップ」が持つこと

**`SetStep` は冒頭で `ClearHighlight()` を呼びます。** 待たせる前のステップでハイライトを掛けると、
直後の `SetStep` に消されます。

```csharp
// 悪い例（実際にこれで詰んだ）
case TutorialStep.PromptTabletSwitch:
    HighlightObject(tabletSwitchButton.gameObject);
    SetStep(TutorialStep.WaitTabletSwitch);   // ← ここで消える
    break;

// 正しい形
case TutorialStep.PromptTabletSwitch:
    SetStep(TutorialStep.WaitTabletSwitch);
    break;

case TutorialStep.WaitTabletSwitch:
    HighlightObject(tabletSwitchButton.gameObject);
    break;
```

`canClickAdvance: false` のステップでこれをやると、**画面クリックでも進めないため詰みます。**

### `HighlightObject` が対象に何をするか

対象へ `Canvas`（`overrideSorting`, `sortingOrder = 100`）と `GraphicRaycaster` を**足します**。
`ClearHighlight` は、**自分で足したものは破棄し**、元からあったものは値だけ戻します。

**無効にして残す方式は使いません。** 残すと描画順が元に戻らないことがあります
（実例: HP バーが赤いまま残った）。

### 失敗したときは自動で出題し直される

`OnTaskResolved` が `WaitMiniGameClear` / `WaitAiProcess` などで面倒を見ています。
**失敗を握りつぶす分岐を足さないでください。** 過去にそれで進行不能になっています
（[振り返り §3-1](../Retrospective/2026-08-06-gamejam.md)）。

## 直したあとの確認

**「わざと失敗しながら最後まで通す」を必ず実行してください。** 成功しながら通すと見つからない
欠陥があります。手が要る不具合を測る手順は
[使い捨ての計装](../Operations/unity-pipeline.md)にあります。

**再生を挟んだ後は `TutorialOverlay.activeSelf` が `false` に戻っているか確かめてください。**
`true` のまま保存されると、通常プレイでチュートリアル UI が出ます。

### ハイライトできない対象は、暗幕ごと諦める

`RectTransform` が無い対象は矢印の位置を決められないため、`HighlightObject` は
**`ClearHighlight()` を呼んでから抜けます。** 矢印だけ消して `return` すると、
入口で出した暗幕が残り「何も指していない暗幕」が 1 ステップぶん出ます。

## 関連資料

- [チュートリアルの Game シーン統合](../Decisions/2026-08-07-tutorial-into-game-scene.md)
- [GameManager（Game シーンの配線）](game-manager.md)
- [シーン構造](scene-structure.md)

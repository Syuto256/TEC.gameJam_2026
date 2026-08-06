# チュートリアルを Game シーンへ統合し、Tutorial シーンを廃止する

日付: 2026-08-07
状態: 合意済み・未着手

## 背景

`Tutorial.unity` は `6036c3b`「チュートリアルの作成」で `Game.unity` を複製して作られた。
以降、**`Game.unity` は 18 回変更されたが、`Tutorial.unity` は 4 回しか追随していない。**
その 4 回もすべて横断作業のついでであり、意図的な同期ではない。

直近 5 件は完全に未反映である。

```
efab700  コンボ数とスコア表示、AIの成功確率のUIを本番用に
a172706  バブルがクリックできない問題を修正
7eb5ff7  タイミングゲームUI修正
3e22938  ゲーム終了後に余韻追加
4d86a85  一時停止中にPC切り替えボタンを無効化
```

ファイルサイズも逆転している（`Tutorial.unity` 212KB > `Game.unity` 195KB）。
複製後に `Game` 側で整理したものが `Tutorial` にだけ残っているためである。

**これは作業量の問題ではない。** 今 18 件ぶん追いつかせても、次に `Game.unity` を触った瞬間に
また古くなる。シーンを複製した時点で確定していた継続コストであり、手作業では原理的に追いつかない。

## 決定

**`Tutorial.unity` を廃止し、チュートリアルを `Game.unity` の 1 モードとして動かす。**

進め方のロジック（`TutorialSequenceController` 479 行）は**そのまま再利用する。**
今回変えるのは器だけであり、チュートリアルの体験には手を入れない。

### 決定事項

| | 決定 | 理由 |
| --- | --- | --- |
| A. 専用 UI の置き方 | `Assets/Prefabs/UI/TutorialOverlay.prefab` に 1 個へまとめる（**入れ子構造は下記**） | `Game.unity` は 50 回変更されている競合の常襲地帯。中身を直接足すと他者の変更とぶつかる |
| B. モードの持ち方 | `GameFlowController.IsTutorial` フラグ | `GameDifficulty` は `difficultyProfiles` と `HighScoreManager` に整数で保存されている。触らない |
| C. 非表示の方式 | 用途で使い分ける（→ `AGENTS.md`） | 既存 80 箇所の一括書き換えはしない。今後触る場所に適用する規約とする |
| D. 設定値の持ち主 | `GameTuningSettings.tutorial`（**`TutorialSettings` 型の専用ブロック**） | 下記の理由により「行」でも `DifficultyProfile` でもない |
| E. 導線 | 難易度選択画面に小さいボタンを置く | 追加素材が要らない。難易度と並列に見せない |

### D を「行」でも `DifficultyProfile` でもなくした理由

**理由 1: enum に足せない。** `difficultyProfiles` の行は `GameDifficulty` の値で引く。
Tutorial を enum に足すと `FindMissingDifficultyProfiles()` が `Enum.GetValues` を回して
「Tutorial の行が無い」と警告し、**チュートリアルが難易度の 1 つとして扱われる。**
決定 E（難易度と並列に見せない）と食い違う。

**理由 2: 実測したら `DifficultyProfile` の形ではなかった。**
2 つのシーンの `MainGameController` を全項目突き合わせた結果、**違うのは 3 フィールドだけ**だった。

```
enableAutoSpawn          Game=True   Tutorial=False
enableTaskRush           Game=True   Tutorial=False
overrideTaskLifetimeSec  Game=0      Tutorial=99
```

`GameTuningSettings` は**両シーンとも同じアセットを参照していた**。つまりチュートリアルは
専用の難易度プロファイルを必要としておらず、**選ばれた難易度の上で挙動だけを変えていた。**

そこで `TutorialSettings` という 5 項目の専用ブロックにした（上の 3 つ ＋ 下記の 2 つ）。
「設定値は `GameTuningSettings` が持つ」という決定 D の趣旨は変わらない。

### 併せて直した: モード判定の流用

`MainGameController` は **`overrideTaskLifetimeSec > 0f` を「チュートリアル中か」の判定に流用**していた。

| 箇所 | 流用していた判定 |
| --- | --- |
| `AiSuccessRate` | チュートリアルなら 100% にする |
| ミニゲームの制限時間 | チュートリアルなら 99 秒にする |
| タスクの生存時間 | **こちらが本来の用途** |

**制限時間の設定値が「今どのモードか」を兼ねていた。** 片方だけ変えるともう片方が黙って変わる。
前回の進行不能バグの握りつぶし（`overrideTaskLifetimeSec > 0f && !success`）と同じ式である。

`MainGameController.isTutorial` を足し、判定はこのフラグだけが持つようにした。
AI 成功率とミニゲーム制限時間は `TutorialSettings` の項目として明示した。

> **注意（08-04 の再発防止）**: Unity が全項目を 0 にするのは
> **Inspector でリストに行を足したとき**である。今回のように**新しいフィールドを足した場合は
> C# の初期値が使われる**（実測で確認済み）。とはいえ確認は省かないこと。

## 実測: 何を持ち出すか

> **訂正（2026-08-07）**: 当初この節は `Assets/Scenes/*.unity` の `m_Name:` を差分して書いていたが、
> **その方法は誤りだった。** Prefab インスタンスの子は YAML に名前を出さないため、
> `PC` `ScreenGlow` `Slot1` `Slot2` が「Tutorial にしかない残骸」に見えていた。
> **実際は `Game.unity` にも存在する。** 以下は Unity Editor 上で読んだ実際の階層である。

**シーンの実差分**

| | `Game.unity` | `Tutorial.unity` |
| --- | --- | --- |
| `Shared` の先頭 | **`FocusDimmer`** | 無い（`FocusLightingView` 改修が未反映） |
| `Shared` の末尾 | — | `TutorialPanel` `InstructionText` `ScreenAdvanceBotton` |
| `MainCanvas` 直下 | — | `FocusMaskPanel` `ArrowPointer`（**どちらも非アクティブ**） |
| シーンルート | — | `TutorialManager` |
| `TabletOnly` の子順 | Background → Tablet → ScreenGlow | Background → ScreenGlow → Tablet |

深い階層では `AISuccesPer` `AISuccesPer-Text` `ScoreAcce` `WindowGlow` も `Game` 側にしかない。
統合すればすべて解消する。

**持ち出す 6 個は 1 つのサブツリーではなく、3 箇所に散っていた。**

## Prefab の構造（決定 A の詳細）

`TutorialSequenceController` は層の制御に 2 つの方法を混在させている。

| 対象 | 制御方法 |
| --- | --- |
| `instructionText` | 実行時に `Canvas` を足して `sortingOrder = 101` |
| ハイライト対象 | 同じく `sortingOrder = 100` |
| `focusMaskPanel` / `arrowPointer` | **なし → 階層位置で決まる** |
| `screenAdvanceButton` | **`SetAsLastSibling()`（親の中で最後へ）** |

5 個を素直に 1 つの親の直下へ並べると、`SetAsLastSibling()` で
**`ScreenAdvanceBotton` が `FocusMaskPanel` より前面に来る。**
マスクはクリックを受ける `Button` を持つため、**入力の通り方が変わる。**

**`Content` を挟んで、並べ替えの影響をその中に閉じ込める。**

```
TutorialOverlay            MainCanvas の最後の子。TutorialSequenceController を持つ
├─ Content                 SetAsLastSibling の影響はこの中だけに閉じる
│   ├─ TutorialPanel
│   ├─ InstructionText
│   └─ ScreenAdvanceBotton
├─ FocusMaskPanel
└─ ArrowPointer
```

`TutorialOverlay` と `Content` は `MainCanvas` と同じ矩形（フルストレッチ）にする。
移動元の `Shared` も同じ矩形なので、**見た目は変わらない**（実測でずれ 0.0000）。

## シーン側で配線する参照

Prefab アセットはシーンオブジェクトを参照できない。**次の 3 つはインスタンス側のオーバーライドで配線する。**

| | 配線先 |
| --- | --- |
| `mainGameController` | `GameManager` |
| `hudView` | `Hud` |
| `tabletSwitchButton` | `TabletTab` |

残り 4 つ（`instructionText` `screenAdvanceButton` `focusMaskPanel` `arrowPointer`）は
Prefab の中で完結する。

## 作業手順

**前提: Unity Editor を起動しておくこと。** Pipeline は `Library/Pipeline/.unity-pipeline-port` が
無いと接続できない。

| | 作業 | 目安 |
| --- | --- | --- |
| 1 | `Tutorial.unity` から 6 個を `TutorialOverlay.prefab` へ切り出す | **済（2026-08-07）** |
| 2 | `GameFlowController` に `IsTutorial` を足し、`StartTutorial()` を `Game` シーンへ向ける | **済（2026-08-07）** |
| 3 | `GameTuningSettings.tutorial` を足し、`Tutorial.unity` の現行値を移す | **済（2026-08-07）** |
| 4 | `Game.unity` に `TutorialOverlay` を 1 個置き、`IsTutorial` が false なら `SetActive(false)` | **済（2026-08-07）** |
| 5 | 難易度選択にチュートリアルボタンを足す | **済（2026-08-07）** |
| 6 | 削除（下表） | **済（2026-08-07）** |
| 7 | 検証 | **済（2026-08-07）。下記の不具合 2 件を発見・修正** |

### 6 で削除するもの

| 対象 | 結果 |
| --- | --- |
| `Assets/Scenes/Tutorial.unity` | 削除。`EditorBuildSettings` は 5 シーンになった |
| `TutorialConfirmDialog.cs` | クラスごと削除 |
| `GameSettings.cs` | **中身が `ShowTutorialConfirm` だけだったのでファイルごと削除** |
| `OptionPanelView.showTutorialConfirmToggle` | フィールドとハンドラを削除 |
| `OptionPanel.prefab` の `TutorialRow` | **Prefab 側で削除**（Game / Title の両インスタンスに反映） |
| `DifficultySelect.unity` の `UIPanel` | 確認ダイアログの置き場ごと削除 |
| `GameFlowController.TutorialSceneName` | 定数ごと削除 |
| `DifficultySelectManager.Select` の分岐 12 行 | → 1 行 |

> `TutorialRow` は **`OptionPanel.prefab` の中にあった。** シーン側だけ直していたら
> もう一方のシーンに残っていた（`Game.unity` と `Title.unity` の両方がこの Prefab を使っている）。

## 追加した導線

`DifficultySelect.unity` の `/MainCanvas/TutorialButton`。
既存の「タイトルへもどる」（`/MainCanvas/Button`）を複製し、左下に対して**右下へ鏡写し**に置いた
（`anchoredPosition` の x だけ符号反転、481.7 × 96.5）。文言は「チュートリアルをあそぶ」。

**追加素材は使っていない。** 文言は `EnkaDotMincho24 SDF` の欠字を避け、カナと平仮名だけで書いた。
配線は `DifficultySelectManager.Start` が行う（他の難易度ボタンと同じ方式。
複製元から引き継いだ `onClick` は消してある）。

### 7 の検証（§3-1 の反省を適用）

**「チュートリアルをわざと失敗しながら最後まで通す」を必ず実行する。**
前回の進行不能バグはこの経路でしか出なかった。成功しながら通しても見つからない。

- [ ] 連打の難度 3 でわざと失敗し、窓が閉じて出題し直されること
- [ ] チュートリアル完了後、本編へ正しく遷移すること
- [ ] 通常の難易度で `TutorialOverlay` が非アクティブであること
- [ ] `tutorialProfile` の値が 0 でないこと（`maxHp` を特に確認）
- [ ] EditMode テストが通ること（`unity command run_tests --mode EditMode`）
- [ ] Console エラー 0 件

## 検証で見つかった不具合 2 件（統合とは別の、元からあったもの）

計測用の使い捨て MonoBehaviour を `TutorialOverlay` に貼り、ステップごとの状態を記録して 1 回通した。
**チュートリアル本体には触れずに外から読むだけ**とした。

### 7-1. タブレット切替で詰む

`SetStep` は**冒頭で `ClearHighlight()` を呼ぶ**。そこへこの順序だった。

```csharp
case TutorialStep.PromptTabletSwitch:
    ShowInstruction("『タブレット』を押してみましょう。", canClickAdvance: false);
    HighlightObject(tabletSwitchButton.gameObject);   // ハイライトを作り
    SetStep(TutorialStep.WaitTabletSwitch);           // 次の行で消していた
```

計測結果（修正前）:

```
step=WaitTabletSwitch | tabletTab=True/True/act=True | mask=False arrow=False
```

**タブが無効だったのではない。押せる状態のまま、指し示すものだけが消えていた。**
`canClickAdvance: false` のため画面クリックでも進めず、進行手段が無くなっていた。

**修正**: ハイライトを「待つ側」の `WaitTabletSwitch` の case へ移した。

> **規則**: ハイライトは**待つステップが持つ**こと。待たせる前のステップで掛けると
> 直後の `SetStep` に消される。

### 7-2. HP バーが途中からずっと赤い

`HighlightObject` は対象へ `Canvas`（`overrideSorting`, `sortingOrder = 100`）と
`GraphicRaycaster` を足すが、`ClearHighlight` は**無効にするだけで外していなかった**。
ステップ 12・13 の対象が `hpBarFill` そのもの（`HudView.HpBarObject`）であるため、
居残った `Canvas` が赤バー（`hpBarDamageFill`）との前後を狂わせていた。

塗り量は正常だった（計測で確認）。**描画順の問題である。**

**修正**: 足したのか元からあったのかを `addedCanvas` / `addedRaycaster` で覚え、
足したものは外す。`Destroy` ではなく `DestroyImmediate` を使う
（`Destroy` はフレーム終わりまで実体が残り、同フレームの再ハイライトで
`AddComponent` が null を返す。`Canvas` / `GraphicRaycaster` は `DisallowMultipleComponent`）。
外す順は `GraphicRaycaster` が先（`Canvas` を要求するため）。

修正後の計測:

```
ExplainDamage1   hpBar[ canvas=有効/ord=100/ovr=True ray=有 ]   ← ハイライト中
ExplainGameOver  hpBar[ canvas=無                    ray=無 ]   ← 外れた
```

### 7-3. 併せて直したもの

後始末が `if (currentHighlightedObject != null)` で丸ごと囲まれていた。
**対象（タスク吹き出し等）が先に破棄されると後始末が全部飛ぶ。** Component 側の null 判定で足りる。

### 残っている軽微なもの

`HighlightObject` は対象に `RectTransform` が無いと矢印を消して `return` するが、
**暗幕は出したまま抜ける**。進行は止まらないが「何も指していない暗幕」が出る。
計測では `ExplainTimeUp` で `mask=True arrow=False` として観測された。

## 効果

- **シーンが 1 本減り、今後ドリフトが発生しない。**
- `Tutorial.unity` 212KB ぶんの二重管理が消える。
- 削除が追加を上回る（クラス 1 個・トグル 1 個・分岐 12 行・シーン 1 本が消え、
  増えるのはフラグ 1 個・フィールド 1 個・Prefab 1 個・ボタン 1 個）。

## 関連資料

- [振り返り](../Retrospective/2026-08-06-gamejam.md) §3-1（検証）、§3-4（着手前に選択肢を出す）
- [実行時 UI 生成の全廃](2026-08-04-remove-runtime-ui-construction.md) — 同じ「複製を消し切る」判断

# タイピングのお題まわりを組み直す（作業指示書）

作成: 2026-08-06 ／ 対象: [統合作業計画](2026-08-06-minigame-overhaul-integrated.md) の 1-C

**表示だけの変更です。** ゲームの進行・判定・難度の計算には手を入れません。

> **⚠ [共通ウィンドウの素材差し替え](2026-08-06-minigame-window-art.md) のあとに着手してください。**
> 窓の下地が変わると、ここで決める文字色や配置の判断が変わります。
> **制限時間バーはそちらへ移しました。** この指示書には含まれません。
>
> **これがタイピングの本組みです。** 発注書 §6 のとおり個別素材が要らないため、
> 素材待ちになりません（発注書 §12-1 の「文書レイアウトとして組み直す」がこの作業です）。
>
> **連打は [別の指示書](2026-08-06-rapidclick-rework.md) へ移しました。**
> 仮素材の作成とファイル切り替えのコードが加わり、量が変わったためです。

## 作業前に必ず読むもの

- [AGENTS.md](../../AGENTS.md) の「先に読むこと: 過去に手戻りが起きた箇所」
- 見た目は Prefab、進行はコード。**座標と文字サイズを C# に書かないでください。**

## 触らないもの

- 判定ロジック（`TypingInputEvaluator` / `TypingCandidateBuilder` / `RomanizationGenerator`）
- 問題データ（`TypingQuestionDatabase`）
- `AppWindowFrame` の `TitleBar` / `MenuBar` / `StatusBar` / `Border` の見た目
- 仕分け（`SortingMiniGame`）。**別 PR です。**

---

# 1. タイピング: お題まわりを組み直す

## 現状

`TypingMiniGame.prefab` の `AppWindowFrame/WorkArea` の中身です。

| 表示 | 文字サイズ | 枠 | 位置 | 出す内容 |
| --- | --- | --- | --- | --- |
| `QuestionText` | 99.4 | 864×77 | (0, 0) | `お題: {漢字}` |
| `TargetRomanizationText` | 54.7 | 864×47 | **(0, -134)** | `ローマ字: {綴り全体}` |
| `AcceptedInputText` | 54.7 | 864×56 | **(0, -134)** | `入力済み: {打てた分}` |
| `RemainingInputText` | 34 | 864×56 | (0, -152) | `残り: {これから}` |

**`TargetRomanizationText` と `AcceptedInputText` は同じ位置に重なっています。**
3 行が 2 段ぶんの高さに押し込まれており、今も読みにくい状態です。

## 目指す形

```
        よみ（ひらがな・小さく薄く）      ← 新規
        お  題                            ← 今より大きく
        すでに打てた分 これから打つ分      ← 3 つを 1 行に統合
```

## やること

### 1-1. 読み（ひらがな）の行を足す

`TypingQuestion.reading` に**ひらがなの読みが既に入っています。**（`TypingQuestionDatabase.cs` を参照）
データ側の作業は要りません。

- `WorkArea` に `ReadingText` を新規作成し、`QuestionText` の**上**に置く
- 文字サイズは 32 前後、色は薄く（お題の 40〜50% の不透明度）
- `TypingMiniGame` に `[SerializeField] private TMP_Text readingText;` を足して割り当てる

**`reading` が空のときは行ごと隠してください。** 英単語のお題には読みがありません。

### 1-2. 入力表示を 1 行に統合する

`TargetRomanizationText` / `AcceptedInputText` / `RemainingInputText` の **3 つを廃止し、
`SpellingText` 1 つ**にまとめます。

打てた分と残りは、**リッチテキストの色分けで区別**します。

```csharp
// 打てた分と、これから打つ分をひと続きに出す。
// 別々の行に分けると、どこまで打てたのかを目で追う手間が増える。
spellingText.text = "<color=#" + acceptedHex + ">" + evaluator.AcceptedInput + "</color>"
    + "<color=#" + remainingHex + ">" + evaluator.RemainingInput + "</color>";
```

- 色は `[SerializeField]` で 2 つ持たせる（打てた分 / これから打つ分）
- **打ち間違い直後の `lockedOutColor` は、これから打つ分の側に効かせてください。**
  今の `ApplyLockoutColor()` と同じ役割です。色の指定先がテキスト全体から
  リッチテキストのタグへ変わります。
- 文字サイズは 54 前後。お題より小さく、今の `RemainingInputText`（34）よりは大きく

**`RefreshHint()` は削除できます。** 英単語のお題で「ローマ字:」の行を隠す処理でしたが、
統合後のこの行は常に必要なので、隠す条件そのものが無くなります。

### 1-3. 書式の項目を整理する

統合で不要になるものを消します。

- `targetRomanizationFormat` / `acceptedInputFormat` / `remainingInputFormat` → **削除**
- `questionFormat`（`"お題: {0}"`）→ **`"{0}"` にする。**
  読みの行が上に付くので、「お題:」の見出しは要りません
- `missFormat` は**そのまま**（`StatusBar/MissText` で使っています）

### 1-4. 配置をやり直す

3 行が縦に並ぶよう、`WorkArea` 内の位置を取り直してください。**重なりを残さないこと。**
今の (0,-134) が 2 つある状態が再現しないよう、保存後に位置を読み直して確認します。

---

## 受け入れ条件

- [ ] 読み・お題・入力の 3 行が**重ならずに**縦に並ぶ
- [ ] 打つほど、打てた分の色が左から増えていくのが 1 行の中で分かる
- [ ] 打ち間違えた直後、これから打つ部分の色が変わる
- [ ] 英単語のお題（読みが空）で、読みの行が出ない
- [ ] `unity command run_tests --mode EditMode` が全件通る（**73 件**）
- [ ] コンソールにエラー・例外が 0 件

## 目視で見てほしいところ

- お題が長いとき、枠（864 幅）からはみ出さないか
- **チュートリアルでもタイピングが正しく出るか。** チュートリアルは制限時間 99 秒で動くため、
  `TimeLimit >= 90f` の分岐（`TypingMiniGame.ProcessInput`）に入ります

---

## 報告してほしいもの

1. `git diff --stat`
2. `run_tests` の通過数
3. コンソールのエラー・例外の件数

指示と食い違う状態を見つけた場合は、直す前に報告してください。
この指示書は 2026-08-06 時点の実測に基づいています。

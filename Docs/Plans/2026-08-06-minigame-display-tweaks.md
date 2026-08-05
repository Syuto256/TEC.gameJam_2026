# ミニゲームの表示を読みやすくする（作業指示書）

作成: 2026-08-06 ／ 対象: [post-merge-worklist](2026-08-06-post-merge-worklist.md) の 4-1

**表示だけの変更を 1 PR にまとめたものです。** 仕分けの作り直し（4-2）とは独立しています。
ゲームの進行・判定・難度の計算には手を入れません。

> **⚠ [共通ウィンドウの素材差し替え](2026-08-06-minigame-window-art.md) のあとに着手してください。**
> 窓の下地が変わると、ここで決める文字色や配置の判断が変わります。
> **制限時間バーはそちらへ移しました。** この指示書には含まれません。
>
> **位置づけ（[発注書](../Specifications/minigame-ui-assets.md) §12 との関係）:**
> 発注書は素材到着後に WorkArea を本組みする計画を持っています。
> - **タイピングはこの指示書が本組みです。** 発注書 §6 のとおり個別素材が要らないため、
>   素材待ちになりません
> - **連打はこの指示書は暫定です。** 本組み（§12-1 プレビュー + ファイル情報、§12-2 の
>   ファイル切り替え）は `Preview_01〜08` / `PreviewFrame` の納品待ち（2026-08-06 時点で 0/9）。
>   ここで作る大きい残り回数は、本組み時にも中央の要素として残せる形にしてあります

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

# 2. 連打: 残り回数を大きく出す

## 現状

**`WorkArea` に文字が 1 つもありません。** 回数は `StatusBar/ProgressText`
（文字サイズ **22** / 枠 216×40）に `連打! {今の回数} / {必要回数}` として出ています。

必要回数は `baseClicks 8 + (レベル-1) × clicksPerLevel 3` です（Prefab の値）。

## やること

`WorkArea` の中央に**残り回数だけを大きく**出します。

- `RemainingClicksText` を新規作成。文字サイズ **160〜200**
- 出すのは**残り回数の数字だけ**（`必要回数 - 今の回数`）。単位も見出しも付けない
- `TypingMiniGame` と同じく `[SerializeField]` で `TMP_Text` を受け、`Refresh()` で更新する

**`StatusBar/ProgressText`（`連打! n / m`）はそのまま残してください。**
大きい数字は残り、小さい行は全体像を出す、という役割分担にします。

### 注意

依頼の文面が「残り連打数」だったため、**大きい数字は残り回数として実装します。**
今の `progressText` は「打った回数 / 必要回数」なので、意味が逆です。混同しないでください。

### 任意

新しい数字にも `RapidMashTextEffect` を付けると、1 回ごとに跳ねます。
**既に窓全体を揺らす `RapidMashTextEffect` がルートに付いているので、
二重に揺らすと読めなくなる可能性があります。** 付けるなら `strength` は小さめにして、
目視で確かめてください。

---

## 受け入れ条件

- [ ] タイピングで、読み・お題・入力の 3 行が**重ならずに**縦に並ぶ
- [ ] 打つほど、打てた分の色が左から増えていくのが 1 行の中で分かる
- [ ] 打ち間違えた直後、これから打つ部分の色が変わる
- [ ] 英単語のお題（読みが空）で、読みの行が出ない
- [ ] 連打で、残り回数が大きく出て 1 回ごとに減る。**0 になる前にクリアする**
- [ ] `unity command run_tests --mode EditMode` が全件通る（**67 件**）
- [ ] コンソールにエラー・例外が 0 件

## 目視で見てほしいところ

- タイピングのお題が長いとき、枠（864 幅）からはみ出さないか
- 連打の大きい数字が 2 桁になったとき、窓からはみ出さないか
- **チュートリアルでもタイピングが正しく出るか。** チュートリアルは制限時間 99 秒で動くため、
  `TimeLimit >= 90f` の分岐（`TypingMiniGame.ProcessInput`）に入ります

---

## 報告してほしいもの

1. `git diff --stat`
2. `run_tests` の通過数
3. コンソールのエラー・例外の件数

指示と食い違う状態を見つけた場合は、直す前に報告してください。
この指示書は 2026-08-06 時点の実測に基づいています。

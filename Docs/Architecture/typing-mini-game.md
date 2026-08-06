# クラス詳細: タイピングミニゲーム

最終更新: 2026-08-06  
実装: `Assets/Scripts/MiniGameS/Typing/TypingQuestionDatabase.cs`, `RomanizationGenerator.cs`, `TypingInputEvaluator.cs`, `TypingMiniGame.cs`  
Prefab: `Assets/Prefabs/MiniGames/TypingMiniGame.prefab`

## 責務

タイピング問題データ、読みからのローマ字生成、前方一致の判定、入力進捗の画面表示を持つ。

## 問題データが持つのは読みだけである

**ローマ字は問題データに書かない。** 読み（ひらがな）から `RomanizationGenerator` が実行時に全部作る。代表の綴りを候補の先頭に置き、入力判定と表示で利用する。

```text
「新聞」 + 読み「しんぶん」
   -> shinbun / sinbun / shinnbun / sinnbun / cinbun / …
```

訓令式とヘボン式、促音・撥音・拗音の打ち方はすべて生成側が持つ。問題を追加する人は読みを1つ書くだけでよい。

## 画面表示

- `ReadingText` に読みを表示する。問題データの `reading` が空なら非表示にする。
- `SpellingText` に入力済み部分と残り部分を1行のリッチテキストで表示する。入力済みは `acceptedInputColor`、残りは `remainingInputColor`、入力無効中の残りは `lockedOutColor` を使う。
- 文字サイズ・配置・背景は `TypingMiniGame` Prefab の子オブジェクトが持ち、入力ロジックは表示レイアウトを持たない。

## 打ち間違えた直後は入力を捨てる

打ち間違えると `missLockoutSeconds`（既定 0.2 秒）のあいだ、すべてのキー入力を無視する。ミス数も進捗も動かさない。無効中は `SpellingText` の残り部分を `lockedOutColor` で表示する。

## 公開契約

| API / イベント | 意味 |
| --- | --- |
| `TypingQuestionDatabase.TryGetRandomQuestion` | レベル1〜4の有効な問題を1件選ぶ。 |
| `TypingQuestionDatabase.FindUnplayableQuestions` | 読みからローマ字を作れない問題を列挙する。 |
| `RomanizationGenerator.TryGenerate` | 読みから打てる綴りをすべて作る。 |
| `TypingInputEvaluator.TryInput` | 候補のいずれかの前方一致を保つ入力だけを受け付ける。 |
| `TypingMiniGame.ProcessInput` | 入力進捗と失敗判定を更新する。無効時間中は `false` を返す。 |
| `TypingMiniGame.IsInputLocked` | 打ち間違えた直後で入力を受け付けない状態か。 |

## データと設定

| 項目 | 場所 |
| --- | --- |
| 問題集 | Prefab 上の `TypingMiniGame.database`（`Assets/Data/MiniGames/Typing/TypingQuestionDatabase.asset`） |
| 制限時間 | `Assets/Data/MiniGameCatalog.asset` の `Typing` 行 |
| 許容ミス数・無効時間・文字色 | Prefab 上の `TypingMiniGame` |
| 読み・綴り・背景の配置 | Prefab の `Question` / `WorkArea` 子オブジェクト |

## 検証と TODO

- EditMode テストでローマ字生成、前方一致、撥音・促音・長音、未対応文字の扱いを確認する。
- `TypingQuestionDatabaseAssetTests` で出題対象の問題からローマ字を生成できることを確認する。
- `TypingMissLockoutTests` で入力無効時間中の進捗とミス数を確認する。
- TODO: IME のオン・オフ両方で実キーボード入力を確認する。

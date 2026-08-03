# クラスカタログ

最終確認: 2026-08-03  
このページは実装済みクラスの索引です。設計意図、外部契約、状態遷移が複雑になったクラスは `../Templates/class-detail.md` を元に個別ページを作成してください。

| クラス | パス | 役割 | 主な依存先 |
| --- | --- | --- | --- |
| `TracingMiniGame` | `Assets/Personal/Suzuki/TracingMiniGame/Scripts/TracingMiniGame.cs` | New Input System のマウス入力で経路逸脱・開始／終点を判定する個人用なぞる試作 | `MiniGameBase`, Input System, uGUI |
| `GameManager` | `Assets/Scripts/Core/GameManager.cs` | HP / スコアと、現在のミニゲーム完了結果を管理する | `GameTuningSettings`, `MiniGameBase` |
| `GameTuningSettings` | `Assets/Scripts/Core/GameTuningSettings.cs` | ゲーム全体の調整値を保持する ScriptableObject | UnityEngine |
| `MiniGameBase` | `Assets/Scripts/Core/MiniGameBase.cs` | ミニゲーム共通のライフサイクル、制限時間、完了イベントを提供する抽象基底クラス | UnityEngine, `System.Action` |
| `RapidClickMiniGame` | `Assets/Scripts/MiniGameSample/RepidClickMiniGame.cs` | 連打数を満たすサンプルのミニゲーム実装 | `MiniGameBase`, TextMeshPro, Input |
| `TestRunner` | `Assets/Scripts/MiniGameSample/TestRunner.cs` | `RapidClickMiniGame` を単独起動して結果をログ出力する試験用コンポーネント | `RapidClickMiniGame` |
| `TypingMiniGame` | `Assets/Personal/Suzuki/TypingMiniGame/Scripts/TypingMiniGame.cs` | 日本語読みのローマ字入力を判定し、共通ミニゲーム結果を通知する個人試作 | `MiniGameBase`, `TypingWordDatabase`, `TypingInputEvaluator` |
| `TypingWordDatabase` / `TypingWordEntry` | `Assets/Personal/Suzuki/TypingMiniGame/Scripts/TypingWordDatabase.cs` | 表示文字列・読み・出現難易度を持つタイピング問題を集約する ScriptableObject | UnityEngine |
| `RomanizationGenerator` | `Assets/Personal/Suzuki/TypingMiniGame/Scripts/RomanizationGenerator.cs` | ひらがなから許容ローマ字候補と表示用の代表候補を生成する | System collections |
| `TypingInputEvaluator` | `Assets/Personal/Suzuki/TypingMiniGame/Scripts/TypingInputEvaluator.cs` | 候補群に対する文字単位の入力進捗を Unity 非依存で判定する | `RomanizationGenerator` の出力 |
| `TypingMiniGameView` | `Assets/Personal/Suzuki/TypingMiniGame/Scripts/TypingMiniGameView.cs` | お題・ローマ字・進捗・ミス・時間・結果を TextMeshPro に表示する | TextMeshPro |
| `MiniGameDebugRunner` | `Assets/Personal/Suzuki/TypingMiniGame/Scripts/MiniGameDebugRunner.cs` | 任意のミニゲームを Inspector 設定で単体起動する再利用可能なデバッグ補助 | `MiniGameBase`, TextMeshPro |

> ファイル名は現状 `RepidClickMiniGame.cs`（`Rapid` のスペルではない）ですが、クラス名は `RapidClickMiniGame` です。リネームする場合は Unity Editor / Pipeline 経由で参照と `.meta` を保ったまま行ってください。

## GameManager

- `Start` で `settings.maxHP` を初期 HP に設定する。
- `currentMiniGame` の `OnCompleted` を購読し、難易度 1 と `settings.miniGameTimes.rapidClick` で初期化する。
- 成功時は `settings.score.baseScoreDiff1` を加算し、失敗時は `settings.damage.playerFail` を HP から減算する。
- 完了を受け取った後にイベント購読を解除する。

**注意:** 現時点では次のミニゲームへの遷移、ゲーム終了判定、`settings` 未設定時の保護は実装されていません。これは仕様未確定のため、勝手に補完しないでください。

## GameTuningSettings

`Assets/Data/GameTuningSettings.asset` に現在の実体があります。以下を一つのアセットに保持します。

- プレイ時間、最大 HP
- プレイヤー・AI・時間切れのダメージ
- AI の成功率、処理時間、クールダウン、倍率
- 難易度別のスコアとクラフトポイント、時間ボーナス
- タイピング、ドラッグ&ドロップ、QTE、タイミング、連打、トレースの制限時間

**契約:** 実行中に参照される共有調整値は、この型を通して渡します。値の意味や単位を変える場合は、仕様資料も更新してください。

## MiniGameBase

- `Initialize(difficulty, timeLimit)` が初期状態を設定し、`IsPlaying` を有効にする。
- `Update` が残り時間を減算し、派生クラスの `OnUpdate(deltaTime)` を実行する。
- 時間切れは `FinishGame(false, "TIME OUT")` で通知する。
- 派生クラスは成功・失敗を `FinishGame(success, reason)` で一度だけ通知する。
- `OnDestroy` でイベント購読をクリアする。

**契約:** 派生クラスは `OnUpdate` を実装し、完了処理を独自イベントで重複させません。`Initialize` をオーバーライドする場合は原則として `base.Initialize` を呼びます。

## RapidClickMiniGame

- 難易度から必要連打数を `8 + difficulty * 4` として設定する。
- 左クリック、または UI Button から呼べる `OnClick` で連打数を増やす。
- 必要数に到達すると `COMPLETE` で成功通知する。
- `TMP_Text` が割り当てられている場合、進捗と残り時間を表示する。

**注意:** Unity の Input System パッケージは導入されていますが、このサンプルは `Input.GetMouseButtonDown` を利用しています。入力方針は仕様決定後に統一します。

## TestRunner

- 起動時に指定した `RapidClickMiniGame` を難易度 1・5 秒で初期化する。
- 完了イベントを受け、結果を Console に出力する。

**位置付け:** 本番進行用ではなくサンプル検証用です。`GameManager` と同一シーンで同じミニゲームを同時に制御しないでください。

## TypingMiniGame（個人試作）

- `MiniGameBase` を継承し、難易度に一致する `TypingWordDatabase` の問題をランダムに選ぶ。
- ひらがなの読みから生成した複数ローマ字候補を `TypingInputEvaluator` に渡し、どの候補にも一致しない入力をミスとする。
- ミス後は 0.2 秒間の入力無効を設け、2 ミスで `MISSED`、完了で `COMPLETE` を通知する。
- 本編のスクリプトを変更しない個人試作であり、本編との接続は未実装である。

## TracingMiniGame（個人試作）

- `MiniGameBase` を継承し、デバッグ設定の制限時間で単体起動する。
- New Input System の `Mouse.current.leftButton` とポインタ位置を使用し、左クリック中だけなぞり判定する。
- 開始点以外でのクリックは無視し、クリック解除はミス数を増やさず試行を開始点へ戻す。
- 経路から許容距離を超えると 1 ミスとして試行をリセットし、2 ミスで `MISSED` を通知する。
- 終点半径内へ到達すると `COMPLETE` を通知する。精度や所要時間は成功条件に含めない。

## MiniGameDebugRunner（個人試作）

- 任意の `MiniGameBase` を指定難易度・制限時間で初期化し、成功／失敗理由を表示する。
- `TypingMiniGame` 専用ではなく、同じ共通契約を満たす別ミニゲームにも利用できる。

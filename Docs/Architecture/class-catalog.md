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

## M1: Shared game progression model (2026-08-03)

| Class | Path | Responsibility | Dependencies |
| --- | --- | --- | --- |
| `TaskModel` / `TaskInstance` | `Assets/Scripts/Core/TaskModel.cs` | Defines task type, state, resolution, and runtime task values. | System |
| `TaskManager` | `Assets/Scripts/Core/TaskManager.cs` | Manages task ownership, expiration, AI processing, and one-time resolution notifications. | `TaskModel` |
| `GameSession` | `Assets/Scripts/Core/GameSession.cs` | Manages HP, score, time, and Clear/GameOver state. | `TaskModel` |
| `MiniGameCatalog` | `Assets/Scripts/Core/MiniGameCatalog.cs` | Maps task types to mini-game Prefabs, icons, and per-level time limits. | UnityEngine, `MiniGameBase` |
| `GameTuningSettings.DifficultyProfile` | `Assets/Scripts/Core/GameTuningSettings.cs` | Stores difficulty start parameters; empty assets use legacy fallback values. | UnityEngine, `GameDifficulty` |

See [TaskManager detail](task-manager.md).

## M2: Scene flow and Canvas foundation (2026-08-03)

| Class | Path | Responsibility | Dependencies |
| --- | --- | --- | --- |
| `GameFlowController` | `Assets/Scripts/Core/GameFlowController.cs` | Keeps selected difficulty and final result across scenes; opens the five shared scenes. | Unity SceneManager, `GameSessionResult` |
| `SceneUiBootstrap` | `Assets/Scripts/Core/SceneUiBootstrap.cs` | Builds the P0 overlay Canvas, EventSystem, scene buttons, and Game placeholder regions. | uGUI, Input System, TextMeshPro |

See [Game flow and UI detail](game-flow-controller.md).

## M3: Task UI and AI integration (2026-08-03)

| Class | Path | Responsibility | Dependencies |
| --- | --- | --- | --- |
| `MainGameController` | `Assets/Scripts/Core/MainGameController.cs` | Creates a session, spawns tasks, applies task results, refreshes HUD, and finishes the session. | `GameTuningSettings`, `TaskManager`, `GameSession`, `GameFlowController` |
| `TaskBubbleView` | `Assets/Scripts/Core/TaskBubbleView.cs` | Writes one task's kind, state, and remaining lifetime into the prefab's widgets, and routes left/right pointer clicks to the controller. Holds no position or size. | uGUI, TextMeshPro, EventSystem, `TaskInstance` |

See [Main game controller detail](main-game-controller.md).

## M4: Shared typing mini-game integration (2026-08-03)

| Class | Path | Responsibility | Dependencies |
| --- | --- | --- | --- |
| `IPlayerMiniGameLauncher` | `Assets/Scripts/Core/IPlayerMiniGameLauncher.cs` | Defines the minimal Core-to-feature launch contract. | `TaskKind`, UnityEngine |
| `TypingQuestionDatabase` / `TypingQuestion` | `Assets/Scripts/MiniGameS/Typing/TypingQuestionDatabase.cs` | Stores levelled Japanese display text and allowed Romanizations. | UnityEngine |
| `TypingInputEvaluator` | `Assets/Scripts/MiniGameS/Typing/TypingInputEvaluator.cs` | Evaluates an input prefix against multiple allowed Romanizations. | System |
| `TypingMiniGame` | `Assets/Scripts/MiniGameS/Typing/TypingMiniGame.cs` | Receives keyboard text input, displays progress, and produces one completion result. | `MiniGameBase`, Input System, TextMeshPro |
| `TypingMiniGameLauncher` | `Assets/Scripts/MiniGameS/Typing/TypingMiniGameLauncher.cs` | Builds the temporary host UI and adapts completion into the Core contract. | `IPlayerMiniGameLauncher`, `TypingMiniGame` |

See [Shared typing mini-game detail](typing-mini-game.md).

## M5: Shared tracing mini-game integration (2026-08-03)

| Class | Path | Responsibility | Dependencies |
| --- | --- | --- |
| `TracingPathDatabase` / `TracingPathEntry` | `Assets/Scripts/MiniGameS/Tracing/TracingPathDatabase.cs` | Stores normalized guide paths and allowed deviations by level. | UnityEngine |
| `TracingPathMath` | `Assets/Scripts/MiniGameS/Tracing/TracingPathMath.cs` | Calculates point-to-polyline distance without physics. | UnityEngine |
| `TracingMiniGame` | `Assets/Scripts/MiniGameS/Tracing/TracingMiniGame.cs` | Draws a guide and resolves start, release, deviation, end, and time-limit states. | `MiniGameBase`, Input System, uGUI, TextMeshPro |
| `TracingMiniGameLauncher` | `Assets/Scripts/MiniGameS/Tracing/TracingMiniGameLauncher.cs` | Adapts the tracing feature to `IPlayerMiniGameLauncher`. | Core, tracing data |

See [Shared tracing mini-game detail](tracing-mini-game.md).

## M6: Existing mini-game connection (2026-08-04)

| Class | Path | Responsibility | Dependencies |
| --- | --- | --- | --- |
| `RapidClickMiniGame` / `RapidClickMiniGameLauncher` | `Assets/Scripts/MiniGameS/RapidClick/` | Connects the rapid-click vertical slice to `TaskKind.RapidClick` and the common completion path. | Core, `MiniGameBase`, uGUI, TextMeshPro |
| `SortingMiniGame` / `SortingMiniGameLauncher` | `Assets/Scripts/MiniGameS/DragDrop/` | Connects the Motonaga-inspired sorting slice to `TaskKind.DragDrop`; owns uGUI card dragging and two-miss resolution. | Core, `MiniGameBase`, uGUI, EventSystem, TextMeshPro |
| `SortingDraggable` / `SortingDropBox` | `Assets/Scripts/MiniGameS/DragDrop/SortingMiniGame.cs` | Routes card drag and drop events into `SortingMiniGame` without physics. | uGUI, EventSystem |

See [Shared rapid-click mini-game detail](rapid-click-mini-game.md) and [Shared drag-and-drop mini-game detail](drag-drop-mini-game.md).

## M6: Audio foundation (2026-08-04)

| Class | Path | Responsibility | Dependencies |
| --- | --- | --- | --- |
| `AudioCatalog` / `AudioCue` | `Assets/Scripts/Core/AudioCatalog.cs` | Maps named BGM/SFX cues to optional clips and volumes. | UnityEngine |
| `AudioManager` | `Assets/Scripts/Core/AudioManager.cs` | Owns persistent BGM/SFX sources, scene BGM selection, and safe cue playback. | Unity audio, SceneManager, `GameFlowController` |

See [Shared audio manager detail](audio-manager.md).

## M7: Game screen layout（承認済み設計・再構成中）

ゲーム画面の承認済み構成は [Game 画面レイアウト案](../GameDesign/game-screen-layout.md) を参照する。シーン上のレイアウトとゲーム進行ロジックの分離は、[DeviceScreenController の設計](device-screen-controller.md) と [GameSceneUiReferences の設計](game-scene-ui-references.md) に従う。

| クラス | パス | 役割 | 主な依存先 |
| --- | --- | --- | --- |
| `GameSceneUiReferences` | `Assets/Scripts/Core/GameSceneUiReferences.cs` | Scene 上の View と Controller を接続するだけの入口。ウィジェット参照は持たない。 | 各 View, `MainGameController`, `DeviceScreenController` |
| `DeviceScreenController` | `Assets/Scripts/Core/DeviceScreenController.cs` | PC / Tablet ワークスペースの排他表示と切替可否を決める。 | `DeviceTabsView`, `TaskSurface` |
| `HudView` / `HudSnapshot` | `Assets/Scripts/Core/UI/HudView.cs` | HP バー・残り時間・スコア・難易度の表示と Pause 要求。時刻書式もここが持つ。 | uGUI, TextMeshPro |
| `DeviceTabsView` | `Assets/Scripts/Core/UI/DeviceTabsView.cs` | PC / Tablet タブの外観と入力。どちらを表示するかは決めない。 | uGUI, `TaskSurface` |
| `MiniGameHostView` | `Assets/Scripts/Core/UI/MiniGameHostView.cs` | 共通ミニゲーム領域の表示状態と Prefab 生成先の提供。 | UnityEngine |
| `PauseMenuView` | `Assets/Scripts/Core/UI/PauseMenuView.cs` | ポーズ／オプションのパネル表示とボタン入力。ゲーム進行は判断しない。 | uGUI |
| `SceneUiValidation` | `Assets/Scripts/Core/UI/SceneUiValidation.cs` | 各 View の必須参照不足を、フィールド名を列挙して一度に報告する。 | UnityEngine |

### タスク吹き出しの調整場所

| 調整対象 | 場所 |
| --- | --- |
| 大きさ、背景、フォント、文字配置、寿命バーの有無 | `Assets/Prefabs/UI/TaskBubble.prefab` |
| 状態別の配色（未着手 / 自力 / AI / 解決済み） | Prefab 上の `TaskBubbleView` の State colors |
| 状態別の表示文字列 | Prefab 上の `TaskBubbleView` の State labels |
| 縦位置、間隔、余白、並ぶ向き | 各 `TaskSpawnArea` の Layout Group |

`TaskSpawnArea` には `VerticalLayoutGroup`（`childAlignment = MiddleCenter`）を付け、吹き出しの並びを出現エリアの中央へ固定する。`HorizontalLayoutGroup` へ差し替えれば横並びになり、コードは変更しない。Layout Group は子の `anchorMin` / `anchorMax` / `anchoredPosition` を driven property として支配するため、Prefab 側でアンカーを設定しても位置には影響しない。大きさは `childControlWidth` / `childControlHeight` を無効にしているため Prefab の `sizeDelta` が保たれる。

`MainGameController` は `taskBubblePrefab` を Inspector で保持し、生成時に親を指定して `Bind` するだけである。座標・サイズ・配色をゲーム進行コードへ戻さないこと。

**契約:** View は `Awake` ではなく `Initialize()` で自身のボタンを配線する。`PausePanel` のように非表示で開始する枝に置かれた場合、`Awake` は走らないためである。`MiniGameHostView.ContentRoot` は Launcher へ渡す親であり、Launcher はこの下の子だけを破棄する。ホスト直下に見出しや装飾を置く場合は `contentArea` の外に置くこと。

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

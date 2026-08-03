# クラスカタログ

最終確認: 2026-08-04  
このページは実装済みクラスの索引です。設計意図、外部契約、状態遷移が複雑になったクラスは `../Templates/class-detail.md` を元に個別ページを作成してください。

はじめて読む場合は [シーン構造](scene-structure.md) を先に読んでください。「どこを触れば何が変わるか」はそちらにまとまっています。

## シーンの入口

| クラス | パス | 役割 | 主な依存先 |
| --- | --- | --- | --- |
| `AppServices` | `Assets/Scripts/Core/AppServices.cs` | 常駐サービス（`GameFlowController` / `AudioManager`）をそろえる静的クラス。各 Manager が `Start` の先頭で呼ぶ。 | `GameFlowController`, `AudioManager` |
| `TitleManager` | `Assets/Scripts/Core/TitleManager.cs` | Title シーンの入口。Start ボタンを難易度選択へつなぐ。 | uGUI, `GameFlowController` |
| `DifficultySelectManager` | `Assets/Scripts/Core/DifficultySelectManager.cs` | DifficultySelect シーンの入口。ボタンと `GameDifficulty` の対応を配列で持つ。 | uGUI, `GameFlowController` |
| `GameManager` | `Assets/Scripts/Core/GameManager.cs` | Game シーンの入口。View と Controller を接続するだけで、ウィジェット参照は持たない。 | 各 View, `MainGameController`, `DeviceScreenController` |
| `ResultManager` | `Assets/Scripts/Core/ResultManager.cs` | Clear / GameOver シーンの入口。直前の結果を書き、ボタンをつなぐ。両シーンで同じクラスを使う。 | uGUI, TextMeshPro, `GameSessionResult` |
| `GameFlowController` | `Assets/Scripts/Core/GameFlowController.cs` | 選択難易度と終了結果をシーン間で保持し、5 シーンを遷移する。 | Unity SceneManager, `GameSessionResult` |

詳細は [シーン遷移と各シーンの入口](game-flow-controller.md)。

## 進行モデル（Unity 非依存）

| クラス | パス | 役割 | 主な依存先 |
| --- | --- | --- | --- |
| `TaskModel` / `TaskInstance` | `Assets/Scripts/Core/TaskModel.cs` | タスクの種別・状態・解決結果と実行時の値を定義する。 | System |
| `TaskManager` | `Assets/Scripts/Core/TaskManager.cs` | タスクの所有、寿命切れ、AI 処理、一度きりの解決通知を管理する。 | `TaskModel` |
| `GameSession` | `Assets/Scripts/Core/GameSession.cs` | HP・スコア・残り時間と Clear / GameOver 状態を管理する。 | `TaskModel` |
| `GameTuningSettings` | `Assets/Scripts/Core/GameTuningSettings.cs` | ゲーム全体の調整値と難易度プロファイルを保持する ScriptableObject。 | UnityEngine |

詳細は [TaskManager](task-manager.md)。

## ゲーム進行とミニゲーム接続

| クラス | パス | 役割 | 主な依存先 |
| --- | --- | --- | --- |
| `MainGameController` | `Assets/Scripts/Core/MainGameController.cs` | セッションを作り、タスクを生成し、結果を適用し、HUD を更新し、終了遷移する。ミニゲームはカタログから引いて `MiniGameHost` に生成する。 | `GameTuningSettings`, `TaskManager`, `GameSession`, `MiniGameCatalog`, `GameFlowController` |
| `MiniGameCatalog` | `Assets/Scripts/Core/MiniGameCatalog.cs` | タスク種別とミニゲーム Prefab・表示名・アイコン・レベル別制限時間の対応を持つ唯一の登録簿。 | UnityEngine, `MiniGameBase` |
| `MiniGameBase` | `Assets/Scripts/Core/MiniGameBase.cs` | ミニゲーム共通のライフサイクル、制限時間、完了イベントを提供する抽象基底クラス。 | UnityEngine |
| `TaskBubbleView` | `Assets/Scripts/Core/TaskBubbleView.cs` | 1 件のタスクの種別・状態・残り寿命を Prefab のウィジェットへ書き込み、左右クリックを Controller へ渡す。座標もサイズも持たない。 | uGUI, TextMeshPro, EventSystem |
| `DeviceScreenController` | `Assets/Scripts/Core/DeviceScreenController.cs` | PC / Tablet ワークスペースの排他表示と切替可否を決める。 | `DeviceTabsView`, `TaskSurface` |

詳細は [MainGameController](main-game-controller.md) と [ミニゲームの追加・改造手順](mini-game-catalog.md)。

## Game シーンの View

| クラス | パス | 役割 | 主な依存先 |
| --- | --- | --- | --- |
| `HudView` / `HudSnapshot` | `Assets/Scripts/Core/UI/HudView.cs` | HP バー・残り時間・スコア・難易度の表示と Pause 要求。時刻書式もここが持つ。 | uGUI, TextMeshPro |
| `DeviceTabsView` | `Assets/Scripts/Core/UI/DeviceTabsView.cs` | PC / Tablet タブの外観と入力。どちらを表示するかは決めない。 | uGUI, `TaskSurface` |
| `DeviceWorkspaceView` | `Assets/Scripts/Core/UI/DeviceWorkspaceView.cs` | 1 デバイス面の表示状態（`CanvasGroup`）と、左右のタスク生成先の選択。 | uGUI, `TaskSurface` |
| `MiniGameHostView` | `Assets/Scripts/Core/UI/MiniGameHostView.cs` | 共通ミニゲーム領域の表示状態と、Prefab の生成・差し替え・破棄。大きさは決めない。 | UnityEngine |
| `PauseMenuView` | `Assets/Scripts/Core/UI/PauseMenuView.cs` | ポーズ／オプションのパネル表示とボタン入力。ゲーム進行は判断しない。 | uGUI |
| `SceneUiValidation` | `Assets/Scripts/Core/UI/SceneUiValidation.cs` | 必須参照の不足を、フィールド名を列挙して一度に報告する。 | UnityEngine |

## ミニゲーム

各ミニゲームは自分の Prefab に画面の実体を持ち、固有データも Prefab が `[SerializeField]` で持つ。Core からは `MiniGameCatalog` 経由でのみ参照される。

| クラス | パス | 役割 | 主な依存先 |
| --- | --- | --- | --- |
| `TypingMiniGame` | `Assets/Scripts/MiniGameS/Typing/TypingMiniGame.cs` | キーボード入力を受け、進捗を表示し、完了結果を 1 回通知する。 | `MiniGameBase`, Input System, TextMeshPro |
| `TypingQuestionDatabase` / `TypingQuestion` | `Assets/Scripts/MiniGameS/Typing/TypingQuestionDatabase.cs` | レベル別の日本語表示文字列と許容ローマ字を持つ。 | UnityEngine |
| `TypingInputEvaluator` | `Assets/Scripts/MiniGameS/Typing/TypingInputEvaluator.cs` | 複数の許容ローマ字に対する入力進捗を Unity 非依存で判定する。 | System |
| `TracingMiniGame` | `Assets/Scripts/MiniGameS/Tracing/TracingMiniGame.cs` | 経路のなぞりを判定する。ガイド線のみ Prefab 上の複製元から実行時に複製する。 | `MiniGameBase`, Input System, uGUI, TextMeshPro |
| `TracingPathDatabase` / `TracingPathEntry` | `Assets/Scripts/MiniGameS/Tracing/TracingPathDatabase.cs` | レベル別の正規化経路と許容逸脱量を持つ。 | UnityEngine |
| `TracingPathMath` | `Assets/Scripts/MiniGameS/Tracing/TracingPathMath.cs` | 点と折れ線の距離を物理演算なしで求める。 | UnityEngine |
| `RapidClickMiniGame` | `Assets/Scripts/MiniGameS/RapidClick/RapidClickMiniGame.cs` | 規定回数までの連打を判定する。必要数は Prefab の 2 値から決める。 | `MiniGameBase`, EventSystem, TextMeshPro |
| `SortingMiniGame` | `Assets/Scripts/MiniGameS/DragDrop/SortingMiniGame.cs` | カードの仕分けを判定する。箱とカードは Prefab 上の実体を配列で受け取る。 | `MiniGameBase`, TextMeshPro |
| `SortingDraggable` | `Assets/Scripts/MiniGameS/DragDrop/SortingDraggable.cs` | カード 1 枚のドラッグ。Canvas の拡大率で移動量を補正する。 | uGUI, EventSystem |
| `SortingDropBox` | `Assets/Scripts/MiniGameS/DragDrop/SortingDropBox.cs` | 受け皿 1 つ。落とされたカードの `categoryId` の一致を判定する。 | uGUI, EventSystem |

詳細は [タイピング](typing-mini-game.md) / [なぞり](tracing-mini-game.md) / [連打](rapid-click-mini-game.md) / [仕分け](drag-drop-mini-game.md)。

## 音声

| クラス | パス | 役割 | 主な依存先 |
| --- | --- | --- | --- |
| `AudioCatalog` / `AudioCue` | `Assets/Scripts/Core/AudioCatalog.cs` | 名前付きの BGM / SE を任意のクリップと音量へ対応づける。 | UnityEngine |
| `AudioManager` | `Assets/Scripts/Core/AudioManager.cs` | 常駐の BGM / SE ソース、シーン別 BGM 選択、安全な再生を持つ。 | Unity audio, SceneManager, `GameFlowController` |

詳細は [AudioManager](audio-manager.md)。

## 調整場所の索引

コードを変更せずに変えられるものと、その場所の一覧である。

### デバイス面

| 調整対象 | 場所 |
| --- | --- |
| 両面に共通する骨格・タスク領域の配置 | `Assets/Prefabs/UI/DeviceWorkspace.prefab` |
| PC 固有の背景色・端末枠・待機文言 | `Assets/Prefabs/UI/DeviceWorkspace_Pc.prefab`（Variant） |
| Tablet 固有の同上 | `Assets/Prefabs/UI/DeviceWorkspace_Tablet.prefab`（Variant） |
| どの面を使うか | `GameManager` の `workspaces` 配列 |

`DeviceWorkspaceView.Surface` は Variant ごとに設定する。同じ `Surface` を持つ面を 2 つ登録すると開始時にエラーで停止する。

### タスク吹き出し

| 調整対象 | 場所 |
| --- | --- |
| 大きさ、背景、フォント、文字配置、寿命バーの有無 | `Assets/Prefabs/UI/TaskBubble.prefab` |
| 状態別の配色（未着手 / 自力 / AI / 解決済み） | Prefab 上の `TaskBubbleView` の State colors |
| 状態別の表示文字列 | Prefab 上の `TaskBubbleView` の State labels |
| 種別名とアイコン | `Assets/Data/MiniGameCatalog.asset` の `displayName` / `icon` |
| 縦位置、間隔、余白、並ぶ向き | 各 `TaskSpawnArea` の Layout Group |

`TaskSpawnArea` には `VerticalLayoutGroup`（`childAlignment = MiddleCenter`）を付け、吹き出しの並びを出現エリアの中央へ固定する。`HorizontalLayoutGroup` へ差し替えれば横並びになり、コードは変更しない。Layout Group は子の `anchorMin` / `anchorMax` / `anchoredPosition` を driven property として支配するため、Prefab 側でアンカーを設定しても位置には影響しない。大きさは `childControlWidth` / `childControlHeight` を無効にしているため Prefab の `sizeDelta` が保たれる。

### ミニゲーム

| 調整対象 | 場所 |
| --- | --- |
| どのタスク種別にどのミニゲームを割り当てるか | `Assets/Data/MiniGameCatalog.asset` |
| レベル別の制限時間 | 同上（`timeLimitsByLevel`） |
| 各ミニゲームの画面と難度パラメータ | `Assets/Prefabs/MiniGames/` の各 Prefab |
| ミニゲームの表示範囲 | `Shared/MiniGameHost` の `RectTransform` |

手順は [ミニゲームの追加・改造手順](mini-game-catalog.md) を参照する。

### メニュー系シーン

| 調整対象 | 場所 |
| --- | --- |
| タイトル・難易度選択・結果画面の文字と配置 | 各シーンの `MainCanvas/ScreenRoot` 配下 |
| 難易度の増減 | シーンにボタンを置き、`DifficultySelectManager` の `choices` へ 1 行足す |
| 結果画面の未プレイ時の表示 | `ResultManager` の `emptyResultText` |

## 契約

- View は `Awake` ではなく `Initialize()` で自身のボタンを配線する。`PausePanel` のように非表示で開始する枝に置かれた場合、`Awake` は走らないためである。
- `MiniGameHostView.Spawn` は生成先の子を差し替えるだけで、生成物の大きさ・位置は決めない。ミニゲーム Prefab のルートでアンカーを Stretch にすること。ホスト直下に見出しや装飾を置く場合は `contentArea` の外に置くこと。
- ミニゲームは自分で `Destroy(gameObject)` しない。結果確定後に `MiniGameHostView.Hide()` が破棄する。
- 共有の調整値は `GameTuningSettings` を通して渡す。ただしミニゲームの制限時間は `MiniGameCatalog` が持ち、`GameTuningSettings` には置かない。

## MiniGameBase の契約

- `Initialize(difficulty, timeLimit)` が初期状態を設定し、`IsPlaying` を有効にする。
- `Update` が残り時間を減算し、派生クラスの `OnUpdate(deltaTime)` を実行する。
- 時間切れは `FinishGame(false, "TIME OUT")` で通知する。
- 派生クラスは成功・失敗を `FinishGame(success, reason)` で一度だけ通知する。
- `OnDestroy` でイベント購読をクリアする。
- 派生クラスは `OnUpdate` を実装し、完了処理を独自イベントで重複させない。`Initialize` をオーバーライドする場合は原則として `base.Initialize` を呼ぶ。

## GameTuningSettings が持つもの

`Assets/Data/GameTuningSettings.asset` に実体がある。

- プレイ時間、最大 HP
- プレイヤー・AI・時間切れのダメージ
- AI の成功率、処理時間、クールダウン、倍率
- タスクレベル別のスコアと時間ボーナス
- 難易度プロファイル（生成間隔、タスク寿命、面あたり最大数、タスクレベルの上下限と上昇間隔）

**注意:** 現在 `difficultyProfiles` は空である。そのため全難易度がフォールバック値で動き、タスクレベルは 1 に固定される。難易度差を付けるにはこのリストへ行を足す。

## 個人試作（`Assets/Personal/`）

本編とは独立した試作領域である。本編のスクリプトやシーンからは参照しない。

| 領域 | 内容 |
| --- | --- |
| `Assets/Personal/Suzuki/TypingMiniGame/` | ローマ字入力判定の試作。本編の `Overwork.MiniGames.Typing` はここから採用した。 |
| `Assets/Personal/Suzuki/TracingMiniGame/` | なぞり判定の試作。本編の `Overwork.MiniGames.Tracing` はここから採用した。 |
| `Assets/Personal/{Asano,Hiroto,Honda,Motonaga,Syuto}/` | 各担当者の試作領域。 |

採用の判断基準は [個人試作の採用方針](../Decisions/2026-08-04-personal-prototype-adoption.md) を参照する。

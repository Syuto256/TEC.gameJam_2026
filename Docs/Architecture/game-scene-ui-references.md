# GameSceneUiReferences（ゲーム画面構成の設計）

最終更新: 2026-08-04  
状態: View 分割・Scene 配線ともに実装済み。画面構成の合意内容は [Game 画面レイアウト案](../GameDesign/game-screen-layout.md) を正とする。

## 責務

`Game.unity` に常設する View とゲーム進行ロジックを接続する。個々のウィジェット（テキスト、バー、ボタン）は各 View が Inspector で保持し、このクラスは **View と Controller をつなぐ以外の責務を持たない**。

## View 分割の方針

HUD の HP バー・残り時間・スコアのように、レイアウト案が要求する表示要素はウィジェット数が増え続ける。それらの参照をこのクラスに集めると、要素を一つ足すたびに「参照クラス・`Initialize` のシグネチャ・Scene」の三箇所を同時に触ることになる。そのため、表示のまとまりごとに View を置き、このクラスは View への参照だけを持つ。

| View | 置き場所 | 保持する参照 | 通知するイベント |
| --- | --- | --- | --- |
| `HudView` | `Shared/Hud` | HP バー（Filled Image）、残り時間、スコア、難易度（任意）、HP 数値（任意）、Pause ボタン | `PauseRequested` |
| `DeviceTabsView` | `Shared/DeviceTabs` | PC / Tablet タブ、選択強調表示（任意） | `SurfaceRequested(TaskSurface)` |
| `MiniGameHostView` | `Shared/MiniGameHost` | ミニゲーム Prefab の生成先 `contentArea`、表示を切り替える `root`（任意） | なし |
| `PauseMenuView` | `Shared/ModalLayer` | ポーズパネル、再開、難易度選択へ戻る、オプション（任意） | `ResumeRequested` / `BackToDifficultyRequested` |
| `DeviceWorkspaceView` | 各デバイス面のルート | `Surface`、左右の `TaskSpawnArea`、`CanvasGroup` | なし |

各 View は `Initialize()` を持ち、必須参照の検証と自身のボタン配線をここで行う。

- `Awake` を使わないのは、`PausePanel` のように**非表示状態で開始する枝に置かれた View では `Awake` が走らない**ためである。配線の実行順を `GameSceneUiReferences` が決める形にして、この事故を構造的に防ぐ。
- 検証は `SceneUiValidation.Require` に集約し、不足しているフィールド名を列挙して報告する。
- `GameSceneUiReferences` は 4 つの `Initialize()` を短絡しない `&` で評価し、不足を一度にすべて報告する。

## Inspector で保持する参照

| 区分 | 参照 |
| --- | --- |
| View | `HudView`、`DeviceTabsView`、`MiniGameHostView`、`PauseMenuView` |
| ワークスペース | `DeviceWorkspaceView[] workspaces`（配列 1 つ） |
| 制御 | `MainGameController`、`DeviceScreenController` |

デバイス面は個別フィールドではなく配列で持つ。3 つ目の面を足す場合は Variant を 1 つ作って配列へ追加するだけで、`GameSceneUiReferences` も `DeviceScreenController` も `MainGameController` も変更しない。配列の `Surface` 重複と未設定要素は開始時に検出して停止する。

左右のタスク生成領域は `DeviceWorkspaceView.PickSpawnArea()` が持ち、吹き出しの少ない側を返す。`MainGameController` は `Surface` から面を引いてこれを呼ぶだけで、左右を判断しない。

## 配置上の注意

- `HudView` は `Shared/Hud`、`DeviceTabsView` は `Shared/DeviceTabs`、`MiniGameHostView` は `Shared/MiniGameHost`、`PauseMenuView` は `Shared/ModalLayer` に置く。`PauseMenuView` を `PausePanel` 自身へ置かないこと。
- `MainCanvas` の子順は `PcOnly` → `TabletOnly` → `Shared` とする。`Shared` を先頭に置くと HUD と `MiniGameHost` がデバイス画面の奥に描画される。
- HP バーの `Image` は Sprite を割り当てたうえで Image Type を `Filled` にする。Sprite 未設定の `Image` は `fillAmount` を無視して常に全面を描画する。

## 検証

- 必須参照が未設定なら、開始時にクラス名・GameObject 名・フィールド名を出して停止する。
- レイアウトを変更しても、参照先を Inspector で更新するだけでゲーム進行ロジックを変更しない。
- 実行時に生成するのはタスク吹き出しとミニゲーム Prefab のみである。
- HUD の表示要素を増減しても、変更は `HudView` と Scene だけで閉じる。

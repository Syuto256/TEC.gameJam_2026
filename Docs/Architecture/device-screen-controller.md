# DeviceScreenController（ゲーム画面構成の設計）

最終更新: 2026-08-04  
状態: 実装済み。Scene 配線は未了。画面構成の合意内容は [Game 画面レイアウト案](../GameDesign/game-screen-layout.md) を正とする。

## 責務

PC / Tablet のうち、どちらのワークスペースを主作業画面として表示するかを担当する。タブ以外の入力方法へ差し替えても、ワークスペースとタスク表示の責務を変えない境界とする。

タブの外観（選択強調、`interactable` の切替）は `DeviceTabsView` が持つ。このコンポーネントは **どちらを表示するか**だけを決め、**どう見せるか**は持たない。

## 公開契約

| API | 条件 | 保証 |
| --- | --- | --- |
| `Initialize(GameObject, GameObject, DeviceTabsView)` | 開始時に一度 | `DeviceTabsView.SurfaceRequested` を購読し、PC を初期表示にする。 |
| `Show(TaskSurface)` | 切替入力が有効 | 指定した一方だけを表示し、View へ選択状態を伝える。 |
| `SetSwitchEnabled(bool)` | ミニゲーム開始・終了時 | View へ入力可否を伝える。 |

## 検証

- PC / Tablet の一方だけが表示される。
- 非表示側のタスクモデルは停止しない（寿命と AI 処理は `MainGameController.Update` が回す `TaskManager` 側にあるため）。
- ミニゲーム中は切替入力を受け付けない（暫定）。

> TODO（2026-08-04）: `SetSwitchEnabled` の呼び出し元が未接続。ミニゲームの開始・終了に合わせて切り替える接続を R4 で入れる。

> TODO（2026-08-04）: 現在の表示切替は `SetActive`。企画側 TODO の画面切替演出（椅子を回す等）を入れる場合は、非表示側を `CanvasGroup` で残す形へ変更する必要がある。

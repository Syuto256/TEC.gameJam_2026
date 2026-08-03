# DeviceScreenController（ゲーム画面構成の設計）

最終更新: 2026-08-04  
状態: 実装済み・Scene 配線済み。画面構成の合意内容は [Game 画面レイアウト案](../GameDesign/game-screen-layout.md) を正とする。

## 責務

どのデバイス面を主作業画面として表示するかと、切替可否だけを担当する。タブ以外の入力方法へ差し替えても、ワークスペースとタスク表示の責務を変えない境界とする。

デバイス面は `DeviceWorkspaceView` の配列として受け取る。PC / Tablet を個別のフィールドで持たないため、3 つ目のデバイス面が増えてもこのクラスは変わらない。

| 持たないもの | 持つ側 |
| --- | --- |
| タブの外観（選択強調、`interactable`） | `DeviceTabsView` |
| デバイス面の隠し方、タスク生成先 | `DeviceWorkspaceView` |
| ミニゲームの進行状態 | `MainGameController` |

## 公開契約

| API | 条件 | 保証 |
| --- | --- | --- |
| `Initialize(DeviceWorkspaceView[], DeviceTabsView)` | 開始時に一度 | `DeviceTabsView.SurfaceRequested` を購読し、PC を初期表示にする。 |
| `Show(TaskSurface)` | 切替入力が有効 | 一致する `Surface` の面だけを表示し、View へ選択状態を伝える。 |
| `SetSwitchEnabled(bool)` | ミニゲーム開始・終了時 | View へ入力可否を伝える。無効中は `Show` を受け付けない。 |

ミニゲーム中の切替禁止は、`MainGameController.PlayerMiniGameActiveChanged` を `GameManager` が `SetSwitchEnabled` へ繋ぐことで成立する。二つの Controller は互いを参照しない。

## 検証

- 一つのデバイス面だけが見える。
- 非表示側は `SetActive(false)` にならず、`CanvasGroup` の `alpha` が 0 になる。
- 非表示側のタスクモデルは停止しない（寿命と AI 処理は `MainGameController.Update` が回す `TaskManager` 側にあるため）。
- ミニゲーム中はタブが両方とも非活性になり、終了で復帰する（暫定仕様）。

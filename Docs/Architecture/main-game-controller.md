# クラス詳細: MainGameController と TaskBubbleView

最終更新: 2026-08-04  
実装: `Assets/Scripts/Core/MainGameController.cs`, `Assets/Scripts/Core/TaskBubbleView.cs`  
状態: 実装済み

## 責務

`MainGameController` は Game シーンのゲーム進行を持つ。`GameTuningSettings` と選択難易度から `TaskManager` / `GameSession` を作り、一定間隔のタスク生成、AI 処理、期限切れ、HP / スコア / HUD 更新、Clear / GameOver への遷移を接続する。

Scene 上の View との配線は `GameManager` が行う。このクラスは View の参照を `Initialize` で受け取るだけで、自分で探さない。

`TaskBubbleView` は 1 件の `TaskInstance` を表示する UI である。Collider は使わず、EventSystem の `IPointerClickHandler` で操作する。座標もサイズも持たず、書き込み先は `Assets/Prefabs/UI/TaskBubble.prefab` にある。

## Inspector で持つもの

| 項目 | 用途 |
| --- | --- |
| `taskBubblePrefab` | タスク吹き出しの見た目 |
| `miniGameCatalog` | ミニゲームの登録簿。開始時に一度だけ内容を検証する |
| `taskSpawnTable` | デバイス面ごとの出現タスク。同じく開始時に検証する |

## 1 フレームの順序

```text
1. taskManager.Tick   寿命を減らす / AI 処理を進める
2. session.Tick       残り時間を減らす
   └ 終了なら FinishSession して以降を中断
3. 生成間隔を超えていれば TrySpawnTask
4. RefreshTaskViews   全吹き出しの表示更新
5. RefreshHud         HudSnapshot を作って HudView へ
```

`taskManager.Tick` を `session.Tick` より先に呼ぶため、HP 0 による GameOver が時間切れ Clear より優先される。

## 操作と状態

| 入力 | 処理 | 状態 |
| --- | --- | --- |
| 左クリック | `TryAssignPlayer` | `PlayerPlaying`。寿命を停止し、カタログから引いた Prefab を `MiniGameHost` に生成する。 |
| 右クリック | `TryAssignAi` | `AiProcessing`。設定時間後、成功率に従って一度だけ解決する。 |
| 未操作 | `TaskManager.Tick` | 寿命が尽きると `Expired`。HP を減らす。 |

初期値 `ai.cooldownSec = 0` では、複数タスクへ連続して AI を依頼できる。正の値に変更すれば全体 AI 依頼クールタイムが有効になる。

## ミニゲームの起動と後片付け

```text
TryAssignPlayer
  └ miniGameCatalog.TryGetEntry(task.Kind)
      └ miniGameHost.Spawn(entry.prefab)
          └ OnCompleted を購読
          └ Initialize(task.Level, entry.GetTimeLimit(task.Level))

OnTaskResolved（成功・失敗・AI・寿命切れのすべてを通る）
  └ miniGameHost.Hide()   ← ここで生成物が破棄される
```

生成物の破棄は必ず `Hide()` が行う。ミニゲーム側は自分を破棄しない。この経路は、ミニゲーム実行中にタスクが寿命切れした場合も同じである。

自力ミニゲームの開始・終了は `PlayerMiniGameActiveChanged` で通知する。`GameManager` がこれを `DeviceScreenController.SetSwitchEnabled` へ繋いで、ミニゲーム中のデバイス切替を止めている。2 つの Controller は互いを参照しない。

## タスクを出す面と種別の決め方

1. `TaskSpawnTable` に出現タスクが設定されていて、上限（`maxTasksPerSurface`）に達していない面を候補にする。
2. 候補のうち、未解決タスクがいちばん少ない面を選ぶ。
3. その面に設定された種別を、面ごとに独立した順番で 1 つ選ぶ。

面の一覧は `workspaces` から取るため、**このクラスは PC / タブレットという具体名を持たない。** デバイス面を 3 つ目に増やしても、Variant を 1 つ作って `GameManager` の配列と出現表へ足すだけで動く。

以前は「面を件数の釣り合いで選ぶ」「種別を全体で 1 本の順番で選ぶ」を独立に行っていたため、タブレット専用にしたい種別が PC にも出ていた。出現表の導入でこの組み合わせが固定された。

## タスクレベル

`CalculateTaskLevel()` は難易度プロファイルの `startingTaskLevel` から始まり、`taskLevelIncreaseIntervalSec` ごとに `maxTaskLevel` まで上がる。

**現状の注意:** `GameTuningSettings.difficultyProfiles` が空のため、フォールバック値（`startingTaskLevel = 1` / `maxTaskLevel = 1`）が使われ、レベルは常に 1 になる。難易度差を付けるにはプロファイルを追加する。

## 検証

- 生成間隔ごとに PC / Pad の `TaskSpawnArea` に吹き出しが生成される。
- AI 依頼後、解決時に対象の吹き出しが除去される。
- 自力担当で `MiniGameHost` が表示され、タスクの寿命が停止する。
- ミニゲーム終了後に Host が閉じ、生成物が残らない。
- タスク期限切れによる HP 減少が GameOver 遷移へ反映される。

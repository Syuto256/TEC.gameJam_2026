# Game 画面レイアウト案（UI 校正用・暫定）

最終更新: 2026-08-04  
状態: 暫定。UI 校正・デザイナー素材の受け入れ・実装のための基準であり、確定仕様ではない。

関連資料: [メインゲーム画面・遷移仕様](../Specifications/main-game-flow.md) / [コアゲーム仕様](../Specifications/gameplay-core.md)

## 画面の目的

プレイヤーは PC とタブレットに発生するタスクを確認して処理する。ゲーム画面は、HP・残り時間・スコアを常に把握でき、現在どちらのデバイスを操作しているかを明瞭にする必要がある。

## 最重要の前提

PC とタブレットは左右に同時表示する二分割画面ではない。常にどちらか一方だけを主作業画面として表示する、二つの独立したデバイス画面である。

- PC ワークスペースでは PC タスクだけを確認・選択・実行する。
- タブレットワークスペースではタブレットタスクだけを確認・選択・実行する。
- 非選択側のタスク吹き出し・タスク詳細は表示しない。
- 現在のデバイスは画面下部の PC / Tablet タブで即時に切り替える暫定案とする。切替入力方法は後から差し替え可能にする。

## UI の所属と固定 Hierarchy

画面上の UI は、表示条件で `Shared`、`PcOnly`、`TabletOnly` の三つにだけ分ける。装飾やレイアウトの調整はこの Hierarchy で行い、ゲーム進行コードは子の見た目や座標を持たない。

```text
MainCanvas
├─ PcOnly              ← DeviceWorkspace_Pc.prefab のインスタンス
│  ├─ Background
│  ├─ DeviceFrame
│  │  └─ WaitingLabel
│  └─ TaskAreas
│     ├─ LeftTaskArea
│     │  └─ TaskSpawnArea
│     └─ RightTaskArea
│        └─ TaskSpawnArea
├─ TabletOnly          ← DeviceWorkspace_Tablet.prefab のインスタンス
│  └─ （PcOnly と同一構造）
└─ Shared
   ├─ Hud
   ├─ DeviceTabs
   ├─ MiniGameHost
   │  └─ Content
   └─ ModalLayer
      ├─ PausePanel
      └─ OptionPanel
```

デバイス面の子は両面で同じ名前とする。`Background` / `DeviceFrame` / `TaskAreas` に `Pc` / `Tablet` の接頭辞を付けない。共通の骨格を `DeviceWorkspace.prefab` に置き、デバイス固有の背景色・端末枠・待機文言だけを Prefab Variant で上書きするためである。3 つ目のデバイス面が必要になった場合も、Variant を 1 つ作って `GameManager` の `workspaces` へ追加するだけで足りる。

`MainCanvas` と同じ階層に `GameManager` GameObject があり、`GameManager` / `MainGameController` / `DeviceScreenController` の 3 つを持つ。ほかに `Main Camera` と `EventSystem` が常設されている。

| 所属 | 含めるもの | 表示条件 |
| --- | --- | --- |
| `Shared` | HUD、PC / Tablet 切替、共通 `MiniGameHost`、ポーズ・オプション | 常時。ただし `MiniGameHost` と各モーダルは必要時だけ有効化する。 |
| `PcOnly` | PC 固有の背景、端末枠、待機表示、PC タスク領域 | PC が選択中のときだけ表示する。 |
| `TabletOnly` | Tablet 固有の背景、端末枠、待機表示、Tablet タスク領域 | Tablet が選択中のときだけ表示する。 |

`Shared` の表示順はデバイス画面より前とする。uGUI の描画順は Hierarchy の並び順なので、`Shared` は `MainCanvas` の**最後の子**に置く。`MiniGameHost` はデバイスの子にせず、共通 UI の最前面に置く。

非選択側のデバイス面は `SetActive(false)` にせず、`CanvasGroup` の `alpha` / `interactable` / `blocksRaycasts` で隠す。枝を無効化すると吹き出しの演出や Coroutine が止まり、切替演出も作れなくなるためである。非表示側のタスク寿命は `TaskManager` 側で進むため、どちらの方式でも仕様は満たす。

## 調整の所有者

位置・サイズ・アンカー・Pivot・余白・色・素材・文字サイズは、原則として `Game.unity` の Hierarchy と Inspector で調整する。画面制御コードはこれらの値を実行時に設定・上書きしない。

| 調整対象 | 調整場所 | コードの責務 |
| --- | --- | --- |
| HUD、タブ、モーダル、端末枠、背景 | 各 GameObject の `RectTransform` と UI コンポーネント | 参照取得と表示状態の切替のみ |
| デバイス面の共通骨格 | `Assets/Prefabs/UI/DeviceWorkspace.prefab` | 触れない |
| デバイス固有の背景・端末枠・待機文言 | `DeviceWorkspace_Pc` / `DeviceWorkspace_Tablet` の Variant | 触れない |
| PC / Tablet の表示範囲 | `PcOnly` / `TabletOnly` の `RectTransform` | 選択中の一方だけを表示 |
| タスクが出現できる範囲 | 各 `TaskSpawnArea` の `RectTransform` | その領域の子としてタスクを生成 |
| タスク吹き出しの見た目 | `Assets/Prefabs/UI/TaskBubble.prefab` | 種別・状態・残り時間を書き込むだけ |
| タスク吹き出しの並び方 | 各 `TaskSpawnArea` の Layout Group | 並びに関与しない |
| タスク種別の表示名・アイコン | `Assets/Data/MiniGameCatalog.asset` | 登録内容を吹き出しへ渡すだけ |
| ミニゲームの表示範囲 | `Shared/MiniGameHost` の `RectTransform` | Host の表示・非表示と Prefab の生成・破棄のみ |
| ミニゲーム 1 本ごとの見た目 | `Assets/Prefabs/MiniGames/` の各 Prefab | 生成して `Initialize` を呼ぶだけ |

タスク吹き出しの縦位置は、`TaskSpawnArea` に付けた `VerticalLayoutGroup`（`childAlignment = MiddleCenter`）で出現エリアの中央に固定する。件数が増えても中央を基準に上下へ広がる。並ぶ向きを変える場合は Layout Group を差し替える。

固定ピクセル座標や Scene 固有のレイアウト値をゲーム進行コードへ追加しない。

## レイアウト構成

### 1. 共通 HUD（最上部）

すべてのゲーム中状態で表示を維持する。

- HP: 数値だけに頼らない横バー。
- 残り時間: もっとも目立つ位置・大きさで表示。
- SCORE: 補助情報として表示。
- 難易度: 必要なら SCORE 付近に小さく表示。
- Pause: 一時停止ボタン。Esc 入力と同じ動作にする。

### 2. デバイス作業ワークスペース（HUD 下の大部分）

選択中の PC またはタブレットのワークスペースを大きく表示する。PC とタブレットはそれぞれ一つのワークスペースとして作り、タブで表示を切り替える。

- 中央: PC またはタブレット本体の画面。タスクを選択するまでは待機画面を表示する。
- 左右: 選択中デバイスに属するタスクの吹き出しを表示する余白。吹き出しには、種別・残り時間・難易度・処理状態を簡潔に表示する。
- 吹き出しの左クリックで共通 `MiniGameHost` に対応ミニゲームを表示する。現行の暫定操作では右クリックが AI 依頼であり、専用 AI ボタン化は UI 校正時の検討項目とする。
- タスクがないときは、左右の余白に空であることを穏やかに示す表示を残す。
- デバイス固有の外枠・背景・装飾は、後から個別に差し替えられるようにする。

### 3. デバイス切替 UI（画面下部）

- `PC` と `TABLET` の二つのタブを常設する。
- 選択中のタブを明るく強調する。
- 非選択側は未処理件数や警告状態を小さなバッジで示せるようにする。
- タブの外観・配置は後で変更可能とし、表示切替ロジックとは分離する。

## タスクとミニゲームの遷移

1. 現在のデバイスワークスペースの左右にある吹き出しからタスクを選択する。
2. 共通の `MiniGameHost` に、対応するミニゲームを表示する。
3. 成功・失敗・時間切れの後、共通の `MiniGameHost` を閉じ、中央デバイス画面は待機表示を維持する。
4. ミニゲーム中も HUD は表示する。
5. ミニゲーム中はデバイス切替 UI を非操作にする暫定案とする。

最後の項目は未確定である。将来、切替を許可して進行中のミニゲームを維持する案が必要になった場合は再検討する。

## 一時停止

画面全体を覆うモーダルとし、以下を表示する。

- 再開
- 難易度選択へ戻る
- オプション

オプションは音量設定の受け皿を用意し、BGM / SE の音量操作は音源・最終 UI の準備後に調整する。

## 素材差し替えと今後の演出

背景、PC / タブレット外枠、発光表現、左右のタスク吹き出し領域、切替タブ、HUD を別々の UI / Prefab 単位として扱い、デザイナー素材をロジック変更なしで差し替えられる形を目指す。

タスク吹き出しは `Assets/Prefabs/UI/TaskBubble.prefab` として先行して Prefab 化済みである。状態別の配色と表示文字列は Prefab 上の `TaskBubbleView` の Inspector に出しており、素材差し替えとあわせてコード変更なしで調整できる。

## 実装時の構造

`Game.unity` に Canvas・共通 UI・PCワークスペース・Tabletワークスペース・オーバーレイを常設する。共通 UI は HUD、デバイス切替タブ、`MiniGameHost`、ポーズなどを保持し、PC / Tablet 固有の装飾・待機画面・タスク領域から分離する。画面配置と装飾は Hierarchy から直接編集し、`GameManager` はそれらをゲームロジックへ参照渡しするだけにする。実行時に生成するのは、左右のタスク吹き出しと、共通 `MiniGameHost` に出すミニゲーム Prefab と、なぞりミニゲームのガイド線のみである。

画面構成と素材受け入れが安定した後に、暗い部屋と選択デバイス画面の発光を表現する疑似 2D ライティングを技術検証する。詳細は [延期した技術検証の記録](../Decisions/2026-08-04-deferred-ui-lighting-prototype.md) を参照。

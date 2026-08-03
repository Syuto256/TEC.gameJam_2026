# メインゲーム画面・接続仕様

> **ステータス: 実装前の合意済み仕様（2026-08-03）**

企画: [ゲーム企画概要](../GameDesign/game-overview.md)
共通ルール: [コアゲームプレイ仕様](gameplay-core.md)

## 1. 目的

個人試作を直接接続せず、共有の Main Game シーンでミニゲームを一貫して起動・終了できる構成を定義する。画面表示とゲームプレイは Canvas UI に統一する。

## 2. シーンと UI 構成

| シーン | 責務 |
| --- | --- |
| `Title` | タイトル、開始、オプション、ゲーム終了。 |
| `DifficultySelect` | 難易度選択、スコア履歴の表示領域、タイトルへ戻る。 |
| `Game` | HUD、PC / パッド、タスク、ミニゲーム、ポーズを保持する。 |
| `Clear` | CLEAR、スコア、戻る。タイマー終了後の一息つく演出の受け皿。 |
| `GameOver` | GAME OVER、スコア、戻る、リトライ。 |

`Game` はルート Canvas の下に `HudPanel`、`PcTaskPanel`、`PadTaskPanel`、`MiniGameHost`、`PausePanel`、`OptionPanel` を持つ。PC / パッドの一方だけを表示・入力可能にするが、両パネルのタスクモデルは常に更新する。

## 3. 実装責務

| コンポーネント | 責務 | 個別ミニゲームへの依存 |
| --- | --- | --- |
| `GameFlowController` | シーン遷移、選択難易度、結果の受け渡し。 | 持たない |
| `MainGameController` | セッション、HUD、ポーズ、終了判定。 | `TaskManager` の結果のみ |
| `TaskManager` | 生成、寿命、AI、解決、タスク面への割当。 | `MiniGameCatalog` のみ |
| `MiniGameCatalog` | 種別と Prefab・アイコン・制限時間の対応を持つ SO。 | Prefab 参照のみ |
| `MiniGameHost` | Prefab の生成、`MiniGameBase` の初期化と結果購読、破棄。 | `MiniGameBase` |
| `InputRouter` | 左右クリック、Esc、UI 入力を Input System に統一。 | 持たない |
| `AudioManager` | BGM / SFX の再生と音量設定。 | 結果イベントを購読 |

`GameManager` は現状のテスト用の責務をそのまま拡張しない。上記の責務へ段階的に置き換え、個別種別の条件分岐を持たせない。

## 4. ミニゲームの接続契約

1. `TaskManager` が `MiniGameCatalog` から対象 Prefab と制限時間を取得する。
2. `MiniGameHost` が `MiniGameBase` を生成し、問題レベルと制限時間を渡す。
3. `MiniGameBase.OnCompleted(success, reason)` を受けた Host は、開始したタスク ID とともに `TaskManager` へ返す。
4. `TaskManager` は自力成功または自力失敗として 1 回だけ解決する。
5. Host は Prefab を破棄して入力をタスク面へ戻す。

AI は `MiniGameHost` を経由しない。AI 処理へ移ったタスクがすでに Host で実行中の場合は、その Host を中断・破棄してから AI 判定へ移る。

## 5. UI 実装規約

- ルート Canvas は `Screen Space - Overlay`、基準解像度は 1920×1080 とする。
- `Canvas Scaler` は `Scale With Screen Size` を用いる。
- クリック・ドラッグ・なぞりは `IPointerClickHandler`、`IBeginDragHandler`、`IDragHandler`、`IEndDragHandler` 等で実装し、Collider と Physics を使わない。
- UI の大半は単一 Canvas の子とし、モーダル・頻繁に更新する領域だけを必要に応じて別 Canvas にする。
- 非表示パネルは `CanvasGroup` により alpha、interactable、blocksRaycasts を一貫して制御する。

### 配置の段階化

P0 では、画面領域、アンカー、入力可能範囲、描画順、タスクの生成領域だけを「機能配置」として確定する。`PcTaskPanel` と `PadTaskPanel` はそれぞれ `TaskSpawnArea` を持ち、タスク View の位置はこの領域を基準に決める。タスクの実行時データはシーン上の座標を保持しない。

余白、ピクセル単位の位置、背景との重なり、色、装飾、画面遷移演出の位置調整は P0 完了後の視覚調整で扱う。後から Canvas のレイアウトを変えても、ゲーム進行・タスク寿命・クリック判定が変わらないことを保証する。

最低限、1920×1080 と低解像度の 1 種で、HUD が読めること、PC / パッドの切替ボタンとタスクが操作できることを P0 中に確認する。

## 6. Personal 原本の扱い

- `Assets/Personal/` 配下のシーン・Prefab・素材・データは原本として保全し、編集・移動・削除しない。
- Unity アセットを共有領域で使う場合は、Unity Pipeline の `copy_asset` で `Assets/Prefabs/MiniGames/` 等へ複製してから編集する。
- C# を同名・同一クラスとして複製するとコンパイル競合するため、`Assets/Scripts/MiniGames/` に新しい本番用クラスを作り、原本を参考にロジックを移植する。
- 本番用データは `Assets/Data/MiniGames/` に新規作成する。個人用 ScriptableObject を共有データへ直接移動しない。

## 7. 接続確認

1. Title から難易度を選び Game を開始できる。
2. PC / パッドを切り替え、非表示側のタスク寿命が継続する。
3. 各接続済みミニゲームが 1 回だけ自力結果を返し、Host が片付く。
4. AI を複数タスクへ依頼でき、クールダウンと結果が正しい。
5. Clear / GameOver から難易度選択へ戻り、前プレイのタスクが残らない。

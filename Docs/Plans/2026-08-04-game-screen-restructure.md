# Game 画面の静的 UI 化・再構成計画

最終更新: 2026-08-04  
状態: 要件整理中

関連資料: [Game 画面レイアウト案](../GameDesign/game-screen-layout.md) / [共通 MiniGameHost の決定](../Decisions/2026-08-04-shared-mini-game-host.md) / [メインゲーム画面・接続仕様](../Specifications/main-game-flow.md)

## 目的

実行時に `SceneUiBootstrap` が生成している Game UI を、`Game.unity` 上の静的な UI へ移す。画面要素は `Shared`、`PcOnly`、`TabletOnly` の三つに分け、デザイナーが Hierarchy と Inspector だけでレイアウト・装飾・素材を調整できる状態にする。

ゲーム進行、タスク寿命、クリック意味、ミニゲームの接続契約は変えない。

## 非対象

- 画面レイアウト案にないゲームルールの追加・変更
- タスク・ミニゲームのデータ形式変更
- Personal 配下のアセットの編集、移動、削除
- 個別ミニゲーム Prefab の見た目調整

## 段階

### R1: 要件・Hierarchy の確定

- `Shared` / `PcOnly` / `TabletOnly` の所属を資料で確定する。
- 固定 Hierarchy と命名を確定する。
- ミニゲームは共通 `MiniGameHost` に一件だけ表示する。
- ミニゲーム中のタスク操作可否など、未決定の入力仕様は TODO として残す。

**確認ゲート:** [画面レイアウト案](../GameDesign/game-screen-layout.md) の所属表と Hierarchy が合意内容に一致する。

### R2: Scene の静的骨格

- Unity Pipeline を使い、`MainCanvas`、`Shared`、`PcOnly`、`TabletOnly` と名前付き空コンテナを作る。
- Canvas を `Screen Space - Overlay`、Canvas Scaler を基準解像度 1920×1080 の `Scale With Screen Size` に設定する。
- この段階では色・画像・文字・ボタンを置かず、既存の動的 UI を置き換えない。

**確認ゲート:** Hierarchy が資料と一致し、1920×1080 の Game View で空の UI 骨格が確認できる。既存の Title / DifficultySelect / Clear / GameOver は変えない。

### R3: 最小の静的 UI と参照境界

- `Shared` に HUD、デバイス切替、`MiniGameHost`、`ModalLayer` の空コンテナを置く。（完了）
- PC / Tablet の各タスク領域と二つの `TaskSpawnArea` を置く。（完了）
- 表示のまとまりごとに View を置き、`GameSceneUiReferences` は View と Controller の接続だけに限定する。（完了）
- 必須参照が不足した場合は、開始時に不足を明示する。（完了）
- `MainCanvas` の子順を `PcOnly` → `TabletOnly` → `Shared` に修正した。`Shared` が先頭にあり、HUD と `MiniGameHost` がデバイス画面の奥に描画されていたためである。

View の分割方針と各 View の保持する参照は [GameSceneUiReferences の設計](../Architecture/game-scene-ui-references.md) を参照する。

**確認ゲート:** すべての Scene 参照が Inspector で追跡でき、レイアウト値が C# に存在しない。

### R4: 表示制御とゲーム進行の接続

- `DeviceScreenController` は PC / Tablet の排他表示と切替入力可否だけを担当する。（完了）
- `SceneUiBootstrap` の Game 用 UI 生成を、静的 UI の初期化へ置き換える。（完了）
- `MainGameController` を既存の HUD、タスク生成領域、共通 `MiniGameHost`、ポーズへ接続する。（完了）
- タブの配色・バッジ・装飾は Scene / Prefab 側で調整可能にする。（完了。バッジは未配置）
- ミニゲーム中の切替禁止を接続する。（完了。`MainGameController.PlayerMiniGameActiveChanged` → `SetSwitchEnabled`）

**確認ゲート:** PC / Tablet の切替、HUD 更新、ポーズ、共通 Host の表示が動き、非表示側のタスク寿命が進む。

**確認結果（2026-08-04・Play モードで検証）:** Console 0 件。タブ切替で `PcOnly` / `TabletOnly` が排他表示になり、選択中タブが非活性になる。HUD は HP バー（`fillAmount`）・残り時間・スコア・難易度を更新する。ポーズで `timeScale` が 0 になり、再開で戻る。非表示だった Tablet 側にタスク吹き出しが 2 件生成され、モデル上の未解決タスク数と一致した。タスク吹き出しの左クリックで `MiniGameHost` が表示され、`Content` 配下にミニゲームが 1 件生成された。

### R5: タスク表示と操作の移行

- タスク吹き出しの親を、選択中デバイス側の左右 `TaskSpawnArea` に振り分ける。（完了。`DeviceWorkspaceView.PickSpawnArea()` が吹き出しの少ない側を返す）
- タスク吹き出しとミニゲーム Prefab 以外の UI を実行時に生成しない。（完了）
- 左クリックの自力開始、右クリックの AI 依頼、ミニゲーム終了時の Host 後片付けを接続する。（完了）

吹き出しの生成は `TaskBubbleView.Create` による動的組み立てをやめ、`Assets/Prefabs/UI/TaskBubble.prefab` の生成に置き換えた。位置は各 `TaskSpawnArea` の `VerticalLayoutGroup` が決めるため、左右振り分けは生成先の `TaskSpawnArea` を選ぶだけで済む。

**確認ゲート:** PC / Tablet の双方で、自力成功・AI 成功/失敗・未着手時間切れが一度だけ反映される。

### R6: 調整可能化と検証

- 背景、端末枠、タスク領域、HUD、タブ、モーダルを個別に Prefab 化する範囲を決める。（タスク吹き出しとデバイス面を Prefab 化済み。HUD・タブ・モーダルは Scene 直置きのまま）
- 素材差し替え手順と、Scene 上で調整する項目を資料化する。（[クラスカタログ](../Architecture/class-catalog.md) の「デバイス面の調整場所」「タスク吹き出しの調整場所」に記載済み）

デバイス面は共通骨格の `DeviceWorkspace.prefab` と、デバイス固有値だけを上書きする Variant 2 つに分けた。子の名前は両面で共通化し、`Pc` / `Tablet` の接頭辞を外している（[画面レイアウト案](../GameDesign/game-screen-layout.md) の Hierarchy を更新済み）。
- 1920×1080 と低解像度一種、PC / Tablet 切替、ミニゲーム、ポーズ、Clear / GameOver を確認する。

**確認ゲート:** Console エラー 0 件。配置・素材をコード変更なしで差し替えられる。

## 実装上の制約

- シーンと Unity アセットの作成・変更は Unity Pipeline 経由で行う。
- ~~R4 完了まで、`SceneUiBootstrap` の既存 Game 用動的生成は削除しない。~~ 解除（2026-08-04）。`SceneUiBootstrap.Start` は Game シーンを `GameSceneUiReferences` へ委譲済みで、`BuildGame` は既に到達不能な死にコードだった。View 分割に伴い削除した。Title / DifficultySelect / Clear / GameOver の動的生成は従来どおり残る。
- 現在未追跡のフォント・スプライトは、本計画の対象外として触れない。
- `RectTransform`、UI の色・素材・文字サイズ、各 UI の表示順をゲーム進行コードから変更しない。これらは `Game.unity` または採用済み UI Prefab の Inspector で調整する。
- 実行時に生成するタスク吹き出しは `TaskSpawnArea` の範囲を基準に配置し、Scene 固有の固定ピクセル座標を保持しない。

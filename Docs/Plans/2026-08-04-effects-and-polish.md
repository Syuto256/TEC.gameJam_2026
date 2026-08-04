# 演出強化・仕上げ計画

最終更新: 2026-08-04
状態: E0 完了。E1 から着手する。

方針の根拠は [コンボの採用と、演出の実現方式](../Decisions/2026-08-04-combo-and-effect-approach.md)。

## 目的

ゲームループとミニゲーム 6 種が通しで動く状態から、演出面を仕上げる。ゲームルール・タスク生成・ミニゲームの判定は変更しない。

## 担当の分かれ方

| 領域 | 担当 | この計画での扱い |
| --- | --- | --- |
| BGM / SE の素材収集と割り当て | 他メンバー | 対象外。仕組み（`AudioManager` / `AudioCatalog`）は実装済みで、クリップを割り当てるだけで鳴る |
| 未確定仕様の決定（ランク基準、こだわりポイント、AI と自力の評価差） | プランナー | 待ち。決まり次第 E5 で実装する |
| 難易度別のバランス調整 | プランナーと並行 | 待ち。調整値はすべて `GameTuningSettings.asset` にあり、コード変更は不要 |
| 演出・トゥイーン・ライティング | 本計画 | E1〜E4 |

## 段階

### E0: コード整理とコンボの調整値化（完了・2026-08-04）

- `GameSession` の死にコード（3 引数版 `CalculateScore`、未使用の `ScoreChanged`）を削除した。
- コンボの 3 つの調整値を `GameTuningSettings.score` へ外出しし、Inspector から変えられるようにした。
- `GameSessionComboTests` を追加した。

**確認ゲート:** コンパイルエラー 0 件、EditMode テスト全件成功。（達成。48/48）

### E1: DOTween の導入と既存演出の置き換え（完了・2026-08-05）

**DOTween 無料版**を `Assets/Plugins/Demigiant/DOTween/` へ導入した。ライセンスの詳細は [方式の決定](../Decisions/2026-08-04-combo-and-effect-approach.md) を参照。

#### asmdef の落とし穴（重要）

`Setup DOTween` の既定では **`DOTween.Modules.asmdef` が作られない**（`DOTweenSettings.asset` の `createASMDEF` が 0）。この状態だと `Modules/` 配下は既定アセンブリ `Assembly-CSharp` に入る。**asmdef で分けたアセンブリは既定アセンブリを参照できない**ため、次の差が出る。

| 対象 | asmdef 側から使えるか |
| --- | --- |
| `DOTween.dll`（`DOTween.To`、`Sequence`、`Transform` のショートカット） | 使える（自動参照される） |
| `Modules/DOTweenModuleUI.cs`（`DOAnchorPos`、`DOFade`、`DOColor`） | **使えない** |

対処として次を行った。**新しいアセンブリから DOTween の UI 拡張を使う場合は、同じ手順が必要になる。**

1. `Assets/Plugins/Demigiant/DOTween/Modules/DOTween.Modules.asmdef` を作成した。
2. `Overwork.Core.asmdef` の `references` に `DOTween.Modules` を追加した。
3. `DOTweenSettings.asset` の `createASMDEF` を 1 にした。再度 `Setup DOTween` を実行しても asmdef が消えないようにするため。

ミニゲーム側の 6 つの asmdef にはまだ追加していない。E2 以降で必要になった時点で、そのアセンブリの `references` に足す。

**TMP モジュールは無効のまま**（`textMeshProEnabled: 0`）。`TextMeshProUGUI` は `Graphic` を継承しているため `DOFade` / `DOColor` は UI モジュールだけで動く。`DOText` のような TMP 専用機能が要るときに有効化する。

#### 置き換えた内容

`HudView` のスコアポップアップを、手書きコルーチンから `Sequence` へ置き換えた。表示時間と緩急を Inspector へ出し（`popupDurationSec` / `popupEase`）、既定は従来と同じ 0.8 秒・等速にしてある。

**併せて直した不具合:** 演出中に次のポップアップが割り込むと、`StopCoroutine` が後始末を飛ばすため、褪せた色を「元の色」として拾い直し、ポップアップが呼ばれるたびに薄くなっていく状態だった。連続でタスクを成功させると必ず起きる。元の色を `Initialize` で控える方式に変更して解消した。

**確認結果（2026-08-05）:** コンパイルエラー 0 件、EditMode テスト 48/48 成功、Console エラー 0 件。Play モードでの目視確認は未実施。

### E2: 既存 UI へのトゥイーン適用（実装完了・2026-08-05 / 目視確認待ち）

調整値はすべて Inspector へ出した。シーンへの新規オブジェクト追加と Prefab の構造変更は無い。

| 対象 | 内容 | 置き場所 |
| --- | --- | --- |
| HP バー | 減少を `DOFillAmount` で補間。初回描画だけは即時反映する | `HudView.hpBarDurationSec` |
| HP バー（赤ゲージ） | 減ったぶんを赤いまま残し、少し待ってから追いつかせる | `HudView.hpDamageBarDelaySec` / `hpDamageBarDurationSec` |
| 吹き出しの出現 | 縮んだ状態から等倍へ（`OutBack`） | `TaskBubble.prefab` の `TaskBubbleView` |
| 吹き出しの消滅 | 0 まで縮めてから破棄（`InBack`） | 同上 |
| 寿命警告 | 残り寿命が一定割合を下回ったら脈動する | 同上 |
| 被弾時の揺れ | 表示中のデバイス面だけを揺らす | `DeviceWorkspace` 系 Prefab の `DeviceWorkspaceView` |

#### 実装上の制約（変更するときに踏むもの）

- **吹き出しは位置をトゥイーンできない。** `TaskSpawnArea` の Layout Group が `anchoredPosition` を driven property として支配するため。出現・消滅・警告はすべて `localScale` で表現している。`localScale` は Layout Group の管轄外である。
- **吹き出しの警告に色は使えない。** `TaskBubbleView.Refresh` が毎フレーム `background.color` を状態色で上書きするため、色のトゥイーンは 1 フレームで潰される。
- `Refresh` は毎フレーム呼ばれるので、警告トゥイーンは**状態が変わったときだけ**作り直す。毎フレーム生成すると脈動が止まって見える。
- 消滅演出のぶんだけ破棄が遅れ、その間は Layout Group の枠を占有するため、残りの吹き出しの詰め直しが遅れる。`LayoutElement.ignoreLayout` で即座に詰めることもできるが、driven property が解放されて位置が飛ぶ危険があるため採らなかった。

#### HP バーの赤ゲージ

`HpBar` の下に `HpBarDamageFill` を 1 枚追加した。並びは **`HpBarDamageFill` → `HpBarFill` → `HpValue`** で、赤いバーが `HpBarFill` より奥に来る。両方とも左から伸びる Filled Horizontal で、同じスプライトを使い、赤いバーだけ色を変えている。

- 被弾すると `HpBarFill` が先に減り、減ったぶんが赤く見える。
- `hpDamageBarDelaySec` 待ってから、赤いバーが `hpDamageBarDurationSec` かけて追いつく。
- 追いつく途中でさらに被弾した場合は、そのときの位置から新しい目標へ引き直す。
- 回復時と初回描画では赤ゲージを残さない。赤いバーが現在 HP より少ない状態を作らないためである。
- `hpBarDamageFill` は任意項目である。未設定なら赤ゲージは出ず、従来どおりの見た目に戻る。

赤の色味は `HpBarDamageFill` の `Image.Color` で変える。専用スプライトを作る場合もここを差し替えるだけでよい。

#### 揺らす範囲

**デバイス面（`PcOnly` / `TabletOnly`）だけを揺らし、HUD は固定する。** 残り時間・HP・スコアを読めなくしないためである。揺れは `DeviceWorkspaceView` が自分で行うので、専用クラスもシーンへの配置も要らない。非表示の面は揺らさない。

背景は画面より上下左右 10px 大きく作られているため、**揺れ幅が 10px を超えると画面端に隙間が見えることがある**。既定値は 8px にしてある。

**確認結果（2026-08-05）:** コンパイルエラー 0 件、EditMode テスト 48/48 成功、Console エラー 0 件。**Play モードでの目視確認は未実施。**

### E3: 新規演出

- FloatingText。`+100pt` を吹き出しの位置に、AI 失敗時に `AI ERROR` を出す。現在は HUD 中央のポップアップのみ。
- 成功・失敗エフェクト。**Image + DOTween のトゥイーンで作る**（`ParticleSystem` は Overlay Canvas の背面に隠れるため使わない）。
- シーン間のフェードイン / フェードアウト。

**確認ゲート:** 5 種類の解決結果すべてに、音と視覚の両方の手応えがある。

### E4: デバイス切替トランジション

現在 `DeviceWorkspaceView.SetVisible` は `CanvasGroup.alpha` を 0 / 1 で即座に切り替えている。ここに横スライドの遷移を入れる。

`PcOnly` / `TabletOnly` は同じ大きさの兄弟であり、非表示側も `SetActive(false)` にしていないため、`anchoredPosition` を振るだけで遷移を作れる。この構造は切替演出のために意図して選ばれている（`DeviceWorkspaceView` の remarks 参照）。

- 退場する面を片側へ、入場する面を反対側から滑らせる。イージングでスピード感を出す。
- 「椅子を回す」ような奥行き回転は `Screen Space - Overlay` では作れない。**横スライド + イージング + わずかなスケール変化**で近似し、必要なら残像用のレイヤーを 1 枚重ねる。
- **遷移中は両面が画面上に出るため、両方の `blocksRaycasts` を落とす。** 既存の `DeviceScreenController.SetSwitchEnabled` を流用する。
- 遷移の所要時間・イージング・移動量は Inspector で調整できるようにする。

**決定（2026-08-04）: 遷移中もゲーム時間を止めない。** 仕様書 14.3 では未定だったが、止めるとタブ連打で時間を凍結できてしまう。0.25〜0.35 秒であれば体感上の不利にもならない。

**確認ゲート:** 連打しても表示が壊れず、遷移中にタスクを誤操作できない。タスク寿命は遷移中も進む。

### E5: デバイス画面の作り込みと発光

#### 画面をそれらしくする（主に Prefab 作業）

端末の外枠（`PcOnly/PC` の `PC.png`）は既にある。作り込む対象は**画面の中身**である。

| 対象 | 場所 | コード変更 |
| --- | --- | --- |
| PC の待機画面をデスクトップ風にする | `DeviceWorkspace_Pc.prefab` の `DeviceFrame`（現在はほぼ白の平面） | 不要 |
| ミニゲーム画面をデスクトップ上のウィンドウ風にする | `Shared/MiniGameHost` の `Image` と各ミニゲーム Prefab | 不要 |
| なぞりをイラストソフト風にする | `Assets/Prefabs/MiniGames/TracingMiniGame.prefab` | 不要 |

**方針: `MiniGameHost` の塗りつぶしをやめ、背後のデバイス画面を透けさせる。**

`MiniGameHost` の `Image` は現在ほぼ不透明の板（`m_Color` の alpha 0.97）で、デバイス画面を完全に覆っている。これを「デスクトップ上に開いたアプリウィンドウ」の意匠に変えると、背後の `DeviceFrame` が透けて見える。結果として、

- PC 面 → デスクトップの上にアプリウィンドウが開いた絵
- タブレット面 → タブレット画面の上にイラストソフトが開いた絵

がデバイス別の分岐なしに成立する。各ミニゲーム Prefab はそれぞれのアプリの中身を持てばよい。`MiniGameHostView` に `TaskSurface` を渡してデバイス別の枠を出し分ける案は、この方針により不要になったため採らない。

**注意: `MiniGameHost` の `Image` と `RaycastTarget = true` は必ず残すこと。** これがミニゲーム中に背後のタスク吹き出しへクリックが抜けるのを止めている。alpha 0 でも `RaycastTarget` が有効なら raycast は止まるが、`Image` ごと消すと裏のタスクを掴めてしまう。

`MiniGameHost` の矩形はデバイス画面より広い場合がある。ウィンドウ意匠を入れる前に、`DeviceFrame` からのはみ出しを確認する。

なぞりのガイド線は `TracingMiniGame` が Prefab 上の複製元を複製して作るので、線の見た目も Prefab で調整できる。

#### 発光（疑似 2D ライティング）

[延期の決定](../Decisions/2026-08-04-deferred-ui-lighting-prototype.md) の前提条件は達成済みのため着手してよい。URP 2D Light は使えない（[方式の決定](../Decisions/2026-08-04-combo-and-effect-approach.md) 参照）。

Hierarchy 上の置き場所は次のとおり。uGUI の描画順は兄弟の並び順である。

- **暗幕は `Shared` の最初の子**（`Hud` より前）に置く。こうするとデバイス画面だけが暗くなり、HUD・タブ・`MiniGameHost` は明るいまま残る。
- **発光は `MiniGameHost` の子で、`Content` より前**（奥）に置き、`Content` より大きくする。
- **暗幕と発光は Raycast Target を必ず切る。** 切り忘れるとクリックが `TaskBubble` に届かなくなる。
- 暗幕の濃さは `MiniGameHost` の表示・非表示に合わせて DOTween で補間する。

**確認ゲート:** 発光を入れた状態で、タスク吹き出しの左右クリックとタブ操作が従来どおり効く。試作を見て採否を判断する。

### E6: 追加の演出要望

演出の要望は今後も増える見込みである。追加分はこの節に積み、E1〜E5 と同じ判断基準（見た目は Scene / Prefab、進行はコード）で扱う。

### E7: プランナー決定待ちの実装

決定が下り次第、次を実装する。

- リザルトの S / A / B / C ランク。現在 `ResultManager` は難易度・スコア・HP の 3 行のみ。
- こだわりポイント。`GameTuningSettings.score.craftPointsDiff1〜3` が未使用のまま残っている。使わないと決まったら削除する。
- AI 成功がコンボを伸ばすかどうか（`GameSession.Apply` の TODO）。

## 対象外

- ゲームルール、タスク生成規則、ミニゲームの判定の変更
- BGM / SE 素材の収集と割り当て
- 難易度別パラメータの数値調整
- `Assets/Personal/` 配下の編集

## 既知の残課題（この計画の外）

- **`.asset` は Git LFS 管理のため、マージ競合が起きると壊れやすい。** 2026-08-04、`6c2bda5` のマージで `Assets/Resources/AudioCatalog.asset` が競合マーカーを含んだままコミットされ、Unity が読み込めず**音が一切鳴らない状態**になっていた（`Syuto/score` 側の内容で復旧済み）。LFS ポインタの競合は Unity 上では「ファイルが壊れている」としか見えないため、`.asset` を含むマージの後は Console のエラーを必ず確認する。
- **QTE と目押しが `TaskSpawnTable.asset` に登録されていないため、本編に一度も出てこない。** Prefab も `MiniGameCatalog` の登録も揃っているので、表に 1 行足せば出るようになる。どちらのデバイス面に出すかは仕様書 22.15 で未定のため、プランナー判断を待つ。
- 日本語フォント `EnkaDotMincho24 SDF` に `締 憩 怠 捗 緊 絡 押 違 込` の字形が無い。
- タイピングの難易度 高 / 超高 の読みが未登録。読みを入れれば `RomanizationGenerator` がローマ字を生成する。
- 難易度 5 種の生成間隔・タスク寿命・同時表示上限が同値で、差は問題レベルのみ。

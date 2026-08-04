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

シーン間フェードはシーン遷移そのものに触るため、E3-a と分けて実施する。

#### E3-a: 決着演出（実装完了・2026-08-05 / 目視確認待ち）

FloatingText と成功・失敗エフェクトは**出る位置が同じ**（決着した吹き出しの場所）なので、`ResultEffectLayerView` 1 つにまとめた。

`Shared/EffectLayer` に置き、その下に非アクティブの複製元を 2 つ持つ。実行時に複製して飛ばし、終わったら破棄する。なぞりのガイド線や QTE のキー枠と同じやり方である。

```text
Shared
├─ Hud
├─ DeviceTabs
├─ MiniGameHost
├─ EffectLayer          ← 追加。ミニゲームより手前、モーダルより奥
│  ├─ FloatingTextTemplate    （TextMeshProUGUI・非アクティブ）
│  └─ BurstParticleTemplate   （Image・非アクティブ）
└─ ModalLayer           ← 最前面へ移動。ポーズが演出に隠れないようにするため
```

決着ごとの文字・色・粒の数は `styles` 配列で決める。既定値は次のとおりで、すべて Inspector で変更できる。

| 決着 | 文字 | 粒 |
| --- | --- | ---: |
| 自力成功 | （なし。下記参照） | 10 |
| AI 成功 | `+{0}`（{0} は加算スコア） | 5 |
| 自力失敗 | `MISS` | 6 |
| AI 失敗 | `AI ERROR` | 6 |
| 未着手時間切れ | `TIME OUT` | 4 |

**自力成功だけ文字を出さない。** 獲得点は `HudView` が画面中央に大きく出すため、吹き出し位置にも出すと二重になる。粒だけ残し、どの吹き出しが片付いたかは見えるようにしている。

**AI 成功には文字が要る。** `MainGameController` が中央ポップアップを出すのは `PlayerSuccess` のときだけで、AI 成功では中央に何も出ない。ここで消すとスコアが入ったことがどこにも表示されなくなる。

- 粒は `BurstParticleTemplate` の**スプライトを差し替えるだけ**で見た目が変わる。現在は無地の四角。紙吹雪や破片の素材ができたらここへ入れる。
- `MainGameController.resultEffectLayer` は任意項目である。未設定でも進行に影響せず、演出が出ないだけになる。
- 演出の位置は、吹き出しが消え始める**前**に控える。消滅演出でスケールが変わるためである。

**確認結果（2026-08-05）:** コンパイルエラー 0 件、EditMode テスト 48/48 成功、Console エラー 0 件。**Play モードでの目視確認は未実施。**

#### E3-b: シーン間フェード（実装完了・2026-08-05 / 目視確認待ち）

暗幕は `Assets/Resources/FadeOverlay.prefab` に置き、`AppServices.Ensure()` が読み込んで常駐させる。`AudioCatalog` と同じ持ち方である。

**シーンに実体を置かないのは、暗幕がどのシーンにも属さないためである。** 5 シーンすべてに同じものを置く方式では、遷移の瞬間に暗幕自体が一度消えてしまう。Prefab から読み込むことで、見た目は Inspector で調整でき、「実行時に UI をコードで組み立てない」規則も守れる。

```text
Assets/Resources/FadeOverlay.prefab
FadeOverlay          Canvas（Overlay・Sorting Order 1000）/ CanvasScaler / GraphicRaycaster
                     CanvasGroup / FadeOverlayView
└─ Fade              Image（黒・全画面・Raycast Target 有効）
```

`GameFlowController` の 4 つの遷移メソッドは、すべて `Transition(sceneName)` を通る。暗転 → `LoadScene` → 明転の順である。

| 項目 | 既定値 |
| --- | ---: |
| 暗くなるまで | 0.25 秒 |
| 真っ暗なまま待つ | 0.05 秒 |
| 明るくなるまで | 0.30 秒 |

##### 実装上の判断

- **フェードは実時間（`SetUpdate(true)`）で動かす。** ポーズ中（`timeScale = 0`）に難易度選択へ戻る経路があるため、スケール時間だと暗転が進まず操作不能になる。
- **遷移中は暗幕が入力を遮る**（`blocksRaycasts`）。さらに `TryRun` が遷移中の再要求を弾くため、暗転中にボタンを連打しても二重にシーンを読み込まない。**受け付けなかった場合に直接 `LoadScene` へ落とさないこと。** それをすると二重遷移の防止が意味を失う。
- 暗幕の Prefab が見つからない場合はエラーを 1 回だけ出し、フェードなしで従来どおり遷移する。進行が止まることはない。
- 起動直後は `alpha = 0` で、入力も遮らない。どのシーンから再生を始めても暗幕は残らない。

**確認結果（2026-08-05）:** コンパイルエラー 0 件、EditMode テスト 48/48 成功、Console エラー 0 件。**Play モードでの目視確認は未実施。**

### E4: デバイス切替トランジション

#### E4-a: 剛体スライド（実装完了・2026-08-05 / 目視確認待ち）

「席が横一列に並んでいて、視点のほうが移動する」という見立てで作る。退場する面と入場する面を**画面幅ぶんずらして固定したまま、2 枚まとめて横へ動かす**（剛体スライド）。半透明で重ねるクロスフェードは採らない。2 枚が重なる瞬間に「同じ場所に 2 つの机がある」ように見え、席を移った感じが消えるためである。

`PcOnly` / `TabletOnly` はどちらも**完全ストレッチ・`anchoredPosition (0,0)`・`sizeDelta (0,0)`** で、矩形が画面と完全に一致する。したがって画面幅ちょうど動かせば継ぎ目に隙間も重なりも出ない。移動量は `rect.width` を実行時に読む（1920 は直書きしない）。アスペクト比が 16:9 以外でも 1 画面ぶんになる。

- **静止時の状態は従来どおり `alpha` 0 / 1。座標を使うのは移動中だけ。** 非表示面を常に画面外へ置く作りは採らない。`SetInteractionEnabled` と `PlayDamageShake` が `alpha > 0` を「今見えている面か」の判定に使っているため、画面外の面まで操作可能・被弾で揺れる対象になってしまう。
- **席の並び順は `GameManager` の `workspaces` 配列順**（0 が左端）。3 面目を足すときは配列に挿す位置だけで移動方向が決まる。席が何個離れていても移動量は画面幅 1 つぶんに固定する（同時に 2 面しか映らないため、伸ばしても速度が落ちるだけ）。
- 調整値は `DeviceScreenController` に集約（`slideDurationSec` 0.28 / `slideEase` `OutQuint` / `slidePeakScale` 1.03）。面ごとに置くと 2 つを手で同期させることになる。
- `Initialize` からの初回 `Show(Pc)` は演出なしの即時パス。

##### スケールは拡大方向にしか動かせない（重要）

当初は「中間で 0.95 まで縮めて戻す」案だったが、**縮小は成立しない。** 画面幅 1920 を 0.95 倍にすると左右に 48px ずつ隙間が空く。`Background` の余白は上下左右 10px しかないため埋まらず、背後のスカイボックスが覗く。面の継ぎ目にも同じ幅の隙間が出る。

**拡大方向（1.0 → 1.03 → 1.0）なら、画面の端も継ぎ目も「重なる」側に倒れるので破綻しない。** `slidePeakScale` は `[Min(1f)]` にして 1 未満を入れられないようにしてある。

##### 被弾の揺れとは排他

`PlayDamageShake` は `OnKill` で `shakeOrigin`（＝画面中央）へ座標を戻すため、**移動中に被弾すると画面外を飛んでいる面が中央へワープする。** `DeviceWorkspaceView.sliding` を立てて、移動中は揺れを見送る。その 0.28 秒だけ揺れは出ないが、HP バーの赤ゲージと SE は通常どおり出るため、被弾が伝わらなくなることはない。

##### 実装上の判断

- **実時間で動かす（`SetUpdate(true)`）。** ポーズボタンは `Hud` にあり移動中も押せる。`timeScale` 依存だと途中で 0 になったとき半端な位置で固まる。
- **ゲーム時間は止めない**（2026-08-04 の決定を維持）。止めるとタブ連打で時間を凍結でき、タスク寿命も延びて切替が有利になる。
- 移動中はタブと両面の操作を落とし、**終了時は `true` 固定ではなく `switchEnabled` を再適用する。** ミニゲーム中の切替禁止を踏み潰さないため。
- 移動中の再要求は無視する。タブは切ってあるが `Show` は public のため。
- 幅が 0（レイアウト未確定）や `slideDurationSec` が 0 のときは演出なしで切り替える。

##### 把握しておく副作用

`EffectLayer` は `Shared` にあり動かない。移動中にタスクが期限切れになると、その決着演出だけ飛んでいく面に付いていかず画面上に取り残される。0.28 秒の窓なので許容する。

**確認ゲート:** 連打しても表示が壊れず、遷移中にタスクを誤操作できない。タスク寿命は遷移中も進む。画面の端と面の継ぎ目に隙間が出ない。

#### E4-b: モーション線（未着手）

剛体スライドに、進行方向と同じ向きの水平な線を重ねてスピード感を足す。

**方式は「細い帯を複製して流す」**（`ResultEffectLayerView` と同じ、テンプレートをシーンに置いて実行時に複製する方式）。太さ 2〜4px の Image を数本、長さ・速度・縦位置をばらつかせて横に走らせ、フェードで消す。**新規アセットが 0 で済む**のが採用理由で、あとからテンプレートに streak 用スプライトを差せばコード変更なしで質を上げられる。タイリングテクスチャを `RawImage` の `uvRect` で流す方式は見た目が上だが、繰り返し可能なモーション線テクスチャが前提になるため後回しとする。

- 置き場所は `Shared` の中、**`Hud` より前**（スライドする面の上・HUD の下）。Raycast Target は OFF。
- スライド本体と同じ `Sequence` に混ぜる。
- **着地より前に消し切る。** settle まで残ると、動きが終わったのに尾を引いてブレたように見える。
- **初期値は控えめにする（本数少なめ・alpha 0.2〜0.35）。** 切替はゲーム中に何度も起きるため、強い線が数秒おきに出るとうるさく、酔いにも繋がる。実際に遊んでから上げ下げする前提で Inspector に出す。

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

# 共通ウィンドウの本素材を繋ぐ（作業指示書）

作成: 2026-08-06（同日改訂）／ 対象: ミニゲーム共通の窓枠と制限時間バー

**これを最初にやってください。** タイピング・連打・仕分けの作業は、すべてこの窓の中に乗ります。

> **改訂の経緯:** 初版は [ミニゲーム作業風 UI 必要素材](../Specifications/minigame-ui-assets.md)
> （以下「発注書」）を読まずに書かれており、方式が発注書と食い違っていた。
> **本素材はモノクロで描き、色はゲーム側で乗せる設計**（発注書 §0）のため、
> 初版にあった「色を白に戻す」「9 スライスを 16 / PPU 100 で設定し直す」は**すべて撤回**。

## 作業前に必ず読むもの

- **[ミニゲーム作業風 UI 必要素材](../Specifications/minigame-ui-assets.md)** — この作業の発注書。特に §0（共通ルール）と §8（仮素材と上書きの想定）
- [AGENTS.md](../../AGENTS.md) の「先に読むこと: 過去に手戻りが起きた箇所」

## 触らないもの

- 各 Prefab の `Image.color`。**濃紺の Body・ピンクの Border は意図された配色です**
  （発注書 §0「色はゲーム側で乗せる」）。仮素材の間に合わせではありません
- `WorkArea` の中身。**別 PR です**
- `QteMiniGame`。発注書 §7 のとおり**仕組みごと作り直す予定**のため、素材はあるが繋ぎません
- ミニゲームの進行・判定

---

## 現状: 本素材は納品済みだが、納品先がずれて繋がっていない

発注書 §8 の想定は「**仮素材と同じ名前で上書きすれば、取り込み設定（ふち 48 / PPU 200）が
自動で引き継がれ、Prefab 側の作業は発生しない**」だった。

実際には、仮素材が `Assets/Sprites/MiniGameUI/Common/`（発注書 §10 の納品先）にあるのに対し、
本素材は **`Assets/Sprites/InGameUI/MiniGameUI/Common/` に納品された。** 上書きされるはずの
ものが並置され、**Prefab は今も仮素材を参照している。**

| | 仮 `Assets/Sprites/MiniGameUI/` | 本番 `Assets/Sprites/InGameUI/MiniGameUI/` |
| --- | --- | --- |
| Prefab からの参照 | **こちらが使われている** | 1 つも使われていない |
| 取り込み設定 | **ふち 48 / PPU 200（正しい）** | ふち 0 / PPU 100（未設定） |
| 中身 | 白い仮図形 | **本番の絵** |

**12 枚すべて、縦横の画素数は仮と本番で完全に一致している。** 発注書の寸法どおり。

### 納品状況の全体（2026-08-06 実測）

| 区分 | 発注 | 納品 | 備考 |
| --- | --- | --- | --- |
| 共通フレーム | 12 | **12** | **この PR で繋ぐ** |
| 仕分け | 12 | 6 | アイコン 6 枚のみ。下地・枠・種類別フォルダは「用意しない」決定済み |
| QTE | 3 | 4 | `KeyCell_Pushed` が追加で来ている。仕様確定待ちのため保留 |
| 連打 | 9 | **0** | `Preview_01〜08` / `PreviewFrame` 未納品 |
| タイミング | 6 | **0** | 未納品 |
| なぞり | 8 | **0** | 未納品 |

---

## やること

### 1. 仮素材の位置へ本素材を入れる（guid を保つ移動 + バイト上書き）

置き場所は**本素材側（`Assets/Sprites/InGameUI/MiniGameUI/`）に一本化**します。
仕分け・QTE の素材が既にそこにあり、今後の納品も同じ場所に来るためです。

ただし単純にコピーすると **Prefab の参照（guid）と取り込み設定（meta）が引き継がれません。**
次の順で行ってください。

1. **本素材 12 枚の PNG バイトを退避する**（一時フォルダへコピー）
2. 本素材のフォルダ `Assets/Sprites/InGameUI/MiniGameUI/Common` を **meta ごと削除**する
   （`delete_asset --confirm true`）
3. 仮素材のフォルダを移動する:
   `AssetDatabase.MoveAsset("Assets/Sprites/MiniGameUI/Common", "Assets/Sprites/InGameUI/MiniGameUI/Common")`
   — **移動は guid を保つため、5 つの Prefab の参照が全部ついてくる**
4. 空になった `Assets/Sprites/MiniGameUI/` を削除する
5. 移動先の PNG 12 枚を、退避しておいた本素材のバイトで**上書き**し、Reimport する
   — meta は仮素材のもの（**ふち 48 / PPU 200**）が残るので、発注書 §8 の
   「同じ名前で上書き」がここで成立する

**順番を守ってください。** 2 より先に 3 をやると同名衝突で移動が失敗します。
1 を忘れて 2 をやると**本素材が消えます。**

### 2. 検証する

- 5 つの Prefab（Typing / RapidClick / Sorting / TimingStop / Tracing）の
  `Body` / `TitleBar` / `Border` / `AppIcon` / `BtnClose` / `BtnMaximize` / `BtnMinimize` が
  **欠けずに絵を表示している**こと（参照切れなら白い四角か missing になる）
- 移動先 meta の `spriteBorder`（Body / Border 全周 48、TitleBar 左右 48）と
  `spritePixelsToUnits: 200` が**変わっていない**こと
- アイコンの対応: Typing=Document / RapidClick=Viewer / Sorting=Explorer /
  TimingStop=Editor / Tracing=Paint（発注書 §1 の表どおり。移動方式なら自動で保たれるはず）

### 3. 制限時間バーを直す

**発注書 §1 に「残り時間ゲージに絵は要らない。単色の帯で出す」と明記されています。**
現状は Unity 標準の `UISprite`（角丸・四隅が透明）が入っており、これが
「左右の端が透明でゲージに見えない」の原因です。発注書の指定どおり単色に戻します。

#### 3-1. 下地の絵を外す

`TimeGauge` の **Source Image を `None`** にします（5 つの Prefab すべて）。

#### 3-2. ⚠ 伸びる部分は絵を外すだけでは壊れます

**`Fill` の Source Image を `None` にすると `fillAmount` が効かなくなり、ゲージが減らなくなります。**

`Image.OnPopulateMesh` は絵が無いとき `Type` を見ずに素の四角を描いて終わるためです
（`Library/PackageCache/com.unity.ugui@e20f1880fa04/Runtime/UGUI/UI/Core/Image.cs:884` で確認済み）。

**そこで `Fill` も絵を `None` にしたうえで、残り時間を「幅」で表すように変えます。**

1. `Fill` の `RectTransform` を左寄せの引き伸ばしにする
   - `anchorMin = (0, 0)` / `anchorMax = (0, 1)` / `offsetMin` `offsetMax` は 0
2. `MiniGameBase.RefreshTimeUi()` で `fillAmount` の代わりに `anchorMax.x` に割合を書く

```csharp
// 絵を外した Image では fillAmount が働かないため、幅そのもので残りを表す。
// anchorMax なら、バーの幅（952 と 832 が混在している）を知らなくても割合で書ける。
var ratio = TimeLimit <= 0f ? 0f : Mathf.Clamp01(remaining / TimeLimit);
var fillRect = timeGaugeFill.rectTransform;
fillRect.anchorMax = new Vector2(ratio, 1f);
```

**`Image` の参照（`timeGaugeFill`）はそのまま残してください。** 色を変えるのに使います。

#### 3-3. 残り時間で色を変える（任意）

余力があれば `MiniGameBase` に色替えを足します。全ミニゲームで一度に効きます。
しきい値（例: 0.35 / 0.15）と色 3 つを `[SerializeField]` で持たせ、Prefab 側で調整できる形に。
**通常時は今の rgb(97,242,199) のままにしてください。**

### 4. 発注書の記録を現実に合わせる

[発注書](../Specifications/minigame-ui-assets.md) の **§10（納品先）を
`Assets/Sprites/InGameUI/MiniGameUI/` に書き換え**、§8 の「仮素材」の段落に
**本素材へ差し替え済み**である旨を追記してください。次の納品（連打・タイミング・なぞり）が
また別の場所へ行かないようにするためです。

---

## 受け入れ条件

- [ ] 5 つの Prefab すべてで、窓の下地・タイトルバー・縁・アイコン・ボタンが**本素材で**表示される
- [ ] 参照切れ（missing / 白四角）が 1 つも無い
- [ ] 移動先の meta が ふち 48（TitleBar は左右のみ）/ PPU 200 のまま
- [ ] `Image.color` を**一切変えていない**
- [ ] `Assets/Sprites/MiniGameUI/`（旧パス）が消えている
- [ ] 制限時間バーの**左右の端がはっきりした縦線**になっている
- [ ] **バーが実際に減る。** 時間切れまで放置して 0 まで減りきることを確かめる
- [ ] `unity command run_tests --mode EditMode` が全件通る（**73 件**）
- [ ] コンソールにエラー・例外が 0 件

## 目視で見てほしいところ（重要）

モノクロ素材に色を乗せる設計は**乗算**なので、素材が暗く描かれていると色が潰れます。
実測では本番 `WindowBody` の中央は `#2B2B2B`、`TitleBar` は `#323232` と、
発注書の「白〜グレー」の中では**暗い側**です。

- **`Body`（`#0F1421` を乗算）が真っ黒に潰れていないか。** 潰れて見えたら、
  素材ではなく**色の側を明るくする方向**で報告してください（素材は正、色は調整可）
- `Border` の線。本番の線は **1px で描かれており**、PPU 200 だと画面上 0.5px 相当に
  なります。**線が消えたり滲んだりしていないか。** 消えていたら報告してください
- タイトルバーの文字とアイコンが、本番の下地の上で読めるか

---

## 報告してほしいもの

1. `git diff --stat`
2. `run_tests` の通過数
3. コンソールのエラー・例外の件数
4. **ミニゲームを開いた状態のスクリーンショット**（乗算潰れと線の太さの判断に使います）

指示と食い違う状態を見つけた場合は、直す前に報告してください。
この指示書は 2026-08-06 時点の実測と発注書に基づいています。

# ミニゲームの窓まわりの暗さと発光を消す（作業指示書）

作成: 2026-08-06 ／ 対象: ミニゲーム表示中に窓の周囲へ出る「暗いエリア」と「発光」

## ⚠ 着手のタイミング

**[共通 UI 部品の整備](2026-08-06-shared-ui-components.md) の PR 2（オプション画面）がマージされてから始めてください。**
どちらも `Game.unity` と `Tutorial.unity` を触ります。同時に進めるとシーンが衝突します。

## 作業前に必ず読むもの

- [AGENTS.md](../../AGENTS.md) の「先に読むこと: 過去に手戻りが起きた箇所」
- シーンと Prefab は Unity CLI（Pipeline）経由で編集します。**YAML を直接書き換えないでください。**

---

## 現状（2026-08-06 に Unity 上で実測）

### 発光は 2 系統ある

| 出どころ | 色 | 不透明度 | 絵の大きさ | 窓の大きさ | 出るシーン |
| --- | --- | --- | --- | --- | --- |
| 各ミニゲーム Prefab の `AppWindowFrame/WindowGlow` | 薄紫 rgb(217,191,255) | 0.35 | 1384×964 | 1000×580 | 全部 |
| `MainCanvas/Shared/MiniGameHost/WindowGlow` | 水色 rgb(153,189,255) | 0.40 | 1525×1062 | 1141×678 | `Game` のみ |

**`Game` では 2 枚が重なります。** どちらも窓より一回り大きい `ScreenGlow` の絵なので、
窓の外側へはみ出した部分が光って見えます。

2 つ目はシーンに直接置かれた値ではなく、`FocusLightingView` が実行時に点けています
（`glowAlpha = 0.40`）。**`Tutorial` には `FocusLightingView` 自体がありません。**

### 暗いエリアは `Tutorial` だけに出る

| 出どころ | 色 | 不透明度 | 大きさ | シーン |
| --- | --- | --- | --- | --- |
| `MainCanvas/Shared/MiniGameHost` の Image | 濃紺 rgb(26,28,43) | **0.97** | 1141×678 | **`Tutorial`** |
| 同じ場所の Image | 濃紺 rgb(26,28,43) | 0.00 | 1141×678 | `Game`（既に見えない） |
| `MainCanvas/Shared/FocusDimmer` | 黒 | 0.00 | 1940×1100 | `Game`（`0128f54` で切った） |

**窓（1000×580）より `MiniGameHost`（1141×678）のほうが大きいため、`Tutorial` では窓の周囲に
70px ほどの濃紺の縁が出ます。** これが「窓の周辺の暗いエリア」の正体です。

`Game` と `Tutorial` で同じ場所の値が違っているのは、**シーンごとに別々に組まれていて
そろえられていない**ためです。

---

## やること

### 1. `Tutorial` の暗い下地を消す

対象: `Assets/Scenes/Tutorial.unity` の `MainCanvas/Shared/MiniGameHost`

Image の色の不透明度を **0.97 → 0** にします。`Game` と同じ状態になります。

**Image コンポーネントは消さないでください。** 色を戻せば元に戻せる形にしておきます。

### 2. シーン側の発光を消す（`Game`）

対象: `Assets/Scenes/Game.unity` の `MainCanvas/Shared/FocusDimmer` に付いた `FocusLightingView`

`glowAlpha` を **0.40 → 0** にします。`dimAlpha` は既に 0 です。

**`FocusLightingView` は消さないでください。** 両方 0 になって何もしなくなりますが、
配線は生きているので、あとで値を戻すだけで演出を復活できます。

### 3. `FocusLightingView` に、光が 0 のときの早抜けを足す

対象: [Assets/Scripts/Core/UI/FocusLightingView.cs](../../Assets/Scripts/Core/UI/FocusLightingView.cs)

現在の `SetFocused` は、`glowAlpha` が 0 でも次を行います。

```csharp
glow.enabled = true;
glowTween = glow.DOFade(glowAlpha, fadeInSec)...
```

**これでは「透明な 1525×1062 の板」を毎フレーム描き続けることになります。**
暗幕側の `FadeDimmer` には同じ早抜けが既に入っているので、光側にもそろえてください。

```csharp
// 強さが 0 なら描画に入れない。入れても、透明な板を毎フレーム描くだけになる。
if (glowAlpha <= 0f)
{
    Clear(glow);
    return;
}
```

`Clear` は既にある private メソッドです。新しく作らないでください。

### 4. ミニゲーム Prefab 側の発光を消す（6 個）

以下の 6 つの Prefab で、`AppWindowFrame/WindowGlow` の **GameObject を非アクティブ**にします。

- `Assets/Prefabs/MiniGames/RapidClickMiniGame.prefab`
- `Assets/Prefabs/MiniGames/SortingMiniGame.prefab`
- `Assets/Prefabs/MiniGames/TimingStopMiniGame.prefab`
- `Assets/Prefabs/MiniGames/TracingMiniGame.prefab`
- `Assets/Prefabs/MiniGames/TypingMiniGame.prefab`
- `Assets/Prefabs/MiniGames/QteMiniGame.prefab` — **この 1 つだけ構造が違います。**
  `AppWindowFrame` を持たず、ルート直下が `Background`（rgb(15,20,33) / 0.97）です。
  `WindowGlow` が無いか確認し、無ければ何もしないでください。

**削除ではなく非アクティブにするのは、戻すのがチェックひとつで済むためです。**

### 5. 見えない当たり判定を外す（1 と 2 の結果として必要になります）

対象: `Game.unity` と `Tutorial.unity` の `MainCanvas/Shared/MiniGameHost` の Image

**Raycast Target を切ってください。**

1 の作業で `Tutorial` の下地が見えなくなると、**見えないまま画面中央の 1141×678 を覆って
クリックを吸う板だけが残ります。**（`Game` は元からこの状態です。）

`0128f54` でミニゲーム中も他のタスクを右クリックで AI に任せられるようにしましたが、
**この板の下にあるタスクだけは任せられません。** 画面 1920×1080 のうち中央の広い範囲が対象です。

窓そのもののクリックは、ミニゲーム Prefab の `AppWindowFrame/Body`（1000×580）と
`Border` が受け止めるため、切っても窓の背後へ抜けることはありません。

---

## 受け入れ条件

- [ ] `Game` でミニゲームを開いたとき、窓の外側に光も暗い縁も出ない
- [ ] `Tutorial` でミニゲームを開いたとき、窓の外側に光も暗い縁も出ない
- [ ] **窓の中の見た目は変わっていない**（`Body` / `TitleBar` / `Border` / `TimeGauge` はそのまま）
- [ ] ミニゲームを開いている最中に、窓の外にあるタスクを右クリックで AI に任せられる
- [ ] 窓の上をクリックしても、背後のタスクに反応しない
- [ ] ミニゲームを閉じたあと、画面に何も残らない
- [ ] `unity command run_tests --mode EditMode` が全件通る
- [ ] コンソールにエラー・例外が 0 件

## 目視で見てほしいところ

- 窓の四隅。**光を消したことで窓の輪郭が背景に沈んでいないか。**
  沈んで見える場合は、`Border`（rgb(255,89,166) の枠）が効いているか確認してください。
- `Game` と `Tutorial` で、窓のまわりの見え方が同じか。**違っていたらどちらかの直し漏れです。**

## 触らないもの

- `AppWindowFrame/ResultOverlay`（rgb(8,10,18) / 0.82）。**決着表示の覆いであり、意図した暗さです。**
- `MainCanvas/PcOnly/RoomDimmer` と `TabletOnly/RoomDimmer`（0.42）。
  ミニゲームとは無関係の、常時かかっている部屋の暗さです。別件で扱います。
- `TracingMiniGame` の `WorkArea/TracingArea`（0.90）。窓の中の描画エリアです。
- ミニゲームの中身のロジック。

---

## 報告してほしいもの

1. `git diff --stat`
2. `run_tests` の通過数
3. コンソールのエラー・例外の件数
4. **`QteMiniGame.prefab` に `WindowGlow` があったかどうか**

指示と食い違う状態を見つけた場合は、直す前に報告してください。
この指示書は 2026-08-06 時点の実測に基づいています。

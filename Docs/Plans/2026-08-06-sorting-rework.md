# 仕分けミニゲームの作り直し（作業指示書）

作成: 2026-08-06 ／ 対象: [post-merge-worklist](2026-08-06-post-merge-worklist.md) の 4-2

**単独の PR です。** 表示だけの変更（4-1）とは独立しています。
この作業だけ長引いても他が止まらないよう分けてあります。

## 作業前に必ず読むもの

- **[ミニゲーム作業風 UI 必要素材](../Specifications/minigame-ui-assets.md)（発注書）の §2 と §12-1** —
  仕分けは「素材到着後の本組みの先頭」と位置づけられています。この指示書がその本組みです
- [AGENTS.md](../../AGENTS.md) の「先に読むこと: 過去に手戻りが起きた箇所」
- 見た目は Prefab、進行はコード

**発注書 §2 との差分:** 発注 12 枚のうち納品は 6 枚です。種類別フォルダ
（`FolderIcon_Images` など）の代わりに汎用 `FolderIcon_Base` + 色分けで組みます。
`FileIcon_Junk` / `TrashIcon`（不要ファイルをゴミ箱へ捨てる要素）は納品されておらず、
**この PR では扱いません。** `DropBoxFrame` / `FileCardFrame` は「用意しない」決定済み
（2026-08-06 の 決まったこと A-5）。発注書 §2 の「箱の発光・赤・緑は着色で出す」は、
`FolderIcon_Base_Open` への差し替え + 色でそのまま実現できます。

## 触らないもの

- 他の 5 つのミニゲーム
- `AppWindowFrame` の `TitleBar` / `MenuBar` / `StatusBar` / `TimeGauge` / `Border`
- `MiniGameBase` と `MiniGameCatalog`

---

## 現状

`SortingMiniGame.prefab` に **4 つのレイアウトが実体として置かれ**、箱もカードも手で並べてあります。

| レベル | 箱 | カード | 許容ミス |
| --- | --- | --- | --- |
| 1 | 1 個 | 1 枚 | 2 |
| 2 | 1 個 | 3 枚 | 2 |
| 3 | 2 個 | 4 枚 | 2 |
| 4 | 3 個 | 4 枚 | 2 |

- 箱は 240×200、画面下（y = -145）。カードは 100×100、画面上（y = 155）
- 種類は `categoryId` の文字列 `"A"` / `"B"` / `"C"`
- **絵は汎用の `Background` スプライトで、ファイルらしさもフォルダらしさもありません**
- ランダム性は無く、毎回まったく同じ配置です

`SortingMiniGame.Initialize` は `levelLayouts` から該当レベルを探して `SetActive` するだけです。

## 目指す形

```
   ┌────────────────────────────────────────┐
   │  [文書]   [画像]   [音声]   [コード]    │ ← フォルダ 4 種を上に固定・常設
   │                                        │
   │      🗎   🗎   🗎   🗎   🗎             │ ← ファイル（レベルごとに数と種類が変わる）
   └────────────────────────────────────────┘
```

- **フォルダは 4 種すべて常に出す。** そのレベルで使わない種類も出したままにする
- ファイルは**レベルごとに「枚数」と「同時に出る種類数の上限」**を決め、その範囲でランダム
- ファイルもフォルダも**実行時に生成**する。手で並べるのをやめる

---

## 先に知っておくこと: 色はファイル側だけに付いています

`Assets/Sprites/InGameUI/MiniGameUI/Sorting/` の PNG を実際に読んで色を測りました。

| ファイル | 実測した色 | 彩度 |
| --- | --- | --- |
| `FileIcon_Image` | **`#94BDEA`**（青） | 0.40 |
| `FileIcon_Audio` | **`#E68F85`**（赤） | 0.45 |
| `FileIcon_Script` | **`#7BB16C`**（緑） | 0.40 |
| `FileIcon_Document` | **無彩色（灰）** | 0.00 |
| `FolderIcon_Base` / `_Open` | **無彩色（薄い灰）** | 0.00 |

**ファイルには既に色が入っています。染め直さないでください。** 絵が持っている色をそのまま出します。

いっぽう**フォルダは無地の灰色が 1 種類しかなく、4 つ並べても区別できません。**
そこで、**フォルダ側だけをファイルの色に合わせて染めます。**

| 種類 | フォルダに指定する色 |
| --- | --- |
| 画像 | `#94BDEA` |
| 音声 | `#E68F85` |
| コード | `#7BB16C` |
| 文書 | **白（`#FFFFFF`）= 染めない。** 元の灰色のまま |

**`Image.color` は掛け算です。** フォルダの絵が薄い灰色なので、
指定した色よりわずかに暗く出ます。狙った色にぴったり合わせたい場合はここを調整してください。

色は `[SerializeField]` で持たせ、Prefab 側で変えられるようにします。

あわせて、**フォルダの下に日本語のラベルを出してください**（文書 / 画像 / 音声 / コード）。
色だけだと、どの色がどの種類なのかを覚えてもらう必要があります。

---

## やること

### 1. 種類を型として定義する

`"A"` / `"B"` / `"C"` の文字列をやめ、4 種を列挙型にします。

```csharp
public enum SortingFileKind { Document, Image, Audio, Script }
```

種類ごとの「絵・色・ラベル」をまとめた `[System.Serializable]` のクラスを作り、
`SortingMiniGame` に 4 件ぶんの配列として持たせてください。

```csharp
[System.Serializable]
public sealed class SortingKindStyle
{
    public SortingFileKind kind;
    [Tooltip("ファイルのアイコン。色は絵が持っているので染めない。")] public Sprite fileIcon;
    [Tooltip("フォルダを染める色。フォルダの絵は無地なので、種類の見分けはこの色が担う。\n" +
             "文書は白（染めない）にしてファイルの灰色と合わせる。")] public Color folderTint;
    [Tooltip("フォルダの下に出す名前。")] public string label;
}
```

`SortingDraggable` と `SortingDropBox` の `categoryId`（string）は
`SortingFileKind` に置き換えます。**文字列の打ち間違いで一致しなくなる事故を無くすためです。**

### 2. ファイルカードの Prefab を作る

`Assets/Prefabs/MiniGames/Parts/SortingFileCard.prefab`（フォルダが無ければ作成）

- 大きさ 100×100
- `Image`（アイコン）+ `SortingDraggable` + `CanvasGroup`
  （`SortingDraggable` は `RectTransform` と `CanvasGroup` を `RequireComponent` しています）
- 種類は実行時に決まるので、**Prefab には仮の絵を入れておくだけでよい**

`SortingDraggable` に、生成後に種類を差し込むためのメソッドを足してください。

```csharp
/// <summary>生成直後に種類と絵を決める。Prefab には種類を持たせない。</summary>
/// <remarks>色は渡さない。ファイルの絵が最初から色を持っているためである。</remarks>
public void Setup(SortingFileKind fileKind, Sprite icon)
```

### 3. フォルダ列を作る

`WorkArea` の中に `FolderRow` を作り、**4 つのフォルダを横一列**に置きます。

- フォルダ 1 つは `Image`（`FolderIcon_Base`）+ `SortingDropBox` + 下にラベル用の `TMP_Text`
- Prefab 化して 4 つ並べるか、実行時に 4 つ生成するか、**どちらでも構いません**
- 大きさの目安: 1 つ 180×150、間隔 24、4 つで 792 幅。`WorkArea`（約 960 幅）に収まります
- 種類・色・ラベルは `SortingKindStyle` から流し込む。**色を付けるのはフォルダの `Image` だけ**で、
  ラベルの文字色は 4 つとも同じにしてください（薄い色だと読めなくなります）

**4 種すべてを常に出してください。** そのレベルで出ないファイルのフォルダも並べたままにします。

### 4. レベル別の設定を持たせる

**`GameTuningSettings` や `DifficultyProfile` には足さないでください。**
確認したところ、これらは**ミニゲーム固有の設定を一切持っていません。**
`RapidClickMiniGame` が `baseClicks` / `clicksPerLevel` を自分で持っているのと同じく、
**`SortingMiniGame` 側に持たせるのがこのプロジェクトのやり方です。**
（計画書には `GameTuningSettings` と書いてありますが、実物と合っていません。）

```csharp
[System.Serializable]
public sealed class SortingLevelSetting
{
    [Range(1, 4)] public int level = 1;
    [Tooltip("整理するファイルの枚数。")]
    [Min(1)] public int fileCount = 3;
    [Tooltip("同時に出る種類数の上限。1 なら 1 種類だけが出る。")]
    [Range(1, 4)] public int maxKinds = 1;
    [Tooltip("このレベルで失敗になるまでの誤配置数。")]
    [Min(1)] public int allowedMisses = 2;
}
```

初期値の目安です。**Prefab 側で調整できる形にしておいてください。**

| レベル | ファイル枚数 | 種類数の上限 | 許容ミス |
| --- | --- | --- | --- |
| 1 | 3 | 1 | 2 |
| 2 | 4 | 2 | 2 |
| 3 | 5 | 3 | 2 |
| 4 | 6 | 4 | 2 |

### 5. 生成のきまり

`Initialize(difficulty, timeLimit)` の中で、レベルに対応する設定を引いて生成します。

1. 4 種類から `maxKinds` 個をランダムに選ぶ
2. **選んだ種類が最低 1 枚ずつ出るようにする。** これをしないと種類数の上限が意味を持ちません
3. 残りの枚数は、選んだ種類の中からランダムに割り振る
4. `fileCount` 枚を**横一列に等間隔**で並べる。位置はスクリプトで計算する

並べる位置の目安: 1 枚 100×100、間隔 30、最大 6 枚で 750 幅。中央ぞろえ。

### 6. 受け入れ中のフィードバック（任意）

`FolderIcon_Base_Open` を使い、**ドラッグ中のファイルがフォルダに重なったら開いた絵に差し替え**ます。

`SortingDraggable` はドラッグ中に `blocksRaycasts = false` にしているため、
`SortingDropBox` に `IPointerEnterHandler` / `IPointerExitHandler` を足せば重なりを拾えます。

### 7. 古い仕組みを消す

置き換えが済んだら、次を削除してください。

- `SortingMiniGame.SortingLevelLayout` と `levelLayouts`、`FindLayout`、`TryActivateLayout`
- Prefab 上の `Level1Layout` 〜 `Level4Layout` と、その中の箱・カード

**動くところまで確認してから消してください。** 先に消すと戻る場所が無くなります。

---

## 技術的な注意

### レイアウト用のコンポーネントを使わないこと

**`GridLayoutGroup` や `HorizontalLayoutGroup` を使わないでください。**

`SortingDraggable.ReturnToStart()` は `anchoredPosition` を直接書き戻します。
レイアウト用のコンポーネントが付いていると、**次のフレームで位置を上書きされ、
ドラッグそのものが壊れます。** 位置はスクリプトで計算して直接入れてください。

### 吸い寄せは今も切れています

`SortingDraggable` の `snapDistance = 0` / `magnetStrength = 0` で、吸い寄せは効いていません。
**この PR で有効にする必要はありません。** 触るとしても最後に、目視で確かめながらにしてください。

### 箱の探し方

`SortingDraggable.OnBeginDrag` は `FindObjectsByType<SortingDropBox>` で毎回探しています。
実行時に生成しても拾えますが、**`activeInHierarchy` で絞っている**ので、
非アクティブなフォルダは対象外になります。

---

## 受け入れ条件

- [ ] レベル 1〜4 のすべてで、フォルダが **4 つとも**出る
- [ ] 4 つのフォルダが**色とラベルで見分けられる**
- [ ] **フォルダの色が、対応するファイルの絵の色と同じ系統になっている**
      （画像=青 / 音声=赤 / コード=緑 / 文書=灰）
- [ ] **ファイルの絵は染めていない。** 元の PNG の色のまま出ている
- [ ] 同じレベルを 2 回開くと、**ファイルの並びか種類が変わる**（毎回同じにならない）
- [ ] レベルごとのファイル枚数と種類数が、設定した値を超えない
- [ ] 正しいフォルダに入れるとファイルが消え、全部入れるとクリアになる
- [ ] 違うフォルダに入れるとミスが増え、上限でゲームオーバーになる
- [ ] **ドラッグして途中で離したファイルが、元の位置に戻る**
- [ ] `Level1Layout` 〜 `Level4Layout` が Prefab から消えている
- [ ] `unity command run_tests --mode EditMode` が全件通る（**67 件**）
- [ ] コンソールにエラー・例外が 0 件

## 目視で見てほしいところ

- ファイルが 6 枚出たとき、窓の幅からはみ出さないか
- フォルダのラベルが読める大きさか
- **ファイルをフォルダの上へ運ぶ距離が遠すぎないか。** 上下に離れているため、
  6 枚あると手数が多く感じるかもしれません
- チュートリアルで仕分けが出るか（出る場合、制限時間 99 秒で動きます）

---

## 報告してほしいもの

1. `git diff --stat`
2. `run_tests` の通過数
3. コンソールのエラー・例外の件数
4. **レベルごとに実際に生成された枚数と種類の内訳**（数回ぶん）

指示と食い違う状態を見つけた場合は、直す前に報告してください。
この指示書は 2026-08-06 時点の実測に基づいています。

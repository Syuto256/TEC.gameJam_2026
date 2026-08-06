# 共通 UI 部品の整備（作業指示書）

作成: 2026-08-06 ／ 対象: [post-merge-worklist](2026-08-06-post-merge-worklist.md) の 3 番

この指示書は **2 つの独立した PR** に分かれます。3-1 と 3-2 は互いに依存しません。
どちらから始めても構いませんが、**混ぜないでください。** 片方が駄目でももう片方を残せます。

## 作業前に必ず読むもの

- [AGENTS.md](../../AGENTS.md) の「先に読むこと: 過去に手戻りが起きた箇所」
- [Unity CLI / Pipeline 運用](../Operations/unity-pipeline.md)

特に効いてくるもの:

- **シーンとアセットは Unity CLI（Pipeline）経由で編集します。** `.unity` / `.prefab` を
  テキストとして書き換えないでください。CRLF が壊れてファイル全体が差分になります。
- **Prefab インスタンスを触るときは、シーン側の上書きで終わらせるか Prefab へ戻すかを毎回決めます。**
- `[Header]` `[Tooltip]` は日本語で書きます。

## 触らないもの

- `Assets/Scripts/MiniGames/` 以下（別 PR で扱います）
- `Assets/Scenes/SampleScene.unity`、`Assets/Scenes/animationscene.unity`（本編では未使用）
- 頼んでいないリファクタリング。**動いているものの整理は不要です。**

---

# PR 1: ボタンのホバー・押下演出を統一する

## 現状（2026-08-06 に Unity 上で全数を数えた結果）

ボタンは全 35 個。`ButtonHoverScale` が付いているのは **7 個だけ**です。

| シーン | ボタン数 | 付いている | 備考 |
| --- | --- | --- | --- |
| `Title` | 10 | 2 | `StartButton` / `CreditsButton` |
| `DifficultySelect` | 7 | 2 | `YesButton` / `NoButton` |
| `Game` | 7 | 0 | |
| `Tutorial` | 8 | 0 | |
| `Clear` | 1 | 1 | |
| `GameOver` | 2 | 2 | |

## やること

### 1. `ButtonHoverScale` を拡張する

対象: [Assets/Scripts/Core/UI/ButtonHoverScale.cs](../../Assets/Scripts/Core/UI/ButtonHoverScale.cs)

現在はホバー時の拡大だけです。ここへ**押下演出**を足します。

- `IPointerDownHandler` / `IPointerUpHandler` を実装し、押している間だけ少し縮める。
- `pressScale` を `[SerializeField]` で持たせる。**既定値 0.95。**
- 指を離したら、カーソルがまだ乗っていればホバーの大きさへ、外れていれば元の大きさへ戻す。
  そのためにカーソルが乗っているかどうかを自前で覚えてください。
- **`Button.interactable` が false のときは動かさない。** 押せないボタンが反応すると押せるように見えます。

あわせて既定値を変えます。

- `hoverScale` の既定値を **1.5 → 1.05** にする。

  1.5 は `DifficultySelect` のカード（423×671）のような大きいボタンには効きすぎます。
  **倍率のままにするのは、`YesButton` / `NoButton` が `localScale 3` で置かれているためです。**
  加算量にすると、この 2 つだけ別の見え方になります。

- **既に付いている 7 個は `hoverScale 1.5` が保存済みです。** 既定値を変えても直りません。
  7 個すべてを 1.05 へ明示的に書き換えてください。

`OnDisable` で元の大きさへ戻す処理は既にあります。**消さないでください。**
パネルが閉じるときに拡大したまま固まるのを防いでいます。

### 2. 付いていないボタンに付けて回る

下の 24 個に `ButtonHoverScale` を付けます。パスは Unity 上で数えた実物です。

**Title（5 個）**

- `MainCanvas/ScreenRoot/OptionButton`
- `MainCanvas/ScreenRoot/CreditsModal/Panel/CloseButton`
- `MainCanvas/ScreenRoot/CreditsModal/Panel/OflLicenseButton`
- `MainCanvas/ScreenRoot/OflLicenseModal/Panel/BackToCreditsButton`
- `MainCanvas/ScreenRoot/OptionModal/OptionPanel/CloseOptionButton`

**DifficultySelect（5 個）**

- `MainCanvas/ScreenRoot/EasyButton`
- `MainCanvas/ScreenRoot/NormalButton`
- `MainCanvas/ScreenRoot/HardButton`
- `MainCanvas/ScreenRoot/EndlessButton`
- `MainCanvas/Button`

**Game（7 個）**

- `MainCanvas/Shared/Hud/PauseButton`
- `MainCanvas/Shared/DeviceTabs/PcTab`
- `MainCanvas/Shared/DeviceTabs/TabletTab`
- `MainCanvas/Shared/ModalLayer/PausePanel/ResumeButton`
- `MainCanvas/Shared/ModalLayer/PausePanel/BackToDifficultyButton`
- `MainCanvas/Shared/ModalLayer/PausePanel/OptionButton`
- `MainCanvas/Shared/ModalLayer/OptionPanel/CloseButton`

**Tutorial（7 個）** — Game と同じ 7 つのパス。

### 3. 付けてはいけないもの（4 個）

以下は**画面いっぱい（1920×1080）の透明ボタン**です。モーダルの外側を押して閉じるための
受け皿であり、ボタンの見た目を持ちません。**付けると画面全体が拡大します。**

- `Title` / `MainCanvas/ScreenRoot/CreditsModal`
- `Title` / `MainCanvas/ScreenRoot/OflLicenseModal`
- `Title` / `MainCanvas/ScreenRoot/OptionModal`
- `Tutorial` / `MainCanvas/Shared/ScreenAdvanceBotton`

**判断基準: 大きさが 1920×1080 のボタンには付けない。**

### 4. Prefab か シーンか（判断が要る箇所）

`DifficultySelect` の `YesButton` / `NoButton` は
[Assets/Prefabs/UI/UIPanel.prefab](../../Assets/Prefabs/UI/UIPanel.prefab) のインスタンスで、
**`ButtonHoverScale` はシーン側の上書きとして付いています。** Prefab 本体には付いていません。

`UIPanel.prefab` を使っているのが `DifficultySelect` だけであることを確認したうえで、
**Prefab 本体へ移してください。** 確認して他にも使われていた場合は、移さずシーンの上書きのまま
にして、その旨を報告してください。

## 受け入れ条件

- [ ] 上の 24 個に付いていて、4 個の全画面ボタンには付いていない
- [ ] `ButtonHoverScale` を持つ全インスタンスの `hoverScale` が 1.05
- [ ] `Button.interactable = false` のボタンがホバー・押下で動かない
- [ ] `unity command run_tests --mode EditMode` が全件通る
- [ ] コンソールにエラー・例外が 0 件

## 目視で見てほしいところ

- `DifficultySelect` の 4 枚のカードに乗せたとき、隣のカードと重ならないか
- `Game` のポーズを開き、`OptionButton` を押して閉じたあと、ボタンが拡大したまま残っていないか

---

# PR 2: オプション画面の中身を入れる

## 現状（計画の記述と違っています）

**「Pause からオプションを開く」は既に動いています。** 作業は残っていません。

`PauseMenuView` の `optionButton` / `optionPanel` / `optionCloseButton` は
`Game` と `Tutorial` の両方で配線済みです。開いて閉じるところまで動きます。

**空なのは中身です。** `ModalLayer/OptionPanel`（691×432）の中は次の 3 つだけです。

- `Title` = 「OPTIONS」
- `Note` = 「音量設定は音源と最終 UI の準備後に追加する。」
- `CloseButton`

そして `PauseMenuView` には **`bgmVolumeSlider` / `sfxVolumeSlider` の受け口が既にあり、
`InitializeVolumeSlider` も実装済みです。未設定なだけ**です。

いっぽう `Title.unity` の `OptionModal/OptionPanel`（760×460）には
BGM と SE のスライダーが揃っており、`TitleManager` が配線しています。

## 方針（原案から変えています）

原案は `Resources/OptionModal.prefab` + 常駐シングルトンでしたが、**シングルトンにはしません。**

`FadeOverlayView` と `PcLidView` が常駐なのは、**シーンの切り替えをまたいで生き残る必要がある**
ためです。オプション画面はシーンをまたぎません。常駐にすると、各シーンの Canvas の重なり順を
別途そろえる必要が出てきて、得るものより手間が増えます。

**代わりに、振る舞いを自分で持つ Prefab を作って各シーンに置きます。**

### 1. `OptionPanelView` を作る

置き場所: `Assets/Scripts/Core/UI/OptionPanelView.cs`

このコンポーネントが、パネル内部の配線を自分で持ちます。外から渡すものはありません。

- BGM スライダー → `AudioManager.BgmVolume`
- SE スライダー → `AudioManager.SfxVolume`（変更時に `AudioCue.UiConfirm` を鳴らして音量を確かめられるようにする。`TitleManager.HandleSfxChanged` が既にこの形です）
- チュートリアル確認のトグル → `GameSettings.ShowTutorialConfirm`
- 閉じるボタン → 自分を非表示にする

公開するのは `Show()` / `Hide()` の 2 つだけにしてください。

**保存の実装は不要です。** `AudioManager.BgmVolume` / `SfxVolume` も
`GameSettings.ShowTutorialConfirm` も、setter の中で `PlayerPrefs` へ書いています。

スライダーへ初期値を入れるときは `SetValueWithoutNotify` を使います。
`value` の代入だと `onValueChanged` が走り、起動しただけで SE が鳴ります。

### 2. `Assets/Prefabs/UI/OptionPanel.prefab` を作る

`Title.unity` の `MainCanvas/ScreenRoot/OptionModal/OptionPanel`（760×460）を土台にします。
**絵と配置が既に整っているのはこちらだけ**なので、Game 側の 691×432 に合わせないでください。

構成:

- 見出し「OPTIONS」
- BGM 行（ラベル + スライダー）
- SE 行（ラベル + スライダー）
- **チュートリアル確認の行（ラベル + Toggle）** ← 新規。既存 2 行と同じ間隔・同じ書式で足す
- 閉じるボタン

行が 1 つ増えるので、パネルの高さは伸ばして構いません。

ルート に `OptionPanelView` を付け、スライダー・トグル・閉じるボタンを割り当てます。

### 3. 3 つのシーンに置き換える

| シーン | 対象 | やること |
| --- | --- | --- |
| `Title` | `ScreenRoot/OptionModal/OptionPanel` | 中身を Prefab インスタンスに置き換える。全画面の `OptionModal`（背景を押して閉じる受け皿）と `TitleManager` の配線はそのまま残す |
| `Game` | `Shared/ModalLayer/OptionPanel` | 丸ごと Prefab インスタンスに置き換える。`PauseMenuView.optionPanel` の参照を差し替える |
| `Tutorial` | 同上 | 同上 |

**「音量設定は音源と最終 UI の準備後に追加する。」の `Note` は消してください。** 用が済みました。

### 4. 重複した配線を片付ける

`TitleManager` と `PauseMenuView` の両方に、スライダーを自前で配線するコードがあります。
`OptionPanelView` が持つようになるので、**それぞれの音量配線は削ってください。**

- `TitleManager`: `bgmSlider` / `sfxSlider` / `HandleSfxChanged` と、対応する `SceneUiValidation.Require` の項目
- `PauseMenuView`: `bgmVolumeSlider` / `sfxVolumeSlider` / `InitializeVolumeSlider`

開く・閉じる（`optionButton` / `optionPanel` / `optionCloseButton`、`ShowOption` / `HideOption`）は
**そのまま残します。** ここは今も正しく動いています。

### 5. `OptionView.cs` を消す

[Assets/Scripts/Core/UI/OptionView.cs](../../Assets/Scripts/Core/UI/OptionView.cs) は
**どのシーンにも Prefab にも付いていません。** 中身のチュートリアル確認トグルは
`OptionPanelView` が引き継ぐので、`.cs` と `.meta` を削除してください。

削除は `unity command delete_asset --asset <path> --confirm true` を使います。

## 受け入れ条件

- [ ] Title / Game / Tutorial の 3 つで、同じ見た目のオプション画面が開く
- [ ] BGM スライダーを動かすと BGM の音量がその場で変わる
- [ ] SE スライダーを動かすと音が鳴り、音量が変わる
- [ ] チュートリアル確認のトグルを切り替え、**ゲームを再起動しても状態が残っている**
- [ ] トグルを切った状態で `DifficultySelect` からイージー / ノーマルを選ぶと、確認ダイアログが出ずに始まる
- [ ] 起動しただけでは SE が鳴らない
- [ ] `OptionView.cs` と `.meta` が消えている
- [ ] `unity command run_tests --mode EditMode` が全件通る
- [ ] コンソールにエラー・例外が 0 件

## 目視で見てほしいところ

- Title と Game でパネルの見た目が同じか（**同じ Prefab なので、違ったら置き換え漏れです**）
- ポーズ中（`Time.timeScale = 0`）でもスライダーが動くか
- 3 行が縦に等間隔で並んでいるか

---

## 報告してほしいもの

作業が終わったら次の 3 つをください。会話のログは要りません。

1. `git diff --stat`
2. `run_tests` の通過数
3. コンソールのエラー・例外の件数

途中で指示と食い違う状態を見つけた場合は、**直す前に報告してください。**
この指示書は 2026-08-06 時点の実測に基づいており、その後の変更は反映されていません。

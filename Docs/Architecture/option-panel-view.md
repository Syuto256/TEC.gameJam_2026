# OptionPanelView（共通オプション画面）

最終更新: 2026-08-06  
実装: `Assets/Scripts/Core/UI/OptionPanelView.cs`  
Prefab: `Assets/Prefabs/UI/OptionPanel.prefab`

## 責務

Title / Game / Tutorial に同じ Prefab として置かれるオプション画面の内部配線を担当する。
BGM 音量、SE 音量、チュートリアル確認表示を保存設定へ反映し、初期値を UI へ戻す。
画面の表示位置・大きさ・色は Prefab が持ち、シーンごとの開閉判断は各シーンの入口または
`PauseMenuView` が持つ。

## 依存関係

| 種別 | 名前 | 利用理由 | 方向 |
| --- | --- | --- | --- |
| 依存先 | `AudioManager` | BGM / SE の音量を読み書きし、SE の確認音を鳴らす。 | このクラス → 依存先 |
| 依存先 | `GameSettings` | チュートリアル確認表示の設定を読み書きする。 | このクラス → 依存先 |
| 依存先 | uGUI | Slider / Toggle / Button の入力を受ける。 | このクラス → 依存先 |
| 利用元 | `TitleManager` / `PauseMenuView` | `Show()` / `Hide()` でパネルの表示状態を切り替える。 | 利用元 → このクラス |

## 公開契約

| API / Event | 入出力 | 呼び出し条件 | 保証すること |
| --- | --- | --- | --- |
| `Show()` | なし | パネルを表示するとき | GameObject を有効化し、`OnEnable` で保存済み設定を UI に反映する。 |
| `Hide()` | なし | パネルを閉じるとき | GameObject を無効化する。設定値は各 setter が保存済み。 |

## ライフサイクル / 状態

- `Awake` で各 UI イベントを一度だけ登録する。
- `OnEnable` では `SetValueWithoutNotify` / `SetIsOnWithoutNotify` を使い、初期表示だけを更新する。
- Slider / Toggle の操作時だけ設定 setter を呼ぶ。SE Slider の操作時は `AudioCue.UiConfirm` を鳴らす。
- 閉じるボタンは `Hide()` を呼ぶ。親側の開閉処理も同じパネルを表示・非表示にする。

## データと設定

| 項目 | 所有者 | 既定値 / 保存 |
| --- | --- | --- |
| BGM 音量 | `AudioManager.BgmVolume` | 0〜1、`PlayerPrefs` に保存 |
| SE 音量 | `AudioManager.SfxVolume` | 0〜1、`PlayerPrefs` に保存 |
| チュートリアル確認表示 | `GameSettings.ShowTutorialConfirm` | 既定 `true`、`PlayerPrefs` に保存 |

## 注意点と検証

- シングルトンにはせず、3 シーンへ同じ Prefab を配置する。
- 初期値設定に `value` を使うと SE が鳴るため、必ず `SetValueWithoutNotify` を使う。
- Prefab の構造や見た目を変えるときは Unity Pipeline 経由で保存する。
- 確認手順: Title / Game / Tutorial で開閉、Slider の即時反映、Toggle の再起動後保持、EditMode テスト、Console エラー 0 件を確認する。

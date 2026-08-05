# AudioManager（音の再生）

最終更新: 2026-08-04  
実装: `Assets/Scripts/Core/AudioManager.cs`, `AudioCatalog.cs`  
登録簿の実体: `Assets/Resources/AudioCatalog.asset`

## 考え方

**鳴らす側は「音の種類」を渡すだけ。実際のクリップと音量はアセットが持つ。**

```csharp
AudioManager.PlaySfx(AudioCue.TaskExpired);
```

クリップが未登録の種類は**無音になるだけ**で、エラーも例外も出ない。そのため、音源が 1 つも無い状態でもフックを先に置いておける。素材が届いたらカタログへ登録するだけで鳴り始め、**コードは一切変更しない。**

## 音源を追加する手順

1. 音声ファイルを `Assets/` 以下へ入れる。
2. `Assets/Resources/AudioCatalog.asset` に行を 1 つ足す。
3. 行の `cue` に鳴らしたい種類を選び、`clip` にファイルを割り当て、`volume` を決める。

以上。どこで鳴るかはコード側で既に決まっている。

## 音の種類と、鳴る場所

BGM はシーンが切り替わったときに自動で選ばれる。

| 種類 | 鳴るタイミング |
| --- | --- |
| `TitleBgm` | Title シーン |
| `DifficultySelectBgm` | DifficultySelect シーン |
| `GameBgm` | Game シーン |
| `ClearBgm` | Clear シーン |
| `GameOverBgm` | GameOver シーン |

SE は以下の場所から鳴る。

| 種類 | 鳴るタイミング | 呼ぶ場所 |
| --- | --- | --- |
| `UiConfirm` | ボタンを押したとき、SE 音量を確認するとき | `AppServices.PlayConfirm` / `PauseMenuView` / `OptionPanelView` |
| `UiCancel` | オプションを閉じたとき | `PauseMenuView` |
| `TaskSpawned` | タスクが出現したとき | `MainGameController` |
| `TaskExpired` | タスクが時間切れになったとき | `MainGameController` |
| `AiRequested` | AI に依頼したとき（右クリック） | `MainGameController` |
| `AiSucceeded` | AI が処理に成功したとき | `MainGameController` |
| `AiFailed` | AI が処理に失敗したとき | `MainGameController` |
| `MiniGameSuccess` | 自力ミニゲームに成功したとき | `MainGameController` |
| `MiniGameFailure` | 自力ミニゲームに失敗したとき | `MainGameController` |
| `MiniGameInputHit` | ミニゲーム中の一手が成功したとき | 各ミニゲーム |
| `MiniGameInputMiss` | ミニゲーム中の一手が失敗したとき | 各ミニゲーム |
| `PauseOpen` | ポーズしたとき | `MainGameController` |
| `PauseClose` | ポーズを解除したとき | `MainGameController` |
| `HpLow` | HP が危険域へ入った瞬間（一度だけ） | `MainGameController` |

`HpLow` のしきい値は `MainGameController` の `hpLowRatio`（既定 0.3）で調整する。危険域を出入りすると再び鳴る。

## ミニゲームから音を鳴らす

`MiniGameBase` に受け口がある。**`AudioManager` を直接呼ぶ必要はない。**

```csharp
PlayInputFeedback(true);    // 一手成功（QTE の 1 押し、連打の 1 回、タイピングの 1 文字）
PlayInputFeedback(false);   // 一手失敗
PlayCue(AudioCue.〇〇);      // ミニゲーム固有の音
```

クリア・失敗そのものの音は `MainGameController` がまとめて鳴らすため、**各ミニゲームが鳴らすのは途中経過だけでよい。**

固有の音を足したい場合は `AudioCue` に種類を追加する。**必ず末尾に足すこと。** アセットは選択肢を整数で保存しているため、途中に挿入すると既存の登録が黙って別の音を指すようになる。

## 音量

| API | 対象 |
| --- | --- |
| `AudioManager.BgmVolume` | BGM 全体（0〜1） |
| `AudioManager.SfxVolume` | SE 全体（0〜1） |

`PlayerPrefs` に保存されるため、次回起動時も維持される。カタログの `volume` はクリップごとの相対音量で、この全体音量と掛け合わされる。

オプション画面から操作するには、各シーンに置いた `OptionPanel.prefab` の `OptionPanelView` が BGM / SE の Slider を `AudioManager` へつなぐ。範囲、保存、現在値の反映も `OptionPanelView` が行うため、Title / Game / Tutorial で個別に音量配線を持たない。

## 動作の約束

- **クリップが未登録の BGM へ切り替わった場合、直前の BGM を鳴らし続ける。** シーンを移るたびに無音へ落ちるのを避けるためである。意図して無音にしたい場合は、無音のクリップを登録する。
- SE は `PlayOneShot` で重ねて鳴らす。同じ音が連続しても打ち消し合わない。
- `AudioManager` は `DontDestroyOnLoad` で 1 つだけ存在する。`AppServices.Ensure()` が用意するため、どのシーンから再生を始めても鳴る。

## 未登録の音を調べる

`AudioCatalog.GetMissingCueNames()` が、クリップ未割り当ての種類を返す。素材の抜けを確認するために使う。

**2026-08-04 時点では、プロジェクトに音声ファイルが 1 件も無く、全 19 種類が未登録である。** フックはすべて配置済みなので、素材が届き次第カタログを埋めれば鳴る。

## 今後の検討事項

- BGM のクロスフェード、AudioMixer への移行は未実装である。現状は即時切り替えのみ。
- シーン名から BGM を選ぶ対応はコード内の `switch` にある。シーンが増えるときはここへ 1 行足す。

# 2026-08-06 マージ後の作業整理

`suzuki/MinigameRefinement` に `main`（チュートリアル）を取り込んだ時点での棚卸し。
挙げてもらった項目を、**依存関係と PR の単位**で並べ直したもの。

---

## 0. 済 — main の取り込み

`49870d1 Merge branch 'main' into suzuki/MinigameRefinement`

衝突は 3 ファイル。いずれも「両側が別のものを足した」型で、片方を捨てる必要は無かった。

| ファイル | 中身 |
| --- | --- |
| `MainGameController.cs` | こちらの `activeMiniGame` 保持と、main のチュートリアル用「失敗を無視」を両立 |
| `DifficultySelectManager.cs` | こちらの「数値だけ表示」を採用（main の `BEST: {n}` は破棄） |
| `DifficultySelect.unity` | こちらのシーンを土台に、main が足した確認ダイアログ一式を入れ直し |

シーンは git が YAML のドキュメント境界を取り違えていたため、
**こちらのシーンへ main の追加分だけを差し込んで**組み立て直した。
HEAD との差分が main の差分（325 追加 / 5 削除）と一致することで裏を取っている。

検証: 参照切れ 0 / ダイアログ・両 View の参照すべて充填 / EditMode 67 件すべて通過 / コンソールにエラー無し。

---

## 1. 次の PR — 液タブのミニゲーム位置合わせ **だけ**

**この PR に他を混ぜない。** ミニゲーム全面改修が失敗したときに戻れる地点を作るのが目的なので、
範囲を広げるとその目的が消える。

やること:

1. 液タブ面の `MiniGameHost` を PC 面と同じ見え方に合わせる（実測で上に +103px はみ出している。`y -106.5` が候補）
2. 背景の液タブ画像も同じ量だけ動かす

判断が要る点が 1 つある。`MiniGameHost` は面ごとに 1 つではなく共有である。
面によって位置を変えるなら、**面ごとの位置を持たせる**か、**`MiniGameHost` を各
`DeviceWorkspaceView` の下へ移す**かのどちらか。後者のほうが素直だが影響範囲が広い。

→ この PR では前者（面ごとの位置）で最小に留め、後者は改修側でやるのが安全。

マージされたのを確認してから以降に進む。

---

## 2. 以降のまとまり

### A. ミニゲーム中の明るさとウィンドウ感

**挙げてもらった「後ろの暗い部分をなくす」と「他画面を暗くする機能を廃止」は、
見た目の面では同じ 1 つの部品を指している。**

`FocusLightingView`（`Assets/Scripts/Core/UI/FocusLightingView.cs`）が 2 枚持っている:

| 名前 | 役割 | 今の値 |
| --- | --- | --- |
| `dimmer` | 全画面の暗幕 | `dimAlpha 0.5` |
| `glow` | 窓のまわりの光（`Fill Center` を切った枠） | `glowAlpha 0.4` |

「ぼかした影っぽくしてウィンドウ感を出す」は `glow` がまさにその位置にいる層なので、
**`dimAlpha` を 0 にして `glow` を影として作り直す**のが最短。
どちらも Inspector の値なので、コードは触らずに済む。

素材待ち: 影の絵（`glow` の Sprite を影用に差し替える）。

### B. ミニゲーム中も AI にタスクを任せられるようにする

A と違い、こちらは**機能**。今は `GameManager.OnPlayerMiniGameActiveChanged`
（[GameManager.cs:96](../../Assets/Scripts/Core/GameManager.cs)）が 3 つを同時にやっている:

1. `SetSwitchEnabled(!active)` — デバイス切替を止める
2. `focusLightingView.SetFocused(active)` — 暗くする（→ A）
3. `workspace.SetInteractionEnabled(!active)` — **全面のタスク操作を殺す** ← これが AI 依頼を塞いでいる本体

3 を外すだけで右クリックの AI 依頼は通る。ただし懸念どおり**左クリックが素通りになる**。

`MainGameController.TryAssignPlayer` は今 `activePlayerTaskId` を見ていない。
`miniGameHostView.Spawn` は中身を捨てて入れ替えるため、
**2 つ目を開くと 1 つ目が黙って消える**（コールバックは残るので状態も壊れる）。

→ `TryAssignPlayer` の入口に `activePlayerTaskId >= 0` なら弾くガードを 1 つ足す。
`ForceAiOnlyMode` のガードのすぐ下が置き場所。

1 と 3 は独立に切れるので、**1（デバイス切替の禁止）は残すか外すかを決める必要がある。**
ミニゲームは面をまたがないので、残しておくほうが事故が少ない。

### C. ボタンのホバー／押下演出の統一

main がチュートリアルで `ButtonHoverScale`（`hoverScale 1.5` / `duration 0.1`）を入れており、
これが事実上の共通部品になりかけている。ただし今は**ダイアログの 2 ボタンにしか付いていない**。

方針の選択肢:

- **案 A: スクリプト 1 つを全ボタンに付ける** — `ButtonHoverScale` を拡張し、押下演出も持たせる。
  既存のボタンに付けて回るだけなので、シーンの構造を壊さない。
- **案 B: ボタンの Prefab を作って派生させる** — 見た目まで揃うが、
  今あるボタンは絵も大きさもばらばらなので、置き換え作業が大きい。

→ **案 A を推す。** 演出の統一が目的で、絵の統一までは求められていない。

`hoverScale 1.5` はカードのような大きいボタンには効きすぎる。倍率ではなく
**加算量**か、Inspector で個別に調整できる形にしておきたい。

### D. オプションをインゲームの Pause からも開く

今の音量パネルは `Title.unity` に直接組んである（`TitleManager` が配線）。
Pause からも出すなら、**シーンに属さない形へ移す**必要がある。

同時に片付くものが 2 つある:

- main が `Assets/Scripts/Core/UI/OptionView.cs` を足しているが、
  **どのシーンにも Prefab にも付いていない。** 中身は「イージー/ノーマルで
  チュートリアル確認を出すか」の Toggle 1 つ。行き場所はこのオプションパネル。
- `AudioManager.BgmVolume` / `SfxVolume` は既に PlayerPrefs へ保存されているので、
  保存まわりの新規実装は要らない。

→ `Resources/OptionModal.prefab` + `OptionModalView` にまとめ、
`FadeOverlayView` / `PcLidView` と同じ常駐シングルトン方式で Title と Pause の両方から開く。
中身は BGM / SE / チュートリアル確認の 3 つ。

C と同じく「共通部品を作る」話なので、**C の直後にやると土台を使い回せる。**

### E. ミニゲーム個別の変更

ここが「全面改修」にあたる部分。1 に挙げた PR が main に入ってから着手する。

| ミニゲーム | 変更 | 素材 |
| --- | --- | --- |
| 仕分け | カードとフォルダの概念へ。フォルダ群は上か左に全種固定。レベルごとに「整理するファイル数」と「同時出現種類数の上限」を決め、その中でランダム | **要**（下地と枠が何を指すか不明瞭なので、まず絵の意図をすり合わせたい） |
| タイピング | お題を大きく。上に小さく薄くひらがなの読み。入力済みと入力ヒントを統合 | 不要 |
| 連打 | 残り連打数をもっと大きく | 不要 |
| 制限時間バー | 左右の端が見づらいので作り直し | 未定 |

仕分けだけ**仕様の変更**（レベルごとの母数と種類数）を含むので、
`GameTuningSettings` / `DifficultyProfile` 側に設定が要る。他の 3 つは表示だけ。

→ **表示だけの 3 つを先にまとめて出し、仕分けを単独で扱う。** 仕分けが長引いても他が止まらない。

### F. フェードイン — 原因確定

**シーンの読み込みが 1 フレームで 0.36 秒かかり、そのあいだに明転が終わってしまっている。**

難易度選択 → ゲーム画面を再生中に毎フレーム計測した結果:

| 時刻 | 不透明度 | フレーム間隔 | シーン |
| --- | --- | --- | --- |
| 0.018 〜 0.243 | 0.00 → 1.00 | 2〜5 ms | DifficultySelect |
| 0.243 〜 0.318 | 1.00 | 3 ms | DifficultySelect |
| **0.318 〜 0.675** | **（フレームが飛ぶ）** | **357 ms** | → Game |
| 0.675 以降 | **0.00** | 2〜3 ms | Game |

暗転は 0.25 秒かけて正常に動いている。問題は次で、
`SceneManager.LoadScene` が同期のため**そのフレームだけが 357 ms 続く**。
DOTween は実時間で動くので、次に進んだ時点で
**待ち 0.05 秒 + 明転 0.30 秒 = 0.35 秒ぶんを 1 回でまとめて消化してしまう。**
結果、暗転 → 固まる → いきなり全部見えている、という見え方になる。

**特定の遷移の問題ではない。** 行き先の読み込みが 0.35 秒を超えれば同じことが起きるので、
「他でも効いていないかも」という見立てのとおり。Game が一番重いので目立っているだけ。

修正案:

- **案 A: 明転を「読み込みが終わってから」始める。** `PcLidView` が既にこの形
  （`action()` のあと `yield return null` をはさむ）なので、`FadeOverlayView` を
  同じコルーチン方式に書き換える。**推奨。**
- 案 B: `LoadSceneAsync` にして完了を待つ。正攻法だが、読み込み中に
  ローディング表示を出すなどの余地を作らないなら案 A と見た目は変わらない。

---

## 3. マージ中に見つかった宿題

### チュートリアルが止まる — 原因確定（**main 由来。マージのせいではない**）

**チュートリアル中にミニゲームを失敗すると、そこで完全に固まる。**

`MainGameController.TryAssignPlayer` に main が入れた握りつぶしがある:

```csharp
if (overrideTaskLifetimeSec > 0f && !success)
{
    Debug.Log("[Tutorial] 失敗判定を無視し、クリアまで継続します。");
    return;   // ← CompletePlayerMiniGame を呼ばずに抜ける
}
```

`Tutorial.unity` は `overrideTaskLifetimeSec: 99` なので、チュートリアル中は常にこの経路。

ところが `MiniGameBase.FinishGame` は**通知より先に `IsPlaying = false` にしている**:

```csharp
protected void FinishGame(bool success, string reason = "")
{
    if (!IsPlaying) return;   // 二重発火防止ガード
    IsPlaying = false;
    ...
    OnCompleted?.Invoke(success, reason);
}
```

したがって失敗した瞬間にこうなる:

1. `IsPlaying = false` になり、`Update` が素通りする → **タイマーも入力も死ぬ**
2. 通知が握りつぶされ、`CompletePlayerMiniGame` に届かない
3. → `TaskResolved` が飛ばない → `CloseMiniGame` が呼ばれない → **窓が閉じない**
4. `PlayerMiniGameActiveChanged(true)` が出たままなので、
   タスク操作もデバイス切替も止まったまま

コメントは「クリアまで継続します」だが、**その時点でミニゲームは既に終わっている。**
再挑戦はできない。抜け道が無いので詰む。

連打の難度 3 で出るのは、チュートリアルで**最初に失敗しうるミニゲーム**がそこだから。
それ以前は難度 1 なので、たいてい成功して通り過ぎてしまう。

切り分けの根拠:

- `Tutorial.unity` / `TutorialSequenceController.cs` / `TutorialConfirmDialog.cs` /
  `GameSettings.cs` は `origin/main` と**バイト単位で同一**
- 握りつぶしは `6036c3b チュートリアルの作成`（main）が入れたもので、こちらは原文のまま残した
- `FinishGame` の `IsPlaying = false` と二重発火ガードは**分岐点より前から**ある

→ つまり `main` 単独でも同じように止まる。

**修正案: 握りつぶしをやめる。**
`TutorialSequenceController.OnTaskResolved` は、`WaitMiniGameClear` /
`WaitAiProcess` などで**失敗したら出題し直す処理を既に持っている**（316〜388 行）。
握りつぶしが先に効くせいで、そのリトライ処理に到達できていない。
外せば設計どおりに動く。失敗ダメージだけを無効にしたいなら、
`GameSession` 側の damage を 0 にするのが筋。

### DifficultySelectView が DifficultySelectManager と完全に重複している

`Assets/Scripts/Core/UI/DifficultySelectView.cs` は
`DifficultySelectManager.Select` とほぼ同じ処理を持ち、**同じ GameObject に両方が乗って、
同じ EasyButton / NormalButton を両方が購読している。**
`GameFlowController.StartMainGame` は `SelectDifficulty` を呼ぶだけの別名なので、処理も同一。

今の実害:

- 確認ダイアログ ON（既定）: 両方が `Show` を呼び、後勝ちで上書き。見た目は正常だが `[Check]` ログが二重に出る
- 確認ダイアログ OFF: **`Transition(Game)` が 2 回走る**
- `DifficultySelectView` は Hard / Endless を持たないので、機能としては `Manager` の下位互換

→ **削除済み。** シーンからコンポーネントを外し、`Assets/Scripts/Core/UI/DifficultySelectView.cs`
も消した。`DifficultySelectManager` は全難易度と確認ダイアログの両方を持っているので、
機能は減っていない。残存参照 0（guid・名前の両方で検索）、参照切れ 0、EditMode 67 件通過。

### DifficultySelectManager が Canvas の下に入っている

main の変更でシーンのルートから `MainCanvas` の子へ移り、
`localScale 2.377` / `localPosition (-1071, -484)` が付いている。
ロジック専用の GameObject なので害は無いが、**ドラッグの事故に見える。**
マージでは main のとおりに再現してある。戻すなら別途。

### OptionView が迷子

上の D に記載。

---

## 4. 前から残っている宿題

- **ミニゲーム中の手のアニメーション**（デザイン D7）— 素材待ち
- **明るさの段差** — `RoomDimmer`（黒 α0.42）が `Game` にしか無いため、蓋を閉じる演出で一段明るくなる
- **`win11Modoki` の縦つぶれ** — PC 面で 10.4%（液タブは 1.1%）。急ぎではない
- **難易度ごとの調整が全 5 段階で同一**
- **`VeryHard` が選べない**
- **フォントアトラスの欠字** — 架 縁 示 捗 押 違 込 締 憩 怠 緊 絡 択 企 誤 了

# ミニゲームの追加・改造手順

最終更新: 2026-08-04  
実装: `Assets/Scripts/Core/MiniGameCatalog.cs`  
登録簿の実体: `Assets/Data/MiniGameCatalog.asset`

## 結論

**自分の Prefab とスクリプトだけを触れば、ミニゲームは 1 本追加できる。**  
`Game.unity` も `MainGameController` も他の人のミニゲームも変更しない。共有ファイルの変更は次の 2 つだけである。

| アセット | 決めること |
| --- | --- |
| `Assets/Data/MiniGameCatalog.asset` | そのミニゲームを**どう動かすか**（Prefab・表示名・制限時間） |
| `Assets/Data/TaskSpawnTable.asset` | そのタスクが**どの面に出るか**（PC / タブレット） |

## 登録簿が持つもの

| 項目 | 用途 |
| --- | --- |
| `kind` | 担当するタスク種別。カタログ内で重複させない。 |
| `displayName` | タスク吹き出しに出す名前。空ならタスク種別名を使う。 |
| `icon` | タスク吹き出しに出すアイコン。任意。 |
| `prefab` | `MiniGameHost` へ生成する Prefab。ルートに `MiniGameBase` 派生と `RectTransform` が必要。 |
| `timeLimitsByLevel` | タスクレベル 1〜4 の制限時間（秒）。要素が足りない分は最後の値を使う。 |

**ミニゲーム固有のデータ（問題集・経路データなど）はカタログに載せない。** 自分の Prefab が `[SerializeField]` で持つ。タイピングの `TypingQuestionDatabase` と、なぞりの `TracingPathDatabase` がその例である。

登録内容は開始時に `MainGameController` が一度だけ検証する。`prefab` 未設定、ルートに `RectTransform` が無い、`kind` の重複は、そこでエラーとして名前付きで報告される。

## 追加する手順

1. `MiniGameBase` を継承したクラスを `Assets/Scripts/MiniGameS/<自分の機能>/` に作る。
   - `Initialize(int difficulty, float timeLimit)` を `override` し、先頭で `base.Initialize` を呼ぶ。
   - 毎フレームの処理は `OnUpdate(float deltaTime)` に書く。
   - 成功・失敗は `FinishGame(success, reason)` で一度だけ通知する。時間切れは基底クラスが自動で通知する。
2. 自分のアセンブリ定義（`.asmdef`）の `references` に `Overwork.Core` を入れる。
3. `Assets/Prefabs/MiniGames/` に Prefab を作る。
   - ルートの `RectTransform` のアンカーを Stretch（`min = (0,0)` / `max = (1,1)`、offset すべて 0）にする。**`MiniGameHost` は生成物の大きさを上書きしない**ため、ここで広げないと小さいまま表示される。
   - 画面に出したいものは、この Prefab の子として実体で置く。コードで `new GameObject` しない。
   - 自分のスクリプトをルートに付け、表示先とデータを `[SerializeField]` でつなぐ。
4. `Assets/Data/MiniGameCatalog.asset` に行を 1 つ足し、`kind` と `prefab` と制限時間を設定する。
5. `Assets/Data/TaskSpawnTable.asset` の、出したい面の `kinds` へその種別を足す。

これで完了である。`MainGameController` は出現表から面と種別を決め、種別からカタログを引き、Prefab を `MiniGameHost` に生成して `Initialize` を呼ぶ。

## どの面に出すかを決める

`Assets/Data/TaskSpawnTable.asset` が、デバイス面ごとの出現タスクを持つ。

| 面 | 現在の出現タスク |
| --- | --- |
| `Pc`（PC） | タイピング、仕分け、連打 |
| `Pad`（タブレット） | なぞり |

- 面ごとに、登録した順で繰り返し出る。順番を変えたい場合は並べ替える。
- 同じ種別を複数の面に入れてよい。両方に出るようになる。
- `kinds` を空にすると、その面にはタスクが出ない。開始時に警告が出るだけで停止はしない。
- 面を増やす場合は行を 1 つ足す。`MainGameController` は面の数を知らないので、コードは変更しない。

**カタログに無い種別を書くと開始時にエラーで停止する。** 出現表とカタログの食い違いを実行前に見つけるためである。

生成先の面は「出現タスクが設定されていて、まだ上限（`maxTasksPerSurface`）に達していない面のうち、未解決タスクがいちばん少ない面」が選ばれる。

## 画面の共通の並び

4 本とも同じ骨格にしている。新しく作る場合もこれに合わせると、プレイヤーが迷わない。

```text
ルート（背景パネル）
├─ TimeGauge          上端の残り時間ゲージ
│  └─ Fill            Image Type = Filled（緑）
├─ …各ミニゲーム固有の表示…
├─ MissLabel          左下・ピンク「ミス: 0 / 2」
├─ StatusLabel        下中央・白（操作の案内。無い場合もある）
└─ TimeLabel          右下・水色「残り時間: 7.0 秒」
```

この並びは、Suzuki の試作（ミスは暖色、時間は寒色）と Motonaga の試作（上端の残り時間ゲージ）から取り込んだものである。

**`TimeGauge/Fill` と `TimeLabel` は `MiniGameBase` が更新する。** Prefab に置いて Inspector で割り当てるだけでよく、各ミニゲームのコードは何も書かない。置かなければ何も起きない。

## 見た目を変えたいとき

自分の Prefab を開いて、子の `RectTransform` と UI コンポーネントを編集する。コードは触らない。

各ミニゲームが Inspector に出している調整値は次のとおりである。文字列はすべて書式付きで外に出しているので、文言もコード変更なしで変えられる。

| ミニゲーム | Prefab | 主な調整項目 |
| --- | --- | --- |
| タイピング | `TypingMiniGame.prefab` | 問題集、許容ミス数、各行の文言（お題／ローマ字／入力済み／残り） |
| なぞり | `TracingMiniGame.prefab` | 経路データ、始点／終点の判定半径、許容ミス数、ガイド線の太さと色、案内の文言 |
| 連打 | `RapidClickMiniGame.prefab` | レベル 1 の必要クリック数、レベルごとの増加数、スペースキーを受けるか |
| 仕分け | `SortingMiniGame.prefab` | 箱とカードの配置・枚数、正解の対応（`categoryId`）、許容ミス数 |

## 表示できる範囲

`MiniGameHost` は PC / Tablet どちらの `DeviceFrame` にも収まる大きさにしてある（`Content` は 1167.8 × 711.5）。Prefab のルートを Stretch にしておけば、この枠いっぱいに広がり、端末の枠からはみ出さない。

`DeviceFrame` の大きさを変えた場合は、`Shared/MiniGameHost` の `RectTransform` も両方の枠の内側に収まるよう調整する。

## 生成と後片付けの契約

| 誰が | 何を |
| --- | --- |
| `MainGameController` | カタログを引き、`MiniGameHostView.Spawn` で生成し、`OnCompleted` を購読し、`Initialize` を呼ぶ |
| `MiniGameHostView` | 生成先の子を差し替える。大きさ・位置は決めない |
| ミニゲーム自身 | 自分の表示とルールだけを持つ。破棄は自分でしない |
| `MainGameController` | 結果が確定したら `MiniGameHostView.Hide()` を呼び、そこで生成物が破棄される |

ミニゲーム側が `Destroy(gameObject)` を呼ぶ必要はない。呼ぶと二重破棄になる。

## 例外: 実行時に作ってよいもの

なぞりミニゲームのガイド線だけは、経路データの点の数で本数が変わるため実行時に複製する。この場合も**見た目は Prefab で決める**。`GuideSegment` という複製元を Prefab 上に非アクティブで置き、コードはそれを複製して位置と長さと角度だけを設定する。太さや色を変えたい場合は複製元を編集する。

同じ理由で本数や個数が可変になるものを作る場合は、この形（Prefab に複製元を置き、コードは配置だけ決める）に合わせる。

## 関連資料

- [シーン構造](scene-structure.md)
- [MiniGameBase のライフサイクル](../Decisions/2026-08-03-mini-game-lifecycle.md)
- [共通 MiniGameHost の決定](../Decisions/2026-08-04-shared-mini-game-host.md)

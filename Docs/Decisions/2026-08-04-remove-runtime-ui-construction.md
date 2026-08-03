# 実行時 UI 生成の全廃と、シーンごと Manager への移行

日付: 2026-08-04  
状態: 実施済み

## 背景

`SceneUiBootstrap` という 1 つのクラスが、無関係な 3 つの仕事を兼ねていた。

1. 常駐サービス（`GameFlowController` / `AudioManager` / `EventSystem`）の用意
2. Title / DifficultySelect / Clear / GameOver の UI を、実行時に C# で組み立てる
3. Game シーンだけシーン名で分岐し、`GameSceneUiReferences` へ委譲する

さらに 4 つのミニゲームは、`UiBootstrap` という GameObject に Launcher コンポーネントを貼っておくと `GetComponents<MonoBehaviour>()` で拾われる、という Inspector からは読み取れない配線で登録されていた。ミニゲーム側の Prefab は GameObject 1 個の空の殻で、画面は各ミニゲームの `Initialize` がコードで組み立てていた。

この結果、「見た目を変えたいとき、Scene を見るのかコードを見るのか」の答えが対象ごとに逆になり、担当者が全体構造を把握できない状態になっていた。ジャム序盤に動かすことを優先した形がそのまま残っていたためであり、設計上の意図があってこうなっていたわけではない。

## 決定

新しい抽象を足すのではなく、旧来のやり方を消し切る方向で単純化する。

1. **`SceneUiBootstrap` を削除する。** 常駐サービスの用意は `AppServices.Ensure()` に移し、`EventSystem` は各シーンへ実体として置く。
2. **各シーンに `<シーン名>Manager` を 1 つ置く。** `TitleManager` / `DifficultySelectManager` / `GameManager` / `ResultManager` とし、シーンの入口を名前から引けるようにする。`Clear` と `GameOver` は同じ `ResultManager` を使い、違いは Inspector の参照だけで表す。
3. **`GameSceneUiReferences` を `GameManager` へ改名する。** 上の命名規則に合わせる。旧 `GameManager.cs`（どこからも参照されていない試験用クラス）は削除する。
4. **`IPlayerMiniGameLauncher` と 4 つの Launcher を削除する。** 4 クラスは Prefab を生成して `Initialize` を呼ぶだけのほぼ同一の定型文であり、`MiniGameCatalog` があれば不要になる。ミニゲーム固有のデータは Prefab 自身が `[SerializeField]` で持つ。
5. **`MiniGameCatalog` を唯一の登録点にする。** ミニゲームの追加は、自分の Prefab を作ってカタログに 1 行足すだけにする。
6. **すべての UI を Scene / Prefab の実体にする。** 4 シーンとミニゲーム 4 本の `BuildUi` を廃止する。

## 例外

なぞりミニゲームのガイド線だけは、経路データの点数で本数が変わるため実行時に複製する。見た目は Prefab 上の複製元 `GuideSegment` で調整できるようにし、コードは位置・長さ・角度だけを決める。

## 併せて削除したもの

| 対象 | 理由 |
| --- | --- |
| `Assets/Scripts/Core/GameManager.cs`（旧） | どこからも参照されていない試験用クラス。名前を新しい入口クラスへ譲る。 |
| `Assets/Scripts/MiniGameSample/` | `TestRunner` と、本編と同名の `RapidClickMiniGame` をグローバル名前空間で重複定義していたサンプル。同名クラスが 2 つ存在することが混乱の原因になっていた。 |
| `GameTuningSettings.miniGameTimes` | 制限時間の持ち主が `MiniGameCatalog` に一本化されたため。旧値はカタログへ引き継いだ。未使用だった `qte` / `timing` はこの機会に落とす。 |

## 影響

- ミニゲームを 1 本足すのに `Game.unity` を触る必要が無くなった。共有ファイルの変更はカタログの 1 行だけになり、複数人が並行して作業できる。
- 「見た目は Scene / Prefab、進行はコード」という 1 つの規則がプロジェクト全体で成り立つようになった。
- `MainGameController.Initialize` の引数が 6 個から 5 個になり、タスク種別ごとの制限時間を選ぶ分岐が消えた。

## 検証結果（2026-08-04）

- EditMode テスト 11 件すべて成功。
- Title → DifficultySelect → Game → GameOver → Retry → Game の一巡を Play モードで確認。Console エラー 0 件。
- ミニゲーム 4 本すべてが `MiniGameHost` の大きさへ広がって起動することを確認。なぞりのガイド線が経路の点数どおり生成されることも確認。
- ミニゲーム中にデバイス切替タブが両方とも非活性になり、終了後に復帰することを確認。

残る警告は、タイピングの日本語問題文に対して既定フォント（LiberationSans）が字形を持たないという既存の問題のみである。日本語表示に使う TMP フォントアセットの作成は別件とする。

## 関連資料

- [シーン構造](../Architecture/scene-structure.md)
- [ミニゲームの追加・改造手順](../Architecture/mini-game-catalog.md)
- [Game 画面の静的 UI 化・再構成計画](../Plans/2026-08-04-game-screen-restructure.md)

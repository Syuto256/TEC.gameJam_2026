# ミニゲームのライフサイクル方針

日付: 2026-08-03  
状態: 採用（起動経路のみ 2026-08-04 に更新。[実行時 UI 生成の全廃](2026-08-04-remove-runtime-ui-construction.md) を参照）

## 決定

`MiniGameHost` は Game Canvas 上に常駐させ、通常時は非表示にする。各ミニゲームは個別の Prefab として管理し、プレイヤーがタスクを開始した時だけ Host の子として生成する。`MiniGameBase.OnCompleted` を受け取ったら、そのミニゲームの結果を `TaskManager.CompletePlayer` に一度だけ渡し、生成した GameObject を破棄して Host を閉じる。

## 理由

- ミニゲーム担当ごとに Prefab、データ、起動アダプターを独立して制作・レビューできる。
- 非表示の常駐ミニゲームで起きやすい入力購読、途中状態、表示状態の持ち越しを避けられる。
- Core は起動契約だけを知り、個別ミニゲームのクラスへの参照を持たない。

## 2026-08-04 の更新

起動契約は `IPlayerMiniGameLauncher` インターフェースから `MiniGameCatalog`（ScriptableObject の登録簿）へ置き換えた。Launcher 4 クラスは Prefab を生成して `Initialize` を呼ぶだけの定型文であり、カタログがあれば不要だったためである。ライフサイクルそのもの（Host に生成し、`OnCompleted` を一度だけ受け、破棄して閉じる）は変えていない。破棄の担当は `MiniGameHostView.Hide()` に一本化した。

## 将来の扱い

- 生成・破棄コストが実測で問題になった場合のみ、同じ契約のまま内部実装をプールへ置き換える。

# TEC.gameJam_2026: AI エージェント作業ガイド

## プロジェクト概要

- Unity `6000.3.21f1` を使用する Unity プロジェクトです。
- Unity 操作は **Unity CLI の `unity command` を経由し、導入済みの Unity PipelinePackage (`com.unity.pipeline`) に接続して行います**。Unity YAML ファイルを直接書き換えてシーンやアセットを操作してはいけません。
- 企画書・仕様書は未整備です。仕様を推測して恒久的なゲーム挙動を実装せず、必要な判断は `Docs/GameDesign/` または `Docs/Specifications/` に TODO として残します。

## 先に読むこと: 過去に手戻りが起きた箇所

**いずれもコードを読んだだけでは気づけません。** 該当する作業に入る前に確認してください。

### 見た目・配置

- **大きさを合わせるのに `localScale` を使わない。** 当たり判定も一緒に伸びます。`RectTransform` の `sizeDelta` で合わせます。
- **`Assets/Sprites/` のデザイン画像は 1920×1080 のレイヤー書き出しです。** 切り出した素材ではないため、画像の寸法そのものに意味はありません。絵の位置は画像の中で既に決まっています。
- **触る前に Prefab か Scene インスタンスかを確かめる。** `Assets/Prefabs/UI/` の Prefab は複数のシーンから使われています。シーン側だけ直すと他のシーンに反映されず、後から Prefab へ移し替える作業が発生します。
- 座標・大きさ・色は Scene / Prefab が持ちます。コードが持つのは進行だけです。
- **配置は手置きの絶対座標（`anchoredPosition`）で統一しています。** `LayoutGroup` / `ContentSizeFitter` / `LayoutElement` はプロジェクト全体で 0 件です。混ぜると崩れるため、新しい UI も同じ方式で組みます。
- **非表示は用途で使い分けます。** 迷ったらこの表に寄せてください。

  | 目的 | 使うもの |
  | --- | --- |
  | 存在を消す（レイアウト計算からも外す） | `SetActive(false)` |
  | 透過で見せ消しする・フェードさせる | `CanvasGroup.alpha` |
  | 見た目は残して操作だけ止める | `.enabled` / `interactable` |

  既存は 3 通りが混在しています（`SetActive` 57 / `alpha` 13 / `enabled` 10）。**一括では直しません**が、触る場所はこの表に合わせます。
- `[Header]` `[Tooltip]` は日本語で書きます。

### Unity 操作

- **`eval_file` に渡す C# はメソッドの本体です。** `using` も `class` も書けません。型は `UnityEditor.SerializedObject` のように完全修飾で書き、`string` を `return` します。
- **値を変えたら `SerializedObject` → `ApplyModifiedPropertiesWithoutUndo()` → `EditorUtility.SetDirty()` → `SaveScene()` まで行います。** どれか欠けると保存されず、保存されなかったこと自体にも気づけません。変更後はシーンを開き直して読み直し、値が入っているか確かめます。
- **テストは `unity test` ではなく `unity command run_tests --mode EditMode` を使います。** `ProjectSettings/ProjectVersion.txt` は `6000.4.4f1` ですが、実際に開いている Editor は `6000.3.21f1` です。`unity test` は前者をインストール済み Editor から探すため、この環境では起動を拒否します。
- 手順とコマンド例は [Unity CLI / Pipeline 運用](Docs/Operations/unity-pipeline.md) を参照します。

### Git

- **`core.autocrlf=true` で、シーンとアセットは CRLF で保存されています。** スクリプトから書き換えると LF になり、中身が同じでもファイル全体が差分になります。Unity YAML は手で書き換えず、Pipeline 経由で操作します。

## 作業を始める前の確認（毎回必須）

1. `git status --short` で他メンバーの未コミット変更を確認する。関係のない変更は触らない。
2. `Docs/README.md` と、変更対象に対応する資料を読む。コードの責務・依存が変わる場合は資料も更新する。
   - **`Docs/Archive/` は読まない。** 終わった計画の保管庫であり、その後の決定で覆っている記述が混ざっています。判断に使うのは `Architecture/` `Specifications/` `Decisions/` の 3 つです。経緯を調べる目的でだけ開き、開いたら「いつの記述か」を必ず確認します。
3. `ProjectSettings/ProjectVersion.txt` と `Packages/manifest.json` を確認し、Unity バージョンと PipelinePackage の導入状況を確かめる。
4. **Unity CLI の有無を確認する。** Windows では `Get-Command unity -ErrorAction SilentlyContinue`、macOS/Linux では `command -v unity` を実行する。メンバーごとに CLI の導入状況は異なるため、確認を省略しない。
5. Unity 操作が必要な場合は、対象プロジェクトを Unity Editor で開き、`Library/Pipeline/.unity-pipeline-port` が存在することを確認してから `unity command --project-path <project-path>` で接続・コマンド一覧を取得する。

CLI が見つからない場合は Unity 操作を実行せず、未導入であることと必要な作業を報告する。PipelinePackage は CLI の代替ではない。

## Unity / Pipeline 運用

- 読み取りは最初に `unity command --project-path <project-path> editor_status` 等の非破壊コマンドで状態を確認する。
- 書き込み操作の前には対象・影響範囲を確認する。`dry_run` を提供するコマンドは先に `dry_run` を使う。
- 削除・上書き・プロジェクト設定変更は、Pipeline コマンドが求める `confirm=true` の意味と対象を確認してから実行する。
- スクリプト、Markdown、設定ファイルなど Unity Editor を介さないテキストは通常の編集手段で変更してよい。ただし `.meta` は対応する Asset と一対で扱い、不要に再生成・削除しない。
- 接続方法とトラブルシュートは `Docs/Operations/unity-pipeline.md` を参照する。

## チーム開発のルール

- `Assets/Scenes/` のシーンは競合しやすい。シーンを変更する担当・対象を共有し、他者のシーン変更を上書きしない。
- 個人の試作は `Assets/Personal/<member>/` に置き、共有化するものだけを適切な共通フォルダへ移す。
- 新しいゲーム機能は `Assets/Scripts/` に置き、既存の `Core` と `MiniGameSample` の責務をまたぐ依存を増やさない。
- 新しいクラス、公開 API、データ形式、主要な依存関係を追加・変更したら、`Docs/Architecture/` の概要とクラス資料を同じ変更で更新する。詳細ページは `Docs/Templates/class-detail.md` を複製して追加する。
- 仕様未確定の事項、設計上の選択、合意待ち事項は `Docs/Decisions/` に日付付きで記録する。

## 検証

- C# を変更したら Unity のコンパイルエラーがないことを確認する。
- 挙動を変更したら、対象シーンまたはテストで最小限の再現確認を行う。
- Unity CLI が使える環境では、可能なら Pipeline 経由で Editor の状態・Console・テストを確認する。CLI 未導入環境では、その制約を引き継ぎ事項に記載する。

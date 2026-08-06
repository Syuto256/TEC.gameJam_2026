# プロジェクト資料

このフォルダは、ゲーム制作チームと AI エージェントが同じ前提で作業するための一次資料です。コードと資料の食い違いを避けるため、構造・責務・主要な公開 API を変えるプルリクエストでは該当資料も更新します。

| 場所 | 内容 | 更新タイミング |
| --- | --- | --- |
| `Architecture/` | 実装済みの構造、依存関係、クラスの責務 | クラス・依存・データの変更時 |
| `GameDesign/` | 企画書、ゲーム体験、ルール | 企画が決まった時点から |
| `Specifications/` | 実装可能な仕様、画面・入力・データ定義 | 仕様が決まった時点から |
| `Plans/` | 合意済み仕様を実装へ分解した段階計画と確認ゲート | 実装開始前・計画変更時 |
| `Operations/` | 開発環境、Unity / Pipeline の運用手順 | ツールや手順の変更時 |
| `Decisions/` | 合意済みの設計判断と保留事項 | 判断・合意時 |
| `Retrospective/` | 開発後の振り返りと、次回に持っていく手順 | 制作が一段落した時点 |
| `Templates/` | 資料作成用テンプレート | テンプレートを改善した時 |

## 読む順番

1. [シーン構造](Architecture/scene-structure.md) — どこを触れば何が変わるか
2. [ミニゲームの追加・改造手順](Architecture/mini-game-catalog.md) — 自分の担当分に手を入れる人はここまでで足りる
3. [アーキテクチャ概要](Architecture/overview.md)
4. [クラスカタログ](Architecture/class-catalog.md)
5. [ゲーム企画概要](GameDesign/game-overview.md)
6. [コアゲームプレイ仕様](Specifications/gameplay-core.md)
7. [メインゲーム画面・接続仕様](Specifications/main-game-flow.md)
8. [Unity CLI / Pipeline 運用](Operations/unity-pipeline.md)
9. 対象機能に対応する企画・仕様・決定記録

## 記載の原則

- 実装済みの事実と、提案・未確定事項を混ぜない。未確定事項は `TODO` と決定日（または確認日）を付ける。
- 企画書・仕様書が追加されるまでは、アーキテクチャ資料を「現在の実装スナップショット」として扱う。
- 新しいクラスの詳しい説明が必要になったら、[クラス詳細テンプレート](Templates/class-detail.md) からページを作り、クラスカタログからリンクする。

# アーキテクチャ概要

最終確認: 2026-08-03  
対象: 現在 `Assets/Scripts/` にある実装

## 現在の構造

ゲーム全体の状態を `GameManager` が保持し、共通のミニゲーム制御を `MiniGameBase` が提供します。各ミニゲームは `MiniGameBase` を継承して成功・失敗をイベントで通知します。調整値は `GameTuningSettings` ScriptableObject に集約されています。

```mermaid
classDiagram
    class GameManager {
        -GameTuningSettings settings
        -MiniGameBase currentMiniGame
        -int currentHP
        -int currentScore
        +Start()
    }
    class GameTuningSettings {
        +gameDurationSec
        +maxHP
        +damage
        +ai
        +score
        +miniGameTimes
    }
    class MiniGameBase {
        <<abstract>>
        +OnCompleted(bool, string)
        +Initialize(int, float)
        #OnUpdate(float)*
        #FinishGame(bool, string)
    }
    class RapidClickMiniGame {
        -TMP_Text uiText
        -int requiredClicks
        -int currentClicks
        +OnClick()
    }
    class TestRunner {
        -RapidClickMiniGame miniGame
    }

    GameManager --> GameTuningSettings : 調整値を読む
    GameManager --> MiniGameBase : 初期化・完了を購読
    RapidClickMiniGame --|> MiniGameBase : 継承
    TestRunner --> RapidClickMiniGame : 単体試行
```

## 実行時フロー

```mermaid
sequenceDiagram
    participant GM as GameManager
    participant Settings as GameTuningSettings
    participant MG as MiniGameBase 実装

    GM->>Settings: maxHP・制限時間を取得
    GM->>MG: OnCompleted を購読
    GM->>MG: Initialize(難易度, 制限時間)
    loop IsPlaying
        MG->>MG: 時間を減算・OnUpdate を実行
    end
    MG-->>GM: OnCompleted(成功/失敗, 理由)
    GM->>GM: スコア加算 または HP 減少
    GM->>MG: OnCompleted を解除
```

## 依存方向と境界

| 層 / 役割 | 置き場所 | 依存してよいもの | 依存させないもの |
| --- | --- | --- | --- |
| 進行管理 | `Assets/Scripts/Core/` | 設定値、ミニゲーム共通契約 | 個別ミニゲームの実装詳細 |
| ミニゲーム共通契約 | `Assets/Scripts/Core/` | Unity 基盤 | `GameManager`、個別ゲーム |
| 個別ミニゲーム | `Assets/Scripts/<Feature>/` | `MiniGameBase`、必要な UI / Input | 他の個別ミニゲーム |
| 調整データ | `Assets/Data/` | ScriptableObject 定義 | シーン固有オブジェクト |
| 試験・試作 | `Assets/Personal/` または明示的なテスト用領域 | 対象機能 | 製品コードの恒久的な制御 |

## 拡張時の指針

1. ミニゲームを追加する場合は `MiniGameBase` を継承し、成功・失敗の通知は `FinishGame` に統一する。
2. 共有の調整値は `GameTuningSettings` へ追加する前に、全ミニゲームで共有する値か個別データかを判断する。個別の値は個別 ScriptableObject を検討する。
3. `GameManager` が個別ミニゲームの種類を直接判定する分岐は増やさない。選択・遷移が必要になったら専用の選択／進行コンポーネントを導入する。
4. 新しい責務または依存を足したら、この図、カタログ、必要に応じて詳細ページを更新する。

## 未確定事項

- TODO: 企画書・仕様書を受領後、ゲームループ、難易度、AI、スコアの正式な定義を確定する。
- TODO: ミニゲームの選択・連続遷移・終了条件を担うコンポーネントの要否を決める。

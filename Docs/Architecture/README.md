# アーキテクチャ資料

はじめて読む場合は上から順に読んでください。

- [シーン構造](scene-structure.md): どこを触れば何が変わるか。**最初に読む資料**
- [ミニゲームの追加・改造手順](mini-game-catalog.md): 自分のミニゲームを足す・直す手順
- [概要・依存関係](overview.md): 層の分かれ方と依存の向き
- [クラスカタログ](class-catalog.md): 現在の全クラスの責務と接点、調整場所の索引

## クラス別の詳細

- [シーン遷移と各シーンの入口](game-flow-controller.md)
- [GameManager（Game シーンの配線）](game-manager.md)
- [MainGameController と TaskBubbleView](main-game-controller.md)
- [DeviceScreenController](device-screen-controller.md)
- [TaskManager](task-manager.md)
- [AudioManager](audio-manager.md)
- [OptionPanelView](option-panel-view.md)

## ミニゲーム別の詳細

- [タイピング](typing-mini-game.md)
- [なぞり](tracing-mini-game.md)
- [連打](rapid-click-mini-game.md)
- [仕分け](drag-drop-mini-game.md)

クラス数が増えてカタログだけでは追いにくくなった時点で、`../Templates/class-detail.md` を使って機能別・クラス別の詳細ページを作成してください。概要ページには常に全体の依存関係だけを残し、実装詳細を詰め込みすぎない方針です。

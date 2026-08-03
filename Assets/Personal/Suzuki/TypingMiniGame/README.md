# TypingMiniGame 個人試作

このフォルダは Suzuki 用のタイピングミニゲームと、その単体デバッグ環境の実装を置く個人作業領域です。既存の本編スクリプトは変更しません。

## 実装済みのデバッグシーン

`Assets/Personal/Suzuki/Suzuki.unity` に `TypingDebugRoot` を配置済みです。

- `TypingWordDatabase.asset` は `Data/` にあり、Easy=`新聞 / しんぶん`、Normal=`学校 / がっこう`、Hard=`共有 / きょうゆう` を登録済みです。
- `TypingMiniGame`、`TypingMiniGameView`、`MiniGameDebugRunner` と、仕様で定めた7個の TextMeshPro 表示欄を参照設定済みです。
- `MiniGameDebugRunner` は Easy（難易度 `1`）・30秒で自動開始します。Inspector で難易度と時間を変更できます。
- 表示欄はデバッグ用の `Screen Space - Overlay` 2D Canvas 上の `TextMeshProUGUI` です。Canvas Scaler は基準解像度 1920×1080・画面サイズ追従です。日本語フォント追加後、7つの TextMeshProUGUI にフォントを割り当ててください。
- 入力は Input System の `Keyboard.onTextInput` を使用します。

再生して、`shinbun` と `sinbun` の両方、誤入力 2 回、時間切れを確認してください。

`MiniGameDebugRunner` は対象の `MiniGameBase` を差し替えられるため、後から作成する別ミニゲームにも流用できる。

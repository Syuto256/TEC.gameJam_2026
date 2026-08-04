# タイトル画面のクレジット表記

記録日: 2026-08-04  
状態: 実装済み（本文は編集待ち）

## 背景

タイトル画面から開けるクレジットモーダルを追加し、借用アセットの出典とライセンスをゲーム内で確認できるようにする。

## 実装方針（確定）

- `Title` シーンの `MainCanvas/ScreenRoot` 配下に `CreditsButton` と `CreditsModal` を置く。
- モーダルは `CanvasGroup` で非表示開始とし、クレジットボタンで開き、閉じるボタンと背景クリックで閉じる。
- `TitleManager` はこれらの Scene 上の参照を配線するだけとし、UI を実行時に生成しない。
- `CreditsModal` の `OFL 1.1` ボタンから、全文を縦スクロールで読める `OflLicenseModal` へ切り替える。背景クリックまたは `BACK TO CREDITS` でクレジットモーダルへ戻る。

## 表記内容

クレジット本文は `Title` シーンの `CreditsModal/Panel/CreditsText` にある TextMeshPro テキストを Inspector から編集する。現時点では編集位置を示すプレースホルダーだけを表示する。

`Assets/Sprites/ge-jamu2.png` はチーム制作素材のため、借用アセットのクレジット対象には含めない。`Assets/Fonts/HackGen-Bold.ttf` はテスト用途のため、削除予定とする。

借用アセットの出典 URL、作者名、ライセンス名、必要な表記文言は、確定後にこの本文へ入力する。

OFL 1.1 の本文は `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` の全文を `OflLicenseModal/Panel/ScrollArea/Viewport/Content/LicenseText` に転記する。

## TODO

- [ ] 確定したクレジット文言を `CreditsText` へ転記する。
- [ ] タイトル画面の実機解像度で、モーダルが閉じられ、本文を最後まで読めることを確認する。

using UnityEngine;
using TMPro; // TextMeshProを使う場合（通常のTextなら UnityEngine.UI）

public class RapidClickMiniGame : MiniGameBase
{
    [Header("【UI設定】")]
    [SerializeField] private TMP_Text uiText; // CanvasのTextを割り当てる

    private int requiredClicks = 10;
    private int currentClicks = 0;

    public override void Initialize(int difficulty, float timeLimit)
    {
        base.Initialize(difficulty, timeLimit); // 親クラスの初期化（IsPlaying = trueになる）
        currentClicks = 0;
        requiredClicks = 8 + (difficulty * 4);
        UpdateUI();
    }

    protected override void OnUpdate(float deltaTime)
    {
        // 毎フレーム残り時間などをUIに反映
        UpdateUI();

        // 画面のどこかをマウスクリックしてもカウントが進むようにしておく
        if (Input.GetMouseButtonDown(0))
        {
            OnClick();
        }
    }

    /// <summary>
    /// 連打カウントを進める関数（UI ButtonのOnClickにも登録可能）
    /// </summary>
    public void OnClick()
    {
        if (!IsPlaying) return;

        currentClicks++;
        UpdateUI();

        if (currentClicks >= requiredClicks)
        {
            FinishGame(true, "COMPLETE"); // 成功通知を飛ばす
        }
    }

    private void UpdateUI()
    {
        if (uiText != null)
        {
            uiText.text = $"連打!: {currentClicks} / {requiredClicks}\n残り時間: {TimeRemaining:F1}秒";
        }
    }
}
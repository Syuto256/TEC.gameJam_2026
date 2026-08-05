using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialSequenceController : MonoBehaviour
{
    public enum TutorialStep
    {
        Title,                    // 1. 「チュートリアル」
        Intro1,                   // 2. 「一人の時間です。」
        Intro2,                   // 3. 「やるべきことをこなしましょう。」
        SpawnTaskNotice,          // 4. 「さっそくタスクが出てきました。」
        PromptTaskClick,          // 5. 「左クリックで始めましょう。」
        WaitMiniGameClear,        // 6. ミニゲームクリア待ち
        MiniGameCleared,          // 7. 「できましたね。」
        ExplainScore,             // 8. 「タスクを終えるとスコアが増えます。」
        
        NoticeTaskExpire,         // 9. 「タスクは時間経過で消滅します。」
        PromptTaskExpireWait,     // 10. ★追加: 「放っておいてみましょう。」(3秒タスク生成)
        WaitTaskExpired,          // 11. タスク消滅（Expired）待ち
        ExplainDamage1,           // 12. 「タスクが消えてダメージを受けましたね。」
        ExplainDamage2,           // 13. 「タスクに対応できなかった場合、HPが減ります。」
        ExplainGameOver,          // 14. 「ゼロになるとゲームオーバーなのでご注意を。」
        SpawnHardTaskNotice,      // 15. 「おっと。」
        ExplainHardTask,          // 16. 「星の数が多いムズいタスクです。」
        ExplainAiFeature,         // 17. 「ムリ？そんな時は私にお任せを。; )」
        PromptAiRightClick,       // 18. 「タスクを右クリックしてください。」
        WaitAiProcess,            // 19. AI処理待ち
        ExplainAiCleared,         // 20. 「無事に対応できましたね。」

        ExplainAiFailurePossibility, // 21. 「たまに失敗するかもしれません。ゴメンネ。XP」
        ExplainOtherTaskIntro,       // 22. 「別のタスクもやってみましょう。」
        PromptTabletSwitch,          // 23. 「『タブレット』を押してみましょう。」
        WaitTabletSwitch,            // 24. タブレットボタン押下待ち
        ExplainTabletWelcome,        // 25. 「こんばんは。」
        ExplainAiOnTablet,           // 26. 「こちらにも私はいるのでご安心を。」
        SpawnTabletTaskNotice,       // 27. 「新しいタスクをやってみましょう。」
        WaitTabletTaskClear,         // 28. タブレットタスクのクリア待ち
        ExplainMultiDeviceQueue,     // 29. 「『PC』と『タブレット』のタスクは別で貯まります。」
        ExplainCheckPeriodically,    // 30. 「定期的に確認はしましょう。」
        ExplainFinalGoal,            // 31. 「これを制限時間までやってもらいます。」

        ExplainTimer,                // 32. 「左上が制限時間です。」
        ExplainTimeUp,               // 33. 「時間ですね。おやすみなさい。」
        Finished                     // 34. チュートリアル終了・画面遷移
    }

    [Header("【接続コンポーネント】")]
    [SerializeField] private MainGameController mainGameController;
    [SerializeField] private HudView hudView;

    [Header("【チュートリアルUI】")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button screenAdvanceButton;

    [Header("【ハイライト・誘導演出部品】")]
    [SerializeField] private GameObject focusMaskPanel;
    [SerializeField] private RectTransform arrowPointer;
    [SerializeField] private Vector2 arrowOffset = new Vector2(0f, 80f);

    [Header("【デバイス切り替えボタン（誘導用）】")]
    [SerializeField] private Button tabletSwitchButton;

    private TutorialStep currentStep = TutorialStep.Title;
    private GameObject currentHighlightedObject;
    private Canvas currentAddedCanvas;
    private Tween arrowTween;

    private void Start()
    {
        if (mainGameController == null)
        {
            Debug.LogError("[Tutorial] MainGameController が設定されていません。");
            return;
        }

        if (screenAdvanceButton != null)
        {
            screenAdvanceButton.onClick.AddListener(OnScreenClicked);
        }

        if (focusMaskPanel != null)
        {
            var maskButton = focusMaskPanel.GetComponent<Button>();
            if (maskButton == null)
            {
                maskButton = focusMaskPanel.AddComponent<Button>();
                maskButton.transition = Selectable.Transition.None;
            }
            maskButton.onClick.AddListener(OnScreenClicked);
        }

        if (tabletSwitchButton != null)
        {
            tabletSwitchButton.onClick.AddListener(OnTabletButtonClicked);
        }

        mainGameController.TaskResolved += OnTaskResolved;
        mainGameController.PlayerMiniGameActiveChanged += OnPlayerMiniGameChanged;

        if (hudView != null)
        {
            hudView.SetVisible(false);
        }

        ClearHighlight();
        SetStep(TutorialStep.Title);
    }

    private void OnDestroy()
    {
        if (mainGameController != null)
        {
            mainGameController.TaskResolved -= OnTaskResolved;
            mainGameController.PlayerMiniGameActiveChanged -= OnPlayerMiniGameChanged;
        }
        arrowTween?.Kill();
    }

    public void SetStep(TutorialStep nextStep)
    {
        currentStep = nextStep;
        ClearHighlight();
        Debug.Log($"[Tutorial Step] -> {currentStep}");

        switch (currentStep)
        {
            case TutorialStep.Title: ShowInstruction("チュートリアル", canClickAdvance: true); break;
            case TutorialStep.Intro1: ShowInstruction("一人の時間です。", canClickAdvance: true); break;
            case TutorialStep.Intro2: ShowInstruction("やるべきことをこなしましょう。", canClickAdvance: true); break;

            case TutorialStep.SpawnTaskNotice:
                ShowInstruction("さっそくタスクが出てきました。", canClickAdvance: true);
                mainGameController.SpawnCustomTaskOnSurface(TaskSurface.Pc, level: 1, customLifetimeSec: 99f);
                break;

            case TutorialStep.PromptTaskClick:
                ShowInstruction("左クリックで始めましょう。", canClickAdvance: false);
                DOVirtual.DelayedCall(0.1f, HighlightFirstTaskBubble).SetLink(gameObject);
                SetStep(TutorialStep.WaitMiniGameClear);
                break;

            case TutorialStep.WaitMiniGameClear: ShowInstruction("", canClickAdvance: false); break;

            case TutorialStep.MiniGameCleared:
                if (hudView != null) hudView.SetVisible(true);
                ShowInstruction("できましたね。", canClickAdvance: true);
                break;

            case TutorialStep.ExplainScore:
                ShowInstruction("タスクを終えるとスコアが増えます。", canClickAdvance: true);
                if (hudView != null && hudView.ScoreTextObject != null)
                {
                    HighlightObject(hudView.ScoreTextObject);
                    hudView.ScoreTextObject.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0f), 0.5f, 5, 1f);
                }
                break;

            // ★ 修正: まず説明文だけ見せてクリック進行を可能にする
            case TutorialStep.NoticeTaskExpire:
                ShowInstruction("タスクは時間経過で消滅します。", canClickAdvance: true);
                break;

            // ★ 追加: クリック後に「放っておいてみましょう。」を表示して3秒タスクを出す
            case TutorialStep.PromptTaskExpireWait:
                ShowInstruction("放っておいてみましょう。", canClickAdvance: false);
                mainGameController.SpawnCustomTaskOnSurface(TaskSurface.Pc, level: 1, customLifetimeSec: 3f);
                DOVirtual.DelayedCall(0.1f, HighlightFirstTaskBubble).SetLink(gameObject);
                SetStep(TutorialStep.WaitTaskExpired);
                break;

            case TutorialStep.WaitTaskExpired: break;

            case TutorialStep.ExplainDamage1:
                ShowInstruction("タスクが消えてダメージを受けましたね。", canClickAdvance: true);
                if (hudView != null && hudView.HpBarObject != null)
                {
                    HighlightObject(hudView.HpBarObject);
                    hudView.HpBarObject.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.5f, 5, 1f);
                }
                break;

            case TutorialStep.ExplainDamage2:
                ShowInstruction("タスクに対応できなかった場合、HPが減ります。", canClickAdvance: true);
                if (hudView != null && hudView.HpBarObject != null) HighlightObject(hudView.HpBarObject);
                break;

            case TutorialStep.ExplainGameOver: ShowInstruction("ゼロになるとゲームオーバーなのでご注意を。", canClickAdvance: true); break;

            case TutorialStep.SpawnHardTaskNotice:
                ShowInstruction("おっと。", canClickAdvance: true);
                mainGameController.SpawnCustomTaskOnSurface(TaskSurface.Pc, level: 3, customLifetimeSec: 99f);
                break;

            case TutorialStep.ExplainHardTask:
                ShowInstruction("星の数が多いムズいタスクです。", canClickAdvance: true);
                DOVirtual.DelayedCall(0.1f, HighlightFirstTaskBubble).SetLink(gameObject);
                break;

            case TutorialStep.ExplainAiFeature: ShowInstruction("ムリ？そんな時は私にお任せを。; )", canClickAdvance: true); break;

            case TutorialStep.PromptAiRightClick:
                ShowInstruction("タスクを右クリックしてください。", canClickAdvance: false);
                DOVirtual.DelayedCall(0.1f, HighlightFirstTaskBubble).SetLink(gameObject);
                SetStep(TutorialStep.WaitAiProcess);
                break;

            case TutorialStep.WaitAiProcess: break;

            case TutorialStep.ExplainAiCleared: ShowInstruction("無事に対応できましたね。", canClickAdvance: true); break;

            case TutorialStep.ExplainAiFailurePossibility:
                ShowInstruction("たまに失敗するかもしれません。\nゴメンネ。XP", canClickAdvance: true);
                break;

            case TutorialStep.ExplainOtherTaskIntro:
                ShowInstruction("別のタスクもやってみましょう。", canClickAdvance: true);
                break;

            case TutorialStep.PromptTabletSwitch:
                ShowInstruction("『タブレット』を押してみましょう。", canClickAdvance: false);
                if (tabletSwitchButton != null) HighlightObject(tabletSwitchButton.gameObject);
                SetStep(TutorialStep.WaitTabletSwitch);
                break;

            case TutorialStep.WaitTabletSwitch: break;

            case TutorialStep.ExplainTabletWelcome: ShowInstruction("こんばんは。", canClickAdvance: true); break;
            case TutorialStep.ExplainAiOnTablet: ShowInstruction("こちらにも私はいるのでご安心を。", canClickAdvance: true); break;

            case TutorialStep.SpawnTabletTaskNotice:
                ShowInstruction("新しいタスクをやってみましょう。", canClickAdvance: false);
                mainGameController.SpawnCustomTaskOnSurface(TaskSurface.Pad, level: 1, customLifetimeSec: 99f);
                DOVirtual.DelayedCall(0.1f, HighlightFirstTaskBubble).SetLink(gameObject);
                SetStep(TutorialStep.WaitTabletTaskClear);
                break;

            case TutorialStep.WaitTabletTaskClear: break;

            case TutorialStep.ExplainMultiDeviceQueue: ShowInstruction("『PC』と『タブレット』のタスクは別で貯まります。", canClickAdvance: true); break;
            case TutorialStep.ExplainCheckPeriodically: ShowInstruction("定期的に確認はしましょう。", canClickAdvance: true); break;
            case TutorialStep.ExplainFinalGoal: ShowInstruction("これを制限時間までやってもらいます。", canClickAdvance: true); break;

            case TutorialStep.ExplainTimer:
                ShowInstruction("左上が制限時間です。", canClickAdvance: true);
                if (hudView != null && hudView.TimeTextObject != null)
                {
                    HighlightObject(hudView.TimeTextObject);
                    hudView.TimeTextObject.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0f), 0.5f, 5, 1f);
                }
                break;

            case TutorialStep.ExplainTimeUp:
                if (focusMaskPanel != null) focusMaskPanel.SetActive(true);
                ShowInstruction("時間ですね。\nおやすみなさい。", canClickAdvance: true);
                break;

            case TutorialStep.Finished:
                GameFlowController.EnsureInstance().OpenDifficultySelect();
                break;
        }
    }

    private void OnScreenClicked()
    {
        Debug.Log($"[Tutorial] 画面クリック検知! 現在のステップ: {currentStep}");
        switch (currentStep)
        {
            case TutorialStep.Title: SetStep(TutorialStep.Intro1); break;
            case TutorialStep.Intro1: SetStep(TutorialStep.Intro2); break;
            case TutorialStep.Intro2: SetStep(TutorialStep.SpawnTaskNotice); break;
            case TutorialStep.SpawnTaskNotice: SetStep(TutorialStep.PromptTaskClick); break;
            case TutorialStep.MiniGameCleared: SetStep(TutorialStep.ExplainScore); break;
            case TutorialStep.ExplainScore: SetStep(TutorialStep.NoticeTaskExpire); break;
            
            // ★ 修正: クリックしたら「放っておいてみましょう。」へ遷移してタスク出現＆待機開始
            case TutorialStep.NoticeTaskExpire: SetStep(TutorialStep.PromptTaskExpireWait); break;
            
            case TutorialStep.ExplainDamage1: SetStep(TutorialStep.ExplainDamage2); break;
            case TutorialStep.ExplainDamage2: SetStep(TutorialStep.ExplainGameOver); break;
            case TutorialStep.ExplainGameOver: SetStep(TutorialStep.SpawnHardTaskNotice); break;
            
            case TutorialStep.SpawnHardTaskNotice: SetStep(TutorialStep.ExplainHardTask); break;
            case TutorialStep.ExplainHardTask: SetStep(TutorialStep.ExplainAiFeature); break;
            case TutorialStep.ExplainAiFeature: SetStep(TutorialStep.PromptAiRightClick); break;
            
            case TutorialStep.ExplainAiCleared: SetStep(TutorialStep.ExplainAiFailurePossibility); break;
            case TutorialStep.ExplainAiFailurePossibility: SetStep(TutorialStep.ExplainOtherTaskIntro); break;
            case TutorialStep.ExplainOtherTaskIntro: SetStep(TutorialStep.PromptTabletSwitch); break;

            case TutorialStep.ExplainTabletWelcome: SetStep(TutorialStep.ExplainAiOnTablet); break;
            case TutorialStep.ExplainAiOnTablet: SetStep(TutorialStep.SpawnTabletTaskNotice); break;

            case TutorialStep.ExplainMultiDeviceQueue: SetStep(TutorialStep.ExplainCheckPeriodically); break;
            case TutorialStep.ExplainCheckPeriodically: SetStep(TutorialStep.ExplainFinalGoal); break;
            case TutorialStep.ExplainFinalGoal: SetStep(TutorialStep.ExplainTimer); break;
            
            case TutorialStep.ExplainTimer: SetStep(TutorialStep.ExplainTimeUp); break;
            case TutorialStep.ExplainTimeUp: SetStep(TutorialStep.Finished); break;

            case TutorialStep.Finished:
                GameFlowController.EnsureInstance().OpenDifficultySelect();
                break;
        }
    }

    private void OnTabletButtonClicked()
    {
        if (currentStep == TutorialStep.WaitTabletSwitch)
        {
            SetStep(TutorialStep.ExplainTabletWelcome);
        }
    }

    private void OnTaskResolved(TaskResolutionResult result)
    {
        Debug.Log($"[Tutorial] タスク解決検知: {result.Resolution} (現在のステップ: {currentStep})");

        // 1. 最初の手動クリア指示ステップ
        if (currentStep == TutorialStep.WaitMiniGameClear)
        {
            if (result.Resolution == TaskResolution.PlayerSuccess)
            {
                SetStep(TutorialStep.MiniGameCleared);
            }
            else
            {
                ShowInstruction("左クリックで自分でやってみましょう！", canClickAdvance: false);
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    mainGameController.SpawnCustomTaskOnSurface(TaskSurface.Pc, level: 1, customLifetimeSec: 99f);
                    DOVirtual.DelayedCall(0.1f, HighlightFirstTaskBubble).SetLink(gameObject);
                }).SetLink(gameObject);
            }
        }
        // 2. 放置ダメージ体験ステップ
        else if (currentStep == TutorialStep.WaitTaskExpired)
        {
            if (result.Resolution == TaskResolution.Expired)
            {
                SetStep(TutorialStep.ExplainDamage1);
            }
            else
            {
                ShowInstruction("今回は触らずに放っておいてみましょう！", canClickAdvance: false);
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    mainGameController.SpawnCustomTaskOnSurface(TaskSurface.Pc, level: 1, customLifetimeSec: 3f);
                    DOVirtual.DelayedCall(0.1f, HighlightFirstTaskBubble).SetLink(gameObject);
                }).SetLink(gameObject);
            }
        }
        // 3. AI依頼ステップ
        else if (currentStep == TutorialStep.WaitAiProcess)
        {
            if (result.Resolution == TaskResolution.AiSuccess)
            {
                SetStep(TutorialStep.ExplainAiCleared);
            }
            else
            {
                ShowInstruction("右クリックでAIに任せてみましょう！", canClickAdvance: false);
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    mainGameController.SpawnCustomTaskOnSurface(TaskSurface.Pc, level: 3, customLifetimeSec: 99f);
                    DOVirtual.DelayedCall(0.1f, HighlightFirstTaskBubble).SetLink(gameObject);
                }).SetLink(gameObject);
            }
        }
        // 4. タブレットタスクステップ
        else if (currentStep == TutorialStep.WaitTabletTaskClear)
        {
            if (result.Resolution == TaskResolution.PlayerSuccess || result.Resolution == TaskResolution.AiSuccess)
            {
                SetStep(TutorialStep.ExplainMultiDeviceQueue);
            }
            else
            {
                ShowInstruction("もう一度タブレットのタスクをやってみましょう。", canClickAdvance: false);
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    mainGameController.SpawnCustomTaskOnSurface(TaskSurface.Pad, level: 1, customLifetimeSec: 99f);
                    DOVirtual.DelayedCall(0.1f, HighlightFirstTaskBubble).SetLink(gameObject);
                }).SetLink(gameObject);
            }
        }
    }

    private void OnPlayerMiniGameChanged(bool isActive)
    {
        if (isActive)
        {
            ClearHighlight();
        }
    }

    private void ShowInstruction(string message, bool canClickAdvance)
    {
        if (instructionText != null)
        {
            instructionText.text = message;
            instructionText.gameObject.SetActive(!string.IsNullOrEmpty(message));

            var textCanvas = instructionText.GetComponent<Canvas>();
            if (textCanvas == null)
            {
                textCanvas = instructionText.gameObject.AddComponent<Canvas>();
            }
            textCanvas.overrideSorting = true;
            textCanvas.sortingOrder = 101;
        }

        if (screenAdvanceButton != null)
        {
            screenAdvanceButton.gameObject.SetActive(canClickAdvance);
            if (canClickAdvance) screenAdvanceButton.transform.SetAsLastSibling();
        }
    }

    public void HighlightObject(GameObject targetObj)
    {
        ClearHighlight();
        if (targetObj == null) return;

        currentHighlightedObject = targetObj;
        if (focusMaskPanel != null) focusMaskPanel.SetActive(true);

        currentAddedCanvas = targetObj.AddComponent<Canvas>();
        currentAddedCanvas.overrideSorting = true;
        currentAddedCanvas.sortingOrder = 100;

        targetObj.AddComponent<GraphicRaycaster>();

        if (arrowPointer != null)
        {
            arrowPointer.gameObject.SetActive(true);
            var targetRect = targetObj.GetComponent<RectTransform>();
            arrowPointer.position = targetRect.position + (Vector3)arrowOffset;

            arrowTween?.Kill();
            arrowTween = arrowPointer.DOAnchorPosY(arrowPointer.anchoredPosition.y + 15f, 0.4f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    public void ClearHighlight()
    {
        if (focusMaskPanel != null) focusMaskPanel.SetActive(false);
        if (arrowPointer != null) arrowPointer.gameObject.SetActive(false);

        arrowTween?.Kill();

        if (currentHighlightedObject != null)
        {
            var raycaster = currentHighlightedObject.GetComponent<GraphicRaycaster>();
            if (raycaster != null) Destroy(raycaster);

            if (currentAddedCanvas != null) Destroy(currentAddedCanvas);
        }

        currentHighlightedObject = null;
        currentAddedCanvas = null;
    }

    private void HighlightFirstTaskBubble()
    {
        var bubble = FindObjectOfType<TaskBubbleView>();
        if (bubble != null) HighlightObject(bubble.gameObject);
    }
}
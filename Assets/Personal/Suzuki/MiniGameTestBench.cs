using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>ミニゲーム 1 本を、本編と同じ経路で単体起動して試すための台。</summary>
/// <remarks>
/// <para>
/// 本編は「出現表で種別を決める → カタログから Prefab を引く → Host に生成して <c>Initialize</c>」という順で動く。
/// この台は前半だけを手動の指定に置き換え、後半は本編とまったく同じことをする。
/// そのため、ここで動けば本編でも動くし、ここで壊れていれば本編でも壊れている。
/// </para>
/// <para>
/// 生成先の大きさは本編の <c>MiniGameHost/Content</c> と同じにしてある。
/// 見た目の崩れをここで確認できるようにするためなので、シーンの大きさは変えないこと。
/// </para>
/// <para>
/// 使い方は <c>Assets/Personal/Suzuki/MiniGameTestBench.unity</c> を開いて再生するだけである。
/// 種別・レベル・制限時間は画面上のボタンか、この Inspector から変えられる。
/// </para>
/// </remarks>
public sealed class MiniGameTestBench : MonoBehaviour
{
    [Header("【使用データ】")]
    [Tooltip("種別から Prefab と制限時間を引く登録簿。本編と同じ Assets/Data/MiniGameCatalog.asset を指す。")]
    [SerializeField] private MiniGameCatalog catalog;

    [Tooltip("カタログを使わず、この Prefab を直接試す。\n" +
             "まだカタログに登録していないミニゲームを試すときに使う。設定するとカタログより優先される。")]
    [SerializeField] private MiniGameBase directPrefab;

    [Header("【試す条件】")]
    [Tooltip("試すミニゲームの種別。カタログに登録されているものだけが対象になる。")]
    [SerializeField] private TaskKind kind = TaskKind.Typing;

    [Tooltip("問題レベル。本編ではタスクの難度に応じて 1〜4 が渡される。")]
    [Range(1, 4)] [SerializeField] private int level = 1;

    [Tooltip("制限時間をカタログの値ではなく、下の秒数で上書きする。\n" +
             "時間切れを待たずに操作感だけ見たいときは、長めにして使う。")]
    [SerializeField] private bool overrideTimeLimit;

    [Min(0.5f)] [SerializeField] private float timeLimitOverride = 30f;

    [Tooltip("終了してからこの秒数で自動的にもう一度始める。0 なら手動で「もう一度」を押す。")]
    [Min(0f)] [SerializeField] private float autoRestartSeconds;

    [Tooltip("再生を始めた時点で自動的に 1 回目を開始する。")]
    [SerializeField] private bool startOnPlay = true;

    [Header("【表示先】")]
    [Tooltip("ミニゲームの生成先。本編の MiniGameHost/Content と同じ大きさにしてある。")]
    [SerializeField] private RectTransform contentArea;

    [SerializeField] private TMP_Text kindLabel;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private TMP_Text timeLimitLabel;
    [SerializeField] private TMP_Text resultLabel;

    [Header("【操作ボタン】")]
    [SerializeField] private Button previousKindButton;
    [SerializeField] private Button nextKindButton;
    [SerializeField] private Button levelDownButton;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button restartButton;

    [Header("【表示する文言】")]
    [SerializeField] private string kindFormat = "ミニゲーム: {0}";
    [SerializeField] private string levelFormat = "レベル: {0}";
    [SerializeField] private string timeLimitFormat = "制限時間: {0:F1} 秒 ({1})";
    [SerializeField] private string runningText = "実行中";
    [SerializeField] private string successFormat = "成功 ({0})";
    [SerializeField] private string failureFormat = "失敗 ({0})";

    private MiniGameBase current;
    private float restartAt = -1f;

    private void Start()
    {
        Bind(previousKindButton, () => ShiftKind(-1));
        Bind(nextKindButton, () => ShiftKind(1));
        Bind(levelDownButton, () => ShiftLevel(-1));
        Bind(levelUpButton, () => ShiftLevel(1));
        Bind(restartButton, Run);

        RefreshLabels();
        if (startOnPlay)
        {
            Run();
        }
    }

    private void Update()
    {
        if (restartAt < 0f || Time.unscaledTime < restartAt)
        {
            return;
        }

        restartAt = -1f;
        Run();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    /// <summary>今の条件でミニゲームを 1 回起動する。実行中のものがあれば捨てる。</summary>
    [ContextMenu("実行")]
    public void Run()
    {
        if (contentArea == null)
        {
            Debug.LogError(nameof(MiniGameTestBench) + ": contentArea が未設定です。", this);
            return;
        }

        restartAt = -1f;
        Clear();

        float timeLimit;
        var prefab = ResolvePrefab(out timeLimit);
        if (prefab == null)
        {
            SetResult(kind + " は カタログに登録されていません");
            RefreshLabels();
            return;
        }

        if (overrideTimeLimit)
        {
            timeLimit = timeLimitOverride;
        }

        current = Instantiate(prefab, contentArea, false);
        current.OnCompleted += HandleCompleted;
        SetResult(runningText);
        RefreshLabels(timeLimit);

        // Initialize は本編と同じく生成の直後に 1 回だけ呼ぶ。
        current.Initialize(level, timeLimit);
    }

    private MiniGameBase ResolvePrefab(out float timeLimit)
    {
        if (directPrefab != null)
        {
            // 直接指定のときはカタログを引かないため、制限時間の既定値を持てない。
            timeLimit = timeLimitOverride;
            return directPrefab;
        }

        MiniGameCatalog.Entry entry;
        if (catalog != null && catalog.TryGetEntry(kind, out entry) && entry.prefab != null)
        {
            timeLimit = entry.GetTimeLimit(level);
            return entry.prefab;
        }

        timeLimit = 0f;
        return null;
    }

    private void HandleCompleted(bool success, string reason)
    {
        SetResult(string.Format(success ? successFormat : failureFormat, reason));
        Debug.Log(nameof(MiniGameTestBench) + ": " + kind + " レベル " + level
                  + " -> " + (success ? "成功" : "失敗") + " (" + reason + ")", this);
        Unsubscribe();

        if (autoRestartSeconds > 0f)
        {
            restartAt = Time.unscaledTime + autoRestartSeconds;
        }
    }

    /// <summary>登録されている種別のうち、今の種別から <paramref name="direction"/> 個ぶんずらす。</summary>
    private void ShiftKind(int direction)
    {
        var kinds = RegisteredKinds();
        if (kinds.Count == 0)
        {
            Debug.LogError(nameof(MiniGameTestBench) + ": カタログに登録が 1 件もありません。", this);
            return;
        }

        var index = kinds.IndexOf(kind);
        if (index < 0)
        {
            index = 0;
        }
        else
        {
            index = (index + direction + kinds.Count) % kinds.Count;
        }

        kind = kinds[index];
        Run();
    }

    private void ShiftLevel(int direction)
    {
        var next = Mathf.Clamp(level + direction, 1, 4);
        if (next == level)
        {
            return;
        }

        level = next;
        Run();
    }

    private List<TaskKind> RegisteredKinds()
    {
        var result = new List<TaskKind>();
        if (catalog == null)
        {
            return result;
        }

        foreach (TaskKind candidate in Enum.GetValues(typeof(TaskKind)))
        {
            MiniGameCatalog.Entry unused;
            if (catalog.TryGetEntry(candidate, out unused))
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    private void Clear()
    {
        Unsubscribe();
        for (var i = contentArea.childCount - 1; i >= 0; i--)
        {
            Destroy(contentArea.GetChild(i).gameObject);
        }

        current = null;
    }

    private void Unsubscribe()
    {
        if (current != null)
        {
            current.OnCompleted -= HandleCompleted;
        }
    }

    private void RefreshLabels()
    {
        float timeLimit;
        ResolvePrefab(out timeLimit);
        RefreshLabels(overrideTimeLimit ? timeLimitOverride : timeLimit);
    }

    private void RefreshLabels(float timeLimit)
    {
        if (kindLabel != null)
        {
            kindLabel.text = string.Format(kindFormat, directPrefab != null ? directPrefab.name : kind.ToString());
        }

        if (levelLabel != null)
        {
            levelLabel.text = string.Format(levelFormat, level);
        }

        if (timeLimitLabel != null)
        {
            var source = overrideTimeLimit || directPrefab != null ? "上書き" : "カタログ";
            timeLimitLabel.text = string.Format(timeLimitFormat, timeLimit, source);
        }
    }

    private void SetResult(string message)
    {
        if (resultLabel != null)
        {
            resultLabel.text = message;
        }
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}

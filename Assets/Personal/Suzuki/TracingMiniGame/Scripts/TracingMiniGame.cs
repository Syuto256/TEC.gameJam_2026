using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 表示された経路をマウスでなぞる個人用のミニゲーム試作。
/// UI は単体デバッグシーン内で実行時に構築し、共通の MiniGameBase へ完了結果だけを通知する。
/// </summary>
public sealed class TracingMiniGame : MiniGameBase
{
    private const int MaxMissCount = 2;
    private static readonly Vector2[] DefaultPath =
    {
        new(-520f, -180f), new(-340f, 130f), new(-100f, -120f),
        new(120f, 160f), new(350f, -20f), new(520f, 180f),
    };

    [Header("単体デバッグ設定")]
    [SerializeField] private bool startOnStart = true;
    [SerializeField, Min(0.1f)] private float debugTimeLimit = 20f;

    [Header("判定設定（Canvas ローカル座標）")]
    [SerializeField, Min(1f)] private float traceTolerance = 34f;
    [SerializeField, Min(1f)] private float startRadius = 36f;
    [SerializeField, Min(1f)] private float endRadius = 36f;
    [SerializeField, Min(1f)] private float guideLineWidth = 18f;

    private readonly List<Vector2> pathPoints = new();
    private RectTransform boardRect;
    private RectTransform pointerMarker;
    private Text missText;
    private Text timeText;
    private Text resultText;
    private bool isTracing;
    private int missCount;

    private void Awake()
    {
        OnCompleted += HandleCompleted;
        pathPoints.AddRange(DefaultPath);
    }

    private void Start()
    {
        BuildDebugUi();
        if (startOnStart)
        {
            Initialize(1, debugTimeLimit);
        }
    }

    public override void Initialize(int difficulty, float timeLimit)
    {
        base.Initialize(difficulty, timeLimit);
        missCount = 0;
        ResetTrial();
        SetResult(string.Empty);
        UpdateHud();
    }

    protected override void OnUpdate(float deltaTime)
    {
        UpdateHud();

        var mouse = Mouse.current;
        if (mouse == null || boardRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                boardRect, mouse.position.ReadValue(), null, out var pointerPosition))
        {
            return;
        }

        if (!isTracing)
        {
            if (mouse.leftButton.wasPressedThisFrame && IsWithinRadius(pointerPosition, pathPoints[0], startRadius))
            {
                isTracing = true;
                ShowPointer(pointerPosition);
                SetResult("TRACING");
            }

            return;
        }

        if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
        {
            ResetTrial();
            SetResult("RESTART FROM START");
            return;
        }

        ShowPointer(pointerPosition);
        if (DistanceToPath(pointerPosition) > traceTolerance)
        {
            RegisterMiss();
            return;
        }

        if (IsWithinRadius(pointerPosition, pathPoints[^1], endRadius))
        {
            FinishGame(true, "COMPLETE");
        }
    }

    protected override void OnDestroy()
    {
        OnCompleted -= HandleCompleted;
        base.OnDestroy();
    }

    private void RegisterMiss()
    {
        missCount++;
        ResetTrial();
        UpdateHud();
        if (missCount >= MaxMissCount)
        {
            FinishGame(false, "MISSED");
            return;
        }

        SetResult("MISS! RETRY FROM START");
    }

    private void ResetTrial()
    {
        isTracing = false;
        if (pointerMarker != null)
        {
            pointerMarker.gameObject.SetActive(false);
        }
    }

    private void HandleCompleted(bool success, string reason)
    {
        ResetTrial();
        SetResult(success ? "SUCCESS!" : $"FAILED ({reason})");
        UpdateHud();
    }

    private void BuildDebugUi()
    {
        var canvasObject = new GameObject("TracingDebugCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        boardRect = CreateImage("Board", canvasObject.transform, new Color(0.06f, 0.08f, 0.13f, 0.97f));
        boardRect.anchorMin = boardRect.anchorMax = boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.anchoredPosition = new Vector2(0f, -20f);
        boardRect.sizeDelta = new Vector2(1320f, 720f);

        CreateText("Title", canvasObject.transform, "TRACE THE PATH", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(700f, 70f), Color.white);
        missText = CreateText("MissCount", canvasObject.transform, string.Empty, 28, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -130f), new Vector2(330f, 50f), new Color(1f, 0.55f, 0.62f));
        timeText = CreateText("Time", canvasObject.transform, string.Empty, 28, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -130f), new Vector2(330f, 50f), new Color(0.55f, 0.9f, 1f));
        resultText = CreateText("Result", canvasObject.transform, "HOLD LEFT MOUSE AT START", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(850f, 60f), Color.white);
        missText.rectTransform.pivot = new Vector2(0f, 0.5f);
        timeText.rectTransform.pivot = new Vector2(1f, 0.5f);

        for (var index = 0; index < pathPoints.Count - 1; index++)
        {
            CreateGuideSegment(boardRect, pathPoints[index], pathPoints[index + 1]);
        }

        CreatePointMarker("Start", boardRect, pathPoints[0], startRadius, new Color(0.2f, 1f, 0.55f));
        CreatePointMarker("End", boardRect, pathPoints[^1], endRadius, new Color(1f, 0.35f, 0.55f));
        pointerMarker = CreatePointMarker("Pointer", boardRect, pathPoints[0], 12f, new Color(0.35f, 0.9f, 1f));
        pointerMarker.gameObject.SetActive(false);
    }

    private void CreateGuideSegment(Transform parent, Vector2 from, Vector2 to)
    {
        var lineRect = CreateImage("GuideSegment", parent, new Color(0.65f, 0.72f, 0.82f));
        var delta = to - from;
        lineRect.anchorMin = lineRect.anchorMax = lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = (from + to) * 0.5f;
        lineRect.sizeDelta = new Vector2(delta.magnitude, guideLineWidth);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private static RectTransform CreatePointMarker(string name, Transform parent, Vector2 position, float radius, Color color)
    {
        var marker = CreateImage(name, parent, color);
        marker.anchorMin = marker.anchorMax = marker.pivot = new Vector2(0.5f, 0.5f);
        marker.anchoredPosition = position;
        marker.sizeDelta = Vector2.one * radius * 2f;
        return marker;
    }

    private static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image.rectTransform;
    }

    private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = value;
        var rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return text;
    }

    private void ShowPointer(Vector2 position)
    {
        pointerMarker.gameObject.SetActive(true);
        pointerMarker.anchoredPosition = position;
    }

    private void UpdateHud()
    {
        if (missText != null) missText.text = $"MISS: {missCount} / {MaxMissCount}";
        if (timeText != null) timeText.text = $"TIME: {Mathf.Max(0f, TimeRemaining):F1}";
    }

    private void SetResult(string value)
    {
        if (resultText != null) resultText.text = value;
    }

    private float DistanceToPath(Vector2 point)
    {
        var minimumDistance = float.MaxValue;
        for (var index = 0; index < pathPoints.Count - 1; index++)
        {
            minimumDistance = Mathf.Min(minimumDistance, DistanceToSegment(point, pathPoints[index], pathPoints[index + 1]));
        }

        return minimumDistance;
    }

    private static bool IsWithinRadius(Vector2 point, Vector2 center, float radius) => (point - center).sqrMagnitude <= radius * radius;

    private static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
    {
        var segment = to - from;
        var lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= Mathf.Epsilon) return Vector2.Distance(point, from);
        var projection = Mathf.Clamp01(Vector2.Dot(point - from, segment) / lengthSquared);
        return Vector2.Distance(point, from + projection * segment);
    }
}

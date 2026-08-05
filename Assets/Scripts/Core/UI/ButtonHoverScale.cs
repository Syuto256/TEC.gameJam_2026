using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// カーソルが乗った時にボタンをスムーズに拡大/縮小するコンポーネント。
/// </summary>
public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("【拡大・押下設定】")]
    [Tooltip("ホバー時の拡大倍率")]
    [SerializeField] private float hoverScale = 1.05f;

    [Tooltip("押下中の縮小倍率")]
    [SerializeField] private float pressScale = 0.95f;

    [Tooltip("拡大・縮小にかかる時間（秒）")]
    [SerializeField] private float duration = 0.1f;

    private Vector3 defaultScale;
    private Coroutine scaleCoroutine;
    private Button button;
    private bool isPointerOver;
    private bool isPointerDown;

    private void Awake()
    {
        // 初期状態のスケールを保持
        defaultScale = transform.localScale;
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        UpdateScaleAnimation();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        UpdateScaleAnimation();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable())
        {
            return;
        }

        isPointerDown = true;
        UpdateScaleAnimation();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable())
        {
            return;
        }

        isPointerDown = false;
        UpdateScaleAnimation();
    }

    private bool IsInteractable()
    {
        return button == null || button.interactable;
    }

    private void UpdateScaleAnimation()
    {
        if (!IsInteractable())
        {
            return;
        }

        var targetScale = defaultScale;
        if (isPointerDown && isPointerOver)
        {
            targetScale = defaultScale * pressScale;
        }
        else if (isPointerOver)
        {
            targetScale = defaultScale * hoverScale;
        }

        StartScaleAnimation(targetScale);
    }

    private void StartScaleAnimation(Vector3 targetScale)
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(AnimateScaleRoutine(targetScale));
    }

    private IEnumerator AnimateScaleRoutine(Vector3 targetScale)
    {
        Vector3 initialScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // ポーズ中（Time.timeScale = 0）でも UI が動くように unscaledDeltaTime を使用
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
            yield return null;
        }

        transform.localScale = targetScale;
        scaleCoroutine = null;
    }

    private void OnDisable()
    {
        // ボタンが非アクティブ化されたときにスケールを初期値に戻す（拡大されたまま残るのを防ぐ）
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        transform.localScale = defaultScale;
    }
}

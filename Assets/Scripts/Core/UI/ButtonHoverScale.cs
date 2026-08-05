using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// カーソルが乗った時にボタンをスムーズに拡大/縮小するコンポーネント。
/// </summary>
public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("【拡大・アジリティ設定】")]
    [Tooltip("ホバー時の拡大倍率")]
    [SerializeField] private float hoverScale = 1.5f;

    [Tooltip("拡大・縮小にかかる時間（秒）")]
    [SerializeField] private float duration = 0.1f;

    private Vector3 defaultScale;
    private Coroutine scaleCoroutine;

    private void Awake()
    {
        // 初期状態のスケールを保持
        defaultScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // カーソルが乗ったら 1.5 倍（指定倍率）へ
        StartScaleAnimation(defaultScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // カーソルが離れたら元のサイズへ
        StartScaleAnimation(defaultScale);
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
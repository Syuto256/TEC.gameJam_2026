using System;
using System.Collections;
using UnityEngine;

/// <summary>シーンをまたいで生き残る暗幕。遷移の前後を暗転・明転でつなぐ。</summary>
/// <remarks>
/// 実体は <c>Assets/Resources/FadeOverlay.prefab</c> にあり、<see cref="AppServices"/> が読み込んで常駐させる。
/// 見た目（色・不透明度の上限・Canvas の描画順）は Prefab 側で調整する。このクラスは時間だけを持つ。
/// Prefab から読み込むのは、暗幕がどのシーンにも属さないためである。シーンに実体を置く方式では
/// 5 シーンすべてに同じものを置くことになり、遷移中に一度消えてしまう。
/// </remarks>
public sealed class FadeOverlayView : MonoBehaviour
{
    private const string ResourcePath = "FadeOverlay";

    private static FadeOverlayView instance;
    private static bool missingPrefabReported;

    [Header("【必須】")]
    [Tooltip("暗幕の不透明度を動かす CanvasGroup。未設定ならこの GameObject のものを使う。")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("【時間】")]
    [Tooltip("暗くなるまでの秒数。長くすると遷移がもったりする。")]
    [Min(0f)] [SerializeField] private float fadeOutDurationSec = 0.25f;

    [Tooltip("真っ暗なまま待つ秒数。新しいシーンの準備が間に合わないときに伸ばす。")]
    [Min(0f)] [SerializeField] private float holdDurationSec = 0.05f;

    [Tooltip("明るくなるまでの秒数。暗くなる時間より少し長いと落ち着いて見える。")]
    [Min(0f)] [SerializeField] private float fadeInDurationSec = 0.30f;

    private Coroutine running;

    /// <summary>暗幕を用意する。Prefab が無い場合は null を返し、呼び出し側はフェードなしで進む。</summary>
    public static FadeOverlayView EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        var prefab = Resources.Load<FadeOverlayView>(ResourcePath);
        if (prefab == null)
        {
            if (!missingPrefabReported)
            {
                missingPrefabReported = true;
                Debug.LogError(
                    "Resources/" + ResourcePath + " が見つかりません。フェードなしでシーンを切り替えます。");
            }

            return null;
        }

        return Instantiate(prefab);
    }

    /// <summary>暗転してから <paramref name="action"/> を実行し、明転する。</summary>
    /// <returns>受け付けたら true。遷移中にもう一度呼ばれた場合は false を返し、何もしない。</returns>
    public bool TryRun(Action action)
    {
        if (action == null || canvasGroup == null || running != null)
        {
            return false;
        }

        running = StartCoroutine(Run(action));
        return true;
    }

    /// <remarks>
    /// **明転は、読み込みが終わってから始める。**
    /// <c>SceneManager.LoadScene</c> は同期で、そのフレームだけが 0.3 秒以上続く。
    /// 時間で動かしている以上、待たずに明転を始めるとその 1 フレームで明転が最後まで
    /// 終わってしまい、暗転しか見えない。
    /// <para>
    /// 実測（難易度選択 → ゲーム画面）: 暗転 0.25 秒は正常に動いていた。そのあと
    /// 1 フレームが 357 ms 続き、待ち 0.05 + 明転 0.30 = 0.35 秒ぶんが 1 回で消化されていた。
    /// **行き先が重いほど起きるので、特定の遷移だけの問題ではない。**
    /// </para>
    /// </remarks>
    private IEnumerator Run(Action action)
    {
        canvasGroup.blocksRaycasts = true;

        // ポーズ中（timeScale = 0）に難易度選択へ戻る経路があるため、実時間で動かす。
        yield return Fade(0f, 1f, fadeOutDurationSec);

        action();

        // **2 フレーム見送る。1 枚では足りない。**
        // 1 枚目は読み込みそのもののフレーム、2 枚目はその長さが deltaTime に出るフレームである。
        yield return null;
        yield return null;

        if (holdDurationSec > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDurationSec);
        }

        yield return Fade(1f, 0f, fadeInDurationSec);

        Finish();
    }

    /// <remarks>
    /// 経過は差分の足し上げではなく実時間の引き算で測る。
    /// 足し上げだと、長いフレームを 1 枚またいだだけでその秒数がまるごと乗ってしまう。
    /// </remarks>
    private IEnumerator Fade(float from, float to, float durationSec)
    {
        if (durationSec <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        var start = Time.realtimeSinceStartup;
        while (true)
        {
            var progress = (Time.realtimeSinceStartup - start) / durationSec;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(progress));
            if (progress >= 1f)
            {
                yield break;
            }

            yield return null;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            Debug.LogError("FadeOverlayView: CanvasGroup が見つかりません。", this);
            return;
        }

        // 起動直後は透明で、入力も遮らない。どのシーンから再生を始めても暗幕は残らない。
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void Finish()
    {
        running = null;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
    }
}

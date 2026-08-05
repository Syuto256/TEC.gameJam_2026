using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>シーンをまたいで PC の蓋を開け閉めする。遷移そのものをこの演出で見せる。</summary>
/// <remarks>
/// 実体は <c>Assets/Resources/PcLidOverlay.prefab</c> にあり、<see cref="AppServices"/> が読み込んで常駐させる。
/// コマ・秒数・光り方は Prefab 側で調整する。このクラスは順番と時間だけを持つ。
/// <para>
/// **Prefab から読み込むのは、演出がどのシーンにも属さないためである。** 蓋が閉じ切ってから
/// 次のシーンを読み込むので、シーンに実体を置く方式では演出の途中で消えてしまう。
/// <see cref="FadeOverlayView"/> と同じ理由・同じ作りである。
/// </para>
/// <para>
/// **コマの絵は 1920x1080 のレイヤーで、背景の PC.png と左右・下端が揃っている。**
/// そのため <c>(0,0)</c> に原寸で重ねるだけで、シーン側の PC とつながって見える。
/// ずらしたり拡大したりしないこと。
/// </para>
/// </remarks>
public sealed class PcLidView : MonoBehaviour
{
    private const string ResourcePath = "PcLidOverlay";

    private static PcLidView instance;
    private static bool missingPrefabReported;

    /// <summary>コマ 1 枚と、それを映しておく時間。</summary>
    [Serializable]
    public sealed class Frame
    {
        [Tooltip("表示するコマ。")]
        public Sprite sprite;

        [Tooltip("このコマを映しておく秒数。")]
        [Min(0f)] public float durationSec = 0.2f;
    }

    [Header("【必須】")]
    [Tooltip("演出全体の不透明度を動かす CanvasGroup。未設定ならこの GameObject のものを使う。")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("コマを映す Image。1920x1080 で画面いっぱいに置くこと。")]
    [SerializeField] private Image lidImage;

    [Header("【任意】")]
    [Tooltip("蓋が開いたあとに画面が起動して見えるように光らせる層。\n" +
             "PC の画面領域（1221x758 @ -4,-31）に合わせて置く。未設定なら光らせない。")]
    [SerializeField] private Image screenFlash;

    [Header("【コマ】")]
    [Tooltip("開けるときのコマ。閉じた状態から並べる。")]
    [SerializeField] private Frame[] openFrames = Array.Empty<Frame>();

    [Tooltip("閉じるときのコマ。開いた状態から並べる。")]
    [SerializeField] private Frame[] closeFrames = Array.Empty<Frame>();

    [Header("【時間】")]
    [Tooltip("画面が光りきるまでの秒数。0 にすると光らない。")]
    [Min(0f)] [SerializeField] private float flashInSec = 0.12f;

    [Tooltip("光が消えるまでの秒数。")]
    [Min(0f)] [SerializeField] private float flashOutSec = 0.35f;

    [Tooltip("次のシーンが出てから、この演出が消えるまでの秒数。\n" +
             "最後のコマとシーン側の PC は完全には同じ絵ではないため、\n" +
             "0 にすると切り替わりが目に見える。")]
    [Min(0f)] [SerializeField] private float handoffSec = 0.18f;

    private Coroutine running;

    /// <summary>演出を用意する。Prefab が無い場合は null を返し、呼び出し側は演出なしで進む。</summary>
    public static PcLidView EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        var prefab = Resources.Load<PcLidView>(ResourcePath);
        if (prefab == null)
        {
            if (!missingPrefabReported)
            {
                missingPrefabReported = true;
                Debug.LogError(
                    "Resources/" + ResourcePath + " が見つかりません。開閉演出なしでシーンを切り替えます。");
            }

            return null;
        }

        return Instantiate(prefab);
    }

    /// <summary>蓋を開けてから <paramref name="action"/> を実行し、演出を引き上げる。</summary>
    /// <returns>受け付けたら true。演出中にもう一度呼ばれた場合は false を返し、何もしない。</returns>
    public bool TryOpen(Action action)
    {
        return TryRun(openFrames, true, action);
    }

    /// <summary>蓋を閉じてから <paramref name="action"/> を実行し、演出を引き上げる。</summary>
    public bool TryClose(Action action)
    {
        return TryRun(closeFrames, false, action);
    }

    private bool TryRun(Frame[] frames, bool flashAfterwards, Action action)
    {
        if (action == null || canvasGroup == null || lidImage == null || running != null)
        {
            return false;
        }

        if (frames == null || frames.Length == 0)
        {
            Debug.LogError(nameof(PcLidView) + ": コマが 1 枚も入っていません。", this);
            return false;
        }

        running = StartCoroutine(Run(frames, flashAfterwards, action));
        return true;
    }

    private IEnumerator Run(Frame[] frames, bool flashAfterwards, Action action)
    {
        // ポーズ中（timeScale = 0）にゲームが終わる経路があるため、実時間で動かす。
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        SetFlashAlpha(0f);

        foreach (var frame in frames)
        {
            if (frame == null || frame.sprite == null)
            {
                continue;
            }

            lidImage.sprite = frame.sprite;
            var remaining = frame.durationSec;
            while (remaining > 0f)
            {
                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 蓋を開けたあとだけ、画面が起動したように光らせる。
        if (flashAfterwards)
        {
            yield return Fade(0f, 1f, flashInSec);
        }

        action();

        // LoadScene はフレームの終わりに効く。次のシーンが出るまで 1 フレーム待つ。
        yield return null;

        if (flashAfterwards)
        {
            yield return Fade(1f, 0f, flashOutSec);
        }

        // 最後のコマとシーン側の PC は完全に同じ絵ではないため、間を置いて引き上げる。
        var elapsed = 0f;
        while (elapsed < handoffSec)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = handoffSec <= 0f ? 0f : 1f - (elapsed / handoffSec);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        SetFlashAlpha(0f);
        running = null;
    }

    private IEnumerator Fade(float from, float to, float durationSec)
    {
        if (screenFlash == null || durationSec <= 0f)
        {
            SetFlashAlpha(to);
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < durationSec)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFlashAlpha(Mathf.Lerp(from, to, elapsed / durationSec));
            yield return null;
        }

        SetFlashAlpha(to);
    }

    private void SetFlashAlpha(float value)
    {
        if (screenFlash == null)
        {
            return;
        }

        var color = screenFlash.color;
        color.a = Mathf.Clamp01(value);
        screenFlash.color = color;
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
            Debug.LogError(nameof(PcLidView) + ": CanvasGroup が見つかりません。", this);
            return;
        }

        // 起動直後は透明で、入力も遮らない。どのシーンから再生を始めても演出は残らない。
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        SetFlashAlpha(0f);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

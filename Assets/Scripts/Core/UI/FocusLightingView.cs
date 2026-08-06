using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>ミニゲーム中だけ背後を落とし、窓のまわりを光らせる。</summary>
/// <remarks>
/// 明るさを持つだけで、ミニゲームの進行には関わらない。開始・終了の通知は
/// <see cref="GameManager"/> が <see cref="MainGameController.PlayerMiniGameActiveChanged"/> から中継する。
/// 常時の部屋の暗さと画面の発光は Scene 側（各デバイス面の RoomDimmer / ScreenGlow）が持っており、
/// こちらはそこへ重ねる差分だけを扱う。
/// </remarks>
public sealed class FocusLightingView : MonoBehaviour
{
    [Header("【必須】")]
    [Tooltip("ミニゲーム中に背後を落とす暗幕。\n" +
             "デバイス面とタスク吹き出しより手前、HUD より奥に置くこと（Shared の最初の子）。\n" +
             "Raycast Target は必ず切ること。切り忘れるとポーズボタンが押せなくなる。")]
    [SerializeField] private Image dimmer;

    [Header("【任意】")]
    [Tooltip("ミニゲームの窓のまわりを光らせる層。MiniGameHost の子で Content より奥に置く。\n" +
             "Fill Center を切り、窓の外側だけが光るようにすること。\n" +
             "未設定なら暗幕だけが効く。")]
    [SerializeField] private Image glow;

    [Header("【濃さ】")]
    [Tooltip("暗幕の濃さ。0 にすると暗くならない。")]
    [Range(0f, 1f)] [SerializeField] private float dimAlpha = 0.5f;

    [Tooltip("窓のまわりの光の強さ。0 にすると光らない。")]
    [Range(0f, 1f)] [SerializeField] private float glowAlpha = 0.4f;

    [Header("【時間】")]
    [Tooltip("ミニゲームが開いてから暗くなりきるまでの秒数。")]
    [Min(0f)] [SerializeField] private float fadeInSec = 0.22f;

    [Tooltip("ミニゲームが閉じてから明るさが戻るまでの秒数。開くときより短くすること。\n" +
             "終わったあとに待たされると、次のタスクへ移るのが遅れて感じられる。")]
    [Min(0f)] [SerializeField] private float fadeOutSec = 0.14f;

    [Tooltip("明るさが変わる緩急。")]
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    private Tween dimTween;
    private Tween glowTween;
    private bool initialized;

    /// <summary>参照を検証し、暗幕も光も消えた状態から始める。</summary>
    public bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (!SceneUiValidation.Require(this, (nameof(dimmer), dimmer)))
        {
            return false;
        }

        Clear(dimmer);
        Clear(glow);

        initialized = true;
        return true;
    }

    /// <summary>ミニゲームの開始・終了に合わせて明るさを切り替える。</summary>
    /// <remarks>
    /// **窓の光は開くときだけ補間し、閉じるときは即座に消す。**
    /// 閉じる側は <c>MainGameController</c> がこの通知の直後に <c>MiniGameHostView.Hide</c> を呼び、
    /// 枝ごと無効になる。補間しても途中で止まり、中途半端な明るさが次回へ持ち越されるためである。
    /// 暗幕は Shared にあって無効にならないため、両方向とも補間する。
    /// </remarks>
    public void SetFocused(bool focused)
    {
        if (!initialized)
        {
            return;
        }

        FadeDimmer(focused);

        if (glow == null)
        {
            return;
        }

        glowTween?.Kill();
        glowTween = null;

        // 強さが 0 なら描画に入れない。入れても、透明な板を毎フレーム描くだけになる。
        if (glowAlpha <= 0f)
        {
            Clear(glow);
            return;
        }

        if (!focused)
        {
            Clear(glow);
            return;
        }

        glow.enabled = true;
        if (fadeInSec <= 0f)
        {
            SetAlpha(glow, glowAlpha);
            return;
        }

        glowTween = glow.DOFade(glowAlpha, fadeInSec).SetEase(fadeEase).SetLink(gameObject);
    }

    private void FadeDimmer(bool focused)
    {
        dimTween?.Kill();
        dimTween = null;

        // 濃さが 0 なら描画に入れない。入れても、透明な全画面の板を毎フレーム描くだけになる。
        if (dimAlpha <= 0f)
        {
            Clear(dimmer);
            return;
        }

        var duration = focused ? fadeInSec : fadeOutSec;
        var target = focused ? dimAlpha : 0f;

        if (duration <= 0f)
        {
            if (focused)
            {
                dimmer.enabled = true;
                SetAlpha(dimmer, target);
            }
            else
            {
                Clear(dimmer);
            }

            return;
        }

        // 描画から外していると補間できないため、先に戻す。消すのは暗くなりきった後で行う。
        dimmer.enabled = true;
        dimTween = dimmer.DOFade(target, duration).SetEase(fadeEase).SetLink(gameObject);
        if (!focused)
        {
            dimTween.OnComplete(() => dimmer.enabled = false);
        }
    }

    /// <summary>透明にしたうえで描画からも外す。透明な全画面の板を毎フレーム描かせないため。</summary>
    private static void Clear(Image image)
    {
        if (image == null)
        {
            return;
        }

        SetAlpha(image, 0f);
        image.enabled = false;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        var color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void OnDestroy()
    {
        dimTween?.Kill();
        dimTween = null;
        glowTween?.Kill();
        glowTween = null;
    }
}

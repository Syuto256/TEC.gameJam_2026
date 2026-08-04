using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>タスクの決着を、それが起きた位置に短い文字と飛び散る粒で知らせる層。</summary>
/// <remarks>
/// 文字と粒は、この層の下に置いた非アクティブの複製元を実行時に複製して作る
/// （なぞりのガイド線や QTE のキー枠と同じやり方）。見た目は複製元の Inspector で調整し、
/// このクラスは位置と動きだけを決める。
/// 決着ごとの文字・色・粒の数は <see cref="styles"/> で変える。
/// </remarks>
public sealed class ResultEffectLayerView : MonoBehaviour
{
    /// <summary>1 つの決着に対する見た目。</summary>
    [Serializable]
    public sealed class ResolutionStyle
    {
        [Tooltip("どの決着に対する見た目か。同じ決着を 2 行入れた場合は先に書いたほうが使われる。")]
        public TaskResolution resolution;

        [Tooltip("表示する文字。{0} は加算スコアに置き換わる。空にすると文字を出さない。")]
        public string text = "{0}";

        [Tooltip("文字と粒の色。")]
        public Color color = Color.white;

        [Tooltip("飛び散る粒の数。0 にすると粒を出さない。増やすほど派手になり、負荷も増える。")]
        [Min(0)] public int burstCount;
    }

    [Header("【複製元】")]
    [Tooltip("浮かび上がる文字の複製元。非アクティブのまま置くこと。フォントと大きさはここで調整する。")]
    [SerializeField] private TextMeshProUGUI floatingTextTemplate;

    [Tooltip("飛び散る粒の複製元。非アクティブのまま置くこと。\n" +
             "スプライトを差し替えると粒の見た目が変わる（紙吹雪・破片など）。未設定なら粒は出ない。")]
    [SerializeField] private Image burstParticleTemplate;

    [Header("【決着ごとの見た目】")]
    [Tooltip("決着の種類ごとの文字・色・粒の数。行が無い決着では何も出ない。")]
    [SerializeField]
    private ResolutionStyle[] styles =
    {
        // 自力成功の獲得点は HUD 中央のポップアップが大きく出すため、ここでは文字を出さない。
        // 粒だけ残し、「どの吹き出しが片付いたか」は見えるようにする。
        new ResolutionStyle
        {
            resolution = TaskResolution.PlayerSuccess,
            text = "",
            color = new Color(0.45f, 1f, 0.55f, 1f),
            burstCount = 10
        },
        new ResolutionStyle
        {
            resolution = TaskResolution.AiSuccess,
            text = "+{0}",
            color = new Color(0.45f, 0.85f, 1f, 1f),
            burstCount = 5
        },
        new ResolutionStyle
        {
            resolution = TaskResolution.PlayerFailure,
            text = "MISS",
            color = new Color(1f, 0.35f, 0.35f, 1f),
            burstCount = 6
        },
        new ResolutionStyle
        {
            resolution = TaskResolution.AiFailure,
            text = "AI ERROR",
            color = new Color(1f, 0.3f, 0.45f, 1f),
            burstCount = 6
        },
        new ResolutionStyle
        {
            resolution = TaskResolution.Expired,
            text = "TIME OUT",
            color = new Color(1f, 0.72f, 0.25f, 1f),
            burstCount = 4
        }
    };

    [Header("【文字の動き】")]
    [Tooltip("文字が浮き上がる距離（ピクセル）。")]
    [SerializeField] private float textRiseDistance = 90f;

    [Tooltip("文字が消えるまでの秒数。")]
    [Min(0.05f)] [SerializeField] private float textDurationSec = 0.9f;

    [Header("【粒の動き】")]
    [Tooltip("粒が飛ぶ距離の下限（ピクセル）。")]
    [Min(0f)] [SerializeField] private float burstMinDistance = 40f;

    [Tooltip("粒が飛ぶ距離の上限（ピクセル）。下限と同じにすると全部同じ距離まで飛ぶ。")]
    [Min(0f)] [SerializeField] private float burstMaxDistance = 140f;

    [Tooltip("粒が消えるまでの秒数。")]
    [Min(0.05f)] [SerializeField] private float burstDurationSec = 0.6f;

    private bool initialized;

    /// <summary>参照を検証し、複製元を隠す。</summary>
    public bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (!SceneUiValidation.Require(this, (nameof(floatingTextTemplate), floatingTextTemplate)))
        {
            return false;
        }

        // 複製元は表示しない。Inspector で誤って有効にしても実行時に隠す。
        floatingTextTemplate.gameObject.SetActive(false);
        if (burstParticleTemplate != null)
        {
            burstParticleTemplate.gameObject.SetActive(false);
        }

        initialized = true;
        return true;
    }

    /// <summary>決着の演出を、指定したワールド位置に出す。</summary>
    /// <param name="worldPosition">吹き出しなど、決着が起きた場所のワールド座標。</param>
    /// <param name="resolution">どの決着か。</param>
    /// <param name="addedScore">加算されたスコア。文字の {0} に入る。</param>
    public void Play(Vector3 worldPosition, TaskResolution resolution, int addedScore)
    {
        if (!initialized || !TryGetStyle(resolution, out var style))
        {
            return;
        }

        SpawnFloatingText(worldPosition, style, addedScore);
        SpawnBurst(worldPosition, style);
    }

    private void SpawnFloatingText(Vector3 worldPosition, ResolutionStyle style, int addedScore)
    {
        if (string.IsNullOrEmpty(style.text))
        {
            return;
        }

        var label = Instantiate(floatingTextTemplate, transform);
        label.gameObject.SetActive(true);
        label.text = Format(style.text, addedScore);
        label.color = style.color;

        var rect = label.rectTransform;
        rect.position = worldPosition;
        var startY = rect.anchoredPosition.y;

        DOTween.Sequence()
            .Join(rect.DOAnchorPosY(startY + textRiseDistance, textDurationSec).SetEase(Ease.OutCubic))
            .Join(label.DOFade(0f, textDurationSec).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(label.gameObject));
    }

    private void SpawnBurst(Vector3 worldPosition, ResolutionStyle style)
    {
        if (burstParticleTemplate == null || style.burstCount <= 0)
        {
            return;
        }

        var maxDistance = Mathf.Max(burstMinDistance, burstMaxDistance);
        for (var i = 0; i < style.burstCount; i++)
        {
            var particle = Instantiate(burstParticleTemplate, transform);
            particle.gameObject.SetActive(true);
            particle.color = style.color;

            var rect = particle.rectTransform;
            rect.position = worldPosition;

            var direction = UnityEngine.Random.insideUnitCircle.normalized;
            var distance = UnityEngine.Random.Range(burstMinDistance, maxDistance);
            var destination = rect.anchoredPosition + direction * distance;

            DOTween.Sequence()
                .Join(rect.DOAnchorPos(destination, burstDurationSec).SetEase(Ease.OutCubic))
                .Join(rect.DOScale(0f, burstDurationSec).SetEase(Ease.InQuad))
                .Join(particle.DOFade(0f, burstDurationSec).SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(particle.gameObject));
        }
    }

    /// <summary>{0} を加算スコアに置き換える。書式が壊れていても演出を止めない。</summary>
    private static string Format(string format, int addedScore)
    {
        try
        {
            return string.Format(format, addedScore);
        }
        catch (FormatException)
        {
            return format;
        }
    }

    private bool TryGetStyle(TaskResolution resolution, out ResolutionStyle style)
    {
        foreach (var candidate in styles)
        {
            if (candidate != null && candidate.resolution == resolution)
            {
                style = candidate;
                return true;
            }
        }

        style = null;
        return false;
    }
}

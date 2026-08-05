using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>デバイス切替の横スライドに重ねる、スピード感のためのモーション線。</summary>
/// <remarks>
/// 線は、この層の下に置いた非アクティブの複製元を実行時に複製して作る
/// （<see cref="ResultEffectLayerView"/> と同じやり方）。見た目は複製元の Inspector で調整し、
/// このクラスは本数と動きだけを決める。複製元にスプライトを差せば、コードを変えずに質を上げられる。
/// 置き場所はスライドする面より手前、HUD より奥。切替はゲーム中に何度も起きるため、
/// 初期値は控えめにしてある。強すぎると画面がうるさく、酔いにも繋がる。
/// </remarks>
public sealed class SlideMotionLinesView : MonoBehaviour
{
    [Header("【複製元】")]
    [Tooltip("流れる線の複製元。非アクティブのまま置くこと。Raycast Target は切っておくこと。\n" +
             "スプライトを差し替えると線の見た目が変わる（端がぼけた尾など）。未設定なら線は出ない。")]
    [SerializeField] private Image lineTemplate;

    [Header("【本数と色】")]
    [Tooltip("1 回の切替で流す線の本数。増やすほど派手になる。0 にすると出ない。")]
    [Min(0)] [SerializeField] private int lineCount = 14;

    [Tooltip("線の色。透明度はここでは使わず、下の範囲から 1 本ごとに抽選する。")]
    [SerializeField] private Color lineColor = Color.white;

    [Tooltip("線の透明度の下限。")]
    [Range(0f, 1f)] [SerializeField] private float minAlpha = 0.15f;

    [Tooltip("線の透明度の上限。下限と同じにすると全部同じ濃さになる。")]
    [Range(0f, 1f)] [SerializeField] private float maxAlpha = 0.35f;

    [Header("【線の形】")]
    [Tooltip("線の長さの下限（ピクセル）。")]
    [Min(1f)] [SerializeField] private float minLength = 160f;

    [Tooltip("線の長さの上限（ピクセル）。下限と同じにすると全部同じ長さになる。")]
    [Min(1f)] [SerializeField] private float maxLength = 520f;

    [Tooltip("線の太さの下限（ピクセル）。")]
    [Min(1f)] [SerializeField] private float minThickness = 2f;

    [Tooltip("線の太さの上限（ピクセル）。")]
    [Min(1f)] [SerializeField] private float maxThickness = 4f;

    [Header("【流れる速さ】")]
    [Tooltip("スライドの所要時間に対して、1 本が画面を横切るのにかける割合。\n" +
             "小さいほど速く鋭くなる。")]
    [Range(0.1f, 1f)] [SerializeField] private float travelRatio = 0.55f;

    [Tooltip("スライドの所要時間に対して、線が出はじめる時刻をばらつかせる幅の割合。\n" +
             "0 にすると全部同時に走り出す。")]
    [Range(0f, 0.5f)] [SerializeField] private float staggerRatio = 0.25f;

    private bool initialized;

    /// <summary>参照を検証し、複製元を隠す。複製元が無い場合も演出なしとして成立させる。</summary>
    public bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        // 複製元は表示しない。Inspector で誤って有効にしても実行時に隠す。
        if (lineTemplate != null)
        {
            lineTemplate.gameObject.SetActive(false);
        }

        initialized = true;
        return true;
    }

    /// <summary>1 回ぶんの線を流す。</summary>
    /// <param name="direction">スライドの向き。+1 なら右の席へ移る（＝画面の中身は左へ流れる）。</param>
    /// <param name="slideDurationSec">スライド全体の秒数。線はこれより前に消え切る。</param>
    public void Play(float direction, float slideDurationSec)
    {
        if (!initialized || lineTemplate == null || lineCount <= 0 || slideDurationSec <= 0f)
        {
            return;
        }

        // レイアウトが確定していないと 0 になる。そのときは線を出さない。
        var area = transform as RectTransform;
        if (area == null || area.rect.width <= 0f || area.rect.height <= 0f)
        {
            return;
        }

        var width = area.rect.width;
        var height = area.rect.height;

        // 画面の中身は移動方向と逆に流れる。線もそちらへ合わせる。
        var flow = direction > 0f ? -1f : 1f;

        // 着地までに消え切らせる。遅れの上限と横切る時間を足しても全体には届かない。
        var travelSec = Mathf.Max(0.01f, slideDurationSec * travelRatio);
        var maxDelay = Mathf.Max(0f, slideDurationSec * staggerRatio);

        for (var i = 0; i < lineCount; i++)
        {
            SpawnLine(width, height, flow, travelSec, maxDelay);
        }
    }

    private void SpawnLine(float width, float height, float flow, float travelSec, float maxDelay)
    {
        var line = Instantiate(lineTemplate, transform);
        line.gameObject.SetActive(true);
        line.color = new Color(
            lineColor.r, lineColor.g, lineColor.b,
            Random.Range(Mathf.Min(minAlpha, maxAlpha), Mathf.Max(minAlpha, maxAlpha)));

        var length = Random.Range(minLength, Mathf.Max(minLength, maxLength));
        var rect = line.rectTransform;
        rect.sizeDelta = new Vector2(length, Random.Range(minThickness, Mathf.Max(minThickness, maxThickness)));

        // 画面の外から入って外へ抜ける。途中で湧いたり消えたりして見えないようにする。
        var edgeX = (width + length) * 0.5f;
        rect.anchoredPosition = new Vector2(-flow * edgeX, Random.Range(-height * 0.5f, height * 0.5f));

        var duration = travelSec * Random.Range(0.8f, 1f);
        DOTween.Sequence()
            .Join(rect.DOAnchorPosX(flow * edgeX, duration).SetEase(Ease.Linear))
            .Join(line.DOFade(0f, duration).SetEase(Ease.InQuad))
            .PrependInterval(Random.Range(0f, maxDelay))

            // スライド本体と同じ実時間で動かす。ポーズを挟んでも線だけ画面に残らない。
            .SetUpdate(true)
            .SetLink(line.gameObject)
            .OnComplete(() => Destroy(line.gameObject));
    }
}

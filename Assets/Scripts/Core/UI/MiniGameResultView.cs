using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>ミニゲームの決着を、窓が閉じる直前に作業領域の上で知らせる。</summary>
/// <remarks>
/// タスク吹き出し側にも結果は出るが、こちらは「作業そのものが終わった」ことを作業画面の中で伝える。
/// **枠付きの小窓（ダイアログ）にはしていない。** 押せるものが出ると「OK を押す」動作を期待させるが、
/// この表示は数フレームで勝手に消えるためである。
/// 文言はミニゲームごとに違うため、値はすべて Inspector に置く。
/// </remarks>
public sealed class MiniGameResultView : MonoBehaviour
{
    [Header("【必須】")]
    [Tooltip("結果を出すあいだだけ有効にする入れ物。作業領域を覆う大きさにすること。\n" +
             "浮かび上がらせるため CanvasGroup を付けておく。")]
    [SerializeField] private GameObject root;

    [Tooltip("結果の文言を出す場所。")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("【文言】")]
    [Tooltip("成功したときの文言。ミニゲームごとに書き換える。")]
    [SerializeField] private string successMessage = "終わりました";

    [Tooltip("失敗・時間切れのときの文言。")]
    [SerializeField] private string failureMessage = "失敗しました";

    [Header("【色】")]
    [SerializeField] private Color successColor = new Color(0.35f, 1f, 0.50f, 1f);
    [SerializeField] private Color failureColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("【時間】")]
    [Tooltip("結果を見せてから窓が閉じるまでの秒数。\n" +
             "窓・暗幕・決着の粒の片付けが、そろってこの秒数だけ遅れる。\n" +
             "0 にすると結果を出さずに閉じるのと同じ見え方になる。")]
    [Min(0f)] [SerializeField] private float holdSec = 0.6f;

    [Tooltip("結果が浮かび上がるまでの秒数。holdSec より短くすること。")]
    [Min(0f)] [SerializeField] private float fadeInSec = 0.12f;

    private Tween fadeTween;
    private bool shown;

    /// <summary>結果を出し、窓を閉じるまで待つ秒数を返す。</summary>
    /// <remarks>
    /// 参照が欠けていれば 0 を返す。**戻り値が 0 のときは待たずに閉じてよい**ので、
    /// この層を置いていないミニゲームでも従来どおり動く。
    /// 二重に呼ばれても最初の 1 回だけを見せる。
    /// </remarks>
    public float Show(bool success)
    {
        if (shown || root == null || messageText == null)
        {
            return 0f;
        }

        shown = true;
        messageText.text = success ? successMessage : failureMessage;
        messageText.color = success ? successColor : failureColor;
        root.SetActive(true);

        var group = root.GetComponent<CanvasGroup>();
        if (group != null)
        {
            if (fadeInSec <= 0f)
            {
                group.alpha = 1f;
            }
            else
            {
                group.alpha = 0f;
                fadeTween = group.DOFade(1f, fadeInSec).SetLink(root);
            }
        }

        return holdSec;
    }

    private void OnDestroy()
    {
        fadeTween?.Kill();
        fadeTween = null;
    }
}

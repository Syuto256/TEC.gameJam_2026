using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Overwork.MiniGames.Qte
{
    /// <summary>警告ダイアログ 1 枚の見た目。判定には関わらない。</summary>
    /// <remarks>
    /// 大きさ・配色・字体は Prefab 上で編集して調整する。
    /// 位置は <see cref="QteMiniGame"/> が置き場所（スロット）の子にすることで決まるため、
    /// ここでは座標を持たない。
    /// </remarks>
    public sealed class QteAlertView : MonoBehaviour
    {
        [Header("【表示先】")]
        [Tooltip("押すキーの枠。")]
        [SerializeField] private QteKeyCell keyCell;

        [Tooltip("「保存してください」などの文言。")]
        [SerializeField] private TMP_Text messageText;

        [Tooltip("猶予の残りを表す帯。左端を軸にして幅が縮む。")]
        [SerializeField] private RectTransform graceFill;

        [Tooltip("猶予の残りで色を変える帯。graceFill と同じものでよい。")]
        [SerializeField] private Image graceImage;

        [Header("【猶予の色】")]
        [Tooltip("まだ余裕があるときの色。")]
        [SerializeField] private Color calmColor = new Color(0.38f, 0.95f, 0.78f, 1f);

        [Tooltip("残りが少ないときの色。")]
        [SerializeField] private Color urgentColor = new Color(0.95f, 0.35f, 0.35f, 1f);

        [Tooltip("この割合を下回ったら急ぎの色にする。")]
        [Range(0f, 1f)] [SerializeField] private float urgentRatio = 0.35f;

        public void Setup(string message, string keyLabel)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }

            if (keyCell != null)
            {
                keyCell.SetLabel(keyLabel);
                keyCell.SetState(QteCellState.Current);
            }

            SetRemaining(1f);
        }

        /// <summary>猶予の残りを 0〜1 で渡す。</summary>
        /// <remarks>
        /// <see cref="Image.fillAmount"/> ではなく幅で表している。
        /// 絵を割り当てていない <see cref="Image"/> では <c>fillAmount</c> が働かず
        /// （<c>Image.OnPopulateMesh</c> が絵の無いときに Type を見ないため）、
        /// 帯が減らないまま気付きにくい不具合になるためである。
        /// </remarks>
        public void SetRemaining(float ratio)
        {
            var clamped = Mathf.Clamp01(ratio);

            if (graceFill != null)
            {
                var max = graceFill.anchorMax;
                graceFill.anchorMax = new Vector2(clamped, max.y);
            }

            if (graceImage != null)
            {
                graceImage.color = clamped <= urgentRatio ? urgentColor : calmColor;
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Overwork.MiniGames.Qte
{
    /// <summary>お題に並ぶキー 1 つ分の見た目。</summary>
    public enum QteCellState
    {
        /// <summary>まだ押していない。</summary>
        Pending,

        /// <summary>今これを押す。</summary>
        Current,

        /// <summary>押し終わった。</summary>
        Done
    }

    /// <summary>キー 1 つ分の枠。色と文字だけを持ち、判定には関わらない。</summary>
    /// <remarks>
    /// 大きさ・角丸・字体は複製元のオブジェクトを Prefab 上で編集して調整する。
    /// 状態ごとの色もここに出しているため、配色を変えるのにコードは触らない。
    /// </remarks>
    public sealed class QteKeyCell : MonoBehaviour
    {
        [Header("【表示先】")]
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;

        [Header("【状態ごとの色】")]
        [Tooltip("まだ押していないキーの色。")]
        [SerializeField] private Color pendingColor = new Color(0.72f, 0.75f, 0.83f, 1f);

        [Tooltip("今押すキーの色。ここだけ目立たせる。")]
        [SerializeField] private Color currentColor = new Color(1f, 0.85f, 0.25f, 1f);

        [Tooltip("押し終わったキーの色。")]
        [SerializeField] private Color doneColor = new Color(0.29f, 0.79f, 0.35f, 1f);

        [Tooltip("今押すキーだけ、この倍率で大きく表示する。1 にすると大きさを変えない。")]
        [Min(1f)] [SerializeField] private float currentScale = 1.15f;

        public void SetLabel(string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        public void SetState(QteCellState state)
        {
            if (background != null)
            {
                background.color = state switch
                {
                    QteCellState.Current => currentColor,
                    QteCellState.Done => doneColor,
                    _ => pendingColor
                };
            }

            transform.localScale = Vector3.one * (state == QteCellState.Current ? currentScale : 1f);
        }
    }
}

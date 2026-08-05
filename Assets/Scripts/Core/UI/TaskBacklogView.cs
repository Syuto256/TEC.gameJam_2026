using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>まだ画面に出せていないタスクの件数を、控えめに表示する。</summary>
/// <remarks>
/// 出しているのは数字だけで、見出しやアイコンは持たない。それらは <c>root</c> の下に
/// 置いた飾りとして Scene 側で作ること（このクラスは <c>root</c> の表示・非表示しか触らない）。
/// 面ごとの内訳は出さない。溜まっている面まで数字で伝えると読む手間が増えるためで、
/// 面ごとに分けたくなった場合は <see cref="TaskManager.CountQueued"/> で取れる。
/// </remarks>
public sealed class TaskBacklogView : MonoBehaviour
{
    [Header("【必須】")]
    [Tooltip("件数が 0 のときに丸ごと隠す入れ物。\n" +
             "見出しやアイコンなど、数字以外の飾りもすべてこの下に置くこと。")]
    [SerializeField] private GameObject root;

    [Tooltip("件数の数字。色・大きさ・書体は Scene 側で決める。")]
    [SerializeField] private TextMeshProUGUI countText;

    [Header("【演出】")]
    [Tooltip("件数が増えたときに数字が跳ねる倍率。1 にすると跳ねない。")]
    [Min(1f)] [SerializeField] private float punchScale = 1.25f;

    [Tooltip("跳ねてから元の大きさに戻るまでの秒数。")]
    [Min(0f)] [SerializeField] private float punchDurationSec = 0.2f;

    private Tween punchTween;
    private int lastCount = int.MinValue;
    private bool initialized;

    /// <summary>参照を検証し、件数 0 の状態から始める。</summary>
    public bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (!SceneUiValidation.Require(this, (nameof(root), root), (nameof(countText), countText)))
        {
            return false;
        }

        root.SetActive(false);
        initialized = true;
        return true;
    }

    /// <summary>件数を反映する。0 件なら丸ごと隠す。</summary>
    /// <remarks>毎フレーム呼ばれる前提のため、値が変わったときだけ書き換える。</remarks>
    public void Render(int queuedCount)
    {
        if (!initialized || queuedCount == lastCount)
        {
            return;
        }

        // 初回は「増えた」とみなさない。開始直後に何もしていないのに跳ねて見えるため。
        var increased = lastCount != int.MinValue && queuedCount > lastCount;
        lastCount = queuedCount;

        if (queuedCount <= 0)
        {
            ResetScale();
            root.SetActive(false);
            return;
        }

        root.SetActive(true);
        countText.text = queuedCount.ToString();

        if (!increased || punchScale <= 1f || punchDurationSec <= 0f)
        {
            return;
        }

        // 跳ねが重なると原寸が狂うため、必ず等倍へ戻してから始める。
        ResetScale();
        punchTween = countText.transform
            .DOPunchScale(Vector3.one * (punchScale - 1f), punchDurationSec, 1, 0.5f)
            .OnKill(ResetScaleOnly);
    }

    private void ResetScale()
    {
        punchTween?.Kill();
        punchTween = null;
        ResetScaleOnly();
    }

    private void ResetScaleOnly()
    {
        if (countText != null)
        {
            countText.transform.localScale = Vector3.one;
        }
    }

    private void OnDestroy()
    {
        punchTween?.Kill();
        punchTween = null;
    }
}

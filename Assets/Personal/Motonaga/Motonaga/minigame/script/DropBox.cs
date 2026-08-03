using UnityEngine;
using UnityEngine.EventSystems;

// 右側（または両サイド）の「箱」に付けるスクリプト
// 必要コンポーネント: Image（見た目、Raycast Target ON）
public class DropBox : MonoBehaviour, IDropHandler
{
    [Tooltip("この箱が受け入れる記号ID。DraggableItem側のmatchIdと一致させる")]
    public string acceptId;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        DraggableItem item = dropped.GetComponent<DraggableItem>();
        if (item == null) return;

        if (item.matchId == acceptId)
        {
            SortingGameManager.Instance.OnCorrectMatch(item.gameObject);
        }
        else
        {
            SortingGameManager.Instance.OnMissMatch();
            item.ReturnToStart();
        }
    }
}
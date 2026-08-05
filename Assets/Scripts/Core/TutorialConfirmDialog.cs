using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialConfirmDialog : MonoBehaviour
{
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onYesClicked;
    private Action onNoClicked;

    private void Awake()
    {
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(() =>
            {
                Hide();
                onYesClicked?.Invoke();
            });
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(() =>
            {
                Hide();
                onNoClicked?.Invoke();
            });
        }

        Hide();
    }

    public void Show(Action onYes, Action onNo)
    {
        onYesClicked = onYes;
        onNoClicked = onNo;

        // ★ ここでしっかり表示されているか確認
        gameObject.SetActive(true);
        
        // ★ 手前に持ってくる保険コード（追加しておくと安全）
        transform.SetAsLastSibling(); 
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
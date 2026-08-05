using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToTitleButton : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "Title"; // タイトルシーン名

    public void OnBackToTitleButtonClicked()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}
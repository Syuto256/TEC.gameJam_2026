using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // 追加

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private string sceneName = "NextScene";

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(string targetScene)
    {
        SceneManager.LoadScene(targetScene);
    }

    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    private void Update()
    {
        // 新Input Systemでのキー入力
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            LoadScene();
        }
    }
}
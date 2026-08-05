using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterAnimController : MonoBehaviour
{
    private Animator animator;

    // Animator Controller側で設定したTrigger名
    private const string TRIGGER_START = "PlayStart";
    private const string TRIGGER_TRANSITION = "PlayTransition";

    void Start()
    {
        animator = GetComponent<Animator>();
        // 開始時に再生
        animator.SetTrigger(TRIGGER_START);
    }

    // シーン遷移を始めるメソッドから呼ぶ
    public void OnSceneTransitionStart()
    {
        animator.SetTrigger(TRIGGER_TRANSITION);
        // 少し待ってから実際のシーンロードを行う場合はコルーチンで
        StartCoroutine(LoadSceneAfterAnimation());
    }

    private System.Collections.IEnumerator LoadSceneAfterAnimation()
    {
        yield return new WaitForSeconds(1.0f); // アニメーションの長さに合わせる
        SceneManager.LoadScene("NextSceneName");
    }
}
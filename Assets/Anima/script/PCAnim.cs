using UnityEngine;

public class PCAnim : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        PlayAnimation();
    }

    // シーン移動が始まるタイミングでこれを呼ぶ
    public void PlayAnimation()
    {
        animator.Play("open", 0, 0f);
    }
}
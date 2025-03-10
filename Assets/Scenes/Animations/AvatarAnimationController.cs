using UnityEngine;

public class AvatarAnimationController : MonoBehaviour
{
    public AudioSource audioSource;

    private Animator animator;
    private bool isTalking = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            // IsTalking has a pointing animation at the start,
            // IsThinking would just talk, no pointing animation.
            if (isTalking)
            {
                animator.SetBool("IsTalking", true);
            }
            else
            {
                animator.SetBool("IsThinking", true);
            }
        }
        else
        {
            animator.SetBool("IsTalking", false);
            animator.SetBool("IsThinking", false);
        }
    }

    public void StartTalking()
    {
        isTalking = true;
    }

    public void StopTalking()
    {
        isTalking = false;
    }
}

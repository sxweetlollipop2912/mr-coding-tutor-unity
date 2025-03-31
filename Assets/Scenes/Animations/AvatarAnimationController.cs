using UnityEngine;

public class AvatarAnimationController : MonoBehaviour
{
    private Animator animator;
    public AudioSource audioSource;
    public bool testYapping = true;
    private bool yapping = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        yapping = testYapping;

        if (audioSource.isPlaying)
        {
            if (yapping)
            {
                animator.SetBool("IsYapping", true);
            }
            else
            {
                animator.SetBool("IsTalking", true);
            }
        }
        else
        {
            animator.SetBool("IsTalking", false);
            animator.SetBool("IsYapping", false);
        }
    }

    public void StartYapping()
    {
        yapping = true;
    }

    public void StopYapping()
    {
        yapping = false;
    }
}

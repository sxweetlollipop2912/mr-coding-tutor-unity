using UnityEngine;

public class AvatarAnimationController : MonoBehaviour
{
    private Animator animator;
    public AudioSource audioSource;
    public int animationCount = 4; // Total number of animations in the Blend Tree

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            animator.SetBool("IsTalking", true);

            // // Randomly pick an animation from the Blend Tree
            // if (!animator.GetCurrentAnimatorStateInfo(0).IsTag("Talking"))
            // {
            //     int randomIndex = Random.Range(0, animationCount);
            //     animator.SetFloat("RandomIndex", randomIndex);
            // }
        }
        else
        {
            animator.SetBool("IsTalking", false);
        }
    }
}

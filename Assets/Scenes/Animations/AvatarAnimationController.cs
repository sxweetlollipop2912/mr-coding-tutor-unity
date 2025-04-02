using System.Collections;
using UnityEngine;

public class AvatarAnimationController : MonoBehaviour
{
    private Animator animator;
    public AudioSource audioSource;
    public bool testYapping = true;
    private bool yapping = false;

    [SerializeField]
    private VRAI_TeacherHand teacherHand;

    [SerializeField]
    private float redDotDelay = 0f; // Default delay before showing red dot

    [SerializeField]
    private float redDotDuration = 5f; // Default duration for red dot visibility

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (testYapping)
        {
            yapping = true;
        }

        if (audioSource.isPlaying)
        {
            if (yapping)
            {
                animator.SetBool("IsYapping", true);
                animator.SetBool("IsTalking", false);
            }
            else
            {
                if (!animator.GetBool("IsTalking"))
                {
                    if (teacherHand != null)
                    {
                        StartCoroutine(ShowRedDotAfterDelay());
                    }
                    animator.SetBool("IsTalking", true);
                    animator.SetBool("IsYapping", false);
                }
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

    private System.Collections.IEnumerator ShowRedDotAfterDelay()
    {
        if (redDotDelay > 0)
        {
            yield return new WaitForSeconds(redDotDelay);
        }

        // Enable the red dot with the specified duration
        teacherHand.EnableRedDotWithTimeout(redDotDuration);
        Debug.Log($"[AvatarAnimationController] Enabled red dot with duration: {redDotDuration}s");
    }
}

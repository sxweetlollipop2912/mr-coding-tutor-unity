using System.Collections;
using UnityEngine;

public class AvatarAnimationController : MonoBehaviour
{
    private Animator animator;
    public AudioSource audioSource;
    public bool testYapping = true;
    private bool yapping = false;
    private bool pointing = false;

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
                animator.SetBool("IsPointing", false);
            }
            else if (pointing)
            {
                animator.SetBool("IsPointing", true);
                animator.SetBool("IsTalking", false);
                animator.SetBool("IsYapping", false);
            }
            else
            {
                if (!animator.GetBool("IsTalking"))
                {
                    if (teacherHand != null && !pointing)
                    {
                        StartCoroutine(ShowRedDotAfterDelay());
                    }
                    animator.SetBool("IsTalking", true);
                    animator.SetBool("IsYapping", false);
                    animator.SetBool("IsPointing", false);
                }
            }
        }
        else
        {
            animator.SetBool("IsTalking", false);
            animator.SetBool("IsYapping", false);
            animator.SetBool("IsPointing", false);
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

    public void StartPointing()
    {
        pointing = true;
        Debug.Log("[AvatarAnimationController] Started pointing animation");
    }

    public void StopPointing()
    {
        pointing = false;
        Debug.Log("[AvatarAnimationController] Stopped pointing animation");
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

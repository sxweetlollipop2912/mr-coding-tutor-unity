using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAnimation : StateMachineBehaviour
{
    // Total number of animations in the Blend Tree
    public int animationCount = 4; // Adjust this based on the number of animations in your Blend Tree

    // OnStateEnter is called when a transition starts, and the state machine starts to evaluate this state
    override public void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex
    )
    {
        // Generate a random index based on the number of animations
        int randomIndex = Random.Range(0, animationCount);

        // Set the RandomIndex parameter in the Animator
        animator.SetFloat("RandomIndex", randomIndex);
    }

    private int lastRandomIndex = -1; // To ensure we don't repeat the same animation

    // OnStateUpdate is called every frame while the Blend Tree is active
    override public void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex
    )
    {
        // Check if the current animation has finished
        if (stateInfo.normalizedTime % 1 >= 0.9f) // Nearly finished
        {
            // Randomly pick a new animation index, different from the last one
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, animationCount);
            } while (randomIndex == lastRandomIndex);

            // Set the new animation index in the Blend Tree
            animator.SetFloat("RandomIndex", randomIndex);

            // Store the last used random index to avoid repetition
            lastRandomIndex = randomIndex;

            // Force the playback time to start at 0 for the newly selected clip
            // (Or you can let it blend from the current point in time if you want.)
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, 0f);
        }
    }

    // OnStateUpdate is optional if you want to dynamically change animations during the state
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Additional logic to modify the RandomIndex during the state (if needed)
    //}

    // OnStateExit can be used to reset parameters when exiting the state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Optional: Reset parameters or trigger other actions when leaving the state
    //}
}

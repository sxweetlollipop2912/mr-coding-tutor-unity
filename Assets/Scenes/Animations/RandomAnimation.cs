using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAnimation : StateMachineBehaviour
{
    // Total number of animations in the Blend Tree
    public int animationCount; // Adjust this based on the number of animations in your Blend Tree

    // OnStateEnter is called when a transition starts, and the state machine starts to evaluate this state
    override public void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex
    )
    {
        int index = Random.Range(0, animationCount);
        animator.SetFloat("RandomIndex", index);
    }
}

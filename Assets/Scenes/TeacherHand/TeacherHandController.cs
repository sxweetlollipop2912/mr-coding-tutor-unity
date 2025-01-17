using UnityEngine;

public class TeacherHandController : MonoBehaviour
{
    public Transform hand; // Reference to the 3D hand model
    public Transform invisibleDot; // Reference to the invisible dot
    public Transform source; // Starting position for the hand
    public float distanceFromDot = 1.0f; // Distance between hand and dot
    public float moveSpeed = 5.0f; // Speed for smooth movement

    void Update()
    {
        var startPosition = source.position;

        // Check if the invisible dot is active
        if (!invisibleDot.gameObject.activeSelf)
        {
            // Disable the hand if the invisible dot is disabled
            hand.gameObject.SetActive(false);
            return;
        }

        // Ensure the hand is active if the invisible dot is active
        hand.gameObject.SetActive(true);

        // Calculate direction from the hand's starting position to the invisible dot
        Vector3 direction = (invisibleDot.position - startPosition).normalized;

        // Calculate target position at a fixed distance from the dot
        Vector3 targetPosition = invisibleDot.position - direction * distanceFromDot;

        // Smoothly move the hand to the target position
        hand.position = Vector3.Lerp(hand.position, targetPosition, Time.deltaTime * moveSpeed);

        // Rotate the hand to point at the invisible dot
        hand.LookAt(invisibleDot.position);
    }
}

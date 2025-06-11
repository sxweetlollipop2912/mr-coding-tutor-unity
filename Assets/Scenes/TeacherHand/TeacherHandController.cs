using UnityEngine;

public class TeacherHandController : MonoBehaviour
{
    public Transform hand; // Reference to the 3D hand model
    private Transform actualHand; // Reference to the actual hand mesh
    public Transform invisibleDot; // Reference to the invisible dot
    public Transform beam; // Reference to the beam
    public Transform source; // Starting position for the hand
    public float distanceFromDot = 1.0f; // Distance between hand and dot
    public float moveSpeed = 5.0f; // Speed for smooth movement
    private bool isHandActive; // Track if the hand is active
    private Vector3 startPosition; // Starting position for the hand

    private void Start()
    {
        // Find the actual hand object within the hand hierarchy
        actualHand = hand.Find("15797_Pointer_v1");
        if (actualHand == null)
        {
            Debug.LogError("15797_Pointer_v1 not found as a child of hand!");
            return;
        }

        // Ensure the hand starts as inactive
        actualHand.gameObject.SetActive(false);
        beam.gameObject.SetActive(false);
        isHandActive = false;

        startPosition = source.position;
        startPosition.y -= 2f;
    }

    private void Update()
    {
        // Check if the invisible dot is active
        if (!invisibleDot.gameObject.activeSelf)
        {
            // Hide the hand and update the active flag
            actualHand.gameObject.SetActive(false);
            beam.gameObject.SetActive(false);
            isHandActive = false;
            return;
        }

        // Calculate direction from the source position to the invisible dot
        Vector3 direction = (invisibleDot.position - startPosition).normalized;

        // Calculate the target position at a fixed distance from the dot
        Vector3 targetPosition = invisibleDot.position - direction * distanceFromDot;

        // If the hand was inactive, immediately snap it to the target position
        if (!isHandActive)
        {
            hand.position = targetPosition;
            beam.gameObject.SetActive(true);
            isHandActive = true;
        }
        else
        {
            // Smoothly move the hand to the target position
            hand.position = Vector3.Lerp(hand.position, targetPosition, Time.deltaTime * moveSpeed);
        }

        // Rotate the hand to point at the invisible dot
        hand.LookAt(invisibleDot.position);

        // Ensure the hand is visible
        actualHand.gameObject.SetActive(true);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class VRAI_TeacherHand : MonoBehaviour
{
    [SerializeField]
    public Transform redDot;

    [SerializeField]
    public RectTransform screen;

    // [SerializeField]
    // private float showDotDuration = 5f;

    [SerializeField]
    private AIPointerBeam pointerBeam; // Reference to the AIPointerBeam component

    private float redDotActivationTime = 0f;
    private float redDotTimeout = 0f;
    private Vector2 registeredPosition = new Vector2(-1, -1); // Initialize to invalid position

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update()
    {
        // Commented out old update logic as per request
        /*
        if (redDot.gameObject.activeSelf && (Time.time - redDotActivationTime) >= showDotDuration)
        {
            PositionRedDot(new Vector2(-1, -1));
        }
        */

        // New update logic for timeout
        // Only check for timeout if timeout value is greater than 0
        if (
            redDotTimeout > 0
            && redDot.gameObject.activeSelf
            && (Time.time - redDotActivationTime) >= redDotTimeout
        )
        {
            DisableRedDot();
        }
    }

    // Commented out as per request, but kept for reference
    /*
    void PositionRedDot(Vector2 normalizedCoordinate)
    {
        // Check for the "hide dot" condition
        if (normalizedCoordinate == new Vector2(-1, -1))
        {
            redDot.gameObject.SetActive(false);

            // Turn off the pointer beam
            if (pointerBeam != null)
            {
                pointerBeam.SetBeamVisibility(false);
            }

            Debug.Log("Hiding red dot and beam");
            return;
        }

        // Calculate the new position for the red dot
        Vector2 imageSize = screen.rect.size;
        Vector2 position = new Vector2(
            normalizedCoordinate.x * imageSize.x,
            -normalizedCoordinate.y * imageSize.y
        );

        // Check if the position has actually changed to avoid redundant updates
        RectTransform redDotTransform = redDot.GetComponent<RectTransform>();
        redDotTransform.anchoredPosition = position;

        // Ensure the red dot is visible
        redDot.gameObject.SetActive(true);
        redDotActivationTime = Time.time;

        redDot.GetComponent<Image>().color = Color.red;

        // Set up and activate the pointer beam
        if (pointerBeam != null)
        {
            pointerBeam.AnchorEnd = redDot;
            pointerBeam.SetBeamVisibility(true);
        }

        Debug.Log(
            "Showing red dot at " + position + ", normalized version: " + normalizedCoordinate
        );
    }
    */

    /// <summary>
    /// Registers the position of the red dot without making it visible
    /// </summary>
    /// <param name="normalizedCoordinate">Vector2 with x and y values between 0 and 1</param>
    public void RegisterRedDotPosition(Vector2 normalizedCoordinate)
    {
        // Calculate the position based on normalized coordinates
        Vector2 imageSize = screen.rect.size;
        Vector2 position = new Vector2(
            normalizedCoordinate.x * imageSize.x,
            -normalizedCoordinate.y * imageSize.y
        );

        // Save the position to the red dot's RectTransform
        RectTransform redDotTransform = redDot.GetComponent<RectTransform>();
        redDotTransform.anchoredPosition = position;

        // Store the position for later use
        registeredPosition = normalizedCoordinate;

        Debug.Log(
            "Registered red dot position: " + position + ", normalized: " + normalizedCoordinate
        );
    }

    /// <summary>
    /// Enables the red dot at the previously registered position for a specified timeout period
    /// </summary>
    /// <param name="timeout">How long (in seconds) the red dot should remain visible. Default is 0 (no timeout)</param>
    public void EnableRedDotWithTimeout(float timeout = 0)
    {
        // Don't enable if position is invalid (-1, -1)
        if (registeredPosition.x == -1 && registeredPosition.y == -1)
        {
            Debug.Log("Not enabling red dot - invalid coordinates (-1, -1)");
            return;
        }

        // Make sure the red dot is visible
        redDot.gameObject.SetActive(true);

        // Record activation time and timeout duration
        redDotActivationTime = Time.time;
        redDotTimeout = timeout;

        // Set up and activate the pointer beam
        if (pointerBeam != null)
        {
            pointerBeam.AnchorEnd = redDot;
            pointerBeam.SetBeamVisibility(true);
        }

        Debug.Log(
            "Enabled red dot with timeout: " + (timeout > 0 ? timeout + " seconds" : "no timeout")
        );
    }

    /// <summary>
    /// Disables the red dot and beam
    /// </summary>
    private void DisableRedDot()
    {
        redDot.gameObject.SetActive(false);

        // Turn off the pointer beam
        if (pointerBeam != null)
        {
            pointerBeam.SetBeamVisibility(false);
        }

        // Reset the registered position to invalid coordinates
        registeredPosition = new Vector2(-1, -1);

        Debug.Log("Disabled red dot and beam, reset position to (-1, -1)");
    }
}

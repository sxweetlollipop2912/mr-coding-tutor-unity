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

    [SerializeField]
    private float showDotDuration = 5f;

    [SerializeField]
    private AIPointerBeam pointerBeam; // Reference to the AIPointerBeam component

    private float redDotActivationTime = 0f;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update()
    {
        if (redDot.gameObject.activeSelf && (Time.time - redDotActivationTime) >= showDotDuration)
        {
            PositionRedDot(new Vector2(-1, -1));
        }
    }

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
}

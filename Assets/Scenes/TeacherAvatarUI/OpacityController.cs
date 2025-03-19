using UnityEngine;
using UnityEngine.UI;

public class OpacityController : MonoBehaviour
{
    // Assign these in the Inspector
    public Slider opacitySlider;
    public RawImage topImage;

    [Range(0f, 1f)]
    public float defaultOpacity = 0.5f; // Default opacity value

    [Range(0f, 1f)]
    public float minOpacity = 0f; // Minimum opacity value

    [Range(0f, 1f)]
    public float maxOpacity = 1f; // Maximum opacity value

    void Start()
    {
        // Ensure slider values respect min/max opacity settings
        opacitySlider.minValue = minOpacity;
        opacitySlider.maxValue = maxOpacity;

        // Initialize with default opacity or current alpha, whichever is appropriate
        float startingOpacity = defaultOpacity;

        // Clamp the starting opacity to ensure it's within our min/max range
        if (startingOpacity < minOpacity)
            startingOpacity = minOpacity;
        if (startingOpacity > maxOpacity)
            startingOpacity = maxOpacity;

        Debug.Log($"Starting opacity (after clamping): {startingOpacity}");

        // Update the slider value and image opacity
        opacitySlider.value = startingOpacity;
        UpdateOpacity(startingOpacity); // Apply the opacity immediately

        // Subscribe to slider changes after initializing values
        opacitySlider.onValueChanged.AddListener(UpdateOpacity);
    }

    // This function is called whenever the slider's value changes
    public void UpdateOpacity(float value)
    {
        // Clamp the value to ensure it stays within bounds
        if (value < minOpacity)
            value = minOpacity;
        if (value > maxOpacity)
            value = maxOpacity;

        Color currentColor = topImage.color;
        currentColor.a = value;
        topImage.color = currentColor;
    }
}

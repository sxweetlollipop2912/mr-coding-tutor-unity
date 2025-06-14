using UnityEngine;
using UnityEngine.UI;

public class CanvasRecordingIndicator : MonoBehaviour
{
    [Header("Recording Indicator Settings")]
    [SerializeField]
    [Tooltip("The Image component that will act as the recording border")]
    private Image borderImage;

    [SerializeField]
    [Tooltip("Color of the recording border (green with transparency)")]
    private Color recordingColor = new Color(0f, 1f, 0f, 0.5f); // Green with 50% transparency

    [SerializeField]
    [Tooltip("Thickness of the recording border in pixels")]
    private float borderThickness = 10f;

    [SerializeField]
    [Tooltip("Enable pulsing animation for the recording border")]
    private bool enablePulsingEffect = true;

    [SerializeField]
    [Tooltip("Speed of the pulsing animation")]
    private float pulseSpeed = 2f;

    [SerializeField]
    [Tooltip("Minimum alpha value during pulsing")]
    [Range(0f, 1f)]
    private float minAlpha = 0.3f;

    [SerializeField]
    [Tooltip("Maximum alpha value during pulsing")]
    [Range(0f, 1f)]
    private float maxAlpha = 0.8f;

    [SerializeField]
    [Tooltip("The target canvas to show recording border on")]
    private Canvas targetCanvas;

    [SerializeField]
    [Tooltip("Current recording state - can be toggled in inspector for testing")]
    private bool isRecording = false;

    private RectTransform canvasRectTransform;
    private float originalAlpha;

    private void Awake()
    {
        InitializeRecordingIndicator();
    }

    private void Start()
    {
        // Hide the border initially
        if (borderImage != null)
        {
            borderImage.gameObject.SetActive(false);
            Debug.Log("[CanvasRecordingIndicator] Border image found and hidden initially");
        }
        else
        {
            Debug.LogWarning("[CanvasRecordingIndicator] No border image found at start");
        }
    }

    private void Update()
    {
        if (isRecording && enablePulsingEffect && borderImage != null)
        {
            UpdatePulsingEffect();
        }
    }

    private void OnValidate()
    {
        // Handle recording state changes in the inspector
        if (Application.isPlaying && borderImage != null)
        {
            if (isRecording)
            {
                // Force activation when toggled to recording
                borderImage.gameObject.SetActive(true);
                if (borderImage.transform.parent != null)
                {
                    borderImage.transform.parent.gameObject.SetActive(true);
                }

                // Set proper color
                Color startColor = recordingColor;
                startColor.a = enablePulsingEffect ? maxAlpha : originalAlpha;
                borderImage.color = startColor;

                Debug.Log(
                    "[CanvasRecordingIndicator] Recording toggled ON via inspector - border activated"
                );
            }
            else
            {
                // Hide when toggled off
                borderImage.gameObject.SetActive(false);
                Debug.Log(
                    "[CanvasRecordingIndicator] Recording toggled OFF via inspector - border hidden"
                );
            }
        }
    }

    private void InitializeRecordingIndicator()
    {
        // If no target canvas is assigned, try to find parent canvas as fallback
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        if (targetCanvas != null)
        {
            canvasRectTransform = targetCanvas.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning(
                "[CanvasRecordingIndicator] No target canvas assigned! Please assign a canvas in the inspector."
            );
        }

        // If no border image is assigned, create one
        if (borderImage == null)
        {
            CreateBorderImage();
        }

        // Store the original alpha value
        originalAlpha = recordingColor.a;
    }

    private void CreateBorderImage()
    {
        if (targetCanvas == null)
        {
            Debug.LogError(
                "[CanvasRecordingIndicator] Cannot create border - no target canvas assigned!"
            );
            return;
        }

        // Create a new GameObject for the border
        GameObject borderObject = new GameObject("RecordingBorder");
        borderObject.transform.SetParent(targetCanvas.transform, false); // Set to canvas, not this transform

        // Explicitly set the border object active
        borderObject.SetActive(true);

        // Add Image component
        borderImage = borderObject.AddComponent<Image>();

        // Set up the RectTransform to cover the entire canvas
        RectTransform borderRect = borderImage.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        borderRect.anchoredPosition = Vector2.zero;

        // Create a simple border sprite (you can replace this with a custom sprite if needed)
        borderImage.sprite = CreateBorderSprite();
        borderImage.color = recordingColor;
        borderImage.type = Image.Type.Sliced;

        // Make sure the border is rendered on top
        borderImage.raycastTarget = false; // Don't block UI interactions
        borderObject.transform.SetAsLastSibling(); // Render on top

        Debug.Log(
            $"[CanvasRecordingIndicator] Created border image: {borderObject.name} under {targetCanvas.name}"
        );
    }

    private Sprite CreateBorderSprite()
    {
        // Create a simple border texture
        int textureSize = 100;
        Texture2D borderTexture = new Texture2D(textureSize, textureSize);

        // Fill with transparent
        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // Create border pixels
        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                // Create border effect
                bool isBorder =
                    x < borderThickness
                    || x >= textureSize - borderThickness
                    || y < borderThickness
                    || y >= textureSize - borderThickness;

                if (isBorder)
                {
                    pixels[y * textureSize + x] = Color.white;
                }
            }
        }

        borderTexture.SetPixels(pixels);
        borderTexture.Apply();

        // Create sprite with border settings
        Sprite borderSprite = Sprite.Create(
            borderTexture,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(borderThickness, borderThickness, borderThickness, borderThickness)
        );

        return borderSprite;
    }

    private void UpdatePulsingEffect()
    {
        if (borderImage == null)
            return;

        // Calculate pulsing alpha
        float pulseValue = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f; // 0 to 1
        float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulseValue);

        // Apply the alpha to the current color
        Color currentColor = borderImage.color;
        currentColor.a = currentAlpha;
        borderImage.color = currentColor;
    }

    /// <summary>
    /// Start the recording indicator effect
    /// </summary>
    public void StartRecording()
    {
        if (borderImage == null)
        {
            Debug.LogWarning("[CanvasRecordingIndicator] Border image is not assigned!");
            return;
        }

        isRecording = true;

        // Force the border object and its parent to be active
        borderImage.gameObject.SetActive(true);
        borderImage.transform.parent.gameObject.SetActive(true); // Ensure parent is also active

        // Reset color with proper alpha
        Color startColor = recordingColor;
        startColor.a = enablePulsingEffect ? maxAlpha : originalAlpha;
        borderImage.color = startColor;

        Debug.Log("[CanvasRecordingIndicator] Recording started - border visible");
    }

    /// <summary>
    /// Stop the recording indicator effect
    /// </summary>
    public void StopRecording()
    {
        if (borderImage == null)
        {
            Debug.LogWarning("[CanvasRecordingIndicator] Border image is not assigned!");
            return;
        }

        isRecording = false;
        borderImage.gameObject.SetActive(false);

        Debug.Log("[CanvasRecordingIndicator] Recording stopped - border hidden");
    }

    /// <summary>
    /// Check if currently recording
    /// </summary>
    public bool IsRecording()
    {
        return isRecording;
    }

    /// <summary>
    /// Set the recording color
    /// </summary>
    public void SetRecordingColor(Color newColor)
    {
        recordingColor = newColor;
        originalAlpha = newColor.a;

        if (borderImage != null && isRecording)
        {
            borderImage.color = recordingColor;
        }
    }

    /// <summary>
    /// Set the border thickness (requires recreating the sprite)
    /// </summary>
    public void SetBorderThickness(float thickness)
    {
        borderThickness = thickness;

        if (borderImage != null)
        {
            borderImage.sprite = CreateBorderSprite();
        }
    }

    /// <summary>
    /// Toggle pulsing effect on/off
    /// </summary>
    public void SetPulsingEffect(bool enabled)
    {
        enablePulsingEffect = enabled;

        if (!enabled && borderImage != null && isRecording)
        {
            // Reset to original alpha when disabling pulsing
            Color currentColor = borderImage.color;
            currentColor.a = originalAlpha;
            borderImage.color = currentColor;
        }
    }
}

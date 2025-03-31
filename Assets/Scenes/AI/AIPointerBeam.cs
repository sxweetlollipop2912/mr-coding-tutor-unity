using UnityEngine;

public class AIPointerBeam : MonoBehaviour
{
    [Header("Beam Anchors")]
    [Tooltip("Start anchor point (origin)")]
    [SerializeField]
    private Transform anchorStart;

    [Tooltip("End anchor point (target)")]
    [SerializeField]
    private Transform anchorEnd;

    [Header("Beam Range")]
    [Tooltip(
        "Position along the beam where visible portion starts (0 = anchor start, 1 = anchor end)"
    )]
    [Range(0f, 0.99f)]
    [SerializeField]
    private float visibleStart = 0f;

    [Tooltip(
        "Position along the beam where visible portion ends (0 = anchor start, 1 = anchor end)"
    )]
    [Range(0.01f, 1f)]
    [SerializeField]
    private float visibleEnd = 0.7f;

    [Header("Beam Appearance")]
    [Tooltip("Width at the visible start point")]
    [SerializeField]
    private float startWidth = 0.1f;

    [Tooltip("Width at the visible end point")]
    [SerializeField]
    private float endWidth = 0.02f;

    [Tooltip("Toggle beam visibility")]
    [SerializeField]
    private bool showBeam = true;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        InitializeLineRenderer();
    }

    private void OnValidate()
    {
        // Ensure visibleStart is less than visibleEnd
        if (visibleStart >= visibleEnd)
        {
            visibleStart = Mathf.Max(0f, visibleEnd - 0.01f);
        }

        // Update LineRenderer when properties change in editor
        if (lineRenderer != null)
        {
            UpdateLineRendererSettings();
        }
    }

    private void InitializeLineRenderer()
    {
        // Get or add LineRenderer component
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        UpdateLineRendererSettings();
    }

    private void UpdateLineRendererSettings()
    {
        // Configure LineRenderer
        lineRenderer.useWorldSpace = true;
    }

    private void Update()
    {
        // First check if we should show the beam at all
        if (!showBeam || anchorStart == null || anchorEnd == null)
        {
            lineRenderer.enabled = false;
            return;
        }
        else
        {
            lineRenderer.enabled = true;
        }

        UpdateBeamGeometry();
    }

    private void UpdateBeamGeometry()
    {
        // Get anchor positions
        Vector3 startPos = anchorStart.position;
        Vector3 endPos = anchorEnd.position;

        // Calculate direction and full distance
        Vector3 direction = (endPos - startPos).normalized;
        float fullDistance = Vector3.Distance(startPos, endPos);

        // Calculate positions based on the visible range parameters
        Vector3 visibleStartPos = startPos + (direction * fullDistance * visibleStart);
        Vector3 visibleEndPos = startPos + (direction * fullDistance * visibleEnd);

        // Create fading points if needed (slightly beyond visible points)
        const float fadeDistance = 0.01f;
        bool needsStartFade = visibleStart > 0f;
        bool needsEndFade = visibleEnd < 1f;

        // Count how many points we need for the beam
        int pointCount = 2; // Minimum is just visibleStart and visibleEnd
        if (needsStartFade)
            pointCount++;
        if (needsEndFade)
            pointCount++;

        // Set the number of points
        lineRenderer.positionCount = pointCount;

        // Create keyframes for the width curve
        Keyframe[] widthKeys = new Keyframe[pointCount];
        Vector3[] positions = new Vector3[pointCount];

        int index = 0;
        float keyTime = 0f;

        // Add fade-in start point if needed
        if (needsStartFade)
        {
            positions[index] = visibleStartPos - (direction * fadeDistance);
            widthKeys[index] = new Keyframe(keyTime, 0f);
            keyTime += 0.01f;
            index++;
        }

        // Add visible start point
        positions[index] = visibleStartPos;
        widthKeys[index] = new Keyframe(keyTime, startWidth);
        keyTime = needsEndFade ? 0.99f : 1f; // Position the next key near the end
        index++;

        // Add visible end point
        positions[index] = visibleEndPos;
        widthKeys[index] = new Keyframe(keyTime, endWidth);
        index++;

        // Add fade-out end point if needed
        if (needsEndFade)
        {
            keyTime = 1f;
            positions[index] = visibleEndPos + (direction * fadeDistance);
            widthKeys[index] = new Keyframe(keyTime, 0f);
        }

        // Apply positions and width curve
        for (int i = 0; i < pointCount; i++)
        {
            lineRenderer.SetPosition(i, positions[i]);
        }

        lineRenderer.widthCurve = new AnimationCurve(widthKeys);
    }

    // Public properties
    public Transform AnchorStart
    {
        get => anchorStart;
        set => anchorStart = value;
    }

    public Transform AnchorEnd
    {
        get => anchorEnd;
        set => anchorEnd = value;
    }

    public float VisibleStart
    {
        get => visibleStart;
        set
        {
            visibleStart = Mathf.Clamp(value, 0f, 0.99f);
            // Ensure visibleStart is less than visibleEnd
            if (visibleStart >= visibleEnd)
            {
                visibleStart = Mathf.Max(0f, visibleEnd - 0.01f);
            }
        }
    }

    public float VisibleEnd
    {
        get => visibleEnd;
        set
        {
            visibleEnd = Mathf.Clamp(value, 0.01f, 1f);
            // Ensure visibleEnd is greater than visibleStart
            if (visibleEnd <= visibleStart)
            {
                visibleEnd = Mathf.Min(1f, visibleStart + 0.01f);
            }
        }
    }

    public float StartWidth
    {
        get => startWidth;
        set => startWidth = Mathf.Max(0f, value);
    }

    public float EndWidth
    {
        get => endWidth;
        set => endWidth = Mathf.Max(0f, value);
    }

    public bool ShowBeam
    {
        get => showBeam;
        set => showBeam = value;
    }

    // Public methods for scripting
    public void SetBeamVisibility(bool visible)
    {
        showBeam = visible;
    }
}

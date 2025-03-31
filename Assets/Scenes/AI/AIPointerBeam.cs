using UnityEngine;

public class AIPointerBeam : MonoBehaviour
{
    [Header("Beam Endpoints")]
    [Tooltip("Origin point of the beam (typically the AI avatar position)")]
    [SerializeField]
    private Transform startPoint;

    [Tooltip("Target position that the beam points toward")]
    [SerializeField]
    private Transform endPoint;

    [Header("Beam Appearance")]
    [Tooltip("Width of the beam at its starting point")]
    [SerializeField]
    private float startWidth = 0.1f;

    [Tooltip("Width at the beam's visible end point before fading to zero")]
    [SerializeField]
    private float endWidth = 0.02f;

    [Tooltip("Toggle beam visibility")]
    [SerializeField]
    private bool showBeam = true;

    [Tooltip(
        "How far the beam extends toward the endpoint (1.0 = full distance, 0.6 = 60% of distance)"
    )]
    [Range(0.1f, 1.0f)]
    [SerializeField]
    private float beamExtent = 0.7f;

    private LineRenderer lineRenderer;

    void Awake()
    {
        InitializeLineRenderer();
    }

    void OnValidate()
    {
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
        lineRenderer.positionCount = 2; // Two points: start and end
        lineRenderer.useWorldSpace = true;
    }

    void Update()
    {
        // First check if we should show the beam at all
        if (!showBeam || startPoint == null || endPoint == null)
        {
            lineRenderer.enabled = false;
            return;
        }
        else
        {
            lineRenderer.enabled = true;
        }

        // Get positions from the transforms
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;

        // Calculate direction and full distance
        Vector3 direction = (endPos - startPos).normalized;
        float fullDistance = Vector3.Distance(startPos, endPos);

        if (beamExtent < 1.0f)
        {
            // For beams that don't reach the full distance, we need three points:
            // 1. Start point with startWidth
            // 2. Visible end point with endWidth
            // 3. Fade point with zero width (very close to visible end)

            lineRenderer.positionCount = 3;

            // Calculate the visible end position
            Vector3 visibleEndPos = startPos + (direction * fullDistance * beamExtent);

            // Set a fade point just a tiny bit further
            Vector3 fadeEndPos = visibleEndPos + (direction * 0.02f);

            // Set positions
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, visibleEndPos);
            lineRenderer.SetPosition(2, fadeEndPos);

            // We need to set individual widths for the points
            lineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0, startWidth),
                new Keyframe(0.99f, endWidth),
                new Keyframe(1, 0)
            );
        }
        else
        {
            // For beams that reach the full distance
            lineRenderer.positionCount = 2;

            // Set positions
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            // Set constant widths
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
        }
    }

    // Public getters and setters to access private fields

    public Transform StartPoint
    {
        get { return startPoint; }
        set { startPoint = value; }
    }

    public Transform EndPoint
    {
        get { return endPoint; }
        set { endPoint = value; }
    }

    public bool ShowBeam
    {
        get { return showBeam; }
        set { showBeam = value; }
    }

    public float StartWidth
    {
        get { return startWidth; }
        set { startWidth = value; }
    }

    public float EndWidth
    {
        get { return endWidth; }
        set { endWidth = value; }
    }

    public float BeamExtent
    {
        get { return beamExtent; }
        set { beamExtent = Mathf.Clamp(value, 0.1f, 1.0f); }
    }

    // Public methods for easier scripting

    public void SetBeamVisibility(bool visible)
    {
        showBeam = visible;
    }

    public void SetEndPoint(Transform target)
    {
        endPoint = target;
    }
}

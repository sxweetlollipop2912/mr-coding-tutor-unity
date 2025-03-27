using UnityEngine;

public class AIPointerBeam : MonoBehaviour
{
    public Transform startPoint; // AI avatar position (origin of the beam)
    public Transform endPoint; // Target position on canvas
    public float diameter = 0.1f; // The thickness of the beam
    public bool showBeam = true; // Toggle to show/hide the beam

    private Transform beamTransform;
    private MeshRenderer beamRenderer;

    void Awake()
    {
        beamTransform = transform;
        beamRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        // First check if we should show the beam at all
        if (!showBeam || startPoint == null || endPoint == null)
        {
            if (beamRenderer != null)
                beamRenderer.enabled = false;
            return;
        }
        else
        {
            if (beamRenderer != null)
                beamRenderer.enabled = true;
        }

        // Get positions from the transforms
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;

        // 1. Calculate Center Position
        Vector3 centerPos = (startPos + endPos) / 2f;
        beamTransform.position = centerPos;

        // 2. Calculate Length (Distance)
        float distance = Vector3.Distance(startPos, endPos);

        // 3. Calculate Orientation (Rotation)
        Vector3 direction = (endPos - startPos).normalized;

        // Ensure direction is not zero vector to avoid errors
        if (direction != Vector3.zero)
        {
            // Make the cylinder's local Y-axis point in the direction vector
            beamTransform.up = direction;
        }

        // 4. Set Scale
        // Y scale determines length. Default cylinder height is 2.
        // So, scale Y by distance / 2.
        // X and Z scale determine diameter
        beamTransform.localScale = new Vector3(diameter, distance / 2.0f, diameter);
    }

    // Public method to toggle the beam visibility
    public void SetBeamVisibility(bool visible)
    {
        showBeam = visible;
    }

    // Public method to set the endpoint
    public void SetEndPoint(Transform target)
    {
        endPoint = target;
    }

    // Public method to set the diameter
    public void SetDiameter(float newDiameter)
    {
        diameter = newDiameter;
    }
}

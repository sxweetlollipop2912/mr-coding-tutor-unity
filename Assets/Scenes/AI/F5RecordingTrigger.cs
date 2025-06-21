using UnityEngine;

public class F5RecordingTrigger : MonoBehaviour
{
    [Header("Recording Trigger Settings")]
    [SerializeField]
    [Tooltip("Drag the WhisperHandler from the scene here")]
    private WhisperHandler whisperHandler;

    [SerializeField]
    [Tooltip("Enable/disable F5 key detection")]
    private bool enableF5Trigger = true;

    [Header("Debug Info")]
    [SerializeField]
    [Tooltip("Shows the last time F5 was pressed (for debugging)")]
    private string lastF5PressTime = "Never";

    private void Start()
    {
        // Validate the whisperHandler reference
        if (whisperHandler == null)
        {
            Debug.LogWarning(
                "[F5RecordingTrigger] WhisperHandler not assigned! Please drag the WhisperHandler component to this script in the inspector."
            );
        }
        else
        {
            Debug.Log(
                "[F5RecordingTrigger] F5 recording trigger initialized successfully. Press F5 to start/stop recording."
            );
        }
    }

    private void Update()
    {
        // Check if F5 trigger is enabled and whisperHandler is assigned
        if (!enableF5Trigger || whisperHandler == null)
            return;

        // Detect F5 key press (GetKeyDown triggers once per press, not continuously)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("[F5RecordingTrigger] F5 key pressed! Triggering recording...");

            // Update debug info
            lastF5PressTime = System.DateTime.Now.ToString("HH:mm:ss");

            // Call the WhisperHandler's TriggerRecording function
            whisperHandler.TriggerRecording();
        }
    }

    /// <summary>
    /// Public method to enable/disable F5 trigger programmatically
    /// </summary>
    public void SetF5TriggerEnabled(bool enabled)
    {
        enableF5Trigger = enabled;
        Debug.Log($"[F5RecordingTrigger] F5 trigger {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Public method to check if F5 trigger is currently enabled
    /// </summary>
    public bool IsF5TriggerEnabled()
    {
        return enableF5Trigger;
    }

    /// <summary>
    /// Public method to manually trigger recording (for testing or other scripts)
    /// </summary>
    public void ManualTriggerRecording()
    {
        if (whisperHandler != null)
        {
            Debug.Log("[F5RecordingTrigger] Manual trigger activated");
            whisperHandler.TriggerRecording();
        }
        else
        {
            Debug.LogWarning(
                "[F5RecordingTrigger] Cannot manually trigger - WhisperHandler not assigned!"
            );
        }
    }

    private void OnValidate()
    {
        // This runs in the editor when values change in the inspector
        if (whisperHandler == null)
        {
            Debug.LogWarning(
                "[F5RecordingTrigger] WhisperHandler reference is missing. Please assign it in the inspector."
            );
        }
    }
}

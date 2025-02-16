using UnityEngine;

public class XRInputHandler : MonoBehaviour
{
    [SerializeField]
    private WhisperHandler whisperHandler;

    [SerializeField]
    private ChatGPTHandler chatGPTHandler;

    [SerializeField]
    private TextToSpeechHandler textToSpeechHandler;

    private void Update()
    {
        // Check if Oculus Quest A button was just pressed
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            // Start recording (Whisper)
            whisperHandler.StartRecording();
        }

        // Check if Oculus Quest A button was just released
        if (OVRInput.GetUp(OVRInput.Button.One))
        {
            // Stop recording (Whisper)
            whisperHandler.StopRecording();
        }

        // --------------------------------------------------------------------
        // Optional: More input checks (e.g. B button) to test ChatGPT or TTS.
        // --------------------------------------------------------------------

        // Example: Press B (OVRInput.Button.Two) to send a test message to ChatGPT.
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            // Just as an example:
            // Set some test text directly and send it to ChatGPT.
            chatGPTHandler.SetInputAndSend("Hello from the XR controller!");
        }

        // Example: Press X (OVRInput.Button.Three) to test TTS quickly.
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            textToSpeechHandler.SpeakText("Hello! This is a text-to-speech test.");
        }
    }
}

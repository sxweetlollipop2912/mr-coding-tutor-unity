using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class TextToSpeechHandler : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource; // AudioSource to play the TTS audio

    [SerializeField]
    private AIProgressStatus progressStatus;

    [SerializeField]
    private YappingHandler yappingHandler; // Add reference to YappingHandler

    private string ttsServerUrl;
    private string outputFilePath;

    private void Start()
    {
        LoadConfigs();
    }

    private void LoadConfigs()
    {
        var config = ConfigLoader.Instance?.ConfigData;
        if (config == null)
        {
            Debug.LogError(
                "[TextToSpeechHandler] ConfigLoader instance or configuration data is not available."
            );
            return;
        }

        ttsServerUrl = config.ttsServerUrl;
        outputFilePath = Path.Combine(Application.streamingAssetsPath, config.ttsOutputFilename);
    }

    public void SpeakText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogError("[TextToSpeechHandler] No text to speak");
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "No text to speak");
            return;
        }

        if (string.IsNullOrWhiteSpace(ttsServerUrl))
        {
            Debug.LogError("[TextToSpeechHandler] TTS not configured");
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "TTS not configured");
            return;
        }

        progressStatus.UpdateStep(AIProgressStatus.AIStep.ConvertingToSpeech);
        StartCoroutine(SendTextToTTS(text));
    }

    private IEnumerator SendTextToTTS(string text)
    {
        // Create JSON payload using a class and JsonUtility
        TTSRequest ttsRequest = new TTSRequest { text = text };
        string jsonPayload = JsonUtility.ToJson(ttsRequest);

        Debug.Log("[TextToSpeechHandler] Sending JSON payload to TTS server: " + jsonPayload);

        // Configure UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(ttsServerUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[TextToSpeechHandler] TTS audio received.");
            progressStatus.UpdateStep(AIProgressStatus.AIStep.ProcessingAudioResponse);

            // Save audio file to the configured output path
            File.WriteAllBytes(outputFilePath, request.downloadHandler.data);

            Debug.Log($"[TextToSpeechHandler] Audio saved at: {outputFilePath}");

            // Stop the yapping gracefully before playing the response
            if (yappingHandler != null)
            {
                Debug.Log(
                    "[TextToSpeechHandler] Stopping yapping gracefully before playing response"
                );
                yield return StartCoroutine(yappingHandler.StopGraceful());

                // Wait for 1 second to sound natural
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Debug.LogWarning("[TextToSpeechHandler] YappingHandler reference is missing");
            }

            progressStatus.UpdateStep(AIProgressStatus.AIStep.PlayingResponse);
            StartCoroutine(PlayAudio(outputFilePath));
        }
        else
        {
            Debug.LogError("[TextToSpeechHandler] TTS Error: " + request.error);
            Debug.LogError("[TextToSpeechHandler] Response: " + request.downloadHandler.text);
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Failed to generate speech");
        }
    }

    private IEnumerator PlayAudio(string filePath)
    {
        using (
            var request = UnityWebRequestMultimedia.GetAudioClip(
                "file://" + filePath,
                AudioType.WAV
            )
        )
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = audioClip;
                audioSource.Play();
                progressStatus.UpdateStep(AIProgressStatus.AIStep.Idle); // Clear the status when audio starts playing
            }
            else
            {
                Debug.LogError("[TextToSpeechHandler] Failed to load audio: " + request.error);
                progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Failed to play audio");
            }
        }
    }

    // Class to represent the TTS request payload
    [System.Serializable]
    private class TTSRequest
    {
        public string text;
    }
}

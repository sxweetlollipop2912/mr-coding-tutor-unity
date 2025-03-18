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

    // Audio clip property to store the current response
    private AudioClip currentAudioClip;

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
        StartCoroutine(SendTextToTTSDirect(text));
    }

    private IEnumerator SendTextToTTSDirect(string text)
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

            // Process audio directly from memory
            yield return StartCoroutine(ProcessAudioDirectly(request.downloadHandler.data));
        }
        else
        {
            Debug.LogError("[TextToSpeechHandler] TTS Error: " + request.error);
            Debug.LogError("[TextToSpeechHandler] Response: " + request.downloadHandler.text);
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Failed to generate speech");
        }
    }

    private IEnumerator ProcessAudioDirectly(byte[] audioData)
    {
        // Create a temporary URL to load the audio data
        string tempAudioUrl = "data:audio/wav;base64," + System.Convert.ToBase64String(audioData);

        using (
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
                tempAudioUrl,
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

                // Store reference for potential cleanup
                currentAudioClip = audioClip;

                progressStatus.UpdateStep(AIProgressStatus.AIStep.Idle); // Clear the status when audio starts playing
            }
            else
            {
                // Fallback for direct processing failure
                Debug.LogError(
                    "[TextToSpeechHandler] Failed to process audio directly: " + request.error
                );

                // Try alternative method using temporary file
                string tempFilePath = Path.Combine(Application.temporaryCachePath, "temp_tts.wav");
                File.WriteAllBytes(tempFilePath, audioData);

                using (
                    UnityWebRequest fallbackRequest = UnityWebRequestMultimedia.GetAudioClip(
                        "file://" + tempFilePath,
                        AudioType.WAV
                    )
                )
                {
                    yield return fallbackRequest.SendWebRequest();

                    if (fallbackRequest.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip fallbackClip = DownloadHandlerAudioClip.GetContent(
                            fallbackRequest
                        );
                        audioSource.clip = fallbackClip;
                        audioSource.Play();
                        currentAudioClip = fallbackClip;
                        progressStatus.UpdateStep(AIProgressStatus.AIStep.Idle);

                        // Clean up temporary file
                        File.Delete(tempFilePath);
                    }
                    else
                    {
                        Debug.LogError(
                            "[TextToSpeechHandler] Fallback audio loading failed: "
                                + fallbackRequest.error
                        );
                        progressStatus.UpdateStep(
                            AIProgressStatus.AIStep.Error,
                            "Failed to play audio"
                        );
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up resources
        if (currentAudioClip != null)
        {
            Destroy(currentAudioClip);
        }
    }

    // Class to represent the TTS request payload
    [System.Serializable]
    private class TTSRequest
    {
        public string text;
    }
}

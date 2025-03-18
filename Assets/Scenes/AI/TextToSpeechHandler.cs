using System;
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
        // Skip the data URI approach which causes "URI string too long" errors with larger audio files
        // Instead, always use the temporary file approach which is more reliable
        string tempFilePath = Path.Combine(
            Application.temporaryCachePath,
            "temp_tts_" + DateTime.Now.Ticks + ".wav"
        );

        // Write audio data to temporary file
        try
        {
            File.WriteAllBytes(tempFilePath, audioData);
        }
        catch (Exception ex)
        {
            Debug.LogError("[TextToSpeechHandler] Error saving audio to temp file: " + ex.Message);
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Error processing audio");
            yield break;
        }

        // Load audio from temporary file
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
            "file://" + tempFilePath,
            AudioType.WAV
        );
        yield return request.SendWebRequest();

        // Delete temporary file as soon as we've loaded it
        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[TextToSpeechHandler] Failed to delete temp file: " + ex.Message);
        }

        // Process the audio clip
        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = audioClip;
                audioSource.Play();

                // Store reference for potential cleanup
                currentAudioClip = audioClip;

                progressStatus.UpdateStep(AIProgressStatus.AIStep.Idle);
            }
            catch (Exception ex)
            {
                Debug.LogError("[TextToSpeechHandler] Error playing audio: " + ex.Message);
                progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Error playing audio");
            }
        }
        else
        {
            Debug.LogError("[TextToSpeechHandler] Failed to load audio: " + request.error);
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Failed to play audio");
        }

        // Dispose of the request
        request.Dispose();
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

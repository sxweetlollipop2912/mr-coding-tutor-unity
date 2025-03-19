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

    [SerializeField]
    private AvatarAnimationController avatarAnimator; // Reference to avatar animator
    private string voiceName = "en-US-ChristopherNeural"; // Default voice

    [SerializeField]
    private string voiceRate = "+15%"; // Default rate

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

    public void SetVoice(string voice)
    {
        if (!string.IsNullOrEmpty(voice))
        {
            voiceName = voice;
            Debug.Log($"[TextToSpeechHandler] Voice set to: {voiceName}");
        }
    }

    public void SetRate(string rate)
    {
        if (!string.IsNullOrEmpty(rate))
        {
            voiceRate = rate;
            Debug.Log($"[TextToSpeechHandler] Rate set to: {voiceRate}");
        }
    }

    public void SpeakText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogError("[TextToSpeechHandler] No text to speak");
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "No text to speak");
            yappingHandler?.Stop();
            return;
        }

        if (string.IsNullOrWhiteSpace(ttsServerUrl))
        {
            Debug.LogError("[TextToSpeechHandler] TTS not configured");
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "TTS not configured");
            yappingHandler?.Stop();
            return;
        }

        progressStatus.UpdateStep(AIProgressStatus.AIStep.ConvertingToSpeech);
        StartCoroutine(SendTextToTTSDirect(text));
    }

    private IEnumerator SendTextToTTSDirect(string text)
    {
        // Create JSON payload with voice and rate parameters
        TTSRequest ttsRequest = new TTSRequest
        {
            text = text,
            voice = voiceName,
            rate = voiceRate,
        };
        string jsonPayload = JsonUtility.ToJson(ttsRequest);

        Debug.Log("[TextToSpeechHandler] Sending JSON payload to TTS server: " + jsonPayload);

        // Configure UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(ttsServerUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Measure time for performance tracking
        float startTime = Time.realtimeSinceStartup;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            float ttsRequestTime = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[TextToSpeechHandler] TTS audio received in {ttsRequestTime:F2} seconds.");
            progressStatus.UpdateStep(AIProgressStatus.AIStep.ProcessingAudioResponse);

            // Stop the yapping gracefully before playing the response
            if (yappingHandler != null)
            {
                Debug.Log(
                    "[TextToSpeechHandler] Stopping yapping gracefully before playing response"
                );
                yield return StartCoroutine(yappingHandler.StopBlocking());

                // Wait for 0.5 second instead of 1 second for faster response
                yield return new WaitForSeconds(0.5f);
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
            yappingHandler?.Stop();
        }
    }

    private IEnumerator ProcessAudioDirectly(byte[] audioData)
    {
        // Use a unique filename for each audio to avoid caching issues
        string tempFilePath = Path.Combine(
            Application.temporaryCachePath,
            "temp_tts_" + DateTime.Now.Ticks + ".mp3"
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
            avatarAnimator?.StopTalking(); // Ensure animation stops on error
            yield break;
        }

        // Load audio from temporary file - note use of AudioType.MPEG instead of WAV
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
            "file://" + tempFilePath,
            AudioType.MPEG
        );

        float startLoadTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float loadTime = Time.realtimeSinceStartup - startLoadTime;
        Debug.Log($"[TextToSpeechHandler] Audio loaded in {loadTime:F2} seconds");

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
            AudioClip audioClip = null;

            try
            {
                audioClip = DownloadHandlerAudioClip.GetContent(request);

                // Log audio clip details for debugging performance
                Debug.Log(
                    $"[TextToSpeechHandler] Audio length: {audioClip.length:F2} seconds, "
                        + $"channels: {audioClip.channels}, frequency: {audioClip.frequency}Hz"
                );

                audioSource.clip = audioClip;

                // Stop thinking and start talking animations
                avatarAnimator?.StartTalking();

                audioSource.Play();

                // Store reference for potential cleanup
                if (currentAudioClip != null)
                {
                    Destroy(currentAudioClip);
                }

                currentAudioClip = audioClip;

                progressStatus.UpdateStep(AIProgressStatus.AIStep.Idle); // Clear the status when audio starts playing
            }
            catch (Exception ex)
            {
                Debug.LogError("[TextToSpeechHandler] Error playing audio: " + ex.Message);
                progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Error playing audio");
                avatarAnimator?.StopTalking(); // Ensure animation stops on error

                // Clean up if needed
                if (audioClip != null && audioClip != currentAudioClip)
                {
                    Destroy(audioClip);
                }

                request.Dispose();
                yield break;
            }

            // Wait for audio to finish playing in a separate try block
            try
            {
                while (audioSource.isPlaying)
                {
                    yield return null;
                }
            }
            finally
            {
                // This will always execute, even if the coroutine is interrupted
                avatarAnimator?.StopTalking();
            }
        }
        else
        {
            Debug.LogError("[TextToSpeechHandler] Failed to load audio: " + request.error);
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Failed to play audio");
            avatarAnimator?.StopTalking(); // Ensure animation stops on error
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
        public string voice;
        public string rate;
    }
}

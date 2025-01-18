using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class TextToSpeechHandler : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource; // AudioSource to play the TTS audio
    private string ttsServerUrl;
    private string outputFilePath;

    private void Start()
    {
        LoadConfigs();
    }

    private void LoadConfigs()
    {
        // Fetch the TTS server URL and output file path from the ConfigLoader
        if (ConfigLoader.Instance != null && ConfigLoader.Instance.ConfigData != null)
        {
            var config = ConfigLoader.Instance.ConfigData;

            ttsServerUrl = config.ttsServerUrl;
            outputFilePath = Path.Combine(Application.dataPath, config.ttsOutputFilename);

            if (string.IsNullOrEmpty(ttsServerUrl))
            {
                Debug.LogError("TTS server URL is missing in the configuration.");
                return;
            }

            if (string.IsNullOrEmpty(outputFilePath))
            {
                Debug.LogError("TTS output file path is missing in the configuration.");
                return;
            }

            Debug.Log("TTS Server URL loaded: " + ttsServerUrl);
            Debug.Log("TTS Output File Path loaded: " + outputFilePath);
        }
        else
        {
            Debug.LogError("ConfigLoader instance or configuration data is not available.");
        }
    }

    public void SpeakText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogError("TTS: No text provided to speak.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ttsServerUrl))
        {
            Debug.LogError("TTS: Server URL is not set.");
            return;
        }

        StartCoroutine(SendTextToTTS(text));
    }

    private IEnumerator SendTextToTTS(string text)
    {
        // Create JSON payload using a class and JsonUtility
        TTSRequest ttsRequest = new TTSRequest { text = text };
        string jsonPayload = JsonUtility.ToJson(ttsRequest);

        Debug.Log("Sending JSON payload to TTS server: " + jsonPayload);

        // Configure UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(ttsServerUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("TTS audio received.");

            // Save audio file to the configured output path
            File.WriteAllBytes(outputFilePath, request.downloadHandler.data);

            Debug.Log($"Audio saved at: {outputFilePath}");

            // Play the saved audio
            StartCoroutine(PlayAudio(outputFilePath));
        }
        else
        {
            Debug.LogError("TTS Error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
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
            }
            else
            {
                Debug.LogError("Failed to load audio: " + request.error);
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

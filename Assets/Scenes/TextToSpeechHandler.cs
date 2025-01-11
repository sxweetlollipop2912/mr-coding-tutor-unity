using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;

public class TextToSpeechHandler : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource; // AudioSource to play the TTS audio
    private string ttsServerUrl = "http://127.0.0.1:5112/tts"; // URL of the TTS server

    public void SpeakText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogError("TTS: No text provided to speak.");
            return;
        }

        StartCoroutine(SendTextToTTS(text));
    }

    private IEnumerator SendTextToTTS(string text)
    {
        // Create JSON payload manually
        string jsonPayload = "{\"text\":\"" + text.Replace("\"", "\\\"") + "\"}";

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

            // Save audio file in the Assets folder
            string filePath = Path.Combine(Application.dataPath, "tts_output.wav");
            File.WriteAllBytes(filePath, request.downloadHandler.data);

            Debug.Log($"Audio saved at: {filePath}");

            // Play the saved audio
            StartCoroutine(PlayAudio(filePath));
        }
        else
        {
            Debug.LogError("TTS Error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    private IEnumerator PlayAudio(string filePath)
    {
        using (var request = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV))
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
}

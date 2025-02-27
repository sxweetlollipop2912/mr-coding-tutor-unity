using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class WhisperHandler : MonoBehaviour
{
    private AudioClip audioClip;
    private string whisperServerUrl;
    private string outputFilePath;

    [SerializeField]
    private GPTHandler GPTHandler;

    [SerializeField]
    private DesktopDuplication desktopDuplication; // TODO: replace with DesktopDuplication

    private void Start()
    {
        LoadConfigs();
    }

    private void LoadConfigs()
    {
        var config = ConfigLoader.Instance?.ConfigData;
        if (config == null)
        {
            Debug.LogError("ConfigLoader instance or configuration data is not available.");
            return;
        }

        whisperServerUrl = config.whisperServerUrl;
        outputFilePath = Path.Combine(
            Application.streamingAssetsPath,
            config.whisperOutputFilename
        );
    }

    public void StartRecording()
    {
        // Start recording from the microphone
        audioClip = Microphone.Start(null, false, 15, 16000); // Record for 10 seconds at 16kHz
        Debug.Log("Recording started...");
    }

    public void StopRecording()
    {
        Debug.Log("Microphone.IsRecording: " + Microphone.IsRecording(null));
        if (Microphone.IsRecording(null))
        {
            // Stop recording
            Microphone.End(null);
            Debug.Log("Recording stopped.");

            // Start coroutine to send the recorded audio to Whisper
            StartCoroutine(SendAudioToWhisper());
        }
    }

    private IEnumerator SendAudioToWhisper()
    {
        // Save audio clip to WAV file in the configured output path
        SaveAudioClipToWav(audioClip, outputFilePath);

        Debug.Log("WAV file saved at: " + outputFilePath);

        // Check if the file exists before sending
        if (!File.Exists(outputFilePath))
        {
            Debug.LogError("WAV file not created or path is incorrect!");
            yield break;
        }

        // Read the audio file as bytes
        byte[] audioData = File.ReadAllBytes(outputFilePath);
        Debug.Log($"Audio file size: {audioData.Length} bytes");

        // Create a form and attach the file
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, Path.GetFileName(outputFilePath), "audio/wav");

        Debug.Log("Sending file to Whisper server...");

        // Create a UnityWebRequest to send the form
        UnityWebRequest request = UnityWebRequest.Post(whisperServerUrl, form);

        // Send the request
        yield return request.SendWebRequest();

        // Handle the server response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Whisper Response: " + request.downloadHandler.text);

            // Pass the transcription to ChatGPTHandler
            string transcription = ParseWhisperResponse(request.downloadHandler.text);
            if (!string.IsNullOrEmpty(transcription))
            {
                Debug.Log("Transcription received: " + transcription);

                // Capture screen and send to ChatGPT
                string base64Image = desktopDuplication.CaptureScreenToBase64();
                GPTHandler.SendTextAndImageToGPT(transcription, base64Image);
            }
            else
            {
                Debug.LogError("Failed to parse transcription from Whisper response.");
            }
        }
        else
        {
            Debug.LogError("Whisper Error: " + request.error);
            Debug.LogError("Response Text: " + request.downloadHandler.text);
        }
    }

    private string ParseWhisperResponse(string jsonResponse)
    {
        // Extract transcription text from Whisper response
        try
        {
            var whisperResponse = JsonUtility.FromJson<WhisperResponse>(jsonResponse);
            return whisperResponse.transcription;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to parse Whisper response: " + e.Message);
            return null;
        }
    }

    [System.Serializable]
    private class WhisperResponse
    {
        public string transcription;
    }

    private void SaveAudioClipToWav(AudioClip clip, string filePath)
    {
        // Save the audio clip to a WAV file
        using (var fileStream = CreateEmptyWav(filePath))
        {
            ConvertAndWriteWav(fileStream, clip);
            WriteWavHeader(fileStream, clip);
        }
    }

    private FileStream CreateEmptyWav(string filePath)
    {
        // Create an empty WAV file with a placeholder header
        var fileStream = new FileStream(filePath, FileMode.Create);
        for (int i = 0; i < 44; i++) // 44 bytes for the WAV header
        {
            fileStream.WriteByte(0);
        }
        return fileStream;
    }

    private void ConvertAndWriteWav(FileStream fileStream, AudioClip clip)
    {
        // Convert the audio data to WAV format
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] bytesData = new byte[samples.Length * 2];
        int offset = 0;

        foreach (float sample in samples)
        {
            short convertedSample = (short)(sample * 32767); // Scale float to short
            bytesData[offset++] = (byte)(convertedSample & 0xFF);
            bytesData[offset++] = (byte)((convertedSample >> 8) & 0xFF);
        }

        // Write the converted audio data to the file
        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    private void WriteWavHeader(FileStream fileStream, AudioClip clip)
    {
        // Write the WAV file header
        fileStream.Seek(0, SeekOrigin.Begin);

        int fileSize = (int)fileStream.Length - 8;

        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4); // Chunk ID
        fileStream.Write(System.BitConverter.GetBytes(fileSize), 0, 4); // Chunk Size
        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4); // Format
        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4); // Subchunk1 ID
        fileStream.Write(System.BitConverter.GetBytes(16), 0, 4); // Subchunk1 Size (PCM)
        fileStream.Write(System.BitConverter.GetBytes((short)1), 0, 2); // Audio Format (1 for PCM)
        fileStream.Write(System.BitConverter.GetBytes((short)clip.channels), 0, 2); // Num Channels
        fileStream.Write(System.BitConverter.GetBytes(clip.frequency), 0, 4); // Sample Rate
        fileStream.Write(System.BitConverter.GetBytes(clip.frequency * clip.channels * 2), 0, 4); // Byte Rate
        fileStream.Write(System.BitConverter.GetBytes((short)(clip.channels * 2)), 0, 2); // Block Align
        fileStream.Write(System.BitConverter.GetBytes((short)16), 0, 2); // Bits Per Sample
        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4); // Subchunk2 ID
        fileStream.Write(System.BitConverter.GetBytes(fileSize - 36), 0, 4); // Subchunk2 Size

        Debug.Log("WAV header written successfully.");
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class WhisperHandler : MonoBehaviour
{
    private AudioClip audioClip;
    private string whisperServerUrl;
    private string outputFilePath;
    private string selectedMicrophoneDevice;
    private bool isCurrentlyRecording = false;

    [SerializeField]
    private GPTHandler GPTHandler;

    [SerializeField]
    private DesktopDuplication desktopDuplication; // TODO: replace with DesktopDuplication

    [SerializeField]
    private AIProgressStatus progressStatus;

    [SerializeField]
    private TMPro.TMP_Dropdown microphoneDropdown; // Add this if you want UI selection

    [SerializeField]
    private YappingHandler yappingHandler; // Add reference to YappingHandler

    private void Start()
    {
        LoadConfigs();
        InitializeMicrophones();
        isCurrentlyRecording = false;
        if (Microphone.IsRecording(null))
        {
            Debug.LogWarning("[WhisperHandler] Found active recording on start, stopping it.");
            Microphone.End(null);
        }
    }

    private void InitializeMicrophones()
    {
        string[] devices = Microphone.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("[WhisperHandler] No microphone devices found!");
            progressStatus.UpdateLabel("Error: No microphone found");
            return;
        }

        // Print detailed information about each microphone
        Debug.Log("[WhisperHandler] === Available Microphone Devices ===");
        for (int i = 0; i < devices.Length; i++)
        {
            int minFreq,
                maxFreq;
            Microphone.GetDeviceCaps(devices[i], out minFreq, out maxFreq);

            string deviceInfo =
                $"\nDevice {i + 1}:"
                + $"\n- Name: {devices[i]}"
                + $"\n- Minimum Frequency: {(minFreq == 0 ? "Any" : minFreq.ToString() + " Hz")}"
                + $"\n- Maximum Frequency: {(maxFreq == 0 ? "Any" : maxFreq.ToString() + " Hz")}";

            Debug.Log(deviceInfo);
        }
        Debug.Log("[WhisperHandler] =================================");

        // If we have a dropdown UI, populate it
        if (microphoneDropdown != null)
        {
            microphoneDropdown.ClearOptions();
            microphoneDropdown.AddOptions(new List<string>(devices));
            microphoneDropdown.onValueChanged.AddListener(OnMicrophoneSelected);
        }

        // Set default microphone
        selectedMicrophoneDevice = devices[0];
        int defaultMinFreq,
            defaultMaxFreq;
        Microphone.GetDeviceCaps(selectedMicrophoneDevice, out defaultMinFreq, out defaultMaxFreq);
        Debug.Log(
            $"[WhisperHandler] Selected default microphone: {selectedMicrophoneDevice}"
                + $"\n- Minimum Frequency: {(defaultMinFreq == 0 ? "Any" : defaultMinFreq.ToString() + " Hz")}"
                + $"\n- Maximum Frequency: {(defaultMaxFreq == 0 ? "Any" : defaultMaxFreq.ToString() + " Hz")}"
        );
    }

    private void OnMicrophoneSelected(int index)
    {
        if (index >= 0 && index < Microphone.devices.Length)
        {
            selectedMicrophoneDevice = Microphone.devices[index];
            Debug.Log($"[WhisperHandler] Switched to microphone: {selectedMicrophoneDevice}");
        }
    }

    private void LoadConfigs()
    {
        var config = ConfigLoader.Instance?.ConfigData;
        if (config == null)
        {
            Debug.LogError(
                "[WhisperHandler] ConfigLoader instance or configuration data is not available."
            );
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
        Debug.Log(
            $"[WhisperHandler] StartRecording called. Current state:"
                + $"\n- Selected device: {selectedMicrophoneDevice}"
                + $"\n- Is currently recording (internal state): {isCurrentlyRecording}"
                + $"\n- Microphone.IsRecording: {Microphone.IsRecording(selectedMicrophoneDevice)}"
        );

        if (string.IsNullOrEmpty(selectedMicrophoneDevice))
        {
            Debug.LogError("[WhisperHandler] No microphone selected!");
            progressStatus.UpdateLabel("Error: No microphone selected");
            return;
        }

        if (isCurrentlyRecording || Microphone.IsRecording(selectedMicrophoneDevice))
        {
            Debug.LogWarning(
                $"[WhisperHandler] Already recording with device: {selectedMicrophoneDevice}"
            );
            return;
        }

        try
        {
            progressStatus.UpdateLabel($"Listening... ({selectedMicrophoneDevice})");
            audioClip = Microphone.Start(selectedMicrophoneDevice, false, 15, 16000);

            if (audioClip == null)
            {
                Debug.LogError("[WhisperHandler] Failed to create AudioClip");
                progressStatus.UpdateLabel("Error: Failed to start recording");
                isCurrentlyRecording = false;
                return;
            }

            isCurrentlyRecording = true;
            Debug.Log(
                $"[WhisperHandler] Recording started successfully:"
                    + $"\n- AudioClip: {audioClip}"
                    + $"\n- Samples: {(audioClip != null ? audioClip.samples.ToString() : "null")}"
                    + $"\n- Channels: {(audioClip != null ? audioClip.channels.ToString() : "null")}"
                    + $"\n- Frequency: {(audioClip != null ? audioClip.frequency.ToString() : "null")}"
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[WhisperHandler] Error starting recording: {e.Message}\n{e.StackTrace}"
            );
            progressStatus.UpdateLabel("Error: Failed to start recording");
            isCurrentlyRecording = false;
        }
    }

    public void StopRecording()
    {
        Debug.Log(
            $"[WhisperHandler] StopRecording called. Current state:"
                + $"\n- Selected device: {selectedMicrophoneDevice}"
                + $"\n- Is currently recording (internal state): {isCurrentlyRecording}"
                + $"\n- Microphone.IsRecording: {Microphone.IsRecording(selectedMicrophoneDevice)}"
        );

        if (!isCurrentlyRecording && !Microphone.IsRecording(selectedMicrophoneDevice))
        {
            Debug.LogWarning("[WhisperHandler] StopRecording called but no active recording found");
            return;
        }

        try
        {
            progressStatus.UpdateLabel("Processing audio...");
            Microphone.End(selectedMicrophoneDevice);
            isCurrentlyRecording = false;

            if (audioClip != null)
            {
                Debug.Log(
                    $"[WhisperHandler] Recording stopped successfully. AudioClip info:"
                        + $"\n- Samples: {audioClip.samples}"
                        + $"\n- Channels: {audioClip.channels}"
                        + $"\n- Frequency: {audioClip.frequency}"
                );

                // Start the yapping while processing the audio
                if (yappingHandler != null)
                {
                    yappingHandler.StartYapping();
                }
                else
                {
                    Debug.LogWarning("[WhisperHandler] YappingHandler reference is missing");
                }

                StartCoroutine(SendAudioToWhisper());
            }
            else
            {
                Debug.LogError("[WhisperHandler] AudioClip is null after recording");
                progressStatus.UpdateLabel("Error: No audio recorded");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[WhisperHandler] Error stopping recording: {e.Message}\n{e.StackTrace}"
            );
            progressStatus.UpdateLabel("Error: Failed to stop recording");
            isCurrentlyRecording = false;
        }
    }

    // Method to manually change microphone
    public void SetMicrophone(string deviceName)
    {
        if (Array.IndexOf(Microphone.devices, deviceName) != -1)
        {
            selectedMicrophoneDevice = deviceName;
            Debug.Log(
                $"[WhisperHandler] Manually switched to microphone: {selectedMicrophoneDevice}"
            );
        }
        else
        {
            Debug.LogError($"[WhisperHandler] Microphone device '{deviceName}' not found!");
        }
    }

    // Method to get current microphone
    public string GetCurrentMicrophone()
    {
        return selectedMicrophoneDevice;
    }

    // Method to get all available microphones
    public string[] GetAvailableMicrophones()
    {
        return Microphone.devices;
    }

    private IEnumerator SendAudioToWhisper()
    {
        progressStatus.UpdateLabel("Converting speech to text...");
        SaveAudioClipToWav(audioClip, outputFilePath);

        Debug.Log("[WhisperHandler] WAV file saved at: " + outputFilePath);

        // Check if the file exists before sending
        if (!File.Exists(outputFilePath))
        {
            Debug.LogError("[WhisperHandler] WAV file not created or path is incorrect!");
            yield break;
        }

        // Read the audio file as bytes
        byte[] audioData = File.ReadAllBytes(outputFilePath);
        Debug.Log($"[WhisperHandler] Audio file size: {audioData.Length} bytes");

        // Create a form and attach the file
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, Path.GetFileName(outputFilePath), "audio/wav");

        Debug.Log("[WhisperHandler] Sending file to Whisper server...");

        // Create a UnityWebRequest to send the form
        UnityWebRequest request = UnityWebRequest.Post(whisperServerUrl, form);

        // Send the request
        yield return request.SendWebRequest();

        // Handle the server response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[WhisperHandler] Whisper Response: " + request.downloadHandler.text);

            string transcription = ParseWhisperResponse(request.downloadHandler.text);
            if (!string.IsNullOrEmpty(transcription))
            {
                Debug.Log("[WhisperHandler] Transcription received: " + transcription);
                progressStatus.UpdateLabel("Sending to AI assistant...");
                string base64Image = desktopDuplication.CaptureScreenToBase64();
                GPTHandler.SendTextAndImageToGPT(transcription, base64Image);
            }
            else
            {
                progressStatus.UpdateLabel("Error: Failed to understand speech");
                Debug.LogError(
                    "[WhisperHandler] Failed to parse transcription from Whisper response."
                );
            }
        }
        else
        {
            progressStatus.UpdateLabel("Error: Failed to process speech");
            Debug.LogError("[WhisperHandler] Whisper Error: " + request.error);
            Debug.LogError("[WhisperHandler] Response Text: " + request.downloadHandler.text);
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

        Debug.Log("[WhisperHandler] WAV header written successfully.");
    }
}

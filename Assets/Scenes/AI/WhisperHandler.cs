using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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

    [SerializeField]
    private float minimumRecordingDuration = 1.0f; // Minimum recording duration in seconds

    // For parallel processing
    private string capturedBase64Image;
    private bool isImageCaptured = false;
    private bool isRecording = false;
    
    // Thread synchronization for TriggerRecording
    private readonly object triggerRecordingLock = new object();

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
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "No microphone found");
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

    public void TriggerRecording()
    {
        // Try to acquire the lock immediately, return if another thread is already executing this function
        if (!System.Threading.Monitor.TryEnter(triggerRecordingLock))
        {
            Debug.Log("[WhisperHandler] TriggerRecording already in progress by another thread, returning immediately");
            return;
        }

        try
        {
            if (isRecording)
            {
                isRecording = false;
                StopRecording();
            }
            else
            {
                isRecording = true;
                StartRecording();
            }
        }
        finally
        {
            // Always release the lock, even if an exception occurs
            System.Threading.Monitor.Exit(triggerRecordingLock);
        }
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
            Debug.LogError("[WhisperHandler] No microphone selected");
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "No microphone selected");
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
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Listening);
            audioClip = Microphone.Start(selectedMicrophoneDevice, false, 30, 16000);

            if (audioClip == null)
            {
                Debug.LogError("[WhisperHandler] Failed to create AudioClip");
                progressStatus.UpdateStep(
                    AIProgressStatus.AIStep.Error,
                    "Failed to start recording"
                );
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
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Failed to start recording");
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
            progressStatus.UpdateStep(AIProgressStatus.AIStep.ProcessingAudio);
            
            // Get the current recording position before ending the recording
            int recordingPosition = Microphone.GetPosition(selectedMicrophoneDevice);
            
            Microphone.End(selectedMicrophoneDevice);
            isCurrentlyRecording = false;

            if (audioClip != null)
            {
                // Calculate the actual recording duration
                float actualDuration = (float)recordingPosition / audioClip.frequency;
                
                Debug.Log(
                    $"[WhisperHandler] Recording stopped successfully. AudioClip info:"
                        + $"\n- Samples: {audioClip.samples}"
                        + $"\n- Channels: {audioClip.channels}"
                        + $"\n- Frequency: {audioClip.frequency}"
                        + $"\n- Recording Position: {recordingPosition}"
                        + $"\n- Actual Duration: {actualDuration:F2} seconds"
                        + $"\n- Minimum Required Duration: {minimumRecordingDuration:F2} seconds"
                );

                // Check if recording meets minimum duration requirement
                if (actualDuration < minimumRecordingDuration)
                {
                    Debug.LogWarning($"[WhisperHandler] Recording too short ({actualDuration:F2}s). Minimum required: {minimumRecordingDuration:F2}s");
                    progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, $"Recording too short ({actualDuration:F1}s). Minimum: {minimumRecordingDuration:F1}s");
                    return;
                }

                // Start the yapping while processing the audio
                if (yappingHandler != null)
                {
                    yappingHandler.StartYapping();
                }
                else
                {
                    Debug.LogWarning("[WhisperHandler] YappingHandler reference is missing");
                }

                // Start capturing image in parallel with audio processing
                StartCoroutine(CaptureScreenInParallel());
                StartCoroutine(SendAudioToWhisperDirect());
            }
            else
            {
                Debug.LogError("[WhisperHandler] AudioClip is null after recording");
                progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "No audio recorded");
                isCurrentlyRecording = false;
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[WhisperHandler] Error stopping recording: {e.Message}\n{e.StackTrace}"
            );
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Failed to stop recording");
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

    private IEnumerator CaptureScreenInParallel()
    {
        // Capture screen while audio is being processed
        if (desktopDuplication != null)
        {
            capturedBase64Image = desktopDuplication.CaptureScreenToBase64();
            isImageCaptured = true;
            Debug.Log("[WhisperHandler] Screen captured in parallel");
        }
        else
        {
            Debug.LogError("[WhisperHandler] DesktopDuplication reference is missing");
            isImageCaptured = false;
        }
        yield return null;
    }

    private IEnumerator SendAudioToWhisperDirect()
    {
        progressStatus.UpdateStep(AIProgressStatus.AIStep.ConvertingSpeechToText);

        // Convert AudioClip directly to WAV format in memory
        byte[] audioData = ConvertAudioClipToWav(audioClip);
        Debug.Log($"[WhisperHandler] Audio data size: {audioData.Length} bytes");

        // Create UnityWebRequest directly with the audio data
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, "audio.wav", "audio/wav");

        Debug.Log("[WhisperHandler] Sending audio data to Whisper server...");

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
                progressStatus.UpdateStep(AIProgressStatus.AIStep.SendingToAI, transcription);

                // Wait for the image capture if it's not ready yet
                if (!isImageCaptured)
                {
                    yield return new WaitUntil(() => isImageCaptured);
                }

                GPTHandler.SendTextAndImageToGPT(transcription, capturedBase64Image);

                // Reset the image captured flag
                isImageCaptured = false;
            }
            else
            {
                progressStatus.UpdateStep(
                    AIProgressStatus.AIStep.Error,
                    "Failed to understand speech"
                );
                Debug.LogError(
                    "[WhisperHandler] Failed to parse transcription from Whisper response."
                );
            }
        }
        else
        {
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Failed to process speech");
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

    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        // Create a memory stream to hold the WAV data
        using (MemoryStream stream = new MemoryStream())
        {
            // Get the audio data from the clip
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Convert to PCM
            byte[] bytesData = new byte[samples.Length * 2];
            int offset = 0;
            foreach (float sample in samples)
            {
                short convertedSample = (short)(sample * 32767);
                bytesData[offset++] = (byte)(convertedSample & 0xFF);
                bytesData[offset++] = (byte)((convertedSample >> 8) & 0xFF);
            }

            // Write WAV header
            int fileSize = bytesData.Length + 36;

            // RIFF header
            stream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
            stream.Write(BitConverter.GetBytes(fileSize), 0, 4);
            stream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);

            // fmt chunk
            stream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
            stream.Write(BitConverter.GetBytes(16), 0, 4);
            stream.Write(BitConverter.GetBytes((short)1), 0, 2);
            stream.Write(BitConverter.GetBytes((short)clip.channels), 0, 2);
            stream.Write(BitConverter.GetBytes(clip.frequency), 0, 4);
            stream.Write(BitConverter.GetBytes(clip.frequency * clip.channels * 2), 0, 4);
            stream.Write(BitConverter.GetBytes((short)(clip.channels * 2)), 0, 2);
            stream.Write(BitConverter.GetBytes((short)16), 0, 2);

            // data chunk
            stream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
            stream.Write(BitConverter.GetBytes(bytesData.Length), 0, 4);
            stream.Write(bytesData, 0, bytesData.Length);

            return stream.ToArray();
        }
    }
}

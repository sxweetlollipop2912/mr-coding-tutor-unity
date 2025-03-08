using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class YappingHandler : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField, Tooltip("Duration in seconds to wait between playing audio clips")]
    private float delayBetweenAudio = 3.0f;

    private string yappingAudioFolderPath;
    private List<string> audioFilePaths = new List<string>();
    private bool isYapping = false;
    private bool shouldStopYapping = false;
    private Coroutine yappingCoroutine;

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
                "[YappingHandler] ConfigLoader instance or configuration data is not available."
            );
            return;
        }

        yappingAudioFolderPath = config.yappingAudioFolderPath;
        LoadAudioFiles();
    }

    private void LoadAudioFiles()
    {
        if (string.IsNullOrEmpty(yappingAudioFolderPath))
        {
            Debug.LogError("[YappingHandler] Yapping audio folder path is not set.");
            return;
        }

        // Use the path directly as specified in the config
        string fullPath = yappingAudioFolderPath;

        Debug.Log($"[YappingHandler] Looking for audio files in: {fullPath}");

        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"[YappingHandler] Directory does not exist: {fullPath}");
            return;
        }

        // Support multiple audio file formats
        string[] supportedExtensions = new string[] { "*.wav", "*.mp3", "*.ogg" };
        audioFilePaths.Clear();

        foreach (string extension in supportedExtensions)
        {
            string[] files = Directory.GetFiles(fullPath, extension);
            foreach (string file in files)
            {
                audioFilePaths.Add(file);
                Debug.Log($"[YappingHandler] Found audio file: {file}");
            }
        }

        if (audioFilePaths.Count == 0)
        {
            Debug.LogWarning(
                $"[YappingHandler] No audio files found in {fullPath}. Make sure you have .wav, .mp3, or .ogg files in this directory."
            );
        }
        else
        {
            Debug.Log(
                $"[YappingHandler] Loaded {audioFilePaths.Count} audio files from {fullPath}"
            );
        }
    }

    public void StartYapping()
    {
        if (isYapping)
        {
            Debug.Log("[YappingHandler] Already yapping, ignoring start request");
            return;
        }

        if (audioFilePaths.Count == 0)
        {
            Debug.LogError("[YappingHandler] No audio files available for yapping");
            return;
        }

        isYapping = true;
        shouldStopYapping = false;
        yappingCoroutine = StartCoroutine(YappingRoutine());
    }

    public IEnumerator StopGraceful()
    {
        Debug.Log("[YappingHandler] Graceful stop requested");
        shouldStopYapping = true;

        // Wait until current audio finishes playing
        while (audioSource.isPlaying)
        {
            yield return null;
        }

        isYapping = false;
        Debug.Log("[YappingHandler] Yapping stopped gracefully");

        yield break;
    }

    private IEnumerator YappingRoutine()
    {
        Debug.Log("[YappingHandler] Starting yapping routine");

        while (isYapping && !shouldStopYapping)
        {
            // Select a random audio file
            string randomAudioFile = audioFilePaths[Random.Range(0, audioFilePaths.Count)];
            Debug.Log($"[YappingHandler] Playing audio file: {randomAudioFile}");

            // Play the audio
            yield return PlayAudio(randomAudioFile);

            // Wait between sentences if we're still supposed to be yapping
            if (isYapping && !shouldStopYapping)
            {
                Debug.Log(
                    $"[YappingHandler] Waiting {delayBetweenAudio} seconds before next phrase"
                );
                yield return new WaitForSeconds(delayBetweenAudio);
            }
        }

        Debug.Log("[YappingHandler] Yapping routine ended");
    }

    private IEnumerator PlayAudio(string filePath)
    {
        // Ensure the file exists
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[YappingHandler] Audio file does not exist: {filePath}");
            yield break;
        }

        // Determine the audio type based on file extension
        AudioType audioType = AudioType.WAV; // Default to WAV
        string extension = Path.GetExtension(filePath).ToLower();

        switch (extension)
        {
            case ".mp3":
                audioType = AudioType.MPEG;
                break;
            case ".ogg":
                audioType = AudioType.OGGVORBIS;
                break;
            case ".wav":
                audioType = AudioType.WAV;
                break;
            default:
                Debug.LogWarning(
                    $"[YappingHandler] Unsupported audio format: {extension}. Trying WAV format."
                );
                break;
        }

        // Convert to proper URI format with absolute path
        string uriPath = "file://" + Path.GetFullPath(filePath).Replace("\\", "/");
        Debug.Log($"[YappingHandler] Loading audio from URI: {uriPath} with type: {audioType}");

        UnityWebRequest request = null;
        bool requestCreationFailed = false;

        try
        {
            request = UnityWebRequestMultimedia.GetAudioClip(uriPath, audioType);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[YappingHandler] Exception creating request: {e.Message}\n{e.StackTrace}"
            );
            requestCreationFailed = true;
        }

        // Handle request creation failure outside the catch block
        if (requestCreationFailed)
        {
            yield return StartCoroutine(PlayAudioAlternative(filePath));
            yield break;
        }

        // This is outside the try block, so it's safe to yield
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AudioClip audioClip = null;
            bool audioClipCreationFailed = false;

            try
            {
                audioClip = DownloadHandlerAudioClip.GetContent(request);
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    $"[YappingHandler] Exception getting audio clip: {e.Message}\n{e.StackTrace}"
                );
                audioClipCreationFailed = true;
            }

            // Handle audio clip creation failure outside the catch block
            if (audioClipCreationFailed)
            {
                yield return StartCoroutine(PlayAudioAlternative(filePath));
                yield break;
            }

            if (audioClip == null)
            {
                Debug.LogError($"[YappingHandler] AudioClip is null after loading: {filePath}");
                yield return StartCoroutine(PlayAudioAlternative(filePath));
                yield break;
            }

            audioSource.clip = audioClip;
            audioSource.Play();

            // Wait until the audio finishes playing
            while (audioSource.isPlaying)
            {
                // If we're asked to stop, don't wait for the current clip to finish
                if (shouldStopYapping)
                {
                    break;
                }
                yield return null;
            }
        }
        else
        {
            Debug.LogError($"[YappingHandler] Failed to load audio: {request.error}");
            Debug.LogError($"[YappingHandler] Response code: {request.responseCode}");

            // Try an alternative approach for local files
            yield return StartCoroutine(PlayAudioAlternative(filePath));
        }
    }

    // Alternative method to load audio files directly
    private IEnumerator PlayAudioAlternative(string filePath)
    {
        Debug.Log($"[YappingHandler] Trying alternative method to load audio: {filePath}");

        // For WAV files, we can try to load them directly
        if (Path.GetExtension(filePath).ToLower() != ".wav")
        {
            Debug.LogError($"[YappingHandler] Alternative method only supports WAV files");
            yield break;
        }

        string tempPath = "";
        bool filePreparationFailed = false;

        try
        {
            // Read the WAV file bytes
            byte[] wavBytes = File.ReadAllBytes(filePath);

            // Create a temporary WAV file in the persistent data path
            tempPath = Path.Combine(Application.persistentDataPath, "temp_audio.wav");
            File.WriteAllBytes(tempPath, wavBytes);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[YappingHandler] Exception preparing audio file: {e.Message}");
            filePreparationFailed = true;
        }

        if (filePreparationFailed)
        {
            yield break;
        }

#pragma warning disable 0618
        WWW www = null;
        bool wwwCreationFailed = false;

        try
        {
            www = new WWW("file://" + tempPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[YappingHandler] Exception creating WWW: {e.Message}");
            wwwCreationFailed = true;
        }

        if (wwwCreationFailed)
        {
            CleanupTempFile(tempPath);
            yield break;
        }

        // This is outside the try block, so it's safe to yield
        yield return www;

        if (string.IsNullOrEmpty(www.error))
        {
            AudioClip clip = null;
            bool clipCreationFailed = false;

            try
            {
                clip = www.GetAudioClip(false, false);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[YappingHandler] Exception getting audio clip: {e.Message}");
                clipCreationFailed = true;
            }

            if (clipCreationFailed)
            {
                www.Dispose();
                CleanupTempFile(tempPath);
                yield break;
            }

            // Wait for the clip to load - this is outside any try block
            while (clip.loadState == AudioDataLoadState.Loading)
                yield return null;

            if (clip.loadState == AudioDataLoadState.Loaded)
            {
                audioSource.clip = clip;
                audioSource.Play();

                // Wait until the audio finishes playing - this is outside any try block
                while (audioSource.isPlaying)
                {
                    if (shouldStopYapping)
                        break;
                    yield return null;
                }
            }
            else
            {
                Debug.LogError(
                    $"[YappingHandler] Alternative method failed to load audio: {clip.loadState}"
                );
            }
        }
        else
        {
            Debug.LogError($"[YappingHandler] Alternative method error: {www.error}");
        }

        www.Dispose();
#pragma warning restore 0618

        CleanupTempFile(tempPath);
    }

    private void CleanupTempFile(string tempPath)
    {
        try
        {
            // Clean up the temporary file
            if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                File.Delete(tempPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[YappingHandler] Error cleaning up temp file: {e.Message}");
        }
    }
}

using System.IO;
using Newtonsoft.Json; // Make sure Newtonsoft.Json is installed via Unity Package Manager
using UnityEngine;

[System.Serializable]
public class AppConfig
{
    public string systemPromptFilename { get; set; }

    public string ffmpegPath { get; set; }

    public string imageServerUrl { get; set; }

    public string openaiApiKey { get; set; }

    public string openaiApiUrl { get; set; }

    public string ttsServerUrl { get; set; }

    public string whisperServerUrl { get; set; }

    public string ttsOutputFilename { get; set; }

    public string whisperOutputFilename { get; set; }
}

public class ConfigLoader : MonoBehaviour
{
    public static ConfigLoader Instance { get; private set; }
    public AppConfig ConfigData { get; private set; }

    private string configFileName = "config.json";

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadConfig();
    }

    private void LoadConfig()
    {
        string filePath = Path.Combine(Application.dataPath, configFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Configuration file not found: {filePath}");
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            ConfigData = JsonConvert.DeserializeObject<AppConfig>(jsonContent);

            Debug.Log("Configuration loaded successfully.");
            Debug.Log($"System Prompt Filename: {ConfigData.systemPromptFilename}");
            Debug.Log($"FFmpeg Path: {ConfigData.ffmpegPath}");
            Debug.Log($"Image Server URL: {ConfigData.imageServerUrl}");
            Debug.Log($"OpenAI API Key: {ConfigData.openaiApiKey}");
            Debug.Log($"OpenAI API URL: {ConfigData.openaiApiUrl}");
            Debug.Log($"TTS Server URL: {ConfigData.ttsServerUrl}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading configuration: {ex.Message}");
        }
    }
}

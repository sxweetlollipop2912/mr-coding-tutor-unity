using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class AppConfig
{
    public string systemPromptFilename { get; set; }
    public string openaiApiKey { get; set; }
    public string openaiApiUrl { get; set; }
    public string ttsServerUrl { get; set; }
    public string ttsOutputFilename { get; set; }
    public string whisperServerUrl { get; set; }
    public string whisperOutputFilename { get; set; }
    public string gptResponseFormatFilename { get; set; }
    public string agoraToken { get; set; } = "";
    public string agoraChannelName { get; set; } = "main";

    // Path to the folder containing yapping audio files, relative to project root
    public string yappingAudioFolderPath { get; set; }
}

public class ConfigLoader : MonoBehaviour
{
    public static ConfigLoader Instance { get; private set; }
    public AppConfig ConfigData { get; private set; }

    private string configFileName = "config.json";

    private void Awake()
    {
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
        string filePath = Path.Combine(Application.streamingAssetsPath, configFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Configuration file not found: {filePath}");
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            ConfigData = JsonConvert.DeserializeObject<AppConfig>(jsonContent);

            if (ConfigData != null)
            {
                if (ValidateConfigData(ConfigData))
                {
                    Debug.Log("Configuration loaded successfully.");
                    LogConfigData(ConfigData);
                }
                else
                {
                    ConfigData = null;
                    Debug.LogWarning("Configuration validation failed. Some values may be missing.");
                }
            }
            else
            {
                Debug.LogError("Failed to deserialize configuration data.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading configuration: {ex.Message}");
        }
    }

    private bool ValidateConfigData(AppConfig config)
    {
        bool isValid = true;

        foreach (PropertyInfo property in typeof(AppConfig).GetProperties())
        {
            if (property.PropertyType == typeof(string))
            {
                string value = (string)property.GetValue(config);
                if (string.IsNullOrEmpty(value))
                {
                    Debug.LogError($"{property.Name} is missing in the configuration.");
                    isValid = false;
                }
            }
        }

        return isValid;
    }

    private void LogConfigData(AppConfig config)
    {
        foreach (PropertyInfo property in typeof(AppConfig).GetProperties())
        {
            // Mask API keys in logs for security
            if (property.Name.Contains("ApiKey"))
            {
                Debug.Log($"{property.Name}: ****MASKED****");
            }
            else
            {
                Debug.Log($"{property.Name}: {property.GetValue(config)}");
            }
        }
    }
}

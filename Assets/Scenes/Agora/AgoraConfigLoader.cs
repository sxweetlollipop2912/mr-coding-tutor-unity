using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace io.agora.rtc.demo
{
    [Serializable]
    public class AgoraConfig
    {
        // AppID is typically not changed after deployment, so we won't include it in the external config
        public string token = "";
        public string channelName = "YOUR_CHANNEL_NAME";
    }

    public static class AgoraConfigLoader
    {
        private const string ConfigFileName = "agora_config.json";

        /// <summary>
        /// Loads the Agora configuration from a JSON file
        /// </summary>
        /// <returns>The loaded configuration or a default one if loading fails</returns>
        public static AgoraConfig LoadConfig()
        {
            try
            {
                // Get the appropriate directory based on whether we're in editor or built application
                string configPath = GetConfigFilePath();
                Debug.Log($"Looking for config file at: {configPath}");

                // Check if the file exists
                if (!File.Exists(configPath))
                {
                    Debug.LogWarning($"Config file not found at {configPath}, creating default");
                    CreateDefaultConfig(configPath);
                }

                // Read and parse the config file
                string jsonContent = File.ReadAllText(configPath);
                AgoraConfig config = JsonConvert.DeserializeObject<AgoraConfig>(jsonContent);

                if (config == null)
                {
                    Debug.LogError("Failed to parse config file");
                    return new AgoraConfig();
                }

                Debug.Log($"Successfully loaded Agora config from {configPath}");
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading Agora config: {ex.Message}");
                return new AgoraConfig();
            }
        }

        /// <summary>
        /// Gets the appropriate path for the config file based on whether we're in the editor or a built application
        /// </summary>
        private static string GetConfigFilePath()
        {
            string configPath;

            // In the Unity Editor
            #if UNITY_EDITOR
                // First try in the Agora folder
                configPath = Path.Combine(Application.dataPath, "Scenes", "Agora", ConfigFileName);
                
                // If not found there, fall back to the Assets folder
                if (!File.Exists(configPath))
                {
                    string assetsFolderPath = Path.Combine(Application.dataPath, ConfigFileName);
                    if (File.Exists(assetsFolderPath))
                    {
                        configPath = assetsFolderPath;
                    }
                }
                
                Debug.Log("Running in Unity Editor, checking Scenes/Agora folder and Assets folder for config");
            // In a built application, use the same directory as the executable
            #else
                // Get the directory where the executable is located
                string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
                configPath = Path.Combine(directoryPath, ConfigFileName);
                Debug.Log($"Running in built application, using executable directory for config: {directoryPath}");
            #endif

            return configPath;
        }

        /// <summary>
        /// Creates a default configuration file at the specified path
        /// </summary>
        private static void CreateDefaultConfig(string configPath)
        {
            try
            {
                // Make sure the directory exists
                string directory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }
                
                AgoraConfig defaultConfig = new AgoraConfig();
                string jsonContent = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                File.WriteAllText(configPath, jsonContent);
                Debug.Log($"Created default config file at {configPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create default config file: {ex.Message}");
            }
        }
    }
} 
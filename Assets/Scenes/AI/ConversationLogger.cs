using System;
using System.IO;
using UnityEngine;

public class ConversationLogger : MonoBehaviour
{
    private string logFilePath;
    private static ConversationLogger _instance;

    // Debug flag to enable/disable detailed logging
    [SerializeField]
    [Tooltip("Enable to include detailed debugging information in the logs")]
    public bool enableDetailedLogging = true;

    // Whether to use persistentDataPath instead of streamingAssetsPath
    [SerializeField]
    [Tooltip(
        "Enable to save logs to persistentDataPath (recommended for builds, especially on Windows)"
    )]
    public bool usePersistentDataPath = false;

    public static ConversationLogger Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject loggerObject = new GameObject("ConversationLogger");
                _instance = loggerObject.AddComponent<ConversationLogger>();
                DontDestroyOnLoad(loggerObject);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[ConversationLogger] Awake called - initializing logger");
        InitializeLogger();
    }

    private void InitializeLogger()
    {
        try
        {
            // Create timestamp-based filename
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"conversation_{timestamp}.txt";

            // Choose base directory based on setting
            string baseDirectory = usePersistentDataPath
                ? Application.persistentDataPath
                : Application.streamingAssetsPath;

            Debug.Log($"[ConversationLogger] Using base directory: {baseDirectory}");

            // Make sure Logs directory exists
            string logsDirectory = Path.Combine(baseDirectory, "Logs");
            Debug.Log($"[ConversationLogger] Logs directory path: {logsDirectory}");

            try
            {
                if (!Directory.Exists(logsDirectory))
                {
                    Debug.Log($"[ConversationLogger] Creating directory: {logsDirectory}");
                    Directory.CreateDirectory(logsDirectory);
                }
            }
            catch (Exception dirEx)
            {
                Debug.LogError(
                    $"[ConversationLogger] Failed to create logs directory: {dirEx.Message}\n{dirEx.StackTrace}"
                );

                // Fallback to persistentDataPath if we failed with StreamingAssets
                if (!usePersistentDataPath)
                {
                    Debug.Log("[ConversationLogger] Falling back to persistentDataPath");
                    usePersistentDataPath = true;
                    baseDirectory = Application.persistentDataPath;
                    logsDirectory = Path.Combine(baseDirectory, "Logs");

                    if (!Directory.Exists(logsDirectory))
                    {
                        Directory.CreateDirectory(logsDirectory);
                    }
                }
            }

            // Set log file path
            logFilePath = Path.Combine(logsDirectory, fileName);
            Debug.Log($"[ConversationLogger] Log file path: {logFilePath}");

            // Create the file with header
            try
            {
                File.WriteAllText(logFilePath, "=== CONVERSATION LOG ===\n");
                File.AppendAllText(logFilePath, $"Session started: {DateTime.Now}\n");

                // Add debugging information about paths and environment
                if (enableDetailedLogging)
                {
                    File.AppendAllText(logFilePath, "\n=== DEBUG INFO ===\n");
                    File.AppendAllText(logFilePath, $"Application path: {Application.dataPath}\n");
                    File.AppendAllText(
                        logFilePath,
                        $"StreamingAssets path: {Application.streamingAssetsPath}\n"
                    );
                    File.AppendAllText(
                        logFilePath,
                        $"PersistentData path: {Application.persistentDataPath}\n"
                    );
                    File.AppendAllText(logFilePath, $"Log file: {logFilePath}\n");
                    File.AppendAllText(logFilePath, $"Platform: {Application.platform}\n");
                    File.AppendAllText(logFilePath, $"Unity version: {Application.unityVersion}\n");
                    File.AppendAllText(logFilePath, $"System OS: {SystemInfo.operatingSystem}\n");
                    File.AppendAllText(logFilePath, "=== END DEBUG INFO ===\n");
                }

                File.AppendAllText(logFilePath, "\n=== CONVERSATION ===\n\n");
                Debug.Log(
                    $"[ConversationLogger] Successfully initialized log file at: {logFilePath}"
                );
            }
            catch (Exception fileEx)
            {
                Debug.LogError(
                    $"[ConversationLogger] Failed to create log file: {fileEx.Message}\n{fileEx.StackTrace}"
                );

                // If we're already using persistentDataPath and still failing, try desktop as last resort
                if (usePersistentDataPath)
                {
                    Debug.Log("[ConversationLogger] Trying desktop as last resort");
                    string desktopPath = Environment.GetFolderPath(
                        Environment.SpecialFolder.Desktop
                    );
                    logsDirectory = Path.Combine(desktopPath, "MrCodingTutor_Logs");

                    if (!Directory.Exists(logsDirectory))
                    {
                        Directory.CreateDirectory(logsDirectory);
                    }

                    logFilePath = Path.Combine(logsDirectory, fileName);
                    Debug.Log($"[ConversationLogger] Trying desktop location: {logFilePath}");

                    // Final attempt - write to desktop
                    File.WriteAllText(logFilePath, "=== CONVERSATION LOG ===\n");
                    Debug.Log(
                        $"[ConversationLogger] Successfully created log on desktop: {logFilePath}"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            // Catch-all for any unexpected exceptions
            Debug.LogError(
                $"[ConversationLogger] Unhandled exception in InitializeLogger: {ex.Message}\n{ex.StackTrace}"
            );
        }
    }

    public void LogUserMessage(string message)
    {
        if (string.IsNullOrEmpty(logFilePath) || string.IsNullOrEmpty(message))
        {
            Debug.LogWarning(
                "[ConversationLogger] Cannot log user message - logFilePath is empty or message is null"
            );
            return;
        }

        try
        {
            string formattedMessage = $"[{DateTime.Now.ToString("HH:mm:ss")}] USER: {message}\n\n";
            File.AppendAllText(logFilePath, formattedMessage);
            Debug.Log(
                $"[ConversationLogger] Logged user message: {message.Substring(0, Math.Min(50, message.Length))}..."
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[ConversationLogger] Failed to log user message: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    public void LogAIResponse(string voiceResponse, string textSummary)
    {
        if (string.IsNullOrEmpty(logFilePath))
        {
            Debug.LogWarning("[ConversationLogger] Cannot log AI response - logFilePath is empty");
            return;
        }

        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{timestamp}] AI:\n";

            if (!string.IsNullOrEmpty(voiceResponse))
            {
                formattedMessage += $"Voice response: {voiceResponse}\n";
            }

            if (!string.IsNullOrEmpty(textSummary))
            {
                formattedMessage += $"Text summary: {textSummary}\n";
            }

            formattedMessage += "\n";
            File.AppendAllText(logFilePath, formattedMessage);
            Debug.Log($"[ConversationLogger] Logged AI response successfully");
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[ConversationLogger] Failed to log AI response: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    // New method for debugging raw data
    public void LogDebugInfo(string debugInfo)
    {
        if (!enableDetailedLogging || string.IsNullOrEmpty(logFilePath))
            return;

        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"\n[{timestamp}] DEBUG INFO:\n{debugInfo}\n\n";
            File.AppendAllText(logFilePath, formattedMessage);
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[ConversationLogger] Failed to log debug info: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    // Method to log raw JSON payloads for debugging
    public void LogRawJson(string label, string jsonData)
    {
        if (!enableDetailedLogging || string.IsNullOrEmpty(logFilePath))
            return;

        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"\n[{timestamp}] {label} JSON:\n{jsonData}\n\n";
            File.AppendAllText(logFilePath, formattedMessage);
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[ConversationLogger] Failed to log JSON data: {e.Message}\n{e.StackTrace}"
            );
        }
    }

    public string GetLogFilePath()
    {
        return logFilePath;
    }
}

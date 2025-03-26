using System;
using System.IO;
using UnityEngine;

public class ConversationLogger : MonoBehaviour
{
    private string logFilePath;
    private static ConversationLogger _instance;

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
        InitializeLogger();
    }

    private void InitializeLogger()
    {
        // Create timestamp-based filename
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"conversation_{timestamp}.txt";

        // Make sure StreamingAssets/Logs directory exists
        string logsDirectory = Path.Combine(Application.streamingAssetsPath, "Logs");
        if (!Directory.Exists(logsDirectory))
        {
            Directory.CreateDirectory(logsDirectory);
        }

        // Set log file path
        logFilePath = Path.Combine(logsDirectory, fileName);

        // Create the file with header
        try
        {
            File.WriteAllText(logFilePath, "=== CONVERSATION LOG ===\n");
            File.AppendAllText(logFilePath, $"Session started: {DateTime.Now}\n\n");
            Debug.Log($"[ConversationLogger] Initialized log file at: {logFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConversationLogger] Failed to create log file: {e.Message}");
        }
    }

    public void LogUserMessage(string message)
    {
        if (string.IsNullOrEmpty(logFilePath))
            return;

        try
        {
            string formattedMessage = $"[{DateTime.Now.ToString("HH:mm:ss")}] USER: {message}\n\n";
            File.AppendAllText(logFilePath, formattedMessage);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConversationLogger] Failed to log user message: {e.Message}");
        }
    }

    public void LogAIResponse(string voiceResponse, string textSummary)
    {
        if (string.IsNullOrEmpty(logFilePath))
            return;

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
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConversationLogger] Failed to log AI response: {e.Message}");
        }
    }
}

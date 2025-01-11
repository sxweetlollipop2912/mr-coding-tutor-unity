using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // For TextMeshPro UI elements
using Newtonsoft.Json; // For JSON serialization and deserialization

public class ChatGPTHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField userInputField; // Input field for user messages
    [SerializeField] private TMP_Text responseText;         // Text field to display GPT's response
    private string apiKey;

    [SerializeField] private TextToSpeechHandler textToSpeechHandler; // Reference to TTS handler

    private void Start()
    {
        // Path to the configuration file in the Assets folder
        string configPath = Application.dataPath + "/config.json";

        if (System.IO.File.Exists(configPath))
        {
            string json = System.IO.File.ReadAllText(configPath);
            Config config = JsonUtility.FromJson<Config>(json);
            apiKey = config.openai_api_key;

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("API Key is missing in the config file.");
            }
            else
            {
                Debug.Log("API Key successfully loaded from config.json in Assets folder.");
            }
        }
        else
        {
            Debug.LogError("Config file not found in Assets folder. Please create a config.json file.");
        }
    }

    [System.Serializable]
    private class Config
    {
        public string openai_api_key;
    }

    // Triggered by the user manually
    public void SendMessageToChatGPT()
    {
        // Get user input
        string userMessage = userInputField.text;

        if (string.IsNullOrEmpty(userMessage))
        {
            responseText.text = "Please enter a message!";
            return;
        }

        // Start coroutine to send API request
        StartCoroutine(SendPostRequest(userMessage));
    }

    // Called from WhisperIntegration with the transcribed text
    public void SetInputAndSend(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            userInputField.text = message; // Set the transcription as input
            SendMessageToChatGPT(); // Send the message to ChatGPT
        }
        else
        {
            Debug.LogError("Received empty transcription from Whisper.");
        }
    }

    private IEnumerator SendPostRequest(string prompt)
    {
        string url = "https://api.openai.com/v1/chat/completions";

        // Construct the JSON payload for the GPT API
        var payload = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "You are a helpful AI tutor specializing in Python programming." },
                new { role = "user", content = prompt }
            }
        };

        string jsonPayload = JsonConvert.SerializeObject(payload);

        // Configure the UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        // Send the request
        yield return request.SendWebRequest();

        // Handle the response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response received: " + request.downloadHandler.text);

            // Parse and display the response
            string responseContent = ParseResponse(request.downloadHandler.text);
            responseText.text = responseContent;

            // Speak the response using TTS
            textToSpeechHandler.SpeakText(responseContent);
        }
        else
        {
            // Log error details
            Debug.LogError("Request error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);

            // Show error message to the user
            responseText.text = "Error talking to GPT. Please try again.";
        }
    }

    private string ParseResponse(string jsonResponse)
    {
        try
        {
            // Parse the response JSON
            ChatGPTResponse response = JsonConvert.DeserializeObject<ChatGPTResponse>(jsonResponse);

            if (response.choices != null && response.choices.Length > 0)
            {
                return response.choices[0].message.content.Trim();
            }
            else
            {
                return "Unexpected response format.";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to parse response: " + ex.Message);
            return "Error parsing GPT response.";
        }
    }

    // Class for deserializing ChatGPT's response
    [System.Serializable]
    private class ChatGPTResponse
    {
        public Choice[] choices;

        [System.Serializable]
        public class Choice
        {
            public Message message;

            [System.Serializable]
            public class Message
            {
                public string content;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json; // For JSON serialization and deserialization
using TMPro; // For TextMeshPro UI elements
using UnityEngine;
using UnityEngine.Networking;

public class ChatGPTHandler : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField userInputField; // Input field for user messages

    [SerializeField]
    private TMP_Text responseText; // Text field to display GPT's response

    [SerializeField]
    private TextToSpeechHandler textToSpeechHandler; // Reference to TTS handler

    private string apiKey;
    private List<Message> conversationHistory = new List<Message>(); // Holds stateful conversation history

    private void Start()
    {
        // Path to the configuration file in the Assets folder
        string configPath = Application.dataPath + "/config.json";

        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            Config config = JsonUtility.FromJson<Config>(json);
            apiKey = config.openai_api_key;

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("API Key is missing in the config file.");
            }

            // Load the system prompt from the file specified in config
            string systemPromptPath = Path.Combine(Application.dataPath, config.system_prompt_file);
            Debug.Log("Loading system prompt from: " + systemPromptPath);
            if (File.Exists(systemPromptPath))
            {
                string systemPrompt = File.ReadAllText(systemPromptPath);

                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    Debug.Log("System prompt successfully loaded from: " + systemPromptPath);

                    // Add the system prompt as the first message in the conversation history
                    if (conversationHistory.Count == 0)
                    {
                        conversationHistory.Add(
                            new Message { role = "system", content = systemPrompt }
                        );
                        Debug.Log("System prompt added to conversation history: " + systemPrompt);
                    }
                }
                else
                {
                    Debug.LogError("System prompt file is empty.");
                }
            }
            else
            {
                Debug.LogError("System prompt file not found: " + systemPromptPath);
            }
        }
        else
        {
            Debug.LogError(
                "Config file not found in Assets folder. Please create a config.json file."
            );
        }
    }

    [System.Serializable]
    private class Config
    {
        public string openai_api_key;
        public string system_prompt_file; // Path to the system prompt file
    }

    // Triggered by the user manually
    public void SendMessageToChatGPT()
    {
        string userMessage = userInputField.text;

        if (string.IsNullOrEmpty(userMessage))
        {
            responseText.text = "Please enter a message!";
            return;
        }

        // Add user's message to conversation history
        conversationHistory.Add(new Message { role = "user", content = userMessage });

        // Start coroutine to send the conversation to ChatGPT
        StartCoroutine(SendPostRequest());
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

    private IEnumerator SendPostRequest()
    {
        string url = "https://api.openai.com/v1/chat/completions";

        // Construct the JSON payload for the GPT API
        var payload = new
        {
            model = "gpt-4",
            messages = conversationHistory, // Include the full conversation history
        };

        string jsonPayload = JsonConvert.SerializeObject(payload);

        // Log the payload being sent to GPT for debugging
        Debug.Log("Request to GPT: " + jsonPayload);

        // Configure the UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        // Send the request
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response received: " + request.downloadHandler.text);

            // Parse and display the response
            string responseContent = ParseResponse(request.downloadHandler.text);
            responseText.text = responseContent;

            // Add ChatGPT's response to the conversation history
            conversationHistory.Add(new Message { role = "assistant", content = responseContent });

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

    [System.Serializable]
    private class Message
    {
        public string role;
        public string content;
    }
}

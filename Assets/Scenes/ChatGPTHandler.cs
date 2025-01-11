using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // For TextMeshPro UI elements
using Newtonsoft.Json; // Install Newtonsoft.Json via Unity Package Manager if not already included

public class ChatGPTHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField userInputField; // Input from user
    [SerializeField] private TMP_Text responseText;         // Response display field
    private string apiKey;

    private void Start()
    {
        // Path to config.json in the Assets folder
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

    private IEnumerator SendPostRequest(string prompt)
    {
        string url = "https://api.openai.com/v1/chat/completions";

        // Construct the JSON payload using Newtonsoft.Json
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

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response received: " + request.downloadHandler.text);

            // Parse and display the response
            string responseContent = ParseResponse(request.downloadHandler.text);
            responseText.text = responseContent;
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

    // Class to parse ChatGPT response
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

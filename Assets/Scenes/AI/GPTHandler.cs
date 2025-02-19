using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReadyPlayerMe.Core.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class GPTHandler : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField userInputField;

    [SerializeField]
    private TMP_Text responseText;

    [SerializeField]
    private TextToSpeechHandler textToSpeechHandler;

    private string openaiApiKey;
    private string systemPrompt;
    private string openaiApiUrl;
    private List<ChatMessage> conversationHistory = new List<ChatMessage>();

    // Constants for GPT parameters
    private const float TEMPERATURE = 1f;
    private const int MAX_COMPLETION_TOKENS = 1024;
    private const float TOP_P = 1f;
    private const float FREQUENCY_PENALTY = 0f;
    private const float PRESENCE_PENALTY = 0f;
    private const string MODEL = "gpt-4o";

    private bool configsLoaded = false;
    public bool ConfigsLoaded => configsLoaded;

    private string responseFormatJson; // Store the loaded response format as JSON

    private void Start()
    {
        LoadConfigs();
    }

    private void LoadConfigs()
    {
        var config = ConfigLoader.Instance?.ConfigData;
        if (config == null)
        {
            Debug.LogError("ConfigLoader instance or configuration data is not available.");
            return;
        }

        openaiApiKey = config.openaiApiKey;
        openaiApiUrl = config.openaiApiUrl;
        systemPrompt = LoadSystemPrompt(config.systemPromptFilename);
        responseFormatJson = LoadResponseFormatJson(config.gptResponseFormatFilename);

        Debug.Log("API Key: " + openaiApiKey);
        Debug.Log("API URL: " + openaiApiUrl);
        Debug.Log("System prompt loaded: " + systemPrompt);
        Debug.Log("Response format loaded: " + responseFormatJson);

        configsLoaded = true;
    }

    private string LoadSystemPrompt(string systemPromptFilename)
    {
        string systemPromptPath = Path.Combine(
            Application.streamingAssetsPath,
            systemPromptFilename
        );

        if (!File.Exists(systemPromptPath))
        {
            Debug.LogError("System prompt file not found: " + systemPromptPath);
            return null;
        }

        return File.ReadAllText(systemPromptPath);
    }

    private string LoadResponseFormatJson(string responseFormatFilename)
    {
        string responseFormatPath = Path.Combine(
            Application.streamingAssetsPath,
            responseFormatFilename
        );

        if (!File.Exists(responseFormatPath))
        {
            Debug.LogError("Response format file not found: " + responseFormatPath);
            return null;
        }

        return File.ReadAllText(responseFormatPath);
    }

    public void SendInputFieldContentToGPT()
    {
        string userMessage = userInputField.text;

        if (string.IsNullOrEmpty(userMessage))
        {
            responseText.text = "Please enter a message!";
            return;
        }

        // Add user's message to conversation history
        AddUserMessageToConversation(userMessage);

        // Start coroutine to send the conversation to ChatGPT
        StartCoroutine(SendPostRequest(userMessage, null));
    }

    public void SendTextAndImageToGPT(string message, string base64Image)
    {
        if (!string.IsNullOrEmpty(message))
        {
            userInputField.text = message;

            // Add user's message with image to conversation history
            AddUserMessageToConversation(message, base64Image);

            // Start coroutine to send the conversation to ChatGPT
            StartCoroutine(SendPostRequest(message, base64Image));
        }
        else
        {
            Debug.LogError("Received empty transcription from Whisper.");
        }
    }

    private void AddUserMessageToConversation(string message, string base64Image = null)
    {
        List<ContentItem> content = new List<ContentItem>
        {
            new ContentItem { type = "text", text = message },
        };

        if (!string.IsNullOrEmpty(base64Image))
        {
            content.Add(
                new ContentItem
                {
                    type = "image_url",
                    image_url = new ImageUrl { url = $"data:image/png;base64,{base64Image}" },
                }
            );
        }

        conversationHistory.Add(new ChatMessage { role = "user", content = content });
    }

    private IEnumerator SendPostRequest(string message = null, string base64Image = null)
    {
        if (string.IsNullOrEmpty(openaiApiKey) || string.IsNullOrEmpty(openaiApiUrl))
        {
            Debug.LogError("API Key or API URL is not set.");
            yield break;
        }

        List<object> messages = new List<object>();

        foreach (var chatMessage in conversationHistory)
        {
            List<object> contentList = new List<object>();
            foreach (var contentItem in chatMessage.content)
            {
                if (contentItem.type == "text")
                {
                    contentList.Add(
                        new Dictionary<string, string>
                        {
                            { "type", contentItem.type },
                            { "text", contentItem.text },
                        }
                    );
                }
                else if (contentItem.type == "image_url" && contentItem.image_url != null)
                {
                    contentList.Add(
                        new Dictionary<string, object>
                        {
                            { "type", contentItem.type },
                            {
                                "image_url",
                                new Dictionary<string, string>
                                {
                                    { "url", contentItem.image_url.url },
                                }
                            },
                        }
                    );
                }
            }

            messages.Add(
                new Dictionary<string, object>
                {
                    { "role", chatMessage.role },
                    { "content", contentList },
                }
            );
        }

        // Construct the payload using the defined structs
        var payload = new ChatRequest
        {
            model = MODEL,
            messages = messages.ToArray(),
            response_format = new ResponseFormat
            {
                type = "json_schema",
                json_schema = JObject.Parse(responseFormatJson),
            },
            temperature = TEMPERATURE,
            max_completion_tokens = MAX_COMPLETION_TOKENS,
            top_p = TOP_P,
            frequency_penalty = FREQUENCY_PENALTY,
            presence_penalty = PRESENCE_PENALTY,
        };

        string jsonPayload = JsonConvert.SerializeObject(
            payload,
            Formatting.Indented,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
        );

        Debug.Log("Request to GPT: " + jsonPayload);
        // Also log the JSON payload to a file
        File.WriteAllText(
            Path.Combine(Application.streamingAssetsPath, "gpt_request.json"),
            jsonPayload
        );

        UnityWebRequest request = new UnityWebRequest(openaiApiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {openaiApiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response received: " + request.downloadHandler.text);

            // Parse the response to the TutorResponseSchema
            TutorResponseSchema parsedResponse = ParseResponse(request.downloadHandler.text);

            // Update the UI with the parsed response
            responseText.text = parsedResponse.text_summary;
            textToSpeechHandler.SpeakText(parsedResponse.voice_response);
        }
        else
        {
            Debug.LogError("Request error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);

            responseText.text = "Error talking to GPT. Please try again.";
        }
    }

    private TutorResponseSchema ParseResponse(string jsonResponse)
    {
        try
        {
            // Deserialize the entire response
            ChatGPTResponse response = JsonConvert.DeserializeObject<ChatGPTResponse>(jsonResponse);

            // Extract the content string
            string contentString = response.choices[0].message.content;

            // Deserialize the content string into TutorResponseSchema
            TutorResponseSchema parsedResponse = JsonConvert.DeserializeObject<TutorResponseSchema>(
                contentString
            );

            return parsedResponse;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse response: " + ex.Message);
            return null;
        }
    }

    [System.Serializable]
    public class ChatGPTResponse
    {
        public Choice[] choices;

        [System.Serializable]
        public class Choice
        {
            public Message message;
        }
    }

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class ChatMessage
    {
        public string role;
        public List<ContentItem> content;
    }

    [System.Serializable]
    public class ContentItem
    {
        public string type;
        public string text;
        public ImageUrl image_url;
    }

    [System.Serializable]
    public class ImageUrl
    {
        public string url;
    }

    // --- Request Payload Classes ---
    [System.Serializable]
    public class ChatRequest
    {
        public string model;
        public object[] messages;
        public ResponseFormat response_format;

        public float temperature;
        public int max_completion_tokens;
        public float top_p;
        public float frequency_penalty;
        public float presence_penalty;
    }

    [System.Serializable]
    public class ResponseFormat
    {
        public string type;
        public JObject json_schema;
    }

    [System.Serializable]
    public class TutorResponseSchema
    {
        public PointedAtProperty pointed_at;
        public string voice_response;
        public string text_summary;
    }

    [System.Serializable]
    public class PointedAtProperty
    {
        public string type;
        public PointedAtProperties properties;
        public string[] required;
        public bool additionalProperties;
    }

    [System.Serializable]
    public class PointedAtProperties
    {
        public PartProperty part;
        public CoordinatesProperty coordinates;
    }

    [System.Serializable]
    public class PartProperty
    {
        public string type;
        public string description;
        public string[] @enum;
    }

    [System.Serializable]
    public class CoordinatesProperty
    {
        public NumberProperty x;
        public NumberProperty y;
    }

    [System.Serializable]
    public class CoordinateProperties
    {
        public NumberProperty x;
        public NumberProperty y;
    }

    [System.Serializable]
    public class NumberProperty
    {
        public string type;
        public string description;
    }

    [System.Serializable]
    public class VoiceResponseProperty
    {
        public string type;
        public string description;
    }

    [System.Serializable]
    public class TextSummaryProperty
    {
        public string type;
        public string description;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GPTHandler : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField userInputField;

    [SerializeField]
    private TMP_Text responseText;

    [SerializeField]
    private TextToSpeechHandler textToSpeechHandler;

    [SerializeField]
    private AIProgressStatus progressStatus;

    [SerializeField]
    private VRAI_TeacherHand teacherHand;

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
            Debug.LogError(
                "[GPTHandler] ConfigLoader instance or configuration data is not available."
            );
            return;
        }

        openaiApiKey = config.openaiApiKey;
        openaiApiUrl = config.openaiApiUrl;
        systemPrompt = LoadSystemPrompt(config.systemPromptFilename);
        responseFormatJson = LoadResponseFormatJson(config.gptResponseFormatFilename);

        Debug.Log("[GPTHandler] API Key: " + openaiApiKey);
        Debug.Log("[GPTHandler] API URL: " + openaiApiUrl);
        Debug.Log("[GPTHandler] System prompt loaded: " + systemPrompt);
        Debug.Log("[GPTHandler] Response format loaded: " + responseFormatJson);

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
            Debug.LogError("[GPTHandler] System prompt file not found: " + systemPromptPath);
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
            Debug.LogError("[GPTHandler] Response format file not found: " + responseFormatPath);
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
            progressStatus.UpdateLabel("Error: Empty message");
            return;
        }

        progressStatus.UpdateLabel("Sending to AI assistant...");
        AddUserMessageToConversation(userMessage);
        StartCoroutine(SendPostRequest(userMessage, null));
    }

    public void SendTextAndImageToGPT(string message, string base64Image)
    {
        if (!string.IsNullOrEmpty(message))
        {
            if (userInputField != null)
            {
                userInputField.text = message;
            }

            progressStatus.UpdateLabel("Processing with AI assistant...");
            AddUserMessageToConversation(message, base64Image);
            StartCoroutine(SendPostRequest(message, base64Image));
        }
        else
        {
            progressStatus.UpdateLabel("Error: Empty message");
            Debug.LogError("[GPTHandler] Received empty transcription from Whisper.");
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
            progressStatus.UpdateLabel("Error: API configuration missing");
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

        Debug.Log("[GPTHandler] Request to GPT: " + jsonPayload);
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
            Debug.Log("[GPTHandler] Response received: " + request.downloadHandler.text);

            progressStatus.UpdateLabel("Processing AI response...");
            TutorResponseSchema parsedResponse = ParseResponse(request.downloadHandler.text);

            if (parsedResponse != null)
            {
                responseText.text = parsedResponse.text_summary;
                progressStatus.UpdateLabel("Converting response to speech...");
                textToSpeechHandler.SpeakText(parsedResponse.voice_response);
            }
            else
            {
                progressStatus.UpdateLabel("Error: Failed to process AI response");
                responseText.text = "Error processing AI response. Please try again.";
            }
        }
        else
        {
            Debug.LogError("Request error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);

            progressStatus.UpdateLabel("Error: Failed to get AI response");
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
            Debug.Log("[GPTHandler] Parsed voice response: " + parsedResponse.voice_response);
            Debug.Log("[GPTHandler] Parsed text summary: " + parsedResponse.text_summary);
            Debug.Log("[GPTHandler] Parsed pointed at - part: " + parsedResponse.pointed_at.part);
            Debug.Log(
                "[GPTHandler] Parsed pointed at - coordinates: "
                    + parsedResponse.pointed_at.coordinates.x
                    + ", "
                    + parsedResponse.pointed_at.coordinates.y
            );

            // Position the red dot based on the coordinates from the response
            if (teacherHand != null)
            {
                // Check if we have valid coordinates (not 0,0 which is often the default)
                if (
                    parsedResponse.pointed_at.coordinates.x != 0
                    || parsedResponse.pointed_at.coordinates.y != 0
                )
                {
                    Debug.Log(
                        "[GPTHandler] Positioning red dot at: "
                            + parsedResponse.pointed_at.coordinates.x
                            + ", "
                            + parsedResponse.pointed_at.coordinates.y
                    );

                    // Call the PositionRedDot method with the normalized coordinates
                    Vector2 normalizedCoordinate = new Vector2(
                        parsedResponse.pointed_at.coordinates.x,
                        parsedResponse.pointed_at.coordinates.y
                    );

                    // Use reflection to call the method to ensure it exists
                    var positionMethod = teacherHand
                        .GetType()
                        .GetMethod(
                            "PositionRedDot",
                            System.Reflection.BindingFlags.Instance
                                | System.Reflection.BindingFlags.Public
                                | System.Reflection.BindingFlags.NonPublic
                        );

                    if (positionMethod != null)
                    {
                        positionMethod.Invoke(teacherHand, new object[] { normalizedCoordinate });
                        Debug.Log("[GPTHandler] Successfully positioned red dot");
                    }
                    else
                    {
                        Debug.LogError(
                            "[GPTHandler] PositionRedDot method not found on teacherHand"
                        );
                    }
                }
                else
                {
                    Debug.Log("[GPTHandler] Skipping red dot positioning - coordinates are (0,0)");
                }
            }
            else
            {
                Debug.LogError("[GPTHandler] Teacher hand reference is missing!");
            }

            return parsedResponse;
        }
        catch (Exception ex)
        {
            Debug.LogError("[GPTHandler] Failed to parse response: " + ex.Message);
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
        public string part;
        public Coordinates coordinates;
    }

    [System.Serializable]
    public class Coordinates
    {
        public float x;
        public float y;
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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ChatGPTHandler : MonoBehaviour
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

    private void Start()
    {
        LoadConfigs();
    }

    private void LoadConfigs()
    {
        if (ConfigLoader.Instance != null && ConfigLoader.Instance.ConfigData != null)
        {
            var config = ConfigLoader.Instance.ConfigData;

            openaiApiKey = config.openaiApiKey;
            openaiApiUrl = config.openaiApiUrl;

            if (string.IsNullOrEmpty(openaiApiKey) || string.IsNullOrEmpty(openaiApiUrl))
            {
                Debug.LogError("API Key or API URL is missing in the configuration.");
                return;
            }

            string systemPromptPath = Path.Combine(
                Application.streamingAssetsPath,
                config.systemPromptFilename
            );
            Debug.Log("Loading system prompt from: " + systemPromptPath);

            if (File.Exists(systemPromptPath))
            {
                systemPrompt = File.ReadAllText(systemPromptPath);

                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    Debug.Log("System prompt successfully loaded from: " + systemPromptPath);

                    if (conversationHistory.Count == 0)
                    {
                        conversationHistory.Add(
                            new ChatMessage
                            {
                                role = "system",
                                content = new List<ContentItem>
                                {
                                    new ContentItem { type = "text", text = systemPrompt },
                                },
                            }
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
            Debug.LogError("ConfigLoader instance or configuration data is not available.");
        }
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
            model = "gpt-4o",
            messages = messages.ToArray(),
            response_format = new ResponseFormat
            {
                type = "json_object",
                json_object = new JsonObject
                {
                    name = "ai_tutor_response",
                    strict = true,
                    schema = new TutorResponseSchema
                    {
                        type = "object",
                        properties = new TutorResponseProperties
                        {
                            pointed_at = new PointedAtProperty
                            {
                                type = "object",
                                properties = new PointedAtProperties
                                {
                                    part = new PartProperty
                                    {
                                        type = "string",
                                        description =
                                            "Describes which portion of the scene is being pointed at.",
                                        @enum = new string[]
                                        {
                                            "top",
                                            "upper-middle",
                                            "lower-middle",
                                            "bottom",
                                        },
                                    },
                                    coordinates = new CoordinatesProperty
                                    {
                                        type = "object",
                                        properties = new CoordinateProperties
                                        {
                                            x = new NumberProperty
                                            {
                                                type = "number",
                                                description =
                                                    "The X coordinate of the point being highlighted.",
                                            },
                                            y = new NumberProperty
                                            {
                                                type = "number",
                                                description =
                                                    "The Y coordinate of the point being highlighted.",
                                            },
                                        },
                                        required = new string[] { "x", "y" },
                                        additionalProperties = false,
                                    },
                                },
                                required = new string[] { "part", "coordinates" },
                                additionalProperties = false,
                            },
                            voice_response = new VoiceResponseProperty
                            {
                                type = "string",
                                description =
                                    "The verbal response from the AI tutor, which can be used for text-to-speech, with all response formatted in plaintext, no markdown.",
                            },
                            text_summary = new TextSummaryProperty
                            {
                                type = "string",
                                description =
                                    "A text summary that aids the user in understanding the voice response, with all response formatted in plaintext, no markdown.",
                            },
                        },
                        required = new string[] { "pointed_at", "voice_response", "text_summary" },
                        additionalProperties = false,
                    },
                },
            },
            temperature = TEMPERATURE,
            max_completion_tokens = MAX_COMPLETION_TOKENS,
            top_p = TOP_P,
            frequency_penalty = FREQUENCY_PENALTY,
            presence_penalty = PRESENCE_PENALTY,
        };

        string jsonPayload = JsonConvert.SerializeObject(
            payload,
            Newtonsoft.Json.Formatting.Indented,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
        );

        Debug.Log("Request to GPT: " + jsonPayload);

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

            string responseContent = ParseResponse(request.downloadHandler.text);
            responseText.text = responseContent;

            //conversationHistory.Add(new Message { role = "assistant", content = responseContent });

            textToSpeechHandler.SpeakText(responseContent);
        }
        else
        {
            Debug.LogError("Request error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);

            responseText.text = "Error talking to GPT. Please try again.";
        }
    }

    private string ParseResponse(string jsonResponse)
    {
        try
        {
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
        public JsonObject json_object;
    }

    [System.Serializable]
    public class JsonObject
    {
        public string name;
        public bool strict;
        public TutorResponseSchema schema;
    }

    [System.Serializable]
    public class TutorResponseSchema
    {
        public string type;
        public TutorResponseProperties properties;
        public string[] required;
        public bool additionalProperties;
    }

    [System.Serializable]
    public class TutorResponseProperties
    {
        public PointedAtProperty pointed_at;
        public VoiceResponseProperty voice_response;
        public TextSummaryProperty text_summary;
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
        public string type;
        public CoordinateProperties properties;
        public string[] required;
        public bool additionalProperties;
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

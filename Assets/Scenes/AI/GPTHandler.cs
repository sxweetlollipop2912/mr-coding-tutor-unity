using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    [SerializeField]
    private bool useStreamingResponse = true; // Option to toggle streaming mode

    private string openaiApiKey;
    private string systemPrompt;
    private string openaiApiUrl;
    private List<ChatMessage> conversationHistory = new List<ChatMessage>();

    // Constants for GPT parameters
    private const float TEMPERATURE = 1f;
    private const int MAX_COMPLETION_TOKENS = 512;
    private const float TOP_P = 1f;
    private const float FREQUENCY_PENALTY = 0f;
    private const float PRESENCE_PENALTY = 0f;
    private string modelName = "gpt-4o"; // Default value, will be overridden by config

    private bool configsLoaded = false;
    public bool ConfigsLoaded => configsLoaded;

    private string responseFormatJson; // Store the loaded response format as JSON

    // Buffer for storing streaming responses
    private string currentStreamingResponse = "";
    private bool isCurrentlyStreaming = false;

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
        modelName = config.gptModelName; // Load model name from config
        Debug.Log($"[GPTHandler] Using GPT model: {modelName}");

        // Load system prompt
        systemPrompt = LoadSystemPrompt(config.systemPromptFilename);
        if (string.IsNullOrEmpty(systemPrompt))
        {
            Debug.LogError("[GPTHandler] Failed to load system prompt.");
            return;
        }

        // Load response format JSON
        responseFormatJson = LoadResponseFormatJson(config.gptResponseFormatFilename);
        if (string.IsNullOrEmpty(responseFormatJson))
        {
            Debug.LogError("[GPTHandler] Failed to load response format JSON.");
            return;
        }

        // Add system message to conversation history
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
            Debug.LogError("[GPTHandler] Empty message");
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Empty message");
            return;
        }

        progressStatus.UpdateStep(AIProgressStatus.AIStep.SendingToAI);
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

            progressStatus.UpdateStep(AIProgressStatus.AIStep.ProcessingWithAI);
            AddUserMessageToConversation(message, base64Image);
            StartCoroutine(SendPostRequest(message, base64Image));
        }
        else
        {
            Debug.LogError("[GPTHandler] Received empty transcription from Whisper.");
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Empty transcription");
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
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "API configuration missing");
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
            model = modelName, // Use the model name from config
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
            stream = useStreamingResponse, // Add streaming option
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

        // Handle streaming or non-streaming based on the setting
        if (useStreamingResponse)
        {
            yield return StartCoroutine(SendStreamingRequest(jsonPayload));
        }
        else
        {
            yield return StartCoroutine(SendStandardRequest(jsonPayload));
        }
    }

    private IEnumerator SendStandardRequest(string jsonPayload)
    {
        // Regular non-streaming request implementation
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

            progressStatus.UpdateStep(AIProgressStatus.AIStep.ProcessingAIResponse);
            TutorResponseSchema parsedResponse = ParseResponse(request.downloadHandler.text);

            if (parsedResponse != null)
            {
                responseText.text = parsedResponse.text_summary;
                progressStatus.UpdateStep(AIProgressStatus.AIStep.ConvertingToSpeech);
                textToSpeechHandler.SpeakText(parsedResponse.voice_response);
            }
            else
            {
                progressStatus.UpdateStep(
                    AIProgressStatus.AIStep.Error,
                    "Failed to process AI response"
                );
                responseText.text = "Error processing AI response. Please try again.";
            }
        }
        else
        {
            Debug.LogError("Request error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);

            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Failed to get AI response");
            responseText.text = "Error talking to GPT. Please try again.";
        }
    }

    private IEnumerator SendStreamingRequest(string jsonPayload)
    {
        // Reset streaming state
        currentStreamingResponse = "";
        isCurrentlyStreaming = true;

        // Create buffer for streaming content
        StringBuilder contentBuilder = new StringBuilder();

        // Configure streaming request
        UnityWebRequest request = new UnityWebRequest(openaiApiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);

        // Use custom download handler for streaming
        StreamingDownloadHandler streamingHandler = new StreamingDownloadHandler();
        request.downloadHandler = streamingHandler;

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {openaiApiKey}");

        // Show streaming status
        progressStatus.UpdateStep(
            AIProgressStatus.AIStep.ProcessingWithAI,
            "Waiting for response..."
        );

        // Send request but don't yield until completion
        request.SendWebRequest();

        // Temporary partial response for ongoing processing
        TutorResponseSchema partialResponse = new TutorResponseSchema
        {
            text_summary = "Processing...",
            voice_response = "",
        };

        // Process incoming chunks
        while (!request.isDone)
        {
            // Check if new data has arrived
            if (streamingHandler.HasNewData())
            {
                string newData = streamingHandler.GetLatestChunk();
                processStreamingChunk(newData, contentBuilder);

                // Try to parse the partial response if we have enough data
                if (contentBuilder.Length > 50)
                {
                    try
                    {
                        // Attempt to parse current accumulated content
                        string tempContent = contentBuilder.ToString();
                        if (
                            tempContent.Contains("\"text_summary\"")
                            && tempContent.Contains("\"voice_response\"")
                        )
                        {
                            // Try to clean up and parse JSON
                            string cleanJson = EnsureValidJson(tempContent);
                            partialResponse = JsonConvert.DeserializeObject<TutorResponseSchema>(
                                cleanJson
                            );

                            // Update UI with partial response
                            if (
                                partialResponse != null
                                && !string.IsNullOrEmpty(partialResponse.text_summary)
                            )
                            {
                                responseText.text = partialResponse.text_summary + "...";
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        // Failed to parse partial response, will try again with more data
                        Debug.Log("[GPTHandler] Still collecting streaming data: " + e.Message);
                    }
                }
            }
            yield return null;
        }

        // Request is complete
        isCurrentlyStreaming = false;

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[GPTHandler] Streaming response completed");

            // Process the final complete response
            string completeResponse = contentBuilder.ToString();
            TutorResponseSchema finalResponse = null;

            try
            {
                // Extract and parse the complete JSON
                string extractedJson = ExtractCompletedJson(completeResponse);
                finalResponse = JsonConvert.DeserializeObject<TutorResponseSchema>(extractedJson);
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    "[GPTHandler] Failed to parse final streaming response: " + e.Message
                );
                progressStatus.UpdateStep(
                    AIProgressStatus.AIStep.Error,
                    "Failed to parse streaming response"
                );
                yield break;
            }

            if (finalResponse != null)
            {
                responseText.text = finalResponse.text_summary;
                progressStatus.UpdateStep(AIProgressStatus.AIStep.ConvertingToSpeech);
                textToSpeechHandler.SpeakText(finalResponse.voice_response);
            }
            else
            {
                progressStatus.UpdateStep(
                    AIProgressStatus.AIStep.Error,
                    "Failed to parse streaming response"
                );
                responseText.text = "Error processing streaming response.";
            }
        }
        else
        {
            Debug.LogError("[GPTHandler] Streaming request error: " + request.error);
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Streaming request failed");
            responseText.text = "Error with streaming response.";
        }

        request.Dispose();
    }

    private void processStreamingChunk(string chunk, StringBuilder contentBuilder)
    {
        // Process SSE format chunks from the OpenAI streaming API
        if (string.IsNullOrEmpty(chunk))
            return;

        // Split the chunk into lines
        string[] lines = chunk.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            // Check for data prefix in SSE format
            if (line.StartsWith("data: "))
            {
                string data = line.Substring(6); // Remove "data: " prefix

                // Check for [DONE] marker
                if (data == "[DONE]")
                {
                    Debug.Log("[GPTHandler] Streaming complete marker received");
                    continue;
                }

                try
                {
                    // Parse the JSON data
                    JObject jsonData = JObject.Parse(data);

                    // Extract content from the streaming response
                    if (
                        jsonData["choices"] != null
                        && jsonData["choices"][0]["delta"]["content"] != null
                    )
                    {
                        string content = jsonData["choices"][0]["delta"]["content"].ToString();
                        contentBuilder.Append(content);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GPTHandler] Error parsing streaming chunk: {e.Message}");
                }
            }
        }
    }

    private string EnsureValidJson(string partialJson)
    {
        // Try to make partial JSON valid for parsing
        if (string.IsNullOrEmpty(partialJson))
            return "{}";

        // Make sure we have an opening bracket
        if (!partialJson.TrimStart().StartsWith("{"))
            partialJson = "{" + partialJson;

        // Make sure we have a closing bracket
        if (!partialJson.TrimEnd().EndsWith("}"))
            partialJson = partialJson + "}";

        return partialJson;
    }

    private string ExtractCompletedJson(string streamingResponse)
    {
        // Find the complete JSON object in the streaming response
        int startBraceIndex = streamingResponse.IndexOf('{');
        int endBraceIndex = streamingResponse.LastIndexOf('}');

        if (startBraceIndex >= 0 && endBraceIndex > startBraceIndex)
        {
            return streamingResponse.Substring(
                startBraceIndex,
                endBraceIndex - startBraceIndex + 1
            );
        }

        throw new System.Exception("Could not find complete JSON object in streaming response");
    }

    // Custom download handler for streaming
    private class StreamingDownloadHandler : DownloadHandlerScript
    {
        private List<byte[]> receivedData = new List<byte[]>();
        private int processedIndex = 0;

        public StreamingDownloadHandler()
            : base() { }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0)
                return false;

            byte[] dataCopy = new byte[dataLength];
            System.Buffer.BlockCopy(data, 0, dataCopy, 0, dataLength);
            receivedData.Add(dataCopy);

            return true;
        }

        public bool HasNewData()
        {
            return processedIndex < receivedData.Count;
        }

        public string GetLatestChunk()
        {
            if (processedIndex >= receivedData.Count)
                return "";

            string result = System.Text.Encoding.UTF8.GetString(receivedData[processedIndex]);
            processedIndex++;
            return result;
        }

        protected override byte[] GetData()
        {
            // Combine all received data
            int totalLength = 0;
            foreach (byte[] chunk in receivedData)
            {
                totalLength += chunk.Length;
            }

            byte[] result = new byte[totalLength];
            int offset = 0;
            foreach (byte[] chunk in receivedData)
            {
                System.Buffer.BlockCopy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }

            return result;
        }

        protected override string GetText()
        {
            byte[] data = GetData();
            if (data == null || data.Length == 0)
                return "";

            return System.Text.Encoding.UTF8.GetString(data);
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
        public bool stream; // Add streaming option
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

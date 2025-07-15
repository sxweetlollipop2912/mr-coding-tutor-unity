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

    [SerializeField]
    private bool optimizeTTSResponses = true; // Whether to optimize TTS responses for speed

    [SerializeField]
    private int maxTTSCharacters = 500; // Maximum characters for TTS responses if optimization is enabled

    [SerializeField]
    private ConversationLogger conversationLogger;

    [SerializeField]
    private WhisperHandler whisperHandler; // Add reference to WhisperHandler for re-enabling recording

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
        // If the conversationLogger wasn't assigned in inspector, try to find it
        if (conversationLogger == null)
        {
            conversationLogger = ConversationLogger.Instance;
        }

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

        // Load problem and replace placeholder in system prompt
        string problem = LoadProblem(config.problemFilename);
        if (!string.IsNullOrEmpty(problem))
        {
            systemPrompt = systemPrompt.Replace("{problem_and_solution}", problem);
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

    private string LoadProblem(string problemFilename)
    {
        if (string.IsNullOrEmpty(problemFilename))
        {
            Debug.LogWarning("[GPTHandler] Problem filename is not specified in config.");
            return null;
        }

        string problemFilePath = Path.Combine(Application.streamingAssetsPath, problemFilename);

        if (!File.Exists(problemFilePath))
        {
            Debug.LogError("[GPTHandler] Problem file not found: " + problemFilePath);
            return null;
        }

        Debug.Log("[GPTHandler] Loading problem from: " + problemFilePath);
        return File.ReadAllText(problemFilePath);
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
            ReEnableRecording();
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
            ReEnableRecording();
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

        // Log the user message to the conversation log
        if (conversationLogger != null)
        {
            conversationLogger.LogUserMessage(message);
        }
    }

    private IEnumerator SendPostRequest(string message = null, string base64Image = null)
    {
        if (string.IsNullOrEmpty(openaiApiKey) || string.IsNullOrEmpty(openaiApiUrl))
        {
            Debug.LogError("API Key or API URL is not set.");
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "API configuration missing");

            // Re-enable recording so user can try again
            ReEnableRecording();
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

        // Log the request payload for debugging
        if (conversationLogger != null)
        {
            conversationLogger.LogRawJson("GPT Request", jsonPayload);
        }

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

            // Log the raw response for debugging
            if (conversationLogger != null)
            {
                conversationLogger.LogRawJson("GPT Response", request.downloadHandler.text);
            }

            progressStatus.UpdateStep(AIProgressStatus.AIStep.ProcessingAIResponse);
            TutorResponseSchema parsedResponse = ParseResponse(request.downloadHandler.text);

            if (parsedResponse != null)
            {
                responseText.text = parsedResponse.text_summary;
                SendToTTS(parsedResponse);
            }
            else
            {
                progressStatus.UpdateStep(
                    AIProgressStatus.AIStep.Error,
                    "Failed to process AI response"
                );
                responseText.text = "Error processing AI response. Please try again.";
                ReEnableRecording();
            }
        }
        else
        {
            Debug.LogError("Request error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);

            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Failed to get AI response");
            responseText.text = "Error talking to GPT. Please try again.";
            ReEnableRecording();
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
            pointed_at = new PointedAtProperty
            {
                coordinates = new Coordinates { x = 0, y = 0 },
            },
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

                // Log the extracted JSON for debugging
                if (conversationLogger != null && extractedJson != null)
                {
                    conversationLogger.LogRawJson("Streaming GPT Response (Parsed)", extractedJson);
                }

                // Log the parsed response details
                if (finalResponse != null)
                {
                    Debug.Log(
                        "[GPTHandler] Parsed voice response: " + finalResponse.voice_response
                    );
                    Debug.Log("[GPTHandler] Parsed text summary: " + finalResponse.text_summary);

                    if (finalResponse.pointed_at != null)
                    {
                        Debug.Log(
                            "[GPTHandler] Parsed pointed at - coordinates: "
                                + finalResponse.pointed_at.coordinates.x
                                + ", "
                                + finalResponse.pointed_at.coordinates.y
                        );

                        // Position the teacher hand based on coordinates
                        PositionTeacherHand(finalResponse);
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[GPTHandler] Parsed response is missing pointed_at property"
                        );
                    }

                    // Log the AI response from streaming
                    if (conversationLogger != null)
                    {
                        conversationLogger.LogAIResponse(
                            finalResponse.voice_response,
                            finalResponse.text_summary
                        );
                    }
                }
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
                ReEnableRecording();
                yield break;
            }

            if (finalResponse != null)
            {
                responseText.text = finalResponse.text_summary;
                SendToTTS(finalResponse);
            }
            else
            {
                progressStatus.UpdateStep(
                    AIProgressStatus.AIStep.Error,
                    "Failed to parse streaming response"
                );
                responseText.text = "Error processing streaming response.";
                ReEnableRecording();
            }
        }
        else
        {
            Debug.LogError("[GPTHandler] Streaming request error: " + request.error);
            progressStatus.UpdateStep(AIProgressStatus.AIStep.Error, "Streaming request failed");
            responseText.text = "Error with streaming response.";
            ReEnableRecording();
        }

        request.Dispose();
    }

    // Helper method to position the teacher hand (extracted from ParseResponse)
    private void PositionTeacherHand(TutorResponseSchema parsedResponse)
    {
        if (
            teacherHand != null
            && parsedResponse.pointed_at != null
            && parsedResponse.pointed_at.coordinates != null
        )
        {
            // Check if we have valid coordinates (not -1,-1 which is often the default)
            if (
                parsedResponse.pointed_at.coordinates.x != -1
                || parsedResponse.pointed_at.coordinates.y != -1
            )
            {
                Debug.Log(
                    "[GPTHandler] Positioning red dot at: "
                        + parsedResponse.pointed_at.coordinates.x
                        + ", "
                        + parsedResponse.pointed_at.coordinates.y
                );

                Vector2 normalizedCoordinate = new Vector2(
                    parsedResponse.pointed_at.coordinates.x,
                    parsedResponse.pointed_at.coordinates.y
                );

                teacherHand.RegisterRedDotPosition(normalizedCoordinate);
                Debug.Log(
                    "[GPTHandler] Successfully registered red dot position: " + normalizedCoordinate
                );
            }
            else
            {
                Debug.Log("[GPTHandler] Skipping red dot positioning - coordinates are (-1,-1)");
            }
        }
        else
        {
            Debug.LogError(
                "[GPTHandler] Teacher hand reference is missing or response lacks coordinates!"
            );
        }
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
            Debug.Log(
                "[GPTHandler] Parsed pointed at - coordinates: "
                    + parsedResponse.pointed_at.coordinates.x
                    + ", "
                    + parsedResponse.pointed_at.coordinates.y
            );

            // Position the teacher hand using the shared method
            PositionTeacherHand(parsedResponse);

            // Log the AI response
            if (conversationLogger != null)
            {
                conversationLogger.LogAIResponse(
                    parsedResponse.voice_response,
                    parsedResponse.text_summary
                );
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

    private void SendToTTS(TutorResponseSchema parsedResponse)
    {
        if (parsedResponse == null || textToSpeechHandler == null)
            return;

        string speechText = parsedResponse.voice_response;

        // If optimization is enabled, trim the text to improve TTS speed
        if (optimizeTTSResponses && !string.IsNullOrEmpty(speechText))
        {
            if (speechText.Length > maxTTSCharacters)
            {
                // Find the last sentence break within the limit
                int lastBreak = FindLastSentenceBreak(speechText, maxTTSCharacters);
                if (lastBreak > 0)
                {
                    string originalLength = speechText.Length.ToString();
                    speechText = speechText.Substring(0, lastBreak + 1); // Keep the period/question mark
                    Debug.Log(
                        $"[GPTHandler] Trimmed TTS text from {originalLength} to {speechText.Length} characters"
                    );
                }
            }
        }

        progressStatus.UpdateStep(AIProgressStatus.AIStep.ConvertingToSpeech);
        textToSpeechHandler.SpeakText(speechText);
    }

    private int FindLastSentenceBreak(string text, int maxLength)
    {
        // Get the portion of text within our limit
        string portion = text.Length <= maxLength ? text : text.Substring(0, maxLength);

        // Find the last sentence break (period, question mark, exclamation point)
        int lastPeriod = portion.LastIndexOf('.');
        int lastQuestion = portion.LastIndexOf('?');
        int lastExclamation = portion.LastIndexOf('!');

        // Find the max of these positions
        return Math.Max(Math.Max(lastPeriod, lastQuestion), lastExclamation);
    }

    /// <summary>
    /// Helper method to re-enable recording when errors occur
    /// </summary>
    private void ReEnableRecording()
    {
        if (whisperHandler != null)
        {
            whisperHandler.EnableRecording();
            Debug.Log("[GPTHandler] Re-enabled recording after error");
        }
        else
        {
            Debug.LogWarning(
                "[GPTHandler] Cannot re-enable recording - WhisperHandler reference is missing"
            );
        }
    }
}

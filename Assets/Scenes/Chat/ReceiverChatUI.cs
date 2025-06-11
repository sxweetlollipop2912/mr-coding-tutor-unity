using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ReceiverChatUI : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect scrollRect;
    public TextMeshProUGUI chatText;
    private bool autoScroll = true;

    private Dictionary<long, ChatMessage> chatMessages = new Dictionary<long, ChatMessage>();

    void Start()
    {
        // Listen for manual scrolls
        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
    }

    void OnScrollValueChanged(Vector2 pos)
    {
        // verticalNormalizedPosition: 1 = top; 0 = bottom :contentReference[oaicite:8]{index=8}
        autoScroll = (scrollRect.verticalNormalizedPosition <= 0.001f);
    }

    public void HandleIncomingChat(string rawMessage)
    {
        string json = rawMessage.Substring("CHAT_MSG:".Length);
        try
        {
            ChatMessage chat = JsonUtility.FromJson<ChatMessage>(json);
            if (chatMessages.ContainsKey(chat.key))
                return;

            chatMessages.Add(chat.key, chat);

            Debug.Log($"[Chat @ {chat.timestamp}] {chat.content}");

            if (chatText.text == "")
            {
                chatText.text = $"{chat.timestamp}[Teacher]\n<noparse>{chat.content}</noparse>";
            }
            else
            {
                chatText.text +=
                    $"\n\n{chat.timestamp}[Teacher]\n<noparse>{chat.content}</noparse>";
            }

            // Force rebuild to update layout immediately
            Canvas.ForceUpdateCanvases();

            // Auto-scroll if we're at the bottom
            if (autoScroll)
            {
                scrollRect.verticalNormalizedPosition = 0f;
                Canvas.ForceUpdateCanvases(); // ensure immediate effect :contentReference[oaicite:9]{index=9}
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("ReceiverChatUI failed to parse chat JSON: " + e.Message);
        }
    }

    private void ScrollToBottom()
    {
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases(); // ensure immediate effect :contentReference[oaicite:9]{index=9}
    }
}

// Same ChatMessage class as in TeacherAgoraScript_NewUI
[System.Serializable]
public class ChatMessage
{
    public long key;
    public string timestamp;
    public string content;
}

using TMPro;
using UnityEngine;

public class ReceiverChatUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text chatHistoryText;

    // This method will be called by StudentAgoraScript when chat arrives
    public void HandleIncomingChat(string rawMessage)
    {
        if (!rawMessage.StartsWith("CHAT_MSG:"))
        {
            Debug.LogWarning("ReceiverChatUI got a non-chat message: " + rawMessage);
            return;
        }

        string json = rawMessage.Substring("CHAT_MSG:".Length);
        try
        {
            ChatMessage chat = JsonUtility.FromJson<ChatMessage>(json);
            Debug.Log($"[Chat @ {chat.timestamp}] {chat.content}");

            // Update UI with the received message
            if (chatHistoryText != null)
            {
                chatHistoryText.text += $"<b>Teacher:</b> {chat.content}\n";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("ReceiverChatUI failed to parse chat JSON: " + e.Message);
        }
    }
}

// Same ChatMessage class as in TeacherAgoraScript_NewUI
[System.Serializable]
public class ChatMessage
{
    public string timestamp;
    public string content;
}

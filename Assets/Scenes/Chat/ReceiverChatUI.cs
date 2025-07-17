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

    [Header("Notification Sound")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip notificationSound;

    [SerializeField]
    private bool playNotificationSound = true;

    [SerializeField]
    private float notificationVolume = 1.0f;

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
                chatText.text = $"[{chat.timestamp}] <noparse>{chat.content}</noparse>";
            }
            else
            {
                chatText.text +=
                    $"\n\n[{chat.timestamp}] <noparse>{chat.content}</noparse>";
            }

            // Force rebuild to update layout immediately
            Canvas.ForceUpdateCanvases();

            // Auto-scroll if we're at the bottom
            if (autoScroll)
            {
                scrollRect.verticalNormalizedPosition = 0f;
                Canvas.ForceUpdateCanvases(); // ensure immediate effect :contentReference[oaicite:9]{index=9}
            }

            // Play notification sound for new messages
            PlayNotificationSound();
        }
        catch (System.Exception e)
        {
            Debug.LogError("ReceiverChatUI failed to parse chat JSON: " + e.Message);
        }
    }

    /// <summary>
    /// Plays the notification sound when a new chat message is received.
    /// This method is modular and can be easily customized or disabled.
    /// </summary>
    private void PlayNotificationSound()
    {
        // Early return if notification sound is disabled
        if (!playNotificationSound)
            return;

        // Validate audio components
        if (audioSource == null)
        {
            Debug.LogWarning("ReceiverChatUI: AudioSource is not assigned. Cannot play notification sound.");
            return;
        }

        if (notificationSound == null)
        {
            Debug.LogWarning("ReceiverChatUI: Notification sound clip is not assigned.");
            return;
        }

        // Play the notification sound
        audioSource.PlayOneShot(notificationSound, notificationVolume);
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

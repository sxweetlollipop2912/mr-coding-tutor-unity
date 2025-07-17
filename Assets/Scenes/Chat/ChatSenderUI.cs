using System;
using System.Collections.Generic;
using Agora_RTC_Plugin.API_Example.Examples.Advanced.ScreenShareWhileVideoCall.TeacherMrCodingTutorUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatOverlayController_TMP : MonoBehaviour
{
    [Header("UI References")]
    public Button chatToggleButton; // The bottom-right "Chat" button
    public GameObject chatOverlay; // The semi-transparent panel
    public Button closeButton; // The "X" button inside the panel

    public TMP_InputField chatInputField; // The Multi Line TMP_InputField
    public Button sendButton; // The "Send" button

    [Header("Scroll View Content")]
    public RectTransform contentParent; // The RectTransform of ScrollView→Viewport→Content

    [Header("Message Prefab")]
    public GameObject chatMessagePrefab; // Must contain a TextMeshProUGUI on its root

    [Header("Agora Integration")]
    [SerializeField]
    private TeacherAgoraScript_NewUI agoraManager; // Reference to the Agora manager

    // In-memory list of all sent messages (just for your reference; not strictly needed to display)
    private List<string> messages = new List<string>();

    private void Awake()
    {
        // 1) Toggle the overlay on clicking the Chat button
        chatToggleButton.onClick.AddListener(ToggleOverlay);

        // 2) ClosePanel only hides it (does NOT clear input! preserves typed text)
        closeButton.onClick.AddListener(ToggleOverlay);

        // 3) SendButton actually sends whatever is in the input field
        sendButton.onClick.AddListener(OnSendButtonClicked);

        // 4) Note: onSubmit doesn't work well with multiline fields, so we'll use Update() instead

        // 5) Ensure the overlay is hidden at start
        chatOverlay.SetActive(false);
    }

    private void ToggleOverlay()
    {
        bool isActive = chatOverlay.activeSelf;
        chatOverlay.SetActive(!isActive);

        // If the overlay was closed (isActive == false), we just opened it → hide the Chat button.
        // If the overlay was open (isActive == true), we just closed it → show the Chat button again.
        chatToggleButton.gameObject.SetActive(isActive);

        // Do NOT clear chatInputField.text here — we want to preserve it if the user closed previously.
        // If you DO want to clear text when opening the first time, only do it if this is the very first toggle.
        // For simplicity, we leave it as "don't clear": re-opening shows whatever was typed last.

        if (!isActive)
        {
            // We just opened it; put keyboard focus on the TMP_InputField
            chatInputField.ActivateInputField();
        }
    }

    private void Update()
    {
        // Check for Enter key press while input field is focused
        if (chatInputField.isFocused && Input.GetKeyDown(KeyCode.Return))
        {
            // Check if Shift is held down for newline, otherwise send
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                // Let Unity handle the newline - do nothing
                return;
            }

            // Send the message when Enter is pressed without Shift
            SubmitCurrentMessage();
        }
    }

    private void OnSendButtonClicked()
    {
        SubmitCurrentMessage();
    }

    private void SubmitCurrentMessage()
    {
        SubmitCurrentMessage(chatInputField.text);
    }

    private void SubmitCurrentMessage(string raw)
    {
        raw = raw.Trim();

        if (string.IsNullOrEmpty(raw))
        {
            // Nothing to send if empty (or only whitespace). You could also trim if you want to disallow whitespace-only.
            return;
        }

        // 1) Add to in-memory list (optional, for your own logic/logging)
        messages.Add(raw);

        // 2) Send via Agora
        if (!agoraManager.SendChatMessage(raw))
        {
            Debug.LogError("Failed to send message via Agora!");
        }

        // 3) Instantiate your prefab under `contentParent`
        GameObject go = Instantiate(chatMessagePrefab, contentParent);
        go.name = "ChatMessage_TMP";

        // 4) Find the TextMeshProUGUI component on the prefab's root (or child)
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            Debug.LogError("chatMessagePrefab must have a TextMeshProUGUI component on its root!");
            return;
        }

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        // You can pick any format you like, e.g. "yyyy-MM-dd HH:mm"
        // Wrap the user text in <noparse>…</noparse> to escape all tags.
        string combined = $"[{timestamp}][Teacher]\n<noparse>{raw}</noparse>";

        // 5) Assign the combined text (timestamp is italic, user text is literal)
        tmp.richText = true; // ensure TMP will honor <i>…</i>
        tmp.text = combined;

        // 6) Force‐update the layout, then scroll to bottom
        Canvas.ForceUpdateCanvases();
        ScrollRect sr = contentParent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            // Set verticalNormalizedPosition = 0 to scroll to the bottom
            sr.verticalNormalizedPosition = 0;
        }

        // 7) Clear the input field and re‐focus it
        //    (If you DO want to clear it; if you'd rather preserve the text, skip setting it to "")
        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }
}

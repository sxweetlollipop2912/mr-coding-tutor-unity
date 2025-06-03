using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatOverlayController_TMP : MonoBehaviour
{
    [Header("UI References")]
    public Button chatToggleButton;       // The bottom-right “Chat” button
    public GameObject chatOverlay;        // The semi-transparent panel
    public Button closeButton;            // The “X” button inside the panel

    public TMP_InputField chatInputField; // The Multi Line TMP_InputField
    public Button sendButton;             // The “Send” button

    [Header("Scroll View Content")]
    public RectTransform contentParent;   // The RectTransform of ScrollView→Viewport→Content

    [Header("Message Prefab")]
    public GameObject chatMessagePrefab;  // Must contain a TextMeshProUGUI on its root

    // In-memory list of all sent messages (just for your reference; not strictly needed to display)
    private List<string> messages = new List<string>();

    private void Awake()
    {
        // 1) Toggle the overlay on clicking the Chat button
        chatToggleButton.onClick.AddListener(ToggleOverlay);

        // 2) ClosePanel only hides it (does NOT clear input! preserves typed text)
        closeButton.onClick.AddListener(() => chatOverlay.SetActive(false));

        // 3) SendButton actually sends whatever is in the input field
        sendButton.onClick.AddListener(OnSendButtonClicked);

        // 4) IMPORTANT: Do NOT hook into onSubmit or onEndEdit for “Enter” anymore.
        //    We want ENTER to create a newline, not send. So we leave chatInputField.onSubmit alone.

        // 5) Ensure the overlay is hidden at start
        chatOverlay.SetActive(false);
    }

    private void ToggleOverlay()
    {
        bool isActive = chatOverlay.activeSelf;
        chatOverlay.SetActive(!isActive);

        // Do NOT clear chatInputField.text here — we want to preserve it if the user closed previously.
        // If you DO want to clear text when opening the first time, only do it if this is the very first toggle.
        // For simplicity, we leave it as “don’t clear”: re‐opening shows whatever was typed last.

        if (!isActive)
        {
            // We just opened it; put keyboard focus on the TMP_InputField
            chatInputField.ActivateInputField();
        }
    }

    private void OnSendButtonClicked()
    {
        SubmitCurrentMessage();
    }

    private void SubmitCurrentMessage()
    {
        string raw = chatInputField.text;
        if (string.IsNullOrEmpty(raw))
        {
            // Nothing to send if empty (or only whitespace). You could also trim if you want to disallow whitespace-only.
            return;
        }

        // 1) Add to in-memory list (optional, for your own logic/logging)
        messages.Add(raw);

        // 2) Instantiate your prefab under `contentParent`
        GameObject go = Instantiate(chatMessagePrefab, contentParent);
        go.name = "ChatMessage_TMP";

        // 3) Find the TextMeshProUGUI component on the prefab’s root (or child)
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            Debug.LogError("chatMessagePrefab must have a TextMeshProUGUI component on its root!");
            return;
        }

        // 4) Assign the text exactly as typed (preserving spaces and newlines)
        tmp.text = raw;

        // 5) Force‐update the layout, then scroll to bottom
        Canvas.ForceUpdateCanvases();
        ScrollRect sr = contentParent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            // Set verticalNormalizedPosition = 0 to scroll to the bottom
            sr.verticalNormalizedPosition = 0;
        }

        // 6) Clear the input field and re‐focus it
        //    (If you DO want to clear it; if you’d rather preserve the text, skip setting it to "")
        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }
}

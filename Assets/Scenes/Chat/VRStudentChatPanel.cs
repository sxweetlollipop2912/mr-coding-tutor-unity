using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VRStudentChatPanel : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect scrollRect;                // assign in Inspector
    public TextMeshProUGUI chatText;             // the TextContainer TMP

    private bool autoScroll = true;

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

    public void AddChatLine(string newText)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm");
        chatText.text += $"\n[{timestamp}] {newText}";

        // Force rebuild to update layout immediately
        Canvas.ForceUpdateCanvases();

        // Auto-scroll if we're at the bottom
        if (autoScroll)
        {
            scrollRect.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases(); // ensure immediate effect :contentReference[oaicite:9]{index=9}
        }
    }
}

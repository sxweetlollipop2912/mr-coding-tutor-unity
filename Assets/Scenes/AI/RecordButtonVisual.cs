using UnityEngine;
using TMPro;
using Oculus.Interaction;

public class RecordButtonVisual : MonoBehaviour
{
    [Header("Button Visuals")]
    public TextMeshProUGUI         buttonLabel;
    public RoundedBoxProperties    boxProps;           // ← new

    public string normalText    = "Press to Record";
    public string recordingText = "Recording…";

    public Color normalColor    = Color.white;
    public Color recordingColor = Color.green;

    private void Start()
    {
        buttonLabel.text = normalText;
        boxProps.Color = normalColor;
    }

    public void StartRecording()
    {
        buttonLabel.text = recordingText;
        boxProps.Color = recordingColor;
    }
    
    public void StopRecording()
    {
        buttonLabel.text = normalText;
        boxProps.Color = normalColor;
    }
}

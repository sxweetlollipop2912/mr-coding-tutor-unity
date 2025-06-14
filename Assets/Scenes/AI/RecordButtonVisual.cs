using UnityEngine;
using TMPro;
using Oculus.Interaction;

public class RecordButtonVisual : MonoBehaviour
{
    [Header("Button Visuals")]
    public TMP_Text buttonLabel;
    public Renderer panelRenderer;

    public string normalText    = "Press to Record";
    public string recordingText = "Recording…";

    public Color normalColor    = Color.white;
    public Color recordingColor = Color.green;

    private void Start()
    {
        buttonLabel.text = normalText;
        panelRenderer.material.color = normalColor;
    }

    public void StartRecording()
    {
        buttonLabel.text = recordingText;
        panelRenderer.material.color = recordingColor;
    }
    
    public void StopRecording()
    {
        buttonLabel.text = normalText;
        panelRenderer.material.color = normalColor;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Oculus.Interaction;

public class RecordButton : MonoBehaviour
{
    [SerializeField]
    private WhisperHandler whisperHandler;

    [SerializeField]
    private TMP_Text label;

    public RoundedBoxProperties buttonBox;

    public Color32 normalColor = new Color32(0xFF, 0xFF, 0xFF, 0x14);
    public Color32 recordingColor = new Color32(0x78, 0xE3, 0x00, 0x5E);

    public string normalText = "Press to Record";
    public string recordingText = "Recording...";

    private bool isRecordingToggle = false;

    void Start()
    {
        label.text = normalText;
        buttonBox.Color = normalColor;
    }

    public void Toggle()
    {
        isRecordingToggle = !isRecordingToggle;
        if (isRecordingToggle)
        {
            whisperHandler.StartRecording();
            label.text = recordingText;
            buttonBox.Color = recordingColor;
        }
        else
        {
            whisperHandler.StopRecording();
            label.text = normalText;
            buttonBox.Color = normalColor;
        }
    }
}

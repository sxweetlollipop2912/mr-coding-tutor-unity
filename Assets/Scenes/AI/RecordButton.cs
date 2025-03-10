using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RecordButton : MonoBehaviour
{
    [SerializeField]
    private WhisperHandler whisperHandler;

    [SerializeField]
    private TMP_Text label;

    private bool isRecordingToggle = false;

    void Start()
    {
        label.text = "Press to Record";
    }

    public void Toggle()
    {
        isRecordingToggle = !isRecordingToggle;
        if (isRecordingToggle)
        {
            whisperHandler.StartRecording();
            label.text = "Recording...";
        }
        else
        {
            whisperHandler.StopRecording();
            label.text = "Press to Record";
        }
    }
}

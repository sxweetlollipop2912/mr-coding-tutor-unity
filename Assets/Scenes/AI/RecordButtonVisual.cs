using UnityEngine;
using TMPro;
using Oculus.Interaction;

public class RecordButtonVisual : MonoBehaviour
{
    [Header("Button Visuals")]
    public TMP_Text buttonLabel;
    public MaterialPropertyBlockEditor  mpbEditor;

    public string normalText    = "Press to Record";
    public string recordingText = "Recording…";
    public string disabledText  = "Processing…";

    public Color normalColor    = Color.white;
    public Color recordingColor = Color.green;
    public Color disabledColor  = Color.gray;

    private void Start()
    {
        buttonLabel.text = normalText;
        SetButtonColor(normalColor);
    }

    public void StartRecording()
    {
        buttonLabel.text = recordingText;
        SetButtonColor(recordingColor);
    }
    
    public void DisableRecording()
    {
        buttonLabel.text = disabledText;
        SetButtonColor(disabledColor);
    }

    public void EnableRecording()
    {
        buttonLabel.text = normalText;
        SetButtonColor(normalColor);
    }

    private void SetButtonColor(Color color)
    {
        // // 2) Find (or create) the "_Color" property entry in the MPB Editor
        // //    NOTE: replace `colorProperties` below with the exact field name
        // //    your MPB Editor script uses for its List<ColorProperty> or similar.
        // var props = mpbEditor.colorProperties;   // e.g. List<MaterialPropertyBlockEditor.ColorProperty>
        // int idx = props.FindIndex(p => p.propertyName == "_Color");
        // if (idx >= 0)
        // {
        //     props[idx].value = color;
        // }
        // else
        // {
        //     // if it wasn’t already in the list, add it now
        //     props.Add(new MaterialPropertyBlockEditor.ColorProperty {
        //         propertyName = "_Color",
        //         value        = color
        //     });
        // }

        // // 3) Force the MPB Editor to re-apply its block immediately
        // //    (many implementations expose an Apply() or Refresh() you can call)
        // mpbEditor.Apply();  
    }
}

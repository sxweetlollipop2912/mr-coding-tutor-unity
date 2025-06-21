using UnityEngine;
using Gma.System.MouseKeyHook;
using System.Windows.Forms;

public class RecordKeyHook : MonoBehaviour
{
    private IKeyboardMouseEvents _globalHook;
    
    [SerializeField]
    private WhisperHandler whisperHandler;
    
    [SerializeField]
    private Keys triggerKey = Keys.P;

    [SerializeField]
    private bool suppressFurtherPropagation = false;
    
    // Called when this component is enabled
    private void OnEnable()
    {
        // Subscribe to global keyboard events
        _globalHook = Hook.GlobalEvents();
        _globalHook.KeyDown += OnGlobalKeyDown;
    }

    // Called when this component is disabled or destroyed
    private void OnDisable()
    {
        if (_globalHook == null) return;
        _globalHook.KeyDown -= OnGlobalKeyDown;
        _globalHook.Dispose();
        _globalHook = null;
    }

    // Handler for key-down events
    private void OnGlobalKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == triggerKey)
        {
            TriggerAction();
            if (suppressFurtherPropagation)
            {
                e.Handled = true;
            }
        }
    }

    // Your arbitrary function
    private void TriggerAction()
    {
        Debug.Log($"Global Ctrl+{triggerKey} detected—triggering recording!");
        
        if (whisperHandler != null)
        {
            whisperHandler.TriggerRecording();
        }
        else
        {
            Debug.LogError("[RecordKeyHook] WhisperHandler reference is missing!");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class GlobalF5RecordingTrigger : MonoBehaviour
{
    [Header("Recording Trigger Settings")]
    [SerializeField]
    [Tooltip("Drag the WhisperHandler from the scene here")]
    private WhisperHandler whisperHandler;

    [SerializeField]
    [Tooltip("Enable/disable global F5 key detection")]
    private bool enableGlobalF5Trigger = true;

    [Header("Debug Info")]
    [SerializeField]
    [Tooltip("Shows the last time F5 was pressed (for debugging)")]
    private string lastF5PressTime = "Never";

    [SerializeField]
    [Tooltip("Shows if the global hotkey is currently registered")]
    private bool isHotkeyRegistered = false;

    // Windows API declarations
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool GetMessage(
        out MSG lpMsg,
        IntPtr hWnd,
        uint wMsgFilterMin,
        uint wMsgFilterMax
    );

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(
        out MSG lpMsg,
        IntPtr hWnd,
        uint wMsgFilterMin,
        uint wMsgFilterMax,
        uint wRemoveMsg
    );

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    // Windows message structure
    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    // Constants
    private const int HOTKEY_ID = 9000;
    private const int VK_F5 = 0x74; // F5 key virtual code
    private const int MOD_NONE = 0x0000; // No modifiers
    private const uint WM_HOTKEY = 0x0312;
    private const uint PM_REMOVE = 0x0001;

    // Threading
    private Thread messageLoopThread;
    private volatile bool shouldStop = false;
    private IntPtr windowHandle;

    // Thread-safe queue for main thread actions
    private readonly Queue<System.Action> mainThreadActions = new Queue<System.Action>();
    private readonly object actionQueueLock = new object();

    private void Start()
    {
        // Validate the whisperHandler reference
        if (whisperHandler == null)
        {
            Debug.LogWarning(
                "[GlobalF5RecordingTrigger] WhisperHandler not assigned! Please drag the WhisperHandler component to this script in the inspector."
            );
            return;
        }

        if (enableGlobalF5Trigger)
        {
            StartGlobalHotkey();
        }
    }

    private void Update()
    {
        // Process any queued actions on the main thread
        ProcessMainThreadActions();
    }

    private void StartGlobalHotkey()
    {
        try
        {
            // Get Unity's window handle
            windowHandle = GetActiveWindow();

            // Register F5 as global hotkey
            if (RegisterHotKey(windowHandle, HOTKEY_ID, MOD_NONE, VK_F5))
            {
                isHotkeyRegistered = true;
                Debug.Log(
                    "[GlobalF5RecordingTrigger] Global F5 hotkey registered successfully! Press F5 anywhere to start/stop recording."
                );

                // Start the message loop thread
                messageLoopThread = new Thread(MessageLoopWorker)
                {
                    IsBackground = true,
                    Name = "GlobalF5MessageLoop",
                };
                messageLoopThread.Start();
            }
            else
            {
                Debug.LogError(
                    "[GlobalF5RecordingTrigger] Failed to register global F5 hotkey! Another application might be using it, or insufficient permissions."
                );
                isHotkeyRegistered = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GlobalF5RecordingTrigger] Error starting global hotkey: {e.Message}");
            isHotkeyRegistered = false;
        }
    }

    private void MessageLoopWorker()
    {
        Debug.Log("[GlobalF5RecordingTrigger] Message loop thread started");

        while (!shouldStop)
        {
            try
            {
                MSG msg;
                // Use PeekMessage with a timeout to avoid blocking indefinitely
                if (PeekMessage(out msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == HOTKEY_ID)
                    {
                        // F5 hotkey was pressed! Queue the action for the main thread
                        lock (actionQueueLock)
                        {
                            mainThreadActions.Enqueue(() => OnF5Pressed());
                        }
                    }
                }

                // Small delay to prevent excessive CPU usage
                Thread.Sleep(10);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GlobalF5RecordingTrigger] Error in message loop: {e.Message}");
                Thread.Sleep(100); // Longer delay on error
            }
        }

        Debug.Log("[GlobalF5RecordingTrigger] Message loop thread stopped");
    }

    private void ProcessMainThreadActions()
    {
        lock (actionQueueLock)
        {
            while (mainThreadActions.Count > 0)
            {
                try
                {
                    System.Action action = mainThreadActions.Dequeue();
                    action?.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError(
                        $"[GlobalF5RecordingTrigger] Error executing main thread action: {e.Message}"
                    );
                }
            }
        }
    }

    private void OnF5Pressed()
    {
        if (!enableGlobalF5Trigger || whisperHandler == null)
            return;

        Debug.Log("[GlobalF5RecordingTrigger] Global F5 key pressed! Triggering recording...");

        // Update debug info
        lastF5PressTime = System.DateTime.Now.ToString("HH:mm:ss");

        // Call the WhisperHandler's TriggerRecording function
        whisperHandler.TriggerRecording();
    }

    private void StopGlobalHotkey()
    {
        try
        {
            // Signal the message loop thread to stop
            shouldStop = true;

            // Wait for the thread to finish (with timeout)
            if (messageLoopThread != null && messageLoopThread.IsAlive)
            {
                if (!messageLoopThread.Join(1000)) // 1 second timeout
                {
                    Debug.LogWarning(
                        "[GlobalF5RecordingTrigger] Message loop thread did not stop gracefully"
                    );
                    messageLoopThread.Abort(); // Force stop if needed
                }
            }

            // Unregister the hotkey
            if (isHotkeyRegistered && windowHandle != IntPtr.Zero)
            {
                if (UnregisterHotKey(windowHandle, HOTKEY_ID))
                {
                    Debug.Log(
                        "[GlobalF5RecordingTrigger] Global F5 hotkey unregistered successfully"
                    );
                }
                else
                {
                    Debug.LogWarning(
                        "[GlobalF5RecordingTrigger] Failed to unregister global F5 hotkey"
                    );
                }
            }

            isHotkeyRegistered = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GlobalF5RecordingTrigger] Error stopping global hotkey: {e.Message}");
        }
    }

    /// <summary>
    /// Public method to enable/disable global F5 trigger
    /// </summary>
    public void SetGlobalF5TriggerEnabled(bool enabled)
    {
        if (enableGlobalF5Trigger == enabled)
            return;

        enableGlobalF5Trigger = enabled;

        if (enabled && !isHotkeyRegistered)
        {
            StartGlobalHotkey();
        }
        else if (!enabled && isHotkeyRegistered)
        {
            StopGlobalHotkey();
        }

        Debug.Log(
            $"[GlobalF5RecordingTrigger] Global F5 trigger {(enabled ? "enabled" : "disabled")}"
        );
    }

    /// <summary>
    /// Public method to check if global F5 trigger is currently enabled and registered
    /// </summary>
    public bool IsGlobalF5TriggerActive()
    {
        return enableGlobalF5Trigger && isHotkeyRegistered;
    }

    /// <summary>
    /// Public method to manually trigger recording (for testing)
    /// </summary>
    public void ManualTriggerRecording()
    {
        if (whisperHandler != null)
        {
            Debug.Log("[GlobalF5RecordingTrigger] Manual trigger activated");
            whisperHandler.TriggerRecording();
        }
        else
        {
            Debug.LogWarning(
                "[GlobalF5RecordingTrigger] Cannot manually trigger - WhisperHandler not assigned!"
            );
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Handle application pause/resume
        if (pauseStatus)
        {
            Debug.Log("[GlobalF5RecordingTrigger] Application paused");
        }
        else
        {
            Debug.Log("[GlobalF5RecordingTrigger] Application resumed");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // This is just for logging - the global hotkey works regardless of focus
        Debug.Log($"[GlobalF5RecordingTrigger] Application focus: {hasFocus}");
    }

    private void OnDestroy()
    {
        StopGlobalHotkey();
    }

    private void OnApplicationQuit()
    {
        StopGlobalHotkey();
    }

    private void OnValidate()
    {
        // This runs in the editor when values change in the inspector
        if (whisperHandler == null)
        {
            Debug.LogWarning(
                "[GlobalF5RecordingTrigger] WhisperHandler reference is missing. Please assign it in the inspector."
            );
        }
    }
}

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
        Debug.Log("[GlobalF5RecordingTrigger] === STARTING GLOBAL F5 TRIGGER ===");
        Debug.Log($"[GlobalF5RecordingTrigger] Platform: {Application.platform}");
        Debug.Log(
            $"[GlobalF5RecordingTrigger] Is Windows Platform: {Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor}"
        );
        Debug.Log($"[GlobalF5RecordingTrigger] enableGlobalF5Trigger: {enableGlobalF5Trigger}");

        // Check if we're on Windows
        if (
            Application.platform != RuntimePlatform.WindowsPlayer
            && Application.platform != RuntimePlatform.WindowsEditor
        )
        {
            Debug.LogError(
                "[GlobalF5RecordingTrigger] This script only works on Windows! Current platform: "
                    + Application.platform
            );
            return;
        }

        // Validate the whisperHandler reference
        if (whisperHandler == null)
        {
            Debug.LogWarning(
                "[GlobalF5RecordingTrigger] WhisperHandler not assigned! Please drag the WhisperHandler component to this script in the inspector."
            );
            return;
        }
        else
        {
            Debug.Log("[GlobalF5RecordingTrigger] WhisperHandler assigned successfully");
        }

        if (enableGlobalF5Trigger)
        {
            Debug.Log("[GlobalF5RecordingTrigger] Starting global hotkey registration...");
            StartGlobalHotkey();
        }
        else
        {
            Debug.Log("[GlobalF5RecordingTrigger] Global F5 trigger is disabled in inspector");
        }
    }

    private void Update()
    {
        // Process any queued actions on the main thread
        ProcessMainThreadActions();
    }

    private void StartGlobalHotkey()
    {
        Debug.Log("[GlobalF5RecordingTrigger] === STARTING GLOBAL HOTKEY REGISTRATION ===");

        try
        {
            // Get Unity's window handle
            Debug.Log("[GlobalF5RecordingTrigger] Getting Unity window handle...");
            windowHandle = GetActiveWindow();
            Debug.Log(
                $"[GlobalF5RecordingTrigger] Window handle: {windowHandle} (0x{windowHandle.ToString("X")})"
            );

            if (windowHandle == IntPtr.Zero)
            {
                Debug.LogError("[GlobalF5RecordingTrigger] Failed to get valid window handle!");
                return;
            }

            // Show all the constants being used
            Debug.Log(
                $"[GlobalF5RecordingTrigger] Using constants: HOTKEY_ID={HOTKEY_ID}, VK_F5=0x{VK_F5:X}, MOD_NONE={MOD_NONE}"
            );

            // Register F5 as global hotkey
            Debug.Log("[GlobalF5RecordingTrigger] Attempting to register F5 hotkey...");
            bool registrationResult = RegisterHotKey(windowHandle, HOTKEY_ID, MOD_NONE, VK_F5);
            Debug.Log($"[GlobalF5RecordingTrigger] RegisterHotKey returned: {registrationResult}");

            if (registrationResult)
            {
                isHotkeyRegistered = true;
                Debug.Log(
                    "[GlobalF5RecordingTrigger] Global F5 hotkey registered successfully! Press F5 anywhere to start/stop recording."
                );

                // Start the message loop thread
                Debug.Log("[GlobalF5RecordingTrigger] Starting message loop thread...");
                messageLoopThread = new Thread(MessageLoopWorker)
                {
                    IsBackground = true,
                    Name = "GlobalF5MessageLoop",
                };
                messageLoopThread.Start();
                Debug.Log(
                    $"[GlobalF5RecordingTrigger] Message loop thread started. ThreadId: {messageLoopThread.ManagedThreadId}"
                );
            }
            else
            {
                // Get the last Windows error
                int lastError = Marshal.GetLastWin32Error();
                Debug.LogError(
                    $"[GlobalF5RecordingTrigger] Failed to register global F5 hotkey! Win32 Error: {lastError} (0x{lastError:X})"
                );
                Debug.LogError(
                    "[GlobalF5RecordingTrigger] Common causes: Another app using F5, insufficient permissions, or invalid window handle"
                );
                isHotkeyRegistered = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[GlobalF5RecordingTrigger] Exception in StartGlobalHotkey: {e.Message}"
            );
            Debug.LogError($"[GlobalF5RecordingTrigger] Stack trace: {e.StackTrace}");
            isHotkeyRegistered = false;
        }

        Debug.Log($"[GlobalF5RecordingTrigger] Final registration status: {isHotkeyRegistered}");
    }

    private void MessageLoopWorker()
    {
        Debug.Log(
            $"[GlobalF5RecordingTrigger] Message loop thread started on ThreadId: {Thread.CurrentThread.ManagedThreadId}"
        );
        Debug.Log(
            $"[GlobalF5RecordingTrigger] Listening for WM_HOTKEY (0x{WM_HOTKEY:X}) with HOTKEY_ID {HOTKEY_ID}"
        );

        int messageCount = 0;
        int hotkeyMessageCount = 0;

        while (!shouldStop)
        {
            try
            {
                MSG msg;
                // Use PeekMessage with a timeout to avoid blocking indefinitely
                if (PeekMessage(out msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    messageCount++;

                    // Log every 10000 messages to show the loop is working (reduced from 1000)
                    if (messageCount % 10000 == 0)
                    {
                        Debug.Log(
                            $"[GlobalF5RecordingTrigger] Processed {messageCount} messages so far..."
                        );
                    }

                    // Only log hotkey messages to reduce spam
                    if (msg.message == WM_HOTKEY)
                    {
                        hotkeyMessageCount++;
                        int receivedHotkeyId = msg.wParam.ToInt32();
                        Debug.Log(
                            $"[GlobalF5RecordingTrigger] HOTKEY MESSAGE! ID: {receivedHotkeyId}, Expected: {HOTKEY_ID}"
                        );

                        if (receivedHotkeyId == HOTKEY_ID)
                        {
                            Debug.Log(
                                "[GlobalF5RecordingTrigger] F5 hotkey detected! Queuing action for main thread..."
                            );
                            // F5 hotkey was pressed! Queue the action for the main thread
                            lock (actionQueueLock)
                            {
                                mainThreadActions.Enqueue(() => OnF5Pressed());
                                Debug.Log(
                                    $"[GlobalF5RecordingTrigger] Action queued. Queue size: {mainThreadActions.Count}"
                                );
                            }
                        }
                        else
                        {
                            Debug.Log(
                                $"[GlobalF5RecordingTrigger] Different hotkey ID received: {receivedHotkeyId}"
                            );
                        }
                    }
                }

                // Small delay to prevent excessive CPU usage
                Thread.Sleep(10);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GlobalF5RecordingTrigger] Error in message loop: {e.Message}");
                Debug.LogError($"[GlobalF5RecordingTrigger] Stack trace: {e.StackTrace}");
                Thread.Sleep(100); // Longer delay on error
            }
        }

        Debug.Log(
            $"[GlobalF5RecordingTrigger] Message loop thread stopped. Total messages processed: {messageCount}, Hotkey messages: {hotkeyMessageCount}"
        );
    }

    private void ProcessMainThreadActions()
    {
        lock (actionQueueLock)
        {
            int actionsProcessed = 0;
            while (mainThreadActions.Count > 0)
            {
                try
                {
                    System.Action action = mainThreadActions.Dequeue();
                    Debug.Log(
                        $"[GlobalF5RecordingTrigger] Processing main thread action {actionsProcessed + 1}..."
                    );
                    action?.Invoke();
                    actionsProcessed++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(
                        $"[GlobalF5RecordingTrigger] Error executing main thread action: {e.Message}"
                    );
                    Debug.LogError($"[GlobalF5RecordingTrigger] Stack trace: {e.StackTrace}");
                }
            }

            if (actionsProcessed > 0)
            {
                Debug.Log(
                    $"[GlobalF5RecordingTrigger] Processed {actionsProcessed} main thread actions"
                );
            }
        }
    }

    private void OnF5Pressed()
    {
        Debug.Log("[GlobalF5RecordingTrigger] === OnF5Pressed() CALLED ===");
        Debug.Log($"[GlobalF5RecordingTrigger] enableGlobalF5Trigger: {enableGlobalF5Trigger}");
        Debug.Log($"[GlobalF5RecordingTrigger] whisperHandler is null: {whisperHandler == null}");

        if (!enableGlobalF5Trigger || whisperHandler == null)
        {
            Debug.LogWarning(
                "[GlobalF5RecordingTrigger] F5 press ignored - trigger disabled or whisperHandler is null"
            );
            return;
        }

        Debug.Log("[GlobalF5RecordingTrigger] Global F5 key pressed! Triggering recording...");

        // Update debug info
        lastF5PressTime = System.DateTime.Now.ToString("HH:mm:ss");
        Debug.Log($"[GlobalF5RecordingTrigger] Updated lastF5PressTime to: {lastF5PressTime}");

        // Call the WhisperHandler's TriggerRecording function
        try
        {
            Debug.Log("[GlobalF5RecordingTrigger] Calling whisperHandler.TriggerRecording()...");
            whisperHandler.TriggerRecording();
            Debug.Log(
                "[GlobalF5RecordingTrigger] whisperHandler.TriggerRecording() completed successfully"
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[GlobalF5RecordingTrigger] Error calling TriggerRecording: {e.Message}"
            );
            Debug.LogError($"[GlobalF5RecordingTrigger] Stack trace: {e.StackTrace}");
        }
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

    /// <summary>
    /// Test method to verify the script is working - call this from inspector button
    /// </summary>
    [ContextMenu("Test F5 Trigger")]
    public void TestF5Trigger()
    {
        Debug.Log("[GlobalF5RecordingTrigger] === MANUAL TEST TRIGGERED ===");
        OnF5Pressed();
    }

    /// <summary>
    /// Debug info method to show current status
    /// </summary>
    [ContextMenu("Show Debug Info")]
    public void ShowDebugInfo()
    {
        Debug.Log("[GlobalF5RecordingTrigger] === DEBUG INFO ===");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"enableGlobalF5Trigger: {enableGlobalF5Trigger}");
        Debug.Log($"isHotkeyRegistered: {isHotkeyRegistered}");
        Debug.Log($"windowHandle: {windowHandle} (0x{windowHandle.ToString("X")})");
        Debug.Log($"whisperHandler assigned: {whisperHandler != null}");
        Debug.Log($"messageLoopThread alive: {messageLoopThread?.IsAlive ?? false}");
        Debug.Log($"lastF5PressTime: {lastF5PressTime}");
        Debug.Log($"shouldStop: {shouldStop}");

        lock (actionQueueLock)
        {
            Debug.Log($"mainThreadActions queue size: {mainThreadActions.Count}");
        }
    }
}

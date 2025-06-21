using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class RecordKeyHook : MonoBehaviour
{
    [Header("Recording Trigger Settings")]
    [SerializeField]
    [Tooltip("Drag the WhisperHandler from the scene here")]
    private WhisperHandler whisperHandler;

    [SerializeField]
    [Tooltip("The key to use as global trigger (F5 = 116)")]
    private int triggerKeyCode = 116; // F5 key

    [SerializeField]
    [Tooltip("Enable/disable global key detection")]
    private bool enableGlobalTrigger = true;

    [Header("Debug Info")]
    [SerializeField]
    [Tooltip("Shows the last time the key was pressed")]
    private string lastKeyPressTime = "Never";

    [SerializeField]
    [Tooltip("Shows if the global hotkey is registered")]
    private bool isHotkeyRegistered = false;

    // Windows API declarations for RegisterHotKey approach
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

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
    private const int HOTKEY_ID = 9001;
    private const int MOD_NONE = 0x0000;
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
        Debug.Log("[RecordKeyHook] === STARTING GLOBAL KEY HOOK ===");
        Debug.Log($"[RecordKeyHook] Platform: {Application.platform}");
        Debug.Log(
            $"[RecordKeyHook] Trigger Key Code: {triggerKeyCode} (F5=116, F1=112, F2=113, etc.)"
        );

        // Check if we're on Windows
        if (
            Application.platform != RuntimePlatform.WindowsPlayer
            && Application.platform != RuntimePlatform.WindowsEditor
        )
        {
            Debug.LogError(
                "[RecordKeyHook] This script only works on Windows! Current platform: "
                    + Application.platform
            );
            return;
        }

        // Validate the whisperHandler reference
        if (whisperHandler == null)
        {
            Debug.LogWarning(
                "[RecordKeyHook] WhisperHandler not assigned! Please drag the WhisperHandler component to this script in the inspector."
            );
            return;
        }

        if (enableGlobalTrigger)
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
        Debug.Log("[RecordKeyHook] === STARTING GLOBAL HOTKEY REGISTRATION ===");

        try
        {
            windowHandle = GetActiveWindow();
            Debug.Log(
                $"[RecordKeyHook] Window handle: {windowHandle} (0x{windowHandle.ToString("X")})"
            );

            if (windowHandle == IntPtr.Zero)
            {
                Debug.LogError("[RecordKeyHook] Failed to get valid window handle!");
                return;
            }

            Debug.Log(
                $"[RecordKeyHook] Registering hotkey: ID={HOTKEY_ID}, Key=0x{triggerKeyCode:X}"
            );

            bool registrationResult = RegisterHotKey(
                windowHandle,
                HOTKEY_ID,
                MOD_NONE,
                triggerKeyCode
            );
            Debug.Log($"[RecordKeyHook] RegisterHotKey returned: {registrationResult}");

            if (registrationResult)
            {
                isHotkeyRegistered = true;
                Debug.Log(
                    $"[RecordKeyHook] Global key (code {triggerKeyCode}) registered successfully!"
                );

                messageLoopThread = new Thread(MessageLoopWorker)
                {
                    IsBackground = true,
                    Name = "GlobalKeyMessageLoop",
                };
                messageLoopThread.Start();
            }
            else
            {
                int lastError = Marshal.GetLastWin32Error();
                Debug.LogError(
                    $"[RecordKeyHook] Failed to register global hotkey! Win32 Error: {lastError} (0x{lastError:X})"
                );
                isHotkeyRegistered = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RecordKeyHook] Exception in StartGlobalHotkey: {e.Message}");
            isHotkeyRegistered = false;
        }
    }

    private void MessageLoopWorker()
    {
        Debug.Log(
            $"[RecordKeyHook] Message loop thread started, listening for key code {triggerKeyCode}"
        );

        while (!shouldStop)
        {
            try
            {
                MSG msg;
                if (PeekMessage(out msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == HOTKEY_ID)
                    {
                        Debug.Log($"[RecordKeyHook] Global key {triggerKeyCode} detected!");
                        lock (actionQueueLock)
                        {
                            mainThreadActions.Enqueue(() => OnGlobalKeyPressed());
                        }
                    }
                }
                Thread.Sleep(10);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RecordKeyHook] Error in message loop: {e.Message}");
                Thread.Sleep(100);
            }
        }

        Debug.Log("[RecordKeyHook] Message loop thread stopped");
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
                        $"[RecordKeyHook] Error executing main thread action: {e.Message}"
                    );
                }
            }
        }
    }

    private void OnGlobalKeyPressed()
    {
        Debug.Log($"[RecordKeyHook] Global key {triggerKeyCode} pressed! Triggering recording...");

        if (!enableGlobalTrigger || whisperHandler == null)
        {
            Debug.LogWarning(
                "[RecordKeyHook] Key press ignored - trigger disabled or whisperHandler is null"
            );
            return;
        }

        lastKeyPressTime = System.DateTime.Now.ToString("HH:mm:ss");

        try
        {
            whisperHandler.TriggerRecording();
            Debug.Log("[RecordKeyHook] whisperHandler.TriggerRecording() completed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RecordKeyHook] Error calling TriggerRecording: {e.Message}");
        }
    }

    private void StopGlobalHotkey()
    {
        try
        {
            shouldStop = true;

            if (messageLoopThread != null && messageLoopThread.IsAlive)
            {
                if (!messageLoopThread.Join(1000))
                {
                    Debug.LogWarning("[RecordKeyHook] Message loop thread did not stop gracefully");
                }
            }

            if (isHotkeyRegistered && windowHandle != IntPtr.Zero)
            {
                if (UnregisterHotKey(windowHandle, HOTKEY_ID))
                {
                    Debug.Log("[RecordKeyHook] Global hotkey unregistered successfully");
                }
            }

            isHotkeyRegistered = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RecordKeyHook] Error stopping global hotkey: {e.Message}");
        }
    }

    /// <summary>
    /// Test method - right click script in inspector and select this
    /// </summary>
    [ContextMenu("Test Key Trigger")]
    public void TestKeyTrigger()
    {
        Debug.Log("[RecordKeyHook] === MANUAL TEST TRIGGERED ===");
        OnGlobalKeyPressed();
    }

    /// <summary>
    /// Show current status - right click script in inspector and select this
    /// </summary>
    [ContextMenu("Show Status")]
    public void ShowStatus()
    {
        Debug.Log($"[RecordKeyHook] === STATUS ===");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Trigger Key Code: {triggerKeyCode}");
        Debug.Log($"enableGlobalTrigger: {enableGlobalTrigger}");
        Debug.Log($"isHotkeyRegistered: {isHotkeyRegistered}");
        Debug.Log($"whisperHandler assigned: {whisperHandler != null}");
        Debug.Log($"lastKeyPressTime: {lastKeyPressTime}");
    }

    private void OnDestroy()
    {
        StopGlobalHotkey();
    }

    private void OnApplicationQuit()
    {
        StopGlobalHotkey();
    }
}

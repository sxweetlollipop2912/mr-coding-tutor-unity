using System;
using System.Diagnostics;
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
    [Tooltip("Shows if the keyboard hook is installed")]
    private bool isHookInstalled = false;

    [SerializeField]
    [Tooltip("Count of key events processed")]
    private int keyEventsProcessed = 0;

    // Low-level keyboard hook approach
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        IntPtr hMod,
        uint dwThreadId
    );

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam
    );

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    // Hook constants
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int HC_ACTION = 0;

    // Structure for low-level keyboard input
    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // Delegate for the hook procedure
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    // Hook variables
    private LowLevelKeyboardProc hookProc;
    private IntPtr hookId = IntPtr.Zero;
    private bool keyPressed = false; // Track if key is currently down
    private bool hasTriggered = false; // Track if we've already triggered for this press cycle

    // Thread synchronization
    private readonly object keyPressLock = new object();

    private void Start()
    {
        UnityEngine.Debug.Log("[RecordKeyHook] === STARTING LOW-LEVEL KEYBOARD HOOK ===");
        UnityEngine.Debug.Log($"[RecordKeyHook] Platform: {Application.platform}");
        UnityEngine.Debug.Log($"[RecordKeyHook] Trigger Key Code: {triggerKeyCode} (F5=116)");

        // Check if we're on Windows
        if (
            Application.platform != RuntimePlatform.WindowsPlayer
            && Application.platform != RuntimePlatform.WindowsEditor
        )
        {
            UnityEngine.Debug.LogError(
                "[RecordKeyHook] This script only works on Windows! Current platform: "
                    + Application.platform
            );
            return;
        }

        // Validate the whisperHandler reference
        if (whisperHandler == null)
        {
            UnityEngine.Debug.LogWarning(
                "[RecordKeyHook] WhisperHandler not assigned! Please drag the WhisperHandler component to this script in the inspector."
            );
            return;
        }

        if (enableGlobalTrigger)
        {
            InstallKeyboardHook();
        }
    }

    private void InstallKeyboardHook()
    {
        UnityEngine.Debug.Log("[RecordKeyHook] Installing low-level keyboard hook...");

        try
        {
            // Create the hook procedure delegate
            hookProc = new LowLevelKeyboardProc(HookCallback);

            // Get current module handle
            IntPtr moduleHandle = GetModuleHandle(
                Process.GetCurrentProcess().MainModule.ModuleName
            );
            UnityEngine.Debug.Log(
                $"[RecordKeyHook] Module handle: {moduleHandle} (0x{moduleHandle.ToString("X")})"
            );

            // Install the hook
            hookId = SetWindowsHookEx(WH_KEYBOARD_LL, hookProc, moduleHandle, 0);

            if (hookId != IntPtr.Zero)
            {
                isHookInstalled = true;
                UnityEngine.Debug.Log(
                    $"[RecordKeyHook] Keyboard hook installed successfully! Hook ID: {hookId} (0x{hookId.ToString("X")})"
                );
                UnityEngine.Debug.Log(
                    $"[RecordKeyHook] Now listening globally for key code {triggerKeyCode}..."
                );
            }
            else
            {
                int lastError = Marshal.GetLastWin32Error();
                UnityEngine.Debug.LogError(
                    $"[RecordKeyHook] Failed to install keyboard hook! Win32 Error: {lastError} (0x{lastError:X})"
                );
                isHookInstalled = false;
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError(
                $"[RecordKeyHook] Exception installing keyboard hook: {e.Message}"
            );
            UnityEngine.Debug.LogError($"[RecordKeyHook] Stack trace: {e.StackTrace}");
            isHookInstalled = false;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= HC_ACTION)
            {
                // Parse the keyboard structure
                KBDLLHOOKSTRUCT kbd = (KBDLLHOOKSTRUCT)
                    Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                // Check for our trigger key first
                bool isOurTriggerKey = (kbd.vkCode == triggerKeyCode);
                bool isKeyDown = (wParam.ToInt32() == WM_KEYDOWN);
                bool isKeyUp = (wParam.ToInt32() == WM_KEYUP);

                if (isOurTriggerKey)
                {
                    if (isKeyDown)
                    {
                        lock (keyPressLock)
                        {
                            if (!keyPressed) // First time pressing down (not a repeat from holding)
                            {
                                keyPressed = true;
                                hasTriggered = false;

                                // Count this as a genuine key event
                                keyEventsProcessed++;

                                UnityEngine.Debug.Log(
                                    $"[RecordKeyHook] Target key {triggerKeyCode} pressed down - triggering action! (Event #{keyEventsProcessed})"
                                );

                                // Trigger action immediately on key down
                                UnityMainThreadDispatcher
                                    .Instance()
                                    .Enqueue(() => OnTargetKeyPressed());

                                hasTriggered = true;
                            }
                            else
                            {
                                // Key is being held down - don't count or trigger
                                UnityEngine.Debug.Log(
                                    $"[RecordKeyHook] Target key {triggerKeyCode} held down (ignored)"
                                );
                            }
                        }
                    }
                    else if (isKeyUp)
                    {
                        lock (keyPressLock)
                        {
                            if (keyPressed)
                            {
                                keyPressed = false; // Reset for next press cycle
                                UnityEngine.Debug.Log(
                                    $"[RecordKeyHook] Target key {triggerKeyCode} released - ready for next trigger"
                                );
                            }
                        }
                    }
                }
                else
                {
                    // For non-trigger keys, count normally but only on key down to avoid duplicates
                    if (isKeyDown)
                    {
                        keyEventsProcessed++;

                        // Log every 100 non-trigger key events to show the hook is working
                        if (keyEventsProcessed % 100 == 0)
                        {
                            UnityEngine.Debug.Log(
                                $"[RecordKeyHook] Processed {keyEventsProcessed} key events so far..."
                            );
                        }
                    }
                }

                // Log the key event details (uncomment for detailed debugging)
                // UnityEngine.Debug.Log($"[RecordKeyHook] Key event: vkCode={kbd.vkCode}, wParam=0x{wParam.ToInt32():X}");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[RecordKeyHook] Exception in hook callback: {e.Message}");
        }

        // Always call the next hook
        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    private void OnTargetKeyPressed()
    {
        UnityEngine.Debug.Log($"[RecordKeyHook] === TARGET KEY {triggerKeyCode} DETECTED ===");

        if (!enableGlobalTrigger || whisperHandler == null)
        {
            UnityEngine.Debug.LogWarning(
                "[RecordKeyHook] Key press ignored - trigger disabled or whisperHandler is null"
            );
            return;
        }

        lastKeyPressTime = System.DateTime.Now.ToString("HH:mm:ss");
        UnityEngine.Debug.Log($"[RecordKeyHook] Updated lastKeyPressTime to: {lastKeyPressTime}");

        try
        {
            UnityEngine.Debug.Log("[RecordKeyHook] Calling whisperHandler.TriggerRecording()...");
            whisperHandler.TriggerRecording();
            UnityEngine.Debug.Log(
                "[RecordKeyHook] whisperHandler.TriggerRecording() completed successfully"
            );
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError(
                $"[RecordKeyHook] Error calling TriggerRecording: {e.Message}"
            );
            UnityEngine.Debug.LogError($"[RecordKeyHook] Stack trace: {e.StackTrace}");
        }
    }

    private void UninstallKeyboardHook()
    {
        if (hookId != IntPtr.Zero)
        {
            UnityEngine.Debug.Log("[RecordKeyHook] Uninstalling keyboard hook...");

            bool result = UnhookWindowsHookEx(hookId);
            if (result)
            {
                UnityEngine.Debug.Log("[RecordKeyHook] Keyboard hook uninstalled successfully");
            }
            else
            {
                int lastError = Marshal.GetLastWin32Error();
                UnityEngine.Debug.LogWarning(
                    $"[RecordKeyHook] Failed to uninstall keyboard hook. Error: {lastError}"
                );
            }

            hookId = IntPtr.Zero;
            isHookInstalled = false;
        }
    }

    /// <summary>
    /// Test method - right click script in inspector and select this
    /// </summary>
    [ContextMenu("Test Key Trigger")]
    public void TestKeyTrigger()
    {
        UnityEngine.Debug.Log("[RecordKeyHook] === MANUAL TEST TRIGGERED ===");
        OnTargetKeyPressed();
    }

    /// <summary>
    /// Show current status - right click script in inspector and select this
    /// </summary>
    [ContextMenu("Show Status")]
    public void ShowStatus()
    {
        UnityEngine.Debug.Log($"[RecordKeyHook] === STATUS ===");
        UnityEngine.Debug.Log($"Platform: {Application.platform}");
        UnityEngine.Debug.Log($"Trigger Key Code: {triggerKeyCode}");
        UnityEngine.Debug.Log($"enableGlobalTrigger: {enableGlobalTrigger}");
        UnityEngine.Debug.Log($"isHookInstalled: {isHookInstalled}");
        UnityEngine.Debug.Log($"hookId: {hookId} (0x{hookId.ToString("X")})");
        UnityEngine.Debug.Log($"whisperHandler assigned: {whisperHandler != null}");
        UnityEngine.Debug.Log($"lastKeyPressTime: {lastKeyPressTime}");
        UnityEngine.Debug.Log($"keyEventsProcessed: {keyEventsProcessed}");
    }

    private void OnDestroy()
    {
        UninstallKeyboardHook();
    }

    private void OnApplicationQuit()
    {
        UninstallKeyboardHook();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        UnityEngine.Debug.Log($"[RecordKeyHook] Application paused: {pauseStatus}");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        UnityEngine.Debug.Log($"[RecordKeyHook] Application focus: {hasFocus}");
        // The hook should continue working regardless of focus
    }
}

// Unity Main Thread Dispatcher helper class
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly System.Collections.Generic.Queue<System.Action> _executionQueue =
        new System.Collections.Generic.Queue<System.Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Enqueue(System.Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}

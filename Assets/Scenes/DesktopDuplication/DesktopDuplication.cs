using System.Collections.Generic;
using HyperDesktopDuplication;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates UI (RawImages) under a user-provided Canvas for each captured desktop.
/// Keeps DesktopRenderer & MouseRenderer enabled with alpha=0 so
/// the library calls TakeCapture() each frame.
/// Flips the desktop's image vertically if needed,
/// but the mouse pointer image is not flipped.
/// Instead, we flip its vertical motion in code.
/// </summary>
public class DesktopDuplication : MonoBehaviour
{
    // Scale factor for converting from monitor pixels to UI coordinates.
    const float scale = 1f;

    [Header("Drag & Drop Your Canvas Here")]
    [SerializeField]
    private Canvas monitorCanvas; // Assign in Inspector

    // We'll store references so we can update mouse pointer each frame
    private List<MonitorUIData> monitorUIList = new List<MonitorUIData>();

    // Helper struct to keep track of each monitor's data
    private struct MonitorUIData
    {
        public GameObject monitorObj; // "Monitor X" object from HDD_Manager
        public RectTransform monitorUI; // The root UI Rect for that monitor
        public RawImage mouseUI; // The UI RawImage for the mouse
        public float monitorWidth; // In UI coords (info.Right - info.Left) / scale
        public float monitorHeight; // In UI coords (info.Bottom - info.Top) / scale
        public int pixelWidth; // info.PixelWidth
        public int pixelHeight; // info.PixelHeight
    }

    async void Start()
    {
        if (monitorCanvas == null)
        {
            Debug.LogError(
                "No Canvas assigned to DesktopDuplication. Please assign one in the Inspector."
            );
            return;
        }

        var manager = GetComponent<HDD_Manager>();
        if (manager == null)
        {
            Debug.LogError("HDD_Manager not found on this GameObject. Cannot proceed.");
            return;
        }

        // Refresh the monitors
        await manager.Refresh();

        // Compute center offset for the primary monitor
        var primaryCenter = Vector3.zero;
        for (int i = 0; i < manager.Monitors.Count; ++i)
        {
            var info = manager.Monitors[i];
            if (info.IsPrimary)
            {
                primaryCenter =
                    new Vector3(
                        (info.Right - info.Left) / 2 + info.Left,
                        (info.Top - info.Bottom) / 2 + info.Bottom,
                        0
                    ) / scale;
                break;
            }
        }
        Debug.Log($"Primary monitor center: {primaryCenter}");

        Debug.Log($"Creating {manager.Monitors.Count} monitors");
        for (int i = 0; i < manager.Monitors.Count; ++i)
        {
            var info = manager.Monitors[i];
            var monitorObj = manager.CreateMonitor(i);
            if (monitorObj == null)
            {
                Debug.LogError($"manager.CreateMonitor({i}) returned null. Skipping this monitor.");
                continue;
            }

            // Position in 3D
            monitorObj.transform.localScale = new Vector3(1 / scale, 1 / scale, 1);
            monitorObj.transform.localPosition =
                new Vector3(
                    (info.Right - info.Left) / 2 + info.Left,
                    (info.Top - info.Bottom) / 2 + info.Bottom,
                    0
                ) / scale
                - primaryCenter;

            // DesktopRenderer: invisible but enabled
            var desktopRendererTF = monitorObj.transform.Find("DesktopRenderer");
            Texture desktopTexture = null;
            if (desktopRendererTF != null)
            {
                var mesh = desktopRendererTF.GetComponent<Renderer>();
                if (mesh != null && mesh.material != null)
                {
                    mesh.enabled = true;
                    desktopTexture = mesh.material.mainTexture;
                    var c = mesh.material.color;
                    mesh.material.color = new Color(c.r, c.g, c.b, 0f); // alpha=0
                }
            }

            // MouseRenderer: invisible but enabled
            var mouseRendererTF = monitorObj.transform.Find("MouseRenderer");
            Texture mouseTexture = null;
            if (mouseRendererTF != null)
            {
                var mesh = mouseRendererTF.GetComponent<Renderer>();
                if (mesh != null && mesh.material != null)
                {
                    mesh.enabled = true;
                    mouseTexture = mesh.material.mainTexture;
                    var c = mesh.material.color;
                    mesh.material.color = new Color(c.r, c.g, c.b, 0f); // alpha=0
                }
            }

            // Create the UI
            var monitorData = CreateMonitorUI(
                i,
                monitorObj,
                info,
                desktopTexture,
                mouseTexture,
                monitorObj.transform.localPosition
            );

            // Store so we can update pointer each frame
            monitorUIList.Add(monitorData);
        }
    }

    /// <summary>
    /// Update pointer texture (in case the library changed it)
    /// and pointer position, flipping the Y motion.
    /// </summary>
    void Update()
    {
        foreach (var data in monitorUIList)
        {
            if (data.monitorObj == null)
                continue;

            // 1) Re-fetch the mouse pointer texture if changed
            var mouseRendererTF = data.monitorObj.transform.Find("MouseRenderer");
            if (mouseRendererTF != null)
            {
                var mesh = mouseRendererTF.GetComponent<Renderer>();
                if (mesh != null && mesh.material != null && data.mouseUI != null)
                {
                    var currentPointerTex = mesh.material.mainTexture;
                    if (data.mouseUI.texture != currentPointerTex)
                    {
                        data.mouseUI.texture = currentPointerTex;
                        // We'll use the default uvRect (no vertical flip on the pointer image)
                        data.mouseUI.uvRect = new Rect(0f, 0f, 1f, -1f);
                    }
                }
            }

            // 2) Sync the pointer position, flipping the Y-axis
            if (data.mouseUI != null && mouseRendererTF != null)
            {
                var localMousePos = mouseRendererTF.localPosition;

                float ratioX = data.monitorWidth / data.pixelWidth;
                float ratioY = data.monitorHeight / data.pixelHeight;

                // Flip the Y coordinate by multiplying by -1
                Vector2 anchoredPos = new Vector2(
                    localMousePos.x * ratioX,
                    -(localMousePos.y * ratioY)
                );

                data.mouseUI.rectTransform.anchoredPosition = anchoredPos;
            }
        }
    }

    /// <summary>
    /// Creates a UI container with the desktop + mouse RawImages.
    /// Flip the desktop texture if needed, but do NOT flip the mouse image.
    /// We flip the mouse's *position* in Update().
    /// </summary>
    private MonitorUIData CreateMonitorUI(
        int monitorIndex,
        GameObject monitorObj,
        Shremdup.DisplayInfo info,
        Texture desktopTexture,
        Texture mouseTexture,
        Vector3 position
    )
    {
        // Root UI
        GameObject monitorUI = new GameObject($"MonitorUI_{monitorIndex}");
        monitorUI.transform.SetParent(monitorCanvas.transform, false);

        RectTransform rt = monitorUI.AddComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localPosition = position;

        float monitorWidth = (info.Right - info.Left) / scale;
        float monitorHeight = (info.Bottom - info.Top) / scale;
        rt.sizeDelta = new Vector2(monitorWidth, monitorHeight);

        // Desktop RawImage
        GameObject desktopGO = new GameObject("DesktopRawImage");
        desktopGO.transform.SetParent(monitorUI.transform, false);

        RectTransform desktopRT = desktopGO.AddComponent<RectTransform>();
        desktopRT.anchorMin = Vector2.zero;
        desktopRT.anchorMax = Vector2.one;
        desktopRT.offsetMin = Vector2.zero;
        desktopRT.offsetMax = Vector2.zero;
        desktopRT.pivot = new Vector2(0.5f, 0.5f);

        RawImage desktopImage = desktopGO.AddComponent<RawImage>();
        if (desktopTexture != null)
        {
            desktopImage.texture = desktopTexture;
            // Flip the desktop vertically if your desktop is upside-down
            desktopImage.uvRect = new Rect(0f, 1f, 1f, -1f);
        }
        else
        {
            Debug.LogError(
                $"Desktop texture for monitor {monitorIndex} is null. UI will be blank."
            );
        }

        // Mouse RawImage
        GameObject mouseGO = new GameObject("MouseRawImage");
        mouseGO.transform.SetParent(monitorUI.transform, false);

        RectTransform mouseRT = mouseGO.AddComponent<RectTransform>();
        mouseRT.sizeDelta = new Vector2(20, 20);
        mouseRT.anchoredPosition = Vector2.zero;
        mouseRT.pivot = new Vector2(0.5f, 0.5f);

        RawImage mouseImage = mouseGO.AddComponent<RawImage>();
        mouseImage.raycastTarget = false;
        if (mouseTexture != null)
        {
            mouseImage.texture = mouseTexture;
            // No vertical flip on the pointer image
            mouseImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
        else
        {
            Debug.LogWarning(
                $"Mouse texture for monitor {monitorIndex} is null initially. Will update later."
            );
        }

        Debug.Log($"Created Monitor UI for monitor {monitorIndex}");

        // Return data for updates
        return new MonitorUIData
        {
            monitorObj = monitorObj,
            monitorUI = rt,
            mouseUI = mouseImage,
            monitorWidth = monitorWidth,
            monitorHeight = monitorHeight,
            pixelWidth = (int)info.PixelWidth,
            pixelHeight = (int)info.PixelHeight,
        };
    }
}

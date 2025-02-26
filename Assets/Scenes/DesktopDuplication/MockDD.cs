using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MockDD : MonoBehaviour
{
    [SerializeField]
    private RawImage rawImage;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }

    /// <summary>
    /// Captures the screen and saves it to a PNG file.
    /// If filePath is null, it only returns the byte array.
    /// </summary>
    public byte[] CaptureScreenToPNG(string filePath = null)
    {
        Texture2D capturedTexture = CaptureScreen();
        if (capturedTexture == null)
        {
            Debug.LogError("CaptureScreenToPNG: Failed to capture screen.");
            return null;
        }

        byte[] pngData = capturedTexture.EncodeToPNG();
        DestroyImmediate(capturedTexture); // Clean up

        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                // streaming assets path
                filePath = Path.Combine(Application.streamingAssetsPath, filePath);
                File.WriteAllBytes(filePath, pngData);
                Debug.Log($"CaptureScreenToPNG: Saved desktop to '{filePath}'.");
            }
            catch (Exception e)
            {
                Debug.LogError($"CaptureScreenToPNG: Failed to write file. {e}");
                return null;
            }
        }

        return pngData;
    }

    /// <summary>
    /// Captures the screen and returns a Base64 encoded string of the PNG data.
    /// </summary>
    public string CaptureScreenToBase64()
    {
        byte[] pngData = CaptureScreenToPNG("CapturedScreen.png");
        if (pngData == null)
        {
            Debug.LogError("CaptureScreenToBase64: Failed to capture screen or encode to PNG.");
            return null;
        }

        try
        {
            return Convert.ToBase64String(pngData);
        }
        catch (Exception e)
        {
            Debug.LogError($"CaptureScreenToBase64: Failed to convert to Base64. {e}");
            return null;
        }
    }

    /// <summary>
    /// Captures the screen from the desktop RawImage and returns a Texture2D.
    /// </summary>
    public Texture2D CaptureScreen()
    {
        // Find the RawImage displaying the desktop
        if (rawImage == null || rawImage.texture == null)
        {
            Debug.LogError("Desktop RawImage not found or texture is null!");
            return null;
        }

        Texture srcTexture = rawImage.texture;
        RenderTexture prevRT = RenderTexture.active;
        Texture2D exportTex = null;

        // --- 1) Read pixels from the source texture ---
        if (srcTexture is RenderTexture rt)
        {
            // RenderTexture case (most common in Desktop Duplication)
            RenderTexture.active = rt;

            exportTex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            exportTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            exportTex.Apply();
        }
        else if (srcTexture is Texture2D tex2D)
        {
            // Direct Texture2D copy if it's marked readable
            try
            {
                exportTex = new Texture2D(tex2D.width, tex2D.height, TextureFormat.ARGB32, false);
                exportTex.SetPixels(tex2D.GetPixels());
                exportTex.Apply();
            }
            catch (System.Exception)
            {
                Debug.LogWarning(
                    "CaptureScreen: Could not copy pixels from Texture2D. "
                        + "Make sure it is marked 'readable'."
                );
                return null;
            }
        }
        else
        {
            Debug.LogWarning(
                "CaptureScreen: Unknown texture type. Expected RenderTexture or Texture2D."
            );
            return null;
        }

        RenderTexture.active = prevRT; // restore

        return exportTex;
    }
}

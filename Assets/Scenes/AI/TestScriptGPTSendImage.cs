using System.Collections;
using System.IO;
using UnityEngine;

public class TestScriptGPTSendImage : MonoBehaviour
{
    public GPTHandler GPTHandler;
    public string testImageFilename = "tmp.png";
    public string testMessage = "What is this image?";

    private bool configsLoaded = false;

    void Start()
    {
        StartCoroutine(LoadAndSend());
    }

    IEnumerator LoadAndSend()
    {
        // Wait until GPTHandler's configurations are loaded
        while (!GPTHandler.ConfigsLoaded)
        {
            Debug.Log("Waiting for GPTHandler configs to load...");
            yield return null; // Wait for the next frame
        }

        Debug.Log("GPTHandler configs loaded, proceeding...");

        if (GPTHandler == null)
        {
            Debug.LogError("ChatGPTHandler not assigned. Please assign in the inspector.");
            yield break;
        }

        string imagePath = Path.Combine(Application.streamingAssetsPath, testImageFilename);

        if (!File.Exists(imagePath))
        {
            Debug.LogError("Image file not found: " + imagePath);
            yield break;
        }

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string base64Image = System.Convert.ToBase64String(imageBytes);

        GPTHandler.SendTextAndImageToGPT(testMessage, base64Image);
    }
}

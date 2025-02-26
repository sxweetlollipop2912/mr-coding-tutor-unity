using System.Collections;
using UnityEngine;

public class MockWhisperTrigger : MonoBehaviour
{
    [SerializeField]
    private WhisperHandler whisperHandler;

    private float startTime;
    private bool recordingStarted = false;

    IEnumerator Start()
    {
        // Wait for 1 second without blocking the main thread
        yield return new WaitForSeconds(1f);
        startTime = Time.time;

        Debug.Log("Starting recording..., time: " + startTime);
        whisperHandler.StartRecording();
        recordingStarted = true;
    }

    void Update()
    {
        if (recordingStarted)
        {
            if (Time.time - startTime >= 10f)
            {
                Debug.Log("Stopping recording..., time: " + Time.time);
                whisperHandler.StopRecording();
                recordingStarted = false; // Ensure this only runs once
            }
        }
    }
}

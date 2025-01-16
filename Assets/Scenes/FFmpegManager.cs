using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;

public class FFmpegManager : MonoBehaviour
{
    [SerializeField]
    private RenderTexture renderTexture; // The RenderTexture to write the captured frames into

    [SerializeField]
    private string ffmpegPath; // Path to FFmpeg binary, configurable via config.json

    private Process ffmpegProcess;
    private Thread captureThread;
    private Texture2D frameTexture;
    private byte[] currentFrameBuffer;
    private bool newFrameAvailable;
    private bool isRunning;

    private int textureWidth;
    private int textureHeight;

    void Start()
    {
        // Validate RenderTexture
        if (renderTexture == null)
        {
            UnityEngine.Debug.LogError("RenderTexture is not assigned.");
            return;
        }

        // Fetch dimensions from RenderTexture
        textureWidth = renderTexture.width;
        textureHeight = renderTexture.height;

        // Initialize a Texture2D with dimensions matching the RenderTexture
        frameTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);

        // Start FFmpeg capture
        StartFFmpeg();
    }

    private void StartFFmpeg()
    {
        // Adjust screen capture parameters as needed
        string arguments =
            $"-f avfoundation -framerate 30 -pixel_format bgr0 -i \"1\" -f rawvideo -pix_fmt bgr24 -";

        // Start the FFmpeg process
        ffmpegProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        try
        {
            ffmpegProcess.Start();
            isRunning = true;

            // Start reading FFmpeg output in a background thread
            captureThread = new Thread(CaptureScreen) { IsBackground = true };
            captureThread.Start();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to start FFmpeg: {e.Message}");
        }
    }

    private void CaptureScreen()
    {
        try
        {
            BinaryReader reader = new BinaryReader(ffmpegProcess.StandardOutput.BaseStream);

            while (isRunning)
            {
                // Buffer to read frame data
                int frameSize = textureWidth * textureHeight * 3; // Width x Height x 3 (RGB channels)
                byte[] buffer = new byte[frameSize];

                // Read raw frame data
                int bytesRead = reader.Read(buffer, 0, frameSize);

                if (bytesRead > 0)
                {
                    lock (this)
                    {
                        currentFrameBuffer = buffer;
                        newFrameAvailable = true;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error during screen capture: {e.Message}");
        }
    }

    void Update()
    {
        // Check if a new frame is available
        if (newFrameAvailable)
        {
            lock (this)
            {
                newFrameAvailable = false;

                // Update the Texture2D with the new frame data
                frameTexture.LoadRawTextureData(currentFrameBuffer);
                frameTexture.Apply();

                // Render to RenderTexture
                RenderToTexture();
            }
        }
    }

    private void RenderToTexture()
    {
        RenderTexture.active = renderTexture;
        Graphics.Blit(frameTexture, renderTexture);
        RenderTexture.active = null;
    }

    private void OnApplicationQuit()
    {
        // Stop FFmpeg and cleanup
        isRunning = false;

        if (ffmpegProcess != null && !ffmpegProcess.HasExited)
        {
            ffmpegProcess.Kill();
        }

        if (captureThread != null && captureThread.IsAlive)
        {
            captureThread.Join(); // Wait for thread to finish
        }
    }
}

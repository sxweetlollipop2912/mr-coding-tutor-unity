using System.Diagnostics;
using UnityEngine;

public class AdminCheck : MonoBehaviour
{
    void Start()
    {
        if (IsRunningAsAdmin())
        {
            UnityEngine.Debug.Log("Unity is running with administrative privileges.");
        }
        else
        {
            UnityEngine.Debug.LogError("Unity is NOT running with administrative privileges.");
        }
    }

    bool IsRunningAsAdmin()
    {
        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "whoami",
                Arguments = "/priv",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output.Contains("SeDebugPrivilege");
    }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class AIProgressStatus : MonoBehaviour
{
    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private Transform labelObject;

    // Define the steps in the AI processing workflow
    public enum AIStep
    {
        Listening,
        ProcessingAudio,
        ConvertingSpeechToText,
        SendingToAI,
        ProcessingWithAI,
        ProcessingAIResponse,
        ConvertingToSpeech,
        ProcessingAudioResponse,
        PlayingResponse,
        Error,
        Idle,
    }

    // User-friendly step descriptions
    private Dictionary<AIStep, string> stepDescriptions = new Dictionary<AIStep, string>
    {
        { AIStep.Listening, "Listening to you..." },
        { AIStep.ProcessingAudio, "Processing your voice..." },
        { AIStep.ConvertingSpeechToText, "Understanding what you said..." },
        { AIStep.SendingToAI, "Sending to AI tutor..." },
        { AIStep.ProcessingWithAI, "AI tutor is thinking..." },
        { AIStep.ProcessingAIResponse, "Preparing response..." },
        { AIStep.ConvertingToSpeech, "Getting ready to talk..." },
        { AIStep.ProcessingAudioResponse, "Almost ready to talk..." },
        { AIStep.PlayingResponse, "" },
        { AIStep.Error, "Error occurred" },
        { AIStep.Idle, "" },
    };

    // Total number of steps in the normal workflow (excluding Error and Idle)
    private const int TotalSteps = 9;

    // Current step
    private AIStep currentStep = AIStep.Idle;

    void Start()
    {
        // Initialize label visibility based on initial content
        UpdateLabelVisibility();
    }

    void Update()
    {
        // No need to check constantly as we'll update visibility when the label changes
    }

    // Method to update label using AIStep directly
    public void UpdateStep(AIStep step, string additionalInfo = null)
    {
        currentStep = step;

        if (step == AIStep.Error && !string.IsNullOrEmpty(additionalInfo))
        {
            // For error messages, show the detailed error
            label.text = $"Error: {additionalInfo}";
        }
        else if (step != AIStep.Idle)
        {
            // Format the user-friendly message with step counter
            int stepNumber = (int)step + 1; // +1 because enum is zero-based
            string stepDescription = stepDescriptions[step];
            label.text = $"{stepDescription} ({stepNumber}/{TotalSteps})";
        }
        else
        {
            // Idle state - clear the label
            label.text = "";
        }

        UpdateLabelVisibility();
    }

    private void UpdateLabelVisibility()
    {
        // Enable/disable based on whether label has content
        labelObject.gameObject.SetActive(!string.IsNullOrEmpty(label.text));
    }
}

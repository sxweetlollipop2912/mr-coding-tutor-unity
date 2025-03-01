using TMPro;
using UnityEngine;

public class AIProgressStatus : MonoBehaviour
{
    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private Transform labelObject;

    void Start()
    {
        // Initialize label visibility based on initial content
        UpdateLabelVisibility();
    }

    void Update()
    {
        // No need to check constantly as we'll update visibility when the label changes
    }

    public void UpdateLabel(string status)
    {
        label.text = status;
        UpdateLabelVisibility();
    }

    private void UpdateLabelVisibility()
    {
        // Enable/disable based on whether label has content
        labelObject.gameObject.SetActive(!string.IsNullOrEmpty(label.text));
    }
}

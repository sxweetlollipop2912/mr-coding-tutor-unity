using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AIProgressStatus : MonoBehaviour
{
    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private Transform labelObject;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (label.text == "")
        {

        }
    }

    void UpdateLabel(string status)
    {
        label.text = status;
    }
}

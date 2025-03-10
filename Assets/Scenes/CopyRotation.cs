using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyRotation : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The transform whose local rotation will be copied")]
    private Transform targetTransform;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update()
    {
        if (targetTransform != null)
        {
            transform.localRotation = targetTransform.localRotation;
        }
    }
}

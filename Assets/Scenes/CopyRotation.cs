using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyRotation : MonoBehaviour
{
    [SerializeField]
    private Transform targetTransform;

    [Tooltip("If true, will copy the world rotation. If false, will copy the local rotation.")]
    [SerializeField]
    private bool useWorldRotation = true;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update()
    {
        if (targetTransform != null)
        {
            if (useWorldRotation)
            {
                transform.rotation = targetTransform.rotation;
            }
            else
            {
                transform.localRotation = targetTransform.localRotation;
            }
        }
    }
}

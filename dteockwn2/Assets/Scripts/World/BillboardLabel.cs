using UnityEngine;

/// <summary>Rotates the GameObject each frame to face the main camera.</summary>
public class BillboardLabel : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        transform.rotation = cam.transform.rotation;
    }
}

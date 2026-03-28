using UnityEngine;

// Sets the main camera background to a solid color.
// Attach this component to any GameObject in the scene (e.g. the camera itself).
[RequireComponent(typeof(Camera))]
public class CameraBackground : MonoBehaviour
{
    [SerializeField] private Color _backgroundColor = Color.black;

    private void Awake()
    {
        Camera cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = _backgroundColor;
    }
}

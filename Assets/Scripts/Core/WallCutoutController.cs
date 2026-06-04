using UnityEngine;

// Scales the stencil mask sphere based on whether a wall occludes the camera's view of the player.
// When occluded, the sphere is placed ON the wall at the raycast hit point (not on the player),
// so the cutout only punches through the specific wall blocking the view rather than cutting
// all walls within radius of the player.
//
//
// Setup:
//   1. Attach to the Player root GameObject.
//   2. Assign the MaskSphere child transform (sphere mesh with StencilMask material).
//   3. Set OccluderMask to the layer(s) used by room/wall meshes.
public class WallCutoutController : MonoBehaviour
{
    [SerializeField] private Transform maskSphere;
    [SerializeField] private float maskRadius = 3f;
    [SerializeField] private float lerpSpeed = 8f;
    [SerializeField] private LayerMask occluderMask;

    private Camera mainCamera;
    private float currentRadius = 0f;

    private void Start()
    {
        mainCamera = Camera.main;
        if (maskSphere != null)
            maskSphere.localScale = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (maskSphere == null || mainCamera == null) return;

        Vector3 camPos = mainCamera.transform.position;
        Vector3 toPlayer = transform.position - camPos;
        float dist = toPlayer.magnitude;

        float targetRadius = 0f;
        if (Physics.Raycast(camPos, toPlayer.normalized, out RaycastHit hit, dist, occluderMask))
        {
            maskSphere.position = hit.point;
            targetRadius = maskRadius;
        }

        currentRadius = Mathf.Lerp(currentRadius, targetRadius, Time.deltaTime * lerpSpeed);
        maskSphere.localScale = Vector3.one * currentRadius;
    }
}

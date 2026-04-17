using UnityEngine;

// Scales the stencil mask sphere based on whether a wall occludes the camera's view of the player.
// When occluded, the sphere becomes visible (in stencil only) and punches a circular hole
// through wall renderers that use the WallCutout shader (Stencil Comp NotEqual).
// The floor child on each room uses a standard material and is unaffected by the stencil.
//
// Setup:
//   1. Attach to the Player root GameObject.
//   2. Assign the MaskSphere child transform (sphere mesh with StencilMask material).
//   3. Set OccluderMask to the layer(s) used by room/wall meshes.
public class WallCutoutController : MonoBehaviour
{
    [SerializeField] private Transform _maskSphere;
    [SerializeField] private float _maskRadius   = 3f;
    [SerializeField] private LayerMask _occluderMask;

    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
        if (_maskSphere != null)
            _maskSphere.localScale = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (_maskSphere == null || _camera == null) return;

        Vector3 camPos = _camera.transform.position;
        Vector3 toPlayer = transform.position - camPos;
        float dist = toPlayer.magnitude;

        bool blocked = Physics.Raycast(camPos, toPlayer.normalized, dist, _occluderMask);
        _maskSphere.localScale = Vector3.one * (blocked ? _maskRadius : 0f);
    }
}

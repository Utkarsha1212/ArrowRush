using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraHandler : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(1f)] private float framingPadding = 1.2f;
    [SerializeField, Min(0.1f)] private float minimumOrthographicSize = 3.5f;
    [SerializeField, Min(0.1f)] private float maximumOrthographicSize = 14f;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    /// <summary>Frames the actual generated puzzle bounds while keeping the camera's current view angle.</summary>
    public void FramePuzzle(Bounds puzzleBounds)
    {
        if (targetCamera == null)
            return;

        Vector3 center = puzzleBounds.center;
        Transform cameraTransform = targetCamera.transform;
        Vector3 cameraRight = cameraTransform.right;
        Vector3 cameraUp = cameraTransform.up;
        Vector3 extents = puzzleBounds.extents;
        float halfWidth = Mathf.Abs(cameraRight.x) * extents.x
                        + Mathf.Abs(cameraRight.y) * extents.y
                        + Mathf.Abs(cameraRight.z) * extents.z;
        float halfHeight = Mathf.Abs(cameraUp.x) * extents.x
                         + Mathf.Abs(cameraUp.y) * extents.y
                         + Mathf.Abs(cameraUp.z) * extents.z;

        float aspect = Mathf.Max(0.1f, targetCamera.aspect);
        float requiredSize = Mathf.Max(halfHeight, halfWidth / aspect) * framingPadding;
        float size = Mathf.Clamp(requiredSize, minimumOrthographicSize, maximumOrthographicSize);

        // Keep the existing camera orientation and distance, but recenter it on the measured puzzle.
        float distance = Mathf.Max(0.1f, Vector3.Distance(cameraTransform.position, center));
        cameraTransform.position = center - cameraTransform.forward * distance;

        if (targetCamera.orthographic)
            targetCamera.orthographicSize = size;
        else
            targetCamera.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(requiredSize / distance) * Mathf.Rad2Deg, 20f, 70f);
    }
}

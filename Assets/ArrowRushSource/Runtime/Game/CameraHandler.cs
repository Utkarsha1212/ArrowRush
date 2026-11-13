using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class CameraHandler : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(1f)] private float framingPadding = 1.2f;
    [SerializeField, Min(0.1f)] private float minimumOrthographicSize = 3.5f;
    [SerializeField, Min(0.1f)] private float maximumOrthographicSize = 14f;
    [SerializeField, Range(0.1f, 2f)] private float zoomSensitivity = 1f;

    private float previousPinchDistance;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (targetCamera == null || !targetCamera.orthographic || TabCube.InputLocked)
        {
            previousPinchDistance = 0f;
            return;
        }

        if (Input.touchCount != 2)
        {
            previousPinchDistance = 0f;
            return;
        }

        Touch first = Input.GetTouch(0);
        Touch second = Input.GetTouch(1);
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null &&
            (eventSystem.IsPointerOverGameObject(first.fingerId) || eventSystem.IsPointerOverGameObject(second.fingerId)))
        {
            previousPinchDistance = 0f;
            return;
        }

        float currentDistance = Vector2.Distance(first.position, second.position);
        if (previousPinchDistance > 0f && currentDistance > 0f)
        {
            float zoomRatio = previousPinchDistance / currentDistance;
            float zoomFactor = Mathf.Pow(zoomRatio, zoomSensitivity);
            targetCamera.orthographicSize = Mathf.Clamp(
                targetCamera.orthographicSize * zoomFactor,
                minimumOrthographicSize,
                maximumOrthographicSize);
        }

        previousPinchDistance = currentDistance;
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

using UnityEngine;
using UnityEngine.EventSystems;

public class RotateCubeSet : MonoBehaviour
{
    [SerializeField] private Transform puzzleParent;
    [SerializeField, Min(0.01f)] private float rotationSensitivity = 0.4f;

    private bool draggingEmptySpace;
    private Vector3 previousPointerPosition;

    private void Awake()
    {
        if (puzzleParent == null)
            puzzleParent = transform;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI() || PointerIsOverCube())
                return;

            draggingEmptySpace = true;
            previousPointerPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
            draggingEmptySpace = false;

        if (!draggingEmptySpace || !Input.GetMouseButton(0))
            return;

        Vector3 pointerPosition = Input.mousePosition;
        Vector3 delta = pointerPosition - previousPointerPosition;
        previousPointerPosition = pointerPosition;

        puzzleParent.Rotate(Vector3.up, -delta.x * rotationSensitivity, Space.World);
    }

    private bool PointerIsOverCube()
    {
        Camera gameCamera = Camera.main;
        if (gameCamera == null)
            return false;

        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return false;

        return hit.collider.GetComponentInParent<TabCube>() != null;
    }

    private static bool IsPointerOverUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        if (Input.touchCount > 0)
            return eventSystem.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return eventSystem.IsPointerOverGameObject();
    }
}

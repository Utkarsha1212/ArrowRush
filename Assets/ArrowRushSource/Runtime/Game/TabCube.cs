using System.Collections;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

[RequireComponent(typeof(Collider))]
public class TabCube : MonoBehaviour
{
    private static readonly Vector3Int[] ArrowFaceNormals =
    {
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.down,
        Vector3Int.up,
        Vector3Int.back,
        Vector3Int.forward
    };

    public static event Action<TabCube, bool> MoveAttempted;
    public static event Action<TabCube> CubeExited;
    public static bool InputLocked { get; private set; }

    public static void SetInputLocked(bool locked)
    {
        InputLocked = locked;
    }

    [Header("Movement")]
    [SerializeField] private Vector3Int direction = Vector3Int.forward;
    [SerializeField, Min(0.1f)] private float moveSpeed = 8f;
    [SerializeField, Min(0.1f)] private float clearMoveDistance = 12f;
    [SerializeField, Min(0f)] private float gapBeforeBlocker = 0.04f;
    [SerializeField, Min(0f)] private float blockerLaneTolerance = 0.1f;

    private bool isMoving;
    private Tween movementTween;
    private static TabCube activeMover;
    private static readonly HashSet<TabCube> activeCubes = new HashSet<TabCube>();
    private Mesh arrowMesh;
    private static Material sharedArrowMaterial;
    private Renderer cubeRenderer;
    private TrailRenderer exitTrail;
    private static Material sharedTrailMaterial;
    private Coroutine hintRoutine;
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

    private Vector3 MoveDirection
    {
        get
        {
            Vector3 localDirection = new Vector3(direction.x, direction.y, direction.z);
            if (localDirection.sqrMagnitude <= 0f)
                localDirection = Vector3.forward;

            // The arrow is drawn in the cube's local space, so movement must use
            // that same space before converting to world coordinates.
            return transform.TransformDirection(localDirection.normalized).normalized;
        }
    }

    private void Awake()
    {
        direction = ToCardinalDirection(direction);
        cubeRenderer = GetComponent<Renderer>();
        CreateDirectionMarker();
        CreateExitTrail();
    }

    private void OnEnable()
    {
        activeCubes.Add(this);
    }

    /// <summary>Assigns one of the six axis directions to this cube.</summary>
    public void SetDirection(Vector3Int newDirection)
    {
        direction = ToCardinalDirection(newDirection);
        UpdateDirectionMarker();
    }

    private void OnMouseDown()
    {
        if (InputLocked || IsPointerOverUI() || isMoving || (activeMover != null && activeMover != this))
            return;

        TryMoveOut();
    }

    private void TryMoveOut()
    {
        // Only one cube can move at a time.
        if (activeMover != null && activeMover != this)
            return;

        activeMover = this;
        isMoving = true;
        Vector3 start = transform.position;
        Vector3 moveDirection = MoveDirection;

        // Validate the actual scene so a blocked arrow never passes through a cube.
        bool blocked = TryFindCubeAhead(moveDirection, out float blockerDistance);
        MoveAttempted?.Invoke(this, blocked);
        if (blocked)
        {
            BounceFromBlocker(start, moveDirection, blockerDistance);
            return;
        }

        Vector3 target = start + moveDirection * clearMoveDistance;
        if (exitTrail != null)
        {
            exitTrail.Clear();
            exitTrail.emitting = true;
        }
        movementTween = transform.DOMove(target, GetTweenDuration(clearMoveDistance))
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                CubeExited?.Invoke(this);
                ReleaseExitTrail();
                activeMover = null;
                movementTween = null;
                Destroy(gameObject);
            });
    }

    public bool HasClearPath()
    {
        return !TryFindCubeAhead(MoveDirection, out _);
    }

    public void ShowHint(float duration)
    {
        if (cubeRenderer == null)
            return;

        if (hintRoutine != null)
            StopCoroutine(hintRoutine);
        hintRoutine = StartCoroutine(HighlightCube(duration));
    }

    private IEnumerator HighlightCube(float duration)
    {
        MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
        cubeRenderer.GetPropertyBlock(originalBlock);
        MaterialPropertyBlock highlightBlock = new MaterialPropertyBlock();
        cubeRenderer.GetPropertyBlock(highlightBlock);
        highlightBlock.SetColor(OutlineColorId, new Color(0.1f, 0.85f, 1f));
        cubeRenderer.SetPropertyBlock(highlightBlock);
        yield return new WaitForSeconds(duration);
        if (cubeRenderer != null)
            cubeRenderer.SetPropertyBlock(originalBlock);
        hintRoutine = null;
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

    private void OnDestroy()
    {
        if (arrowMesh != null)
            Destroy(arrowMesh);
    }

    private void BounceFromBlocker(Vector3 start, Vector3 moveDirection, float blockerDistance)
    {
        float halfSizeAlongDirection = GetHalfSizeAlongDirection(moveDirection);
        float travelDistance = Mathf.Max(0f, blockerDistance - halfSizeAlongDirection - gapBeforeBlocker);
        Vector3 bumpPosition = travelDistance > 0.03f
            ? start + moveDirection * travelDistance
            : start - moveDirection * 0.12f;
        float bumpDuration = GetTweenDuration(Vector3.Distance(start, bumpPosition));
        float returnDuration = GetTweenDuration(Vector3.Distance(bumpPosition, start));

        movementTween = DOTween.Sequence()
            .Append(transform.DOMove(bumpPosition, bumpDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOMove(start, returnDuration).SetEase(Ease.OutBack))
            .OnComplete(() =>
            {
                isMoving = false;
                activeMover = null;
                movementTween = null;
            });
    }

    private float GetTweenDuration(float distance)
    {
        return Mathf.Max(0.08f, distance / Mathf.Max(0.1f, moveSpeed));
    }

    private bool TryFindCubeAhead(Vector3 moveDirection, out float nearestBlockerDistance)
    {
        nearestBlockerDistance = 0f;
        bool foundBlocker = false;
        float nearestDistance = float.MaxValue;

        foreach (TabCube cube in activeCubes)
        {
            if (cube == null || cube == this)
                continue;

            // Use cube centers instead of collider raycasts. Randomly scaled
            // neighboring cubes can overlap, and Unity raycasts may miss a
            // collider when their origin starts inside it.
            Vector3 toCube = cube.transform.position - transform.position;
            float alongPath = Vector3.Dot(toCube, moveDirection);
            Vector3 offPath = toCube - moveDirection * alongPath;
            if (alongPath <= 0f || alongPath > clearMoveDistance + cube.GetHalfSizeAlongDirection(moveDirection) ||
                offPath.sqrMagnitude > blockerLaneTolerance * blockerLaneTolerance)
                continue;

            float distanceToNearFace = Mathf.Max(0f, alongPath - cube.GetHalfSizeAlongDirection(moveDirection));
            if (distanceToNearFace < nearestDistance)
            {
                nearestDistance = distanceToNearFace;
                foundBlocker = true;
            }
        }

        if (foundBlocker)
            nearestBlockerDistance = nearestDistance;

        return foundBlocker;
    }

    private void OnDisable()
    {
        activeCubes.Remove(this);
        if (movementTween != null && movementTween.IsActive())
            movementTween.Kill();
        movementTween = null;
        isMoving = false;
        if (activeMover == this)
            activeMover = null;
    }

    private float GetHalfSizeAlongDirection(Vector3 moveDirection)
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Collider cubeCollider = GetComponent<Collider>();
            if (cubeCollider == null)
                return 0.5f;

            Vector3 fallbackExtents = cubeCollider.bounds.extents;
            return Mathf.Abs(moveDirection.x) * fallbackExtents.x
                 + Mathf.Abs(moveDirection.y) * fallbackExtents.y
                 + Mathf.Abs(moveDirection.z) * fallbackExtents.z;
        }

        Vector3 localDirection = transform.InverseTransformDirection(moveDirection).normalized;
        Vector3 halfSize = Vector3.Scale(boxCollider.size * 0.5f, new Vector3(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z)));
        return Mathf.Abs(localDirection.x) * halfSize.x
             + Mathf.Abs(localDirection.y) * halfSize.y
             + Mathf.Abs(localDirection.z) * halfSize.z;
    }

    private static Vector3Int ToCardinalDirection(Vector3Int value)
    {
        if (value == Vector3Int.zero)
            return Vector3Int.forward;

        if (Mathf.Abs(value.x) >= Mathf.Abs(value.y) && Mathf.Abs(value.x) >= Mathf.Abs(value.z))
            return value.x >= 0 ? Vector3Int.right : Vector3Int.left;
        if (Mathf.Abs(value.y) >= Mathf.Abs(value.z))
            return value.y >= 0 ? Vector3Int.up : Vector3Int.down;
        return value.z >= 0 ? Vector3Int.forward : Vector3Int.back;
    }

    private void CreateDirectionMarker()
    {
        GameObject marker = new GameObject("DirectionArrow");
        marker.transform.SetParent(transform, false);

        MeshFilter meshFilter = marker.AddComponent<MeshFilter>();
        MeshRenderer arrowRenderer = marker.AddComponent<MeshRenderer>();
        arrowMesh = new Mesh { name = "DirectionArrowMesh" };
        meshFilter.sharedMesh = arrowMesh;
        arrowRenderer.sharedMaterial = GetArrowMaterial();
        arrowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        arrowRenderer.receiveShadows = false;
        UpdateDirectionMarker();
    }

    private void CreateExitTrail()
    {
        GameObject trailObject = new GameObject("ExitTrail");
        trailObject.transform.SetParent(transform, false);
        trailObject.transform.localPosition = Vector3.zero;
        exitTrail = trailObject.AddComponent<TrailRenderer>();
        exitTrail.time = 0.28f;
        exitTrail.startWidth = 0.34f;
        exitTrail.endWidth = 0f;
        exitTrail.minVertexDistance = 0.08f;
        exitTrail.alignment = LineAlignment.View;
        exitTrail.emitting = false;
        exitTrail.shadowCastingMode = ShadowCastingMode.Off;
        exitTrail.receiveShadows = false;
        exitTrail.startColor = new Color(0.18f, 0.83f, 1f, 0.88f);
        exitTrail.endColor = new Color(0.18f, 0.83f, 1f, 0f);
        if (sharedTrailMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null) sharedTrailMaterial = new Material(shader);
        }
        if (sharedTrailMaterial != null) exitTrail.sharedMaterial = sharedTrailMaterial;
    }

    private void ReleaseExitTrail()
    {
        if (exitTrail == null) return;
        TrailRenderer trail = exitTrail;
        exitTrail = null;
        trail.emitting = false;
        trail.transform.SetParent(null, true);
        Destroy(trail.gameObject, trail.time);
    }

    private static Material GetArrowMaterial()
    {
        if (sharedArrowMaterial == null)
        {
            Shader arrowShader = Shader.Find("Unlit/Color");
            if (arrowShader == null)
                arrowShader = Shader.Find("Sprites/Default");

            sharedArrowMaterial = new Material(arrowShader)
            {
                color = Color.black
            };
            if (sharedArrowMaterial.HasProperty("_BaseColor"))
                sharedArrowMaterial.SetColor("_BaseColor", Color.black);
            if (sharedArrowMaterial.HasProperty("_Color"))
                sharedArrowMaterial.SetColor("_Color", Color.black);
        }

        return sharedArrowMaterial;
    }

    private void UpdateDirectionMarker()
    {
        if (arrowMesh == null)
            return;

        // A solid seven-point arrow reads more clearly than three thin strokes.
        Vector2[] shape =
        {
            new Vector2(-0.22f, -0.045f),
            new Vector2(0.035f, -0.045f),
            new Vector2(0.035f, -0.13f),
            new Vector2(0.27f, 0f),
            new Vector2(0.035f, 0.13f),
            new Vector2(0.035f, 0.045f),
            new Vector2(-0.22f, 0.045f)
        };

        Vector3 arrowDirection = new Vector3(direction.x, direction.y, direction.z).normalized;
        List<Vector3> vertices = new List<Vector3>(ArrowFaceNormals.Length * (shape.Length + 1));
        List<int> triangles = new List<int>(ArrowFaceNormals.Length * shape.Length * 3);

        // Repeat the same exit arrow on every face where it can be drawn flat.
        // The two faces perpendicular to movement are skipped because a flat
        // decal there could not communicate the same direction accurately.
        foreach (Vector3Int integerNormal in ArrowFaceNormals)
        {
            Vector3 faceNormal = integerNormal;
            if (Mathf.Abs(Vector3.Dot(faceNormal, arrowDirection)) > 0.001f)
                continue;

            Vector3 perpendicular = Vector3.Cross(faceNormal, arrowDirection).normalized;
            Vector3 faceOffset = faceNormal * 0.506f;
            int firstVertex = vertices.Count;
            vertices.Add(faceOffset);

            for (int i = 0; i < shape.Length; i++)
                vertices.Add(faceOffset + arrowDirection * shape[i].x + perpendicular * shape[i].y);

            for (int i = 0; i < shape.Length; i++)
            {
                int current = firstVertex + i + 1;
                int next = firstVertex + ((i + 1) % shape.Length) + 1;
                triangles.Add(firstVertex);
                triangles.Add(current);
                triangles.Add(next);
            }
        }

        arrowMesh.Clear();
        arrowMesh.SetVertices(vertices);
        arrowMesh.SetTriangles(triangles, 0);
        arrowMesh.RecalculateNormals();
        arrowMesh.RecalculateBounds();
    }
}

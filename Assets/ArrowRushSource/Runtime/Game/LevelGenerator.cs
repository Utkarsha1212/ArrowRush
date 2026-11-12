using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly Vector3Int[] CubeDirections =
    {
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.down,
        Vector3Int.up,
        Vector3Int.back,
        Vector3Int.forward
    };

    [SerializeField] private GameObject puzzlePrefab;
    [SerializeField] private Transform puzzleParent;
    [Min(1)] [SerializeField] private int levelNumber = 1;
    [SerializeField] private float cubeSpacing = 1f;
    [Header("Level Difficulty")]
    [Min(1)] [SerializeField] private int startingCubeCount = 2;
    [Min(1)] [SerializeField] private int cubesAddedPerLevel = 2;
    [Min(1)] [SerializeField] private int difficultyCapLevel = 2000;
    [Header("Cube Movement")]
    [Min(0.1f)] [SerializeField] private float cubeSlideSpeed = 8f;
    [Header("Level Timer")]
    [Min(1f)] [SerializeField] private float baseLevelTime = 30f;
    [Min(0f)] [SerializeField] private float timePerSqrtCube = 5f;
    [Min(10f)] [SerializeField] private float maximumLevelTime = 360f;
    [Header("Spawn Animation")]
    [Min(0.01f)] [SerializeField] private float cubeScaleUpDuration = 0.4f;
    [Min(0f)] [SerializeField] private float cubeScaleUpStagger = 0.015f;
    [Min(0f)] [SerializeField] private float maximumScaleUpDelay = 0.5f;
    private readonly List<TabCube> generatedCubes = new List<TabCube>();
    private MaterialPropertyBlock materialPropertyBlock;
    private LevelUIManager levelUI;
    private CameraHandler cameraHandler;
    private int pendingScaleAnimations;

    private void Start()
    {
        levelNumber = Mathf.Clamp(PlayerPrefs.GetInt("ArrowRush.SelectedLevel", levelNumber), 1, Mathf.Max(1, difficultyCapLevel));
        GenerateLevel();
    }

    [ContextMenu("Generate Level")]
    public void GenerateLevel()
    {
        if (puzzlePrefab == null)
        {
            Debug.LogError("Assign a cube prefab to LevelGenerator before generating a level.", this);
            return;
        }

        if (puzzleParent == null)
            puzzleParent = transform;

        ClearGeneratedCubes();

        if (cameraHandler == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                cameraHandler = mainCamera.GetComponent<CameraHandler>();
        }

        int cubeCount = GetCubeCountForLevel();
        List<Vector3Int> cells = BuildCubeStructure(cubeCount, out Vector3Int structureSize);
        if (Application.isPlaying)
        {
            if (levelUI == null)
                levelUI = GetComponent<LevelUIManager>();
            if (levelUI == null)
            {
                Debug.LogError("Add a LevelUIManager component with a scene-authored Canvas to LevelGenerator.", this);
                return;
            }
            float levelTime = Mathf.Min(maximumLevelTime, baseLevelTime + Mathf.Sqrt(cells.Count) * timePerSqrtCube);
            levelUI.InitializeLevel(levelNumber, cells.Count, levelTime, LoadNextLevel, RestartLevel);
            pendingScaleAnimations = cells.Count;
            TabCube.SetInputLocked(true);
        }

        Vector3 center = GetCenter(cells);
        Bounds puzzleBounds = default;
        bool hasPuzzleBounds = false;
        // Each child exactly fills its grid cell so the parent reads as one solid cube.
        float spacing = Mathf.Max(0.01f, cubeSpacing);

        for (int i = 0; i < cells.Count; i++)
        {
            GameObject cube = Instantiate(puzzlePrefab, puzzleParent);
            cube.transform.localPosition = ((Vector3)cells[i] - center) * spacing;
            cube.transform.localRotation = Quaternion.identity;
            Vector3 finalScale = Vector3.one * spacing;
            cube.transform.localScale = finalScale;
            ApplyCubeColor(cube);

            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                if (hasPuzzleBounds)
                    puzzleBounds.Encapsulate(cubeRenderer.bounds);
                else
                {
                    puzzleBounds = cubeRenderer.bounds;
                    hasPuzzleBounds = true;
                }
            }

            if (Application.isPlaying)
            {
                cube.transform.localScale = Vector3.zero;
                float delay = Mathf.Min(i * cubeScaleUpStagger, maximumScaleUpDelay);
                cube.transform
                    .DOScale(finalScale, cubeScaleUpDuration)
                    .SetDelay(delay)
                    .SetEase(Ease.OutBack)
                    .OnComplete(HandleCubeScaleAnimationCompleted);
            }

            TabCube tabCube = cube.GetComponent<TabCube>();
            if (tabCube != null)
            {
                int levelDirectionOffset = i + (Mathf.Max(1, levelNumber) - 1) * 5;
                Vector3Int outwardDirection = FindCubeExitDirection(cells[i], structureSize, levelDirectionOffset);
                tabCube.SetDirection(outwardDirection);
                tabCube.SetMoveSpeed(cubeSlideSpeed);
                generatedCubes.Add(tabCube);
            }
        }

        if (cameraHandler != null && hasPuzzleBounds)
            cameraHandler.FramePuzzle(puzzleBounds);
    }

    private void ApplyCubeColor(GameObject cube)
    {
        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        if (cubeRenderer == null)
            return;

        if (materialPropertyBlock == null)
            materialPropertyBlock = new MaterialPropertyBlock();

        materialPropertyBlock.Clear();
        materialPropertyBlock.SetColor(BaseColorId, Color.white);
        materialPropertyBlock.SetColor(ColorId, Color.white);
        cubeRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void ClearGeneratedCubes()
    {
        pendingScaleAnimations = 0;
        foreach (TabCube cube in generatedCubes)
        {
            if (cube == null)
                continue;

            cube.transform.DOKill();

            if (Application.isPlaying)
                Destroy(cube.gameObject);
            else
                DestroyImmediate(cube.gameObject);
        }

        generatedCubes.Clear();
    }

    private void HandleCubeScaleAnimationCompleted()
    {
        pendingScaleAnimations = Mathf.Max(0, pendingScaleAnimations - 1);
        if (pendingScaleAnimations == 0)
            TabCube.SetInputLocked(false);
    }

    private int GetCubeCountForLevel()
    {
        int progressionLevel = Mathf.Min(Mathf.Max(1, levelNumber), Mathf.Max(1, difficultyCapLevel));
        long count = Mathf.Max(1, startingCubeCount)
                   + (long)(progressionLevel - 1) * Mathf.Max(1, cubesAddedPerLevel);
        return count > 10000 ? 10000 : (int)count;
    }

    private static List<Vector3Int> BuildCubeStructure(int count, out Vector3Int structureSize)
    {
        count = Mathf.Max(1, count);
        int side = Mathf.CeilToInt(Mathf.Pow(count, 1f / 3f));
        int layerCapacity = side * side;
        int height = Mathf.CeilToInt(count / (float)layerCapacity);
        structureSize = new Vector3Int(side, height, side);

        List<Vector3Int> cells = new List<Vector3Int>(count);
        for (int y = 0; y < height && cells.Count < count; y++)
        {
            for (int z = 0; z < side && cells.Count < count; z++)
            {
                for (int x = 0; x < side && cells.Count < count; x++)
                    cells.Add(new Vector3Int(x, y, z));
            }
        }

        return cells;
    }

    private static Vector3Int FindCubeExitDirection(Vector3Int cell, Vector3Int structureSize, int offset)
    {
        int[] distances =
        {
            cell.x,
            structureSize.x - 1 - cell.x,
            cell.y,
            structureSize.y - 1 - cell.y,
            cell.z,
            structureSize.z - 1 - cell.z
        };
        int nearestFace = distances[0];
        for (int i = 1; i < distances.Length; i++)
            nearestFace = Mathf.Min(nearestFace, distances[i]);

        // Point each arrow toward its nearest outside face. Inner cubes are
        // initially blocked and become available as the outer cubes are cleared.
        int start = Mathf.Abs(offset) % CubeDirections.Length;
        for (int i = 0; i < CubeDirections.Length; i++)
        {
            int candidate = (start + i) % CubeDirections.Length;
            if (distances[candidate] == nearestFace)
                return CubeDirections[candidate];
        }

        return Vector3Int.left;
    }

    public void LoadNextLevel()
    {
        levelNumber++;
        PlayerPrefs.SetInt("ArrowRush.SelectedLevel", levelNumber);
        PlayerPrefs.Save();
        GenerateLevel();
    }

    public void RestartLevel()
    {
        GenerateLevel();
    }

    private static Vector3 GetCenter(List<Vector3Int> cells)
    {
        Vector3 total = Vector3.zero;
        foreach (Vector3Int cell in cells)
            total += (Vector3)cell;
        return total / cells.Count;
    }
}

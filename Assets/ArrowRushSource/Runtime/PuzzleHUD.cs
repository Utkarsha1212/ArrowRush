using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime HUD for the arrow puzzle: level, taps, feedback, and hints.</summary>
public class PuzzleHUD : MonoBehaviour
{
    private static readonly Color Navy = new Color(0.055f, 0.075f, 0.17f, 0.92f);
    private static readonly Color Cyan = new Color(0.12f, 0.68f, 0.96f, 1f);

    private Text levelText;
    private Text cubesText;
    private Text moveText;
    private Text timerText;
    private Text statusText;
    private Text buttonLabel;
    private Button actionButton;
    private Action nextLevelAction;
    private Action retryLevelAction;
    private int moveCount;
    private int cubesRemaining;
    private float timeRemaining;
    private int displayedSeconds = -1;
    private bool levelActive;

    private void OnEnable()
    {
        TabCube.MoveAttempted += HandleMoveAttempted;
        TabCube.CubeExited += HandleCubeExited;
    }

    private void OnDisable()
    {
        TabCube.MoveAttempted -= HandleMoveAttempted;
        TabCube.CubeExited -= HandleCubeExited;
    }

    private void Awake()
    {
        BuildCanvas();
    }

    public void Initialize(int level, int cubeCount, float timeLimit, Action onNextLevel, Action onRetryLevel)
    {
        moveCount = 0;
        cubesRemaining = cubeCount;
        timeRemaining = Mathf.Max(1f, timeLimit);
        displayedSeconds = -1;
        levelActive = true;
        nextLevelAction = onNextLevel;
        retryLevelAction = onRetryLevel;
        TabCube.SetInputLocked(false);
        if (levelText != null)
            levelText.text = $"LEVEL  {Mathf.Max(1, level)}";
        UpdateCubesText();
        UpdateMoveText();
        UpdateTimerText();
        SetStatus("Tap a cube whose arrow has a clear path.");
        if (actionButton != null)
        {
            actionButton.interactable = true;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(ShowHint);
        }
        if (buttonLabel != null)
            buttonLabel.text = "HINT  •  SHOW A CLEAR CUBE";
    }

    private void Update()
    {
        if (!levelActive)
            return;

        timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
        UpdateTimerText();
        if (timeRemaining <= 0f)
            HandleTimeExpired();
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Arrow Puzzle HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        GameObject panelObject = CreateUIObject("Top Panel", root);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        SetAnchoredRect(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(930f, 205f));
        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = Navy;
        panelImage.raycastTarget = false;

        levelText = CreateText("Level", panel, 34, TextAnchor.MiddleLeft, Color.white);
        SetAnchoredRect(levelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 10f), new Vector2(260f, 74f));

        cubesText = CreateText("Cubes Remaining", panel, 28, TextAnchor.MiddleCenter, new Color(0.55f, 0.83f, 1f));
        SetAnchoredRect(cubesText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(280f, 74f));

        moveText = CreateText("Moves", panel, 32, TextAnchor.MiddleRight, Color.white);
        moveText.fontSize = 22;
        SetAnchoredRect(moveText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 10f), new Vector2(160f, 74f));

        timerText = CreateText("Level Timer", panel, 25, TextAnchor.MiddleCenter, Color.white);
        SetAnchoredRect(timerText.rectTransform, new Vector2(0.68f, 0.5f), new Vector2(0.68f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(150f, 74f));

        cubesText.rectTransform.anchorMin = new Vector2(0.45f, 0.5f);
        cubesText.rectTransform.anchorMax = new Vector2(0.45f, 0.5f);
        cubesText.rectTransform.sizeDelta = new Vector2(220f, 74f);

        statusText = CreateText("Status", panel, 24, TextAnchor.MiddleCenter, new Color(0.79f, 0.86f, 1f));
        SetAnchoredRect(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(850f, 62f));

        GameObject buttonObject = CreateUIObject("Hint Button", root);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        SetAnchoredRect(buttonRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(360f, 100f));
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = Cyan;
        actionButton = buttonObject.AddComponent<Button>();
        actionButton.targetGraphic = buttonImage;
        ColorBlock colors = actionButton.colors;
        colors.highlightedColor = new Color(0.3f, 0.8f, 1f);
        colors.pressedColor = new Color(0.06f, 0.48f, 0.76f);
        actionButton.colors = colors;

        buttonLabel = CreateText("Hint Label", buttonRect, 27, TextAnchor.MiddleCenter, Color.white);
        buttonLabel.text = "HINT  •  SHOW A CLEAR CUBE";
        buttonLabel.fontStyle = FontStyle.Bold;
        buttonLabel.raycastTarget = false;
        SetAnchoredRect(buttonLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    private void ShowHint()
    {
        TabCube[] cubes = FindObjectsOfType<TabCube>();
        TabCube selected = null;
        float bestScreenDistance = float.MaxValue;
        Camera camera = Camera.main;

        foreach (TabCube cube in cubes)
        {
            if (cube == null || !cube.HasClearPath())
                continue;

            float screenDistance = 0f;
            if (camera != null)
            {
                Vector3 viewport = camera.WorldToViewportPoint(cube.transform.position);
                if (viewport.z <= 0f)
                    continue;
                screenDistance = (new Vector2(viewport.x, viewport.y) - new Vector2(0.5f, 0.5f)).sqrMagnitude;
            }

            if (screenDistance < bestScreenDistance)
            {
                bestScreenDistance = screenDistance;
                selected = cube;
            }
        }

        if (selected == null)
        {
            SetStatus("No clear arrow is available right now.");
            return;
        }

        selected.ShowHint(1.8f);
        SetStatus("Follow the glowing arrow to clear a cube.");
    }

    private void HandleMoveAttempted(TabCube cube, bool blocked)
    {
        moveCount++;
        UpdateMoveText();
        SetStatus(blocked ? "Blocked! Another cube is in the way." : "Clear path! Cube moving out.");
    }

    private void HandleCubeExited(TabCube cube)
    {
        cubesRemaining = Mathf.Max(0, cubesRemaining - 1);
        UpdateCubesText();
        if (cubesRemaining == 0)
        {
            levelActive = false;
            TabCube.SetInputLocked(true);
            SetStatus($"LEVEL CLEARED!  {moveCount} moves");
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(AdvanceToNextLevel);
            }
            if (buttonLabel != null)
                buttonLabel.text = "NEXT LEVEL";
        }
    }

    private void HandleTimeExpired()
    {
        if (!levelActive)
            return;

        levelActive = false;
        TabCube.SetInputLocked(true);
        timeRemaining = 0f;
        UpdateTimerText();
        SetStatus("TIME UP! Try the level again.");
        if (actionButton != null)
        {
            actionButton.interactable = true;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(RetryLevel);
        }
        if (buttonLabel != null)
            buttonLabel.text = "RETRY LEVEL";
    }

    private void RetryLevel()
    {
        retryLevelAction?.Invoke();
    }

    private void AdvanceToNextLevel()
    {
        nextLevelAction?.Invoke();
    }

    private void UpdateCubesText()
    {
        if (cubesText != null)
            cubesText.text = $"CUBES  {cubesRemaining}";
    }

    private void UpdateMoveText()
    {
        if (moveText != null)
            moveText.text = $"MOVES\n{moveCount}";
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(timeRemaining);
        if (seconds == displayedSeconds)
            return;

        displayedSeconds = seconds;
        timerText.text = $"TIME\n{seconds / 60:00}:{seconds % 60:00}";
        timerText.color = seconds <= 10 ? new Color(1f, 0.35f, 0.3f) : Color.white;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private static Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}

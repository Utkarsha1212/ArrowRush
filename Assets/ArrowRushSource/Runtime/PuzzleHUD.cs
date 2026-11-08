using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

/// <summary>Runtime HUD for the arrow puzzle: level, taps, feedback, and hints.</summary>
public class PuzzleHUD : MonoBehaviour
{
    private static readonly Color Navy = new Color(0.055f, 0.075f, 0.17f, 0.92f);
    private static readonly Color Cyan = new Color(0.12f, 0.68f, 0.96f, 1f);
    private static readonly Color Green = new Color(0.06f, 0.80f, 0.23f, 1f);

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
    private int currentLevel;
    private bool levelActive;
    private bool paused;
    private GameObject pausePanel;
    private GameObject resultPanel;
    private Text resultTitle;
    private Text resultDetails;
    private Image[] resultStars;
    private static Sprite[] buttonSprites;

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
        currentLevel = Mathf.Max(1, level);
        cubesRemaining = cubeCount;
        timeRemaining = Mathf.Max(1f, timeLimit);
        displayedSeconds = -1;
        levelActive = true;
        paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
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
            actionButton.onClick.AddListener(() => ArrowRushAudio.Instance?.Play("click"));
        }
        if (buttonLabel != null)
            buttonLabel.text = "HINT  •  SHOW A CLEAR CUBE";
    }

    private void Update()
    {
        if (!levelActive || paused)
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
        buttonImage.sprite = GetButtonSprite("MenuUI_6");
        buttonImage.color = Color.white;
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

        GameObject pauseObject = CreateUIObject("Pause Button", root);
        RectTransform pauseRect = pauseObject.GetComponent<RectTransform>();
        SetAnchoredRect(pauseRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -260f), new Vector2(120f, 120f));
        Image pauseImage = pauseObject.AddComponent<Image>();
        pauseImage.sprite = GetButtonSprite("MenuUI_7");
        pauseImage.color = Color.white;
        Button pauseButton = pauseObject.AddComponent<Button>();
        pauseButton.targetGraphic = pauseImage;
        pauseButton.onClick.AddListener(ShowPausePanel);
        Text pauseLabel = CreateText("Pause Label", pauseRect, 56, TextAnchor.MiddleCenter, Color.white);
        pauseLabel.text = "Ⅱ";
        SetAnchoredRect(pauseLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        pausePanel = CreateUIObject("Pause Panel", root);
        RectTransform pausePanelRect = pausePanel.GetComponent<RectTransform>();
        SetAnchoredRect(pausePanelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image pauseShade = pausePanel.AddComponent<Image>();
        pauseShade.color = new Color(0.015f, 0.035f, 0.12f, 0.78f);
        GameObject pauseCard = CreateUIObject("Pause Card", pausePanelRect);
        RectTransform pauseCardRect = pauseCard.GetComponent<RectTransform>();
        SetAnchoredRect(pauseCardRect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(760f, 680f));
        Image cardImage = pauseCard.AddComponent<Image>();
        cardImage.color = Navy;
        Text pauseTitle = CreateText("Pause Title", pauseCardRect, 58, TextAnchor.MiddleCenter, Color.white);
        pauseTitle.text = "PAUSED";
        SetAnchoredRect(pauseTitle.rectTransform, new Vector2(.5f, .82f), new Vector2(.5f, .82f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(650f, 110f));
        CreatePauseAction("RESUME", Green, pauseCardRect, .62f, ResumeGame);
        CreatePauseAction("RESTART", Cyan, pauseCardRect, .42f, () => retryLevelAction?.Invoke());
        CreatePauseAction("HOME", new Color(.96f, .10f, .39f), pauseCardRect, .22f, () => SceneManager.LoadScene("Menu"));
        pausePanel.SetActive(false);
        BuildResultPanel(root);
    }

    private void CreatePauseAction(string label, Color color, Transform parent, float y, Action action)
    {
        GameObject buttonObject = CreateUIObject(label + " Button", parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetAnchoredRect(rect, new Vector2(.5f, y), new Vector2(.5f, y), new Vector2(.5f, .5f), Vector2.zero, new Vector2(520f, 105f));
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = GetButtonSprite(color == Cyan ? "MenuUI_6" : color.r > .8f ? "MenuUI_7" : "MenuUI_5");
        image.color = Color.white;
        Button button = buttonObject.AddComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener(() => { ArrowRushAudio.Instance?.Play("click"); action?.Invoke(); });
        Text text = CreateText(label + " Label", rect, 36, TextAnchor.MiddleCenter, Color.white);
        text.text = label; text.fontStyle = FontStyle.Bold;
        SetAnchoredRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
    }

    private void BuildResultPanel(RectTransform root)
    {
        resultPanel = CreateUIObject("Level Result Panel", root);
        RectTransform overlayRect = resultPanel.GetComponent<RectTransform>();
        SetAnchoredRect(overlayRect, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        Image shade = resultPanel.AddComponent<Image>(); shade.color = new Color(.015f, .03f, .12f, .82f);
        GameObject card = CreateUIObject("Result Card", overlayRect);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        SetAnchoredRect(cardRect, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(790f, 980f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.sprite = GetSprite("BrickBreakoutPuzzle/Sprites/UI/LevelCompletionUI", "LevelCompletionUI_0");
        cardImage.color = Color.white;
        resultTitle = CreateText("Result Title", cardRect, 58, TextAnchor.MiddleCenter, new Color(.05f,.20f,.55f));
        SetAnchoredRect(resultTitle.rectTransform, new Vector2(.5f,.82f), new Vector2(.5f,.82f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(670f,120f));
        resultDetails = CreateText("Result Details", cardRect, 34, TextAnchor.MiddleCenter, new Color(.08f,.13f,.3f));
        SetAnchoredRect(resultDetails.rectTransform, new Vector2(.5f,.61f), new Vector2(.5f,.61f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(650f,170f));
        resultStars = new Image[3];
        for (int i = 0; i < resultStars.Length; i++)
        {
            GameObject starObject = CreateUIObject("Completion Star " + (i + 1), cardRect);
            RectTransform starRect = starObject.GetComponent<RectTransform>();
            SetAnchoredRect(starRect, new Vector2(.5f, .44f), new Vector2(.5f, .44f), new Vector2(.5f, .5f), new Vector2((i - 1) * 145f, i == 1 ? 12f : 0f), new Vector2(115f, 125f));
            resultStars[i] = starObject.AddComponent<Image>();
            resultStars[i].sprite = GetSprite("BrickBreakoutPuzzle/Sprites/UI/LevelCompletionUI", "LevelCompletionUI_9");
            resultStars[i].preserveAspect = true;
            resultStars[i].raycastTarget = false;
            starObject.transform.localScale = Vector3.zero;
        }
        CreatePauseAction("NEXT LEVEL", Cyan, cardRect, .34f, AdvanceToNextLevel);
        CreatePauseAction("RETRY", new Color(1f,.22f,.32f), cardRect, .19f, RetryLevel);
        resultPanel.SetActive(false);
    }

    private static Sprite GetSprite(string resourcePath, string spriteName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites != null)
            foreach (Sprite sprite in sprites)
                if (sprite.name == spriteName) return sprite;
        return null;
    }

    private IEnumerator AnimateCompletionStars()
    {
        for (int i = 0; i < resultStars.Length; i++)
        {
            Transform star = resultStars[i].transform;
            star.localScale = Vector3.zero;
            star.DOScale(Vector3.one, .3f).SetEase(Ease.OutBack);
            ArrowRushAudio.Instance?.Play("LevelStar_" + (i + 1));
            yield return new WaitForSecondsRealtime(.2f);
        }
    }

    private void ShowPausePanel()
    {
        if (!levelActive || TabCube.InputLocked || pausePanel == null) return;
        paused = true;
        TabCube.SetInputLocked(true);
        pausePanel.SetActive(true);
        ArrowRushAudio.Instance?.Play("click");
    }

    private void ResumeGame()
    {
        paused = false;
        TabCube.SetInputLocked(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void ShowHint()
    {
        ArrowRushAudio.Instance?.Play("pop-6");
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
        ArrowRushAudio.Instance?.Play(blocked ? "Wrong" : "click");
        moveCount++;
        UpdateMoveText();
        SetStatus(blocked ? "Blocked! Another cube is in the way." : "Clear path! Cube moving out.");
    }

    private void HandleCubeExited(TabCube cube)
    {
        ArrowRushAudio.Instance?.Play("Slide");
        cubesRemaining = Mathf.Max(0, cubesRemaining - 1);
        UpdateCubesText();
        if (cubesRemaining == 0)
        {
            int highest = Mathf.Max(1, PlayerPrefs.GetInt("ArrowRush.HighestUnlockedLevel", 1));
            if (currentLevel >= highest)
            {
                PlayerPrefs.SetInt("ArrowRush.HighestUnlockedLevel", currentLevel + 1);
                PlayerPrefs.Save();
            }
            levelActive = false;
            TabCube.SetInputLocked(true);
            ArrowRushAudio.Instance?.Play("StarBlast");
            SetStatus($"LEVEL CLEARED!  {moveCount} moves");
            resultTitle.text = "LEVEL COMPLETE!";
            resultDetails.text = $"LEVEL {currentLevel:00}\n{moveCount} MOVES";
            SetResultActions(true);
            resultPanel.SetActive(true);
            StartCoroutine(AnimateCompletionStars());
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(AdvanceToNextLevel);
                actionButton.onClick.AddListener(() => ArrowRushAudio.Instance?.Play("click"));
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
        paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        TabCube.SetInputLocked(true);
        timeRemaining = 0f;
        UpdateTimerText();
        SetStatus("TIME UP! Try the level again.");
        resultTitle.text = "LEVEL FAILED";
        resultDetails.text = "TIME UP\nTRY AGAIN";
        SetResultActions(false);
        resultPanel.SetActive(true);
        if (actionButton != null)
        {
            actionButton.interactable = true;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(RetryLevel);
            actionButton.onClick.AddListener(() => ArrowRushAudio.Instance?.Play("click"));
        }
        if (buttonLabel != null)
            buttonLabel.text = "RETRY LEVEL";
        ArrowRushAudio.Instance?.Play("Wrong");
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
        text.font = Resources.Load<Font>("BrickBreakoutPuzzle/Fonts/Bubble Bobble") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
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

    private void SetResultActions(bool completed)
    {
        Transform card = resultPanel.transform.Find("Result Card");
        if (card == null) return;
        Transform next = card.Find("NEXT LEVEL Button");
        Transform retry = card.Find("RETRY Button");
        if (next != null) next.gameObject.SetActive(completed);
        if (retry != null) retry.gameObject.SetActive(!completed);
    }

    private static Sprite GetButtonSprite(string spriteName)
    {
        if (buttonSprites == null)
            buttonSprites = Resources.LoadAll<Sprite>("BrickBreakoutPuzzle/Sprites/UI/MenuUI");
        if (buttonSprites != null)
            foreach (Sprite sprite in buttonSprites)
                if (sprite.name == spriteName) return sprite;
        return null;
    }
}

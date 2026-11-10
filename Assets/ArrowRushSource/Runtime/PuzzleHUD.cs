using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

/// <summary>Runtime HUD for the arrow puzzle: level, taps, feedback, and hints.</summary>
public class PuzzleHUD : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Text levelText;
    [SerializeField] private Text cubesText;
    [SerializeField] private Text moveText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text buttonLabel;
    [SerializeField] private Button actionButton;
    private Action nextLevelAction;
    private Action retryLevelAction;
    private int moveCount;
    private int cubesRemaining;
    private float timeRemaining;
    private int displayedSeconds = -1;
    private int currentLevel;
    private bool levelActive;
    private bool paused;
    private float levelTimeLimit;
    private int completionStarsEarned;
    private int lastCountdownSecond = -1;
    private Vector3 timerTextInitialScale;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultTitle;
    [SerializeField] private Text resultDetails;
    [SerializeField] private Image[] resultStars;

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
        if (uiCanvas == null)
        {
            Debug.LogError("ArrowRush level Canvas is missing from Level.unity.", this);
            return;
        }
        if (timerText != null)
            timerTextInitialScale = timerText.transform.localScale;
        BindSceneButtons();
    }

    public void Initialize(int level, int cubeCount, float timeLimit, Action onNextLevel, Action onRetryLevel)
    {
        moveCount = 0;
        currentLevel = Mathf.Max(1, level);
        cubesRemaining = cubeCount;
        timeRemaining = Mathf.Max(1f, timeLimit);
        levelTimeLimit = timeRemaining;
        displayedSeconds = -1;
        lastCountdownSecond = -1;
        completionStarsEarned = 0;
        levelActive = true;
        paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        StarSystem.HideStars(resultStars);
        nextLevelAction = onNextLevel;
        retryLevelAction = onRetryLevel;
        TabCube.SetInputLocked(false);
        if (levelText != null) levelText.text = $"LEVEL  {currentLevel}";
        UpdateCubesText();
        UpdateMoveText();
        UpdateTimerText();
        SetStatus("Tap a cube whose arrow has a clear path.");
        if (actionButton != null)
        {
            actionButton.interactable = PlayerPrefs.GetInt("ArrowRush.HintsEnabled", 1) == 1;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(ShowHint);
            actionButton.onClick.AddListener(() => ArrowRushAudio.Instance?.Play("click"));
        }
        RefreshHintButton();
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

    private void BindSceneButtons()
    {
        Button[] buttons = uiCanvas.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            button.onClick.RemoveAllListeners();
            if (button.gameObject.name == "Pause Button")
                button.onClick.AddListener(ShowPausePanel);
            else if (button.gameObject.name == "RESUME Button")
                button.onClick.AddListener(() => { ArrowRushAudio.Instance?.Play("click"); ResumeGame(); });
            else if (button.gameObject.name == "RESTART Button")
                button.onClick.AddListener(() => { ArrowRushAudio.Instance?.Play("click"); retryLevelAction?.Invoke(); });
            else if (button.gameObject.name == "HOME Button")
                button.onClick.AddListener(() => { ArrowRushAudio.Instance?.Play("click"); SceneManager.LoadScene("Menu"); });
            else if (button.gameObject.name == "NEXT LEVEL Button")
                button.onClick.AddListener(() => { ArrowRushAudio.Instance?.Play("click"); AdvanceToNextLevel(); });
            else if (button.gameObject.name == "RETRY Button")
                button.onClick.AddListener(() => { ArrowRushAudio.Instance?.Play("click"); RetryLevel(); });
        }
    }

    private void ShowPausePanel()
    {
        if (!levelActive || TabCube.InputLocked || pausePanel == null) return;
        paused = true;
        TabCube.SetInputLocked(true);
        pausePanel.SetActive(true);
    }

    private void ResumeGame()
    {
        paused = false;
        TabCube.SetInputLocked(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void ShowHint()
    {
        if (PlayerPrefs.GetInt("ArrowRush.HintsEnabled", 1) != 1)
        {
            SetStatus("Hints are turned off in Settings.");
            return;
        }
        if (ArrowRushEconomy.Hints <= 0)
        {
            SetStatus("No hints left. Buy more from the menu shop.");
            return;
        }
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

        ArrowRushEconomy.TryConsumeHint();
        RefreshHintButton();
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
            completionStarsEarned = CalculateStarsEarned();
            SaveBestStars(completionStarsEarned);
            int coinReward = ArrowRushEconomy.AwardLevelClear(completionStarsEarned);
            ArrowRushAudio.Instance?.Play("StarBlast");
            SetStatus($"LEVEL CLEARED!  {moveCount} moves  •  +{coinReward} coins");
            if (resultTitle != null) resultTitle.text = "LEVEL COMPLETE!";
            if (resultDetails != null) resultDetails.text = $"LEVEL {currentLevel:00}\n{moveCount} MOVES\nTIME {FormatTime(Mathf.CeilToInt(timeRemaining))}\nSTARS {completionStarsEarned}/3";
            SetResultActions(true);
            if (resultPanel != null) resultPanel.SetActive(true);
            StartCoroutine(StarSystem.PresentStars(resultStars, completionStarsEarned, clip => ArrowRushAudio.Instance?.Play(clip)));
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
        if (resultTitle != null) resultTitle.text = "LEVEL FAILED";
        if (resultDetails != null) resultDetails.text = "TIME UP\nTRY AGAIN";
        SetResultActions(false);
        if (resultPanel != null) resultPanel.SetActive(true);
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

    private int CalculateStarsEarned()
    {
        float timeRatio = levelTimeLimit <= 0f ? 0f : timeRemaining / levelTimeLimit;
        if (timeRatio >= 0.66f) return 3;
        if (timeRatio >= 0.33f) return 2;
        return 1;
    }

    private void SaveBestStars(int stars)
    {
        string key = "ArrowRush.LevelStars." + currentLevel;
        int best = Mathf.Clamp(PlayerPrefs.GetInt(key, 0), 0, 3);
        if (stars <= best) return;
        PlayerPrefs.SetInt(key, stars);
        PlayerPrefs.Save();
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

    private void RefreshHintButton()
    {
        if (buttonLabel != null)
            buttonLabel.text = $"HINT  •  {ArrowRushEconomy.Hints} LEFT";
        if (actionButton != null)
            actionButton.interactable = PlayerPrefs.GetInt("ArrowRush.HintsEnabled", 1) == 1 && ArrowRushEconomy.Hints > 0;
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(timeRemaining);
        if (seconds != displayedSeconds)
        {
            displayedSeconds = seconds;
            timerText.text = $"TIME\n{FormatTime(seconds)}";
            timerText.color = seconds <= 10 ? new Color(1f, 0.35f, 0.3f) : Color.white;
        }

        if (seconds > 10)
        {
            if (lastCountdownSecond != -1)
                ResetTimerCountdownPulse();
            return;
        }

        if (seconds <= 0 || seconds == lastCountdownSecond)
            return;

        lastCountdownSecond = seconds;
        Transform timerTransform = timerText.transform;
        timerTransform.DOKill();
        timerTransform.localScale = timerTextInitialScale;
        timerTransform.DOScale(timerTextInitialScale * 1.15f, 0.16f)
            .SetEase(Ease.InOutSine)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true);
        ArrowRushAudio.Instance?.Play("Count");
    }

    private void ResetTimerCountdownPulse()
    {
        lastCountdownSecond = -1;
        Transform timerTransform = timerText.transform;
        timerTransform.DOKill();
        timerTransform.localScale = timerTextInitialScale;
    }

    private static string FormatTime(int seconds)
    {
        seconds = Mathf.Max(0, seconds);
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void SetResultActions(bool completed)
    {
        if (resultPanel == null) return;
        Transform card = resultPanel.transform.Find("Result Card");
        if (card == null) return;
        Transform next = card.Find("NEXT LEVEL Button");
        Transform retry = card.Find("RETRY Button");
        if (next != null) next.gameObject.SetActive(completed);
        if (retry != null) retry.gameObject.SetActive(!completed);
    }


}



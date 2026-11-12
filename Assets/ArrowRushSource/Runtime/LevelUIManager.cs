using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;
using TMPro;
using FuncrateStudio;

/// <summary>Runtime HUD for the arrow puzzle: level, timer, and hints.</summary>
public class LevelUIManager : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private Button actionButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject arrowHolder;
    [SerializeField] private TMP_Text coinRewardText;
    private Action loadNextLevelAction;
    private Action restartLevelAction;
    private int moveCount;
    private int cubesRemaining;
    private float timeRemaining;
    private int displayedSeconds = -1;
    private int currentLevel;
    private bool levelActive;
    private bool paused;
    private bool rewardAdPending;
    private float levelTimeLimit;
    private int completionStarsEarned;
    private int lastCountdownSecond = -1;
    private Vector3 timerTextInitialScale;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject failedPanel;
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text resultDetails;
    [SerializeField] private TMP_Text failedTitle;
    [SerializeField] private TMP_Text failedDetails;
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

    public void InitializeLevel(int level, int cubeCount, float timeLimit, Action loadNextLevel, Action restartLevel)
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
        if (failedPanel != null) failedPanel.SetActive(false);
        UpdateGameplayUIVisibility(true);
        StarSystem.HideStars(resultStars);
        loadNextLevelAction = loadNextLevel;
        restartLevelAction = restartLevel;
        TabCube.SetInputLocked(false);
        if (levelText != null) levelText.text = $"LEVEL  {currentLevel}";
        UpdateTimerUI();
        if (actionButton != null)
        {
            actionButton.interactable = GameManager.HintButtonEnabled;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(UseHint);
            actionButton.onClick.AddListener(() => AudioManager.Instance?.PlaySFX("click"));
        }
        UpdateHintCountUI();
    }

    private void Update()
    {
        if (!levelActive || paused)
            return;

        timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
        UpdateTimerUI();
        if (timeRemaining <= 0f)
            HandleLevelFailed();
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
                button.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX("click"); ResumeGame(); });
            else if (button.gameObject.name == "RESTART Button")
                button.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX("click"); restartLevelAction?.Invoke(); });
            else if (button.gameObject.name == "HOME Button")
                button.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX("click"); SceneManager.LoadScene("Menu"); });
            else if (button.gameObject.name == "HomeBtn" || button.gameObject.name == "RETRY Button")
                button.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX("click"); SceneManager.LoadScene("Menu"); });
            else if (button.gameObject.name == "NEXT LEVEL Button")
                button.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX("click"); LoadNextLevel(); });
            else if (button.gameObject.name == "RetryBtn")
                button.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX("click"); RestartLevel(); });
            else if (button.gameObject.name == "WatchAd")
                button.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX("click"); ResumeWithCoinsOrAd(); });
        }
    }

    private void ShowPausePanel()
    {
        if (!levelActive || TabCube.InputLocked || pausePanel == null) return;
        paused = true;
        TabCube.SetInputLocked(true);
        UpdateGameplayUIVisibility(false);
        pausePanel.SetActive(true);
    }

    private void ResumeGame()
    {
        paused = false;
        TabCube.SetInputLocked(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        UpdateGameplayUIVisibility(true);
    }

    private void UseHint()
    {
        if (!GameManager.HintButtonEnabled)
            return;
        if (GameManager.Hints <= 0)
        {
            actionButton.transform.DOKill();
            actionButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.35f, 6, 0.7f).SetUpdate(true);
            AudioManager.Instance?.PlaySFX("Wrong");
            FSAdsManager ads = FSAdsManager.Instance;
            if (rewardAdPending || ads == null || !ads.IsRewardedAdReady) return;
            rewardAdPending = true;
            ads.ShowRewardedAd(() =>
            {
                GameManager.AddHints(1);
                UpdateHintCountUI();
                UseHint();
            }, () => rewardAdPending = false);
            return;
        }
        AudioManager.Instance?.PlaySFX("pop-6");
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
            return;

        GameManager.TryUseHint();
        UpdateHintCountUI();
        selected.ShowHint(1.8f);
    }

    private void HandleMoveAttempted(TabCube cube, bool blocked)
    {
        AudioManager.Instance?.PlaySFX(blocked ? "Wrong" : "click");
        moveCount++;
    }

    private void HandleCubeExited(TabCube cube)
    {
        AudioManager.Instance?.PlaySFX("Slide");
        cubesRemaining = Mathf.Max(0, cubesRemaining - 1);
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
            int coinReward = GameManager.AwardCoinsForLevelStars(completionStarsEarned);
            AudioManager.Instance?.PlaySFX("StarBlast");
            if (resultTitle != null) resultTitle.text = "LEVEL COMPLETE!";
            if (resultDetails != null) resultDetails.text = $"LEVEL {currentLevel:00}\nTIME: {FormatTime(Mathf.CeilToInt(timeRemaining))}";
            if (coinRewardText != null) coinRewardText.text = $"+{coinReward}";
            UpdateGameplayUIVisibility(false);
            if (failedPanel != null) failedPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(true);
            StartCoroutine(StarSystem.PresentStars(resultStars, completionStarsEarned, clip => AudioManager.Instance?.PlaySFX(clip)));
        }
    }

    private void HandleLevelFailed()
    {
        if (!levelActive)
            return;

        levelActive = false;
        paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        TabCube.SetInputLocked(true);
        timeRemaining = 0f;
        UpdateTimerUI();
        if (resultPanel != null) resultPanel.SetActive(false);
        if (failedTitle != null) failedTitle.text = "LEVEL FAILED";
        if (failedDetails != null) failedDetails.text = $"LEVEL {currentLevel:00}\nHINTS: {GameManager.Hints}\nTIME: {FormatTime(Mathf.CeilToInt(timeRemaining))}";
        UpdateGameplayUIVisibility(false);
        if (failedPanel != null) failedPanel.SetActive(true);
        AudioManager.Instance?.PlaySFX("Wrong");
    }

    private void RestartLevel()
    {
        restartLevelAction?.Invoke();
    }

    private void ResumeWithCoinsOrAd()
    {
        if (!failedPanel.activeSelf || rewardAdPending) return;
        if (GameManager.Coins >= 50)
        {
            if (GameManager.TrySpendCoins(50)) ResumeAfterTime();
            return;
        }

        FSAdsManager ads = FSAdsManager.Instance;
        if (ads == null || !ads.IsRewardedAdReady)
        {
            if (continueButton != null) continueButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.35f, 5, 0.7f).SetUpdate(true);
            return;
        }

        rewardAdPending = true;
        ads.ShowRewardedAd(ResumeAfterTime, () => rewardAdPending = false);
    }

    private void ResumeAfterTime()
    {
        timeRemaining = 10f;
        levelActive = true;
        paused = false;
        if (failedPanel != null) failedPanel.SetActive(false);
        TabCube.SetInputLocked(false);
        UpdateGameplayUIVisibility(true);
        UpdateTimerUI();
    }

    private void UpdateGameplayUIVisibility(bool visible)
    {
        if (levelText != null) levelText.gameObject.SetActive(visible);
        if (timerText != null) timerText.gameObject.SetActive(visible);
        if (actionButton != null) actionButton.gameObject.SetActive(visible);
        if (pauseButton != null) pauseButton.gameObject.SetActive(visible);
        if (arrowHolder != null) arrowHolder.SetActive(visible);
    }

    private void LoadNextLevel()
    {
        loadNextLevelAction?.Invoke();
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

    private void UpdateHintCountUI()
    {
        if (buttonLabel != null)
            buttonLabel.text = GameManager.Hints.ToString();
        if (actionButton != null)
            actionButton.interactable = GameManager.HintButtonEnabled;
    }

    private void UpdateTimerUI()
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
        AudioManager.Instance?.PlaySFX("Count");
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

}



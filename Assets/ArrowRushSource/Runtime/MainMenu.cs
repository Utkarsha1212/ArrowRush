using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>Controls the scene-authored ArrowRush menu.</summary>
public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_Text modalTitle;
    [SerializeField] private TMP_Text modalBody;
    [SerializeField] private GameObject levelsPage;
    [SerializeField] private RectTransform levelsContent;
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private GameObject shopPage;
    [SerializeField] private TMP_Text progressText;
    private int chosenLevel;
    private int highestUnlocked;

    private void Awake()
    {
        if (canvas == null) { Debug.LogError("ArrowRush menu Canvas is missing from Menu.unity.", this); return; }
        highestUnlocked = Mathf.Max(1, PlayerPrefs.GetInt("ArrowRush.HighestUnlockedLevel", 1));
        chosenLevel = Mathf.Clamp(PlayerPrefs.GetInt("ArrowRush.SelectedLevel", highestUnlocked), 1, highestUnlocked);
        BindSceneButtons(); BindSettingsControls(); UpdateProgressLabel();
    }
    private void OpenLevels() { if (modalTitle != null) modalTitle.text = "SELECT LEVEL"; if (modalBody != null) modalBody.gameObject.SetActive(false); RefreshLevelButtons(); ShowPage(levelsPage); }
    private void OpenSettings() { if (modalTitle != null) modalTitle.text = "SETTINGS"; if (modalBody != null) modalBody.gameObject.SetActive(false); RefreshSettings(); ShowPage(settingsPage); }
    private void OpenShop() { if (modalTitle != null) modalTitle.text = "SHOP"; RefreshShop(); ShowPage(shopPage); }
    private void StartGame() => SceneManager.LoadScene("Level");

    private void BindSceneButtons()
    {
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            Action action = null;
            switch (button.gameObject.name)
            {
                case "PLAY Button": action = StartGame; break;
                case "LEVELS Button": action = OpenLevels; break;
                case "SETTINGS Button": action = OpenSettings; break;
                case "EXIT Button": action = Application.Quit; break;
                case "SHOP Button": action = OpenShop; break;
                case "Close":
                case "CLOSE Button": action = () => ShowPage(null); break;
                case "Toggle Music": action = ToggleMusic; break;
                case "Toggle SFX": action = ToggleSfx; break;
                case "Toggle Hints": action = ToggleHints; break;
                case "BUY 1 HINT": action = () => BuyHints(1, 50); break;
                case "BUY 5 HINTS": action = () => BuyHints(5, 200); break;
                case "BUY 10 HINTS": action = () => BuyHints(10, 350); break;
                case "WATCH AD FOR COINS": action = WatchAdForCoins; break;
                default:
                    if (button.gameObject.name.StartsWith("Level ") && int.TryParse(button.gameObject.name.Substring(6), out int level)) action = () => SelectLevel(level);
                    break;
            }
            if (action == null) continue;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => AudioManager.Instance?.PlaySFX("click"));
            button.onClick.AddListener(() => action());
        }
        RefreshLevelButtons();
    }
    private void BindSettingsControls()
    {
        if (settingsPage == null) return;

        // Settings controls use generic object names. Their Volume/SFX/Music/
        // Hints row is the reliable identifier in the authored hierarchy.
        foreach (Slider slider in settingsPage.GetComponentsInChildren<Slider>(true))
        {
            string category = GetSettingsCategory(slider.transform);
            if (category != "Volume" && category != "SFX") continue;
            AudioManager audio = AudioManager.Instance;
            bool isMasterVolume = category == "Volume";
            float volume = audio == null ? 1f : (isMasterVolume ? audio.GetVolume() : audio.SfxVolume);
            slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, volume));
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(value =>
            {
                float normalized = Mathf.InverseLerp(slider.minValue, slider.maxValue, value);
                if (isMasterVolume) AudioManager.Instance?.SetVolume(normalized);
                else AudioManager.Instance?.SetSfxVolume(normalized);
            });
        }

        foreach (Toggle toggle in settingsPage.GetComponentsInChildren<Toggle>(true))
        {
            string category = GetSettingsCategory(toggle.transform);
            AudioManager audio = AudioManager.Instance;
            bool isOn;
            switch (category)
            {
                case "Music": isOn = audio == null || audio.MusicEnabled; break;
                case "SFX": isOn = audio == null || audio.SfxEnabled; break;
                case "Hints": isOn = GameManager.HintButtonEnabled; break;
                default: continue;
            }
            toggle.SetIsOnWithoutNotify(isOn);
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(value =>
            {
                AudioManager.Instance?.PlaySFX("click");
                switch (category)
                {
                    case "Music": AudioManager.Instance?.SetMusicEnabled(value); break;
                    case "SFX": AudioManager.Instance?.SetSfxEnabled(value); break;
                    case "Hints": GameManager.SetHintButtonEnabled(value); break;
                }
            });
        }
    }
    private string GetSettingsCategory(Transform control)
    {
        for (Transform current = control; current != null && current != settingsPage.transform; current = current.parent)
            if (current.name == "Volume" || current.name == "Music" || current.name == "SFX" || current.name == "Hints") return current.name;
        return null;
    }
    private void SelectLevel(int level) { if (level > highestUnlocked) return; chosenLevel = level; PlayerPrefs.SetInt("ArrowRush.SelectedLevel", level); PlayerPrefs.Save(); StartGame(); }
    private void ToggleMusic() { if (AudioManager.Instance == null) return; AudioManager.Instance.SetMusicEnabled(!AudioManager.Instance.MusicEnabled); OpenSettings(); }
    private void ToggleSfx() { if (AudioManager.Instance == null) return; AudioManager.Instance.SetSfxEnabled(!AudioManager.Instance.SfxEnabled); OpenSettings(); }
    private void ToggleHints() { GameManager.SetHintButtonEnabled(!GameManager.HintButtonEnabled); RefreshSettings(); }
    private void RefreshSettings()
    {
        AudioManager audio = AudioManager.Instance; bool music = audio == null || audio.MusicEnabled; bool sfx = audio == null || audio.SfxEnabled;
        bool hints = GameManager.HintButtonEnabled;
        if (settingsPage != null)
            foreach (Toggle toggle in settingsPage.GetComponentsInChildren<Toggle>(true))
            {
                string category = GetSettingsCategory(toggle.transform);
                if (category == "Music") toggle.SetIsOnWithoutNotify(music);
                else if (category == "SFX") toggle.SetIsOnWithoutNotify(sfx);
                else if (category == "Hints") toggle.SetIsOnWithoutNotify(hints);
            }
        SetButtonLabel("Toggle Music", "MUSIC  " + (music ? "ON" : "OFF")); SetButtonLabel("Toggle SFX", "SOUND  " + (sfx ? "ON" : "OFF")); SetButtonLabel("Toggle Hints", "HINTS  " + (hints ? "ON" : "OFF"));
    }
    private void RefreshShop()
    {
        SetButtonLabel("BUY 1 HINT", "1 HINT  •  50 COINS"); SetButtonLabel("BUY 5 HINTS", "5 HINTS  •  200 COINS"); SetButtonLabel("BUY 10 HINTS", "10 HINTS  •  350 COINS");
        if (modalBody != null) { modalBody.text = $"COINS  {GameManager.Coins}     HINTS  {GameManager.Hints}\nEarn coins by clearing levels or watching a rewarded ad."; modalBody.gameObject.SetActive(true); }
    }
    private void BuyHints(int amount, int price)
    {
        if (!GameManager.TryBuyHints(amount, price)) { if (modalBody != null) modalBody.text = $"NOT ENOUGH COINS\nCOINS  {GameManager.Coins}     HINTS  {GameManager.Hints}"; return; }
        RefreshShop();
    }
    private void WatchAdForCoins()
    {
        if (modalBody != null) modalBody.text = "REWARDED ADS ARE NOT CONFIGURED YET. COINS ARE ADDED ONLY AFTER AN AD IS COMPLETED.";
    }
    private void RefreshLevelButtons()
    {
        if (levelsPage == null || levelsContent == null || levelButtonPrefab == null)
        {
            Debug.LogError("Assign the Levels content and Level button prefab on MainMenu.", this);
            return;
        }

        EnsureLevelButtons();
        foreach (Button button in levelsPage.GetComponentsInChildren<Button>(true))
        {
            if (!button.gameObject.name.StartsWith("Level ") || !int.TryParse(button.gameObject.name.Substring(6), out int level)) continue;
            bool unlocked = level <= highestUnlocked; button.interactable = unlocked; TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            int stars = Mathf.Clamp(PlayerPrefs.GetInt("ArrowRush.LevelStars." + level, 0), 0, 3);
            if (label != null) { label.text = unlocked ? $"LEVEL {level:00}\nSTARS {stars}/3" : "LOCKED"; label.fontSize = 23; }
            button.image.color = unlocked ? Color.white : new Color(.6f, .6f, .6f);
            RefreshLevelStarIcons(button.transform, stars);
        }
    }
    private static void RefreshLevelStarIcons(Transform levelButton, int earnedStars)
    {
        Transform starHolder = levelButton.Find("StarHolder");
        if (starHolder == null) return;

        for (int i = 0; i < starHolder.childCount; i++)
        {
            Transform starFill = starHolder.GetChild(i).Find("Star");
            if (starFill != null) starFill.gameObject.SetActive(i < earnedStars);
        }
    }
    private void EnsureLevelButtons()
    {
        int completedSet = (Mathf.Max(1, highestUnlocked) - 1) / 100;
        int levelCount = (completedSet + 1) * 100;
        for (int level = 1; level <= levelCount; level++)
        {
            string objectName = "Level " + level;
            Transform existing = levelsContent.Find(objectName);
            if (existing != null) continue;

            GameObject levelObject = Instantiate(levelButtonPrefab, levelsContent);
            levelObject.name = objectName;
            Button button = levelObject.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("The level button prefab needs a Button component on its root.", levelObject);
                Destroy(levelObject);
                return;
            }

            int selectedLevel = level;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => AudioManager.Instance?.PlaySFX("click"));
            button.onClick.AddListener(() => SelectLevel(selectedLevel));
        }
    }
    private void UpdateProgressLabel() { if (progressText != null) progressText.text = $"LEVEL {chosenLevel:00}"; }
    private void SetButtonLabel(string objectName, string label)
    { foreach (Button button in canvas.GetComponentsInChildren<Button>(true)) if (button.gameObject.name == objectName) { TMP_Text text = button.GetComponentInChildren<TMP_Text>(); if (text != null) text.text = label; return; } }
    private void ShowPage(GameObject page) { if (levelsPage != null) levelsPage.SetActive(page == levelsPage); if (settingsPage != null) settingsPage.SetActive(page == settingsPage); if (shopPage != null) shopPage.SetActive(page == shopPage); }
}

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
        BindSceneButtons(); BindVolumeSliders(); UpdateProgressLabel();
    }
    private void OpenLevels() { if (modalTitle != null) modalTitle.text = "SELECT LEVEL"; if (modalBody != null) modalBody.gameObject.SetActive(false); RefreshLevelButtons(); ShowPage(levelsPage); }
    private void OpenSettings() { if (modalTitle != null) modalTitle.text = "SETTINGS"; if (modalBody != null) modalBody.gameObject.SetActive(false); RefreshSettings(); ShowPage(settingsPage); }
    private void OpenShop() { if (modalTitle != null) modalTitle.text = "SHOP"; RefreshShop(); ShowPage(shopPage); }
    private void StartGame() => SceneManager.LoadScene("Level");

    private void BindSceneButtons()
    {
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => AudioManager.Instance?.PlaySFX("click"));
            switch (button.gameObject.name)
            {
                case "PLAY Button": button.onClick.AddListener(StartGame); break;
                case "LEVELS Button": button.onClick.AddListener(OpenLevels); break;
                case "SETTINGS Button": button.onClick.AddListener(OpenSettings); break;
                case "EXIT Button": button.onClick.AddListener(Application.Quit); break;
                case "SHOP Button": button.onClick.AddListener(OpenShop); break;
                case "Close":
                case "CLOSE Button": button.onClick.AddListener(() => ShowPage(null)); break;
                case "Toggle Music": button.onClick.AddListener(ToggleMusic); break;
                case "Toggle SFX": button.onClick.AddListener(ToggleSfx); break;
                case "Toggle Hints": button.onClick.AddListener(ToggleHints); break;
                case "BUY 1 HINT": button.onClick.AddListener(() => BuyHints(1, 50)); break;
                case "BUY 5 HINTS": button.onClick.AddListener(() => BuyHints(5, 200)); break;
                case "BUY 10 HINTS": button.onClick.AddListener(() => BuyHints(10, 350)); break;
                case "WATCH AD FOR COINS": button.onClick.AddListener(WatchAdForCoins); break;
                default: if (button.gameObject.name.StartsWith("Level ") && int.TryParse(button.gameObject.name.Substring(6), out int level)) button.onClick.AddListener(() => SelectLevel(level)); break;
            }
        }
        RefreshLevelButtons();
    }
    private void BindVolumeSliders()
    {
        foreach (Slider slider in canvas.GetComponentsInChildren<Slider>(true))
        {
            slider.onValueChanged.RemoveAllListeners();
            if (slider.name == "Music Volume Slider")
            {
                slider.SetValueWithoutNotify(AudioManager.Instance == null ? 1f : AudioManager.Instance.MusicVolume);
                slider.onValueChanged.AddListener(value => AudioManager.Instance?.SetMusicVolume(value));
            }
            else if (slider.name == "SFX Volume Slider")
            {
                slider.SetValueWithoutNotify(AudioManager.Instance == null ? 1f : AudioManager.Instance.SfxVolume);
                slider.onValueChanged.AddListener(value => AudioManager.Instance?.SetSfxVolume(value));
            }
        }
    }
    private void SelectLevel(int level) { if (level > highestUnlocked) return; chosenLevel = level; PlayerPrefs.SetInt("ArrowRush.SelectedLevel", level); PlayerPrefs.Save(); StartGame(); }
    private void ToggleMusic() { if (AudioManager.Instance == null) return; AudioManager.Instance.SetMusicEnabled(!AudioManager.Instance.MusicEnabled); OpenSettings(); }
    private void ToggleSfx() { if (AudioManager.Instance == null) return; AudioManager.Instance.SetSfxEnabled(!AudioManager.Instance.SfxEnabled); OpenSettings(); }
    private void ToggleHints() { GameManager.SetHintButtonEnabled(!GameManager.HintButtonEnabled); RefreshSettings(); }
    private void RefreshSettings()
    {
        AudioManager audio = AudioManager.Instance; bool music = audio == null || audio.MusicEnabled; bool sfx = audio == null || audio.SfxEnabled;
        bool hints = GameManager.HintButtonEnabled;
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
        if (levelsPage == null) return;
        foreach (Button button in levelsPage.GetComponentsInChildren<Button>(true))
        {
            if (!button.gameObject.name.StartsWith("Level ") || !int.TryParse(button.gameObject.name.Substring(6), out int level)) continue;
            bool unlocked = level <= highestUnlocked; button.interactable = unlocked; TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            int stars = Mathf.Clamp(PlayerPrefs.GetInt("ArrowRush.LevelStars." + level, 0), 0, 3);
            if (label != null) { label.text = unlocked ? $"LEVEL {level:00}\nSTARS {stars}/3" : "LOCKED"; label.fontSize = 23; }
            button.image.color = unlocked ? Color.white : new Color(.6f, .6f, .6f);
        }
    }
    private void UpdateProgressLabel() { if (progressText != null) progressText.text = $"LEVEL {chosenLevel:00}"; }
    private void SetButtonLabel(string objectName, string label)
    { foreach (Button button in canvas.GetComponentsInChildren<Button>(true)) if (button.gameObject.name == objectName) { TMP_Text text = button.GetComponentInChildren<TMP_Text>(); if (text != null) text.text = label; return; } }
    private void ShowPage(GameObject page) { if (levelsPage != null) levelsPage.SetActive(page == levelsPage); if (settingsPage != null) settingsPage.SetActive(page == settingsPage); if (shopPage != null) shopPage.SetActive(page == shopPage); }
}

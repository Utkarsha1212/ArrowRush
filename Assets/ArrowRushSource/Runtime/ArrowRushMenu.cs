using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controls the scene-authored ArrowRush menu.</summary>
public sealed class ArrowRushMenu : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject modal;
    [SerializeField] private Text modalTitle;
    [SerializeField] private Text modalBody;
    [SerializeField] private GameObject levelsPage;
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private GameObject shopPage;
    [SerializeField] private Text progressText;
    private int chosenLevel;
    private int highestUnlocked;

    private void Awake()
    {
        if (canvas == null) { Debug.LogError("ArrowRush menu Canvas is missing from Menu.unity.", this); return; }
        highestUnlocked = Mathf.Max(1, PlayerPrefs.GetInt("ArrowRush.HighestUnlockedLevel", 1));
        chosenLevel = Mathf.Clamp(PlayerPrefs.GetInt("ArrowRush.SelectedLevel", highestUnlocked), 1, highestUnlocked);
        BindSceneButtons(); BindVolumeSliders(); UpdateProgressLabel();
    }
    private void OpenLevels() { modalTitle.text = "SELECT LEVEL"; modalBody.gameObject.SetActive(false); RefreshLevelButtons(); ShowPage(levelsPage); modal.SetActive(true); }
    private void OpenSettings() { modalTitle.text = "SETTINGS"; modalBody.gameObject.SetActive(false); RefreshSettings(); ShowPage(settingsPage); modal.SetActive(true); }
    private void OpenShop() { modalTitle.text = "SHOP"; RefreshShop(); ShowPage(shopPage); modal.SetActive(true); }
    private void StartGame() => SceneManager.LoadScene("Level");

    private void BindSceneButtons()
    {
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => ArrowRushAudio.Instance?.Play("click"));
            switch (button.gameObject.name)
            {
                case "PLAY Button": button.onClick.AddListener(StartGame); break;
                case "LEVELS Button": button.onClick.AddListener(OpenLevels); break;
                case "SETTINGS Button": button.onClick.AddListener(OpenSettings); break;
                case "EXIT Button": button.onClick.AddListener(Application.Quit); break;
                case "SHOP Button": button.onClick.AddListener(OpenShop); break;
                case "CLOSE Button": button.onClick.AddListener(() => modal.SetActive(false)); break;
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
                slider.SetValueWithoutNotify(ArrowRushAudio.Instance == null ? 1f : ArrowRushAudio.Instance.MusicVolume);
                slider.onValueChanged.AddListener(value => ArrowRushAudio.Instance?.SetMusicVolume(value));
            }
            else if (slider.name == "SFX Volume Slider")
            {
                slider.SetValueWithoutNotify(ArrowRushAudio.Instance == null ? 1f : ArrowRushAudio.Instance.SfxVolume);
                slider.onValueChanged.AddListener(value => ArrowRushAudio.Instance?.SetSfxVolume(value));
            }
        }
    }
    private void SelectLevel(int level) { if (level > highestUnlocked) return; chosenLevel = level; PlayerPrefs.SetInt("ArrowRush.SelectedLevel", level); PlayerPrefs.Save(); StartGame(); }
    private void ToggleMusic() { if (ArrowRushAudio.Instance == null) return; ArrowRushAudio.Instance.SetMusicEnabled(!ArrowRushAudio.Instance.MusicEnabled); OpenSettings(); }
    private void ToggleSfx() { if (ArrowRushAudio.Instance == null) return; ArrowRushAudio.Instance.SetSfxEnabled(!ArrowRushAudio.Instance.SfxEnabled); OpenSettings(); }
    private void ToggleHints() { bool enabled = PlayerPrefs.GetInt("ArrowRush.HintsEnabled", 1) == 0; PlayerPrefs.SetInt("ArrowRush.HintsEnabled", enabled ? 1 : 0); PlayerPrefs.Save(); RefreshSettings(); }
    private void RefreshSettings()
    {
        ArrowRushAudio audio = ArrowRushAudio.Instance; bool music = audio == null || audio.MusicEnabled; bool sfx = audio == null || audio.SfxEnabled;
        bool hints = PlayerPrefs.GetInt("ArrowRush.HintsEnabled", 1) == 1;
        SetButtonLabel("Toggle Music", "MUSIC  " + (music ? "ON" : "OFF")); SetButtonLabel("Toggle SFX", "SOUND  " + (sfx ? "ON" : "OFF")); SetButtonLabel("Toggle Hints", "HINTS  " + (hints ? "ON" : "OFF"));
    }
    private void RefreshShop()
    {
        SetButtonLabel("BUY 1 HINT", "1 HINT  •  50 COINS"); SetButtonLabel("BUY 5 HINTS", "5 HINTS  •  200 COINS"); SetButtonLabel("BUY 10 HINTS", "10 HINTS  •  350 COINS");
        if (modalBody != null) { modalBody.text = $"COINS  {ArrowRushEconomy.Coins}     HINTS  {ArrowRushEconomy.Hints}\nEarn coins by clearing levels or watching a rewarded ad."; modalBody.gameObject.SetActive(true); }
    }
    private void BuyHints(int amount, int price)
    {
        if (!ArrowRushEconomy.TryBuyHints(amount, price)) { if (modalBody != null) modalBody.text = $"NOT ENOUGH COINS\nCOINS  {ArrowRushEconomy.Coins}     HINTS  {ArrowRushEconomy.Hints}"; return; }
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
            bool unlocked = level <= highestUnlocked; button.interactable = unlocked; Text label = button.GetComponentInChildren<Text>();
            int stars = Mathf.Clamp(PlayerPrefs.GetInt("ArrowRush.LevelStars." + level, 0), 0, 3);
            if (label != null) { label.text = unlocked ? $"LEVEL {level:00}\nSTARS {stars}/3" : "LOCKED"; label.fontSize = 23; }
            button.image.color = unlocked ? Color.white : new Color(.6f, .6f, .6f);
        }
    }
    private void UpdateProgressLabel() { if (progressText != null) progressText.text = $"LEVEL {chosenLevel:00}"; }
    private void SetButtonLabel(string objectName, string label)
    { foreach (Button button in canvas.GetComponentsInChildren<Button>(true)) if (button.gameObject.name == objectName) { Text text = button.GetComponentInChildren<Text>(); if (text != null) text.text = label; return; } }
    private void ShowPage(GameObject page) { if (levelsPage != null) levelsPage.SetActive(page == levelsPage); if (settingsPage != null) settingsPage.SetActive(page == settingsPage); if (shopPage != null) shopPage.SetActive(page == shopPage); }
}

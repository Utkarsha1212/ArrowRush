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
        if (canvas == null)
        {
            Debug.LogError("ArrowRush menu Canvas is missing from Menu.unity.", this);
            return;
        }
        highestUnlocked = Mathf.Max(1, PlayerPrefs.GetInt("ArrowRush.HighestUnlockedLevel", 1));
        chosenLevel = Mathf.Clamp(PlayerPrefs.GetInt("ArrowRush.SelectedLevel", highestUnlocked), 1, highestUnlocked);
        BindSceneButtons();
        UpdateProgressLabel();
    }

    private void OpenLevels()
    {
        modalTitle.text = "SELECT LEVEL";
        modalBody.gameObject.SetActive(false);
        RefreshLevelButtons();
        ShowPage(levelsPage);
        modal.SetActive(true);
    }

    private void OpenSettings()
    {
        modalTitle.text = "SETTINGS";
        modalBody.gameObject.SetActive(false);
        RefreshSettings();
        ShowPage(settingsPage);
        modal.SetActive(true);
    }

    private void OpenShop()
    {
        modalTitle.text = "HINT SHOP";
        RefreshShop();
        ShowPage(shopPage);
        modal.SetActive(true);
    }

    private void StartGame()
    {
        SceneManager.LoadScene("Level");
    }

    private void BindSceneButtons()
    {
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ArrowRushAudio.Instance?.Play("click"));
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
                case "MUSIC VOLUME DOWN": button.onClick.AddListener(() => AdjustMusicVolume(-0.1f)); break;
                case "MUSIC VOLUME UP": button.onClick.AddListener(() => AdjustMusicVolume(0.1f)); break;
                case "SFX VOLUME DOWN": button.onClick.AddListener(() => AdjustSfxVolume(-0.1f)); break;
                case "SFX VOLUME UP": button.onClick.AddListener(() => AdjustSfxVolume(0.1f)); break;
                case "Toggle Hints": button.onClick.AddListener(ToggleHints); break;
                case "BUY 1 HINT": button.onClick.AddListener(() => BuyHints(1, 50)); break;
                case "BUY 5 HINTS": button.onClick.AddListener(() => BuyHints(5, 200)); break;
                case "BUY 10 HINTS": button.onClick.AddListener(() => BuyHints(10, 350)); break;
                default:
                    if (button.gameObject.name.StartsWith("Level ") && int.TryParse(button.gameObject.name.Substring(6), out int level))
                        button.onClick.AddListener(() => SelectLevel(level));
                    break;
            }
        }
        RefreshLevelButtons();
    }

    private void SelectLevel(int level)
    {
        if (level > highestUnlocked) return;
        chosenLevel = level;
        PlayerPrefs.SetInt("ArrowRush.SelectedLevel", level);
        PlayerPrefs.Save();
        StartGame();
    }

    private void ToggleMusic()
    {
        if (ArrowRushAudio.Instance == null) return;
        ArrowRushAudio.Instance.SetMusicEnabled(!ArrowRushAudio.Instance.MusicEnabled);
        OpenSettings();
    }

    private void ToggleSfx()
    {
        if (ArrowRushAudio.Instance == null) return;
        ArrowRushAudio.Instance.SetSfxEnabled(!ArrowRushAudio.Instance.SfxEnabled);
        OpenSettings();
    }

    private void AdjustMusicVolume(float amount)
    {
        if (ArrowRushAudio.Instance == null) return;
        ArrowRushAudio.Instance.SetMusicVolume(ArrowRushAudio.Instance.MusicVolume + amount);
        RefreshSettings();
    }

    private void AdjustSfxVolume(float amount)
    {
        if (ArrowRushAudio.Instance == null) return;
        ArrowRushAudio.Instance.SetSfxVolume(ArrowRushAudio.Instance.SfxVolume + amount);
        RefreshSettings();
    }

    private void ToggleHints()
    {
        bool enabled = PlayerPrefs.GetInt("ArrowRush.HintsEnabled", 1) == 0;
        PlayerPrefs.SetInt("ArrowRush.HintsEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
        RefreshSettings();
    }

    private void RefreshSettings()
    {
        ArrowRushAudio audio = ArrowRushAudio.Instance;
        bool music = audio == null || audio.MusicEnabled;
        bool sfx = audio == null || audio.SfxEnabled;
        bool hints = PlayerPrefs.GetInt("ArrowRush.HintsEnabled", 1) == 1;
        SetButtonLabel("Toggle Music", "MUSIC  " + (music ? "ON" : "OFF"));
        SetButtonLabel("Toggle SFX", "SOUND  " + (sfx ? "ON" : "OFF"));
        int musicVolume = Mathf.RoundToInt((audio == null ? 1f : audio.MusicVolume) * 100f);
        int sfxVolume = Mathf.RoundToInt((audio == null ? 1f : audio.SfxVolume) * 100f);
        SetButtonLabel("MUSIC VOLUME DOWN", $"MUSIC VOL  {musicVolume}%  −");
        SetButtonLabel("MUSIC VOLUME UP", $"MUSIC VOL  {musicVolume}%  +");
        SetButtonLabel("SFX VOLUME DOWN", $"SOUND VOL  {sfxVolume}%  −");
        SetButtonLabel("SFX VOLUME UP", $"SOUND VOL  {sfxVolume}%  +");
        SetButtonLabel("Toggle Hints", "HINTS  " + (hints ? "ON" : "OFF"));
    }

    private void RefreshShop()
    {
        SetButtonLabel("BUY 1 HINT", "1 HINT  •  50 COINS");
        SetButtonLabel("BUY 5 HINTS", "5 HINTS  •  200 COINS");
        SetButtonLabel("BUY 10 HINTS", "10 HINTS  •  350 COINS");
        if (modalBody != null)
        {
            modalBody.text = $"COINS  {ArrowRushEconomy.Coins}     HINTS  {ArrowRushEconomy.Hints}\nEarn coins by clearing levels.";
            modalBody.gameObject.SetActive(true);
        }
    }

    private void BuyHints(int amount, int price)
    {
        if (!ArrowRushEconomy.TryBuyHints(amount, price))
        {
            if (modalBody != null) modalBody.text = $"NOT ENOUGH COINS\nCOINS  {ArrowRushEconomy.Coins}     HINTS  {ArrowRushEconomy.Hints}\nEarn coins by clearing levels.";
            return;
        }
        RefreshShop();
    }

    private void RefreshLevelButtons()
    {
        if (levelsPage == null) return;
        foreach (Button button in levelsPage.GetComponentsInChildren<Button>(true))
        {
            if (!button.gameObject.name.StartsWith("Level ") || !int.TryParse(button.gameObject.name.Substring(6), out int level)) continue;
            bool unlocked = level <= highestUnlocked;
            button.interactable = unlocked;
            Text label = button.GetComponentInChildren<Text>();
            int savedStars = Mathf.Clamp(PlayerPrefs.GetInt("ArrowRush.LevelStars." + level, 0), 0, 3);
            if (label != null)
            {
                label.text = unlocked ? $"LEVEL {level:00}\nSTARS {savedStars}/3" : "LOCKED";
                label.fontSize = 23;
            }
            button.image.color = unlocked ? Color.white : new Color(.6f,.6f,.6f);
        }
    }

    private void UpdateProgressLabel()
    {
        if (progressText != null) progressText.text = $"LEVEL {chosenLevel:00}";
    }

    private void SetButtonLabel(string objectName, string label)
    {
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.name != objectName) continue;
            Text text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
            return;
        }
    }
    private void ShowPage(GameObject page)
    {
        if (levelsPage != null) levelsPage.SetActive(page == levelsPage);
        if (settingsPage != null) settingsPage.SetActive(page == settingsPage);
        if (shopPage != null) shopPage.SetActive(page == shopPage);
    }


}


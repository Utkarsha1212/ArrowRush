using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the Brick Breakout inspired menu and level picker for ArrowRush.</summary>
public sealed class ArrowRushMenu : MonoBehaviour
{
    private static readonly Color Navy = new Color(0.025f, 0.08f, 0.30f, 0.93f);
    private static readonly Color Blue = new Color(0.03f, 0.36f, 0.95f, 1f);
    private static readonly Color Green = new Color(0.06f, 0.80f, 0.23f, 1f);
    private static readonly Color Pink = new Color(0.96f, 0.10f, 0.39f, 1f);
    private static Sprite[] buttonSprites;
    private Canvas canvas;
    private GameObject modal;
    private Text modalTitle;
    private Text modalBody;
    private int chosenLevel;
    private int highestUnlocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Menu") return;
        if (Object.FindObjectOfType<ArrowRushMenu>() != null) return;
        new GameObject("ArrowRush Menu UI").AddComponent<ArrowRushMenu>();
    }

    private void Awake()
    {
        EnsureEventSystem();
        highestUnlocked = Mathf.Max(1, PlayerPrefs.GetInt("ArrowRush.HighestUnlockedLevel", 1));
        chosenLevel = Mathf.Clamp(PlayerPrefs.GetInt("ArrowRush.SelectedLevel", highestUnlocked), 1, highestUnlocked);
        Build();
    }

    private void Build()
    {
        GameObject canvasObject = new GameObject("Brick Breakout Style Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform root = canvasObject.GetComponent<RectTransform>();
        Stretch(root);

        Texture2D background = Resources.Load<Texture2D>("BrickBreakoutPuzzle/Sprites/Glossy Blue-Purple Game Menu Background");
        if (background != null)
        {
            Image bg = MakeImage("Background", root, GetSprite(background), Color.white);
            Stretch(bg.rectTransform);
            bg.transform.SetAsFirstSibling();
        }
        Texture2D logo = Resources.Load<Texture2D>("BrickBreakoutPuzzle/Sprites/Glossy Breakout Puzzle Game Logo");
        if (logo != null)
        {
            Image title = MakeImage("Game Logo", root, GetSprite(logo), Color.white);
            Place(title.rectTransform, new Vector2(.5f, .75f), new Vector2(.5f, .5f), new Vector2(900f, 360f));
        }

        Button play = MakeButton("PLAY", Green, root, new Vector2(.5f, .48f), new Vector2(730f, 150f), StartGame);
        play.GetComponentInChildren<Text>().fontSize = 62;
        MakeButton("LEVELS", Blue, root, new Vector2(.5f, .35f), new Vector2(520f, 118f), OpenLevels);
        MakeButton("SETTINGS", Blue, root, new Vector2(.34f, .235f), new Vector2(310f, 105f), OpenSettings);
        MakeButton("EXIT", Pink, root, new Vector2(.66f, .235f), new Vector2(310f, 105f), Application.Quit);
        MakeButton("SHOP", new Color(.98f, .56f, .04f), root, new Vector2(.5f, .13f), new Vector2(310f, 100f), OpenShop);
        Text progress = MakeText(root, $"LEVEL {chosenLevel:00}", 34, Color.white);
        Place(progress.rectTransform, new Vector2(.5f, .065f), new Vector2(.5f, .5f), new Vector2(400f, 60f));
        BuildModal(root);
    }

    private void BuildModal(RectTransform root)
    {
        modal = new GameObject("Menu Modal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(root, false);
        RectTransform rect = modal.GetComponent<RectTransform>();
        Place(rect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(850f, 1050f));
        modal.GetComponent<Image>().color = Navy;
        modal.SetActive(false);
        modalTitle = MakeText(rect, "LEVEL SELECT", 48, Color.white);
        Place(modalTitle.rectTransform, new Vector2(.5f, .92f), new Vector2(.5f, .5f), new Vector2(760f, 90f));
        modalBody = MakeText(rect, string.Empty, 38, Color.white);
        Place(modalBody.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(680f, 260f));
        MakeButton("CLOSE", Pink, rect, new Vector2(.5f, .09f), new Vector2(440f, 100f), () => modal.SetActive(false));
    }

    private void OpenLevels()
    {
        modalTitle.text = "SELECT LEVEL";
        modalBody.gameObject.SetActive(false);
        foreach (Transform child in modal.transform)
            if (child.name.StartsWith("Level ") || child.name.StartsWith("Toggle ")) Destroy(child.gameObject);
        for (int i = 1; i <= 12; i++)
        {
            int level = i;
            int col = (i - 1) % 3;
            int row = (i - 1) / 3;
            bool unlocked = level <= highestUnlocked;
            Button button = MakeButton(unlocked ? $"LEVEL {level:00}" : "LOCKED", !unlocked ? new Color(.36f,.42f,.58f) : level == chosenLevel ? Green : Blue, modal.transform,
                new Vector2(.22f + col * .28f, .72f - row * .15f), new Vector2(205f, 110f), () =>
                {
                    if (level > highestUnlocked) return;
                    chosenLevel = level;
                    PlayerPrefs.SetInt("ArrowRush.SelectedLevel", level);
                    PlayerPrefs.Save();
                    StartGame();
                });
            button.gameObject.name = "Level " + level;
            button.GetComponentInChildren<Text>().fontSize = 31;
        }
        modal.SetActive(true);
    }

    private void OpenSettings()
    {
        modalTitle.text = "SETTINGS";
        modalBody.gameObject.SetActive(false);
        foreach (Transform child in modal.transform)
            if (child.name.StartsWith("Level ") || child.name.StartsWith("Toggle ")) Destroy(child.gameObject);
        bool music = ArrowRushAudio.Instance == null || ArrowRushAudio.Instance.MusicEnabled;
        bool sfx = ArrowRushAudio.Instance == null || ArrowRushAudio.Instance.SfxEnabled;
        MakeButton("MUSIC  " + (music ? "ON" : "OFF"), Blue, modal.transform, new Vector2(.5f, .66f), new Vector2(560f, 115f), () =>
        {
            if (ArrowRushAudio.Instance == null) return;
            ArrowRushAudio.Instance.SetMusicEnabled(!ArrowRushAudio.Instance.MusicEnabled);
            OpenSettings();
        }).gameObject.name = "Toggle Music";
        MakeButton("SOUND  " + (sfx ? "ON" : "OFF"), Blue, modal.transform, new Vector2(.5f, .49f), new Vector2(560f, 115f), () =>
        {
            if (ArrowRushAudio.Instance == null) return;
            ArrowRushAudio.Instance.SetSfxEnabled(!ArrowRushAudio.Instance.SfxEnabled);
            OpenSettings();
        }).gameObject.name = "Toggle SFX";
        modal.SetActive(true);
    }

    private void OpenShop()
    {
        modalTitle.text = "HINT SHOP";
        modalBody.text = "Hints are ready in every level.\nTap HINT during play to reveal a clear cube.";
        modalBody.gameObject.SetActive(true);
        foreach (Transform child in modal.transform)
            if (child.name.StartsWith("Level ") || child.name.StartsWith("Toggle ")) Destroy(child.gameObject);
        modal.SetActive(true);
    }

    private void StartGame()
    {
        SceneManager.LoadScene("Level");
    }

    private Button MakeButton(string label, Color color, Transform parent, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction click)
    {
        GameObject go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Place(rect, anchor, new Vector2(.5f, .5f), size);
        Image image = go.GetComponent<Image>();
        image.sprite = GetButtonSprite(label, color); image.type = Image.Type.Simple; image.color = Color.white;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => ArrowRushAudio.Instance?.Play("click"));
        button.onClick.AddListener(click);
        Text text = MakeText(rect, label, 36, Color.white);
        text.fontStyle = FontStyle.Bold;
        Stretch(text.rectTransform);
        return button;
    }

    private Text MakeText(Transform parent, string value, int size, Color color)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.Load<Font>("BrickBreakoutPuzzle/Fonts/Bubble Bobble") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size; text.alignment = TextAnchor.MiddleCenter; text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.raycastTarget = false;
        return text;
    }

    private Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = false;
        return image;
    }

    private Sprite GetButtonSprite(string label, Color color)
    {
        if (buttonSprites == null)
            buttonSprites = Resources.LoadAll<Sprite>("BrickBreakoutPuzzle/Sprites/UI/MenuUI");
        int spriteIndex = label == "PLAY" ? 0 : color == Green ? 5 : color == Blue ? 6 : color == Pink ? 7 : color.r > .8f && color.g > .4f ? 1 : 6;
        if (buttonSprites != null)
            foreach (Sprite sprite in buttonSprites)
                if (sprite.name == "MenuUI_" + spriteIndex) return sprite;
        Texture2D pixel = Texture2D.whiteTexture;
        return Sprite.Create(pixel, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f, 0, SpriteMeshType.FullRect, new Vector4(.4f,.4f,.4f,.4f));
    }

    private static Sprite GetSprite(Texture2D texture) => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject system = new GameObject("EventSystem", typeof(EventSystem));
        system.AddComponent<StandaloneInputModule>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 size)
    {
        rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
    }
}

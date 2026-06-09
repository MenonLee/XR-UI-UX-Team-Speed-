using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(G29PauseMenu))]
public class PauseMenuUiBuilder : MonoBehaviour
{
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool useWorldSpaceCanvas = true;
    [SerializeField] private float vrMenuDistance = 1f;
    [SerializeField] private float vrMenuScale = 0.001f;
    [SerializeField] private string title = "PAUSED";
    [SerializeField] private string subtitle = "Race session stopped";

    private G29PauseMenu pauseMenu;
    private static Material noZTestMaterial;

    private void Awake()
    {
        pauseMenu = GetComponent<G29PauseMenu>();
    }

    private void Start()
    {
        if (buildOnStart)
        {
            Build();
        }
    }

    [ContextMenu("Build Pause Menu UI")]
    public void Build()
    {
        if (pauseMenu == null)
        {
            pauseMenu = GetComponent<G29PauseMenu>();
        }

        Transform existingMenu = transform.Find("PauseMenuCanvas");

        if (existingMenu != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existingMenu.gameObject);
            }
            else
            {
                DestroyImmediate(existingMenu.gameObject);
            }
        }

        GameObject root = new GameObject("PauseMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.sortingOrder = 100;

        if (useWorldSpaceCanvas)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            root.AddComponent<VRCameraCanvasPlacement>()
                .Configure(Camera.main != null ? Camera.main.transform : null, vrMenuDistance, Vector2.zero, vrMenuScale, false);
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = useWorldSpaceCanvas ? CanvasScaler.ScaleMode.ConstantPixelSize : CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image dim = CreateImage("Dim", root.transform, new Color(0f, 0f, 0f, 0f));
        RectTransform dimRect = dim.rectTransform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        Image panel = CreateImage("Panel", root.transform, new Color(0.02f, 0.025f, 0.03f, 0.82f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(540f, 380f);
        panelRect.anchoredPosition = Vector2.zero;
        panel.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.55f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 28, 28);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image accent = CreateImage("Accent", panel.transform, new Color(0.08f, 0.72f, 0.56f, 1f));
        AddLayoutElement(accent.gameObject, 6f);

        Text titleText = CreateText("Title", panel.transform, title, 46, FontStyle.Bold, new Color(0.96f, 0.98f, 1f, 1f));
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.65f);
        AddLayoutElement(titleText.gameObject, 56f);

        Text subtitleText = CreateText("Subtitle", panel.transform, subtitle, 20, FontStyle.Normal, new Color(0.72f, 0.78f, 0.86f, 1f));
        subtitleText.alignment = TextAnchor.MiddleCenter;
        AddLayoutElement(subtitleText.gameObject, 28f);

        AddSpacer(panel.transform, 2f);
        Button resumeButton = CreateButton(panel.transform, "Resume", new Color(0.10f, 0.58f, 0.42f, 1f), pauseMenu.Resume);
        Button lobbyButton = CreateButton(panel.transform, "Return To Lobby", new Color(0.14f, 0.27f, 0.68f, 1f), pauseMenu.ReturnToLobby);
        Button quitButton = CreateButton(panel.transform, "Quit Game", new Color(0.70f, 0.08f, 0.12f, 1f), pauseMenu.QuitGame);

        root.SetActive(false);
        pauseMenu.SetPauseMenuRoot(root);
        pauseMenu.SetPauseMenuButtons(new[] { resumeButton, lobbyButton, quitButton });
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        Image image = obj.GetComponent<Image>();
        image.color = color;
        ApplyNoZTestMaterial(image);
        return image;
    }

    private static Text CreateText(string objectName, Transform parent, string content, int size, FontStyle style, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);

        Text text = obj.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        ApplyNoZTestMaterial(text);
        return text;
    }

    private static Button CreateButton(Transform parent, string label, Color normalColor, UnityAction action)
    {
        GameObject obj = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);

        Image image = obj.GetComponent<Image>();
        image.color = normalColor;
        ApplyNoZTestMaterial(image);

        Button button = obj.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.14f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        colors.fadeDuration = 0f;
        button.colors = colors;
        button.onClick.AddListener(action);

        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(0f, -3f);

        Text text = CreateText("Label", obj.transform, label, 28, FontStyle.Bold, Color.white);
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = 28;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        AddLayoutElement(obj, 64f);
        return button;
    }

    private static void ApplyNoZTestMaterial(Graphic graphic)
    {
        if (graphic == null)
        {
            return;
        }

        Material material = GetNoZTestMaterial();

        if (material != null)
        {
            graphic.material = material;
        }
    }

    private static Material GetNoZTestMaterial()
    {
        if (noZTestMaterial != null)
        {
            return noZTestMaterial;
        }

        Shader shader = Shader.Find("UI/NoZTest");

        if (shader == null)
        {
            return null;
        }

        noZTestMaterial = new Material(shader)
        {
            name = "Runtime UI NoZTest"
        };

        return noZTestMaterial;
    }

    private static void AddSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(parent, false);
        AddLayoutElement(spacer, height);
    }

    private static void AddLayoutElement(GameObject obj, float preferredHeight)
    {
        LayoutElement element = obj.GetComponent<LayoutElement>();

        if (element == null)
        {
            element = obj.AddComponent<LayoutElement>();
        }

        element.preferredHeight = preferredHeight;
        element.minHeight = preferredHeight;
        element.flexibleHeight = 0f;
    }
}

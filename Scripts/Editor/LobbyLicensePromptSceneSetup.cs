using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[InitializeOnLoad]
public static class LobbyLicensePromptSceneSetup
{
    private const string ScenePath = "Assets/Scenes/LobbyScene.unity";
    private const string PanelName = "LicensePromptPanel";
    private const string ControllerName = "LobbyLicensePromptController";
    private const string StyleVersionMarkerName = "LicensePromptStyle_v3";

    static LobbyLicensePromptSceneSetup()
    {
        EditorApplication.delayCall += SetupOpenLobbySceneIfNeeded;
    }

    [DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += SetupOpenLobbySceneIfNeeded;
    }

    private static void SetupOpenLobbySceneIfNeeded()
    {
        if (HasPanelInSceneFile())
        {
            return;
        }

        SetupLobbyScene();
    }

    [MenuItem("Tools/Racing Game/Setup Lobby License Prompt UI")]
    public static void SetupLobbyScene()
    {
        Scene previousActiveScene = EditorSceneManager.GetActiveScene();
        Scene scene = FindLoadedScene(ScenePath);
        bool openedAdditive = false;

        if (!scene.IsValid())
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            openedAdditive = previousActiveScene.IsValid() && previousActiveScene.path != ScenePath;
        }

        EditorSceneManager.SetActiveScene(scene);
        RemoveIfExists(PanelName);
        RemoveIfExists(ControllerName);
        RemoveIfExists(StyleVersionMarkerName);

        Transform parent = FindLobbyUiParent();
        Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;

        if (canvas == null)
        {
            canvas = CreateWorldSpaceCanvas();
            parent = canvas.transform;
        }

        canvas.renderMode = RenderMode.WorldSpace;

        if (Camera.main != null)
        {
            canvas.worldCamera = Camera.main;
        }

        GameObject panel = CreatePanel(parent);
        Button automaticButton = CreateButton(panel.transform, "AutomaticButton", "Automatic", new Vector2(-135f, -58f), new Color(0.05f, 0.58f, 0.39f, 1f));
        Button manualButton = CreateButton(panel.transform, "ManualButton", "Manual", new Vector2(135f, -58f), new Color(0.11f, 0.27f, 0.72f, 1f));
        ConfigureNavigation(automaticButton, manualButton);

        GameObject controller = new GameObject(ControllerName);
        controller.transform.SetParent(parent, false);
        LobbyLicensePrompt prompt = controller.AddComponent<LobbyLicensePrompt>();

        SerializedObject serializedPrompt = new SerializedObject(prompt);
        serializedPrompt.FindProperty("promptRoot").objectReferenceValue = panel;
        serializedPrompt.FindProperty("promptCanvasGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
        serializedPrompt.FindProperty("automaticButton").objectReferenceValue = automaticButton;
        serializedPrompt.FindProperty("manualButton").objectReferenceValue = manualButton;
        serializedPrompt.FindProperty("hideOnStart").boolValue = true;
        serializedPrompt.ApplyModifiedPropertiesWithoutUndo();

        EnsureEventSystem();
        panel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (openedAdditive)
        {
            EditorSceneManager.SetActiveScene(previousActiveScene);
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log("[LobbyLicensePromptSceneSetup] Created LicensePromptPanel and LobbyLicensePromptController in LobbyScene.");
    }

    private static Scene FindLoadedScene(string path)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.IsValid() && scene.path == path)
            {
                return scene;
            }
        }

        return default;
    }

    private static bool HasPanelInSceneFile()
    {
        string fullPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ScenePath);
        return System.IO.File.Exists(fullPath)
            && System.IO.File.ReadAllText(fullPath).Contains("m_Name: " + StyleVersionMarkerName);
    }

    private static Transform FindLobbyUiParent()
    {
        GameObject lobbyUiGroup = GameObject.Find("Lobby_UI_Group");

        if (lobbyUiGroup != null)
        {
            return lobbyUiGroup.transform;
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        return canvas != null ? canvas.transform : null;
    }

    private static Canvas CreateWorldSpaceCanvas()
    {
        GameObject canvasObject = new GameObject("LobbyLicenseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920f, 1080f);
        rect.localScale = Vector3.one * 0.002f;

        if (Camera.main != null)
        {
            canvas.worldCamera = Camera.main;
            rect.position = Camera.main.transform.position + Camera.main.transform.forward * 1.4f;
            rect.rotation = Camera.main.transform.rotation;
        }

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        return canvas;
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject(PanelName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.layer = LayerMask.NameToLayer("UI");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -12f);
        rect.sizeDelta = new Vector2(640f, 340f);

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.018f, 0.022f, 0.03f, 0.96f);
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.16f, 0.82f, 0.76f, 0.55f);
        panelOutline.effectDistance = new Vector2(2f, -2f);
        Shadow panelShadow = panel.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        panelShadow.effectDistance = new Vector2(6f, -6f);

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        GameObject styleMarker = new GameObject(StyleVersionMarkerName, typeof(RectTransform));
        styleMarker.layer = LayerMask.NameToLayer("UI");
        styleMarker.transform.SetParent(panel.transform, false);

        Text title = CreateText(panel.transform, "Title", "Your Lisence?", 46, FontStyle.Bold);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 86f);
        titleRect.sizeDelta = new Vector2(520f, 72f);

        Shadow titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        titleShadow.effectDistance = new Vector2(2f, -2f);

        return panel;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Color color)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.layer = LayerMask.NameToLayer("UI");
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(230f, 72f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = Color.Lerp(color, Color.white, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
        shadow.effectDistance = new Vector2(4f, -4f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.28f);
        colors.selectedColor = Color.Lerp(color, Color.white, 0.28f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.disabledColor = Color.Lerp(color, Color.black, 0.45f);
        button.colors = colors;

        GameObject highlight = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
        highlight.layer = LayerMask.NameToLayer("UI");
        highlight.transform.SetParent(buttonObject.transform, false);
        RectTransform highlightRect = highlight.GetComponent<RectTransform>();
        highlightRect.anchorMin = new Vector2(0f, 0.62f);
        highlightRect.anchorMax = new Vector2(1f, 1f);
        highlightRect.offsetMin = new Vector2(8f, 0f);
        highlightRect.offsetMax = new Vector2(-8f, -7f);
        Image highlightImage = highlight.GetComponent<Image>();
        highlightImage.color = new Color(1f, 1f, 1f, 0.12f);
        highlightImage.raycastTarget = false;

        Text text = CreateText(buttonObject.transform, "Label", label, 28, FontStyle.Bold);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Shadow textShadow = text.gameObject.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        textShadow.effectDistance = new Vector2(2f, -2f);

        return button;
    }

    private static Text CreateText(Transform parent, string name, string text, int fontSize, FontStyle style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.layer = LayerMask.NameToLayer("UI");
        textObject.transform.SetParent(parent, false);

        Text label = textObject.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static void ConfigureNavigation(Button automaticButton, Button manualButton)
    {
        Navigation autoNav = automaticButton.navigation;
        autoNav.mode = Navigation.Mode.Explicit;
        autoNav.selectOnRight = manualButton;
        automaticButton.navigation = autoNav;

        Navigation manualNav = manualButton.navigation;
        manualNav.mode = Navigation.Mode.Explicit;
        manualNav.selectOnLeft = automaticButton;
        manualButton.navigation = manualNav;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void RemoveIfExists(string objectName)
    {
        GameObject existing = FindSceneObject(objectName);

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];

            if (obj != null && obj.name == objectName && obj.scene.IsValid())
            {
                return obj;
            }
        }

        return null;
    }
}

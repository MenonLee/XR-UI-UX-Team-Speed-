using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
#endif

public class LobbyLicensePrompt : MonoBehaviour
{
    public static LobbyLicensePrompt Instance { get; private set; }

    [Header("Scene UI References")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private Button automaticButton;
    [SerializeField] private Button manualButton;

    [Header("Selection")]
    [SerializeField] private string playerPrefsKey = "TransmissionMode";
    [SerializeField] private string automaticValue = "Automatic";
    [SerializeField] private string manualValue = "Manual";
    [SerializeField] private bool hideOnStart = true;

    [Header("G29 Prompt Input")]
    [SerializeField] private int circleButtonIndex = 3;
    [SerializeField] private float dpadDeadZone = 0.5f;
    [SerializeField] private float inputArmDelay = 0.2f;

    private string pendingSceneName;
    private bool initialized;
    private bool hasShown;
    private bool isShowing;
    private bool inputArmed;
    private int selectedIndex;
    private int previousDpadDirection;
    private bool previousCirclePressed;
    private float shownTime;

    public static bool IsShowing => Instance != null && Instance.isShowing;

    public static void ShowForScene(string sceneName)
    {
        LobbyLicensePrompt prompt = GetInstance();

        if (prompt == null)
        {
            Debug.LogWarning("[LobbyLicensePrompt] No LobbyLicensePrompt found in the scene. Loading scene directly.");
            SceneManager.LoadScene(sceneName);
            return;
        }

        prompt.Show(sceneName);
    }

    private static LobbyLicensePrompt GetInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LobbyLicensePrompt[] prompts = Resources.FindObjectsOfTypeAll<LobbyLicensePrompt>();

        for (int i = 0; i < prompts.Length; i++)
        {
            LobbyLicensePrompt prompt = prompts[i];

            if (prompt != null && prompt.gameObject.scene.IsValid())
            {
                Instance = prompt;
                return prompt;
            }
        }

        return null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Initialize();
    }

    private void Start()
    {
        EnsureEventSystem();

        if (hideOnStart && !hasShown)
        {
            Hide();
        }
    }

    public void Show(string sceneName)
    {
        pendingSceneName = sceneName;
        hasShown = true;
        Initialize();
        EnsureEventSystem();

        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 1f;
            promptCanvasGroup.interactable = true;
            promptCanvasGroup.blocksRaycasts = true;
        }

        if (automaticButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(automaticButton.gameObject);
        }

        selectedIndex = 0;
        previousDpadDirection = 0;
        previousCirclePressed = IsCirclePressedNow();
        shownTime = Time.unscaledTime;
        inputArmed = false;
        isShowing = true;
    }

    public void Hide()
    {
        Initialize();
        isShowing = false;
        inputArmed = false;

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.interactable = false;
            promptCanvasGroup.blocksRaycasts = false;
        }

        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
    }

    public void SelectAutomatic()
    {
        ChooseMode(automaticValue);
    }

    public void SelectManual()
    {
        ChooseMode(manualValue);
    }

    private void Update()
    {
        if (!isShowing)
        {
            return;
        }

        UpdateInputArmedState();
        HandlePromptInput();
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        if (promptRoot == null)
        {
            promptRoot = gameObject;
        }

        if (promptCanvasGroup == null)
        {
            promptCanvasGroup = promptRoot.GetComponent<CanvasGroup>();
        }

        if (promptCanvasGroup == null)
        {
            promptCanvasGroup = promptRoot.AddComponent<CanvasGroup>();
        }

        if (automaticButton != null)
        {
            automaticButton.onClick.RemoveListener(SelectAutomatic);
            automaticButton.onClick.AddListener(SelectAutomatic);
        }

        if (manualButton != null)
        {
            manualButton.onClick.RemoveListener(SelectManual);
            manualButton.onClick.AddListener(SelectManual);
        }

        ConfigureButtonNavigation();
        initialized = true;
    }

    private void ChooseMode(string mode)
    {
        isShowing = false;
        inputArmed = false;
        PlayerPrefs.SetString(playerPrefsKey, mode);
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(pendingSceneName))
        {
            SceneManager.LoadScene(pendingSceneName);
        }
    }

    private void HandlePromptInput()
    {
#if ENABLE_INPUT_SYSTEM
        bool circlePressed = IsCirclePressedNow();

        if (!inputArmed)
        {
            previousCirclePressed = circlePressed;
            return;
        }

        int dpadDirection = ReadDpadDirection();

        if (dpadDirection != 0)
        {
            SelectPromptButton(dpadDirection > 0 ? 1 : 0);
        }

        if (circlePressed && !previousCirclePressed)
        {
            SubmitSelectedPromptButton();
        }

        previousCirclePressed = circlePressed;
#endif
    }

    private void UpdateInputArmedState()
    {
#if ENABLE_INPUT_SYSTEM
        if (inputArmed)
        {
            return;
        }

        bool delayPassed = Time.unscaledTime - shownTime >= inputArmDelay;
        bool submitReleased = !IsCirclePressedNow();

        if (delayPassed && submitReleased)
        {
            inputArmed = true;
            previousCirclePressed = false;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private int ReadDpadDirection()
    {
        Vector2 value = Vector2.zero;
        Joystick joystick = Joystick.current;

        if (joystick != null)
        {
            Vector2Control dpad = joystick.TryGetChildControl<Vector2Control>("dpad")
                ?? joystick.TryGetChildControl<Vector2Control>("hat");

            if (dpad != null)
            {
                value = dpad.ReadValue();
            }
        }

        if (value == Vector2.zero && Gamepad.current != null && Gamepad.current.dpad != null)
        {
            value = Gamepad.current.dpad.ReadValue();
        }

        int currentDirection = Mathf.Abs(value.x) >= dpadDeadZone ? (value.x > 0f ? 1 : -1) : 0;
        int pressedDirection = currentDirection != 0 && previousDpadDirection == 0 ? currentDirection : 0;
        previousDpadDirection = currentDirection;
        return pressedDirection;
    }

    private bool IsCirclePressedNow()
    {
        Joystick joystick = Joystick.current;

        if (joystick != null && circleButtonIndex >= 0)
        {
            ButtonControl button = joystick.TryGetChildControl<ButtonControl>($"button{circleButtonIndex}");

            if (button != null && button.isPressed)
            {
                return true;
            }
        }

        return Gamepad.current != null
            && Gamepad.current.buttonEast != null
            && Gamepad.current.buttonEast.isPressed;
    }
#endif

    private void SelectPromptButton(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, 1);
        Button selectedButton = selectedIndex == 0 ? automaticButton : manualButton;

        if (selectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(selectedButton.gameObject);
        }
    }

    private void SubmitSelectedPromptButton()
    {
        Button selectedButton = selectedIndex == 0 ? automaticButton : manualButton;

        if (selectedButton != null && selectedButton.IsActive() && selectedButton.interactable)
        {
            selectedButton.onClick.Invoke();
        }
    }

    private void ConfigureButtonNavigation()
    {
        if (automaticButton == null || manualButton == null)
        {
            return;
        }

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
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
        DontDestroyOnLoad(eventSystem);
    }
}

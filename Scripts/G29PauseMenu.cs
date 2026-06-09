using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class G29PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private int squareButtonIndex = 1;
    [SerializeField] private int circleButtonIndex = 2;
    [SerializeField] private int dpadUpButtonIndex = -1;
    [SerializeField] private int dpadDownButtonIndex = -1;
    [SerializeField] private int dpadLeftButtonIndex = -1;
    [SerializeField] private int dpadRightButtonIndex = -1;
    [SerializeField] private string lobbySceneName = "";
    [SerializeField] private bool pauseAudio = true;

    private RaceSessionManager raceSessionManager;
    private Button[] pauseButtons = new Button[0];
    private Image[] pauseButtonImages = new Image[0];
    private Color[] pauseButtonBaseColors = new Color[0];
    private int selectedButtonIndex;
    private Vector2 previousDpadDirection;
    private bool previousCirclePressed;
    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        raceSessionManager = FindObjectOfType<RaceSessionManager>();
        SetPaused(false);
    }

    private void Update()
    {
        if (IsRaceFinished())
        {
            if (isPaused)
            {
                SetPaused(false);
            }

            return;
        }

        if (WasPauseButtonPressed())
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void RestartCurrentScene()
    {
        SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToLobby()
    {
        SetPaused(false);

        if (!string.IsNullOrWhiteSpace(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            Debug.LogWarning("Lobby scene is not set, and there are no scenes in Build Settings.");
        }
    }

    public void QuitGame()
    {
        SetPaused(false);

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetPauseMenuRoot(GameObject root)
    {
        pauseMenuRoot = root;
        SetPaused(isPaused);
    }

    public void SetPauseMenuButtons(Button[] buttons)
    {
        pauseButtons = buttons ?? new Button[0];
        pauseButtonImages = new Image[pauseButtons.Length];
        pauseButtonBaseColors = new Color[pauseButtons.Length];

        for (int i = 0; i < pauseButtons.Length; i++)
        {
            if (pauseButtons[i] == null)
            {
                continue;
            }

            pauseButtonImages[i] = pauseButtons[i].GetComponent<Image>();
            pauseButtonBaseColors[i] = pauseButtonImages[i] != null ? pauseButtonImages[i].color : Color.white;
        }

        selectedButtonIndex = Mathf.Clamp(selectedButtonIndex, 0, Mathf.Max(0, pauseButtons.Length - 1));
        RefreshSelectedButton();
    }

    private bool WasPauseButtonPressed()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return true;
        }

        Joystick joystick = Joystick.current;

        if (joystick == null)
        {
            return false;
        }

        ButtonControl button = joystick.TryGetChildControl<ButtonControl>($"button{squareButtonIndex}");
        return button != null && button.wasPressedThisFrame;
    }

    private bool IsRaceFinished()
    {
        if (raceSessionManager == null)
        {
            raceSessionManager = FindObjectOfType<RaceSessionManager>();
        }

        return raceSessionManager != null && raceSessionManager.RaceFinished;
    }

    private void HandlePauseMenuInput()
    {
        Vector2 dpadDirection = ReadDpadDirection();

        if (dpadDirection != Vector2.zero && dpadDirection != previousDpadDirection)
        {
            if (Mathf.Abs(dpadDirection.y) >= Mathf.Abs(dpadDirection.x))
            {
                SelectRelative(dpadDirection.y > 0f ? -1 : 1);
            }
            else
            {
                SelectRelative(dpadDirection.x > 0f ? 1 : -1);
            }
        }

        previousDpadDirection = dpadDirection;

        bool circlePressed = IsCirclePressed();

        if (circlePressed && !previousCirclePressed)
        {
            SubmitSelectedButton();
        }

        previousCirclePressed = circlePressed;
    }

    private Vector2 ReadDpadDirection()
    {
        Vector2 direction = Vector2.zero;
        Joystick joystick = Joystick.current;

        if (joystick != null)
        {
            Vector2Control dpad = joystick.TryGetChildControl<Vector2Control>("dpad")
                ?? joystick.TryGetChildControl<Vector2Control>("hat");

            if (dpad != null)
            {
                direction = dpad.ReadValue();
            }

            direction += ReadButtonDpadFallback(joystick);
        }

        if (direction == Vector2.zero && Gamepad.current != null && Gamepad.current.dpad != null)
        {
            direction = Gamepad.current.dpad.ReadValue();
        }

        return new Vector2(
            Mathf.Abs(direction.x) > 0.5f ? Mathf.Sign(direction.x) : 0f,
            Mathf.Abs(direction.y) > 0.5f ? Mathf.Sign(direction.y) : 0f);
    }

    private Vector2 ReadButtonDpadFallback(Joystick joystick)
    {
        Vector2 direction = Vector2.zero;

        if (IsJoystickButtonPressed(joystick, dpadUpButtonIndex))
        {
            direction.y += 1f;
        }

        if (IsJoystickButtonPressed(joystick, dpadDownButtonIndex))
        {
            direction.y -= 1f;
        }

        if (IsJoystickButtonPressed(joystick, dpadLeftButtonIndex))
        {
            direction.x -= 1f;
        }

        if (IsJoystickButtonPressed(joystick, dpadRightButtonIndex))
        {
            direction.x += 1f;
        }

        return direction;
    }

    private bool IsCirclePressed()
    {
        Joystick joystick = Joystick.current;

        if (joystick != null && IsJoystickButtonPressed(joystick, circleButtonIndex))
        {
            return true;
        }

        return Gamepad.current != null
            && Gamepad.current.buttonEast != null
            && Gamepad.current.buttonEast.isPressed;
    }

    private static bool IsJoystickButtonPressed(Joystick joystick, int buttonIndex)
    {
        if (joystick == null || buttonIndex < 0)
        {
            return false;
        }

        ButtonControl button = joystick.TryGetChildControl<ButtonControl>($"button{buttonIndex}");
        return button != null && button.isPressed;
    }

    private void SelectRelative(int offset)
    {
        if (pauseButtons.Length == 0)
        {
            return;
        }

        selectedButtonIndex = (selectedButtonIndex + offset + pauseButtons.Length) % pauseButtons.Length;
        RefreshSelectedButton();
    }

    private void SubmitSelectedButton()
    {
        if (pauseButtons.Length == 0)
        {
            return;
        }

        Button selectedButton = pauseButtons[selectedButtonIndex];

        if (selectedButton == null || !selectedButton.IsActive() || !selectedButton.interactable)
        {
            return;
        }

        selectedButton.onClick.Invoke();
    }

    private void RefreshSelectedButton()
    {
        for (int i = 0; i < pauseButtonImages.Length; i++)
        {
            if (pauseButtonImages[i] == null)
            {
                continue;
            }

            pauseButtonImages[i].color = i == selectedButtonIndex
                ? Color.Lerp(pauseButtonBaseColors[i], Color.white, 0.28f)
                : pauseButtonBaseColors[i];
        }

        if (isPaused && pauseButtons.Length > 0 && pauseButtons[selectedButtonIndex] != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(pauseButtons[selectedButtonIndex].gameObject);
        }
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(paused);
        }

        Time.timeScale = paused || ShouldKeepGameFrozenAfterResume() ? 0f : 1f;

        if (pauseAudio)
        {
            AudioListener.pause = paused;
        }

        previousDpadDirection = Vector2.zero;
        previousCirclePressed = false;

        if (paused)
        {
            selectedButtonIndex = 0;
            RefreshSelectedButton();
        }
    }

    private void LateUpdate()
    {
        if (isPaused)
        {
            HandlePauseMenuInput();
        }
    }

    private bool ShouldKeepGameFrozenAfterResume()
    {
        if (isPaused)
        {
            return true;
        }

        RaceCountdown countdown = FindObjectOfType<RaceCountdown>();
        return countdown != null && countdown.IsRunning && countdown.FreezesGameDuringCountdown;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;

        if (pauseAudio)
        {
            AudioListener.pause = false;
        }
    }
}

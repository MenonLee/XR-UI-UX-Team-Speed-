using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RaceCountdown : MonoBehaviour
{
    [SerializeField] private int startNumber = 5;
    [SerializeField] private float secondsPerNumber = 1f;
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private bool freezeGameDuringCountdown = true;
    [SerializeField] private bool useWorldSpaceCanvas = true;
    [SerializeField] private float vrCountdownDistance = 0.8f;
    [SerializeField] private float vrCountdownScale = 0.001f;
    [SerializeField] private string[] onlyRunInSceneNames = { };
    [SerializeField] private string[] excludedSceneNames = { };
    [SerializeField] private UnityEvent onCountdownFinished;

    private GameObject countdownRoot;
    private Text countdownText;
    private Coroutine countdownRoutine;
    private G29PauseMenu pauseMenu;
    private bool hasFinished;
    private static Material noZTestMaterial;

    public event Action CountdownFinished;
    public bool IsRunning => countdownRoutine != null;
    public bool HasFinished => hasFinished;
    public bool FreezesGameDuringCountdown => freezeGameDuringCountdown;

    private void Start()
    {
        if (startAutomatically && ShouldRunInCurrentScene())
        {
            StartCountdown();
        }
    }

    public void StartCountdown()
    {
        if (!ShouldRunInCurrentScene())
        {
            return;
        }

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
        }

        hasFinished = false;
        pauseMenu = FindObjectOfType<G29PauseMenu>();
        BuildUiIfNeeded();
        countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    private bool ShouldRunInCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        for (int i = 0; i < excludedSceneNames.Length; i++)
        {
            if (excludedSceneNames[i] == currentSceneName)
            {
                return false;
            }
        }

        if (onlyRunInSceneNames.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < onlyRunInSceneNames.Length; i++)
        {
            if (onlyRunInSceneNames[i] == currentSceneName)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator CountdownRoutine()
    {
        float previousTimeScale = Time.timeScale;

        if (freezeGameDuringCountdown)
        {
            Time.timeScale = 0f;
        }

        countdownRoot.SetActive(true);

        for (int current = startNumber; current >= 1; current--)
        {
            countdownText.text = current.ToString();

            float remaining = secondsPerNumber;

            while (remaining > 0f)
            {
                if (!IsPauseMenuPaused())
                {
                    remaining -= Time.unscaledDeltaTime;
                }

                yield return null;
            }
        }

        countdownRoot.SetActive(false);

        if (freezeGameDuringCountdown)
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }

        CountdownFinished?.Invoke();
        onCountdownFinished?.Invoke();
        hasFinished = true;
        countdownRoutine = null;
    }

    private bool IsPauseMenuPaused()
    {
        if (pauseMenu == null)
        {
            pauseMenu = FindObjectOfType<G29PauseMenu>();
        }

        return pauseMenu != null && pauseMenu.IsPaused;
    }

    private void BuildUiIfNeeded()
    {
        if (countdownRoot != null)
        {
            return;
        }

        countdownRoot = new GameObject("RaceCountdownCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        countdownRoot.transform.SetParent(transform, false);

        Canvas canvas = countdownRoot.GetComponent<Canvas>();
        canvas.sortingOrder = 90;

        if (useWorldSpaceCanvas)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            countdownRoot.AddComponent<VRCameraCanvasPlacement>()
                .Configure(Camera.main != null ? Camera.main.transform : null, vrCountdownDistance, Vector2.zero, vrCountdownScale, false);
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        CanvasScaler scaler = countdownRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = useWorldSpaceCanvas ? CanvasScaler.ScaleMode.ConstantPixelSize : CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObject = new GameObject("CountdownText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(countdownRoot.transform, false);

        countdownText = textObject.GetComponent<Text>();
        countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdownText.fontSize = 180;
        countdownText.fontStyle = FontStyle.Bold;
        countdownText.alignment = TextAnchor.MiddleCenter;
        countdownText.color = Color.white;
        ApplyNoZTestMaterial(countdownText);
        countdownText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.75f);

        RectTransform rect = countdownText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(360f, 240f);
        rect.anchoredPosition = Vector2.zero;

        countdownRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (freezeGameDuringCountdown && countdownRoutine != null)
        {
            Time.timeScale = 1f;
        }
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
}

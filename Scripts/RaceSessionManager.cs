using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RaceSessionManager : MonoBehaviour
{
    [Serializable]
    private class OpponentResult
    {
        public string driverName = "AI Driver";
        public float finishTime = 95f;
    }

    private struct ResultEntry
    {
        public string Name;
        public float Time;
        public bool IsPlayer;
    }

    private class CarProgress
    {
        public Transform Car;
        public int CompletedLaps;
        public bool IgnoredFirstPass;
        public bool Finished;
        public float FinishTime;
        public string Name;
        public bool IsPlayer;
        public int LastCheckpoint;
        public int NextCheckpoint;
        public int TotalCheckpoints;
    }

    private enum TutorialTipId
    {
        None,
        Accelerate,
        Brake,
        LapPrompt,
        Upshift,
        GearSuccess,
        HighGearLowSpeed,
        OffRoadReset,
        Pause,
        FreeDrive
    }

    private struct TutorialTipMessage
    {
        public TutorialTipId Id;
        public string Message;
        public float Duration;
    }

    [Header("Race")]
    [SerializeField] private int totalLaps = 3;
    [SerializeField] private bool waitForCountdown = true;
    [SerializeField] private bool freezeGameWhenFinished = true;
    [SerializeField] private bool pauseAudioWhenFinished = true;
    [SerializeField] private bool keepBgmPlayingWhenFinished = true;
    [SerializeField] private string playerName = "Player";
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool ignoreFirstFinishLinePass = true;
    [SerializeField] private bool enableDebugFinishShortcut = true;

    [Header("Tutorial")]
    [SerializeField] private bool tutorialMode;
    [SerializeField] private string tutorialSceneName = "MainTrack";
    [SerializeField] private int tutorialTotalLaps = 2;
    [SerializeField] private float tutorialAccelerateInput = 0.2f;
    [SerializeField] private float tutorialBrakeInput = 0.25f;
    [SerializeField] private float tutorialBrakeDistance = 20f;
    [SerializeField] private float tutorialStoppedSpeedKmh = 5f;
    [SerializeField] private float tutorialPauseTipDelay = 3f;
    [SerializeField] private float tutorialFreeDriveTipDelay = 4f;
    [SerializeField] private float tutorialLowGearTipHoldTime = 1.4f;
    [SerializeField] private float tutorialOffRoadTipHoldTime = 1.4f;
    [SerializeField] private float tutorialTipMinimumReadTime = 1f;

    [Header("Engine Audio Master")]
    [SerializeField, Range(0f, 2f)] private float playerEngineMasterVolume = 1f;
    [SerializeField, Range(0f, 2f)] private float opponentEngineMasterVolume = 1f;
    [SerializeField] private bool applyEngineMasterInPlayMode = true;

    [Header("Position")]
    [SerializeField] private Transform playerCar;
    [SerializeField] private Transform finishLine;
    [SerializeField] private Transform[] opponentCars = { };
    [SerializeField] private string rankingPathName = "Path_1";
    [SerializeField] private ALIyerEdon.Checkpoint_Manager checkpointManager;
    [SerializeField] private float positionChangeMargin = 0.0025f;

    [Header("VR UI")]
    [SerializeField] private bool useWorldSpaceCanvas = true;
    [SerializeField] private float vrHudDistance = 0.8f;
    [SerializeField] private float vrMenuDistance = 0.75f;
    [SerializeField] private float vrCanvasScale = 0.001f;

    [Header("Minimap")]
    [SerializeField] private bool buildRuntimeMinimap = true;
    [SerializeField] private float minimapCameraHeight = 260f;
    [SerializeField] private float minimapOrthographicSize = 115f;
    [SerializeField] private int minimapTextureSize = 512;

    [Header("G29 Result Menu")]
    [SerializeField] private int circleButtonIndex = 3;
    [SerializeField] private int dpadUpButtonIndex = -1;
    [SerializeField] private int dpadDownButtonIndex = -1;
    [SerializeField] private int dpadLeftButtonIndex = -1;
    [SerializeField] private int dpadRightButtonIndex = -1;

    [Header("Fallback Ranking")]
    [SerializeField] private OpponentResult[] opponentResults = { };

    private GameObject hudRoot;
    private Text lapText;
    private Text timerText;
    private Text modeText;
    private Text lapTimesText;
    private Text positionText;
    private Text speedText;
    private Text gearText;
    private GameObject tutorialTipRoot;
    private Text tutorialTipText;
    private RaceMinimapController minimapController;
    private GameObject resultsRoot;
    private RectTransform resultsPanelRect;
    private Text resultsText;
    private LayoutElement resultsTextLayout;
    private Button[] resultButtons = new Button[0];
    private Image[] resultButtonImages = new Image[0];
    private Color[] resultButtonBaseColors = new Color[0];
    private static Material noZTestMaterial;

    private int currentLap = 1;
    private readonly List<float> completedLapTimes = new List<float>();
    private readonly Dictionary<Transform, CarProgress> carProgress = new Dictionary<Transform, CarProgress>();
    private readonly List<Transform> rankingWaypoints = new List<Transform>();
    private readonly Dictionary<string, List<Transform>> rankingPathCache = new Dictionary<string, List<Transform>>();
    private int selectedResultButtonIndex;
    private float raceStartTime;
    private float currentLapStartTime;
    private float finalTime;
    private bool raceStarted;
    private bool raceFinished;
    private bool hasIgnoredFirstFinishLinePass;
    private Vector2 previousDpadDirection;
    private bool previousCirclePressed;
    private bool tutorialActive;
    private bool tutorialTipsDisabled;
    private bool tutorialAwaitingAcceleration;
    private bool hasShownAccelerateTip;
    private bool hasShownBrakeTip;
    private bool hasShownLapPromptTip;
    private bool hasShownUpshiftTip;
    private bool hasShownGearSuccessTip;
    private bool hasShownHighGearLowSpeedTip;
    private bool hasShownOffRoadResetTip;
    private bool hasShownPauseTip;
    private bool hasShownFreeDriveTip;
    private bool tutorialAccelerateCompleted;
    private bool tutorialBrakeCompleted;
    private bool tutorialLapPromptCompleted;
    private bool tutorialPauseCompleted;
    private TutorialTipId activeTutorialTip;
    private readonly Queue<TutorialTipMessage> pendingTutorialTips = new Queue<TutorialTipMessage>();
    private float activeTutorialTipShownTime;
    private int tutorialLastObservedGear = 1;
    private float tutorialLowGearTimer;
    private float tutorialOffRoadTimer;
    private float tutorialFirstLapTime = -1f;
    private float tutorialTipHideTime = -1f;
    private float tutorialRaceTimeOffset;
    private float tutorialPauseCompletedTime = -1f;
    private Vector3 tutorialStartPosition;
    private float lastAppliedPlayerEngineMasterVolume = -1f;
    private float lastAppliedOpponentEngineMasterVolume = -1f;
    private CarController playerCarController;
    private G29PauseMenu pauseMenu;

    public int CurrentLap => currentLap;
    public int TotalLaps => totalLaps;
    public float FinalTime => finalTime;
    public bool RaceStarted => raceStarted;
    public bool RaceFinished => raceFinished;
    public event Action RaceBegan;
    public event Action RaceEnded;

    private void Awake()
    {
        ConfigureTutorialModeForScene();
        AutoAssignReferences();
        SetupCheckpointTriggers();
    }

    private void Start()
    {
        BuildUiIfNeeded();
        ApplyEngineMasterVolumes(true);
        SetPlayerControlsEnabled(false);
        UpdateHud();

        RaceCountdown countdown = FindObjectOfType<RaceCountdown>();

        if (waitForCountdown && countdown != null)
        {
            countdown.CountdownFinished += BeginRace;

            if (countdown.HasFinished)
            {
                BeginRace();
            }

            return;
        }

        BeginRace();
    }

    private void Update()
    {
        if (applyEngineMasterInPlayMode)
        {
            ApplyEngineMasterVolumes(false);
        }

        if (enableDebugFinishShortcut
            && !raceFinished
            && Keyboard.current != null
            && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            if (!raceStarted)
            {
                BeginRace();
            }

            FinishRace();
            return;
        }

        if (raceStarted && !raceFinished)
        {
            UpdateHud();
        }

        if (raceFinished)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                RetryRace();
                return;
            }

            HandleResultMenuInput();
        }

        UpdateTutorial();
    }

    private void OnDestroy()
    {
        ResumeAudioIfPaused();
    }

    public void BeginRace()
    {
        if (raceStarted)
        {
            return;
        }

        ConfigureTutorialModeForScene();
        currentLap = 1;
        completedLapTimes.Clear();
        carProgress.Clear();
        finalTime = 0f;
        raceStarted = true;
        raceFinished = false;
        hasIgnoredFirstFinishLinePass = false;
        raceStartTime = Time.realtimeSinceStartup;
        currentLapStartTime = raceStartTime;
        SetPlayerControlsEnabled(true);
        RaceBegan?.Invoke();
        RegisterKnownCarsForProgress();
        StartTutorialIfNeeded();

        if (hudRoot != null)
        {
            hudRoot.SetActive(true);
        }

        if (resultsRoot != null)
        {
            resultsRoot.SetActive(false);
        }

        UpdateHud();
    }

    public void RegisterFinishLinePass()
    {
        if (playerCar == null)
        {
            AutoAssignReferences();
        }

        RegisterFinishLinePass(playerCar);
    }

    public void RegisterFinishLinePass(Transform car)
    {
        if (car == null)
        {
            return;
        }

        if (checkpointManager != null)
        {
            return;
        }

        bool isPlayer = IsPlayerCar(car);

        if (!isPlayer)
        {
            RegisterOpponentFinishLinePass(car);
            return;
        }

        if (!raceStarted || raceFinished)
        {
            return;
        }

        if (ignoreFirstFinishLinePass && !hasIgnoredFirstFinishLinePass)
        {
            hasIgnoredFirstFinishLinePass = true;
            CarProgress playerProgress = GetOrCreateProgress(playerCar, true);

            if (playerProgress != null)
            {
                playerProgress.IgnoredFirstPass = true;
            }

            currentLapStartTime = Time.realtimeSinceStartup;
            return;
        }

        float completedLapTime = Time.realtimeSinceStartup - currentLapStartTime;
        completedLapTimes.Add(completedLapTime);

        if (currentLap >= totalLaps)
        {
            FinishRace();
            return;
        }

        OnTutorialPlayerLapCompleted(currentLap);
        currentLap++;
        currentLapStartTime = Time.realtimeSinceStartup;
        UpdateHud();
    }

    public void RegisterCheckpointPass(Transform car, int checkpointIndex, int totalCheckpoints)
    {
        if (!raceStarted || raceFinished || car == null || totalCheckpoints <= 0)
        {
            return;
        }

        bool isPlayer = IsPlayerCar(car);
        CarProgress progress = GetOrCreateProgress(car, isPlayer);

        if (progress == null || progress.Finished)
        {
            return;
        }

        if (progress.TotalCheckpoints <= 0)
        {
            progress.TotalCheckpoints = totalCheckpoints;
        }

        if (!IsAcceptableCheckpoint(progress, checkpointIndex, totalCheckpoints))
        {
            return;
        }

        progress.LastCheckpoint = checkpointIndex;
        progress.NextCheckpoint = (checkpointIndex + 1) % totalCheckpoints;

        if (checkpointIndex != 0)
        {
            UpdateHud();
            return;
        }

        if (ignoreFirstFinishLinePass && !progress.IgnoredFirstPass)
        {
            progress.IgnoredFirstPass = true;

            if (isPlayer)
            {
                hasIgnoredFirstFinishLinePass = true;
                currentLapStartTime = Time.realtimeSinceStartup;
            }

            UpdateHud();
            return;
        }

        progress.CompletedLaps++;

        if (isPlayer)
        {
            float completedLapTime = Time.realtimeSinceStartup - currentLapStartTime;
            completedLapTimes.Add(completedLapTime);

            if (progress.CompletedLaps >= totalLaps)
            {
                FinishRace();
                return;
            }

            OnTutorialPlayerLapCompleted(progress.CompletedLaps);
            currentLap = Mathf.Clamp(progress.CompletedLaps + 1, 1, totalLaps);
            currentLapStartTime = Time.realtimeSinceStartup;
        }
        else if (progress.CompletedLaps >= totalLaps)
        {
            progress.Finished = true;
            progress.FinishTime = Time.realtimeSinceStartup - raceStartTime;
        }

        UpdateHud();
    }

    private bool IsAcceptableCheckpoint(CarProgress progress, int checkpointIndex, int totalCheckpoints)
    {
        if (checkpointIndex == progress.NextCheckpoint)
        {
            return true;
        }

        if (progress.LastCheckpoint < 0)
        {
            return true;
        }

        int forwardDelta = (checkpointIndex - progress.LastCheckpoint + totalCheckpoints) % totalCheckpoints;
        return forwardDelta > 0 && forwardDelta <= 3;
    }

    private void RegisterOpponentFinishLinePass(Transform car)
    {
        if (!raceStarted || raceFinished)
        {
            return;
        }

        CarProgress progress = GetOrCreateProgress(car, false);

        if (progress.Finished)
        {
            return;
        }

        if (ignoreFirstFinishLinePass && !progress.IgnoredFirstPass)
        {
            progress.IgnoredFirstPass = true;
            return;
        }

        progress.CompletedLaps++;

        if (progress.CompletedLaps >= totalLaps)
        {
            progress.Finished = true;
            progress.FinishTime = Time.realtimeSinceStartup - raceStartTime;
        }

        UpdateHud();
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        ResumeAudioIfPaused();

        if (!string.IsNullOrWhiteSpace(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
        }
    }

    public void RetryRace()
    {
        Time.timeScale = 1f;
        ResumeAudioIfPaused();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        ResumeAudioIfPaused();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetFinishLine(Transform target)
    {
        finishLine = target;
    }

    public void SetOpponentCars(Transform[] targets)
    {
        opponentCars = targets ?? Array.Empty<Transform>();
        RegisterKnownCarsForProgress();
        ApplyEngineMasterVolumes(true);
        UpdateHud();
    }

    public bool TryGetRespawnPose(Transform car, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (car == null)
        {
            return false;
        }

        if (checkpointManager == null)
        {
            checkpointManager = FindObjectOfType<ALIyerEdon.Checkpoint_Manager>();
        }

        if (checkpointManager == null || checkpointManager.checkpoints == null || checkpointManager.checkpoints.Count == 0)
        {
            return false;
        }

        CarProgress progress = GetOrCreateProgress(car, IsPlayerCar(car));
        int index = progress != null && progress.LastCheckpoint >= 0
            ? progress.LastCheckpoint
            : FindNearestCheckpointIndex(car);

        index = Mathf.Clamp(index, 0, checkpointManager.checkpoints.Count - 1);
        int next = (index + 1) % checkpointManager.checkpoints.Count;

        Transform currentCheckpoint = checkpointManager.checkpoints[index];
        Transform nextCheckpoint = checkpointManager.checkpoints[next];

        if (currentCheckpoint == null)
        {
            return false;
        }

        position = currentCheckpoint.position;

        if (nextCheckpoint == null)
        {
            rotation = currentCheckpoint.rotation;
            return true;
        }

        Vector3 direction = nextCheckpoint.position - currentCheckpoint.position;
        direction.y = 0f;

        rotation = direction.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : currentCheckpoint.rotation;

        return true;
    }

    private void FinishRace()
    {
        raceFinished = true;
        finalTime = Time.realtimeSinceStartup - raceStartTime;
        SetPlayerControlsEnabled(false);
        RaceEnded?.Invoke();
        UpdateHud();

        if (tutorialMode)
        {
            ShowTutorialComplete();
        }
        else
        {
            ShowResults();
        }

        if (freezeGameWhenFinished)
        {
            Time.timeScale = 0f;
        }

        if (pauseAudioWhenFinished)
        {
            if (keepBgmPlayingWhenFinished)
            {
                StopEngineAudio();
            }
            else
            {
                AudioListener.pause = true;
            }
        }
    }

    private void ResumeAudioIfPaused()
    {
        AudioListener.pause = false;
    }

    private void StopEngineAudio()
    {
        CarEngineSound[] engineSounds = FindObjectsOfType<CarEngineSound>(true);

        for (int i = 0; i < engineSounds.Length; i++)
        {
            if (engineSounds[i] != null)
            {
                engineSounds[i].StopEngineAudio();
            }
        }
    }

    private void AutoAssignReferences()
    {
        if (playerCar == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);

            if (player != null)
            {
                playerCar = player.transform;
                playerCarController = playerCar.GetComponent<CarController>()
                    ?? playerCar.GetComponentInChildren<CarController>(true);
            }
            else
            {
                GameObject namedPlayer = GameObject.Find("PlayerCar");
                CarController controller = namedPlayer != null
                    ? namedPlayer.GetComponent<CarController>() ?? namedPlayer.GetComponentInChildren<CarController>(true)
                    : FindObjectOfType<CarController>();

                playerCar = controller != null ? controller.transform : null;
                playerCarController = controller;
            }
        }

        if (finishLine == null)
        {
            FinishLineTrigger trigger = FindObjectOfType<FinishLineTrigger>();
            finishLine = trigger != null ? trigger.transform : null;
        }

        if (checkpointManager == null)
        {
            checkpointManager = FindObjectOfType<ALIyerEdon.Checkpoint_Manager>();
        }

        LoadRankingWaypoints();
    }

    private void ApplyEngineMasterVolumes(bool force)
    {
        playerEngineMasterVolume = Mathf.Clamp(playerEngineMasterVolume, 0f, 2f);
        opponentEngineMasterVolume = Mathf.Clamp(opponentEngineMasterVolume, 0f, 2f);

        bool playerChanged = force || !Mathf.Approximately(playerEngineMasterVolume, lastAppliedPlayerEngineMasterVolume);
        bool opponentChanged = force || !Mathf.Approximately(opponentEngineMasterVolume, lastAppliedOpponentEngineMasterVolume);

        if (!playerChanged && !opponentChanged)
        {
            return;
        }

        if (playerChanged)
        {
            ApplyEngineMasterVolume(playerCar, playerEngineMasterVolume);
            lastAppliedPlayerEngineMasterVolume = playerEngineMasterVolume;
        }

        if (opponentChanged)
        {
            if (opponentCars != null)
            {
                for (int i = 0; i < opponentCars.Length; i++)
                {
                    ApplyEngineMasterVolume(opponentCars[i], opponentEngineMasterVolume);
                }
            }

            lastAppliedOpponentEngineMasterVolume = opponentEngineMasterVolume;
        }
    }

    private static void ApplyEngineMasterVolume(Transform car, float volume)
    {
        if (car == null)
        {
            return;
        }

        CarEngineSound[] engineSounds = car.GetComponentsInChildren<CarEngineSound>(true);

        for (int i = 0; i < engineSounds.Length; i++)
        {
            if (engineSounds[i] != null)
            {
                engineSounds[i].SetMasterVolume(volume);
            }
        }
    }

    private void ConfigureTutorialModeForScene()
    {
        if (SceneManager.GetActiveScene().name == tutorialSceneName)
        {
            tutorialMode = true;
            totalLaps = tutorialTotalLaps;
        }
    }

    private void StartTutorialIfNeeded()
    {
        if (!tutorialMode)
        {
            return;
        }

        tutorialActive = true;
        tutorialTipsDisabled = false;
        tutorialAwaitingAcceleration = true;
        tutorialAccelerateCompleted = false;
        tutorialBrakeCompleted = false;
        tutorialLapPromptCompleted = false;
        tutorialPauseCompleted = false;
        activeTutorialTip = TutorialTipId.None;
        activeTutorialTipShownTime = 0f;
        pendingTutorialTips.Clear();
        tutorialStartPosition = playerCar != null ? playerCar.position : Vector3.zero;
        tutorialLastObservedGear = 1;
        tutorialLowGearTimer = 0f;
        tutorialOffRoadTimer = 0f;
        tutorialFirstLapTime = -1f;
        tutorialTipHideTime = -1f;
        tutorialRaceTimeOffset = 0f;
        tutorialPauseCompletedTime = -1f;
        pauseMenu = FindObjectOfType<G29PauseMenu>();
        QueueTutorialTipOnce(ref hasShownAccelerateTip, TutorialTipId.Accelerate, "출발합니다!\n엑셀을 밟아 앞으로 전진하세요");
    }

    private void UpdateTutorial()
    {
        UpdateTutorialTipAutoHide();

        if (!tutorialMode || !tutorialActive || tutorialTipsDisabled || raceFinished)
        {
            return;
        }

        CarController controller = GetPlayerCarController();

        if (controller == null)
        {
            return;
        }

        UpdateTutorialOffRoadTip(controller);

        if (activeTutorialTip == TutorialTipId.Accelerate
            && CanCompleteActiveTutorialTip()
            && tutorialAwaitingAcceleration
            && controller.AccelInput >= tutorialAccelerateInput)
        {
            tutorialAwaitingAcceleration = false;
            tutorialAccelerateCompleted = true;
            float waitDuration = Mathf.Max(0f, Time.realtimeSinceStartup - raceStartTime);
            raceStartTime += waitDuration;
            currentLapStartTime += waitDuration;
            tutorialRaceTimeOffset += waitDuration;
            CompleteActiveTutorialTip();
        }

        if (!hasShownBrakeTip && tutorialAccelerateCompleted)
        {
            Vector3 flatStart = tutorialStartPosition;
            Vector3 flatCurrent = playerCar != null ? playerCar.position : flatStart;
            flatStart.y = 0f;
            flatCurrent.y = 0f;

            if (Vector3.Distance(flatStart, flatCurrent) >= tutorialBrakeDistance)
            {
                QueueTutorialTipOnce(ref hasShownBrakeTip, TutorialTipId.Brake, "좋아요.\n이제 브레이크를 밟아 멈춰보세요");
            }
        }

        if (activeTutorialTip == TutorialTipId.Brake
            && CanCompleteActiveTutorialTip()
            && !tutorialBrakeCompleted
            && controller.BrakeInput >= tutorialBrakeInput
            && controller.CurrentSpeedKmh <= tutorialStoppedSpeedKmh)
        {
            tutorialBrakeCompleted = true;
            CompleteActiveTutorialTip();
            hasShownLapPromptTip = true;
            tutorialLapPromptCompleted = true;
        }

        if (tutorialLapPromptCompleted)
        {
            UpdateTutorialManualTips(controller);
        }

        UpdateTutorialAfterFirstLapTips();
    }

    private void UpdateTutorialManualTips(CarController controller)
    {
        CarTransmissionController transmission = controller.Transmission;

        if (transmission == null || !transmission.IsManual)
        {
            return;
        }

        int gear = transmission.CurrentGear;
        float speed = controller.CurrentSpeedKmh;

        if (!hasShownUpshiftTip && gear == 1 && speed >= transmission.GetGearMaxSpeedKmh(1) * 0.85f)
        {
            QueueTutorialTipOnce(ref hasShownUpshiftTip, TutorialTipId.Upshift, "클러치를 밟고 기어 업 버튼을 눌러\n기어를 올려보세요");
        }

        if (activeTutorialTip == TutorialTipId.Upshift
            && CanCompleteActiveTutorialTip()
            && !hasShownGearSuccessTip
            && tutorialLastObservedGear < 2
            && gear >= 2)
        {
            CompleteActiveTutorialTip();
            QueueTutorialTipOnce(ref hasShownGearSuccessTip, TutorialTipId.GearSuccess, "좋아요.\n기어가 올라가면 더 높은 속도까지 가속할 수 있습니다.", 5f);
        }

        tutorialLastObservedGear = gear;

        if (hasShownHighGearLowSpeedTip || gear < 3 || controller.AccelInput < 0.5f)
        {
            tutorialLowGearTimer = 0f;
            return;
        }

        float powerStartSpeed = transmission.GetGearPowerStartSpeedKmh(gear);

        if (speed < powerStartSpeed)
        {
            tutorialLowGearTimer += Time.unscaledDeltaTime;

            if (tutorialLowGearTimer >= tutorialLowGearTipHoldTime)
            {
                QueueTutorialTipOnce(ref hasShownHighGearLowSpeedTip, TutorialTipId.HighGearLowSpeed, "낮은 속도에서 높은 기어를 사용하면\n가속에 제한이 생깁니다.", 5f);
            }
        }
        else
        {
            tutorialLowGearTimer = 0f;
        }
    }

    private void UpdateTutorialOffRoadTip(CarController controller)
    {
        if (hasShownOffRoadResetTip)
        {
            return;
        }

        if (controller.IsCurrentlyOffRoad())
        {
            tutorialOffRoadTimer += Time.unscaledDeltaTime;

            if (tutorialOffRoadTimer >= tutorialOffRoadTipHoldTime)
            {
                QueueTutorialTipOnce(ref hasShownOffRoadResetTip, TutorialTipId.OffRoadReset, "도로를 벗어났습니다!\n도로가 아닌 곳으로 달리면 속도가 줄어듭니다.\n세모를 눌러 차량을 리셋하세요.", 6f);
            }
        }
        else
        {
            tutorialOffRoadTimer = 0f;
        }
    }

    private void UpdateTutorialAfterFirstLapTips()
    {
        if (tutorialFirstLapTime < 0f)
        {
            return;
        }

        if (!hasShownPauseTip && (activeTutorialTip != TutorialTipId.None || pendingTutorialTips.Count > 0))
        {
            tutorialFirstLapTime = Time.unscaledTime;
            return;
        }

        float elapsed = Time.unscaledTime - tutorialFirstLapTime;

        if (!hasShownPauseTip && elapsed >= tutorialPauseTipDelay)
        {
            QueueTutorialTipOnce(ref hasShownPauseTip, TutorialTipId.Pause, "네모 버튼을 눌러\n일시정지 메뉴를 열어보세요");
            return;
        }

        if (activeTutorialTip == TutorialTipId.Pause
            && CanCompleteActiveTutorialTip()
            && IsPauseMenuPaused())
        {
            tutorialPauseCompleted = true;
            tutorialPauseCompletedTime = Time.unscaledTime;
            CompleteActiveTutorialTip();
        }

        if (!hasShownFreeDriveTip
            && tutorialPauseCompleted
            && !IsPauseMenuPaused()
            && Time.unscaledTime - tutorialPauseCompletedTime >= tutorialFreeDriveTipDelay)
        {
            pendingTutorialTips.Clear();
            QueueTutorialTipOnce(ref hasShownFreeDriveTip, TutorialTipId.FreeDrive, "이제 자유롭게 주행해보세요!", 5f);
            tutorialTipsDisabled = true;
        }
    }

    private void UpdateTutorialTipAutoHide()
    {
        if (tutorialTipHideTime > 0f && Time.unscaledTime >= tutorialTipHideTime)
        {
            CompleteActiveTutorialTip();
        }
    }

    private void OnTutorialPlayerLapCompleted(int completedLap)
    {
        if (!tutorialMode || completedLap != 1 || hasShownPauseTip)
        {
            return;
        }

        tutorialFirstLapTime = Time.unscaledTime;
    }

    private void QueueTutorialTipOnce(ref bool shownFlag, TutorialTipId id, string message, float duration = 0f)
    {
        if (shownFlag)
        {
            return;
        }

        shownFlag = true;
        TutorialTipMessage tip = new TutorialTipMessage
        {
            Id = id,
            Message = message,
            Duration = duration
        };

        if (activeTutorialTip == TutorialTipId.None)
        {
            ShowTutorialTip(tip);
        }
        else
        {
            pendingTutorialTips.Enqueue(tip);
        }
    }

    private void ShowTutorialTip(TutorialTipMessage tip)
    {
        if (tutorialTipRoot == null || tutorialTipText == null)
        {
            return;
        }

        activeTutorialTip = tip.Id;
        activeTutorialTipShownTime = Time.unscaledTime;
        tutorialTipText.text = tip.Message;
        tutorialTipRoot.SetActive(true);
        tutorialTipHideTime = tip.Duration > 0f ? Time.unscaledTime + tip.Duration : -1f;
    }

    private bool CanCompleteActiveTutorialTip()
    {
        return activeTutorialTip != TutorialTipId.None
            && Time.unscaledTime - activeTutorialTipShownTime >= tutorialTipMinimumReadTime;
    }

    private bool IsPauseMenuPaused()
    {
        if (pauseMenu == null)
        {
            pauseMenu = FindObjectOfType<G29PauseMenu>();
        }

        return pauseMenu != null && pauseMenu.IsPaused;
    }

    private void CompleteActiveTutorialTip()
    {
        if (activeTutorialTip == TutorialTipId.LapPrompt)
        {
            tutorialLapPromptCompleted = true;
        }

        activeTutorialTip = TutorialTipId.None;
        tutorialTipHideTime = -1f;
        HideTutorialTip();
        ShowNextQueuedTutorialTip();
    }

    private void ShowNextQueuedTutorialTip()
    {
        if (activeTutorialTip != TutorialTipId.None || pendingTutorialTips.Count == 0)
        {
            return;
        }

        ShowTutorialTip(pendingTutorialTips.Dequeue());
    }

    private void HideTutorialTip()
    {
        if (tutorialTipRoot != null)
        {
            tutorialTipRoot.SetActive(false);
        }
    }

    private void UpdateHud()
    {
        if (lapText != null)
        {
            lapText.text = $"LAP {Mathf.Clamp(currentLap, 1, totalLaps)} / {totalLaps}";
        }

        float elapsed = raceFinished ? finalTime : Mathf.Max(0f, Time.realtimeSinceStartup - raceStartTime);

        if (!raceStarted)
        {
            elapsed = 0f;
        }

        if (timerText != null)
        {
            timerText.text = $"TIME {FormatTime(elapsed)}";
        }

        CarController controller = GetPlayerCarController();

        if (modeText != null)
        {
            modeText.text = $"MODE {(controller != null ? controller.TransmissionModeLabel : "Automatic")}";
        }

        if (speedText != null)
        {
            float speedKmh = controller != null ? controller.CurrentSpeedKmh : GetPlayerSpeedKmh();
            speedText.text = $"{Mathf.RoundToInt(speedKmh):000} KM/H";
        }

        if (gearText != null)
        {
            gearText.text = $"GEAR {(controller != null ? controller.GearLabel : "D")}";
        }

        if (lapTimesText != null)
        {
            lapTimesText.text = BuildCompletedLapText();
        }

        if (positionText != null)
        {
            positionText.text = GetPositionText(CalculateLivePosition());
        }
    }

    private CarController GetPlayerCarController()
    {
        if (playerCarController != null)
        {
            return playerCarController;
        }

        if (playerCar == null)
        {
            AutoAssignReferences();
        }

        if (playerCar != null)
        {
            playerCarController = playerCar.GetComponent<CarController>()
                ?? playerCar.GetComponentInChildren<CarController>(true);
        }

        return playerCarController;
    }

    private float GetPlayerSpeedKmh()
    {
        if (playerCar == null)
        {
            return 0f;
        }

        Rigidbody playerRigidbody = playerCar.GetComponent<Rigidbody>()
            ?? playerCar.GetComponentInChildren<Rigidbody>(true);

        return playerRigidbody != null ? playerRigidbody.linearVelocity.magnitude * 3.6f : 0f;
    }

    private string BuildCompletedLapText()
    {
        if (completedLapTimes.Count == 0)
        {
            return "";
        }

        string result = "";

        for (int i = 0; i < completedLapTimes.Count; i++)
        {
            result += $"LAP {i + 1}  {FormatTime(completedLapTimes[i])}";

            if (i < completedLapTimes.Count - 1)
            {
                result += "\n";
            }
        }

        return result;
    }

    private int CalculateLivePosition()
    {
        if (playerCar == null || opponentCars == null || opponentCars.Length == 0)
        {
            return 1;
        }

        float playerScore = GetProgressScore(playerCar);
        int position = 1;

        for (int i = 0; i < opponentCars.Length; i++)
        {
            if (opponentCars[i] == null || opponentCars[i] == playerCar)
            {
                continue;
            }

            if (GetProgressScore(opponentCars[i]) > playerScore + positionChangeMargin)
            {
                position++;
            }
        }

        return position;
    }

    private float GetProgressScore(Transform car)
    {
        if (car == null)
        {
            return float.MinValue;
        }

        CarProgress progress = GetOrCreateProgress(car, IsPlayerCar(car));
        float lapScore = progress.CompletedLaps;

        if (progress.CompletedLaps == 0 && !progress.IgnoredFirstPass && finishLine != null)
        {
            return GetStartGridScore(car);
        }

        CarControllerWaypointAi ai = car.GetComponent<CarControllerWaypointAi>()
            ?? car.GetComponentInParent<CarControllerWaypointAi>()
            ?? car.GetComponentInChildren<CarControllerWaypointAi>();

        string pathForCar = ai != null && !string.IsNullOrWhiteSpace(ai.PathName)
            ? ai.PathName
            : rankingPathName;

        float routeProgress = GetPathProgress01(car, pathForCar);

        if (routeProgress > 0f)
        {
            return lapScore + routeProgress;
        }

        return lapScore + GetPathProgress01(car, rankingPathName);
    }

    private float GetStartGridScore(Transform car)
    {
        if (playerCar != null)
        {
            Vector3 playerForward = playerCar.forward;
            playerForward.y = 0f;

            if (playerForward.sqrMagnitude > 0.01f)
            {
                return Vector3.Dot(car.position - playerCar.position, playerForward.normalized);
            }
        }

        Vector3 delta = car.position - finishLine.position;
        delta.y = 0f;

        return -delta.magnitude;
    }

    private void RegisterKnownCarsForProgress()
    {
        if (playerCar == null)
        {
            AutoAssignReferences();
        }

        if (playerCar != null)
        {
            GetOrCreateProgress(playerCar, true);
        }

        if (opponentCars == null)
        {
            return;
        }

        for (int i = 0; i < opponentCars.Length; i++)
        {
            if (opponentCars[i] != null)
            {
                GetOrCreateProgress(opponentCars[i], false);
            }
        }
    }

    private CarProgress GetOrCreateProgress(Transform car, bool isPlayer)
    {
        if (car == null)
        {
            return null;
        }

        Transform key = car.root != null ? car.root : car;

        if (!carProgress.TryGetValue(key, out CarProgress progress))
        {
            int totalCheckpoints = checkpointManager != null && checkpointManager.checkpoints != null
                ? checkpointManager.checkpoints.Count
                : 0;

            int nearestCheckpoint = totalCheckpoints > 0 ? FindNearestCheckpointIndex(key) : -1;
            int nextCheckpoint = totalCheckpoints > 0 ? (nearestCheckpoint + 1) % totalCheckpoints : 0;

            progress = new CarProgress
            {
                Car = key,
                CompletedLaps = 0,
                IgnoredFirstPass = false,
                Finished = false,
                FinishTime = 0f,
                Name = isPlayer ? playerName : BuildOpponentName(key),
                IsPlayer = isPlayer,
                LastCheckpoint = nearestCheckpoint,
                NextCheckpoint = nextCheckpoint,
                TotalCheckpoints = totalCheckpoints
            };

            carProgress.Add(key, progress);
        }

        if (isPlayer)
        {
            progress.Name = playerName;
            progress.IsPlayer = true;
            progress.CompletedLaps = checkpointManager != null ? progress.CompletedLaps : Mathf.Max(0, currentLap - 1);
            progress.IgnoredFirstPass = hasIgnoredFirstFinishLinePass;
            progress.Finished = raceFinished;
            progress.FinishTime = raceFinished ? finalTime : progress.FinishTime;
        }

        return progress;
    }

    private int FindNearestCheckpointIndex(Transform car)
    {
        if (checkpointManager == null || checkpointManager.checkpoints == null || checkpointManager.checkpoints.Count == 0 || car == null)
        {
            return -1;
        }

        int nearest = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < checkpointManager.checkpoints.Count; i++)
        {
            Transform checkpoint = checkpointManager.checkpoints[i];

            if (checkpoint == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(car.position - checkpoint.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = i;
            }
        }

        return nearest;
    }

    private bool IsPlayerCar(Transform car)
    {
        if (car == null || playerCar == null)
        {
            return false;
        }

        Transform carRoot = car.root != null ? car.root : car;
        Transform playerRoot = playerCar.root != null ? playerCar.root : playerCar;

        return car == playerCar || carRoot == playerRoot;
    }

    private string BuildOpponentName(Transform car)
    {
        if (car == null)
        {
            return "AI Driver";
        }

        string rawName = car.name.Replace("(Clone)", "").Trim();
        return string.IsNullOrWhiteSpace(rawName) ? "AI Driver" : rawName;
    }

    private float GetPathProgress01(Transform car, string pathName)
    {
        List<Transform> path = GetRankingPath(pathName);

        if (path.Count < 2 || car == null)
        {
            return 0f;
        }

        float bestProgress = 0f;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < path.Count; i++)
        {
            Transform a = path[i];
            Transform b = path[(i + 1) % path.Count];

            if (a == null || b == null)
            {
                continue;
            }

            Vector3 segment = b.position - a.position;
            Vector3 delta = car.position - a.position;
            segment.y = 0f;
            delta.y = 0f;

            float segmentLengthSqr = segment.sqrMagnitude;

            if (segmentLengthSqr <= 0.01f)
            {
                continue;
            }

            float t = Mathf.Clamp01(Vector3.Dot(delta, segment) / segmentLengthSqr);
            Vector3 projected = a.position + segment * t;
            projected.y = car.position.y;

            float distance = Vector3.SqrMagnitude(car.position - projected);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestProgress = (i + t) / path.Count;
            }
        }

        return bestProgress;
    }

    private float GetCheckpointProgress01(Transform car, CarProgress progress)
    {
        if (checkpointManager == null || checkpointManager.checkpoints == null || checkpointManager.checkpoints.Count == 0)
        {
            return 0f;
        }

        int total = checkpointManager.checkpoints.Count;
        int last = Mathf.Clamp(progress.LastCheckpoint, 0, total - 1);
        int next = Mathf.Clamp(progress.NextCheckpoint, 0, total - 1);
        float baseProgress = progress.LastCheckpoint < 0 ? 0f : last / (float)total;

        Transform currentCheckpoint = checkpointManager.checkpoints[last];
        Transform nextCheckpoint = checkpointManager.checkpoints[next];
        float segmentFactor = 0f;

        if (currentCheckpoint != null && nextCheckpoint != null)
        {
            Vector3 segment = nextCheckpoint.position - currentCheckpoint.position;
            Vector3 carDelta = car.position - currentCheckpoint.position;
            segment.y = 0f;
            carDelta.y = 0f;

            float segmentLengthSqr = segment.sqrMagnitude;

            if (segmentLengthSqr > 0.01f)
            {
                segmentFactor = Mathf.Clamp01(Vector3.Dot(carDelta, segment) / segmentLengthSqr) / total;
            }
        }

        return baseProgress + segmentFactor;
    }

    private void SetupCheckpointTriggers()
    {
        if (checkpointManager == null)
        {
            checkpointManager = FindObjectOfType<ALIyerEdon.Checkpoint_Manager>();
        }

        if (checkpointManager == null || checkpointManager.checkpoints == null)
        {
            return;
        }

        int total = checkpointManager.checkpoints.Count;

        for (int i = 0; i < total; i++)
        {
            Transform checkpoint = checkpointManager.checkpoints[i];

            if (checkpoint == null)
            {
                continue;
            }

            RaceCheckpointTrigger trigger = checkpoint.GetComponent<RaceCheckpointTrigger>();

            if (trigger == null)
            {
                trigger = checkpoint.gameObject.AddComponent<RaceCheckpointTrigger>();
            }

            trigger.Configure(this, i, total);
        }
    }

    private void LoadRankingWaypoints()
    {
        rankingWaypoints.Clear();

        if (string.IsNullOrWhiteSpace(rankingPathName))
        {
            return;
        }

        GameObject pathObject = GameObject.Find(rankingPathName);

        if (pathObject == null)
        {
            return;
        }

        foreach (Transform child in pathObject.transform)
        {
            rankingWaypoints.Add(child);
        }
    }

    private List<Transform> GetRankingPath(string pathName)
    {
        string key = string.IsNullOrWhiteSpace(pathName) ? rankingPathName : pathName;

        if (rankingPathCache.TryGetValue(key, out List<Transform> cached) && cached.Count > 0)
        {
            return cached;
        }

        List<Transform> path = new List<Transform>();
        GameObject pathObject = GameObject.Find(key);

        if (pathObject != null)
        {
            foreach (Transform child in pathObject.transform)
            {
                path.Add(child);
            }
        }

        rankingPathCache[key] = path;
        return path;
    }

    private string GetPositionText(int position)
    {
        int totalCars = Mathf.Max(1, (opponentCars?.Length ?? 0) + 1);
        return $"POS {position} / {totalCars}";
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (playerCar == null)
        {
            AutoAssignReferences();
        }

        CarController controller = null;

        if (playerCar != null)
        {
            controller = playerCar.GetComponent<CarController>()
                ?? playerCar.GetComponentInParent<CarController>()
                ?? playerCar.GetComponentInChildren<CarController>();
        }

        if (controller == null)
        {
            controller = FindObjectOfType<CarController>();
        }

        if (controller != null)
        {
            controller.SetControlsEnabled(enabled);
        }
    }

    private void ShowResults()
    {
        if (resultsRoot == null || resultsText == null)
        {
            return;
        }

        List<ResultEntry> entries = new List<ResultEntry>
        {
            new ResultEntry { Name = playerName, Time = finalTime, IsPlayer = true }
        };

        RegisterKnownCarsForProgress();

        Transform[] liveOpponents = opponentCars ?? Array.Empty<Transform>();

        for (int i = 0; i < liveOpponents.Length; i++)
        {
            if (liveOpponents[i] == null)
            {
                continue;
            }

            CarProgress progress = GetOrCreateProgress(liveOpponents[i], false);
            float time = progress.Finished ? progress.FinishTime : finalTime + Mathf.Max(1f, totalLaps - progress.CompletedLaps) * 30f + i;

            entries.Add(new ResultEntry
            {
                Name = progress.Name,
                Time = time,
                IsPlayer = false
            });
        }

        for (int i = 0; i < opponentResults.Length; i++)
        {
            if (i < liveOpponents.Length && liveOpponents[i] != null)
            {
                continue;
            }

            entries.Add(new ResultEntry
            {
                Name = opponentResults[i].driverName,
                Time = opponentResults[i].finishTime,
                IsPlayer = false
            });
        }

        entries.Sort((a, b) => a.Time.CompareTo(b.Time));

        string result = "RACE RESULT\n\n";

        for (int i = 0; i < entries.Count; i++)
        {
            string marker = entries[i].IsPlayer ? "  < YOU" : "";
            result += $"{i + 1}. {entries[i].Name}  {FormatTime(entries[i].Time)}{marker}\n";
        }

        ResizeResultsUi(entries.Count);
        resultsText.text = result;
        resultsRoot.SetActive(true);
        selectedResultButtonIndex = 0;
        previousDpadDirection = Vector2.zero;
        previousCirclePressed = false;
        RefreshSelectedResultButton();
    }

    private void ShowTutorialComplete()
    {
        activeTutorialTip = TutorialTipId.None;
        pendingTutorialTips.Clear();
        HideTutorialTip();

        if (resultsRoot == null || resultsText == null)
        {
            return;
        }

        ResizeResultsUi(1);
        resultsText.text = "튜토리얼 완료!\n\n이제 준비가 끝났습니다.";
        resultsRoot.SetActive(true);
        selectedResultButtonIndex = 0;
        previousDpadDirection = Vector2.zero;
        previousCirclePressed = false;
        RefreshSelectedResultButton();
    }

    private void HandleResultMenuInput()
    {
        Vector2 dpadDirection = ReadDpadDirection();

        if (dpadDirection != Vector2.zero && dpadDirection != previousDpadDirection)
        {
            if (Mathf.Abs(dpadDirection.y) >= Mathf.Abs(dpadDirection.x))
            {
                SelectResultButton(dpadDirection.y > 0f ? -1 : 1);
            }
            else
            {
                SelectResultButton(dpadDirection.x > 0f ? 1 : -1);
            }
        }

        previousDpadDirection = dpadDirection;

        bool circlePressed = IsCirclePressed();

        if (circlePressed && !previousCirclePressed)
        {
            SubmitSelectedResultButton();
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

    private void SelectResultButton(int offset)
    {
        if (resultButtons.Length == 0)
        {
            return;
        }

        selectedResultButtonIndex = (selectedResultButtonIndex + offset + resultButtons.Length) % resultButtons.Length;
        RefreshSelectedResultButton();
    }

    private void SubmitSelectedResultButton()
    {
        if (resultButtons.Length == 0)
        {
            return;
        }

        Button selectedButton = resultButtons[selectedResultButtonIndex];

        if (selectedButton != null && selectedButton.IsActive() && selectedButton.interactable)
        {
            selectedButton.onClick.Invoke();
        }
    }

    private void RefreshSelectedResultButton()
    {
        for (int i = 0; i < resultButtonImages.Length; i++)
        {
            if (resultButtonImages[i] == null)
            {
                continue;
            }

            resultButtonImages[i].color = i == selectedResultButtonIndex
                ? Color.Lerp(resultButtonBaseColors[i], Color.white, 0.28f)
                : resultButtonBaseColors[i];
        }

        if (resultsRoot != null && resultsRoot.activeInHierarchy && EventSystem.current != null && resultButtons.Length > 0)
        {
            EventSystem.current.SetSelectedGameObject(resultButtons[selectedResultButtonIndex].gameObject);
        }
    }

    private void BuildUiIfNeeded()
    {
        if (hudRoot != null)
        {
            return;
        }

        hudRoot = new GameObject("RaceHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        hudRoot.transform.SetParent(transform, false);

        Canvas hudCanvas = hudRoot.GetComponent<Canvas>();
        hudCanvas.sortingOrder = 80;

        if (useWorldSpaceCanvas)
        {
            hudCanvas.renderMode = RenderMode.WorldSpace;
            hudCanvas.worldCamera = Camera.main;
            hudRoot.AddComponent<VRCameraCanvasPlacement>()
                .Configure(Camera.main != null ? Camera.main.transform : null, vrHudDistance, new Vector2(0f, 0.08f), vrCanvasScale, true);
        }
        else
        {
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        CanvasScaler hudScaler = hudRoot.GetComponent<CanvasScaler>();
        hudScaler.uiScaleMode = useWorldSpaceCanvas ? CanvasScaler.ScaleMode.ConstantPixelSize : CanvasScaler.ScaleMode.ScaleWithScreenSize;
        hudScaler.referenceResolution = new Vector2(1920f, 1080f);
        hudScaler.matchWidthOrHeight = 0.5f;

        Image hudPanel = CreateImage("HudPanel", hudRoot.transform, new Color(0f, 0f, 0f, 0f));
        RectTransform hudPanelRect = hudPanel.rectTransform;
        hudPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        hudPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        hudPanelRect.pivot = new Vector2(0.5f, 0.5f);
        hudPanelRect.sizeDelta = new Vector2(660f, 220f);
        hudPanelRect.anchoredPosition = new Vector2(0f, -210f);

        lapText = CreateText("LapText", hudPanel.transform, "LAP 1 / 3", 30, FontStyle.Bold, Color.white);
        SetStretch(lapText.rectTransform, new Vector2(24f, 166f), new Vector2(-250f, -14f));

        positionText = CreateText("PositionText", hudPanel.transform, "POS 1 / 1", 26, FontStyle.Bold, new Color(0.90f, 0.92f, 1f, 1f));
        SetStretch(positionText.rectTransform, new Vector2(24f, 124f), new Vector2(-250f, -54f));

        timerText = CreateText("TimerText", hudPanel.transform, "TIME 00:00:00", 30, FontStyle.Bold, new Color(0.08f, 0.72f, 0.56f, 1f));
        SetStretch(timerText.rectTransform, new Vector2(24f, 78f), new Vector2(-250f, -100f));

        modeText = CreateText("ModeText", hudPanel.transform, "MODE Automatic", 24, FontStyle.Bold, new Color(0.78f, 0.88f, 1f, 1f));
        SetStretch(modeText.rectTransform, new Vector2(24f, 38f), new Vector2(-250f, -138f));

        speedText = CreateText("SpeedText", hudPanel.transform, "000 KM/H", 34, FontStyle.Bold, new Color(1f, 0.94f, 0.72f, 1f));
        speedText.alignment = TextAnchor.MiddleRight;
        SetStretch(speedText.rectTransform, new Vector2(420f, 120f), new Vector2(-24f, -42f));

        gearText = CreateText("GearText", hudPanel.transform, "GEAR D", 34, FontStyle.Bold, new Color(0.28f, 0.92f, 0.72f, 1f));
        gearText.alignment = TextAnchor.MiddleRight;
        SetStretch(gearText.rectTransform, new Vector2(420f, 58f), new Vector2(-24f, -104f));

        lapTimesText = CreateText("LapTimesText", hudPanel.transform, "", 24, FontStyle.Bold, new Color(1f, 0.82f, 0.28f, 1f));
        lapTimesText.alignment = TextAnchor.UpperLeft;
        SetStretch(lapTimesText.rectTransform, new Vector2(24f, -74f), new Vector2(-250f, -210f));

        BuildTutorialTipUi();
        BuildMinimapUi();
        BuildResultsUi();
    }

    private void BuildTutorialTipUi()
    {
        Image panel = CreateImage("TutorialTipPanel", hudRoot.transform, new Color(0f, 0f, 0f, 0.58f));
        tutorialTipRoot = panel.gameObject;

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(820f, 190f);
        panelRect.anchoredPosition = new Vector2(0f, 6f);

        tutorialTipText = CreateText("TutorialTipText", panel.transform, "", 34, FontStyle.Bold, Color.white);
        tutorialTipText.alignment = TextAnchor.MiddleCenter;
        tutorialTipText.resizeTextForBestFit = true;
        tutorialTipText.resizeTextMinSize = 22;
        tutorialTipText.resizeTextMaxSize = 34;

        RectTransform textRect = tutorialTipText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(34f, 22f);
        textRect.offsetMax = new Vector2(-34f, -22f);

        tutorialTipRoot.SetActive(false);
    }

    private void BuildMinimapUi()
    {
        if (!buildRuntimeMinimap)
        {
            return;
        }

        DisableLegacyMinimapControllers();

        Renderer minimapQuad = FindPlayerMinimapQuad();

        if (minimapQuad == null)
        {
            Debug.LogWarning("[RaceSessionManager] Player minimap Quad was not found. Runtime minimap was skipped.");
            return;
        }

        Camera camera = FindOrCreateMinimapCamera();
        minimapController = camera.gameObject.GetComponent<RaceMinimapController>();

        if (minimapController == null)
        {
            minimapController = camera.gameObject.AddComponent<RaceMinimapController>();
        }

        minimapController.ConfigureQuad(
            camera,
            minimapQuad,
            playerCar,
            opponentCars,
            minimapCameraHeight,
            minimapOrthographicSize,
            minimapTextureSize);
    }

    private Renderer FindPlayerMinimapQuad()
    {
        if (playerCar == null)
        {
            AutoAssignReferences();
        }

        if (playerCar == null)
        {
            return null;
        }

        Transform[] children = playerCar.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child != null && (child.name == "MiniMap" || child.name == "Quad"))
            {
                Renderer renderer = child.GetComponent<Renderer>();

                if (renderer != null)
                {
                    return renderer;
                }
            }
        }

        return null;
    }

    private Camera FindOrCreateMinimapCamera()
    {
        GameObject cameraObject = GameObject.Find("MinimapCamera");

        if (cameraObject == null)
        {
            cameraObject = new GameObject("MinimapCamera");
        }

        Camera camera = cameraObject.GetComponent<Camera>();

        if (camera == null)
        {
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.enabled = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.03f, 0.035f, 0.035f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = minimapOrthographicSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = Mathf.Max(600f, minimapCameraHeight + 200f);
        camera.depth = -20f;

        MinimapCameraRotation legacyRotation = cameraObject.GetComponent<MinimapCameraRotation>();

        if (legacyRotation != null)
        {
            legacyRotation.enabled = false;
        }

        ALIyerEdon.Minimap_Camera legacyAssetCamera = cameraObject.GetComponent<ALIyerEdon.Minimap_Camera>();

        if (legacyAssetCamera != null)
        {
            legacyAssetCamera.enabled = false;
        }

        return camera;
    }

    private static void DisableLegacyMinimapControllers()
    {
        MinimapController[] controllers = FindObjectsOfType<MinimapController>(true);

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
            {
                if (controllers[i].minimapImage != null)
                {
                    controllers[i].minimapImage.gameObject.SetActive(false);
                }

                if (controllers[i].playerDot != null)
                {
                    controllers[i].playerDot.gameObject.SetActive(false);
                }

                controllers[i].enabled = false;
            }
        }

        MinimapDot[] dots = FindObjectsOfType<MinimapDot>(true);

        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null)
            {
                dots[i].enabled = false;
            }
        }
    }

    private void BuildResultsUi()
    {
        resultsRoot = new GameObject("RaceResultCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        resultsRoot.transform.SetParent(transform, false);

        Canvas canvas = resultsRoot.GetComponent<Canvas>();
        canvas.sortingOrder = 110;

        if (useWorldSpaceCanvas)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            resultsRoot.AddComponent<VRCameraCanvasPlacement>()
                .Configure(Camera.main != null ? Camera.main.transform : null, vrMenuDistance, Vector2.zero, vrCanvasScale, false);
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        CanvasScaler scaler = resultsRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = useWorldSpaceCanvas ? CanvasScaler.ScaleMode.ConstantPixelSize : CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image dim = CreateImage("Dim", resultsRoot.transform, new Color(0f, 0f, 0f, 0f));
        RectTransform dimRect = dim.rectTransform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        Image panel = CreateImage("Panel", resultsRoot.transform, new Color(0.02f, 0.025f, 0.03f, 0.82f));
        RectTransform panelRect = panel.rectTransform;
        resultsPanelRect = panelRect;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(620f, 520f);
        panelRect.anchoredPosition = new Vector2(0f, -80f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(44, 44, 36, 36);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        resultsText = CreateText("ResultsText", panel.transform, "RACE RESULT", 28, FontStyle.Bold, Color.white);
        resultsText.alignment = TextAnchor.UpperCenter;
        AddLayoutElement(resultsText.gameObject, 230f);
        resultsTextLayout = resultsText.GetComponent<LayoutElement>();

        Button retryButton = CreateButton(panel.transform, "Retry", new Color(0.10f, 0.58f, 0.42f, 1f), RetryRace);
        Button lobbyButton = CreateButton(panel.transform, "Return To Lobby", new Color(0.14f, 0.27f, 0.68f, 1f), ReturnToLobby);
        Button quitButton = CreateButton(panel.transform, "Quit Game", new Color(0.70f, 0.08f, 0.12f, 1f), QuitGame);

        SetResultButtons(new[] { retryButton, lobbyButton, quitButton });
        resultsRoot.SetActive(false);
    }

    private void ResizeResultsUi(int entryCount)
    {
        float textHeight = Mathf.Clamp(92f + entryCount * 34f, 230f, 520f);
        float panelHeight = Mathf.Clamp(textHeight + 292f, 520f, 760f);

        if (resultsTextLayout != null)
        {
            resultsTextLayout.preferredHeight = textHeight;
            resultsTextLayout.minHeight = textHeight;
        }

        if (resultsPanelRect != null)
        {
            resultsPanelRect.sizeDelta = new Vector2(resultsPanelRect.sizeDelta.x, panelHeight);
        }
    }

    private void SetResultButtons(Button[] buttons)
    {
        resultButtons = buttons ?? new Button[0];
        resultButtonImages = new Image[resultButtons.Length];
        resultButtonBaseColors = new Color[resultButtons.Length];

        for (int i = 0; i < resultButtons.Length; i++)
        {
            if (resultButtons[i] == null)
            {
                continue;
            }

            resultButtonImages[i] = resultButtons[i].GetComponent<Image>();
            resultButtonBaseColors[i] = resultButtonImages[i] != null ? resultButtonImages[i].color : Color.white;
        }
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

    private static RawImage CreateRawImage(string objectName, Transform parent, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
        obj.transform.SetParent(parent, false);

        RawImage image = obj.GetComponent<RawImage>();
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
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        text.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.55f);
        ApplyNoZTestMaterial(text);
        return text;
    }

    private static Button CreateButton(Transform parent, string label, Color normalColor, UnityEngine.Events.UnityAction action)
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

        Text text = CreateText("Label", obj.transform, label, 26, FontStyle.Bold, Color.white);
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = 26;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        AddLayoutElement(obj, 62f);
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

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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

    private static string FormatTime(float time)
    {
        TimeSpan span = TimeSpan.FromSeconds(Mathf.Max(0f, time));
        int minutes = Mathf.FloorToInt((float)span.TotalMinutes);
        int centiseconds = Mathf.FloorToInt(span.Milliseconds / 10f);
        return $"{minutes:00}:{span.Seconds:00}:{centiseconds:00}";
    }
}

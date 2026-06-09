using UnityEngine;
using UnityEngine.InputSystem;

public class CarTransmissionController : MonoBehaviour
{
    public enum TransmissionMode
    {
        Automatic,
        Manual
    }

    [Header("Mode")]
    public bool useTestTransmissionOverride;
    public TransmissionMode testTransmissionMode = TransmissionMode.Automatic;
    public string playerPrefsKey = "TransmissionMode";

    [Header("Recommended Manual Tuning")]
    public bool useRecommendedManualTuning = true;

    [Header("Input")]
    public PlayerInput playerInput;
    public Rigidbody carRb;
    public string gearUpActionName = "Gear Up";
    public string gearDownActionName = "Gear Down";
    public string clutchActionName = "Clutch";
    public bool invertClutch;
    public float clutchDeadZone = 0.05f;
    public float clutchShiftThreshold = 0.5f;

    [Header("Manual Gears")]
    [Range(1, 6)] public int currentGear = 1;
    public float manualTorqueMultiplier = 2.2f;
    public float[] gearRatios = { 2.2f, 1.8f, 1.45f, 1.25f, 1.12f, 1.02f };
    public float[] gearMaxSpeedsKmh = { 95f, 140f, 185f, 230f, 280f, 340f };
    public float[] upshiftMinSpeedsKmh = { 0f, 35f, 75f, 115f, 155f, 200f };
    public float[] gearPowerStartSpeedsKmh = { 0f, 15f, 45f, 80f, 120f, 165f };
    public float gearLowSpeedRecoveryRangeKmh = 40f;
    public float gearLowSpeedMinimumTorque = 0.12f;
    public float fullTorqueSpeedRatio = 0.8f;
    public float softLimitSpeedRatio = 1.15f;
    public float lowGearMinimumTorque = 0.04f;
    public float highGearMinimumTorque = 0.38f;
    public bool clutchCutsPower = true;

    [Header("Manual High Speed Assist")]
    public float manualAssistStartKmh = 95f;
    public float manualAssistMaxKmh = 345f;
    public float manualAssistForce = 30000f;
    public float[] manualAssistGearMultipliers = { 0.1f, 0.35f, 0.7f, 1.15f, 1.6f, 2.2f };

    private TransmissionMode resolvedMode = TransmissionMode.Automatic;
    private float clutchAmount;
    private InputAction gearUpAction;
    private InputAction gearDownAction;
    private InputAction clutchAction;

    public bool IsManual => resolvedMode == TransmissionMode.Manual;
    public string ModeLabel => IsManual ? "Manual" : "Automatic";
    public string GearLabel => IsManual ? currentGear.ToString() : "D";
    public float ClutchAmount => clutchAmount;
    public int CurrentGear => currentGear;

    private void Awake()
    {
        ApplyRecommendedManualTuningIfNeeded();

        if (carRb == null)
        {
            carRb = GetComponent<Rigidbody>();
        }

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        ResolveMode();
        CacheActions();
        currentGear = Mathf.Clamp(currentGear, 1, 6);
    }

    private void OnEnable()
    {
        ApplyRecommendedManualTuningIfNeeded();
        ResolveMode();
        CacheActions();
    }

    private void Update()
    {
        ResolveMode();
        CacheActions();
        UpdateClutch();

        if (!IsManual)
        {
            return;
        }

        if (WasPressed(gearUpAction))
        {
            TryShift(1);
        }

        if (WasPressed(gearDownAction))
        {
            TryShift(-1);
        }
    }

    public float CalculateManualMotorTorque(float baseMotorTorque, float speedKmh, float accelInput)
    {
        if (!IsManual || accelInput <= 0f)
        {
            return 0f;
        }

        int index = Mathf.Clamp(currentGear - 1, 0, 5);
        float ratio = GetArrayValue(gearRatios, index, 1f);
        float maxSpeed = Mathf.Max(1f, GetArrayValue(gearMaxSpeedsKmh, index, 100f));
        float speedRatio = Mathf.Abs(speedKmh) / maxSpeed;
        float speedLimitMultiplier = CalculateSpeedLimitMultiplier(speedRatio, currentGear);
        float lowSpeedMultiplier = CalculateLowSpeedGearMultiplier(index, speedKmh);
        float clutchMultiplier = clutchCutsPower ? 1f - clutchAmount : 1f;

        return Mathf.Max(0f, baseMotorTorque)
            * manualTorqueMultiplier
            * ratio
            * Mathf.Clamp01(accelInput)
            * lowSpeedMultiplier
            * speedLimitMultiplier
            * Mathf.Clamp01(clutchMultiplier);
    }

    public float CalculateManualAssistForce(float speedKmh, float accelInput)
    {
        if (!IsManual || accelInput <= 0f || speedKmh < manualAssistStartKmh || speedKmh >= manualAssistMaxKmh)
        {
            return 0f;
        }

        int index = Mathf.Clamp(currentGear - 1, 0, 5);
        float maxSpeed = Mathf.Max(1f, GetArrayValue(gearMaxSpeedsKmh, index, 100f));
        float speedRatio = Mathf.Abs(speedKmh) / maxSpeed;
        float speedLimitMultiplier = CalculateSpeedLimitMultiplier(speedRatio, currentGear);
        float lowSpeedMultiplier = CalculateLowSpeedGearMultiplier(index, speedKmh);
        float assistBlend = Mathf.InverseLerp(manualAssistStartKmh, manualAssistMaxKmh, speedKmh);
        float gearAssistMultiplier = GetArrayValue(manualAssistGearMultipliers, index, 1f);
        float clutchMultiplier = clutchCutsPower ? 1f - clutchAmount : 1f;

        return manualAssistForce
            * gearAssistMultiplier
            * Mathf.Lerp(0.55f, 1f, assistBlend)
            * Mathf.Clamp01(accelInput)
            * lowSpeedMultiplier
            * speedLimitMultiplier
            * Mathf.Clamp01(clutchMultiplier);
    }

    private void TryShift(int direction)
    {
        if (clutchAmount < clutchShiftThreshold)
        {
            return;
        }

        if (direction > 0 && !CanUpshift())
        {
            return;
        }

        currentGear = Mathf.Clamp(currentGear + direction, 1, 6);
    }

    private void ResolveMode()
    {
        if (useTestTransmissionOverride)
        {
            resolvedMode = testTransmissionMode;
            return;
        }

        string savedMode = PlayerPrefs.GetString(playerPrefsKey, "Automatic");
        resolvedMode = savedMode == "Manual" ? TransmissionMode.Manual : TransmissionMode.Automatic;
    }

    private void ApplyRecommendedManualTuningIfNeeded()
    {
        if (!useRecommendedManualTuning)
        {
            return;
        }

        manualTorqueMultiplier = 2.2f;
        gearRatios = new[] { 2.2f, 1.8f, 1.45f, 1.25f, 1.12f, 1.02f };
        gearMaxSpeedsKmh = new[] { 95f, 140f, 185f, 230f, 280f, 340f };
        upshiftMinSpeedsKmh = new[] { 0f, 35f, 75f, 115f, 155f, 200f };
        gearPowerStartSpeedsKmh = new[] { 0f, 15f, 45f, 80f, 120f, 165f };
        gearLowSpeedRecoveryRangeKmh = 40f;
        gearLowSpeedMinimumTorque = 0.12f;
        fullTorqueSpeedRatio = 0.8f;
        softLimitSpeedRatio = 1.15f;
        lowGearMinimumTorque = 0.04f;
        highGearMinimumTorque = 0.38f;
        manualAssistStartKmh = 95f;
        manualAssistMaxKmh = 345f;
        manualAssistForce = 30000f;
        manualAssistGearMultipliers = new[] { 0.1f, 0.35f, 0.7f, 1.15f, 1.6f, 2.2f };
    }

    private void CacheActions()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (carRb == null)
        {
            carRb = GetComponent<Rigidbody>();
        }

        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        if (gearUpAction == null)
        {
            gearUpAction = playerInput.actions.FindAction(gearUpActionName);
        }

        if (gearDownAction == null)
        {
            gearDownAction = playerInput.actions.FindAction(gearDownActionName);
        }

        if (clutchAction == null)
        {
            clutchAction = playerInput.actions.FindAction(clutchActionName);
        }
    }

    private void UpdateClutch()
    {
        float rawClutch = clutchAction != null ? clutchAction.ReadValue<float>() : 0f;

        if (invertClutch)
        {
            rawClutch = 1f - rawClutch;
        }

        clutchAmount = Mathf.Clamp01(rawClutch);

        if (clutchAmount < clutchDeadZone)
        {
            clutchAmount = 0f;
        }
    }

    private static bool WasPressed(InputAction action)
    {
        return action != null && action.WasPressedThisFrame();
    }

    private float CalculateSpeedLimitMultiplier(float speedRatio, int gear)
    {
        float minimum = gear >= 5 ? highGearMinimumTorque : lowGearMinimumTorque;

        if (speedRatio <= fullTorqueSpeedRatio)
        {
            return 1f;
        }

        float t = Mathf.InverseLerp(fullTorqueSpeedRatio, softLimitSpeedRatio, speedRatio);
        t = Mathf.SmoothStep(0f, 1f, t);
        return Mathf.Lerp(1f, minimum, t);
    }

    private float CalculateLowSpeedGearMultiplier(int gearIndex, float speedKmh)
    {
        if (gearIndex <= 0)
        {
            return 1f;
        }

        float startSpeed = GetArrayValue(gearPowerStartSpeedsKmh, gearIndex, 0f);
        float recoveryEndSpeed = startSpeed + Mathf.Max(1f, gearLowSpeedRecoveryRangeKmh);
        float t = Mathf.InverseLerp(startSpeed, recoveryEndSpeed, Mathf.Abs(speedKmh));
        t = Mathf.SmoothStep(0f, 1f, t);
        return Mathf.Lerp(Mathf.Clamp01(gearLowSpeedMinimumTorque), 1f, t);
    }

    private bool CanUpshift()
    {
        int targetGear = currentGear + 1;

        if (targetGear > 6)
        {
            return false;
        }

        int targetIndex = Mathf.Clamp(targetGear - 1, 0, 5);
        float requiredSpeed = GetArrayValue(upshiftMinSpeedsKmh, targetIndex, 0f);
        return GetCurrentSpeedKmh() >= requiredSpeed;
    }

    private float GetCurrentSpeedKmh()
    {
        return carRb != null ? carRb.linearVelocity.magnitude * 3.6f : 0f;
    }

    private static float GetArrayValue(float[] values, int index, float fallback)
    {
        return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
    }

    public float GetGearMaxSpeedKmh(int gear)
    {
        int index = Mathf.Clamp(gear - 1, 0, 5);
        return GetArrayValue(gearMaxSpeedsKmh, index, 100f);
    }

    public float GetGearPowerStartSpeedKmh(int gear)
    {
        int index = Mathf.Clamp(gear - 1, 0, 5);
        return GetArrayValue(gearPowerStartSpeedsKmh, index, 0f);
    }
}

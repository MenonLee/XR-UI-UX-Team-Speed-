using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("물리 바퀴 (Wheel Colliders)")]
    [Tooltip("앞 왼쪽 바퀴의 WheelCollider입니다. 실제 물리 계산에 사용됩니다.")]
    public WheelCollider frontLeftCollider;

    [Tooltip("앞 오른쪽 바퀴의 WheelCollider입니다. 실제 물리 계산에 사용됩니다.")]
    public WheelCollider frontRightCollider;

    [Tooltip("뒤 왼쪽 바퀴의 WheelCollider입니다. 구동 토크가 적용됩니다.")]
    public WheelCollider rearLeftCollider;

    [Tooltip("뒤 오른쪽 바퀴의 WheelCollider입니다. 구동 토크가 적용됩니다.")]
    public WheelCollider rearRightCollider;

    [Header("눈에 보이는 바퀴 (Visual Meshes)")]
    [Tooltip("앞 왼쪽 바퀴의 보이는 모델입니다. WheelCollider 위치와 회전에 맞춰 움직입니다.")]
    public Transform frontLeftMesh;

    [Tooltip("앞 오른쪽 바퀴의 보이는 모델입니다. WheelCollider 위치와 회전에 맞춰 움직입니다.")]
    public Transform frontRightMesh;

    [Tooltip("뒤 왼쪽 바퀴의 보이는 모델입니다. WheelCollider 위치와 회전에 맞춰 움직입니다.")]
    public Transform rearLeftMesh;

    [Tooltip("뒤 오른쪽 바퀴의 보이는 모델입니다. WheelCollider 위치와 회전에 맞춰 움직입니다.")]
    public Transform rearRightMesh;

    [Header("Input System 연동")]
    [Tooltip("Unity Input System의 PlayerInput 컴포넌트입니다. 비워두면 같은 오브젝트에서 자동으로 찾습니다.")]
    public PlayerInput playerInput;

    [Tooltip("조향 입력 액션 이름입니다. Input Actions에 있는 이름과 정확히 같아야 합니다.")]
    public string steeringActionName = "Steering";

    [Tooltip("엑셀 입력 액션 이름입니다. Input Actions에 있는 이름과 정확히 같아야 합니다.")]
    public string accelerateActionName = "Accelerate";

    [Tooltip("브레이크 입력 액션 이름입니다. Input Actions에 있는 이름과 정확히 같아야 합니다.")]
    public string brakeActionName = "Brake";

    [Header("가속")]
    [Tooltip("바퀴에 전달되는 기본 엔진 힘입니다. 높을수록 차가 강하게 가속합니다.")]
    public float motorTorque = 22000f;

    [Tooltip("최종 가속 힘에 곱해지는 배율입니다. motorTorque를 전체적으로 키우거나 줄일 때 사용합니다.")]
    public float accelerationMultiplier = 3.6f;

    [Tooltip("엑셀 입력 반응 곡선입니다. 1이면 그대로, 1보다 크면 초반 가속이 부드러워집니다.")]
    public float throttleCurve = 1.0f;

    [Tooltip("엑셀 입력이 이 값보다 커야 가속을 시작합니다. 페달 떨림 방지용입니다.")]
    public float throttleStartThreshold = 0.05f;

    [Header("High Speed Assist")]
    public float highSpeedAssistStartKmh = 85f;
    public float highSpeedAssistMaxKmh = 285f;
    public float highSpeedAssistForce = 18000f;

    [Header("Surface Speed Penalty")]
    [Tooltip("켜두면 바퀴가 Road 태그가 아닌 표면을 밟을 때 가속 힘을 줄입니다.")]
    public bool reducePowerOffRoad = true;

    [Tooltip("도로로 인정할 태그 이름입니다.")]
    public string roadSurfaceTag = "Road";

    [Tooltip("도로가 아닌 표면을 밟을 때 적용할 구동력 비율입니다. 0.8이면 20% 감소입니다.")]
    [Range(0.1f, 1f)]
    public float offRoadPowerMultiplier = 0.8f;
    [Header("엑셀 뗐을 때 자연 감속")]
    [Tooltip("기본 구름 저항입니다. 너무 높으면 엑셀을 뗐을 때 차가 빨리 멈춥니다.")]
    public float rollingResistance = 80f;

    [Tooltip("엑셀을 뗐을 때 엔진브레이크처럼 작동하는 감속 힘입니다.")]
    public float engineBrakeForce = 220f;

    [Tooltip("속도가 빠를수록 커지는 공기 저항입니다. 고속에서 자연스럽게 속도를 줄입니다.")]
    public float airResistance = 0.6f;

    [Tooltip("저속에서 감속 힘이 서서히 줄어들기 시작하는 속도입니다. km/h 기준입니다.")]
    public float coastFadeStartSpeed = 3f;

    [Tooltip("이 속도 이상에서는 자연 감속 힘이 정상적으로 적용됩니다. km/h 기준입니다.")]
    public float coastFadeEndSpeed = 25f;

    [Tooltip("엑셀을 뗐을 때 바퀴에 거는 아주 약한 브레이크입니다. 0에 가까울수록 더 오래 굴러갑니다.")]
    public float idleWheelBrakeTorque = 5f;

    [Header("브레이크 / 후진")]
    [Tooltip("브레이크를 끝까지 밟았을 때 적용되는 최대 브레이크 힘입니다.")]
    public float maxBrakeTorque = 50000f;

    [Tooltip("켜두면 정지 상태에서 브레이크를 계속 눌러 후진할 수 있습니다.")]
    public bool enableReverse = true;

    [Tooltip("이 속도 이하일 때만 후진 대기 시간이 쌓입니다. km/h 기준입니다.")]
    public float reverseReadySpeed = 0.3f;

    [Tooltip("정지 상태에서 브레이크를 이 시간만큼 누르고 있어야 후진이 시작됩니다.")]
    public float reverseDelayTime = 0.7f;

    [Tooltip("후진할 때 뒤쪽 바퀴에 적용되는 힘입니다.")]
    public float reverseTorque = 5000f;

    [Tooltip("후진 속도가 이 값 이상이면 더 이상 후진 토크를 주지 않습니다. km/h 기준입니다.")]
    public float maxReverseSpeed = 25f;

    [Header("급브레이크 보조 감속")]
    [Tooltip("브레이크를 밟을 때 Rigidbody에 추가로 주는 감속 힘입니다. 강한 제동감을 만듭니다.")]
    public float brakeAssistForce = 18000f;

    [Tooltip("브레이크 입력 반응 곡선입니다. 1보다 작으면 초반 브레이크 반응이 강해집니다.")]
    public float brakeResponseCurve = 0.65f;

    [Header("조향")]
    [Tooltip("저속에서 사용할 최대 앞바퀴 조향 각도입니다.")]
    public float maxSteerAngle = 30f;

    [Header("차량 안정성")]
    [Tooltip("차량 Rigidbody의 질량입니다.")]
    public float carMass = 1200f;

    [Tooltip("차량 회전 감쇠값입니다. 높을수록 차가 덜 휘청이고 회전이 빨리 안정됩니다.")]
    public float angularDrag = 2.5f;

    [Tooltip("차량 무게중심 보정값입니다. Y를 낮추면 전복이 줄어듭니다.")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.6f, 0f);

    [Tooltip("속도에 비례해서 차를 아래로 누르는 힘입니다. 고속 안정성을 높입니다.")]
    public float downforce = 320f;

    [Header("속도별 조향 제한")]
    [Tooltip("고속에서 제한될 최대 조향 각도입니다.")]
    public float highSpeedSteerAngle = 18f;

    [Tooltip("이 속도부터 조향 각도가 줄어들기 시작합니다. km/h 기준입니다.")]
    public float steerReduceStartSpeed = 80f;

    [Tooltip("이 속도에 도달하면 highSpeedSteerAngle까지 조향이 제한됩니다. km/h 기준입니다.")]
    public float steerReduceEndSpeed = 180f;

    [Header("바퀴 마찰")]
    [Tooltip("바퀴가 앞뒤 방향으로 노면을 잡는 힘입니다. 높을수록 가속과 제동 접지가 강해집니다.")]
    public float forwardStiffness = 2.5f;

    [Tooltip("바퀴가 좌우 방향으로 노면을 잡는 힘입니다. 높을수록 미끄러짐이 줄어듭니다.")]
    public float sidewaysStiffness = 2.0f;

    [Header("입력 설정")]
    [Tooltip("엑셀 입력값을 반대로 뒤집습니다. 페달을 안 밟았는데 1로 들어오는 장비에서 사용합니다.")]
    public bool invertAccelerate = true;

    [Tooltip("브레이크 입력값을 반대로 뒤집습니다. 페달을 안 밟았는데 1로 들어오는 장비에서 사용합니다.")]
    public bool invertBrake = true;

    [Tooltip("핸들 입력값을 반대로 뒤집습니다. 왼쪽/오른쪽 조향이 반대로 움직일 때 사용합니다.")]
    public bool invertSteering = false;

    [Tooltip("핸들이 중앙에 있을 때 입력값이 0에서 살짝 벗어나 있으면 보정합니다.")]
    public float steeringCenterOffset = 0f;

    [Tooltip("핸들 전용 데드존입니다. 값이 작을수록 미세 조향을 더 잘 읽습니다.")]
    public float steeringDeadZone = 0.0005f;

    [Tooltip("엑셀/브레이크 페달 전용 데드존입니다. 페달 떨림이나 미세 입력을 무시합니다.")]
    public float pedalDeadZone = 0.05f;

    [Tooltip("조향 반응 곡선입니다. 1이면 원본 그대로, 1보다 작으면 중앙 근처가 더 민감해집니다.")]
    public float steeringResponse = 1.0f;

    [Header("핸들 동기화")]
    [Tooltip("차 안에 보이는 핸들 모델입니다. 실제 조향 입력에 맞춰 회전합니다.")]
    public Transform steeringWheelMesh;

    [Tooltip("핸들이 한쪽으로 끝까지 돌아갔을 때의 시각적 회전 각도입니다. G29는 보통 450 정도입니다.")]
    public float realWheelRotationHalf = 450f;

    [Tooltip("보이는 핸들이 목표 각도로 따라가는 속도입니다.")]
    public float visualSteerSpeed = 10f;

    [Header("계기판 연동")]
    [Tooltip("속도계 바늘 Transform입니다.")]
    public Transform speedNeedle;

    [Tooltip("RPM 게이지 바늘 Transform입니다.")]
    public Transform rpmNeedle;

    [Header("계기판 스펙 설정")]
    [Tooltip("속도를 제한하는 값이 아니라, 속도계 바늘이 끝까지 움직이는 기준 속도입니다. 계기판 최대 숫자와 맞추세요.")]
    public float maxSpeedForDash = 400f;

    [Tooltip("RPM 게이지의 최대 표시 RPM입니다.")]
    public float maxRpm = 7000f;

    [Tooltip("속도계 바늘이 0에서 최대 속도까지 움직이는 전체 각도입니다. 방향이 반대면 부호를 바꾸세요.")]
    public float speedSweepAngle = -260f;

    [Tooltip("RPM 바늘이 0에서 최대 RPM까지 움직이는 전체 각도입니다. 방향이 반대면 부호를 바꾸세요.")]
    public float rpmSweepAngle = -260f;

    [Header("엔진 사운드 연동")]
    [Tooltip("엔진 사운드를 제어하는 CarEngineSound 컴포넌트입니다. 비워두면 같은 오브젝트에서 자동으로 찾습니다.")]
    public CarEngineSound engineSound;

    [Header("변속기")]
    [Tooltip("플레이어 차량의 자동/수동 변속 모듈입니다. 비워두면 같은 오브젝트에서 자동으로 찾습니다.")]
    public CarTransmissionController transmission;

    [Tooltip("메인트랙 씬을 바로 재생할 때 로비 선택값을 무시하고 테스트 모드를 사용합니다.")]
    public bool useTransmissionTestOverride;

    [Tooltip("테스트 오버라이드가 켜져 있을 때 사용할 변속 모드입니다.")]
    public CarTransmissionController.TransmissionMode testTransmissionMode = CarTransmissionController.TransmissionMode.Automatic;

    [Header("디버그")]
    [Tooltip("켜면 입력값, 속도, 브레이크, 후진 상태 등을 Console에 출력합니다.")]
    public bool showInputDebug = true;

    [Header("레이스 상태")]
    [SerializeField] private bool controlsEnabled = true;

    private float steeringInput;
    private float rawSteeringInput;
    private Vector2 rawSteeringStickInput;
    private float accelInput;
    private float brakeInput;
    private bool useExternalInput;


    private Rigidbody rb;
    private float currentSpeed;
    private float currentRpm;

    private Quaternion startSpeedRot;
    private Quaternion startRpmRot;

    private float reverseHoldTimer = 0f;
    private bool reverseReady = false;

    public float CurrentSpeedKmh => rb != null ? rb.linearVelocity.magnitude * 3.6f : currentSpeed;
    public string TransmissionModeLabel => transmission != null ? transmission.ModeLabel : "Automatic";
    public string GearLabel => transmission != null ? transmission.GearLabel : "D";
    public float AccelInput => accelInput;
    public float BrakeInput => brakeInput;
    public CarTransmissionController Transmission => transmission;
    public bool IsManualTransmission => transmission != null && transmission.IsManual;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.mass = carMass;
            rb.angularDamping = angularDrag;
            rb.linearDamping = 0.02f;
            rb.centerOfMass += centerOfMassOffset;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (engineSound == null)
            engineSound = GetComponent<CarEngineSound>();

        if (transmission == null)
            transmission = GetComponent<CarTransmissionController>();

        if (transmission == null && CompareTag("Player"))
            transmission = gameObject.AddComponent<CarTransmissionController>();

        if (transmission != null && transmission.playerInput == null)
            transmission.playerInput = playerInput;

        SyncTransmissionSettings();

        SetupWheelFriction(frontLeftCollider);
        SetupWheelFriction(frontRightCollider);
        SetupWheelFriction(rearLeftCollider);
        SetupWheelFriction(rearRightCollider);

        if (speedNeedle != null)
            startSpeedRot = speedNeedle.localRotation;

        if (rpmNeedle != null)
            startRpmRot = rpmNeedle.localRotation;
    }

    void FixedUpdate()
    {
        if (!controlsEnabled)
        {
            HoldVehicleStill();
            return;
        }

        if (!useExternalInput)
        {
            ReadInputContinuously();
        }

        SyncTransmissionSettings();
        UpdateReverseTimer();

        ApplySteering();
        ApplyDriveBrakeAndCoast();
        ApplyHighSpeedAssist();
        ApplyBrakeAssist();
        ApplyNaturalCoastDeceleration();
        ApplyDownforce();
    }

    public void SetAiInput(float steer, float accel, float brake)
    {
        useExternalInput = true;
        rawSteeringStickInput = new Vector2(Mathf.Clamp(steer, -1f, 1f), 0f);
        rawSteeringInput = rawSteeringStickInput.x;
        steeringInput = ProcessSteeringInput(rawSteeringInput);
        accelInput = Mathf.Clamp01(accel);
        brakeInput = Mathf.Clamp01(brake);

        if (engineSound != null)
        {
            engineSound.SetThrottle(accelInput);
        }
    }

    public void ClearAiInput()
    {
        useExternalInput = false;
        rawSteeringStickInput = Vector2.zero;
        rawSteeringInput = 0f;
        steeringInput = 0f;
        accelInput = 0f;
        brakeInput = 0f;
    }
    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;

        if (!controlsEnabled)
        {
            HoldVehicleStill();
        }
    }

    private void HoldVehicleStill()
    {
        steeringInput = 0f;
        rawSteeringInput = 0f;
        rawSteeringStickInput = Vector2.zero;
        accelInput = 0f;
        brakeInput = 0f;
        reverseHoldTimer = 0f;
        reverseReady = false;

        if (engineSound != null)
        {
            engineSound.SetThrottle(0f);
        }

        ApplyParkBrake(frontLeftCollider);
        ApplyParkBrake(frontRightCollider);
        ApplyParkBrake(rearLeftCollider);
        ApplyParkBrake(rearRightCollider);
    }

    private void ApplyParkBrake(WheelCollider wheel)
    {
        if (wheel == null)
        {
            return;
        }

        wheel.motorTorque = 0f;
        wheel.brakeTorque = maxBrakeTorque;
    }

    void ReadInputContinuously()
    {
        if (playerInput == null || playerInput.actions == null)
            return;

        InputAction steeringAction = playerInput.actions.FindAction(steeringActionName);
        InputAction accelerateAction = playerInput.actions.FindAction(accelerateActionName);
        InputAction brakeAction = playerInput.actions.FindAction(brakeActionName);

        if (steeringAction != null)
        {
            rawSteeringStickInput = steeringAction.ReadValue<Vector2>();
            rawSteeringInput = rawSteeringStickInput.x;
            steeringInput = ProcessSteeringInput(rawSteeringInput);
        }

        if (accelerateAction != null)
        {
            float rawAccel = accelerateAction.ReadValue<float>();

            if (invertAccelerate)
                rawAccel = 1f - rawAccel;

            accelInput = Mathf.Clamp01(rawAccel);

            if (accelInput < pedalDeadZone)
                accelInput = 0f;
        }

        if (brakeAction != null)
        {
            float rawBrake = brakeAction.ReadValue<float>();

            if (invertBrake)
                rawBrake = 1f - rawBrake;

            brakeInput = Mathf.Clamp01(rawBrake);

            if (brakeInput < pedalDeadZone)
                brakeInput = 0f;
        }

        if (engineSound != null)
            engineSound.SetThrottle(accelInput);

        if (showInputDebug)
        {
            float speedKmh = rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
            float forwardSpeedKmh = rb != null ? transform.InverseTransformDirection(rb.linearVelocity).z * 3.6f : 0f;
            float rearMotor = rearLeftCollider != null ? rearLeftCollider.motorTorque : 0f;
            float rearBrake = rearLeftCollider != null ? rearLeftCollider.brakeTorque : 0f;

            Debug.Log(
                "RawStick: " + rawSteeringStickInput.ToString("F4") +
                " / RawSteerX: " + rawSteeringInput.ToString("F4") +
                " / Steer: " + steeringInput.ToString("F4") +
                " / Accel: " + accelInput.ToString("F2") +
                " / Brake: " + brakeInput.ToString("F2") +
                " / Speed: " + speedKmh.ToString("F1") +
                " / ForwardSpeed: " + forwardSpeedKmh.ToString("F1") +
                " / RearMotor: " + rearMotor.ToString("F1") +
                " / RearBrake: " + rearBrake.ToString("F1") +
                " / ReverseTimer: " + reverseHoldTimer.ToString("F2") +
                " / ReverseReady: " + reverseReady
            );
        }
    }

    float ProcessSteeringInput(float rawSteer)
    {
        if (invertSteering)
            rawSteer = -rawSteer;

        rawSteer -= steeringCenterOffset;
        rawSteer = Mathf.Clamp(rawSteer, -1f, 1f);

        if (Mathf.Abs(rawSteer) <= steeringDeadZone)
            return 0f;

        float sign = Mathf.Sign(rawSteer);
        float absValue = Mathf.Abs(rawSteer);

        absValue = (absValue - steeringDeadZone) / (1f - steeringDeadZone);
        absValue = Mathf.Clamp01(absValue);

        absValue = Mathf.Pow(absValue, steeringResponse);

        return sign * absValue;
    }

    void UpdateReverseTimer()
    {
        if (rb == null)
            return;

        float forwardSpeedKmh =
            transform.InverseTransformDirection(rb.linearVelocity).z * 3.6f;

        bool almostStopped = Mathf.Abs(forwardSpeedKmh) <= reverseReadySpeed;
        bool alreadyReversing = forwardSpeedKmh < -0.1f;
        bool brakePressed = brakeInput > pedalDeadZone;

        if (enableReverse && alreadyReversing && brakePressed)
        {
            reverseReady = true;
            reverseHoldTimer = reverseDelayTime;
            return;
        }

        if (enableReverse && almostStopped && brakePressed)
        {
            reverseHoldTimer += Time.fixedDeltaTime;
            reverseReady = reverseHoldTimer >= reverseDelayTime;
        }
        else
        {
            reverseHoldTimer = 0f;
            reverseReady = false;
        }
    }

    void ApplySteering()
    {
        if (rb == null)
            return;

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        float t = Mathf.InverseLerp(
            steerReduceStartSpeed,
            steerReduceEndSpeed,
            speedKmh
        );

        float limitedSteerAngle = Mathf.Lerp(
            maxSteerAngle,
            highSpeedSteerAngle,
            t
        );

        float currentSteerAngle = limitedSteerAngle * steeringInput;

        if (frontLeftCollider != null)
            frontLeftCollider.steerAngle = currentSteerAngle;

        if (frontRightCollider != null)
            frontRightCollider.steerAngle = currentSteerAngle;
    }

    void ApplyDriveBrakeAndCoast()
    {
        if (rb == null)
            return;

        float forwardSpeedKmh =
            transform.InverseTransformDirection(rb.linearVelocity).z * 3.6f;

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        float finalMotorTorque = 0f;
        float finalBrakeTorque = 0f;

        float curvedAccelInput = Mathf.Pow(accelInput, throttleCurve);
        float curvedBrakeInput = Mathf.Pow(brakeInput, brakeResponseCurve);

        bool movingForward = forwardSpeedKmh > reverseReadySpeed;
        bool movingBackward = forwardSpeedKmh < -0.1f;

        if (brakeInput > pedalDeadZone)
        {
            if (movingForward)
            {
                finalMotorTorque = 0f;
                finalBrakeTorque = maxBrakeTorque * curvedBrakeInput;
            }
            else if (enableReverse && movingBackward)
            {
                float reverseSpeedKmh = Mathf.Abs(forwardSpeedKmh);

                if (reverseSpeedKmh < maxReverseSpeed)
                {
                    finalMotorTorque = -reverseTorque * curvedBrakeInput;
                    finalBrakeTorque = 0f;
                }
                else
                {
                    finalMotorTorque = 0f;
                    finalBrakeTorque = 0f;
                }
            }
            else if (enableReverse && reverseReady)
            {
                finalMotorTorque = -reverseTorque * curvedBrakeInput;
                finalBrakeTorque = 0f;
            }
            else
            {
                finalMotorTorque = 0f;
                finalBrakeTorque = maxBrakeTorque * curvedBrakeInput;
            }
        }
        else if (accelInput > throttleStartThreshold)
        {
            if (movingBackward)
            {
                finalMotorTorque = 0f;
                finalBrakeTorque = maxBrakeTorque * curvedAccelInput;
            }
            else
            {
                if (IsManualTransmission)
                {
                    finalMotorTorque = transmission.CalculateManualMotorTorque(
                        motorTorque,
                        speedKmh,
                        curvedAccelInput);
                }
                else
                {
                    finalMotorTorque =
                        motorTorque *
                        accelerationMultiplier *
                        curvedAccelInput;
                }

                finalBrakeTorque = 0f;
            }
        }
        else
        {
            finalMotorTorque = 0f;

            float brakeFade = Mathf.InverseLerp(0f, coastFadeEndSpeed, speedKmh);
            finalBrakeTorque = idleWheelBrakeTorque * brakeFade;
        }

        finalMotorTorque *= GetSurfacePowerMultiplier();

        if (rearLeftCollider != null)
            rearLeftCollider.motorTorque = finalMotorTorque;

        if (rearRightCollider != null)
            rearRightCollider.motorTorque = finalMotorTorque;

        if (frontLeftCollider != null)
            frontLeftCollider.motorTorque = 0f;

        if (frontRightCollider != null)
            frontRightCollider.motorTorque = 0f;

        if (frontLeftCollider != null)
            frontLeftCollider.brakeTorque = finalBrakeTorque;

        if (frontRightCollider != null)
            frontRightCollider.brakeTorque = finalBrakeTorque;

        if (rearLeftCollider != null)
            rearLeftCollider.brakeTorque = finalBrakeTorque;

        if (rearRightCollider != null)
            rearRightCollider.brakeTorque = finalBrakeTorque;
    }

    void ApplyHighSpeedAssist()
    {
        if (rb == null || accelInput <= throttleStartThreshold)
            return;

        float forwardSpeedKmh = transform.InverseTransformDirection(rb.linearVelocity).z * 3.6f;

        if (IsManualTransmission)
        {
            float manualAssistForce = transmission.CalculateManualAssistForce(forwardSpeedKmh, accelInput);

            if (manualAssistForce > 0f)
            {
                rb.AddForce(transform.forward * manualAssistForce * GetSurfacePowerMultiplier(), ForceMode.Force);
            }

            return;
        }

        if (forwardSpeedKmh < highSpeedAssistStartKmh || forwardSpeedKmh >= highSpeedAssistMaxKmh)
            return;

        float speedFactor = Mathf.InverseLerp(highSpeedAssistStartKmh, highSpeedAssistMaxKmh, forwardSpeedKmh);
        float assistForce = highSpeedAssistForce * Mathf.Lerp(0.65f, 1f, speedFactor) * accelInput * GetSurfacePowerMultiplier();
        rb.AddForce(transform.forward * assistForce, ForceMode.Force);
    }

    private void SyncTransmissionSettings()
    {
        if (transmission == null)
        {
            return;
        }

        transmission.useTestTransmissionOverride = useTransmissionTestOverride;
        transmission.testTransmissionMode = testTransmissionMode;

        if (transmission.playerInput == null)
        {
            transmission.playerInput = playerInput;
        }
    }
    private float GetSurfacePowerMultiplier()
    {
        if (!reducePowerOffRoad)
            return 1f;

        if (IsCurrentlyOffRoad())
        {
            return offRoadPowerMultiplier;
        }

        return 1f;
    }

    public bool IsCurrentlyOffRoad()
    {
        bool hasGroundedWheel = false;

        if (IsWheelOffRoad(frontLeftCollider, ref hasGroundedWheel)
            || IsWheelOffRoad(frontRightCollider, ref hasGroundedWheel)
            || IsWheelOffRoad(rearLeftCollider, ref hasGroundedWheel)
            || IsWheelOffRoad(rearRightCollider, ref hasGroundedWheel))
        {
            return true;
        }

        return false;
    }

    private bool IsWheelOffRoad(WheelCollider wheel, ref bool hasGroundedWheel)
    {
        if (wheel == null || !wheel.GetGroundHit(out WheelHit hit))
            return false;

        hasGroundedWheel = true;
        return !IsRoadSurface(hit.collider);
    }

    private bool IsRoadSurface(Collider surface)
    {
        if (surface == null)
            return false;

        Transform current = surface.transform;

        while (current != null)
        {
            if (current.CompareTag(roadSurfaceTag))
                return true;

            current = current.parent;
        }

        return false;
    }
    void ApplyBrakeAssist()
    {
        if (rb == null)
            return;

        if (brakeInput <= pedalDeadZone)
            return;

        Vector3 velocity = rb.linearVelocity;

        if (velocity.sqrMagnitude < 0.0001f)
            return;

        float forwardSpeedKmh =
            transform.InverseTransformDirection(velocity).z * 3.6f;

        if (enableReverse && forwardSpeedKmh <= reverseReadySpeed)
            return;

        float curvedBrakeInput = Mathf.Pow(brakeInput, brakeResponseCurve);
        Vector3 brakeDirection = -velocity.normalized;

        float force = brakeAssistForce * curvedBrakeInput;

        rb.AddForce(brakeDirection * force, ForceMode.Force);
    }

    void ApplyNaturalCoastDeceleration()
    {
        if (rb == null)
            return;

        if (accelInput > throttleStartThreshold || brakeInput > pedalDeadZone)
            return;

        Vector3 velocity = rb.linearVelocity;

        if (velocity.sqrMagnitude < 0.0001f)
            return;

        float speed = velocity.magnitude;
        float speedKmh = speed * 3.6f;

        float coastFade = Mathf.InverseLerp(
            coastFadeStartSpeed,
            coastFadeEndSpeed,
            speedKmh
        );

        coastFade = Mathf.SmoothStep(0f, 1f, coastFade);

        Vector3 oppositeDirection = -velocity.normalized;

        float rollingForce = rollingResistance * coastFade;
        float engineBrake = engineBrakeForce * coastFade;
        float airDragForce = airResistance * speed * speed;

        float totalDecelerationForce =
            rollingForce +
            engineBrake +
            airDragForce;

        rb.AddForce(oppositeDirection * totalDecelerationForce, ForceMode.Force);
    }

    void ClearWheelForces()
    {
        if (frontLeftCollider != null)
        {
            frontLeftCollider.motorTorque = 0f;
            frontLeftCollider.brakeTorque = 0f;
        }

        if (frontRightCollider != null)
        {
            frontRightCollider.motorTorque = 0f;
            frontRightCollider.brakeTorque = 0f;
        }

        if (rearLeftCollider != null)
        {
            rearLeftCollider.motorTorque = 0f;
            rearLeftCollider.brakeTorque = 0f;
        }

        if (rearRightCollider != null)
        {
            rearRightCollider.motorTorque = 0f;
            rearRightCollider.brakeTorque = 0f;
        }
    }

    void ApplyDownforce()
    {
        if (rb == null)
            return;

        float speed = rb.linearVelocity.magnitude;
        rb.AddForce(-transform.up * downforce * speed, ForceMode.Force);
    }

    void SetupWheelFriction(WheelCollider wheel)
    {
        if (wheel == null)
            return;

        WheelFrictionCurve forward = wheel.forwardFriction;
        forward.extremumSlip = 0.4f;
        forward.extremumValue = 1f;
        forward.asymptoteSlip = 0.8f;
        forward.asymptoteValue = 0.5f;
        forward.stiffness = forwardStiffness;
        wheel.forwardFriction = forward;

        WheelFrictionCurve sideways = wheel.sidewaysFriction;
        sideways.extremumSlip = 0.2f;
        sideways.extremumValue = 1f;
        sideways.asymptoteSlip = 0.5f;
        sideways.asymptoteValue = 0.75f;
        sideways.stiffness = sidewaysStiffness;
        wheel.sidewaysFriction = sideways;

        JointSpring spring = wheel.suspensionSpring;
        spring.spring = 35000f;
        spring.damper = 5000f;
        spring.targetPosition = 0.5f;
        wheel.suspensionSpring = spring;

        wheel.suspensionDistance = 0.2f;
        wheel.mass = 20f;
    }

    void Update()
    {
        UpdateWheel(frontLeftCollider, frontLeftMesh);
        UpdateWheel(frontRightCollider, frontRightMesh);
        UpdateWheel(rearLeftCollider, rearLeftMesh);
        UpdateWheel(rearRightCollider, rearRightMesh);

        UpdateSteeringWheel();
        UpdateDashBoard();
    }

    void UpdateWheel(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null)
            return;

        Vector3 pos;
        Quaternion rot;

        col.GetWorldPose(out pos, out rot);

        mesh.position = pos;
        mesh.rotation = rot;
    }

    void UpdateSteeringWheel()
    {
        if (steeringWheelMesh == null)
            return;

        float targetAngle = steeringInput * realWheelRotationHalf;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, -targetAngle);

        steeringWheelMesh.localRotation = Quaternion.Lerp(
            steeringWheelMesh.localRotation,
            targetRotation,
            Time.deltaTime * visualSteerSpeed
        );
    }

    void UpdateDashBoard()
    {
        if (rb == null)
            return;

        currentSpeed = rb.linearVelocity.magnitude * 3.6f;

        float targetRpm = (currentSpeed / maxSpeedForDash) * maxRpm;

        if (accelInput > 0f)
            targetRpm += 4200f * accelInput;

        currentRpm = Mathf.Lerp(
            currentRpm,
            Mathf.Clamp(targetRpm, 800f, maxRpm),
            Time.deltaTime * 5f
        );

        if (speedNeedle != null)
        {
            float speedPercent = Mathf.Clamp01(currentSpeed / maxSpeedForDash);
            float currentAngle = speedSweepAngle * speedPercent;

            speedNeedle.localRotation =
                startSpeedRot * Quaternion.Euler(0f, 0f, currentAngle);
        }

        if (rpmNeedle != null)
        {
            float rpmPercent = Mathf.Clamp01(currentRpm / maxRpm);
            float currentAngle = rpmSweepAngle * rpmPercent;

            rpmNeedle.localRotation =
                startRpmRot * Quaternion.Euler(0f, 0f, currentAngle);
        }
    }

    void OnSteering(InputValue value)
    {
        rawSteeringStickInput = value.Get<Vector2>();
        rawSteeringInput = rawSteeringStickInput.x;
        steeringInput = ProcessSteeringInput(rawSteeringInput);
    }

    void OnAccelerate(InputValue value)
    {
        float raw = value.Get<float>();

        if (invertAccelerate)
            raw = 1f - raw;

        accelInput = Mathf.Clamp01(raw);

        if (accelInput < pedalDeadZone)
            accelInput = 0f;
    }

    void OnBrake(InputValue value)
    {
        float raw = value.Get<float>();

        if (invertBrake)
            raw = 1f - raw;

        brakeInput = Mathf.Clamp01(raw);

        if (brakeInput < pedalDeadZone)
            brakeInput = 0f;
    }
}

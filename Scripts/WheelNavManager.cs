using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class WheelNavManager : MonoBehaviour
{
    [Header("조작할 카드 슬롯들 (순서대로)")]
    public GameObject[] cardSlots;

    [Header("UI 방해 오브젝트 제거 설정")]
    public GameObject[] carLights;

    [Header("로비 차 라이트 설정")]
    [SerializeField] private bool applyCarLightSettings = true;
    [SerializeField] private Color carLightColor = Color.white;
    [SerializeField] private float carLightIntensity = 12f;
    [SerializeField] private float carLightRange = 30f;
    [SerializeField] private float carLightSpotAngle = 38f;
    [SerializeField] private float carLightInnerSpotAngle = 24f;

    [Header("시동 연출 대기 시간 (초)")]
    [Tooltip("게임 시작 후 몇 초 뒤에 렌즈 플레어를 끌지 설정합니다.")]
    public float delayTime = 2.0f; // ?? 2초 뒤에 꺼지도록 설정 (원하는 초로 인스펙터에서 수정 가능)

    [Header("뉴 인풋 액션 연결")]
    public InputActionReference leftAction;
    public InputActionReference rightAction;
    public InputActionReference submitAction;

    [Header("G29 십자키 직접 입력")]
    [SerializeField] private float dpadDeadZone = 0.5f;

    private int currentIndex = 0;
    private int previousDpadDirection = 0;

    void Start()
    {
        ApplyCarLightSettings();

        // ? 처음에 바로 끄지 않고, 지정한 시간(delayTime) 뒤에 끄도록 예약합니다!
        Invoke("DisableLightsAfterDelay", delayTime);

        // 씬이 켜지면 0.1초 뒤 첫 번째 카드를 자동으로 선택(포커스)합니다.
        Invoke("SelectInitialCard", 0.1f);
    }

    // 설정한 시간이 지나면 호출되는 함수
    void DisableLightsAfterDelay()
    {
        ToggleCarLights(false);
    }

    void OnEnable()
    {
        if (leftAction != null) leftAction.action.Enable();
        if (rightAction != null) rightAction.action.Enable();
        if (submitAction != null) submitAction.action.Enable();
    }

    void SelectInitialCard()
    {
        if (LobbyLicensePrompt.IsShowing)
        {
            return;
        }

        if (cardSlots.Length > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(cardSlots[currentIndex]);
        }
    }

    void Update()
    {
        if (LobbyLicensePrompt.IsShowing)
        {
            previousDpadDirection = 0;
            return;
        }

        if (cardSlots.Length == 0 || EventSystem.current == null) return;

        int dpadDirection = ReadDpadDirection();
        if (dpadDirection != 0)
        {
            MoveSelection(dpadDirection);
            return;
        }

        // 1. 왼쪽 버튼 조작
        if (leftAction != null && leftAction.action.WasPressedThisFrame())
        {
            MoveSelection(-1);
        }

        // 2. 오른쪽 버튼 조작
        if (rightAction != null && rightAction.action.WasPressedThisFrame())
        {
            MoveSelection(1);
        }

        // 3. 결정 버튼 조작
        if (submitAction != null && submitAction.action.WasPressedThisFrame())
        {
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            if (currentSelected != null)
            {
                // 게임 레이싱 씬으로 넘어갈 때 라이트를 다시 켜줍니다.
                ToggleCarLights(true);
                ExecuteEvents.Execute(currentSelected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            }
        }
    }

    private int ReadDpadDirection()
    {
        Vector2 value = Vector2.zero;

        Joystick joystick = Joystick.current;
        if (joystick != null)
        {
            Vector2Control dpad = joystick.TryGetChildControl<Vector2Control>("dpad");
            if (dpad == null)
            {
                dpad = joystick.TryGetChildControl<Vector2Control>("hat");
            }

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

    private void MoveSelection(int direction)
    {
        currentIndex += direction;

        if (currentIndex < 0)
        {
            currentIndex = cardSlots.Length - 1;
        }
        else if (currentIndex >= cardSlots.Length)
        {
            currentIndex = 0;
        }

        EventSystem.current.SetSelectedGameObject(cardSlots[currentIndex]);
    }
    private void ToggleCarLights(bool state)
    {
        if (carLights == null || carLights.Length == 0) return;
        foreach (GameObject lightObj in carLights)
        {
            if (lightObj != null)
            {
                lightObj.SetActive(state);
                ApplyCarLightSettings(lightObj);
            }
        }
    }

    [ContextMenu("Apply Car Light Settings")]
    private void ApplyCarLightSettings()
    {
        if (!applyCarLightSettings || carLights == null)
        {
            return;
        }

        foreach (GameObject lightObj in carLights)
        {
            ApplyCarLightSettings(lightObj);
        }
    }

    private void ApplyCarLightSettings(GameObject lightObj)
    {
        if (!applyCarLightSettings || lightObj == null)
        {
            return;
        }

        Light[] lights = lightObj.GetComponentsInChildren<Light>(true);

        foreach (Light targetLight in lights)
        {
            if (targetLight == null)
            {
                continue;
            }

            targetLight.color = carLightColor;
            targetLight.intensity = carLightIntensity;
            targetLight.range = carLightRange;
            targetLight.spotAngle = carLightSpotAngle;
            targetLight.innerSpotAngle = Mathf.Min(carLightInnerSpotAngle, carLightSpotAngle);
        }
    }

    private void OnValidate()
    {
        ApplyCarLightSettings();
    }
}

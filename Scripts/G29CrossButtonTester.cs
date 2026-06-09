using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class G29CrossButtonTester : MonoBehaviour
{
    [Header("Button Scan")]
    [SerializeField] private int maxButtonIndex = 32;
    [SerializeField] private int expectedCrossButtonIndex = 0;

    [Header("D-Pad Scan")]
    [SerializeField] private float dpadDeadZone = 0.5f;

    private readonly Dictionary<string, bool> previousPressedStates = new Dictionary<string, bool>();
    private Vector2 previousDpadValue;
    private bool announcedDevice;

    private void Update()
    {
        Joystick joystick = Joystick.current;
        Gamepad gamepad = Gamepad.current;

        if (joystick == null && gamepad == null)
        {
            return;
        }

        AnnounceDevice(joystick, gamepad);

        if (joystick != null)
        {
            ScanJoystickButtons(joystick);
            ScanJoystickDpad(joystick);
        }

        if (gamepad != null)
        {
            ScanGamepadDpad(gamepad);
        }
    }

    private void AnnounceDevice(Joystick joystick, Gamepad gamepad)
    {
        if (announcedDevice)
        {
            return;
        }

        announcedDevice = true;

        if (joystick != null)
        {
            Debug.Log($"[G29 Cross Test] Joystick detected: {joystick.displayName} ({joystick.layout})");
        }

        if (gamepad != null)
        {
            Debug.Log($"[G29 Cross Test] Gamepad detected: {gamepad.displayName} ({gamepad.layout})");
        }
    }

    private void ScanJoystickButtons(Joystick joystick)
    {
        for (int i = 0; i < maxButtonIndex; i++)
        {
            ButtonControl button = joystick.TryGetChildControl<ButtonControl>($"button{i}");

            if (button == null)
            {
                continue;
            }

            bool isPressed = button.isPressed;
            string key = $"{joystick.deviceId}:button{i}";
            bool wasPressed = previousPressedStates.TryGetValue(key, out bool previous) && previous;

            if (isPressed && !wasPressed)
            {
                string label = i == expectedCrossButtonIndex ? "EXPECTED CROSS" : "button";
                Debug.Log($"[G29 Cross Test] {label} pressed: button{i}");
            }

            if (!isPressed && wasPressed)
            {
                Debug.Log($"[G29 Cross Test] button released: button{i}");
            }

            previousPressedStates[key] = isPressed;
        }
    }

    private void ScanJoystickDpad(Joystick joystick)
    {
        Vector2Control dpad = joystick.TryGetChildControl<Vector2Control>("dpad");

        if (dpad == null)
        {
            dpad = joystick.TryGetChildControl<Vector2Control>("hat");
        }

        if (dpad == null)
        {
            return;
        }

        LogDpadIfChanged("Joystick", dpad.ReadValue());
    }

    private void ScanGamepadDpad(Gamepad gamepad)
    {
        if (gamepad.dpad == null)
        {
            return;
        }

        LogDpadIfChanged("Gamepad", gamepad.dpad.ReadValue());
    }

    private void LogDpadIfChanged(string source, Vector2 value)
    {
        Vector2 cleanedValue = new Vector2(
            Mathf.Abs(value.x) >= dpadDeadZone ? Mathf.Sign(value.x) : 0f,
            Mathf.Abs(value.y) >= dpadDeadZone ? Mathf.Sign(value.y) : 0f);

        if (cleanedValue == previousDpadValue)
        {
            return;
        }

        previousDpadValue = cleanedValue;

        if (cleanedValue == Vector2.zero)
        {
            Debug.Log($"[G29 Cross Test] {source} D-Pad released");
            return;
        }

        Debug.Log($"[G29 Cross Test] {source} D-Pad pressed: {cleanedValue}");
    }
}

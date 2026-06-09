using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class G29ButtonLogger : MonoBehaviour
{
    [SerializeField] private int maxButtonIndex = 20;

    private void Update()
    {
        Joystick joystick = Joystick.current;

        if (joystick == null)
        {
            return;
        }

        for (int i = 0; i < maxButtonIndex; i++)
        {
            ButtonControl button = joystick.TryGetChildControl<ButtonControl>($"button{i}");

            if (button != null && button.wasPressedThisFrame)
            {
                Debug.Log($"G29 button pressed: button{i}");
            }
        }
    }
}

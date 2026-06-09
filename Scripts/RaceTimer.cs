using UnityEngine;
using TMPro;

public class RaceTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;  // 인스펙터에서 TimerText 연결

    private float elapsedTime = 0f;
    private bool isRunning = false;

    void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        timerText.text = FormatTime(elapsedTime);
    }

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        int milliseconds = (int)((time * 100) % 100);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }

    public void StartTimer() => isRunning = true;
    public void StopTimer() => isRunning = false;
    public void ResetTimer() { elapsedTime = 0f; isRunning = false; }
    public float GetTime() => elapsedTime;
}
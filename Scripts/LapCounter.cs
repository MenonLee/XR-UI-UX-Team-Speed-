using UnityEngine;
using TMPro;

public class LapCounter : MonoBehaviour
{
    public TextMeshProUGUI lapText;
    public int totalLaps = 3;

    private int currentLap = 1;
    private float cooldown = 0f;
    public float cooldownTime = 3f;  // 3초 안에 중복 감지 방지

    void Start()
    {
        UpdateLapText();
    }

    void Update()
    {
        if (cooldown > 0f)
            cooldown -= Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") &&
            !other.transform.root.CompareTag("Player")) return;

        if (cooldown > 0f) return;  // 쿨다운 중이면 무시

        cooldown = cooldownTime;    // 쿨다운 시작

        if (currentLap < totalLaps)
        {
            currentLap++;
            UpdateLapText();
        }
        else
        {
            lapText.text = "FINISH!";
        }
    }

    void UpdateLapText()
    {
        lapText.text = $"LAP {currentLap} / {totalLaps}";
    }

    public int GetCurrentLap() => currentLap;
}
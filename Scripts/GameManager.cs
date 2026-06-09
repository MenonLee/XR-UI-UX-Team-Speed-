using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public RaceTimer raceTimer;
    public LapCounter lapCounter;
    public RacePosition racePosition;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartRace();
    }

    public void StartRace()
    {
        raceTimer.StartTimer();
        Debug.Log("레이스 시작!");
    }

    public void EndRace()
    {
        raceTimer.StopTimer();
        Debug.Log($"레이스 종료! 기록: {raceTimer.GetTime():F2}초");
    }
}
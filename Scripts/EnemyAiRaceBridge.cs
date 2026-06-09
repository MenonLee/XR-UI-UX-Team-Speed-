using System.Collections;
using System.Collections.Generic;
using ALIyerEdon;
using UnityEngine;

public class EnemyAiRaceBridge : MonoBehaviour
{
    [SerializeField] private RaceSessionManager raceSessionManager;
    [SerializeField] private bool autoFindAiCars = true;
    [SerializeField] private bool registerAiCarsForHudPosition = true;
    [SerializeField] private bool releaseClutchOnRaceStart = true;
    [SerializeField] private bool stopAiOnRaceEnd = true;
    [SerializeField] private float reverseCheckDelay = 2f;

    private readonly List<Car_AI> aiCars = new List<Car_AI>();
    private Coroutine reverseCheckRoutine;

    private void Awake()
    {
        if (raceSessionManager == null)
        {
            raceSessionManager = FindObjectOfType<RaceSessionManager>();
        }

        RefreshAiCars();
        SetAiRaceStarted(false);
        RegisterOpponents();
    }

    private void OnEnable()
    {
        if (raceSessionManager == null)
        {
            raceSessionManager = FindObjectOfType<RaceSessionManager>();
        }

        if (raceSessionManager == null)
        {
            return;
        }

        raceSessionManager.RaceBegan += StartAiRace;
        raceSessionManager.RaceEnded += StopAiRace;

        if (raceSessionManager.RaceStarted && !raceSessionManager.RaceFinished)
        {
            StartAiRace();
        }
    }

    private void OnDisable()
    {
        if (raceSessionManager == null)
        {
            return;
        }

        raceSessionManager.RaceBegan -= StartAiRace;
        raceSessionManager.RaceEnded -= StopAiRace;
    }

    public void RefreshAiCars()
    {
        aiCars.Clear();

        if (!autoFindAiCars)
        {
            return;
        }

        Car_AI[] foundAiCars = FindObjectsOfType<Car_AI>();

        for (int i = 0; i < foundAiCars.Length; i++)
        {
            if (foundAiCars[i] != null && !foundAiCars[i].CompareTag("Player"))
            {
                aiCars.Add(foundAiCars[i]);
            }
        }
    }

    private void StartAiRace()
    {
        RefreshAiCars();
        RegisterOpponents();
        SetAiRaceStarted(true);

        if (reverseCheckRoutine != null)
        {
            StopCoroutine(reverseCheckRoutine);
        }

        reverseCheckRoutine = StartCoroutine(EnableReverseCheckAfterDelay());
    }

    private void StopAiRace()
    {
        if (!stopAiOnRaceEnd)
        {
            return;
        }

        if (reverseCheckRoutine != null)
        {
            StopCoroutine(reverseCheckRoutine);
            reverseCheckRoutine = null;
        }

        SetAiRaceStarted(false);
    }

    private void SetAiRaceStarted(bool started)
    {
        for (int i = 0; i < aiCars.Count; i++)
        {
            Car_AI ai = aiCars[i];

            if (ai == null)
            {
                continue;
            }

            ai.raceStarted = started;
            ai.canReverseCheck = false;

            EasyCarController controller = ai.GetComponent<EasyCarController>();

            if (controller == null)
            {
                continue;
            }

            controller.throttleInput = started ? controller.throttleInput : 0f;
            controller.steerInput = 0f;

            if (releaseClutchOnRaceStart)
            {
                controller.Clutch = !started;
            }
        }
    }

    private IEnumerator EnableReverseCheckAfterDelay()
    {
        yield return new WaitForSeconds(reverseCheckDelay);

        for (int i = 0; i < aiCars.Count; i++)
        {
            if (aiCars[i] != null)
            {
                aiCars[i].canReverseCheck = true;
            }
        }

        reverseCheckRoutine = null;
    }

    private void RegisterOpponents()
    {
        if (!registerAiCarsForHudPosition || raceSessionManager == null)
        {
            return;
        }

        List<Transform> opponentCars = new List<Transform>();

        for (int i = 0; i < aiCars.Count; i++)
        {
            if (aiCars[i] != null)
            {
                opponentCars.Add(aiCars[i].transform);
            }
        }

        if (opponentCars.Count == 0)
        {
            return;
        }

        raceSessionManager.SetOpponentCars(opponentCars.ToArray());
    }
}

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FinishLineTrigger : MonoBehaviour
{
    [SerializeField] private RaceSessionManager raceSessionManager;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float minimumSecondsBetweenPasses = 2f;

    private readonly Dictionary<Transform, float> lastPassTimes = new Dictionary<Transform, float>();

    private void Awake()
    {
        if (raceSessionManager == null)
        {
            raceSessionManager = FindObjectOfType<RaceSessionManager>();
        }

        if (raceSessionManager != null)
        {
            raceSessionManager.SetFinishLine(transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EnsureRaceSessionManager();

        Transform raceCar = GetRaceCarRoot(other);

        if (raceCar == null)
        {
            return;
        }

        if (lastPassTimes.TryGetValue(raceCar, out float lastPassTime)
            && Time.realtimeSinceStartup - lastPassTime < minimumSecondsBetweenPasses)
        {
            return;
        }

        lastPassTimes[raceCar] = Time.realtimeSinceStartup;

        if (raceSessionManager != null)
        {
            raceSessionManager.RegisterFinishLinePass(raceCar);
        }
    }

    private Transform GetRaceCarRoot(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            return other.transform.root;
        }

        CarControllerWaypointAi ai = other.GetComponentInParent<CarControllerWaypointAi>();

        if (ai != null)
        {
            return ai.transform.root;
        }

        return null;
    }

    private void EnsureRaceSessionManager()
    {
        if (raceSessionManager != null)
        {
            return;
        }

        raceSessionManager = FindObjectOfType<RaceSessionManager>();

        if (raceSessionManager != null)
        {
            raceSessionManager.SetFinishLine(transform);
        }
    }

    private void Reset()
    {
        BoxCollider triggerCollider = GetComponent<BoxCollider>();

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(8f, 4f, 1.5f);
        }
    }
}

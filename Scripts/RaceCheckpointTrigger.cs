using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RaceCheckpointTrigger : MonoBehaviour
{
    [SerializeField] private RaceSessionManager raceSessionManager;
    [SerializeField] private int checkpointIndex;
    [SerializeField] private int totalCheckpoints;
    [SerializeField] private string playerTag = "Player";

    public void Configure(RaceSessionManager manager, int index, int total)
    {
        raceSessionManager = manager;
        checkpointIndex = index;
        totalCheckpoints = total;
    }

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();

        if (trigger != null)
        {
            trigger.isTrigger = true;
        }

        if (raceSessionManager == null)
        {
            raceSessionManager = FindObjectOfType<RaceSessionManager>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (raceSessionManager == null)
        {
            raceSessionManager = FindObjectOfType<RaceSessionManager>();
        }

        if (raceSessionManager == null)
        {
            return;
        }

        Transform car = GetRaceCarRoot(other);

        if (car == null)
        {
            return;
        }

        raceSessionManager.RegisterCheckpointPass(car, checkpointIndex, totalCheckpoints);
    }

    private Transform GetRaceCarRoot(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            return other.transform.root;
        }

        CarController carController = other.GetComponentInParent<CarController>();

        if (carController != null)
        {
            return carController.transform.root;
        }

        CarControllerWaypointAi ai = other.GetComponentInParent<CarControllerWaypointAi>();

        if (ai != null)
        {
            return ai.transform.root;
        }

        return null;
    }
}

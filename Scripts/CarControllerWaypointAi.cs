using System.Collections.Generic;
using ALIyerEdon;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class CarControllerWaypointAi : MonoBehaviour
{
    [SerializeField] private string pathName = "Path_1";
    [SerializeField] private float waypointReachDistance = 18f;
    [SerializeField] private float throttle = 0.9f;
    [SerializeField] private float cornerSlowSteer = 0.45f;
    [SerializeField] private float cornerBrakeSteer = 0.75f;
    [SerializeField] private float lookAheadWaypoints = 1f;
    [SerializeField] private float minLookAheadWaypoints = 1f;
    [SerializeField] private float maxLookAheadWaypoints = 2f;
    [SerializeField] private float lookAheadFullSpeedKmh = 180f;
    [SerializeField] private float steeringSmooth = 8f;
    [SerializeField] private float straightTargetSpeedKmh = 190f;
    [SerializeField] private float sharpCornerTargetSpeedKmh = 58f;
    [SerializeField] private float cornerPredictionWaypoints = 3f;
    [SerializeField] private float cornerBrakeStrength = 0.6f;
    [SerializeField] private float overspeedBrakeMarginKmh = 8f;
    [SerializeField] private float routeRecoveryDistance = 45f;
    [SerializeField] private float stuckSpeedKmh = 4f;
    [SerializeField] private float stuckDetectTime = 2.5f;
    [SerializeField] private float reverseRecoverTime = 1.2f;
    [SerializeField] private float hardResetTime = 6f;
    [SerializeField] private float resetHeightOffset = 0.4f;
    [SerializeField] private bool useStrongCatchUp = true;
    [SerializeField] private float catchUpStartDistance = 0f;
    [SerializeField] private float catchUpMaxDistance = 160f;
    [SerializeField] private float catchUpMaxBoost = 0.85f;
    [SerializeField] private float catchUpMinBoost = 0.32f;
    [SerializeField] private float catchUpMinCornerFactor = 0.45f;

    private readonly List<Transform> waypoints = new List<Transform>();
    private CarController carController;
    private Rigidbody rb;
    private Transform player;
    private int currentWaypoint;
    private bool raceStarted;
    private float stuckTimer;
    private float reverseTimer;
    private float routeCheckTimer;
    private Vector3 lastHealthyPosition;
    private Quaternion lastHealthyRotation;
    private float smoothedSteer;

    public int CurrentWaypointIndex => currentWaypoint;
    public int WaypointCount => waypoints.Count;
    public string PathName => pathName;
    public float RouteProgress01 => waypoints.Count > 0 ? Mathf.Clamp01(currentWaypoint / (float)waypoints.Count) : 0f;

    private void Awake()
    {
        EnsureReferences();
        ApplyStrongCatchUpDefaults();
        LoadPath();

        if (carController != null)
        {
            carController.SetControlsEnabled(false);
        }
    }

    private void FixedUpdate()
    {
        if (!raceStarted || waypoints.Count == 0)
        {
            return;
        }

        EnsureReferences();

        if (carController == null)
        {
            return;
        }

        RecoverRouteIfNeeded();
        UpdateHealthyPose();

        if (HandleStuckRecovery())
        {
            return;
        }

        AdvanceWaypointIfNeeded();

        float speedKmh = rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
        int targetIndex = GetLookAheadTargetIndex(speedKmh);
        Transform target = waypoints[targetIndex];

        if (target == null)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
            return;
        }

        Vector3 localTarget = transform.InverseTransformPoint(new Vector3(
            target.position.x,
            transform.position.y,
            target.position.z));

        float targetSteer = Mathf.Clamp(localTarget.x / Mathf.Max(1f, localTarget.magnitude), -1f, 1f);
        float steerLerp = 1f - Mathf.Exp(-steeringSmooth * Time.fixedDeltaTime);
        smoothedSteer = Mathf.Lerp(smoothedSteer, targetSteer, steerLerp);

        float steer = Mathf.Clamp(smoothedSteer, -1f, 1f);
        float absSteer = Mathf.Abs(steer);
        float cornerSeverity = CalculateCornerSeverity();
        float catchUpBoost = CalculateCatchUpBoost(absSteer);
        float targetSpeed = Mathf.Lerp(straightTargetSpeedKmh, sharpCornerTargetSpeedKmh, cornerSeverity);
        float overspeed = speedKmh - targetSpeed;
        float speedFactor = Mathf.InverseLerp(targetSpeed + overspeedBrakeMarginKmh, targetSpeed - 20f, speedKmh);
        float cornerThrottleFactor = Mathf.Lerp(1f, 0.42f, cornerSeverity);
        float accel = throttle * cornerThrottleFactor * speedFactor + catchUpBoost;
        float brake = 0f;

        if (overspeed > overspeedBrakeMarginKmh || absSteer > cornerBrakeSteer)
        {
            float overspeedFactor = Mathf.InverseLerp(overspeedBrakeMarginKmh, 45f, overspeed);
            brake = Mathf.Clamp01(Mathf.Max(overspeedFactor, absSteer > cornerBrakeSteer ? cornerBrakeStrength : 0f));
            accel *= Mathf.Lerp(1f, 0.35f, brake);
        }

        carController.SetAiInput(steer, Mathf.Clamp01(accel), brake);
    }

    public void ConfigurePath(string newPathName)
    {
        pathName = newPathName;
        LoadPath();
    }

    public void SetPlayerTarget(Transform target)
    {
        player = target;
    }

    public void SetRaceStarted(bool started)
    {
        EnsureReferences();

        raceStarted = started;

        if (carController == null)
        {
            return;
        }

        carController.SetControlsEnabled(started);

        if (!started)
        {
            carController.ClearAiInput();
            stuckTimer = 0f;
            reverseTimer = 0f;
        }
    }

    private void LoadPath()
    {
        waypoints.Clear();
        currentWaypoint = 0;

        GameObject pathObject = GameObject.Find(pathName);

        if (pathObject == null)
        {
            Debug.LogWarning($"[CarControllerWaypointAi] Path '{pathName}' not found.", this);
            return;
        }

        Waypoint_System path = pathObject.GetComponent<Waypoint_System>();

        if (path != null && path.waypoints.Count > 0)
        {
            waypoints.AddRange(path.waypoints);
            currentWaypoint = FindClosestWaypointIndex();
            return;
        }

        foreach (Transform child in pathObject.transform)
        {
            waypoints.Add(child);
        }

        currentWaypoint = FindClosestWaypointIndex();
        UpdateHealthyPose();
    }

    private void EnsureReferences()
    {
        if (carController == null)
        {
            carController = GetComponent<CarController>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private int FindClosestWaypointIndex()
    {
        int closest = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(transform.position - waypoints[i].position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = i;
            }
        }

        return closest;
    }

    private void AdvanceWaypointIfNeeded()
    {
        if (waypoints[currentWaypoint] == null)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
            return;
        }

        Vector3 flatDelta = waypoints[currentWaypoint].position - transform.position;
        flatDelta.y = 0f;

        float speedKmh = rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
        float reachDistance = waypointReachDistance + Mathf.Clamp(speedKmh * 0.08f, 0f, 16f);

        if (flatDelta.magnitude <= reachDistance)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
        }
    }

    private void RecoverRouteIfNeeded()
    {
        routeCheckTimer -= Time.fixedDeltaTime;

        if (routeCheckTimer > 0f || waypoints.Count == 0)
        {
            return;
        }

        routeCheckTimer = 0.5f;

        int closest = FindClosestWaypointIndex();

        if (waypoints[closest] == null)
        {
            return;
        }

        Vector3 flatDelta = waypoints[closest].position - transform.position;
        flatDelta.y = 0f;

        if (flatDelta.sqrMagnitude > routeRecoveryDistance * routeRecoveryDistance)
        {
            currentWaypoint = (closest + 1) % waypoints.Count;
            smoothedSteer = 0f;
        }
    }

    private bool HandleStuckRecovery()
    {
        if (rb == null || waypoints.Count == 0)
        {
            return false;
        }

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        if (speedKmh > stuckSpeedKmh)
        {
            stuckTimer = 0f;
            reverseTimer = 0f;
            return false;
        }

        stuckTimer += Time.fixedDeltaTime;

        if (stuckTimer < stuckDetectTime)
        {
            return false;
        }

        if (stuckTimer >= hardResetTime)
        {
            HardResetToRoute();
            stuckTimer = 0f;
            reverseTimer = 0f;
            return true;
        }

        reverseTimer += Time.fixedDeltaTime;

        if (reverseTimer <= reverseRecoverTime)
        {
            float steer = Mathf.Sin(Time.time * 3f) > 0f ? 0.65f : -0.65f;
            carController.SetAiInput(steer, 0f, 1f);
            return true;
        }

        return false;
    }

    private void HardResetToRoute()
    {
        int closest = FindClosestWaypointIndex();
        int next = (closest + 1) % waypoints.Count;

        if (waypoints[closest] == null || waypoints[next] == null)
        {
            transform.SetPositionAndRotation(lastHealthyPosition, lastHealthyRotation);
        }
        else
        {
            Vector3 position = waypoints[closest].position + Vector3.up * resetHeightOffset;
            Vector3 direction = waypoints[next].position - waypoints[closest].position;
            direction.y = 0f;

            Quaternion rotation = direction.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : lastHealthyRotation;

            transform.SetPositionAndRotation(position, rotation);
            currentWaypoint = next;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void UpdateHealthyPose()
    {
        if (rb == null)
        {
            return;
        }

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        if (speedKmh > stuckSpeedKmh && Vector3.Dot(transform.up, Vector3.up) > 0.55f)
        {
            lastHealthyPosition = transform.position;
            lastHealthyRotation = transform.rotation;
        }
    }

    private float CalculateCatchUpBoost(float absSteer)
    {
        if (player == null)
        {
            return 0f;
        }

        Vector3 playerLocal = transform.InverseTransformPoint(player.position);

        if (playerLocal.z <= 0f)
        {
            return 0f;
        }

        float distanceFactor = Mathf.InverseLerp(catchUpStartDistance, catchUpMaxDistance, playerLocal.z);
        float cornerFactor = Mathf.Lerp(catchUpMinCornerFactor, 1f, Mathf.InverseLerp(cornerBrakeSteer, 0f, absSteer));

        return Mathf.Lerp(catchUpMinBoost, catchUpMaxBoost, distanceFactor) * cornerFactor;
    }

    private int GetLookAheadTargetIndex(float speedKmh)
    {
        int step = Mathf.Max(1, Mathf.RoundToInt(GetDynamicLookAheadWaypoints(speedKmh)));
        return (currentWaypoint + step) % waypoints.Count;
    }

    private float GetDynamicLookAheadWaypoints(float speedKmh)
    {
        float speedFactor = Mathf.InverseLerp(0f, lookAheadFullSpeedKmh, speedKmh);
        float dynamicLookAhead = Mathf.Lerp(minLookAheadWaypoints, maxLookAheadWaypoints, speedFactor);
        return Mathf.Max(lookAheadWaypoints, dynamicLookAhead);
    }

    private float CalculateCornerSeverity()
    {
        if (waypoints.Count < 3)
        {
            return 0f;
        }

        Transform current = waypoints[currentWaypoint];
        Transform next = waypoints[(currentWaypoint + 1) % waypoints.Count];
        Transform future = waypoints[(currentWaypoint + Mathf.Max(2, Mathf.RoundToInt(cornerPredictionWaypoints))) % waypoints.Count];

        if (current == null || next == null || future == null)
        {
            return 0f;
        }

        Vector3 firstLeg = next.position - current.position;
        Vector3 secondLeg = future.position - next.position;
        firstLeg.y = 0f;
        secondLeg.y = 0f;

        if (firstLeg.sqrMagnitude < 0.01f || secondLeg.sqrMagnitude < 0.01f)
        {
            return 0f;
        }

        float angle = Vector3.Angle(firstLeg.normalized, secondLeg.normalized);
        return Mathf.InverseLerp(10f, 85f, angle);
    }

    private void ApplyStrongCatchUpDefaults()
    {
        if (!useStrongCatchUp)
        {
            return;
        }

        throttle = Mathf.Max(throttle, 1f);
        minLookAheadWaypoints = Mathf.Max(minLookAheadWaypoints, 1f);
        maxLookAheadWaypoints = Mathf.Clamp(maxLookAheadWaypoints, 1f, 2f);
        straightTargetSpeedKmh = Mathf.Max(straightTargetSpeedKmh, 190f);
        sharpCornerTargetSpeedKmh = Mathf.Min(sharpCornerTargetSpeedKmh, 58f);
        steeringSmooth = Mathf.Max(steeringSmooth, 8f);
        catchUpStartDistance = Mathf.Min(catchUpStartDistance, 0f);
        catchUpMaxDistance = Mathf.Max(catchUpMaxDistance, 160f);
        catchUpMaxBoost = Mathf.Max(catchUpMaxBoost, 0.85f);
        catchUpMinBoost = Mathf.Max(catchUpMinBoost, 0.32f);
        catchUpMinCornerFactor = Mathf.Max(catchUpMinCornerFactor, 0.45f);
    }
}

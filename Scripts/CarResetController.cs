using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CarResetController : MonoBehaviour
{
    [Header("Input System")]
    public PlayerInput playerInput;
    public string resetActionName = "ResetCar";
    public int triangleButtonIndex = 2;
    public int[] fallbackTriangleButtonIndices = { 2, 3, 4 };
    public int[] ignoredResetButtonIndices = { 2, 3 };
    public bool allowKeyboardRReset = true;

    [Header("Safe Position")]
    public float safeSaveDistance = 8f;
    public float uprightCheckDot = 0.65f;
    public float maxVerticalSpeedForSafeSave = 2f;
    public float minSpeedForSafeSave = 3f;

    [Header("Obstacle Check")]
    public float obstacleCheckDistance = 3f;
    public LayerMask obstacleLayers = ~0;

    [Header("Respawn")]
    public float resetHeightOffset = 2.2f;
    public bool respawnToCheckpointRoadCenter = true;
    public bool preferNearestPathRespawn = true;
    public string respawnPathName = "Path_1";
    public float respawnDelay = 1.2f;
    public float groundRaycastHeight = 35f;
    public float groundRaycastDistance = 100f;
    public float roadSearchRadius = 8f;
    public string roadTag = "Road";
    public LayerMask groundLayers = ~0;
    public bool allowAnyGroundFallback = true;

    [Header("After Reset")]
    public bool clearVelocityOnReset = true;
    public bool clearWheelForcesOnReset = true;
    public bool logResetDebug = true;
    private Rigidbody rb;
    private WheelCollider[] wheelColliders;
    private Collider[] carColliders;
    private CarController carController;
    private ALIyerEdon.Checkpoint_Manager checkpointManager;
    private RaceSessionManager raceSessionManager;
    private G29PauseMenu pauseMenu;
    private bool isRespawning;

    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheelColliders = GetComponentsInChildren<WheelCollider>();
        carColliders = GetComponentsInChildren<Collider>();
        carController = GetComponent<CarController>();
        checkpointManager = FindObjectOfType<ALIyerEdon.Checkpoint_Manager>();
        raceSessionManager = FindObjectOfType<RaceSessionManager>();
        pauseMenu = FindObjectOfType<G29PauseMenu>();

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (playerInput == null)
        {
            playerInput = GetComponentInParent<PlayerInput>();
        }

        if (playerInput == null)
        {
            playerInput = GetComponentInChildren<PlayerInput>();
        }

        if (groundLayers.value == 0)
        {
            groundLayers = ~0;
        }
    }

    private void Start()
    {
        SnapCurrentPositionToRoad();
        SaveCurrentPositionAsSafe();
    }

    private void FixedUpdate()
    {
        UpdateSafePositionByDistance();
    }

    private void Update()
    {
        CheckResetInput();
    }

    private void CheckResetInput()
    {
        if (IsPauseMenuPaused())
        {
            return;
        }

        bool pressed = false;

        if (playerInput != null && playerInput.actions != null)
        {
            InputAction resetAction = playerInput.actions.FindAction(resetActionName);
            pressed = resetAction != null && resetAction.WasPressedThisFrame();
        }

        if (pressed || WasTrianglePressedThisFrame())
        {
            RequestResetCar();
        }
    }

    private bool IsPauseMenuPaused()
    {
        if (pauseMenu == null)
        {
            pauseMenu = FindObjectOfType<G29PauseMenu>();
        }

        return pauseMenu != null && pauseMenu.IsPaused;
    }

    private bool WasTrianglePressedThisFrame()
    {
        if (allowKeyboardRReset && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            return true;
        }

        Joystick joystick = Joystick.current;

        if (joystick != null)
        {
            if (IsJoystickButtonPressedThisFrame(joystick, triangleButtonIndex))
            {
                return !IsIgnoredResetButtonIndex(triangleButtonIndex);
            }

            if (fallbackTriangleButtonIndices != null)
            {
                for (int i = 0; i < fallbackTriangleButtonIndices.Length; i++)
                {
                    int fallbackButtonIndex = fallbackTriangleButtonIndices[i];

                    if (fallbackButtonIndex == triangleButtonIndex || IsIgnoredResetButtonIndex(fallbackButtonIndex))
                    {
                        continue;
                    }

                    if (IsJoystickButtonPressedThisFrame(joystick, fallbackButtonIndex))
                    {
                        return true;
                    }
                }
            }
        }

        return Gamepad.current != null
            && Gamepad.current.buttonNorth != null
            && Gamepad.current.buttonNorth.wasPressedThisFrame;
    }

    private bool IsIgnoredResetButtonIndex(int buttonIndex)
    {
        if (ignoredResetButtonIndices == null)
        {
            return false;
        }

        for (int i = 0; i < ignoredResetButtonIndices.Length; i++)
        {
            if (ignoredResetButtonIndices[i] == buttonIndex)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsJoystickButtonPressedThisFrame(Joystick joystick, int buttonIndex)
    {
        if (joystick == null || buttonIndex < 0)
        {
            return false;
        }

        ButtonControl button = joystick.TryGetChildControl<ButtonControl>($"button{buttonIndex}");
        return button != null && button.wasPressedThisFrame;
    }

    private void UpdateSafePositionByDistance()
    {
        if (rb == null || isRespawning)
        {
            return;
        }

        float distanceFromLastSafe = Vector3.Distance(transform.position, lastSafePosition);

        if (distanceFromLastSafe < safeSaveDistance)
        {
            return;
        }

        if (IsSafeToSavePosition())
        {
            SaveCurrentPositionAsSafe();
        }
    }

    private bool IsSafeToSavePosition()
    {
        bool isUpright = Vector3.Dot(transform.up, Vector3.up) >= uprightCheckDot;
        bool verticalSpeedOk = Mathf.Abs(rb.linearVelocity.y) <= maxVerticalSpeedForSafeSave;
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        bool speedOk = speedKmh >= minSpeedForSafeSave;

        bool obstacleInFront = Physics.Raycast(
            transform.position + Vector3.up * 0.5f,
            transform.forward,
            obstacleCheckDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);

        return isUpright && verticalSpeedOk && speedOk && !obstacleInFront;
    }

    private void SaveCurrentPositionAsSafe()
    {
        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;
    }

    public void RequestResetCar()
    {
        if (isRespawning)
        {
            return;
        }

        StartCoroutine(ResetCarAfterDelay());
    }

    private IEnumerator ResetCarAfterDelay()
    {
        isRespawning = true;

        if (carController != null)
        {
            carController.SetControlsEnabled(false);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (clearWheelForcesOnReset)
        {
            ClearWheelForces();
        }

        yield return new WaitForSeconds(respawnDelay);

        ResetCar();

        if (carController != null)
        {
            carController.SetControlsEnabled(true);
        }

        isRespawning = false;
    }

    public void ResetCar()
    {
        Vector3 targetPosition = lastSafePosition;
        Quaternion targetRotation = lastSafeRotation;

        if (respawnToCheckpointRoadCenter)
        {
            GetRaceRespawnPose(out targetPosition, out targetRotation);
        }

        targetPosition = SnapRespawnPositionToGround(targetPosition);
        Vector3 finalPosition = targetPosition + Vector3.up * resetHeightOffset;
        transform.SetPositionAndRotation(finalPosition, targetRotation);

        if (rb != null && clearVelocityOnReset)
        {
            rb.detectCollisions = true;
            rb.position = finalPosition;
            rb.rotation = targetRotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (clearWheelForcesOnReset)
        {
            ClearWheelForces();
        }

        Physics.SyncTransforms();
        SaveCurrentPositionAsSafe();

        if (logResetDebug)
        {
            Debug.Log($"[CarReset] Respawned '{name}' to {finalPosition} using target {targetPosition}.", this);
        }
    }

    private void GetRaceRespawnPose(out Vector3 position, out Quaternion rotation)
    {
        position = lastSafePosition;
        rotation = lastSafeRotation;

        if (preferNearestPathRespawn && TryGetNearestPathPose(out position, out rotation))
        {
            return;
        }
        if (raceSessionManager == null)
        {
            raceSessionManager = FindObjectOfType<RaceSessionManager>();
        }

        if (raceSessionManager != null && raceSessionManager.TryGetRespawnPose(transform.root, out position, out rotation))
        {
            return;
        }

        if (checkpointManager == null)
        {
            checkpointManager = FindObjectOfType<ALIyerEdon.Checkpoint_Manager>();
        }

        if (checkpointManager == null || checkpointManager.checkpoints == null || checkpointManager.checkpoints.Count == 0)
        {
            return;
        }

        int closest = FindClosestCheckpointIndex();
        int next = (closest + 1) % checkpointManager.checkpoints.Count;
        Transform closestCheckpoint = checkpointManager.checkpoints[closest];
        Transform nextCheckpoint = checkpointManager.checkpoints[next];

        if (closestCheckpoint == null)
        {
            return;
        }

        position = closestCheckpoint.position;

        if (nextCheckpoint == null)
        {
            rotation = closestCheckpoint.rotation;
            return;
        }

        Vector3 direction = nextCheckpoint.position - closestCheckpoint.position;
        direction.y = 0f;

        rotation = direction.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : closestCheckpoint.rotation;
    }

    private bool TryGetNearestPathPose(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (string.IsNullOrWhiteSpace(respawnPathName))
        {
            return false;
        }

        GameObject pathObject = GameObject.Find(respawnPathName);

        if (pathObject == null)
        {
            return false;
        }

        List<Transform> points = new List<Transform>();
        ALIyerEdon.Waypoint_System waypointSystem = pathObject.GetComponent<ALIyerEdon.Waypoint_System>();

        if (waypointSystem != null && waypointSystem.waypoints != null && waypointSystem.waypoints.Count > 0)
        {
            points.AddRange(waypointSystem.waypoints);
        }
        else
        {
            foreach (Transform child in pathObject.transform)
            {
                points.Add(child);
            }
        }

        if (points.Count < 2)
        {
            return false;
        }

        Vector3 carPosition = transform.position;
        float bestDistance = float.MaxValue;
        Vector3 bestPoint = points[0].position;
        Vector3 bestDirection = transform.forward;

        for (int i = 0; i < points.Count; i++)
        {
            Transform a = points[i];
            Transform b = points[(i + 1) % points.Count];

            if (a == null || b == null)
            {
                continue;
            }

            Vector3 segment = b.position - a.position;
            Vector3 delta = carPosition - a.position;
            segment.y = 0f;
            delta.y = 0f;

            float segmentLengthSqr = segment.sqrMagnitude;

            if (segmentLengthSqr <= 0.01f)
            {
                continue;
            }

            float t = Mathf.Clamp01(Vector3.Dot(delta, segment) / segmentLengthSqr);
            Vector3 projected = a.position + (b.position - a.position) * t;
            Vector3 flatProjected = projected;
            flatProjected.y = carPosition.y;

            float distance = Vector3.SqrMagnitude(carPosition - flatProjected);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPoint = projected;
                bestDirection = segment.normalized;
            }
        }

        if (bestDistance >= float.MaxValue)
        {
            return false;
        }

        position = bestPoint;
        bestDirection.y = 0f;
        rotation = bestDirection.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(bestDirection.normalized, Vector3.up)
            : transform.rotation;

        return true;
    }

    private int FindClosestCheckpointIndex()
    {
        int closest = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < checkpointManager.checkpoints.Count; i++)
        {
            Transform checkpoint = checkpointManager.checkpoints[i];

            if (checkpoint == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(transform.position - checkpoint.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = i;
            }
        }

        return closest;
    }

    private Vector3 SnapRespawnPositionToGround(Vector3 position)
    {
        SetCarCollidersEnabled(false);

        bool hitGround = TryFindRoadSurfaceNear(position, out RaycastHit hit);

        SetCarCollidersEnabled(true);

        if (hitGround)
        {
            return hit.point;
        }

        return position;
    }

    private bool TryFindRoadSurfaceNear(Vector3 position, out RaycastHit roadHit)
    {
        Vector3[] offsets =
        {
            Vector3.zero,
            Vector3.forward * roadSearchRadius,
            Vector3.back * roadSearchRadius,
            Vector3.left * roadSearchRadius,
            Vector3.right * roadSearchRadius,
            (Vector3.forward + Vector3.left).normalized * roadSearchRadius,
            (Vector3.forward + Vector3.right).normalized * roadSearchRadius,
            (Vector3.back + Vector3.left).normalized * roadSearchRadius,
            (Vector3.back + Vector3.right).normalized * roadSearchRadius
        };

        roadHit = default;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 origin = position + offsets[i] + Vector3.up * groundRaycastHeight;

            if (!TryFindRoadSurface(origin, out RaycastHit hit))
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(position - hit.point);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                roadHit = hit;
            }
        }

        return bestDistance < float.MaxValue;
    }

    private bool TryFindRoadSurface(Vector3 origin, out RaycastHit roadHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            groundRaycastDistance,
            groundLayers.value == 0 ? ~0 : groundLayers.value,
            QueryTriggerInteraction.Ignore);

        roadHit = default;
        RaycastHit fallbackHit = default;
        float bestRoadDistance = float.MaxValue;
        float bestFallbackDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (hitCollider == null)
            {
                continue;
            }

            if (IsRoadCollider(hitCollider))
            {
                if (hits[i].distance < bestRoadDistance)
                {
                    bestRoadDistance = hits[i].distance;
                    roadHit = hits[i];
                }

                continue;
            }

            if (allowAnyGroundFallback && hits[i].distance < bestFallbackDistance)
            {
                bestFallbackDistance = hits[i].distance;
                fallbackHit = hits[i];
            }
        }

        if (bestRoadDistance < float.MaxValue)
        {
            return true;
        }

        if (allowAnyGroundFallback && bestFallbackDistance < float.MaxValue)
        {
            roadHit = fallbackHit;
            return true;
        }

        return false;
    }

    private bool IsRoadCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        Transform current = hitCollider.transform;

        while (current != null)
        {
            if (current.CompareTag(roadTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void SnapCurrentPositionToRoad()
    {
        Vector3 snapped = SnapRespawnPositionToGround(transform.position);
        Vector3 finalPosition = snapped + Vector3.up * resetHeightOffset;
        transform.position = finalPosition;

        if (rb != null && clearVelocityOnReset)
        {
            rb.detectCollisions = true;
            rb.position = finalPosition;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void SetCarCollidersEnabled(bool enabled)
    {
        if (carColliders == null)
        {
            return;
        }

        for (int i = 0; i < carColliders.Length; i++)
        {
            if (carColliders[i] != null)
            {
                carColliders[i].enabled = enabled;
            }
        }
    }

    private void ClearWheelForces()
    {
        if (wheelColliders == null)
        {
            return;
        }

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (wheelColliders[i] == null)
            {
                continue;
            }

            wheelColliders[i].motorTorque = 0f;
            wheelColliders[i].brakeTorque = 0f;
        }
    }
}




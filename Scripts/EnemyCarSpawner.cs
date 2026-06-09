using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyCarSpawner : MonoBehaviour
{
    [SerializeField] private RaceSessionManager raceSessionManager;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool spawnEnemiesAtRuntime = false;
    [SerializeField] private int enemyCount = 4;
    [SerializeField] private Vector3[] spawnOffsets =
    {
        new Vector3(-4f, 0f, -8f),
        new Vector3(4f, 0f, -8f),
        new Vector3(-4f, 0f, -16f),
        new Vector3(4f, 0f, -16f)
    };
    [SerializeField] private string[] pathNames = { "Path_1", "Path_2", "Path_1", "Path_2" };
    [SerializeField] private Color[] bodyColors =
    {
        new Color(0.9f, 0.05f, 0.04f, 1f),
        new Color(0.05f, 0.35f, 0.95f, 1f),
        new Color(1f, 0.8f, 0.05f, 1f),
        new Color(0.05f, 0.75f, 0.25f, 1f)
    };
    [SerializeField] private bool snapCarsToRoadOnStart = true;
    [SerializeField] private string roadTag = "Road";
    [SerializeField] private LayerMask roadLayers = ~0;
    [SerializeField] private float roadRaycastHeight = 35f;
    [SerializeField] private float roadRaycastDistance = 100f;
    [SerializeField] private float roadSearchRadius = 8f;
    [SerializeField] private float roadHeightOffset = 2.2f;
    [SerializeField] private bool allowAnyGroundFallback = true;

    private readonly List<CarControllerWaypointAi> spawnedAi = new List<CarControllerWaypointAi>();
    private Transform playerTarget;

    private void Awake()
    {
        if (raceSessionManager == null)
        {
            raceSessionManager = FindObjectOfType<RaceSessionManager>();
        }

        if (spawnEnemiesAtRuntime)
        {
            SpawnEnemies();
        }

        FindPreplacedEnemies();
        AssignPlayerTarget();
        SnapRaceCarsToRoad();
        RegisterOpponents();
        SetRaceStarted(false);
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

        raceSessionManager.RaceBegan += StartEnemies;
        raceSessionManager.RaceEnded += StopEnemies;
    }

    private void OnDisable()
    {
        if (raceSessionManager == null)
        {
            return;
        }

        raceSessionManager.RaceBegan -= StartEnemies;
        raceSessionManager.RaceEnded -= StopEnemies;
    }

    private void SpawnEnemies()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);

        if (player == null)
        {
            CarController playerController = FindObjectOfType<CarController>();
            player = playerController != null ? playerController.gameObject : null;
        }

        if (player == null)
        {
            Debug.LogWarning("[EnemyCarSpawner] Player car not found. Enemy cars were not spawned.", this);
            return;
        }

        playerTarget = player.transform;

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 offset = i < spawnOffsets.Length ? spawnOffsets[i] : new Vector3((i % 2 == 0 ? -4f : 4f), 0f, -8f * (i + 1));
            GameObject enemy = Instantiate(player, player.transform.position + player.transform.TransformDirection(offset), player.transform.rotation);
            enemy.name = $"Enemy_AI_Car_{i + 1}";
            SetTagsRecursively(enemy, "Untagged");

            StripPlayerOnlyComponents(enemy);
            ConfigureOpponentAudio(enemy);
            PreparePhysics(enemy);
            SnapCarToRoad(enemy.transform);
            ApplyBodyColor(enemy, i < bodyColors.Length ? bodyColors[i] : Color.Lerp(Color.red, Color.cyan, i / Mathf.Max(1f, enemyCount - 1f)));

            CarController controller = enemy.GetComponent<CarController>();
            controller.showInputDebug = false;
            controller.playerInput = null;
            controller.SetControlsEnabled(false);

            CarControllerWaypointAi ai = enemy.AddComponent<CarControllerWaypointAi>();
            ai.ConfigurePath(i < pathNames.Length ? pathNames[i] : "Path_1");
            ai.SetPlayerTarget(playerTarget);
            spawnedAi.Add(ai);
        }
    }

    private void FindPreplacedEnemies()
    {
        CarControllerWaypointAi[] aiCars = Resources.FindObjectsOfTypeAll<CarControllerWaypointAi>();
        List<CarControllerWaypointAi> sceneAiCars = new List<CarControllerWaypointAi>();

        for (int i = 0; i < aiCars.Length; i++)
        {
            if (aiCars[i] == null || aiCars[i].gameObject.scene != gameObject.scene)
            {
                continue;
            }

            sceneAiCars.Add(aiCars[i]);
        }

        sceneAiCars.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        for (int i = 0; i < sceneAiCars.Count; i++)
        {
            if (!spawnedAi.Contains(sceneAiCars[i]))
            {
                spawnedAi.Add(sceneAiCars[i]);
            }

            ConfigureOpponentAudio(sceneAiCars[i].gameObject);
        }
    }

    private void AssignPlayerTarget()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);

            if (player == null)
            {
                CarController playerController = FindObjectOfType<CarController>();
                player = playerController != null ? playerController.gameObject : null;
            }

            playerTarget = player != null ? player.transform : null;
        }

        for (int i = 0; i < spawnedAi.Count; i++)
        {
            if (spawnedAi[i] != null)
            {
                spawnedAi[i].SetPlayerTarget(playerTarget);
            }
        }
    }

    private void StripPlayerOnlyComponents(GameObject enemy)
    {
        foreach (PlayerInput input in enemy.GetComponentsInChildren<PlayerInput>(true))
        {
            Destroy(input);
        }

        foreach (Camera camera in enemy.GetComponentsInChildren<Camera>(true))
        {
            Destroy(camera.gameObject);
        }

        foreach (AudioListener listener in enemy.GetComponentsInChildren<AudioListener>(true))
        {
            Destroy(listener);
        }

        foreach (G29ButtonLogger logger in enemy.GetComponentsInChildren<G29ButtonLogger>(true))
        {
            Destroy(logger);
        }

        foreach (G29CrossButtonTester tester in enemy.GetComponentsInChildren<G29CrossButtonTester>(true))
        {
            Destroy(tester);
        }

        foreach (CarResetController resetController in enemy.GetComponentsInChildren<CarResetController>(true))
        {
            Destroy(resetController);
        }
    }

    private void SetTagsRecursively(GameObject target, string tagName)
    {
        target.tag = tagName;

        foreach (Transform child in target.transform)
        {
            SetTagsRecursively(child.gameObject, tagName);
        }
    }

    private void PreparePhysics(GameObject enemy)
    {
        Rigidbody rb = enemy.GetComponent<Rigidbody>();

        if (rb == null)
        {
            return;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void ConfigureOpponentAudio(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        foreach (CarEngineSound engineSound in enemy.GetComponentsInChildren<CarEngineSound>(true))
        {
            engineSound.ConfigureAsOpponentAudio();
        }
    }

    private void ApplyBodyColor(GameObject enemy, Color color)
    {
        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            string lowerName = renderer.name.ToLowerInvariant();

            if (lowerName.Contains("glass") || lowerName.Contains("window") || lowerName.Contains("wheel") || lowerName.Contains("tire"))
            {
                continue;
            }

            Material[] materials = renderer.materials;

            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] == null)
                {
                    continue;
                }

                if (materials[m].HasProperty("_BaseColor"))
                {
                    materials[m].SetColor("_BaseColor", color);
                }
                else if (materials[m].HasProperty("_Color"))
                {
                    materials[m].SetColor("_Color", color);
                }
            }
        }
    }

    private void StartEnemies()
    {
        SetRaceStarted(true);
    }

    private void StopEnemies()
    {
        SetRaceStarted(false);
    }

    private void SetRaceStarted(bool started)
    {
        for (int i = 0; i < spawnedAi.Count; i++)
        {
            if (spawnedAi[i] != null)
            {
                spawnedAi[i].SetRaceStarted(started);
            }
        }
    }

    private void RegisterOpponents()
    {
        if (raceSessionManager == null)
        {
            return;
        }

        List<Transform> opponents = new List<Transform>();

        for (int i = 0; i < spawnedAi.Count; i++)
        {
            if (spawnedAi[i] != null)
            {
                opponents.Add(spawnedAi[i].transform);
            }
        }

        raceSessionManager.SetOpponentCars(opponents.ToArray());
        Debug.Log($"[EnemyCarSpawner] Registered {opponents.Count} opponent cars.", this);
    }

    private void SnapRaceCarsToRoad()
    {
        if (!snapCarsToRoadOnStart)
        {
            return;
        }

        if (playerTarget != null)
        {
            SnapCarToRoad(playerTarget);
        }

        for (int i = 0; i < spawnedAi.Count; i++)
        {
            if (spawnedAi[i] != null)
            {
                SnapCarToRoad(spawnedAi[i].transform);
            }
        }
    }

    private void SnapCarToRoad(Transform car)
    {
        if (car == null)
        {
            return;
        }

        Collider[] colliders = car.GetComponentsInChildren<Collider>();
        SetCollidersEnabled(colliders, false);

        bool found = TryFindRoadSurfaceNear(car.position, out RaycastHit bestHit);

        SetCollidersEnabled(colliders, true);

        if (!found)
        {
            return;
        }

        car.position = bestHit.point + Vector3.up * roadHeightOffset;

        Rigidbody rb = car.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
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
        float bestRoadDistance = float.MaxValue;
        float bestFallbackDistance = float.MaxValue;
        RaycastHit fallbackHit = default;

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 origin = position + offsets[i] + Vector3.up * roadRaycastHeight;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                roadRaycastDistance,
                roadLayers,
                QueryTriggerInteraction.Ignore);

            for (int h = 0; h < hits.Length; h++)
            {
                if (hits[h].collider == null)
                {
                    continue;
                }

                if (IsRoadCollider(hits[h].collider))
                {
                    float distance = Vector3.SqrMagnitude(position - hits[h].point);

                    if (distance < bestRoadDistance)
                    {
                        bestRoadDistance = distance;
                        roadHit = hits[h];
                    }

                    continue;
                }

                if (allowAnyGroundFallback)
                {
                    float distance = Vector3.SqrMagnitude(position - hits[h].point);

                    if (distance < bestFallbackDistance)
                    {
                        bestFallbackDistance = distance;
                        fallbackHit = hits[h];
                    }
                }
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

    private static void SetCollidersEnabled(Collider[] colliders, bool enabled)
    {
        if (colliders == null)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabled;
            }
        }
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
}

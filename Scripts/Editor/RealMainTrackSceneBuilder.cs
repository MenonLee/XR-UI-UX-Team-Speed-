using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RealMainTrackSceneBuilder
{
    private const string MainTrackScenePath = "Assets/Scenes/MainTrack.unity";
    private const string RealMainTrackScenePath = "Assets/Scenes/RealMainTrack.unity";
    private const string SourceTrackScenePath = "Assets/Racing_Game/Scene/Race_Track_7.unity";
    private const string PendingRequestPath = "Temp/BuildRealMainTrack.request";

    private static readonly HashSet<string> PreserveRootNames = new HashSet<string>
    {
        "RMCar26",
        "RMCar26_Main",
        "RaceSessionRuntime",
        "PauseManager",
        "EventSystem",
        "MinimapCamera",
        "MainTrack_BGM_Player",
        "test"
    };

    private static readonly HashSet<string> SkipSourceRootNames = new HashSet<string>
    {
        "Main Camera",
        "Camera_Parent",
        "Skidmarks_Manager",
        "Race_Manager",
        "RaceUI",
        "Canvas",
        "EventSystem"
    };

    static RealMainTrackSceneBuilder()
    {
        EditorApplication.delayCall += RunPendingBuildRequest;
    }

    [MenuItem("Tools/Racing Game/Build RealMainTrack From Track 7")]
    public static void BuildRealMainTrackFromTrack7()
    {
        EditorSceneManager.SaveOpenScenes();
        Build();
    }

    public static void BuildFromCommandLine()
    {
        Build();
        EditorApplication.Exit(0);
    }

    private static void RunPendingBuildRequest()
    {
        string requestPath = Path.Combine(Directory.GetCurrentDirectory(), PendingRequestPath);

        if (!File.Exists(requestPath))
        {
            return;
        }

        File.Delete(requestPath);

        try
        {
            EditorSceneManager.SaveOpenScenes();
            Build();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void Build()
    {
        if (!File.Exists(ToProjectPath(MainTrackScenePath)))
        {
            throw new FileNotFoundException("MainTrack scene not found.", MainTrackScenePath);
        }

        if (!File.Exists(ToProjectPath(SourceTrackScenePath)))
        {
            throw new FileNotFoundException("Race_Track_7 scene not found.", SourceTrackScenePath);
        }

        BackupExistingRealMainTrack();
        CopyMainTrackToRealMainTrack();

        Scene targetScene = EditorSceneManager.OpenScene(RealMainTrackScenePath, OpenSceneMode.Single);
        Scene sourceScene = EditorSceneManager.OpenScene(SourceTrackScenePath, OpenSceneMode.Additive);

        RemoveOldTrackRoots(targetScene);
        CopyTrackRoots(sourceScene, targetScene);
        RemoveUnwantedCopiedSystems();
        WireRealMainTrackScene(targetScene);

        EditorSceneManager.MarkSceneDirty(targetScene);
        EditorSceneManager.SaveScene(targetScene);
        EditorSceneManager.CloseScene(sourceScene, true);
        AssetDatabase.Refresh();

        Debug.Log("[RealMainTrackSceneBuilder] Built Assets/Scenes/RealMainTrack.unity from MainTrack systems and Race_Track_7 map.");
    }

    private static void BackupExistingRealMainTrack()
    {
        if (!File.Exists(ToProjectPath(RealMainTrackScenePath)))
        {
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupPath = $"Assets/Scenes/RealMainTrack_Backup_{timestamp}.unity";
        AssetDatabase.CopyAsset(RealMainTrackScenePath, backupPath);
        AssetDatabase.DeleteAsset(RealMainTrackScenePath);
        Debug.Log($"[RealMainTrackSceneBuilder] Existing RealMainTrack backed up to {backupPath}");
    }

    private static void CopyMainTrackToRealMainTrack()
    {
        if (!AssetDatabase.CopyAsset(MainTrackScenePath, RealMainTrackScenePath))
        {
            throw new InvalidOperationException("Failed to copy MainTrack scene to RealMainTrack.");
        }

        AssetDatabase.Refresh();
    }

    private static void RemoveOldTrackRoots(Scene targetScene)
    {
        GameObject[] roots = targetScene.GetRootGameObjects();

        for (int i = roots.Length - 1; i >= 0; i--)
        {
            GameObject root = roots[i];

            if (root == null || ShouldPreserveTargetRoot(root))
            {
                continue;
            }

            Undo.DestroyObjectImmediate(root);
        }
    }

    private static bool ShouldPreserveTargetRoot(GameObject root)
    {
        string rootName = root.name;

        if (PreserveRootNames.Contains(rootName))
        {
            return true;
        }

        if (rootName.StartsWith("Enemy_AI_Car_", StringComparison.Ordinal))
        {
            return true;
        }

        if (root.GetComponentInChildren<CarControllerWaypointAi>(true) != null)
        {
            return true;
        }

        if (root.GetComponentInChildren<CarEngineSound>(true) != null
            && root.GetComponentInChildren<CarController>(true) != null)
        {
            return true;
        }

        return false;
    }

    private static void CopyTrackRoots(Scene sourceScene, Scene targetScene)
    {
        GameObject[] sourceRoots = sourceScene.GetRootGameObjects();

        for (int i = 0; i < sourceRoots.Length; i++)
        {
            GameObject sourceRoot = sourceRoots[i];

            if (sourceRoot == null || SkipSourceRootNames.Contains(sourceRoot.name))
            {
                continue;
            }

            GameObject copy = UnityEngine.Object.Instantiate(sourceRoot);
            copy.name = sourceRoot.name;
            SceneManager.MoveGameObjectToScene(copy, targetScene);
        }
    }

    private static void RemoveUnwantedCopiedSystems()
    {
        RemoveObjectIfFound("Race_Manager");
        RemoveObjectIfFound("RaceUI");
        RemoveObjectsByExactName("Race_Manager");
        RemoveObjectsByExactName("RaceUI");
    }

    private static void RemoveObjectIfFound(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);

        if (obj != null)
        {
            Undo.DestroyObjectImmediate(obj);
        }
    }

    private static void RemoveObjectsByExactName(string objectName)
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true);

        for (int i = allObjects.Length - 1; i >= 0; i--)
        {
            GameObject obj = allObjects[i];

            if (obj != null && obj.name == objectName)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }

    private static void WireRealMainTrackScene(Scene targetScene)
    {
        EditorSceneManager.SetActiveScene(targetScene);

        RaceSessionManager session = UnityEngine.Object.FindObjectOfType<RaceSessionManager>(true);
        FinishLineTrigger finishLine = UnityEngine.Object.FindObjectOfType<FinishLineTrigger>(true);

        if (finishLine == null)
        {
            finishLine = CreateFinishLineFromCheckpointZero();
        }

        if (session != null)
        {
            Transform finishTransform = finishLine != null ? finishLine.transform : null;
            session.SetFinishLine(finishTransform);
            session.SetOpponentCars(FindOpponentCars());
        }

        RaceCountdown countdown = UnityEngine.Object.FindObjectOfType<RaceCountdown>(true);

        if (countdown != null && session != null)
        {
            countdown.CountdownFinished -= session.BeginRace;
            countdown.CountdownFinished += session.BeginRace;
        }

        ConfigureAiPaths();
        PlaceCarsAtTrack7SpawnPoints();
    }

    private static FinishLineTrigger CreateFinishLineFromCheckpointZero()
    {
        GameObject checkpointZero = GameObject.Find("Checkpoint_0");
        GameObject finishObject = new GameObject("FinishLine");

        if (checkpointZero != null)
        {
            finishObject.transform.SetPositionAndRotation(checkpointZero.transform.position, checkpointZero.transform.rotation);

            BoxCollider sourceCollider = checkpointZero.GetComponent<BoxCollider>();
            BoxCollider finishCollider = finishObject.AddComponent<BoxCollider>();
            finishCollider.isTrigger = true;

            if (sourceCollider != null)
            {
                finishCollider.center = sourceCollider.center;
                finishCollider.size = sourceCollider.size;
            }
            else
            {
                finishCollider.size = new Vector3(28f, 8f, 4f);
            }
        }
        else
        {
            finishObject.transform.position = Vector3.up;
            BoxCollider finishCollider = finishObject.AddComponent<BoxCollider>();
            finishCollider.isTrigger = true;
            finishCollider.size = new Vector3(28f, 8f, 4f);
        }

        return finishObject.AddComponent<FinishLineTrigger>();
    }

    private static Transform[] FindOpponentCars()
    {
        List<Transform> opponents = new List<Transform>();
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true);

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];

            if (obj != null && obj.name.StartsWith("Enemy_AI_Car_", StringComparison.Ordinal))
            {
                opponents.Add(obj.transform);
            }
        }

        opponents.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return opponents.ToArray();
    }

    private static void ConfigureAiPaths()
    {
        CarControllerWaypointAi[] aiCars = UnityEngine.Object.FindObjectsOfType<CarControllerWaypointAi>(true);

        for (int i = 0; i < aiCars.Length; i++)
        {
            if (aiCars[i] == null)
            {
                continue;
            }

            string path = i % 2 == 0 ? "Path_1" : "Path_2";
            aiCars[i].ConfigurePath(path);
        }
    }

    private static void PlaceCarsAtTrack7SpawnPoints()
    {
        List<Transform> spawnPoints = FindSpawnPoints();

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[RealMainTrackSceneBuilder] No Spawn_Position points found in Race_Track_7 map. Car positions were kept from MainTrack.");
            return;
        }

        Transform player = FindPlayerCar();

        if (player != null)
        {
            PlaceCar(player, spawnPoints[0]);
        }

        Transform[] opponents = FindOpponentCars();

        for (int i = 0; i < opponents.Length; i++)
        {
            int spawnIndex = Mathf.Min(i + 1, spawnPoints.Count - 1);
            PlaceCar(opponents[i], spawnPoints[spawnIndex]);
        }
    }

    private static List<Transform> FindSpawnPoints()
    {
        List<Transform> spawns = new List<Transform>();
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true);

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];

            if (obj == null)
            {
                continue;
            }

            if (obj.name.StartsWith("Spawn_Position", StringComparison.Ordinal))
            {
                spawns.Add(obj.transform);
            }
        }

        spawns.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return spawns;
    }

    private static Transform FindPlayerCar()
    {
        GameObject taggedPlayer = GameObject.FindWithTag("Player");

        if (taggedPlayer != null)
        {
            return taggedPlayer.transform;
        }

        GameObject namedPlayer = GameObject.Find("RMCar26");
        return namedPlayer != null ? namedPlayer.transform : null;
    }

    private static void PlaceCar(Transform car, Transform spawn)
    {
        if (car == null || spawn == null)
        {
            return;
        }

        Vector3 position = spawn.position + Vector3.up * 0.45f;
        Quaternion rotation = spawn.rotation;
        car.SetPositionAndRotation(position, rotation);

        Rigidbody rb = car.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private static string ToProjectPath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public static class RaceSessionBootstrap
{
    private static readonly string[] ExcludedSceneNames =
    {
        "LobbyScene",
        "Garage"
    };

    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        InstallForScene(SceneManager.GetActiveScene());

        if (subscribed)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        subscribed = true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForScene(scene);
    }

    private static void InstallForScene(Scene scene)
    {
        if (!scene.isLoaded || ShouldSkip(scene.name))
        {
            return;
        }

        bool hasSessionManager = Object.FindObjectOfType<RaceSessionManager>() != null;
        bool hasCountdown = Object.FindObjectOfType<RaceCountdown>() != null;

        if (hasSessionManager && hasCountdown)
        {
            return;
        }

        GameObject root = GameObject.Find("RaceSessionRuntime");

        if (root == null)
        {
            root = new GameObject("RaceSessionRuntime");
        }

        if (!hasSessionManager && root.GetComponent<RaceSessionManager>() == null)
        {
            root.AddComponent<RaceSessionManager>();
        }

        if (!hasCountdown && root.GetComponent<RaceCountdown>() == null)
        {
            root.AddComponent<RaceCountdown>();
        }
    }

    private static bool ShouldSkip(string sceneName)
    {
        for (int i = 0; i < ExcludedSceneNames.Length; i++)
        {
            if (sceneName == ExcludedSceneNames[i])
            {
                return true;
            }
        }

        return false;
    }
}

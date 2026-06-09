using System.Collections;
using UnityEngine;

public class DarkIntroController : MonoBehaviour
{
    [Header("시네마틱 조명 설정")]
    public GameObject allLightsGroup;
    public AudioSource engineAudio;
    public EngineStartEffect engineShake;
    public GameObject lobbyCarRoot;
    public string lobbyCarName = "LobbyCar1";
    public string lobbyCarLightGroupName = "Intro_Lights";
    private GameObject lobbyCarLightGroup;

    [Header("UI 페이드 설정")]
    public CanvasGroup mainUI;
    public float uiDelayAfterLight = 1.0f;
    public float uiFadeDuration = 1.5f;

    [Header("타이밍 설정")]
    public float waitTimeBeforeLight = 1.5f;

    [Header("로비 차량 하이빔")]
    public bool configureLobbyHighBeam = true;
    public float highBeamRange = 55f;
    public float highBeamIntensity = 18f;
    public float highBeamSpotAngle = 24f;
    public float highBeamInnerSpotAngle = 12f;
    public float fillBeamRange = 24f;
    public float fillBeamIntensity = 4.5f;
    public float fillBeamSpotAngle = 58f;
    public float headlightEmissionIntensity = 3.5f;
    public Color highBeamColor = new Color(0.86f, 0.93f, 1f, 1f);

    private void Start()
    {
        ConfigureHighBeamLights();

        if (allLightsGroup != null)
        {
            allLightsGroup.SetActive(false);
        }

        if (lobbyCarLightGroup != null)
        {
            lobbyCarLightGroup.SetActive(false);
        }

        if (mainUI != null)
        {
            mainUI.alpha = 0f;
            mainUI.interactable = false;
            mainUI.blocksRaycasts = false;
        }

        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(waitTimeBeforeLight);

        if (engineAudio != null)
        {
            engineAudio.Play();
        }

        ConfigureHighBeamLights();

        if (allLightsGroup != null)
        {
            allLightsGroup.SetActive(true);
        }

        if (lobbyCarLightGroup != null)
        {
            lobbyCarLightGroup.SetActive(true);
        }

        if (engineShake != null)
        {
            engineShake.PlayEngineShake();
        }

        yield return new WaitForSeconds(uiDelayAfterLight);

        if (mainUI != null)
        {
            float elapsed = 0f;

            while (elapsed < uiFadeDuration)
            {
                elapsed += Time.deltaTime;
                mainUI.alpha = Mathf.Lerp(0f, 1f, elapsed / uiFadeDuration);
                yield return null;
            }

            mainUI.alpha = 1f;
            mainUI.interactable = true;
            mainUI.blocksRaycasts = true;
        }
    }

    private void ConfigureHighBeamLights()
    {
        if (!configureLobbyHighBeam)
        {
            return;
        }

        ApplyHighBeamMinimums();
        ConfigureIntroLights();

        GameObject carRoot = GetLobbyCarRoot();

        if (carRoot == null)
        {
            return;
        }

        Transform carLightGroup = EnsureLobbyCarLightGroup(carRoot.transform);
        EnsureCarMountedHighBeam(carLightGroup, "Left", new Vector3(-0.66f, 0.7f, 2.55f), Quaternion.Euler(2f, -5.5f, 0f));
        EnsureCarMountedHighBeam(carLightGroup, "Right", new Vector3(0.635f, 0.7f, 2.55f), Quaternion.Euler(2f, 7.5f, 0f));
        ConfigureHeadlightEmission(carRoot);
    }

    private void ApplyHighBeamMinimums()
    {
        highBeamRange = Mathf.Max(highBeamRange, 90f);
        highBeamIntensity = Mathf.Max(highBeamIntensity, 42f);
        highBeamSpotAngle = Mathf.Max(highBeamSpotAngle, 34f);
        highBeamInnerSpotAngle = Mathf.Max(highBeamInnerSpotAngle, 18f);
        fillBeamRange = Mathf.Max(fillBeamRange, 48f);
        fillBeamIntensity = Mathf.Max(fillBeamIntensity, 13f);
        fillBeamSpotAngle = Mathf.Max(fillBeamSpotAngle, 82f);
        headlightEmissionIntensity = Mathf.Max(headlightEmissionIntensity, 8f);
    }

    private void ConfigureIntroLights()
    {
        if (allLightsGroup == null)
        {
            return;
        }

        Light[] lights = allLightsGroup.GetComponentsInChildren<Light>(true);

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];

            if (light == null)
            {
                continue;
            }

            string lightName = light.name.ToLowerInvariant();

            if (lightName.Contains("widefill"))
            {
                continue;
            }

            if (!lightName.Contains("left light") && !lightName.Contains("right light"))
            {
                continue;
            }

            ConfigureMainHighBeam(light);
            EnsureFillBeam(light);
        }
    }

    private GameObject GetLobbyCarRoot()
    {
        if (lobbyCarRoot != null)
        {
            return lobbyCarRoot;
        }

        GameObject namedCar = GameObject.Find(lobbyCarName);

        if (namedCar != null)
        {
            lobbyCarRoot = namedCar;
            return lobbyCarRoot;
        }

        GameObject inactiveNamedCar = FindInactiveSceneObject(lobbyCarName);

        if (inactiveNamedCar != null)
        {
            lobbyCarRoot = inactiveNamedCar;
            return lobbyCarRoot;
        }

        GameObject taggedCar = GameObject.FindGameObjectWithTag("Player");

        if (taggedCar != null)
        {
            lobbyCarRoot = taggedCar;
        }

        return lobbyCarRoot;
    }

    private Transform EnsureLobbyCarLightGroup(Transform carRoot)
    {
        Transform existing = carRoot.Find(lobbyCarLightGroupName);

        if (existing != null)
        {
            lobbyCarLightGroup = existing.gameObject;
            return existing;
        }

        GameObject groupObject = new GameObject(lobbyCarLightGroupName);
        groupObject.transform.SetParent(carRoot, false);
        groupObject.transform.localPosition = Vector3.zero;
        groupObject.transform.localRotation = Quaternion.identity;
        groupObject.transform.localScale = Vector3.one;
        lobbyCarLightGroup = groupObject;

        return groupObject.transform;
    }

    private static GameObject FindInactiveSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate == null || candidate.name != objectName)
            {
                continue;
            }

            if (!candidate.scene.IsValid())
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private void EnsureCarMountedHighBeam(Transform parent, string side, Vector3 localPosition, Quaternion localRotation)
    {
        string lightName = "LobbyCar1_HighBeam_" + side;
        Transform existing = parent.Find(lightName);
        GameObject lightObject = existing != null ? existing.gameObject : new GameObject(lightName);

        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.localRotation = localRotation;
        lightObject.transform.localScale = Vector3.one;

        Light mainLight = lightObject.GetComponent<Light>();

        if (mainLight == null)
        {
            mainLight = lightObject.AddComponent<Light>();
        }

        ConfigureMainHighBeam(mainLight);
        EnsureFillBeam(mainLight);
    }

    private void ConfigureHeadlightEmission(GameObject carRoot)
    {
        Renderer[] renderers = carRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            string rendererName = targetRenderer.name.ToLowerInvariant();

            if (!rendererName.Contains("mainlight") && !rendererName.Contains("daylight"))
            {
                continue;
            }

            Material[] materials = targetRenderer.materials;

            for (int m = 0; m < materials.Length; m++)
            {
                Material material = materials[m];

                if (material == null)
                {
                    continue;
                }

                Color emission = highBeamColor * headlightEmissionIntensity;

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emission);
                }

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", highBeamColor);
                }
                else if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", highBeamColor);
                }
            }
        }
    }

    private void ConfigureMainHighBeam(Light light)
    {
        light.type = LightType.Spot;
        light.color = highBeamColor;
        light.range = highBeamRange;
        light.intensity = highBeamIntensity;
        light.spotAngle = highBeamSpotAngle;
        light.innerSpotAngle = highBeamInnerSpotAngle;
        light.shadows = LightShadows.None;
    }

    private void EnsureFillBeam(Light source)
    {
        string fillName = source.name.Trim() + "_WideFill";
        Transform existing = source.transform.Find(fillName);

        if (existing != null)
        {
            Light existingLight = existing.GetComponent<Light>();

            if (existingLight != null)
            {
                ConfigureFillBeam(existingLight);
            }

            return;
        }

        GameObject fillObject = new GameObject(fillName);
        fillObject.transform.SetParent(source.transform, false);
        fillObject.transform.localPosition = Vector3.zero;
        fillObject.transform.localRotation = Quaternion.Euler(3f, 0f, 0f);
        fillObject.transform.localScale = Vector3.one;

        Light fillLight = fillObject.AddComponent<Light>();
        ConfigureFillBeam(fillLight);
    }

    private void ConfigureFillBeam(Light light)
    {
        light.type = LightType.Spot;
        light.color = highBeamColor;
        light.range = fillBeamRange;
        light.intensity = fillBeamIntensity;
        light.spotAngle = fillBeamSpotAngle;
        light.innerSpotAngle = fillBeamSpotAngle * 0.45f;
        light.shadows = LightShadows.None;
    }
}

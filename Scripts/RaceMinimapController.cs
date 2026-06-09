using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaceMinimapController : MonoBehaviour
{
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private Renderer minimapQuadRenderer;
    [SerializeField] private RectTransform minimapRect;
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] opponents = { };
    [SerializeField] private float cameraHeight = 260f;
    [SerializeField] private float cameraBackOffset = 0f;
    [SerializeField] private float orthographicSize = 115f;
    [SerializeField] private int textureSize = 512;
    [SerializeField] private bool rotateWithPlayer = true;
    [SerializeField] private int minimapDotLayer = 31;
    [SerializeField] private Color playerDotColor = new Color(0.1f, 1f, 0.25f, 1f);
    [SerializeField] private Color opponentDotColor = new Color(1f, 0.18f, 0.12f, 1f);

    private readonly List<RectTransform> opponentDots = new List<RectTransform>();
    private readonly List<SpriteRenderer> opponentWorldDots = new List<SpriteRenderer>();
    private RectTransform playerDot;
    private SpriteRenderer playerWorldDot;
    private RenderTexture runtimeTexture;
    private Material runtimeQuadMaterial;

    public void Configure(
        Camera camera,
        RawImage image,
        RectTransform rect,
        Transform playerTarget,
        Transform[] opponentTargets,
        float height,
        float size,
        int renderTextureSize)
    {
        minimapCamera = camera;
        minimapImage = image;
        minimapRect = rect;
        player = playerTarget;
        opponents = opponentTargets ?? new Transform[0];
        cameraHeight = height;
        orthographicSize = size;
        textureSize = Mathf.Clamp(renderTextureSize, 128, 1024);
        EnsureSetup();
    }

    public void ConfigureQuad(
        Camera camera,
        Renderer quadRenderer,
        Transform playerTarget,
        Transform[] opponentTargets,
        float height,
        float size,
        int renderTextureSize)
    {
        minimapCamera = camera;
        minimapQuadRenderer = quadRenderer;
        minimapImage = null;
        minimapRect = null;
        player = playerTarget;
        opponents = opponentTargets ?? new Transform[0];
        cameraHeight = height;
        orthographicSize = size;
        textureSize = Mathf.Clamp(renderTextureSize, 128, 1024);
        EnsureSetup();
    }

    private void Awake()
    {
        EnsureSetup();
    }

    private void OnDestroy()
    {
        if (runtimeTexture != null)
        {
            runtimeTexture.Release();
            Destroy(runtimeTexture);
        }

        if (runtimeQuadMaterial != null)
        {
            Destroy(runtimeQuadMaterial);
        }
    }

    private void LateUpdate()
    {
        bool usingUiMinimap = minimapRect != null;
        bool usingQuadMinimap = minimapQuadRenderer != null;

        if (player == null || minimapCamera == null || (!usingUiMinimap && !usingQuadMinimap))
        {
            return;
        }

        Vector3 forward = player.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        Vector3 cameraPosition = player.position - forward * cameraBackOffset + Vector3.up * cameraHeight;
        minimapCamera.transform.position = cameraPosition;
        minimapCamera.transform.rotation = rotateWithPlayer
            ? Quaternion.LookRotation(Vector3.down, forward)
            : Quaternion.Euler(90f, 0f, 0f);
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = orthographicSize;
        ConfigureCameraCullingMasks();

        if (playerDot != null)
        {
            playerDot.anchoredPosition = Vector2.zero;
            playerDot.localRotation = Quaternion.identity;
        }

        if (playerWorldDot != null)
        {
            PlaceWorldDot(playerWorldDot.transform, player, 0.8f);
            playerWorldDot.transform.localScale = Vector3.one * Mathf.Max(4f, orthographicSize * 0.055f);
            playerWorldDot.color = playerDotColor;
        }

        for (int i = 0; usingUiMinimap && i < opponentDots.Count; i++)
        {
            RectTransform dot = opponentDots[i];
            Transform target = i < opponents.Length ? opponents[i] : null;

            if (dot == null)
            {
                continue;
            }

            if (target == null)
            {
                dot.gameObject.SetActive(false);
                continue;
            }

            Vector3 viewPos = minimapCamera.WorldToViewportPoint(target.position);
            bool visible = viewPos.z > 0f
                && viewPos.x >= 0f && viewPos.x <= 1f
                && viewPos.y >= 0f && viewPos.y <= 1f;

            dot.gameObject.SetActive(visible);

            if (!visible)
            {
                continue;
            }

            float x = (viewPos.x - 0.5f) * minimapRect.rect.width;
            float y = (viewPos.y - 0.5f) * minimapRect.rect.height;
            dot.anchoredPosition = new Vector2(x, y);
        }

        for (int i = 0; i < opponentWorldDots.Count; i++)
        {
            SpriteRenderer dot = opponentWorldDots[i];
            Transform target = i < opponents.Length ? opponents[i] : null;

            if (dot == null)
            {
                continue;
            }

            if (target == null)
            {
                dot.gameObject.SetActive(false);
                continue;
            }

            dot.gameObject.SetActive(true);
            PlaceWorldDot(dot.transform, target, 0.8f);
            dot.transform.localScale = Vector3.one * Mathf.Max(3f, orthographicSize * 0.045f);
            dot.color = ColorForOpponent(target.name);
        }
    }

    private void EnsureSetup()
    {
        if ((minimapImage == null || minimapRect == null) && minimapQuadRenderer == null)
        {
            return;
        }

        if (minimapCamera != null && runtimeTexture == null)
        {
            runtimeTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "RaceMinimapRuntimeTexture",
                antiAliasing = 1,
                useMipMap = false
            };

            runtimeTexture.Create();
            minimapCamera.targetTexture = runtimeTexture;

            if (minimapImage != null)
            {
                minimapImage.texture = runtimeTexture;
            }

            if (minimapQuadRenderer != null)
            {
                runtimeQuadMaterial = new Material(minimapQuadRenderer.sharedMaterial);
                runtimeQuadMaterial.mainTexture = runtimeTexture;
                minimapQuadRenderer.sharedMaterial = runtimeQuadMaterial;
            }
        }

        if (minimapRect != null && playerDot == null)
        {
            playerDot = CreateDot("PlayerDot", playerDotColor, 17f);
        }

        while (minimapRect != null && opponentDots.Count < opponents.Length)
        {
            Transform target = opponentDots.Count < opponents.Length ? opponents[opponentDots.Count] : null;
            string dotName = target != null ? target.name + "_Dot" : "OpponentDot_" + (opponentDots.Count + 1);
            Color dotColor = target != null ? ColorForOpponent(target.name) : opponentDotColor;
            opponentDots.Add(CreateDot(dotName, dotColor, 12f));
        }

        if (minimapQuadRenderer != null && playerWorldDot == null)
        {
            playerWorldDot = CreateWorldDot("PlayerDot", playerDotColor, 1);
        }

        while (minimapQuadRenderer != null && opponentWorldDots.Count < opponents.Length)
        {
            Transform target = opponentWorldDots.Count < opponents.Length ? opponents[opponentWorldDots.Count] : null;
            string dotName = target != null ? target.name + "_Dot" : "OpponentDot_" + (opponentWorldDots.Count + 1);
            Color dotColor = target != null ? ColorForOpponent(target.name) : opponentDotColor;
            opponentWorldDots.Add(CreateWorldDot(dotName, dotColor, 0));
        }
    }

    private RectTransform CreateDot(string objectName, Color color, float size)
    {
        GameObject dotObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        dotObject.transform.SetParent(minimapRect, false);

        RectTransform rect = dotObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);

        Image image = dotObject.GetComponent<Image>();
        image.color = color;
        ApplyNoZTestMaterial(image);

        return rect;
    }

    private SpriteRenderer CreateWorldDot(string objectName, Color color, int sortingOrder)
    {
        GameObject dotObject = new GameObject(objectName);
        dotObject.transform.SetParent(transform, false);

        SpriteRenderer renderer = dotObject.AddComponent<SpriteRenderer>();
        dotObject.layer = minimapDotLayer;
        renderer.sprite = CreateDotSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void ConfigureCameraCullingMasks()
    {
        int dotLayerMask = 1 << minimapDotLayer;

        if (minimapCamera != null)
        {
            minimapCamera.cullingMask |= dotLayerMask;
        }

        Camera[] cameras = Camera.allCameras;

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];

            if (camera == null || camera == minimapCamera)
            {
                continue;
            }

            camera.cullingMask &= ~dotLayerMask;
        }
    }

    private void PlaceWorldDot(Transform dot, Transform target, float yOffset)
    {
        dot.position = target.position + Vector3.up * yOffset;
        dot.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static Sprite CreateDotSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "RaceMinimapDotSprite"
        };

        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Color ColorForOpponent(string objectName)
    {
        string lowerName = objectName.ToLowerInvariant();

        if (lowerName.Contains("yellow"))
        {
            return new Color(1f, 0.86f, 0.08f, 1f);
        }

        if (lowerName.Contains("blue"))
        {
            return new Color(0.12f, 0.45f, 1f, 1f);
        }

        if (lowerName.Contains("green"))
        {
            return new Color(0.1f, 0.9f, 0.22f, 1f);
        }

        if (lowerName.Contains("purple"))
        {
            return new Color(0.78f, 0.18f, 1f, 1f);
        }

        if (lowerName.Contains("orange"))
        {
            return new Color(1f, 0.48f, 0.08f, 1f);
        }

        if (lowerName.Contains("white"))
        {
            return Color.white;
        }

        return new Color(1f, 0.18f, 0.12f, 1f);
    }

    private static void ApplyNoZTestMaterial(Graphic graphic)
    {
        Shader shader = Shader.Find("UI/NoZTest");

        if (shader == null || graphic == null)
        {
            return;
        }

        graphic.material = new Material(shader)
        {
            name = "Race Minimap UI NoZTest"
        };
    }
}

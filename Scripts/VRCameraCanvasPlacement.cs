using UnityEngine;

public class VRCameraCanvasPlacement : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float distanceFromCamera = 1f;
    [SerializeField] private Vector2 viewOffsetMeters = Vector2.zero;
    [SerializeField] private float worldScale = 0.001f;
    [SerializeField] private bool followCamera;
    [SerializeField] private bool placeOnEnable = true;

    private RectTransform rectTransform;

    public void Configure(Transform targetCamera, float distance, Vector2 offset, float scale, bool shouldFollow)
    {
        cameraTransform = targetCamera;
        distanceFromCamera = distance;
        viewOffsetMeters = offset;
        worldScale = scale;
        followCamera = shouldFollow;
        PlaceInFrontOfCamera();
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (placeOnEnable)
        {
            PlaceInFrontOfCamera();
        }
    }

    private void LateUpdate()
    {
        if (followCamera)
        {
            PlaceInFrontOfCamera();
        }
    }

    public void PlaceInFrontOfCamera()
    {
        Transform target = GetCameraTransform();

        if (target == null)
        {
            return;
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        transform.position = target.position
            + target.forward * distanceFromCamera
            + target.right * viewOffsetMeters.x
            + target.up * viewOffsetMeters.y;
        transform.rotation = target.rotation;
        transform.localScale = Vector3.one * worldScale;

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(1920f, 1080f);
        }
    }

    private Transform GetCameraTransform()
    {
        if (cameraTransform != null)
        {
            return cameraTransform;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
            return cameraTransform;
        }

        Camera fallbackCamera = FindObjectOfType<Camera>();
        cameraTransform = fallbackCamera != null ? fallbackCamera.transform : null;
        return cameraTransform;
    }
}

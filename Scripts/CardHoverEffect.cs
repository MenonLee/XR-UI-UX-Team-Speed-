using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 👈 ISelectHandler, IDeselectHandler, ISubmitHandler가 새로 추가되었습니다!
public class CardHoverEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("연결할 오브젝트")]
    public RectTransform cardVisual;
    public RawImage carPhoto;

    [Header("크기 설정 (Width, Height)")]
    public Vector2 normalSize = new Vector2(350f, 500f);
    public Vector2 hoverSize = new Vector2(500f, 500f);

    [Header("색상 설정")]
    public Color normalColor = Color.white;
    public Color unselectedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("효과음 설정")]
    public AudioClip hoverSound;
    private AudioSource audioSource;

    [Header("이동할 씬 설정")]
    public string sceneToLoad;

    [Header("애니메이션 속도")]
    public float transitionSpeed = 12f;

    private Vector2 targetSize;
    private Color targetColor;
    private Canvas cardCanvas;

    void Start()
    {
        targetSize = normalSize;
        targetColor = normalColor;

        cardVisual.sizeDelta = normalSize;
        if (carPhoto != null) carPhoto.color = normalColor;

        cardCanvas = cardVisual.gameObject.GetComponent<Canvas>();
        if (cardCanvas == null) cardCanvas = cardVisual.gameObject.AddComponent<Canvas>();

        if (cardVisual.gameObject.GetComponent<GraphicRaycaster>() == null)
            cardVisual.gameObject.AddComponent<GraphicRaycaster>();

        cardCanvas.overrideSorting = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (cardVisual.sizeDelta != targetSize)
            cardVisual.sizeDelta = Vector2.Lerp(cardVisual.sizeDelta, targetSize, Time.deltaTime * transitionSpeed);

        if (carPhoto != null && carPhoto.color != targetColor)
            carPhoto.color = Color.Lerp(carPhoto.color, targetColor, Time.deltaTime * transitionSpeed);
    }

    // --- 휠 버튼으로 "선택(포커스)" 되었을 때 실행 (Enter와 동일) ---
    public void OnSelect(BaseEventData eventData)
    {
        targetSize = hoverSize;
        cardCanvas.overrideSorting = true;
        cardCanvas.sortingOrder = 10;

        if (hoverSound != null && audioSource != null) audioSource.PlayOneShot(hoverSound);

        CardHoverEffect[] allCards = transform.parent.GetComponentsInChildren<CardHoverEffect>();
        foreach (CardHoverEffect card in allCards)
        {
            if (card == this) card.targetColor = normalColor;
            else card.targetColor = unselectedColor;
        }
    }

    // --- 휠 버튼을 움직여 다른 카드로 넘어가 "선택 해제" 되었을 때 실행 (Exit와 동일) ---
    public void OnDeselect(BaseEventData eventData)
    {
        targetSize = normalSize;
        cardCanvas.overrideSorting = false;

        CardHoverEffect[] allCards = transform.parent.GetComponentsInChildren<CardHoverEffect>();
        foreach (CardHoverEffect card in allCards) card.targetColor = normalColor;
    }

    // --- 휠 결정 버튼을 눌러 "제출(Submit)" 되었을 때 실행 (Click과 동일) ---
    public void OnSubmit(BaseEventData eventData)
    {
        ExecuteSceneLoad();
    }

    // 기존 마우스 클릭/진입 로직도 그대로 유지 (멀티 조작 가능)
    public void OnPointerClick(PointerEventData eventData) { ExecuteSceneLoad(); }
    public void OnPointerEnter(PointerEventData eventData) { EventSystem.current.SetSelectedGameObject(gameObject); }
    public void OnPointerExit(PointerEventData eventData) { EventSystem.current.SetSelectedGameObject(null); }

    private void ExecuteSceneLoad()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            return;
        }

        LobbyLicensePrompt.ShowForScene(sceneToLoad);
    }
}

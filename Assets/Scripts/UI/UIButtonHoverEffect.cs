using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float pressedScale = 0.97f;
    [SerializeField] private float animationSpeed = 14f;

    private Button button;
    private Outline outline;
    private AudioSource hoverAudioSource;
    private AudioClip hoverClip;

    private Vector3 baseScale;
    private Vector3 targetScale;
    private Color baseOutlineColor;
    private Color targetOutlineColor;
    private bool isHovered;

    private void Awake()
    {
        button = GetComponent<Button>();
        outline = GetComponent<Outline>();
        baseScale = transform.localScale;
        targetScale = baseScale;

        if (outline != null)
        {
            baseOutlineColor = outline.effectColor;
            targetOutlineColor = baseOutlineColor;
        }
    }

    public void ConfigureAudio(AudioSource source, AudioClip clip)
    {
        hoverAudioSource = source;
        hoverClip = clip;
    }

    private void OnEnable()
    {
        isHovered = false;
        transform.localScale = baseScale;
        targetScale = baseScale;

        if (outline != null)
        {
            outline.effectColor = baseOutlineColor;
            targetOutlineColor = baseOutlineColor;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime * animationSpeed;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, dt);

        if (outline != null)
        {
            outline.effectColor = Color.Lerp(outline.effectColor, targetOutlineColor, dt);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        isHovered = true;
        targetScale = baseScale * hoverScale;

        if (outline != null)
        {
            targetOutlineColor = new Color(1f, 0.85f, 0.2f, 1f);
        }

        if (hoverAudioSource != null && hoverClip != null)
        {
            hoverAudioSource.PlayOneShot(hoverClip, 0.35f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = baseScale;

        if (outline != null)
        {
            targetOutlineColor = baseOutlineColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        targetScale = baseScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        targetScale = isHovered ? baseScale * hoverScale : baseScale;
    }
}
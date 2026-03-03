using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Configurable menu button.
/// - Colors applied to the child "Visual" Image via Button.targetGraphic.
/// - Hitbox size controls the transparent raycast area on this GameObject.
/// - Visual size controls the sprite size on the child "Visual" GameObject.
/// - Hover triggers a smooth scale-up on the Visual child.
/// - Hover and click play assignable SFX clips.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(AudioSource))]
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Colors")]
    public Color normalColor    = Color.white;
    public Color highlightColor = new Color(0.7f, 0.9f, 1f, 1f);
    public Color pressedColor   = new Color(0.4f, 0.6f, 1f, 1f);

    [Header("Sizes")]
    [Tooltip("Clickable area. Independent from the sprite.")]
    public Vector2 hitboxSize = new Vector2(300f, 100f);
    [Tooltip("Sprite display size. Independent from the hitbox.")]
    public Vector2 visualSize = new Vector2(300f, 100f);

    [Header("Hover Scale")]
    [Tooltip("Scale multiplier applied to the Visual sprite on hover.")]
    public float hoverScale = 1.08f;
    [Tooltip("Duration of the scale transition in seconds.")]
    public float scaleDuration = 0.12f;

    [Header("SFX")]
    public AudioClip hoverClip;
    public AudioClip clickClip;

    private Transform _visual;
    private Coroutine _scaleCoroutine;
    private AudioSource _audioSource;

    private void Awake()
    {
        _visual      = transform.Find("Visual");
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        Apply();
    }

    /// <summary>
    /// Applies colors to the Button and sizes to both RectTransforms.
    /// </summary>
    public void Apply()
    {
        Button button = GetComponent<Button>();

        ColorBlock cb       = button.colors;
        cb.normalColor      = normalColor;
        cb.highlightedColor = highlightColor;
        cb.pressedColor     = pressedColor;
        cb.selectedColor    = highlightColor;
        cb.fadeDuration     = 0.1f;
        button.colors = cb;

        GetComponent<RectTransform>().sizeDelta = hitboxSize;

        Transform visual = transform.Find("Visual");
        if (visual != null)
            visual.GetComponent<RectTransform>().sizeDelta = visualSize;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ScaleVisual(hoverScale);
        PlayClip(hoverClip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ScaleVisual(1f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClip(clickClip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        _audioSource.PlayOneShot(clip);
    }

    private void ScaleVisual(float targetScale)
    {
        if (_visual == null) return;
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(ScaleCoroutine(targetScale));
    }

    private IEnumerator ScaleCoroutine(float targetScale)
    {
        Vector3 target = Vector3.one * targetScale;
        Vector3 start  = _visual.localScale;
        float elapsed  = 0f;

        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            _visual.localScale = Vector3.Lerp(start, target, elapsed / scaleDuration);
            yield return null;
        }

        _visual.localScale = target;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Apply();
    }
#endif
}

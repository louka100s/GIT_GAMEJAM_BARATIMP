using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placé directement sur le cube tireuse (qui a déjà un BoxCollider trigger).
/// Détecte le verre en drag via OnTriggerEnter/Stay/Exit.
/// Le cube brille quand le verre entre — le verre se snape au SnapPoint au relâchement.
/// </summary>
public class TapDropZone : MonoBehaviour
{
    public static readonly List<TapDropZone> All = new List<TapDropZone>();

    // ── Configuration ────────────────────────────────────────────────────

    [Header("Tireuse")]
    [Tooltip("Type de bière : blonde | rousse | brune")]
    [SerializeField] private string beerType = "blonde";

    [Tooltip("Position de snap du verre (enfant SnapPoint).")]
    [SerializeField] private Transform snapPoint;

    [Tooltip("Renderer à illuminer (laisse vide = prend celui du même GO).")]
    [SerializeField] private Renderer tapVisual;

    [Tooltip("Intensité de la surbrillance (1 = couleur d'origine, 2 = deux fois plus clair).")]
    [SerializeField, Range(1f, 4f)] private float highlightBrightness = 2f;

    // ── Runtime ──────────────────────────────────────────────────────────

    private Color              _normalColor;
    private Color              _highlightColor;
    private bool               _highlighted;
    private Renderer           _tapRenderer;
    private BoxCollider        _col;
    private MaterialPropertyBlock _mpb;

    public string    BeerType  => beerType;
    public Transform SnapPoint => snapPoint;

    // ── Unity ────────────────────────────────────────────────────────────

    private void Awake()
    {
        All.Add(this);
        _mpb         = new MaterialPropertyBlock();
        _col         = GetComponent<BoxCollider>();
        _tapRenderer = tapVisual != null ? tapVisual : GetComponent<Renderer>();
    }

    private void Start()
    {
        // Lire la couleur APRÈS que TapGizmo.Awake() a écrit dans le MPB
        if (_tapRenderer == null) return;

        _tapRenderer.GetPropertyBlock(_mpb);
        _normalColor = _mpb.HasColor(BaseColorId)
            ? _mpb.GetColor(BaseColorId)
            : (_tapRenderer.sharedMaterial.HasColor(BaseColorId)
                ? _tapRenderer.sharedMaterial.GetColor(BaseColorId)
                : _tapRenderer.sharedMaterial.color);

        _highlightColor = new Color(
            Mathf.Clamp01(_normalColor.r * highlightBrightness),
            Mathf.Clamp01(_normalColor.g * highlightBrightness),
            Mathf.Clamp01(_normalColor.b * highlightBrightness),
            _normalColor.a);
    }

    private void OnDestroy() => All.Remove(this);

    private void Update()
    {
        if (_col == null) return;

        // Interroge directement le moteur physique chaque frame
        Vector3    center   = transform.TransformPoint(_col.center);
        Vector3    halfSize = _col.size * 0.5f;
        Collider[] hits     = Physics.OverlapBox(center, halfSize, transform.rotation);

        bool anyDragging = false;
        foreach (var hit in hits)
        {
            DraggableGlass glass = hit.GetComponent<DraggableGlass>();
            if (glass != null && glass.IsDragging)
            {
                anyDragging = true;
                glass.NotifyTapEnter(this);
                break;
            }
        }

        SetHighlight(anyDragging);
    }

    // ── API publique ─────────────────────────────────────────────────────

    /// <summary>Retourne le verre actuellement snapé dans cette tireuse, ou null.</summary>
    public DraggableGlass GetSnappedGlass()
    {
        foreach (var g in DraggableGlass.All)
            if (g.IsSnappedToTap(this)) return g;
        return null;
    }

    /// <summary>Active ou désactive la surbrillance URP-compatible.</summary>
    public void SetHighlight(bool on)
    {
        if (_highlighted == on || _tapRenderer == null) return;
        _highlighted = on;
        _tapRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorId, on ? _highlightColor : _normalColor);
        _tapRenderer.SetPropertyBlock(_mpb);
    }

    // ── Privé ────────────────────────────────────────────────────────────

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f,
            $"TapDrop [{beerType}]");
    }
#endif
}


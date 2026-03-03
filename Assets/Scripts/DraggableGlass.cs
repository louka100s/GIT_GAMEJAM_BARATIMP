using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Verre draggable en 3D (plan horizontal fixe).
/// Détection via triggers 3D — TapDropZone et ServiceSlot notifient ce script.
/// Sprite mis à jour selon le type de bière et le taux de remplissage.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class DraggableGlass : MonoBehaviour
{
    // ── Sprites ──────────────────────────────────────────────────────────

    [System.Serializable]
    public class BeerSprites
    {
        [Tooltip("Type de bière : blonde | rousse | brune")]
        public string beerType;
        [Tooltip("Sprite affiché entre 40 % et 80 %.")]
        public Sprite partial;
        [Tooltip("Sprite affiché entre 80 % et 100 % (bonne bière).")]
        public Sprite full;
        [Tooltip("Sprite affiché au-delà de 100 % (débordement).")]
        public Sprite overflow;
    }

    // ── Statics ──────────────────────────────────────────────────────────

    public static readonly List<DraggableGlass> All = new List<DraggableGlass>();

    // ── Configuration ────────────────────────────────────────────────────

    [Header("Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite melangeSprite;
    [SerializeField] private BeerSprites[] beerSprites;

    [Header("Drag")]
    [Tooltip("Hauteur Y du plan horizontal de déplacement.")]
    [SerializeField] private float dragPlaneY = 0.6f;
    [Tooltip("Rayon en pixels pour re-saisir le verre depuis la tireuse.")]
    [SerializeField] private float pickupScreenRadius = 60f;

    // ── Runtime ──────────────────────────────────────────────────────────

    [Header("Runtime")]
    [SerializeField, ReadOnly] private string _beerType  = string.Empty;
    [SerializeField, ReadOnly] private float  _fillLevel = 0f;

    public string BeerType
    {
        get => _beerType;
        set { _beerType = value; UpdateSprite(); }
    }

    public float FillLevel
    {
        get => _fillLevel;
        set { _fillLevel = value; UpdateSprite(); }
    }

    public bool IsGoodBeer => _fillLevel >= 0.8f && _fillLevel <= 1.0f;
    public bool HasBeer    => _fillLevel > 0f;
    public bool IsDragging => _state == GlassState.Dragging;

    // ── État ─────────────────────────────────────────────────────────────

    private enum GlassState { Dragging, SnappedToTap, OnCounter }
    private GlassState _state = GlassState.Dragging;

    private TapDropZone _snappedTap;
    private TapDropZone _hoveredTap;
    private ServiceSlot _hoveredSlot;
    private bool        _costCharged;

    private Camera         _cam;
    private SpriteRenderer _sr;
    private Rigidbody      _rb;

    // ── Unity ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _cam = Camera.main;
        _sr  = GetComponent<SpriteRenderer>();
        _rb  = GetComponent<Rigidbody>();
        All.Add(this);

        _rb.isKinematic = true;
        _rb.useGravity  = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        BoxCollider col = GetComponent<BoxCollider>();
        col.size        = new Vector3(0.35f, 0.35f, 0.35f);
        col.isTrigger   = false;

        UpdateSprite();
    }

    private void OnDestroy() => All.Remove(this);

    private void Update()
    {
        switch (_state)
        {
            case GlassState.Dragging:
                FollowMouse();
                if (Mouse.current.leftButton.wasReleasedThisFrame)
                    OnRelease();
                break;

            case GlassState.SnappedToTap:
                if (Mouse.current.leftButton.wasPressedThisFrame
                    && !TapHandle.AnyHandlePressed
                    && IsMouseNearGlass())
                    BeginDrag();
                break;
        }
    }

    // ── API publique ─────────────────────────────────────────────────────

    /// <summary>Démarre le drag depuis GlassStack ou re-pick depuis tireuse.</summary>
    public void BeginDrag()
    {
        _state      = GlassState.Dragging;
        _snappedTap = null;
        _hoveredTap = null;
    }

    /// <summary>Snape le verre au centre de la tireuse.</summary>
    public void SnapToTap(TapDropZone tap)
    {
        _state      = GlassState.SnappedToTap;
        _snappedTap = tap;
        _hoveredTap = null;

        // Si le verre est vide, il prend le type de la tireuse
        // S'il a déjà de la bière, on ne touche à rien — le mélange se détecte à la poignée
        if (!HasBeer)
            BeerType = tap.BeerType;

        transform.position = tap.SnapPoint.position;
        _rb.position       = tap.SnapPoint.position;
    }

    /// <summary>Pose le verre sur le comptoir — non récupérable.</summary>
    public void PlaceOnCounter(Vector3 position)
    {
        _state       = GlassState.OnCounter;
        _rb.position = position;
    }

    /// <summary>Vrai si ce verre est snapé dans la tireuse donnée.</summary>
    public bool IsSnappedToTap(TapDropZone tap) =>
        _state == GlassState.SnappedToTap && _snappedTap == tap;

    /// <summary>Appelé par TapDropZone (OnTriggerEnter/Stay) quand le verre draggé survole.</summary>
    public void NotifyTapEnter(TapDropZone tap) => _hoveredTap = tap;

    /// <summary>Appelé par TapDropZone (OnTriggerExit) quand le verre quitte la zone.</summary>
    public void NotifyTapExit(TapDropZone tap)
    {
        if (_hoveredTap == tap) _hoveredTap = null;
    }

    /// <summary>Appelé par ServiceSlot (OnTriggerEnter/Stay) quand le verre draggé survole.</summary>
    public void NotifySlotEnter(ServiceSlot slot) => _hoveredSlot = slot;

    /// <summary>Appelé par ServiceSlot (OnTriggerExit) quand le verre quitte la zone.</summary>
    public void NotifySlotExit(ServiceSlot slot)
    {
        if (_hoveredSlot == slot) _hoveredSlot = null;
    }

    /// <summary>Coûte 1 $ quand fill dépasse 20 % — une seule fois par verre.</summary>
    public void TryChargeCost()
    {
        if (_costCharged) return;
        _costCharged = true;
        GameManager.Instance.SpendForBeer();
    }

    // ── Sprite ───────────────────────────────────────────────────────────

    private void UpdateSprite()
    {
        if (_sr == null) return;

        if (_fillLevel <= 0f || string.IsNullOrEmpty(_beerType))
        {
            _sr.sprite = emptySprite;
            return;
        }

        if (_beerType == "melange")
        {
            _sr.sprite = melangeSprite != null ? melangeSprite : emptySprite;
            return;
        }

        BeerSprites bs = GetBeerSprites(_beerType);
        if (bs == null) { _sr.sprite = emptySprite; return; }

        if      (_fillLevel >  1.0f) _sr.sprite = bs.overflow;
        else if (_fillLevel >= 0.8f) _sr.sprite = bs.full;
        else if (_fillLevel >= 0.4f) _sr.sprite = bs.partial;
        else                          _sr.sprite = emptySprite;
    }

    private BeerSprites GetBeerSprites(string type)
    {
        if (beerSprites == null) return null;
        foreach (var bs in beerSprites)
            if (bs.beerType == type) return bs;
        return null;
    }

    // ── Privé ────────────────────────────────────────────────────────────

    private void FollowMouse()
    {
        Ray   ray   = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up, Vector3.up * dragPlaneY);
        if (plane.Raycast(ray, out float dist))
            _rb.MovePosition(ray.GetPoint(dist));
    }

    private void OnRelease()
    {
        // Interroge directement le moteur physique au moment du relâchement
        BoxCollider col = GetComponent<BoxCollider>();
        Collider[] hits = Physics.OverlapBox(
            transform.TransformPoint(col.center),
            col.size * 0.5f,
            transform.rotation);

        TapDropZone tap  = null;
        ServiceSlot slot = null;

        foreach (var hit in hits)
        {
            if (tap  == null) tap  = hit.GetComponent<TapDropZone>();
            if (slot == null) slot = hit.GetComponent<ServiceSlot>();
        }

        if (tap  != null) { SnapToTap(tap);          return; }
        if (slot != null) { slot.ReceiveGlass(this);  return; }

        if (!HasBeer) { Destroy(gameObject); return; }

        ServiceSlot.ServeFirst(this);
    }

    private bool IsMouseNearGlass()
    {
        Vector2 gs = _cam.WorldToScreenPoint(transform.position);
        Vector2 ms = Mouse.current.position.ReadValue();
        return Vector2.Distance(gs, ms) < pickupScreenRadius;
    }
}


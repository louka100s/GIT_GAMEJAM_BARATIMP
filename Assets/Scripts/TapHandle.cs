using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Placé sur le cube-poignée visible au-dessus d'une tireuse.
/// Maintenir le clic gauche dessus remplit le verre snapé dans la tireuse associée.
/// Utilise Physics.Raycast pour la détection (BoxCollider non-trigger requis).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TapHandle : MonoBehaviour
{
    public static readonly List<TapHandle> All = new List<TapHandle>();

    /// <summary>Vrai si au moins une poignée est en cours de pression ce frame.</summary>
    public static bool AnyHandlePressed { get; private set; }

    // ── Configuration ────────────────────────────────────────────────────

    [Header("Références")]
    [Tooltip("TapDropZone associée (cube du bas).")]
    [SerializeField] private TapDropZone associatedTap;

    [Header("Remplissage")]
    [Tooltip("Vitesse de remplissage par seconde (0 → 1 = 0 → 100 %).")]
    [SerializeField, Range(0.05f, 1f)] private float fillSpeed = 0.2f;
    [Tooltip("Remplissage maximum autorisé avant de bloquer (ex : 1.15 = 115 %).")]
    [SerializeField] private float maxFill = 1.15f;

    [Header("Barre de remplissage")]
    [Tooltip("SpriteRenderer du fond (pleine hauteur fixe).")]
    [SerializeField] private SpriteRenderer fillBarBg;
    [Tooltip("SpriteRenderer du remplissage (grandit depuis le bas).")]
    [SerializeField] private SpriteRenderer fillBar;

    [Tooltip("Couleur quand fill < 80 %.")]
    [SerializeField] private Color colorLow    = Color.green;
    [Tooltip("Couleur dans la zone cible (80 – 100 %).")]
    [SerializeField] private Color colorTarget = new Color(0f, 1f, 0.3f);
    [Tooltip("Couleur en débordement (> 100 %).")]
    [SerializeField] private Color colorOver   = Color.red;

    // ── Runtime ──────────────────────────────────────────────────────────

    private Camera   _cam;
    private Collider _col;
    private bool     _isPressing;
    private bool     _barVisible;

    private float _fillBarBottomLocalY;
    private float _fillBarInitScaleY;

    // ── Unity ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _cam = Camera.main;
        _col = GetComponent<Collider>();
        _col.isTrigger = false;
        All.Add(this);

        if (fillBar != null)
        {
            _fillBarInitScaleY   = fillBar.transform.localScale.y;
            _fillBarBottomLocalY = fillBar.transform.localPosition.y - _fillBarInitScaleY * 0.5f;
        }

        HideBar();
    }

    private void OnDestroy() => All.Remove(this);

    private void Update()
    {
        bool mouseHeld = Mouse.current.leftButton.isPressed;
        _isPressing    = mouseHeld && IsMouseOverHandle();

        // Met à jour le flag statique chaque frame
        AnyHandlePressed = false;
        foreach (var h in All)
            if (h._isPressing) { AnyHandlePressed = true; break; }

        if (_isPressing)
        {
            DraggableGlass glass = associatedTap != null
                ? associatedTap.GetSnappedGlass()
                : null;

            if (glass != null)
            {
                // Mélange : bière existante d'un type différent + nouvelle tireuse
                if (glass.HasBeer
                    && glass.BeerType != associatedTap.BeerType
                    && glass.BeerType != "melange")
                {
                    glass.BeerType = "melange";
                }

                glass.FillLevel = Mathf.Min(glass.FillLevel + fillSpeed * Time.deltaTime, maxFill);

                if (glass.FillLevel > 0.2f)
                    glass.TryChargeCost();

                ShowBar(glass.FillLevel);
                return;
            }
        }

        if (_barVisible) HideBar();
    }

    // ── Privé ────────────────────────────────────────────────────────────

    private bool IsMouseOverHandle()
    {
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out RaycastHit hit) && hit.collider == _col;
    }

    private void ShowBar(float fill)
    {
        if (fillBar == null) return;

        if (!_barVisible)
        {
            fillBar.gameObject.SetActive(true);
            if (fillBarBg != null) fillBarBg.gameObject.SetActive(true);
            _barVisible = true;
        }

        float ratio = Mathf.Clamp01(fill);

        Vector3 s = fillBar.transform.localScale;
        s.y = _fillBarInitScaleY * ratio;
        fillBar.transform.localScale = s;

        // Garde le bas fixe, grandit vers le haut
        Vector3 lp = fillBar.transform.localPosition;
        lp.y = _fillBarBottomLocalY + s.y * 0.5f;
        fillBar.transform.localPosition = lp;

        fillBar.color = fill < 0.8f  ? colorLow
                      : fill <= 1.0f ? colorTarget
                                     : colorOver;
    }

    private void HideBar()
    {
        _barVisible = false;
        if (fillBar   != null) fillBar.gameObject.SetActive(false);
        if (fillBarBg != null) fillBarBg.gameObject.SetActive(false);
    }
}


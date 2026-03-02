using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Représente un verre draggable. Instancié par GlassStack.
/// Suit la souris sur un plan Z fixe (distance caméra configurable).
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DraggableGlass : MonoBehaviour
{
    // ── Singleton du verre en cours de drag (pour l'UI debug) ───────────
    public static DraggableGlass CurrentDragged { get; private set; }

    // ── Données exposées ────────────────────────────────────────────────
    [HideInInspector] public string beerType  = string.Empty;
    [HideInInspector] public float  fillLevel = 0f;
    [HideInInspector] public bool   isFilled  = false;

    // ── Configuration ───────────────────────────────────────────────────
    [SerializeField] private float dragPlaneDistance = 5f;

    // ── Privé ───────────────────────────────────────────────────────────
    private bool      _isDragging;
    private Camera    _mainCamera;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _rigidbody  = GetComponent<Rigidbody>();

        _rigidbody.isKinematic = true;
    }

    private void Update()
    {
        TryPickUpOnClick();

        if (!_isDragging) return;

        FollowMouse();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            StopDrag();
    }

    // ── Méthodes publiques ──────────────────────────────────────────────

    /// <summary>Démarre le drag depuis l'extérieur (ex : GlassStack).</summary>
    public void StartDrag()
    {
        _isDragging    = true;
        CurrentDragged = this;
    }

    /// <summary>Arrête le drag et dépose le verre à sa position actuelle.</summary>
    public void StopDrag()
    {
        _isDragging    = false;
        CurrentDragged = null;
    }

    private void OnDestroy()
    {
        if (CurrentDragged == this)
            CurrentDragged = null;
    }

    // ── Méthodes privées ────────────────────────────────────────────────

    /// <summary>Permet de re-saisir un verre déjà posé en cliquant dessus.</summary>
    private void TryPickUpOnClick()
    {
        if (_isDragging) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            StartDrag();
    }

    private void FollowMouse()
    {
        Vector3 screenPos   = Mouse.current.position.ReadValue();
        screenPos.z         = dragPlaneDistance;
        transform.position  = _mainCamera.ScreenToWorldPoint(screenPos);
    }
}

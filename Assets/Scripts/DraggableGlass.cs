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

    // ── Configuration ───────────────────────────────────────────────────
    [Header("Drag")]
    [Tooltip("Hauteur Y du plan horizontal sur lequel le verre glisse (hauteur du comptoir).")]
    [SerializeField, Range(0f, 5f)] private float dragHeight = 1f;

    // ── Runtime (visible en Inspector) ───────────────────────────────────
    [Header("Runtime — État du verre")]
    [SerializeField, ReadOnly] private string _beerType  = string.Empty;
    [SerializeField, ReadOnly] private float  _fillLevel = 0f;
    [SerializeField, ReadOnly] private bool   _isFilled  = false;
    [SerializeField, ReadOnly] private bool   _isDragging;

    // ── Accesseurs publics ───────────────────────────────────────────────
    public string beerType  { get => _beerType;  set => _beerType  = value; }
    public float  fillLevel { get => _fillLevel; set => _fillLevel = value; }
    public bool   isFilled  { get => _isFilled;  set => _isFilled  = value; }

    // ── Privé ───────────────────────────────────────────────────────────
    private Camera    _mainCamera;
    private Rigidbody _rigidbody;
    private Vector3   _originalScale;   // initialisé en Awake, utilisé par le juice (prompt 8)

    private void Awake()
    {
        _mainCamera    = Camera.main;
        _rigidbody     = GetComponent<Rigidbody>();
        _originalScale = transform.localScale;

        _rigidbody.isKinematic = true;
    }

    private void Update()
    {
        TryPickUpOnClick();

        if (!_isDragging) return;

        FollowMouse();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            OnMouseUp();
    }

    // ── Méthodes publiques ──────────────────────────────────────────────

    /// <summary>Démarre le drag depuis l'extérieur (ex : GlassStack).</summary>
    public void StartDrag()
    {
        _isDragging    = true;
        CurrentDragged = this;
    }

    /// <summary>Arrête le drag sans service (ex : abandon, pickup depuis GlassStack).</summary>
    public void StopDrag()
    {
        _isDragging    = false;
        CurrentDragged = null;
    }

    // ── Méthodes privées ────────────────────────────────────────────────

    /// <summary>Appelé au relâchement de la souris. Gère la dépose et le service multi-bières.</summary>
    private void OnMouseUp()
    {
        _isDragging = false;
        transform.localScale = _originalScale;
        transform.rotation   = Quaternion.identity;
        CurrentDragged       = null;

        if (!_isFilled)
        {
            Destroy(gameObject);
            return;
        }

        Collider[] hits  = Physics.OverlapSphere(transform.position, 0.5f);
        bool       served = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("ServiceZone"))
            {
                ClientController client = ClientQueueManager.Instance.GetServableClient();
                if (client != null)
                {
                    string result = client.ReceiveBeer(_beerType);

                    if (result == "complete")
                    {
                        // commande finie, le client a tout reçu
                        GameManager.Instance.ServeBeerSuccess();
                        client.Deactivate();
                        ClientQueueManager.Instance.RemoveServedClient(client);
                    }
                    else if (result == "correct")
                    {
                        // bonne bière mais il en veut encore — le client reste
                        // pas de paiement partiel, on paye à la fin
                    }
                    else // "wrong"
                    {
                        // mauvaise bière, client mécontent, il part
                        GameManager.Instance.ServeBeerFail();
                        client.Deactivate();
                        ClientQueueManager.Instance.RemoveServedClient(client);
                    }
                    served = true;
                }
                break;
            }
        }

        Destroy(gameObject);
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
        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane dragPlane = new Plane(Vector3.up, Vector3.up * dragHeight);
        if (dragPlane.Raycast(ray, out float distance))
            transform.position = ray.GetPoint(distance);
    }
}

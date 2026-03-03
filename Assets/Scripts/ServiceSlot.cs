using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zone 3D sur le comptoir où le joueur pose un verre pour servir le client.
/// Détecte le verre via trigger 3D (BoxCollider isTrigger=true requis sur ce GO).
/// Valide le remplissage, transmet au ClientController et gère le multi-bières.
/// </summary>
public class ServiceSlot : MonoBehaviour
{
    public static readonly List<ServiceSlot> All = new List<ServiceSlot>();

    // ── Configuration ────────────────────────────────────────────────────

    [Header("Visuel")]
    [Tooltip("Optionnel — laisse vide, le MeshRenderer du même GameObject est utilisé automatiquement.")]
    [SerializeField] private Renderer slotVisual;

    [Tooltip("Couleur de surbrillance.")]
    [SerializeField] private Color highlightColor = Color.green;

    [Header("Zone de détection")]
    [Tooltip("Taille XZ de la zone de détection (visible en éditeur).")]
    [SerializeField] private Vector2 size = new Vector2(1.5f, 1f);

    [Header("Targets de pose")]
    [Tooltip("Points où les verres se posent dans l'ordre. Crée-les comme enfants dans la scène.")]
    [SerializeField] private Transform[] slotTargets;

    // ── Runtime ──────────────────────────────────────────────────────────

    private Color              _normalColor;
    private bool               _highlighted;
    private Renderer           _slotRenderer;
    private MaterialPropertyBlock _mpb;
    private readonly List<DraggableGlass> _glassesOnCounter = new List<DraggableGlass>();

    // ── Unity ────────────────────────────────────────────────────────────

    private void Awake()
    {
        All.Add(this);
        _mpb          = new MaterialPropertyBlock();
        _slotRenderer = slotVisual != null ? slotVisual : GetComponent<Renderer>();

        if (_slotRenderer != null && _slotRenderer.sharedMaterial != null)
        {
            _normalColor = _slotRenderer.sharedMaterial.HasColor(BaseColorId)
                ? _slotRenderer.sharedMaterial.GetColor(BaseColorId)
                : _slotRenderer.sharedMaterial.color;
        }
    }

    private void OnEnable()  => ClientQueueManager.OnServiceClientChanged += OnServiceClientChanged;
    private void OnDisable() => ClientQueueManager.OnServiceClientChanged -= OnServiceClientChanged;

    /// <summary>Quand le client part sans être servi, on vide le comptoir.</summary>
    private void OnServiceClientChanged(ClientController client)
    {
        if (client == null)
            ClearCounter();
    }

    private void OnDestroy() => All.Remove(this);

    // ── Trigger 3D ───────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        DraggableGlass glass = other.GetComponent<DraggableGlass>();
        if (glass == null || !glass.IsDragging) return;
        SetHighlight(true);
        glass.NotifySlotEnter(this);
    }

    private void OnTriggerStay(Collider other)
    {
        DraggableGlass glass = other.GetComponent<DraggableGlass>();
        if (glass == null || !glass.IsDragging) return;
        SetHighlight(true);
        glass.NotifySlotEnter(this);
    }

    private void OnTriggerExit(Collider other)
    {
        DraggableGlass glass = other.GetComponent<DraggableGlass>();
        if (glass == null) return;
        SetHighlight(false);
        glass.NotifySlotExit(this);
    }

    // ── API publique ─────────────────────────────────────────────────────

    /// <summary>Active ou désactive la surbrillance du comptoir.</summary>
    public void SetHighlight(bool on)
    {
        if (_highlighted == on || _slotRenderer == null) return;
        _highlighted = on;
        if (on)
        {
            _slotRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, highlightColor);
            _slotRenderer.SetPropertyBlock(_mpb);
        }
        else
        {
            _slotRenderer.SetPropertyBlock(null);
        }
    }

    // ── URP helper ───────────────────────────────────────────────────────

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>
    /// Appelé quand un verre est déposé dans la zone de service.
    /// Gare le verre et vérifie si la commande est complète.
    /// </summary>
    public void ReceiveGlass(DraggableGlass glass)
    {
        ClientController client = ClientQueueManager.Instance.GetServableClient();

        if (client == null)
        {
            Destroy(glass.gameObject);
            return;
        }

        ParkOnCounter(glass);
        TryAutoServe(client);
    }

    /// <summary>
    /// Dès que le nombre de verres posés atteint la taille de la commande,
    /// on valide tout d'un coup sans action supplémentaire du joueur.
    /// </summary>
    private void TryAutoServe(ClientController client)
    {
        List<string> order = client.GetOrderedBeers();

        if (_glassesOnCounter.Count < order.Count) return;

        // Vérification sans ordre : chaque bière commandée doit avoir un verre correspondant
        bool allCorrect = true;
        List<DraggableGlass> remaining = new List<DraggableGlass>(_glassesOnCounter);

        foreach (string beerType in order)
        {
            int match = remaining.FindIndex(
                g => g.BeerType == beerType && client.AcceptsFill(g.FillLevel));

            if (match < 0) { allCorrect = false; break; }
            remaining.RemoveAt(match);
        }

        if (allCorrect)
        {
            GameManager.Instance.ServeBeerSuccess();
            client.characterRenderer.sprite = client.spriteContent;
        }
        else
        {
            client.ShowUnsatisfied();
            GameManager.Instance.ServeBeerFail();
        }

        client.Deactivate();
        ClientQueueManager.Instance.RemoveServedClient(client);
        ClearCounter();
    }

    /// <summary>
    /// Auto-service quand un verre rempli est lâché hors zone.
    /// Utilise le premier ServiceSlot disponible.
    /// </summary>
    public static void ServeFirst(DraggableGlass glass)
    {
        if (All.Count > 0)
            All[0].ReceiveGlass(glass);
        else
            Object.Destroy(glass.gameObject);
    }

    // ── Privé ────────────────────────────────────────────────────────────

    private void ParkOnCounter(DraggableGlass glass)
    {
        int index = _glassesOnCounter.Count;

        Vector3 pos;
        if (slotTargets != null && index < slotTargets.Length && slotTargets[index] != null)
        {
            pos = slotTargets[index].position;
        }
        else
        {
            // Fallback : dessus du collider
            BoxCollider col = GetComponent<BoxCollider>();
            float topY = col != null
                ? transform.position.y + col.center.y * transform.lossyScale.y + col.size.y * transform.lossyScale.y * 0.5f
                : transform.position.y + transform.lossyScale.y * 0.5f;
            pos = new Vector3(transform.position.x + index * 0.35f, topY, transform.position.z);
        }

        glass.PlaceOnCounter(pos);
        _glassesOnCounter.Add(glass);
    }

    private void ClearCounter()
    {
        foreach (var g in _glassesOnCounter)
            if (g != null) Destroy(g.gameObject);
        _glassesOnCounter.Clear();
    }

    // ── Gizmo éditeur ───────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, new Vector3(size.x, 0.02f, size.y));
        Gizmos.color = new Color(0f, 1f, 0f, 1f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0.02f, size.y));
        Gizmos.matrix = prev;

        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.1f, "ServiceSlot");
    }
#endif
}

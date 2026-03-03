using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Zone cliquable représentant la pile de verres.
/// Au clic, instancie le prefab Glass à la position de la souris
/// et démarre immédiatement le drag.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GlassStack : MonoBehaviour
{
    // ── Configuration ───────────────────────────────────────────────────
    [Header("Références")]
    [Tooltip("Prefab du verre à instancier au clic.")]
    [SerializeField] private GameObject glassPrefab;

    // ── Privé ───────────────────────────────────────────────────────────
    private Camera _mainCamera;

    // ── Unity ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            SpawnAndDragGlass();
    }

    // ── Méthodes privées ────────────────────────────────────────────────

    private void SpawnAndDragGlass()
    {
        if (glassPrefab == null)
        {
            Debug.LogError("[GlassStack] glassPrefab non assigné !", this);
            return;
        }

        // Spawn à la position du GlassStack — le drag horizontal prend le relais immédiatement
        GameObject     glassObj = Instantiate(glassPrefab, transform.position, Quaternion.identity);
        DraggableGlass glass    = glassObj.GetComponent<DraggableGlass>();

        if (glass != null)
            glass.StartDrag();
        else
            Debug.LogError("[GlassStack] Le prefab Glass n'a pas de composant DraggableGlass.", glassObj);
    }
}

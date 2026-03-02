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
    [SerializeField] private GameObject glassPrefab;
    [SerializeField] private float      dragPlaneDistance = 5f;

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

        Vector3 spawnScreenPos   = Mouse.current.position.ReadValue();
        spawnScreenPos.z         = dragPlaneDistance;
        Vector3 spawnWorldPos    = _mainCamera.ScreenToWorldPoint(spawnScreenPos);

        GameObject    glassObj = Instantiate(glassPrefab, spawnWorldPos, Quaternion.identity);
        DraggableGlass glass   = glassObj.GetComponent<DraggableGlass>();

        if (glass != null)
            glass.StartDrag();
        else
            Debug.LogError("[GlassStack] Le prefab Glass n'a pas de composant DraggableGlass.", glassObj);
    }
}

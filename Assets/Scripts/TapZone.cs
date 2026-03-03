using UnityEngine;

/// <summary>
/// Placé sur chaque tireuse (BoxCollider trigger).
/// Tags attendus : Tap_Blonde | Tap_Brune | Tap_Ambree.
/// Remplit le verre présent dans la zone, le détruit s'il déborde.
/// Intégré au GameManager : SpendForBeer à l'entrée, ServeBeerFail si débordement.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TapZone : MonoBehaviour
{
    // ── Configuration ───────────────────────────────────────────────────
    [Header("Remplissage")]
    [Tooltip("Vitesse de remplissage en unités par seconde (0 → 1).")]
    [SerializeField, Range(0.05f, 2f)] private float fillSpeed = 0.25f;

    [Tooltip("Seuil de remplissage à partir duquel le verre déborde.")]
    [SerializeField, Range(0.5f, 1f)]  private float overflowThreshold = 1f;

    // ── Runtime ──────────────────────────────────────────────────────────
    [Header("Runtime")]
    [SerializeField, ReadOnly] private DraggableGlass _glassInZone;

    // ── Unity ───────────────────────────────────────────────────────────

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    /// <summary>Assigne le type de bière et débite beerCost argent dès l'entrée dans la zone.</summary>
    private void OnTriggerEnter(Collider other)
    {
        DraggableGlass glass = other.GetComponent<DraggableGlass>();
        if (glass == null || glass.isFilled) return;

        _glassInZone   = glass;
        glass.beerType = gameObject.tag;
        GameManager.Instance?.SpendForBeer();
    }

    /// <summary>Remplit progressivement le verre. Détruit et pénalise si débordement.</summary>
    private void OnTriggerStay(Collider other)
    {
        DraggableGlass glass = other.GetComponent<DraggableGlass>();
        if (glass == null || glass.isFilled) return;

        glass.fillLevel = Mathf.Clamp01(glass.fillLevel + fillSpeed * Time.deltaTime);

        if (glass.fillLevel >= overflowThreshold)
        {
            Debug.Log($"[TapZone] Débordement ! Verre détruit ({glass.beerType}).");
            _glassInZone = null;
            Destroy(glass.gameObject);
            GameManager.Instance?.ServeBeerFail();
        }
    }

    /// <summary>Marque le verre comme rempli quand il quitte la tireuse.</summary>
    private void OnTriggerExit(Collider other)
    {
        DraggableGlass glass = other.GetComponent<DraggableGlass>();
        if (glass == null) return;

        if (glass.fillLevel > 0f && glass.fillLevel < overflowThreshold)
        {
            glass.isFilled = true;
            Debug.Log($"[TapZone] Verre retiré — type : {glass.beerType}, remplissage : {glass.fillLevel:P0}");
        }

        if (_glassInZone == glass)
            _glassInZone = null;
    }
}

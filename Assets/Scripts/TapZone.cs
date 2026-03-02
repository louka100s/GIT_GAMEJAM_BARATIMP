using UnityEngine;

/// <summary>
/// Placé sur chaque tireuse (BoxCollider trigger).
/// Tags attendus : Tap_Blonde | Tap_Brune | Tap_Ambree.
/// Remplit le verre présent dans la zone, le détruit s'il déborde.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TapZone : MonoBehaviour
{
    // ── Configuration ───────────────────────────────────────────────────
    public float fillSpeed = 0.25f; // unités de fillLevel par seconde

    // ── Constantes ──────────────────────────────────────────────────────
    private const string TagBlonde  = "Tap_Blonde";
    private const string TagBrune   = "Tap_Brune";
    private const string TagAmbree  = "Tap_Ambree";
    private const float  MaxFill    = 1f;

    // ── Unity ───────────────────────────────────────────────────────────

    private void Awake()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger   = true;
    }

    /// <summary>Assigne le type de bière dès l'entrée dans la zone.</summary>
    private void OnTriggerEnter(Collider other)
    {
        DraggableGlass glass = other.GetComponent<DraggableGlass>();
        if (glass == null || glass.isFilled) return;

        glass.beerType = gameObject.tag;
    }

    /// <summary>Remplit progressivement le verre. Détruit s'il déborde.</summary>
    private void OnTriggerStay(Collider other)
    {
        DraggableGlass glass = other.GetComponent<DraggableGlass>();
        if (glass == null || glass.isFilled) return;

        glass.fillLevel = Mathf.Clamp01(glass.fillLevel + fillSpeed * Time.deltaTime);

        if (glass.fillLevel >= MaxFill)
        {
            Debug.Log($"[TapZone] Débordement ! Verre détruit ({glass.beerType}).");
            Destroy(glass.gameObject);
        }
    }

    /// <summary>Marque le verre comme rempli quand il quitte la tireuse.</summary>
    private void OnTriggerExit(Collider other)
    {
        DraggableGlass glass = other.GetComponent<DraggableGlass>();
        if (glass == null) return;

        if (glass.fillLevel > 0f && glass.fillLevel < MaxFill)
        {
            glass.isFilled = true;
            Debug.Log($"[TapZone] Verre retiré — type : {glass.beerType}, remplissage : {glass.fillLevel:P0}");
        }
    }
}

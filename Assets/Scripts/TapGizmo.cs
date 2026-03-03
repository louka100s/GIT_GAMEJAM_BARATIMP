using UnityEngine;

/// <summary>
/// Affiche un gizmo éditeur sur chaque tireuse (BoxCollider trigger).
/// Expose baseColor pour contrôler la couleur et l'opacité du cube dans la scène.
/// Ne fait rien au runtime sauf appliquer la couleur.
/// </summary>
public class TapGizmo : MonoBehaviour
{
    [Header("Gizmo Éditeur")]
    public Color  gizmoColor = Color.yellow;
    public string tapName    = "Tireuse";

    [Header("Couleur de base (opacité ajustable)")]
    [ColorUsage(showAlpha: true)]
    public Color baseColor = new Color(1f, 1f, 1f, 0.4f);

    private MaterialPropertyBlock _mpb;
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private void Awake()       => ApplyColor();
    private void OnValidate()  => ApplyColor();

    /// <summary>Applique baseColor au MeshRenderer via MaterialPropertyBlock (pas d'effet sur le matériau partagé).</summary>
    private void ApplyColor()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null) return;

        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        mr.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorID, baseColor);
        mr.SetPropertyBlock(_mpb);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.color  = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(box.center, box.size);
        }

        UnityEditor.Handles.color = gizmoColor;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.8f,
            tapName,
            new GUIStyle()
            {
                normal    = { textColor = gizmoColor },
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            }
        );
    }
#endif
}


using UnityEngine;

/// <summary>
/// Affiche un gizmo éditeur sur les slots clients, SpawnPoint et DespawnPoint.
/// Ne fait rien au runtime.
/// </summary>
public class SlotGizmo : MonoBehaviour
{
    public Color  gizmoColor = Color.cyan;
    public string label      = "Slot";
    public float  radius     = 0.3f;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.5f);

        UnityEditor.Handles.color = gizmoColor;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.7f,
            label,
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

using UnityEngine;

/// <summary>
/// Affiche un gizmo éditeur sur chaque tireuse (BoxCollider trigger).
/// Ne fait rien au runtime.
/// </summary>
public class TapGizmo : MonoBehaviour
{
    public Color  gizmoColor = Color.yellow;
    public string tapName    = "Tireuse";

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

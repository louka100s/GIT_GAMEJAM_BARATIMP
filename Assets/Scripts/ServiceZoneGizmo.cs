using UnityEngine;

/// <summary>
/// Affiche un gizmo éditeur sur la ServiceZone (BoxCollider trigger).
/// Ne fait rien au runtime.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ServiceZoneGizmo : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color  = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(box.center, box.size);

        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1f,
            "ZONE DE SERVICE",
            new GUIStyle()
            {
                normal    = { textColor = Color.yellow },
                fontSize  = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            }
        );
    }
#endif
}

using UnityEngine;

/// <summary>
/// Marque un champ SerializeField comme non-éditable dans l'Inspector.
/// Utiliser avec le PropertyDrawer ReadOnlyDrawer (dossier Editor).
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }

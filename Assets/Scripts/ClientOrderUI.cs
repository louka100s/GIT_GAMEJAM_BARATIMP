using TMPro;
using UnityEngine;

/// <summary>
/// Affiche temporairement la commande du client en slot 2 en haut à gauche.
/// Attacher sur un TextMeshProUGUI dans le Canvas.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class ClientOrderUI : MonoBehaviour
{
    private TextMeshProUGUI _label;

    private void Awake() => _label = GetComponent<TextMeshProUGUI>();

    private void OnEnable()  => ClientQueueManager.OnServiceClientChanged += Refresh;
    private void OnDisable() => ClientQueueManager.OnServiceClientChanged -= Refresh;

    /// <summary>Met à jour le texte quand le client en service change.</summary>
    private void Refresh(ClientController client)
    {
        if (client == null)
        {
            _label.text = string.Empty;
            return;
        }

        System.Collections.Generic.List<string> beers = client.GetOrderedBeers();
        _label.text = "Commande : " + string.Join(" + ", beers);
    }
}

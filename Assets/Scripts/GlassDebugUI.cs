using TMPro;
using UnityEngine;

/// <summary>
/// Affiche en temps réel le beerType et le fillLevel du verre actuellement dragué.
/// Attaché au TextMeshProUGUI de l'overlay debug.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class GlassDebugUI : MonoBehaviour
{
    private TextMeshProUGUI _label;

    private const string IdleText = "— Aucun verre en main —";

    private void Awake()
    {
        _label = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        DraggableGlass glass = DraggableGlass.CurrentDragged;

        if (glass == null)
        {
            _label.text = IdleText;
            return;
        }

        string beer    = string.IsNullOrEmpty(glass.beerType) ? "vide" : glass.beerType;
        int    percent = Mathf.RoundToInt(glass.fillLevel * 100f);

        _label.text = $"Bière : <b>{beer}</b>\nRemplissage : <b>{percent}%</b>";
    }
}

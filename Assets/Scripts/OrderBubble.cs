using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Système de bulle de commande unique dans la scène.
/// Se positionne au-dessus du client au slot 2 et affiche sa commande.
/// </summary>
public class OrderBubble : MonoBehaviour
{
    public static OrderBubble Instance { get; private set; }

    [Header("Refs")]
    public SpriteRenderer bubbleSprite;
    public Transform iconsParent;

    [Header("Icon Prefabs")]
    public GameObject iconBlondePrefab;
    public GameObject iconRoussePrefab;
    public GameObject iconBrunePrefab;

    [Header("Config")]
    public Vector3 offset           = new Vector3(0f, 2f, 0f);
    [Tooltip("Scale de la bulle dans la scène (0.15 recommandé).")]
    public float   bubbleScale      = 0.15f;
    public float   iconSpacing      = 0.35f;
    public float   iconScale        = 0.4f;
    public int     iconSortingOrder = 6;

    private List<GameObject> currentIcons = new List<GameObject>();
    private Transform currentTarget;
    private bool isShowing = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Hide();
    }

    private void LateUpdate()
    {
        if (!isShowing || currentTarget == null) return;
        transform.position = currentTarget.position + offset;
    }

    /// <summary>Affiche la bulle au-dessus du client cible avec sa commande.</summary>
    public void Show(Transform target, List<string> beers)
    {
        currentTarget = target;
        isShowing     = true;
        transform.localScale = Vector3.one * bubbleScale;

        ClearIcons();
        bubbleSprite.gameObject.SetActive(true);

        float startX = -((beers.Count - 1) * iconSpacing) / 2f;

        for (int i = 0; i < beers.Count; i++)
        {
            GameObject prefab = null;
            switch (beers[i])
            {
                case "blonde": prefab = iconBlondePrefab; break;
                case "rousse": prefab = iconRoussePrefab; break;
                case "brune":  prefab = iconBrunePrefab;  break;
            }

            if (prefab != null)
            {
                GameObject icon = Instantiate(prefab, iconsParent);
                icon.transform.localPosition = new Vector3(startX + i * iconSpacing, 0f, 0f);
                icon.transform.localScale    = Vector3.one * iconScale;

                SpriteRenderer sr = icon.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = iconSortingOrder;

                currentIcons.Add(icon);
            }
        }

        transform.position = target.position + offset;
    }

    /// <summary>Grise l'icône à l'index donné (bière correcte servie).</summary>
    public void MarkIconServed(int index)
    {
        if (index >= 0 && index < currentIcons.Count)
        {
            SpriteRenderer sr = currentIcons[index].GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.3f);
        }
    }

    /// <summary>Cache la bulle. Appelé quand le client part (servi, fail, timeout).</summary>
    public void Hide()
    {
        isShowing     = false;
        currentTarget = null;
        ClearIcons();
        bubbleSprite.gameObject.SetActive(false);
    }

    private void ClearIcons()
    {
        foreach (var icon in currentIcons)
        {
            if (icon != null) Destroy(icon);
        }
        currentIcons.Clear();
    }
}

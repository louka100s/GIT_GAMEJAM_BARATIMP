using System.Collections.Generic;
using UnityEngine;

public class ClientController : MonoBehaviour
{
    [Header("Sprite States")]
    public SpriteRenderer characterRenderer;
    public Sprite spriteContent;
    public Sprite spritePasContent;

    [Header("Order Bubble")]
    public SpriteRenderer bubbleBackground;   // le sprite "bulle"
    public Transform iconsParent;              // parent vide qui contient les icônes de commande

    [Header("Beer Icons Prefabs")]
    public GameObject iconBlondePrefab;
    public GameObject iconRoussePrefab;
    public GameObject iconBrunePrefab;

    [Header("Patience — NE PAS TOUCHER, déjà fonctionnel")]
    public float maxPatience = 15f;
    private float currentPatience;
    private bool isActive = true;
    public SpriteRenderer patienceBarFill;
    public SpriteRenderer patienceBarBg;

    [HideInInspector] public int slotIndex = -1;

    // commande : liste ordonnée de bières à servir
    private List<string> orderedBeers = new List<string>();
    private List<GameObject> orderIcons = new List<GameObject>();
    private int servedCount = 0;

    private string[] beerTypes = { "blonde", "rousse", "brune" };

    public void InitializeOrder(int beerCount)
    {
        // beerCount = 1, 2 ou 3 selon la progression
        orderedBeers.Clear();
        servedCount = 0;

        for (int i = 0; i < beerCount; i++)
        {
            orderedBeers.Add(beerTypes[Random.Range(0, beerTypes.Length)]);
        }

        // affiche le sprite neutre (content par défaut)
        characterRenderer.sprite = spriteContent;

        BuildBubbleIcons();
    }

    private void BuildBubbleIcons()
    {
        // nettoie les anciennes icônes
        foreach (var icon in orderIcons)
        {
            if (icon != null) Destroy(icon);
        }
        orderIcons.Clear();

        // active la bulle
        bubbleBackground.gameObject.SetActive(true);

        // place les icônes dans la bulle, espacées horizontalement
        float spacing = 0.35f;
        float startX = -((orderedBeers.Count - 1) * spacing) / 2f;

        for (int i = 0; i < orderedBeers.Count; i++)
        {
            GameObject prefab = null;
            switch (orderedBeers[i])
            {
                case "blonde": prefab = iconBlondePrefab; break;
                case "rousse": prefab = iconRoussePrefab; break;
                case "brune":  prefab = iconBrunePrefab;  break;
            }

            if (prefab != null)
            {
                GameObject icon = Instantiate(prefab, iconsParent);
                icon.transform.localPosition = new Vector3(startX + i * spacing, 0f, 0f);
                icon.transform.localScale = Vector3.one * 0.4f;
                orderIcons.Add(icon);
            }
        }
    }

    /// <summary>
    /// Appelé quand le barman sert une bière. Retourne :
    /// "correct" si la bière correspond à la prochaine commande,
    /// "wrong" si mauvaise bière,
    /// "complete" si c'était la dernière bière et tout est correct.
    /// </summary>
    public string ReceiveBeer(string beerType)
    {
        if (servedCount >= orderedBeers.Count) return "wrong";

        if (beerType == orderedBeers[servedCount])
        {
            // bonne bière, grise l'icône correspondante
            if (servedCount < orderIcons.Count)
            {
                SpriteRenderer sr = orderIcons[servedCount].GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.3f);
            }

            servedCount++;

            if (servedCount >= orderedBeers.Count)
            {
                // commande complète
                characterRenderer.sprite = spriteContent;
                bubbleBackground.gameObject.SetActive(false);
                return "complete";
            }
            return "correct";
        }
        else
        {
            // mauvaise bière
            characterRenderer.sprite = spritePasContent;
            return "wrong";
        }
    }

    public void ShowUnsatisfied()
    {
        characterRenderer.sprite = spritePasContent;
    }

    public int GetRemainingBeers()
    {
        return orderedBeers.Count - servedCount;
    }

    // ============ PATIENCE (existant, ne pas modifier) ============

    void Start()
    {
        currentPatience = maxPatience;
    }

    void Update()
    {
        if (!isActive) return;

        currentPatience -= Time.deltaTime;
        float ratio = Mathf.Clamp01(currentPatience / maxPatience);

        Vector3 s = patienceBarFill.transform.localScale;
        s.x = ratio;
        patienceBarFill.transform.localScale = s;

        if (ratio > 0.5f)
            patienceBarFill.color = Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
        else if (ratio > 0.25f)
            patienceBarFill.color = Color.Lerp(Color.red, Color.yellow, (ratio - 0.25f) * 4f);
        else
            patienceBarFill.color = Color.red;

        if (currentPatience <= 0f)
        {
            isActive = false;
            ShowUnsatisfied();
            GameManager.Instance.ClientTimeout();
            ClientQueueManager.Instance.RemoveServedClient(this);
        }
    }

    public void SetPatience(float value)
    {
        maxPatience = value;
        currentPatience = value;
    }

    public void Deactivate()
    {
        isActive = false;
    }
}

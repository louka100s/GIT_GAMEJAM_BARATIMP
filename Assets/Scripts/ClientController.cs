using System.Collections.Generic;
using UnityEngine;

public class ClientController : MonoBehaviour
{
    [Header("Sprite States")]
    public SpriteRenderer characterRenderer;
    public Sprite spriteContent;
    public Sprite spritePasContent;

    [Header("Taille du sprite")]
    [Tooltip("Scale appliqué au root du prefab au spawn. Ajustable par personnage.")]
    [SerializeField] private float spriteScale = 1f;

    [Header("Patience")]
    [Tooltip("Patience de base de ce type de client (secondes).")]
    public float basePatience = 20f;
    [Tooltip("Patience minimale plancher pour ce type de client (secondes).")]
    public float minPatience  = 12f;

    [Header("Qualité de la bière acceptée")]
    [Tooltip("Remplissage minimum pour que le client accepte la bière (ex : 0.8 = 80 %).")]
    [Range(0f, 1f)] public float minFillAccepted = 0.8f;
    [Tooltip("Remplissage maximum pour que le client accepte la bière (ex : 1.0 = 100 %).")]
    [Range(0f, 1.2f)] public float maxFillAccepted = 1.0f;

    private float maxPatience;
    private float currentPatience;
    private bool  isActive         = true;
    private bool  _shownUnsatisfied = false;

    public SpriteRenderer patienceBarFill;
    public SpriteRenderer patienceBarBg;

    [HideInInspector] public int slotIndex = -1;

    private List<string> orderedBeers = new List<string>();
    private int servedCount = 0;

    private static readonly string[] beerTypes = { "blonde", "rousse", "brune" };

    // ── Commande ────────────────────────────────────────────────────────

    public void InitializeOrder(int beerCount)
    {
        orderedBeers.Clear();
        servedCount = 0;

        for (int i = 0; i < beerCount; i++)
            orderedBeers.Add(beerTypes[Random.Range(0, beerTypes.Length)]);

        characterRenderer.sprite = spriteContent;
        transform.localScale     = Vector3.one * spriteScale;
    }

    /// <summary>Override du scale depuis ClientQueueManager (paramètre global).</summary>
    public void SetSpriteScale(float scale)
    {
        spriteScale          = scale;
        transform.localScale = Vector3.one * scale;
    }

    /// <summary>Vrai si le remplissage du verre est dans la plage acceptée par ce client.</summary>
    public bool AcceptsFill(float fill) => fill >= minFillAccepted && fill <= maxFillAccepted;

    /// <summary>Liste ordonnée des bières demandées.</summary>
    public List<string> GetOrderedBeers() => orderedBeers;

    /// <summary>Nombre de bières déjà servies.</summary>
    public int GetServedCount() => servedCount;

    /// <summary>
    /// Appelé quand le barman sert une bière. Retourne :
    /// "correct"  — bière juste, il en reste encore,
    /// "complete" — dernière bière, commande terminée,
    /// "wrong"    — mauvaise bière.
    /// </summary>
    public string ReceiveBeer(string beerType)
    {
        if (servedCount >= orderedBeers.Count) return "wrong";

        if (beerType == orderedBeers[servedCount])
        {
            servedCount++;

            if (servedCount >= orderedBeers.Count)
            {
                characterRenderer.sprite = spriteContent;
                return "complete";
            }
            return "correct";
        }
        else
        {
            characterRenderer.sprite = spritePasContent;
            return "wrong";
        }
    }

    public void ShowUnsatisfied() => characterRenderer.sprite = spritePasContent;

    public int GetRemainingBeers() => orderedBeers.Count - servedCount;

    // ── Patience ────────────────────────────────────────────────────────

    private void Start()
    {
        currentPatience = maxPatience;
    }

    private void Update()
    {
        if (!isActive) return;

        currentPatience -= Time.deltaTime;
        float ratio = Mathf.Clamp01(currentPatience / maxPatience);

        // Barre de patience
        Vector3 s = patienceBarFill.transform.localScale;
        s.x = ratio;
        patienceBarFill.transform.localScale = s;

        if (ratio > 0.5f)
            patienceBarFill.color = Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
        else if (ratio > 0.25f)
            patienceBarFill.color = Color.Lerp(Color.red, Color.yellow, (ratio - 0.25f) * 4f);
        else
            patienceBarFill.color = Color.red;

        // Sprite mécontent à 70% de patience écoulée (30% restant)
        if (!_shownUnsatisfied && ratio <= 0.3f)
        {
            _shownUnsatisfied = true;
            ShowUnsatisfied();
        }

        // Timeout : passe derrière les autres clients et part
        if (currentPatience <= 0f)
        {
            isActive = false;

            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = -10;

            ClientQueueManager.Instance.RemoveServedClient(this);
            GameManager.Instance.ClientTimeout();
        }
    }

    /// <summary>Applique la patience calculée par ClientQueueManager.</summary>
    public void SetPatience(float value)
    {
        maxPatience     = value;
        currentPatience = value;
    }

    /// <summary>Stoppe la patience (client servi ou parti).</summary>
    public void Deactivate()
    {
        isActive = false;
    }
}

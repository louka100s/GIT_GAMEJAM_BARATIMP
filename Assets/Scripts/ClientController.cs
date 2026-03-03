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

    [Header("Patience — NE PAS TOUCHER, déjà fonctionnel")]
    public float maxPatience = 15f;
    private float currentPatience;
    private bool isActive = true;
    public SpriteRenderer patienceBarFill;
    public SpriteRenderer patienceBarBg;

    [HideInInspector] public int slotIndex = -1;

    private List<string> orderedBeers = new List<string>();
    private int servedCount = 0;

    private string[] beerTypes = { "blonde", "rousse", "brune" };

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

    /// <summary>Liste ordonnée des bières demandées (lecture seule pour OrderBubble).</summary>
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
            OrderBubble.Instance.MarkIconServed(servedCount);
            servedCount++;

            if (servedCount >= orderedBeers.Count)
            {
                characterRenderer.sprite = spriteContent;
                OrderBubble.Instance.Hide();
                return "complete";
            }
            return "correct";
        }
        else
        {
            characterRenderer.sprite = spritePasContent;
            OrderBubble.Instance.Hide();
            return "wrong";
        }
    }

    public void ShowUnsatisfied()
    {
        characterRenderer.sprite = spritePasContent;
    }

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
            OrderBubble.Instance.Hide();
            GameManager.Instance.ClientTimeout();
            ClientQueueManager.Instance.RemoveServedClient(this);
        }
    }

    /// <summary>Réinitialise la patience.</summary>
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

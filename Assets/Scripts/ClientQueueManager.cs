using System.Collections;
using UnityEngine;

public class ClientQueueManager : MonoBehaviour
{
    public static ClientQueueManager Instance { get; private set; }

    [Header("Références scène")]
    public Transform   spawnPoint;
    public Transform   despawnPoint;
    public Transform[] slots;              // 3 slots : index 0 = gauche, 2 = service

    [Header("Prefabs clients")]
    public GameObject[] clientPrefabs;

    [Header("Mouvement")]
    public float moveSpeed = 3f;

    [Header("Délai de spawn initial")]
    [Tooltip("Délai minimum (secondes) entre chaque client au démarrage.")]
    [SerializeField] private float spawnDelayMin = 1f;
    [Tooltip("Délai maximum (secondes) entre chaque client au démarrage.")]
    [SerializeField] private float spawnDelayMax = 2f;

    [Header("Taille des sprites clients")]
    [Tooltip("0 = utilise le spriteScale défini dans chaque prefab.")]
    [SerializeField] private float globalClientScale = 0f;

    // ── Tableau d'occupation : null = slot libre ─────────────────────
    private ClientController[] slotOccupants = new ClientController[3];
    private ClientController   waitingClient;

    // ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(InitialSpawnSequence());
    }

    private IEnumerator InitialSpawnSequence()
    {
        // Spawn les 3 premiers clients avec un délai entre chacun
        for (int i = 0; i < 3; i++)
        {
            if (i > 0)
                yield return new WaitForSeconds(Random.Range(spawnDelayMin, spawnDelayMax));

            ClientController c   = CreateClient();
            c.transform.position = spawnPoint.position;
            c.slotIndex          = i;
            slotOccupants[i]     = c;
        }

        // Client en attente hors champ après un dernier délai
        yield return new WaitForSeconds(Random.Range(spawnDelayMin, spawnDelayMax));
        waitingClient                    = CreateClient();
        waitingClient.transform.position = spawnPoint.position;
        waitingClient.slotIndex          = -1;

        // Bulle sur le client au slot 2 (zone de service)
        if (slotOccupants[2] != null)
            OrderBubble.Instance.Show(slotOccupants[2].transform, slotOccupants[2].GetOrderedBeers());
    }

    private void Update()
    {
        // Chaque occupant glisse en douceur vers sa cible
        for (int i = 0; i < slotOccupants.Length; i++)
        {
            if (slotOccupants[i] == null) continue;
            slotOccupants[i].transform.position = Vector3.MoveTowards(
                slotOccupants[i].transform.position,
                slots[i].position,
                moveSpeed * Time.deltaTime);
        }
    }

    // ── API publique ─────────────────────────────────────────────────

    /// <summary>Retourne le client actuellement en zone de service (slot 2).</summary>
    public ClientController GetServableClient() => slotOccupants[2];

    /// <summary>
    /// Retire un client de la file et déclenche le glissement des suivants.
    /// Vérifie que le slot de destination est libre avant chaque avancement.
    /// </summary>
    public void RemoveServedClient(ClientController client)
    {
        for (int i = 0; i < slotOccupants.Length; i++)
        {
            if (slotOccupants[i] == client)
            {
                slotOccupants[i] = null;
                break;
            }
        }
        StartCoroutine(DespawnAndShift(client));
    }

    // ── Coroutine de départ + shift ──────────────────────────────────

    private IEnumerator DespawnAndShift(ClientController client)
    {
        // Déplace le client servi/parti vers le DespawnPoint
        while (Vector3.Distance(client.transform.position, despawnPoint.position) > 0.05f)
        {
            client.transform.position = Vector3.MoveTowards(
                client.transform.position, despawnPoint.position, moveSpeed * 2f * Time.deltaTime);
            yield return null;
        }
        Destroy(client.gameObject);

        // Shift slot par slot — avance uniquement si le slot cible est libre
        if (slotOccupants[2] == null && slotOccupants[1] != null)
        {
            slotOccupants[2]           = slotOccupants[1];
            slotOccupants[1]           = null;
            slotOccupants[2].slotIndex = 2;
        }
        if (slotOccupants[1] == null && slotOccupants[0] != null)
        {
            slotOccupants[1]           = slotOccupants[0];
            slotOccupants[0]           = null;
            slotOccupants[1].slotIndex = 1;
        }
        if (slotOccupants[0] == null && waitingClient != null)
        {
            slotOccupants[0]           = waitingClient;
            slotOccupants[0].slotIndex = 0;
            waitingClient              = null;
        }

        // Bulle sur le nouveau client en slot 2
        if (slotOccupants[2] != null)
            OrderBubble.Instance.Show(slotOccupants[2].transform, slotOccupants[2].GetOrderedBeers());

        // Nouveau client en attente au SpawnPoint
        waitingClient                    = CreateClient();
        waitingClient.transform.position = spawnPoint.position;
        waitingClient.slotIndex          = -1;
    }

    // ── Création d'un client ─────────────────────────────────────────

    private ClientController CreateClient()
    {
        int              idx = Random.Range(0, clientPrefabs.Length);
        GameObject        go = Instantiate(clientPrefabs[idx]);
        ClientController  cc = go.GetComponent<ClientController>();

        cc.InitializeOrder(GetBeerCountForCurrentPhase());

        if (globalClientScale > 0f)
            cc.SetSpriteScale(globalClientScale);

        cc.SetPatience(GameManager.Instance.GetCurrentPatience());
        cc.slotIndex = -1;
        return cc;
    }

    private int GetBeerCountForCurrentPhase()
    {
        float e = Time.timeSinceLevelLoad;
        if (e < 30f)  return 1;
        if (e < 90f)  return Random.Range(1, 3);
        if (e < 150f) return Random.Range(2, 4);
        return 3;
    }
}

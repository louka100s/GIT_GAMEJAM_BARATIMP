using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Gère le spawn et le déplacement des clients vers leurs slots.
/// Logique : un client tente le slot 2 (service) en premier, puis 1, puis 0.
/// Si les 3 slots sont occupés, aucun spawn. Sinon, spawn aléatoire toutes les
/// spawnDelayMin à spawnDelayMax secondes.
/// </summary>
public class ClientQueueManager : MonoBehaviour
{
    public static ClientQueueManager Instance { get; private set; }

    /// <summary>Déclenché quand le client en slot 2 change. null = slot vide.</summary>
    public static event Action<ClientController> OnServiceClientChanged;

    [Header("Références scène")]
    public Transform   spawnPoint;
    public Transform   despawnPoint;
    public Transform[] slots;               // index 0 = gauche, 2 = service

    [Header("Prefabs clients")]
    public GameObject[] clientPrefabs;

    [Header("Mouvement")]
    public float moveSpeed = 3f;

    [Header("Spawn")]
    [Tooltip("Délai minimum (secondes) entre deux spawns.")]
    [SerializeField] private float spawnDelayMin = 5f;
    [Tooltip("Délai maximum (secondes) entre deux spawns.")]
    [SerializeField] private float spawnDelayMax = 15f;

    [Header("Taille des sprites clients")]
    [Tooltip("0 = utilise le spriteScale défini dans chaque prefab.")]
    [SerializeField] private float globalClientScale = 0f;

    private ClientController[] slotOccupants  = new ClientController[3];
    private float              _nextSpawnTime;
    private int                _totalSpawnCount;
    private bool               _serviceAnnounced;   // vrai quand l'event slot-2 a déjà été émis

    // ── Unity ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ScheduleNextSpawn();
    }

    private void Update()
    {
        for (int i = 0; i < slotOccupants.Length; i++)
        {
            if (slotOccupants[i] == null) continue;

            slotOccupants[i].transform.position = Vector3.MoveTowards(
                slotOccupants[i].transform.position,
                slots[i].position,
                moveSpeed * Time.deltaTime);

            if (Vector3.Distance(slotOccupants[i].transform.position, slots[i].position) < 0.01f)
                TryAdvanceClient(i);
        }

        // Déclenche l'event commande quand le client arrive PHYSIQUEMENT au slot 2
        if (!_serviceAnnounced && slotOccupants[2] != null &&
            Vector3.Distance(slotOccupants[2].transform.position, slots[2].position) < 0.01f)
        {
            _serviceAnnounced = true;
            OnServiceClientChanged?.Invoke(slotOccupants[2]);
        }

        if (Time.time >= _nextSpawnTime)
            TrySpawnClient();
    }

    // ── API publique ─────────────────────────────────────────────────────

    /// <summary>Retourne le client en zone de service (slot 2).</summary>
    public ClientController GetServableClient() => slotOccupants[2];

    /// <summary>Retire un client de la file, lance son départ et avance la queue.</summary>
    public void RemoveServedClient(ClientController client)
    {
        int freedSlot = -1;
        for (int i = 0; i < slotOccupants.Length; i++)
        {
            if (slotOccupants[i] != client) continue;
            slotOccupants[i] = null;
            freedSlot = i;
            break;
        }

        if (freedSlot == 2)
        {
            _serviceAnnounced = false;
            OnServiceClientChanged?.Invoke(null);
        }

        // Départ garanti avant tout appel externe susceptible de lever une exception
        StartCoroutine(DespawnClient(client));

        if (freedSlot > 0) TryAdvanceClient(freedSlot - 1);
    }

    /// <summary>
    /// Si le client au slot donné est arrivé à destination et que le slot suivant est libre,
    /// l'y déplace. La chaîne se propage naturellement via Update() au tick suivant.
    /// </summary>
    private void TryAdvanceClient(int slotIndex)
    {
        int next = slotIndex + 1;
        if (next >= slotOccupants.Length)     return;
        if (slotOccupants[slotIndex] == null) return;
        if (slotOccupants[next]      != null) return;

        slotOccupants[next]           = slotOccupants[slotIndex];
        slotOccupants[slotIndex]      = null;
        slotOccupants[next].slotIndex = next;
    }

    // ── Spawn ─────────────────────────────────────────────────────────────

    private void TrySpawnClient()
    {
        // Timer réarmé en premier — garantit qu'aucune exception en aval ne bloque le cycle
        ScheduleNextSpawn();

        // Priorité : slot 2 → slot 1 → slot 0
        int targetSlot = -1;
        for (int i = 2; i >= 0; i--)
        {
            if (slotOccupants[i] == null) { targetSlot = i; break; }
        }

        if (targetSlot == -1) return;

        ClientController client   = CreateClient();
        client.transform.position = spawnPoint.position;
        client.slotIndex          = targetSlot;
        slotOccupants[targetSlot] = client;
    }

    private void ScheduleNextSpawn()
    {
        _nextSpawnTime = Time.time + Random.Range(spawnDelayMin, spawnDelayMax);
    }

    // ── Despawn ───────────────────────────────────────────────────────────

    private IEnumerator DespawnClient(ClientController client)
    {
        while (Vector3.Distance(client.transform.position, despawnPoint.position) > 0.05f)
        {
            client.transform.position = Vector3.MoveTowards(
                client.transform.position, despawnPoint.position, moveSpeed * 2f * Time.deltaTime);
            yield return null;
        }

        Destroy(client.gameObject);
    }

    // ── Création d'un client ──────────────────────────────────────────────

    private ClientController CreateClient()
    {
        int             idx = Random.Range(0, clientPrefabs.Length);
        GameObject       go = Instantiate(clientPrefabs[idx]);
        ClientController cc = go.GetComponent<ClientController>();

        cc.InitializeOrder(GetBeerCountForCurrentPhase());

        if (globalClientScale > 0f)
            cc.SetSpriteScale(globalClientScale);

        // Patience : basePatience du prefab − 1s tous les 5 spawns, clampée à minPatience
        float reduction = Mathf.Floor(_totalSpawnCount / 5f);
        float patience  = Mathf.Max(cc.basePatience - reduction, cc.minPatience);
        cc.SetPatience(patience);

        _totalSpawnCount++;
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

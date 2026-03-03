using System.Collections.Generic;
using UnityEngine;

public class ClientQueueManager : MonoBehaviour
{
    public static ClientQueueManager Instance { get; private set; }

    public Transform spawnPoint;
    public Transform despawnPoint;
    public Transform[] slots;         // index 0=gauche, 2=droite (service)

    public GameObject[] clientPrefabs; // 5 prefabs
    public float moveSpeed = 3f;

    private List<ClientController> queue = new List<ClientController>();
    private ClientController waitingClient;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            var client = SpawnClient();
            client.transform.position = slots[i].position;
            client.slotIndex = i;
            queue.Add(client);
        }
        waitingClient = SpawnClient();
        waitingClient.transform.position = spawnPoint.position;
    }

    void Update()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] == null) continue;
            Vector3 target = slots[queue[i].slotIndex].position;
            queue[i].transform.position = Vector3.MoveTowards(
                queue[i].transform.position, target, moveSpeed * Time.deltaTime);
        }

        if (waitingClient != null && waitingClient.slotIndex >= 0)
        {
            Vector3 target = slots[waitingClient.slotIndex].position;
            waitingClient.transform.position = Vector3.MoveTowards(
                waitingClient.transform.position, target, moveSpeed * Time.deltaTime);
        }
    }

    public ClientController GetServableClient()
    {
        foreach (var c in queue)
            if (c != null && c.slotIndex == 2) return c;
        return null;
    }

    public void RemoveServedClient(ClientController client)
    {
        queue.Remove(client);
        StartCoroutine(DespawnAndShift(client));
    }

    private System.Collections.IEnumerator DespawnAndShift(ClientController client)
    {
        while (Vector3.Distance(client.transform.position, despawnPoint.position) > 0.1f)
        {
            client.transform.position = Vector3.MoveTowards(
                client.transform.position, despawnPoint.position, moveSpeed * 2f * Time.deltaTime);
            yield return null;
        }
        Destroy(client.gameObject);

        foreach (var c in queue)
            if (c != null) c.slotIndex++;

        if (waitingClient != null)
        {
            waitingClient.slotIndex = 0;
            queue.Insert(0, waitingClient);
        }

        waitingClient = SpawnClient();
        waitingClient.transform.position = spawnPoint.position;
    }

    private ClientController SpawnClient()
    {
        int index = Random.Range(0, clientPrefabs.Length);
        GameObject go = Instantiate(clientPrefabs[index]);
        ClientController cc = go.GetComponent<ClientController>();

        // nombre de bières selon le temps de jeu
        int beerCount = GetBeerCountForCurrentPhase();
        cc.InitializeOrder(beerCount);

        float patience = GameManager.Instance.GetCurrentPatience();
        cc.SetPatience(patience);

        cc.slotIndex = -1;
        return cc;
    }

    private int GetBeerCountForCurrentPhase()
    {
        float elapsed = Time.timeSinceLevelLoad;
        if (elapsed < 30f)  return 1;                        // warm-up : 1 bière
        if (elapsed < 90f)  return Random.Range(1, 3);       // montée : 1-2 bières
        if (elapsed < 150f) return Random.Range(2, 4);       // rush : 2-3 bières
        return 3;                                             // chaos : toujours 3
    }
}

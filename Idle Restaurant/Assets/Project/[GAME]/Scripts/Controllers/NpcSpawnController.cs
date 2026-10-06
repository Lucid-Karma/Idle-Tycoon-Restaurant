using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NpcSpawnController : MonoBehaviour
{
    [SerializeField] private List<GameObject> targetChairs = new();
    private int chairIndex;

    [SerializeField] private GameObject[] npcPrefabs;
    private List<GameObject> _npcList = new();
    private int amountToPool = 2;
    private Vector3 spawnPos;
    private float timeBreak;
    private int levelCustomerCount; // customers in the restaurant at once (the opening wave; each leaver is replaced)
    private int spawnedCount;       // customers sent in this shift; capped at ScoreManager.CustomersPerLevel

    void OnEnable()
    {
        EventManager.OnLevelStart.AddListener(StartLevelCustomers);
        EventManager.OnCustomerWent.AddListener(StartDelayedCreation);
    }
    void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(StartLevelCustomers);
        EventManager.OnCustomerWent.RemoveListener(StartDelayedCreation);
    }

    private void StartLevelCustomers() => StartCoroutine(CreateLevelCustomers());
    private void StartDelayedCreation() => StartCoroutine(DelayedCreation());

    void Start()
    {
        for (int i = 0; i < npcPrefabs.Length; i++)
        {
            for (int j = 0; j < amountToPool; j++)
            {
                GameObject obj = (GameObject)Instantiate(npcPrefabs[i]);
                obj.SetActive(false);
                _npcList.Add(obj);
            }
        }
        
        // Every extra table bought fills one more seat at a time (and adds a customer to the shift).
        levelCustomerCount = 3 + CafeShop.ExtraCustomers;
    }

    // A free customer of a kind the cafe is known to (adventurers start coming at higher cafe levels).
    private GameObject GetPooledNpc()
    {
        var free = _npcList.Where(x => !x.activeInHierarchy && Customers.IsUnlocked(x.GetComponent<NpcFsm>().Kind)).ToList();
        return free.Count > 0 ? free[Random.Range(0, free.Count)] : null;
    }

    // Chairs of tables not bought yet are in the list but inactive.
    private bool IsFree(GameObject chair) => chair.activeInHierarchy && chair.GetComponent<ISedile>().IsEmpty;

    public void CreateNpc()
    {
        // A shift has a fixed number of customers; extra ones used to keep arriving (and got frozen when
        // the result screen paused the game).
        if (spawnedCount >= ScoreManager.Instance.CustomersPerLevel) return;

        GameObject npc = GetPooledNpc();
        if(targetChairs.Any(IsFree))
        {
            if(npc != null)
            {
                List<GameObject> specificChairs = targetChairs.Where(IsFree).ToList();
                chairIndex = Random.Range(0, specificChairs.Count); // max is exclusive: the last chair was never picked
                //Debug.Log("SChairCount: " + specificChairs.Count + " chairIndex: " + chairIndex);
                npc.GetComponent<NpcFsm>().chair = specificChairs[chairIndex].GetComponent<ISedile>();

                spawnPos = new Vector3(21.35f, 1.2f, 8.79f);
                npc.transform.position = spawnPos;
                npc.SetActive(true);
                spawnedCount++;
            }
            
        }
    }

    // Rush-hour pacing: the first customer is at the door almost at once, the opening wave builds quickly,
    // and a free seat is taken again soon after (used to be 3–20 s and 10–30 s: long idle stretches).
    [SerializeField] private Vector2 firstArrival = new Vector2(2f, 4f);
    [SerializeField] private Vector2 openingGap = new Vector2(10f, 16f);
    [SerializeField] private Vector2 refillGap = new Vector2(5f, 11f);

    IEnumerator DelayedCreation()
    {
        timeBreak = Random.Range(refillGap.x, refillGap.y);
        yield return new WaitForSeconds(timeBreak);
        CreateNpc();
    }

    IEnumerator CreateLevelCustomers()
    {
        for (int i = 0; i < levelCustomerCount; i++)
        {
            timeBreak = i == 0 ? Random.Range(firstArrival.x, firstArrival.y) : Random.Range(openingGap.x, openingGap.y);
            yield return new WaitForSeconds(timeBreak);
            CreateNpc();
        }
    }
}

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
        NpcFsm.SeatFreed += StartDelayedCreation;
    }
    void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(StartLevelCustomers);
        NpcFsm.SeatFreed -= StartDelayedCreation;
    }

    // Once per shift: a second OnLevelStart in the same scene (it is a static event, also raised by Replay's
    // auto-start) would send a second opening wave - six customers at once - and the walkouts that follow.
    private bool waveSent;
    private void StartLevelCustomers()
    {
        if (waveSent) return;
        waveSent = true;
        StartCoroutine(CreateLevelCustomers());
    }
    private void StartDelayedCreation(Vector3 seat) => StartCoroutine(DelayedCreation(seat));

    // Customers walk in calmly from the door (NpcFsm.WalkSpeed) - except in the very first lesson, where they appear a
    // few steps from their table (NpcFsm.Approach) so the first burger is not kept waiting. The walk is taken off the
    // wait before the next one sets off, and the next one sets off as soon as a seat is freed (not once the last
    // customer has walked all the way out): seats do not stand empty while the shift lasts (user, 2026-11-07).
    private static bool FromNearby => Tutorial.Running || !CafeProgress.Taught;

    private static float WalkSeconds(Vector3 seat) => FromNearby ? 3f : Vector3.Distance(NpcFsm.Door, seat) * 1.25f / NpcFsm.WalkSpeed;

    // The usual walk, for waves sent before the seat is known.
    private float AverageWalk()
    {
        var seats = targetChairs.Where(c => c != null && c.activeInHierarchy).Select(c => c.GetComponent<ISedile>().CalculateSitPos()).ToList();
        return seats.Count == 0 ? 0f : seats.Average(WalkSeconds);
    }

    // The next shift in the same scene (ShiftFlow): a new opening wave, sized for the tables owned now.
    public void ResetForNextShift()
    {
        waveSent = false;
        spawnedCount = 0;
        levelCustomerCount = 3 + CafeShop.ExtraCustomers;
    }

    void Start()
    {
        // Every extra table bought fills one more seat at a time (and adds a customer to the shift).
        levelCustomerCount = 3 + CafeShop.ExtraCustomers;
    }

    // A free customer of a kind the cafe is known to (adventurers start coming at higher cafe levels). The pool is
    // filled as customers are needed - up to amountToPool of each - not all eighteen at the scene's start, which took
    // a good part of the load (2026-10-30: the game must open fast on phones). Picked exactly as before: at random
    // among the free ones, counting those not made yet.
    private readonly Dictionary<GameObject, int> made = new();

    private GameObject GetPooledNpc()
    {
        var free = _npcList.Where(x => !x.activeInHierarchy && Customers.IsUnlocked(x.GetComponent<NpcFsm>().Kind)).ToList();
        var notYet = new List<GameObject>();
        foreach (var prefab in npcPrefabs)
        {
            if (prefab == null || !Customers.IsUnlocked(prefab.GetComponent<NpcFsm>().Kind)) continue;
            made.TryGetValue(prefab, out int count);
            for (int j = count; j < amountToPool; j++) notYet.Add(prefab);
        }
        int pick = Random.Range(0, free.Count + notYet.Count);
        if (free.Count + notYet.Count == 0) return null;
        if (pick < free.Count) return free[pick];
        var chosen = notYet[pick - free.Count];
        made.TryGetValue(chosen, out int already);
        made[chosen] = already + 1;
        var npc = Instantiate(chosen);
        npc.SetActive(false);
        _npcList.Add(npc);
        return npc;
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
                // In the first lesson, only seats the player can see: one under the chef card at the bottom of the
                // screen got pointed at and could not be found (Tutorial.SeatInClearView).
                if (Tutorial.Running)
                {
                    var seen = specificChairs.Where(c => Tutorial.SeatInClearView(c.GetComponent<ISedile>().CalculateSitPos())).ToList();
                    if (seen.Count > 0) specificChairs = seen;
                }
                chairIndex = Random.Range(0, specificChairs.Count); // max is exclusive: the last chair was never picked
                // The lesson's customers take the seat nearest the door: the first burger is ready in seconds now
                // (Tutorial.ReadyKitchen), and a walk across the room kept it waiting (2026-10-20).
                if (Tutorial.Running)
                {
                    var door = new Vector3(21.35f, 1.2f, 8.79f);
                    chairIndex = specificChairs.IndexOf(specificChairs.OrderBy(c => Vector3.Distance(c.transform.position, door)).First());
                }
                //Debug.Log("SChairCount: " + specificChairs.Count + " chairIndex: " + chairIndex);
                var fsm = npc.GetComponent<NpcFsm>();
                fsm.chair = specificChairs[chairIndex].GetComponent<ISedile>();

                spawnPos = FromNearby ? NpcFsm.Approach(fsm.chair.CalculateSitPos()) : NpcFsm.Door;
                fsm.ExitPoint = spawnPos;
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

    IEnumerator DelayedCreation(Vector3 seat)
    {
        timeBreak = Random.Range(refillGap.x, refillGap.y) - WalkSeconds(seat);
        yield return new WaitForSeconds(Mathf.Max(0.5f, timeBreak));
        CreateNpc();
    }

    IEnumerator CreateLevelCustomers()
    {
        for (int i = 0; i < levelCustomerCount; i++)
        {
            timeBreak = i == 0 ? (CafeProgress.Taught ? Random.Range(firstArrival.x, firstArrival.y) : 0.3f)   // the lesson's first customer at once
                               : Random.Range(openingGap.x, openingGap.y);
            yield return new WaitForSeconds(Mathf.Max(i == 0 ? 0.3f : 2f, timeBreak - (i == 0 ? AverageWalk() : 0f)));
            CreateNpc();
        }
    }
}

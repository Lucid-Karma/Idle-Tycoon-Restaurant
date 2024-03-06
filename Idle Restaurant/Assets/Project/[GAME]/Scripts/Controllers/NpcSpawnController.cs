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
    private int npcIndex;
    private Vector3 spawnPos;
    private float timeBreak;
    private int levelCustomerCount; // comes from difficultyManager.

    void OnEnable()
    {
        EventManager.OnLevelStart.AddListener(() => StartCoroutine(CreateLevelCustomers()));
        EventManager.OnCustomerWent.AddListener(() => StartCoroutine(DelayedCreation()));
    }
    void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(() => StartCoroutine(CreateLevelCustomers()));
        EventManager.OnCustomerWent.RemoveListener(() => StartCoroutine(DelayedCreation()));
    }

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
        
        levelCustomerCount = 3;
    }

    private GameObject GetPooledNpc()
    {
        for (int i = 0; i < _npcList.Count; i++) 
        {
            npcIndex = Random.Range(0, _npcList.Count);
            if (!_npcList[npcIndex].activeInHierarchy) 
            {
                try
                {
                    return _npcList[npcIndex];
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Can't find a track prefab " + ex.ToString());
                    return null;
                }
            }
        }
        
        return null;
    }

    public void CreateNpc()
    {
        GameObject npc = GetPooledNpc();
        if(targetChairs.Any(x => x.GetComponent<ISedile>().IsEmpty))
        {
            if(npc != null)
            {
                List<GameObject> specificChairs = targetChairs.Where(x => x.GetComponent<ISedile>().IsEmpty).ToList();
                chairIndex = Random.Range(0, specificChairs.Count - 1);
                //Debug.Log("SChairCount: " + specificChairs.Count + " chairIndex: " + chairIndex);
                npc.GetComponent<NpcFsm>().chair = specificChairs[chairIndex].GetComponent<ISedile>();

                spawnPos = new Vector3(21.35f, 1.2f, 8.79f);
                npc.transform.position = spawnPos;
                npc.SetActive(true);
            }
            
        }
    }

    IEnumerator DelayedCreation()
    {
        timeBreak = Random.Range(3, 10);
        yield return new WaitForSeconds(timeBreak);
        CreateNpc();
    }

    IEnumerator CreateLevelCustomers()
    {
        for (int i = 0; i < levelCustomerCount; i++)
        {
            timeBreak = Random.Range(3, 10);
            yield return new WaitForSeconds(timeBreak);
            CreateNpc();
        }
    }
}

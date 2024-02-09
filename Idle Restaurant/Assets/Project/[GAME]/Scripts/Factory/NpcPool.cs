using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcPool 
{
    private List<GameObject> _pooledNPCs = new List<GameObject>();
    private int _amountToPool = 15;
    [SerializeField] private GameObject _npcPrefab;

    public void FillPool(GameObject _objectToPool)
    {
        for (int i = 0; i < _amountToPool; i++) 
        {
            GameObject obj = Object.Instantiate(_objectToPool);
            obj.SetActive(false); 
            _pooledNPCs.Add(obj);
        }
    }

    private GameObject GetPooledNpc() 
    {
        for (int i = 0; i < _pooledNPCs.Count; i++) 
        {
            if (!_pooledNPCs[i].activeInHierarchy) 
            {
                return _pooledNPCs[i];
            }
        }
        
        return null;
    }

    private Vector3 GetPos()
    {
        return Vector3.zero;
    }

    public void GetObject()   
    {
        GameObject npc = GetPooledNpc();

        if(npc != null)
        {   
            npc.transform.rotation = Quaternion.identity;
            npc.transform.position = GetPos();
            npc.SetActive(true);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientsSource : MonoBehaviour, ISpawnable, ISelectable
{
    private List<GameObject> spawnPrefabList = new List<GameObject>();
    [SerializeField] private GameObject spawnPrefab;
    private DynamicFoodPool dynamicPool;
    private Transform playerTransform;

    void Start()
    {
        dynamicPool = new DynamicFoodPool();
    
        playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
    }

    public void Spawn()
    {   
        dynamicPool.GetObject(playerTransform, spawnPrefab, spawnPrefabList);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientsSource : MonoBehaviour, ISpawnable
{
    [SerializeField] private GameObject spawnPrefab;
    private FoodPool foodPool;

    void Awake()
    {
        foodPool = new FoodPool();
        foodPool.FillPool(spawnPrefab);
    }

    void Start()
    {
        foodPool.spawnPosRef = GameObject.FindWithTag("Player").GetComponent<Transform>();
    }

    public void Spawn()
    {
        foodPool.GetObject();
    }
}

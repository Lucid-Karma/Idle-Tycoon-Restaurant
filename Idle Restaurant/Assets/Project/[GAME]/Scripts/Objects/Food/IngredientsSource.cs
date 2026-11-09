using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientsSource : MonoBehaviour, ISpawnable, ISelectable
{
    private List<GameObject> spawnPrefabList = new List<GameObject>();
    [SerializeField] private GameObject spawnPrefab;
    private DynamicFoodPool dynamicPool;
    private Transform playerTransform;

    [SerializeField] private Material defaultMaterial;


    void Start()
    {
        dynamicPool = new DynamicFoodPool();
    
        playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
    }

    public void Spawn()
    {   
        dynamicPool.GetObject(playerTransform, spawnPrefab, spawnPrefabList);
    }

    // For the first lesson's ready kitchen (Tutorial.ReadyKitchen): one of these straight onto a station, not into
    // the chef's hands.
    public EdibleBase SpawnInto(NonStackBase station)
    {
        dynamicPool ??= new DynamicFoodPool();
        dynamicPool.GetObject(station.Slot, spawnPrefab, spawnPrefabList);
        var food = dynamicPool.currentObject != null ? dynamicPool.currentObject.GetComponent<EdibleBase>() : null;
        if (food != null) station.UseFood(food);
        return food;
    }

    // What it gives, by ingredient name ("bun", "tomato"... as EdibleBase.Name and the recipes' layers): each food's
    // class is named after it.
    public string Ingredient => spawnPrefab != null && spawnPrefab.GetComponent<EdibleBase>() is EdibleBase food
        ? food.GetType().Name.ToLowerInvariant() : "";

    public Material DefaultMaterial()
    {
        return defaultMaterial;
    }

    public Vector3 SelectablePos()
    {
        return gameObject.transform.position;
    }
}

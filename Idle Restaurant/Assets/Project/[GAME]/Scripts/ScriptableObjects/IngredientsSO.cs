using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "IngredientsSO/Ingredient")]
public class IngredientsSO : ScriptableObject 
{
    public GameObject prefab;
    [Space]
    public GameObject uncookedPrefab;
    public GameObject cookedPrefab;
    public GameObject overcookedPrefab;
    [Space]
    public GameObject chopped;
    public GameObject slice;
    [Space]
    public bool shouldCooked;
    public bool shouldSliced;

    [HideInInspector] public bool isAdded = false;
    [HideInInspector] public int totalPoint = 0;
    [HideInInspector] public Dictionary<GameObject, int> cookingPoints;

    private void OnEnable()
    {
        if(!shouldCooked)   return;

        cookingPoints = new Dictionary<GameObject, int>();

        cookingPoints.Add(uncookedPrefab, Random.Range(-5, -2));
        cookingPoints.Add(cookedPrefab, 10);
        cookingPoints.Add(overcookedPrefab, Random.Range(-5, -2));
    }
}

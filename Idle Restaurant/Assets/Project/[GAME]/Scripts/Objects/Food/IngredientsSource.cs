using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientsSource : MonoBehaviour, ISpawnable
{
    [SerializeField] private GameObject ingredientPrefab;


    public GameObject Spawn()
    {
        // var food = new Tomato();
        // GameObject obj = (GameObject)Instantiate(food.pure);
        GameObject obj = (GameObject)Instantiate(ingredientPrefab);

        if(obj != null)
        {
            return obj;
        }

        Debug.LogError("There is not any instantiated Object!");
        return null;
    }
}

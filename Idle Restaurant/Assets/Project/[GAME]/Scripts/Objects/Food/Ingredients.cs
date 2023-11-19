using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ingredients : MonoBehaviour, ISpawnable
{
    [SerializeField] private GameObject ingredientPrefab;


    public GameObject Spawn()
    {
        GameObject obj = (GameObject)Instantiate(ingredientPrefab);

        if(obj != null)
        {
            return obj;
        }

        Debug.LogError("There is not any instantiated Object!");
        return null;
    }
}

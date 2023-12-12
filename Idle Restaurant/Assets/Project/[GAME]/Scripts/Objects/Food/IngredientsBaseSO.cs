using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientsBaseSO : ScriptableObject, IEdible
{
    public GameObject pure;
    private GameObject prefab;
    private bool isHolded;
    [HideInInspector] public Vector3 ingredientPos;
    [HideInInspector] public bool isAdded = false;
    [HideInInspector] public int totalPoint = 0;
    Transform hands;

    void OnEnable()
    {
        hands = GameObject.FindWithTag("Player").GetComponent<Transform>();
        isHolded = false;
    }

    // public void OnPlayerInteract() 
    // {
    //     if (isHolded) 
    //     {
    //         DropObject(); 
    //     } 
    //     else 
    //     {
    //         PickUpObject();
    //     }
    // }

    // void PickUpObject() 
    // {
    //     GameObject currentFood = SetFood(); 

    //     currentFood.transform.parent = hands;
    //     currentFood.transform.localRotation = Quaternion.identity;
    //     currentFood.transform.position = hands.position;

    //     isHolded = true;
    // }

    public void SpawnObject()
    {
        if(!isHolded)
        {
            prefab = (GameObject)Instantiate(pure);
            prefab.transform.parent = hands;
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.position = hands.position;

            //prefab.SetActive(true);

            isHolded = true;
        }
    }

    public GameObject SetFood()
    {
        if(prefab != null)  return prefab;

        return pure;
    }
}

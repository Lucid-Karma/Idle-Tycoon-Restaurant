using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SelectableBase : MonoBehaviour, IEdible
{
    protected GameObject pure;
    protected GameObject prefab;
    private bool isHolded;
    [HideInInspector] public Vector3 ingredientPos;
    [HideInInspector] public bool isAdded = false;
    [HideInInspector] public int totalPoint = 0;
    Transform hands;

    void OnEnable()
    {
        // hands = GameObject.FindWithTag("Player").GetComponent<Transform>();
        // isHolded = false;
        pure = this.gameObject;
    }

    // public void SpawnObject()
    // {
    //     if(!isHolded)
    //     {
    //         prefab = (GameObject)Instantiate(pure);
    //         prefab.transform.parent = hands;
    //         prefab.transform.localRotation = Quaternion.identity;
    //         prefab.transform.position = hands.position;

    //         isHolded = true;
    //     }
    // }

    public virtual GameObject SetFood()
    {
        if(prefab != null)  return prefab;

        return pure;
    }
}

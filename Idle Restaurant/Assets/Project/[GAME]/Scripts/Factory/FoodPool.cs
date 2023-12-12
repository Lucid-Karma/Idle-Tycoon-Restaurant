using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodPool 
{
    protected List<GameObject> _pooledObjects = new List<GameObject>();
    private int _amountToPool = 15;
    public Transform spawnPosRef;

    public void FillPool(GameObject _objectToPool)
    {
        for (int i = 0; i < _amountToPool; i++) 
        {
            GameObject obj = Object.Instantiate(_objectToPool);
            obj.SetActive(false); 
            _pooledObjects.Add(obj);
        }
    }

    private GameObject GetPooledObject() 
    {
        for (int i = 0; i < _pooledObjects.Count; i++) 
        {
            if (!_pooledObjects[i].activeInHierarchy) 
            {
                return _pooledObjects[i];
            }
        }
        
        return null;
    }

    public void GetObject()   
    {
        GameObject ingredient = GetPooledObject();
        Debug.Log("ingredientBase has been created.");

        if(ingredient != null)
        {
            ingredient.transform.parent = spawnPosRef;
            ingredient.transform.rotation = Quaternion.identity;
            ingredient.transform.position = spawnPosRef.position;
            ingredient.SetActive(true);
        }
    }
}

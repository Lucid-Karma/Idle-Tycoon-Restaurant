using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DynamicFoodPool 
{
    private List<GameObject> _pooledObjects = new();
    private GameObject poolObject;
    public GameObject currentObject;

    private GameObject ExpandPool()
    {
        GameObject obj = Object.Instantiate(poolObject);
        _pooledObjects.Add(obj);
        return obj;
    }

    private GameObject GetPooledObject() 
    {
        if (_pooledObjects != null)
        {
            for (int i = 0; i < _pooledObjects.Count; i++) 
            {
                if (!_pooledObjects[i].activeInHierarchy) 
                {
                    return _pooledObjects[i];
                }
            }
        }
        return ExpandPool();
    }

    public void GetObject(Transform spawnTransform, GameObject desiredObject, List<GameObject> desiredList)   
    {
        poolObject = desiredObject;
        _pooledObjects = desiredList;
        GameObject ingredient = GetPooledObject();

        if(ingredient != null)
        {   
            ingredient.transform.parent = spawnTransform;
            ingredient.transform.rotation = Quaternion.identity;
            ingredient.transform.position = spawnTransform.position;
            ingredient.SetActive(true);
        }
        currentObject = ingredient;
    }
}
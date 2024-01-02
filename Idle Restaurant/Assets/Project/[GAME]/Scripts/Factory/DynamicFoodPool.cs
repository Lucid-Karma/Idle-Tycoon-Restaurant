using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicFoodPool 
{
    private List<GameObject> _pooledObjects = new List<GameObject>();
    private GameObject poolObject;

    public void CreateFood(GameObject _objectToPool)
    {
        poolObject = _objectToPool;

        GameObject obj = Object.Instantiate(poolObject);
        obj.SetActive(false); 
        _pooledObjects.Add(obj);
    }

    private GameObject ExpandPool()
    {
        GameObject obj = Object.Instantiate(poolObject);
        obj.SetActive(false); 
        _pooledObjects.Add(obj);
        return obj;
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
        
        return ExpandPool();
    }

    public void GetObject(Transform spawnTransform)   
    {
        GameObject ingredient = GetPooledObject();

        if(ingredient != null)
        {   
            ingredient.transform.parent = spawnTransform;
            ingredient.transform.rotation = Quaternion.identity;
            ingredient.transform.position = spawnTransform.position;
            ingredient.SetActive(true);
        }
    }
}

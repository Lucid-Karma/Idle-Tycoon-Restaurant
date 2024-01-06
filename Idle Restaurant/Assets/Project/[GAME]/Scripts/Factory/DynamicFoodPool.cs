using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DynamicFoodPool 
{
    //private List<List<GameObject>> _mainList = new List<List<GameObject>>();
    private List<GameObject> _pooledObjects = new List<GameObject>();
    private GameObject poolObject;
    public GameObject currentObject;

    // public void CreateFood(GameObject _objectToPool, List<GameObject> objectList)
    // {
    //     _pooledObjects = objectList;
    //     poolObject = _objectToPool;

    //     GameObject obj = Object.Instantiate(poolObject);
    //     obj.SetActive(false); 
    //     _pooledObjects.Add(obj);
    //     _mainList.Add(_pooledObjects);
    //     Debug.Log("created: " + poolObject.name);
    // }

    private GameObject ExpandPool()
    {
        GameObject obj = Object.Instantiate(poolObject);
        obj.SetActive(false); 
        _pooledObjects.Add(obj);
        Debug.Log("recreated " + poolObject.name);
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
        Debug.Log("pool is NULL");
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
        Debug.Log(desiredObject.name);
        currentObject = ingredient;
    }
}

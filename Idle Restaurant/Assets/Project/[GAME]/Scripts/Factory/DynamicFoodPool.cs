using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DynamicFoodPool 
{
    public List<GameObject> _pooledObjects = new();
    private GameObject poolObject;
    public GameObject currentObject;

    private GameObject ExpandPool()
    {
        GameObject obj = Object.Instantiate(poolObject);
        _pooledObjects.Add(obj);
        //Debug.Log("name: " + obj.name + "count: " + _pooledObjects.Count);
        return obj;
    }

    private GameObject GetPooledObject() 
    {
        if (_pooledObjects != null)
        {
            for (int i = 0; i < _pooledObjects.Count; i++) 
            {
                if(_pooledObjects[i] != null)
                {
                    if (!_pooledObjects[i].activeInHierarchy) 
                    {
                        return _pooledObjects[i];
                    }
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
            //ingredient.GetComponent<Collider>().enabled = true;   ?????
            ingredient.transform.parent = spawnTransform;
            ingredient.transform.rotation = Quaternion.identity;
            ingredient.transform.position = spawnTransform.position;
            ingredient.SetActive(true);
        }
        currentObject = ingredient;
    }

    public void GetObjectWOutPos(GameObject desiredObject, List<GameObject> desiredList)   
    {
        poolObject = desiredObject;
        _pooledObjects = desiredList;
        GameObject ingredient = GetPooledObject();

        if(ingredient != null)
        {   
            ingredient.transform.rotation = Quaternion.identity;
            ingredient.SetActive(true);
        }
        currentObject = ingredient;
    }
}

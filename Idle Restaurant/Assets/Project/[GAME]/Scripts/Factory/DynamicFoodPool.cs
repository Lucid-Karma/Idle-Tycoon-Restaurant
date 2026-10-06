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
                    // Free only if it was switched off itself. activeInHierarchy also treated objects that are
                    // still in use under an inactive parent as free (e.g. an eaten burger's plate) and moved them.
                    if (!_pooledObjects[i].activeSelf)
                    {
                        var free = _pooledObjects[i].transform;
                        free.parent = null;
                        // A free object is usually still a child of whatever used it last (a bun's raw look, under
                        // the bun that went into a burger), and taking it out of there keeps its size in the world.
                        // If that burger was small at that moment (the waiter carries it on a tray) the object came
                        // out small for good: the next bun in the oven was tiny. Whatever it was inside, it comes
                        // out the size it was made.
                        free.localScale = poolObject.transform.localScale;
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

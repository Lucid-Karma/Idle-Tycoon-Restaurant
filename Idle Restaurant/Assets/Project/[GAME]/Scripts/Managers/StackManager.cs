using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StackManager : Singleton<StackManager>
{
    public List<GameObject> stackedList = new List<GameObject>();

    private float distanceBetweenObjects;

    public void Stack(GameObject stackObj, Transform parentTransform, Transform refTransform)
    {
        //stackedList.Add(stackObj);

        // if(stackedList.Count == null)
            distanceBetweenObjects = stackObj.transform.localScale.y;
        // else
        //     distanceBetweenObjects = stackedList.Last().transform.localScale.y;

        stackObj.transform.parent = parentTransform;
        Vector3 desiredPos = refTransform.localPosition;
        desiredPos.y += distanceBetweenObjects;    
        
        stackObj.transform.localRotation = Quaternion.identity;
        stackObj.transform.localPosition = desiredPos; 

        refTransform.position = stackObj.transform.position;
    }
}

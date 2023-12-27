using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpController 
{
    [HideInInspector]
    public GameObject currentPickedUpObject;
    [HideInInspector]
    public bool isPickedUp = false;
    public bool isCurrentObjEatable = false;

    public void PickUp(Transform _transform)
    {
        if(isPickedUp)  return;

        currentPickedUpObject.transform.parent = _transform;    // ???
        currentPickedUpObject.transform.localRotation = Quaternion.identity;
        currentPickedUpObject.transform.position = _transform.position;

        isPickedUp = true;
    }

    public void Drop(Transform _transform, Transform refTransform)
    {
        if(!isPickedUp)  return;

        // if(isCurrentObjEatable)
        //     StackManager.Instance.Stack(currentPickedUpObject, _transform, refTransform);
        // else
        // {
        //     currentPickedUpObject.transform.parent = _transform;
        //     currentPickedUpObject.transform.position = _transform.position;
        // }
        
    
        isPickedUp = false;
    }
}

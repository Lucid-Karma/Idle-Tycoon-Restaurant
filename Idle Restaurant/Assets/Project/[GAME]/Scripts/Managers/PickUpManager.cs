using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpManager : Singleton<PickUpManager>
{
    [HideInInspector]
    public GameObject currentPickedUpObject;
    [HideInInspector]
    public bool isPickedUp = false;

    public void PickUp(Transform _transform)
    {
        if(isPickedUp)  return;

        currentPickedUpObject.transform.parent = _transform;    // ???
        currentPickedUpObject.transform.localRotation = Quaternion.identity;
        currentPickedUpObject.transform.position = _transform.position;

        isPickedUp = true;
    }

    public void Drop(Transform _transform)
    {
        if(!isPickedUp)  return;

        currentPickedUpObject.transform.parent = _transform;
        currentPickedUpObject.transform.position = _transform.position;
    
        isPickedUp = false;
    }
}

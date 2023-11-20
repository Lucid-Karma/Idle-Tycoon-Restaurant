using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plate : MonoBehaviour, IPlaceable
{
    public Transform placeTransform { get; set; }
    public Transform refTransform { get; set; }

    void Start()
    {
        placeTransform = gameObject.transform;
        refTransform = gameObject.transform.GetChild(0).transform;
    }

    public void UpdateScale(GameObject stackedObj)
    {
        gameObject.transform.localScale += stackedObj.transform.localScale;
    }
}

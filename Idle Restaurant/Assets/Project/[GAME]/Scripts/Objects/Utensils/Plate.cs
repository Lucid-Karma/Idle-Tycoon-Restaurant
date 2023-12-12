using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plate : MonoBehaviour, IPlaceable
{
    public Transform parentTransform { get; set; }
    public Transform refTransform { get; set; }

    void Start()
    {
        parentTransform = gameObject.transform;
        refTransform = gameObject.transform.GetChild(0).transform;
    }

    public void UpdateScale(GameObject stackedObj)
    {
        gameObject.transform.localScale += stackedObj.transform.localScale;
    }
}

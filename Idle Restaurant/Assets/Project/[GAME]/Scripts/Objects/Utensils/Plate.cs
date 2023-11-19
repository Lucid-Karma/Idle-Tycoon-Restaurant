using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plate : MonoBehaviour, IPlaceable
{
    public Transform placeTransform { get; set; }

    void Start()
    {
        placeTransform = gameObject.transform;
    }
}

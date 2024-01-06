using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bun : EdibleBase
{
    private List<GameObject> slicedBuns = new List<GameObject>();
    [SerializeField] private GameObject bunSlice;

    public override GameObject SetFood()
    {
        currentVersion.SetActive(false);
        pool.GetObject(this.gameObject.transform, bunSlice, slicedBuns);
        currentVersion = pool.currentObject;
        //currentVersion.SetActive(false);    // !!!
        return currentVersion;
    }

    public override bool IsBun()
    {
        return true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttableBase : EdibleBase
{
    [HideInInspector] public bool isSliced;

    public override void Start()
    {
        isSliced = false;

        base.Start();
    }

    public virtual void SetSliced()
    {
        currentVersion.SetActive(false);
        isSliced = true;
    }

    public override GameObject SetFood()
    {
        return currentVersion;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        isSliced = false;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttableBase : EdibleBase
{
    public virtual void SetSliced()
    {
        currentVersion.SetActive(false);
    }

    public override GameObject SetFood()
    {
        if(prefab != null)  return prefab;

        return pure;
    }
}

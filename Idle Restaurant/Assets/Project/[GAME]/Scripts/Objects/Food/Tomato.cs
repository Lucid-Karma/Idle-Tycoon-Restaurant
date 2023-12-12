using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tomato : SelectableBase
{
    public override GameObject SetFood()
    {
        if(prefab != null)  return prefab;

        return pure;
    }
}

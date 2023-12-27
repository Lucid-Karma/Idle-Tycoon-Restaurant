using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EdibleBase : MonoBehaviour, IEdible
{
    protected GameObject pure;
    protected GameObject prefab;
    //public Mesh slicedMesh;
    private bool isHolded;
    [HideInInspector] public Vector3 ingredientPos;
    [HideInInspector] public bool isAdded = false;
    [HideInInspector] public int totalPoint = 0;
    [HideInInspector] public bool isLastPiece;
    Transform hands;

    IPlaceable placeable;

    void OnEnable()
    {
        pure = this.gameObject;
        isLastPiece = true;

        //GetComponent<MeshFilter>().sharedMesh = slicedMesh;
    }

    public void GetPlaceable(IPlaceable _placable)
    {
        placeable = _placable;
    }
    public void RemoveFromList()
    {
        if(placeable != null)
            placeable.RemoveFoodFromPlate(this);
    }

    public virtual GameObject SetFood()
    {
        if(prefab != null)  return prefab;

        return pure;
    }
}

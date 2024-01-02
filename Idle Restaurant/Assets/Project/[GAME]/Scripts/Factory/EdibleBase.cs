using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EdibleBase : MonoBehaviour, IEdible
{
    public GameObject purePrefab;
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
    protected DynamicFoodPool pool;

    void OnEnable()
    {
        isLastPiece = true;

        pool = new DynamicFoodPool();
        pool.CreateFood(purePrefab);

        //GetComponent<MeshFilter>().sharedMesh = slicedMesh;
    }

    void Start()
    {
        pool.GetObject(this.gameObject.transform);
    }

    public void GetPlaceable(IPlaceable _placable)
    {
        placeable = _placable;
    }
    public void RemoveFromList()
    {
        if(placeable != null)
            placeable.RemoveFood(this);
    }

    public virtual GameObject SetFood()
    {
        return pure;
    }
}

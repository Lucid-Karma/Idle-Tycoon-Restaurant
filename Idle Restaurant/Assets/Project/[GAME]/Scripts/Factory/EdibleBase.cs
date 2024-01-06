using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EdibleBase : MonoBehaviour, IEdible
{
    public GameObject purePrefab;
    protected GameObject pure;
    protected GameObject prefab;
    protected GameObject currentVersion;
    //public Mesh slicedMesh;
    private bool isHolded;
    [HideInInspector] public Vector3 ingredientPos;
    [HideInInspector] public bool isAdded = false;
    [HideInInspector] public int totalPoint = 0;
    [HideInInspector] public bool isLastPiece;
    Transform hands;

    IPlaceable placeable;
    protected DynamicFoodPool pool = new DynamicFoodPool();
    protected List<GameObject> pureList = new List<GameObject>();

    // void Awake()
    // {
    //     //GetComponent<MeshFilter>().sharedMesh = slicedMesh;
    // }

    public virtual void Start()
    {
        isLastPiece = true;

        pool.GetObject(this.gameObject.transform, purePrefab, pureList);
        currentVersion = pool.currentObject;
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

    public virtual bool IsBun()
    {
        return false;
    }
}

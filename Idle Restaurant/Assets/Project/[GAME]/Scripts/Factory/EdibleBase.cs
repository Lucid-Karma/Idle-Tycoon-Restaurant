using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EdibleBase : MonoBehaviour, IEdible
{
    public GameObject purePrefab;
    protected GameObject pure;
    protected GameObject prefab;
    protected GameObject currentVersion;
    private bool isHolded;
    [HideInInspector] public Vector3 ingredientPos;
    [HideInInspector] public bool isAdded = false;
    [HideInInspector] public int totalPoint = 0;
    [HideInInspector] public bool isLastPiece;
    Transform hands;

    public new BoxCollider collider;
    protected Vector3 colSize;
    protected Vector3 colCenter;

    protected PlaceableBase placeable;
    protected DynamicFoodPool pool = new DynamicFoodPool();
    protected List<GameObject> pureList = new List<GameObject>();

    public virtual void Start()
    {
        collider = GetComponent<BoxCollider>();
        colSize = collider.size;
        colCenter = collider.center;

        SetStarterVersion();
    }
    protected void SetStarterVersion()
    {
        isLastPiece = true;

        pool.GetObject(this.gameObject.transform, purePrefab, pureList);
        currentVersion = pool.currentObject;
    }

    public void GetPlaceable(PlaceableBase _placable)
    {
        placeable = _placable;
    }
    public void RemoveFromList()
    {
        if(placeable != null)
            placeable.RemoveFood(this);

        LeavePlaceable();
    }
    public virtual void LeavePlaceable()
    {
        placeable = null;
    }

    public virtual GameObject SetFood()
    {
        return currentVersion;
    }

    public virtual bool IsBun()
    {
        return false;
    }

    protected virtual void OnDisable()
    {
        colSize = collider.size;
        colCenter = collider.center;
    }
}

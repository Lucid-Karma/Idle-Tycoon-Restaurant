using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EdibleBase : MonoBehaviour, IEdible
{
    [HideInInspector] public string Name;
    public GameObject purePrefab;
    public float point;
    protected float defaultPoint;
    protected GameObject currentVersion;
    [HideInInspector] public bool untouchable;
    [HideInInspector] public bool isLastPiece;

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
        untouchable = false;
        point = defaultPoint;

        pool.GetObject(this.gameObject.transform, purePrefab, pureList);
        currentVersion = pool.currentObject;
    }

    public void SetPlaceable(PlaceableBase _placable)
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

    public bool IsPlaced()
    {
        return (placeable != null)? true: false;
    }

    public virtual void OnEnable()
    {
        if (currentVersion != null && !currentVersion.activeInHierarchy)
        {
            SetStarterVersion();
        }
    }

    protected virtual void OnDisable()
    {
        collider.size = colSize;
        collider.center = colCenter;

        currentVersion.SetActive(false);
    }
}

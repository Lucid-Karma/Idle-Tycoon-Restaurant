using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// How well an ingredient was prepared when it went into a burger (see BurgerReview).
public enum Prep
{
    Good,   // cooked / baked / sliced as intended
    Whole,  // never went on the chopping board
    Raw,    // never cooked or baked
    Burnt   // left too long on the pan / in the oven
}

public abstract class EdibleBase : MonoBehaviour, IEdible, ISelectable
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

    [SerializeField] protected Material defaultMaterial;

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

    public virtual Prep Preparation => Prep.Good;

    public bool IsPlaced()
    {
        return (placeable != null)? true: false;
    }

    public virtual void OnEnable()
    {
        // The pool hands out any look that is switched off, and a food waiting in the pool has its own look
        // switched off: so another food can take it (a bun going onto a plate asks for a bottom bun, and gets
        // one off a pooled bun). That food then came back with a look that now sits under another burger,
        // shown nowhere: an invisible bun in the oven with only its timer. A look that is no longer under this
        // food is lost, and it asks for a new one just as it does when its look is merely switched off.
        if (currentVersion != null && (!currentVersion.activeInHierarchy || currentVersion.transform.parent != transform))
        {
            SetStarterVersion();
            gameObject.GetComponent<Collider>().enabled = true;
        }
    }

    protected virtual void OnDisable()
    {
        collider.size = colSize;
        collider.center = colCenter;

        currentVersion.SetActive(false);
    }

    public Material DefaultMaterial()
    {
        return defaultMaterial;
    }

    public Vector3 SelectablePos()
    {
        return gameObject.transform.position;
    }
}

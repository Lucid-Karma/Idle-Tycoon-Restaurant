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

    // Where it lies: a station, a plate, a table (null while carried or in a crate).
    public PlaceableBase Placeable => placeable;

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

    // How far its look reaches below and above its own position, in metres, unrotated: what stacking it (in the
    // chef's hands, on a plate) must make room for. Its look, not its collider: a collider can be much smaller than
    // what is drawn (a whole onion's is 0.6 m for a 1.07 m onion), and the layer above it then sat inside it - the
    // onion bulged out of the stack (user, 2026-11-04). `squash` is a factor on its scale that is not its real size
    // (CarryStack's squash and stretch).
    public (float bottom, float top) LookSpan(float squash = 1f)
    {
        bool any = false;
        var box = new Bounds();
        foreach (var r in GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!any) { box = r.bounds; any = true; } else box.Encapsulate(r.bounds);
        }
        float k = 1f / Mathf.Max(0.01f, squash);
        if (!any)
        {
            float h = collider != null ? Mathf.Abs(collider.size.y * transform.lossyScale.y) : 0.12f;
            return (0f, h * k);
        }
        return ((box.min.y - transform.position.y) * k, (box.max.y - transform.position.y) * k);
    }

    // Like LookSpan, but in its parent's space and as if it stood upright, its squash taken out: what a pile in the
    // chef's hand needs. LookSpan's world box grew whenever the hand was tilted (a step of the run) as a layer was
    // added, and that layer got a gap under the next (user, 2026-11-05: "sometimes there is space between them").
    public (float bottom, float top) UprightSpan(float squash = 1f)
    {
        bool any = false;
        float lo = 0f, hi = 0f;
        var toFood = transform.worldToLocalMatrix;
        foreach (var r in GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            var m = toFood * r.localToWorldMatrix;
            var b = r.localBounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                float y = m.MultiplyPoint3x4(corner).y;
                if (!any) { lo = hi = y; any = true; }
                else { lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); }
            }
        }
        float s = transform.localScale.y / Mathf.Max(0.01f, squash);
        if (!any) return (0f, (collider != null ? Mathf.Abs(collider.size.y) : 0.12f / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y))) * s);
        return (lo * s, hi * s);
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
        // Taken out of the pool for a new use: it lies nowhere yet. A food switched off where it lay (in a burger that
        // was eaten) kept its old plate, and the next time it was picked up it "left" that plate again - taking a layer
        // count off a plate it was not on (chaos bot, 2026-11-04).
        placeable = null;
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

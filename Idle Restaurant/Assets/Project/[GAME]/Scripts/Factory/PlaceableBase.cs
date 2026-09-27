using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlaceableBase : MonoBehaviour, IPlaceable, ISelectable
{
    protected BoxCollider placeableCollider;
    [SerializeField] protected Material defaultMaterial;

    // EventManager events are static and outlive a Replay (scene reload): listeners must be named methods
    // so OnDisable really removes them. A lambda stayed subscribed, and after Replay the destroyed
    // placeable's collider threw inside OnFoodDropped and aborted the drop.
    protected virtual void OnEnable()
    {
        EventManager.OnFoodHolded.AddListener(EnableCollider);
        EventManager.OnFoodDropped.AddListener(DisableCollider);
    }
    protected virtual void OnDisable()
    {
        EventManager.OnFoodHolded.RemoveListener(EnableCollider);
        EventManager.OnFoodDropped.RemoveListener(DisableCollider);
    }
    public abstract void EnableCollider();
    private void DisableCollider() => placeableCollider.enabled = false;

    public virtual void Start()
    {
        placeableCollider = GetComponent<BoxCollider>();
    }

    public abstract void UseFood(EdibleBase ingredient);

    public abstract void RemoveFood(EdibleBase ingredient);

    public abstract bool IsSuitable(EdibleBase ingredient);

    public Material DefaultMaterial()
    {
        return defaultMaterial;
    }

    public Vector3 SelectablePos()
    {
        return gameObject.transform.position;
    }
}

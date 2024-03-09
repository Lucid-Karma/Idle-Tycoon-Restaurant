using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlaceableBase : MonoBehaviour, IPlaceable, ISelectable
{
    protected BoxCollider placeableCollider;

    protected virtual void OnEnable()
    {
        EventManager.OnFoodHolded.AddListener(EnableCollider);
        EventManager.OnFoodDropped.AddListener(() => placeableCollider.enabled = false);
    }
    protected virtual void OnDisable()
    {
        EventManager.OnFoodHolded.RemoveListener(EnableCollider);
        EventManager.OnFoodDropped.RemoveListener(() => placeableCollider.enabled = false);
    }
    public abstract void EnableCollider();

    public virtual void Start()
    {
        placeableCollider = GetComponent<BoxCollider>();
    }

    public abstract void UseFood(EdibleBase ingredient);

    public abstract void RemoveFood(EdibleBase ingredient);

    public abstract bool IsSuitable(EdibleBase ingredient);
}

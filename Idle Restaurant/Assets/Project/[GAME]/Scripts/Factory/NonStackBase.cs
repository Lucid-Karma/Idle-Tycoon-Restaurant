using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonStackBase : PlaceableBase
{
    protected GameObject currentObject;
    private Transform foodSlot;

    // Food goes into an unscaled slot at this utensil's position rather than under the utensil itself:
    // some utensil objects are non-uniformly scaled (the oven is 1.41 × 0.01 × 1.15), and a rotated child
    // of a non-uniform parent gets sheared — the food mesh and its billboard bubble came out skewed.
    private Transform FoodSlot
    {
        get
        {
            if (foodSlot == null)
            {
                foodSlot = new GameObject(name + " FoodSlot").transform;
                foodSlot.SetPositionAndRotation(transform.position, transform.rotation);
            }
            return foodSlot;
        }
    }

    public override void EnableCollider()
    {
        if(currentObject == null)
            placeableCollider.enabled = true;
    }

    // The food item on this utensil (currentObject is only its current visual).
    public EdibleBase Food { get; private set; }

    public override void UseFood(EdibleBase ingredient)
    {
        ingredient.SetPlaceable(this);
        Food = ingredient;
        currentObject = ingredient.SetFood();

        var food = ingredient.gameObject.transform;
        food.SetParent(FoodSlot, true);
        food.localPosition = Vector3.zero;
        food.localRotation = Quaternion.identity;
    }

    public override void RemoveFood(EdibleBase ingredient)
    {
        currentObject = null;
        Food = null;
    }

    public override bool IsSuitable(EdibleBase ingredient)
    {
        return true;
    }

    public bool IsHaveFood()
    {
        if(currentObject == null)   return false;
        return true;
    }

    public Hamburger GetHamburger()
    {
        return currentObject.GetComponent<Hamburger>(); 
    }
}

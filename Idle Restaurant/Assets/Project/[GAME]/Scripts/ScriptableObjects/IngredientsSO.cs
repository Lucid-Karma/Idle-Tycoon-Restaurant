using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "IngredientsSO/Ingredient")]
public class IngredientsSO : IngredientsBaseSO
{
    public GameObject chopped;
    public GameObject slice;

    void Chop() 
    {
        //choppingController.StartChopping(this); // Start chopping process
        // Change state to chopped or sliced
    }
}

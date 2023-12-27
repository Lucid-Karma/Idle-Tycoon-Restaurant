using UnityEngine;

public interface IPlaceable 
{
    void UseFood(EdibleBase ingredient);
    void RemoveFoodFromPlate(EdibleBase ingredient);
}

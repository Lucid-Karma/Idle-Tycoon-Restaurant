using UnityEngine;

public interface IPlaceable 
{
    void UseFood(EdibleBase ingredient);
    void RemoveFood(EdibleBase ingredient);
}

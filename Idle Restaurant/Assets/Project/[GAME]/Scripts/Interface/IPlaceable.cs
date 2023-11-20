using UnityEngine;

public interface IPlaceable 
{
    Transform placeTransform { get; set; }
    Transform refTransform { get; set; }

    void UpdateScale(GameObject obj);
}

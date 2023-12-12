using UnityEngine;

public interface IPlaceable 
{
    Transform parentTransform { get; set; }
    Transform refTransform { get; set; }

    void UpdateScale(GameObject obj);
}

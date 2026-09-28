using System.Collections.Generic;
using UnityEngine;

// Makes a whole piece of furniture tappable on behalf of the utensil inside it (the oven body for the
// oven tray, which is thin and hard to hit). With food in hand the tap goes to the utensil; with empty
// hands it goes to the food inside (to take it out). The highlight covers this object too.
[RequireComponent(typeof(Collider))]
public class TapProxy : MonoBehaviour
{
    [SerializeField] private NonStackBase target;

    public static readonly List<TapProxy> All = new();
    public NonStackBase Target => target;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    // The collider a tap here stands for, or null when there is nothing to do (e.g. the oven is already
    // full and the chef is holding something).
    public Collider Resolve(bool handsFull)
    {
        if (!handsFull) return target.Food != null ? target.Food.GetComponent<Collider>() : null;
        return target.IsHaveFood() ? null : target.GetComponent<Collider>();
    }
}

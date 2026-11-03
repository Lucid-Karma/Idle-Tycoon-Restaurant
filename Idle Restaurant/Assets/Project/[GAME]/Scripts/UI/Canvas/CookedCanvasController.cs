using UnityEngine;

// The cooking bubble over bacon on the griddle or a potato in the fryer (CookedBase), as BurgerCanvasController is the
// patty's: kept at a fixed world offset from the food, gone once it is taken off or has burnt.
public class CookedCanvasController : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0.5f, 2.062f, 0f);

    private CookedBase food;
    private CookedBase Food => food != null ? food : food = GetComponentInParent<CookedBase>();

    void Update()
    {
        if (!Food.IsPlaced() || Food.IsOver) gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        transform.position = Food.transform.position + worldOffset;
    }
}

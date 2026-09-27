using UnityEngine;

public class BurgerCanvasController : MonoBehaviour
{
    // Kept at a fixed world offset from the patty every frame (see BunCanvasController for why).
    [SerializeField] private Vector3 worldOffset = new Vector3(0.5f, 2.062f, 0f);

    Burger burger;
    Burger Burger{ get { return (burger == null) ? burger = GetComponentInParent<Burger>() : burger;}}

    void Update()
    {
        if(!Burger.IsPlaced())    gameObject.SetActive(false);
        if(Burger.isOver)   gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        transform.position = Burger.transform.position + worldOffset;
    }
}

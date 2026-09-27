using UnityEngine;

public class BunCanvasController : MonoBehaviour
{
    // Kept at a fixed world offset from the bun every frame. Adding/removing a world offset on enable/disable
    // drifted the bubble whenever the bun had been rotated in between (it moves between hand, oven and plate).
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.1f, -0.16f);

    Bun bun;
    Bun Bun{ get { return (bun == null) ? bun = GetComponentInParent<Bun>() : bun;}}

    void Update()
    {
        if(!Bun.IsPlaced())    gameObject.SetActive(false);
        if(Bun.isOver)   gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        transform.position = Bun.transform.position + worldOffset;
    }
}

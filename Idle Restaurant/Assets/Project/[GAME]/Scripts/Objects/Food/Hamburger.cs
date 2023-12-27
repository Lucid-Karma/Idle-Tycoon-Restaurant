using UnityEngine;

public class Hamburger : EdibleBase
{
    private BoxCollider hamCollider;

    public void ExtendCollider(GameObject stackedObj)
    {
        hamCollider = GetComponent<BoxCollider>();

        hamCollider.size = new Vector3(hamCollider.size.x, hamCollider.size.y + stackedObj.transform.localScale.y / 2, hamCollider.size.z);
        hamCollider.center = new Vector3(hamCollider.center.x, hamCollider.center.y + stackedObj.transform.localScale.y / 4, hamCollider.center.z);
    }

    public override GameObject SetFood()
    {
        return gameObject;
    }
}

using UnityEngine;

public class Bin : MonoBehaviour, ISelectable
{
    [SerializeField] private Material defaultMaterial;
    public Material DefaultMaterial()
    {
        return defaultMaterial;
    }

    public void Junk(EdibleBase edibleObject)
    {
        edibleObject.gameObject.SetActive(false);
    }

    public Vector3 SelectablePos()
    {
        return gameObject.transform.position;
    }
}

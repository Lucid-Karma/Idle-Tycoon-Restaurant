using System.Collections.Generic;
using UnityEngine;

public class Hamburger : EdibleBase
{
    List<GameObject> ingredients = new();
    readonly List<BurgerLayer> layers = new();
    private BurgerReview review;
    [SerializeField] private GameObject finishBun;
    public GameObject bunHolder;
    private Vector3 hamSize = new Vector3(0.95f, 0.1f, 0.95f);
    private Vector3 hamCenter = new Vector3(-1.490116e-08f, 0.05f, 0);

    // The six layers as they were stacked, with how each was prepared (read by the customer who eats it).
    public IReadOnlyList<BurgerLayer> Layers => layers;
    public BurgerReview Review => review ??= BurgerReview.Of(layers);

    protected override void OnDisable()
    {
        collider.size = hamSize;
        collider.center = hamCenter;

        untouchable = false;
        point = 0;

        foreach (GameObject item in ingredients)
        {
            item.SetActive(false);
        }
        ingredients.Clear();
        layers.Clear();
        review = null;
    }

    public override void Start()
    {
        pool = PoolingManager.hamburgerPool;
        pureList = PoolingManager.plateList;

        base.Start();
    }

    public void ExtendCollider(EdibleBase stackedObj)
    {
        collider = GetComponent<BoxCollider>();

        collider.size = new Vector3(collider.size.x, collider.size.y + (stackedObj.collider.size.y * 10f), collider.size.z);
        collider.center = new Vector3(collider.center.x, collider.center.y + (stackedObj.collider.center.y * 10f), collider.center.z);
    }

    public void AddIngredient(EdibleBase item)
    {
        item.gameObject.transform.parent = transform;
        ExtendCollider(item);
        ingredients.Add(item.gameObject);
        layers.Add(new BurgerLayer(item.Name, item.Preparation));
        review = null;

        item.gameObject.GetComponent<Collider>().enabled = false;
    }

    public void PutLastBun(Transform refTransform, Transform parentTransform, float distanceBetweenObjects)
    {
        pool.GetObjectWOutPos(finishBun, PoolingManager.bunTopList);
        pool.currentObject.transform.parent = parentTransform;

        Vector3 desiredPos = refTransform.localPosition;
        desiredPos.y += distanceBetweenObjects;
        pool.currentObject.transform.localRotation = Quaternion.identity;
        pool.currentObject.transform.localPosition = desiredPos;
        pool.currentObject.transform.parent = transform;
        ingredients.Add(pool.currentObject);


        collider = GetComponent<BoxCollider>();
        collider.size = new Vector3(collider.size.x, collider.size.y + (0.3071972f * 10f), collider.size.z);
        collider.center = new Vector3(collider.center.x, collider.center.y + (0.1536008f * 10f), collider.center.z);
    }

    public override GameObject SetFood()
    {
        return gameObject;
    }
}

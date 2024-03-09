using System.Collections.Generic;
using UnityEngine;

public class Hamburger : EdibleBase
{
    HashSet<string> ingredientPointSet = new();
    List<GameObject> ingredients = new();
    [SerializeField] private GameObject finishBun;
    public GameObject bunHolder;
    private Vector3 hamSize = new Vector3(0.95f, 0.1f, 0.95f);
    private Vector3 hamCenter = new Vector3(-1.490116e-08f, 0.05f, 0);

    public override void OnEnable()
    {
        EventManager.OnScoreUpdate.AddListener(() => gameObject.SetActive(false));
    }
    protected override void OnDisable()
    {
        collider.size = hamSize;
        collider.center = hamCenter;

        pool._pooledObjects.Clear();
        ingredientPointSet.Clear();

        untouchable = false;
        point = 0;

        EventManager.OnScoreUpdate.RemoveListener(() => gameObject.SetActive(false));
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

        if(!ingredientPointSet.Contains(item.Name))
        {
            point += item.point;
            defaultPoint = point;
            //Debug.Log("item's point: " + item.point);
        }
        ingredientPointSet.Add(item.Name);
        
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
        

        collider = GetComponent<BoxCollider>();
        collider.size = new Vector3(collider.size.x, collider.size.y + (0.3071972f * 10f), collider.size.z);
        collider.center = new Vector3(collider.center.x, collider.center.y + (0.1536008f * 10f), collider.center.z);
    }

    public override GameObject SetFood()
    {
        return gameObject;
    }

    public float CalculateScore()
    {
        point = point / 6;
        point *= (float)3 / 10;
        //Debug.Log("point: " + point);
        return point;
    }

}

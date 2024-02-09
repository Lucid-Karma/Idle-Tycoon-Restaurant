using System.Collections.Generic;
using UnityEngine;

public class Hamburger : EdibleBase
{
    private BoxCollider hamCollider;
    [SerializeField] private GameObject finishBun;
    public GameObject bunHolder;

    public override void Start()
    {
        pool = PoolingManager.hamburgerPool;
        pureList = PoolingManager.plateList;

        base.Start();
    }

    public void ExtendCollider(EdibleBase stackedObj)
    {
        hamCollider = GetComponent<BoxCollider>();

        hamCollider.size = new Vector3(hamCollider.size.x, hamCollider.size.y + stackedObj.collider.size.y, hamCollider.size.z);
        hamCollider.center = new Vector3(hamCollider.center.x, hamCollider.center.y + stackedObj.collider.center.y, hamCollider.center.z);
    }

    public void AddIngredient(EdibleBase item)
    {
        item.gameObject.transform.parent = transform;
        ExtendCollider(item);
        point += item.point;
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
        

        hamCollider = GetComponent<BoxCollider>();
        hamCollider.size = new Vector3(hamCollider.size.x, hamCollider.size.y + 0.3071972f, hamCollider.size.z);
        hamCollider.center = new Vector3(hamCollider.center.x, hamCollider.center.y + 0.1536008f, hamCollider.center.z);
    }

    public override GameObject SetFood()
    {
        return gameObject;
    }

    public float CalculateScore()
    {
        point = point / 6;
        Debug.Log("point: " + point);
        point *= (float)3 / 10;
        Debug.Log("point: " + point);
        return point;
    }

}

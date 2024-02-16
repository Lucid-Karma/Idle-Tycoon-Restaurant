using System.Collections.Generic;
using UnityEngine;

public class Hamburger : EdibleBase
{
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
        collider = GetComponent<BoxCollider>();

        collider.size = new Vector3(collider.size.x, collider.size.y + stackedObj.collider.size.y, collider.size.z);
        collider.center = new Vector3(collider.center.x, collider.center.y + stackedObj.collider.center.y, collider.center.z);
        //Debug.Log("stackObject's size: " + stackedObj.collider.size.y + "total size: " + collider.size.y);
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
        

        collider = GetComponent<BoxCollider>();
        collider.size = new Vector3(collider.size.x, (collider.size.y + 0.3071972f) * 10f, collider.size.z);
        collider.center = new Vector3(collider.center.x, (collider.center.y + 0.1536008f) * 10f, collider.center.z);
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

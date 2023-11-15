using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject objectToPool;
    public int objectSize;
    public int xScale, zScale;


    private Vector3 offset;
    private List<Vector3> possiblePos = new List<Vector3>();
    int xPos = 0;
    int zPos = 0;
    int posIndex = 0;


    [ContextMenu("Create")]
    void Create() 
    {
        for (int i = 0; i < xScale; i++)
        {
            for (int j = 0; j < zScale; j++)
            {
                Vector3 newPos = new Vector3(xPos,1f,zPos);
                possiblePos.Add(newPos);

                zPos += objectSize;
            }
            xPos += objectSize;
            zPos = 0;
        }

        GetObject();
    }
    
    private void GetObject()
    {
        for (int j = 0; j < zScale; j++)
        {
            for (int i = 0; i < xScale; i++)
            {
                GameObject obj = (GameObject)Instantiate(objectToPool);

                offset = possiblePos[posIndex];

                if(obj != null)
                {
                    obj.transform.position = offset;
                    obj.SetActive(true);
                    posIndex ++;
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chair : MonoBehaviour, ISedile
{
    public bool IsEmpty{ get; set; }
    public NonStackBase service;

    void Start()
    {
        IsEmpty = true;
    }
    
    public Vector3 CalculateSitPos()
    {
        return transform.position + transform.forward * 0.65f;
    }

    public Quaternion GetSedileRot()
    {
        return transform.rotation;
    }

    public NonStackBase GetTableService()
    {
        return service;
    }

    public void UpdateChairState()
    {
        IsEmpty = !service.IsHaveFood();
    }
}

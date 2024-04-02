using UnityEngine;

public interface ISedile 
{
    bool IsEmpty{ get; set; }
    Vector3 CalculateSitPos();
    Quaternion GetSedileRot();
    NonStackBase GetTableService();

    void UpdateChairState();
}

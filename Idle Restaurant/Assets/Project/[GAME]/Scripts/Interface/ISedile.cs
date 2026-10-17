using UnityEngine;

public interface ISedile 
{
    bool IsEmpty{ get; set; }
    Vector3 CalculateSitPos();
    Quaternion GetSedileRot();
    NonStackBase GetTableService();

    void UpdateChairState();

    // Lets this customer walk into (or out of) the chair's place; everyone else walks round it.
    void Open(Object who, bool open);
}

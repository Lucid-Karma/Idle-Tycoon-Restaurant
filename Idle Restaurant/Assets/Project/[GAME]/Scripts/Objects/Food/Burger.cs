using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Burger : MonoBehaviour, IEatable
{
    public GameObject CurrentFood()
    {
        return gameObject;
    }
}

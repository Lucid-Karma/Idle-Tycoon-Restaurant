using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurgerCanvasController : MonoBehaviour
{
    Burger burger;
    Burger Burger{ get { return (burger == null) ? burger = GetComponentInParent<Burger>() : burger;}}

    void OnEnable()
    {
        transform.position += new Vector3(0.5f, 0f, 0f);
    }

    void Update()
    {
        if(!Burger.IsPlaced())    gameObject.SetActive(false);
        if(Burger.isOver)   gameObject.SetActive(false);
    }

    void OnDisable()
    {
        transform.position -= new Vector3(0.5f, 0f, 0f);
    }
}

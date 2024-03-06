using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BunCanvasController : MonoBehaviour
{
    Bun bun;
    Bun Bun{ get { return (bun == null) ? bun = GetComponentInParent<Bun>() : bun;}}

    void OnEnable()
    {
        transform.position += new Vector3(0f, 0f, -0.16f);
    }

    void Update()
    {
        if(!Bun.IsPlaced())    gameObject.SetActive(false);
        if(Bun.isOver)   gameObject.SetActive(false);
    }

    void OnDisable()
    {
        transform.position -= new Vector3(0f, 0f, -0.16f);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bun : EdibleBase
{
    [SerializeField] private GameObject bunSlice;
    private bool isSlicedBun;

    #region Bake
    private int _currentBunState = 0;
    [HideInInspector] public float maxCookingTime;
    [HideInInspector] public float bakeTimer;
    [HideInInspector] public bool isOver;
    #endregion

    public override void Start()
    {
        maxCookingTime = 3.0f;
        isOver = false;

        isSlicedBun = false;
        pool = PoolingManager.bunPool;
        pureList = PoolingManager.bunPureList;

        base.Start();
    }

    public void SetCookedBun()
    {
        switch (_currentBunState)
        {
            case 0:
            Debug.Log("bun cooked.");

            _currentBunState ++;
            break;

            case 1:
            Debug.Log("bun burned.");

            _currentBunState ++;
            isOver = true;
            break;
        }
    }

    public override GameObject SetFood()
    {
        currentVersion.SetActive(false);
        pool.GetObject(this.gameObject.transform, bunSlice, PoolingManager.bunBottomList);
        currentVersion = pool.currentObject;
        isSlicedBun = true;
        return currentVersion;
    }

    public override bool IsBun()
    {
        return isSlicedBun? true: false;
    }

    void OnDisable()
    {
        _currentBunState = 0;  
        bakeTimer = 0f;
        isOver = false;
    }
}

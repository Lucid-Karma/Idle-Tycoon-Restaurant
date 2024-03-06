using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bun : EdibleBase
{
    [SerializeField] private GameObject progressBar;
    [SerializeField] private GameObject bunSlice;
    private Vector3 slicedBunColSize = new Vector3(0.693757f, 0.2f, 0.693757f);
    private Vector3 slicedBunColCenter = new Vector3(0f, 0.1f, 0f);
    private bool isSlicedBun;

    #region Bake
    private int _currentBunState = 0;
    [HideInInspector] public float maxCookingTime;
    [HideInInspector] public float bakeTimer;
    [HideInInspector] public bool isOver;
    #endregion

    public override void Start()
    {
        Name = "bun";

        maxCookingTime = 10.0f;
        isOver = false;
        defaultPoint = 1.5f;
        point = defaultPoint;

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
            point = 10f;

            _currentBunState ++;
            break;

            case 1:
            Debug.Log("bun burned.");
            point = 4f;

            _currentBunState ++;
            isOver = true;
            break;
        }
    }

    public override GameObject SetFood()
    {
        progressBar.SetActive(true);

        currentVersion.SetActive(false);
        pool.GetObject(this.gameObject.transform, bunSlice, PoolingManager.bunBottomList);
        currentVersion = pool.currentObject;
        collider.size = slicedBunColSize;
        collider.center = slicedBunColCenter;
        isSlicedBun = true;
        return currentVersion;
    }

    public override bool IsBun()
    {
        return isSlicedBun? true: false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        _currentBunState = 0;  
        bakeTimer = 0f;
        isOver = false;
        progressBar.GetComponentInChildren<CookingProgressBar>().ResetProgressBar();
    }
}

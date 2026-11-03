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

    public override Prep Preparation => _currentBunState == 0 ? Prep.Raw : _currentBunState == 1 ? Prep.Good : Prep.Burnt;

    public void SetCookedBun()
    {
        switch (_currentBunState)
        {
            case 0:
            point = 10f;
            GameSfx.Play(GameSfx.Cue.Ready);

            _currentBunState ++;
            break;

            case 1:
            point = 4f;
            GameSfx.Play(GameSfx.Cue.Burnt);
            BurnSmoke.On(this);

            _currentBunState ++;
            isOver = true;
            break;
        }
    }

    // The bottom half (the bottom of a burger in the chef's hands, as on a plate) or the whole bun (going into the oven).
    public void LookSliced(bool sliced)
    {
        if (sliced == isSlicedBun || currentVersion == null) return;
        currentVersion.SetActive(false);
        if (sliced)
        {
            pool.GetObject(transform, bunSlice, PoolingManager.bunBottomList);
            collider.size = slicedBunColSize;
            collider.center = slicedBunColCenter;
        }
        else
        {
            pool.GetObject(transform, purePrefab, pureList);
            collider.size = colSize;
            collider.center = colCenter;
        }
        currentVersion = pool.currentObject;
        currentVersion.transform.localPosition = Vector3.zero;
        currentVersion.transform.localRotation = Quaternion.identity;
        isSlicedBun = sliced;
    }

    public override GameObject SetFood()
    {
        if (placeable is CookingBase)
        {
            progressBar.SetActive(true);
        }
        else if (placeable is Plate)
        {
            currentVersion.SetActive(false);
            pool.GetObject(this.gameObject.transform, bunSlice, PoolingManager.bunBottomList);
            currentVersion = pool.currentObject;
            collider.size = slicedBunColSize;
            collider.center = slicedBunColCenter;
            isSlicedBun = true;
        }
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
        isSlicedBun = false;
        progressBar.GetComponentInChildren<CookingProgressBar>().ResetProgressBar();
    }
}

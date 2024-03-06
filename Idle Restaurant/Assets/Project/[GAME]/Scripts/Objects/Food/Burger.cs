using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Burger : EdibleBase
{
    [SerializeField] private GameObject progressBar;
    [SerializeField] private GameObject cookedBurger;
    [SerializeField] private GameObject overcookedBurger;
    private int _currentBurgerState = 0;
    [HideInInspector] public float maxCookingTime;
    [HideInInspector] public float fryingTimer;
    [HideInInspector] public bool isOver;
    

    public override void Start()
    {  
        Name = "burger";
        
        maxCookingTime = 10.0f;
        isOver = false;
        defaultPoint = 0.1f;
        point = defaultPoint;

        pool = PoolingManager.burgerPool;
        pureList = PoolingManager.burgerUncookedList;

        base.Start();
    }

    public override GameObject SetFood()
    {
        if(placeable is CookingBase)
            progressBar.SetActive(true);
        
        return currentVersion;
    }

    public void SetCooked()
    {
        switch (_currentBurgerState)
        {
            case 0:
            currentVersion.SetActive(false);
            pool.GetObject(this.gameObject.transform, cookedBurger, PoolingManager.burgerCookedList);
            currentVersion = pool.currentObject;
            point = 10f;

            _currentBurgerState ++;
            break;

            case 1:
            currentVersion.SetActive(false);
            pool.GetObject(this.gameObject.transform, overcookedBurger, PoolingManager.burgerOvercookedList);
            currentVersion = pool.currentObject;
            point = 0.2f;

            _currentBurgerState ++;
            isOver = true;
            break;
        }
    }

    // void OnEnable()
    // {
    //     if (currentVersion != null && !currentVersion.activeInHierarchy)
    //     {
    //         SetStarterVersion();
    //     }
    // }
    protected override void OnDisable()
    {
        base.OnDisable();
        _currentBurgerState = 0;  
        fryingTimer = 0f;
        isOver = false;
        progressBar.GetComponentInChildren<CookingProgressBar>().ResetProgressBar();

        //currentVersion.SetActive(false);
    }
}

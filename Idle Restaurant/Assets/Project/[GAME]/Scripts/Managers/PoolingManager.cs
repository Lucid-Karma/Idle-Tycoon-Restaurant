using System.Collections.Generic;
using UnityEngine;

public static class PoolingManager 
{
    #region Tomato
    public static DynamicFoodPool tomatoPool = new DynamicFoodPool();
    public static List<GameObject> tomatoPureList = new List<GameObject>();
    public static List<GameObject> tomatoSlicedList = new List<GameObject>();
    #endregion
    #region Burger
    public static DynamicFoodPool burgerPool = new DynamicFoodPool();
    public static List<GameObject> burgerUncookedList = new List<GameObject>();
    public static List<GameObject> burgerCookedList = new List<GameObject>();
    public static List<GameObject> burgerOvercookedList = new List<GameObject>();
    #endregion
    #region Cheese
    public static DynamicFoodPool cheesePool = new DynamicFoodPool();
    public static List<GameObject> cheesePureList = new List<GameObject>();
    public static List<GameObject> cheeseSlicedList = new List<GameObject>();
    #endregion
    #region Onion
    public static DynamicFoodPool onionPool = new DynamicFoodPool();
    public static List<GameObject> onionPureList = new List<GameObject>();
    public static List<GameObject> onionSlicedList = new List<GameObject>();
    #endregion
    #region Lettuce
    public static DynamicFoodPool lettucePool = new DynamicFoodPool();
    public static List<GameObject> lettuceSlicedList = new List<GameObject>();
    #endregion
    #region Bun
    public static DynamicFoodPool bunPool = new DynamicFoodPool();
    public static List<GameObject> bunPureList = new List<GameObject>();
    public static List<GameObject> bunBottomList = new List<GameObject>();
    public static List<GameObject> bunTopList = new List<GameObject>();
    #endregion
}

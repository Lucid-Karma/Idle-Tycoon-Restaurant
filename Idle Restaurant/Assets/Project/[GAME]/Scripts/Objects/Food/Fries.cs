using System.Collections.Generic;
using UnityEngine;

// Fries (2026-10-28): a potato from the potato crate goes into the fryer (Fryer) and comes out as a carton of golden
// fries - no board on the way, a side should not cost a burger's work. They are a side, not a layer: a customer who
// ordered them (NpcFsm.WantsFries, the little carton by their order) gets them tossed, like a snack. Raw it is still a
// potato (a bonk if thrown), burnt they are charcoal. Opens at level 5 (Stations).
public class Fries : CookedBase
{
    public override float CookSeconds => 8f;
    public override float BurnSeconds => 10f;
    protected override List<GameObject> CookedList => PoolingManager.friesList;
    protected override List<GameObject> BurntList => PoolingManager.friesBurntList;

    public override void Start()
    {
        Name = "fries";
        defaultPoint = 0.1f;
        point = defaultPoint;
        pool = PoolingManager.friesPool;
        pureList = PoolingManager.potatoList;

        base.Start();
    }

    // How it is called in a line: a potato until it has been fried.
    public static string Label(Prep prep) => prep == Prep.Raw ? "potato" : "fries";
}

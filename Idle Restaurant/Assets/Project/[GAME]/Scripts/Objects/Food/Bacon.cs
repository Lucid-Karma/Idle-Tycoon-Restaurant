using System.Collections.Generic;
using UnityEngine;

// A rasher of bacon (two strips, as it goes on a burger): from the bacon plate, crisped on the griddle (Griddle), a
// layer like any other. Quicker than a patty, so a bacon burger is not two patties' wait. Opens at level 3 (Stations).
public class Bacon : CookedBase
{
    public override float CookSeconds => 7f;
    public override float BurnSeconds => 10f;
    protected override List<GameObject> CookedList => PoolingManager.baconCookedList;
    protected override List<GameObject> BurntList => PoolingManager.baconBurntList;

    public override void Start()
    {
        Name = "bacon";
        defaultPoint = 0.1f;
        point = defaultPoint;
        pool = PoolingManager.baconPool;
        pureList = PoolingManager.baconRawList;

        base.Start();
    }
}

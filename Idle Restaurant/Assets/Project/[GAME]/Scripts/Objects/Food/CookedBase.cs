using System.Collections.Generic;
using UnityEngine;

// Food cooked on a station of its own the way a patty is on the pan (2026-10-28: the bacon griddle and the fryer,
// HeatStation): raw when it goes on, done after CookSeconds (the "ready" ding and its done look), burnt BurnSeconds
// after that (smoke and its burnt look) - a little longer than the cooking, since it is usually waiting while the
// patty and the bun are made. Taken off and put back, it carries on where it was. Its three looks come from
// pools, like the patty's (DynamicFoodPool). The cooking bubble over it (UICookingBubble) runs on the same clock.
public abstract class CookedBase : EdibleBase
{
    [SerializeField] private GameObject progressBar;
    [SerializeField] private GameObject cookedVersion, burntVersion;

    private int stage;                           // 0 raw, 1 done, 2 burnt
    [HideInInspector] public float cookTimer;    // how long it has cooked at this stage (kept while it is off the heat)

    public abstract float CookSeconds { get; }
    public virtual float BurnSeconds => CookSeconds;
    // How long this stage lasts on the heat: until done, then until burnt.
    public float StageSeconds => stage == 0 ? CookSeconds : BurnSeconds;
    protected abstract List<GameObject> CookedList { get; }
    protected abstract List<GameObject> BurntList { get; }

    public bool IsOver => stage >= 2;
    public override Prep Preparation => stage == 0 ? Prep.Raw : stage == 1 ? Prep.Good : Prep.Burnt;

    public override void Start()
    {
        base.Start();
        if (progressBar != null) progressBar.GetComponentInChildren<CookingProgressBar>(true).SetTimes(CookSeconds, CookSeconds + BurnSeconds);
    }

    public override GameObject SetFood()
    {
        if (placeable is CookingBase && progressBar != null) progressBar.SetActive(true);
        return currentVersion;
    }

    // Its station's clock reached the next stage.
    public void Advance()
    {
        if (stage >= 2) return;
        currentVersion.SetActive(false);
        pool.GetObject(transform, stage == 0 ? cookedVersion : burntVersion, stage == 0 ? CookedList : BurntList);
        currentVersion = pool.currentObject;
        stage++;
        if (stage == 1)
        {
            point = 10f;
            GameSfx.Play(GameSfx.Cue.Ready);
        }
        else
        {
            point = 0.2f;
            GameSfx.Play(GameSfx.Cue.Burnt);
            BurnSmoke.On(this);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        stage = 0;
        cookTimer = 0f;
        if (progressBar != null) progressBar.GetComponentInChildren<CookingProgressBar>(true).ResetProgressBar();
    }
}

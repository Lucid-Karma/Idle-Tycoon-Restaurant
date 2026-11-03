using UnityEngine;

// A station that cooks one kind of food (CookedBase) the way the pan cooks a patty: the bacon griddle (Griddle) and the
// fryer (Fryer). One thing at a time; it sizzles while it cooks (and the fryer's oil bubbles), goes quiet once it has
// burnt. Built by Tools/Chibi Cafe/16 (CafeStationsBuilder), shown from its level on (KitchenStations).
public abstract class HeatStation : CookingBase
{
    [SerializeField] private ParticleSystem bubbles;   // while it cooks: the fryer's oil bubbling, the griddle's steam

    private CookedBase food;

    // What it cooks.
    public abstract bool Takes(EdibleBase food);

    // Deep oil (the fryer) crackles; a hot plate (the griddle) sizzles like the pan.
    protected virtual bool DeepFries => false;

    private void Sound(bool on)
    {
        if (DeepFries) GameSfx.Fry(this, on);
        else GameSfx.Sizzle(this, on);
    }

    void Update()
    {
        bool cooking = currentObject != null && state == State.Cook && food != null;
        bool loud = cooking && food.Preparation != Prep.Burnt;
        Sound(loud);
        if (bubbles != null)
        {
            if (loud && !bubbles.isPlaying) bubbles.Play();
            else if (!loud && bubbles.isPlaying) bubbles.Stop();
        }
        if (!cooking || food.IsOver) return;
        cookingTimer += Time.deltaTime * Tutorial.CookSpeed;
        if (cookingTimer > food.StageSeconds)
        {
            food.Advance();
            cookingTimer = 0f;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Sound(false);
    }

    public override void UseFood(EdibleBase ingredient)
    {
        // Busy, or not its kind of food: nothing happens (and IsSuitable says no, so he keeps it).
        if (currentObject != null || ingredient is not CookedBase cooked || !Takes(cooked)) return;
        food = cooked;
        base.UseFood(ingredient);
        cookingTimer = food.cookTimer;
        state = food.IsOver ? State.Idle : State.Cook;
    }

    public override void RemoveFood(EdibleBase ingredient)
    {
        if (food != null) food.cookTimer = cookingTimer;
        base.RemoveFood(ingredient);
        food = null;
        state = State.Idle;
    }

    public override bool IsSuitable(EdibleBase ingredient) => ingredient != null && ingredient == food;

    // For hints and the lesson's next tap: would it take this food now?
    public bool Accepts(EdibleBase held) => currentObject == null && held is CookedBase && Takes(held) && held.Preparation == Prep.Raw;
}

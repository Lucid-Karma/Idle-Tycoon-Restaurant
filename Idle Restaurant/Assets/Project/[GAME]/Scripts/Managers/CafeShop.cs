using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeKind { Kitchen, Dining, Chef, Staff }

// One thing the shop sells. Bought things stay bought (CafeProgress), and are put in place again every
// time the scene loads.
[Serializable]
public class Upgrade
{
    public string id;
    public string title;
    public string blurb;              // what it does, in a few words
    public int price;
    public int level = 1;             // cafe level that opens it in the shop
    public UpgradeKind kind;
    public bool isHat;                // one hat at a time: owned hats can be put on and taken off
    public Sprite icon;
    public GameObject[] show = Array.Empty<GameObject>();   // appears once bought (a hat: while worn)
    public GameObject[] hide = Array.Empty<GameObject>();   // goes away once bought

    [Header("Perks")]
    public int extraCustomers;        // more customers per shift (counts from the next shift)
    public float patienceBonus;       // 0.15 = customers wait 15% longer
    public int tipJar;                // extra $ on every order rated 4 stars or more
    public float chefSpeed;           // 0.15 = the chef moves 15% faster
}

// The cafe's upgrades: what's for sale, what it does, and putting bought things in the scene.
// Growth is gated twice: money buys things, and the cafe level (stars from customers) opens the shelves.
public class CafeShop : MonoBehaviour
{
    public static CafeShop Instance { get; private set; }

    // Something was bought or a hat changed: cached lists of pans, chairs, service points... need a refresh.
    // Static and outliving a Replay: subscribe with named methods and unsubscribe in OnDisable.
    public static event Action<Upgrade> Changed;

    [SerializeField] private List<Upgrade> upgrades = new();
    [SerializeField] private GameObject revealVfx;       // confetti burst where a new thing appears
    [SerializeField] private AudioSource revealSound;
    [SerializeField] private float revealSeconds = 0.5f;

    public IReadOnlyList<Upgrade> Upgrades => upgrades;

    private readonly List<GameObject> pendingReveal = new();
    private readonly Dictionary<GameObject, Vector3> restScale = new();

    public enum State { Locked, TooExpensive, Buyable, Owned, Worn }

    private void Awake()
    {
        Instance = this;
        foreach (var upgrade in upgrades) Apply(upgrade, false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #region Perks (bought upgrades only)
    public static int ExtraCustomers => Mathf.RoundToInt(Sum(u => u.extraCustomers));
    public static float PatienceScale => 1f + Sum(u => u.patienceBonus);
    public static int TipJar => Mathf.RoundToInt(Sum(u => u.tipJar));
    public static float ChefSpeedScale => 1f + Sum(u => u.chefSpeed);

    private static float Sum(Func<Upgrade, float> perk)
    {
        if (Instance == null) return 0f;
        float total = 0f;
        foreach (var upgrade in Instance.upgrades)
            if (CafeProgress.Owns(upgrade.id)) total += perk(upgrade);
        return total;
    }
    #endregion

    public State StateOf(Upgrade upgrade)
    {
        if (CafeProgress.Owns(upgrade.id))
            return upgrade.isHat && CafeProgress.Hat == upgrade.id ? State.Worn : State.Owned;
        if (CafeProgress.Level < upgrade.level) return State.Locked;
        return CafeProgress.Coins >= upgrade.price ? State.Buyable : State.TooExpensive;
    }

    // Unlocked, not owned and affordable right now (the shop button shows a dot while there is one).
    public bool AnythingToBuy()
    {
        foreach (var upgrade in upgrades)
            if (StateOf(upgrade) == State.Buyable) return true;
        return false;
    }

    public bool Buy(Upgrade upgrade)
    {
        if (StateOf(upgrade) != State.Buyable || !CafeProgress.TrySpend(upgrade.price)) return false;

        CafeProgress.AddOwned(upgrade.id);
        if (upgrade.isHat) WearOnly(upgrade.id);
        else Apply(upgrade, true);

        Changed?.Invoke(upgrade);
        EventManager.OnScoreUpdate.Invoke();
        return true;
    }

    // Owned hat tapped in the shop: put it on (taking the other one off), or take it off.
    public void ToggleHat(Upgrade upgrade)
    {
        if (!upgrade.isHat || !CafeProgress.Owns(upgrade.id)) return;
        WearOnly(CafeProgress.Hat == upgrade.id ? "" : upgrade.id);
        Changed?.Invoke(upgrade);
    }

    private void WearOnly(string hatId)
    {
        CafeProgress.Hat = hatId;
        foreach (var upgrade in upgrades)
            if (upgrade.isHat) Apply(upgrade, true);
    }

    private static bool IsOn(Upgrade upgrade) =>
        CafeProgress.Owns(upgrade.id) && (!upgrade.isHat || CafeProgress.Hat == upgrade.id);

    private void Apply(Upgrade upgrade, bool reveal)
    {
        bool owned = CafeProgress.Owns(upgrade.id);
        bool on = IsOn(upgrade);
        foreach (var go in upgrade.hide) if (go != null) go.SetActive(!owned);
        foreach (var go in upgrade.show)
        {
            if (go == null) continue;
            if (reveal && on && !go.activeSelf) QueueReveal(go);
            go.SetActive(on);
        }
    }

    #region Reveal
    // Bought while the shop covers the screen: the new thing shows up with a pop when the shop closes.
    private void QueueReveal(GameObject go)
    {
        if (!restScale.ContainsKey(go)) restScale[go] = go.transform.localScale;
        go.transform.localScale = Vector3.zero;
        if (!pendingReveal.Contains(go)) pendingReveal.Add(go);
    }

    public void RevealPending()
    {
        if (pendingReveal.Count == 0) return;
        var batch = new List<GameObject>(pendingReveal);
        pendingReveal.Clear();
        StartCoroutine(Reveal(batch));
    }

    // Unscaled: the result screen keeps the game paused behind it.
    private IEnumerator Reveal(List<GameObject> batch)
    {
        if (revealSound != null) revealSound.Play();
        foreach (var go in batch)
        {
            if (go == null || !go.activeInHierarchy) continue;
            if (revealVfx != null) Burst(Center(go));
        }

        float scale = 0f, velocity = 0f, t = 0f;
        while (t < revealSeconds * 2f)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            t += dt;
            UITokens.Spring(ref scale, ref velocity, 1f, dt);
            foreach (var go in batch)
                if (go != null) go.transform.localScale = restScale[go] * Mathf.Max(0f, scale);
            yield return null;
        }
        foreach (var go in batch)
            if (go != null) go.transform.localScale = restScale[go];
    }

    private static Vector3 Center(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return go.transform.position;
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return bounds.center;
    }

    private void Burst(Vector3 at)
    {
        var fx = Instantiate(revealVfx, at, Quaternion.identity);
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.useUnscaledTime = true;
            ps.Play(true);
        }
        Destroy(fx, 3f);
    }
    #endregion
}

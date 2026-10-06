using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The shop panel: one tile per CafeShop upgrade, in the order they open up (cafe level, then price).
// Opened from the HUD during a shift (pauses the kitchen) and from the result card between shifts
// (stays paused); on close, what was bought pops into the kitchen (CafeShop.RevealPending).
public class UIShop : MonoBehaviour
{
    [SerializeField] private UIShopTile tileTemplate;
    [SerializeField] private RectTransform grid;

    private readonly List<UIShopTile> tiles = new();
    private float resumeTimeScale = 1f;

    private void Awake()
    {
        tileTemplate.gameObject.SetActive(false);
        foreach (var upgrade in CafeShop.Instance.Upgrades.OrderBy(u => u.level).ThenBy(u => u.price))
        {
            var tile = Instantiate(tileTemplate, grid);
            tile.name = "Tile " + upgrade.id;
            tile.gameObject.SetActive(true);
            tile.Bind(upgrade);
            tiles.Add(tile);
        }
    }

    private void OnEnable()
    {
        CafeProgress.Changed += Refresh;
        CafeShop.Changed += OnShopChanged;
        Refresh();
    }

    private void OnDisable()
    {
        CafeProgress.Changed -= Refresh;
        CafeShop.Changed -= OnShopChanged;
    }

    private void OnShopChanged(Upgrade upgrade) => Refresh();

    private void Refresh()
    {
        foreach (var tile in tiles) tile.Refresh();
    }

    public void Open()
    {
        if (gameObject.activeSelf) return;
        EventManager.OnClick.Invoke();
        resumeTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (!gameObject.activeSelf) return;
        EventManager.OnClick.Invoke();
        gameObject.SetActive(false);
        Time.timeScale = resumeTimeScale;
        CafeShop.Instance.RevealPending();
    }
}

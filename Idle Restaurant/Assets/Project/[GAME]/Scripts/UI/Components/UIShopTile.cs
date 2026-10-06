using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One upgrade in the shop: picture, name, what it does, and one button whose label says what a tap does
// ("$60" to buy, "Wear" / "Take off" for a hat) or why it can't ("Lv 4", "Owned").
public class UIShopTile : MonoBehaviour
{
    [SerializeField] private Image surface;
    [SerializeField] private Image icon;
    [SerializeField] private Image lockBadge;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text blurb;
    [SerializeField] private TMP_Text category;
    [SerializeField] private Button button;
    [SerializeField] private Image buttonFace, buttonEdge;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private Image buttonIcon;
    [SerializeField] private Sprite checkIcon, lockIcon;

    private Upgrade upgrade;
    private float punch = 1f, punchVelocity;

    public void Bind(Upgrade shopUpgrade)
    {
        upgrade = shopUpgrade;
        title.text = upgrade.title;
        icon.sprite = upgrade.icon;
        icon.enabled = upgrade.icon != null;
        category.text = upgrade.kind switch
        {
            UpgradeKind.Kitchen => "KITCHEN",
            UpgradeKind.Dining => "DINING",
            UpgradeKind.Staff => "STAFF",
            _ => upgrade.isHat ? "HAT" : "CHEF",
        };
        button.onClick.AddListener(OnTap);
        Refresh();
    }

    public void Refresh()
    {
        if (upgrade == null || CafeShop.Instance == null) return;
        var state = CafeShop.Instance.StateOf(upgrade);
        bool locked = state == CafeShop.State.Locked;

        blurb.text = upgrade.blurb;   // shown while locked too: something to look forward to ("Lv 4" on the button)
        icon.color = locked ? UITokens.WithAlpha(UITokens.Colors.Ink, 0.18f) : Color.white;
        lockBadge.gameObject.SetActive(locked);
        surface.color = state == CafeShop.State.Owned || state == CafeShop.State.Worn
            ? UITokens.Colors.CreamSoft : UITokens.Colors.Cream;

        switch (state)
        {
            case CafeShop.State.Locked:
                Style(false, $"Lv {upgrade.level}", lockIcon, false);
                break;
            case CafeShop.State.TooExpensive:
                Style(true, "$" + upgrade.price, null, false);
                break;
            case CafeShop.State.Buyable:
                Style(true, "$" + upgrade.price, null, true);
                break;
            case CafeShop.State.Owned:
                if (upgrade.isHat) Style(true, "Wear", null, true);
                else Style(false, "Owned", checkIcon, false);
                break;
            case CafeShop.State.Worn:
                Style(false, "Take off", null, true);
                break;
        }
    }

    // primary: teal button, else the subtle pink one; enabled: can be tapped.
    private void Style(bool primary, string label, Sprite glyph, bool enabled)
    {
        buttonFace.color = primary ? UITokens.Colors.Teal : UITokens.Colors.SubtleFace;
        buttonEdge.color = primary ? UITokens.Colors.DarkTeal : UITokens.Colors.SubtleEdge;
        buttonLabel.color = primary ? UITokens.Colors.WarmWhite : UITokens.Colors.Ink;
        buttonLabel.text = label;
        buttonIcon.gameObject.SetActive(glyph != null);
        if (glyph != null)
        {
            buttonIcon.sprite = glyph;
            buttonIcon.color = primary ? UITokens.Colors.WarmWhite : UITokens.Colors.InkSoft;
        }
        button.interactable = enabled;
    }

    private void OnTap()
    {
        var shop = CafeShop.Instance;
        var state = shop.StateOf(upgrade);
        if (state == CafeShop.State.Buyable)
        {
            if (!shop.Buy(upgrade)) return;
        }
        else if (upgrade.isHat && (state == CafeShop.State.Owned || state == CafeShop.State.Worn))
            shop.ToggleHat(upgrade);
        else return;

        EventManager.OnClick.Invoke();
        punch = UITokens.Motion.PunchScale;
        punchVelocity = 0f;
    }

    private void LateUpdate()
    {
        if (punch == 1f && punchVelocity == 0f) return;
        UITokens.Spring(ref punch, ref punchVelocity, 1f, Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
        if (Mathf.Abs(punch - 1f) < 0.001f && Mathf.Abs(punchVelocity) < 0.01f) { punch = 1f; punchVelocity = 0f; }
        icon.rectTransform.localScale = new Vector3(punch, punch, 1f);
    }
}

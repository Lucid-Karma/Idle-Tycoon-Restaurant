using UnityEngine;

// A small yellow dot on a shop button while something in the shop can be bought right now.
public class UIShopBadge : MonoBehaviour
{
    [SerializeField] private GameObject dot;

    private float pop = 1f, velocity;

    private void OnEnable()
    {
        CafeProgress.Changed += Refresh;
        EventManager.OnScoreUpdate.AddListener(Refresh);
        Refresh();
    }

    private void OnDisable()
    {
        CafeProgress.Changed -= Refresh;
        EventManager.OnScoreUpdate.RemoveListener(Refresh);
    }

    private void Refresh()
    {
        bool show = CafeShop.Instance != null && CafeShop.Instance.AnythingToBuy();
        if (show && !dot.activeSelf) pop = 0.3f;
        dot.SetActive(show);
    }

    private void LateUpdate()
    {
        if (!dot.activeSelf || (pop == 1f && velocity == 0f)) return;
        UITokens.Spring(ref pop, ref velocity, 1f, Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
        if (Mathf.Abs(pop - 1f) < 0.001f && Mathf.Abs(velocity) < 0.01f) { pop = 1f; velocity = 0f; }
        dot.transform.localScale = new Vector3(pop, pop, 1f);
    }
}

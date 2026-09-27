using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shift progress as one pip per customer. Pip count and the "/ N" label come from
// ScoreManager so the HUD can never disagree with the level rules.
public class UICustomerPips : MonoBehaviour
{
    [SerializeField] private Image pipTemplate;
    [SerializeField] private TMP_Text totalLabel;

    private Image[] pips;
    private float[] pop;
    private int shown;

    private void Start()
    {
        int count = ScoreManager.Instance.CustomersPerLevel;
        if (totalLabel != null) totalLabel.text = "/ " + count;

        // Keep the template as pip #0 and remove any editor-preview copies.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != pipTemplate.transform) Destroy(child.gameObject);
        }
        pips = new Image[count];
        pop = new float[count];
        pips[0] = pipTemplate;
        for (int i = 1; i < count; i++)
            pips[i] = Instantiate(pipTemplate, transform);
        Refresh(false);
    }

    private void OnEnable() => EventManager.OnCustomerWent.AddListener(OnCustomerWent);
    private void OnDisable() => EventManager.OnCustomerWent.RemoveListener(OnCustomerWent);

    private void OnCustomerWent() => Refresh(true);

    private void Refresh(bool animate)
    {
        if (pips == null) return;
        int served = ScoreManager.Instance.HostedCustomerCount;
        for (int i = 0; i < pips.Length; i++)
        {
            bool filled = i < served;
            if (animate && filled && i >= shown) pop[i] = 1f;
            pips[i].color = filled ? UITokens.Colors.Teal : UITokens.Colors.LightPink;
        }
        shown = served;
    }

    private void LateUpdate()
    {
        if (pips == null) return;
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < pips.Length; i++)
        {
            if (pop[i] <= 0f) continue;
            pop[i] = Mathf.Max(0f, pop[i] - dt / UITokens.Motion.Slow);
            float s = 1f + 0.5f * Mathf.Sin(pop[i] * Mathf.PI);
            pips[i].rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }
}

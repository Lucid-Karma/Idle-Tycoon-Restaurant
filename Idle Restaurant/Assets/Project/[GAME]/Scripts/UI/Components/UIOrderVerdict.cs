using TMPro;
using UnityEngine;

// Name of the last served burger ("Chef's Classic", "Charcoal Special", "Chaos Burger"...), shown next to
// the star rating on the chef card. Ink for a good rating, tomato for a poor one.
[RequireComponent(typeof(TMP_Text))]
public class UIOrderVerdict : MonoBehaviour
{
    [SerializeField] private string waitingText = "No orders yet";

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        Set(waitingText, UITokens.Colors.InkMuted);
    }

    private void OnEnable() => EventManager.OnOrderRated.AddListener(OnOrderRated);

    private void OnDisable() => EventManager.OnOrderRated.RemoveListener(OnOrderRated);

    private void OnOrderRated()
    {
        var order = ScoreManager.Instance.LastOrder;
        Set(order.Review.Title, order.Rating < 2.5f ? UITokens.Colors.Tomato : UITokens.Colors.Ink);
    }

    private void Set(string text, Color color)
    {
        label.text = text;
        label.color = color;
    }
}

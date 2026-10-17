using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The Help screen. Any tap that is not on one of its buttons closes it (back to the kitchen), and "Show me again"
// closes it too as it starts the lesson - it used to start the lesson behind the still-open, paused Help screen.
public class HelpPanel : Panel, IPointerClickHandler
{
    private void Awake()
    {
        var again = transform.Find("HelpCard/TeachMeBTN");
        if (again != null && again.TryGetComponent<Button>(out var button)) button.onClick.AddListener(Close);
    }

    // Buttons take their own clicks; everything else (the dimmed backdrop, the card's empty space, its text) ends up here.
    public void OnPointerClick(PointerEventData eventData) => Close();

    private void Close()
    {
        EventManager.OnClick.Invoke();
        EventManager.OnLevelContine.Invoke();
    }
}

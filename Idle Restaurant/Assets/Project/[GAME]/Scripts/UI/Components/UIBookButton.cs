using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The HUD's notebook button (from the report, 2026-10-28): this week's three tasks (WeeklyTasks) and the burger book
// (BurgerBook), as two cards over the kitchen (UIBreakCard; the game goes on under them). A copy of the help button
// with a notebook drawn on it, on the right under the whole-cafe button. Hidden during the lesson and its titles.
public class UIBookButton : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Attach();
    }

    private static void OnLoaded(Scene scene, LoadSceneMode mode) => Attach();

    private static void Attach()
    {
        var help = GameObject.Find("<<<UI>>>/StaticCanvas/InGamePanel/HUD/HelpBTN");
        if (help == null || help.transform.parent.Find("BookBTN") != null) return;
        var copy = Instantiate(help, help.transform.parent);
        copy.name = "BookBTN";
        foreach (var helper in copy.GetComponents<HelpButton>()) Destroy(helper);
        foreach (var text in copy.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.gameObject.SetActive(false);
        ((RectTransform)copy.transform).anchoredPosition = new Vector2(-32f, -328f);
        var face = copy.transform.Find("Face");
        var icon = new GameObject("Notebook", typeof(RectTransform)).AddComponent<Image>();
        icon.transform.SetParent(face != null ? face : copy.transform, false);
        var r = icon.rectTransform;
        r.anchorMin = new Vector2(0.2f, 0.2f);
        r.anchorMax = new Vector2(0.8f, 0.8f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        icon.sprite = UIShapes.Notebook();
        icon.color = UITokens.Colors.Ink;
        icon.raycastTarget = false;
        var button = copy.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        var book = copy.AddComponent<UIBookButton>();
        button.onClick.AddListener(book.Open);
    }

    private void Update()
    {
        bool hidden = Tutorial.Running || LessonIntro.Playing;
        foreach (var image in GetComponentsInChildren<Image>(true)) image.enabled = !hidden;
        GetComponent<Button>().interactable = !hidden;
    }

    private void Open()
    {
        var ui = UIBreakCard.Instance;
        if (ui == null || ui.Showing) return;
        EventManager.OnClick.Invoke();
        ui.LastButton = "Close";
        ui.Play(new List<UIBreakCard.Card> { WeekCard(ui), BookCard() }, null, tapOutsideCloses: true);
    }

    // "THIS WEEK · NEW TASKS IN 3 DAYS / Weekly tasks" and a row per task.
    private static UIBreakCard.Card WeekCard(UIBreakCard ui)
    {
        int days = WeeklyTasks.DaysLeft;
        var card = new UIBreakCard.Card
        {
            Kicker = $"THIS WEEK  ·  NEW TASKS IN {days} DAY{(days == 1 ? "" : "S")}",
            Title = "Weekly tasks",
        };
        foreach (var task in WeeklyTasks.Tasks)
            card.Rows.Add(new UIBreakCard.Row
            {
                Icon = task.Complete ? ui.checkIcon : ui.starIcon,
                Tint = task.Complete ? UITokens.Colors.Teal : UITokens.Colors.DeepYellow,
                Label = task.Text,
                Value = task.Complete ? "Done!" : $"{task.Done}/{task.Target}   <alpha=#8C>+${task.Reward}",
            });
        return card;
    }

    // "BURGER BOOK / 7 of 22" over the names (UIBreakCard draws them).
    private static UIBreakCard.Card BookCard() => new()
    {
        Kicker = "BURGER BOOK",
        Title = $"{BurgerBook.Count} of {BurgerBook.All.Length}",
        Footer = $"Every new name pays +${BurgerBook.NewEntryTip} - mistakes count too!",
        Book = true,
    };
}

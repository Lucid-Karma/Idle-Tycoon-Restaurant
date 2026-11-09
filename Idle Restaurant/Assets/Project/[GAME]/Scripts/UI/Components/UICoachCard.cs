using System;
using UnityEngine;

// What is left of the lesson's coach card: nothing on screen. It was Mochi's card along the bottom (his face, one line,
// a "tap to go on"); the words went first (user, 2026-11-07: "I don't want text - the chef can stay"), then the little
// chef left sitting in its disc ("he just sits there, tiny, in the middle of the floor - change it"): the lesson's hand
// and light teach, and the chef only jumps out to react (UIChefLaugh). It still comes and goes with the lesson's steps.
// (Every lesson step ends by itself - Tutorial.Beat.Done - so nothing waits for a tap on it.)
public class UICoachCard : MonoBehaviour
{
    private void Awake() => gameObject.SetActive(false);

    public void Show(string text, Action tap) => gameObject.SetActive(true);

    public void OfferSkip(Action skip) { }

    public void SetLine(string text) { }

    public void Hide() => gameObject.SetActive(false);
}

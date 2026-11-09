using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChefAnimationController : MonoBehaviour
{
    private Animator animator;
    public Animator Animator { get { return (animator == null) ? animator = GetComponent<Animator>() : animator; } }

    private float waitForIdleTime;
    // Named methods, not lambdas: EventManager events are static, and a lambda can't be removed, so after a
    // Replay the destroyed chef card kept reacting (and threw, aborting the rest of the event).
    private void OnEnable()
    {
        EventManager.OnCustomerWent.AddListener(OnCustomerWent);
        EventManager.OnScoreGood.AddListener(OnScoreGood);
        EventManager.OnScoreNotBad.AddListener(OnScoreNotBad);
        EventManager.OnScoreBad.AddListener(OnScoreBad);
        // (A customer leaving hungry is not a burger's rating: the corner chef smirks at it out of his card -
        // UIChefLaugh - and the card's faces are only for ratings; user, 2026-11-04.)
    }

    private void OnDisable()
    {
        EventManager.OnCustomerWent.RemoveListener(OnCustomerWent);
        EventManager.OnScoreGood.RemoveListener(OnScoreGood);
        EventManager.OnScoreNotBad.RemoveListener(OnScoreNotBad);
        EventManager.OnScoreBad.RemoveListener(OnScoreBad);
    }

    private void OnCustomerWent() => StartCoroutine(IdleAgain());
    private void OnScoreGood() => InvokeTrigger("Happy");
    private void OnScoreNotBad() => InvokeTrigger("HappyIdle");
    private void OnScoreBad() => InvokeTrigger("Angry");

    private void InvokeTrigger(string value)
    {
        Animator.SetTrigger(value);
    }

    #region EventBasedMethods
    public void DoHappyIdle()
    {
        InvokeTrigger("HappyIdle");
    }

    public void DoSleepyIdle()
    {
        InvokeTrigger("SleepyIdle");
    }
    #endregion

    IEnumerator IdleAgain()
    {
        waitForIdleTime = Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        yield return new WaitForSeconds(waitForIdleTime);
        InvokeTrigger("Idle");
    }
}

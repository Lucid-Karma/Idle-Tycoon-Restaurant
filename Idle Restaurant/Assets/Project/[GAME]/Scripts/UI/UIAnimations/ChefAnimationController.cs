using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChefAnimationController : MonoBehaviour
{
    private Animator animator;
    public Animator Animator { get { return (animator == null) ? animator = GetComponent<Animator>() : animator; } }

    private float waitForIdleTime;

    private void OnEnable()
    {
        EventManager.OnCustomerWent.AddListener(() => StartCoroutine(IdleAgain()));
        EventManager.OnScoreGood.AddListener(() => InvokeTrigger("Happy"));
        EventManager.OnScoreNotBad.AddListener(() => InvokeTrigger("HappyIdle"));
        EventManager.OnScoreBad.AddListener(() => InvokeTrigger("Angry"));
        EventManager.OnCustomerProtest.AddListener(() => InvokeTrigger("Sad"));
    }

    private void OnDisable()
    {
        EventManager.OnCustomerWent.RemoveListener(() => StartCoroutine(IdleAgain()));
        EventManager.OnScoreGood.RemoveListener(() => InvokeTrigger("Happy"));
        EventManager.OnScoreNotBad.RemoveListener(() => InvokeTrigger("HappyIdle"));
        EventManager.OnScoreBad.RemoveListener(() => InvokeTrigger("Angry"));
        EventManager.OnCustomerProtest.RemoveListener(() => InvokeTrigger("Sad"));
    }

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

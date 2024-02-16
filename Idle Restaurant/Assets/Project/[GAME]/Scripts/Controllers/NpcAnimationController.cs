using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcAnimationController : MonoBehaviour
{
    private Animator animator;
    public Animator Animator { get { return (animator == null) ? animator = GetComponent<Animator>() : animator; } }

    NpcFsm npcFsm;
    NpcFsm NpcFsm { get { return (npcFsm == null) ? npcFsm = GetComponentInParent<NpcFsm>() : npcFsm; } }

    private void OnEnable()
    {
        NpcFsm.OnNpcWalk.AddListener(() => InvokeTrigger("Walk"));
        NpcFsm.OnNpcSitChairDown.AddListener(() => InvokeTrigger("SitChairDown"));
        NpcFsm.OnNpcSitChairIdle.AddListener(() => InvokeTrigger("SitChairIdle"));
        NpcFsm.OnNpcSitChairStandUp.AddListener(() => InvokeTrigger("SitChairStandUp"));
    }

    private void OnDisable()
    {
        NpcFsm.OnNpcWalk.RemoveListener(() => InvokeTrigger("Walk"));
        NpcFsm.OnNpcSitChairDown.RemoveListener(() => InvokeTrigger("SitChairDown"));
        NpcFsm.OnNpcSitChairIdle.RemoveListener(() => InvokeTrigger("SitChairIdle"));
        NpcFsm.OnNpcSitChairStandUp.RemoveListener(() => InvokeTrigger("SitChairStandUp"));
    }

    private void InvokeTrigger(string value)
    {
        Animator.SetTrigger(value);
    }

    #region EventBasedMethods
    
    #endregion
}
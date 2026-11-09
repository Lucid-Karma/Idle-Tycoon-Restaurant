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

    // The walk cycle (Walking_A, from Skeleton_Minion like their other clips) carries a customer 0.75 m/s (tuned in play: 0.71 left the planted foot sliding back, 0.8 forward) (measured: Editor/StrideMeter); it plays as fast as they
    // really move, so their feet do not slide (user, 2026-11-07: "ice skating"). Sitting, standing up: normal speed.
    private const float WalkStride = 0.75f;

    private void Update()
    {
        var npc = NpcFsm;
        var state = npc.executingNpcState;
        bool walking = (state == ExecutingNpcState.COME || state == ExecutingNpcState.GO) && npc.Agent != null && npc.Agent.enabled;
        Animator.speed = walking ? Mathf.Clamp(npc.Agent.velocity.magnitude / WalkStride, 0.35f, 3.5f) : 1f;
    }

    private void InvokeTrigger(string value)
    {
        Animator.SetTrigger(value);
    }

    #region EventBasedMethods
    
    #endregion
}
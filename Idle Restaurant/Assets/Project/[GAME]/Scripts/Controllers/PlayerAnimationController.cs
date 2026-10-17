using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    public Animator Animator { get { return (animator == null) ? animator = GetComponent<Animator>() : animator; } }

    PlayerFSM playerFsm;
    PlayerFSM PlayerFSM { get { return (playerFsm == null) ? playerFsm = GetComponentInParent<PlayerFSM>() : playerFsm; } }

    private void OnEnable()
    {
        PlayerFSM.OnPlayerIdle.AddListener(() => InvokeTrigger("Idle"));
        PlayerFSM.OnPlayerRun.AddListener(() => InvokeTrigger("Run"));
        PlayerFSM.OnPlayerInteract.AddListener(() => InvokeTrigger("Interact"));
        PlayerFSM.OnPlayerSlip.AddListener(OnSlip);
    }

    private void OnSlip() => InvokeTrigger("Slip");

    private void OnDisable()
    {
        PlayerFSM.OnPlayerIdle.RemoveListener(() => InvokeTrigger("Idle"));
        PlayerFSM.OnPlayerRun.RemoveListener(() => InvokeTrigger("Run"));
        PlayerFSM.OnPlayerInteract.RemoveListener(() => InvokeTrigger("Interact"));
        PlayerFSM.OnPlayerSlip.RemoveListener(OnSlip);
    }

    // The run cycle matches about 3.5 m/s. He runs faster now (PlayerFSM.runSpeed, more with the coffee), so the
    // cycle plays faster with him and his feet don't slide; everything else plays at normal speed.
    private const float RunCycleSpeed = 3.5f;

    private void Update()
    {
        var fsm = PlayerFSM;
        float pace = fsm.executingState == ExecutingState.RUN ? fsm.Agent.velocity.magnitude / RunCycleSpeed : 1f;
        Animator.speed = Mathf.Clamp(pace, 1f, 1.45f);
    }

    private void InvokeTrigger(string value)
    {
        Animator.SetTrigger(value);
    }

    #region EventBasedMethods
    public void StopPlayerInteraction()
    {
        PlayerFSM.executingState = ExecutingState.IDLE;
    }
    #endregion
}
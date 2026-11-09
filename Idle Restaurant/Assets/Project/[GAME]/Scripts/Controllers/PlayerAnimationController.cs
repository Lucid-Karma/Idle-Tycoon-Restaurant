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

    private void OnSlip() => InvokeTrigger(PlayerFSM.SlipsForward ? "SlipForward" : "Slip");

    private void OnDisable()
    {
        PlayerFSM.OnPlayerIdle.RemoveListener(() => InvokeTrigger("Idle"));
        PlayerFSM.OnPlayerRun.RemoveListener(() => InvokeTrigger("Run"));
        PlayerFSM.OnPlayerInteract.RemoveListener(() => InvokeTrigger("Interact"));
        PlayerFSM.OnPlayerSlip.RemoveListener(OnSlip);
    }

    // The run cycle (Running_B) carries him 2.2 m/s, measured over its feet's ground contacts (Editor/StrideMeter). It plays
    // exactly as fast as he moves - faster at a full run (4.6 m/s, more with the coffee), slower as he speeds up and
    // slows down - so a foot on the floor stays where it is put (user, 2026-11-07: "they slide as if ice skating";
    // it was held between 1x and 1.45x, and slid). Everything else plays at normal speed. Checked in play frame by frame
    // (StrideMeter.Probe, 2026-11-07): strides 1.95-2.19 swept, the planted foot's slide crosses zero at 2.19.
    private const float RunStride = 2.19f;

    private void Update()
    {
        var fsm = PlayerFSM;
        float pace = fsm.executingState == ExecutingState.RUN ? fsm.Agent.velocity.magnitude / RunStride : 1f;
        Animator.speed = fsm.executingState == ExecutingState.RUN ? Mathf.Clamp(pace, 0.35f, 3f) : 1f;
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
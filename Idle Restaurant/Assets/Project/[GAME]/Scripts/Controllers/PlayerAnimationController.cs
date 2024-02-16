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
    }

    private void OnDisable()
    {
        PlayerFSM.OnPlayerIdle.RemoveListener(() => InvokeTrigger("Idle"));
        PlayerFSM.OnPlayerRun.RemoveListener(() => InvokeTrigger("Run"));
        PlayerFSM.OnPlayerInteract.RemoveListener(() => InvokeTrigger("Interact"));
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
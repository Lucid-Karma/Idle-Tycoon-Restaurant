using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum ExecutingState
{
    IDLE,
    WALK
}
public class PlayerFSM : MonoBehaviour
{
    #region FSM
    public ExecutingState executingState;
    public PlayerStates currentState;

    public PlayerIdleState playerIdleState = new PlayerIdleState();
    public PlayerWalkState playerWalkState = new PlayerWalkState();
    #endregion

    #region Components
    private NavMeshAgent agent;
    public NavMeshAgent Agent{ get { return (agent == null) ? agent = GetComponent<UnityEngine.AI.NavMeshAgent>() : agent; } }
    #endregion

    ISpawnable spawnable;
    IPlaceable placeable;
    IEatable eatable;

    #region Parameters
    Camera _playerCam;
    Ray ray;
    RaycastHit hit;

    [SerializeField] private GameObject holdParent;
    #endregion

    void Start()
    {
        _playerCam = Camera.main;

        executingState = ExecutingState.IDLE;
        currentState = playerIdleState;
        currentState.EnterState(this);
    }

    void Update()
    {
        currentState.UpdateState(this);
        //MovePlayer();
        //DoneWithPath();
    }

    public void MovePlayer()
    {
        if(Input.GetMouseButtonDown(0))
        {
            ray = _playerCam.ScreenPointToRay(Input.mousePosition);

            if(Physics.Raycast(ray, out hit, 100))
            {
                spawnable = hit.collider.GetComponent<ISpawnable>();
                placeable = hit.collider.GetComponent<IPlaceable>();
                eatable = hit.collider.GetComponent<IEatable>();


                //executingPlayerState = ExecutingPlayerState.WALK;
                Agent.SetDestination(hit.point);
                //Debug.Log("player must go somewhere.");

                if(spawnable != null)
                {
                    if(!PickUpManager.Instance.isPickedUp)
                    {
                        PickUpManager.Instance.currentPickedUpObject = spawnable?.Spawn();
                        PickUpManager.Instance.PickUp(holdParent.transform);
                    }
                }
                else if(eatable != null)
                {
                    if(!PickUpManager.Instance.isPickedUp)
                    {
                        PickUpManager.Instance.isCurrentObjEatable = true;
                        PickUpManager.Instance.currentPickedUpObject = eatable.CurrentFood();
                        PickUpManager.Instance.PickUp(holdParent.transform);
                    }
                }
                else if(placeable != null)
                {
                    PickUpManager.Instance.Drop(placeable?.placeTransform, placeable?.refTransform);
                }
            }
        }
    }
    public void DoneWithPath()
    {
        if(Agent.remainingDistance <= Agent.stoppingDistance)
        {
            //executingPlayerState = ExecutingPlayerState.IDLE;
            
        }
    }

    public void SwitchState(PlayerStates nextState)
    {
        currentState = nextState;
        currentState.EnterState(this);
    }
}

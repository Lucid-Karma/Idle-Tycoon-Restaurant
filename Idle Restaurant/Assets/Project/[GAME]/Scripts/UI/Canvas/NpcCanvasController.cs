using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcCanvasController : MonoBehaviour
{
    NpcFsm npcFsm;
    NpcFsm NpcFsm { get { return (npcFsm == null) ? npcFsm = GetComponentInParent<NpcFsm>() : npcFsm; } }

    [SerializeField] private GameObject eatPB;
    [SerializeField] private GameObject orderPB;

    void Start()
    {
        eatPB.SetActive(false);
        orderPB.SetActive(false);
    }

    void OnEnable()
    {
        NpcFsm.OnNpcSitChairDown.AddListener(InitializeOrderPB);
        NpcFsm.OnNpcEatStart.AddListener(InitializeEatPB);
        NpcFsm.OnNpcEatEnd.AddListener(DesposeAll);
        NpcFsm.OnNpcWaitEnd.AddListener(DesposeAll);
    }
    void OnDisable()
    {
        NpcFsm.OnNpcSitChairDown.RemoveListener(InitializeOrderPB);
        NpcFsm.OnNpcEatStart.RemoveListener(InitializeEatPB);
        NpcFsm.OnNpcEatEnd.RemoveListener(DesposeAll);
        NpcFsm.OnNpcWaitEnd.RemoveListener(DesposeAll);
        DesposeAll();
    }

    private void InitializeEatPB()
    {
        orderPB.SetActive(false);
        eatPB.SetActive(true);
    }
    private void InitializeOrderPB()
    {
        eatPB.SetActive(false);
        orderPB.SetActive(true);
    }

    private void DesposeAll()
    {
        eatPB.SetActive(false);
        orderPB.SetActive(false);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcCanvasController : MonoBehaviour
{
    NpcFsm npcFsm;
    NpcFsm NpcFsm { get { return (npcFsm == null) ? npcFsm = GetComponentInParent<NpcFsm>() : npcFsm; } }

    [SerializeField] private GameObject progressBar;
    [SerializeField] private GameObject orderIMG;

    void Start()
    {
        progressBar.SetActive(false);
        orderIMG.SetActive(false);
    }

    void OnEnable()
    {
        NpcFsm.OnNpcSitChairDown.AddListener(InitializeOrderIMG);
        NpcFsm.OnNpcEatStart.AddListener(InitializeProgressBar);
        NpcFsm.OnNpcEatEnd.AddListener(DesposeAll);
    }
    void OnDisable()
    {
        NpcFsm.OnNpcSitChairDown.RemoveListener(InitializeOrderIMG);
        NpcFsm.OnNpcEatStart.RemoveListener(InitializeProgressBar);
        NpcFsm.OnNpcEatEnd.RemoveListener(DesposeAll);
        DesposeAll();
    }

    private void InitializeProgressBar()
    {
        orderIMG.SetActive(false);
        progressBar.SetActive(true);
    }
    private void InitializeOrderIMG()
    {
        progressBar.SetActive(false);
        orderIMG.SetActive(true);
    }

    private void DesposeAll()
    {
        progressBar.SetActive(false);
        orderIMG.SetActive(false);
    }
}

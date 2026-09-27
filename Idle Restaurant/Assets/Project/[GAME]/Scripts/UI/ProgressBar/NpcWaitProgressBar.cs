using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NpcWaitProgressBar : MonoBehaviour, IProgress01
{
    NpcFsm npcFsm;
    NpcFsm NpcFsm { get { return (npcFsm == null) ? npcFsm = GetComponentInParent<NpcFsm>() : npcFsm; } }

    public float maximum = 240f;
    private float current = 0;

    public float Progress01 => Mathf.Clamp01(current / maximum);
    private Image mask;
    private float fillAmount;

    void OnEnable()
    {
        mask = GetComponent<Image>();
        maximum = NpcFsm.Patience; // shown when they sit down; the customer owns the patience value
    }

    void Update()
    {
        if(current < maximum)
            GetCurrentFill();
        else
        {
            NpcFsm.executingNpcState = ExecutingNpcState.PROTEST;
            NpcFsm.OnNpcWaitEnd.Invoke();
            current = 0;
            mask.fillAmount = 0;
        }
    }

    void GetCurrentFill()
    {
        current += Time.deltaTime;
        fillAmount = (float)current / (float)maximum;
        mask.fillAmount = fillAmount;
    }

    private void OnDisable()
    {
        current = 0;
        mask.fillAmount = 0;
    }
}

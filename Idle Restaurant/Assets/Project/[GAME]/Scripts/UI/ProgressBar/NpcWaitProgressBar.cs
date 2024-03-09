using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NpcWaitProgressBar : MonoBehaviour
{
    NpcFsm npcFsm;
    NpcFsm NpcFsm { get { return (npcFsm == null) ? npcFsm = GetComponentInParent<NpcFsm>() : npcFsm; } }

    public float maximum = 240f;
    private float current = 0;
    private Image mask;
    private float fillAmount;

    void Start()
    {
        mask = GetComponent<Image>();
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
}

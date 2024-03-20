using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NpcEatProgressBar : MonoBehaviour
{
    NpcFsm npcFsm;
    NpcFsm NpcFsm { get { return (npcFsm == null) ? npcFsm = GetComponentInParent<NpcFsm>() : npcFsm; } }

    public float maximum = 3f;
    //[Range(0.0f, 5.0f)]
    private float current = 0;
    private Image mask;
    private float fillAmount;

    void OnEnable()
    {
        mask = GetComponent<Image>();
    }

    void Update()
    {
        if(current < maximum)
            GetCurrentFill();
        else
        {
            NpcFsm.executingNpcState = ExecutingNpcState.REACT;
            NpcFsm.OnNpcEatEnd.Invoke();
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

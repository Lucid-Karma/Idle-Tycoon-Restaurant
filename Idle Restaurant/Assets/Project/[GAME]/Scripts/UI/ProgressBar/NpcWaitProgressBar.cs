using UnityEngine;
using UnityEngine.UI;

// Shows how long the customer has been waiting. The customer owns the timer (NpcFsm.WaitedSeconds /
// Patience) — this bar used to run its own clock and end the wait itself, which drifted as soon as the
// wait could change (a thrown snack buys time).
public class NpcWaitProgressBar : MonoBehaviour, IProgress01
{
    NpcFsm npcFsm;
    NpcFsm NpcFsm { get { return (npcFsm == null) ? npcFsm = GetComponentInParent<NpcFsm>() : npcFsm; } }

    public float maximum = 240f;
    private float current = 0;

    public float Progress01 => Mathf.Clamp01(current / maximum);
    private Image mask;

    void OnEnable()
    {
        mask = GetComponent<Image>();
        maximum = NpcFsm.Patience;
    }

    void Update()
    {
        current = NpcFsm.WaitedSeconds;
        mask.fillAmount = Progress01;
    }

    private void OnDisable()
    {
        current = 0;
        mask.fillAmount = 0;
    }
}

using UnityEngine;

// Small talk while they wait (user, 2026-11-04: "life in the quiet moments"): every so often one waiting customer says
// something about the cafe, the smell, the chef - in their kind's own voice - in the same bubble their reactions use.
// Never in the first lesson, never over a reaction they have just had (they have waited a while), never two at once.
// Added by GameManager at runtime.
public class CafeChatter : MonoBehaviour
{
    private const float MinGap = 13f, MaxGap = 22f, QuietFor = 8f;
    private float next;

    private static readonly string[] Anyone =
    {
        // Nothing that sounds like an order ("Is that onion?" with nothing served yet - user, 2026-11-05).
        "Smells amazing!", "I'm SO hungry...", "Love this place!", "Ooh, sizzle sizzle!", "Best cafe in town!",
        "Cozy in here~", "My friends said it's great!",
    };

    private static string KindLine(CustomerKind kind) => kind switch
    {
        CustomerKind.Knight => "Take your time, chef!",
        CustomerKind.Barbarian => "Make it MESSY!",
        CustomerKind.Mage => "Hmm. Interesting...",
        CustomerKind.Rogue => "Psst... hurry!",
        CustomerKind.Ranger => "Got any snacks?",
        _ => null,
    };

    private void Start() => next = Time.time + Random.Range(MinGap, MaxGap);

    private void Update()
    {
        if (Time.time < next || Time.timeScale == 0f) return;
        next = Time.time + Random.Range(MinGap, MaxGap);
        if (Tutorial.Running || NpcFsm.Active.Count == 0) return;
        NpcFsm who = null;
        int seen = 0;
        foreach (var npc in NpcFsm.Active)
        {
            if (npc == null || npc.executingNpcState != ExecutingNpcState.WAIT || npc.WaitedSeconds < QuietFor) continue;
            if (Random.Range(0, ++seen) == 0) who = npc;   // one of them, each as likely
        }
        if (who == null) return;
        var own = KindLine(who.Kind);
        string line = own != null && Random.value < 0.4f ? own : Anyone[Random.Range(0, Anyone.Length)];
        // Only when he wears one (it was said before the player could buy any).
        if (!string.IsNullOrEmpty(CafeProgress.Hat) && Random.value < 0.15f) line = "Nice hat, chef!";
        who.OnNpcSays.Invoke(line, Mood.Amused);
    }
}

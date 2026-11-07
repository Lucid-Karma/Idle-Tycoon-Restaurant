using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

// Nothing stays stuck (user, 2026-11-04: "if something gets stuck for long, the game quietly fixes it - say, puts the
// chef on the nearest free spot"). Twice a second it looks at everyone who walks - the chef, Mochi, the customers -
// and whoever has somewhere to go but has not moved for a few seconds, or has lost the floor under them, is put
// right without a word: the chef onto the nearest floor and then, if he is still stuck, next to where he was going;
// Mochi home; a customer back on the floor, or - if they were leaving anyway - simply gone. The chaos bot
// (Assets/Editor/ChaosBot.cs) found customers left standing for good on their way out, holding the shift open.
// Added by GameManager at runtime.
public class Unstuck : MonoBehaviour
{
    private const float Every = 0.5f, StuckAfter = 3f;
    private float next;
    private readonly Dictionary<Object, (Vector3 at, float since, int tries)> watched = new();
    private readonly List<Object> gone = new();

    private void Update()
    {
        if (Time.time < next || Time.timeScale == 0f) return;
        next = Time.time + Every;

        var chef = FindFirstObjectByType<PlayerFSM>();
        if (chef != null && chef.Agent != null && chef.Agent.enabled)
        {
            var agent = chef.Agent;
            bool wantsToGo = agent.isOnNavMesh && agent.hasPath && !agent.pathPending && agent.remainingDistance > 0.4f;
            if (!agent.isOnNavMesh) Floor(agent, 6f);
            else if (Stuck(chef, chef.transform.position, wantsToGo && !chef.IsStunned, out int tries))
            {
                var goal = agent.destination;
                // First a nudge onto the floor beside him; then next to where he was going.
                if (tries == 1) Floor(agent, 1.5f, chef.transform.position + (goal - chef.transform.position).normalized * 0.6f);
                else Floor(agent, 3f, goal);
                agent.SetDestination(goal);
                watched[chef] = (chef.transform.position, Time.time, tries);   // (the warp is not progress: a second fix goes further)
            }
        }

        var mochi = FindFirstObjectByType<Waiter>();
        if (mochi != null && mochi.isActiveAndEnabled && Stuck(mochi, mochi.transform.position, mochi.OnTheMove, out _))
            mochi.Unstick();

        foreach (var npc in NpcFsm.Active)
        {
            if (npc == null) continue;
            var state = npc.executingNpcState;
            bool walking = state == ExecutingNpcState.COME || state == ExecutingNpcState.GO;
            var agent = npc.Agent;
            bool lost = agent != null && agent.enabled && !agent.isOnNavMesh;
            if (Stuck(npc, npc.transform.position, walking || lost, out _)) npc.Unstick();
        }

        LooseFood();

        // Forget whoever has left.
        gone.Clear();
        foreach (var key in watched.Keys) if (key == null) gone.Add(key);
        foreach (var key in gone) watched.Remove(key);
    }

    // Food that belongs to nothing - no hand, plate, station, table, burger or tray, lying (or hanging in the air)
    // still for a while - is tossed into the bin. Nothing should ever end up like that; when it does anyway (a burger
    // Mochi could not put down floated at tray height for good: user's phone, 2026-11-04) it does not stay.
    private const float LooseFor = 6f;
    private readonly Dictionary<EdibleBase, (Vector3 at, float since)> loose = new();
    private readonly List<EdibleBase> settled = new();

    private void LooseFood()
    {
        var chef = FindFirstObjectByType<PlayerFSM>();
        settled.Clear();
        foreach (var f in FindObjectsByType<EdibleBase>(FindObjectsSortMode.None))
        {
            bool orphan = f.transform.parent == null && f.Placeable == null && (chef == null || !chef.Carried.Contains(f));
            if (!orphan) continue;
            settled.Add(f);
            var at = f.transform.position;
            if (!loose.TryGetValue(f, out var was) || (was.at - at).sqrMagnitude > 0.0025f) { loose[f] = (at, Time.time); continue; }
            if (Time.time - was.since < LooseFor) continue;
            loose.Remove(f);
            var bin = FindFirstObjectByType<Bin>();
            if (bin != null) bin.Junk(f);
            else f.gameObject.SetActive(false);
        }
        gone.Clear();
        foreach (var key in loose.Keys) if (key == null || !settled.Contains(key)) gone.Add(key);
        foreach (var key in gone) loose.Remove((EdibleBase)key);
    }

    // True once `who` should have moved for StuckAfter seconds and has not (then the clock starts again, and
    // `tries` counts the fixes in a row).
    private bool Stuck(Object who, Vector3 at, bool shouldMove, out int tries)
    {
        tries = 0;
        if (!shouldMove) { watched.Remove(who); return false; }
        if (!watched.TryGetValue(who, out var w) || (w.at - at).sqrMagnitude > 0.0025f)
        {
            watched[who] = (at, Time.time, 0);
            return false;
        }
        if (Time.time - w.since < StuckAfter) return false;
        tries = w.tries + 1;
        watched[who] = (at, Time.time, tries);
        return true;
    }

    // Puts the agent on the nearest floor to `near` (its own position by default), within `radius` metres.
    public static bool Floor(NavMeshAgent agent, float radius, Vector3? near = null)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(near ?? agent.transform.position, out var hit, radius, filter)) return false;
        if (agent.Warp(hit.position)) return true;
        agent.enabled = false;
        agent.transform.position = hit.position;
        agent.enabled = true;
        return agent.isOnNavMesh;
    }
}

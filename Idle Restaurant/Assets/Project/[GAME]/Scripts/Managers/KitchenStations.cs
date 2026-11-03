using System.Collections;
using UnityEngine;

// The stations in the kitchen (Stations): each is a counter with its cooker and its crate, built by Tools/Chibi Cafe/16
// (CafeStationsBuilder) where one of the kitchen's own counters stood (the sink, the dish rack), and hidden until its
// level. One that opens while the game is running pops in when its level starts - a spring from nothing, confetti and
// the level-up jingle, like a bought upgrade (CafeShop) - so the player sees where it is before its first order.
public class KitchenStations : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public Stations.Kind kind;
        public GameObject station;
        public GameObject[] replaces;
    }

    [SerializeField] private Entry[] entries;
    [SerializeField] private float popSeconds = 0.5f;

    public static KitchenStations Instance { get; private set; }

    // A station came or went: whoever keeps a list of stations looks again. Static: subscribe with named methods.
    public static event System.Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Changed = null;

    // Before anything looks for stations (their Starts), like the bought upgrades (CafeShop.Awake).
    private void Awake()
    {
        Instance = this;
        Apply(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable() => EventManager.OnLevelStart.AddListener(OnLevelStart);
    private void OnDisable() => EventManager.OnLevelStart.RemoveListener(OnLevelStart);
    private void OnLevelStart() => Apply(true);

    // The station of a kind, open or not (the lesson's pointing hand and the camera look for it).
    public GameObject StationOf(Stations.Kind kind)
    {
        foreach (var entry in entries) if (entry.kind == kind) return entry.station;
        return null;
    }

    private void Apply(bool reveal)
    {
        bool changed = false;
        foreach (var entry in entries)
        {
            if (entry.station == null) continue;
            bool open = Stations.Open(entry.kind);
            if (entry.station.activeSelf == open) continue;
            changed = true;
            foreach (var go in entry.replaces) if (go != null) go.SetActive(!open);
            entry.station.SetActive(open);
            if (open && reveal) StartCoroutine(Pop(entry.station.transform));
            if (open) GoogleAnalytics.Event("station_open", "station", entry.kind.ToString(), "level", CafeProgress.LevelNumber);
        }
        if (changed) Changed?.Invoke();
    }

    private IEnumerator Pop(Transform station)
    {
        var rest = station.localScale;
        station.localScale = Vector3.zero;
        GameSfx.Play(GameSfx.Cue.LevelUp);
        if (CafeShop.Instance != null) CafeShop.Instance.Confetti(Center(station));
        float scale = 0f, velocity = 0f;
        for (float t = 0f; t < popSeconds * 2f; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            UITokens.Spring(ref scale, ref velocity, 1f, Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
            station.localScale = rest * Mathf.Max(0f, scale);
            yield return null;
        }
        station.localScale = rest;
    }

    private static Vector3 Center(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return t.position;
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return bounds.center;
    }
}

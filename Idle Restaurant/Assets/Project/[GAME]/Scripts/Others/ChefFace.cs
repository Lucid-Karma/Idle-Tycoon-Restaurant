using UnityEngine;

// The chef's mouth. KayKit heads come with eyes and brows but nothing below them, so the shapes are built
// into the head bone by the editor step (CafeLookBuilder) in the same colour as his eyes, and this swaps
// which one is showing: a small smile while he works, a grin when an order pays or the cafe levels up, an
// "o" when a tomato gets him, a frown when someone leaves hungry. Everything but the smile is held for a
// moment and then he goes back to work.
public class ChefFace : MonoBehaviour
{
    // The order the mouths are stored in.
    public enum Look { Smile, Grin, Shock, Frown }

    [SerializeField] private GameObject[] mouths = System.Array.Empty<GameObject>();
    [SerializeField] private float holdSeconds = 1.6f;

    private float until;
    private Look showing = Look.Smile;

    public void Show(Look look)
    {
        until = look == Look.Smile ? 0f : Time.unscaledTime + holdSeconds;
        if (look == showing) return;
        showing = look;
        for (int i = 0; i < mouths.Length; i++)
            if (mouths[i] != null) mouths[i].SetActive(i == (int)look);
    }

    private void Awake() => Show(Look.Smile);

    private void Update()
    {
        if (showing != Look.Smile && Time.unscaledTime >= until) Show(Look.Smile);
    }

    // Named methods: these events are static and outlive a Replay.
    private void OnEnable()
    {
        EventManager.OnOrderRated.AddListener(OnOrderRated);
        EventManager.OnCustomerProtest.AddListener(OnProtest);
        FoodFight.OnChefSplatted.AddListener(OnSplatted);
        CafeProgress.LeveledUp += OnLevelUp;
    }

    private void OnDisable()
    {
        EventManager.OnOrderRated.RemoveListener(OnOrderRated);
        EventManager.OnCustomerProtest.RemoveListener(OnProtest);
        FoodFight.OnChefSplatted.RemoveListener(OnSplatted);
        CafeProgress.LeveledUp -= OnLevelUp;
    }

    private void OnOrderRated() => Show(Look.Grin);
    private void OnProtest() => Show(Look.Frown);
    private void OnSplatted(Vector3 at, FoodFight.Hit hit) => Show(Look.Shock);
    private void OnLevelUp(int level) => Show(Look.Grin);
}

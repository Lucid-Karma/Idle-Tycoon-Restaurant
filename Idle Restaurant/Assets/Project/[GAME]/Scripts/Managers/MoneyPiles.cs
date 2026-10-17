using System.Collections.Generic;
using TMPro;
using UnityEngine;

// A paid order leaves its money on the table as a stack of banknotes, and the chef picks it up by walking past
// (the "arcade idle" pickup of games like Burger Life): the stack lifts off the table where it is and vanishes with
// a "+$7" rising over it and a jingle, and the wallet counts up. It used to fly across the room into the chef's
// hands - also by itself after a while, from wherever he was - which out of nowhere looked like a bug (user). Picking
// up never needs a tap and nothing can be lost: a pile nobody comes for cashes itself in, in place, after a while,
// and opening the shop sweeps every pile in first. The stacks stand still (they used to turn slowly).
// The money itself is saved the moment the order is rated (ScoreManager.RateOrder), so quitting with money
// on the tables loses nothing; only the wallet's number waits for the pickup (Pending, EarningTextController).
// It has to read as MONEY at a glance: banknotes, the arcade-idle money - green notes with a "$" in an oval
// (Tools/FX/money_art.py), stacked a little untidily with a yellow paper band round a bundle and one note slid
// off; a white glint flashing on it now and then; and the amount floating over it ("$5").
// Built into the scene by Tools/Chibi Cafe/12 Arcade Flow (CafeFlowBuilder).
public class MoneyPiles : MonoBehaviour
{
    public static MoneyPiles Instance { get; private set; }
    // Money on the tables that the wallet does not show yet.
    public static int Pending { get; private set; }

    [SerializeField] private Material billMaterial, edgeMaterial, bandMaterial;
    [SerializeField] private Sprite sparkleSprite;
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private Material labelMaterial;
    [SerializeField] private Vector2 billSize = new(0.72f, 0.36f);   // metres: big enough to read from the game camera
    [SerializeField] private float labelSize = 6f;
    [SerializeField] private float pickupRadius = 2.1f;   // flat distance from the chef
    [SerializeField] private float autoCollectAfter = 20f;

    // Thick notes, so a stack reads as a stack from the game camera; cashing in: the stack lifts and goes (LiftTime)
    // while the "+$7" rises and fades (CashTime).
    private const float Thickness = 0.06f, PopTime = 0.25f, LiftTime = 0.35f, CashTime = 0.9f;
    private const float SparkleTime = 0.45f, SparkleSize = 0.95f;

    private sealed class Pile
    {
        public Transform Root, Spinner, Sparkle, Label;
        public readonly List<Transform> Notes = new();
        public int Amount;
        public float Born, Top;
        public bool Cashing;
        public float CashStart;
        public Vector3 From, LabelFrom;
        public float NextSparkle, SparkleStart = -1f;
        public Vector3 SparkleAt;
    }

    private readonly List<Pile> piles = new();
    private readonly Stack<Transform> spareNotes = new();
    private Mesh billMesh;
    private Transform chef;
    private Camera cam;

    private void Awake()
    {
        Instance = this;
        Pending = 0;
        billMesh = BuildBillMesh();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; Pending = 0; }
    }

    private void Start()
    {
        var player = FindFirstObjectByType<PlayerFSM>();
        if (player != null) chef = player.transform;
        cam = Camera.main;
    }

    // An order's money, on the table it was served at.
    public static void Drop(Vector3 at, int amount)
    {
        if (Instance == null || amount <= 0) return;
        Instance.Spawn(at, amount);
    }

    private void Spawn(Vector3 at, int amount)
    {
        var pile = new Pile { Amount = amount, Born = Time.time, NextSparkle = Time.time + Random.Range(0.4f, 1f) };
        pile.Root = new GameObject("Money $" + amount).transform;
        pile.Root.SetParent(transform, false);
        pile.Root.position = at;
        pile.Spinner = new GameObject("Spin").transform;
        pile.Spinner.SetParent(pile.Root, false);

        // One note per $2 and two more, up to twelve: a bigger order is a taller stack. Stacked a little untidily,
        // a yellow band round it once it is a bundle (4+), and the last note slid off beside it.
        int count = Mathf.Clamp(amount / 2 + 2, 3, 12);
        int stacked = count >= 3 ? count - 1 : count;
        for (int i = 0; i < count; i++)
        {
            var note = Bill();
            note.SetParent(pile.Spinner, false);
            if (i < stacked)
            {
                note.localPosition = new Vector3(Random.Range(-0.02f, 0.02f), Thickness * (i + 0.5f), Random.Range(-0.015f, 0.015f));
                note.localRotation = Quaternion.Euler(0f, Random.Range(-7f, 7f), 0f);
            }
            else
            {
                note.localPosition = new Vector3(billSize.x * 0.38f, Thickness * 0.5f + 0.03f, billSize.y * 0.55f);
                note.localRotation = Quaternion.Euler(0f, Random.Range(25f, 40f), Random.Range(6f, 10f));
            }
            pile.Notes.Add(note);
        }
        pile.Top = Thickness * stacked;
        if (stacked >= 4)
        {
            var band = Box("Band", bandMaterial, null);
            band.SetParent(pile.Spinner, false);
            band.localPosition = Vector3.up * (pile.Top * 0.5f);
            band.localScale = new Vector3(billSize.x * 0.16f, pile.Top + 0.008f, billSize.y + 0.012f);
        }

        pile.Sparkle = Glint();
        pile.Sparkle.SetParent(pile.Root, false);
        pile.Sparkle.localScale = Vector3.zero;
        pile.Label = Label("$" + amount);
        pile.Label.SetParent(pile.Root, false);
        pile.Label.localPosition = Vector3.up * (pile.Top + 0.42f);

        pile.Root.localScale = Vector3.zero;
        piles.Add(pile);
        Pending += amount;
        EventManager.OnScoreUpdate.Invoke();   // the wallet holds back what is on the tables
    }

    #region Pieces
    private Transform Bill()
    {
        if (spareNotes.Count > 0)
        {
            var reused = spareNotes.Pop();
            reused.gameObject.SetActive(true);
            return reused;
        }
        var note = Box("Note", billMaterial, edgeMaterial);
        note.localScale = new Vector3(billSize.x, Thickness, billSize.y);
        return note;
    }

    // A unit box: its top and bottom (submesh 0, the note's face) and its four sides (submesh 1, the paper's edge).
    private Transform Box(string name, Material faces, Material sides)
    {
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = billMesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterials = new[] { faces, sides ?? faces };
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    // A sprite, so it draws with the default sprite material: a legacy particle material on a mesh drew
    // nothing in this pipeline (and neither does a URP particle material made from code).
    private Transform Glint()
    {
        var go = new GameObject("Glint");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sparkleSprite;
        sr.sortingOrder = 10;
        return go.transform;
    }

    // "$5" floating over the heap, white with an ink outline, always facing the camera.
    private Transform Label(string text)
    {
        var go = new GameObject("Amount");
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = labelFont;
        if (labelMaterial != null) tmp.fontSharedMaterial = labelMaterial;
        tmp.text = text;
        tmp.fontSize = labelSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.6f);
        return go.transform;
    }

    // A 1 x 1 x 1 box: top and bottom carry the note's picture (the bottom mirrored so it reads from below too),
    // the four sides are the plain paper edge.
    private static Mesh BuildBillMesh()
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var faces = new List<int>();
        var sides = new List<int>();

        void Quad(Vector3 c, Vector3 u, Vector3 v, Vector3 n, List<int> into, bool mirror)
        {
            int k = vertices.Count;
            vertices.Add(c - u - v); vertices.Add(c - u + v); vertices.Add(c + u + v); vertices.Add(c + u - v);
            for (int q = 0; q < 4; q++) normals.Add(n);
            float a = mirror ? 1f : 0f, b = mirror ? 0f : 1f;
            uvs.Add(new Vector2(a, 0f)); uvs.Add(new Vector2(a, 1f)); uvs.Add(new Vector2(b, 1f)); uvs.Add(new Vector2(b, 0f));
            into.Add(k); into.Add(k + 1); into.Add(k + 2); into.Add(k); into.Add(k + 2); into.Add(k + 3);
        }

        var h = 0.5f;
        // Seen from outside each face, u points right and v up: Unity's clockwise front faces.
        Quad(new Vector3(0, h, 0), new Vector3(h, 0, 0), new Vector3(0, 0, h), Vector3.up, faces, false);
        Quad(new Vector3(0, -h, 0), new Vector3(-h, 0, 0), new Vector3(0, 0, h), Vector3.down, faces, true);
        Quad(new Vector3(h, 0, 0), new Vector3(0, 0, h), new Vector3(0, h, 0), Vector3.right, sides, false);
        Quad(new Vector3(-h, 0, 0), new Vector3(0, 0, -h), new Vector3(0, h, 0), Vector3.left, sides, false);
        Quad(new Vector3(0, 0, h), new Vector3(-h, 0, 0), new Vector3(0, h, 0), Vector3.forward, sides, false);
        Quad(new Vector3(0, 0, -h), new Vector3(h, 0, 0), new Vector3(0, h, 0), Vector3.back, sides, false);

        var mesh = new Mesh { name = "Banknote" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(faces, 0);
        mesh.SetTriangles(sides, 1);
        mesh.RecalculateBounds();
        return mesh;
    }
    #endregion

    // Everything on the tables, to the wallet now (the shop is opening: what you see is what you can spend).
    public static void CollectAll()
    {
        if (Instance == null) return;
        foreach (var pile in Instance.piles) Instance.Bank(pile);
        Instance.piles.Clear();
        EventManager.OnScoreUpdate.Invoke();
    }

    private void Update()
    {
        var facing = cam != null ? cam.transform.rotation : Quaternion.identity;
        for (int i = piles.Count - 1; i >= 0; i--)
        {
            var pile = piles[i];
            pile.Label.rotation = facing;
            pile.Sparkle.rotation = facing;
            if (!pile.Cashing)
            {
                float age = Time.time - pile.Born;
                float t = Mathf.Clamp01(age / PopTime);
                // A little overshoot as it lands on the table; then it stands still, a glint flashing on it now and then.
                pile.Root.localScale = Vector3.one * (t < 1f ? Mathf.Sin(t * Mathf.PI * 0.75f) / Mathf.Sin(Mathf.PI * 0.75f) : 1f);
                Glint(pile);
                bool near = chef != null && Flat(chef.position - pile.Root.position) < pickupRadius;
                if (age > PopTime && (near || age > autoCollectAfter)) StartCashing(pile);
                continue;
            }

            // Cashing in, where it is: the stack lifts a little and shrinks away, the amount rises over it and fades.
            float c = Time.time - pile.CashStart;
            float lift = Mathf.Clamp01(c / LiftTime);
            pile.Root.position = pile.From + Vector3.up * (0.35f * (1f - (1f - lift) * (1f - lift)));
            pile.Root.localScale = Vector3.one * (1f - lift * lift);
            float rise = Mathf.Clamp01(c / CashTime);
            pile.Label.position = pile.LabelFrom + Vector3.up * (0.6f * (1f - (1f - rise) * (1f - rise)));
            var tmp = pile.Label.GetComponent<TextMeshPro>();
            tmp.alpha = 1f - Mathf.Clamp01((rise - 0.6f) / 0.4f);
            if (rise >= 1f)
            {
                Bank(pile);
                piles.RemoveAt(i);
            }
        }
    }

    private void StartCashing(Pile pile)
    {
        pile.Cashing = true;
        pile.CashStart = Time.time;
        pile.From = pile.Root.position;
        pile.Sparkle.localScale = Vector3.zero;
        // The amount goes on by itself (the stack under it shrinks away), now saying what came in.
        pile.LabelFrom = pile.Label.position;
        pile.Label.SetParent(transform, true);
        pile.Label.localScale = Vector3.one;
        pile.Label.GetComponent<TextMeshPro>().text = "+$" + pile.Amount;
        // The wallet counts it now, as it is seen going.
        Pending = Mathf.Max(0, Pending - pile.Amount);
        pile.Amount = 0;
        GameSfx.Play(GameSfx.Cue.Tip);
        EventManager.OnScoreUpdate.Invoke();
    }

    private void Glint(Pile pile)
    {
        if (pile.SparkleStart < 0f)
        {
            if (Time.time < pile.NextSparkle) return;
            pile.SparkleStart = Time.time;
            // Somewhere on the top note.
            pile.SparkleAt = new Vector3(Random.Range(-0.2f, 0.2f), pile.Top + 0.04f, Random.Range(-0.1f, 0.1f));
        }
        float s = (Time.time - pile.SparkleStart) / SparkleTime;
        if (s >= 1f)
        {
            pile.SparkleStart = -1f;
            pile.NextSparkle = Time.time + Random.Range(0.8f, 1.6f);
            pile.Sparkle.localScale = Vector3.zero;
            return;
        }
        // A little towards the camera, so the stack never cuts it in half.
        pile.Sparkle.position = pile.Root.TransformPoint(pile.SparkleAt) - (cam != null ? cam.transform.forward * 0.35f : Vector3.zero);
        pile.Sparkle.localScale = Vector3.one * (Mathf.Sin(s * Mathf.PI) * SparkleSize / SpriteWidth());
        pile.Sparkle.rotation *= Quaternion.Euler(0f, 0f, s * 90f);
    }

    private float SpriteWidth() => sparkleSprite != null ? Mathf.Max(0.01f, sparkleSprite.bounds.size.x) : 1f;

    private void Bank(Pile pile)
    {
        Pending = Mathf.Max(0, Pending - pile.Amount);
        foreach (var note in pile.Notes)
        {
            note.SetParent(transform, false);
            note.gameObject.SetActive(false);
            spareNotes.Push(note);
        }
        Destroy(pile.Root.gameObject);   // with its glint
        if (pile.Label != null) Destroy(pile.Label.gameObject);
    }

    private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
}

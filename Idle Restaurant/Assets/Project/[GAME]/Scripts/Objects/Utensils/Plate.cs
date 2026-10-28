using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Plate : PlaceableBase
{
    private List<EdibleBase> ingredients = new List<EdibleBase>();
    private int _ingredientsCount;
    private float distanceBetweenObjects;
    private Transform parentTransform;
    private Transform refTransform;
    private Vector3 colSize, colCenter;
    [SerializeField] private GameObject hamburger;
    private bool doesHaveHamburger;

    public override void EnableCollider()
    {
        if(!doesHaveHamburger)
            placeableCollider.enabled = true;
    }

    public override void Start()
    {
        doesHaveHamburger = false;

        parentTransform = gameObject.transform;
        refTransform = gameObject.transform.GetChild(0).transform;

        base.Start();
        colSize = placeableCollider.size;
        colCenter = placeableCollider.center;
    }

    private void ExtendCollider(GameObject stackedObj)
    {
        placeableCollider.size = new Vector3(placeableCollider.size.x, placeableCollider.size.y + stackedObj.transform.localScale.y / 2, placeableCollider.size.z);
        placeableCollider.center = new Vector3(placeableCollider.center.x, placeableCollider.center.y + stackedObj.transform.localScale.y / 4, placeableCollider.center.z);
    }
    private void CompressCollider()
    {
        _ingredientsCount = ingredients.Count;

        placeableCollider.size = new Vector3(placeableCollider.size.x, placeableCollider.size.y - (ingredients[_ingredientsCount-1].transform.localScale.y / 2), placeableCollider.size.z);
        placeableCollider.center = new Vector3(placeableCollider.center.x, placeableCollider.center.y - ingredients[_ingredientsCount-1].transform.localScale.y / 4, placeableCollider.center.z);
    }
    private void ResetColAndRef()
    {
        refTransform.position = gameObject.transform.position;

        placeableCollider.size = colSize;
        placeableCollider.center = colCenter;
        placeableCollider.enabled = true;
    }

    private void SetDistanceBetweenIngredients()
    {
        _ingredientsCount = ingredients.Count;

        if(_ingredientsCount >= 1)
        {
            distanceBetweenObjects = (ingredients[_ingredientsCount-1].collider.size.y);
            ingredients.Last().isLastPiece = false;
        } 
        else
            distanceBetweenObjects = 0;
    }
    private void SetIngredientPos(EdibleBase ingredient)
    {
        ingredient.gameObject.transform.parent = parentTransform;
        Vector3 desiredPos = refTransform.localPosition;
        desiredPos.y += distanceBetweenObjects;    
        
        ingredient.gameObject.transform.localRotation = Quaternion.identity;
        ingredient.gameObject.transform.localPosition = desiredPos; 
    }

    #region Orders
    // A plate is making one customer's order (Orders): the next order up (Orders.NextUp - what the order card was
    // showing) when the first layer goes on, kept to the end, and handed on to someone else only if they leave - for
    // the hints, the order number badges (UIOrderTags), the bubble showing what is still missing and Mochi. It does NOT end the burger: the player does, by
    // tapping the plate with empty hands (Close), or it ends by itself at six layers. (For a while it closed itself
    // as soon as it held as many layers as the order, and the user lost the fun of it: no six-layer monster for a
    // cheeseburger customer, no making a bigger order first.)
    public const int MaxLayers = 6;
    public NpcFsm ForCustomer { get; private set; }
    public Recipe Order => ForCustomer != null ? ForCustomer.Ordered : null;
    public int Target => MaxLayers;

    // Everything some waiting customer ordered is on it (extras or not): worth closing and serving.
    public bool CoversAnOrder =>
        !doesHaveHamburger && ingredients.Count > 0 &&
        (Order != null ? Orders.Missing(Order, LayerNames).Count == 0
                       : NpcFsm.Active.Any(n => Orders.Waiting(n) && Orders.Missing(n.Ordered, LayerNames).Count == 0));

    // The player says it is done: the top bun goes on and it is a burger.
    public bool Close()
    {
        if (doesHaveHamburger || ingredients.Count == 0) return false;
        ClaimOnClose();
        GenerateHamburger(true);
        return doesHaveHamburger;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Orders.Register(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Orders.Unregister(this);
    }

    private void Update()
    {
        if (doesHaveHamburger || ingredients.Count == 0) return;
        // Placeables switch their colliders off while the chef's hands are empty (taps then go to the food);
        // a plate with layers on it stays tappable, because empty hands on it mean "done!" (PlayerFSM.CloseBurger).
        if (placeableCollider != null && !placeableCollider.enabled) placeableCollider.enabled = true;
        // Their patience ran out, or they were served something else meanwhile: make it for someone else.
        if (ForCustomer != null && !Orders.Waiting(ForCustomer)) ForCustomer = null;
        if (ForCustomer == null) Claim();

    }

    // Whose order this plate is: picked once and kept (2026-10-27, user: "which recipe is being made, and for whom,
    // is confusing - especially when the order card suddenly changes"). It used to be picked again with every layer
    // - the best match for what was on it so far - so the card and the bubbles jumped from one customer to another
    // mid-burger. Now the first layer is for the order the card was showing (Orders.NextUp) and the plate keeps it;
    // a burger made for nobody yet (its customer left) goes to the best match for what is on it.
    private void Claim()
    {
        var layers = LayerNames.ToList();
        ForCustomer = (layers.Count <= 1 ? Orders.NextUp(this) : null) ?? Orders.BestFor(layers, this);
    }

    // Closing: it stays with its customer - unless it is exactly somebody else's order and not its own, then it is
    // theirs (a Classic made on a cheeseburger plate goes to the Classic customer, and its badge says so).
    private void ClaimOnClose()
    {
        var layers = LayerNames.ToList();
        if (ForCustomer != null && Orders.Fit(layers, ForCustomer.Ordered) >= 2000f) return;
        NpcFsm exact = null;
        foreach (var npc in NpcFsm.Active)
            if (npc != ForCustomer && Orders.Waiting(npc) && !Orders.Claimed(npc, this, burgersOnly: true)
                && Orders.Fit(layers, npc.Ordered) >= 2000f && (exact == null || npc.WaitedSeconds > exact.WaitedSeconds))
                exact = npc;
        if (exact != null) ForCustomer = exact;
        else if (ForCustomer == null) Claim();
    }
    #endregion

    public bool HasHamburger => doesHaveHamburger;
    public Hamburger FinishedBurger => doesHaveHamburger ? placedHamburger : null;
    public int LayerCount => ingredients.Count;
    // What is on the plate, by ingredient name ("bun", "burger", "tomato"...): the first-shift lesson walks
    // the player through the ingredients that are still missing.
    public IEnumerable<string> LayerNames => ingredients.Select(x => x.Name);
    // The same with how each was made (the order card ticks a layer only when it is cooked/sliced/baked).
    public IEnumerable<(string name, Prep prep)> LayerPreps => ingredients.Select(x => (x.Name, x.Preparation));

    // A finished burger in hand can be set down on an empty plate (to free the chef's hands). It used to be
    // stacked as a 7th "ingredient" of a new burger.
    private Hamburger placedHamburger;

    public override void UseFood(EdibleBase ingredient)
    {
        if(doesHaveHamburger)   return;

        if (ingredient is Hamburger burger)
        {
            if (ingredients.Count > 0) return;
            burger.transform.SetParent(transform, true);
            burger.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
            burger.SetPlaceable(this);
            placedHamburger = burger;
            doesHaveHamburger = true;
            placeableCollider.enabled = false;
            return;
        }

        if (ingredients.Count < MaxLayers)
        {
            SetDistanceBetweenIngredients();

            ShowTopBun(ingredient);
            ingredients.Add(ingredient);
            ingredient.SetPlaceable(this);

            ExtendCollider(ingredient.SetFood());
            SetIngredientPos(ingredient);

            refTransform.position = ingredient.gameObject.transform.position;

            // The first bun is the bottom bun, whenever it comes: lettuce put on before it ends up on top of it
            // (user). A second bun just stacks where it is put.
            if (ingredient.IsBun() && ingredients.Count > 1 && ingredients.Count(x => x.IsBun()) == 1)
            {
                var bottom = ingredients[0].transform.localPosition;   // where the stack starts on this plate
                ingredients.Remove(ingredient);
                ingredients.Insert(0, ingredient);
                Restack(bottom);
            }

            if (ForCustomer == null) Claim();   // the first layer decides whose order it is; then it is kept
            GenerateHamburger();
        }
    }

    // Lays the layers out again from the plate up, in list order, the way they are stacked one by one: each sits
    // on the one below it, raised by that one's height.
    private void Restack(Vector3 bottom)
    {
        float y = bottom.y;
        for (int i = 0; i < ingredients.Count; i++)
        {
            var piece = ingredients[i];
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localPosition = new Vector3(bottom.x, y, bottom.z);
            piece.isLastPiece = i == ingredients.Count - 1;
            y += piece.collider != null ? piece.collider.size.y : 0f;
        }
        refTransform.position = ingredients[ingredients.Count - 1].transform.position;
    }

    public override void RemoveFood(EdibleBase ingredient)
    {
        
        if(ingredients.Count > 1)
        {
            ingredients.Remove(ingredient);
            refTransform.position = ingredients.Last().gameObject.transform.position;
            ingredients.Last().isLastPiece = true;
            CompressCollider();
        }   
        else if(ingredients.Count == 1)
        {
            ingredients.Remove(ingredient);
            ResetColAndRef();
            ForCustomer = null;
        }
        else
        {
            ResetColAndRef();
            doesHaveHamburger = false;
            placedHamburger = null;
        }

        if (ingredient is Bun)
            HideTopBun();
    }
   
    private void GenerateHamburger(bool closing = false)
    {
        if (ingredients.Count > 0 && (closing || ingredients.Count >= MaxLayers))
        {
            SetDistanceBetweenIngredients();
            placeableCollider.enabled = false;
            PoolingManager.HamburgerPool.GetObject(transform, hamburger, PoolingManager.HamburgerList);
            GameObject obj = PoolingManager.HamburgerPool.currentObject;
            Hamburger _hamburger = obj.GetComponent<Hamburger>();
            foreach (EdibleBase item in ingredients)
            {
                _hamburger.AddIngredient(item);
            }
            if (ingredients.Any(x => x.IsBun()))
            {
                HideTopBun();
                _hamburger.PutLastBun(refTransform, parentTransform, distanceBetweenObjects);
            }

            EdibleBase _edibleHam = obj.GetComponent<EdibleBase>();
            ingredients.Add(_edibleHam);
            _edibleHam.SetPlaceable(this);
            doesHaveHamburger = true;
            placedHamburger = _hamburger;
            _hamburger.ForCustomer = ForCustomer;
            ForCustomer = null;
            GameSfx.Play(GameSfx.Cue.BurgerDone);
            KitchenFx.BurgerDone(obj.transform.position + Vector3.up * 0.8f);

            ingredients.Clear();
            refTransform.position = transform.position;
        }
    }

    // PlayerFSM.DropObject asks this right after UseFood, so "just placed this burger here" counts too.
    public override bool IsSuitable(EdibleBase ingredient)
    {
        if (ingredient is Hamburger)
            return placedHamburger == ingredient || (!doesHaveHamburger && ingredients.Count == 0);
        return true;
    }

    // For hints: would this plate take the food the chef is holding?
    public bool Accepts(EdibleBase food) =>
        !doesHaveHamburger && (food is Hamburger ? ingredients.Count == 0 : ingredients.Count < Target);

    #region Additional Features
    /// <summary>
    /// Following codes should be perform in the Bun script actually. I just felt lazy ^d^
    /// </summary>
    [SerializeField] private GameObject guestTopBunPrefab;
    GameObject guestTopBun;
    private void ShowTopBun(EdibleBase ingredient)
    {
        if(ingredient is Bun)
        {
            if (!ingredients.Any(x => x.IsBun()))
            {
                //if (guestTopBun != null)    guestTopBun.SetActive(true);
                //else
                //{
                //    GameObject obj = Object.Instantiate(guestTopBunPrefab);
                //    obj.transform.parent = transform;
                //    obj.transform.localPosition = new Vector3(0.5f, 0f, 0.7f);
                //    guestTopBun = obj;
                //}

                PoolingManager.HamburgerPool.GetObject(transform, guestTopBunPrefab, PoolingManager.bunTopList);
                guestTopBun = PoolingManager.HamburgerPool.currentObject;
                guestTopBun.transform.localPosition = new Vector3(0.5f, 0f, 0.7f);
            }
        }
    }
    private void HideTopBun()
    {
        if(guestTopBun != null)
        {
            guestTopBun.transform.parent = null;
            guestTopBun?.SetActive(false);
        }
    }
    #endregion
}

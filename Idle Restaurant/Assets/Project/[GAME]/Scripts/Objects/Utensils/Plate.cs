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

    public bool HasHamburger => doesHaveHamburger;
    public Hamburger FinishedBurger => doesHaveHamburger ? placedHamburger : null;
    public int LayerCount => ingredients.Count;
    // What is on the plate, by ingredient name ("bun", "burger", "tomato"...): the first-shift lesson walks
    // the player through the ingredients that are still missing.
    public IEnumerable<string> LayerNames => ingredients.Select(x => x.Name);

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

        if (ingredients.Count <= 5)
        {
            SetDistanceBetweenIngredients();

            ShowTopBun(ingredient);
            ingredients.Add(ingredient);
            ingredient.SetPlaceable(this);

            ExtendCollider(ingredient.SetFood());
            SetIngredientPos(ingredient);

            refTransform.position = ingredient.gameObject.transform.position;
              
            GenerateHamburger();
        }
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
   
    private void GenerateHamburger()
    {
        if(ingredients.Count == 6)
        {
            SetDistanceBetweenIngredients();
            placeableCollider.enabled = false;
            PoolingManager.HamburgerPool.GetObject(transform, hamburger, PoolingManager.HamburgerList);
            GameObject obj = PoolingManager.HamburgerPool.currentObject;
            Hamburger _hamburger = obj.GetComponent<Hamburger>();
            foreach (EdibleBase item in ingredients)
            {
                print(item);
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
            GameSfx.Play(GameSfx.Cue.BurgerDone);

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
        !doesHaveHamburger && (food is Hamburger ? ingredients.Count == 0 : ingredients.Count < 6);

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

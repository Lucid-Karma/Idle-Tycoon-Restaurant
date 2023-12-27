using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Plate : MonoBehaviour, IPlaceable
{
    private List<EdibleBase> ingredients = new List<EdibleBase>();
    private int _ingredientsCount;
    private float distanceBetweenObjects;
    private Transform parentTransform { get; set; }
    private Transform refTransform { get; set; }
    private BoxCollider plateCollider;
    Vector3 colSize, colCenter;
    [SerializeField] private GameObject hamburger;
    private bool doesHaveHamburger;
    
    void OnEnable()
    {
        EventManager.OnFoodHolded.AddListener(DisableCollider);
        EventManager.OnFoodDropped.AddListener(() => plateCollider.enabled = false);
    }
    void OnDisable()
    {
        EventManager.OnFoodHolded.RemoveListener(DisableCollider);
        EventManager.OnFoodDropped.RemoveListener(() => plateCollider.enabled = false);
    }
    private void DisableCollider()
    {
        if(!doesHaveHamburger)
            plateCollider.enabled = true;
    }

    void Start()
    {
        doesHaveHamburger = false;

        parentTransform = gameObject.transform;
        refTransform = gameObject.transform.GetChild(0).transform;
    
        plateCollider = GetComponent<BoxCollider>();
        colSize = plateCollider.size;
        colCenter = plateCollider.center;
    }

    private void ExtendCollider(GameObject stackedObj)
    {
        plateCollider.size = new Vector3(plateCollider.size.x, plateCollider.size.y + stackedObj.transform.localScale.y / 2, plateCollider.size.z);
        plateCollider.center = new Vector3(plateCollider.center.x, plateCollider.center.y + stackedObj.transform.localScale.y / 4, plateCollider.center.z);
    }
    private void CompressCollider()
    {
        _ingredientsCount = ingredients.Count;

        plateCollider.size = new Vector3(plateCollider.size.x, plateCollider.size.y - (ingredients[_ingredientsCount-1].transform.localScale.y / 2), plateCollider.size.z);
        plateCollider.center = new Vector3(plateCollider.center.x, plateCollider.center.y - ingredients[_ingredientsCount-1].transform.localScale.y / 4, plateCollider.center.z);
    }

    public void UseFood(EdibleBase ingredient) 
    {
        if(doesHaveHamburger)   return;

        if(ingredients.Count <= 6)
        {
            _ingredientsCount = ingredients.Count;

            if(_ingredientsCount >= 1)
            {
                distanceBetweenObjects = (ingredients[_ingredientsCount-1].gameObject.transform.localScale.y / 2);
                ingredients.Last().isLastPiece = false;
                
                //Debug.Log("local:" + ingredients[_ingredientsCount-1].transform.lossyScale.y / 2 + " \nlossy:" + ingredients[_ingredientsCount-1].transform.localScale.y / 2);
            } 
            else
                distanceBetweenObjects = 0;
            
            ingredients.Add(ingredient);
            ingredient.GetPlaceable(this);

            ExtendCollider(ingredient.gameObject);

            ingredient.gameObject.transform.parent = parentTransform;
            Vector3 desiredPos = refTransform.localPosition;
            desiredPos.y += distanceBetweenObjects;    
            
            ingredient.gameObject.transform.localRotation = Quaternion.identity;
            ingredient.gameObject.transform.localPosition = desiredPos; 

            refTransform.position = ingredient.gameObject.transform.position;

            if(ingredients.Count == 7)
            {
                plateCollider.enabled = false;
                GameObject obj = (GameObject)Instantiate(hamburger);
                obj.transform.position = transform.position;
                foreach (EdibleBase item in ingredients)
                {
                    item.gameObject.transform.parent = obj.transform;
                    obj.GetComponent<Hamburger>().ExtendCollider(item.gameObject);
                    item.gameObject.GetComponent<Collider>().enabled = false;
                }
                EdibleBase _hamburger = obj.GetComponent<EdibleBase>();
                ingredients.Add(_hamburger);
                _hamburger.GetPlaceable(this);
                doesHaveHamburger = true;

                ingredients.Clear();
                refTransform.position = transform.position;
            }
        }
    }

    public void RemoveFoodFromPlate(EdibleBase ingredient)
    {
        ingredients.Remove(ingredient);
        if(ingredients.Count >= 1)
        {
            refTransform.position = ingredients.Last().gameObject.transform.position;
            ingredients.Last().isLastPiece = true;
            CompressCollider();
        }   
        else
        {
            refTransform.position = gameObject.transform.position;

            plateCollider.size = colSize;
            plateCollider.center = colCenter;
            plateCollider.enabled = true;

            doesHaveHamburger = false;
        }
    }
}

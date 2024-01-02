using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Plate : PlaceableBase
{
    private List<EdibleBase> ingredients = new List<EdibleBase>();
    private int _ingredientsCount;
    private float distanceBetweenObjects;
    private Transform parentTransform { get; set; }
    private Transform refTransform { get; set; }
    Vector3 colSize, colCenter;
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

    public override void UseFood(EdibleBase ingredient) 
    {
        if(doesHaveHamburger)   return;

        if(ingredients.Count <= 5)
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

            if(ingredients.Count == 6)
            {
                placeableCollider.enabled = false;
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

    public override void RemoveFood(EdibleBase ingredient)
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

            placeableCollider.size = colSize;
            placeableCollider.center = colCenter;
            placeableCollider.enabled = true;

            doesHaveHamburger = false;
        }
    }
}

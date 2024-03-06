using UnityEngine;

public class Billboard : MonoBehaviour
{
    Canvas canvas;
    private Transform _mainCameraTransform;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
        
        _mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + _mainCameraTransform.forward);
    }
}

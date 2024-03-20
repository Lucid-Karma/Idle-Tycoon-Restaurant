using UnityEngine;
using UnityEngine.EventSystems;

public class HighlightController : MonoBehaviour
{
    private ISelectable selectable;
    private Transform _selection;

    private Renderer selectionRenderer;
    private Transform selection;

    private Camera _camera;
    private Ray ray;
    private RaycastHit hit;

    [SerializeField] private Material highlightMaterial;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (_selection != null)
        {
            selectionRenderer = _selection.GetComponent<Renderer>();
            if (selectionRenderer != null)
            {
                selectionRenderer.material = selectable?.DefaultMaterial();
            }
            else
            {
                if (selectable is not Hamburger)
                {
                    if (_selection.childCount > 0)
                    {
                        for (int i = 0; i < _selection.childCount; i++)
                        {
                            selectionRenderer = _selection.GetChild(i).GetComponent<Renderer>();
                            if (selectionRenderer != null)
                            {
                                selectionRenderer.material = selectable?.DefaultMaterial();
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _selection.childCount; i++)
                    {
                        selectionRenderer = _selection.GetChild(i).GetComponent<Renderer>();
                        if (selectionRenderer != null)
                        {
                            selectionRenderer.material = selectable?.DefaultMaterial();
                        }

                        for (int j = 0; j < _selection.GetChild(i).childCount; j++)
                        {
                            selectionRenderer = _selection.GetChild(i).GetChild(j).GetComponent<Renderer>();
                            if (selectionRenderer != null)
                            {
                                selectionRenderer.material = selectable?.DefaultMaterial();
                            }
                        }
                    }
                }
            }
            _selection = null;
        }

        ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            selection = hit.transform;
            selectable = selection.GetComponent<ISelectable>();
            if (selectable == null) return;

            selectionRenderer = selection.GetComponent<Renderer>();
            if (selectionRenderer != null)
            {
                selectionRenderer.material = highlightMaterial;
            }
            else
            {
                if(selection.childCount > 0)
                {
                    if (selectable is not Hamburger)
                    {
                        for (int i = 0; i < selection.childCount; i++)
                        {
                            selectionRenderer = selection.GetChild(i).GetComponent<Renderer>();
                            if (selectionRenderer != null)
                            {
                                selectionRenderer.material = highlightMaterial;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < selection.childCount; i++)
                        {
                            selectionRenderer = selection.GetChild(i).GetComponent<Renderer>();
                            if (selectionRenderer != null)
                            {
                                selectionRenderer.material = highlightMaterial;
                            }

                            for (int j = 0; j < selection.GetChild(i).childCount; j++)
                            {
                                selectionRenderer = selection.GetChild(i).GetChild(j).GetComponent<Renderer>();
                                if (selectionRenderer != null)
                                {
                                    selectionRenderer.material = highlightMaterial;
                                }
                            }
                        }
                    }
                }
            }
            _selection = selection;
        }
    }
}

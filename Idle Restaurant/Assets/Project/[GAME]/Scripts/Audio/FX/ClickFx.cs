using UnityEngine;

public class ClickFx : MonoBehaviour
{
    private AudioSource clickFx;

    void Start()
    {
        clickFx = gameObject.GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        EventManager.OnClick.AddListener(() => clickFx.Play());
    }
    void OnDisable()
    {
        EventManager.OnClick.RemoveListener(() => clickFx.Play());
    }
}

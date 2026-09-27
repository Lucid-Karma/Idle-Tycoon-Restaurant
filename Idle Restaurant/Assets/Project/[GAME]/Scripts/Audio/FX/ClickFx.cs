using UnityEngine;

public class ClickFx : MonoBehaviour
{
    private AudioSource clickFx;

    void Awake()
    {
        clickFx = gameObject.GetComponent<AudioSource>();
    }

    // A named method (not a lambda) so RemoveListener matches what AddListener registered.
    void OnEnable()
    {
        EventManager.OnClick.AddListener(PlayClick);
    }
    void OnDisable()
    {
        EventManager.OnClick.RemoveListener(PlayClick);
    }

    private void PlayClick()
    {
        clickFx.Play();
    }
}

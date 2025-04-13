using UnityEngine;
using UnityEngine.UIElements;

public class NeonRandomFlicker : MonoBehaviour
{
    public Material neonMaterial;
    public Color baseColor = Color.cyan;
    public float minTime = 0.05f;
    public float maxTime = 0.5f;
    public AudioSource flickerSound;

    private float nextFlickerTime;
    private bool isOn = true;
    private bool isMusicOn;

    void Start()
    {
        nextFlickerTime = Time.time + Random.Range(minTime, maxTime);
    }

    void Update()
    {
        if (Time.time >= nextFlickerTime)
        {
            isOn = !isOn;
            Color finalColor = isOn ? baseColor * Mathf.LinearToGammaSpace(2.0f) : Color.black;
            neonMaterial.SetColor("_EmissionColor", finalColor);

            if(isMusicOn)
            {
                if (isOn && flickerSound != null)
                {
                    flickerSound.Play();
                }
            }

            nextFlickerTime = Time.time + Random.Range(minTime, maxTime);
        }
    }

    void OnEnable()
    {
        isMusicOn = true;

        EventManager.OnMusicOn.AddListener(PlayMusic);
        EventManager.OnMusicOff.AddListener(PauseMusic);
        EventManager.OnGameEnd.AddListener(PauseMusic);
    }
    private void OnDisable()
    {
        neonMaterial.SetColor("_EmissionColor", baseColor);

        EventManager.OnMusicOn.RemoveListener(PlayMusic);
        EventManager.OnMusicOff.RemoveListener(PauseMusic);
        EventManager.OnGameEnd.RemoveListener(PauseMusic);
    }

    public void PlayMusic()
    {
        flickerSound.Play();
        isMusicOn = true;
    }
    public void PauseMusic()
    {
        flickerSound.Pause();
        isMusicOn = false;
    }
}

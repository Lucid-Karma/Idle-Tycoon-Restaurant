using UnityEngine;

public class NeonRandomFlicker : MonoBehaviour
{
    public Material neonMaterial;
    public Color baseColor = Color.cyan;
    public float minTime = 0.05f;
    public float maxTime = 0.5f;
    public AudioSource flickerSound;

    private float nextFlickerTime;
    private bool isOn = true;

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

            if (isOn && flickerSound != null)
            {
                flickerSound.Play();
            }

            nextFlickerTime = Time.time + Random.Range(minTime, maxTime);
        }
    }

    private void OnDisable()
    {
        neonMaterial.SetColor("_EmissionColor", baseColor);
    }
}

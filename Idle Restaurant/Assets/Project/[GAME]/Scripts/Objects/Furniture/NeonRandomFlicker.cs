using UnityEngine;

public class NeonRandomFlicker : MonoBehaviour
{
    public Material neonMaterial;
    public Color baseColor = Color.cyan;
    public float minTime = 1.5f;
    public float maxTime = 10.5f;
    public AudioSource flickerSound;
    public AudioClip flickerClip;

    private float nextFlickerTime;
    private bool isOn = true;

    private float[] spectrumData = new float[256]; // Frekans verisi için dizi

    void Start()
    {
        nextFlickerTime = Time.time + Random.Range(minTime, maxTime);
        //flickerSound.Play();
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
                PlayRandomFlickerSound();
            }

            nextFlickerTime = Time.time + Random.Range(minTime, maxTime);
        }

        //// Sesin frekans verisini alýyoruz
        //flickerSound.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        //// Sesin frekans verilerine göre ýþýðý kontrol et
        //float maxAmplitude = 0f;

        //// Dalga verisinde en yüksek genliði buluyoruz
        //for (int i = 0; i < spectrumData.Length; i++)
        //{
        //    if (spectrumData[i] > maxAmplitude)
        //    {
        //        maxAmplitude = spectrumData[i];
        //    }
        //}

        //// Eðer genlik belli bir eþik deðerini aþarsa ýþýðý aç
        //if (maxAmplitude > 0.1f) // Bu eþik deðeri sesin þiddetine göre ayarlanabilir
        //{
        //    UpdateNeonEmission(true);
        //}
        //else
        //{
        //    UpdateNeonEmission(false);
        //}
    }

    void UpdateNeonEmission(bool state)
    {
        Color finalColor = state ? baseColor * Mathf.LinearToGammaSpace(2.0f) : Color.black;
        neonMaterial.SetColor("_EmissionColor", finalColor);
        nextFlickerTime = Time.time + Random.Range(minTime, maxTime);
    }

    void PlayRandomFlickerSound()
    {
        //AudioClip clip = flickerClips[Random.Range(0, flickerClips.Length)];
        //flickerSound.pitch = Random.Range(1.0f, 1.5f);
        //flickerSound.volume = Random.Range(0.1f, 0.7f);
        //flickerSound.PlayOneShot(flickerClip);
        flickerSound.Play();
    }

    private void OnDisable()
    {
        neonMaterial.SetColor("_EmissionColor", baseColor);
    }
}

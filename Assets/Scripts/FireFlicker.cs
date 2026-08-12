using UnityEngine;


public class FireFlicker : MonoBehaviour
{
    [Header("Pengaturan Flicker")]
    [Tooltip("Intensitas cahaya minimum")]
    public float minIntensity = 1.2f;
    [Tooltip("Intensitas cahaya maksimum")]
    public float maxIntensity = 3.5f;
    [Tooltip("Seberapa cepat flicker terjadi (lebih tinggi = lebih cepat)")]
    public float flickerSpeed = 8f;
    [Tooltip("Perubahan posisi acak kecil untuk efek lebih dinamis")]
    public float positionJitter = 0.02f;

    [Header("Warna Api")]
    public Color colorA = new Color(1.0f, 0.6f, 0.1f); // Oranye hangat
    public Color colorB = new Color(1.0f, 0.8f, 0.3f); // Kuning cerah

    private Light lightComp;
    private Vector3 originalLocalPos;
    private float noiseOffset;

    private void Awake()
    {
        lightComp = GetComponent<Light>();
        originalLocalPos = transform.localPosition;
        noiseOffset = Random.Range(0f, 100f);

        if (lightComp == null)
        {
            Debug.LogWarning("[FireFlicker] Tidak ada komponen Light ditemukan!");
        }
    }

    private void Update()
    {
        if (lightComp == null) return;

        float time = Time.time * flickerSpeed + noiseOffset;

        // Gunakan Perlin Noise untuk flicker yang smooth dan alami
        float noiseVal = Mathf.PerlinNoise(time, time * 0.7f);
        float noiseVal2 = Mathf.PerlinNoise(time * 1.3f + 5f, time * 0.5f);

        // Intensitas cahaya & warna bergeser antara oranye dan kuning
        lightComp.intensity = Mathf.Lerp(minIntensity, maxIntensity, noiseVal);
        lightComp.color = Color.Lerp(colorA, colorB, noiseVal2);

        // Jitter posisi hanya jika diaktifkan
        if (positionJitter > 0.001f)
        {
            float jx = (Mathf.PerlinNoise(time * 2f + 10f, 0f) - 0.5f) * 2f * positionJitter;
            float jy = (Mathf.PerlinNoise(0f, time * 2f + 10f) - 0.5f) * 2f * positionJitter;
            transform.localPosition = originalLocalPos + new Vector3(jx, jy, 0f);
        }
    }

    /// <summary>Matikan flicker (saat api padam)</summary>
    public void StopFlicker()
    {
        if (lightComp != null)
        {
            lightComp.intensity = 0f;
            enabled = false;
        }
    }

    /// <summary>Nyalakan kembali flicker</summary>
    public void StartFlicker()
    {
        if (lightComp != null)
        {
            lightComp.intensity = minIntensity;
            enabled = true;
        }
    }
}

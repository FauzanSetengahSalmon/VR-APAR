using UnityEngine;


public class KitchenLightFlicker : MonoBehaviour
{
    [Header("Pengaturan Lampu")]
    public float baseIntensity = 2.5f;
    [Tooltip("Seberapa sering lampu neon 'berkedip' sekali (Hz)")]
    public float flickerChance = 0.008f;
    [Tooltip("Durasi kedip sesaat (detik)")]
    public float flickerDuration = 0.05f;
    [Tooltip("Apakah lampu bisa berkedip 2-3 kali berturut?")]
    public bool allowDoubleFlicker = true;

    private Light lightComp;
    private bool isFlickering = false;
    private float flickerEndTime = 0f;
    private int flickerCount = 0;

    private void Awake()
    {
        lightComp = GetComponent<Light>();
        if (lightComp != null) lightComp.intensity = baseIntensity;
    }

    private void Update()
    {
        if (lightComp == null) return;

        if (isFlickering)
        {
            // Lampu mati sesaat
            lightComp.intensity = 0f;
            if (Time.time >= flickerEndTime)
            {
                lightComp.intensity = baseIntensity;
                isFlickering = false;

                // Kemungkinan double/triple flicker
                if (allowDoubleFlicker && flickerCount < 3 && Random.value < 0.4f)
                {
                    flickerCount++;
                    TriggerFlicker();
                }
                else
                {
                    flickerCount = 0;
                }
            }
        }
        else
        {
            // Sedikit noise pada intensitas (neon tidak sempurna)
            lightComp.intensity = baseIntensity + Mathf.Sin(Time.time * 60f) * 0.02f;

            // Cek apakah harus berkedip
            if (Random.value < flickerChance)
            {
                flickerCount = 0;
                TriggerFlicker();
            }
        }
    }

    private void TriggerFlicker()
    {
        isFlickering = true;
        flickerEndTime = Time.time + flickerDuration;
        if (lightComp != null) lightComp.intensity = 0f;
    }

    public void SetIntensity(float intensity)
    {
        baseIntensity = intensity;
        if (lightComp != null) lightComp.intensity = intensity;
    }
}

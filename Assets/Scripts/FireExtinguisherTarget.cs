using System.Collections;
using UnityEngine;

/// <summary>
/// Dipasang pada setiap titik api.
/// Menangani pemadaman bertahap & visual api meredup secara presisi.
/// </summary>
public class FireExtinguisherTarget : MonoBehaviour
{
    [Header("Referensi Api")]
    public ParticleSystem fireParticle;
    public ParticleSystem innerFireParticle;
    public ParticleSystem smokeFromFire;
    public ParticleSystem embersParticle;

    [Header("Point Light Api")]
    public Light fireLight;

    [Header("Pengaturan Pemadaman")]
    [Tooltip("Lama waktu padam dalam detik. Misal 0.2 = butuh ~5 detik semprotan konstan")]
    public float extinguishSpeed = 0.2f;

    [Header("Audio")]
    public AudioClip hissAudioClip;
    [Range(0f, 1f)] public float hissVolume = 0.85f;

    [Header("Manager")]
    public FireAlarmSystem alarmSystem;
    public FireManager fireManager;

    // --- State Internal ---
    private float currentHealth = 1.0f;
    private Vector3 originalScale;
    private bool isExtinguished = false;
    private float timeSinceLastHit = 99f;

    // --- Cache ---
    private AudioSource audioSource;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.EmissionModule innerEmissionModule;
    private ParticleSystem.EmissionModule embersEmissionModule;
    private float originalEmissionRate = 80f;
    private float originalInnerEmissionRate = 40f;
    private float originalEmbersEmissionRate = 25f;
    private FireFlicker fireFlickerScript;

    private void Start()
    {
        if (fireParticle == null)
            fireParticle = GetComponent<ParticleSystem>();

        originalScale = transform.localScale;

        if (fireParticle != null)
        {
            emissionModule = fireParticle.emission;
            originalEmissionRate = emissionModule.rateOverTime.constant;
            if (originalEmissionRate <= 0f) originalEmissionRate = 80f;
        }

        if (innerFireParticle != null)
        {
            innerEmissionModule = innerFireParticle.emission;
            originalInnerEmissionRate = innerEmissionModule.rateOverTime.constant;
            if (originalInnerEmissionRate <= 0f) originalInnerEmissionRate = 40f;
        }

        if (embersParticle != null)
        {
            embersEmissionModule = embersParticle.emission;
            originalEmbersEmissionRate = embersEmissionModule.rateOverTime.constant;
            if (originalEmbersEmissionRate <= 0f) originalEmbersEmissionRate = 25f;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.volume = hissVolume;
        audioSource.minDistance = 0.3f;
        audioSource.maxDistance = 5f;

        if (fireLight != null)
            fireFlickerScript = fireLight.GetComponent<FireFlicker>();

        if (alarmSystem == null)
            alarmSystem = FindFirstObjectByType<FireAlarmSystem>();
        if (fireManager == null)
            fireManager = FindFirstObjectByType<FireManager>();
    }

    /// <summary>
    /// Dipanggil bertahap saat terkena semprotan APAR
    /// </summary>
    public void ExtinguishGradually(float deltaTime)
    {
        if (isExtinguished) return;

        timeSinceLastHit = 0f;

        // Kurangi HP api berdasarkan Extinguish Speed di Inspector
        currentHealth -= deltaTime * extinguishSpeed;
        currentHealth = Mathf.Clamp01(currentHealth);

        // Putar suara desisan
        if (audioSource != null && hissAudioClip != null && !audioSource.isPlaying)
        {
            audioSource.clip = hissAudioClip;
            audioSource.Play();
        }

        ApplyHealthToVisuals();

        if (currentHealth <= 0.01f)
        {
            OnFireExtinguished();
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (isExtinguished) return;
        if (!other.CompareTag("Smoke")) return;

        // Pemadaman berbasis benturan partikel fisika
        ExtinguishGradually(Time.deltaTime);
    }

    private void Update()
    {
        if (isExtinguished) return;

        timeSinceLastHit += Time.deltaTime;

        // Jika tidak disiram lagi > 0.3s, suara desisan berhenti
        if (timeSinceLastHit > 0.3f && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void ApplyHealthToVisuals()
    {
        float safeHealth = Mathf.Max(currentHealth, 0.001f);
        transform.localScale = originalScale * safeHealth;

        if (fireParticle != null)
            emissionModule.rateOverTime = originalEmissionRate * currentHealth;
        if (innerFireParticle != null)
            emissionModule.rateOverTime = originalInnerEmissionRate * currentHealth;
        if (embersParticle != null)
            embersEmissionModule.rateOverTime = originalEmbersEmissionRate * currentHealth;

        if (fireLight != null)
        {
            fireLight.intensity = Mathf.Lerp(0f, 3.5f, currentHealth);
            if (currentHealth <= 0.2f && fireFlickerScript != null)
                fireFlickerScript.enabled = false;
        }
    }

    private void OnFireExtinguished()
    {
        isExtinguished = true;

        if (fireParticle != null)
        {
            emissionModule.rateOverTime = 0f;
            fireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (innerFireParticle != null)
        {
            innerEmissionModule.rateOverTime = 0f;
            innerFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (embersParticle != null)
        {
            embersEmissionModule.rateOverTime = 0f;
            embersParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (smokeFromFire != null)
            smokeFromFire.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (fireLight != null) fireLight.intensity = 0f;
        if (fireFlickerScript != null) fireFlickerScript.StopFlicker();

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (fireManager != null)
        {
            fireManager.OnFireExtinguished(this);
        }

        StartCoroutine(DisableAfterDelay(1.5f));
    }

    private IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FireExtinguisherTarget : MonoBehaviour
{
    [Header("Referensi Api")]
    public ParticleSystem fireParticle;
    public ParticleSystem innerFireParticle;
    public ParticleSystem smokeFromFire;
    public ParticleSystem embersParticle;

    [Header("Efek Gosong (Decal)")]
    [Tooltip("Drag GameObject BurnDecal ke sini")]
    public DecalProjector burnDecal;

    [Header("Point Light Api")]
    public Light fireLight;
    private Vector3 initialWorldPosition;
    private Quaternion initialWorldRotation;

    [Header("Pengaturan Pemadaman")]
    public float extinguishSpeed = 0.2f;

    [Header("Audio")]
    public AudioClip hissAudioClip;
    [Range(0f, 1f)] public float hissVolume = 0.85f;

    [Header("Manager")]
    public FireAlarmSystem alarmSystem;
    public FireManager fireManager;

    private float currentHealth = 1.0f;
    private Vector3 originalScale;
    private bool isExtinguished = false;
    private float timeSinceLastHit = 99f;

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

        if (burnDecal != null)
        {
            // 1. Simpan posisi & rotasi asli Decal di world sebelum parent-nya mengecil
            initialWorldPosition = burnDecal.transform.position;
            initialWorldRotation = burnDecal.transform.rotation;

            burnDecal.fadeFactor = 0f; // Sembunyikan di awal
        }

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

    public void ExtinguishGradually(float deltaTime)
    {
        if (isExtinguished) return;

        timeSinceLastHit = 0f;

        currentHealth -= deltaTime * extinguishSpeed;
        currentHealth = Mathf.Clamp01(currentHealth);

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

    private float lastParticleCollisionTime = 0f;

    private void OnParticleCollision(GameObject other)
    {
        if (isExtinguished) return;

        if (Time.time - lastParticleCollisionTime < 0.1f) return;
        lastParticleCollisionTime = Time.time;

        if (other.CompareTag("Smoke"))
        {
            ExtinguishGradually(0.1f);
        }
    }

    private void Update()
    {
        if (isExtinguished) return;

        timeSinceLastHit += Time.deltaTime;

        if (timeSinceLastHit > 0.3f && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void ApplyHealthToVisuals()
    {
        float safeHealth = Mathf.Max(currentHealth, 0.001f);

        // Logika mengecilkan api tetap seperti bawaan kamu
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

        if (burnDecal != null)
        {
            // 2. Lepas parent
            burnDecal.transform.SetParent(null);

            // 3. KEMBALIKAN posisi, rotasi, & skala murni dari koordinat awal
            burnDecal.transform.position = initialWorldPosition;
            burnDecal.transform.rotation = initialWorldRotation;
            burnDecal.transform.localScale = Vector3.one;

            burnDecal.fadeFactor = 1f; // Tampilkan 100%
        }

        if (fireParticle != null)
        {
            emissionModule.rateOverTime = 0f;
            fireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (innerFireParticle != null)
        {
            innerEmissionModule = innerFireParticle.emission;
            innerEmissionModule.rateOverTime = 0f;
            innerFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (embersParticle != null)
        {
            embersEmissionModule = embersParticle.emission;
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
        gameObject.SetActive(false); // Objek Fire mati, tapi BurnDecal tetap ada karena sudah di-unparent
    }
}
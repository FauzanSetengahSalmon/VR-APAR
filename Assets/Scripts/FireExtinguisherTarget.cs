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

    [Header("Efek Gosong (Quad Mesh / Decal)")]
    [Tooltip("Drag GameObject Quad / Mesh bekas gosong ke sini (Sangat direkomendasikan untuk VR)")]
    public GameObject burnMarkObject;

    [Tooltip("Drag GameObject BurnDecal jika memakai DecalProjector")]
    public DecalProjector burnDecal;

    [Header("Point Light Api")]
    public Light fireLight;

    [Header("Pengaturan Pemadaman")]
    public float extinguishSpeed = 0.2f;

    [Header("Audio")]
    public AudioClip hissAudioClip;
    [Range(0f, 1f)] public float hissVolume = 0.85f;

    [Header("Manager")]
    public FireAlarmSystem alarmSystem;
    public FireManager fireManager;

    [Header("Batas Fisik Api (Mentok)")]
    [Tooltip("Radius pembatas fisik solid agar pemain tidak bisa menginjak/menerobos api (meter).")]
    public float barrierRadius = 0.85f;
    [Tooltip("Tinggi pembatas fisik (meter).")]
    public float barrierHeight = 2.0f;

    public bool IsExtinguished => isExtinguished;
    public float CurrentHealth => currentHealth;

    private float currentHealth = 1.0f;
    private Vector3 originalScale;
    private bool isExtinguished = false;
    private float timeSinceLastHit = 99f;

    private CapsuleCollider _barrierCollider;
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

        // ── 1. BURN MARK: Selalu aktif & terlihat dari awal sampai akhir ──
        if (burnMarkObject != null)
        {
            // Lepas parent sejak awal dengan mempertahankan world transform asli
            burnMarkObject.transform.SetParent(null, true);
            burnMarkObject.SetActive(true); // Selalu aktif dari awal sampai akhir
        }

        if (burnDecal != null)
        {
            // Lepas parent sejak awal dengan mempertahankan world transform asli
            burnDecal.transform.SetParent(null, true);
            burnDecal.fadeFactor = 1f; // Selalu terlihat 100% dari awal sampai akhir
        }

        // ── 2. SMOKE: Auto-find jika belum diisi & Unparent sejak awal tanpa merusak transform ──
        if (smokeFromFire == null)
        {
            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps != fireParticle && ps != innerFireParticle && ps != embersParticle)
                {
                    if (ps.gameObject.name.IndexOf("smoke", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        ps.gameObject.name.IndexOf("asap", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        smokeFromFire = ps;
                        break;
                    }
                }
            }
        }

        if (smokeFromFire != null)
        {
            // Lepas parent dengan mempertahankan posisi, rotasi, dan skala asli 100%
            smokeFromFire.transform.SetParent(null, true);
            smokeFromFire.gameObject.SetActive(true);
            smokeFromFire.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        SetupPhysicalBarrier();
    }

    private void SetupPhysicalBarrier()
    {
        // Buat collider pembatas solid berdiri tegak di world space agar player tertahan dan tidak bisa menginjak api
        GameObject barrierGO = new GameObject($"{gameObject.name}_Physical_Barrier");
        barrierGO.transform.position = transform.position;
        barrierGO.transform.rotation = Quaternion.identity;
        barrierGO.transform.localScale = Vector3.one;

        _barrierCollider = barrierGO.AddComponent<CapsuleCollider>();
        _barrierCollider.radius = barrierRadius;
        _barrierCollider.height = barrierHeight;
        _barrierCollider.center = Vector3.up * (barrierHeight * 0.5f);
        _barrierCollider.isTrigger = false; // Solid physical barrier
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

        // Logika mengecilkan api tetap seperti semula
        transform.localScale = originalScale * safeHealth;

        if (fireParticle != null)
            emissionModule.rateOverTime = originalEmissionRate * currentHealth;
        if (innerFireParticle != null)
            innerEmissionModule.rateOverTime = originalInnerEmissionRate * currentHealth;
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

        // Hancurkan pembatas fisik agar area api bisa dilewati kembali setelah padam
        if (_barrierCollider != null)
        {
            _barrierCollider.enabled = false;
            Destroy(_barrierCollider.gameObject);
        }

        // Pastikan Burn Mark tetap aktif
        if (burnMarkObject != null)
        {
            burnMarkObject.SetActive(true);
        }

        if (burnDecal != null)
        {
            burnDecal.fadeFactor = 1f;
        }

        // Matikan lidah api & percikan
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

        // ── 2. SMOKE: Munculkan saat api padam persis sesuai settingan asli partikel Anda ──
        if (smokeFromFire != null)
        {
            smokeFromFire.gameObject.SetActive(true);
            var em = smokeFromFire.emission;
            em.enabled = true;

            smokeFromFire.Play(true);
            Debug.Log($"[FireExtinguisherTarget] 💨 Api padam! Asap '{smokeFromFire.gameObject.name}' berhasil dimunculkan.");
        }
        else
        {
            Debug.LogWarning("[FireExtinguisherTarget] ⚠️ Api padam, tetapi slot 'Smoke From Fire' kosong! Silakan drag Particle System asap ke slot tersebut di Inspector.");
        }

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
        gameObject.SetActive(false); // Objek Fire mati, tapi Burn mark & Smoke tetap ada karena sudah di-unparent sejak Start
    }
}
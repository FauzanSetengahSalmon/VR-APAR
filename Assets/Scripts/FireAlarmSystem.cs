using UnityEngine;

public class FireAlarmSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────
    [Header("Referensi Manager")]
    [Tooltip("Bisa dikosongkan – akan dicari otomatis")]
    public FireManager fireManager;

    [Header("Lampu Indikator")]
    [Tooltip("Light component pada Smoke Detector (Point / Spot light). Bisa dikosongkan – akan dibuat otomatis.")]
    public Light indicatorLight;

    [Tooltip("Intensitas saat lampu merah menyala penuh")]
    public float redMaxIntensity = 5f;

    [Tooltip("Kecepatan kedip merah (berapa kali per detik, misal 2 = 2x kedip/detik)")]
    public float blinkRate = 2f;

    [Tooltip("Intensitas lampu hijau saat api padam")]
    public float greenIntensity = 2f;

    [Tooltip("Range lampu indikator (meter)")]
    public float lightRange = 3f;

    [Header("Audio Alarm")]
    [Tooltip("Clip suara sirene / alarm detektor. Jika kosong, akan dibuat beep otomatis.")]
    public AudioClip alarmClip;

    [Tooltip("Jika true, alarm akan berulang terus. Untuk clip 7 detik biasanya lebih baik false agar tidak terdengar putus-putus.")]
    public bool loopAlarm = false;

    [Range(0f, 1f)]
    public float alarmVolume = 0.8f;

    [Tooltip("Frekuensi nada beep (Hz) jika generate otomatis")]
    public float beepFrequency = 880f;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────
    private AudioSource audioSource;
    private bool alarmActive = true;
    private float blinkTimer = 0f;
    private bool lightOn = true;

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    private void Awake()
    {
        // --- Buat Light otomatis jika belum di-assign ---
        if (indicatorLight == null)
        {
            // Cek apakah sudah ada child AlarmIndicatorLight
            Transform existing = transform.Find("AlarmIndicatorLight");
            if (existing != null)
            {
                indicatorLight = existing.GetComponent<Light>();
            }
            else
            {
                var lightGO = new GameObject("AlarmIndicatorLight");
                lightGO.transform.SetParent(transform);
                lightGO.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                indicatorLight = lightGO.AddComponent<Light>();
                indicatorLight.type = LightType.Point;
                indicatorLight.range = lightRange;
                indicatorLight.shadows = LightShadows.None;
            }
        }

        // --- Audio setup ---
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Generate beep otomatis jika tidak ada alarmClip
        if (alarmClip == null)
            alarmClip = GenerateBeepClip(beepFrequency, 0.5f);

        audioSource.clip         = alarmClip;
        audioSource.loop         = loopAlarm;
        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 1f;      // 3D sound
        audioSource.volume       = alarmVolume;
        audioSource.minDistance  = 0.5f;
        audioSource.maxDistance  = 15f;

        // Cari FireManager otomatis jika belum di-assign
        if (fireManager == null)
            fireManager = FindFirstObjectByType<FireManager>();
    }

    private void Start()
    {
        // Inisialisasi: lampu merah menyala, alarm berbunyi
        if (indicatorLight != null)
        {
            indicatorLight.color     = Color.red;
            indicatorLight.intensity = redMaxIntensity;
            indicatorLight.range     = lightRange;
        }

        StartAlarm();
    }

    private void Update()
    {
        if (!alarmActive) return;

        // ── Kedip merah: toggle ON/OFF tegas sesuai blinkRate ──
        blinkTimer += Time.deltaTime;
        float halfPeriod = 0.5f / blinkRate;   // durasi tiap fase nyala/mati

        if (blinkTimer >= halfPeriod)
        {
            blinkTimer -= halfPeriod;
            lightOn = !lightOn;

            if (indicatorLight != null)
                indicatorLight.intensity = lightOn ? redMaxIntensity : 0f;
        }
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Dipanggil FireManager saat SEMUA api padam.
    /// Matikan alarm dan ganti lampu ke HIJAU solid.
    /// </summary>
    public void StopAlarm()
    {
        if (!alarmActive) return;
        alarmActive = false;

        // Hentikan suara sirene
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        // Ganti lampu ke HIJAU solid
        if (indicatorLight != null)
        {
            indicatorLight.color     = Color.green;
            indicatorLight.intensity = greenIntensity;
        }

        Debug.Log("[FireAlarmSystem] ✅ Alarm dimatikan – semua api padam. Lampu hijau menyala.");
    }

    /// <summary>
    /// Nyalakan alarm secara manual (atau reset ulang).
    /// </summary>
    public void StartAlarm()
    {
        alarmActive = true;
        lightOn     = true;
        blinkTimer  = 0f;

        if (indicatorLight != null)
        {
            indicatorLight.color     = Color.red;
            indicatorLight.intensity = redMaxIntensity;
        }

        if (audioSource != null && alarmClip != null)
        {
            audioSource.clip = alarmClip;
            audioSource.loop = loopAlarm;

            if (!audioSource.isPlaying)
                audioSource.Play();
        }

        Debug.Log("[FireAlarmSystem] 🔴 Alarm aktif – lampu merah berkedip + suara alarm.");
    }

    // ─────────────────────────────────────────────
    //  UTILITY: Generate Beep Clip Procedural
    // ─────────────────────────────────────────────

    /// <summary>
    /// Generate AudioClip berupa beep sederhana (sine wave) secara procedural.
    /// Tidak membutuhkan file audio external.
    /// </summary>
    private AudioClip GenerateBeepClip(float frequency, float duration)
    {
        int sampleRate   = 44100;
        int sampleCount  = Mathf.RoundToInt(sampleRate * duration);
        float[] samples  = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Sine wave beep
            float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);
            // Fade out di akhir supaya tidak klik
            float envelope = 1f;
            if (i > sampleCount - 2000)
                envelope = (float)(sampleCount - i) / 2000f;
            samples[i] = wave * envelope * 0.7f;
        }

        AudioClip clip = AudioClip.Create("AlarmBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}


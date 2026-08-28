using System.Collections.Generic;
using UnityEngine;

public class FireManager : MonoBehaviour
{
    [Header("Daftar Api")]
    public List<FireExtinguisherTarget> fireTargets = new List<FireExtinguisherTarget>();

    [Header("Karakter NPC Audio")]
    [Tooltip("Drag AudioSource milik karakter-karakter yang berteriak ke sini")]
    public List<AudioSource> characterAudioSources = new List<AudioSource>();

    [Header("Efek Sukses Kebakaran Padam")]
    public AudioClip victoryAudioClip;
    public ParticleSystem victorySmokeEffect;
    [Tooltip("Partikel kabut asap plafon ruangan")]
    public ParticleSystem indoorCeilingSmoke;

    [Header("Akumulasi Asap Plafon Bertahap")]
    [Tooltip("Apakah asap bertambah tebal dan banyak seiring berjalannya waktu kebakaran?")]
    public bool enableProgressiveSmoke = true;
    [Tooltip("Waktu dalam detik untuk mencapai ketebalan asap maksimal")]
    public float timeToMaxSmoke = 25f;
    [Tooltip("Jumlah emisi asap awal (detik pertama)")]
    public float minSmokeEmission = 2f;
    [Tooltip("Jumlah emisi asap puncak (saat kebakaran lama)")]
    public float maxSmokeEmission = 55f;

    private int activeFireCount = 0;
    private AudioSource audioSource;
    private bool allExtinguished = false;
    private float smokeBurningTime = 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound for UI/Victory
    }

    private void Start()
    {
        // Cari semua titik api di scene jika list kosong
        if (fireTargets.Count == 0)
        {
            fireTargets.AddRange(FindObjectsByType<FireExtinguisherTarget>(FindObjectsSortMode.None));
        }

        activeFireCount = fireTargets.Count;
        Debug.Log("[FireManager] Jumlah titik api aktif: " + activeFireCount);

        if (indoorCeilingSmoke == null)
        {
            var ceilingSmokeGO = GameObject.Find("Indoor_Ceiling_Smoke");
            if (ceilingSmokeGO != null) indoorCeilingSmoke = ceilingSmokeGO.GetComponent<ParticleSystem>();
        }

        if (indoorCeilingSmoke != null && enableProgressiveSmoke)
        {
            var main = indoorCeilingSmoke.main;
            main.prewarm = false;
            var em = indoorCeilingSmoke.emission;
            em.rateOverTime = minSmokeEmission;
            indoorCeilingSmoke.Play();
        }
    }

    private void Update()
    {
        if (allExtinguished || indoorCeilingSmoke == null || !enableProgressiveSmoke) return;

        // Akumulasi asap meningkat seiring berjalannya waktu kebakaran
        smokeBurningTime += Time.deltaTime;
        float progress = Mathf.Clamp01(smokeBurningTime / Mathf.Max(timeToMaxSmoke, 1f));

        var em = indoorCeilingSmoke.emission;
        em.rateOverTime = Mathf.Lerp(minSmokeEmission, maxSmokeEmission, progress);
    }

    /// <summary>Dipanggil oleh FireExtinguisherTarget saat 1 api padam</summary>
    public void OnFireExtinguished(FireExtinguisherTarget fire)
    {
        if (allExtinguished) return;

        activeFireCount--;
        Debug.Log("[FireManager] 1 Api Padam! Sisa api: " + activeFireCount);

        if (activeFireCount <= 0)
        {
            allExtinguished = true;
            OnAllFiresExtinguished();
        }
    }

    private void OnAllFiresExtinguished()
    {
        Debug.Log("🎉 SEMUA API BERHASIL DIPADAMKAN! SIMULASI VR SUKSES!");

        // Matikan alarm kebakaran
        var alarm = FindFirstObjectByType<FireAlarmSystem>();
        if (alarm != null) alarm.StopAlarm();

        // ── Matikan Suara Teriakan Karakter ──────────────────
        foreach (AudioSource charAudio in characterAudioSources)
        {
            if (charAudio != null && charAudio.isPlaying)
            {
                charAudio.Stop();
            }
        }

        // Play victory sound
        if (audioSource != null && victoryAudioClip != null)
        {
            audioSource.PlayOneShot(victoryAudioClip);
        }

        if (victorySmokeEffect != null)
        {
            victorySmokeEffect.Play();
        }

        if (indoorCeilingSmoke != null)
        {
            indoorCeilingSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // Tampilkan Kotak Hasil & Grade Penilaian di UI
        if (VRSimulationUIManager.Instance != null)
        {
            VRSimulationUIManager.Instance.OnMissionCompleted(VRSimulationUIManager.Instance.missionTimer);
        }
    }
}
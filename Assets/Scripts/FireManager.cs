using System.Collections.Generic;
using UnityEngine;

public class FireManager : MonoBehaviour
{
    public static FireManager Instance { get; private set; }

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

    [Header("Akumulasi Asap Plafon (Hanya Berjalan Setelah MCB Mati)")]
    [Tooltip("Apakah asap bertambah tebal dan banyak seiring berjalannya waktu kebakaran?")]
    public bool enableProgressiveSmoke = true;
    [Tooltip("Waktu dalam detik untuk mencapai ketebalan asap maksimal")]
    public float timeToMaxSmoke = 35f;
    [Tooltip("Jumlah emisi asap awal (detik pertama)")]
    public float minSmokeEmission = 2f;
    [Tooltip("Jumlah emisi asap puncak (saat kebakaran lama)")]
    public float maxSmokeEmission = 55f;

    private int activeFireCount = 0;
    private AudioSource audioSource;
    private bool allExtinguished = false;

    // ── Status Asap ──
    private bool isSmokeAccumulationActive = false;
    private float smokeBurningTime = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(this); return; }

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

        if (indoorCeilingSmoke != null)
        {
            var main = indoorCeilingSmoke.main;
            main.prewarm = false;

            var em = indoorCeilingSmoke.emission;
            em.rateOverTime = minSmokeEmission;
            indoorCeilingSmoke.Play();
        }
    }

    /// <summary>
    /// Dipanggil saat MCB dimatikan -> Asap mulai dihitung dan menebal seiring waktu!
    /// </summary>
    public void StartSmokeAccumulation()
    {
        isSmokeAccumulationActive = true;
        smokeBurningTime = 0f;
        Debug.Log("[FireManager] 💨 MCB Off -> Akumulasi asap dan hitungan bahaya asap resmi DIMULAI!");
    }

    private void Update()
    {
        if (allExtinguished || indoorCeilingSmoke == null || !enableProgressiveSmoke) return;

        // Asap HANYA menebal jika MCB sudah dimatikan
        if (isSmokeAccumulationActive)
        {
            smokeBurningTime += Time.deltaTime;

            float progress = Mathf.Clamp01(smokeBurningTime / Mathf.Max(timeToMaxSmoke, 1f));
            var em = indoorCeilingSmoke.emission;
            em.rateOverTime = Mathf.Lerp(minSmokeEmission, maxSmokeEmission, progress);
        }
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

        // ── Matikan Suara Teriakan Karakter & Bunyikan Kemenangan (SEMUA karakter) ──
        foreach (CharacterVoiceController voice in CharacterVoiceController.All)
        {
            if (voice != null)
                voice.PlayVictoryVoice();
        }

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
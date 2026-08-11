using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pengelola banyak titik api (Multiple Fires Manager).
/// Memantau semua titik api di dapur (kompor, meja, tabung gas, dinding).
/// Menampilkan notifikasi / suara sukses ketika SEMUA api berhasil dipadamkan!
/// </summary>
public class FireManager : MonoBehaviour
{
    [Header("Daftar Api")]
    public List<FireExtinguisherTarget> fireTargets = new List<FireExtinguisherTarget>();

    [Header("Efek Sukses Kebakaran Padam")]
    public AudioClip victoryAudioClip;
    public ParticleSystem victorySmokeEffect;

    private int activeFireCount = 0;
    private AudioSource audioSource;
    private bool allExtinguished = false;

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

        // Play victory sound
        if (audioSource != null && victoryAudioClip != null)
        {
            audioSource.PlayOneShot(victoryAudioClip);
        }

        if (victorySmokeEffect != null)
        {
            victorySmokeEffect.Play();
        }
    }
}

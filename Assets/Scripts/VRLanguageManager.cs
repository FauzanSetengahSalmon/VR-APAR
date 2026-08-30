using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum AppLanguage
{
    Indonesian,
    English
}

/// <summary>
/// Pusat pengaturan bahasa (ID / EN) untuk simulasi VR APAR.
/// Mengatur pergantian UI Awal Game, UI Panduan, dan UI Edukasi Jenis APAR.
/// </summary>
public class VRLanguageManager : MonoBehaviour
{
    public static VRLanguageManager Instance { get; private set; }

    [Header("Pengaturan Bahasa")]
    [Tooltip("Bahasa yang sedang aktif saat ini")]
    public AppLanguage currentLanguage = AppLanguage.Indonesian;

    [Header("Referensi UI Landing")]
    [Tooltip("GameObject UI Awal Game versi Indonesia")]
    public GameObject uiAwalGameIndonesia;

    [Tooltip("GameObject UI Awal Game versi Inggris")]
    public GameObject uiAwalGameInggris;

    [Header("Referensi UI Edukasi Jenis APAR (Opsional di Lobby/Menu)")]
    [Tooltip("GameObject UI Jenis-Jenis APAR versi Indonesia")]
    public GameObject uiJenisAPARIndonesia;

    [Tooltip("GameObject UI Jenis-Jenis APAR versi Inggris")]
    public GameObject uiJenisAPARInggris;

    [Header("Referensi UI Panduan APAR (Hanya tampil setelah MCB dimatikan)")]
    [Tooltip("GameObject UI Panduan APAR versi Indonesia")]
    public GameObject uiPanduanIndonesia;

    [Tooltip("GameObject UI Panduan APAR versi Inggris")]
    public GameObject uiPanduanInggris;

    [Header("Status Panduan")]
    [Tooltip("Apakah panduan APAR sudah diizinkan tampil (setelah MCB OFF)")]
    public bool isGuideAllowedToShow = false;

    [Header("Event Ketika Bahasa Berubah")]
    public UnityEvent<AppLanguage> onLanguageChangedUnityEvent;
    public static event Action<AppLanguage> OnLanguageChanged;

    public static AppLanguage CurrentLanguage
    {
        get => Instance != null ? Instance.currentLanguage : AppLanguage.Indonesian;
    }

    public static bool IsEnglish => CurrentLanguage == AppLanguage.English;
    public static bool IsIndonesian => CurrentLanguage == AppLanguage.Indonesian;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        AutoFindUIReferences();
    }

    private void Start()
    {
        isGuideAllowedToShow = false;
        AutoFindUIReferences();
        ApplyLanguage(currentLanguage, false);
    }

    public void AutoFindUIReferences()
    {
        if (uiAwalGameIndonesia == null)
            uiAwalGameIndonesia = FindGameObjectIncludingInactive("UI Awal Game Indonesia") ?? FindGameObjectIncludingInactive("UI LANDING PAGE");

        if (uiAwalGameInggris == null)
            uiAwalGameInggris = FindGameObjectIncludingInactive("UI Awal Game Inggris") ?? FindGameObjectIncludingInactive("UI LANDING PAGE EN") ?? FindGameObjectIncludingInactive("UI LANDING PAGE _ EN");

        if (uiJenisAPARIndonesia == null)
            uiJenisAPARIndonesia = FindGameObjectIncludingInactive("UI Jenis APAR Indonesia") 
                                ?? FindGameObjectIncludingInactive("UI JENIS APAR")
                                ?? FindGameObjectIncludingInactive("UI Jenis APAR");

        if (uiJenisAPARInggris == null)
            uiJenisAPARInggris = FindGameObjectIncludingInactive("UI Jenis APAR Inggris") 
                              ?? FindGameObjectIncludingInactive("UI JENIS APAR EN") 
                              ?? FindGameObjectIncludingInactive("UI JENIS APAR _ EN")
                              ?? FindGameObjectIncludingInactive("UI Jenis APAR English");

        if (uiPanduanIndonesia == null)
            uiPanduanIndonesia = FindGameObjectIncludingInactive("UI Panduan Indonesia") ?? FindGameObjectIncludingInactive("UI PANDUAN");

        if (uiPanduanInggris == null)
            uiPanduanInggris = FindGameObjectIncludingInactive("UI Panduan Inggris") ?? FindGameObjectIncludingInactive("UI PANDUAN EN") ?? FindGameObjectIncludingInactive("UI PANDUAN _ EN");

        // Pastikan tidak ada objek UI yang saling tertanam
        if (uiAwalGameIndonesia != null && uiAwalGameInggris != null)
        {
            if (uiAwalGameIndonesia.transform.IsChildOf(uiAwalGameInggris.transform))
                uiAwalGameIndonesia.transform.SetParent(null);
            if (uiAwalGameInggris.transform.IsChildOf(uiAwalGameIndonesia.transform))
                uiAwalGameInggris.transform.SetParent(null);
        }

        SyncTransformsAndComponents();
        SetupEnglishHoldButton();
        SetupToggleColliders();
    }

    /// <summary>
    /// Menyamakan posisi, rotasi, skala, dan VRBillboardUI antara versi Indonesia dan Inggris
    /// </summary>
    private void SyncTransformsAndComponents()
    {
        // 1. Sync Landing Page / Awal Game
        if (uiAwalGameIndonesia != null && uiAwalGameInggris != null)
        {
            uiAwalGameInggris.transform.position = uiAwalGameIndonesia.transform.position;
            uiAwalGameInggris.transform.rotation = uiAwalGameIndonesia.transform.rotation;
            uiAwalGameInggris.transform.localScale = uiAwalGameIndonesia.transform.localScale;

            var indoBillboard = uiAwalGameIndonesia.GetComponent<VRBillboardUI>();
            if (indoBillboard != null)
            {
                var engBillboard = uiAwalGameInggris.GetComponent<VRBillboardUI>();
                if (engBillboard == null)
                    engBillboard = uiAwalGameInggris.AddComponent<VRBillboardUI>();

                engBillboard.distance = indoBillboard.distance;
                engBillboard.minDistance = indoBillboard.minDistance;
                engBillboard.heightOffset = indoBillboard.heightOffset;
                engBillboard.smoothSpeed = indoBillboard.smoothSpeed;
            }
        }

        // 2. Sync UI Jenis-Jenis APAR
        if (uiJenisAPARIndonesia != null && uiJenisAPARInggris != null)
        {
            uiJenisAPARInggris.transform.position = uiJenisAPARIndonesia.transform.position;
            uiJenisAPARInggris.transform.rotation = uiJenisAPARIndonesia.transform.rotation;
            uiJenisAPARInggris.transform.localScale = uiJenisAPARIndonesia.transform.localScale;

            var indoBillboard = uiJenisAPARIndonesia.GetComponent<VRBillboardUI>();
            if (indoBillboard != null)
            {
                var engBillboard = uiJenisAPARInggris.GetComponent<VRBillboardUI>();
                if (engBillboard == null)
                    engBillboard = uiJenisAPARInggris.AddComponent<VRBillboardUI>();

                engBillboard.distance = indoBillboard.distance;
                engBillboard.minDistance = indoBillboard.minDistance;
                engBillboard.heightOffset = indoBillboard.heightOffset;
                engBillboard.smoothSpeed = indoBillboard.smoothSpeed;
            }
        }

        // 3. Sync UI Panduan (canvas/panel)
        if (uiPanduanIndonesia != null && uiPanduanInggris != null)
        {
            uiPanduanInggris.transform.position = uiPanduanIndonesia.transform.position;
            uiPanduanInggris.transform.rotation = uiPanduanIndonesia.transform.rotation;
            uiPanduanInggris.transform.localScale = uiPanduanIndonesia.transform.localScale;

            var indoBillboard = uiPanduanIndonesia.GetComponent<VRBillboardUI>();
            if (indoBillboard != null)
            {
                var engBillboard = uiPanduanInggris.GetComponent<VRBillboardUI>();
                if (engBillboard == null)
                    engBillboard = uiPanduanInggris.AddComponent<VRBillboardUI>();

                engBillboard.distance = indoBillboard.distance;
                engBillboard.minDistance = indoBillboard.minDistance;
                engBillboard.heightOffset = indoBillboard.heightOffset;
                engBillboard.smoothSpeed = indoBillboard.smoothSpeed;
            }
        }
    }

    /// <summary>
    /// Memastikan UI Awal Game versi Inggris memiliki tombol VRHoldButton yang sama persis dengan versi Indonesia
    /// </summary>
    private void SetupEnglishHoldButton()
    {
        if (uiAwalGameIndonesia == null || uiAwalGameInggris == null) return;

        VRHoldButton indoHold = uiAwalGameIndonesia.GetComponent<VRHoldButton>();
        BoxCollider indoCollider = uiAwalGameIndonesia.GetComponent<BoxCollider>();

        if (indoCollider != null)
        {
            BoxCollider engCollider = uiAwalGameInggris.GetComponent<BoxCollider>();
            if (engCollider == null)
                engCollider = uiAwalGameInggris.AddComponent<BoxCollider>();

            engCollider.center = indoCollider.center;
            engCollider.size = indoCollider.size;
            engCollider.isTrigger = indoCollider.isTrigger;
        }

        if (indoHold != null)
        {
            VRHoldButton engHold = uiAwalGameInggris.GetComponent<VRHoldButton>();
            if (engHold == null)
                engHold = uiAwalGameInggris.AddComponent<VRHoldButton>();

            engHold.holdDuration = indoHold.holdDuration;
            engHold.startFillColor = indoHold.startFillColor;
            engHold.endFillColor = indoHold.endFillColor;

            if (VRSimulationUIManager.Instance != null)
            {
                engHold.OnHoldComplete.RemoveListener(VRSimulationUIManager.Instance.StartLoadingFlow);
                engHold.OnHoldComplete.AddListener(VRSimulationUIManager.Instance.StartLoadingFlow);
            }
        }
    }

    /// <summary>
    /// Memastikan tombol toggle ID / EN pada kedua landing page memiliki collider & script VRLanguageToggle
    /// </summary>
    private void SetupToggleColliders()
    {
        SetupToggleOnObject(uiAwalGameIndonesia, "Toogle Indo");
        SetupToggleOnObject(uiAwalGameInggris, "Toogle Inggris");
    }

    private void SetupToggleOnObject(GameObject landingGO, string preferredName)
    {
        if (landingGO == null) return;

        // Hapus VRLanguageToggle dari root landing page jika tidak sengaja terpasang di root
        VRLanguageToggle rootToggle = landingGO.GetComponent<VRLanguageToggle>();
        if (rootToggle != null)
        {
            Destroy(rootToggle);
        }

        // Bersihkan child duplikat dari run sebelumnya
        for (int i = landingGO.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = landingGO.transform.GetChild(i);
            if (child.name.StartsWith("Auto_Language_Toggle"))
                Destroy(child.gameObject);
        }

        Transform toggleChild = landingGO.transform.Find(preferredName)
                             ?? landingGO.transform.Find("Toogle Indo")
                             ?? landingGO.transform.Find("Toogle Inggris")
                             ?? landingGO.transform.Find("Toggle EN_English")
                             ?? landingGO.transform.Find("Toggle")
                             ?? landingGO.transform.Find("Toogle");

        if (toggleChild != null)
        {
            BoxCollider bc = toggleChild.GetComponent<BoxCollider>();
            if (bc == null)
            {
                bc = toggleChild.gameObject.AddComponent<BoxCollider>();
                bc.size = new Vector3(1.2f, 0.6f, 0.4f);
            }

            VRLanguageToggle toggleScript = toggleChild.GetComponent<VRLanguageToggle>();
            if (toggleScript == null)
                toggleScript = toggleChild.gameObject.AddComponent<VRLanguageToggle>();
        }
    }

    public static GameObject FindGameObjectIncludingInactive(string name)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded) return null;

        var rootObjects = scene.GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            var match = FindChildRecursive(root.transform, name);
            if (match != null)
                return match.gameObject;
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            var result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null)
                return result;
        }
        return null;
    }

    public void SetIndonesian() => SetLanguage(AppLanguage.Indonesian);
    public void SetEnglish() => SetLanguage(AppLanguage.English);
    public void ToggleLanguage() => SetLanguage(currentLanguage == AppLanguage.Indonesian ? AppLanguage.English : AppLanguage.Indonesian);

    public void SetLanguage(AppLanguage newLanguage)
    {
        currentLanguage = newLanguage;
        ApplyLanguage(newLanguage, true);
    }

    private void ApplyLanguage(AppLanguage lang, bool notifyListeners)
    {
        AutoFindUIReferences();

        bool isId = (lang == AppLanguage.Indonesian);

        // Cek apakah sedang di fase StartLanding (Lobby / Awal Game)
        bool isStartLanding = VRSimulationUIManager.Instance == null || 
                              VRSimulationUIManager.Instance.currentPhase == VRSimulationUIManager.UIPhase.StartLanding;

        if (isStartLanding)
        {
            // Update visibilitas UI Awal Game
            if (uiAwalGameIndonesia != null) uiAwalGameIndonesia.SetActive(isId);
            if (uiAwalGameInggris != null) uiAwalGameInggris.SetActive(!isId);

            // Sembunyikan UI Panduan & UI Jenis APAR saat di Lobby/Landing
            if (uiJenisAPARIndonesia != null) uiJenisAPARIndonesia.SetActive(false);
            if (uiJenisAPARInggris != null) uiJenisAPARInggris.SetActive(false);
            if (uiPanduanIndonesia != null) uiPanduanIndonesia.SetActive(false);
            if (uiPanduanInggris != null) uiPanduanInggris.SetActive(false);
        }
        else
        {
            if (uiAwalGameIndonesia != null) uiAwalGameIndonesia.SetActive(false);
            if (uiAwalGameInggris != null) uiAwalGameInggris.SetActive(false);

            // Update visibilitas UI Panduan & UI Jenis APAR di dalam misi
            UpdateInGameUIVisibility();
        }

        Debug.Log($"[VRLanguageManager] 🌐 Bahasa diubah ke: {lang}");

        if (notifyListeners)
        {
            OnLanguageChanged?.Invoke(lang);
            onLanguageChangedUnityEvent?.Invoke(lang);
        }
    }

    /// <summary>
    /// Mengatur visibilitas UI Panduan dan UI Jenis-Jenis APAR di dalam misi.
    /// </summary>
    public void UpdateInGameUIVisibility()
    {
        if (!isGuideAllowedToShow)
        {
            if (uiPanduanIndonesia != null) uiPanduanIndonesia.SetActive(false);
            if (uiPanduanInggris != null) uiPanduanInggris.SetActive(false);
            if (uiJenisAPARIndonesia != null) uiJenisAPARIndonesia.SetActive(false);
            if (uiJenisAPARInggris != null) uiJenisAPARInggris.SetActive(false);
            return;
        }

        bool isId = (currentLanguage == AppLanguage.Indonesian);
        if (uiPanduanIndonesia != null) uiPanduanIndonesia.SetActive(isId);
        if (uiPanduanInggris != null) uiPanduanInggris.SetActive(!isId);
        if (uiJenisAPARIndonesia != null) uiJenisAPARIndonesia.SetActive(isId);
        if (uiJenisAPARInggris != null) uiJenisAPARInggris.SetActive(!isId);
    }

    /// <summary>
    /// Alias untuk backwards compatibility.
    /// </summary>
    public void UpdateGuidePanelVisibility()
    {
        UpdateInGameUIVisibility();
    }

    /// <summary>
    /// Dipanggil saat animasi Mulai selesai dan Misi Aktif dimulai -> UI Panduan & UI Jenis-Jenis APAR ditampilkan!
    /// </summary>
    public void ShowMissionInGameUI()
    {
        isGuideAllowedToShow = true;
        UpdateInGameUIVisibility();
        Debug.Log($"[VRLanguageManager] 📋 UI Panduan & UI Jenis-Jenis APAR diaktifkan dalam bahasa: {currentLanguage}");
    }

    /// <summary>
    /// Dipanggil saat Saklar MCB berhasil dimatikan atau saat Misi Aktif dimulai.
    /// </summary>
    public void AllowAndShowGuidePoster()
    {
        ShowMissionInGameUI();
    }

    /// <summary>
    /// Sembunyikan semua UI awal saat simulasi dimulai.
    /// </summary>
    public void HideAllStartUI()
    {
        AutoFindUIReferences();

        if (uiAwalGameIndonesia != null) uiAwalGameIndonesia.SetActive(false);
        if (uiAwalGameInggris != null) uiAwalGameInggris.SetActive(false);
        if (uiJenisAPARIndonesia != null) uiJenisAPARIndonesia.SetActive(false);
        if (uiJenisAPARInggris != null) uiJenisAPARInggris.SetActive(false);

        isGuideAllowedToShow = false;
        if (uiPanduanIndonesia != null) uiPanduanIndonesia.SetActive(false);
        if (uiPanduanInggris != null) uiPanduanInggris.SetActive(false);
    }

    /// <summary>
    /// Tampilkan kembali UI awal sesuai bahasa aktif (saat kembali ke lobby).
    /// </summary>
    public void ShowStartUI()
    {
        isGuideAllowedToShow = false;
        ApplyLanguage(currentLanguage, false);
    }

    public void HideAllGuidePosters()
    {
        isGuideAllowedToShow = false;
        if (uiPanduanIndonesia != null) uiPanduanIndonesia.SetActive(false);
        if (uiPanduanInggris != null) uiPanduanInggris.SetActive(false);
        if (uiJenisAPARIndonesia != null) uiJenisAPARIndonesia.SetActive(false);
        if (uiJenisAPARInggris != null) uiJenisAPARInggris.SetActive(false);
    }
}

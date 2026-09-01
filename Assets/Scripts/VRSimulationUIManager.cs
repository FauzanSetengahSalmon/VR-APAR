using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class VRSimulationUIManager : MonoBehaviour
{
    public static VRSimulationUIManager Instance { get; private set; }

    public enum UIPhase
    {
        StartLanding,
        Loading,
        EmergencyCall113,
        ActiveMission,
        VictoryGrade,
        GameOver
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 1. STATUS MISI RUNTIME
    // ═══════════════════════════════════════════════════════════════════════
    [Header("1. Status Misi Runtime")]
    public UIPhase currentPhase = UIPhase.StartLanding;
    public float missionTimer = 0f;
    public bool isTimerRunning = false;
    [Tooltip("Status apakah peringatan asap sedang aktif")]
    public bool isSmokeWarningActive = false;
    [Tooltip("Status apakah Game Over sedang aktif")]
    public bool isGameOver = false;

    // ═══════════════════════════════════════════════════════════════════════
    // 2. WAKTU PERINGATAN ASAP & GAME OVER
    // ═══════════════════════════════════════════════════════════════════════
    [Header("2. Waktu Peringatan Asap & Game Over (Detik)")]
    [Tooltip("Waktu (detik) sejak MCB dimatikan sebelum Peringatan 1 (Asap Mulai Menebal) muncul")]
    public float waktuPeringatanAsap1 = 35f;

    [Tooltip("Waktu (detik) sejak MCB dimatikan sebelum Peringatan 2 (Kondisi Kritis) muncul")]
    public float waktuPeringatanAsap2Kritis = 60f;

    [Tooltip("Jeda waktu (detik) setelah Peringatan Kritis sebelum Layar Game Over muncul")]
    public float jedaGameOverSetelahKritis = 5f;

    [Tooltip("Durasi Peringatan 1 tampil di layar (detik, default 20 detik agar tidak cepat hilang)")]
    public float durasiPeringatan1Tampil = 20f;

    [Tooltip("Skala ukuran banner Peringatan 1 & 2 di dunia VR. 1 = ukuran default. Naikkan (misal 1.3 - 1.6) supaya banner terasa lebih besar/dekat dan lebih mudah dibaca, tanpa mengubah jarak/ukuran panel lain (Loading, Telepon, Victory, Game Over).")]
    [Range(0.5f, 2.5f)]
    public float skalaBannerPeringatan = 1.4f;

    [Tooltip("Geser banner Peringatan 1 & 2 lebih dekat ke arah pemain (nilai positif = lebih dekat/lebih besar terasa di mata, tanpa memperbesar teks lain). Dalam satuan meter.")]
    public float majuKeArahPemainMeter = 0.35f;

    // ═══════════════════════════════════════════════════════════════════════
    // 3. SLOT GAMBAR UI DARI TIM UI/UX (KUSTOM PNG)
    // ═══════════════════════════════════════════════════════════════════════
    [Header("3. Slot Gambar UI Kustom dari UI/UX (PNG)")]
    [Tooltip("Banner Peringatan 1 versi Bahasa Indonesia (Rekomendasi: 1200 x 360 px)")]
    public Sprite uiWarning1_Indonesia;
    [Tooltip("Banner Peringatan 1 versi Bahasa Inggris (Rekomendasi: 1200 x 360 px)")]
    public Sprite uiWarning1_Inggris;

    [Tooltip("Banner Peringatan 2 / Kritis versi Bahasa Indonesia (Rekomendasi: 1200 x 360 px)")]
    public Sprite uiWarning2_Indonesia;
    [Tooltip("Banner Peringatan 2 / Kritis versi Bahasa Inggris (Rekomendasi: 1200 x 360 px)")]
    public Sprite uiWarning2_Inggris;

    [Tooltip("Kartu Game Over versi Bahasa Indonesia (Rekomendasi: 1080 x 1500 px)")]
    public Sprite uiGameOver_Indonesia;
    [Tooltip("Kartu Game Over versi Bahasa Inggris (Rekomendasi: 1080 x 1500 px)")]
    public Sprite uiGameOver_Inggris;

    // ═══════════════════════════════════════════════════════════════════════
    // 4. PENGATURAN GETARAN (HAPTIC FEEDBACK)
    // ═══════════════════════════════════════════════════════════════════════
    [Header("4. Pengaturan Getaran (Haptic Feedback)")]
    [Tooltip("Aktifkan getaran controller saat APAR menyemprot")]
    public bool enableExtinguisherHaptics = true;
    [Range(0f, 1f)] public float extinguisherHapticAmplitude = 0.3f;
    public float extinguisherHapticDuration = 0.08f;
    public float extinguisherHapticInterval = 0.08f;

    [Tooltip("Aktifkan getaran saat terlalu dekat dengan titik api")]
    public bool enableFireProximityHaptics = true;
    public float fireDangerDistance = 1.5f;
    public float fireCriticalDistance = 0.7f;
    [Range(0f, 1f)] public float fireHapticAmplitude = 0.2f;
    [Range(0f, 1f)] public float fireCriticalHapticAmplitude = 0.5f;
    public float fireHapticInterval = 0.3f;

    // ═══════════════════════════════════════════════════════════════════════
    // 5. PENGATURAN AUDIO UI (OPTIONAL)
    // ═══════════════════════════════════════════════════════════════════════
    [Header("5. Pengaturan Audio UI (Optional)")]
    public AudioClip loadingBeepClip;
    public AudioClip phoneRingingClip;
    public AudioClip phoneDispatchClip;
    public AudioClip victoryFanfareClip;

    [Header("Kustom Ikon HP Damkar 113")]
    [Tooltip("Ikon kontak avatar Damkar (center circle). Kosongkan = pakai simbol telepon default.")]
    public Sprite iconAvatar;

    [Tooltip("Ikon tombol Angkat Telepon (hijau). Kosongkan = pakai simbol ☎ default.")]
    public Sprite iconCallBtn;

    [Tooltip("Ikon tombol Mute (abu). Kosongkan = pakai simbol 🔇 default.")]
    public Sprite iconMuteBtn;

    [Tooltip("Ikon tombol Tutup/End (merah). Kosongkan = pakai simbol ✕ default.")]
    public Sprite iconEndBtn;

    [Tooltip("Wallpaper / background layar HP. Kosongkan = hitam solid.")]
    public Sprite phoneWallpaper;

    [Header("UI Skor Bintang (Indonesia)")]
    [Tooltip("Gambar UI Skor Bintang 1 (waktu lambat)")]
    public Sprite uiSkorBintang1;

    [Tooltip("Gambar UI Skor Bintang 2 (waktu sedang)")]
    public Sprite uiSkorBintang2;

    [Tooltip("Gambar UI Skor Bintang 3 (waktu cepat)")]
    public Sprite uiSkorBintang3;

    [Header("UI Skor Bintang (English)")]
    [Tooltip("Gambar UI Skor Bintang 1 versi Inggris (1.PNG di folder Assets/UIUX)")]
    public Sprite uiSkorBintang1_EN;

    [Tooltip("Gambar UI Skor Bintang 2 versi Inggris (2.PNG di folder Assets/UIUX)")]
    public Sprite uiSkorBintang2_EN;

    [Tooltip("Gambar UI Skor Bintang 3 versi Inggris (3.PNG di folder Assets/UIUX)")]
    public Sprite uiSkorBintang3_EN;

    [Tooltip("Batas waktu MAKSIMAL untuk Bintang 3")]
    public float maxTimeFor3Stars = 30f;

    [Tooltip("Batas waktu MAKSIMAL untuk Bintang 2")]
    public float maxTimeFor2Stars = 60f;

    [Header("Posisi Teks Timer di Kartu Victory (per Bahasa)")]
    [Tooltip("Posisi teks waktu (00:00) relatif terhadap tengah kartu, KHUSUS versi Indonesia. Geser X/Y di sini sampai pas dengan desain PNG Indonesia.")]
    public Vector2 posisiTimerIndonesia = new Vector2(0f, 54f);

    [Tooltip("Posisi teks waktu (00:00) relatif terhadap tengah kartu, KHUSUS versi Inggris. Geser X/Y di sini sampai pas dengan desain PNG Inggris.")]
    public Vector2 posisiTimerInggris = new Vector2(0f, 54f);

    [Header("Posisi Teks Waktu di Kartu Game Over (per Bahasa)")]
    [Tooltip("Posisi teks 'Outage Time' (MM:SS) relatif terhadap tengah kartu Game Over, versi Indonesia. Geser X/Y sampai pas dengan desain PNG.")]
    public Vector2 posisiWaktuGameOverIndonesia = new Vector2(0f, 140f);

    [Tooltip("Posisi teks 'Outage Time' (MM:SS) relatif terhadap tengah kartu Game Over, versi Inggris. Geser X/Y sampai pas dengan desain PNG.")]
    public Vector2 posisiWaktuGameOverInggris = new Vector2(0f, 140f);

    [Tooltip("Posisi tombol 'Try Again / Mulai Lagi' relatif terhadap tengah kartu Game Over. Ukuran & gaya disamakan dengan tombol kartu Victory (460x120).")]
    public Vector2 posisiTombolRetryGameOver = new Vector2(0f, -300f);

    [Tooltip("Posisi tombol 'Kembali ke Beranda/Lobby' relatif terhadap tengah kartu Game Over. Ukuran & gaya disamakan dengan tombol kartu Victory (460x120).")]
    public Vector2 posisiTombolLobbyGameOver = new Vector2(0f, -440f);

    // ── Referensi Scene & UI ──
    private GameObject originalLandingPageGO;
    private Canvas mainCanvas;
    private VRBillboardUI billboardScript;
    private AudioSource uiAudioSource;

    // ── Sub-Panels VR Canvas ──
    private GameObject loadingPanel;
    private GameObject phoneCallPanel;
    private GameObject victoryPanel;
    private GameObject gameOverPanel;

    // ── Internal Smoke & Warning State ──
    private bool isSmokeAccumulationStarted = false;
    private float smokeElapsedTime = 0f;
    private bool warning1Triggered = false;
    private bool warning2Triggered = false;

    // ── Elements Smoke Warning ──
    private CanvasGroup warn1CanvasGroup;
    private Image warn1BgImage;
    private CanvasGroup warn2CanvasGroup;
    private Image warn2BgImage;

    // ── Elements Loading ──
    private Image loadingProgressBar;
    private TextMeshProUGUI loadingTitleText;
    private TextMeshProUGUI loadingPercentText;
    private TextMeshProUGUI loadingStatusText;

    // ── Elements Smartphone 113 ──
    private RectTransform phoneContainerRT;
    private TextMeshProUGUI phoneDialText;
    private TextMeshProUGUI phoneStatusText;
    private TextMeshProUGUI phoneDispatchMessage;
    private Image phoneAvatarPulseHalo;
    private Image[] equalizerBars;
    private Image[] rippleRings;
    private Image[] dialPadButtons;

    // ── Slot Image Custom Icon ──
    private Image avatarCenterImage;
    private Image callIconImage;
    private Image muteIconImage;
    private Image endIconImage;
    private Image phoneScreenBgImage;

    private TextMeshProUGUI avatarCenterText;
    private TextMeshProUGUI callIconText;
    private TextMeshProUGUI muteIconText;
    private TextMeshProUGUI endIconText;

    // ── Elements Victory ──
    private Image victoryBgImage;
    private TextMeshProUGUI victoryTimeText;

    // ── Elements Game Over ──
    private Image gameOverBgImage;
    private TextMeshProUGUI gameOverTitleText;
    private TextMeshProUGUI gameOverDescText;
    private TextMeshProUGUI gameOverSurvivalTimeText;

    // ── Circle Sprite Cache ──
    private Sprite circleSprite;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        uiAudioSource = GetComponent<AudioSource>();

        if (uiAudioSource == null)
            uiAudioSource = gameObject.AddComponent<AudioSource>();

        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0f;

        circleSprite = CreateCircleSprite(128);

        // Auto-load English score sprites from Resources or UIUX if unassigned
        #if UNITY_EDITOR
        if (uiSkorBintang1_EN == null) uiSkorBintang1_EN = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIUX/1.PNG");
        if (uiSkorBintang2_EN == null) uiSkorBintang2_EN = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIUX/2.PNG");
        if (uiSkorBintang3_EN == null) uiSkorBintang3_EN = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIUX/3.PNG");
        #endif

        SetupWorldSpaceCanvas();
        BuildUIComponents();

        // Inisialisasi Sistem VR APAR:
        VRSceneEnvironmentColliders.GenerateEnvironmentColliders();
        
        if (FindFirstObjectByType<VRSimulationControlManager>() == null)
            gameObject.AddComponent<VRSimulationControlManager>();

        if (FindFirstObjectByType<VRFireProximityWarning>() == null)
            gameObject.AddComponent<VRFireProximityWarning>();

        if (FindFirstObjectByType<VROptimizer>() == null)
            gameObject.AddComponent<VROptimizer>();

        if (FindFirstObjectByType<VRLanguageManager>() == null)
            gameObject.AddComponent<VRLanguageManager>();
    }

    private void Start()
    {
        originalLandingPageGO = GameObject.Find("UI LANDING PAGE");

        VRHoldButton[] holdButtons = FindObjectsByType<VRHoldButton>(FindObjectsSortMode.None);
        foreach (var holdBtn in holdButtons)
        {
            if (holdBtn != null)
            {
                holdBtn.OnHoldComplete.RemoveListener(StartLoadingFlow);
                holdBtn.OnHoldComplete.AddListener(StartLoadingFlow);
            }
        }

        ApplyCustomIcons();

        LockAllAPAR();

        SetPhase(UIPhase.StartLanding);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MISSION LOCK SYSTEM
    // ═══════════════════════════════════════════════════════════════════════

    private void LockAllAPAR()
    {
        Debug.Log("[VRUIManager] 🔒 Semua interaksi APAR dikunci. Tunggu hold Mulai Misi.");
    }

    private void UnlockAllAPAR()
    {
        var propStateMachines =
            FindObjectsByType<APARPropStateMachine>(FindObjectsSortMode.None);

        foreach (var psm in propStateMachines)
            psm.SetMissionStarted();

        var pinIndicators =
            FindObjectsByType<APARPinIndicator>(FindObjectsSortMode.None);

        foreach (var pi in pinIndicators)
            pi.SetMissionStarted();

        var hoseGrabbers =
            FindObjectsByType<APARHoseGrabber>(FindObjectsSortMode.None);

        foreach (var hg in hoseGrabbers)
            hg.SetMissionStarted();

        var aparPins =
            FindObjectsByType<APARPin>(FindObjectsSortMode.None);

        foreach (var ap in aparPins)
            ap.SetMissionStarted();

        var extinguishers =
            FindObjectsByType<AutoFireExtinguisher>(FindObjectsSortMode.None);

        foreach (var ext in extinguishers)
            ext.SetMissionStarted();

        var pinGuides =
            FindObjectsByType<APARPinGuideAnimation>(FindObjectsSortMode.None);

        foreach (var pg in pinGuides)
            pg.SetMissionStarted();

        Debug.Log(
            $"[VRUIManager] ✅ APAR Unlocked: " +
            $"{propStateMachines.Length} StateMachine, " +
            $"{pinIndicators.Length} PinIndicator, " +
            $"{hoseGrabbers.Length} HoseGrabber, " +
            $"{aparPins.Length} APARPin, " +
            $"{extinguishers.Length} Extinguisher, " +
            $"{pinGuides.Length} PinGuide"
        );
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CUSTOM ICONS
    // ═══════════════════════════════════════════════════════════════════════

    private void ApplyCustomIcons()
    {
        if (avatarCenterImage != null)
        {
            bool useCustom = iconAvatar != null;

            avatarCenterImage.gameObject.SetActive(useCustom);
            avatarCenterImage.sprite = useCustom ? iconAvatar : circleSprite;
            avatarCenterImage.color =
                useCustom
                    ? Color.white
                    : new Color(0.8f, 0.08f, 0.08f, 0.7f);

            avatarCenterImage.preserveAspect = useCustom;

            if (avatarCenterText != null)
                avatarCenterText.gameObject.SetActive(!useCustom);
        }

        if (callIconImage != null)
        {
            bool useCustom = iconCallBtn != null;

            callIconImage.gameObject.SetActive(useCustom);
            callIconImage.sprite = useCustom ? iconCallBtn : null;

            if (callIconText != null)
                callIconText.gameObject.SetActive(!useCustom);
        }

        if (muteIconImage != null)
        {
            bool useCustom = iconMuteBtn != null;

            muteIconImage.gameObject.SetActive(useCustom);
            muteIconImage.sprite = useCustom ? iconMuteBtn : null;

            if (muteIconText != null)
                muteIconText.gameObject.SetActive(!useCustom);
        }

        if (endIconImage != null)
        {
            bool useCustom = iconEndBtn != null;

            endIconImage.gameObject.SetActive(useCustom);
            endIconImage.sprite = useCustom ? iconEndBtn : null;

            if (endIconText != null)
                endIconText.gameObject.SetActive(!useCustom);
        }

        if (phoneScreenBgImage != null && phoneWallpaper != null)
        {
            phoneScreenBgImage.sprite = phoneWallpaper;
            phoneScreenBgImage.color = Color.white;
            phoneScreenBgImage.type = Image.Type.Simple;
            phoneScreenBgImage.preserveAspect = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UPDATE
    // ═══════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (isTimerRunning)
        {
            missionTimer += Time.deltaTime;
        }

        if (currentPhase == UIPhase.EmergencyCall113)
        {
            AnimateSmartphoneVisuals();
        }

        // ── SISTEM PERINGATAN ASAP & GAME OVER RUNTIME ──
        if (currentPhase == UIPhase.ActiveMission && isSmokeAccumulationStarted && !isGameOver)
        {
            smokeElapsedTime += Time.deltaTime;

            // 1. Cek Warning 1 (Asap Mulai Menebal)
            if (smokeElapsedTime >= waktuPeringatanAsap1 && !warning1Triggered)
            {
                TriggerWarning1();
            }

            // 2. Cek Warning 2 (Kondisi Kritis Asap Tebal)
            if (smokeElapsedTime >= waktuPeringatanAsap2Kritis && !warning2Triggered)
            {
                TriggerWarning2();
            }

            // 3. Cek Game Over setelah Jeda Waktu Kritis
            if (warning2Triggered && smokeElapsedTime >= (waktuPeringatanAsap2Kritis + jedaGameOverSetelahKritis))
            {
                TriggerGameOver();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CIRCLE SPRITE
    // ═══════════════════════════════════════════════════════════════════════

    private Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false
        );

        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];

        float radius = resolution * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(
                    new Vector2(x + 0.5f, y + 0.5f),
                    center
                );

                float alpha = Mathf.Clamp01(
                    (radius - dist) / 1.5f
                );

                pixels[y * resolution + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f)
        );
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SMARTPHONE ANIMATION
    // ═══════════════════════════════════════════════════════════════════════

    private void AnimateSmartphoneVisuals()
    {
        float time = Time.time;

        if (phoneContainerRT != null)
        {
            float hoverY = Mathf.Sin(time * 2.2f) * 6f;
            float tiltZ = Mathf.Sin(time * 1.6f) * 1.2f;

            phoneContainerRT.anchoredPosition =
                new Vector2(0f, hoverY);

            phoneContainerRT.localRotation =
                Quaternion.Euler(0f, 0f, tiltZ);
        }

        if (phoneAvatarPulseHalo != null)
        {
            float pulse =
                1.0f + Mathf.Sin(time * 6f) * 0.12f;

            phoneAvatarPulseHalo.transform.localScale =
                Vector3.one * pulse;
        }

        if (equalizerBars != null)
        {
            for (int i = 0; i < equalizerBars.Length; i++)
            {
                if (equalizerBars[i] == null)
                    continue;

                float h =
                    Mathf.Abs(
                        Mathf.Sin(time * 11f + i * 1.4f)
                    ) * 0.82f + 0.18f;

                RectTransform barRT =
                    equalizerBars[i].GetComponent<RectTransform>();

                if (barRT != null)
                    barRT.sizeDelta =
                        new Vector2(7f, 30f * h);
            }
        }

        if (rippleRings != null)
        {
            for (int i = 0; i < rippleRings.Length; i++)
            {
                if (rippleRings[i] == null)
                    continue;

                float phase =
                    (time * 1.3f + i * 0.45f) % 1.0f;

                float scale =
                    Mathf.Lerp(0.75f, 1.7f, phase);

                float alpha =
                    Mathf.Lerp(0.55f, 0.0f, phase);

                rippleRings[i].transform.localScale =
                    Vector3.one * scale;

                rippleRings[i].color =
                    new Color(
                        1f,
                        0.22f,
                        0.22f,
                        alpha
                    );
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SIMULATION FLOW
    // ═══════════════════════════════════════════════════════════════════════

    public void StartLoadingFlow()
    {
        if (currentPhase != UIPhase.StartLanding)
            return;

        StartCoroutine(LoadingRoutine());
    }

    private IEnumerator LoadingRoutine()
    {
        if (billboardScript != null)
            billboardScript.SnapToFront();

        SetPhase(UIPhase.Loading);

        bool isEnglish = VRLanguageManager.IsEnglish;

        if (loadingTitleText != null)
            loadingTitleText.text = isEnglish ? "PREPARING MISSION" : "MENYIAPKAN MISI";

        if (loadingPanel != null)
            StartCoroutine(
                AnimatePopUpScale(loadingPanel.transform)
            );

        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsed / duration);

            if (loadingProgressBar != null)
            {
                loadingProgressBar.fillAmount = progress;

                float pulse =
                    (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;

                loadingProgressBar.color = Color.Lerp(
                    new Color(0.85f, 0.15f, 0.15f), // Merah Solid
                    new Color(1f, 0.35f, 0.2f),     // Merah-Oranye Terang
                    pulse
                );
            }

            if (loadingPercentText != null)
                loadingPercentText.text =
                    $"{Mathf.RoundToInt(progress * 100)}%";

            if (progress < 0.35f)
            {
                loadingStatusText.text =
                    isEnglish ? "Initializing APAR System..." : "Menginisialisasi Sistem APAR...";
            }
            else if (progress < 0.75f)
            {
                loadingStatusText.text =
                    isEnglish ? "Preparing Mission Scenario..." : "Menyiapkan Skenario Misi...";
            }
            else
            {
                loadingStatusText.text =
                    isEnglish ? "Connecting Emergency Line..." : "Menghubungkan Saluran Darurat...";
            }

            yield return null;
        }

        StartCoroutine(EmergencyCall113Routine());
    }

    private IEnumerator EmergencyCall113Routine()
    {
        SetPhase(UIPhase.EmergencyCall113);

        bool isEnglish = VRLanguageManager.IsEnglish;

        if (phoneContainerRT != null)
            StartCoroutine(
                AnimatePopUpScale(phoneContainerRT)
            );

        phoneDialText.text = "";
        phoneStatusText.text = isEnglish ? "Entering Number..." : "Masukkan Nomor...";
        phoneDispatchMessage.gameObject.SetActive(false);

        SetDialpadVisible(true);
        SetRingingVisible(false);

        int[] dialSequence = { 0, 0, 2 };
        string[] dialKeys = { "1", "1", "3" };

        for (int i = 0; i < dialKeys.Length; i++)
        {
            phoneDialText.text += dialKeys[i];

            if (uiAudioSource != null &&
                loadingBeepClip != null)
            {
                uiAudioSource.PlayOneShot(
                    loadingBeepClip,
                    0.6f
                );
            }

            if (dialPadButtons != null &&
                dialSequence[i] < dialPadButtons.Length)
            {
                StartCoroutine(
                    AnimateButtonPress(
                        dialPadButtons[dialSequence[i]]
                    )
                );
            }

            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.1f);

        phoneStatusText.text = isEnglish ? "Calling..." : "Memanggil...";

        if (dialPadButtons != null &&
            dialPadButtons.Length > 9)
        {
            StartCoroutine(
                AnimateButtonPress(
                    dialPadButtons[9]
                )
            );
        }

        yield return new WaitForSeconds(0.15f);

        SetDialpadVisible(false);
        SetRingingVisible(true);

        phoneStatusText.text =
            isEnglish ? "Calling Fire Dept 113..." : "Memanggil Damkar 113...";

        if (uiAudioSource != null &&
            phoneRingingClip != null)
        {
            uiAudioSource.clip = phoneRingingClip;
            uiAudioSource.loop = true;
            uiAudioSource.Play();
        }

        yield return new WaitForSeconds(0.6f);

        if (uiAudioSource != null &&
            uiAudioSource.isPlaying)
        {
            uiAudioSource.Stop();
        }

        phoneStatusText.text =
            isEnglish ? "Connected • Fire Dept 113" : "Terhubung • Damkar 113";

        phoneDispatchMessage.gameObject.SetActive(true);

        if (uiAudioSource != null &&
            phoneDispatchClip != null)
        {
            uiAudioSource.PlayOneShot(
                phoneDispatchClip
            );
        }

        string fullMessage = isEnglish
            ? "<color=#FF5722><b>FIRE REPORT RECEIVED!</b></color> Firefighters dispatched. Grab the fire extinguisher, pull the safety pin, aim nozzle at the base of fire, and squeeze!"
            : "<color=#FF5722><b>LAPORAN KEBAKARAN DITERIMA!</b></color> Unit Pemadam meluncur. Segera ambil APAR, cabut pin safety, arahkan corong ke pangkal api, dan semprot!";

        phoneDispatchMessage.text = "";

        string currentText = "";
        bool insideTag = false;

        for (int i = 0; i < fullMessage.Length; i++)
        {
            char c = fullMessage[i];

            if (c == '<')
                insideTag = true;

            currentText += c;

            if (c == '>')
                insideTag = false;

            if (!insideTag)
            {
                phoneDispatchMessage.text =
                    currentText;

                yield return new WaitForSeconds(0.01f);
            }
        }

        phoneDispatchMessage.text = fullMessage;

        yield return new WaitForSeconds(2.0f);

        StartActiveMission();
    }

    private void SetDialpadVisible(bool visible)
    {
        if (dialPadButtons == null)
            return;

        foreach (var btn in dialPadButtons)
        {
            if (btn != null)
                btn.transform.parent.gameObject.SetActive(
                    visible
                );
        }
    }

    private void SetRingingVisible(bool visible)
    {
        if (rippleRings != null)
        {
            foreach (var r in rippleRings)
            {
                if (r != null)
                    r.gameObject.SetActive(visible);
            }
        }

        if (equalizerBars != null)
        {
            foreach (var b in equalizerBars)
            {
                if (b != null)
                {
                    b.transform.parent.gameObject.SetActive(
                        visible
                    );
                }
            }
        }
    }

    private IEnumerator AnimateButtonPress(Image btnImg)
    {
        if (btnImg == null)
            yield break;

        Color origColor = btnImg.color;
        Color pressColor = Color.white;

        btnImg.color = pressColor;

        RectTransform rt =
            btnImg.GetComponent<RectTransform>();

        Vector3 origScale =
            rt != null
                ? rt.localScale
                : Vector3.one;

        if (rt != null)
            rt.localScale =
                Vector3.one * 0.85f;

        yield return new WaitForSeconds(0.08f);

        if (rt != null)
            rt.localScale = origScale;

        btnImg.color = origColor;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // START MISSION
    // ═══════════════════════════════════════════════════════════════════════

    public void StartActiveMission()
    {
        missionTimer = 0f;
        isTimerRunning = false; // Timer belum dihitung sebelum saklar MCB dimatikan

        SetPhase(UIPhase.ActiveMission);

        // Tampilkan UI Panduan dan UI Jenis-Jenis APAR setelah animasi mulai selesai
        if (VRLanguageManager.Instance != null)
        {
            VRLanguageManager.Instance.ShowMissionInGameUI();
        }

        var alarmSystem =
            FindFirstObjectByType<FireAlarmSystem>();

        if (alarmSystem != null)
            alarmSystem.StartAlarm();

        // 🔊 Suara panik NPC mulai LANGSUNG saat tombol "Mulai Misi" diklik (SEMUA karakter)
        foreach (CharacterVoiceController voice in CharacterVoiceController.All)
        {
            if (voice != null)
                voice.PlayPanicSound();
        }

        // ── Langkah 1: Aktifkan wajib matikan saklar MCB ──────────────────────
        var switchManager = FindFirstObjectByType<SwitchStepManager>();
        if (switchManager != null)
        {
            switchManager.ActivateSwitchStep();
            Debug.Log("[VRUIManager] Langkah 1 aktif: Pemain harus matikan saklar MCB dulu!");
        }
        else
        {
            // Jika tidak ada SwitchStepManager, langsung unlock dan mulai timer (fallback)
            UnlockAllAPAR();
            isTimerRunning = true;
        }

        Debug.Log(
            "[VRUIManager] Misi Pemadaman APAR Dimulai! (Waktu/timer akan dihitung setelah MCB dimatikan)"
        );
    }

    /// <summary>
    /// Dipanggil oleh SwitchStepManager setelah saklar MCB berhasil dimatikan.
    /// Di sini semua APAR di-unlock dan penghitungan waktu/timer misi resmi dimulai!
    /// </summary>
    public void OnSwitchStepCompleted()
    {
        UnlockAllAPAR();

        missionTimer = 0f;
        isTimerRunning = true; // ⏱️ Waktu misi resmi dihitung mulai dari saat MCB dimatikan!

        // 💨 Mulai akumulasi asap bertahap hanya setelah MCB dimatikan!
        isSmokeAccumulationStarted = true;
        smokeElapsedTime = 0f;
        warning1Triggered = false;
        warning2Triggered = false;

        if (VRLanguageManager.Instance != null)
            VRLanguageManager.Instance.ShowMissionInGameUI();

        if (FireManager.Instance != null)
            FireManager.Instance.StartSmokeAccumulation();

        Debug.Log("[VRUIManager] ⚡ Saklar MCB sudah dimatikan. Timer resmi & Akumulasi Asap DIMULAI! (Suara panik NPC sudah berjalan sejak Mulai Misi)");
    }

    /// <summary>
    /// Memulai penghitungan waktu misi pemadaman (fallback jika timer belum berjalan).
    /// </summary>
    public void StartMissionTimer()
    {
        if (!isTimerRunning && currentPhase == UIPhase.ActiveMission)
        {
            isTimerRunning = true;
            Debug.Log("[VRUIManager] ⏱️ Timer misi pemadaman resmi DIMULAI!");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SISTEM PERINGATAN ASAP & GAME OVER RUNTIME
    // ═══════════════════════════════════════════════════════════════════════

    public void TriggerWarning1()
    {
        if (warning1Triggered || isGameOver || currentPhase != UIPhase.ActiveMission) return;
        warning1Triggered = true;
        isSmokeWarningActive = true;

        bool isEng = VRLanguageManager.IsEnglish;
        Sprite customSprite = isEng ? uiWarning1_Inggris : uiWarning1_Indonesia;
        if (customSprite == null) customSprite = isEng ? uiWarning1_Indonesia : uiWarning1_Inggris;

        // Panel kosong -> tinggal isi sprite kamu sendiri di
        // uiWarning1_Indonesia / uiWarning1_Inggris (Inspector).
        if (warn1BgImage != null)
        {
            warn1BgImage.sprite = customSprite;
            warn1BgImage.color = Color.white;
        }

        if (warn1CanvasGroup != null)
        {
            // SetActive langsung, sama seperti Victory/GameOver panel. Tidak ada
            // lagi animasi fade yang bisa numpuk/konflik dan bikin kedat-kedut.
            warn1CanvasGroup.gameObject.SetActive(true);
        }

        Debug.Log("[VRUIManager] ⚠️ PERINGATAN 1: Asap mulai tebal.");
    }

    public void TriggerWarning2()
    {
        if (warning2Triggered || isGameOver || currentPhase != UIPhase.ActiveMission) return;
        warning2Triggered = true;
        isSmokeWarningActive = true;

        // Kondisi sudah lebih kritis -> Warning 1 langsung disembunyikan, gantian ke Warning 2
        if (warn1CanvasGroup != null)
        {
            warn1CanvasGroup.gameObject.SetActive(false);
        }

        bool isEng = VRLanguageManager.IsEnglish;
        Sprite customSprite = isEng ? uiWarning2_Inggris : uiWarning2_Indonesia;
        if (customSprite == null) customSprite = isEng ? uiWarning2_Indonesia : uiWarning2_Inggris;

        // Panel kosong -> tinggal isi sprite kamu sendiri di
        // uiWarning2_Indonesia / uiWarning2_Inggris (Inspector).
        if (warn2BgImage != null)
        {
            warn2BgImage.sprite = customSprite;
            warn2BgImage.color = Color.white;
        }

        if (warn2CanvasGroup != null)
        {
            warn2CanvasGroup.gameObject.SetActive(true);
        }

        Debug.Log("[VRUIManager] 🚨 PERINGATAN 2 / KRITIS: Asap sangat tebal! Hitungan Game Over dimulai.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GAME OVER SYSTEM (BERDASARKAN ASAP, BUKAN TIMER)
    // ═══════════════════════════════════════════════════════════════════════

    public void TriggerGameOver()
    {
        if (isGameOver || currentPhase == UIPhase.VictoryGrade) return;

        isGameOver = true;
        isTimerRunning = false;
        isSmokeWarningActive = false;

        SetPhase(UIPhase.GameOver);

        // Hentikan suara panik karakter & alarm (SEMUA karakter)
        foreach (CharacterVoiceController voice in CharacterVoiceController.All)
        {
            if (voice != null)
                voice.StopPanicSound();
        }

        // PENTING: pakai StopAlarmKeepRedLight(), BUKAN StopAlarm().
        // Saat Game Over, api/asap belum padam — jadi lampu harus tetap MERAH,
        // hanya suara sirenenya saja yang dihentikan. StopAlarm() (lampu jadi HIJAU)
        // hanya boleh dipanggil oleh FireManager saat semua api benar-benar padam.
        var alarm = FindFirstObjectByType<FireAlarmSystem>();
        if (alarm != null) alarm.StopAlarmKeepRedLight();

        // Reset & lock APAR
        AutoFireExtinguisher apar = FindFirstObjectByType<AutoFireExtinguisher>();
        if (apar != null) apar.ResetToInitialPosition();

        // Sembunyikan warning panels
        if (warn1CanvasGroup != null) warn1CanvasGroup.gameObject.SetActive(false);
        if (warn2CanvasGroup != null) warn2CanvasGroup.gameObject.SetActive(false);

        // Format waktu bertahan
        int minutes = Mathf.FloorToInt(missionTimer / 60f);
        int seconds = Mathf.FloorToInt(missionTimer % 60f);
        string timeStr = $"{minutes:00}:{seconds:00}";

        bool isEng = VRLanguageManager.IsEnglish;

        // Terapkan teks / sprite bilingual
        if (gameOverTitleText != null)
            gameOverTitleText.text = isEng ? "GAME OVER\nMISSION FAILED" : "GAME OVER\nMISI GAGAL";

        if (gameOverDescText != null)
            gameOverDescText.text = isEng 
                ? "The room was filled with toxic smoke.\nYou lost consciousness due to smoke inhalation.\nIn a real fire, safety is the top priority!" 
                : "Ruangan telah dipenuhi asap beracun.\nAnda kehilangan kesadaran akibat menghirup asap kebakaran.\nDalam kebakaran nyata, utamakan selalu keselamatan!";

        if (gameOverSurvivalTimeText != null)
        {
            gameOverSurvivalTimeText.text = timeStr;

            RectTransform survivalTimeRT = gameOverSurvivalTimeText.rectTransform;
            survivalTimeRT.anchoredPosition = isEng ? posisiWaktuGameOverInggris : posisiWaktuGameOverIndonesia;
        }

        Sprite customSprite = isEng ? uiGameOver_Inggris : uiGameOver_Indonesia;
        if (customSprite == null) customSprite = isEng ? uiGameOver_Indonesia : uiGameOver_Inggris;

        if (gameOverBgImage != null && customSprite != null)
        {
            gameOverBgImage.sprite = customSprite;
            gameOverBgImage.color = Color.white;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            StartCoroutine(AnimatePopUpScale(gameOverPanel.transform));
        }

        Debug.Log($"[VRUIManager] ☠️ GAME OVER: Paparan asap terlalu lama. Waktu bertahan: {timeStr}");
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration, float autoHideDelay = 0f)
    {
        if (cg == null) yield break;

        float startAlpha = cg.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;

        if (autoHideDelay > 0f)
        {
            yield return new WaitForSeconds(autoHideDelay);
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(targetAlpha, 0f, elapsed / duration);
                yield return null;
            }
            cg.alpha = 0f;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MISSION COMPLETE
    // ═══════════════════════════════════════════════════════════════════════

    public void OnMissionCompleted(float totalTime)
    {
        if (!isTimerRunning &&
            (currentPhase == UIPhase.VictoryGrade || currentPhase == UIPhase.GameOver))
            return;

        isTimerRunning = false;
        missionTimer = totalTime;

        ShowVictoryGradeBox(totalTime);
    }

    [ContextMenu("🔍 Preview Victory (Test Posisi Timer)")]
    private void PreviewVictoryForTesting()
    {
        // Klik kanan komponen ini di Inspector (saat Play Mode) -> pilih menu ini
        // untuk langsung lihat kartu Victory + posisi timer TANPA harus
        // menyelesaikan seluruh simulasi. Ubah posisiTimerIndonesia /
        // posisiTimerInggris, lalu jalankan preview ini lagi untuk cek hasilnya.
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[VRUIManager] Preview hanya bisa dijalankan saat Play Mode.");
            return;
        }
        ShowVictoryGradeBox(37.5f); // contoh waktu dummy 00:37
    }

    private void ShowVictoryGradeBox(float totalTime)
    {
        SetPhase(UIPhase.VictoryGrade);

        // Sembunyikan warning panels asap (api sudah padam)
        if (warn1CanvasGroup != null) warn1CanvasGroup.gameObject.SetActive(false);
        if (warn2CanvasGroup != null) warn2CanvasGroup.gameObject.SetActive(false);

        if (uiAudioSource != null &&
            victoryFanfareClip != null)
        {
            uiAudioSource.PlayOneShot(
                victoryFanfareClip
            );
        }

        // ─────────────────────────────────────────────
        // FORMAT WAKTU
        // ─────────────────────────────────────────────

        int minutes =
            Mathf.FloorToInt(totalTime / 60f);

        int seconds =
            Mathf.FloorToInt(totalTime % 60f);

        string timeStr =
            $"{minutes:00}:{seconds:00}";

        if (victoryTimeText != null)
            victoryTimeText.text = timeStr;

        // ─────────────────────────────────────────────
        // POSISI TIMER (BEDA UNTUK ID / EN KARENA DESAIN PNG BEDA)
        // ─────────────────────────────────────────────

        bool isEnglishForTimerPos = VRLanguageManager.IsEnglish;

        if (victoryTimeText != null)
        {
            RectTransform timerRT = victoryTimeText.rectTransform;
            timerRT.anchoredPosition = isEnglishForTimerPos ? posisiTimerInggris : posisiTimerIndonesia;
        }

        // ─────────────────────────────────────────────
        // PILIH BINTANG (BILINGUAL SUPPORT)
        // ─────────────────────────────────────────────

        bool isEnglish = VRLanguageManager.IsEnglish;
        Sprite chosenSprite;

        if (totalTime <= maxTimeFor3Stars)
        {
            chosenSprite = (isEnglish && uiSkorBintang3_EN != null) ? uiSkorBintang3_EN : uiSkorBintang3;
        }
        else if (totalTime <= maxTimeFor2Stars)
        {
            chosenSprite = (isEnglish && uiSkorBintang2_EN != null) ? uiSkorBintang2_EN : uiSkorBintang2;
        }
        else
        {
            chosenSprite = (isEnglish && uiSkorBintang1_EN != null) ? uiSkorBintang1_EN : uiSkorBintang1;
        }

        if (victoryBgImage != null &&
            chosenSprite != null)
        {
            victoryBgImage.sprite =
                chosenSprite;
        }

        // ─────────────────────────────────────────────
        // RESET APAR
        // ─────────────────────────────────────────────

        AutoFireExtinguisher apar =
            FindFirstObjectByType<AutoFireExtinguisher>();

        if (apar != null)
            apar.ResetToInitialPosition();

        if (victoryPanel != null)
        {
            StartCoroutine(
                AnimatePopUpScale(
                    victoryPanel.transform
                )
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCENE NAVIGATION
    // ═══════════════════════════════════════════════════════════════════════

    public void RestartSimulation()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void GoToLobby()
    {
        SceneManager.LoadScene(0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE
    // ═══════════════════════════════════════════════════════════════════════

    private void SetPhase(UIPhase newPhase)
    {
        currentPhase = newPhase;

        if (newPhase == UIPhase.StartLanding)
        {
            if (VRLanguageManager.Instance != null)
                VRLanguageManager.Instance.ShowStartUI();

            if (originalLandingPageGO != null)
                originalLandingPageGO.SetActive(true);
        }
        else
        {
            if (newPhase == UIPhase.Loading || newPhase == UIPhase.EmergencyCall113)
            {
                if (VRLanguageManager.Instance != null)
                    VRLanguageManager.Instance.HideAllStartUI();
            }

            if (originalLandingPageGO != null)
                originalLandingPageGO.SetActive(false);
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(
                newPhase == UIPhase.Loading
            );
        }

        if (phoneCallPanel != null)
        {
            phoneCallPanel.SetActive(
                newPhase == UIPhase.EmergencyCall113
            );
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(
                newPhase == UIPhase.VictoryGrade
            );
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(
                newPhase == UIPhase.GameOver
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // POPUP ANIMATION
    // ═══════════════════════════════════════════════════════════════════════

    private IEnumerator AnimatePopUpScale(Transform target)
    {
        target.localScale = Vector3.zero;

        float duration = 0.42f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            float scale =
                Mathf.Sin(
                    t * Mathf.PI * 0.5f
                );

            if (t > 0.7f)
            {
                scale =
                    1.0f +
                    Mathf.Sin(
                        (t - 0.7f) *
                        Mathf.PI /
                        0.3f
                    ) * 0.06f;
            }

            target.localScale =
                Vector3.one *
                Mathf.Clamp(
                    scale,
                    0f,
                    1.07f
                );

            yield return null;
        }

        target.localScale = Vector3.one;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WORLD SPACE CANVAS
    // ═══════════════════════════════════════════════════════════════════════

    private void SetupWorldSpaceCanvas()
    {
        GameObject canvasGO =
            new GameObject(
                "VR_Simulation_UI_Canvas"
            );

        canvasGO.transform.position =
            new Vector3(
                0f,
                1.6f,
                2.0f
            );

        mainCanvas =
            canvasGO.AddComponent<Canvas>();

        mainCanvas.renderMode =
            RenderMode.WorldSpace;

        mainCanvas.sortingOrder = 50;

        CanvasScaler scaler =
            canvasGO.AddComponent<CanvasScaler>();

        scaler.dynamicPixelsPerUnit = 180;

        canvasGO.AddComponent<GraphicRaycaster>();
        if (canvasGO.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() == null)
        {
            canvasGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
        }

        billboardScript =
            canvasGO.AddComponent<VRBillboardUI>();

        billboardScript.distance = 1.7f;
        billboardScript.minDistance = 0.5f;
        billboardScript.heightOffset = 0.05f;
        billboardScript.smoothSpeed = 8.0f;

        RectTransform canvasRT =
            canvasGO.GetComponent<RectTransform>();

        canvasRT.sizeDelta =
            new Vector2(1000f, 900f);

        canvasRT.localScale =
            Vector3.one * 0.0018f;
    }

    private void BuildUIComponents()
    {
        BuildLoadingPanel();
        BuildPhonePanel();
        BuildVictoryPanel();
        BuildGameOverPanel();
        BuildSmokeWarningPanels();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LOADING PANEL
    // ═══════════════════════════════════════════════════════════════════════

    private void BuildLoadingPanel()
    {
        // Panel Background Hitam Merah Gelap
        loadingPanel =
            CreateRoundedPanel(
                mainCanvas.gameObject,
                "LoadingPanel",
                new Vector2(650f, 312f),
                new Color(
                    0.07f,
                    0.03f,
                    0.03f,
                    0.96f
                ),
                Vector2.zero
            );

        loadingTitleText = CreateText(
            loadingPanel,
            "LoadingTitle",
            "MENYIAPKAN MISI",
            34,
            FontStyles.Bold,
            new Vector2(0f, 94f),
            new Color(1f, 0.35f, 0f) // #FF5722
        );

        // Garis Pembatas (DotLine) -> Merah Solid Transparan
        CreateRoundedPanel(
            loadingPanel,
            "DotLine",
            new Vector2(39f, 4f),
            new Color(
                0.85f,
                0.15f,
                0.15f,
                0.8f
            ),
            new Vector2(0f, 68f)
        );

        // Background Track Loading Bar -> Dark Slate / Charcoal
        GameObject barBg =
            CreateRoundedPanel(
                loadingPanel,
                "ProgressBG",
                new Vector2(546f, 23f),
                new Color(
                    0.16f,
                    0.16f,
                    0.18f,
                    1f
                ),
                new Vector2(0f, 18f)
            );

        GameObject barFill =
            new GameObject(
                "ProgressFill"
            );

        barFill.transform.SetParent(
            barBg.transform,
            false
        );

        loadingProgressBar =
            barFill.AddComponent<Image>();

        loadingProgressBar.color = new Color(0.85f, 0.15f, 0.15f);

        loadingProgressBar.type =
            Image.Type.Filled;

        loadingProgressBar.fillMethod =
            Image.FillMethod.Horizontal;

        RectTransform fillRT =
            barFill.GetComponent<RectTransform>();

        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;
        fillRT.anchoredPosition = Vector2.zero;

        // Text Persentase (0%) -> Putih Netral
        loadingPercentText =
            CreateText(
                loadingPanel,
                "PercentText",
                "0%",
                28,
                FontStyles.Bold,
                new Vector2(0f, -23f),
                Color.white
            );

        // Text Status ("Menginisialisasi...") -> Putih Netral
        loadingStatusText =
            CreateText(
                loadingPanel,
                "StatusText",
                "Menginisialisasi...",
                17,
                FontStyles.Italic,
                new Vector2(0f, -76f),
                Color.white
            );
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHONE PANEL
    // ═══════════════════════════════════════════════════════════════════════

    private void BuildPhonePanel()
    {
        phoneCallPanel =
            new GameObject(
                "PhoneCallContainer"
            );

        phoneCallPanel.transform.SetParent(
            mainCanvas.transform,
            false
        );

        phoneContainerRT =
            phoneCallPanel.AddComponent<RectTransform>();

        phoneContainerRT.anchorMin =
            new Vector2(0.5f, 0.5f);

        phoneContainerRT.anchorMax =
            new Vector2(0.5f, 0.5f);

        phoneContainerRT.pivot =
            new Vector2(0.5f, 0.5f);

        phoneContainerRT.anchoredPosition =
            Vector2.zero;

        phoneContainerRT.sizeDelta =
            new Vector2(420f, 780f);

        phoneContainerRT.localScale =
            Vector3.one * 1.20f;

        CreateRoundedPanel(
            phoneCallPanel,
            "OuterShadow",
            new Vector2(422f, 782f),
            new Color(
                0f,
                0f,
                0f,
                0.7f
            ),
            new Vector2(3f, -4f)
        );

        GameObject phoneChassis =
            CreateRoundedPanel(
                phoneCallPanel,
                "PhoneChassis",
                new Vector2(415f, 776f),
                new Color(
                    0.10f,
                    0.10f,
                    0.12f,
                    1f
                ),
                Vector2.zero
            );

        CreateRoundedPanel(
            phoneChassis,
            "TitaniumRing",
            new Vector2(408f, 769f),
            new Color(
                0.18f,
                0.18f,
                0.22f,
                1f
            ),
            Vector2.zero
        );

        CreateRoundedPanel(
            phoneChassis,
            "VolUp",
            new Vector2(4f, 38f),
            new Color(
                0.16f,
                0.16f,
                0.20f,
                1f
            ),
            new Vector2(-159f, 105f)
        );

        CreateRoundedPanel(
            phoneChassis,
            "VolDown",
            new Vector2(4f, 38f),
            new Color(
                0.16f,
                0.16f,
                0.20f,
                1f
            ),
            new Vector2(-159f, 56f)
        );

        CreateRoundedPanel(
            phoneChassis,
            "Silent",
            new Vector2(4f, 22f),
            new Color(
                0.16f,
                0.16f,
                0.20f,
                1f
            ),
            new Vector2(-159f, 155f)
        );

        CreateRoundedPanel(
            phoneChassis,
            "PowerBtn",
            new Vector2(4f, 52f),
            new Color(
                0.16f,
                0.16f,
                0.20f,
                1f
            ),
            new Vector2(159f, 85f)
        );

        GameObject phoneScreen =
            CreateRoundedPanel(
                phoneChassis,
                "PhoneScreen",
                new Vector2(398f, 758f),
                new Color(
                    0.04f,
                    0.04f,
                    0.06f,
                    1f
                ),
                Vector2.zero
            );

        CreateRoundedPanel(
            phoneScreen,
            "Glare",
            new Vector2(180f, 4f),
            new Color(
                1f,
                1f,
                1f,
                0.06f
            ),
            new Vector2(0f, 265f)
        );

        GameObject island =
            CreateRoundedPanel(
                phoneScreen,
                "DynamicIsland",
                new Vector2(115f, 28f),
                new Color(
                    0.01f,
                    0.01f,
                    0.01f,
                    1f
                ),
                new Vector2(0f, 343f)
            );

        CreateCircleImage(
            island,
            "CamDot",
            9f,
            new Color(
                0.08f,
                0.12f,
                0.25f,
                1f
            ),
            new Vector2(39f, 0f)
        );

        CreateText(
            phoneScreen,
            "SBTime",
            "9:41",
            12,
            FontStyles.Bold,
            new Vector2(-150f, 341f),
            Color.white
        );

        CreateText(
            phoneScreen,
            "SBIcons",
            "5G  ▐▌ 100%",
            11,
            FontStyles.Normal,
            new Vector2(130f, 341f),
            Color.white
        );

        GameObject callStateArea =
            new GameObject(
                "CallStateArea"
            );

        callStateArea.transform.SetParent(
            phoneScreen.transform,
            false
        );

        RectTransform callAreaRT =
            callStateArea.AddComponent<RectTransform>();

        callAreaRT.anchorMin =
            new Vector2(0.5f, 0.5f);

        callAreaRT.anchorMax =
            new Vector2(0.5f, 0.5f);

        callAreaRT.pivot =
            new Vector2(0.5f, 0.5f);

        callAreaRT.anchoredPosition =
            new Vector2(0f, 175f);

        callAreaRT.sizeDelta =
            new Vector2(380f, 340f);

        rippleRings = new Image[4];

        for (int r = 0; r < 4; r++)
        {
            float size = 90f + r * 24f;

            GameObject ringGO =
                CreateCircleImageGO(
                    callStateArea,
                    $"Ring_{r}",
                    size,
                    new Color(
                        1f,
                        0.22f,
                        0.22f,
                        0.35f
                    )
                );

            rippleRings[r] =
                ringGO.GetComponent<Image>();

            ringGO.GetComponent<RectTransform>()
                .anchoredPosition =
                new Vector2(0f, 78f);
        }

        GameObject haloGO =
            CreateCircleImageGO(
                callStateArea,
                "AvatarHalo",
                112f,
                new Color(
                    0.9f,
                    0.15f,
                    0.15f,
                    0.5f
                )
            );

        phoneAvatarPulseHalo =
            haloGO.GetComponent<Image>();

        haloGO.GetComponent<RectTransform>()
            .anchoredPosition =
            new Vector2(0f, 78f);

        GameObject avatarBg =
            CreateCircleImageGO(
                callStateArea,
                "AvatarBG",
                94f,
                new Color(
                    0.15f,
                    0.15f,
                    0.22f,
                    1f
                )
            );

        avatarBg.GetComponent<RectTransform>()
            .anchoredPosition =
            new Vector2(0f, 78f);

        GameObject avatarRed =
            CreateCircleImageGO(
                callStateArea,
                "AvatarRed",
                88f,
                new Color(
                    0.8f,
                    0.08f,
                    0.08f,
                    0.7f
                )
            );

        avatarRed.GetComponent<RectTransform>()
            .anchoredPosition =
            new Vector2(0f, 78f);

        avatarRed.transform.SetSiblingIndex(
            avatarBg.transform.GetSiblingIndex()
        );

        avatarCenterImage =
            avatarRed.GetComponent<Image>();

        avatarCenterText =
            CreateText(
                avatarBg,
                "PhoneIcon",
                "☎",
                40,
                FontStyles.Bold,
                new Vector2(0f, 2f),
                Color.white
            );

        GameObject avatarCustomImg =
            CreateCircleImageGO(
                avatarBg,
                "AvatarCustomIcon",
                78f,
                Color.white
            );

        avatarCustomImg.GetComponent<RectTransform>()
            .anchoredPosition =
            Vector2.zero;

        avatarCenterImage =
            avatarCustomImg.GetComponent<Image>();

        avatarCenterImage.sprite =
            circleSprite;

        avatarCenterImage.color =
            new Color(
                0.8f,
                0.08f,
                0.08f,
                0.7f
            );

        avatarCenterImage.gameObject.SetActive(false);

        CreateText(
            callStateArea,
            "ContactName",
            "DAMKAR DARURAT",
            20,
            FontStyles.Bold,
            new Vector2(0f, 13f),
            Color.white
        );

        CreateText(
            callStateArea,
            "ContactSub",
            "Dinas Pemadam Kebakaran",
            12,
            FontStyles.Normal,
            new Vector2(0f, -10f),
            new Color(
                0.65f,
                0.7f,
                0.8f
            )
        );

        phoneDialText =
            CreateText(
                callStateArea,
                "DialText",
                "",
                36,
                FontStyles.Bold,
                new Vector2(0f, -39f),
                new Color(
                    1f,
                    0.88f,
                    0.2f
                )
            );

        phoneDialText.characterSpacing = 10f;

        phoneStatusText =
            CreateText(
                callStateArea,
                "PhoneStatus",
                "Masukkan Nomor...",
                13,
                FontStyles.Normal,
                new Vector2(0f, -75f),
                new Color(
                    0.7f,
                    0.75f,
                    0.9f
                )
            );

        GameObject eqContainer =
            new GameObject(
                "EQContainer"
            );

        eqContainer.transform.SetParent(
            callStateArea.transform,
            false
        );

        RectTransform eqRT =
            eqContainer.AddComponent<RectTransform>();

        eqRT.anchorMin =
            new Vector2(0.5f, 0.5f);

        eqRT.anchorMax =
            new Vector2(0.5f, 0.5f);

        eqRT.pivot =
            new Vector2(0.5f, 0.5f);

        eqRT.anchoredPosition =
            new Vector2(0f, -104f);

        eqRT.sizeDelta =
            new Vector2(130f, 52f);

        equalizerBars = new Image[7];

        for (int i = 0; i < 7; i++)
        {
            float px = -55f + i * 18f;

            GameObject barGO =
                CreateRoundedPanel(
                    eqContainer,
                    $"EQ_{i}",
                    new Vector2(9f, 31f),
                    new Color(
                        1f,
                        0.32f,
                        0.18f,
                        0.95f
                    ),
                    new Vector2(px, 0f)
                );

            equalizerBars[i] =
                barGO.GetComponent<Image>();
        }

        GameObject msgBox =
            CreateRoundedPanel(
                phoneScreen,
                "MsgBox",
                new Vector2(360f, 124f),
                new Color(
                    0.09f,
                    0.11f,
                    0.18f,
                    0.97f
                ),
                new Vector2(0f, -124f)
            );

        phoneDispatchMessage =
            CreateText(
                msgBox,
                "DispatchMsg",
                "",
                13,
                FontStyles.Normal,
                Vector2.zero,
                Color.white
            );

        phoneDispatchMessage.alignment =
            TextAlignmentOptions.Center;

        RectTransform msgRT =
            phoneDispatchMessage.GetComponent<RectTransform>();

        msgRT.sizeDelta =
            new Vector2(
                345f,
                117f
            );

        msgBox.SetActive(false);

        string[] padLabels =
        {
            "1", "2", "3",
            "4", "5", "6",
            "7", "8", "9",
            "*", "0", "#"
        };

        dialPadButtons =
            new Image[
                padLabels.Length + 1
            ];

        GameObject dialpadGrid =
            new GameObject(
                "DialpadGrid"
            );

        dialpadGrid.transform.SetParent(
            phoneScreen.transform,
            false
        );

        RectTransform gridRT =
            dialpadGrid.AddComponent<RectTransform>();

        gridRT.anchorMin =
            new Vector2(0.5f, 0.5f);

        gridRT.anchorMax =
            new Vector2(0.5f, 0.5f);

        gridRT.pivot =
            new Vector2(0.5f, 0.5f);

        gridRT.anchoredPosition =
            new Vector2(0f, -78f);

        gridRT.sizeDelta =
            new Vector2(288f, 260f);

        for (int i = 0; i < padLabels.Length; i++)
        {
            int col = i % 3;
            int row = i / 3;

            float px =
                -91f + col * 91f;

            float py =
                98f - row * 65f;

            GameObject btnContainer =
                new GameObject(
                    $"PadKey_{padLabels[i]}"
                );

            btnContainer.transform.SetParent(
                dialpadGrid.transform,
                false
            );

            RectTransform btnContRT =
                btnContainer.AddComponent<RectTransform>();

            btnContRT.anchorMin =
                new Vector2(0.5f, 0.5f);

            btnContRT.anchorMax =
                new Vector2(0.5f, 0.5f);

            btnContRT.pivot =
                new Vector2(0.5f, 0.5f);

            btnContRT.anchoredPosition =
                new Vector2(px, py);

            btnContRT.sizeDelta =
                new Vector2(59f, 59f);

            GameObject padBg =
                CreateCircleImageGO(
                    btnContainer,
                    "PadBG",
                    52f,
                    new Color(
                        0.18f,
                        0.2f,
                        0.28f,
                        0.9f
                    )
                );

            padBg.GetComponent<RectTransform>()
                .anchoredPosition =
                Vector2.zero;

            dialPadButtons[i] =
                padBg.GetComponent<Image>();

            CreateText(
                btnContainer,
                "KeyLabel",
                padLabels[i],
                20,
                FontStyles.Bold,
                Vector2.zero,
                Color.white
            );
        }

        float bottomRowY = -260f;

        // MUTE
        GameObject muteBtnContainer =
            new GameObject(
                "MuteBtn"
            );

        muteBtnContainer.transform.SetParent(
            phoneScreen.transform,
            false
        );

        RectTransform muteContRT =
            muteBtnContainer.AddComponent<RectTransform>();

        muteContRT.anchorMin =
            new Vector2(0.5f, 0.5f);

        muteContRT.anchorMax =
            new Vector2(0.5f, 0.5f);

        muteContRT.pivot =
            new Vector2(0.5f, 0.5f);

        muteContRT.anchoredPosition =
            new Vector2(-98f, bottomRowY);

        muteContRT.sizeDelta =
            new Vector2(60f, 60f);

        GameObject muteCircle =
            CreateCircleImageGO(
                muteBtnContainer,
                "MuteCircle",
                57f,
                new Color(
                    0.22f,
                    0.24f,
                    0.33f,
                    1f
                )
            );

        muteCircle.GetComponent<RectTransform>()
            .anchoredPosition =
            Vector2.zero;

        muteIconText =
            CreateText(
                muteBtnContainer,
                "MuteIconText",
                "M",
                20,
                FontStyles.Bold,
                new Vector2(0f, 1f),
                Color.white
            );

        GameObject muteIconGO =
            CreateCircleImageGO(
                muteBtnContainer,
                "MuteIconImg",
                31f,
                Color.white
            );

        muteIconGO.GetComponent<RectTransform>()
            .anchoredPosition =
            new Vector2(0f, 1f);

        muteIconImage =
            muteIconGO.GetComponent<Image>();

        muteIconImage.preserveAspect = true;
        muteIconImage.gameObject.SetActive(false);

        // CALL
        GameObject callBtnContainer =
            new GameObject(
                "CallBtn"
            );

        callBtnContainer.transform.SetParent(
            phoneScreen.transform,
            false
        );

        RectTransform callContRT =
            callBtnContainer.AddComponent<RectTransform>();

        callContRT.anchorMin =
            new Vector2(0.5f, 0.5f);

        callContRT.anchorMax =
            new Vector2(0.5f, 0.5f);

        callContRT.pivot =
            new Vector2(0.5f, 0.5f);

        callContRT.anchoredPosition =
            new Vector2(0f, bottomRowY);

        callContRT.sizeDelta =
            new Vector2(68f, 68f);

        GameObject callCircle =
            CreateCircleImageGO(
                callBtnContainer,
                "CallCircle",
                65f,
                new Color(
                    0.1f,
                    0.78f,
                    0.3f,
                    1f
                )
            );

        callCircle.GetComponent<RectTransform>()
            .anchoredPosition =
            Vector2.zero;

        dialPadButtons[padLabels.Length] =
            callCircle.GetComponent<Image>();

        callIconText =
            CreateText(
                callBtnContainer,
                "CallIconText",
                "☎",
                31,
                FontStyles.Bold,
                new Vector2(0f, 1f),
                Color.white
            );

        GameObject callIconGO =
            CreateCircleImageGO(
                callBtnContainer,
                "CallIconImg",
                39f,
                Color.white
            );

        callIconGO.GetComponent<RectTransform>()
            .anchoredPosition =
            new Vector2(0f, 1f);

        callIconImage =
            callIconGO.GetComponent<Image>();

        callIconImage.preserveAspect = true;
        callIconImage.gameObject.SetActive(false);

        // END
        GameObject endBtnContainer =
            new GameObject(
                "EndBtn"
            );

        endBtnContainer.transform.SetParent(
            phoneScreen.transform,
            false
        );

        RectTransform endContRT =
            endBtnContainer.AddComponent<RectTransform>();

        endContRT.anchorMin =
            new Vector2(0.5f, 0.5f);

        endContRT.anchorMax =
            new Vector2(0.5f, 0.5f);

        endContRT.pivot =
            new Vector2(0.5f, 0.5f);

        endContRT.anchoredPosition =
            new Vector2(98f, bottomRowY);

        endContRT.sizeDelta =
            new Vector2(60f, 60f);

        GameObject endCircle =
            CreateCircleImageGO(
                endBtnContainer,
                "EndCircle",
                57f,
                new Color(
                    0.85f,
                    0.12f,
                    0.12f,
                    1f
                )
            );

        endCircle.GetComponent<RectTransform>()
            .anchoredPosition =
            Vector2.zero;

        endIconText =
            CreateText(
                endBtnContainer,
                "EndIconText",
                "✕",
                20,
                FontStyles.Bold,
                new Vector2(0f, 1f),
                Color.white
            );

        GameObject endIconGO =
            CreateCircleImageGO(
                endBtnContainer,
                "EndIconImg",
                31f,
                Color.white
            );

        endIconGO.GetComponent<RectTransform>()
            .anchoredPosition =
            new Vector2(0f, 1f);

        endIconImage =
            endIconGO.GetComponent<Image>();

        endIconImage.preserveAspect = true;
        endIconImage.gameObject.SetActive(false);

        phoneScreenBgImage =
            phoneScreen.GetComponent<Image>();

        CreateRoundedPanel(
            phoneScreen,
            "HomeBar",
            new Vector2(117f, 5f),
            new Color(
                0.5f,
                0.5f,
                0.6f,
                0.5f
            ),
            new Vector2(0f, -353f)
        );
    }

    // ═══════════════════════════════════════════════════════════════════════
    // VICTORY PANEL
    // ═══════════════════════════════════════════════════════════════════════

    private void BuildVictoryPanel()
    {
        victoryPanel = new GameObject("VictoryPanel");
        victoryPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform vpRT = victoryPanel.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.pivot = new Vector2(0.5f, 0.5f);
        vpRT.sizeDelta = Vector2.zero;
        vpRT.anchoredPosition = Vector2.zero;

        GameObject bgGO = new GameObject("VictoryBgImage");
        bgGO.transform.SetParent(victoryPanel.transform, false);

        victoryBgImage = bgGO.AddComponent<Image>();
        victoryBgImage.sprite = uiSkorBintang1;
        victoryBgImage.preserveAspect = true;
        victoryBgImage.type = Image.Type.Simple;
        victoryBgImage.raycastTarget = false;

        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0.5f);
        bgRT.anchorMax = new Vector2(0.5f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta = new Vector2(530f, 750f);

        GameObject timerGO = new GameObject("VictoryTimeText");
        timerGO.transform.SetParent(bgGO.transform, false);

        Image oldTimerImg = timerGO.GetComponent<Image>();
        if (oldTimerImg != null)
        {
            Destroy(oldTimerImg);
        }

        victoryTimeText = timerGO.AddComponent<TextMeshProUGUI>();

        victoryTimeText.text = "00:00";
        victoryTimeText.fontSize = 62;
        victoryTimeText.fontStyle = FontStyles.Bold;
        victoryTimeText.color = Color.white;
        victoryTimeText.alignment = TextAlignmentOptions.Center;
        victoryTimeText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        victoryTimeText.verticalAlignment = VerticalAlignmentOptions.Middle;
        victoryTimeText.textWrappingMode = TextWrappingModes.NoWrap;
        victoryTimeText.overflowMode = TextOverflowModes.Overflow;
        victoryTimeText.margin = Vector4.zero;
        victoryTimeText.raycastTarget = false;

        RectTransform timerRT = timerGO.GetComponent<RectTransform>();

        timerRT.anchorMin = new Vector2(0.5f, 0.5f);
        timerRT.anchorMax = new Vector2(0.5f, 0.5f);
        timerRT.pivot = new Vector2(0.5f, 0.5f);

        timerRT.anchoredPosition = posisiTimerIndonesia;
        timerRT.sizeDelta = new Vector2(360f, 75f);

        GameObject lobbyBtnGO = new GameObject("LobbyButton");
        lobbyBtnGO.transform.SetParent(bgGO.transform, false);

        Image lobbyBtnImage = lobbyBtnGO.AddComponent<Image>();

        lobbyBtnImage.color = new Color(1f, 1f, 1f, 0.001f);
        lobbyBtnImage.raycastTarget = true;

        Button lobbyBtn = lobbyBtnGO.AddComponent<Button>();
        lobbyBtn.transition = Selectable.Transition.None;

        lobbyBtn.onClick.RemoveAllListeners();
        lobbyBtn.onClick.AddListener(() =>
        {
            Debug.Log("[VRUIManager] Tombol KEMBALI KE LOBBY ditekan.");
            GoToLobby();
        });

        RectTransform lobbyRT = lobbyBtnGO.GetComponent<RectTransform>();

        lobbyRT.anchorMin = new Vector2(0.5f, 0.5f);
        lobbyRT.anchorMax = new Vector2(0.5f, 0.5f);
        lobbyRT.pivot = new Vector2(0.5f, 0.5f);

        lobbyRT.anchoredPosition = new Vector2(0f, -315f);
        lobbyRT.sizeDelta = new Vector2(460f, 120f);
    }

    private void BuildGameOverPanel()
    {
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform gopRT = gameOverPanel.AddComponent<RectTransform>();
        gopRT.anchorMin = Vector2.zero;
        gopRT.anchorMax = Vector2.one;
        gopRT.pivot = new Vector2(0.5f, 0.5f);
        gopRT.sizeDelta = Vector2.zero;
        gopRT.anchoredPosition = Vector2.zero;

        // ── Gambar UI Kustom dari Tim UI/UX ──
        GameObject bgGO = new GameObject("GameOverBgImage");
        bgGO.transform.SetParent(gameOverPanel.transform, false);

        gameOverBgImage = bgGO.AddComponent<Image>();
        gameOverBgImage.preserveAspect = true;
        gameOverBgImage.type = Image.Type.Simple;
        gameOverBgImage.raycastTarget = false;
        gameOverBgImage.color = Color.white;

        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0.5f);
        bgRT.anchorMax = new Vector2(0.5f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta = new Vector2(530f, 750f); // Disamakan dengan ukuran kartu Victory

        // ── Teks Waktu (Outage Time / Survival Time) ──
        // Sama seperti VictoryTimeText: teks dinamis ditaruh DI ATAS gambar UI/UX,
        // supaya angka MM:SS asli benar-benar tampil (bukan cuma placeholder di gambar).
        GameObject survivalTimeGO = new GameObject("GameOverSurvivalTimeText");
        survivalTimeGO.transform.SetParent(bgGO.transform, false);

        gameOverSurvivalTimeText = survivalTimeGO.AddComponent<TextMeshProUGUI>();
        gameOverSurvivalTimeText.text = "00:00";
        gameOverSurvivalTimeText.fontSize = 62; // Disamakan dengan ukuran font timer Victory
        gameOverSurvivalTimeText.fontStyle = FontStyles.Bold;
        gameOverSurvivalTimeText.color = Color.white;
        gameOverSurvivalTimeText.alignment = TextAlignmentOptions.Center;
        gameOverSurvivalTimeText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        gameOverSurvivalTimeText.verticalAlignment = VerticalAlignmentOptions.Middle;
        gameOverSurvivalTimeText.textWrappingMode = TextWrappingModes.NoWrap;
        gameOverSurvivalTimeText.overflowMode = TextOverflowModes.Overflow;
        gameOverSurvivalTimeText.margin = Vector4.zero;
        gameOverSurvivalTimeText.raycastTarget = false;

        RectTransform survivalTimeRT = survivalTimeGO.GetComponent<RectTransform>();
        survivalTimeRT.anchorMin = new Vector2(0.5f, 0.5f);
        survivalTimeRT.anchorMax = new Vector2(0.5f, 0.5f);
        survivalTimeRT.pivot = new Vector2(0.5f, 0.5f);
        survivalTimeRT.anchoredPosition = posisiWaktuGameOverIndonesia;
        survivalTimeRT.sizeDelta = new Vector2(360f, 75f);

        // ── Tombol Mulai Lagi / Try Again ──
        // Slot button ini ditempatkan di bagian bawah kartu UI/UX kamu
        // Sesuaikan posisi anchoredPosition jika perlu cocokkan dengan desain PNG
        GameObject retryBtnGO = new GameObject("MulaiLagiButton");
        retryBtnGO.transform.SetParent(bgGO.transform, false);

        Button retryBtn = retryBtnGO.AddComponent<Button>();
        retryBtn.transition = Selectable.Transition.ColorTint;
        retryBtn.onClick.RemoveAllListeners();
        retryBtn.onClick.AddListener(() =>
        {
            Debug.Log("[VRUIManager] Tombol MULAI LAGI ditekan.");
            RestartSimulation();
        });

        Image retryImg = retryBtnGO.AddComponent<Image>();
        retryImg.color = new Color(1f, 1f, 1f, 0.001f); // hampir transparan agar tidak menutupi gambar UI/UX tapi tetap bisa diklik

        RectTransform retryRT = retryBtnGO.GetComponent<RectTransform>();
        retryRT.anchorMin = new Vector2(0.5f, 0.5f);
        retryRT.anchorMax = new Vector2(0.5f, 0.5f);
        retryRT.pivot = new Vector2(0.5f, 0.5f);
        retryRT.anchoredPosition = posisiTombolRetryGameOver;
        retryRT.sizeDelta = new Vector2(460f, 120f);       // Disamakan dengan ukuran tombol Victory

        // ── Tombol Kembali ke Beranda / Lobby ──
        // Sama seperti LobbyButton di Victory panel, supaya Game Over juga bisa
        // kembali ke halaman awal, bukan cuma retry saja.
        GameObject lobbyBtnGO = new GameObject("LobbyButton");
        lobbyBtnGO.transform.SetParent(bgGO.transform, false);

        Image lobbyBtnImage = lobbyBtnGO.AddComponent<Image>();
        lobbyBtnImage.color = new Color(1f, 1f, 1f, 0.001f);
        lobbyBtnImage.raycastTarget = true;

        Button lobbyBtn = lobbyBtnGO.AddComponent<Button>();
        lobbyBtn.transition = Selectable.Transition.None;

        lobbyBtn.onClick.RemoveAllListeners();
        lobbyBtn.onClick.AddListener(() =>
        {
            Debug.Log("[VRUIManager] Tombol KEMBALI KE LOBBY (dari Game Over) ditekan.");
            GoToLobby();
        });

        RectTransform lobbyRT = lobbyBtnGO.GetComponent<RectTransform>();
        lobbyRT.anchorMin = new Vector2(0.5f, 0.5f);
        lobbyRT.anchorMax = new Vector2(0.5f, 0.5f);
        lobbyRT.pivot = new Vector2(0.5f, 0.5f);
        lobbyRT.anchoredPosition = posisiTombolLobbyGameOver;
        lobbyRT.sizeDelta = new Vector2(460f, 120f); // Disamakan dengan ukuran tombol Victory

        gameOverPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SMOKE WARNING PANELS (WARNING 1 & WARNING 2 / KRITIS)
    // ═══════════════════════════════════════════════════════════════════════

    private void BuildSmokeWarningPanels()
    {
        // Panel dibuat KOSONG/transparan (bukan kotak warna + teks bawaan lagi).
        // Tinggal isi sprite desain kamu sendiri di Inspector:
        // uiWarning1_Indonesia / uiWarning1_Inggris / uiWarning2_Indonesia / uiWarning2_Inggris

        // ── Panel Peringatan 1: Asap Mulai Menebal ──
        // Ukuran disamakan rasionya dengan rekomendasi PNG (1200 x 360 = rasio 10:3)
        // supaya gambar tidak gepeng saat di-stretch ke panel.
        GameObject w1GO = CreateRoundedPanel(mainCanvas.gameObject, "SmokeWarning1_Panel", new Vector2(900f, 270f), new Color(0f, 0f, 0f, 0f), new Vector2(0f, 210f));
        warn1BgImage = w1GO.GetComponent<Image>();
        warn1BgImage.preserveAspect = true;

        // Perbesar banner (skalaBannerPeringatan) dan geser sedikit ke arah pemain
        // (majuKeArahPemainMeter, dikonversi ke unit lokal via skala canvas) supaya
        // lebih mudah terbaca di VR tanpa mengubah panel lain.
        w1GO.transform.localScale = Vector3.one * skalaBannerPeringatan;
        w1GO.transform.localPosition += new Vector3(0f, 0f, -majuKeArahPemainMeter / Mathf.Max(0.0001f, mainCanvas.transform.localScale.z));

        warn1CanvasGroup = w1GO.AddComponent<CanvasGroup>();
        warn1CanvasGroup.alpha = 1f;
        warn1CanvasGroup.blocksRaycasts = false;

        // ── Panel Peringatan 2: Kondisi Kritis Asap Sangat Tebal ──
        GameObject w2GO = CreateRoundedPanel(mainCanvas.gameObject, "SmokeWarning2_Panel", new Vector2(900f, 270f), new Color(0f, 0f, 0f, 0f), new Vector2(0f, 210f));
        warn2BgImage = w2GO.GetComponent<Image>();
        warn2BgImage.preserveAspect = true;

        w2GO.transform.localScale = Vector3.one * skalaBannerPeringatan;
        w2GO.transform.localPosition += new Vector3(0f, 0f, -majuKeArahPemainMeter / Mathf.Max(0.0001f, mainCanvas.transform.localScale.z));

        warn2CanvasGroup = w2GO.AddComponent<CanvasGroup>();
        warn2CanvasGroup.alpha = 1f;
        warn2CanvasGroup.blocksRaycasts = false;

        // Sama seperti Victory/GameOver panel: nonaktif total di awal via SetActive,
        // BUKAN cuma alpha 0. Ini yang bikin nggak ada lagi efek "kedat-kedut".
        w1GO.SetActive(false);
        w2GO.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPER UI
    // ═══════════════════════════════════════════════════════════════════════

    private GameObject CreateRoundedPanel(
        GameObject parent,
        string name,
        Vector2 size,
        Color bgClr,
        Vector2 pos
    )
    {
        GameObject go =
            new GameObject(name);

        go.transform.SetParent(
            parent.transform,
            false
        );

        Image img =
            go.AddComponent<Image>();

        img.color =
            bgClr;

        RectTransform rt =
            go.GetComponent<RectTransform>();

        rt.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.anchoredPosition =
            pos;

        rt.sizeDelta =
            size;

        return go;
    }

    private GameObject CreateCircleImageGO(
        GameObject parent,
        string name,
        float size,
        Color clr
    )
    {
        GameObject go =
            new GameObject(name);

        go.transform.SetParent(
            parent.transform,
            false
        );

        Image img =
            go.AddComponent<Image>();

        img.color =
            clr;

        img.sprite =
            circleSprite;

        img.type =
            Image.Type.Simple;

        img.preserveAspect =
            false;

        RectTransform rt =
            go.GetComponent<RectTransform>();

        rt.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.anchoredPosition =
            Vector2.zero;

        rt.sizeDelta =
            new Vector2(
                size,
                size
            );

        return go;
    }

    private Image CreateCircleImage(
        GameObject parent,
        string name,
        float size,
        Color clr,
        Vector2 pos
    )
    {
        GameObject go =
            CreateCircleImageGO(
                parent,
                name,
                size,
                clr
            );

        go.GetComponent<RectTransform>()
            .anchoredPosition =
            pos;

        return go.GetComponent<Image>();
    }

    private TextMeshProUGUI CreateText(
        GameObject parent,
        string name,
        string textStr,
        float fontSize,
        FontStyles style,
        Vector2 pos,
        Color clr
    )
    {
        GameObject go =
            new GameObject(name);

        go.transform.SetParent(
            parent.transform,
            false
        );

        TextMeshProUGUI tmp =
            go.AddComponent<TextMeshProUGUI>();

        tmp.text =
            textStr;

        tmp.fontSize =
            fontSize;

        tmp.fontStyle =
            style;

        tmp.color =
            clr;

        tmp.alignment =
            TextAlignmentOptions.Center;

        RectTransform rt =
            go.GetComponent<RectTransform>();

        rt.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.anchoredPosition =
            pos;

        RectTransform parentRT =
            parent.GetComponent<RectTransform>();

        float parentW =
            parentRT != null
                ? parentRT.sizeDelta.x
                : 400f;

        rt.sizeDelta =
            new Vector2(
                parentW - 14f,
                fontSize + 22f
            );

        return tmp;
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager UI & Alur Simulasi VR APAR Terpadu:
/// 1. Menggunakan UI LANDING PAGE asli bawaan scene (Tanpa UI ganda).
/// 2. Animasi Loading Futuristic Glassmorphism Card (0 - 100%).
/// 3. Animasi Panggilan Darurat 113 bergaya iPhone 16 Pro Max Premium:
///    - Frame HP Titanium Ultra Premium dengan Dynamic Island.
///    - Tombol numpad dial klik-klik animasi.
///    - Avatar & tombol lingkaran (circle sprite).
///    - Audio Waveform Bars & Concentric Ripple Rings saat ringing.
///    - Typewriter message dari Petugas Damkar 113.
/// 4. Timer TIDAK ditampilkan saat misi aktif — muncul hanya di Victory Box.
/// 5. Pop-up Kotak Penilaian / Grade Box (S, A, B, C, F & Waktu Padam).
/// </summary>
public class VRSimulationUIManager : MonoBehaviour
{
    public static VRSimulationUIManager Instance { get; private set; }

    public enum UIPhase
    {
        StartLanding,
        Loading,
        EmergencyCall113,
        ActiveMission,
        VictoryGrade
    }

    [Header("Status Misi Runtime")]
    public UIPhase currentPhase = UIPhase.StartLanding;
    public float missionTimer = 0f;
    public bool isTimerRunning = false;

    [Header("Pengaturan Audio UI (Optional)")]
    public AudioClip loadingBeepClip;
    public AudioClip phoneRingingClip;
    public AudioClip phoneDispatchClip;
    public AudioClip victoryFanfareClip;

    [Header("Custom Icons HP (Drag PNG dari folder Assets)")]
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

    // ── Referensi Scene & UI ──
    private GameObject originalLandingPageGO;
    private Canvas mainCanvas;
    private VRBillboardUI billboardScript;
    private AudioSource uiAudioSource;

    // ── Sub-Panels VR Canvas ──
    private GameObject loadingPanel;
    private GameObject phoneCallPanel;
    private GameObject victoryPanel;
    // Timer HUD dihapus — waktu hanya tampil di Victory

    // ── Elements Loading ──
    private Image loadingProgressBar;
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

    // ── Slot Image untuk custom icon (diisi di BuildPhonePanel) ──
    private Image avatarCenterImage;   // tengah circle avatar
    private Image callIconImage;       // ikon di tombol call hijau
    private Image muteIconImage;       // ikon di tombol mute
    private Image endIconImage;        // ikon di tombol end merah
    private Image phoneScreenBgImage;  // wallpaper layar HP
    private TextMeshProUGUI avatarCenterText;  // fallback teks ikon avatar
    private TextMeshProUGUI callIconText;      // fallback teks ikon call
    private TextMeshProUGUI muteIconText;      // fallback teks ikon mute
    private TextMeshProUGUI endIconText;       // fallback teks ikon end

    // ── Elements Victory ──
    private TextMeshProUGUI victoryTimeText;
    private TextMeshProUGUI victoryGradeBadgeText;
    private TextMeshProUGUI victoryTitleText;
    private TextMeshProUGUI victoryDescriptionText;

    // ── Circle Sprite Cache ──
    private Sprite circleSprite;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        uiAudioSource = GetComponent<AudioSource>();
        if (uiAudioSource == null) uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0f;

        circleSprite = CreateCircleSprite(128);

        SetupWorldSpaceCanvas();
        BuildUIComponents();
    }

    private void Start()
    {
        originalLandingPageGO = GameObject.Find("UI LANDING PAGE");

        VRHoldButton holdBtn = FindFirstObjectByType<VRHoldButton>();
        if (holdBtn != null)
        {
            holdBtn.OnHoldComplete.RemoveListener(StartLoadingFlow);
            holdBtn.OnHoldComplete.AddListener(StartLoadingFlow);
        }

        // Terapkan custom icons dari Inspector (harus setelah BuildUIComponents di Awake)
        ApplyCustomIcons();

        // Kunci semua interaksi APAR sampai misi dimulai
        LockAllAPAR();

        SetPhase(UIPhase.StartLanding);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MISSION LOCK SYSTEM
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Kunci semua interaksi APAR di scene (dipanggil di awal sebelum misi dimulai).
    /// Semua grab, pin, spray, dan UI indikator akan tidak aktif.
    /// </summary>
    private void LockAllAPAR()
    {
        // APARPropStateMachine — kunci logic pin dan lever
        // (tidak perlu action khusus: isMissionStarted = false sudah by default)

        // APARPinIndicator — canvas sudah disembunyikan otomatis saat dibuat
        // (tidak perlu action khusus)

        // APARHoseGrabber — isMissionStarted sudah false by default
        // (tidak perlu action khusus)

        // APARPin — isMissionStarted sudah false by default
        // (tidak perlu action khusus)

        Debug.Log("[VRUIManager] 🔒 Semua interaksi APAR dikunci. Tunggu hold Mulai Misi.");
    }

    /// <summary>
    /// Buka kunci semua interaksi APAR di scene (dipanggil setelah animasi selesai, misi aktif).
    /// </summary>
    private void UnlockAllAPAR()
    {
        // Buka kunci semua APARPropStateMachine
        var propStateMachines = FindObjectsByType<APARPropStateMachine>(FindObjectsSortMode.None);
        foreach (var psm in propStateMachines)
            psm.SetMissionStarted();

        // Tampilkan semua APARPinIndicator
        var pinIndicators = FindObjectsByType<APARPinIndicator>(FindObjectsSortMode.None);
        foreach (var pi in pinIndicators)
            pi.SetMissionStarted();

        // Buka kunci semua APARHoseGrabber
        var hoseGrabbers = FindObjectsByType<APARHoseGrabber>(FindObjectsSortMode.None);
        foreach (var hg in hoseGrabbers)
            hg.SetMissionStarted();

        // Buka kunci semua APARPin
        var aparPins = FindObjectsByType<APARPin>(FindObjectsSortMode.None);
        foreach (var ap in aparPins)
            ap.SetMissionStarted();

        // Aktifkan grab body APAR (AutoFireExtinguisher)
        var extinguishers = FindObjectsByType<AutoFireExtinguisher>(FindObjectsSortMode.None);
        foreach (var ext in extinguishers)
            ext.SetMissionStarted();

        // Tampilkan panduan animasi APAR (APARPinGuideAnimation)
        var pinGuides = FindObjectsByType<APARPinGuideAnimation>(FindObjectsSortMode.None);
        foreach (var pg in pinGuides)
            pg.SetMissionStarted();

        Debug.Log($"[VRUIManager] ✅ APAR Unlocked: {propStateMachines.Length} StateMachine, " +
                  $"{pinIndicators.Length} PinIndicator, {hoseGrabbers.Length} HoseGrabber, " +
                  $"{aparPins.Length} APARPin, {extinguishers.Length} Extinguisher, {pinGuides.Length} PinGuide");
    }

    /// <summary>
    /// Terapkan Sprite custom yang di-assign di Inspector ke slot Image HP.
    /// Setiap slot: jika Sprite != null → tampilkan gambar, sembunyikan teks fallback.
    ///              jika Sprite == null → tampilkan teks unicode fallback.
    /// </summary>
    private void ApplyCustomIcons()
    {
        // ─ Avatar center ─
        if (avatarCenterImage != null)
        {
            bool useCustom = iconAvatar != null;
            avatarCenterImage.sprite = useCustom ? iconAvatar : circleSprite;
            avatarCenterImage.color = useCustom ? Color.white : new Color(0.8f, 0.08f, 0.08f, 0.7f);
            avatarCenterImage.preserveAspect = useCustom;
            if (avatarCenterText != null) avatarCenterText.gameObject.SetActive(!useCustom);
        }

        // ─ Call button icon ─
        if (callIconImage != null)
        {
            bool useCustom = iconCallBtn != null;
            callIconImage.gameObject.SetActive(useCustom);
            callIconImage.sprite = useCustom ? iconCallBtn : null;
            if (callIconText != null) callIconText.gameObject.SetActive(!useCustom);
        }

        // ─ Mute button icon ─
        if (muteIconImage != null)
        {
            bool useCustom = iconMuteBtn != null;
            muteIconImage.gameObject.SetActive(useCustom);
            muteIconImage.sprite = useCustom ? iconMuteBtn : null;
            if (muteIconText != null) muteIconText.gameObject.SetActive(!useCustom);
        }

        // ─ End button icon ─
        if (endIconImage != null)
        {
            bool useCustom = iconEndBtn != null;
            endIconImage.gameObject.SetActive(useCustom);
            endIconImage.sprite = useCustom ? iconEndBtn : null;
            if (endIconText != null) endIconText.gameObject.SetActive(!useCustom);
        }

        // ─ Wallpaper layar HP ─
        if (phoneScreenBgImage != null && phoneWallpaper != null)
        {
            phoneScreenBgImage.sprite = phoneWallpaper;
            phoneScreenBgImage.color = Color.white;
            phoneScreenBgImage.type = Image.Type.Simple;
            phoneScreenBgImage.preserveAspect = false;
        }
    }


    private void Update()
    {
        if (isTimerRunning)
        {
            missionTimer += Time.deltaTime;
            // Timer tidak ditampilkan di HUD — hanya dicatat
        }

        if (currentPhase == UIPhase.EmergencyCall113)
        {
            AnimateSmartphoneVisuals();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CIRCLE SPRITE GENERATOR (Render Circle tanpa texture external)
    // ═══════════════════════════════════════════════════════════════════════

    private Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[resolution * resolution];
        float radius = resolution * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ANIMASI SMARTPHONE REALTIME
    // ═══════════════════════════════════════════════════════════════════════

    private void AnimateSmartphoneVisuals()
    {
        float time = Time.time;

        // 1. Floating Hover Sway HP di VR Space
        if (phoneContainerRT != null)
        {
            float hoverY = Mathf.Sin(time * 2.2f) * 6f;
            float tiltZ = Mathf.Sin(time * 1.6f) * 1.2f;
            phoneContainerRT.anchoredPosition = new Vector2(0f, hoverY);
            phoneContainerRT.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
        }

        // 2. Pulsa Halo Avatar
        if (phoneAvatarPulseHalo != null)
        {
            float pulse = 1.0f + Mathf.Sin(time * 6f) * 0.12f;
            phoneAvatarPulseHalo.transform.localScale = Vector3.one * pulse;
        }

        // 3. Equalizer Waveform Bars Bounce
        if (equalizerBars != null)
        {
            for (int i = 0; i < equalizerBars.Length; i++)
            {
                if (equalizerBars[i] == null) continue;
                float h = Mathf.Abs(Mathf.Sin(time * 11f + i * 1.4f)) * 0.82f + 0.18f;
                RectTransform barRT = equalizerBars[i].GetComponent<RectTransform>();
                if (barRT != null) barRT.sizeDelta = new Vector2(7f, 30f * h);
            }
        }

        // 4. Ripple Rings Expansion
        if (rippleRings != null)
        {
            for (int i = 0; i < rippleRings.Length; i++)
            {
                if (rippleRings[i] == null) continue;
                float phase = (time * 1.3f + i * 0.45f) % 1.0f;
                float scale = Mathf.Lerp(0.75f, 1.7f, phase);
                float alpha = Mathf.Lerp(0.55f, 0.0f, phase);
                rippleRings[i].transform.localScale = Vector3.one * scale;
                rippleRings[i].color = new Color(1f, 0.22f, 0.22f, alpha);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ALUR SIMULASI VR
    // ═══════════════════════════════════════════════════════════════════════

    public void StartLoadingFlow()
    {
        if (currentPhase != UIPhase.StartLanding) return;
        StartCoroutine(LoadingRoutine());
    }

    private IEnumerator LoadingRoutine()
    {
        SetPhase(UIPhase.Loading);
        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            if (loadingProgressBar != null) loadingProgressBar.fillAmount = progress;
            if (loadingPercentText != null) loadingPercentText.text = $"{Mathf.RoundToInt(progress * 100)}%";

            if (progress < 0.35f)
                loadingStatusText.text = "Menginisialisasi Sistem Sensor APAR...";
            else if (progress < 0.75f)
                loadingStatusText.text = "Menyiapkan Skenario Kebakaran Dapur...";
            else
                loadingStatusText.text = "Menghubungkan ke Saluran Darurat...";

            yield return null;
        }

        StartCoroutine(EmergencyCall113Routine());
    }

private IEnumerator EmergencyCall113Routine()
    {
        SetPhase(UIPhase.EmergencyCall113);

        if (phoneContainerRT != null)
            StartCoroutine(AnimatePopUpScale(phoneContainerRT));

        // 1. Animasi Dialing — numpad klik-klik cepat
        phoneDialText.text = "";
        phoneStatusText.text = "Masukkan Nomor...";
        phoneDispatchMessage.gameObject.SetActive(false);

        // Tampilkan dialpad, sembunyikan ripple/eq dulu
        SetDialpadVisible(true);
        SetRingingVisible(false);

        int[] dialSequence = { 0, 0, 2 }; // index 0=1, 1=1, 2=3 pada numpad [1,2,3]
        string[] dialKeys = { "1", "1", "3" };

        for (int i = 0; i < dialKeys.Length; i++)
        {
            phoneDialText.text += dialKeys[i];
            if (uiAudioSource != null && loadingBeepClip != null)
                uiAudioSource.PlayOneShot(loadingBeepClip, 0.6f);

            // Flash press animasi tombol
            if (dialPadButtons != null && dialSequence[i] < dialPadButtons.Length)
                StartCoroutine(AnimateButtonPress(dialPadButtons[dialSequence[i]]));

            yield return new WaitForSeconds(0.15f); // DIPERCEPAT: dari 0.4f menjadi 0.15f
        }

        yield return new WaitForSeconds(0.1f); // DIPERCEPAT: dari 0.3f menjadi 0.1f

        // 2. Tekan tombol Call (hijau)
        phoneStatusText.text = "Memanggil...";
        if (dialPadButtons != null && dialPadButtons.Length > 9)
            StartCoroutine(AnimateButtonPress(dialPadButtons[9])); // tombol call

        yield return new WaitForSeconds(0.15f); // DIPERCEPAT: dari 0.5f menjadi 0.15f

        // 3. State Ringing (Nada Dering Singkat)
        SetDialpadVisible(false);
        SetRingingVisible(true);
        phoneStatusText.text = "Memanggil Damkar 113...";

        if (uiAudioSource != null && phoneRingingClip != null)
        {
            uiAudioSource.clip = phoneRingingClip;
            uiAudioSource.loop = true;
            uiAudioSource.Play();
        }

        yield return new WaitForSeconds(0.6f); // DIPERCEPAT BANYAK: dari 2.5f menjadi 0.6f (dering langsung tersambung)

        if (uiAudioSource != null && uiAudioSource.isPlaying)
            uiAudioSource.Stop();

        // 4. Connected — Terhubung langsung ke petugas
        phoneStatusText.text = "Terhubung • Damkar 113";
        phoneDispatchMessage.gameObject.SetActive(true);

        if (uiAudioSource != null && phoneDispatchClip != null)
            uiAudioSource.PlayOneShot(phoneDispatchClip);

        string fullMessage = "<color=#FF5722><b>LAPORAN KEBAKARAN DITERIMA!</b></color>\n\"Unit Pemadam meluncur. Segera ambil APAR, cabut pin safety, arahkan corong ke pangkal api, dan semprot!\"";

        phoneDispatchMessage.text = "";
        string currentText = "";
        bool insideTag = false;

        for (int i = 0; i < fullMessage.Length; i++)
        {
            char c = fullMessage[i];
            if (c == '<') insideTag = true;
            currentText += c;
            if (c == '>') insideTag = false;

            if (!insideTag)
            {
                phoneDispatchMessage.text = currentText;
                yield return new WaitForSeconds(0.01f); // TEKS TYPEWRITER DIPERCEPAT: dari 0.022f menjadi 0.01f
            }
        }
        phoneDispatchMessage.text = fullMessage;

        yield return new WaitForSeconds(2.0f); // DIPERCEPAT: menunggu instruksi dibaca dari 3.5f menjadi 2.0f

        StartActiveMission();
    }

    private void SetDialpadVisible(bool visible)
    {
        if (dialPadButtons == null) return;
        foreach (var btn in dialPadButtons)
        {
            if (btn != null)
                btn.transform.parent.gameObject.SetActive(visible);
        }
    }

    private void SetRingingVisible(bool visible)
    {
        if (rippleRings != null)
            foreach (var r in rippleRings)
                if (r != null) r.gameObject.SetActive(visible);

        if (equalizerBars != null)
            foreach (var b in equalizerBars)
                if (b != null) b.transform.parent.gameObject.SetActive(visible);
    }

    private IEnumerator AnimateButtonPress(Image btnImg)
    {
        if (btnImg == null) yield break;
        Color origColor = btnImg.color;
        Color pressColor = Color.white;

        btnImg.color = pressColor;
        RectTransform rt = btnImg.GetComponent<RectTransform>();
        Vector3 origScale = rt != null ? rt.localScale : Vector3.one;

        if (rt != null) rt.localScale = Vector3.one * 0.85f;
        yield return new WaitForSeconds(0.08f);

        if (rt != null) rt.localScale = origScale;
        btnImg.color = origColor;
    }

    public void StartActiveMission()
    {
        missionTimer = 0f;
        isTimerRunning = true;
        SetPhase(UIPhase.ActiveMission);

        var alarmSystem = FindFirstObjectByType<FireAlarmSystem>();
        if (alarmSystem != null) alarmSystem.StartAlarm();

        // Buka kunci semua interaksi APAR sekarang misi aktif
        UnlockAllAPAR();

        Debug.Log("[VRUIManager] Misi Pemadaman APAR Dimulai!");
    }

    public void OnMissionCompleted(float totalTime)
    {
        if (!isTimerRunning && currentPhase == UIPhase.VictoryGrade) return;
        isTimerRunning = false;
        missionTimer = totalTime;
        ShowVictoryGradeBox(totalTime);
    }

    private void ShowVictoryGradeBox(float totalTime)
    {
        SetPhase(UIPhase.VictoryGrade);

        if (uiAudioSource != null && victoryFanfareClip != null)
            uiAudioSource.PlayOneShot(victoryFanfareClip);

        int minutes = Mathf.FloorToInt(totalTime / 60f);
        float seconds = totalTime % 60f;
        string timeStr = $"{minutes:00}:{seconds:05.1}s";
        if (victoryTimeText != null)
            victoryTimeText.text = $"Waktu Pemadaman: <b>{timeStr}</b>";

        string grade; Color gradeColor; string titleDesc; string detailDesc;

        if (totalTime < 20f)
        {
            grade = "S"; gradeColor = new Color(1f, 0.84f, 0f);
            titleDesc = "LUAR BIASA!";
            detailDesc = "Eksekusi pemadaman api sangat cepat & presisi! Api diatasi sepenuhnya.";
        }
        else if (totalTime < 35f)
        {
            grade = "A"; gradeColor = new Color(0.2f, 1f, 0.55f);
            titleDesc = "SANGAT BAIK!";
            detailDesc = "Respon tanggap & efektif. APAR digunakan dengan tepat pada pangkal api.";
        }
        else if (totalTime < 50f)
        {
            grade = "B"; gradeColor = new Color(0.15f, 0.85f, 1f);
            titleDesc = "BAIK!";
            detailDesc = "Api berhasil dipadamkan. Coba lebih cepat dalam mencabut pin & mengarahkan selang.";
        }
        else if (totalTime < 70f)
        {
            grade = "C"; gradeColor = new Color(1f, 0.62f, 0.1f);
            titleDesc = "CUKUP";
            detailDesc = "Waktu pemadaman agak lambat. Semprot langsung ke pangkal api secara konstan.";
        }
        else
        {
            grade = "F"; gradeColor = new Color(1f, 0.25f, 0.25f);
            titleDesc = "PERLU EVALUASI";
            detailDesc = "Pemadaman sangat lambat. Dalam situasi nyata ini sangat berbahaya.";
        }

        if (victoryGradeBadgeText != null) { victoryGradeBadgeText.text = grade; victoryGradeBadgeText.color = gradeColor; }
        if (victoryTitleText != null) { victoryTitleText.text = titleDesc; victoryTitleText.color = gradeColor; }
        if (victoryDescriptionText != null) victoryDescriptionText.text = detailDesc;

        if (victoryPanel != null) StartCoroutine(AnimatePopUpScale(victoryPanel.transform));
    }

    public void RestartSimulation()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SetPhase(UIPhase newPhase)
    {
        currentPhase = newPhase;

        if (originalLandingPageGO != null)
            originalLandingPageGO.SetActive(newPhase == UIPhase.StartLanding);

        if (loadingPanel != null) loadingPanel.SetActive(newPhase == UIPhase.Loading);
        if (phoneCallPanel != null) phoneCallPanel.SetActive(newPhase == UIPhase.EmergencyCall113);
        if (victoryPanel != null) victoryPanel.SetActive(newPhase == UIPhase.VictoryGrade);
        // Tidak ada activeTimerHUD — timer disembunyikan selama misi aktif
    }

    private IEnumerator AnimatePopUpScale(Transform target)
    {
        target.localScale = Vector3.zero;
        float duration = 0.42f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Elastic overshoot: sin curve dengan sedikit bounce
            float scale = Mathf.Sin(t * Mathf.PI * 0.5f);
            if (t > 0.7f) scale = 1.0f + Mathf.Sin((t - 0.7f) * Mathf.PI / 0.3f) * 0.06f;
            target.localScale = Vector3.one * Mathf.Clamp(scale, 0f, 1.07f);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  UI BUILDER — iPhone 16 Pro Max Style Premium
    // ═══════════════════════════════════════════════════════════════════════

    private void SetupWorldSpaceCanvas()
    {
        GameObject canvasGO = new GameObject("VR_Simulation_UI_Canvas");
        canvasGO.transform.position = new Vector3(0f, 1.6f, 2.0f);

        mainCanvas = canvasGO.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.WorldSpace;
        mainCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        canvasGO.AddComponent<GraphicRaycaster>();

        billboardScript = canvasGO.AddComponent<VRBillboardUI>();
        billboardScript.distance = 2.2f;
        billboardScript.heightOffset = 0.1f;
        billboardScript.smoothSpeed = 6.0f;

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(800f, 700f);
        canvasRT.localScale = Vector3.one * 0.0018f;
    }

    private void BuildUIComponents()
    {
        BuildLoadingPanel();
        BuildPhonePanel();
        BuildVictoryPanel();
    }

    // ─── Loading Panel ───
    private void BuildLoadingPanel()
    {
        loadingPanel = CreateRoundedPanel(mainCanvas.gameObject, "LoadingPanel", new Vector2(500f, 240f),
            new Color(0.05f, 0.07f, 0.12f, 0.96f), Vector2.zero);

        CreateText(loadingPanel, "LoadingTitle", "MENYIAPKAN MISI", 26, FontStyles.Bold,
            new Vector2(0f, 72f), Color.cyan);

        // Dot baris dekoratif
        GameObject dotLine = CreateRoundedPanel(loadingPanel, "DotLine", new Vector2(30f, 3f),
            new Color(0f, 0.8f, 1f, 0.6f), new Vector2(0f, 52f));

        GameObject barBg = CreateRoundedPanel(loadingPanel, "ProgressBG", new Vector2(420f, 18f),
            new Color(0.14f, 0.18f, 0.28f, 1f), new Vector2(0f, 14f));

        GameObject barFill = new GameObject("ProgressFill");
        barFill.transform.SetParent(barBg.transform, false);
        loadingProgressBar = barFill.AddComponent<Image>();
        loadingProgressBar.color = new Color(0f, 0.88f, 1f);
        loadingProgressBar.type = Image.Type.Filled;
        loadingProgressBar.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fillRT = barFill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;
        fillRT.anchoredPosition = Vector2.zero;

        loadingPercentText = CreateText(loadingPanel, "PercentText", "0%", 22, FontStyles.Bold,
            new Vector2(0f, -18f), Color.white);
        loadingStatusText = CreateText(loadingPanel, "StatusText", "Menginisialisasi...", 14,
            FontStyles.Italic, new Vector2(0f, -58f), new Color(0.7f, 0.85f, 1f));
    }

// ─── Phone Panel — iPhone 16 Pro Max Style ───
    private void BuildPhonePanel()
    {
        // Container HP
        phoneCallPanel = new GameObject("PhoneCallContainer");
        phoneCallPanel.transform.SetParent(mainCanvas.transform, false);
        phoneContainerRT = phoneCallPanel.AddComponent<RectTransform>();
        phoneContainerRT.anchorMin = new Vector2(0.5f, 0.5f);
        phoneContainerRT.anchorMax = new Vector2(0.5f, 0.5f);
        phoneContainerRT.pivot = new Vector2(0.5f, 0.5f);
        phoneContainerRT.anchoredPosition = Vector2.zero;
        phoneContainerRT.sizeDelta = new Vector2(320f, 600f);

        // ── Outer frame (Titanium Natural / Black Titanium) ──
        GameObject outerShadow = CreateRoundedPanel(phoneCallPanel, "OuterShadow", new Vector2(322f, 602f),
            new Color(0f, 0f, 0f, 0.7f), Vector2.zero);
        outerShadow.GetComponent<RectTransform>().anchoredPosition = new Vector2(3f, -4f);

        GameObject phoneChassis = CreateRoundedPanel(phoneCallPanel, "PhoneChassis", new Vector2(315f, 596f),
            new Color(0.10f, 0.10f, 0.12f, 1f), Vector2.zero);

        // Titanium edge highlight (inner ring)
        GameObject titaniumRing = CreateRoundedPanel(phoneChassis, "TitaniumRing", new Vector2(308f, 589f),
            new Color(0.18f, 0.18f, 0.22f, 1f), Vector2.zero);

        // Side volume buttons (left)
        GameObject volUp = CreateRoundedPanel(phoneChassis, "VolUp", new Vector2(4f, 38f),
            new Color(0.16f, 0.16f, 0.20f, 1f), new Vector2(-159f, 105f));
        GameObject volDown = CreateRoundedPanel(phoneChassis, "VolDown", new Vector2(4f, 38f),
            new Color(0.16f, 0.16f, 0.20f, 1f), new Vector2(-159f, 56f));
        GameObject silentBtn = CreateRoundedPanel(phoneChassis, "Silent", new Vector2(4f, 22f),
            new Color(0.16f, 0.16f, 0.20f, 1f), new Vector2(-159f, 155f));

        // Side power button (right)
        GameObject powerBtn = CreateRoundedPanel(phoneChassis, "PowerBtn", new Vector2(4f, 52f),
            new Color(0.16f, 0.16f, 0.20f, 1f), new Vector2(159f, 85f));

        // ── Screen glass display ──
        GameObject phoneScreen = CreateRoundedPanel(phoneChassis, "PhoneScreen", new Vector2(298f, 578f),
            new Color(0.04f, 0.04f, 0.06f, 1f), Vector2.zero);

        // Screen top specular glare
        GameObject glare = CreateRoundedPanel(phoneScreen, "Glare", new Vector2(180f, 4f),
            new Color(1f, 1f, 1f, 0.06f), new Vector2(0f, 265f));

        // ── Dynamic Island (Notch pill) ──
        GameObject island = CreateRoundedPanel(phoneScreen, "DynamicIsland", new Vector2(88f, 22f),
            new Color(0.01f, 0.01f, 0.01f, 1f), new Vector2(0f, 263f));

        // Camera dot inside island
        Image camDot = CreateCircleImage(island, "CamDot", 7f, new Color(0.08f, 0.12f, 0.25f, 1f),
            new Vector2(30f, 0f));

        // ── Status Bar ──
        CreateText(phoneScreen, "SBTime", "9:41", 10, FontStyles.Bold,
            new Vector2(-110f, 261f), Color.white);
        CreateText(phoneScreen, "SBIcons", "5G  ▐▌ 100%", 9, FontStyles.Normal,
            new Vector2(100f, 261f), Color.white);

        // ── CALL STATE: Incoming/Outgoing screen ──
        GameObject callStateArea = new GameObject("CallStateArea");
        callStateArea.transform.SetParent(phoneScreen.transform, false);
        RectTransform callAreaRT = callStateArea.AddComponent<RectTransform>();
        callAreaRT.anchorMin = new Vector2(0.5f, 0.5f);
        callAreaRT.anchorMax = new Vector2(0.5f, 0.5f);
        callAreaRT.pivot = new Vector2(0.5f, 0.5f);
        callAreaRT.anchoredPosition = new Vector2(0f, 135f); // Dinaikkan sedikit agar tidak menabrak grid angka
        callAreaRT.sizeDelta = new Vector2(290f, 260f);

        // Concentric Ripple Rings
        rippleRings = new Image[4];
        for (int r = 0; r < 4; r++)
        {
            float size = 70f + r * 18f;
            GameObject ringGO = CreateCircleImageGO(callStateArea, $"Ring_{r}", size, new Color(1f, 0.22f, 0.22f, 0.35f));
            rippleRings[r] = ringGO.GetComponent<Image>();
            ringGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);
        }

        // Avatar halo glow
        GameObject haloGO = CreateCircleImageGO(callStateArea, "AvatarHalo", 86f, new Color(0.9f, 0.15f, 0.15f, 0.5f));
        phoneAvatarPulseHalo = haloGO.GetComponent<Image>();
        haloGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);

        // Avatar circle background
        GameObject avatarBg = CreateCircleImageGO(callStateArea, "AvatarBG", 72f, new Color(0.15f, 0.15f, 0.22f, 1f));
        avatarBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);

        // Avatar Red Slot
        GameObject avatarRed = CreateCircleImageGO(callStateArea, "AvatarRed", 68f, new Color(0.8f, 0.08f, 0.08f, 0.7f));
        avatarRed.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);
        avatarRed.transform.SetSiblingIndex(avatarBg.transform.GetSiblingIndex());
        avatarCenterImage = avatarRed.GetComponent<Image>();

        avatarCenterText = CreateText(avatarBg, "PhoneIcon", "\u260E", 32, FontStyles.Bold,
            new Vector2(0f, 2f), Color.white);

        GameObject avatarCustomImg = CreateCircleImageGO(avatarBg, "AvatarCustomIcon", 60f, Color.white);
        avatarCustomImg.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        avatarCenterImage = avatarCustomImg.GetComponent<Image>();
        avatarCenterImage.sprite = circleSprite;
        avatarCenterImage.color = new Color(0.8f, 0.08f, 0.08f, 0.7f);
        avatarCenterImage.gameObject.SetActive(false);

        // Contact name & Status teks
        CreateText(callStateArea, "ContactName", "DAMKAR DARURAT", 16, FontStyles.Bold,
            new Vector2(0f, 10f), Color.white);
        CreateText(callStateArea, "ContactSub", "Dinas Pemadam Kebakaran", 10, FontStyles.Normal,
            new Vector2(0f, -8f), new Color(0.65f, 0.7f, 0.8f));

        phoneDialText = CreateText(callStateArea, "DialText", "", 28, FontStyles.Bold,
            new Vector2(0f, -30f), new Color(1f, 0.88f, 0.2f));
        phoneDialText.characterSpacing = 8f;

        phoneStatusText = CreateText(callStateArea, "PhoneStatus", "Masukkan Nomor...", 11, FontStyles.Normal,
            new Vector2(0f, -58f), new Color(0.7f, 0.75f, 0.9f));

        // Equalizer Waveform Bars
        GameObject eqContainer = new GameObject("EQContainer");
        eqContainer.transform.SetParent(callStateArea.transform, false);
        RectTransform eqRT = eqContainer.AddComponent<RectTransform>();
        eqRT.anchorMin = new Vector2(0.5f, 0.5f);
        eqRT.anchorMax = new Vector2(0.5f, 0.5f);
        eqRT.pivot = new Vector2(0.5f, 0.5f);
        eqRT.anchoredPosition = new Vector2(0f, -80f);
        eqRT.sizeDelta = new Vector2(100f, 40f);

        equalizerBars = new Image[7];
        for (int i = 0; i < 7; i++)
        {
            float px = -42f + i * 14f;
            GameObject barGO = CreateRoundedPanel(eqContainer, $"EQ_{i}", new Vector2(7f, 24f),
                new Color(1f, 0.32f, 0.18f, 0.95f), new Vector2(px, 0f));
            equalizerBars[i] = barGO.GetComponent<Image>();
        }

        // ── Message dispatch box ──
        GameObject msgBox = CreateRoundedPanel(phoneScreen, "MsgBox", new Vector2(270f, 95f),
            new Color(0.09f, 0.11f, 0.18f, 0.97f), new Vector2(0f, -95f));

        phoneDispatchMessage = CreateText(msgBox, "DispatchMsg", "", 11, FontStyles.Normal,
            Vector2.zero, Color.white);
        phoneDispatchMessage.alignment = TextAlignmentOptions.Center;
        RectTransform msgRT = phoneDispatchMessage.GetComponent<RectTransform>();
        msgRT.sizeDelta = new Vector2(255f, 90f);
        msgBox.SetActive(false);

        // ── DIAL PAD GRID (Dilebarkan & Diperbesar ukurannya agar tidak menumpuk) ──
        string[] padLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "#" };
        dialPadButtons = new Image[padLabels.Length + 1];

        GameObject dialpadGrid = new GameObject("DialpadGrid");
        dialpadGrid.transform.SetParent(phoneScreen.transform, false);
        RectTransform gridRT = dialpadGrid.AddComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0.5f, 0.5f);
        gridRT.anchorMax = new Vector2(0.5f, 0.5f);
        gridRT.pivot = new Vector2(0.5f, 0.5f);
        gridRT.anchoredPosition = new Vector2(0f, -60f); // Posisi grid disesuaikan tengah-bawah
        gridRT.sizeDelta = new Vector2(220f, 200f);     // Ukuran container diperbesar

        for (int i = 0; i < padLabels.Length; i++)
        {
            int col = i % 3;
            int row = i / 3;

            // Spacing antar tombol diperlebar secara merata
            float px = -70f + col * 70f;
            float py = 75f - row * 50f;

            GameObject btnContainer = new GameObject($"PadKey_{padLabels[i]}");
            btnContainer.transform.SetParent(dialpadGrid.transform, false);

            RectTransform btnContRT = btnContainer.AddComponent<RectTransform>();
            btnContRT.anchorMin = new Vector2(0.5f, 0.5f);
            btnContRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnContRT.pivot = new Vector2(0.5f, 0.5f);
            btnContRT.anchoredPosition = new Vector2(px, py);
            btnContRT.sizeDelta = new Vector2(45f, 45f);

            GameObject padBg = CreateCircleImageGO(btnContainer, "PadBG", 40f, new Color(0.18f, 0.2f, 0.28f, 0.9f));
            padBg.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            dialPadButtons[i] = padBg.GetComponent<Image>();

            CreateText(btnContainer, "KeyLabel", padLabels[i], 16, FontStyles.Bold, new Vector2(0f, 0f), Color.white);
        }

        // ── TOMBOL KONTROL BAWAH (Mute, Call, End) — Berjejer Horisontal Rapi ──
        float bottomRowY = -200f; // Koordinat Y untuk seluruh baris tombol bawah

        // 1. Tombol Mute (Kiri)
        GameObject muteBtnContainer = new GameObject("MuteBtn");
        muteBtnContainer.transform.SetParent(phoneScreen.transform, false);
        RectTransform muteContRT = muteBtnContainer.AddComponent<RectTransform>();
        muteContRT.anchorMin = new Vector2(0.5f, 0.5f);
        muteContRT.anchorMax = new Vector2(0.5f, 0.5f);
        muteContRT.pivot = new Vector2(0.5f, 0.5f);
        muteContRT.anchoredPosition = new Vector2(-75f, bottomRowY); // Di sebelah kiri tombol call
        muteContRT.sizeDelta = new Vector2(46f, 46f);

        GameObject muteCircle = CreateCircleImageGO(muteBtnContainer, "MuteCircle", 44f, new Color(0.22f, 0.24f, 0.33f, 1f));
        muteCircle.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        muteIconText = CreateText(muteBtnContainer, "MuteIconText", "M", 16, FontStyles.Bold, new Vector2(0f, 1f), Color.white);

        GameObject muteIconGO = CreateCircleImageGO(muteBtnContainer, "MuteIconImg", 24f, Color.white);
        muteIconGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 1f);
        muteIconImage = muteIconGO.GetComponent<Image>();
        muteIconImage.preserveAspect = true;
        muteIconImage.gameObject.SetActive(false);

        // 2. Tombol Call (Tengah - Hijau Besar)
        GameObject callBtnContainer = new GameObject("CallBtn");
        callBtnContainer.transform.SetParent(phoneScreen.transform, false);
        RectTransform callContRT = callBtnContainer.AddComponent<RectTransform>();
        callContRT.anchorMin = new Vector2(0.5f, 0.5f);
        callContRT.anchorMax = new Vector2(0.5f, 0.5f);
        callContRT.pivot = new Vector2(0.5f, 0.5f);
        callContRT.anchoredPosition = new Vector2(0f, bottomRowY); // Berada tepat di tengah
        callContRT.sizeDelta = new Vector2(52f, 52f);

        GameObject callCircle = CreateCircleImageGO(callBtnContainer, "CallCircle", 50f, new Color(0.1f, 0.78f, 0.3f, 1f));
        callCircle.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        dialPadButtons[padLabels.Length] = callCircle.GetComponent<Image>();

        callIconText = CreateText(callBtnContainer, "CallIconText", "\u260E", 24, FontStyles.Bold, new Vector2(0f, 1f), Color.white);

        GameObject callIconGO = CreateCircleImageGO(callBtnContainer, "CallIconImg", 30f, Color.white);
        callIconGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 1f);
        callIconImage = callIconGO.GetComponent<Image>();
        callIconImage.preserveAspect = true;
        callIconImage.gameObject.SetActive(false);

        // 3. Tombol End/Hangup (Kanan - Merah)
        GameObject endBtnContainer = new GameObject("EndBtn");
        endBtnContainer.transform.SetParent(phoneScreen.transform, false);
        RectTransform endContRT = endBtnContainer.AddComponent<RectTransform>();
        endContRT.anchorMin = new Vector2(0.5f, 0.5f);
        endContRT.anchorMax = new Vector2(0.5f, 0.5f);
        endContRT.pivot = new Vector2(0.5f, 0.5f);
        endContRT.anchoredPosition = new Vector2(75f, bottomRowY); // Di sebelah kanan tombol call
        endContRT.sizeDelta = new Vector2(46f, 46f);

        GameObject endCircle = CreateCircleImageGO(endBtnContainer, "EndCircle", 44f, new Color(0.85f, 0.12f, 0.12f, 1f));
        endCircle.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        endIconText = CreateText(endBtnContainer, "EndIconText", "✕", 16, FontStyles.Bold, new Vector2(0f, 1f), Color.white);

        GameObject endIconGO = CreateCircleImageGO(endBtnContainer, "EndIconImg", 24f, Color.white);
        endIconGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 1f);
        endIconImage = endIconGO.GetComponent<Image>();
        endIconImage.preserveAspect = true;
        endIconImage.gameObject.SetActive(false);

        // Wallpaper & Home Bar
        phoneScreenBgImage = phoneScreen.GetComponent<Image>();
        GameObject homeBar = CreateRoundedPanel(phoneScreen, "HomeBar", new Vector2(90f, 4f),
            new Color(0.5f, 0.5f, 0.6f, 0.5f), new Vector2(0f, -271f));
    }

    // ─── Victory Grade Box ───
    private void BuildVictoryPanel()
    {
        victoryPanel = CreateRoundedPanel(mainCanvas.gameObject, "VictoryPanel",
            new Vector2(560f, 420f), new Color(0.04f, 0.06f, 0.10f, 0.97f), Vector2.zero);

        // Top accent line
        CreateRoundedPanel(victoryPanel, "AccentLine", new Vector2(90f, 4f),
            new Color(1f, 0.84f, 0f, 0.8f), new Vector2(0f, 185f));

        victoryTitleText = CreateText(victoryPanel, "VicTitle", "SIMULASI SELESAI!", 28,
            FontStyles.Bold, new Vector2(0f, 158f), Color.yellow);
        victoryTimeText = CreateText(victoryPanel, "VicTime", "Waktu Pemadaman: 00:00.0s", 18,
            FontStyles.Normal, new Vector2(0f, 118f), new Color(0.85f, 0.9f, 1f));

        // Grade circle
        GameObject gradeCircle = CreateCircleImageGO(victoryPanel, "GradeCircle", 110f,
            new Color(0.08f, 0.10f, 0.18f, 1f));
        gradeCircle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 22f);

        // Grade border ring
        GameObject gradeBorderRing = CreateCircleImageGO(victoryPanel, "GradeBorder", 118f,
            new Color(1f, 0.84f, 0f, 0.3f));
        gradeBorderRing.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 22f);

        victoryGradeBadgeText = CreateText(gradeCircle, "GradeBadge", "S", 68, FontStyles.Bold,
            new Vector2(0f, 5f), new Color(1f, 0.84f, 0f));

        victoryDescriptionText = CreateText(victoryPanel, "VicDesc", "Deskripsi hasil...", 14,
            FontStyles.Italic, new Vector2(0f, -78f), new Color(0.82f, 0.88f, 1f));
        victoryDescriptionText.GetComponent<RectTransform>().sizeDelta = new Vector2(510f, 60f);

        // Restart button (circle-rounded)
        GameObject restartGO = new GameObject("RestartBtn");
        restartGO.transform.SetParent(victoryPanel.transform, false);
        RectTransform restartRT = restartGO.AddComponent<RectTransform>();
        restartRT.anchorMin = new Vector2(0.5f, 0.5f);
        restartRT.anchorMax = new Vector2(0.5f, 0.5f);
        restartRT.pivot = new Vector2(0.5f, 0.5f);
        restartRT.anchoredPosition = new Vector2(0f, -148f);
        restartRT.sizeDelta = new Vector2(220f, 46f);

        Image restartImg = restartGO.AddComponent<Image>();
        restartImg.color = new Color(0.1f, 0.6f, 0.3f, 1f);

        Button restartBtn = restartGO.AddComponent<Button>();
        restartBtn.onClick.AddListener(RestartSimulation);

        CreateText(restartGO, "RestartLabel", "ULANGI SIMULASI", 16, FontStyles.Bold,
            Vector2.zero, Color.white);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  HELPER UI CREATORS
    // ═══════════════════════════════════════════════════════════════════════

    private GameObject CreateRoundedPanel(GameObject parent, string name, Vector2 size, Color bgClr, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        Image img = go.AddComponent<Image>();
        img.color = bgClr;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        return go;
    }

    private GameObject CreateCircleImageGO(GameObject parent, string name, float size, Color clr)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        Image img = go.AddComponent<Image>();
        img.color = clr;
        img.sprite = circleSprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);

        return go;
    }

    private Image CreateCircleImage(GameObject parent, string name, float size, Color clr, Vector2 pos)
    {
        GameObject go = CreateCircleImageGO(parent, name, size, clr);
        go.GetComponent<RectTransform>().anchoredPosition = pos;
        return go.GetComponent<Image>();
    }

    private TextMeshProUGUI CreateText(GameObject parent, string name, string textStr, float fontSize,
        FontStyles style, Vector2 pos, Color clr)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = textStr;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = clr;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;

        RectTransform parentRT = parent.GetComponent<RectTransform>();
        float parentW = parentRT != null ? parentRT.sizeDelta.x : 400f;
        rt.sizeDelta = new Vector2(parentW - 14f, fontSize + 22f);

        return tmp;
    }
}

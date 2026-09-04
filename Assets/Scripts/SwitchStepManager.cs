using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Mengelola urutan langkah: Matikan Saklar MCB dahulu, baru APAR bisa digunakan.
/// Tampilkan peringatan UI jika pemain mencoba grab APAR sebelum saklar dimatikan.
/// </summary>
public class SwitchStepManager : MonoBehaviour
{
    public static SwitchStepManager Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    public enum SwitchStep { WaitingForSwitch, SwitchDone }
    public SwitchStep CurrentStep { get; private set; } = SwitchStep.WaitingForSwitch;
    public bool IsSwitchDone => CurrentStep == SwitchStep.SwitchDone;

    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Referensi")]
    [Tooltip("Script ElectricalSwitch pada MCB di scene")]
    public ElectricalSwitch electricalSwitch;

    [Tooltip("Semua XRGrabInteractable APAR yang harus dikunci sebelum saklar dimatikan")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable[] aparInteractables;

    [Header("Warning UI")]
    [Tooltip("Panel peringatan dunia VR yang muncul jika grab APAR terlalu awal")]
    public GameObject warningPanel;
    [Tooltip("Durasi panel peringatan tampil (detik)")]
    public float warningDuration = 3.0f;

    [Header("Step Guide UI")]
    [Tooltip("Panel panduan langkah 1: Matikan Saklar. Tampil saat misi mulai.")]
    public GameObject stepGuidePanel;

    // ── Private ────────────────────────────────────────────────────────────────
    private bool isActive = false;
    private Coroutine hideWarningCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // Auto-find jika belum di-assign
        if (electricalSwitch == null)
            electricalSwitch = FindFirstObjectByType<ElectricalSwitch>();

        if (aparInteractables == null || aparInteractables.Length == 0)
        {
            var aparFull = GameObject.Find("APAR Full");
            if (aparFull != null)
            {
                var grab = aparFull.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                if (grab != null)
                    aparInteractables = new UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable[] { grab };
            }
        }

        if (warningPanel != null)
            warningPanel.SetActive(false);
        if (stepGuidePanel != null)
            stepGuidePanel.SetActive(false);
    }

    private void Update()
    {
        // Proximity check dihapus:
        // Peringatan hanya muncul ketika pemain mencoba melakukan grab pada APAR sebelum saklar MCB mati.
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Dipanggil oleh VRSimulationUIManager.StartActiveMission()</summary>
    public void ActivateSwitchStep()
    {
        isActive = true;
        CurrentStep = SwitchStep.WaitingForSwitch;

        // Aktifkan MCB agar bisa diinteraksi
        if (electricalSwitch != null)
            electricalSwitch.SetMissionStarted();

        // Daftarkan event dari MCB
        if (electricalSwitch != null)
            electricalSwitch.OnSwitchTurnedOff.AddListener(OnSwitchSuccessfullyOff);

        // Aktifkan grab interactable APAR agar event percobaan grab bisa ditangkap dan diberi warning
        var ext = FindFirstObjectByType<AutoFireExtinguisher>();
        if (ext != null)
            ext.SetMissionStarted();

        // Tampilkan panduan langkah 1
        if (stepGuidePanel != null)
        {
            stepGuidePanel.SetActive(true);
            ShowSOPGuide();
        }

        Debug.Log("[SwitchStepManager] Langkah 1 aktif: Tunggu pemain matikan MCB.");
    }

    /// <summary>Dipanggil oleh ElectricalSwitch.OnSwitchTurnedOff</summary>
    public void OnSwitchSuccessfullyOff()
    {
        if (!isActive) return;
        CurrentStep = SwitchStep.SwitchDone;

        // Sembunyikan warning dan step guide
        HideWarning();
        if (stepGuidePanel != null)
            stepGuidePanel.SetActive(false);

        // Beritahu VRSimulationUIManager agar unlock penuh
        if (VRSimulationUIManager.Instance != null)
            VRSimulationUIManager.Instance.OnSwitchStepCompleted();

        Debug.Log("[SwitchStepManager] MCB sudah OFF. APAR sekarang bisa digunakan!");
    }

    /// <summary>Dipanggil saat pemain mencoba grab APAR tapi saklar belum dimatikan</summary>
    public void OnAPARAttemptedBeforeSwitch()
    {
        if (!isActive || IsSwitchDone) return;
        ShowWarning();
    }

    private void OnEnable()
    {
        VRLanguageManager.OnLanguageChanged += OnLanguageChangedHandler;
    }

    private void OnDisable()
    {
        VRLanguageManager.OnLanguageChanged -= OnLanguageChangedHandler;
    }

    private void OnLanguageChangedHandler(AppLanguage newLang)
    {
        if (CurrentStep == SwitchStep.WaitingForSwitch && stepGuidePanel != null && stepGuidePanel.activeSelf)
        {
            if (hideWarningCoroutine != null)
                ShowWarning();
            else
                ShowSOPGuide();
        }
    }

    // ── Smart Dynamic Panel Transition ───────────────────────────────────────
    private void ShowWarning()
    {
        if (stepGuidePanel == null) return;
        stepGuidePanel.SetActive(true);

        bool isEnglish = VRLanguageManager.IsEnglish;

        // Transformasi visual panel ke mode PERINGATAN MERAH (Alert Mode)
        SetPanelContent(
            bgColor: new Color(0.22f, 0.03f, 0.03f, 0.96f), // Deep Crimson Glass
            accentColor: new Color(1f, 0.25f, 0.25f, 1f),    // Bright Red
            badgeText: isEnglish ? "[!] SAFETY WARNING | ELECTRICAL SHOCK HAZARD" : "[!] PERINGATAN KESELAMATAN | BAHAYA SENGATAN LISTRIK",
            badgeColor: new Color(1f, 0.35f, 0.35f, 1f),
            titleText: isEnglish ? "Turn Off Electric Switch (MCB) First!" : "Matikan Saklar Listrik (MCB) Dulu!",
            descText: "",
            footerText: isEnglish 
                ? "> Flip down the MCB switch lever on the wall to the right of the extinguisher" 
                : "> Ceklek tuas saklar MCB di dinding sebelah kanan tabung APAR"
        );

        if (hideWarningCoroutine != null)
            StopCoroutine(hideWarningCoroutine);
        hideWarningCoroutine = StartCoroutine(RestoreGuidePanelAfterDelay(3.5f));
    }

    private void ShowSOPGuide()
    {
        if (stepGuidePanel == null) return;
        bool isEnglish = VRLanguageManager.IsEnglish;

        SetPanelContent(
            bgColor: new Color(0.05f, 0.07f, 0.12f, 0.96f), // Dark Slate Glass
            accentColor: new Color(1.0f, 0.60f, 0.10f, 1.0f), // Gold / Amber
            badgeText: isEnglish ? "BPBD SAFETY PROCEDURE | CLASS C FIRE" : "PROSEDUR KESELAMATAN BPBD | KEBAKARAN KELAS C",
            badgeColor: new Color(1.0f, 0.68f, 0.18f, 1.0f),
            titleText: isEnglish ? "Cut Off Electrical Power (MCB)" : "Putuskan Aliran Listrik (MCB)",
            descText: "",
            footerText: isEnglish ? "> Click / touch the MCB switch lever on the right wall" : "> Klik / sentuh tuas saklar MCB di dinding sebelah kanan"
        );
    }

    private void HideWarning()
    {
        if (hideWarningCoroutine != null)
        {
            StopCoroutine(hideWarningCoroutine);
            hideWarningCoroutine = null;
        }
    }

    private IEnumerator RestoreGuidePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Jika saklar belum dimatikan, kembalikan tampilan ke mode SOP Emas/Biru yang elegan
        if (CurrentStep == SwitchStep.WaitingForSwitch && stepGuidePanel != null && stepGuidePanel.activeSelf)
        {
            ShowSOPGuide();
        }

        hideWarningCoroutine = null;
    }

    private void SetPanelContent(Color bgColor, Color accentColor, string badgeText, Color badgeColor, string titleText, string descText, string footerText)
    {
        if (stepGuidePanel == null) return;

        var bgImg = stepGuidePanel.transform.Find("Guide_BG")?.GetComponent<UnityEngine.UI.Image>();
        if (bgImg != null) bgImg.color = bgColor;

        var accentImg = stepGuidePanel.transform.Find("Top_Accent")?.GetComponent<UnityEngine.UI.Image>();
        if (accentImg != null) accentImg.color = accentColor;

        var bText = stepGuidePanel.transform.Find("SOP_Badge")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (bText != null) { bText.text = badgeText; bText.color = badgeColor; }

        var tText = stepGuidePanel.transform.Find("Title_Text")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (tText != null) tText.text = titleText;

        var dText = stepGuidePanel.transform.Find("Desc_Text")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (dText != null)
        {
            dText.text = descText;
            dText.gameObject.SetActive(!string.IsNullOrEmpty(descText));
        }

        var fText = stepGuidePanel.transform.Find("Footer_Hint")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (fText != null) fText.text = footerText;
    }
}

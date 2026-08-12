using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Indikator UI dunia nyata (World Space) yang mengarahkan pemain untuk mencabut pin APAR.
/// 
/// CARA SETUP DI UNITY:
/// 1. Attach script ini ke GameObject APAR (parent yang punya AutoFireExtinguisher)
/// 2. Assign 'mainExtinguisher' ke script AutoFireExtinguisher di APAR body
/// 3. Assign 'pinTransform' ke Transform GameObject pin APAR
///
/// Indikator akan:
/// - Muncul di atas pin dengan animasi bob + pulse
/// - Menampilkan panah ▼ dan teks "CABUT PIN DULU!"
/// - Hilang otomatis saat pin berhasil dicabut (pinPulled = true)
/// </summary>
public class APARPinIndicator : MonoBehaviour
{
    [Header("Referensi (Wajib Diisi)")]
    [Tooltip("Drag AutoFireExtinguisher dari APAR body ke sini")]
    public AutoFireExtinguisher mainExtinguisher;

    [Tooltip("Drag Transform dari GameObject Pin APAR ke sini")]
    public Transform pinTransform;

    [Header("Posisi & Ukuran Indikator")]
    [Tooltip("Ketinggian indikator di atas pin (meter)")]
    public float heightAbovePin = 0.25f;

    [Tooltip("Skala dunia untuk canvas (0.001 = kecil, 0.003 = besar)")]
    public float worldScale = 0.002f;

    [Header("Animasi")]
    [Tooltip("Kecepatan naik-turun")]
    public float bobSpeed = 2.5f;
    [Tooltip("Tinggi gerakan naik-turun (meter)")]
    public float bobHeight = 0.04f;
    [Tooltip("Kecepatan kedip/pulse")]
    public float pulseSpeed = 2.5f;

    [Header("Warna")]
    [Tooltip("Warna panah (kuning terang untuk visibilitas tinggi)")]
    public Color arrowColor = new Color(1f, 0.92f, 0.016f);
    [Tooltip("Warna teks label")]
    public Color labelColor = Color.white;
    [Tooltip("Warna background panel")]
    public Color bgColor = new Color(0.05f, 0.05f, 0.05f, 0.75f);

    // ── Internal ──────────────────────────────────────────────────────────
    private GameObject canvasGO;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI arrowText;
    private TextMeshProUGUI labelText;
    private TextMeshProUGUI subLabelText;
    private bool isHidden = false;
    private bool isCreated = false;

    // ═══════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════════

    private void Start()
    {
        // Cari referensi otomatis jika tidak di-assign
        if (mainExtinguisher == null)
            mainExtinguisher = GetComponent<AutoFireExtinguisher>();
        if (mainExtinguisher == null)
            mainExtinguisher = GetComponentInParent<AutoFireExtinguisher>();

        CreateIndicatorUI();
    }

    private void Update()
    {
        if (isHidden || !isCreated) return;

        // Cek pin sudah dicabut → sembunyikan indikator
        if (mainExtinguisher != null && mainExtinguisher.pinPulled)
        {
            HideIndicator();
            return;
        }

        // ── Posisi: ikuti pin + gerakan bob ────────────────────────────────
        if (pinTransform != null)
        {
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            canvasGO.transform.position = pinTransform.position + Vector3.up * (heightAbovePin + bobOffset);
        }

        // ── Billboard: selalu menghadap kamera ────────────────────────────
        Camera cam = Camera.main;
        if (cam != null)
        {
            canvasGO.transform.LookAt(cam.transform.position);
            canvasGO.transform.Rotate(0f, 180f, 0f);
        }

        // ── Pulse opacity ──────────────────────────────────────────────────
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.Lerp(0.55f, 1f, pulse);

        // ── Pulse warna panah ──────────────────────────────────────────────
        if (arrowText != null)
            arrowText.color = Color.Lerp(arrowColor, Color.white, pulse * 0.6f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  BUAT UI SECARA RUNTIME
    // ═══════════════════════════════════════════════════════════════════════

    private void CreateIndicatorUI()
    {
        // --- Canvas (World Space) ------------------------------------------
        canvasGO = new GameObject("APAR_PinIndicator");
        canvasGO.transform.SetParent(null); // Berdiri sendiri di scene

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(380f, 180f);
        canvasRT.localScale = Vector3.one * worldScale;

        // Posisi awal
        if (pinTransform != null)
            canvasGO.transform.position = pinTransform.position + Vector3.up * heightAbovePin;
        else
            canvasGO.transform.position = transform.position + Vector3.up * 0.5f;

        // --- Background panel ---------------------------------------------
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = bgColor;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Rounded corners (jika Unity 2021+)
        bgImg.pixelsPerUnitMultiplier = 10f;

        // --- Garis border kuning ------------------------------------------
        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(canvasGO.transform, false);
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(arrowColor.r, arrowColor.g, arrowColor.b, 0.9f);
        RectTransform borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-3, -3);
        borderRT.offsetMax = new Vector2(3, 3);
        borderImg.raycastTarget = false;
        borderGO.transform.SetSiblingIndex(0);

        // --- Teks panah ---------------------------------------------------
        GameObject arrowGO = new GameObject("ArrowText");
        arrowGO.transform.SetParent(canvasGO.transform, false);
        arrowText = arrowGO.AddComponent<TextMeshProUGUI>();
        arrowText.text = "▼";
        arrowText.fontSize = 80;
        arrowText.color = arrowColor;
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.fontStyle = FontStyles.Bold;
        RectTransform arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchoredPosition = new Vector2(0f, 30f);
        arrowRT.sizeDelta = new Vector2(380f, 100f);

        // --- Label utama --------------------------------------------------
        GameObject labelGO = new GameObject("LabelText");
        labelGO.transform.SetParent(canvasGO.transform, false);
        labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = "CABUT PIN DULU!";
        labelText.fontSize = 34;
        labelText.color = labelColor;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchoredPosition = new Vector2(0f, -35f);
        labelRT.sizeDelta = new Vector2(380f, 50f);

        // --- Sub-label instruksi ------------------------------------------
        GameObject subGO = new GameObject("SubLabel");
        subGO.transform.SetParent(canvasGO.transform, false);
        subLabelText = subGO.AddComponent<TextMeshProUGUI>();
        subLabelText.text = "Grip pin → tahan → tarik keluar";
        subLabelText.fontSize = 22;
        subLabelText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        subLabelText.alignment = TextAlignmentOptions.Center;
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchoredPosition = new Vector2(0f, -68f);
        subRT.sizeDelta = new Vector2(380f, 40f);

        isCreated = true;
        Debug.Log("[APARPinIndicator] Indikator pin berhasil dibuat.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SEMBUNYIKAN INDIKATOR
    // ═══════════════════════════════════════════════════════════════════════

    private void HideIndicator()
    {
        isHidden = true;
        if (canvasGO != null)
        {
            // Fade out lalu destroy
            canvasGO.SetActive(false);
            Debug.Log("[APARPinIndicator] ✅ Pin dicabut — indikator disembunyikan!");
        }
    }

    private void OnDestroy()
    {
        // Cleanup agar tidak ada canvas yang tersisa di scene
        if (canvasGO != null)
            Destroy(canvasGO);
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Animasi Guide & Petunjuk Visual VR untuk Cabut Pin APAR.
/// 
/// FUNGSI:
///   1. Meng-highlight posisi Pin (`wire_ring_low`) dengan efek animasi pulsa glowing.
///   2. Menampilkan UI World-Space melayang di atas pin: "CABUT PIN DULU ➔".
///   3. Membuat animasi gerak panah tarik (pull animation) yang menunjuk arah cabut pin.
///   4. Otomatis HILANG saat pin berhasil dicabut!
/// </summary>
public class APARPinGuideAnimation : MonoBehaviour
{
    [Header("Referensi (Auto-Find jika kosong)")]
    [Tooltip("Transform dari Ring / Pin APAR (misal: wire_ring_low)")]
    public Transform pinTransform;

    [Tooltip("Script AutoFireExtinguisher utama")]
    public AutoFireExtinguisher mainExtinguisher;

    [Header("Pengaturan Animasi Teks UI")]
    public float uiHeightAbovePin = 0.28f;
    public float uiScale = 0.0018f;
    public Color guideColor = new Color(1f, 0.85f, 0.1f); // Kuning terang glowing

    // ── Private Cache ──────────────────────────────────────────────────────
    private GameObject guideCanvasGO;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI mainLabelText;
    private TextMeshProUGUI arrowAnimText;
    private Camera mainCamera;
    private bool isGuideActive = true;
    private Vector3 initialPinScale;

    private void Start()
    {
        // 1. Auto-find AutoFireExtinguisher
        if (mainExtinguisher == null)
            mainExtinguisher = GetComponent<AutoFireExtinguisher>();
        if (mainExtinguisher == null)
            mainExtinguisher = GetComponentInParent<AutoFireExtinguisher>();

        // 2. Auto-find Pin Transform (wire_ring_low) jika belum di-assign
        if (pinTransform == null)
        {
            Transform foundRing = transform.Find("wire_ring_low");
            if (foundRing == null) foundRing = transform.Find("wire_p1_low");
            if (foundRing != null) pinTransform = foundRing;
        }

        if (pinTransform != null)
            initialPinScale = pinTransform.localScale;

        mainCamera = Camera.main;

        // 3. Buat UI Guide
        CreateGuideUI();
    }

    private void Update()
    {
        if (!isGuideActive) return;

        // Jika pin sudah dicabut ➔ HENTIKAN & SEMBUNYIKAN ANIMASI
        if (mainExtinguisher != null && mainExtinguisher.pinPulled)
        {
            HideGuide();
            return;
        }

        if (mainCamera == null) mainCamera = Camera.main;

        // ── A. Animasi Pulsa Glowing pada Pin (wire_ring_low) ───────────────
        if (pinTransform != null)
        {
            float pulseScale = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
            pinTransform.localScale = initialPinScale * pulseScale;
        }

        // ── B. Update Posisi UI Melayang + Billboard (Hadap Kamera VR) ──────
        if (guideCanvasGO != null)
        {
            Vector3 targetPos = (pinTransform != null) ? pinTransform.position : transform.position;
            float bobbing = Mathf.Sin(Time.time * 3f) * 0.02f;
            guideCanvasGO.transform.position = targetPos + Vector3.up * (uiHeightAbovePin + bobbing);

            if (mainCamera != null)
            {
                guideCanvasGO.transform.LookAt(mainCamera.transform.position);
                guideCanvasGO.transform.Rotate(0f, 180f, 0f);
            }

            // Opacity pulsing
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0.6f, 1.0f, (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f);
        }

        // ── C. Animasi Panah Tarik (Pull Arrow Animation) ───────────────────
        if (arrowAnimText != null)
        {
            int step = (int)(Time.time * 4f) % 4;
            switch (step)
            {
                case 0: arrowAnimText.text = "> >"; break;
                case 1: arrowAnimText.text = "> > >"; break;
                case 2: arrowAnimText.text = "> > > >"; break;
                case 3: arrowAnimText.text = ">"; break;
            }
        }
    }

    private void CreateGuideUI()
    {
        guideCanvasGO = new GameObject("APAR_Pin_Guide_UI");
        guideCanvasGO.transform.SetParent(null);

        Canvas canvas = guideCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        canvasGroup = guideCanvasGO.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform canvasRT = guideCanvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(400f, 160f);
        canvasRT.localScale = Vector3.one * uiScale;

        // Background Panel Glowing
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(guideCanvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Border Glowing Kuning
        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(guideCanvasGO.transform, false);
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = guideColor;
        RectTransform borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-4, -4);
        borderRT.offsetMax = new Vector2(4, 4);
        borderGO.transform.SetSiblingIndex(0);

        // Teks Utama: "CABUT PIN DULU!"
        GameObject labelGO = new GameObject("MainLabel");
        labelGO.transform.SetParent(guideCanvasGO.transform, false);
        mainLabelText = labelGO.AddComponent<TextMeshProUGUI>();
        mainLabelText.text = "CABUT PIN DULU!";
        mainLabelText.fontSize = 36;
        mainLabelText.color = guideColor;
        mainLabelText.fontStyle = FontStyles.Bold;
        mainLabelText.alignment = TextAlignmentOptions.Center;
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchoredPosition = new Vector2(0f, 20f);
        labelRT.sizeDelta = new Vector2(380f, 60f);

        // Teks Animasi Panah Tarik (Gunakan ASCII >>> agar kompatibel dengan Font TextMeshPro)
        GameObject arrowGO = new GameObject("ArrowAnim");
        arrowGO.transform.SetParent(guideCanvasGO.transform, false);
        arrowAnimText = arrowGO.AddComponent<TextMeshProUGUI>();
        arrowAnimText.text = ">>>";
        arrowAnimText.fontSize = 42;
        arrowAnimText.color = Color.white;
        arrowAnimText.fontStyle = FontStyles.Bold;
        arrowAnimText.alignment = TextAlignmentOptions.Center;
        RectTransform arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchoredPosition = new Vector2(0f, -35f);
        arrowRT.sizeDelta = new Vector2(380f, 50f);
    }

    private void HideGuide()
    {
        isGuideActive = false;

        // Reset ukuran pin ke semula
        if (pinTransform != null)
            pinTransform.localScale = initialPinScale;

        // Hapus UI Guide
        if (guideCanvasGO != null)
            Destroy(guideCanvasGO);

        Debug.Log("[APARPinGuide] ✅ Pin berhasil dicabut! Petunjuk animasi selesai.");
    }

    private void OnDestroy()
    {
        if (guideCanvasGO != null)
            Destroy(guideCanvasGO);
    }
}

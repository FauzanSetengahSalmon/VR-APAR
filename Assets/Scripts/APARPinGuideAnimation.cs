using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class APARPinGuideAnimation : MonoBehaviour
{
    [Header("Referensi APAR & Pin")]
    public Transform pinTransform;
    public AutoFireExtinguisher mainExtinguisher;
    public XRGrabInteractable grabInteractable;

    [Header("Gambar Controller Asli (Optional)")]
    [Tooltip("Masukkan File Gambar/Sprite Controller Meta Quest 3 kamu di sini")]
    public Sprite controllerRealSprite;

    [Header("Pengaturan UI VR")]
    public float uiHeightAbovePin = 0.35f;
    public float uiScale = 0.001f;

    // Private Cache
    private GameObject guideCanvasGO;
    private TextMeshProUGUI mainLabelText;
    private TextMeshProUGUI subLabelText;
    private Image controllerImageUI;
    private Camera mainCamera;
    private bool isGuideActive = true;
    private bool isAPARGrabbed = false;
    private Vector3 initialPinScale;

    private void Start()
    {
        // Auto-find components
        if (mainExtinguisher == null) mainExtinguisher = GetComponentInParent<AutoFireExtinguisher>();
        if (grabInteractable == null) grabInteractable = GetComponentInParent<XRGrabInteractable>();

        if (pinTransform == null && mainExtinguisher != null)
        {
            Transform foundRing = mainExtinguisher.transform.Find("wire_ring_low");
            if (foundRing != null) pinTransform = foundRing;
        }

        if (pinTransform != null) initialPinScale = pinTransform.localScale;
        mainCamera = Camera.main;

        // Pasang Event Detector dari XR Grab Interactable
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnAPARGrabbed);
            grabInteractable.selectExited.AddListener(OnAPARReleased);
        }

        CreateCleanGuideUI();
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnAPARGrabbed);
            grabInteractable.selectExited.RemoveListener(OnAPARReleased);
        }
        if (guideCanvasGO != null) Destroy(guideCanvasGO);
    }

    // Fungsi yang OTOMATIS dipanggil saat tangan VR memegang APAR
    private void OnAPARGrabbed(SelectEnterEventArgs args)
    {
        isAPARGrabbed = true;
        UpdateUIState();
    }

    // Fungsi dipanggil saat APAR dilepas
    private void OnAPARReleased(SelectExitEventArgs args)
    {
        isAPARGrabbed = false;
        UpdateUIState();
    }

    private void Update()
    {
        if (!isGuideActive) return;

        // Jika Pin sudah dicabut ➔ Hapus UI
        if (mainExtinguisher != null && mainExtinguisher.pinPulled)
        {
            HideGuide();
            return;
        }

        if (mainCamera == null) mainCamera = Camera.main;

        // Efek Pulsa Pin jika APAR sudah dipegang
        if (isAPARGrabbed && pinTransform != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.08f;
            pinTransform.localScale = initialPinScale * pulse;
        }

        // Billboard UI (Selalu Menghadap Kamera VR)
        if (guideCanvasGO != null)
        {
            Vector3 targetPos = (pinTransform != null) ? pinTransform.position : transform.position;
            float floatEffect = Mathf.Sin(Time.time * 2.5f) * 0.012f;
            guideCanvasGO.transform.position = targetPos + Vector3.up * (uiHeightAbovePin + floatEffect);

            if (mainCamera != null)
            {
                guideCanvasGO.transform.LookAt(mainCamera.transform.position);
                guideCanvasGO.transform.Rotate(0f, 180f, 0f);
            }
        }
    }

    private void UpdateUIState()
    {
        if (!isAPARGrabbed)
        {
            // TAHAP 1
            if (mainLabelText != null) mainLabelText.text = "AMBIL APAR";
            if (subLabelText != null) subLabelText.text = "Tekan & Tahan tombol <color=#00DCFF>GRIP</color> Samping";
        }
        else
        {
            // TAHAP 2
            if (mainLabelText != null) mainLabelText.text = "TARIK PIN APAR";
            if (subLabelText != null) subLabelText.text = "Tekan <color=#00DCFF>TRIGGER</color> & Tarik Pin";
        }
    }

    private void CreateCleanGuideUI()
    {
        guideCanvasGO = new GameObject("APAR_Guide_UI");
        guideCanvasGO.transform.SetParent(null);

        Canvas canvas = guideCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        CanvasGroup cg = guideCanvasGO.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;

        RectTransform canvasRT = guideCanvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(480f, 160f);
        canvasRT.localScale = Vector3.one * uiScale;

        // Background Dark Glass Panel
        GameObject bgGO = new GameObject("CardBG");
        bgGO.transform.SetParent(guideCanvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.07f, 0.1f, 0.9f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // Container Gambar Controller Real
        GameObject imgGO = new GameObject("ControllerImage");
        imgGO.transform.SetParent(guideCanvasGO.transform, false);
        controllerImageUI = imgGO.AddComponent<Image>();
        if (controllerRealSprite != null) controllerImageUI.sprite = controllerRealSprite;
        else controllerImageUI.color = new Color(0.2f, 0.2f, 0.25f);

        RectTransform imgRT = imgGO.GetComponent<RectTransform>();
        imgRT.anchoredPosition = new Vector2(-150f, 0f);
        imgRT.sizeDelta = new Vector2(110f, 110f);

        // Container Teks
        GameObject textContainer = new GameObject("TextGroup");
        textContainer.transform.SetParent(guideCanvasGO.transform, false);
        RectTransform textContainerRT = textContainer.AddComponent<RectTransform>();
        textContainerRT.anchoredPosition = new Vector2(40f, 0f);
        textContainerRT.sizeDelta = new Vector2(300f, 120f);

        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(textContainer.transform, false);
        mainLabelText = titleGO.AddComponent<TextMeshProUGUI>();
        mainLabelText.fontSize = 30;
        mainLabelText.color = Color.white;
        mainLabelText.fontStyle = FontStyles.Bold;
        mainLabelText.alignment = TextAlignmentOptions.Left;
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchoredPosition = new Vector2(0f, 20f);
        titleRT.sizeDelta = new Vector2(300f, 45f);

        GameObject subGO = new GameObject("SubText");
        subGO.transform.SetParent(textContainer.transform, false);
        subLabelText = subGO.AddComponent<TextMeshProUGUI>();
        subLabelText.fontSize = 18;
        subLabelText.color = new Color(0.85f, 0.88f, 0.92f);
        subLabelText.alignment = TextAlignmentOptions.Left;
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchoredPosition = new Vector2(0f, -22f);
        subRT.sizeDelta = new Vector2(300f, 50f);

        UpdateUIState();
    }

    private void HideGuide()
    {
        isGuideActive = false;
        if (pinTransform != null) pinTransform.localScale = initialPinScale;
        if (guideCanvasGO != null) Destroy(guideCanvasGO);
    }
}
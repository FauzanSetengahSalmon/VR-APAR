using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class APARPinGuideAnimation : MonoBehaviour
{
    // ============================================================
    // REFERENSI APAR & PIN
    // ============================================================

    [Header("Referensi APAR & Pin")]

    [Tooltip("Transform pin APAR yang akan diberi efek pulsa.")]
    public Transform pinTransform;

    [Tooltip("Komponen AutoFireExtinguisher pada APAR.")]
    public AutoFireExtinguisher mainExtinguisher;

    [Tooltip("XR Grab Interactable dari APAR.")]
    public XRGrabInteractable grabInteractable;


    // ============================================================
    // POSTER ENVIRONMENT
    // ============================================================

    [Header("Referensi Poster Environment")]

    [Tooltip("Masukkan GameObject APAR_Safety_Poster dari Hierarchy.")]
    public GameObject safetyPoster;


    // ============================================================
    // GAMBAR PANDUAN
    // ============================================================

    [Header("Gambar Panduan Tiap Tahap")]

    [Tooltip("Gambar/foto khusus untuk tahap AMBIL APAR.")]
    public Sprite ambilAPARSpr;

    [Tooltip("Gambar/foto khusus untuk tahap TARIK PIN APAR.")]
    public Sprite tarikPinSprite;


    // ============================================================
    // PENGATURAN UI VR
    // ============================================================

    [Header("Pengaturan UI VR")]

    [Tooltip("Jarak UI dari posisi pin APAR.")]
    public float uiHeightAbovePin = 0.65f;

    [Tooltip("Ukuran Canvas World Space.")]
    public float uiScale = 0.001f;


    // ============================================================
    // PRIVATE CACHE
    // ============================================================

    private GameObject guideCanvasGO;

    private TextMeshProUGUI mainLabelText;
    private TextMeshProUGUI subLabelText;

    private Image guideImageUI;

    private Camera mainCamera;

    private bool isGuideActive = true;
    private bool isAPARGrabbed = false;

    private Vector3 initialPinScale;


    // ============================================================
    // MISSION LOCK
    // ============================================================

    // APAR dan UI tidak aktif sebelum misi dimulai.
    private bool isMissionStarted = false;


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        // Pastikan UI tidak terlalu rendah.
        if (uiHeightAbovePin < 0.65f)
        {
            uiHeightAbovePin = 0.65f;
        }


        // --------------------------------------------------------
        // AUTO FIND COMPONENT
        // --------------------------------------------------------

        if (mainExtinguisher == null)
        {
            mainExtinguisher =
                GetComponentInParent<AutoFireExtinguisher>();
        }

        if (grabInteractable == null)
        {
            grabInteractable =
                GetComponentInParent<XRGrabInteractable>();
        }


        // --------------------------------------------------------
        // AUTO FIND PIN
        // --------------------------------------------------------

        if (pinTransform == null && mainExtinguisher != null)
        {
            Transform foundRing =
                mainExtinguisher.transform.Find("wire_ring_low");

            if (foundRing != null)
            {
                pinTransform = foundRing;
            }
        }


        // Simpan scale awal pin.
        if (pinTransform != null)
        {
            initialPinScale = pinTransform.localScale;
        }


        // Cari kamera VR.
        mainCamera = Camera.main;


        // --------------------------------------------------------
        // XR GRAB EVENTS
        // --------------------------------------------------------

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(
                OnAPARGrabbed
            );

            grabInteractable.selectExited.AddListener(
                OnAPARReleased
            );
        }


        // --------------------------------------------------------
        // BUAT UI
        // --------------------------------------------------------

        CreateCleanGuideUI();


        // --------------------------------------------------------
        // SEMBUNYIKAN POSTER
        // --------------------------------------------------------

        if (safetyPoster != null)
        {
            safetyPoster.SetActive(false);
        }
    }


    // ============================================================
    // ON DESTROY
    // ============================================================

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(
                OnAPARGrabbed
            );

            grabInteractable.selectExited.RemoveListener(
                OnAPARReleased
            );
        }

        if (guideCanvasGO != null)
        {
            Destroy(guideCanvasGO);
        }
    }


    // ============================================================
    // APAR DIAMBIL
    // ============================================================

    private void OnAPARGrabbed(SelectEnterEventArgs args)
    {
        // Jangan izinkan interaksi sebelum misi dimulai.
        if (!isMissionStarted)
        {
            Debug.Log(
                "[APARPinGuide] Grab APAR diblokir - misi belum dimulai."
            );

            return;
        }


        // Tandai APAR sedang dipegang.
        isAPARGrabbed = true;


        // Update teks + gambar menjadi tahap TARIK PIN.
        UpdateUIState();


        // Mulai penghitungan waktu misi
        if (VRSimulationUIManager.Instance != null)
        {
            VRSimulationUIManager.Instance.StartMissionTimer();
        }

        Debug.Log("[APARPinGuide] APAR diambil, poster panduan tetap tampil.");
    }


    // ============================================================
    // APAR DILEPAS
    // ============================================================

    private void OnAPARReleased(SelectExitEventArgs args)
    {
        isAPARGrabbed = false;

        UpdateUIState();
    }


    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        // Jangan update sebelum misi dimulai.
        if (!isMissionStarted || !isGuideActive)
        {
            return;
        }


        // --------------------------------------------------------
        // CEK PIN SUDAH DICABUT
        // --------------------------------------------------------

        if (
            mainExtinguisher != null &&
            mainExtinguisher.pinPulled
        )
        {
            HideGuide();

            return;
        }


        // Pastikan kamera tersedia.
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }


        // --------------------------------------------------------
        // ANIMASI PULSA PIN
        // --------------------------------------------------------

        if (isAPARGrabbed && pinTransform != null)
        {
            float pulse =
                1f + Mathf.Sin(Time.time * 5f) * 0.08f;

            pinTransform.localScale =
                initialPinScale * pulse;
        }


        // --------------------------------------------------------
        // BILLBOARD UI
        // --------------------------------------------------------

        if (guideCanvasGO != null)
        {
            Vector3 targetPos =
                pinTransform != null
                    ? pinTransform.position
                    : transform.position;


            // Efek UI sedikit naik-turun.
            float floatEffect =
                Mathf.Sin(Time.time * 2.5f) * 0.012f;


            guideCanvasGO.transform.position =
                targetPos +
                Vector3.up *
                (uiHeightAbovePin + floatEffect);


            // UI selalu menghadap kamera VR.
            if (mainCamera != null)
            {
                guideCanvasGO.transform.LookAt(
                    mainCamera.transform.position
                );

                guideCanvasGO.transform.Rotate(
                    0f,
                    180f,
                    0f
                );
            }
        }
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
        if (isGuideActive && guideCanvasGO != null && guideCanvasGO.activeSelf)
        {
            UpdateUIState();
        }
    }

    // ============================================================
    // UPDATE UI
    // ============================================================

    private void UpdateUIState()
    {
        bool isEnglish = VRLanguageManager.IsEnglish;

        // ========================================================
        // TAHAP 1
        // ========================================================

        if (!isAPARGrabbed)
        {
            // Judul.
            if (mainLabelText != null)
            {
                mainLabelText.text = isEnglish ? "GRAB EXTINGUISHER" : "AMBIL APAR";
            }


            // Instruksi.
            if (subLabelText != null)
            {
                subLabelText.text = isEnglish
                    ? "Press & Hold side <color=#00DCFF>GRIP</color> button"
                    : "Tekan & Tahan tombol <color=#00DCFF>GRIP</color> Samping";
            }


            // ----------------------------------------------------
            // GAMBAR TAHAP 1
            // ----------------------------------------------------

            if (
                guideImageUI != null &&
                ambilAPARSpr != null
            )
            {
                guideImageUI.sprite = ambilAPARSpr;

                guideImageUI.preserveAspect = true;
            }
        }


        // ========================================================
        // TAHAP 2
        // ========================================================

        else
        {
            // Judul.
            if (mainLabelText != null)
            {
                mainLabelText.text = isEnglish ? "PULL SAFETY PIN" : "TARIK PIN APAR";
            }


            // Instruksi.
            if (subLabelText != null)
            {
                subLabelText.text = isEnglish
                    ? "Pull safety pin <color=#00DCFF>directly on Extinguisher</color>"
                    : "Tarik Pin Apar <color=#00DCFF>Di APAR Langsung</color>";
            }


            // ----------------------------------------------------
            // GAMBAR TAHAP 2
            // ----------------------------------------------------

            if (
                guideImageUI != null &&
                tarikPinSprite != null
            )
            {
                guideImageUI.sprite = tarikPinSprite;

                guideImageUI.preserveAspect = true;
            }
        }
    }


    // ============================================================
    // CREATE UI
    // ============================================================

    private void CreateCleanGuideUI()
    {
        // --------------------------------------------------------
        // ROOT CANVAS
        // --------------------------------------------------------

        guideCanvasGO =
            new GameObject("APAR_Guide_UI");

        guideCanvasGO.transform.SetParent(null);


        // --------------------------------------------------------
        // CANVAS
        // --------------------------------------------------------

        Canvas canvas =
            guideCanvasGO.AddComponent<Canvas>();

        canvas.renderMode =
            RenderMode.WorldSpace;

        canvas.sortingOrder = 100;


        // --------------------------------------------------------
        // CANVAS GROUP
        // --------------------------------------------------------

        CanvasGroup cg =
            guideCanvasGO.AddComponent<CanvasGroup>();

        cg.interactable = false;
        cg.blocksRaycasts = false;


        // --------------------------------------------------------
        // CANVAS RECT
        // --------------------------------------------------------

        RectTransform canvasRT =
            guideCanvasGO.GetComponent<RectTransform>();

        canvasRT.sizeDelta =
            new Vector2(480f, 160f);

        canvasRT.localScale =
            Vector3.one * uiScale;


        // ========================================================
        // BACKGROUND
        // ========================================================

        GameObject bgGO =
            new GameObject("CardBG");

        bgGO.transform.SetParent(
            guideCanvasGO.transform,
            false
        );


        Image bgImg =
            bgGO.AddComponent<Image>();

        bgImg.color =
            new Color(
                0.06f,
                0.07f,
                0.1f,
                0.9f
            );


        RectTransform bgRT =
            bgGO.GetComponent<RectTransform>();

        bgRT.anchorMin =
            Vector2.zero;

        bgRT.anchorMax =
            Vector2.one;

        bgRT.sizeDelta =
            Vector2.zero;


        // ========================================================
        // GAMBAR PANDUAN
        // ========================================================

        GameObject imgGO =
            new GameObject("GuideImage");

        imgGO.transform.SetParent(
            guideCanvasGO.transform,
            false
        );


        guideImageUI =
            imgGO.AddComponent<Image>();


        // --------------------------------------------------------
        // DEFAULT IMAGE
        // --------------------------------------------------------

        if (ambilAPARSpr != null)
        {
            guideImageUI.sprite =
                ambilAPARSpr;
        }
        else
        {
            guideImageUI.color =
                new Color(
                    0.2f,
                    0.2f,
                    0.25f
                );
        }


        guideImageUI.preserveAspect = true;


        // --------------------------------------------------------
        // IMAGE RECT
        // --------------------------------------------------------

        RectTransform imgRT =
            imgGO.GetComponent<RectTransform>();


        imgRT.anchoredPosition =
            new Vector2(-155f, 0f);

        imgRT.sizeDelta =
            new Vector2(100f, 100f);


        // ========================================================
        // TEXT CONTAINER
        // ========================================================

        GameObject textContainer =
            new GameObject("TextGroup");

        textContainer.transform.SetParent(
            guideCanvasGO.transform,
            false
        );


        RectTransform textContainerRT =
            textContainer.AddComponent<RectTransform>();


        textContainerRT.anchoredPosition =
            new Vector2(85f, 0f);

        textContainerRT.sizeDelta =
            new Vector2(300f, 120f);


        // ========================================================
        // TITLE
        // ========================================================

        GameObject titleGO =
            new GameObject("TitleText");

        titleGO.transform.SetParent(
            textContainer.transform,
            false
        );


        mainLabelText =
            titleGO.AddComponent<TextMeshProUGUI>();


        mainLabelText.fontSize = 30;

        mainLabelText.color =
            Color.white;

        mainLabelText.fontStyle =
            FontStyles.Bold;

        mainLabelText.alignment =
            TextAlignmentOptions.Left;


        RectTransform titleRT =
            titleGO.GetComponent<RectTransform>();


        titleRT.anchoredPosition =
            new Vector2(0f, 20f);

        titleRT.sizeDelta =
            new Vector2(300f, 45f);


        // ========================================================
        // SUB TEXT
        // ========================================================

        GameObject subGO =
            new GameObject("SubText");

        subGO.transform.SetParent(
            textContainer.transform,
            false
        );


        subLabelText =
            subGO.AddComponent<TextMeshProUGUI>();


        subLabelText.fontSize = 18;

        subLabelText.color =
            new Color(
                0.85f,
                0.88f,
                0.92f
            );

        subLabelText.alignment =
            TextAlignmentOptions.Left;


        RectTransform subRT =
            subGO.GetComponent<RectTransform>();


        subRT.anchoredPosition =
            new Vector2(0f, -22f);

        subRT.sizeDelta =
            new Vector2(300f, 50f);


        // ========================================================
        // SEMBUNYIKAN UI
        // ========================================================

        guideCanvasGO.SetActive(false);


        // Set state awal.
        UpdateUIState();
    }


    // ============================================================
    // MISSION START
    // ============================================================

    public void SetMissionStarted()
    {
        isMissionStarted = true;


        // --------------------------------------------------------
        // TAMPILKAN GUIDE
        // --------------------------------------------------------

        if (
            guideCanvasGO != null &&
            mainExtinguisher != null &&
            !mainExtinguisher.pinPulled
        )
        {
            guideCanvasGO.SetActive(true);

            Debug.Log(
                "[APARPinGuide] Misi dimulai - panduan APAR ditampilkan."
            );
        }


        // --------------------------------------------------------
        // TAMPILKAN POSTER
        // --------------------------------------------------------

        if (safetyPoster != null)
        {
            safetyPoster.SetActive(true);

            Debug.Log(
                "[APARPinGuide] Poster K3 muncul."
            );
        }


        // Pastikan state UI benar.
        UpdateUIState();
    }


    // ============================================================
    // HIDE GUIDE
    // ============================================================

    private void HideGuide()
    {
        isGuideActive = false;


        // Kembalikan ukuran pin ke ukuran awal.
        if (pinTransform != null)
        {
            pinTransform.localScale =
                initialPinScale;
        }


        // Hapus UI.
        if (guideCanvasGO != null)
        {
            Destroy(guideCanvasGO);
        }
    }
}

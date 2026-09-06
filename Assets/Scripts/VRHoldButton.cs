using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRHoldButton : XRSimpleInteractable, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hold Settings")]
    [Tooltip("Durasi menahan tombol sampai misi dimulai (detik)")]
    public float holdDuration = 3.0f;

    [Header("UI Visual Indicator")]
    [Tooltip("Gambar ring aktif yang berputar mengisi lingkaran saat ditahan")]
    public Image progressFillImage;
    [Tooltip("Gambar ring latar belakang (faint circle) sebagai panduan batas lingkaran tombol")]
    public Image backgroundTrackImage;

    [Tooltip("Warna awal saat mulai menahan tombol")]
    public Color startFillColor = new Color(0f, 0.88f, 1f, 0.95f);
    [Tooltip("Warna akhir saat tombol selesai ditahan")]
    public Color endFillColor = new Color(0.2f, 1f, 0.55f, 1f);
    [Tooltip("Warna garis lingkaran pemandu (faint guide ring)")]
    public Color trackColor = new Color(1f, 1f, 1f, 0.22f);

    [Header("Ring Dimensions & Alignment (Calibrated)")]
    [Tooltip("Posisi lokal tepat ring canvas (Pos X, Pos Y, Pos Z)")]
    public Vector3 ringLocalPosition = new Vector3(-0.173f, -3.036f, -0.02f);
    [Tooltip("Skala ukuran ring canvas (Scale X, Y, Z)")]
    public float ringLocalScale = 0.006929f;
    [Tooltip("Rasio ketebalan garis melingkar")]
    [Range(0.04f, 0.35f)]
    public float ringThicknessRatio = 0.10f;

    [Header("Events")]
    public UnityEvent OnHoldComplete;

    private float currentHoldTime = 0f;
    private Vector3 defaultScale;
    private GameObject ringCanvasGO;

    // Track state dari berbagai input method (XRI, EventSystem UI, Mouse)
    private IXRInteractor currentActivatingInteractor;
    private IXRInteractor currentSelectingInteractor;
    private bool isPointerDown = false;
    private bool isPointerHovered = false;
    private bool isHoldTriggered = false;

    protected override void Awake()
    {
        base.Awake();
        defaultScale = transform.localScale;
        EnsureColliders();
        SetupProgressFillRing();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateRingTransform();
    }

    private void OnValidate()
    {
        if (ringCanvasGO != null)
        {
            UpdateRingTransform();
            if (progressFillImage != null)
            {
                progressFillImage.sprite = CreateRingSprite(512, ringThicknessRatio);
                progressFillImage.color = startFillColor;
                if (backgroundTrackImage != null)
                {
                    backgroundTrackImage.sprite = progressFillImage.sprite;
                    backgroundTrackImage.color = trackColor;
                }
            }
        }
    }

    private void EnsureColliders()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                boxCol.size = new Vector3(rt.rect.width, rt.rect.height, 0.2f);
                if (colliders.Count == 0)
                {
                    colliders.Add(boxCol);
                }
            }
        }
        else if (colliders.Count == 0)
        {
            colliders.Add(col);
        }
    }

    public void SetupProgressFillRing()
    {
        Transform existingCanvas = transform.Find("HoldButtonCanvas");
        if (existingCanvas != null)
        {
            ringCanvasGO = existingCanvas.gameObject;
        }
        else
        {
            ringCanvasGO = new GameObject("HoldButtonCanvas");
            ringCanvasGO.transform.SetParent(transform, false);
            Canvas canvas = ringCanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            ringCanvasGO.AddComponent<CanvasScaler>();
        }

        UpdateRingTransform();

        Sprite ringSprite = CreateRingSprite(512, ringThicknessRatio);

        // 1. Background Track Ring (Garis lingkaran tipis pemandu batas tombol)
        Transform trackTransform = ringCanvasGO.transform.Find("TrackRingImage");
        if (trackTransform == null)
        {
            GameObject trackGO = new GameObject("TrackRingImage");
            trackGO.transform.SetParent(ringCanvasGO.transform, false);
            backgroundTrackImage = trackGO.AddComponent<Image>();

            RectTransform trackRT = trackGO.GetComponent<RectTransform>();
            trackRT.anchorMin = Vector2.zero;
            trackRT.anchorMax = Vector2.one;
            trackRT.sizeDelta = Vector2.zero;
            trackRT.anchoredPosition = Vector2.zero;
        }
        else
        {
            backgroundTrackImage = trackTransform.GetComponent<Image>();
        }

        if (backgroundTrackImage != null)
        {
            backgroundTrackImage.sprite = ringSprite;
            backgroundTrackImage.color = trackColor;
            backgroundTrackImage.raycastTarget = false;
        }

        // 2. Active Progress Fill Ring (Radial 360 Fill memutar melingkari tombol)
        Transform fillTransform = ringCanvasGO.transform.Find("AutoProgressFill");
        if (fillTransform == null)
        {
            GameObject fillGO = new GameObject("AutoProgressFill");
            fillGO.transform.SetParent(ringCanvasGO.transform, false);
            progressFillImage = fillGO.AddComponent<Image>();

            RectTransform fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = Vector2.zero;
            fillRT.anchoredPosition = Vector2.zero;
        }
        else
        {
            progressFillImage = fillTransform.GetComponent<Image>();
        }

        if (progressFillImage != null)
        {
            progressFillImage.sprite = ringSprite;
            progressFillImage.type = Image.Type.Filled;
            progressFillImage.fillMethod = Image.FillMethod.Radial360;
            progressFillImage.fillOrigin = (int)Image.Origin360.Top; // Mulai memutar dari arah jam 12 (atas)
            progressFillImage.fillClockwise = true;
            progressFillImage.fillAmount = 0f;
            progressFillImage.color = startFillColor;
            progressFillImage.raycastTarget = false;
        }
    }

    public void UpdateRingTransform()
    {
        if (ringCanvasGO == null) return;

        ringCanvasGO.transform.localPosition = ringLocalPosition;
        ringCanvasGO.transform.localRotation = Quaternion.identity;

        RectTransform canvasRT = ringCanvasGO.GetComponent<RectTransform>();
        if (canvasRT != null)
        {
            canvasRT.sizeDelta = new Vector2(256f, 256f);
            canvasRT.localScale = Vector3.one * ringLocalScale;
        }
    }

    private Sprite CreateRingSprite(int resolution, float thickness)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float centerCoord = resolution * 0.5f;
        Vector2 center = new Vector2(centerCoord, centerCoord);
        float radiusOuter = centerCoord * 0.94f;
        float radiusInner = radiusOuter * Mathf.Clamp01(1f - thickness);
        float edgeSmooth = 2.0f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                if (dist <= radiusOuter + edgeSmooth && dist >= radiusInner - edgeSmooth)
                {
                    float alphaOuter = Mathf.Clamp01((radiusOuter - dist) / edgeSmooth + 0.5f);
                    float alphaInner = Mathf.Clamp01((dist - radiusInner) / edgeSmooth + 0.5f);
                    float alpha = alphaOuter * alphaInner;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    // --- Event XRI Activate ---
    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        currentActivatingInteractor = args.interactorObject;
    }

    protected override void OnDeactivated(DeactivateEventArgs args)
    {
        base.OnDeactivated(args);
        if (args.interactorObject == currentActivatingInteractor)
        {
            currentActivatingInteractor = null;
        }
    }

    // --- Event XRI Select ---
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        currentSelectingInteractor = args.interactorObject;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        if (args.interactorObject == currentSelectingInteractor)
        {
            currentSelectingInteractor = null;
        }
    }

    // --- Event Systems UI Pointer (Canvas WorldSpace Raycast) ---
    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerHovered = false;
    }

    private void Update()
    {
        // Pengecekan status penahanan dari VR XRI (Select atau Activate)
        bool isXriHolding = isSelected || (interactorsSelecting.Count > 0) || currentSelectingInteractor != null || currentActivatingInteractor != null;

        // Pengecekan status penahanan dari EventSystem UI Canvas
        bool isUiHolding = isPointerDown;

        // Pengecekan mouse di Unity Editor / PC Debug
        bool isHoveredAny = isHovered || isPointerHovered;
        bool isMouseHolding = false;

        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            isMouseHolding = isHoveredAny && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
        }
        #else
        if (Input.GetMouseButton(0))
        {
            isMouseHolding = isHoveredAny;
        }
        #endif

        bool isPressed = isXriHolding || isUiHolding || isMouseHolding;

        if (isPressed && !isHoldTriggered)
        {
            currentHoldTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentHoldTime / holdDuration);

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = progress;
                progressFillImage.color = Color.Lerp(startFillColor, endFillColor, progress);
            }

            transform.localScale = Vector3.Lerp(transform.localScale, defaultScale * (1.0f + progress * 0.06f), Time.deltaTime * 12f);

            if (currentHoldTime >= holdDuration)
            {
                isHoldTriggered = true;
                Debug.Log("[VRHoldButton] ✅ Hold Selesai! Memulai Misi...");
                OnHoldComplete?.Invoke();

                currentHoldTime = 0f;
                currentActivatingInteractor = null;
                currentSelectingInteractor = null;
                isPointerDown = false;

                if (progressFillImage != null)
                    progressFillImage.fillAmount = 0f;
            }
        }
        else
        {
            if (!isPressed)
            {
                isHoldTriggered = false;
            }

            if (currentHoldTime > 0f)
            {
                currentHoldTime = Mathf.MoveTowards(currentHoldTime, 0f, Time.deltaTime * 4f);
                float progress = Mathf.Clamp01(currentHoldTime / holdDuration);

                if (progressFillImage != null)
                {
                    progressFillImage.fillAmount = progress;
                    progressFillImage.color = Color.Lerp(startFillColor, endFillColor, progress);
                }
            }

            transform.localScale = Vector3.Lerp(transform.localScale, defaultScale, Time.deltaTime * 10f);
        }
    }
}
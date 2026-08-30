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
    public float holdDuration = 3.0f;

    [Header("UI Visual Indicator")]
    public Image progressFillImage;
    public Color startFillColor = new Color(0f, 0.85f, 1f, 0.9f);
    public Color endFillColor = new Color(0.2f, 1f, 0.55f, 1f);

    [Header("Events")]
    public UnityEvent OnHoldComplete;

    private float currentHoldTime = 0f;
    private Vector3 defaultScale;

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
        SetupProgressFillImage();

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                boxCol.size = new Vector3(rt.rect.width, rt.rect.height, 1.0f);
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

    private void SetupProgressFillImage()
    {
        if (progressFillImage != null)
        {
            ConfigureFillImage(progressFillImage);
            return;
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            string n = img.gameObject.name.ToLower();
            if (n.Contains("fill") || n.Contains("progress") || n.Contains("loading"))
            {
                progressFillImage = img;
                ConfigureFillImage(progressFillImage);
                return;
            }
        }

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("HoldButtonCanvas");
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localPosition = Vector3.zero;
            canvasGO.transform.localRotation = Quaternion.identity;

            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(200f, 200f);
            canvasRT.localScale = Vector3.one * 0.005f;

            canvasGO.AddComponent<CanvasScaler>();
        }

        GameObject fillGO = new GameObject("AutoProgressFill");
        fillGO.transform.SetParent(canvas.transform, false);

        progressFillImage = fillGO.AddComponent<Image>();
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;
        fillRT.anchoredPosition = Vector2.zero;

        progressFillImage.sprite = CreateCircleSprite(128);
        ConfigureFillImage(progressFillImage);
    }

    private void ConfigureFillImage(Image img)
    {
        if (img == null) return;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = true;
        img.fillAmount = 0f;
        img.color = startFillColor;
    }

    private Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float radius = resolution * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius && dist >= radius * 0.70f)
                {
                    float alpha = Mathf.SmoothStep(1f, 0f, Mathf.Abs(dist - (radius * 0.85f)) / (radius * 0.15f));
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

            transform.localScale = Vector3.Lerp(transform.localScale, defaultScale * (1.0f + progress * 0.08f), Time.deltaTime * 12f);

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
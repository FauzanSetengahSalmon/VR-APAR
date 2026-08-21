using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRHoldButton : XRSimpleInteractable
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
    private IXRActivateInteractor currentActivatingInteractor;

    protected override void Awake()
    {
        base.Awake();
        defaultScale = transform.localScale;
        SetupProgressFillImage();

        Collider col = GetComponent<Collider>();
        if (col != null && colliders.Count == 0)
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

    // Catat interactor mana yang menekan trigger
    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        currentActivatingInteractor = args.interactorObject;
    }

    // Reset jika trigger dilepas
    protected override void OnDeactivated(DeactivateEventArgs args)
    {
        base.OnDeactivated(args);
        if (args.interactorObject == currentActivatingInteractor)
        {
            currentActivatingInteractor = null;
        }
    }

    private void Update()
    {
        // Pengecekan input terpisah dengan bersih
        bool isVrHolding = isHovered && currentActivatingInteractor != null;
        
        bool isMouseHolding = false;
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            isMouseHolding = isHovered && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
        }
        #endif

        bool isPressed = isVrHolding || isMouseHolding;

        if (isPressed)
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
                Debug.Log("[HoldButton] Hold Selesai!");
                OnHoldComplete?.Invoke();

                currentHoldTime = 0f;
                currentActivatingInteractor = null;

                if (progressFillImage != null)
                    progressFillImage.fillAmount = 0f;
            }
        }
        else
        {
            // Reset interactor jika ray melenceng keluar objek saat menahan
            if (!isHovered)
            {
                currentActivatingInteractor = null;
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
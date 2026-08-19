using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

// Extend dari XRSimpleInteractable bawaan XR Toolkit
public class VRHoldButton : UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable
{
    [Header("Hold Settings")]
    public float holdDuration = 3.0f; // Tahan 3 detik

    [Header("UI Visual Indicator")]
    [Tooltip("Image UI untuk animasi radial fill lingkaran. Diisi otomatis jika kosong.")]
    public Image progressFillImage;

    [Tooltip("Warna awal progress fill")]
    public Color startFillColor = new Color(0f, 0.85f, 1f, 0.9f); // Cyan
    [Tooltip("Warna akhir saat progress penuh")]
    public Color endFillColor = new Color(0.2f, 1f, 0.55f, 1f);   // Glowing Green/Cyan

    [Header("Events")]
    public UnityEvent OnHoldComplete;

    private float currentHoldTime = 0f;
    private bool isTriggerActivated = false;
    private Vector3 defaultScale;

    protected override void Awake()
    {
        base.Awake();
        defaultScale = transform.localScale;
        SetupProgressFillImage();

        // Auto-assign Collider agar tidak error jika inspector lupa dimasukkan
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

        // 1. Cari Image bernama "ProgressFill", "Fill", "LoadingFill"
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

        // 2. Buat otomatis Radial Progress Ring Overlay jika belum ada
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

        // Buat sprite lingkaran procedural
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

    // Hanya dipanggil oleh Trigger Depan (Activate) dari Controller yang MENUNJUK tombol
    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        isTriggerActivated = true;
    }

    // Dipanggil saat Trigger Depan dilepas
    protected override void OnDeactivated(DeactivateEventArgs args)
    {
        base.OnDeactivated(args);
        isTriggerActivated = false;
    }

    private void Update()
    {
        // Pengecekan aman: Murni Trigger VR (isTriggerActivated) ATAU Klik Mouse jika sedang di Simulator
        bool isPressed = isTriggerActivated;

        if (UnityEngine.InputSystem.Mouse.current != null && 
            UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
        {
            isPressed = true;
        }

        // HANYA jalan jika Ray menunjuk tombol (isHovered) DAN tombol ditekan (isPressed)
        if (isHovered && isPressed)
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
                Debug.Log("[HoldButton] 🚀 Hold Selesai — Mulai Misi!");
                OnHoldComplete?.Invoke();
                
                // Reset state agar tidak loop
                currentHoldTime = 0f;
                isTriggerActivated = false;

                if (progressFillImage != null)
                    progressFillImage.fillAmount = 0f;
            }
        }
        else
        {
            // Decaying/Reset saat trigger dilepas atau Ray melenceng dari tombol
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
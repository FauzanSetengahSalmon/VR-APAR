using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_VR || UNITY_XR_MANAGEMENT
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#endif

/// <summary>
/// Tombol toggle bahasa (ID | EN).
/// Mendukung interaksi VR XRI (Select / Activate) dan Mouse PC.
/// </summary>
#if ENABLE_VR || UNITY_XR_MANAGEMENT
public class VRLanguageToggle : XRSimpleInteractable, IPointerClickHandler
#else
public class VRLanguageToggle : MonoBehaviour, IPointerClickHandler
#endif
{
    public enum ToggleAction
    {
        ToggleCycle,        // Klik untuk bergantian ID <-> EN
        SetToIndonesian,    // Khusus tombol "ID"
        SetToEnglish        // Khusus tombol "EN"
    }

    [Header("Mode Tombol")]
    public ToggleAction action = ToggleAction.ToggleCycle;

    [Header("Visual Toggle (Opsional)")]
    [Tooltip("Sprite saat Bahasa Indonesia aktif")]
    public Sprite spriteIndonesian;

    [Tooltip("Sprite saat Bahasa Inggris aktif")]
    public Sprite spriteEnglish;

    [Tooltip("Image component target jika menggunakan Canvas UI")]
    public Image targetImage;

    [Tooltip("SpriteRenderer target jika menggunakan World Sprite")]
    public SpriteRenderer targetSpriteRenderer;

    [Header("Audio Feedback (Opsional)")]
    public AudioClip toggleSound;
    private AudioSource audioSource;

    private float lastToggleTime = -99f;
    private const float TOGGLE_COOLDOWN = 0.5f;

    #if ENABLE_VR || UNITY_XR_MANAGEMENT
    protected override void Awake()
    {
        base.Awake();
        InitComponent();
    }
    #else
    private void Awake()
    {
        InitComponent();
    }
    #endif

    private void InitComponent()
    {
        audioSource = GetComponent<AudioSource>();
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (targetSpriteRenderer == null) targetSpriteRenderer = GetComponent<SpriteRenderer>();

        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc == null)
        {
            bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.0f, 0.5f, 0.3f);
        }

        #if ENABLE_VR || UNITY_XR_MANAGEMENT
        if (!colliders.Contains(bc))
            colliders.Add(bc);
        #endif
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        VRLanguageManager.OnLanguageChanged += OnLanguageChangedHandler;
        UpdateVisuals(VRLanguageManager.CurrentLanguage);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        VRLanguageManager.OnLanguageChanged -= OnLanguageChangedHandler;
    }

    private void OnLanguageChangedHandler(AppLanguage newLang)
    {
        UpdateVisuals(newLang);
    }

    public void UpdateVisuals(AppLanguage currentLang)
    {
        Sprite activeSprite = (currentLang == AppLanguage.English) ? spriteEnglish : spriteIndonesian;
        if (activeSprite != null)
        {
            if (targetImage != null) targetImage.sprite = activeSprite;
            if (targetSpriteRenderer != null) targetSpriteRenderer.sprite = activeSprite;
        }
    }

    public void ExecuteToggle()
    {
        if (GetComponent<VRHoldButton>() != null)
        {
            return;
        }

        if (Time.time - lastToggleTime < TOGGLE_COOLDOWN) return;
        lastToggleTime = Time.time;

        if (VRLanguageManager.Instance == null)
        {
            var mgr = FindFirstObjectByType<VRLanguageManager>();
            if (mgr == null)
            {
                GameObject go = new GameObject("VR_Language_Manager");
                go.AddComponent<VRLanguageManager>();
            }
        }

        switch (action)
        {
            case ToggleAction.ToggleCycle:
                VRLanguageManager.Instance?.ToggleLanguage();
                break;
            case ToggleAction.SetToIndonesian:
                VRLanguageManager.Instance?.SetIndonesian();
                break;
            case ToggleAction.SetToEnglish:
                VRLanguageManager.Instance?.SetEnglish();
                break;
        }

        if (audioSource != null && toggleSound != null)
            audioSource.PlayOneShot(toggleSound);

        Debug.Log($"[VRLanguageToggle] 🌐 Toggle dieksekusi: {action} → {VRLanguageManager.CurrentLanguage}");
    }

    // ── VR XR Interaction Toolkit ─────────────────────────────────
    #if ENABLE_VR || UNITY_XR_MANAGEMENT
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        ExecuteToggle();
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        ExecuteToggle();
    }
    #endif

    // ── UI Canvas EventSystem ──────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        ExecuteToggle();
    }

    // ── Mouse Click PC / Editor ────────────────────────────────────
    private void Update()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            CheckMouseRaycastClick();
        #else
        if (Input.GetMouseButtonDown(0))
            CheckMouseRaycastClick();
        #endif
        #endif
    }

    private void CheckMouseRaycastClick()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        #if ENABLE_INPUT_SYSTEM
        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        #else
        Vector2 mousePos = Input.mousePosition;
        #endif

        Ray ray = cam.ScreenPointToRay(mousePos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 20f);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                Debug.Log($"[VRLanguageToggle] 🖱️ Mouse Raycast Hit: {gameObject.name}");
                ExecuteToggle();
                break;
            }
        }
    }
}

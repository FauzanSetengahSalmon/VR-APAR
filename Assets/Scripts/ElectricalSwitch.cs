using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

/// <summary>
/// MCB (Miniature Circuit Breaker) interaktif untuk simulasi VR BPBD.
/// Hanya akan mati (Turn Off) saat tombol Trigger / Grip BENAR-BENAR DITEKAN saat mengarah ke saklar.
/// </summary>
public class ElectricalSwitch : XRSimpleInteractable, IPointerClickHandler
{
    public bool IsSwitchedOff => isSwitchedOff;

    [Header("Referensi Visual MCB")]
    [Tooltip("Transform tuas biru yang akan dianimasikan saat di-flip")]
    public Transform leverTransform;

    [Header("Pengaturan Animasi Tuas")]
    [Tooltip("Rotasi tuas saat MCB ON (posisi awal = menyala)")]
    public Vector3 leverRotationON  = new Vector3( 20f, 0f, 0f);
    [Tooltip("Rotasi tuas saat MCB OFF (dimatikan oleh pemain)")]
    public Vector3 leverRotationOFF = new Vector3(-25f, 0f, 0f);
    [Tooltip("Durasi animasi flip tuas (detik)")]
    public float flipDuration = 0.22f;

    [Header("Visual Indikator")]
    public Renderer bodyRenderer;
    public Color colorON  = new Color(0.78f, 0.82f, 0.88f);
    public Color colorOFF = new Color(0.25f, 0.25f, 0.30f);

    [Header("Audio")]
    public AudioClip clickClip;
    [Range(0f, 1f)] public float clickVolume = 0.9f;

    [Header("Events")]
    public UnityEvent OnSwitchTurnedOff;
    public UnityEvent OnSwitchTurnedOn;

    // ── Private State ────────────────────────────────────────────────────────
    private bool isSwitchedOff = false;
    private bool isAnimating = false;
    private AudioSource audioSource;

    protected override void Awake()
    {
        base.Awake();

        // Register collider ke base XRSimpleInteractable
        var col = GetComponent<Collider>();
        if (col != null && colliders.Count == 0)
        {
            colliders.Add(col);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake  = false;
            audioSource.maxDistance  = 3f;
        }

        if (leverTransform != null)
            leverTransform.localRotation = Quaternion.Euler(leverRotationON);

        if (bodyRenderer != null)
            SetBodyColor(colorON);
    }

    private void Start()
    {
        // Pastikan semua collider terdaftar
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            if (!colliders.Contains(c))
                colliders.Add(c);
        }

        if (interactionManager == null)
            interactionManager = FindFirstObjectByType<XRInteractionManager>();
    }

    // ── Method dipanggil oleh SwitchStepManager ───────────────────────────────
    public void SetMissionStarted()
    {
        // Siap menerima interaksi saat misi dimulai
        Debug.Log("[ElectricalSwitch] Misi dimulai, MCB siap diinteraksi.");
    }

    // ── Event XRI Native: Hanya Aktif Saat Action Trigger/Activate Ditekan ──
    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        Debug.Log("[ElectricalSwitch] 🎯 OnActivated (Trigger Ditekan) dari: " + args.interactorObject.transform.name);
        TurnOff();
    }

    private void Update()
    {
        if (isSwitchedOff || isAnimating) return;

        // Pengecekan Input Manual: Mengarah (Hover) + Menekan Trigger / Grip
        if (isHovered)
        {
            var lHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var rHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            bool trigPressed = (lHand.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool lt) && lt) ||
                               (rHand.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool rt) && rt) ||
                               (lHand.TryGetFeatureValue(XRCommonUsages.gripButton, out bool lg) && lg) ||
                               (rHand.TryGetFeatureValue(XRCommonUsages.gripButton, out bool rg) && rg);

            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                trigPressed = true;
            #endif

            if (trigPressed)
            {
                Debug.Log("[ElectricalSwitch] 🎯 Hovered + Trigger Ditekan!");
                TurnOff();
                return;
            }
        }
    }

    // ── Event Canvas Pointer (UI Raycast Click) ──────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnimating || isSwitchedOff) return;
        Debug.Log("[ElectricalSwitch] 🖱️ PointerClick diterima!");
        TurnOff();
    }

    // ── Logika Mematikan Saklar ───────────────────────────────────────────────
    public void TurnOff()
    {
        if (isSwitchedOff) return;
        isSwitchedOff = true;

        if (clickClip != null && audioSource != null)
            audioSource.PlayOneShot(clickClip, clickVolume);

        StartCoroutine(AnimateLever(leverRotationOFF));
        SetBodyColor(colorOFF);

        OnSwitchTurnedOff?.Invoke();
        Debug.Log("[ElectricalSwitch] ⚡ MCB BERHASIL DIMATIKAN!");
    }

    private IEnumerator AnimateLever(Vector3 targetEuler)
    {
        if (leverTransform == null) yield break;
        isAnimating = true;
        Quaternion startRot  = leverTransform.localRotation;
        Quaternion targetRot = Quaternion.Euler(targetEuler);
        float elapsed = 0f;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flipDuration);
            leverTransform.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }

        leverTransform.localRotation = targetRot;
        isAnimating = false;
    }

    private void SetBodyColor(Color c)
    {
        if (bodyRenderer == null) return;
        var mpb = new MaterialPropertyBlock();
        bodyRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c);
        mpb.SetColor("_Color",     c);
        bodyRenderer.SetPropertyBlock(mpb);
    }
}
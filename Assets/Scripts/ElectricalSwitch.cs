using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

/// <summary>
/// MCB (Miniature Circuit Breaker) interaktif untuk simulasi VR BPBD.
/// Mewarisi langsung dari XRSimpleInteractable sehingga bekerja sempurna dengan XR Device Simulator & Headset VR.
/// </summary>
public class ElectricalSwitch : XRSimpleInteractable, IPointerClickHandler, IPointerDownHandler
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
    private Transform[] cachedVRHands;

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

        // Cache controller tangan
        var xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (var t in xrOrigin.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLower();
                if (n.Contains("controller") || n.Contains("hand") || n.Contains("interactor"))
                {
                    list.Add(t);
                }
            }
            cachedVRHands = list.ToArray();
        }
    }

    public void SetMissionStarted()
    {
        // Ready
    }

    // ── XRI Native Virtual Overrides (Dipanggil langsung oleh XR Device Simulator & VR Controller) ──
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        Debug.Log("[ElectricalSwitch] 🎯 OnSelectEntered dari: " + args.interactorObject.transform.name);
        TurnOff();
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        Debug.Log("[ElectricalSwitch] 🎯 OnActivated dari: " + args.interactorObject.transform.name);
        TurnOff();
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        Debug.Log("[ElectricalSwitch] 🎯 OnHoverEntered dari: " + args.interactorObject.transform.name);
    }

    private void Update()
    {
        if (isSwitchedOff || isAnimating) return;

        // Cek jika sedang di-select oleh interactor XR
        if (isSelected || interactorsSelecting.Count > 0)
        {
            TurnOff();
            return;
        }

        // Cek jika sedang di-hover dan tombol ditekan di Device Simulator / VR
        if (isHovered)
        {
            var lHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var rHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            bool trigPressed = (lHand.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool lt) && lt) ||
                               (rHand.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool rt) && rt) ||
                               (lHand.TryGetFeatureValue(XRCommonUsages.gripButton, out bool lg) && lg) ||
                               (rHand.TryGetFeatureValue(XRCommonUsages.gripButton, out bool rg) && rg);

            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
                trigPressed = true;
            #endif

            if (trigPressed)
            {
                Debug.Log("[ElectricalSwitch] 🎯 Hovered + Button Press terdeteksi!");
                TurnOff();
                return;
            }
        }

        // 1. Mouse Raycast Click di Editor/PC
        #if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 15f))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        TurnOff();
                        return;
                    }
                }
            }
        }
        #endif

        // 2. Deteksi Jarak Tangan VR Controller Kiri / Kanan
        CheckVRHandProximityInput();
    }

    private void CheckVRHandProximityInput()
    {
        Vector3 mcbPos = transform.position;

        // A. Cek Transform tangan VR yang di-cache
        if (cachedVRHands != null)
        {
            foreach (var hand in cachedVRHands)
            {
                if (hand != null && hand.gameObject.activeInHierarchy)
                {
                    float d = Vector3.Distance(hand.position, mcbPos);
                    if (d < 0.45f)
                    {
                        // Jika tangan berada sangat dekat (< 45 cm)
                        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

                        bool pressed = (left.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool lt) && lt) ||
                                       (left.TryGetFeatureValue(XRCommonUsages.gripButton, out bool lg) && lg) ||
                                       (right.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool rt) && rt) ||
                                       (right.TryGetFeatureValue(XRCommonUsages.gripButton, out bool rg) && rg);

                        // Jika menyentuh langsung (< 18 cm) ATAU menekan tombol dalam jarak dekat
                        if (d < 0.18f || pressed)
                        {
                            Debug.Log($"[ElectricalSwitch] 🫱 Tangan VR '{hand.name}' berjarak {d:F2}m -> Matikan MCB!");
                            TurnOff();
                            return;
                        }
                    }
                }
            }
        }

        // B. Cek XRNode Device Position
        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHand.isValid && rightHand.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 rPos))
        {
            if (Vector3.Distance(rPos, mcbPos) < 0.50f)
            {
                if ((rightHand.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool tb) && tb) ||
                    (rightHand.TryGetFeatureValue(XRCommonUsages.gripButton, out bool gb) && gb))
                {
                    TurnOff();
                    return;
                }
            }
        }

        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.isValid && leftHand.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 lPos))
        {
            if (Vector3.Distance(lPos, mcbPos) < 0.50f)
            {
                if ((leftHand.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool tb) && tb) ||
                    (leftHand.TryGetFeatureValue(XRCommonUsages.gripButton, out bool gb) && gb))
                {
                    TurnOff();
                    return;
                }
            }
        }
    }

    // ── UI / Pointer Events (Raycast Click) ───────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnimating || isSwitchedOff) return;
        Debug.Log("[ElectricalSwitch] 🖱️ PointerClick diterima!");
        TurnOff();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isAnimating || isSwitchedOff) return;
        TurnOff();
    }

    private void OnMouseDown()
    {
        if (isAnimating || isSwitchedOff) return;
        Debug.Log("[ElectricalSwitch] 🖱️ MouseDown diterima!");
        TurnOff();
    }

    // ── Physical Trigger Touch (Sentuhan langsung ujung jari / controller) ────
    private void OnTriggerEnter(Collider other)
    {
        if (isAnimating || isSwitchedOff) return;

        string n = other.name.ToLower();
        if (n.Contains("hand") || n.Contains("controller") || n.Contains("finger") || n.Contains("poke") || n.Contains("direct"))
        {
            Debug.Log("[ElectricalSwitch] 🖐️ Sentuhan fisik terdeteksi dari: " + other.name);
            TurnOff();
        }
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
        Debug.Log("[ElectricalSwitch] ⚡⚡ MCB BERHASIL DIMATIKAN! (SWITCH OFF)");
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


using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Script utama APAR (Alat Pemadam Api Ringan) dengan mekanisme realistis:
/// - Tangan kanan memegang body APAR (handle utama)
/// - Tangan kanan juga dipakai untuk mencabut pin (via APARPin.cs)
/// - Tangan kiri memegang hose/moncong (via APARHoseGrabber.cs)
/// - Spray HANYA nyala jika: PIN sudah dicabut AND tangan kiri sedang grip hose
/// - Saat grip hose dilepas → spray langsung mati
/// </summary>
public class AutoFireExtinguisher : MonoBehaviour
{
    [Header("Referensi Spray & Audio")]
    public ParticleSystem sprayEffect;
    public AudioClip sprayAudioClip;
    [Range(0f, 1f)] public float sprayVolume = 0.9f;

    [Header("Logika Jarak Tembak")]
    [Tooltip("Jarak tembak asap APAR dalam meter")]
    public float extinguishRange = 4f;

    [Header("Mekanisme Pin APAR")]
    [Tooltip("Apakah pin sudah dicabut? Diatur otomatis oleh script APARPin saat pin di-grab.")]
    public bool pinPulled = false;

    [Header("Offset Pegangan Tangan")]
    [Tooltip("Geser posisi APAR relatif terhadap tangan kanan")]
    public Vector3 handOffsetPosition = Vector3.zero;
    [Tooltip("Putar rotasi APAR relatif terhadap tangan kanan")]
    public Vector3 handOffsetRotation = Vector3.zero;

    [Header("Testing / Debug")]
    [Tooltip("Tekan Space/G di keyboard untuk toggle spray saat testing di Editor")]
    public bool debugForceSpray = false;

    // ── Referensi internal ──────────────────────────────────────────────────
    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    private bool isAttachedToHand = false;
    private bool isTestingActive = false;

    // ── Status genggaman (di-set dari luar oleh APARHoseGrabber) ───────────
    [HideInInspector] public bool isMainHandleHeld = false;
    [HideInInspector] public bool isHoseHeld = false;

    // ── State spray internal ────────────────────────────────────────────────
    private bool wasSprayingLastFrame = false;

    // ═══════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════════

    private APARPropStateMachine propStateMachine;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        propStateMachine = GetComponent<APARPropStateMachine>();
        if (propStateMachine == null) propStateMachine = GetComponentInParent<APARPropStateMachine>();

        // Kunci fisika agar APAR tidak jatuh saat game dimulai
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = sprayAudioClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = sprayVolume;

        // Setup particle spray — pastikan mati di awal
        if (sprayEffect == null)
            sprayEffect = GetComponentInChildren<ParticleSystem>();

        if (sprayEffect != null)
        {
            var main = sprayEffect.main;
            main.playOnAwake = false;
            if (sprayEffect.isPlaying)
                sprayEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // ⚠️ PAKSA RESET — override nilai Inspector lama yang mungkin masih true
        pinPulled          = false;
        debugForceSpray    = false;
        isTestingActive    = false;
        wasSprayingLastFrame = false;
        isAttachedToHand   = false;
        isMainHandleHeld   = false;
        isHoseHeld         = false;
        Debug.Log("[APAR] State direset. Cabut pin dulu sebelum bisa spray.");
    }

    private void OnEnable()
    {
        if (grabInteractable == null) grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabEnter);
            grabInteractable.selectExited.AddListener(OnGrabExit);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabEnter);
            grabInteractable.selectExited.RemoveListener(OnGrabExit);
        }
        StopSpray();
    }

    private void Update()
    {
        // Update status genggaman handle utama (hanya jika belum attached ke tangan)
        if (grabInteractable != null && !isAttachedToHand)
        {
            isMainHandleHeld = grabInteractable.isSelected || grabInteractable.interactorsSelecting.Count > 0;
        }

        // ── Testing Keyboard di Editor ─────────────────────────────────────
        bool keyboardLeverPressed = false;
        if (Keyboard.current != null)
        {
            // Tekan 'P' di keyboard untuk cabut/pasang pin
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                pinPulled = !pinPulled;
                Debug.Log($"[APAR] 🔑 Pin State (via 'P'): {(pinPulled ? "DICABUT (UNLOCKED)" : "TERPASANG (LOCKED)")}");
                if (propStateMachine != null)
                {
                    if (pinPulled) propStateMachine.PullPin();
                    else propStateMachine.LockPin();
                }
            }

            // Tahan 'Space' di keyboard untuk tekan gagang
            keyboardLeverPressed = Keyboard.current.spaceKey.isPressed;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // KONDISI SPRAY — Wajib Mengikuti State Machine & Pin
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        bool isPropSpraying = (propStateMachine != null && propStateMachine.IsSpraying);
        bool isKeyboardSpraying = pinPulled && keyboardLeverPressed;
        bool isStandardVRSpraying = pinPulled && isHoseHeld;
        bool shouldSpray = isPropSpraying || isKeyboardSpraying || isStandardVRSpraying || debugForceSpray;

        // Nyalakan/matikan spray hanya saat ada perubahan state (efisien)
        if (shouldSpray && !wasSprayingLastFrame)
        {
            StartSpray();
        }
        else if (!shouldSpray && wasSprayingLastFrame)
        {
            StopSpray();
        }

        wasSprayingLastFrame = shouldSpray;

        // Lakukan penghitungan pemadam jika spray aktif
        if (shouldSpray)
        {
            ExtinguishFiresGradually();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GRAB EVENTS — hanya mengurus attachment APAR ke tangan, BUKAN spray
    // ═══════════════════════════════════════════════════════════════════════

    private void OnGrabEnter(SelectEnterEventArgs args)
    {
        isMainHandleHeld = true;

        if (args.interactorObject != null)
        {
            VRHandAnimator handAnim = args.interactorObject.transform.GetComponentInParent<VRHandAnimator>();
            if (handAnim != null) handAnim.SetForceGrip(true);
        }

        // Kunci APAR sebagai child dari controller tangan kanan (hanya sekali)
        if (!isAttachedToHand && args.interactorObject != null)
        {
            isAttachedToHand = true;

            Transform handTransform = args.interactorObject.transform;
            transform.SetParent(handTransform);
            transform.localPosition = handOffsetPosition;
            transform.localRotation = Quaternion.Euler(handOffsetRotation);

            // Matikan fisika agar tidak goyang
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Nonaktifkan XRGrabInteractable setelah ter-attach
            if (grabInteractable != null)
                grabInteractable.enabled = false;

            Debug.Log("[APAR] APAR ter-attach ke tangan kanan.");
        }

        // ❌ TIDAK ada StartSpray() di sini — spray hanya dari Update()
    }

    private void OnGrabExit(SelectExitEventArgs args)
    {
        // Jika sudah menempel di tangan, pertahankan status isMainHandleHeld
        if (isAttachedToHand)
        {
            isMainHandleHeld = true;
            return;
        }

        isMainHandleHeld = false;
        // ❌ TIDAK ada StopSpray() di sini — spray hanya dikontrol dari Update()
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  EKSTINGSI API
    // ═══════════════════════════════════════════════════════════════════════

    private void ExtinguishFiresGradually()
    {
        Transform nozzle = sprayEffect != null ? sprayEffect.transform : transform;
        Debug.DrawRay(nozzle.position, nozzle.forward * extinguishRange, Color.cyan);

        RaycastHit[] hits = Physics.SphereCastAll(nozzle.position, 0.4f, nozzle.forward, extinguishRange);
        foreach (RaycastHit hit in hits)
        {
            FireExtinguisherTarget target = hit.collider.GetComponentInParent<FireExtinguisherTarget>();
            if (target != null)
            {
                target.ExtinguishGradually(Time.deltaTime);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  START / STOP SPRAY
    // ═══════════════════════════════════════════════════════════════════════

    public void StartSpray()
    {
        if (sprayEffect != null)
        {
            if (!sprayEffect.gameObject.activeSelf)
                sprayEffect.gameObject.SetActive(true);
            if (!sprayEffect.isPlaying)
                sprayEffect.Play(true);
        }

        if (audioSource != null && sprayAudioClip != null && !audioSource.isPlaying)
            audioSource.Play();

        Debug.Log("[APAR] ✅ Spray NYALA — pin dicabut & hose digenggam.");
    }

    public void StopSpray()
    {
        if (sprayEffect != null && sprayEffect.isPlaying)
            sprayEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        Debug.Log("[APAR] 🚫 Spray MATI.");
    }
}
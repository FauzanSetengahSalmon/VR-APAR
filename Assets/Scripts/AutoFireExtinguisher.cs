using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Script utama APAR (Alat Pemadam Api Ringan) VR:
/// - Tangan KIRI memegang Body/Tabung APAR (posisi ter-offset pas, tidak menutupi kamera)
/// - Tangan KANAN otomatis memegang Corong saat APAR di-grab
/// - Tombol X atau Y pada VR Controller (atau tombol X/Y di keyboard) untuk cabut PIN
/// - Setelah PIN dicabut, menekan TRIGGER controller akan membuka gagang & mengeluarkan ASAP dari Corong
/// - Asap & raycast pemadaman selalu mengikuti rotasi & posisi Corong (tangan kanan)
/// </summary>
public class AutoFireExtinguisher : MonoBehaviour
{
    [Header("Referensi Spray & Audio")]
    [Tooltip("ParticleSystem asap (Smoke) pada Corong — diisi otomatis jika kosong")]
    public ParticleSystem sprayEffect;
    public AudioClip sprayAudioClip;
    [Range(0f, 1f)] public float sprayVolume = 0.9f;

    [Header("Logika Jarak Tembak")]
    [Tooltip("Jarak tembak asap APAR dalam meter")]
    public float extinguishRange = 4f;

    [Header("Mekanisme Pin APAR")]
    [Tooltip("Apakah pin sudah dicabut? Bisa dicabut via tombol X/Y VR Controller atau grab Pin.")]
    public bool pinPulled = false;

    [Header("Offset Pegangan Tangan Kiri (Tabung)")]
    [Tooltip("Geser posisi tabung APAR relatif terhadap tangan kiri (mencegah menutupi layar VR)")]
    public Vector3 handOffsetPosition = new Vector3(-0.1f, -0.45f, 0.25f);
    [Tooltip("Putar rotasi tabung APAR relatif terhadap tangan kiri")]
    public Vector3 handOffsetRotation = new Vector3(15f, -15f, 0f);

    [Header("Referensi Mesh 3D Selang Statis")]
    [Tooltip("GameObject Mesh 3D Selang bawaan model 3D (akan dinonaktifkan otomatis saat APAR diambil)")]
    public GameObject staticMeshSelang;

    [Header("Testing / Debug")]
    [Tooltip("Tekan Space di keyboard untuk toggle spray saat testing di Editor")]
    public bool debugForceSpray = false;

    // ── Referensi internal ──────────────────────────────────────────────────
    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    private bool isAttachedToHand = false;

    // ── Status genggaman (di-set dari luar oleh APARHoseGrabber) ───────────
    [HideInInspector] public bool isMainHandleHeld = false;
    [HideInInspector] public bool isHoseHeld = false;

    // ── Referensi Corong (nozzle) — diisi oleh APARHoseGrabber ─────────────
    [HideInInspector] public Transform nozzleTransform;

    // ── State spray internal ────────────────────────────────────────────────
    private bool wasSprayingLastFrame = false;

    // ── Mission Lock ─────────────────────────────────────────────────────────
    private bool isMissionStarted = false;

    // ── State Input Controller VR ───────────────────────────────────────────
    private bool isTriggerPressedOnController = false;

    private APARPropStateMachine propStateMachine;

    // ═══════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        propStateMachine = GetComponent<APARPropStateMachine>();
        if (propStateMachine == null) propStateMachine = GetComponentInParent<APARPropStateMachine>();

        // Kunci fisika agar tabung tidak jatuh saat game dimulai
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

        // Setup particle spray — cari "Smoke" atau "ExtinguisherSmoke" di seluruh hierarchy
        SetupParticleSpray();

        // ⚠️ PAKSA RESET
        pinPulled = false;
        debugForceSpray = false;
        wasSprayingLastFrame = false;
        isAttachedToHand = false;
        isMainHandleHeld = false;
        isHoseHeld = false;

        // Kunci grab APAR sampai misi dimulai
        if (grabInteractable != null)
            grabInteractable.enabled = false;

        Debug.Log("[APAR] Initialized. Tekan X/Y untuk cabut pin.");
    }

    private void SetupParticleSpray()
    {
        if (staticMeshSelang == null)
        {
            Transform s = transform.Find("Selang");
            if (s != null) staticMeshSelang = s.gameObject;
        }

        // Matikan mesh 3D Selang statis sejak awal agar tidak ada selang ganda di scene
        if (staticMeshSelang != null && staticMeshSelang.activeSelf)
        {
            staticMeshSelang.SetActive(false);
            Debug.Log("[APAR] 🙈 Mesh 3D 'Selang' statis dinonaktifkan di awal.");
        }

        if (sprayEffect == null)
        {
            ParticleSystem[] psList = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in psList)
            {
                string n = ps.gameObject.name;
                if (n.Equals("Smoke", System.StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("ExtinguisherSmoke", System.StringComparison.OrdinalIgnoreCase))
                {
                    sprayEffect = ps;
                    break;
                }
            }
            if (sprayEffect == null && psList.Length > 0)
                sprayEffect = psList[0];
        }

        if (sprayEffect != null)
        {
            var main = sprayEffect.main;
            main.playOnAwake = false;
            if (sprayEffect.isPlaying)
                sprayEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            nozzleTransform = sprayEffect.transform;
        }
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
        // ── 1. Cek Input Tombol X dan Y (Cabut PIN) ────────────────────────
        CheckPinPullInput();

        // ── 2. Cek Input Trigger / Gagang ─────────────────────────────────
        CheckTriggerInput();

        // ── 3. Evaluasi Kondisi Spray ──────────────────────────────────────
        bool isPropSpraying  = (propStateMachine != null && propStateMachine.IsSpraying);
        bool isKeyboardSpray = pinPulled && (Keyboard.current != null && Keyboard.current.spaceKey.isPressed);
        bool isVRSpraying    = pinPulled && (isHoseHeld || isMainHandleHeld) && isTriggerPressedOnController;
        bool isAnySpraying   = pinPulled && (isVRSpraying || isPropSpraying || isKeyboardSpray);

        bool shouldSpray     = isAnySpraying || debugForceSpray;

        if (shouldSpray && !wasSprayingLastFrame)      StartSpray();
        else if (!shouldSpray && wasSprayingLastFrame) StopSpray();

        wasSprayingLastFrame = shouldSpray;

        if (shouldSpray) ExtinguishFiresGradually();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  INPUT HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Memeriksa tombol X atau Y pada VR Controller (atau key X/Y di keyboard) untuk cabut PIN.
    /// </summary>
    private void CheckPinPullInput()
    {
        if (pinPulled) return;
        if (!isMissionStarted && !isAttachedToHand) return;

        bool pullTriggered = false;
        string source = "";

        // A. Keyboard (X / Y key)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.xKey.wasPressedThisFrame || Keyboard.current.yKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)
            {
                pullTriggered = true;
                source = "Keyboard (X/Y/P)";
            }
        }

        // B. VR Controller Primary/Secondary Button (X/Y di Left Controller, A/B di Right Controller)
        if (!pullTriggered)
        {
            var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftHand.isValid)
            {
                if ((leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool xBtn) && xBtn) ||
                    (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool yBtn) && yBtn))
                {
                    pullTriggered = true;
                    source = "Left Controller (Tombol X/Y)";
                }
            }

            var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!pullTriggered && rightHand.isValid)
            {
                if ((rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool aBtn) && aBtn) ||
                    (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bBtn) && bBtn))
                {
                    pullTriggered = true;
                    source = "Right Controller (Tombol A/B)";
                }
            }
        }

        if (pullTriggered)
        {
            PullPinFromInput(source);
        }
    }

    public void PullPinFromInput(string inputSource = "VR Controller")
    {
        if (pinPulled) return;

        pinPulled = true;
        Debug.Log($"[APAR] 🔑 PIN DICABUT via {inputSource}! APAR siap digunakan.");

        // Cari script APARPin dan triggernya
        APARPin aparPin = GetComponentInChildren<APARPin>();
        if (aparPin != null)
        {
            // Panggil melepaskan pin visual
            var pinTransform = aparPin.transform;
            if (pinTransform != null && pinTransform.parent != null)
                pinTransform.SetParent(null);
        }

        if (propStateMachine != null)
            propStateMachine.PullPin();
    }

    /// <summary>
    /// Memeriksa status penekanan Trigger pada controller.
    /// </summary>
    private void CheckTriggerInput()
    {
        isTriggerPressedOnController = false;

        // Cek trigger dari Left & Right Controller
        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.isValid)
        {
            if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float leftTrig) && leftTrig > 0.15f)
            {
                isTriggerPressedOnController = true;
                return;
            }
        }

        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHand.isValid)
        {
            if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float rightTrig) && rightTrig > 0.15f)
            {
                isTriggerPressedOnController = true;
                return;
            }
        }

        // Fallback editor / keyboard space
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        {
            isTriggerPressedOnController = true;
        }
    }

    /// <summary>Panggil saat misi resmi dimulai — body tabung bisa di-grab.</summary>
    public void SetMissionStarted()
    {
        isMissionStarted = true;
        if (grabInteractable != null)
            grabInteractable.enabled = true;
        Debug.Log("[APAR] ✅ Misi dimulai — grab tabung APAR sekarang aktif!");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GRAB EVENTS — Tabung APAR → Tangan KIRI
    // ═══════════════════════════════════════════════════════════════════════

    private void OnGrabEnter(SelectEnterEventArgs args)
    {
        if (!isMissionStarted)
        {
            Debug.Log("[APAR] 🔒 Grab tabung diblokir — misi belum dimulai!");
            return;
        }

        isMainHandleHeld = true;

        if (args.interactorObject != null)
        {
            VRHandAnimator handAnim = args.interactorObject.transform.GetComponentInParent<VRHandAnimator>();
            if (handAnim != null) handAnim.SetForceGrip(true);
        }

        // Attach tabung APAR ke Tangan KIRI dengan offset yang rapi (tidak menutupi muka)
        if (!isAttachedToHand && args.interactorObject != null)
        {
            isAttachedToHand = true;

            Transform leftHandTransform = args.interactorObject.transform;
            transform.SetParent(leftHandTransform);
            transform.localPosition = handOffsetPosition;
            transform.localRotation = Quaternion.Euler(handOffsetRotation);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (grabInteractable != null)
                grabInteractable.enabled = false;

            // Nonaktifkan mesh 3D Selang statis bawaan model agar tidak mengganggu selang elastis
            if (staticMeshSelang != null)
            {
                staticMeshSelang.SetActive(false);
                Debug.Log("[APAR] 🙈 Mesh 3D 'Selang' statis dinonaktifkan!");
            }

            Debug.Log("[APAR] 🤚 Tabung ter-attach ke Tangan KIRI.");

            // 🌟 OTOMATIS AKTIFKAN TANGAN KANAN UNTUK MEMEGANG CORONG
            APARHoseGrabber hoseGrabber = GetComponentInChildren<APARHoseGrabber>();
            if (hoseGrabber != null)
            {
                hoseGrabber.AutoGrabRightHand();
            }
        }
    }

    private void OnGrabExit(SelectExitEventArgs args)
    {
        if (isAttachedToHand)
        {
            isMainHandleHeld = true;
            return;
        }

        isMainHandleHeld = false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  EKSTINGSI API — raycast dari moncong Corong (Smoke)
    // ═══════════════════════════════════════════════════════════════════════

    private void ExtinguishFiresGradually()
    {
        Transform nozzle = nozzleTransform != null ? nozzleTransform :
                           (sprayEffect != null ? sprayEffect.transform : transform);

        Debug.DrawRay(nozzle.position, nozzle.forward * extinguishRange, Color.cyan);

        RaycastHit[] hits = Physics.SphereCastAll(nozzle.position, 0.4f, nozzle.forward, extinguishRange);
        foreach (RaycastHit hit in hits)
        {
            FireExtinguisherTarget target = hit.collider.GetComponentInParent<FireExtinguisherTarget>();
            if (target != null)
                target.ExtinguishGradually(Time.deltaTime);
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

        Debug.Log("[APAR] ✅ Spray NYALA — Asap menyemprot dari Corong.");
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
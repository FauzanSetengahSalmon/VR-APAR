using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class APARHoseGrabber : MonoBehaviour
{
    [Header("Referensi Utama")]
    [Tooltip("Script AutoFireExtinguisher pada root APAR Full")]
    public AutoFireExtinguisher mainExtinguisher;

    [Tooltip("Transform pangkal selang di bodi tabung. Diisi otomatis jika kosong.")]
    public Transform hoseBodyOutlet;

    [Header("Pengaturan Selang Visual")]
    [Tooltip("Ketebalan selang karet")]
    public float hoseThickness = 0.025f;

    [Tooltip("Kelenturan gravitasi selang (sag). Makin besar makin melengkung ke bawah.")]
    public float hoseSagAmount = 0.22f;

    [Tooltip("Kecepatan corong mengikuti tangan kanan (smooth lerp)")]
    public float followSmoothSpeed = 25f;

    [Tooltip("Offset posisi lokal corong saat digenggam tangan kanan")]
    public Vector3 nozzleHoldOffset = new Vector3(0.05f, -0.15f, 0.35f);
    [Tooltip("Offset rotasi lokal corong saat digenggam tangan kanan (putar 180 jika kebalik)")]
    public Vector3 nozzleHoldRotOffset = new Vector3(0f, 180f, 0f);

    [Tooltip("Offset lokal titik sambungan selang di belakang corong (pangkal belakang corong)")]
     public Vector3 hoseNozzleConnectOffset = new Vector3(0f, 0f, 0.030f);

    // ── Private State ───────────────────────────────────────────────────────
    private XRGrabInteractable hoseGrabInteractable;
    private Transform rightHandTransform;   // Tangan KANAN memegang corong
    private bool isHoseGrabbed = false;

    private LineRenderer lr;
    private Vector3 nozzleRestLocalPos;
    private Quaternion nozzleRestLocalRot;

    private const int HOSE_SEGMENTS = 50;   // Lebih banyak → lebih mulus
    private Transform smokeTransform;        // Cache Smoke child

    // ── Mission Lock ─────────────────────────────────────────────────────────
    private bool isMissionStarted = false;

    // ═══════════════════════════════════════════════════════════════════════
    //  AWAKE / START
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // --- Setup XRGrabInteractable ----------------------------------------
        hoseGrabInteractable = GetComponent<XRGrabInteractable>();
        if (hoseGrabInteractable == null)
            hoseGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();

        hoseGrabInteractable.movementType      = XRBaseInteractable.MovementType.Instantaneous;
        hoseGrabInteractable.trackPosition     = false;
        hoseGrabInteractable.trackRotation     = false;

        // --- Rigidbody kinematic ─────────────────────────────────────────────
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        if (mainExtinguisher == null)
            mainExtinguisher = GetComponentInParent<AutoFireExtinguisher>();

        nozzleRestLocalPos = transform.localPosition;
        nozzleRestLocalRot = transform.localRotation;

        FindHoseOutlet();
        SetupHoseLineRenderer();
    }

    private void Start()
    {
        SetupSmokeReference();

        if (mainExtinguisher != null)
            mainExtinguisher.nozzleTransform = smokeTransform != null ? smokeTransform : transform;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  AUTO-ATTACH RIGHT HAND
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dipanggil otomatis saat Tabung APAR di-grab Tangan Kiri.
    /// Mencari Tangan Kanan di scene dan langsung meng-attach Corong ke Tangan Kanan.
    /// </summary>
    public void AutoGrabRightHand()
    {
        if (rightHandTransform != null && isHoseGrabbed) return;

        Transform foundRightHand = FindRightHandTransform();
        if (foundRightHand != null)
        {
            rightHandTransform = foundRightHand;
            isHoseGrabbed = true;

            if (mainExtinguisher != null)
                mainExtinguisher.isHoseHeld = true;

            VRHandAnimator handAnim = foundRightHand.GetComponentInParent<VRHandAnimator>();
            if (handAnim != null) handAnim.SetForceGrip(true);

            // Sembunyikan mesh 3D Selang statis agar tidak mengganggu selang elastis
            if (mainExtinguisher != null && mainExtinguisher.staticMeshSelang != null)
            {
                mainExtinguisher.staticMeshSelang.SetActive(false);
            }
            else if (transform.parent != null)
            {
                Transform s = transform.parent.Find("Selang");
                if (s != null) s.gameObject.SetActive(false);
            }

            Debug.Log($"[APARHoseGrabber] 🤝 Corong OTOMATIS ter-attach ke Tangan Kanan ('{foundRightHand.name}')!");
        }
        else
        {
            Debug.LogWarning("[APARHoseGrabber] ⚠️ Tangan Kanan belum ditemukan di scene. Corong dapat di-grab secara manual.");
        }
    }

    private Transform FindRightHandTransform()
    {
        // 1. Cari berdasarkan GameObject name
        string[] searchNames = {
            "RightHand Controller", "Right Controller", "RightHandDirectInteractor",
            "RightHand", "Right Interaction Follower", "RightHand Index-Tip"
        };

        foreach (string n in searchNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) return go.transform;
        }

        // 2. Cari berdasarkan XRBaseInteractor dengan tag/nama 'Right'
        var interactors = FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None);
        foreach (var interactor in interactors)
        {
            string name = interactor.gameObject.name.ToLower();
            if (name.Contains("right") && !name.Contains("ui"))
            {
                return interactor.transform;
            }
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SETUP HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private void FindHoseOutlet()
    {
        if (hoseBodyOutlet != null) return;
        if (transform.parent == null) return;

        Transform root = transform.parent;
        string[] outletNames = { "HoseOrigin", "SelangOrigin", "HoseStart", "mid_tube_p1_low", "fire_tube_low" };
        foreach (string n in outletNames)
        {
            hoseBodyOutlet = root.Find(n);
            if (hoseBodyOutlet != null) break;
        }

        if (hoseBodyOutlet == null)
        {
            Transform tabung = root.Find("Tabung");
            if (tabung == null) tabung = root.Find("Selang");
            if (tabung != null) hoseBodyOutlet = tabung;
        }

        if (hoseBodyOutlet == null) hoseBodyOutlet = root;

        Debug.Log($"[APARHoseGrabber] 🔗 HoseOutlet ditemukan: '{hoseBodyOutlet?.name}'");
    }

    private void SetupSmokeReference()
    {
        ParticleSystem[] list = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in list)
        {
            string n = ps.gameObject.name;
            if (n.Equals("Smoke", System.StringComparison.OrdinalIgnoreCase) ||
                n.Equals("ExtinguisherSmoke", System.StringComparison.OrdinalIgnoreCase))
            {
                smokeTransform = ps.transform;

                if (mainExtinguisher != null && mainExtinguisher.sprayEffect == null)
                    mainExtinguisher.sprayEffect = ps;

                Debug.Log($"[APARHoseGrabber] 💨 Smoke '{ps.gameObject.name}' ditemukan di Corong.");
                break;
            }
        }

        if (smokeTransform == null && list.Length > 0)
        {
            smokeTransform = list[0].transform;
            if (mainExtinguisher != null && mainExtinguisher.sprayEffect == null)
                mainExtinguisher.sprayEffect = list[0];
        }

        if (smokeTransform == null && mainExtinguisher != null && mainExtinguisher.sprayEffect != null)
        {
            smokeTransform = mainExtinguisher.sprayEffect.transform;
        }

        // Pastikan smokeTransform terikat sebagai child dari Corong (transform) agar selalu mengikuti arah corong
        if (smokeTransform != null && smokeTransform.parent != transform)
        {
            smokeTransform.SetParent(transform, true);
            Debug.Log("[APARHoseGrabber] 💨 Smoke particle system diparentkan secara eksplisit ke Corong.");
        }
    }

    private void SetupHoseLineRenderer()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        lr.positionCount  = HOSE_SEGMENTS;
        lr.startWidth     = hoseThickness;
        lr.endWidth       = hoseThickness * 0.80f;
        lr.useWorldSpace  = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.numCapVertices = 4;

        Material hoseMat = new Material(Shader.Find("Sprites/Default"));
        hoseMat.color = new Color(0.10f, 0.10f, 0.12f, 1f);
        lr.material = hoseMat;

        lr.enabled = true;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ENABLE / DISABLE / GRAB
    // ═══════════════════════════════════════════════════════════════════════

    private void OnEnable()
    {
        if (hoseGrabInteractable != null)
        {
            hoseGrabInteractable.selectEntered.AddListener(OnHoseGrabbed);
            hoseGrabInteractable.selectExited.AddListener(OnHoseReleased);
        }
        if (lr != null) lr.enabled = true;
    }

    private void OnDisable()
    {
        if (hoseGrabInteractable != null)
        {
            hoseGrabInteractable.selectEntered.RemoveListener(OnHoseGrabbed);
            hoseGrabInteractable.selectExited.RemoveListener(OnHoseReleased);
        }
        if (lr != null) lr.enabled = false;
    }

    private void OnHoseGrabbed(SelectEnterEventArgs args)
    {
        if (!isMissionStarted) return;

        isHoseGrabbed = true;
        if (args.interactorObject != null)
        {
            rightHandTransform = args.interactorObject.transform;

            VRHandAnimator handAnim = args.interactorObject.transform.GetComponentInParent<VRHandAnimator>();
            if (handAnim != null) handAnim.SetForceGrip(true);
        }

        if (mainExtinguisher != null)
            mainExtinguisher.isHoseHeld = true;

        if (lr != null) lr.enabled = true;
    }

    private void OnHoseReleased(SelectExitEventArgs args)
    {
        if (!isMissionStarted) return;

        isHoseGrabbed = false;
        if (args.interactorObject != null)
        {
            VRHandAnimator handAnim = args.interactorObject.transform.GetComponentInParent<VRHandAnimator>();
            if (handAnim != null) handAnim.SetForceGrip(false);
        }

        rightHandTransform = null;

        if (mainExtinguisher != null)
            mainExtinguisher.isHoseHeld = false;

        if (lr != null) lr.enabled = true;
    }

    public void SetMissionStarted()
    {
        isMissionStarted = true;
    }

    /// <summary>
    /// Lepas corong dari tangan kanan dan kembalikan ke posisi awal pada bodi APAR.
    /// </summary>
    public void ResetToRestPosition()
    {
        if (rightHandTransform != null)
        {
            VRHandAnimator handAnim = rightHandTransform.GetComponentInParent<VRHandAnimator>();
            if (handAnim != null) handAnim.SetForceGrip(false);
        }

        isHoseGrabbed = false;
        rightHandTransform = null;

        if (mainExtinguisher != null)
            mainExtinguisher.isHoseHeld = false;

        if (transform.parent != null)
        {
            transform.localPosition = nozzleRestLocalPos;
            transform.localRotation = nozzleRestLocalRot;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════════════════════

    private void Update()
    {
        // ── Gerakan Corong mengikuti Tangan Kanan ─────────────────────────
        if (isHoseGrabbed && rightHandTransform != null)
        {
            Vector3 targetPos = rightHandTransform.TransformPoint(nozzleHoldOffset);
            Quaternion targetRot = rightHandTransform.rotation * Quaternion.Euler(nozzleHoldRotOffset);

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSmoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSmoothSpeed);
        }
        else if (transform.parent != null)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, nozzleRestLocalPos, Time.deltaTime * 12f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, nozzleRestLocalRot, Time.deltaTime * 12f);
        }

        if (hoseGrabInteractable != null && mainExtinguisher != null)
        {
            bool sel = hoseGrabInteractable.isSelected || hoseGrabInteractable.interactorsSelecting.Count > 0;
            mainExtinguisher.isHoseHeld = sel || isHoseGrabbed;
        }

        DrawElasticHose();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SELANG ELASTIS — Cubic Bezier dengan tangent realistis
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawElasticHose()
    {
        if (lr == null) return;
        if (!lr.enabled) lr.enabled = true;

        Vector3 p0 = (hoseBodyOutlet != null)
            ? hoseBodyOutlet.position
            : (transform.parent != null ? transform.parent.position : transform.position);

        // Titik AKHIR selang: pangkal belakang corong (bukan di tengah pivot corong)
        Vector3 p3 = transform.TransformPoint(hoseNozzleConnectOffset);

        float dist = Vector3.Distance(p0, p3);
        Vector3 outDir = (hoseBodyOutlet != null) ? hoseBodyOutlet.up : Vector3.up;

        // Tangent masuk tepat dari pangkal belakang corong
        Vector3 inDir = hoseNozzleConnectOffset != Vector3.zero
            ? transform.TransformDirection(hoseNozzleConnectOffset.normalized)
            : -transform.forward;

        float handle = Mathf.Clamp(dist * 0.55f, 0.08f, 0.70f);
        float sag = hoseSagAmount * Mathf.Clamp01(dist / 0.6f);

        Vector3 p1 = p0 + outDir * handle + Vector3.down * sag * 0.45f;
        Vector3 p2 = p3 + inDir  * handle + Vector3.down * sag * 0.65f;

        for (int i = 0; i < HOSE_SEGMENTS; i++)
        {
            float t = i / (float)(HOSE_SEGMENTS - 1);
            lr.SetPosition(i, CubicBezier(t, p0, p1, p2, p3));
        }
    }

    private static Vector3 CubicBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u   = 1f - t;
        float tt  = t * t;
        float uu  = u * u;
        return (uu * u) * p0
             + (3f * uu * t) * p1
             + (3f * u * tt) * p2
             + (tt * t)      * p3;
    }
}
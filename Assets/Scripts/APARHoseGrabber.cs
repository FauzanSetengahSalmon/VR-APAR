using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Script Kontrol Selang APAR Fleksibel (Meta Quest 3).
/// 
/// FUNGSI:
///   1. Rapi & Clean (Tanpa Double Hose):
///      - Saat selang TIDAK digenggam: LineRenderer DINONAKTIFKAN agar model 3D APAR terlihat rapi dan alami.
///      - Saat selang DIGENGGAM Tangan Kiri: LineRenderer OTOMATIS AKTIF, membentang melengkung dari bodi ke tangan kiri.
///   2. Anti-Jitter / Anti-Kejut: Movement 100% smooth tanpa bentrokan fisika.
///   3. Semprotan Smoke Presisi: Mengikuti moncong selang di tangan kiri.
/// </summary>
public class APARHoseGrabber : MonoBehaviour
{
    [Header("Referensi Utama")]
    [Tooltip("Script AutoFireExtinguisher utama")]
    public AutoFireExtinguisher mainExtinguisher;

    [Tooltip("Transform pangkal keluar selang dari bodi APAR (misal: HoseOrigin)")]
    public Transform hoseBodyOutlet;

    [Header("Pengaturan Selang Visual")]
    [Tooltip("Ketebalan selang karet hitam")]
    public float hoseThickness = 0.028f;

    [Tooltip("Kelenturan lengkungan selang ke bawah saat ditarik")]
    public float hoseSagAmount = 0.15f;

    [Tooltip("Kecepatan kehalusan ikuti tangan kiri")]
    public float followSmoothSpeed = 30f;

    // ── Private Internal State ──────────────────────────────────────────────
    private XRGrabInteractable hoseGrabInteractable;
    private Transform leftHandTransform;
    private bool isHoseGrabbed = false;
    private LineRenderer hoseLineRenderer;
    private Vector3 nozzleRestLocalPos;
    private Quaternion nozzleRestLocalRot;
    private const int HOSE_SEGMENTS = 20;

    // ── Mission Lock ─────────────────────────────────────────────────────────
    // Selang tidak bisa di-grab sampai misi resmi dimulai
    private bool isMissionStarted = false;

    private void Awake()
    {
        // Setup interactable
        hoseGrabInteractable = GetComponent<XRGrabInteractable>();
        if (hoseGrabInteractable == null)
            hoseGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();

        hoseGrabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        hoseGrabInteractable.trackPosition = false; // Kita kelola posisi di Update agar 100% halus tanpa kejut
        hoseGrabInteractable.trackRotation = false;

        // Pastikan fisika tidak bentrok
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (mainExtinguisher == null)
            mainExtinguisher = GetComponentInParent<AutoFireExtinguisher>();

        // Simpan posisi & rotasi resting awal
        nozzleRestLocalPos = transform.localPosition;
        nozzleRestLocalRot = transform.localRotation;

        AutoFindHoseOutlet();
        SetupHoseLineRenderer();
    }

    private void AutoFindHoseOutlet()
    {
        if (hoseBodyOutlet == null && transform.parent != null)
        {
            hoseBodyOutlet = transform.parent.Find("HoseOrigin");
            if (hoseBodyOutlet == null) hoseBodyOutlet = transform.parent.Find("mid_tube_p1_low");
            if (hoseBodyOutlet == null) hoseBodyOutlet = transform.parent.Find("fire_tube_low");
        }
    }

    private void SetupHoseLineRenderer()
    {
        hoseLineRenderer = GetComponent<LineRenderer>();
        if (hoseLineRenderer == null)
            hoseLineRenderer = gameObject.AddComponent<LineRenderer>();

        hoseLineRenderer.positionCount = HOSE_SEGMENTS;
        hoseLineRenderer.startWidth = hoseThickness;
        hoseLineRenderer.endWidth = hoseThickness * 0.85f;
        hoseLineRenderer.useWorldSpace = true;
        hoseLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hoseLineRenderer.receiveShadows = false;

        Material hoseMat = new Material(Shader.Find("Sprites/Default"));
        hoseMat.color = new Color(0.12f, 0.12f, 0.14f, 1f); // Hitam karet doff
        hoseLineRenderer.material = hoseMat;

        // Mati di awal agar tidak tumpang tindih dengan mesh bawaan 3D model APAR
        hoseLineRenderer.enabled = false;
    }

    private void OnEnable()
    {
        if (hoseGrabInteractable != null)
        {
            hoseGrabInteractable.selectEntered.AddListener(OnHoseGrabbed);
            hoseGrabInteractable.selectExited.AddListener(OnHoseReleased);
        }
    }

    private void OnDisable()
    {
        if (hoseGrabInteractable != null)
        {
            hoseGrabInteractable.selectEntered.RemoveListener(OnHoseGrabbed);
            hoseGrabInteractable.selectExited.RemoveListener(OnHoseReleased);
        }
        if (hoseLineRenderer != null) hoseLineRenderer.enabled = false;
    }

    private void OnHoseGrabbed(SelectEnterEventArgs args)
    {
        // Blokir grab sebelum misi dimulai
        if (!isMissionStarted)
        {
            Debug.Log("[APARHoseGrabber] 🔒 Grab selang diblokir — misi belum dimulai!");
            return;
        }

        Debug.Log("[APARHoseGrabber] 🖐️ Selang APAR digenggam Tangan Kiri!");
        isHoseGrabbed = true;

        if (args.interactorObject != null)
        {
            leftHandTransform = args.interactorObject.transform;
            VRHandAnimator handAnim = args.interactorObject.transform.GetComponentInParent<VRHandAnimator>();
            if (handAnim != null) handAnim.SetForceGrip(true);
        }

        if (mainExtinguisher != null)
        {
            mainExtinguisher.isHoseHeld = true;
        }

        // Nyalakan LineRenderer selang lentur saat digenggam
        if (hoseLineRenderer != null)
        {
            hoseLineRenderer.enabled = true;
        }
    }

    private void OnHoseReleased(SelectExitEventArgs args)
    {
        // Jika misi belum dimulai, tidak ada yang perlu direset
        if (!isMissionStarted) return;

        Debug.Log("[APARHoseGrabber] 🖐️ Selang APAR dilepas.");
        isHoseGrabbed = false;

        if (args.interactorObject != null)
        {
            VRHandAnimator handAnim = args.interactorObject.transform.GetComponentInParent<VRHandAnimator>();
            if (handAnim != null) handAnim.SetForceGrip(false);
        }

        leftHandTransform = null;

        if (mainExtinguisher != null)
        {
            mainExtinguisher.isHoseHeld = false;
        }

        // Matikan LineRenderer selang saat dilepas (kembali ke model bawaan APAR)
        if (hoseLineRenderer != null)
        {
            hoseLineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Panggil method ini (dari VRSimulationUIManager) saat misi resmi dimulai.
    /// Setelah dipanggil, selang bisa di-grab.
    /// </summary>
    public void SetMissionStarted()
    {
        isMissionStarted = true;
        Debug.Log("[APARHoseGrabber] ✅ Misi dimulai — grab selang sekarang aktif!");
    }

    private void Update()
    {
        // ── 1. GERAKAN MULUS HOSE NOZZLE MENGIKUTI TANGAN KIRI ───────────────
        if (isHoseGrabbed && leftHandTransform != null)
        {
            transform.position = Vector3.Lerp(transform.position, leftHandTransform.position, Time.deltaTime * followSmoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, leftHandTransform.rotation, Time.deltaTime * followSmoothSpeed);

            // Update kurva selang lentur
            UpdateVisualHoseCurve();
        }
        else if (transform.parent != null)
        {
            // Kembalikan HoseNozzle ke posisi resting di bodi APAR
            transform.localPosition = Vector3.Lerp(transform.localPosition, nozzleRestLocalPos, Time.deltaTime * 15f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, nozzleRestLocalRot, Time.deltaTime * 15f);

            if (hoseLineRenderer != null && hoseLineRenderer.enabled)
            {
                hoseLineRenderer.enabled = false;
            }
        }

        // ── 2. SYNC STATUS HOLD SAFETY ──────────────────────────────────────
        if (hoseGrabInteractable != null && mainExtinguisher != null)
        {
            bool currentlySelected = hoseGrabInteractable.isSelected || hoseGrabInteractable.interactorsSelecting.Count > 0;
            mainExtinguisher.isHoseHeld = currentlySelected || isHoseGrabbed;
        }
    }

    private void UpdateVisualHoseCurve()
    {
        if (hoseLineRenderer == null || !hoseLineRenderer.enabled) return;

        Vector3 startPos = (hoseBodyOutlet != null) ? hoseBodyOutlet.position : (transform.parent != null ? transform.parent.position : transform.position);
        Vector3 endPos = transform.position;

        float dist = Vector3.Distance(startPos, endPos);
        float dynamicSag = hoseSagAmount * Mathf.Clamp01(dist / 0.8f);
        Vector3 midPos = (startPos + endPos) * 0.5f + (Vector3.down * dynamicSag);

        for (int i = 0; i < HOSE_SEGMENTS; i++)
        {
            float t = i / (float)(HOSE_SEGMENTS - 1);
            Vector3 pointOnCurve = CalculateQuadraticBezierPoint(t, startPos, midPos, endPos);
            hoseLineRenderer.SetPosition(i, pointOnCurve);
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1f - t;
        return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class APARHoseGrabber : MonoBehaviour
{
    [Header("Referensi Utama")]
    [Tooltip("Script AutoFireExtinguisher pada root APAR Full")]
    public AutoFireExtinguisher mainExtinguisher;

    [Header("Referensi Tangan Kanan")]
    public Transform rightHandTransformManual;

    [Header("Pengaturan Gerakan Corong")]
    [Tooltip("Kecepatan corong mengikuti tangan kanan")]
    public float followSmoothSpeed = 25f;

    [Tooltip("Offset posisi lokal corong saat digenggam tangan kanan")]
    public Vector3 nozzleHoldOffset = new Vector3(0.05f, -0.15f, 0.35f);

    [Tooltip("Offset rotasi lokal corong saat digenggam tangan kanan")]
    public Vector3 nozzleHoldRotOffset = new Vector3(0f, 180f, 0f);

    // ── Private State ───────────────────────────────────────────────────────

    private XRGrabInteractable hoseGrabInteractable;

    private Transform rightHandTransform;

    private bool isHoseGrabbed = false;

    // Variabel penampung posisi & rotasi World Space
    private Vector3 nozzleRestWorldPos;
    private Quaternion nozzleRestWorldRot;

    private Transform smokeTransform;

    // ── Mission Lock ─────────────────────────────────────────────────────────

    private bool isMissionStarted = false;


    // ═══════════════════════════════════════════════════════════════════════
    // AWAKE / START
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // ---------------------------------------------------------
        // Setup XR Grab Interactable
        // ---------------------------------------------------------

        hoseGrabInteractable = GetComponent<XRGrabInteractable>();

        if (hoseGrabInteractable == null)
            hoseGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();

        hoseGrabInteractable.movementType =
            XRBaseInteractable.MovementType.Instantaneous;

        // Posisi dan rotasi corong kita atur sendiri melalui script
        hoseGrabInteractable.trackPosition = false;
        hoseGrabInteractable.trackRotation = false;


        // ---------------------------------------------------------
        // Rigidbody
        // ---------------------------------------------------------

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }


        // ---------------------------------------------------------
        // Collider
        // ---------------------------------------------------------

        Collider col = GetComponent<Collider>();

        if (col != null)
            col.isTrigger = true;


        // ---------------------------------------------------------
        // Cari AutoFireExtinguisher
        // ---------------------------------------------------------

        if (mainExtinguisher == null)
            mainExtinguisher = GetComponentInParent<AutoFireExtinguisher>();

        // Simpan posisi & rotasi murni di WORLD SPACE saat Start/Awake
        nozzleRestWorldPos = transform.position;
        nozzleRestWorldRot = transform.rotation;
    }


    private void Start()
    {
        SetupSmokeReference();

        // Beritahu AutoFireExtinguisher posisi smoke/nozzle
        if (mainExtinguisher != null)
        {
            mainExtinguisher.nozzleTransform =
                smokeTransform != null
                    ? smokeTransform
                    : transform;
        }
    }


    // ═══════════════════════════════════════════════════════════════════════
    // AUTO GRAB RIGHT HAND
    // ═══════════════════════════════════════════════════════════════════════

    public void AutoGrabRightHand()
    {
        if (rightHandTransform != null && isHoseGrabbed)
            return;

        Transform foundRightHand = rightHandTransformManual;

        if (foundRightHand == null)
            foundRightHand = FindRightHandTransform();


        if (foundRightHand != null)
        {
            rightHandTransform = foundRightHand;
            isHoseGrabbed = true;


            if (mainExtinguisher != null)
                mainExtinguisher.isHoseHeld = true;


            VRHandAnimator handAnim =
                foundRightHand.GetComponentInParent<VRHandAnimator>();

            if (handAnim != null)
                handAnim.SetForceGrip(true);


            // Selang asli dimatikan.
            // Selang pengganti sekarang ditangani oleh
            // StretchBetweenPoint.
            if (mainExtinguisher != null &&
                mainExtinguisher.staticMeshSelang != null)
            {
                mainExtinguisher.staticMeshSelang.SetActive(false);
            }


            Debug.Log(
                $"[APARHoseGrabber] 🤝 Corong otomatis mengikuti tangan kanan: {foundRightHand.name}"
            );
        }
        else
        {
            Debug.LogError(
                "[APARHoseGrabber] ❌ Tangan kanan tidak ditemukan!"
            );
        }
    }


    // ═══════════════════════════════════════════════════════════════════════
    // FIND RIGHT HAND
    // ═══════════════════════════════════════════════════════════════════════

    private Transform FindRightHandTransform()
    {
        // ---------------------------------------------------------
        // Cari berdasarkan nama
        // ---------------------------------------------------------

        string[] searchNames =
        {
            "RightHand Controller",
            "Right Controller",
            "RightHandDirectInteractor",
            "RightHand",
            "Right Interaction Follower",
            "RightHand Index-Tip"
        };


        foreach (string n in searchNames)
        {
            GameObject go = GameObject.Find(n);

            if (go != null)
                return go.transform;
        }


        // ---------------------------------------------------------
        // Cari XR Interactor dengan nama Right
        // ---------------------------------------------------------

        var interactors =
            FindObjectsByType<XRBaseInteractor>(
                FindObjectsSortMode.None
            );


        foreach (var interactor in interactors)
        {
            string name =
                interactor.gameObject.name.ToLower();

            if (name.Contains("right") &&
                !name.Contains("ui"))
            {
                return interactor.transform;
            }
        }


        return null;
    }


    // ═══════════════════════════════════════════════════════════════════════
    // SMOKE REFERENCE
    // ═══════════════════════════════════════════════════════════════════════

    private void SetupSmokeReference()
    {
        ParticleSystem[] list =
            GetComponentsInChildren<ParticleSystem>(true);


        // ---------------------------------------------------------
        // Cari Smoke
        // ---------------------------------------------------------

        foreach (var ps in list)
        {
            string n = ps.gameObject.name;


            if (n.Equals(
                    "Smoke",
                    System.StringComparison.OrdinalIgnoreCase)
                ||
                n.Equals(
                    "ExtinguisherSmoke",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                smokeTransform = ps.transform;


                if (mainExtinguisher != null &&
                    mainExtinguisher.sprayEffect == null)
                {
                    mainExtinguisher.sprayEffect = ps;
                }


                Debug.Log(
                    $"[APARHoseGrabber] 💨 Smoke '{ps.gameObject.name}' ditemukan di Corong."
                );

                break;
            }
        }


        // ---------------------------------------------------------
        // Fallback kalau tidak menemukan nama Smoke
        // ---------------------------------------------------------

        if (smokeTransform == null &&
            list.Length > 0)
        {
            smokeTransform = list[0].transform;


            if (mainExtinguisher != null &&
                mainExtinguisher.sprayEffect == null)
            {
                mainExtinguisher.sprayEffect = list[0];
            }
        }


        // ---------------------------------------------------------
        // Fallback terakhir
        // ---------------------------------------------------------

        if (smokeTransform == null &&
            mainExtinguisher != null &&
            mainExtinguisher.sprayEffect != null)
        {
            smokeTransform =
                mainExtinguisher.sprayEffect.transform;
        }


        // ---------------------------------------------------------
        // Pastikan Smoke menjadi child Corong
        // ---------------------------------------------------------

        if (smokeTransform != null &&
            smokeTransform.parent != transform)
        {
            smokeTransform.SetParent(transform, true);

            Debug.Log(
                "[APARHoseGrabber] 💨 Smoke particle system diparentkan ke Corong."
            );
        }
    }


    // ═══════════════════════════════════════════════════════════════════════
    // ENABLE / DISABLE
    // ═══════════════════════════════════════════════════════════════════════

    private void OnEnable()
    {
        if (hoseGrabInteractable != null)
        {
            hoseGrabInteractable.selectEntered
                .AddListener(OnHoseGrabbed);

            hoseGrabInteractable.selectExited
                .AddListener(OnHoseReleased);
        }
    }


    private void OnDisable()
    {
        if (hoseGrabInteractable != null)
        {
            hoseGrabInteractable.selectEntered
                .RemoveListener(OnHoseGrabbed);

            hoseGrabInteractable.selectExited
                .RemoveListener(OnHoseReleased);
        }
    }


    // ═══════════════════════════════════════════════════════════════════════
    // GRAB CORONG
    // ═══════════════════════════════════════════════════════════════════════

    private void OnHoseGrabbed(SelectEnterEventArgs args)
    {
        if (!isMissionStarted)
            return;


        // ---------------------------------------------------------
        // Kalau APAR belum diambil
        // otomatis ambil APAR + corong
        // ---------------------------------------------------------

        if (mainExtinguisher != null &&
            !mainExtinguisher.isAttachedToHand)
        {
            mainExtinguisher.TriggerAutoGrabBothHands();
            return;
        }


        isHoseGrabbed = true;


        if (args.interactorObject != null)
        {
            rightHandTransform =
                args.interactorObject.transform;


            VRHandAnimator handAnim =
                args.interactorObject.transform
                    .GetComponentInParent<VRHandAnimator>();


            if (handAnim != null)
                handAnim.SetForceGrip(true);
        }


        if (mainExtinguisher != null)
            mainExtinguisher.isHoseHeld = true;
    }


    // ═══════════════════════════════════════════════════════════════════════
    // RELEASE CORONG
    // ═══════════════════════════════════════════════════════════════════════

    private void OnHoseReleased(SelectExitEventArgs args)
    {
        if (!isMissionStarted)
            return;


        isHoseGrabbed = false;


        if (args.interactorObject != null)
        {
            VRHandAnimator handAnim =
                args.interactorObject.transform
                    .GetComponentInParent<VRHandAnimator>();


            if (handAnim != null)
                handAnim.SetForceGrip(false);
        }


        rightHandTransform = null;


        if (mainExtinguisher != null)
            mainExtinguisher.isHoseHeld = false;
    }


    // ═══════════════════════════════════════════════════════════════════════
    // MISSION
    // ═══════════════════════════════════════════════════════════════════════

    public void SetMissionStarted()
    {
        isMissionStarted = true;
    }


    // ═══════════════════════════════════════════════════════════════════════
    // RESET CORONG
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lepas corong dari tangan kanan dan
    /// kembalikan ke posisi awal pada bodi APAR.
    /// </summary>
    public void ResetToRestPosition()
    {
        if (hoseGrabInteractable != null && hoseGrabInteractable.firstInteractorSelecting != null)
        {
            var manager = hoseGrabInteractable.interactionManager;
            if (manager != null)
            {
                manager.SelectExit(hoseGrabInteractable.firstInteractorSelecting, hoseGrabInteractable);
            }
        }

        if (rightHandTransform != null)
        {
            VRHandAnimator handAnim = rightHandTransform.GetComponentInParent<VRHandAnimator>();
            if (handAnim != null) handAnim.SetForceGrip(false);
        }

        isHoseGrabbed = false;
        rightHandTransform = null;

        if (mainExtinguisher != null)
            mainExtinguisher.isHoseHeld = false;

        // Kembali ke koordinat World awal
        transform.position = nozzleRestWorldPos;
        transform.rotation = nozzleRestWorldRot;
    }


    // ═══════════════════════════════════════════════════════════════════════
    // UPDATE
    // ═══════════════════════════════════════════════════════════════════════

    private void Update()
    {
        // ---------------------------------------------------------
        // Corong mengikuti tangan kanan
        // ---------------------------------------------------------

        if (isHoseGrabbed &&
            rightHandTransform != null)
        {
            Vector3 targetPos =
                rightHandTransform.TransformPoint(
                    nozzleHoldOffset
                );


            Quaternion targetRot =
                rightHandTransform.rotation *
                Quaternion.Euler(
                    nozzleHoldRotOffset
                );


            transform.position =
                Vector3.Lerp(
                    transform.position,
                    targetPos,
                    Time.deltaTime *
                    followSmoothSpeed
                );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    Time.deltaTime *
                    followSmoothSpeed
                );
        }


        // ---------------------------------------------------------
        // Kalau tidak digenggam,
        // kembali perlahan ke posisi world awal (tanpa butuh parent)
        // ---------------------------------------------------------

        else
        {
            transform.position =
                Vector3.Lerp(
                    transform.position,
                    nozzleRestWorldPos,
                    Time.deltaTime * 12f
                );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    nozzleRestWorldRot,
                    Time.deltaTime * 12f
                );
        }


        // ---------------------------------------------------------
        // Sync status hose dengan XR Grab
        // ---------------------------------------------------------

        if (hoseGrabInteractable != null &&
            mainExtinguisher != null)
        {
            bool selected =
                hoseGrabInteractable.isSelected ||
                hoseGrabInteractable.interactorsSelecting.Count > 0;


            mainExtinguisher.isHoseHeld =
                selected || isHoseGrabbed;
        }
    }
}
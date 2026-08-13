using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach script ini ke GameObject pin APAR.
/// Saat pin di-grab (dicabut), flag pinPulled di AutoFireExtinguisher akan aktif.
/// Pin tidak bisa dipasang kembali setelah dicabut.
/// </summary>
public class APARPin : MonoBehaviour
{
    [Header("Referensi")]
    [Tooltip("Referensi ke script AutoFireExtinguisher di body APAR")]
    public AutoFireExtinguisher mainExtinguisher;

    [Header("Pengaturan Pin")]
    [Tooltip("Jarak minimum pin harus ditarik dari posisi awal agar dianggap tercabut")]
    public float pullDistanceThreshold = 0.05f;

    [Tooltip("Jika true, pin langsung tercabut saat pertama kali di-grab (tanpa perlu jarak)")]
    public bool pullOnFirstGrab = true;

    private XRGrabInteractable pinGrabInteractable;
    private bool isPinPulled = false;
    private Vector3 initialLocalPosition;
    private Transform initialParent;

    // ── Mission Lock ─────────────────────────────────────────────────────────
    // Pin tidak bisa di-grab sampai misi resmi dimulai
    private bool isMissionStarted = false;

    private void Awake()
    {
        pinGrabInteractable = GetComponent<XRGrabInteractable>();
        if (pinGrabInteractable == null)
            pinGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();

        // Simpan posisi awal pin (sebelum dicabut)
        initialLocalPosition = transform.localPosition;
        initialParent = transform.parent;
    }

    private void OnEnable()
    {
        if (pinGrabInteractable != null)
        {
            pinGrabInteractable.selectEntered.AddListener(OnPinGrabbed);
            pinGrabInteractable.selectExited.AddListener(OnPinReleased);
        }
    }

    private void OnDisable()
    {
        if (pinGrabInteractable != null)
        {
            pinGrabInteractable.selectEntered.RemoveListener(OnPinGrabbed);
            pinGrabInteractable.selectExited.RemoveListener(OnPinReleased);
        }
    }

    private void OnPinGrabbed(SelectEnterEventArgs args)
    {
        // Blokir grab sebelum misi dimulai
        if (!isMissionStarted)
        {
            Debug.Log("[APARPin] 🔒 Grab pin diblokir — misi belum dimulai!");
            return;
        }

        if (isPinPulled) return; // Pin sudah tercabut, abaikan

        Debug.Log("[APARPin] Pin di-grab!");

        if (pullOnFirstGrab)
        {
            // Langsung cabut pin saat pertama kali di-grab
            PullPin();
        }
    }

    private void OnPinReleased(SelectExitEventArgs args)
    {
        // Kalau misi belum dimulai, tidak ada yang perlu dilakukan
        if (!isMissionStarted) return;

        if (isPinPulled) return; // Sudah tercabut, tidak perlu cek lagi

        // Cek jarak dari posisi awal jika tidak pakai pullOnFirstGrab
        if (!pullOnFirstGrab && initialParent != null)
        {
            Vector3 currentLocalPos = initialParent.InverseTransformPoint(transform.position);
            float distance = Vector3.Distance(currentLocalPos, initialLocalPosition);

            if (distance >= pullDistanceThreshold)
            {
                PullPin();
            }
            else
            {
                // Belum cukup ditarik, kembalikan ke posisi semula
                transform.SetParent(initialParent);
                transform.localPosition = initialLocalPosition;
                transform.localRotation = Quaternion.identity;
                Debug.Log("[APARPin] Pin belum cukup ditarik, dikembalikan ke posisi awal.");
            }
        }
    }

    /// <summary>
    /// Panggil method ini (dari VRSimulationUIManager) saat misi resmi dimulai.
    /// Setelah dipanggil, pin bisa di-grab dan dicabut.
    /// </summary>
    public void SetMissionStarted()
    {
        isMissionStarted = true;
        Debug.Log("[APARPin] ✅ Misi dimulai — grab pin sekarang aktif!");
    }

    private void PullPin()
    {
        if (isPinPulled) return;

        isPinPulled = true;
        Debug.Log("[APARPin] PIN TERCABUT! APAR siap digunakan.");

        // Beritahu AutoFireExtinguisher bahwa pin sudah dicabut
        if (mainExtinguisher != null)
        {
            mainExtinguisher.pinPulled = true;
        }
        else
        {
            AutoFireExtinguisher found = GetComponentInParent<AutoFireExtinguisher>();
            if (found != null)
            {
                found.pinPulled = true;
                mainExtinguisher = found;
            }
        }

        // Beritahu APARPropStateMachine jika ada
        APARPropStateMachine propState = GetComponentInParent<APARPropStateMachine>();
        if (propState != null)
        {
            propState.PullPin();
        }

        // Lepaskan parent (pin terpisah dari APAR)
        transform.SetParent(null);
    }
}

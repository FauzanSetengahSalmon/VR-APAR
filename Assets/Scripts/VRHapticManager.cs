using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Manajer Getaran Controller VR (Haptic Feedback) terpusat untuk simulasi APAR.
/// Mendukung stimulasi haptic pada Meta Quest / XR controller secara aman dan efisien.
/// </summary>
public class VRHapticManager : MonoBehaviour
{
    public static VRHapticManager Instance { get; private set; }

    [Header("Pengaturan Haptic Global")]
    [Tooltip("Aktifkan atau nonaktifkan semua getaran haptic secara global")]
    public bool enableHaptics = true;

    [Header("Intensitas & Durasi Default")]
    [Range(0f, 1f)] public float mcbFlipAmplitude = 0.7f;
    public float mcbFlipDuration = 0.15f;

    [Range(0f, 1f)] public float pinPullAmplitude = 0.85f;
    public float pinPullDuration = 0.20f;

    [Range(0f, 1f)] public float grabAmplitude = 0.25f;
    public float grabDuration = 0.06f;

    private float nextSprayHapticTime = 0f;
    private float nextProximityHapticTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static VRHapticManager GetOrCreate()
    {
        if (Instance != null) return Instance;

        var existing = FindFirstObjectByType<VRHapticManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("VRHapticManager");
        Instance = go.AddComponent<VRHapticManager>();
        return Instance;
    }

    /// <summary>
    /// Memicu getaran haptic pada controller tangan tertentu (Left / Right).
    /// </summary>
    public static void TriggerHaptic(XRNode node, float amplitude, float duration)
    {
        var manager = GetOrCreate();
        if (manager == null || !manager.enableHaptics) return;

        var device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid)
        {
            device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), duration);
        }
    }

    /// <summary>
    /// Memicu getaran haptic pada kedua controller tangan (kiri & kanan).
    /// </summary>
    public static void TriggerHapticBothHands(float amplitude, float duration)
    {
        TriggerHaptic(XRNode.LeftHand, amplitude, duration);
        TriggerHaptic(XRNode.RightHand, amplitude, duration);
    }

    /// <summary>
    /// Memicu getaran dari interactor objek XR.
    /// </summary>
    public static void TriggerHapticFromInteractor(IXRInteractor interactor, float amplitude, float duration)
    {
        if (interactor == null)
        {
            TriggerHapticBothHands(amplitude, duration);
            return;
        }

        string n = interactor.transform.name.ToLower();
        if (n.Contains("left") || n.Contains("kiri"))
        {
            TriggerHaptic(XRNode.LeftHand, amplitude, duration);
        }
        else if (n.Contains("right") || n.Contains("kanan"))
        {
            TriggerHaptic(XRNode.RightHand, amplitude, duration);
        }
        else
        {
            TriggerHapticBothHands(amplitude, duration);
        }
    }

    /// <summary>
    /// Getaran saat saklar MCB dimatikan.
    /// </summary>
    public static void PlayMCBHaptic()
    {
        var m = GetOrCreate();
        TriggerHapticBothHands(m.mcbFlipAmplitude, m.mcbFlipDuration);
    }

    /// <summary>
    /// Getaran sentakan saat Pin Pengaman APAR dicabut.
    /// </summary>
    public static void PlayPinPullHaptic()
    {
        var m = GetOrCreate();
        TriggerHapticBothHands(m.pinPullAmplitude, m.pinPullDuration);
    }

    /// <summary>
    /// Getaran saat melakukan grab pada objek.
    /// </summary>
    public static void PlayGrabHaptic(IXRInteractor interactor = null)
    {
        var m = GetOrCreate();
        TriggerHapticFromInteractor(interactor, m.grabAmplitude, m.grabDuration);
    }

    /// <summary>
    /// Getaran haptic saat APAR menyemprot (mengikuti setting di VRSimulationUIManager jika tersedia).
    /// </summary>
    public static void PlayExtinguisherSprayHaptic(XRNode targetNode = XRNode.RightHand)
    {
        var m = GetOrCreate();
        if (m == null || !m.enableHaptics) return;

        float amp = 0.3f;
        float dur = 0.08f;
        float interval = 0.08f;
        bool enabled = true;

        if (VRSimulationUIManager.Instance != null)
        {
            enabled = VRSimulationUIManager.Instance.enableExtinguisherHaptics;
            amp = VRSimulationUIManager.Instance.extinguisherHapticAmplitude;
            dur = VRSimulationUIManager.Instance.extinguisherHapticDuration;
            interval = VRSimulationUIManager.Instance.extinguisherHapticInterval;
        }

        if (!enabled) return;

        if (Time.time >= m.nextSprayHapticTime)
        {
            m.nextSprayHapticTime = Time.time + interval;
            TriggerHaptic(targetNode, amp, dur);
        }
    }

    /// <summary>
    /// Getaran saat terlalu dekat dengan api (Bahaya / Kritis).
    /// </summary>
    public static void PlayFireProximityHaptic(bool isCritical, XRNode targetNode = XRNode.RightHand)
    {
        var m = GetOrCreate();
        if (m == null || !m.enableHaptics) return;

        float amp = isCritical ? 0.5f : 0.2f;
        float dur = 0.15f;
        float interval = 0.3f;
        bool enabled = true;

        if (VRSimulationUIManager.Instance != null)
        {
            enabled = VRSimulationUIManager.Instance.enableFireProximityHaptics;
            amp = isCritical ? VRSimulationUIManager.Instance.fireCriticalHapticAmplitude : VRSimulationUIManager.Instance.fireHapticAmplitude;
            interval = VRSimulationUIManager.Instance.fireHapticInterval;
        }

        if (!enabled) return;

        if (Time.time >= m.nextProximityHapticTime)
        {
            m.nextProximityHapticTime = Time.time + interval;
            TriggerHaptic(targetNode, amp, dur);
        }
    }
}

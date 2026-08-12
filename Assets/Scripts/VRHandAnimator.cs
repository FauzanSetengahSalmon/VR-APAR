using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Tipe alias untuk namespace XR
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

/// <summary>
/// Script Animasi & Visual Tangan VR (VR Hand Animator Controller & Skin Tone Visualizer).
/// 
/// FUNGSI & FITUR BARU:
///   1. Membaca input analog Grip & Trigger dari Controller Meta Quest 3.
///   2. Menggerakkan jari-jari model tangan 3D (Grip/Flex/Trigger).
///   3. Menerapkan Warna Kulit Manusia Realistis (Human Skin Tone) ke Mesh Tangan VR!
///   4. Pose mengepal/genggam paksa (Full Grip 1.0) otomatis saat memegang APAR & Selang.
/// </summary>
public class VRHandAnimator : MonoBehaviour
{
    public enum HandType
    {
        RightHand,
        LeftHand
    }

    [Header("Pengaturan Tangan VR")]
    public HandType handType = HandType.RightHand;

    [Tooltip("Animator pada model 3D tangan VR (akan dicari otomatis jika kosong)")]
    public Animator handAnimator;

    [Header("Pengaturan Tampilan Warna Kulit Tangan (Skin Tone Visualizer)")]
    [Tooltip("Aktifkan untuk memberikan warna kulit tangan manusia pada model 3D tangan VR")]
    public bool customSkinTone = true;

    [Tooltip("Warna kulit tangan manusia (Human Skin Color Tone)")]
    public Color skinColor = new Color(0.86f, 0.67f, 0.54f); // Natural Light Tan Skin

    [Header("Nama Parameter Animator (Sesuai Controller Animator)")]
    public string gripParamName = "Grip";
    public string triggerParamName = "Trigger";
    public string flexParamName = "Flex";

    [Header("Status Paksa Genggam (Grip Force Override)")]
    [Tooltip("Jika true, tangan akan dipaksa dalam pose mengepal/genggam penuh (1.0)")]
    public bool forceGripPose = false;

    // ── Internal Cache ──────────────────────────────────────────────────────
    private XRInputDevice targetController;
    private bool controllerFound = false;

    private int gripHash;
    private int triggerHash;
    private int flexHash;

    private float currentGripValue = 0f;
    private float currentTriggerValue = 0f;

    private void Awake()
    {
        if (handAnimator == null)
            handAnimator = GetComponent<Animator>();
        if (handAnimator == null)
            handAnimator = GetComponentInChildren<Animator>();

        gripHash = Animator.StringToHash(gripParamName);
        triggerHash = Animator.StringToHash(triggerParamName);
        flexHash = Animator.StringToHash(flexParamName);

        ApplySkinTone();
    }

    private void OnEnable()
    {
        XRInputDevices.deviceConnected += OnXRDeviceConnected;
        XRInputDevices.deviceDisconnected += OnXRDeviceDisconnected;
        RefreshDevice();
    }

    private void OnDisable()
    {
        XRInputDevices.deviceConnected -= OnXRDeviceConnected;
        XRInputDevices.deviceDisconnected -= OnXRDeviceDisconnected;
    }

    private void Update()
    {
        float targetGrip = 0f;
        float targetTrigger = 0f;

        if (forceGripPose)
        {
            targetGrip = 1.0f;
            targetTrigger = 0.8f;
        }
        else if (controllerFound)
        {
            if (targetController.TryGetFeatureValue(XRCommonUsages.grip, out float gripVal))
                targetGrip = gripVal;

            if (targetController.TryGetFeatureValue(XRCommonUsages.trigger, out float trgVal))
                targetTrigger = trgVal;
        }

        // Haluskan transisi animasi genggam jari (Smooth Lerp)
        currentGripValue = Mathf.Lerp(currentGripValue, targetGrip, Time.deltaTime * 15f);
        currentTriggerValue = Mathf.Lerp(currentTriggerValue, targetTrigger, Time.deltaTime * 15f);

        // Update Animator Parameters
        if (handAnimator != null && handAnimator.enabled)
        {
            if (HasParameter(gripParamName)) handAnimator.SetFloat(gripHash, currentGripValue);
            if (HasParameter(triggerParamName)) handAnimator.SetFloat(triggerHash, currentTriggerValue);
            if (HasParameter(flexParamName)) handAnimator.SetFloat(flexHash, currentGripValue);
        }
    }

    /// <summary>
    /// Menerapkan warna kulit manusia (Skin Tone) ke semua Mesh Hand Renderer
    /// </summary>
    public void ApplySkinTone()
    {
        if (!customSkinTone) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            // Jangan ubah material jika itu UI atau Particle System
            if (rend is ParticleSystemRenderer || rend is CanvasRenderer) continue;

            foreach (Material mat in rend.materials)
            {
                if (mat != null)
                {
                    if (mat.HasProperty("_Color")) mat.color = skinColor;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", skinColor);
                }
            }
        }
    }

    /// <summary>
    /// Set paksa status pose genggam (dipanggil saat APAR / Selang di-grab)
    /// </summary>
    public void SetForceGrip(bool isGrabbing)
    {
        forceGripPose = isGrabbing;
    }

    private bool HasParameter(string paramName)
    {
        if (handAnimator == null) return false;
        foreach (AnimatorControllerParameter param in handAnimator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    private void RefreshDevice()
    {
        var devices = new List<XRInputDevice>();
        InputDeviceCharacteristics characteristics = InputDeviceCharacteristics.Controller;

        if (handType == HandType.RightHand)
            characteristics |= InputDeviceCharacteristics.Right;
        else
            characteristics |= InputDeviceCharacteristics.Left;

        XRInputDevices.GetDevicesWithCharacteristics(characteristics, devices);

        if (devices.Count > 0)
        {
            targetController = devices[0];
            controllerFound = true;
        }
        else
        {
            controllerFound = false;
        }
    }

    private void OnXRDeviceConnected(XRInputDevice device)
    {
        RefreshDevice();
    }

    private void OnXRDeviceDisconnected(XRInputDevice device)
    {
        if (device == targetController)
        {
            controllerFound = false;
        }
    }
}

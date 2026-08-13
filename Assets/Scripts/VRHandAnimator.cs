using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

// Tipe alias untuk namespace XR
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

/// <summary>
/// Script Animasi & Visual Tangan VR (VR Hand Animator Controller, Procedural Finger Flexing & Skin Tone Visualizer).
/// 
/// FUNGSI & FITUR BARU:
///   1. Membaca input analog Grip & Trigger dari Controller Meta Quest 3 / XR Simulator / Mouse.
///   2. Menggerakkan jari-jari model tangan 3D secara presisi melalui Animator ATAU Procedural Bone Curling!
///   3. Mencegah Tangan Hilang (menjamin SkinnedMeshRenderer tetap aktif di Mode Controller & Simulator).
///   4. Menerapkan Warna Kulit Manusia Realistis (Human Skin Tone) ke Mesh Tangan VR tanpa merusak Depth Mask.
///   5. Pose mengepal/genggam paksa (Full Grip 1.0) otomatis saat memegang APAR & Selang.
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

    private SkinnedMeshRenderer[] handMeshRenderers;

    // ── Procedural Bone Curling Cache ───────────────────────────────────────
    private class BoneData
    {
        public Transform transform;
        public Quaternion defaultLocalRotation;
        public Vector3 curlAxis = new Vector3(1f, 0f, 0f); // Sumbu rotasi menekuk jari
        public float maxBendAngle = 65f;
    }

    private List<BoneData> indexBones = new List<BoneData>();
    private List<BoneData> middleBones = new List<BoneData>();
    private List<BoneData> ringBones = new List<BoneData>();
    private List<BoneData> littleBones = new List<BoneData>();
    private List<BoneData> thumbBones = new List<BoneData>();

    private bool hasBoneCache = false;

    private void Awake()
    {
        if (handAnimator == null)
            handAnimator = GetComponent<Animator>();
        if (handAnimator == null)
            handAnimator = GetComponentInChildren<Animator>();

        gripHash = Animator.StringToHash(gripParamName);
        triggerHash = Animator.StringToHash(triggerParamName);
        flexHash = Animator.StringToHash(flexParamName);

        handMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        CacheFingerBones();
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

    private void CacheFingerBones()
    {
        indexBones.Clear();
        middleBones.Clear();
        ringBones.Clear();
        littleBones.Clear();
        thumbBones.Clear();

        string prefix = (handType == HandType.LeftHand) ? "L_" : "R_";

        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            string name = t.name;
            if (!name.StartsWith(prefix)) continue;

            if (name.Contains("IndexProximal") || name.Contains("IndexIntermediate") || name.Contains("IndexDistal"))
                indexBones.Add(new BoneData { transform = t, defaultLocalRotation = t.localRotation, maxBendAngle = 70f });
            else if (name.Contains("MiddleProximal") || name.Contains("MiddleIntermediate") || name.Contains("MiddleDistal"))
                middleBones.Add(new BoneData { transform = t, defaultLocalRotation = t.localRotation, maxBendAngle = 75f });
            else if (name.Contains("RingProximal") || name.Contains("RingIntermediate") || name.Contains("RingDistal"))
                ringBones.Add(new BoneData { transform = t, defaultLocalRotation = t.localRotation, maxBendAngle = 75f });
            else if (name.Contains("LittleProximal") || name.Contains("LittleIntermediate") || name.Contains("LittleDistal"))
                littleBones.Add(new BoneData { transform = t, defaultLocalRotation = t.localRotation, maxBendAngle = 80f });
            else if (name.Contains("ThumbProximal") || name.Contains("ThumbDistal"))
                thumbBones.Add(new BoneData { transform = t, defaultLocalRotation = t.localRotation, maxBendAngle = 45f });
        }

        hasBoneCache = (indexBones.Count > 0 || middleBones.Count > 0);
    }

    private void Update()
    {
        // ── 1. GUARANTEE MESH VISIBILITY ────────────────────────────────────
        // Paksa SkinnedMeshRenderer agar SELALU aktif (mencegah disembunyikan oleh XRHandMeshController saat untracked)
        if (handMeshRenderers != null)
        {
            foreach (var smr in handMeshRenderers)
            {
                if (smr != null && !smr.enabled)
                {
                    smr.enabled = true;
                }
            }
        }

        // ── 2. READ INPUT VALUES ────────────────────────────────────────────
        float targetGrip = 0f;
        float targetTrigger = 0f;

        if (forceGripPose)
        {
            targetGrip = 1.0f;
            targetTrigger = 0.85f;
        }
        else
        {
            if (controllerFound)
            {
                if (targetController.TryGetFeatureValue(XRCommonUsages.grip, out float gripVal))
                    targetGrip = gripVal;

                if (targetController.TryGetFeatureValue(XRCommonUsages.trigger, out float trgVal))
                    targetTrigger = trgVal;
            }

            // Fallback Editor Testing via Mouse
            if (Mouse.current != null)
            {
                if (Mouse.current.rightButton.isPressed) targetGrip = 1.0f;
                if (Mouse.current.leftButton.isPressed) targetTrigger = 1.0f;
            }
        }

        // Haluskan transisi animasi genggam jari (Smooth Lerp)
        currentGripValue = Mathf.Lerp(currentGripValue, targetGrip, Time.deltaTime * 18f);
        currentTriggerValue = Mathf.Lerp(currentTriggerValue, targetTrigger, Time.deltaTime * 18f);

        // ── 3. UPDATE ANIMATOR OR PROCEDURAL BONES ──────────────────────────
        bool animatorUpdated = false;
        if (handAnimator != null && handAnimator.enabled && handAnimator.runtimeAnimatorController != null)
        {
            if (HasParameter(gripParamName)) { handAnimator.SetFloat(gripHash, currentGripValue); animatorUpdated = true; }
            if (HasParameter(triggerParamName)) { handAnimator.SetFloat(triggerHash, currentTriggerValue); animatorUpdated = true; }
            if (HasParameter(flexParamName)) { handAnimator.SetFloat(flexHash, currentGripValue); animatorUpdated = true; }
        }

        // Jika Animator tidak ada atau tidak memiliki parameter, jalankan Procedural Bone Curling
        if (!animatorUpdated && hasBoneCache)
        {
            AnimateProceduralBones();
        }
    }

    /// <summary>
    /// Animasi jari-jari tangan secara prosedural berdasarkan rotasi sendi tulang (Bones).
    /// </summary>
    private void AnimateProceduralBones()
    {
        // Jari Telunjuk dikontrol oleh Trigger (atau Grip jika forceGripPose)
        float indexCurl = forceGripPose ? Mathf.Max(currentGripValue, currentTriggerValue) : currentTriggerValue;
        ApplyCurlToBoneList(indexBones, indexCurl);

        // Jari Tengah, Manis, Kelingking dikontrol oleh Grip
        ApplyCurlToBoneList(middleBones, currentGripValue);
        ApplyCurlToBoneList(ringBones, currentGripValue);
        ApplyCurlToBoneList(littleBones, currentGripValue);

        // Ibu Jari tekuk sedikit saat mengepal
        float thumbCurl = Mathf.Max(currentGripValue * 0.7f, currentTriggerValue * 0.4f);
        ApplyCurlToBoneList(thumbBones, thumbCurl);
    }

    private void ApplyCurlToBoneList(List<BoneData> bones, float curlAmount)
    {
        foreach (var b in bones)
        {
            if (b.transform == null) continue;
            Quaternion targetRot = b.defaultLocalRotation * Quaternion.AngleAxis(curlAmount * b.maxBendAngle, b.curlAxis);
            b.transform.localRotation = Quaternion.Slerp(b.transform.localRotation, targetRot, Time.deltaTime * 20f);
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
            if (rend is ParticleSystemRenderer) continue;

            foreach (Material mat in rend.materials)
            {
                if (mat != null)
                {
                    // Abaikan material DepthOnly agar tidak merusak shader mask
                    if (mat.shader != null && mat.shader.name.Contains("DepthOnly")) continue;

                    if (mat.HasProperty("_Color")) mat.color = skinColor;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", skinColor);
                    if (mat.HasProperty("_HandColor")) mat.SetColor("_HandColor", skinColor);
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
        if (handAnimator == null || handAnimator.runtimeAnimatorController == null) return false;
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


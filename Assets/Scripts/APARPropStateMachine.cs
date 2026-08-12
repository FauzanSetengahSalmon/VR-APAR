using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// Tipe alias untuk menghindari kerancuan namespace antara XR dan Input System
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRDeviceNode = UnityEngine.XR.XRNode;

/// <summary>
/// State Machine Interaksi APAR VR untuk Custom Physical Prop Controller.
/// 
/// ALUR INTERAKSI:
///   1. State 1 [SafetyPinLocked] : Pin pengaman fisik terpasang di prop. Software terkunci.
///   2. State 2 [PinPulled_Ready] : Pin ditarik/dicabut. Kunci terbuka, spray BELUM aktif.
///   3. State 3 [SprayActive]     : Gagang fisik APAR ditekan SETELAH pin dicabut. Spray MENYEMPROT!
/// </summary>
public class APARPropStateMachine : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════
    //  STATE MACHINE ENUM
    // ═══════════════════════════════════════════════════════════════════════

    public enum APARState
    {
        SafetyPinLocked,   // 1. Pin terpasang, input terkunci
        PinPulled_Ready,   // 2. Pin dicabut, software unlocked, belum spray
        SprayActive        // 3. Gagang ditekan setelah pin dicabut, spray NYALA
    }

    public enum ControllerHand
    {
        RightHand,
        LeftHand
    }

    public enum PhysicalInputSource
    {
        Trigger,     // RT / Right Trigger (Umum untuk gagang APAR)
        GripButton,  // Side Grip Button
        CustomKey    // Debug Keyboard
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  INSPECTOR FIELDS
    // ═══════════════════════════════════════════════════════════════════════

    [Header("State Machine Status (Read-Only di Runtime)")]
    [Tooltip("Status interaksi APAR saat ini")]
    [SerializeField] private APARState currentState = APARState.SafetyPinLocked;

    [Header("Pengaturan Input Controller")]
    [Tooltip("Tangan controller tempat prop APAR terpasang (Default: RightHand)")]
    public ControllerHand controllerHand = ControllerHand.RightHand;

    [Tooltip("Tombol fisik controller yang ditekan oleh gagang APAR (Default: Trigger / RT)")]
    public PhysicalInputSource leverInputButton = PhysicalInputSource.Trigger;

    [Tooltip("Ambang batas nilai analog trigger (0.1 - 1.0) untuk dianggap 'ditekan'")]
    [Range(0.1f, 0.9f)] public float leverPressThreshold = 0.5f;

    [Header("Mekanisme Pin Pengaman Physical Prop")]
    [Tooltip("Set true HANYA jika menggunakan Prop Fisik di mana Pin Pengaman menahan tombol controller (held) secara mekanis.")]
    public bool usePhysicalPinButtonLock = false;

    [Tooltip("Tombol fisik controller yang ditahan oleh Pin (jika usePhysicalPinButtonLock = true)")]
    public PhysicalInputSource pinHoldButton = PhysicalInputSource.GripButton;

    [Header("Efek Visual & Audio (Optional Direct Binding)")]
    [Tooltip("Particle system asap APAR")]
    public ParticleSystem sprayParticleEffect;
    [Tooltip("Suara semprotan APAR")]
    public AudioSource sprayAudioSource;
    [Tooltip("Script pemadaman api (AutoFireExtinguisher)")]
    public AutoFireExtinguisher mainExtinguisher;

    [Header("Events (Untuk Integrasi UI / Sound / Visual)")]
    public UnityEvent<APARState> OnStateChanged;
    public UnityEvent OnPinPulled;
    public UnityEvent OnSprayStarted;
    public UnityEvent OnSprayStopped;

    [Header("Testing & Debug")]
    [Tooltip("Tekan tombol ini di keyboard (Editor) untuk simulasi cabut pin")]
    public Key debugPullPinKey = Key.P;
    [Tooltip("Tekan tombol ini di keyboard (Editor) untuk simulasi tekan gagang")]
    public Key debugPressLeverKey = Key.Space;

    // ═══════════════════════════════════════════════════════════════════════
    //  PRIVATE PROPERTIES & STATE
    // ═══════════════════════════════════════════════════════════════════════

    public APARState CurrentState => currentState;
    public bool IsPinPulled => currentState != APARState.SafetyPinLocked;
    public bool IsSpraying => currentState == APARState.SprayActive;

    private XRInputDevice targetController;
    private bool controllerFound = false;

    // Tracker state tombol pin fisik (untuk mendeteksi transisi lepas)
    private bool previousPinButtonState = false;

    // ═══════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (mainExtinguisher == null)
            mainExtinguisher = GetComponent<AutoFireExtinguisher>();

        if (sprayParticleEffect == null && mainExtinguisher != null)
            sprayParticleEffect = mainExtinguisher.sprayEffect;

        // Reset state awal: SELALU mulai dari SafetyPinLocked
        SetState(APARState.SafetyPinLocked, true);
    }

    private void OnEnable()
    {
        XRInputDevices.deviceConnected += OnXRDeviceConnected;
        XRInputDevices.deviceDisconnected += OnXRDeviceDisconnected;
        RefreshControllerDevice();
    }

    private void OnDisable()
    {
        XRInputDevices.deviceConnected -= OnXRDeviceConnected;
        XRInputDevices.deviceDisconnected -= OnXRDeviceDisconnected;

        StopSprayEffect();
    }

    private void Update()
    {
        // 1. Cek input pencabutan pin (jika masih terkunci)
        CheckPinStateInput();

        // 2. Cek input penekanan gagang fisik APAR
        CheckLeverInput();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  LOGIC CHECKING & STATE TRANSITIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Memeriksa apakah pin pengaman sudah dicabut (dari physical prop / VR pin / Keyboard)
    /// </summary>
    private void CheckPinStateInput()
    {
        if (currentState != APARState.SafetyPinLocked) return;

        bool pinReleasedTriggered = false;

        // A. Cek Debug Keyboard (Editor Test)
        if (Keyboard.current != null && Keyboard.current[debugPullPinKey].wasPressedThisFrame)
        {
            Debug.Log("[APARProp] 🔑 Debug Keyboard: Pin dicabut!");
            pinReleasedTriggered = true;
        }

        // B. Cek Physical Prop Pin Button Lock (Tombol controller yang ditahan oleh pin fisik)
        if (usePhysicalPinButtonLock && controllerFound)
        {
            bool currentPinButtonState = ReadButtonState(pinHoldButton);

            // Jika pin fisik terpasang → tombol HELD (true).
            // Saat pin ditarik → tombol TERLEPAS (transition from true to false).
            if (previousPinButtonState && !currentPinButtonState)
            {
                Debug.Log("[APARProp] 🔑 Physical Pin ditarik dari prop! Lock released.");
                pinReleasedTriggered = true;
            }

            previousPinButtonState = currentPinButtonState;
        }

        // Jika pin dicabut → Pindah ke State 2 (PinPulled_Ready)
        if (pinReleasedTriggered)
        {
            PullPin();
        }
    }

    /// <summary>
    /// Memeriksa input gagang fisik APAR (Lever)
    /// </summary>
    private void CheckLeverInput()
    {
        // Jika pin masih terpasang, gagang TIDAK BISA menyemprot
        if (currentState == APARState.SafetyPinLocked)
        {
            return;
        }

        // Baca input gagang fisik (Trigger / Grip / Keyboard)
        bool isLeverPressed = false;

        // A. Debug Keyboard
        if (Keyboard.current != null && Keyboard.current[debugPressLeverKey].isPressed)
        {
            isLeverPressed = true;
        }

        // B. Input Physical Controller (Gagang menekan Trigger/Grip)
        if (controllerFound)
        {
            float leverValue = ReadAnalogInputValue(leverInputButton);
            if (leverValue >= leverPressThreshold)
                isLeverPressed = true;
        }

        // ── Transisi State berdasarkan input gagang ──
        if (currentState == APARState.PinPulled_Ready && isLeverPressed)
        {
            // Transisi ke State 3: SPRAY ACTIVE!
            SetState(APARState.SprayActive);
        }
        else if (currentState == APARState.SprayActive && !isLeverPressed)
        {
            // Kembali ke State 2: PIN PULLED READY (Gagang dilepas, spray berhenti)
            SetState(APARState.PinPulled_Ready);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PUBLIC API FOR EXTERNAL CALLS (VR Grab, Virtual Pin, etc.)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Panggil fungsi ini saat pin ditarik (bisa dipanggil oleh APARPin.cs atau Event)
    /// </summary>
    public void PullPin()
    {
        if (currentState == APARState.SafetyPinLocked)
        {
            SetState(APARState.PinPulled_Ready);
            OnPinPulled?.Invoke();
        }
    }

    /// <summary>
    /// Pasang kembali pin pengaman (Reset ke State 1)
    /// </summary>
    public void LockPin()
    {
        SetState(APARState.SafetyPinLocked);
    }

    /// <summary>
    /// Mengubah state interaksi APAR dan memicu event/efek yang sesuai
    /// </summary>
    private void SetState(APARState newState, bool force = false)
    {
        if (currentState == newState && !force) return;

        APARState oldState = currentState;
        currentState = newState;

        Debug.Log($"[APARProp] 🔄 State Change: {oldState} ➔ {newState}");

        // Sync flag ke AutoFireExtinguisher jika terhubung
        if (mainExtinguisher != null)
        {
            mainExtinguisher.pinPulled = IsPinPulled;
        }

        // Pemicu Efek berdasarkan State Baru
        switch (newState)
        {
            case APARState.SafetyPinLocked:
                StopSprayEffect();
                break;

            case APARState.PinPulled_Ready:
                StopSprayEffect();
                break;

            case APARState.SprayActive:
                StartSprayEffect();
                break;
        }

        OnStateChanged?.Invoke(newState);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  EF EK SPRAY & AUDIO
    // ═══════════════════════════════════════════════════════════════════════

    private void StartSprayEffect()
    {
        if (sprayParticleEffect != null)
        {
            if (!sprayParticleEffect.gameObject.activeSelf)
                sprayParticleEffect.gameObject.SetActive(true);
            if (!sprayParticleEffect.isPlaying)
                sprayParticleEffect.Play(true);
        }

        if (sprayAudioSource != null && !sprayAudioSource.isPlaying)
        {
            sprayAudioSource.Play();
        }

        if (mainExtinguisher != null)
        {
            mainExtinguisher.StartSpray();
        }

        OnSprayStarted?.Invoke();
        Debug.Log("[APARProp] 💨 SPRAY AKTIF! Gagang ditekan.");
    }

    private void StopSprayEffect()
    {
        if (sprayParticleEffect != null && sprayParticleEffect.isPlaying)
        {
            sprayParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (sprayAudioSource != null && sprayAudioSource.isPlaying)
        {
            sprayAudioSource.Stop();
        }

        if (mainExtinguisher != null)
        {
            mainExtinguisher.StopSpray();
        }

        OnSprayStopped?.Invoke();
        Debug.Log("[APARProp] 🚫 SPRAY MATI. Gagang dilepas / pin locked.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  HELPER BACA INPUT XR DEVICE
    // ═══════════════════════════════════════════════════════════════════════

    private float ReadAnalogInputValue(PhysicalInputSource inputSource)
    {
        if (!controllerFound) return 0f;

        switch (inputSource)
        {
            case PhysicalInputSource.Trigger:
                if (targetController.TryGetFeatureValue(XRCommonUsages.trigger, out float triggerVal))
                    return triggerVal;
                break;

            case PhysicalInputSource.GripButton:
                if (targetController.TryGetFeatureValue(XRCommonUsages.grip, out float gripVal))
                    return gripVal;
                break;
        }

        return 0f;
    }

    private bool ReadButtonState(PhysicalInputSource inputSource)
    {
        if (!controllerFound) return false;

        switch (inputSource)
        {
            case PhysicalInputSource.Trigger:
                if (targetController.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool trgBtn))
                    return trgBtn;
                return ReadAnalogInputValue(PhysicalInputSource.Trigger) >= leverPressThreshold;

            case PhysicalInputSource.GripButton:
                if (targetController.TryGetFeatureValue(XRCommonUsages.gripButton, out bool grpBtn))
                    return grpBtn;
                return ReadAnalogInputValue(PhysicalInputSource.GripButton) >= leverPressThreshold;
        }

        return false;
    }

    private void RefreshControllerDevice()
    {
        var devices = new List<XRInputDevice>();
        InputDeviceCharacteristics characteristics = InputDeviceCharacteristics.Controller;

        if (controllerHand == ControllerHand.RightHand)
            characteristics |= InputDeviceCharacteristics.Right;
        else
            characteristics |= InputDeviceCharacteristics.Left;

        XRInputDevices.GetDevicesWithCharacteristics(characteristics, devices);

        if (devices.Count > 0)
        {
            targetController = devices[0];
            controllerFound = true;
            Debug.Log($"[APARProp] ✔ Prop Controller terhubung ({controllerHand}): {targetController.name}");

            // Simpan state awal tombol pin untuk tracking lepas
            if (usePhysicalPinButtonLock)
                previousPinButtonState = ReadButtonState(pinHoldButton);
        }
        else
        {
            controllerFound = false;
        }
    }

    private void OnXRDeviceConnected(XRInputDevice device)
    {
        RefreshControllerDevice();
    }

    private void OnXRDeviceDisconnected(XRInputDevice device)
    {
        if (device == targetController)
        {
            controllerFound = false;
            Debug.Log("[APARProp] Prop Controller terputus.");
        }
    }
}

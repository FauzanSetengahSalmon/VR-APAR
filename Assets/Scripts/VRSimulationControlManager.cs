using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Mengelola konfigurasi input controller VR sesuai standar pelatihan simulasi:
/// 1. Menonaktifkan joystick gerak maju-mundur/strafe (Hanya menggunakan room-scale physical walking).
/// 2. Menonaktifkan tombol A, B, X, Y (mencegah salah pencet lompat/menu/teleport).
/// 3. Mempertahankan Grip (Pegang APAR/Selang/Pin) dan Trigger (Semprot APAR/Klik UI).
/// </summary>
[DefaultExecutionOrder(-50)]
public class VRSimulationControlManager : MonoBehaviour
{
    public static VRSimulationControlManager Instance { get; private set; }

    [Header("Pengaturan Joystick")]
    [Tooltip("Nonaktifkan joystick gerak maju/mundur/kiri/kanan (Gunakan room-scale physical walking).")]
    public bool disableStickMovement = true;

    [Tooltip("Nonaktifkan joystick rotasi putar (Snap/Smooth turn).")]
    public bool disableStickTurning = true;

    [Header("Pengaturan Tombol Controller")]
    [Tooltip("Nonaktifkan tombol tombol sekunder A, B, X, Y.")]
    public bool disableABXYButtons = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        ApplyControlRestrictions();
    }

    private void Start()
    {
        ApplyControlRestrictions();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (FindFirstObjectByType<VRSimulationControlManager>() == null)
        {
            GameObject go = new GameObject("VR_Simulation_Control_Manager");
            go.AddComponent<VRSimulationControlManager>();
        }
    }

    /// <summary>
    /// Terapkan pembatasan kontrol ke seluruh locomotion & input provider di scene.
    /// </summary>
    public void ApplyControlRestrictions()
    {
        // 1. Matikan Continuous Move Provider (Joystick Maju/Mundur)
        if (disableStickMovement)
        {
            var moveProviders = FindObjectsByType<ContinuousMoveProvider>(FindObjectsSortMode.None);
            foreach (var mp in moveProviders)
            {
                if (mp != null)
                {
                    mp.moveSpeed = 0f;
                    mp.enabled = false;
                }
            }

            // Cari provider movement turunan MonoBehaviour jika ada
            var allLocomotion = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in allLocomotion)
            {
                if (mb == null) continue;
                string typeName = mb.GetType().Name;
                if (typeName.Contains("ContinuousMove") || typeName.Contains("DynamicMove") || typeName.Contains("MoveProvider"))
                {
                    mb.enabled = false;
                }
            }
        }

        // 2. Matikan Turn Provider jika diinginkan
        if (disableStickTurning)
        {
            var turnProviders = FindObjectsByType<ContinuousTurnProvider>(FindObjectsSortMode.None);
            foreach (var tp in turnProviders)
            {
                if (tp != null) tp.enabled = false;
            }

            var snapTurnProviders = FindObjectsByType<SnapTurnProvider>(FindObjectsSortMode.None);
            foreach (var stp in snapTurnProviders)
            {
                if (stp != null) stp.enabled = false;
            }
        }

        // 3. Matikan Teleportation Provider
        var teleportProviders = FindObjectsByType<TeleportationProvider>(FindObjectsSortMode.None);
        foreach (var tp in teleportProviders)
        {
            if (tp != null) tp.enabled = false;
        }

        Debug.Log($"[VRControlManager] 🎮 Kontrol VR Diperbarui: Stick Maju-Mundur={(disableStickMovement ? "OFF (RoomScale)" : "ON")}, Stick Putar={(disableStickTurning ? "OFF" : "ON")}, Tombol A/B/X/Y={(disableABXYButtons ? "OFF" : "ON")}.");
    }
}

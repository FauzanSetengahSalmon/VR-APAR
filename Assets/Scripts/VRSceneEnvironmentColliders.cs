using UnityEngine;

/// <summary>
/// Memastikan semua objek lingkungan kantor (tembok, lantai, sekat, perabot) memiliki MeshCollider solid.
/// Berjalan otomatis di awal permainan agar Player dan UI tidak pernah menembus tembok.
/// </summary>
[DefaultExecutionOrder(-100)]
public class VRSceneEnvironmentColliders : MonoBehaviour
{
    public static VRSceneEnvironmentColliders Instance { get; private set; }

    [Header("Pengaturan Auto Collider")]
    [Tooltip("Otomatis buat MeshCollider pada semua mesh statis di scene yang belum memiliki collider.")]
    public bool autoAddEnvironmentColliders = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (autoAddEnvironmentColliders)
        {
            GenerateEnvironmentColliders();
        }
    }

    /// <summary>
    /// Pindai semua MeshFilter di scene dan tambahkan MeshCollider jika belum ada.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void GenerateEnvironmentColliders()
    {
        MeshFilter[] allMeshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        int addedCount = 0;
        int existingCount = 0;

        foreach (MeshFilter mf in allMeshFilters)
        {
            if (mf == null || mf.sharedMesh == null) continue;

            GameObject go = mf.gameObject;

            // Abaikan objek dinamis / khusus:
            // 1. Part of Player / XR Rig
            if (go.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null) continue;
            if (go.name.Contains("XR Origin") || go.name.Contains("Controller") || go.name.Contains("Hand")) continue;

            // 2. APAR dinamis / selang / pin (sudah punya setup fisika sendiri)
            if (go.GetComponentInParent<AutoFireExtinguisher>() != null) continue;
            if (go.GetComponentInParent<APARHoseGrabber>() != null) continue;
            if (go.GetComponentInParent<APARPin>() != null) continue;

            // 3. Efek api / asap / decal / UI
            if (go.name.Contains("Decal") || go.name.Contains("Burn") || go.name.Contains("Smoke") || go.name.Contains("Fire")) continue;
            if (go.name.Contains("Canvas") || go.name.Contains("UI ") || go.name.Contains("Ui ")) continue;

            // Cek apakah sudah punya collider jenis apapun
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                existingCount++;
                continue;
            }

            // Tambahkan static MeshCollider
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false; // Static level geometry mesh collider (tembok, lantai, ruangan)
            addedCount++;
        }

        Debug.Log($"[VRSceneEnvironmentColliders] 🛡️ Collider Lingkungan Aktif: {addedCount} MeshCollider baru ditambahkan, {existingCount} collider sudah ada.");
    }
}

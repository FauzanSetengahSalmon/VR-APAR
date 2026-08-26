using UnityEngine;

/// <summary>
/// Mencegah semua UI world-space (SpriteRenderer & Canvas) di scene dari
/// nembus/clipping melalui tembok. Attach ke VR_Simulation_UI_Manager atau
/// GameObject manapun yang aktif sepanjang game.
///
/// Cara kerja:
/// Setiap LateUpdate, raycast dilakukan dari Main Camera ke posisi setiap UI.
/// Jika ada collider (tembok) di antara keduanya, UI digeser ke titik sebelum
/// tembok + safeMargin agar tidak tembus.
/// </summary>
public class VRUIAntiWallClip : MonoBehaviour
{
    [Header("Anti Wall Clip Settings")]
    [Tooltip("Jarak aman UI dari permukaan tembok (meter).")]
    public float safeMargin = 0.12f;

    [Tooltip("Layer mask untuk deteksi tembok. Default = semua layer.")]
    public LayerMask wallLayers = ~0;

    [Tooltip("Jarak minimum antara kamera dan UI agar tidak terlalu dekat (meter).")]
    public float minDistanceFromCamera = 0.3f;

    private Transform _camera;

    void Start()
    {
        if (Camera.main != null)
            _camera = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (_camera == null)
        {
            if (Camera.main != null)
                _camera = Camera.main.transform;
            return;
        }

        // --- Cek semua SpriteRenderer yang merupakan UI world-space ---
        var spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (var sr in spriteRenderers)
        {
            if (sr == null || !sr.gameObject.activeInHierarchy) continue;
            if (!IsUIObject(sr.gameObject)) continue;
            // VRBillboardUI sudah handle sendiri -- skip duplikasi
            if (sr.GetComponent<VRBillboardUI>() != null) continue;

            CheckAndPushOutOfWall(sr.transform);
        }

        // --- Cek semua Canvas world-space (dibuat oleh VRSimulationUIManager) ---
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (c.renderMode != RenderMode.WorldSpace) continue;

            CheckAndPushOutOfWall(c.transform);
        }
    }

    /// <summary>
    /// Raycast dari kamera ke UI. Jika ada tembok, geser UI ke depan tembok.
    /// </summary>
    void CheckAndPushOutOfWall(Transform uiTransform)
    {
        Vector3 dir = uiTransform.position - _camera.position;
        float dist = dir.magnitude;

        if (dist < 0.01f) return;

        RaycastHit hit;
        if (Physics.Raycast(_camera.position, dir.normalized, out hit, dist, wallLayers))
        {
            // Ada tembok -- geser UI ke titik tepat sebelum tembok
            float distToHit = hit.distance;

            // Pastikan UI tidak terlalu dekat ke kamera
            float safeDistance = Mathf.Max(distToHit - safeMargin, minDistanceFromCamera);

            Vector3 newPos = _camera.position + dir.normalized * safeDistance;
            // Pertahankan ketinggian Y asli agar tidak melayang aneh
            newPos.y = uiTransform.position.y;

            uiTransform.position = newPos;
        }
    }

    /// <summary>
    /// Tentukan apakah GameObject ini adalah UI (bukan karakter / objek dunia).
    /// Diidentifikasi dari nama dengan prefix "Ui " / "UI " atau komponen VRBillboardUI.
    /// </summary>
    bool IsUIObject(GameObject go)
    {
        string n = go.name;
        return n.StartsWith("Ui ") || n.StartsWith("UI ") || n.StartsWith("ui ")
            || go.GetComponent<VRBillboardUI>() != null;
    }
}

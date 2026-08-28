using UnityEngine;

/// <summary>
/// Mencegah UI melayang (floating UI) dari tertelan/clipping di dalam tembok.
/// UI yang sudah memiliki VRBillboardUI di-skip karena ditangani secara independen dan efisien.
/// </summary>
public class VRUIAntiWallClip : MonoBehaviour
{
    [Header("Anti Wall Clip Settings")]
    [Tooltip("Jarak aman UI dari permukaan tembok (meter).")]
    public float safeMargin = 0.12f;

    [Tooltip("Layer mask untuk deteksi tembok. Default = semua layer solid.")]
    public LayerMask wallLayers = ~0;

    [Tooltip("Jarak minimum antara kamera dan UI agar tidak terlalu dekat (meter).")]
    public float minDistanceFromCamera = 0.35f;

    private Transform _camera;
    private float _nextScanTime = 0f;
    private Canvas[] _cachedCanvases;

    void Start()
    {
        if (Camera.main != null)
            _camera = Camera.main.transform;

        RefreshUIElements();
    }

    void RefreshUIElements()
    {
        _cachedCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
    }

    void LateUpdate()
    {
        if (_camera == null)
        {
            if (Camera.main != null)
                _camera = Camera.main.transform;
            else
                return;
        }

        // Refresh cache berkala setiap 1.5 detik
        if (Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + 1.5f;
            RefreshUIElements();
        }

        if (_cachedCanvases == null) return;

        foreach (var c in _cachedCanvases)
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (c.renderMode != RenderMode.WorldSpace) continue;

            // Jangan sentuh UI billboard (VRBillboardUI menangani diri sendiri)
            if (c.GetComponent<VRBillboardUI>() != null) continue;

            // Jangan sentuh UI MCB yang sudah tertempel rapi di dinding
            if (c.gameObject.name.StartsWith("MCB_")) continue;

            // Jangan sentuh UI tombol meja / canvas anak objek lain yang ter-anchor
            if (c.transform.parent != null && !IsFloatingUI(c.gameObject)) continue;

            CheckAndPushOutOfWall(c.transform);
        }
    }

    /// <summary>
    /// Raycast dari kamera ke UI. Jika ada tembok di antara keduanya, geser UI ke depan tembok.
    /// </summary>
    void CheckAndPushOutOfWall(Transform uiTransform)
    {
        Vector3 dir = uiTransform.position - _camera.position;
        float dist = dir.magnitude;

        if (dist < 0.05f) return;

        if (Physics.Raycast(_camera.position, dir.normalized, out RaycastHit hit, dist, wallLayers, QueryTriggerInteraction.Ignore))
        {
            // Jangan geser jika hit adalah bagian dari UI itu sendiri
            if (hit.transform.IsChildOf(uiTransform) || uiTransform.IsChildOf(hit.transform)) return;

            float distToHit = hit.distance;
            float safeDistance = Mathf.Max(distToHit - safeMargin, minDistanceFromCamera);

            Vector3 newPos = _camera.position + dir.normalized * safeDistance;
            uiTransform.position = newPos;
        }
    }

    bool IsFloatingUI(GameObject go)
    {
        string n = go.name.ToLower();
        return n.Contains("indicator") || n.Contains("guide") || n.Contains("floating");
    }
}

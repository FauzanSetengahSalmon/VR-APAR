using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Mencegah player (XR Origin / VR Rig) dari menembus tembok dalam kondisi apapun:
/// 1. Movement joystick (smooth locomotion) ditahan oleh CharacterController (radius & height dinamis).
/// 2. Movement fisik (room-scale walking/leaning kepala) didorong mundur secara otomatis
///    jika kepala mendekati/menembus collider tembok.
/// </summary>
public class VRPlayerAntiWallClip : MonoBehaviour
{
    [Header("Player Anti Wall Settings")]
    [Tooltip("Radius tubuh/badan pemain (meter). Default 0.35m = 70cm lebar badan.")]
    public float bodyRadius = 0.35f;

    [Tooltip("Jarak aman minimal kepala dari permukaan tembok (meter).")]
    public float headBuffer = 0.25f;

    [Tooltip("Layer mask untuk tembok. Default = semua layer solid.")]
    public LayerMask wallLayers = ~0;

    private XROrigin _xrOrigin;
    private CharacterController _characterController;
    private Transform _headTransform;

    void Start()
    {
        _xrOrigin = GetComponent<XROrigin>();
        _characterController = GetComponent<CharacterController>();

        if (_xrOrigin != null && _xrOrigin.Camera != null)
        {
            _headTransform = _xrOrigin.Camera.transform;
        }
        else if (Camera.main != null)
        {
            _headTransform = Camera.main.transform;
        }

        if (_characterController != null)
        {
            _characterController.radius = bodyRadius;
            _characterController.skinWidth = 0.05f;
            _characterController.minMoveDistance = 0.001f;
        }
    }

    void Update()
    {
        UpdateCharacterControllerShape();
        PreventPhysicalHeadWallClip();
    }

    /// <summary>
    /// Update tinggi dan center CharacterController secara dinamis mengikuti posisi HMD / kamera.
    /// </summary>
    void UpdateCharacterControllerShape()
    {
        if (_characterController == null || _headTransform == null) return;

        float headHeight = Mathf.Clamp(_headTransform.localPosition.y, 0.8f, 2.5f);
        _characterController.height = headHeight;

        Vector3 newCenter = _headTransform.localPosition;
        newCenter.y = headHeight * 0.5f;
        _characterController.center = newCenter;
    }

    /// <summary>
    /// Cek apakah kepala pemain (HMD) mendekati/menembus collider tembok.
    /// Jika ya, dorong XR Origin mundur agar kepala tetap di luar tembok.
    /// </summary>
    void PreventPhysicalHeadWallClip()
    {
        if (_headTransform == null) return;

        Vector3 headPos = _headTransform.position;
        Collider[] hitColliders = Physics.OverlapSphere(headPos, headBuffer, wallLayers);

        foreach (var col in hitColliders)
        {
            // Abaikan trigger dan collider milik player sendiri
            if (col == null || col.isTrigger || col.gameObject == gameObject || col.transform.IsChildOf(transform))
                continue;

            Vector3 closestPoint = col.ClosestPoint(headPos);
            Vector3 pushDirection = headPos - closestPoint;
            pushDirection.y = 0f; // Dorong horizontal saja

            float distance = pushDirection.magnitude;

            if (distance < headBuffer && distance > 0.0001f)
            {
                float penetration = headBuffer - distance;
                Vector3 pushVector = pushDirection.normalized * penetration;

                // Geser XR Origin mundur dari tembok
                transform.position += pushVector;
            }
            else if (distance <= 0.0001f)
            {
                // Kepala tenggelam sepenuhnya di dalam collider - dorong dari center collider
                Vector3 dirFromCol = headPos - col.bounds.center;
                dirFromCol.y = 0f;
                if (dirFromCol.sqrMagnitude < 0.0001f) dirFromCol = -transform.forward;

                transform.position += dirFromCol.normalized * headBuffer;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (_headTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_headTransform.position, headBuffer);
        }
    }
}

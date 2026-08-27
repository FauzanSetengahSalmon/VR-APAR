using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Mencegah player (XR Origin / VR Rig) dari menembus tembok dalam kondisi apapun:
/// 1. Smooth Locomotion (Joystick) ditahan oleh CharacterController (radius & height tersinkronisasi).
/// 2. Room-scale Physical Movement (gerakan fisik badan/kepala) didorong mundur secara otomatis
///    jika kepala atau tubuh mendekati/menembus collider tembok (termasuk MeshCollider ruangan).
/// </summary>
[DefaultExecutionOrder(50)]
public class VRPlayerAntiWallClip : MonoBehaviour
{
    [Header("Player Body & Head Dimensions")]
    [Tooltip("Radius tubuh pemain untuk CharacterController (meter). Default 0.32m = 64cm lebar badan.")]
    public float bodyRadius = 0.32f;

    [Tooltip("Radius bola deteksi kepala HMD (meter).")]
    public float headRadius = 0.15f;

    [Tooltip("Jarak aman tambahan kepala dari permukaan tembok (meter).")]
    public float headBuffer = 0.12f;

    [Header("Collision Layers")]
    [Tooltip("Layer mask untuk tembok/rintangan solid. Default = semua layer.")]
    public LayerMask wallLayers = ~0;

    [Header("Batas Ketinggian")]
    public float minHeight = 0.8f;
    public float maxHeight = 2.4f;

    private XROrigin _xrOrigin;
    private CharacterController _characterController;
    private Transform _headTransform;

    private Vector3 _lastSafeHeadPos;
    private Vector3 _lastSafeRigPos;
    private bool _hasInitialized = false;

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
            _characterController.slopeLimit = 45f;
            _characterController.stepOffset = 0.3f;
        }

        if (_headTransform != null)
        {
            _lastSafeHeadPos = _headTransform.position;
            _lastSafeRigPos = transform.position;
            _hasInitialized = true;
        }

        // Pastikan collider lingkungan sudah aktif
        VRSceneEnvironmentColliders.GenerateEnvironmentColliders();
    }

    void Update()
    {
        if (_headTransform == null)
        {
            if (Camera.main != null)
                _headTransform = Camera.main.transform;
            else
                return;
        }

        if (!_hasInitialized)
        {
            _lastSafeHeadPos = _headTransform.position;
            _lastSafeRigPos = transform.position;
            _hasInitialized = true;
        }

        UpdateCharacterControllerShape();
        PreventPhysicalHeadWallClip();
    }

    /// <summary>
    /// Update tinggi dan center CharacterController secara dinamis mengikuti posisi HMD / kamera
    /// dalam koordinat lokal XR Origin.
    /// </summary>
    void UpdateCharacterControllerShape()
    {
        if (_characterController == null || _headTransform == null) return;

        Vector3 headLocalPos = transform.InverseTransformPoint(_headTransform.position);
        float headHeight = Mathf.Clamp(headLocalPos.y, minHeight, maxHeight);

        _characterController.height = headHeight;

        Vector3 newCenter = new Vector3(headLocalPos.x, headHeight * 0.5f, headLocalPos.z);
        _characterController.center = newCenter;
        _characterController.radius = bodyRadius;
    }

    /// <summary>
    /// Deteksi lintasan dan posisi kepala pemain terhadap collider tembok (MeshCollider, BoxCollider, dll).
    /// Jika kepala mendekati atau menembus tembok karena gerakan fisik room-scale,
    /// dorong XR Origin mundur agar kepala tetap di luar tembok.
    /// </summary>
    void PreventPhysicalHeadWallClip()
    {
        if (_headTransform == null) return;

        Vector3 currentHeadPos = _headTransform.position;
        Vector3 bodyOrigin = transform.position + Vector3.up * (_characterController != null ? _characterController.height * 0.5f : 1.0f);

        bool collisionDetected = false;
        Vector3 pushbackVector = Vector3.zero;

        // 1. Raycast / SphereCast dari posisi aman terakhir ke posisi kepala saat ini
        Vector3 moveDir = currentHeadPos - _lastSafeHeadPos;
        float moveDist = moveDir.magnitude;

        if (moveDist > 0.001f)
        {
            if (Physics.SphereCast(_lastSafeHeadPos, headRadius, moveDir.normalized, out RaycastHit hit, moveDist + headBuffer, wallLayers, QueryTriggerInteraction.Ignore))
            {
                if (!IsPlayerChildCollider(hit.collider))
                {
                    collisionDetected = true;
                    // Hitung seberapa jauh kepala melewati batas aman tembok
                    Vector3 targetSafePos = hit.point + hit.normal * (headRadius + headBuffer);
                    Vector3 offset = targetSafePos - currentHeadPos;
                    offset.y = 0f; // Dorong horizontal saja
                    pushbackVector = offset;
                }
            }
        }

        // 2. Linecast dari badan / origin ke kepala (mencegah menjulurkan kepala lewat dinding)
        if (!collisionDetected)
        {
            Vector3 bodyToHead = currentHeadPos - bodyOrigin;
            float bodyToHeadDist = bodyToHead.magnitude;

            if (bodyToHeadDist > 0.05f)
            {
                if (Physics.Raycast(bodyOrigin, bodyToHead.normalized, out RaycastHit bodyHit, bodyToHeadDist + headRadius, wallLayers, QueryTriggerInteraction.Ignore))
                {
                    if (!IsPlayerChildCollider(bodyHit.collider))
                    {
                        collisionDetected = true;
                        Vector3 targetSafePos = bodyHit.point + bodyHit.normal * (headRadius + headBuffer);
                        Vector3 offset = targetSafePos - currentHeadPos;
                        offset.y = 0f;
                        pushbackVector = offset;
                    }
                }
            }
        }

        // 3. Overlap Sphere cek langsung di posisi kepala saat ini
        if (!collisionDetected)
        {
            Collider[] colliders = Physics.OverlapSphere(currentHeadPos, headRadius, wallLayers, QueryTriggerInteraction.Ignore);
            foreach (var col in colliders)
            {
                if (col == null || col.isTrigger || IsPlayerChildCollider(col)) continue;

                collisionDetected = true;
                Vector3 closestPoint = col.ClosestPoint(currentHeadPos);
                Vector3 dir = currentHeadPos - closestPoint;
                dir.y = 0f;

                if (dir.sqrMagnitude < 0.0001f)
                {
                    dir = -_headTransform.forward;
                    dir.y = 0f;
                }

                pushbackVector = dir.normalized * (headRadius + headBuffer);
                break;
            }
        }

        // Eksekusi dorongan mundur jika ada tabrakan
        if (collisionDetected)
        {
            if (pushbackVector.sqrMagnitude > 0.00001f)
            {
                transform.position += pushbackVector;
            }
            else
            {
                // Fallback ke posisi aman terakhir
                transform.position = _lastSafeRigPos;
            }
        }
        else
        {
            // Catat posisi aman saat ini
            _lastSafeHeadPos = currentHeadPos;
            _lastSafeRigPos = transform.position;
        }
    }

    /// <summary>
    /// Cek apakah collider adalah milik player sendiri.
    /// </summary>
    bool IsPlayerChildCollider(Collider col)
    {
        if (col == null) return true;
        if (col.gameObject == gameObject) return true;
        if (col.transform.IsChildOf(transform)) return true;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (_headTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_headTransform.position, headRadius + headBuffer);
        }
    }
}

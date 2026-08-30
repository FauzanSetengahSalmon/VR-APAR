using System.Collections;
using UnityEngine;

/// <summary>
/// Menjaga UI Billboard agar selalu berada di depan pandangan mata VR Player
/// dan secara otomatis mencegah UI menembus tembok, sekat, atau objek padat lainnya.
/// </summary>
public class VRBillboardUI : MonoBehaviour
{
    [Header("Posisi Billboard")]
    public Transform targetCamera;

    [Tooltip("Jarak ideal UI di depan mata pemain (meter).")]
    public float distance = 1.8f;

    [Tooltip("Jarak minimal UI dari kamera saat terhalang tembok (meter).")]
    public float minDistance = 0.5f;

    [Tooltip("Offset ketinggian UI relatif terhadap tinggi mata (meter).")]
    public float heightOffset = 0.05f;

    [Tooltip("Kecepatan lerp mengikuti gerakan kepala.")]
    public float smoothSpeed = 8.0f;

    [Header("Anti Wall Clip")]
    [Tooltip("Layer mask untuk deteksi tembok/rintangan solid.")]
    public LayerMask wallLayers = ~0;

    [Tooltip("Jarak aman UI dari permukaan tembok (meter).")]
    public float wallSafeMargin = 0.15f;

    [Tooltip("Radius bola spherecast untuk mendeteksi batas lebar UI.")]
    public float checkRadius = 0.2f;

    // Flag agar billboard tidak aktif sebelum tracking headset stabil
    private bool _isReady = false;

    void Start()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
        StartCoroutine(WaitForTracking());
    }

    private IEnumerator WaitForTracking()
    {
        _isReady = false;

        // Tunggu beberapa frame sampai tracking XR stabil
        for (int i = 0; i < 5; i++)
            yield return null;

        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;

        if (targetCamera != null)
        {
            Vector3 forward = targetCamera.forward;
            forward.y = 0f;
            int maxWait = 30;
            while (forward.sqrMagnitude < 0.01f && maxWait-- > 0)
            {
                yield return null;
                forward = targetCamera.forward;
                forward.y = 0f;
            }

            SnapToFront();
        }

        _isReady = true;
    }

    void LateUpdate()
    {
        if (!_isReady) return;

        if (targetCamera == null)
        {
            if (Camera.main != null) targetCamera = Camera.main.transform;
            else return;
        }

        // Arah pandang kamera horizontal (sumbu Y dinetralkan)
        Vector3 cameraForward = targetCamera.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.01f) return;
        cameraForward.Normalize();

        Vector3 camPos = targetCamera.position;
        float targetDist = distance;

        // === Anti-Wall Clip: SphereCast & Raycast dari kamera ke arah UI ===
        RaycastHit hit;
        if (Physics.SphereCast(camPos, checkRadius, cameraForward, out hit, distance + checkRadius, wallLayers, QueryTriggerInteraction.Ignore))
        {
            // Ada tembok/rintangan di depan -- batasi jarak agar tetap di depan tembok
            float hitDist = hit.distance;
            targetDist = Mathf.Max(minDistance, hitDist - wallSafeMargin);
        }
        else if (Physics.Raycast(camPos, cameraForward, out hit, distance, wallLayers, QueryTriggerInteraction.Ignore))
        {
            targetDist = Mathf.Max(minDistance, hit.distance - wallSafeMargin);
        }

        // Posisi target yang aman
        Vector3 targetPosition = camPos + (cameraForward * targetDist);
        targetPosition.y = camPos.y + heightOffset;

        // === Instant Wall Barrier ===
        // Jika posisi UI saat ini lebih jauh dari jarak batas tembok,
        // langsung tarik UI mendekat seketika agar tidak ada frame tembus tembok saat lerp
        float currentDistFromCam = Vector3.Distance(camPos, transform.position);
        if (currentDistFromCam > targetDist + 0.1f)
        {
            Vector3 clampedPos = camPos + (transform.position - camPos).normalized * targetDist;
            clampedPos.y = camPos.y + heightOffset;
            transform.position = clampedPos;
        }

        // Lerp mulus ke posisi target
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // Rotasi selalu menghadap ke pemain
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    /// <summary>
    /// Langsung posisikan UI tepat di depan mata pemain seketika (tanpa delay).
    /// </summary>
    public void SnapToFront()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;

        if (targetCamera == null) return;

        Vector3 cameraForward = targetCamera.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude > 0.01f)
        {
            cameraForward.Normalize();
            Vector3 camPos = targetCamera.position;
            float targetDist = distance;

            if (Physics.SphereCast(camPos, checkRadius, cameraForward, out RaycastHit hit, distance + checkRadius, wallLayers, QueryTriggerInteraction.Ignore))
            {
                targetDist = Mathf.Max(minDistance, hit.distance - wallSafeMargin);
            }

            Vector3 targetPosition = camPos + (cameraForward * targetDist);
            targetPosition.y = camPos.y + heightOffset;
            transform.position = targetPosition;
            transform.rotation = Quaternion.LookRotation(cameraForward);
        }
    }
}
using System.Collections;
using UnityEngine;

public class VRBillboardUI : MonoBehaviour
{
    public Transform targetCamera;
    public float distance = 2.0f;
    public float heightOffset = 0.0f; // atur tinggi UI dari mata
    public float smoothSpeed = 5.0f;

    [Header("Anti Wall Clip")]
    [Tooltip("Layer mask untuk deteksi tembok. Default = semua layer.")]
    public LayerMask wallLayers = ~0; // semua layer

    [Tooltip("Jarak aman UI dari permukaan tembok (meter)")]
    public float wallSafeMargin = 0.12f;

    // Flag agar billboard tidak aktif sampai tracking headset stabil
    private bool _isReady = false;

    void Start()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
        StartCoroutine(WaitForTracking());
    }

    // Tunggu beberapa frame agar headset tracking benar-benar aktif
    private IEnumerator WaitForTracking()
    {
        _isReady = false;

        // Tunggu minimal 5 frame sampai tracking XR stabil
        for (int i = 0; i < 5; i++)
            yield return null;

        // Jika tracking belum memberikan arah yang valid, tunggu lagi
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

            // Langsung snap ke posisi yang benar (tanpa lerp) saat pertama kali
            forward.Normalize();
            if (forward.sqrMagnitude > 0.01f)
            {
                Vector3 snapPos = targetCamera.position + forward * distance;
                snapPos.y = targetCamera.position.y + heightOffset;
                transform.position = snapPos;
                transform.rotation = Quaternion.LookRotation(forward);
            }
        }

        _isReady = true;
    }

    void LateUpdate()
    {
        if (!_isReady || targetCamera == null) return;

        // Ambil arah pandang kamera tapi Nol-kan sumbu Y (biar ga ngikut atas/bawah)
        Vector3 cameraForward = targetCamera.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        // Guard: jika forward tidak valid (tracking belum stabil), skip frame ini
        if (cameraForward.sqrMagnitude < 0.01f) return;

        // Tentukan posisi UI tepat di depan pemain sejajar mata
        Vector3 targetPosition = targetCamera.position + (cameraForward * distance);
        targetPosition.y = targetCamera.position.y + heightOffset; // Jaga ketinggian tetap relatif

        // === Anti Wall Clip: raycast dari kamera ke target ===
        Vector3 rayOrigin = targetCamera.position;
        Vector3 rayDir = targetPosition - rayOrigin;
        float rayDist = rayDir.magnitude;
        if (rayDist > 0.01f)
        {
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDist, wallLayers))
            {
                // Ada tembok di antara kamera dan posisi UI — mundurkan UI ke depan tembok
                targetPosition = hit.point - rayDir.normalized * wallSafeMargin;
                targetPosition.y = targetCamera.position.y + heightOffset;
            }
        }

        // Gerakkan posisi secara smooth
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // Putar UI agar selalu menghadap ke pemain (hanya rotasi Y / Kiri-Kanan)
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    /// <summary>
    /// Langsung posisikan UI tepat di depan mata pemain seketika (tanpa animasi lerp geser)
    /// </summary>
    public void SnapToFront()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;

        if (targetCamera == null) return;

        Vector3 cameraForward = targetCamera.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        if (cameraForward.sqrMagnitude > 0.01f)
        {
            Vector3 targetPosition = targetCamera.position + (cameraForward * distance);
            targetPosition.y = targetCamera.position.y + heightOffset;

            // Anti wall clip
            Vector3 rayOrigin = targetCamera.position;
            Vector3 rayDir = targetPosition - rayOrigin;
            float rayDist = rayDir.magnitude;
            if (rayDist > 0.01f)
            {
                if (Physics.Raycast(rayOrigin, rayDir.normalized, out RaycastHit hit, rayDist, wallLayers))
                {
                    targetPosition = hit.point - rayDir.normalized * wallSafeMargin;
                    targetPosition.y = targetCamera.position.y + heightOffset;
                }
            }

            transform.position = targetPosition;
            transform.rotation = Quaternion.LookRotation(cameraForward);
        }
    }
}
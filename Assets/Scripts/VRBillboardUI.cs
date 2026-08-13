using UnityEngine;

public class VRBillboardUI : MonoBehaviour
{
    public Transform targetCamera;
    public float distance = 2.0f;
    public float heightOffset = 0.0f; // atur tinggi UI dari mata
    public float smoothSpeed = 5.0f;

    void Start()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // Ambil arah pandang kamera tapi Nol-kan sumbu Y (biar ga ngikut atas/bawah)
        Vector3 cameraForward = targetCamera.forward;
        cameraForward.y = 0; 
        cameraForward.Normalize();

        // Tentukan posisi UI tepat di depan pemain sejajar mata
        Vector3 targetPosition = targetCamera.position + (cameraForward * distance);
        targetPosition.y = targetCamera.position.y + heightOffset; // Jaga ketinggian tetap relatif

        // Gerakkan posisi secara smooth
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // Putar UI agar selalu menghadap ke pemain (hanya rotasi Y / Kiri-Kanan)
        if (cameraForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
        }
    }
}
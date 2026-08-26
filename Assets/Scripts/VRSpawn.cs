using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

public class VRSpawn : MonoBehaviour
{
    public Transform targetSpawnPoint;

    [SerializeField] private int waitFrames = 5;

    private XROrigin           _xrOrigin;
    private CharacterController _cc;

    IEnumerator Start()
    {
        _xrOrigin = GetComponent<XROrigin>();
        _cc = GetComponent<CharacterController>();

        // Nonaktifkan CharacterController sementara agar tidak block teleport
        if (_cc != null) _cc.enabled = false;

        // Tunggu beberapa frame agar headset tracking benar-benar aktif
        for (int i = 0; i < waitFrames; i++)
            yield return null;

        if (targetSpawnPoint == null)
        {
            Debug.LogWarning("[VRSpawn] targetSpawnPoint belum di-assign!");
            if (_cc != null) _cc.enabled = true;
            yield break;
        }

        // Pastikan Camera sudah ada
        if (Camera.main == null)
        {
            Debug.LogError("[VRSpawn] Camera.main tidak ditemukan!");
            if (_cc != null) _cc.enabled = true;
            yield break;
        }

        // ── Langkah 1: Rotasi XR Origin agar arah hadap kepala = target.forward ──
        Vector3 camForwardFlat = Camera.main.transform.forward;
        camForwardFlat.y = 0f;
        if (camForwardFlat.sqrMagnitude > 0.001f)
        {
            camForwardFlat.Normalize();
            Vector3 targetForwardFlat = targetSpawnPoint.forward;
            targetForwardFlat.y = 0f;
            targetForwardFlat.Normalize();

            float angleDiff = Vector3.SignedAngle(camForwardFlat, targetForwardFlat, Vector3.up);
            transform.Rotate(0f, angleDiff, 0f);
        }

        // ── Langkah 2: Geser XR Origin agar posisi kepala = targetSpawnPoint.position ──
        // Hitung offset horizontal antara kepala dan XR Origin (= room-scale tracking offset)
        Vector3 headPos = Camera.main.transform.position;
        Vector3 xrPos   = transform.position;

        float headOffsetX = headPos.x - xrPos.x;
        float headOffsetZ = headPos.z - xrPos.z;

        Vector3 target = targetSpawnPoint.position;

        transform.position = new Vector3(
            target.x - headOffsetX,
            target.y,           // Y = lantai dari spawn point
            target.z - headOffsetZ
        );

        Debug.Log($"[VRSpawn] Spawned. XROrigin={transform.position}, Head={Camera.main.transform.position}");

        // Aktifkan kembali CharacterController
        if (_cc != null) _cc.enabled = true;
    }
}
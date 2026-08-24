using System.Collections;
using UnityEngine;

public class VRSpawn : MonoBehaviour
{
    public Transform targetSpawnPoint;

    IEnumerator Start()
    {
        yield return null; // Tunggu tracking headset aktif

        if (targetSpawnPoint != null && Camera.main != null)
        {
            Vector3 targetPos = targetSpawnPoint.position;
            // Kunci Y ke posisi lantai spawn point
            targetPos.y = targetSpawnPoint.position.y; 

            Vector3 camOffset = Camera.main.transform.position - transform.position;
            camOffset.y = 0; // Abaikan tinggi mata headset

            transform.position = targetPos - camOffset;
            transform.rotation = targetSpawnPoint.rotation;
        }
    }
}
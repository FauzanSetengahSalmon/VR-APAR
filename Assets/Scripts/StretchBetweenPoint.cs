using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class StretchBetweenPoint : MonoBehaviour
{
    [Header("Titik Selang")]
    [Tooltip("Pangkal selang di bodi tabung APAR.")]
    public Transform pointA;

    [Tooltip("Ujung selang di corong.")]
    public Transform pointB;

    [Header("Referensi APAR")]
    [Tooltip("AutoFireExtinguisher pada APAR Full.")]
    public AutoFireExtinguisher apar;

    [Header("Bentuk Selang")]
    [Range(2, 60)]
    public int segments = 30;

    [Tooltip("Seberapa besar lengkungan ke bawah.")]
    public float sagAmount = 0.18f;

    [Header("Ketebalan Selang")]
    public float hoseWidth = 0.025f;

    private LineRenderer lr;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.positionCount = segments;
        lr.startWidth = hoseWidth;
        lr.endWidth = hoseWidth;
        lr.numCapVertices = 6;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Cari APAR kalau belum diisi
        if (apar == null)
            apar = GetComponentInParent<AutoFireExtinguisher>();

        // Cari titik secara lokal, bukan GameObject.Find seluruh scene
        if (pointA == null && apar != null)
        {
            Transform[] children = apar.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in children)
            {
                if (t.name == "Point A")
                {
                    pointA = t;
                    break;
                }
            }
        }

        if (pointB == null && apar != null)
        {
            Transform[] children = apar.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in children)
            {
                if (t.name == "GrapPoint")
                {
                    pointB = t;
                    break;
                }
            }
        }

        // Pastikan awalnya tidak menggambar selang
        lr.enabled = false;
    }

    private void LateUpdate()
    {
        if (lr == null)
            return;

        // =====================================================
        // SELANG PENGGANTI HANYA AKTIF SAAT APAR SUDAH DIAMBIL
        // =====================================================

        if (apar == null || !apar.isAttachedToHand)
        {
            lr.enabled = false;
            return;
        }

        if (pointA == null || pointB == null)
        {
            lr.enabled = false;
            return;
        }

        // Aktifkan LineRenderer setelah APAR diambil
        lr.enabled = true;

        lr.positionCount = segments;
        lr.startWidth = hoseWidth;
        lr.endWidth = hoseWidth;

        DrawHose();
    }

    private void DrawHose()
    {
        Vector3 start = pointA.position;
        Vector3 end = pointB.position;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);

            Vector3 pos = Vector3.Lerp(start, end, t);

            // Lengkungan selang ke bawah
            float sag = 4f * t * (1f - t) * sagAmount;
            pos.y -= sag;

            lr.SetPosition(i, pos);
        }
    }
}
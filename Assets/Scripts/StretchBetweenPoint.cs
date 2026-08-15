using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class StretchBetweenPoint : MonoBehaviour
{
    [Header("Titik Selang")]
    public Transform pointA;
    public Transform pointB;

    [Header("Bentuk Selang")]
    [Range(2, 50)]
    public int segments = 20;

    [Tooltip("Semakin besar, semakin melengkung ke bawah.")]
    public float sagAmount = 0.25f;

    [Tooltip("Arah lengkungan. Biasanya Vector3.down.")]
    public Vector3 sagDirection = Vector3.down;

    [Header("Ketebalan")]
    public float startWidth = 0.025f;
    public float endWidth = 0.025f;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();

        line.useWorldSpace = true;

        line.positionCount = segments;

        line.startWidth = startWidth;
        line.endWidth = endWidth;
    }

    void LateUpdate()
    {
        if (pointA == null || pointB == null)
            return;

        UpdateHose();
    }

    void UpdateHose()
    {
        Vector3 start = pointA.position;
        Vector3 end = pointB.position;

        Vector3 direction = sagDirection.normalized;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);

            // Posisi lurus antara A dan B
            Vector3 position = Vector3.Lerp(start, end, t);

            // Lengkungan berbentuk parabola.
            // Nilai 0 di ujung dan maksimal di tengah.
            float curve = 4f * t * (1f - t);

            position += direction * (curve * sagAmount);

            line.SetPosition(i, position);
        }
    }
}
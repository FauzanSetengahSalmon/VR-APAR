using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SketchBetweenPoint : MonoBehaviour
{
    [Header("Points")]
    public Transform pointA;
    public Transform pointB;

    [Header("Curve Direction")]
    public Transform curveDirection;

    [Range(4, 50)]
    public int segments = 20;

    public float curveAmount = 0.3f;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments;
    }

    void Update()
    {
        if (pointA == null || pointB == null)
            return;

        UpdateHose();
    }

    void UpdateHose()
    {
        Vector3 start = pointA.position;
        Vector3 end = pointB.position;

        Vector3 direction = end - start;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        Vector3 curveDir;

        if (curveDirection != null)
        {
            curveDir = Vector3.ProjectOnPlane(
                -curveDirection.up,
                direction
            ).normalized;
        }
        else
        {
            curveDir = Vector3.down;
        }

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);

            Vector3 position = Vector3.Lerp(
                start,
                end,
                t
            );

            float curve = Mathf.Sin(t * Mathf.PI) * curveAmount;

            position += curveDir * curve;

            line.SetPosition(i, position);
        }
    }
}
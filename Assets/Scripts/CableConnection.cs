
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CableConnection : MonoBehaviour
{
    private LineRenderer line;

    private Transform startPoint;
    private Transform endPoint;

    void Awake()
    {
        line = GetComponent<LineRenderer>();

        line.positionCount = 2;
        line.useWorldSpace = true;
    }

    public void SetConnection(
        Transform start,
        Transform end)
    {
        startPoint = start;
        endPoint = end;
    }

    void LateUpdate()
    {
        if (startPoint == null || endPoint == null)
            return;

        line.SetPosition(
            0,
            startPoint.position
        );

        line.SetPosition(
            1,
            endPoint.position
        );
    }
}

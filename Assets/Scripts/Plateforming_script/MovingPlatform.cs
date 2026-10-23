using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public enum MoveAxis
    {
        Horizontal,
        Vertical
    }

    public enum FirstDirection
    {
        Positive,
        Negative
    }

    [Header("Movement Settings")]
    [SerializeField] private MoveAxis moveAxis = MoveAxis.Horizontal;
    [SerializeField] private FirstDirection firstDirection = FirstDirection.Positive;
    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Debug")]
    [SerializeField] private bool drawPath = true;

    private Vector3 startPosition;
    private Vector3 pointA;
    private Vector3 pointB;
    private Vector3 targetPoint;

    private Vector3 previousPosition;
    private Vector3 deltaPosition;

    public Vector3 DeltaPosition => deltaPosition;

    private void Start()
    {
        startPosition = transform.position;

        Vector3 direction = GetMoveDirection();

        pointA = startPosition;
        pointB = startPosition + direction * moveDistance;

        targetPoint = pointB;

        previousPosition = transform.position;
        deltaPosition = Vector3.zero;
    }

    private void FixedUpdate()
    {
        previousPosition = transform.position;

        MovePlatform();

        deltaPosition = transform.position - previousPosition;
    }

    private void MovePlatform()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPoint,
            moveSpeed * Time.fixedDeltaTime
        );

        if (Vector3.Distance(transform.position, targetPoint) <= 0.01f)
        {
            SwitchTarget();
        }
    }

    private void SwitchTarget()
    {
        if (targetPoint == pointA)
        {
            targetPoint = pointB;
        }
        else
        {
            targetPoint = pointA;
        }
    }

    private Vector3 GetMoveDirection()
    {
        Vector3 direction = Vector3.right;

        if (moveAxis == MoveAxis.Horizontal)
        {
            direction = Vector3.right;
        }
        else if (moveAxis == MoveAxis.Vertical)
        {
            direction = Vector3.up;
        }

        if (firstDirection == FirstDirection.Negative)
        {
            direction *= -1f;
        }

        return direction;
    }

    private void OnValidate()
    {
        if (moveDistance < 0f)
        {
            moveDistance = 0f;
        }

        if (moveSpeed < 0f)
        {
            moveSpeed = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPath) return;

        Vector3 previewStart = Application.isPlaying ? startPosition : transform.position;
        Vector3 previewEnd = previewStart + GetMoveDirection() * moveDistance;

        Gizmos.DrawLine(previewStart, previewEnd);
        Gizmos.DrawWireSphere(previewStart, 0.15f);
        Gizmos.DrawWireSphere(previewEnd, 0.15f);
    }
}
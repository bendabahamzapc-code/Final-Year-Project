using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float patrolDistance = 3f;

    [Header("Visual")]
    [SerializeField] private bool flipSprite = true;

    private Vector2 startPosition;
    private int direction = 1;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        Patrol();
    }

    private void Patrol()
    {
        transform.Translate(Vector2.right * direction * moveSpeed * Time.deltaTime);

        float distanceFromStart = transform.position.x - startPosition.x;

        if (distanceFromStart >= patrolDistance)
        {
            direction = -1;
            Flip();
        }
        else if (distanceFromStart <= -patrolDistance)
        {
            direction = 1;
            Flip();
        }
    }

    private void Flip()
    {
        if (!flipSprite) return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? startPosition : (Vector2)transform.position;

        Gizmos.DrawLine(
            center + Vector3.left * patrolDistance,
            center + Vector3.right * patrolDistance
        );

        Gizmos.DrawWireSphere(center + Vector3.left * patrolDistance, 0.15f);
        Gizmos.DrawWireSphere(center + Vector3.right * patrolDistance, 0.15f);
    }
}
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class IrregularEnemyPatrol : MonoBehaviour
{
    public enum PatrolAxis
    {
        Horizontal,
        Vertical
    }

    public enum StartingDirection
    {
        Positive,
        Negative
    }

    [Header("Main Patrol")]
    [SerializeField] private PatrolAxis patrolAxis = PatrolAxis.Horizontal;
    [SerializeField] private StartingDirection startingDirection = StartingDirection.Positive;
    [SerializeField] private float patrolDistance = 4f;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Irregular Movement")]
    [SerializeField] private float irregularityIntensity = 0.5f;
    [SerializeField] private float irregularitySpeed = 1.5f;

    [Header("Visual")]
    [SerializeField] private bool flipSpriteWhenHorizontal = true;
    [SerializeField] private Transform visualTransform;

    [Header("Debug")]
    [SerializeField] private bool drawPatrolPath = true;

    private Rigidbody2D rb;

    private Vector2 startPosition;
    private float mainAxisOffset;
    private int currentDirection;

    private float noiseSeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (visualTransform == null)
        {
            visualTransform = transform;
        }
    }

    private void Start()
    {
        startPosition = rb.position;

        currentDirection =
            startingDirection == StartingDirection.Positive ? 1 : -1;

        noiseSeed = Random.Range(0f, 1000f);

        UpdateVisualDirection();
    }

    private void FixedUpdate()
    {
        MoveEnemy();
    }

    private void MoveEnemy()
    {
        // Avance sur l’axe principal.
        mainAxisOffset += currentDirection * moveSpeed * Time.fixedDeltaTime;

        // Inverse la direction aux limites de la patrouille.
        if (mainAxisOffset >= patrolDistance)
        {
            mainAxisOffset = patrolDistance;
            ChangeDirection(-1);
        }
        else if (mainAxisOffset <= -patrolDistance)
        {
            mainAxisOffset = -patrolDistance;
            ChangeDirection(1);
        }

        Vector2 mainAxis;
        Vector2 secondaryAxis;

        if (patrolAxis == PatrolAxis.Horizontal)
        {
            mainAxis = Vector2.right;
            secondaryAxis = Vector2.up;
        }
        else
        {
            mainAxis = Vector2.up;
            secondaryAxis = Vector2.right;
        }

        // Génère une variation aléatoire mais fluide.
        float noiseValue = Mathf.PerlinNoise(
            noiseSeed,
            Time.time * irregularitySpeed
        );

        // Transforme le résultat de 0–1 vers -1–1.
        float secondaryOffset =
            (noiseValue * 2f - 1f) * irregularityIntensity;

        Vector2 targetPosition =
            startPosition
            + mainAxis * mainAxisOffset
            + secondaryAxis * secondaryOffset;

        rb.MovePosition(targetPosition);
    }

    private void ChangeDirection(int newDirection)
    {
        if (currentDirection == newDirection)
        {
            return;
        }

        currentDirection = newDirection;
        UpdateVisualDirection();
    }

    private void UpdateVisualDirection()
    {
        if (!flipSpriteWhenHorizontal)
        {
            return;
        }

        if (patrolAxis != PatrolAxis.Horizontal)
        {
            return;
        }

        if (visualTransform == null)
        {
            return;
        }

        Vector3 scale = visualTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * currentDirection;
        visualTransform.localScale = scale;
    }

    private void OnValidate()
    {
        patrolDistance = Mathf.Max(0f, patrolDistance);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        irregularityIntensity = Mathf.Max(0f, irregularityIntensity);
        irregularitySpeed = Mathf.Max(0f, irregularitySpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPatrolPath)
        {
            return;
        }

        Vector3 center = Application.isPlaying
            ? (Vector3)startPosition
            : transform.position;

        Vector3 mainAxis =
            patrolAxis == PatrolAxis.Horizontal
                ? Vector3.right
                : Vector3.up;

        Vector3 secondaryAxis =
            patrolAxis == PatrolAxis.Horizontal
                ? Vector3.up
                : Vector3.right;

        Vector3 firstEnd = center + mainAxis * patrolDistance;
        Vector3 secondEnd = center - mainAxis * patrolDistance;

        Gizmos.DrawLine(firstEnd, secondEnd);

        Gizmos.DrawWireSphere(firstEnd, 0.15f);
        Gizmos.DrawWireSphere(secondEnd, 0.15f);

        Gizmos.DrawLine(
            firstEnd - secondaryAxis * irregularityIntensity,
            firstEnd + secondaryAxis * irregularityIntensity
        );

        Gizmos.DrawLine(
            secondEnd - secondaryAxis * irregularityIntensity,
            secondEnd + secondaryAxis * irregularityIntensity
        );
    }
}
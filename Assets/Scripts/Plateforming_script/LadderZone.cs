using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LadderZone : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Ladder Points")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private Transform topPoint;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform topStandPoint;

    [Header("Snap Settings")]
    [SerializeField] private bool useExactSnapPoint = false;

    [Header("Exit Settings")]
    [SerializeField] private bool exitAtBottom = true;
    [SerializeField] private bool holdPlayerAtTopWithoutGround = true;

    public Vector2 SnapPosition =>
        snapPoint != null ? snapPoint.position : transform.position;

    public Vector2 TopStandPosition =>
        topStandPoint != null
            ? topStandPoint.position
            : new Vector2(SnapPosition.x, GetTopY());

    public bool UseExactSnapPoint => useExactSnapPoint;
    public bool ExitAtBottom => exitAtBottom;
    public bool HoldPlayerAtTopWithoutGround => holdPlayerAtTopWithoutGround;

    public float GetTopY()
    {
        float firstY = topPoint != null
            ? topPoint.position.y
            : transform.position.y + 1f;

        float secondY = bottomPoint != null
            ? bottomPoint.position.y
            : transform.position.y - 1f;

        return Mathf.Max(firstY, secondY);
    }

    public float GetBottomY()
    {
        float firstY = topPoint != null
            ? topPoint.position.y
            : transform.position.y + 1f;

        float secondY = bottomPoint != null
            ? bottomPoint.position.y
            : transform.position.y - 1f;

        return Mathf.Min(firstY, secondY);
    }

    private void Reset()
    {
        Collider2D ladderCollider = GetComponent<Collider2D>();

        if (ladderCollider != null)
        {
            ladderCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Évite que la hitbox d’attaque ou un autre enfant soit
        // considéré comme le joueur.
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        PlayerLadderController ladderController =
            other.GetComponent<PlayerLadderController>();

        if (ladderController == null)
        {
            ladderController =
                other.GetComponentInParent<PlayerLadderController>();
        }

        if (ladderController != null)
        {
            ladderController.EnterLadderZone(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        PlayerLadderController ladderController =
            other.GetComponent<PlayerLadderController>();

        if (ladderController == null)
        {
            ladderController =
                other.GetComponentInParent<PlayerLadderController>();
        }

        if (ladderController != null)
        {
            ladderController.ExitLadderZone(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 snapPosition = snapPoint != null
            ? snapPoint.position
            : transform.position;

        Vector3 topPosition = topPoint != null
            ? topPoint.position
            : transform.position + Vector3.up;

        Vector3 bottomPosition = bottomPoint != null
            ? bottomPoint.position
            : transform.position + Vector3.down;

        Vector3 standingPosition = topStandPoint != null
            ? topStandPoint.position
            : topPosition;

        Gizmos.DrawWireSphere(snapPosition, 0.15f);
        Gizmos.DrawWireSphere(topPosition, 0.15f);
        Gizmos.DrawWireSphere(bottomPosition, 0.15f);

        Gizmos.DrawWireCube(
            standingPosition,
            new Vector3(0.3f, 0.1f, 0f)
        );

        Gizmos.DrawLine(topPosition, bottomPosition);
    }
}
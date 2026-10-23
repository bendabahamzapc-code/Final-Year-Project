using UnityEngine;

public class MovingPlatformCarrier : MonoBehaviour
{
    [Header("Passenger Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Carry Settings")]
    [SerializeField] private bool carryPlayerWhenIdle = true;
    [SerializeField] private bool carryPlayerVertically = true;

    private MovingPlatform movingPlatform;
    private Rigidbody2D playerRb;
    private bool playerOnPlatform;

    private void Awake()
    {
        movingPlatform = GetComponent<MovingPlatform>();
    }

    private void FixedUpdate()
    {
        if (!playerOnPlatform)
        {
            return;
        }

        if (playerRb == null)
        {
            return;
        }

        if (movingPlatform == null)
        {
            return;
        }

        Vector3 platformDelta =
            movingPlatform.DeltaPosition;

        float horizontalInput = 0f;

        /*
         * NEW INPUT SYSTEM
         */
        if (GameInput.Instance != null)
        {
            horizontalInput =
                GameInput.Instance.MoveHorizontal;
        }

        Vector2 movementToApply =
            Vector2.zero;

        /*
         * Horizontal :
         *
         * La plateforme transporte le joueur
         * seulement s'il n'essaie pas lui-même
         * de marcher horizontalement.
         */
        if (
            carryPlayerWhenIdle &&
            Mathf.Abs(horizontalInput) < 0.01f
        )
        {
            movementToApply.x =
                platformDelta.x;
        }

        /*
         * Vertical :
         *
         * Le joueur suit toujours le mouvement
         * vertical de la plateforme.
         */
        if (carryPlayerVertically)
        {
            movementToApply.y =
                platformDelta.y;
        }

        playerRb.position +=
            movementToApply;
    }

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        if (
            !collision.collider.CompareTag(
                playerTag
            )
        )
        {
            return;
        }

        if (IsPlayerAbovePlatform(collision))
        {
            playerRb =
                collision.collider
                    .GetComponent<Rigidbody2D>();

            playerOnPlatform = true;
        }
    }

    private void OnCollisionStay2D(
        Collision2D collision
    )
    {
        if (
            !collision.collider.CompareTag(
                playerTag
            )
        )
        {
            return;
        }

        if (IsPlayerAbovePlatform(collision))
        {
            playerRb =
                collision.collider
                    .GetComponent<Rigidbody2D>();

            playerOnPlatform = true;
        }
    }

    private void OnCollisionExit2D(
        Collision2D collision
    )
    {
        if (
            !collision.collider.CompareTag(
                playerTag
            )
        )
        {
            return;
        }

        playerOnPlatform = false;
        playerRb = null;
    }

    private bool IsPlayerAbovePlatform(
        Collision2D collision
    )
    {
        foreach (
            ContactPoint2D contact
            in collision.contacts
        )
        {
            /*
             * La normale est orientée vers le bas
             * du point de vue de la plateforme
             * lorsque le joueur se trouve dessus.
             */
            if (contact.normal.y < -0.5f)
            {
                return true;
            }
        }

        return false;
    }
}
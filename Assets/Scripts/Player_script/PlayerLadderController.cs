using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerLadderController : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private Player playerMovement;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Rigidbody2D rb;

    [Header("Climbing")]
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float ladderEndTolerance = 0.05f;

    [Header("Ladder Jump")]
    [SerializeField] private float ladderJumpVerticalVelocity = 10f;
    [SerializeField] private float ladderJumpHorizontalVelocity = 6f;

    [Header("Damage Fall")]
    [SerializeField] private float damageFallSpeed = 3f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDelayAfterJump = 0.1f;

    [Header("Input")]
    [SerializeField] private float ladderEnterThreshold = 0.5f;

    private LadderZone availableLadder;
    private LadderZone currentLadder;

    private bool isClimbing;
    private bool isHoldingAtTop;
    private bool waitingToTouchGround;
    private bool forceVerticalFall;
    private bool mustLeaveZoneBeforeReentering;

    private bool playerMovementWasEnabled;
    private float savedGravityScale;
    private float groundCheckDelayTimer;

    private int lastKnownHealth;
    private bool healthValueInitialized;

    public bool IsClimbing => isClimbing;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<Player>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        CheckForDamage();

        if (waitingToTouchGround)
        {
            UpdateGroundedExit();
            return;
        }

        if (isClimbing)
        {
            if (JumpPressedThisFrame())
            {
                ExitLadderWithJump();
            }

            return;
        }

        TryEnterLadder();
    }

    private void FixedUpdate()
    {
        if (waitingToTouchGround)
        {
            if (forceVerticalFall)
            {
                rb.linearVelocity = new Vector2(
                    0f,
                    rb.linearVelocity.y
                );
            }

            return;
        }

        if (!isClimbing || currentLadder == null)
        {
            return;
        }

        if (isHoldingAtTop)
        {
            HandleTopHold();
            return;
        }

        MoveOnLadder();
    }

    // =====================================================
    // ENTER LADDER
    // =====================================================

    private void TryEnterLadder()
    {
        if (availableLadder == null)
        {
            return;
        }

        if (mustLeaveZoneBeforeReentering)
        {
            return;
        }

        if (ReadVerticalInput() > ladderEnterThreshold)
        {
            BeginClimbing(availableLadder);
        }
    }

    private void BeginClimbing(LadderZone ladder)
    {
        currentLadder = ladder;

        isClimbing = true;
        isHoldingAtTop = false;
        waitingToTouchGround = false;
        forceVerticalFall = false;

        mustLeaveZoneBeforeReentering = true;

        savedGravityScale = rb.gravityScale;

        if (playerMovement != null)
        {
            playerMovementWasEnabled =
                playerMovement.enabled;

            playerMovement.enabled = false;
        }

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        Vector2 snapPosition =
            ladder.SnapPosition;

        if (ladder.UseExactSnapPoint)
        {
            rb.position =
                snapPosition;
        }
        else
        {
            rb.position =
                new Vector2(
                    snapPosition.x,
                    rb.position.y
                );
        }
    }

    // =====================================================
    // LADDER MOVEMENT
    // =====================================================

    private void MoveOnLadder()
    {
        float verticalInput =
            ReadVerticalInput();

        rb.gravityScale = 0f;

        // Aucun déplacement horizontal pendant l'escalade.
        rb.linearVelocity =
            new Vector2(
                0f,
                verticalInput * climbSpeed
            );

        float topY =
            currentLadder.GetTopY();

        float bottomY =
            currentLadder.GetBottomY();

        if (
            verticalInput > 0f &&
            rb.position.y >=
            topY - ladderEndTolerance
        )
        {
            ReachTopOfLadder();
            return;
        }

        if (
            currentLadder.ExitAtBottom &&
            verticalInput < 0f &&
            rb.position.y <=
            bottomY + ladderEndTolerance
        )
        {
            rb.position =
                new Vector2(
                    rb.position.x,
                    bottomY
                );

            ExitLadderNormally();
        }
    }

    // =====================================================
    // TOP OF LADDER
    // =====================================================

    private void ReachTopOfLadder()
    {
        rb.position =
            currentLadder.TopStandPosition;

        rb.linearVelocity =
            Vector2.zero;

        Physics2D.SyncTransforms();

        if (IsGrounded())
        {
            ExitLadderNormally();
            return;
        }

        if (
            currentLadder
                .HoldPlayerAtTopWithoutGround
        )
        {
            isHoldingAtTop = true;
            rb.gravityScale = 0f;
            return;
        }

        ExitLadderNormally();
    }

    private void HandleTopHold()
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        rb.position =
            currentLadder.TopStandPosition;

        float verticalInput =
            ReadVerticalInput();

        // Appuyer vers le bas permet de redescendre.
        if (
            verticalInput <
            -ladderEnterThreshold
        )
        {
            isHoldingAtTop = false;

            rb.position =
                new Vector2(
                    currentLadder.SnapPosition.x,
                    currentLadder.GetTopY() -
                    ladderEndTolerance * 2f
                );
        }
    }

    // =====================================================
    // JUMP OFF LADDER
    // =====================================================

    private void ExitLadderWithJump()
    {
        float horizontalInput =
            Mathf.Clamp(
                ReadHorizontalInput(),
                -1f,
                1f
            );

        isClimbing = false;
        isHoldingAtTop = false;
        currentLadder = null;

        waitingToTouchGround = false;
        forceVerticalFall = false;

        rb.gravityScale =
            savedGravityScale;

        if (
            playerMovement != null &&
            playerMovementWasEnabled
        )
        {
            playerMovement.enabled = true;
        }

        rb.linearVelocity =
            new Vector2(
                horizontalInput *
                ladderJumpHorizontalVelocity,

                ladderJumpVerticalVelocity
            );
    }

    // =====================================================
    // DAMAGE
    // =====================================================

    private void DropFromLadderAfterDamage()
    {
        isClimbing = false;
        isHoldingAtTop = false;
        currentLadder = null;

        rb.gravityScale =
            savedGravityScale;

        // Chute totalement verticale.
        rb.linearVelocity =
            new Vector2(
                0f,
                -Mathf.Abs(damageFallSpeed)
            );

        waitingToTouchGround = true;
        forceVerticalFall = true;

        groundCheckDelayTimer =
            groundCheckDelayAfterJump;
    }

    // =====================================================
    // NORMAL LADDER EXIT
    // =====================================================

    private void ExitLadderNormally()
    {
        isClimbing = false;
        isHoldingAtTop = false;
        currentLadder = null;

        rb.gravityScale =
            savedGravityScale;

        rb.linearVelocity =
            Vector2.zero;

        if (IsGrounded())
        {
            RestoreNormalMovement();
        }
        else
        {
            waitingToTouchGround = true;
            forceVerticalFall = true;
        }
    }

    // =====================================================
    // DAMAGE CHECK
    // =====================================================

    private void CheckForDamage()
    {
        if (playerHealth == null)
        {
            return;
        }

        int currentHealth =
            playerHealth.CurrentHealth;

        if (!healthValueInitialized)
        {
            lastKnownHealth =
                currentHealth;

            healthValueInitialized =
                true;

            return;
        }

        bool healthWasReduced =
            currentHealth <
            lastKnownHealth;

        lastKnownHealth =
            currentHealth;

        if (
            healthWasReduced &&
            (isClimbing ||
             isHoldingAtTop)
        )
        {
            DropFromLadderAfterDamage();
        }
    }

    // =====================================================
    // RETURN TO NORMAL MOVEMENT
    // =====================================================

    private void UpdateGroundedExit()
    {
        if (
            groundCheckDelayTimer > 0f
        )
        {
            groundCheckDelayTimer -=
                Time.deltaTime;

            return;
        }

        if (IsGrounded())
        {
            RestoreNormalMovement();
        }
    }

    private void RestoreNormalMovement()
    {
        waitingToTouchGround = false;
        forceVerticalFall = false;

        rb.gravityScale =
            savedGravityScale;

        if (
            playerMovement != null &&
            playerMovementWasEnabled
        )
        {
            playerMovement.enabled = true;
        }
    }

    // =====================================================
    // GROUND CHECK
    // =====================================================

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    // =====================================================
    // NEW INPUT SYSTEM
    // =====================================================

    private float ReadVerticalInput()
    {
        if (GameInput.Instance == null)
        {
            return 0f;
        }

        return GameInput.Instance.ClimbVertical;
    }

    private float ReadHorizontalInput()
    {
        if (GameInput.Instance == null)
        {
            return 0f;
        }

        return GameInput.Instance.MoveHorizontal;
    }

    private bool JumpPressedThisFrame()
    {
        if (GameInput.Instance == null)
        {
            return false;
        }

        return
            GameInput.Instance
                .JumpPressedThisFrame;
    }

    // =====================================================
    // LADDER ZONE
    // =====================================================

    public void EnterLadderZone(
        LadderZone ladder
    )
    {
        availableLadder = ladder;
    }

    public void ExitLadderZone(
        LadderZone ladder
    )
    {
        if (availableLadder == ladder)
        {
            availableLadder = null;
        }

        /*
         * Le joueur peut reprendre cette échelle
         * seulement après être réellement sorti
         * de son trigger.
         */
        mustLeaveZoneBeforeReentering =
            false;

        /*
         * On ne force pas la sortie du mode
         * échelle ici.
         */
    }

    // =====================================================
    // DISABLE
    // =====================================================

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.gravityScale =
                savedGravityScale;
        }

        if (
            playerMovement != null &&
            playerMovementWasEnabled
        )
        {
            playerMovement.enabled = true;
        }
    }

    // =====================================================
    // GIZMOS
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}
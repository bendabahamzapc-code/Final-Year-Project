using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float groundMoveSpeed = 5f;
    [SerializeField] private float airMoveSpeed = 3f;
    [SerializeField] [Range(0f, 1f)] private float airControl = 0.7f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float timeToJumpApex = 0.4f;
    [SerializeField] private float fallMultiplier = 2f;

    [Header("Variable Jump")]
    [SerializeField] [Range(0.1f, 1f)]
    private float shortJumpMultiplier = 0.5f;

    [Header("Jump Forgiveness")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float groundCheckDisableAfterJump = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Grapple Settings")]
    [SerializeField] private Transform grappleOrigin;

    [SerializeField] [Range(0f, 1f)]
    private float chargingMoveMultiplier = 0.25f;

    [SerializeField] private float grappleLineSpeed = 25f;
    [SerializeField] private float grapplePullSpeed = 12f;
    [SerializeField] private float grappleMaxDistance = 12f;
    [SerializeField] private float grappleArrivalDistance = 0.15f;
    [SerializeField] private LayerMask grappleCollisionLayer;
    [SerializeField] private string grippableTag = "aggripable";

    [Header("Grapple Visual")]
    [SerializeField] private LineRenderer grappleLine;

    [Header("Grapple Preview")]
    [SerializeField] private LineRenderer grapplePreviewLine;

    [SerializeField] private Color grapplePreviewColor =
        new Color(0.5f, 0.5f, 0.5f, 0.35f);

    [SerializeField] private float grapplePreviewWidth = 0.04f;
    [SerializeField] private float grapplePreviewLength = 12f;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private PlayerSFXController playerSFX;

    private bool isGrounded;

    private float moveInput;
    private float jumpVelocity;
    private float gravityScale;

    private float coyoteTimeCounter;
    private float groundCheckDisableCounter;

    private int facingDirection = 1;

    private bool isChargingGrapple;
    private bool isGrapleFlying;
    private bool isBeingPulled;
    private bool isHooked;

    private Vector2 grappleDirection;
    private Vector2 currentGrappleEndPoint;
    private Vector2 grappleTargetPoint;

    private bool horizontalMovementLocked;

    private LandingDustEffect landingDustEffect;

    // =====================================================
    // LANDING SFX
    // =====================================================

    private bool waitingForLandingSFX;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerSFX = GetComponent<PlayerSFXController>();
        rb = GetComponent<Rigidbody2D>();

        if (grappleLine == null)
        {
            grappleLine = GetComponent<LineRenderer>();
        }

        landingDustEffect =
    GetComponent<LandingDustEffect>();
    }

    private void Start()
    {
        CalculateJumpValues();

        if (grappleLine != null)
        {
            grappleLine.positionCount = 2;
            grappleLine.enabled = false;
        }

        if (grapplePreviewLine != null)
        {
            grapplePreviewLine.positionCount = 2;
            grapplePreviewLine.enabled = false;

            grapplePreviewLine.startColor = grapplePreviewColor;
            grapplePreviewLine.endColor = grapplePreviewColor;

            grapplePreviewLine.startWidth = grapplePreviewWidth;
            grapplePreviewLine.endWidth = grapplePreviewWidth;
        }
    }

    private void Update()
    {
        moveInput = ReadHorizontalInput();

        if (moveInput > 0f)
        {
            facingDirection = 1;
        }
        else if (moveInput < 0f)
        {
            facingDirection = -1;
        }

        CheckGround();
        CheckForFalling();
        UpdateCoyoteTime();

        HandleJumpInput();
        HandleGrappleInput();

        UpdateGrappleLineVisual();
        UpdateGrapplePreviewLine();
    }

    private void CheckForFalling()
{
    /*
     * Si le joueur n'est plus au sol
     * et descend réellement,
     * le prochain contact avec le sol
     * doit produire l'effet d'atterrissage.
     */
    if (
        !isGrounded &&
        rb.linearVelocity.y < -0.1f &&
        !isHooked &&
        !isBeingPulled &&
        !isGrapleFlying
    )
    {
        waitingForLandingSFX = true;
    }
}

    private void FixedUpdate()
    {
        if (
            playerHealth != null &&
            playerHealth.IsKnockedBack
        )
        {
            return;
        }

        if (isHooked)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isGrapleFlying)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;

            MoveGrappleLine();

            return;
        }

        if (isBeingPulled)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;

            PullPlayerToGrapplePoint();

            return;
        }

        Move();
        ApplyBetterFall();
    }

    // =====================================================
    // JUMP
    // =====================================================

    private void HandleJumpInput()
    {
        if (JumpPressedThisFrame())
        {
            if (
                isHooked ||
                isBeingPulled ||
                isGrapleFlying
            )
            {
                ForceExitGrappleAndJump();
                return;
            }

            if (coyoteTimeCounter > 0f)
            {
                Jump();
            }
        }

        if (
            JumpReleasedThisFrame() &&
            rb.linearVelocity.y > 0f &&
            !isHooked &&
            !isBeingPulled &&
            !isGrapleFlying
        )
        {
            CutJump();
        }
    }

    private void Jump()
    {
        coyoteTimeCounter = 0f;

        groundCheckDisableCounter =
            groundCheckDisableAfterJump;

        rb.gravityScale =
            gravityScale;

        rb.linearVelocity =
            new Vector2(
                rb.linearVelocity.x,
                jumpVelocity
            );

        if (playerSFX != null)
        {
            playerSFX.PlayJump();
        }

        /*
         * Le prochain vrai contact avec le sol
         * déclenchera le SFX d'atterrissage.
         */
        waitingForLandingSFX = true;
    }

    private void CutJump()
    {
        rb.linearVelocity =
            new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y *
                shortJumpMultiplier
            );
    }

    // =====================================================
    // LANDING
    // =====================================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!waitingForLandingSFX)
        {
            return;
        }

        /*
         * Vérifie que l'objet touché se trouve
         * bien dans une layer considérée comme Ground.
         */
        int collisionLayer =
            1 << collision.gameObject.layer;

        if (
            (groundLayer.value & collisionLayer) == 0
        )
        {
            return;
        }

        /*
         * Vérifie que le joueur atterrit réellement
         * SUR le sol.
         *
         * Cela évite de déclencher le son
         * si le joueur touche simplement un mur.
         */
        foreach (
            ContactPoint2D contact
            in collision.contacts
        )
        {
            if (contact.normal.y > 0.5f)
            {
                if (playerSFX != null)
                {
                    playerSFX.PlayLanding();
                }

                if (landingDustEffect != null)
{
    landingDustEffect.Play();
}

                waitingForLandingSFX = false;

                return;
            }
        }
    }

    // =====================================================
    // ATTACK MOVEMENT LOCK
    // =====================================================

    public void LockHorizontalMovement(bool locked)
    {
        horizontalMovementLocked = locked;

        if (locked)
        {
            rb.linearVelocity =
                new Vector2(
                    0f,
                    rb.linearVelocity.y
                );
        }
    }

    // =====================================================
    // GRAPPLE INPUT
    // =====================================================

private void HandleGrappleInput()
{
    /*
     * Si le jeu est en pause ou sur l'écran de mort,
     * aucun nouvel input de grappin n'est accepté.
     *
     * PauseMenu et PlayerHealth utilisent tous les deux
     * Time.timeScale = 0f.
     */
    if (Time.timeScale == 0f)
    {
        /*
         * Si le joueur était en train de charger au moment
         * où le jeu a été mis en pause, on annule la charge
         * et son SFX.
         */
        if (isChargingGrapple)
        {
            isChargingGrapple = false;

            if (playerSFX != null)
            {
                playerSFX.StopGrappleCharge();
            }

            if (grapplePreviewLine != null)
            {
                grapplePreviewLine.enabled = false;
            }
        }

        return;
    }

    if (GrapplePressedThisFrame())
    {
        if (
            isHooked ||
            isBeingPulled ||
            isGrapleFlying
        )
        {
            return;
        }

        isChargingGrapple = true;

        if (playerSFX != null)
        {
            playerSFX.StartGrappleCharge();
        }
    }

    if (GrappleReleasedThisFrame())
    {
        if (!isChargingGrapple)
        {
            return;
        }

        isChargingGrapple = false;

        if (playerSFX != null)
        {
            playerSFX.StopGrappleCharge();
        }

        FireGrapple();
    }
}

    // =====================================================
    // GRAPPLE FIRE
    // =====================================================

    private void FireGrapple()
    {

            if (Time.timeScale == 0f)
    {
        return;
    }
    
        if (grapplePreviewLine != null)
        {
            grapplePreviewLine.enabled = false;
        }

        grappleDirection =
            new Vector2(
                facingDirection,
                0f
            );

        currentGrappleEndPoint =
            grappleOrigin.position;

        isGrapleFlying = true;
        isBeingPulled = false;
        isHooked = false;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        if (grappleLine != null)
        {
            grappleLine.enabled = true;

            grappleLine.SetPosition(
                0,
                grappleOrigin.position
            );

            grappleLine.SetPosition(
                1,
                currentGrappleEndPoint
            );
        }

        if (playerSFX != null)
        {
            playerSFX.StartGrappleShot();
        }
    }

    private void MoveGrappleLine()
    {
        Vector2 previousPoint =
            currentGrappleEndPoint;

        Vector2 nextPoint =
            currentGrappleEndPoint +
            grappleDirection *
            grappleLineSpeed *
            Time.fixedDeltaTime;

        float distanceThisFrame =
            Vector2.Distance(
                previousPoint,
                nextPoint
            );

        RaycastHit2D hit =
            Physics2D.Raycast(
                previousPoint,
                grappleDirection,
                distanceThisFrame,
                grappleCollisionLayer
            );

        if (hit.collider != null)
        {
            currentGrappleEndPoint =
                hit.point;

            if (
                hit.collider.CompareTag(
                    grippableTag
                )
            )
            {
                grappleTargetPoint =
                    hit.point;

                if (playerSFX != null)
                {
                    playerSFX.StopGrappleShot();
                    playerSFX.PlayGrappleValidHit();
                }

                StartPullingPlayer();
            }
            else
            {
                if (playerSFX != null)
                {
                    playerSFX.StopGrappleShot();
                    playerSFX.PlayGrappleInvalidHit();
                }

                CancelGrapple();
            }

            return;
        }

        currentGrappleEndPoint =
            nextPoint;

        float distanceFromOrigin =
            Vector2.Distance(
                grappleOrigin.position,
                currentGrappleEndPoint
            );

        if (
            distanceFromOrigin >=
            grappleMaxDistance
        )
        {
            CancelGrapple();
        }
    }

    // =====================================================
    // GRAPPLE PULL
    // =====================================================

    private void StartPullingPlayer()
    {
        isGrapleFlying = false;
        isBeingPulled = true;
        isHooked = false;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        if (playerSFX != null)
        {
            playerSFX.StartGrappleMovement();
        }
    }

    private void PullPlayerToGrapplePoint()
    {
        Vector2 currentPosition =
            rb.position;

        Vector2 newPosition =
            Vector2.MoveTowards(
                currentPosition,
                grappleTargetPoint,
                grapplePullSpeed *
                Time.fixedDeltaTime
            );

        rb.MovePosition(newPosition);

        float distanceToTarget =
            Vector2.Distance(
                newPosition,
                grappleTargetPoint
            );

        if (
            distanceToTarget <=
            grappleArrivalDistance
        )
        {
            rb.position =
                grappleTargetPoint;

            rb.linearVelocity =
                Vector2.zero;

            rb.gravityScale = 0f;

            isBeingPulled = false;
            isGrapleFlying = false;
            isHooked = true;

            if (grappleLine != null)
            {
                grappleLine.enabled = false;
            }

            if (playerSFX != null)
            {
                playerSFX.StopGrappleMovement();
            }
        }
    }

    // =====================================================
    // GRAPPLE EXIT
    // =====================================================

    private void ForceExitGrappleAndJump()
    {
        isHooked = false;
        isBeingPulled = false;
        isGrapleFlying = false;
        isChargingGrapple = false;

        coyoteTimeCounter = 0f;

        groundCheckDisableCounter =
            groundCheckDisableAfterJump;

        rb.gravityScale =
            gravityScale;

        rb.linearVelocity =
            Vector2.zero;

        if (grappleLine != null)
        {
            grappleLine.enabled = false;
        }

        rb.linearVelocity =
            new Vector2(
                rb.linearVelocity.x,
                jumpVelocity
            );

        if (grapplePreviewLine != null)
        {
            grapplePreviewLine.enabled = false;
        }

        if (playerSFX != null)
        {
            playerSFX.StopAllGrappleSounds();
            playerSFX.PlayJump();
        }

        /*
         * Le saut après grappin déclenche
         * également un SFX lorsqu'il retombe.
         */
        waitingForLandingSFX = true;
    }

    private void CancelGrapple()
    {
        isGrapleFlying = false;
        isBeingPulled = false;
        isHooked = false;
        isChargingGrapple = false;

        rb.gravityScale =
            gravityScale;

        if (grappleLine != null)
        {
            grappleLine.enabled = false;
        }

        if (grapplePreviewLine != null)
        {
            grapplePreviewLine.enabled = false;
        }

        if (playerSFX != null)
        {
            playerSFX.StopAllGrappleSounds();
        }
    }

    // =====================================================
    // GRAPPLE VISUAL
    // =====================================================

    private void UpdateGrappleLineVisual()
    {
        if (
            grappleLine == null ||
            !grappleLine.enabled ||
            grappleOrigin == null
        )
        {
            return;
        }

        Vector3 startPoint =
            grappleOrigin.position;

        Vector3 endPoint;

        if (
            isBeingPulled ||
            isHooked
        )
        {
            endPoint =
                grappleTargetPoint;
        }
        else
        {
            endPoint =
                currentGrappleEndPoint;
        }

        grappleLine.SetPosition(
            0,
            startPoint
        );

        grappleLine.SetPosition(
            1,
            endPoint
        );
    }

    private void UpdateGrapplePreviewLine()
    {
        if (
            grapplePreviewLine == null ||
            grappleOrigin == null
        )
        {
            return;
        }

        if (
            !isChargingGrapple ||
            isGrapleFlying ||
            isBeingPulled ||
            isHooked
        )
        {
            grapplePreviewLine.enabled = false;
            return;
        }

        grapplePreviewLine.enabled = true;

        grapplePreviewLine.startColor =
            grapplePreviewColor;

        grapplePreviewLine.endColor =
            grapplePreviewColor;

        grapplePreviewLine.startWidth =
            grapplePreviewWidth;

        grapplePreviewLine.endWidth =
            grapplePreviewWidth;

        Vector3 startPoint =
            grappleOrigin.position;

        Vector3 direction =
            new Vector3(
                facingDirection,
                0f,
                0f
            );

        Vector3 endPoint =
            startPoint +
            direction *
            grapplePreviewLength;

        grapplePreviewLine.SetPosition(
            0,
            startPoint
        );

        grapplePreviewLine.SetPosition(
            1,
            endPoint
        );
    }

    // =====================================================
    // MOVEMENT
    // =====================================================

    private void Move()
    {
        if (horizontalMovementLocked)
        {
            rb.linearVelocity =
                new Vector2(
                    0f,
                    rb.linearVelocity.y
                );

            return;
        }

        float targetSpeed;

        if (isGrounded)
        {
            targetSpeed =
                moveInput *
                groundMoveSpeed;
        }
        else
        {
            targetSpeed =
                moveInput *
                airMoveSpeed *
                airControl;
        }

        if (isChargingGrapple)
        {
            targetSpeed *=
                chargingMoveMultiplier;
        }

        rb.linearVelocity =
            new Vector2(
                targetSpeed,
                rb.linearVelocity.y
            );
    }

    // =====================================================
    // GRAVITY
    // =====================================================

    private void ApplyBetterFall()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.gravityScale =
                gravityScale *
                fallMultiplier;
        }
        else
        {
            rb.gravityScale =
                gravityScale;
        }
    }

    // =====================================================
    // GROUND
    // =====================================================

    private void CheckGround()
    {
        if (
            groundCheckDisableCounter > 0f
        )
        {
            isGrounded = false;
            return;
        }

        isGrounded =
            Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );
    }

    private void UpdateCoyoteTime()
    {
        if (
            groundCheckDisableCounter > 0f
        )
        {
            groundCheckDisableCounter -=
                Time.deltaTime;

            coyoteTimeCounter = 0f;

            return;
        }

        if (isGrounded)
        {
            coyoteTimeCounter =
                coyoteTime;
        }
        else
        {
            coyoteTimeCounter -=
                Time.deltaTime;
        }
    }

    private void CalculateJumpValues()
    {
        float gravity =
            (2f * jumpHeight) /
            Mathf.Pow(
                timeToJumpApex,
                2f
            );

        jumpVelocity =
            gravity *
            timeToJumpApex;

        gravityScale =
            gravity /
            Mathf.Abs(
                Physics2D.gravity.y
            );

        rb.gravityScale =
            gravityScale;
    }

    // =====================================================
    // NEW INPUT SYSTEM
    // =====================================================

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

        return GameInput.Instance.JumpPressedThisFrame;
    }

    private bool JumpReleasedThisFrame()
    {
        if (GameInput.Instance == null)
        {
            return false;
        }

        return GameInput.Instance.JumpReleasedThisFrame;
    }

    private bool GrapplePressedThisFrame()
    {
        if (GameInput.Instance == null)
        {
            return false;
        }

        return GameInput.Instance.GrapplePressedThisFrame;
    }

    private bool GrappleReleasedThisFrame()
    {
        if (GameInput.Instance == null)
        {
            return false;
        }

        return GameInput.Instance.GrappleReleasedThisFrame;
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private void OnValidate()
    {
        if (timeToJumpApex <= 0.05f)
        {
            timeToJumpApex = 0.05f;
        }

        if (jumpHeight <= 0.1f)
        {
            jumpHeight = 0.1f;
        }

        if (fallMultiplier < 1f)
        {
            fallMultiplier = 1f;
        }

        if (shortJumpMultiplier < 0.1f)
        {
            shortJumpMultiplier = 0.1f;
        }

        if (shortJumpMultiplier > 1f)
        {
            shortJumpMultiplier = 1f;
        }

        if (coyoteTime < 0f)
        {
            coyoteTime = 0f;
        }

        if (
            groundCheckDisableAfterJump < 0f
        )
        {
            groundCheckDisableAfterJump = 0f;
        }

        if (chargingMoveMultiplier < 0f)
        {
            chargingMoveMultiplier = 0f;
        }

        if (chargingMoveMultiplier > 1f)
        {
            chargingMoveMultiplier = 1f;
        }

        if (grappleLineSpeed <= 0f)
        {
            grappleLineSpeed = 1f;
        }

        if (grapplePullSpeed <= 0f)
        {
            grapplePullSpeed = 1f;
        }

        if (grappleMaxDistance <= 0f)
        {
            grappleMaxDistance = 1f;
        }

        if (
            grappleArrivalDistance <= 0.01f
        )
        {
            grappleArrivalDistance = 0.01f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }

        if (grappleOrigin != null)
        {
            Gizmos.DrawWireSphere(
                grappleOrigin.position,
                0.1f
            );
        }
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }
}
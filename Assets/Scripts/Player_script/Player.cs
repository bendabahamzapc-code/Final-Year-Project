using UnityEngine;

public class Player : MonoBehaviour
{
    // =====================================================
    // MOVEMENT
    // =====================================================

    [Header("Movement")]
    [SerializeField] private float groundMoveSpeed = 5f;
    [SerializeField] private float airMoveSpeed = 3f;

    [SerializeField]
    [Range(0f, 1f)]
    private float airControl = 0.7f;


    // =====================================================
    // JUMP
    // =====================================================

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float timeToJumpApex = 0.4f;
    [SerializeField] private float fallMultiplier = 2f;


    [Header("Variable Jump")]
    [SerializeField]
    [Range(0.1f, 1f)]
    private float shortJumpMultiplier = 0.5f;


    [Header("Jump Forgiveness")]
    [SerializeField] private float coyoteTime = 0.12f;

    [SerializeField]
    private float groundCheckDisableAfterJump = 0.1f;


    // =====================================================
    // GROUND CHECK
    // =====================================================

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;


    // =====================================================
    // GRAPPLE
    // =====================================================

    [Header("Grapple Settings")]
    [SerializeField] private Transform grappleOrigin;

    [SerializeField]
    [Range(0f, 1f)]
    private float chargingMoveMultiplier = 0.25f;

    [SerializeField] private float grappleLineSpeed = 25f;
    [SerializeField] private float grapplePullSpeed = 12f;
    [SerializeField] private float grappleMaxDistance = 12f;
    [SerializeField] private float grappleArrivalDistance = 0.15f;

    [SerializeField] private LayerMask grappleCollisionLayer;

    [SerializeField]
    private string grippableTag = "aggripable";


    // =====================================================
    // GRAPPLE VISUAL
    // =====================================================

    [Header("Grapple Visual")]
    [SerializeField] private LineRenderer grappleLine;


    [Header("Grapple Preview")]
    [SerializeField] private LineRenderer grapplePreviewLine;

    [SerializeField]
    private Color grapplePreviewColor =
        new Color(0.5f, 0.5f, 0.5f, 0.35f);

    [SerializeField] private float grapplePreviewWidth = 0.04f;
    [SerializeField] private float grapplePreviewLength = 12f;


    // =====================================================
    // REFERENCES
    // =====================================================

    private Rigidbody2D rb;

    private PlayerHealth playerHealth;
    private PlayerSFXController playerSFX;
    private LandingDustEffect landingDustEffect;


    // =====================================================
    // MOVEMENT VARIABLES
    // =====================================================

    private bool isGrounded;

    private float moveInput;

    private float jumpVelocity;
    private float gravityScale;

    private float coyoteTimeCounter;
    private float groundCheckDisableCounter;

    private int facingDirection = 1;

    private bool horizontalMovementLocked;


    // =====================================================
    // LANDING
    // =====================================================

    private bool waitingForLandingSFX;


    // =====================================================
    // GRAPPLE STATE
    // =====================================================

    private bool isChargingGrapple;
    private bool isGrapleFlying;
    private bool isBeingPulled;
    private bool isHooked;

    private Vector2 grappleDirection;

    private Vector2 currentGrappleEndPoint;

    /*
     * Position fixe utilisée comme sécurité / fallback.
     */
    private Vector2 grappleTargetPoint;


    // =====================================================
    // MOVING GRAPPLE TARGET
    // =====================================================

    /*
     * Objet réellement attrapé par le grappin.
     *
     * Si cet objet bouge, le point d'accroche
     * bougera avec lui.
     */
    private Transform grappleTargetTransform;

    /*
     * Position du point d'impact relativement
     * à l'objet attrapé.
     */
    private Vector3 grappleTargetLocalPoint;

    /*
     * Position du Player relativement à l'objet
     * une fois complètement accroché.
     */
    private Vector3 hookedLocalPosition;


    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        playerHealth =
            GetComponent<PlayerHealth>();

        playerSFX =
            GetComponent<PlayerSFXController>();

        landingDustEffect =
            GetComponent<LandingDustEffect>();

        if (grappleLine == null)
        {
            grappleLine =
                GetComponent<LineRenderer>();
        }
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

            grapplePreviewLine.startColor =
                grapplePreviewColor;

            grapplePreviewLine.endColor =
                grapplePreviewColor;

            grapplePreviewLine.startWidth =
                grapplePreviewWidth;

            grapplePreviewLine.endWidth =
                grapplePreviewWidth;
        }
    }


    private void Update()
    {
        moveInput =
            ReadHorizontalInput();

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


    private void FixedUpdate()
    {
        if (
            playerHealth != null &&
            playerHealth.IsKnockedBack
        )
        {
            return;
        }

        // =================================================
        // ACCROCHÉ
        // =================================================

        if (isHooked)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;

            /*
             * NOUVEAU :
             *
             * si l'objet attrapé bouge,
             * le joueur bouge avec lui.
             */
            FollowMovingGrappleTarget();

            return;
        }

        // =================================================
        // GRAPPIN EN VOL
        // =================================================

        if (isGrapleFlying)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;

            MoveGrappleLine();

            return;
        }

        // =================================================
        // JOUEUR TIRÉ
        // =================================================

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

        waitingForLandingSFX = true;

        if (playerSFX != null)
        {
            playerSFX.PlayJump();
        }
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
    // LANDING / FALL
    // =====================================================

    private void CheckForFalling()
    {
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


    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        if (!waitingForLandingSFX)
        {
            return;
        }

        int collisionLayer =
            1 << collision.gameObject.layer;

        if (
            (groundLayer.value & collisionLayer)
            == 0
        )
        {
            return;
        }

        foreach (
            ContactPoint2D contact
            in collision.contacts
        )
        {
            /*
             * Le joueur doit toucher le sol
             * par dessous ses pieds.
             */
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
    // HORIZONTAL MOVEMENT LOCK
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
         * Empêche de charger ou tirer le grappin
         * pendant Pause / Game Over.
         */
        if (Time.timeScale == 0f)
        {
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
    // FIRE GRAPPLE
    // =====================================================

    private void FireGrapple()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        /*
         * On oublie l'ancien objet attrapé.
         */
        grappleTargetTransform = null;

        if (grapplePreviewLine != null)
        {
            grapplePreviewLine.enabled = false;
        }

        /*
         * Le grappin part actuellement uniquement
         * à gauche ou à droite.
         */
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

        if (playerSFX != null)
        {
            playerSFX.StartGrappleShot();
        }

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
    }


    // =====================================================
    // GRAPPLE FLYING
    // =====================================================

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

            // =============================================
            // OBJET AGRIPPABLE
            // =============================================

            if (
                hit.collider.CompareTag(
                    grippableTag
                )
            )
            {
                /*
                 * Position d'origine dans le monde.
                 */
                grappleTargetPoint =
                    hit.point;

                /*
                 * NOUVEAU :
                 *
                 * On mémorise quel objet a été touché.
                 */
                grappleTargetTransform =
                    hit.collider.transform;

                /*
                 * On transforme le point d'impact
                 * en coordonnées locales de l'objet.
                 *
                 * Ainsi, si l'objet bouge,
                 * ce point bouge avec lui.
                 */
                grappleTargetLocalPoint =
                    grappleTargetTransform
                    .InverseTransformPoint(
                        hit.point
                    );

                if (playerSFX != null)
                {
                    playerSFX.StopGrappleShot();
                    playerSFX.PlayGrappleValidHit();
                }

                StartPullingPlayer();
            }

            // =============================================
            // OBJET NON AGRIPPABLE
            // =============================================

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
    // CURRENT GRAPPLE TARGET
    // =====================================================

    private Vector2 GetCurrentGrappleTargetPosition()
    {
        /*
         * Si l'objet existe encore,
         * on récupère le point local converti
         * vers sa nouvelle position dans le monde.
         */
        if (grappleTargetTransform != null)
        {
            return
                grappleTargetTransform.TransformPoint(
                    grappleTargetLocalPoint
                );
        }

        /*
         * Sécurité pour un point statique.
         */
        return grappleTargetPoint;
    }


    // =====================================================
    // START PULL
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


    // =====================================================
    // PULL PLAYER
    // =====================================================

    private void PullPlayerToGrapplePoint()
    {
        Vector2 targetPosition =
            GetCurrentGrappleTargetPosition();

        Vector2 currentPosition =
            rb.position;

        Vector2 newPosition =
            Vector2.MoveTowards(
                currentPosition,
                targetPosition,
                grapplePullSpeed *
                Time.fixedDeltaTime
            );

        rb.MovePosition(newPosition);

        float distanceToTarget =
            Vector2.Distance(
                newPosition,
                targetPosition
            );

        if (
            distanceToTarget <=
            grappleArrivalDistance
        )
        {
            /*
             * On récupère encore une fois
             * la position car la plateforme
             * a pu bouger pendant le trajet.
             */
            targetPosition =
                GetCurrentGrappleTargetPosition();

            rb.position =
                targetPosition;

            rb.linearVelocity =
                Vector2.zero;

            rb.gravityScale = 0f;

            isBeingPulled = false;
            isGrapleFlying = false;
            isHooked = true;

            if (playerSFX != null)
            {
                playerSFX.StopGrappleMovement();
            }

            /*
             * NOUVEAU :
             *
             * Une fois accroché,
             * on mémorise la position du joueur
             * relativement à l'objet.
             */
            if (grappleTargetTransform != null)
            {
                hookedLocalPosition =
                    grappleTargetTransform
                    .InverseTransformPoint(
                        rb.position
                    );
            }

            if (grappleLine != null)
            {
                grappleLine.enabled = false;
            }
        }
    }


    // =====================================================
    // FOLLOW MOVING GRAPPLE TARGET
    // =====================================================

    private void FollowMovingGrappleTarget()
    {
        if (!isHooked)
        {
            return;
        }

        /*
         * Objet détruit ou inexistant :
         * on reste simplement accroché
         * au dernier point connu.
         */
        if (grappleTargetTransform == null)
        {
            return;
        }

        /*
         * Convertit la position locale mémorisée
         * vers la position actuelle dans le monde.
         *
         * Donc si la plateforme se déplace,
         * le joueur se déplace avec elle.
         */
        Vector2 newPlayerPosition =
            grappleTargetTransform.TransformPoint(
                hookedLocalPosition
            );

        rb.MovePosition(
            newPlayerPosition
        );

        rb.linearVelocity =
            Vector2.zero;
    }


    // =====================================================
    // EXIT GRAPPLE + JUMP
    // =====================================================

    private void ForceExitGrappleAndJump()
    {
        isHooked = false;
        isBeingPulled = false;
        isGrapleFlying = false;
        isChargingGrapple = false;

        /*
         * Le joueur n'est plus attaché
         * à l'objet mobile.
         */
        grappleTargetTransform = null;

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

        if (grapplePreviewLine != null)
        {
            grapplePreviewLine.enabled = false;
        }

        if (playerSFX != null)
        {
            playerSFX.StopAllGrappleSounds();
            playerSFX.PlayJump();
        }

        waitingForLandingSFX = true;

        rb.linearVelocity =
            new Vector2(
                rb.linearVelocity.x,
                jumpVelocity
            );
    }


    // =====================================================
    // CANCEL GRAPPLE
    // =====================================================

    private void CancelGrapple()
    {
        isGrapleFlying = false;
        isBeingPulled = false;
        isHooked = false;
        isChargingGrapple = false;

        /*
         * On enlève toute référence
         * vers l'ancien objet attrapé.
         */
        grappleTargetTransform = null;

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
    // GRAPPLE LINE VISUAL
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
            /*
             * NOUVEAU :
             * la ligne suit elle aussi
             * l'objet mobile.
             */
            endPoint =
                GetCurrentGrappleTargetPosition();
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


    // =====================================================
    // GRAPPLE PREVIEW
    // =====================================================

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
    // NORMAL MOVEMENT
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
    // FALL
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

        if (groundCheck == null)
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


    // =====================================================
    // JUMP VALUES
    // =====================================================

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
    // INPUT SYSTEM
    // =====================================================

    private float ReadHorizontalInput()
    {
        if (GameInput.Instance == null)
        {
            return 0f;
        }

        return
            GameInput.Instance.MoveHorizontal;
    }


    private bool JumpPressedThisFrame()
    {
        if (GameInput.Instance == null)
        {
            return false;
        }

        return
            GameInput.Instance.JumpPressedThisFrame;
    }


    private bool JumpReleasedThisFrame()
    {
        if (GameInput.Instance == null)
        {
            return false;
        }

        return
            GameInput.Instance.JumpReleasedThisFrame;
    }


    private bool GrapplePressedThisFrame()
    {
        if (GameInput.Instance == null)
        {
            return false;
        }

        return
            GameInput.Instance.GrapplePressedThisFrame;
    }


    private bool GrappleReleasedThisFrame()
    {
        if (GameInput.Instance == null)
        {
            return false;
        }

        return
            GameInput.Instance.GrappleReleasedThisFrame;
    }


    // =====================================================
    // PUBLIC
    // =====================================================

    public bool IsGrounded()
    {
        return isGrounded;
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

        shortJumpMultiplier =
            Mathf.Clamp(
                shortJumpMultiplier,
                0.1f,
                1f
            );

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

        chargingMoveMultiplier =
            Mathf.Clamp01(
                chargingMoveMultiplier
            );

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


    // =====================================================
    // GIZMOS
    // =====================================================

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
}
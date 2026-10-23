using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MiniBossPatternController : MonoBehaviour
{
    public enum BossState
    {
        SeekingDistance,
        PreparingCharge,
        Charging,
        Recoil,
        Stunned,
        Recovery
    }

    public enum ChargeStopMode
    {
        LayerMask,
        SpecificColliders,
        LayerMaskOrSpecificColliders
    }

    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private MiniBossVisualController visualController;

    [Header("Preferred Distance")]
    [Tooltip("Distance minimale souhaitée par rapport au joueur.")]
    [SerializeField] private float minimumPreferredDistance = 4f;

    [Tooltip("Distance maximale souhaitée par rapport au joueur.")]
    [SerializeField] private float maximumPreferredDistance = 7f;

    [Tooltip("Vitesse utilisée pour se rapprocher ou s’éloigner.")]
    [SerializeField] private float positioningSpeed = 2f;

    [Header("Passive Movement")]
    [Tooltip("Intensité du petit mouvement gauche-droite.")]
    [SerializeField] private float passiveMovementSpeed = 0.4f;

    [Tooltip("Temps entre chaque changement de direction passive.")]
    [SerializeField] private float passiveDirectionChangeInterval = 0.8f;

    [Header("Charge Preparation")]
    [Tooltip("Durée pendant laquelle le boss reste immobile avant la charge.")]
    [SerializeField] private float chargePreparationDuration = 1.5f;

    [Header("Charge")]
    [SerializeField] private float chargeSpeed = 12f;

    [Tooltip("Durée maximale de la charge si aucun obstacle n’est touché.")]
    [SerializeField] private float maximumChargeDuration = 3f;

    [Header("Objects Stopping Charge")]
    [SerializeField] private ChargeStopMode chargeStopMode =
        ChargeStopMode.LayerMask;

    [Tooltip("Layers qui peuvent arrêter la charge.")]
    [SerializeField] private LayerMask chargeStopLayers;

    [Tooltip("Colliders précis qui peuvent arrêter la charge.")]
    [SerializeField] private Collider2D[] specificChargeStopColliders;

    [Header("Impact Recoil")]
    [Tooltip("Vitesse du recul après avoir percuté un obstacle.")]
    [SerializeField] private float impactRecoilSpeed = 3f;

    [Tooltip("Durée du recul après l’impact.")]
    [SerializeField] private float impactRecoilDuration = 0.2f;

    [Header("Stun")]
    [SerializeField] private float stunDuration = 2f;

    [Tooltip("Point où le symbole de stun apparaît.")]
    [SerializeField] private Transform stunSpritePoint;

    [Tooltip("Sprite affiché au-dessus du boss pendant le stun.")]
    [SerializeField] private Sprite stunSprite;

    [SerializeField] private Vector2 stunSpriteScale = Vector2.one;
    [SerializeField] private int stunSpriteSortingOrder = 20;

    [Header("Recovery")]
    [Tooltip("Temps d’attente après le stun.")]
    [SerializeField] private float recoveryDuration = 1f;

    [Header("Visual Direction")]
    [SerializeField] private bool flipVisualWithDirection = true;

    [Tooltip("Active si le sprite original regarde vers la droite.")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    [Header("Debug")]
    [SerializeField] private bool drawPreferredDistances = true;
    [SerializeField] private BossState currentState;

    private float stateTimer;
    private float chargeTimer;
    private float passiveDirectionTimer;

    private int passiveDirection = 1;
    private int facingDirection = 1;
    private int chargeDirection = 1;

    private GameObject stunSpriteObject;
    private SpriteRenderer stunSpriteRenderer;

    public BossState CurrentState => currentState;
    public bool IsCharging => currentState == BossState.Charging;
    public bool IsStunned => currentState == BossState.Stunned;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        if (visualController == null)
        {
            visualController =
                GetComponentInChildren<MiniBossVisualController>();
        }

        CreateStunSprite();
    }

    private void Start()
    {
        FindPlayerIfNecessary();

        passiveDirection =
            Random.value < 0.5f ? -1 : 1;

        passiveDirectionTimer =
            passiveDirectionChangeInterval;

        EnterSeekingState();
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            FindPlayerIfNecessary();
            StopHorizontalMovement();
            return;
        }

        switch (currentState)
        {
            case BossState.SeekingDistance:
                HandleSeekingDistance();
                break;

            case BossState.PreparingCharge:
                HandleChargePreparation();
                break;

            case BossState.Charging:
                HandleCharge();
                break;

            case BossState.Recoil:
                HandleRecoil();
                break;

            case BossState.Stunned:
                HandleStun();
                break;

            case BossState.Recovery:
                HandleRecovery();
                break;
        }
    }

    private void HandleSeekingDistance()
    {
        UpdatePassiveDirection();

        float differenceToPlayer =
            player.position.x - rb.position.x;

        float distanceToPlayer =
            Mathf.Abs(differenceToPlayer);

        int directionToPlayer = GetDirectionToPlayer();

        bool isAtPreferredDistance =
            distanceToPlayer >= minimumPreferredDistance &&
            distanceToPlayer <= maximumPreferredDistance;

        if (isAtPreferredDistance)
        {
            BeginChargePreparation();
            return;
        }

        int positioningDirection;

        if (distanceToPlayer > maximumPreferredDistance)
        {
            positioningDirection = directionToPlayer;
        }
        else
        {
            positioningDirection = -directionToPlayer;
        }

        float mainMovement =
            positioningDirection * positioningSpeed;

        float passiveMovement =
            passiveDirection * passiveMovementSpeed;

        float finalMovement =
            mainMovement + passiveMovement;

        /*
         * Empêche le mouvement passif de faire partir le boss
         * dans la mauvaise direction.
         */
        if (
            Mathf.Abs(finalMovement) < 0.05f ||
            Mathf.Sign(finalMovement) != positioningDirection
        )
        {
            finalMovement = mainMovement;
        }

        rb.linearVelocity = new Vector2(
            finalMovement,
            rb.linearVelocity.y
        );

        UpdateFacingDirection(positioningDirection);
    }

    private void BeginChargePreparation()
    {
        ChangeState(BossState.PreparingCharge);

        StopHorizontalMovement();

        stateTimer = chargePreparationDuration;

        /*
         * La direction de la charge est choisie au début
         * de la préparation et reste ensuite verrouillée.
         */
        chargeDirection = GetDirectionToPlayer();

        UpdateFacingDirection(chargeDirection);
    }

    private void HandleChargePreparation()
    {
        StopHorizontalMovement();

        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f)
        {
            BeginCharge();
        }
    }

    private void BeginCharge()
    {
        ChangeState(BossState.Charging);

        chargeTimer = 0f;

        rb.linearVelocity = new Vector2(
            chargeDirection * chargeSpeed,
            rb.linearVelocity.y
        );
    }

    private void HandleCharge()
    {
        rb.linearVelocity = new Vector2(
            chargeDirection * chargeSpeed,
            rb.linearVelocity.y
        );

        chargeTimer += Time.fixedDeltaTime;

        if (chargeTimer >= maximumChargeDuration)
        {
            BeginRecovery();
        }
    }

    private void BeginImpactRecoil()
    {
        if (currentState != BossState.Charging)
        {
            return;
        }

        ChangeState(BossState.Recoil);

        stateTimer = impactRecoilDuration;

        rb.linearVelocity = new Vector2(
            -chargeDirection * impactRecoilSpeed,
            rb.linearVelocity.y
        );
    }

    private void HandleRecoil()
    {
        rb.linearVelocity = new Vector2(
            -chargeDirection * impactRecoilSpeed,
            rb.linearVelocity.y
        );

        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f)
        {
            BeginStun();
        }
    }

    private void BeginStun()
    {
        ChangeState(BossState.Stunned);

        StopHorizontalMovement();

        stateTimer = stunDuration;

        SetStunSpriteVisible(true);
    }

    private void HandleStun()
    {
        StopHorizontalMovement();

        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f)
        {
            SetStunSpriteVisible(false);
            BeginRecovery();
        }
    }

    private void BeginRecovery()
    {
        ChangeState(BossState.Recovery);

        StopHorizontalMovement();

        stateTimer = recoveryDuration;

        SetStunSpriteVisible(false);
    }

    private void HandleRecovery()
    {
        StopHorizontalMovement();

        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f)
        {
            EnterSeekingState();
        }
    }

    private void EnterSeekingState()
    {
        ChangeState(BossState.SeekingDistance);

        stateTimer = 0f;
        chargeTimer = 0f;

        passiveDirectionTimer =
            passiveDirectionChangeInterval;

        SetStunSpriteVisible(false);
    }

    private void UpdatePassiveDirection()
    {
        passiveDirectionTimer -= Time.fixedDeltaTime;

        if (passiveDirectionTimer > 0f)
        {
            return;
        }

        passiveDirection *= -1;

        passiveDirectionTimer =
            passiveDirectionChangeInterval;
    }

    private int GetDirectionToPlayer()
    {
        if (player == null)
        {
            return facingDirection;
        }

        float difference =
            player.position.x - rb.position.x;

        if (Mathf.Abs(difference) <= 0.01f)
        {
            return facingDirection;
        }

        return difference > 0f ? 1 : -1;
    }

    private void UpdateFacingDirection(int newDirection)
    {
        if (newDirection == 0)
        {
            return;
        }

        facingDirection = newDirection;

        if (!flipVisualWithDirection)
        {
            return;
        }

        if (visualTransform == null)
        {
            return;
        }

        Vector3 scale =
            visualTransform.localScale;

        float absoluteScaleX =
            Mathf.Abs(scale.x);

        bool shouldFaceRight =
            facingDirection > 0;

        if (spriteFacesRightByDefault)
        {
            scale.x = shouldFaceRight
                ? absoluteScaleX
                : -absoluteScaleX;
        }
        else
        {
            scale.x = shouldFaceRight
                ? -absoluteScaleX
                : absoluteScaleX;
        }

        visualTransform.localScale = scale;
    }

    private void StopHorizontalMovement()
    {
        rb.linearVelocity = new Vector2(
            0f,
            rb.linearVelocity.y
        );
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckChargeCollision(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckChargeCollision(other);
    }

    private void CheckChargeCollision(Collider2D touchedCollider)
    {
        /*
         * L’obstacle ne stun le boss que pendant la charge.
         */
        if (currentState != BossState.Charging)
        {
            return;
        }

        if (!CanStopCharge(touchedCollider))
        {
            return;
        }

        BeginImpactRecoil();
    }

    private bool CanStopCharge(Collider2D touchedCollider)
    {
        if (touchedCollider == null)
        {
            return false;
        }

        bool matchesLayer =
            IsColliderInChargeStopLayer(touchedCollider);

        bool matchesSpecificCollider =
            IsSpecificChargeStopCollider(touchedCollider);

        switch (chargeStopMode)
        {
            case ChargeStopMode.LayerMask:
                return matchesLayer;

            case ChargeStopMode.SpecificColliders:
                return matchesSpecificCollider;

            case ChargeStopMode.LayerMaskOrSpecificColliders:
                return matchesLayer || matchesSpecificCollider;
        }

        return false;
    }

    private bool IsColliderInChargeStopLayer(
        Collider2D touchedCollider
    )
    {
        int colliderLayer =
            touchedCollider.gameObject.layer;

        return
            (chargeStopLayers.value & (1 << colliderLayer))
            != 0;
    }

    private bool IsSpecificChargeStopCollider(
        Collider2D touchedCollider
    )
    {
        if (
            specificChargeStopColliders == null ||
            specificChargeStopColliders.Length == 0
        )
        {
            return false;
        }

        foreach (
            Collider2D stopCollider
            in specificChargeStopColliders
        )
        {
            if (stopCollider == null)
            {
                continue;
            }

            if (touchedCollider == stopCollider)
            {
                return true;
            }

            if (
                touchedCollider.transform.IsChildOf(
                    stopCollider.transform
                )
            )
            {
                return true;
            }
        }

        return false;
    }

    private void CreateStunSprite()
    {
        stunSpriteObject =
            new GameObject("MiniBoss_StunIndicator");

        Transform parent =
            stunSpritePoint != null
                ? stunSpritePoint
                : transform;

        stunSpriteObject.transform.SetParent(parent);

        stunSpriteObject.transform.localPosition =
            Vector3.zero;

        stunSpriteObject.transform.localRotation =
            Quaternion.identity;

        stunSpriteObject.transform.localScale =
            new Vector3(
                stunSpriteScale.x,
                stunSpriteScale.y,
                1f
            );

        stunSpriteRenderer =
            stunSpriteObject.AddComponent<SpriteRenderer>();

        stunSpriteRenderer.sprite = stunSprite;

        stunSpriteRenderer.sortingOrder =
            stunSpriteSortingOrder;

        stunSpriteObject.SetActive(false);
    }

    private void SetStunSpriteVisible(bool visible)
    {
        if (stunSpriteObject == null)
        {
            return;
        }

        if (stunSpriteRenderer != null)
        {
            stunSpriteRenderer.sprite = stunSprite;
        }

        stunSpriteObject.SetActive(
            visible && stunSprite != null
        );
    }

    private void FindPlayerIfNecessary()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject =
            GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void ChangeState(BossState newState)
    {
        currentState = newState;

        if (visualController != null)
        {
            visualController.SetState(newState);
        }
    }

    private void OnValidate()
    {
        minimumPreferredDistance =
            Mathf.Max(0f, minimumPreferredDistance);

        maximumPreferredDistance =
            Mathf.Max(
                minimumPreferredDistance,
                maximumPreferredDistance
            );

        positioningSpeed =
            Mathf.Max(0f, positioningSpeed);

        passiveMovementSpeed =
            Mathf.Max(0f, passiveMovementSpeed);

        passiveDirectionChangeInterval =
            Mathf.Max(
                0.05f,
                passiveDirectionChangeInterval
            );

        chargePreparationDuration =
            Mathf.Max(0f, chargePreparationDuration);

        chargeSpeed =
            Mathf.Max(0f, chargeSpeed);

        maximumChargeDuration =
            Mathf.Max(0.1f, maximumChargeDuration);

        impactRecoilSpeed =
            Mathf.Max(0f, impactRecoilSpeed);

        impactRecoilDuration =
            Mathf.Max(0f, impactRecoilDuration);

        stunDuration =
            Mathf.Max(0f, stunDuration);

        recoveryDuration =
            Mathf.Max(0f, recoveryDuration);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPreferredDistances)
        {
            return;
        }

        Vector3 bossPosition =
            transform.position;

        Vector3 minimumLeft =
            bossPosition +
            Vector3.left * minimumPreferredDistance;

        Vector3 maximumLeft =
            bossPosition +
            Vector3.left * maximumPreferredDistance;

        Vector3 minimumRight =
            bossPosition +
            Vector3.right * minimumPreferredDistance;

        Vector3 maximumRight =
            bossPosition +
            Vector3.right * maximumPreferredDistance;

        Gizmos.DrawLine(
            minimumLeft,
            maximumLeft
        );

        Gizmos.DrawLine(
            minimumRight,
            maximumRight
        );

        Gizmos.DrawWireSphere(
            minimumLeft,
            0.15f
        );

        Gizmos.DrawWireSphere(
            maximumLeft,
            0.15f
        );

        Gizmos.DrawWireSphere(
            minimumRight,
            0.15f
        );

        Gizmos.DrawWireSphere(
            maximumRight,
            0.15f
        );
    }
}
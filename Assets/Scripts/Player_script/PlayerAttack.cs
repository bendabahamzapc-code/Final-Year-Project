using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackDuration = 0.15f;

    [Header("Attack Hitbox")]
    [SerializeField] private GameObject attackHitbox;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float attackDistanceFromOrigin = 0.7f;

    [Header("References")]
    [SerializeField] private Player player;

    private PlayerSFXController playerSFX;

    private float cooldownTimer;
    private float attackTimer;

    private bool isAttacking;

    private int facingDirection = 1;

    private bool lockedMovementForThisAttack;

    private void Start()
    {
        if (player == null)
        {
            player = GetComponent<Player>();
        }

        playerSFX = GetComponent<PlayerSFXController>();

        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);

            PlayerAttackHitbox hitboxScript =
                attackHitbox.GetComponent<PlayerAttackHitbox>();

            if (hitboxScript != null)
            {
                hitboxScript.SetDamage(attackDamage);
            }
        }
    }

    private void Update()
    {
        UpdateFacingDirection();
        UpdateCooldown();
        UpdateAttackDuration();

        if (
            GameInput.Instance != null &&
            GameInput.Instance.AttackPressedThisFrame &&
            cooldownTimer <= 0f &&
            !isAttacking
        )
        {
            StartAttack();
        }
    }

    private void UpdateFacingDirection()
    {
        if (GameInput.Instance == null)
        {
            return;
        }

        float horizontalInput =
            GameInput.Instance.MoveHorizontal;

        if (horizontalInput > 0f)
        {
            facingDirection = 1;
        }
        else if (horizontalInput < 0f)
        {
            facingDirection = -1;
        }
    }

    private void UpdateCooldown()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private void UpdateAttackDuration()
    {
        if (!isAttacking)
        {
            return;
        }

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            EndAttack();
        }
    }

    private void StartAttack()
    {
        if (attackHitbox == null)
        {
            Debug.LogWarning(
                "Attack Hitbox is missing on PlayerAttack."
            );

            return;
        }

        if (attackOrigin == null)
        {
            Debug.LogWarning(
                "Attack Origin is missing on PlayerAttack."
            );

            return;
        }

        lockedMovementForThisAttack = false;

        if (
            player != null &&
            player.IsGrounded()
        )
        {
            player.LockHorizontalMovement(true);
            lockedMovementForThisAttack = true;
        }

        isAttacking = true;

        attackTimer =
            attackDuration;

        cooldownTimer =
            attackCooldown;

        // SFX :
        // On indique qu'une nouvelle attaque commence.
        // Le PlayerSFXController saura ensuite si elle touche
        // un ennemi ou si elle finit dans le vide.
        if (playerSFX != null)
        {
            playerSFX.BeginAttack();
        }

        Vector3 hitboxPosition =
            attackOrigin.position +
            new Vector3(
                facingDirection *
                attackDistanceFromOrigin,
                0f,
                0f
            );

        attackHitbox.transform.position =
            hitboxPosition;

        PlayerAttackHitbox hitboxScript =
            attackHitbox.GetComponent<PlayerAttackHitbox>();

        if (hitboxScript != null)
        {
            hitboxScript.SetDamage(
                attackDamage
            );

            hitboxScript.ResetAlreadyHitObjects();
        }

        attackHitbox.SetActive(true);
    }

    private void EndAttack()
    {
        isAttacking = false;

        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);
        }

        // SFX :
        // Si aucun ennemi n'a été enregistré comme touché,
        // le PlayerSFXController jouera ici un son de coup dans le vide.
        if (playerSFX != null)
        {
            playerSFX.EndAttack();
        }

        if (
            player != null &&
            lockedMovementForThisAttack
        )
        {
            player.LockHorizontalMovement(false);
        }

        lockedMovementForThisAttack = false;
    }

    private void OnValidate()
    {
        if (attackDamage < 0)
        {
            attackDamage = 0;
        }

        if (attackCooldown < 0f)
        {
            attackCooldown = 0f;
        }

        if (attackDuration <= 0.01f)
        {
            attackDuration = 0.01f;
        }

        if (attackDistanceFromOrigin < 0f)
        {
            attackDistanceFromOrigin = 0f;
        }
    }
}
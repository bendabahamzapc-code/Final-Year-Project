using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Damage Settings")]
    [SerializeField] private float invincibilityTimeAfterHit = 1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 8f;
    [SerializeField] private float knockbackForceY = 4f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Invincibility Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color invincibleColor =
        new Color(1f, 1f, 1f, 0.35f);

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverScreen;

    [Header("Invincibility Collision")]
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private string enemyTag = "enemi";

    private readonly List<Collider2D> ignoredEnemyColliders =
        new List<Collider2D>();

    private int currentHealth;

    private bool isDead;
    private bool isInvincible;

    private float invincibilityTimer;

    private bool isKnockedBack;
    private float knockbackTimer;

    private float blinkTimer;
    private bool blinkState;

    private Rigidbody2D rb;
    private PlayerSFXController playerSFX;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsKnockedBack => isKnockedBack;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        playerSFX =
            GetComponent<PlayerSFXController>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (playerCollider == null)
        {
            playerCollider =
                GetComponent<Collider2D>();
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;

        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                normalColor;
        }

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void Update()
    {
        UpdateInvincibilityTimer();
        UpdateKnockbackTimer();
    }

    private void UpdateInvincibilityTimer()
    {
        if (!isInvincible)
        {
            return;
        }

        invincibilityTimer -=
            Time.deltaTime;

        UpdateBlinkVisual();

        if (invincibilityTimer <= 0f)
        {
            EndInvincibility();
        }
    }

    private void UpdateBlinkVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        blinkTimer -= Time.deltaTime;

        if (blinkTimer <= 0f)
        {
            blinkState = !blinkState;
            blinkTimer = blinkInterval;

            spriteRenderer.color =
                blinkState
                    ? invincibleColor
                    : normalColor;
        }
    }

    private void EndInvincibility()
    {
        isInvincible = false;

        blinkTimer = 0f;
        blinkState = false;

        SetEnemyCollisionIgnored(false);

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                normalColor;
        }
    }

    private void UpdateKnockbackTimer()
    {
        if (!isKnockedBack)
        {
            return;
        }

        knockbackTimer -= Time.deltaTime;

        if (knockbackTimer <= 0f)
        {
            isKnockedBack = false;
        }
    }

    // =====================================================
    // DAMAGE WITHOUT KNOCKBACK
    // =====================================================

    public void TakeDamage(int damageAmount)
    {
        if (isDead)
        {
            return;
        }

        if (isInvincible)
        {
            return;
        }

        if (damageAmount <= 0)
        {
            return;
        }

        currentHealth -= damageAmount;

        // SFX : le joueur a réellement subi des dégâts.
        if (playerSFX != null)
        {
            playerSFX.PlayDamage();
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;

            OnHealthChanged?.Invoke(
                currentHealth,
                maxHealth
            );

            Die();
            return;
        }

        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        StartInvincibility();
    }

    // =====================================================
    // DAMAGE WITH KNOCKBACK
    // =====================================================

    public void TakeDamage(
        int damageAmount,
        Vector2 damageSourcePosition
    )
    {
        if (isDead)
        {
            return;
        }

        if (isInvincible)
        {
            return;
        }

        if (damageAmount <= 0)
        {
            return;
        }

        currentHealth -= damageAmount;

        // SFX : le joueur a réellement subi des dégâts.
        if (playerSFX != null)
        {
            playerSFX.PlayDamage();
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;

            OnHealthChanged?.Invoke(
                currentHealth,
                maxHealth
            );

            Die();
            return;
        }

        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        ApplyKnockback(
            damageSourcePosition
        );

        StartInvincibility();
    }

    // =====================================================
    // KNOCKBACK
    // =====================================================

    private void ApplyKnockback(
        Vector2 damageSourcePosition
    )
    {
        if (rb == null)
        {
            return;
        }

        float direction =
            transform.position.x >=
            damageSourcePosition.x
                ? 1f
                : -1f;

        rb.linearVelocity =
            Vector2.zero;

        rb.AddForce(
            new Vector2(
                direction * knockbackForceX,
                knockbackForceY
            ),
            ForceMode2D.Impulse
        );

        isKnockedBack = true;
        knockbackTimer =
            knockbackDuration;
    }

    // =====================================================
    // INVINCIBILITY
    // =====================================================

    private void StartInvincibility()
    {
        isInvincible = true;

        invincibilityTimer =
            invincibilityTimeAfterHit;

        blinkTimer = 0f;
        blinkState = false;

        SetEnemyCollisionIgnored(true);
    }

    private void SetEnemyCollisionIgnored(
        bool ignore
    )
    {
        if (playerCollider == null)
        {
            return;
        }

        if (ignore)
        {
            ignoredEnemyColliders.Clear();

            GameObject[] enemies =
                GameObject.FindGameObjectsWithTag(
                    enemyTag
                );

            foreach (GameObject enemy in enemies)
            {
                Collider2D[] enemyColliders =
                    enemy.GetComponentsInChildren<Collider2D>();

                foreach (
                    Collider2D enemyCollider
                    in enemyColliders
                )
                {
                    if (enemyCollider == null)
                    {
                        continue;
                    }

                    Physics2D.IgnoreCollision(
                        playerCollider,
                        enemyCollider,
                        true
                    );

                    ignoredEnemyColliders.Add(
                        enemyCollider
                    );
                }
            }
        }
        else
        {
            foreach (
                Collider2D enemyCollider
                in ignoredEnemyColliders
            )
            {
                if (enemyCollider == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(
                    playerCollider,
                    enemyCollider,
                    false
                );
            }

            ignoredEnemyColliders.Clear();
        }
    }

    private void OnDestroy()
    {
        SetEnemyCollisionIgnored(false);
    }

    // =====================================================
    // DEATH
    // =====================================================

    private void Die()
    {
        isDead = true;

        EndInvincibility();

        Time.timeScale = 0f;

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;

        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.name
        );
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private void OnValidate()
    {
        if (maxHealth < 1)
        {
            maxHealth = 1;
        }

        if (
            invincibilityTimeAfterHit < 0f
        )
        {
            invincibilityTimeAfterHit = 0f;
        }

        if (knockbackForceX < 0f)
        {
            knockbackForceX = 0f;
        }

        if (knockbackForceY < 0f)
        {
            knockbackForceY = 0f;
        }

        if (knockbackDuration < 0f)
        {
            knockbackDuration = 0f;
        }

        if (blinkInterval <= 0.01f)
        {
            blinkInterval = 0.01f;
        }
    }
}
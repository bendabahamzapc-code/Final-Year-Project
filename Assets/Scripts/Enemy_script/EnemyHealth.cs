using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 1;

    [Header("Visual Feedback")]
    [SerializeField] private EnemyDamageVisual damageVisual;

    [Header("Death")]
    [SerializeField] private EnemyDeathController deathController;

    private int currentHealth;
    private bool isDead;

    private void Awake()
    {
        if (damageVisual == null)
        {
            damageVisual =
                GetComponent<EnemyDamageVisual>();
        }

        if (deathController == null)
        {
            deathController =
                GetComponent<EnemyDeathController>();
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    // =====================================================
    // OLD VERSION / COMPATIBILITY
    // =====================================================

    public void TakeDamage(int damageAmount)
    {
        /*
         * Cette version reste présente pour ne pas
         * casser d'autres scripts éventuels.
         *
         * Mais pour une bonne direction de projection,
         * utilise la version avec damageSourcePosition.
         */

        Vector2 defaultDamageSource =
            (Vector2)transform.position +
            Vector2.left;

        TakeDamage(
            damageAmount,
            defaultDamageSource
        );
    }

    // =====================================================
    // DAMAGE WITH SOURCE POSITION
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

        if (damageAmount <= 0)
        {
            return;
        }

        currentHealth -=
            damageAmount;

        if (damageVisual != null)
        {
            damageVisual.PlayDamageFlash();
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;

            Die(
                damageSourcePosition
            );
        }
    }

    // =====================================================
    // DEATH
    // =====================================================

    private void Die(
        Vector2 damageSourcePosition
    )
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        // =====================================================
        // DESTRUCTIBLE
        // =====================================================

        /*
         * Les destructibles gardent leur
         * fonctionnement actuel.
         */
        if (CompareTag("destructible"))
        {
            if (
                PlayerSFXController.Instance != null
            )
            {
                PlayerSFXController.Instance
                    .PlayDestructibleDestroyed();
            }

            Destroy(gameObject);
            return;
        }

        // =====================================================
        // ENEMY DEATH
        // =====================================================

        if (deathController != null)
        {
            deathController.StartDeath(
                damageSourcePosition
            );

            return;
        }

        /*
         * Sécurité :
         * si aucun EnemyDeathController n'est présent,
         * comportement classique.
         */
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        if (maxHealth < 1)
        {
            maxHealth = 1;
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDeathController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("Mets ici uniquement le GameObject visuel contenant le SpriteRenderer.")]
    [SerializeField] private Transform visualTransform;

    [Header("Death Launch")]
    [SerializeField] private float horizontalLaunchForce = 6f;
    [SerializeField] private float verticalLaunchForce = 5f;

    [Header("Death Physics")]
    [SerializeField] private float deathGravityScale = 3f;

    [Tooltip("Utile notamment pour les ennemis volants.")]
    [SerializeField] private bool forceDynamicBodyOnDeath = true;

    [Header("Death Rotation")]
    [SerializeField] private float deathRotationZ = 90f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Death Effect")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private Transform deathEffectSpawnPoint;

    [Header("Disable Behaviours On Death")]
    [Tooltip("IA, patrouille, mouvement, vol, etc.")]
    [SerializeField] private MonoBehaviour[] behavioursToDisable;

    private EnemyDamage enemyDamage;

    private bool deathStarted;
    private bool touchedGround;

    public bool DeathStarted => deathStarted;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        enemyDamage =
            GetComponent<EnemyDamage>();

        if (visualTransform == null)
        {
            visualTransform = transform;
        }
    }

    public void StartDeath(Vector2 damageSourcePosition)
    {
        if (deathStarted)
        {
            return;
        }

        deathStarted = true;
        touchedGround = false;

        // =====================================================
        // DISABLE DAMAGE
        // =====================================================

        /*
         * IMPORTANT :
         *
         * On désactive le script de dégâts,
         * PAS le collider.
         *
         * Le collider doit rester actif pour
         * permettre à l'ennemi de toucher le sol.
         */
        if (enemyDamage != null)
        {
            enemyDamage.enabled = false;
        }

        // =====================================================
        // DISABLE AI / MOVEMENT
        // =====================================================

        if (behavioursToDisable != null)
        {
            foreach (
                MonoBehaviour behaviour
                in behavioursToDisable
            )
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour == this)
                {
                    continue;
                }

                if (behaviour == enemyDamage)
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }

        // =====================================================
        // DEATH PHYSICS
        // =====================================================

        if (rb != null)
        {
            if (forceDynamicBodyOnDeath)
            {
                rb.bodyType =
                    RigidbodyType2D.Dynamic;
            }

            rb.gravityScale =
                deathGravityScale;

            rb.linearVelocity =
                Vector2.zero;

            rb.angularVelocity =
                0f;
        }

        // =====================================================
        // ROTATE VISUAL
        // =====================================================

        if (visualTransform != null)
        {
            Vector3 rotation =
                visualTransform.localEulerAngles;

            rotation.z =
                deathRotationZ;

            visualTransform.localEulerAngles =
                rotation;
        }

        // =====================================================
        // LAUNCH
        // =====================================================

        if (rb == null)
        {
            return;
        }

        float launchDirection =
            transform.position.x >=
            damageSourcePosition.x
                ? 1f
                : -1f;

        Vector2 deathForce =
            new Vector2(
                launchDirection *
                horizontalLaunchForce,

                verticalLaunchForce
            );

        rb.AddForce(
            deathForce,
            ForceMode2D.Impulse
        );
    }

    // =====================================================
    // GROUND COLLISION
    // =====================================================

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        if (!deathStarted)
        {
            return;
        }

        if (touchedGround)
        {
            return;
        }

        int collisionLayer =
            1 << collision.gameObject.layer;

        if (
            (groundLayer.value &
             collisionLayer) == 0
        )
        {
            return;
        }

        /*
         * À partir du moment où l'ennemi mort
         * entre réellement en collision avec
         * une surface Ground, il disparaît.
         *
         * Ici on ne dépend plus de l'orientation
         * de la normale du contact.
         */
        LandOnGround();
    }

    // =====================================================
    // FINAL DEATH
    // =====================================================

    private void LandOnGround()
    {
        if (touchedGround)
        {
            return;
        }

        touchedGround = true;

        if (deathEffectPrefab != null)
        {
            Vector3 spawnPosition =
                deathEffectSpawnPoint != null
                    ? deathEffectSpawnPoint.position
                    : transform.position;

            Instantiate(
                deathEffectPrefab,
                spawnPosition,
                Quaternion.identity
            );
        }

        Destroy(gameObject);
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private void OnValidate()
    {
        if (horizontalLaunchForce < 0f)
        {
            horizontalLaunchForce = 0f;
        }

        if (verticalLaunchForce < 0f)
        {
            verticalLaunchForce = 0f;
        }

        if (deathGravityScale < 0f)
        {
            deathGravityScale = 0f;
        }
    }
}
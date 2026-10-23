using UnityEngine;

public class LandingDustEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Dust Sprites")]
    [SerializeField] private Sprite leftDustSprite;
    [SerializeField] private Sprite rightDustSprite;

    [Header("Spawn Position")]
    [SerializeField] private float horizontalSpawnOffset = 0.25f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float upwardMovement = 0.15f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 0.3f;

    public void Play()
    {
        if (dustPrefab == null)
        {
            return;
        }

        Vector3 centerPosition =
            spawnPoint != null
                ? spawnPoint.position
                : transform.position;

        // Poussière gauche
        SpawnDust(
            centerPosition +
            Vector3.left * horizontalSpawnOffset,

            new Vector2(
                -1f,
                upwardMovement
            ),

            leftDustSprite
        );

        // Poussière droite
        SpawnDust(
            centerPosition +
            Vector3.right * horizontalSpawnOffset,

            new Vector2(
                1f,
                upwardMovement
            ),

            rightDustSprite
        );
    }

    private void SpawnDust(
        Vector3 position,
        Vector2 direction,
        Sprite sprite
    )
    {
        GameObject dust =
            Instantiate(
                dustPrefab,
                position,
                Quaternion.identity
            );

        // Change le sprite selon le côté.
        SpriteRenderer spriteRenderer =
            dust.GetComponent<SpriteRenderer>();

        if (
            spriteRenderer != null &&
            sprite != null
        )
        {
            spriteRenderer.sprite = sprite;
        }

        LandingDustParticle particle =
            dust.GetComponent<LandingDustParticle>();

        if (particle != null)
        {
            particle.Initialize(
                direction,
                moveSpeed,
                lifetime
            );
        }
    }

    private void OnValidate()
    {
        if (horizontalSpawnOffset < 0f)
        {
            horizontalSpawnOffset = 0f;
        }

        if (moveSpeed < 0f)
        {
            moveSpeed = 0f;
        }

        if (lifetime <= 0.01f)
        {
            lifetime = 0.01f;
        }
    }
}
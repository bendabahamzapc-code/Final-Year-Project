using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LandingDustParticle : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private Vector2 moveDirection;
    private float moveSpeed;
    private float lifetime;

    private float timer;
    private Color startColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startColor = spriteRenderer.color;
    }

    public void Initialize(
        Vector2 direction,
        float speed,
        float duration
    )
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        lifetime = duration;

        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        transform.position +=
            (Vector3)(
                moveDirection *
                moveSpeed *
                Time.deltaTime
            );

        float progress =
            Mathf.Clamp01(
                timer / lifetime
            );

        Color color =
            startColor;

        color.a =
            Mathf.Lerp(
                startColor.a,
                0f,
                progress
            );

        spriteRenderer.color =
            color;

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
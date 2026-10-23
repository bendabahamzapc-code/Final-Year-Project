using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damageAmount = 1;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject objectTouched)
    {
        PlayerHealth playerHealth = objectTouched.GetComponent<PlayerHealth>();

        if (playerHealth == null) return;

        playerHealth.TakeDamage(damageAmount, transform.position);
    }
}
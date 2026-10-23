using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    private int damage = 1;

    private readonly HashSet<GameObject>
        alreadyHitObjects =
            new HashSet<GameObject>();

    private PlayerSFXController playerSFX;

    private Transform playerTransform;

    private void Awake()
    {
        playerSFX =
            GetComponentInParent<PlayerSFXController>();

        /*
         * La racine correspond normalement
         * au Player.
         */
        playerTransform =
            transform.root;
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void ResetAlreadyHitObjects()
    {
        alreadyHitObjects.Clear();
    }

    private void OnTriggerEnter2D(
        Collider2D collision
    )
    {
        TryDamageObject(
            collision.gameObject
        );
    }

    private void OnTriggerStay2D(
        Collider2D collision
    )
    {
        TryDamageObject(
            collision.gameObject
        );
    }

    private void TryDamageObject(
        GameObject objectHit
    )
    {
        if (
            alreadyHitObjects.Contains(
                objectHit
            )
        )
        {
            return;
        }

        bool isEnemy =
            objectHit.CompareTag(
                "enemi"
            );

        bool isDestructible =
            objectHit.CompareTag(
                "destructible"
            );

        if (
            !isEnemy &&
            !isDestructible
        )
        {
            return;
        }

        alreadyHitObjects.Add(
            objectHit
        );

        EnemyHealth enemyHealth =
            objectHit.GetComponent<EnemyHealth>();

        if (enemyHealth == null)
        {
            Debug.Log(
                objectHit.name +
                " touched by attack, but has no EnemyHealth script."
            );

            return;
        }

        Vector2 damageSourcePosition;

        if (playerTransform != null)
        {
            damageSourcePosition =
                playerTransform.position;
        }
        else
        {
            damageSourcePosition =
                transform.position;
        }

        enemyHealth.TakeDamage(
            damage,
            damageSourcePosition
        );

        if (
            isEnemy &&
            playerSFX != null
        )
        {
            playerSFX.RegisterEnemyHit();
        }
    }
}
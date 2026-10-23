using UnityEngine;

public class EnemyRespawner : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Camera Detection")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float cameraMargin = 0.1f;

    [Header("Respawn Settings")]
    [SerializeField] private bool spawnEnemyOnStart = true;
    [SerializeField] private bool respawnOnlyWhenSpawnPointIsOffScreen = true;

    private GameObject currentEnemy;
    private bool enemyHasBeenSpawnedOnce;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        if (spawnEnemyOnStart)
        {
            SpawnEnemy();
        }
    }

    private void Update()
    {
        if (currentEnemy != null) return;

        if (respawnOnlyWhenSpawnPointIsOffScreen && IsSpawnPointVisible())
        {
            return;
        }

        if (enemyHasBeenSpawnedOnce)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy prefab is missing on " + gameObject.name);
            return;
        }

        currentEnemy = Instantiate(
            enemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        enemyHasBeenSpawnedOnce = true;
    }

    private bool IsSpawnPointVisible()
    {
        if (targetCamera == null) return false;

        Vector3 viewportPosition = targetCamera.WorldToViewportPoint(spawnPoint.position);

        bool isInFrontOfCamera = viewportPosition.z > 0f;

        bool isInsideHorizontalView =
            viewportPosition.x > -cameraMargin &&
            viewportPosition.x < 1f + cameraMargin;

        bool isInsideVerticalView =
            viewportPosition.y > -cameraMargin &&
            viewportPosition.y < 1f + cameraMargin;

        return isInFrontOfCamera && isInsideHorizontalView && isInsideVerticalView;
    }

    private void OnDrawGizmosSelected()
    {
        Transform point = spawnPoint != null ? spawnPoint : transform;

        Gizmos.DrawWireSphere(point.position, 0.25f);
        Gizmos.DrawLine(point.position + Vector3.left * 0.5f, point.position + Vector3.right * 0.5f);
        Gizmos.DrawLine(point.position + Vector3.up * 0.5f, point.position + Vector3.down * 0.5f);
    }
}
using UnityEngine;

public class MiniBossCameraSpawner : MonoBehaviour
{
    [Header("Boss")]
    [Tooltip("Prefab du mini-boss à faire apparaître.")]
    [SerializeField] private GameObject miniBossPrefab;

    [Tooltip(
        "Mini-boss déjà présent dans la scène. " +
        "Laisse vide si le spawner doit le créer lui-même."
    )]
    [SerializeField] private GameObject initialMiniBoss;

    [Tooltip("Parent optionnel du mini-boss créé.")]
    [SerializeField] private Transform bossParent;

    [Header("Spawn Point")]
    [Tooltip("Point exact où le mini-boss doit apparaître.")]
    [SerializeField] private Transform spawnPoint;

    [Header("Camera")]
    [Tooltip("Caméra utilisée par le joueur.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip(
        "Petite marge autour de la caméra. " +
        "0 signifie que le point doit réellement être dans l’écran."
    )]
    [SerializeField] private float viewportMargin = 0f;

    [Header("Spawn Settings")]
    [Tooltip(
        "Le mini-boss apparaît seulement quand le Spawn Point " +
        "est visible par la caméra."
    )]
    [SerializeField] private bool spawnOnlyWhenPointIsVisible = true;

    [Tooltip("Temps avant de recréer le boss après sa disparition.")]
    [SerializeField] private float respawnDelay = 0.25f;

    [Header("Despawn Settings")]
    [Tooltip(
        "Temps pendant lequel le boss doit rester hors caméra " +
        "avant de disparaître."
    )]
    [SerializeField] private float offScreenDurationBeforeDespawn = 0.15f;

    [Tooltip(
        "Temps après son apparition pendant lequel le boss " +
        "ne peut pas être supprimé."
    )]
    [SerializeField] private float spawnProtectionDuration = 0.25f;

    [Header("Death Settings")]
    [Tooltip(
        "Active cette option si le boss doit aussi réapparaître " +
        "après avoir été tué."
    )]
    [SerializeField] private bool respawnAfterDeath = false;

    [SerializeField] private float respawnDelayAfterDeath = 3f;

    [Header("Debug")]
    [SerializeField] private bool drawSpawnPoint = true;
    [SerializeField] private bool bossIsCurrentlyVisible;

    private GameObject currentMiniBoss;

    private float offScreenTimer;
    private float spawnProtectionTimer;
    private float respawnTimer;

    private bool hasSpawnedAtLeastOnce;
    private bool removedBecauseOffScreen;
    private bool deathDetected;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        if (initialMiniBoss != null)
        {
            currentMiniBoss = initialMiniBoss;
            hasSpawnedAtLeastOnce = true;
            spawnProtectionTimer = spawnProtectionDuration;
        }
    }

    private void Start()
    {
        if (currentMiniBoss != null)
        {
            return;
        }

        if (!spawnOnlyWhenPointIsVisible || IsSpawnPointVisible())
        {
            SpawnMiniBoss();
        }
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;

            if (targetCamera == null)
            {
                return;
            }
        }

        if (currentMiniBoss != null)
        {
            UpdateExistingBoss();
        }
        else
        {
            UpdateMissingBoss();
        }
    }

    private void UpdateExistingBoss()
    {
        if (spawnProtectionTimer > 0f)
        {
            spawnProtectionTimer -= Time.deltaTime;
        }

        bossIsCurrentlyVisible =
            IsGameObjectVisible(currentMiniBoss);

        if (bossIsCurrentlyVisible)
        {
            offScreenTimer = 0f;
            return;
        }

        if (spawnProtectionTimer > 0f)
        {
            return;
        }

        offScreenTimer += Time.deltaTime;

        if (
            offScreenTimer >=
            offScreenDurationBeforeDespawn
        )
        {
            RemoveBossBecauseOffScreen();
        }
    }

    private void UpdateMissingBoss()
    {
        bossIsCurrentlyVisible = false;

        /*
         * Le boss existait, mais il n’a pas été supprimé par
         * le système de caméra : il a probablement été tué.
         */
        if (
            hasSpawnedAtLeastOnce &&
            !removedBecauseOffScreen &&
            !deathDetected
        )
        {
            deathDetected = true;

            if (respawnAfterDeath)
            {
                respawnTimer = respawnDelayAfterDeath;
            }
        }

        bool bossCanRespawn =
            removedBecauseOffScreen ||
            (respawnAfterDeath && deathDetected);

        if (!hasSpawnedAtLeastOnce)
        {
            bossCanRespawn = true;
        }

        if (!bossCanRespawn)
        {
            return;
        }

        if (respawnTimer > 0f)
        {
            respawnTimer -= Time.deltaTime;
            return;
        }

        if (
            spawnOnlyWhenPointIsVisible &&
            !IsSpawnPointVisible()
        )
        {
            return;
        }

        SpawnMiniBoss();
    }

    private void SpawnMiniBoss()
    {
        if (miniBossPrefab == null)
        {
            Debug.LogWarning(
                "MiniBossCameraSpawner : aucun prefab de mini-boss assigné.",
                this
            );

            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning(
                "MiniBossCameraSpawner : aucun Spawn Point assigné.",
                this
            );

            return;
        }

        currentMiniBoss = Instantiate(
            miniBossPrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            bossParent
        );

        hasSpawnedAtLeastOnce = true;
        removedBecauseOffScreen = false;
        deathDetected = false;

        offScreenTimer = 0f;
        respawnTimer = 0f;
        spawnProtectionTimer = spawnProtectionDuration;
    }

    private void RemoveBossBecauseOffScreen()
    {
        if (currentMiniBoss == null)
        {
            return;
        }

        removedBecauseOffScreen = true;
        deathDetected = false;

        respawnTimer = respawnDelay;
        offScreenTimer = 0f;

        Destroy(currentMiniBoss);
        currentMiniBoss = null;
    }

    private bool IsSpawnPointVisible()
    {
        if (spawnPoint == null || targetCamera == null)
        {
            return false;
        }

        return IsWorldPositionVisible(
            spawnPoint.position
        );
    }

    private bool IsWorldPositionVisible(
        Vector3 worldPosition
    )
    {
        Vector3 viewportPosition =
            targetCamera.WorldToViewportPoint(
                worldPosition
            );

        if (viewportPosition.z <= 0f)
        {
            return false;
        }

        return
            viewportPosition.x >= -viewportMargin &&
            viewportPosition.x <= 1f + viewportMargin &&
            viewportPosition.y >= -viewportMargin &&
            viewportPosition.y <= 1f + viewportMargin;
    }

    private bool IsGameObjectVisible(
        GameObject objectToCheck
    )
    {
        if (
            objectToCheck == null ||
            targetCamera == null
        )
        {
            return false;
        }

        Renderer[] renderers =
            objectToCheck.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return IsWorldPositionVisible(
                objectToCheck.transform.position
            );
        }

        bool boundsInitialized = false;
        Bounds combinedBounds = new Bounds();

        foreach (Renderer currentRenderer in renderers)
        {
            if (
                currentRenderer == null ||
                !currentRenderer.enabled ||
                !currentRenderer.gameObject.activeInHierarchy
            )
            {
                continue;
            }

            if (!boundsInitialized)
            {
                combinedBounds = currentRenderer.bounds;
                boundsInitialized = true;
            }
            else
            {
                combinedBounds.Encapsulate(
                    currentRenderer.bounds
                );
            }
        }

        if (!boundsInitialized)
        {
            return IsWorldPositionVisible(
                objectToCheck.transform.position
            );
        }

        Plane[] cameraPlanes =
            GeometryUtility.CalculateFrustumPlanes(
                targetCamera
            );

        return GeometryUtility.TestPlanesAABB(
            cameraPlanes,
            combinedBounds
        );
    }

    private void OnValidate()
    {
        viewportMargin =
            Mathf.Max(0f, viewportMargin);

        respawnDelay =
            Mathf.Max(0f, respawnDelay);

        offScreenDurationBeforeDespawn =
            Mathf.Max(
                0f,
                offScreenDurationBeforeDespawn
            );

        spawnProtectionDuration =
            Mathf.Max(
                0f,
                spawnProtectionDuration
            );

        respawnDelayAfterDeath =
            Mathf.Max(
                0f,
                respawnDelayAfterDeath
            );
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawSpawnPoint)
        {
            return;
        }

        Vector3 position =
            spawnPoint != null
                ? spawnPoint.position
                : transform.position;

        Gizmos.DrawWireSphere(
            position,
            0.25f
        );

        Gizmos.DrawLine(
            position + Vector3.left * 0.4f,
            position + Vector3.right * 0.4f
        );

        Gizmos.DrawLine(
            position + Vector3.down * 0.4f,
            position + Vector3.up * 0.4f
        );
    }
}
using System;
using UnityEngine;

public class GrappleSpriteSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer ropeRenderer;
    [SerializeField] private SpriteRenderer anchorRenderer;

    [Header("Anchor Movement")]
    [SerializeField] private float anchorLaunchSpeed = 15f;
    [SerializeField] private float maximumDistance = 10f;
    [SerializeField] private float anchorHitRadius = 0.12f;

    [Header("Collision")]
    [SerializeField] private LayerMask grappleCollisionLayers;
    [SerializeField] private string grapplableTag = "agripable";

    [Tooltip(
        "Si activé, toucher un objet non agrippable arrête aussi l’ancre."
    )]
    [SerializeField] private bool stopOnNonGrapplableObject = true;

    [Header("Rope")]
    [Tooltip("Épaisseur du sprite de corde.")]
    [SerializeField] private float ropeThickness = 0.15f;

    [Tooltip(
        "Le sprite de corde doit être dessiné horizontalement, " +
        "de gauche à droite."
    )]
    [SerializeField] private bool useTiledRope = true;

    [Header("Anchor Visual")]
    [SerializeField] private float anchorRotationOffset = 0f;

    [Tooltip(
        "Active si l’ancre doit regarder dans la direction de son déplacement."
    )]
    [SerializeField] private bool rotateAnchorWithDirection = true;

    [Header("Preview Visual")]
    [SerializeField] private Color previewRopeColor =
        new Color(1f, 1f, 1f, 0.45f);

    [SerializeField] private Color previewAnchorColor =
        new Color(1f, 1f, 1f, 0.6f);

    [Header("Launched Visual")]
    [SerializeField] private Color launchedRopeColor = Color.white;
    [SerializeField] private Color launchedAnchorColor = Color.white;

    [Header("Setup")]
    [Tooltip(
        "Détache les visuels du joueur au lancement afin que " +
        "l’ancre ne suive pas automatiquement ses mouvements."
    )]
    [SerializeField] private bool detachVisualRootOnStart = true;

    private Vector2 anchorPosition;
    private Vector2 launchStartPosition;
    private Vector2 launchDirection;

    private bool previewIsVisible;
    private bool anchorIsFlying;
    private bool anchorIsAttached;
    private bool visualsAreVisible;

    public event Action<Vector2, Collider2D> OnAnchorAttached;
    public event Action OnAnchorFailed;

    public bool IsFlying => anchorIsFlying;
    public bool IsAttached => anchorIsAttached;
    public Vector2 AnchorPosition => anchorPosition;

    private void Awake()
    {
        if (firePoint == null)
        {
            firePoint = transform;
        }

        if (
            detachVisualRootOnStart &&
            visualRoot != null
        )
        {
            visualRoot.SetParent(null, true);
        }

        if (ropeRenderer != null && useTiledRope)
        {
            ropeRenderer.drawMode = SpriteDrawMode.Tiled;
        }

        HideVisuals();
    }

    private void FixedUpdate()
    {
        if (!anchorIsFlying)
        {
            return;
        }

        MoveAnchor();
    }

    private void LateUpdate()
    {
        if (!visualsAreVisible)
        {
            return;
        }

        UpdateRopeAndAnchorVisuals();
    }

    public void ShowPreview(Vector2 direction)
    {
        if (anchorIsFlying || anchorIsAttached)
        {
            return;
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        previewIsVisible = true;
        visualsAreVisible = true;

        launchDirection = direction.normalized;

        Vector2 startPosition = firePoint.position;

        anchorPosition = GetPreviewEndPosition(
            startPosition,
            launchDirection
        );

        SetVisualObjectsActive(true);

        if (ropeRenderer != null)
        {
            ropeRenderer.color = previewRopeColor;
        }

        if (anchorRenderer != null)
        {
            anchorRenderer.color = previewAnchorColor;
        }

        UpdateRopeAndAnchorVisuals();
    }

    public void HidePreview()
    {
        if (!previewIsVisible)
        {
            return;
        }

        previewIsVisible = false;

        if (!anchorIsFlying && !anchorIsAttached)
        {
            HideVisuals();
        }
    }

    public void Launch(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            FailGrapple();
            return;
        }

        previewIsVisible = false;
        anchorIsFlying = true;
        anchorIsAttached = false;
        visualsAreVisible = true;

        launchDirection = direction.normalized;
        launchStartPosition = firePoint.position;
        anchorPosition = launchStartPosition;

        SetVisualObjectsActive(true);

        if (ropeRenderer != null)
        {
            ropeRenderer.color = launchedRopeColor;
        }

        if (anchorRenderer != null)
        {
            anchorRenderer.color = launchedAnchorColor;
        }

        UpdateRopeAndAnchorVisuals();
    }

    private void MoveAnchor()
    {
        float movementDistance =
            anchorLaunchSpeed * Time.fixedDeltaTime;

        RaycastHit2D hit = Physics2D.CircleCast(
            anchorPosition,
            anchorHitRadius,
            launchDirection,
            movementDistance,
            grappleCollisionLayers
        );

        if (hit.collider != null)
        {
            anchorPosition = hit.point;

            if (hit.collider.CompareTag(grapplableTag))
            {
                AttachAnchor(hit.point, hit.collider);
                return;
            }

            if (stopOnNonGrapplableObject)
            {
                FailGrapple();
                return;
            }
        }

        anchorPosition +=
            launchDirection * movementDistance;

        float travelledDistance = Vector2.Distance(
            launchStartPosition,
            anchorPosition
        );

        if (travelledDistance >= maximumDistance)
        {
            anchorPosition =
                launchStartPosition +
                launchDirection * maximumDistance;

            FailGrapple();
        }
    }

    private void AttachAnchor(
        Vector2 attachmentPoint,
        Collider2D targetCollider
    )
    {
        anchorPosition = attachmentPoint;

        anchorIsFlying = false;
        anchorIsAttached = true;
        visualsAreVisible = true;

        UpdateRopeAndAnchorVisuals();

        OnAnchorAttached?.Invoke(
            attachmentPoint,
            targetCollider
        );
    }

    private void FailGrapple()
    {
        anchorIsFlying = false;
        anchorIsAttached = false;
        previewIsVisible = false;

        HideVisuals();

        OnAnchorFailed?.Invoke();
    }

    public void CancelGrappleVisual()
    {
        anchorIsFlying = false;
        anchorIsAttached = false;
        previewIsVisible = false;

        HideVisuals();
    }

    public void SetAttachedPosition(Vector2 newPosition)
    {
        anchorPosition = newPosition;

        anchorIsFlying = false;
        anchorIsAttached = true;
        visualsAreVisible = true;

        SetVisualObjectsActive(true);
        UpdateRopeAndAnchorVisuals();
    }

    private Vector2 GetPreviewEndPosition(
        Vector2 startPosition,
        Vector2 direction
    )
    {
        RaycastHit2D hit = Physics2D.CircleCast(
            startPosition,
            anchorHitRadius,
            direction,
            maximumDistance,
            grappleCollisionLayers
        );

        if (hit.collider != null)
        {
            return hit.point;
        }

        return startPosition +
               direction * maximumDistance;
    }

    private void UpdateRopeAndAnchorVisuals()
    {
        if (firePoint == null)
        {
            return;
        }

        Vector2 startPosition = firePoint.position;
        Vector2 difference =
            anchorPosition - startPosition;

        float distance = difference.magnitude;

        if (distance <= 0.001f)
        {
            return;
        }

        Vector2 direction = difference.normalized;

        UpdateRopeVisual(
            startPosition,
            anchorPosition,
            direction,
            distance
        );

        UpdateAnchorVisual(
            anchorPosition,
            direction
        );
    }

    private void UpdateRopeVisual(
        Vector2 startPosition,
        Vector2 endPosition,
        Vector2 direction,
        float distance
    )
    {
        if (ropeRenderer == null)
        {
            return;
        }

        Vector2 middlePosition =
            (startPosition + endPosition) * 0.5f;

        ropeRenderer.transform.position =
            new Vector3(
                middlePosition.x,
                middlePosition.y,
                ropeRenderer.transform.position.z
            );

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        ropeRenderer.transform.rotation =
            Quaternion.Euler(0f, 0f, angle);

        if (useTiledRope)
        {
            ropeRenderer.size = new Vector2(
                distance,
                ropeThickness
            );
        }
        else
        {
            Vector3 scale =
                ropeRenderer.transform.localScale;

            scale.x = distance;
            scale.y = ropeThickness;

            ropeRenderer.transform.localScale = scale;
        }
    }

    private void UpdateAnchorVisual(
        Vector2 position,
        Vector2 direction
    )
    {
        if (anchorRenderer == null)
        {
            return;
        }

        anchorRenderer.transform.position =
            new Vector3(
                position.x,
                position.y,
                anchorRenderer.transform.position.z
            );

        if (!rotateAnchorWithDirection)
        {
            return;
        }

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        anchorRenderer.transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle + anchorRotationOffset
            );
    }

    private void SetVisualObjectsActive(bool active)
    {
        if (ropeRenderer != null)
        {
            ropeRenderer.gameObject.SetActive(active);
        }

        if (anchorRenderer != null)
        {
            anchorRenderer.gameObject.SetActive(active);
        }
    }

    private void HideVisuals()
    {
        visualsAreVisible = false;

        SetVisualObjectsActive(false);
    }

    private void OnDisable()
    {
        CancelGrappleVisual();
    }

    private void OnValidate()
    {
        anchorLaunchSpeed =
            Mathf.Max(0f, anchorLaunchSpeed);

        maximumDistance =
            Mathf.Max(0.1f, maximumDistance);

        anchorHitRadius =
            Mathf.Max(0f, anchorHitRadius);

        ropeThickness =
            Mathf.Max(0.01f, ropeThickness);
    }

    private void OnDrawGizmosSelected()
    {
        if (!anchorIsFlying && !anchorIsAttached)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            anchorPosition,
            anchorHitRadius
        );
    }
}
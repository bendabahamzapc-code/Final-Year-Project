using UnityEngine;

public class CameraScreenController : MonoBehaviour
{
    public enum CameraMode
    {
        ScreenBased,
        HorizontalFollow,
        Frozen
    }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Camera Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private bool instantMove = false;

    [Header("Horizontal Follow")]
    [SerializeField] private float followSmoothSpeed = 8f;
    [SerializeField] private float horizontalOffset = 0f;
    [SerializeField] private float minFollowX = -10f;
    [SerializeField] private float maxFollowX = 10f;
    [SerializeField] private float leftDeadZone = 2f;
    [SerializeField] private float rightDeadZone = 2f;

    [Header("Slow Motion During Screen Transition")]
    [SerializeField] private bool slowMotionDuringTransition = true;
    [SerializeField] private float transitionTimeScale = 0.1f;

    private CameraMode currentMode = CameraMode.ScreenBased;

    private Vector3 targetPosition;
    private bool isMovingToScreenTarget;

    private float fixedY;
    private float fixedZ;

    private void Start()
    {
        targetPosition = transform.position;

        fixedY = transform.position.y;
        fixedZ = transform.position.z;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (currentMode == CameraMode.HorizontalFollow)
        {
            FollowPlayerHorizontally();
            return;
        }

        if (currentMode == CameraMode.Frozen)
        {
            return;
        }

        if (currentMode == CameraMode.ScreenBased)
        {
            MoveToScreenTarget();
        }
    }

    private void MoveToScreenTarget()
    {
        if (!isMovingToScreenTarget) return;

        if (instantMove)
        {
            transform.position = targetPosition;
            StopScreenTransition();
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.unscaledDeltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            StopScreenTransition();
        }
    }

    private void FollowPlayerHorizontally()
{
    if (player == null) return;

    float cameraX = transform.position.x;
    float playerX = player.position.x + horizontalOffset;

    float leftLimit = cameraX - leftDeadZone;
    float rightLimit = cameraX + rightDeadZone;

    float targetX = cameraX;

    if (playerX < leftLimit)
    {
        targetX = playerX + leftDeadZone;
    }
    else if (playerX > rightLimit)
    {
        targetX = playerX - rightDeadZone;
    }

    targetX = Mathf.Clamp(targetX, minFollowX, maxFollowX);

    Vector3 desiredPosition = new Vector3(
        targetX,
        fixedY,
        fixedZ
    );

    transform.position = Vector3.Lerp(
        transform.position,
        desiredPosition,
        followSmoothSpeed * Time.deltaTime
    );
}

    public void MoveCameraTo(Transform newTarget)
    {
        currentMode = CameraMode.ScreenBased;

        fixedY = newTarget.position.y;
        fixedZ = transform.position.z;

        targetPosition = new Vector3(
            newTarget.position.x,
            newTarget.position.y,
            transform.position.z
        );

        StartScreenTransition();
    }

    public void StartHorizontalFollow(float minX, float maxX)
    {
        currentMode = CameraMode.HorizontalFollow;

        minFollowX = minX;
        maxFollowX = maxX;

        fixedY = transform.position.y;
        fixedZ = transform.position.z;

        isMovingToScreenTarget = false;

        if (slowMotionDuringTransition)
        {
            Time.timeScale = 1f;
        }
    }

    public void StartHorizontalFollow(float minX, float maxX, float newFixedY)
    {
        currentMode = CameraMode.HorizontalFollow;

        minFollowX = minX;
        maxFollowX = maxX;

        fixedY = newFixedY;
        fixedZ = transform.position.z;

        isMovingToScreenTarget = false;

        if (slowMotionDuringTransition)
        {
            Time.timeScale = 1f;
        }
    }

    public void FreezeCamera()
    {
        currentMode = CameraMode.Frozen;
        isMovingToScreenTarget = false;

        if (slowMotionDuringTransition)
        {
            Time.timeScale = 1f;
        }
    }

    public void ReturnToScreenBased()
    {
        currentMode = CameraMode.ScreenBased;
    }

    private void StartScreenTransition()
    {
        isMovingToScreenTarget = true;

        if (slowMotionDuringTransition)
        {
            Time.timeScale = transitionTimeScale;
        }
    }

    private void StopScreenTransition()
    {
        isMovingToScreenTarget = false;

        if (slowMotionDuringTransition)
        {
            Time.timeScale = 1f;
        }
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}
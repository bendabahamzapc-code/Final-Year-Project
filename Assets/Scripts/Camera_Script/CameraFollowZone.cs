using UnityEngine;

public class CameraFollowZone : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private CameraScreenController cameraController;

    [Header("Follow Limits")]
    [SerializeField] private float minCameraX;
    [SerializeField] private float maxCameraX;

    [Header("Vertical Lock")]
    [SerializeField] private bool useCurrentCameraY = true;
    [SerializeField] private float fixedCameraY;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag)) return;

        if (cameraController == null)
        {
            Debug.LogWarning("Camera Controller is missing on " + gameObject.name);
            return;
        }

        if (useCurrentCameraY)
        {
            cameraController.StartHorizontalFollow(minCameraX, maxCameraX);
        }
        else
        {
            cameraController.StartHorizontalFollow(minCameraX, maxCameraX, fixedCameraY);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 leftLimit = new Vector3(minCameraX, transform.position.y, 0f);
        Vector3 rightLimit = new Vector3(maxCameraX, transform.position.y, 0f);

        Gizmos.DrawWireSphere(leftLimit, 0.3f);
        Gizmos.DrawWireSphere(rightLimit, 0.3f);
        Gizmos.DrawLine(leftLimit, rightLimit);
    }
}
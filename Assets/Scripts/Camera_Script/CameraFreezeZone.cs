using UnityEngine;

public class CameraFreezeZone : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private CameraScreenController cameraController;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag)) return;
        if (hasTriggered) return;

        hasTriggered = true;

        if (cameraController == null)
        {
            Debug.LogWarning("Camera Controller is missing on " + gameObject.name);
            return;
        }

        cameraController.FreezeCamera();
    }
}
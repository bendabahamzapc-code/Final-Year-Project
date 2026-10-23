using UnityEngine;

public class CameraTransitionTrigger : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private CameraScreenController cameraController;

    [Header("Target Screen")]
    [SerializeField] private Transform cameraTarget;

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

        if (cameraTarget == null)
        {
            Debug.LogWarning("Camera Target is missing on " + gameObject.name);
            return;
        }

        cameraController.MoveCameraTo(cameraTarget);
    }
}
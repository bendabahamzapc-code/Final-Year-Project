using UnityEngine;

public class DestroyOnAnimationEnd : MonoBehaviour
{
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
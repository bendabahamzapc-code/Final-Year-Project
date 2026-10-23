using System.Collections;
using UnityEngine;

public class EnemyDamageVisual : MonoBehaviour
{
    [Header("Sprite References")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("Damage Flash")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float damageColorDuration = 0.1f;

    private Color[] originalColors;
    private Coroutine damageFlashCoroutine;

    private void Awake()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }
    }

    public void PlayDamageFlash()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }

        damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    private IEnumerator DamageFlashCoroutine()
    {
        SetSpriteColor(damageColor);

        yield return new WaitForSeconds(damageColorDuration);

        RestoreOriginalColors();
        damageFlashCoroutine = null;
    }

    private void SetSpriteColor(Color newColor)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = newColor;
            }
        }
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = originalColors[i];
            }
        }
    }

    private void OnDisable()
    {
        RestoreOriginalColors();
    }

    private void OnValidate()
    {
        if (damageColorDuration < 0.01f)
        {
            damageColorDuration = 0.01f;
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform healthContainer;

    [Header("Heart Images")]
    [SerializeField] private Image healthIconPrefab;
    [SerializeField] private Sprite fullHealthSprite;
    [SerializeField] private Sprite emptyHealthSprite;

    private readonly List<Image> healthIcons = new List<Image>();

    private void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealthHUD: No PlayerHealth found.");
            return;
        }

        playerHealth.OnHealthChanged += UpdateHealthDisplay;

        CreateHealthIcons(playerHealth.MaxHealth);
        UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthDisplay;
        }
    }

    private void CreateHealthIcons(int maxHealth)
    {
        ClearExistingIcons();

        for (int i = 0; i < maxHealth; i++)
        {
            Image newIcon = Instantiate(healthIconPrefab, healthContainer);
            healthIcons.Add(newIcon);
        }
    }

    private void ClearExistingIcons()
    {
        for (int i = healthIcons.Count - 1; i >= 0; i--)
        {
            if (healthIcons[i] != null)
            {
                Destroy(healthIcons[i].gameObject);
            }
        }

        healthIcons.Clear();
    }

    private void UpdateHealthDisplay(int currentHealth, int maxHealth)
    {
        if (healthIcons.Count != maxHealth)
        {
            CreateHealthIcons(maxHealth);
        }

        for (int i = 0; i < healthIcons.Count; i++)
        {
            if (i < currentHealth)
            {
                healthIcons[i].sprite = fullHealthSprite;
            }
            else
            {
                healthIcons[i].sprite = emptyHealthSprite;
            }
        }
    }
}
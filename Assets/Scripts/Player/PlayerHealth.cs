using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invulnerabilityDuration = 1.5f;

    private int currentHealth;
    private bool isInvulnerable;
    private SpriteRenderer spriteRenderer;

    public event System.Action Died;

    public bool IsDead => currentHealth <= 0;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable || currentHealth <= 0 || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log("Player health: " + currentHealth + "/" + maxHealth);

        if (currentHealth == 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvulnerabilityCoroutine());
    }

    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;

        float elapsed = 0f;

        while (elapsed < invulnerabilityDuration)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        isInvulnerable = false;
    }

    private void Die()
    {
        Debug.Log("Player defeated");
        Died?.Invoke();
    }
}
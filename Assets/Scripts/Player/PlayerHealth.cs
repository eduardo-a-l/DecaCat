using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invulnerabilityDuration = 1.5f;

    private int currentHealth;
    private bool isInvulnerable;
    private float invulnerableUntil;
    private SpriteRenderer spriteRenderer;

    public event System.Action Died;
    public event System.Action Damaged;

    public bool IsDead => currentHealth <= 0;
    public bool IsInvulnerable => isInvulnerable || Time.time < invulnerableUntil;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int damage, DamageElement element)
    {
        if (IsInvulnerable || currentHealth <= 0 || damage <= 0)
            return;

        PlayerPower power = PlayerPower.Instance;

        if (power != null && power.TryAbsorbDamage(element))
            return;

        TakeDamage(damage);
    }

    public bool Heal(int amount)
    {
        if (currentHealth <= 0 || currentHealth >= maxHealth || amount <= 0)
            return false;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        return true;
    }

    public void GrantInvulnerability(float seconds)
    {
        if (seconds <= 0f)
            return;

        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);

        if (seconds > 0.5f)
            StartCoroutine(GhostlyCoroutine(seconds));
    }

    public void TakeDamage(int damage)
    {
        if (IsInvulnerable || currentHealth <= 0 || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log("Player health: " + currentHealth + "/" + maxHealth);

        Damaged?.Invoke();

        if (currentHealth == 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvulnerabilityCoroutine());
    }

    public void RestoreFullHealth()
    {
        StopAllCoroutines();

        isInvulnerable = false;
        invulnerableUntil = 0f;
        currentHealth = maxHealth;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;

            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }

    private IEnumerator GhostlyCoroutine(float seconds)
    {
        float end = Time.time + seconds;

        while (Time.time < end)
        {
            SetAlpha(0.55f + 0.2f * Mathf.Sin(Time.time * 14f));

            yield return null;
        }

        SetAlpha(1f);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
            return;

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
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
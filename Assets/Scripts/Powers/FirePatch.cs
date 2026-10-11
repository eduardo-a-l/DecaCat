using UnityEngine;

public class FirePatch : MonoBehaviour
{
    private const float CheckInterval = 0.2f;
    private const float FadeTime = 0.6f;

    private SpriteRenderer spriteRenderer;
    private Color color;
    private float radius;
    private float lifetime;
    private float burnSeconds;
    private float age;
    private float nextCheck;

    public static FirePatch Spawn(
        Vector2 position, float radius, float lifetime, Color color,
        float burnSeconds)
    {
        SpriteRenderer renderer = PowerFx.CreateSprite(
            "FirePatch", PowerFx.Circle, color, position,
            Vector2.one * radius * 2f, -4, PowerFx.EffectParent
        );

        FirePatch patch = renderer.gameObject.AddComponent<FirePatch>();
        patch.spriteRenderer = renderer;
        patch.color = color;
        patch.radius = radius;
        patch.lifetime = lifetime;
        patch.burnSeconds = burnSeconds;

        return patch;
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float flicker = 0.8f + 0.2f * Mathf.Sin(age * 18f + radius * 7f);
        float fade = Mathf.Clamp01((lifetime - age) / FadeTime);

        Color current = color;
        current.a = color.a * flicker * fade;
        spriteRenderer.color = current;

        if (Time.time < nextCheck)
            return;

        nextCheck = Time.time + CheckInterval;

        foreach (Enemy enemy in PowerFx.GetEnemiesInRadius(transform.position, radius))
            enemy.Burn(burnSeconds);
    }
}

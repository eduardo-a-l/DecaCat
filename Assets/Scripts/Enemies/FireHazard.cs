using UnityEngine;

public class FireHazard : MonoBehaviour
{
    private const float CheckInterval = 0.1f;
    private const float HitCooldown = 0.8f;
    private const float FadeTime = 0.4f;

    private static readonly Color OuterColor = new Color(1f, 0.4f, 0.1f, 0.4f);
    private static readonly Color InnerColor = new Color(1f, 0.8f, 0.25f, 0.65f);

    private SpriteRenderer outer;
    private SpriteRenderer inner;
    private float radius;
    private float lifetime;
    private float age;
    private float nextCheck;
    private float nextHit;
    private int damage;

    public static FireHazard Spawn(Vector2 position, float radius, float lifetime, int damage)
    {
        GameObject root = new GameObject("FireHazard");

        if (PowerFx.EffectParent != null)
            root.transform.SetParent(PowerFx.EffectParent, true);

        root.transform.position = position;

        FireHazard fire = root.AddComponent<FireHazard>();
        fire.radius = radius;
        fire.lifetime = lifetime;
        fire.damage = damage;

        fire.outer = PowerFx.CreateSprite(
            "Outer", PowerFx.Circle, OuterColor, position,
            Vector2.one * radius * 2f, -3, root.transform
        );

        fire.inner = PowerFx.CreateSprite(
            "Inner", PowerFx.Circle, InnerColor, position,
            Vector2.one * radius * 1.2f, -2, root.transform
        );

        return fire;
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float fade = Mathf.Clamp01((lifetime - age) / FadeTime);
        float flicker = 0.8f + 0.2f * Mathf.Sin(Time.time * 18f + radius * 5f);

        Color outerColor = OuterColor;
        outerColor.a = OuterColor.a * fade;
        outer.color = outerColor;

        Color innerColor = InnerColor;
        innerColor.a = InnerColor.a * fade;
        inner.color = innerColor;

        inner.transform.localScale = new Vector3(
            radius * 1.2f * flicker, radius * 1.2f * flicker, 1f
        ) / Mathf.Max(0.0001f, inner.sprite.bounds.size.x);

        if (Time.time < nextCheck || Time.time < nextHit)
            return;

        nextCheck = Time.time + CheckInterval;

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();

            if (player == null)
                continue;

            player.TakeDamage(damage, DamageElement.Fire);
            nextHit = Time.time + HitCooldown;

            return;
        }
    }
}

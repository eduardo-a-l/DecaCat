using UnityEngine;

public class ElectrifiedTile : MonoBehaviour
{
    private const float CheckInterval = 0.1f;
    private const float HitCooldown = 0.8f;
    private const float FadeTime = 0.5f;

    private static readonly Color TileColor = new Color(1f, 0.92f, 0.2f, 0.55f);

    private SpriteRenderer spriteRenderer;
    private Vector2 size;
    private float lifetime;
    private float age;
    private float nextCheck;
    private float nextHit;
    private int damage;

    public static ElectrifiedTile Spawn(Vector2 position, float size, float lifetime, int damage)
    {
        SpriteRenderer renderer = PowerFx.CreateSprite(
            "ElectrifiedTile", PowerFx.Square, TileColor, position,
            Vector2.one * size, -4, PowerFx.EffectParent
        );

        ElectrifiedTile tile = renderer.gameObject.AddComponent<ElectrifiedTile>();
        tile.spriteRenderer = renderer;
        tile.size = Vector2.one * size;
        tile.lifetime = lifetime;
        tile.damage = damage;

        return tile;
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
        float flicker = 0.75f + 0.25f * Mathf.Sin(Time.time * 35f);

        Color color = TileColor;
        color.a = TileColor.a * fade * flicker;
        spriteRenderer.color = color;

        if (Time.time < nextCheck)
            return;

        nextCheck = Time.time + CheckInterval;

        if (Time.time < nextHit)
            return;

        foreach (Collider2D hit in Physics2D.OverlapBoxAll(transform.position, size, 0f))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();

            if (player == null)
                continue;

            player.TakeDamage(damage, DamageElement.Electric);
            nextHit = Time.time + HitCooldown;

            return;
        }
    }
}

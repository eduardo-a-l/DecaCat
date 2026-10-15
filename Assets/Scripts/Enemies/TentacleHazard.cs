using UnityEngine;

public class TentacleHazard : MonoBehaviour
{
    private const float RiseSeconds = 0.3f;
    private const float RetractSeconds = 0.3f;
    private const float HitCooldown = 0.8f;
    private const float Height = 1.4f;
    private const float Width = 0.6f;

    private SpriteRenderer spriteRenderer;
    private Vector2 basePosition;
    private float activeSeconds;
    private float age;
    private float nextHit;
    private int damage;
    private float spriteHeight;

    public static TentacleHazard Spawn(
        Vector2 position, Sprite sprite, float activeSeconds, int damage)
    {
        Sprite used = sprite != null ? sprite : PowerFx.Square;

        SpriteRenderer renderer = PowerFx.CreateSprite(
            "Tentacle", used, Color.white, position,
            new Vector2(Width, Height), 2, PowerFx.EffectParent
        );

        TentacleHazard tentacle = renderer.gameObject.AddComponent<TentacleHazard>();
        tentacle.spriteRenderer = renderer;
        tentacle.basePosition = position;
        tentacle.activeSeconds = activeSeconds;
        tentacle.damage = damage;
        tentacle.spriteHeight = renderer.transform.localScale.y;

        tentacle.Apply(0f);

        return tentacle;
    }

    private void Apply(float height01)
    {
        float visible = Mathf.Max(0.02f, height01);

        transform.localScale = new Vector3(
            transform.localScale.x, spriteHeight * visible, 1f
        );

        transform.position = new Vector3(
            basePosition.x, basePosition.y + Height * 0.5f * visible - 0.1f, 0f
        );
    }

    private void Update()
    {
        age += Time.deltaTime;

        float total = RiseSeconds + activeSeconds + RetractSeconds;

        if (age >= total)
        {
            Destroy(gameObject);
            return;
        }

        float height01;

        if (age < RiseSeconds)
            height01 = age / RiseSeconds;
        else if (age > RiseSeconds + activeSeconds)
            height01 = 1f - (age - RiseSeconds - activeSeconds) / RetractSeconds;
        else
            height01 = 1f;

        Apply(height01);

        if (height01 < 0.6f || Time.time < nextHit)
            return;

        Vector2 center = basePosition + Vector2.up * (Height * 0.4f);

        foreach (Collider2D hit in
                 Physics2D.OverlapBoxAll(center, new Vector2(Width * 0.8f, Height * 0.8f), 0f))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();

            if (player == null)
                continue;

            player.TakeDamage(damage);
            nextHit = Time.time + HitCooldown;

            return;
        }
    }
}

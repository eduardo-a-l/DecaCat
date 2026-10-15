using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Vector2 velocity;
    private int damage;
    private float radius;
    private float lifetime;
    private DamageElement element;
    private bool reflected;
    private bool ignoreObstacles;
    private SpriteRenderer spriteRenderer;

    public static EnemyProjectile Spawn(
        Vector2 position, Vector2 direction, float speed, int damage,
        float size, Color color, DamageElement element = DamageElement.None)
    {
        SpriteRenderer renderer = PowerFx.CreateSprite(
            "EnemyProjectile", PowerFx.Circle, color, position,
            Vector2.one * size, 5, PowerFx.EffectParent
        );

        EnemyProjectile projectile = renderer.gameObject.AddComponent<EnemyProjectile>();
        projectile.velocity = direction.normalized * speed;
        projectile.damage = damage;
        projectile.radius = size * 0.4f;
        projectile.lifetime = 6f;
        projectile.element = element;
        projectile.spriteRenderer = renderer;

        return projectile;
    }

    public EnemyProjectile PassThroughObstacles()
    {
        ignoreObstacles = true;

        return this;
    }

    public void Redirect(Vector2 direction, float speedMultiplier, bool becomeAlly)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        velocity = direction.normalized * velocity.magnitude * speedMultiplier;
        lifetime = Mathf.Max(lifetime, 3f);

        if (!becomeAlly || reflected)
            return;

        reflected = true;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.55f, 1f, 1f, 1f);
    }

    private void Update()
    {
        float step = Time.deltaTime;

        lifetime -= step;

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 position = (Vector2)transform.position + velocity * step;

        transform.position = position;

        if (PowerFx.IsOutsideRoom(position))
        {
            Destroy(gameObject);
            return;
        }

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, radius))
        {
            if (reflected)
            {
                Enemy enemy = hit.GetComponentInParent<Enemy>();

                if (enemy != null && enemy.CurrentHealth > 0)
                {
                    enemy.TakeDamage(Mathf.Max(1, damage));
                    Destroy(gameObject);
                    return;
                }

                continue;
            }

            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();

            if (player != null)
            {
                player.TakeDamage(damage, element);
                Destroy(gameObject);
                return;
            }

            AllyMinion minion = hit.GetComponentInParent<AllyMinion>();

            if (minion != null)
            {
                minion.TakeHit(damage);
                Destroy(gameObject);
                return;
            }
        }

        int mask = ignoreObstacles ? PhysicsLayers.BorderMask : PhysicsLayers.WallMask;

        if (mask != 0 && Physics2D.OverlapCircle(position, radius, mask) != null)
            Destroy(gameObject);
    }
}

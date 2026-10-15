using UnityEngine;

public class WaveProjectile : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Vector2 origin;
    private Vector2 direction;
    private Vector2 sideways;
    private float speed;
    private float amplitude;
    private float frequency;
    private float phase;
    private bool cosine;
    private float age;
    private float lifetime;
    private float radius;
    private int damage;
    private bool free;
    private bool passThrough;
    private Vector2 velocity;

    public static WaveProjectile Spawn(
        Vector2 origin, Vector2 direction, float speed, float amplitude,
        float frequency, float phase, bool cosine, int damage, float size,
        Color color, float lifetime = 8f, bool passThrough = false)
    {
        SpriteRenderer renderer = PowerFx.CreateSprite(
            "WaveProjectile", PowerFx.Circle, color, origin,
            Vector2.one * size, 5, PowerFx.EffectParent
        );

        WaveProjectile projectile = renderer.gameObject.AddComponent<WaveProjectile>();
        projectile.spriteRenderer = renderer;
        projectile.origin = origin;
        projectile.direction = direction.normalized;
        projectile.sideways = new Vector2(-projectile.direction.y, projectile.direction.x);
        projectile.speed = speed;
        projectile.amplitude = amplitude;
        projectile.frequency = frequency;
        projectile.phase = phase;
        projectile.cosine = cosine;
        projectile.damage = damage;
        projectile.radius = size * 0.4f;
        projectile.lifetime = lifetime;
        projectile.passThrough = passThrough;

        return projectile;
    }

    public void Release(Vector2 newVelocity)
    {
        free = true;
        velocity = newVelocity;
    }

    private float Wave(float time)
    {
        float angle = frequency * time + phase;

        return cosine ? Mathf.Cos(angle) : Mathf.Sin(angle);
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 position;

        if (free)
        {
            position = (Vector2)transform.position + velocity * Time.deltaTime;
        }
        else
        {
            float offset = amplitude * (Wave(age) - Wave(0f));

            position = origin + direction * (speed * age) + sideways * offset;
        }

        transform.position = position;

        if (PowerFx.IsOutsideRoom(position))
        {
            Destroy(gameObject);
            return;
        }

        int wallMask = passThrough ? PhysicsLayers.BorderMask : PhysicsLayers.WallMask;

        if (wallMask != 0 && Physics2D.OverlapCircle(position, radius, wallMask) != null)
        {
            Destroy(gameObject);
            return;
        }

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, radius))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();

            if (player == null)
                continue;

            player.TakeDamage(damage);
            Destroy(gameObject);

            return;
        }
    }
}
